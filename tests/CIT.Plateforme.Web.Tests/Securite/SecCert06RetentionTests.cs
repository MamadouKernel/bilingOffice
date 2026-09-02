using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Formulaires;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Securite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CIT.Plateforme.Web.Tests.Securite;

/// <summary>
/// Suite de certification contractuelle SEC-06 (SEC-CERT-06) — Rétention, archivage,
/// anonymisation et protection des données personnelles (Loi ARTCI & OHADA).
/// </summary>
public class SecCert06RetentionTests
{
    private sealed class HorlogeTest(DateTime maintenantUtc) : IHorloge
    {
        public DateTime MaintenantUtc { get; set; } = maintenantUtc;
    }

    private static PlateformeDbContext CreerDbContext()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PlateformeDbContext(options);
    }

    [Fact]
    public async Task Scenario1_BrouillonMoinsDe30Jours_NonPurge()
    {
        using var db = CreerDbContext();
        var dateRef = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var horloge = new HorlogeTest(dateRef.AddDays(25)); // 25 jours < 30j

        var brouillon = BrouillonSaisie.Creer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "DemandeImport", "{}", dateRef);
        db.BrouillonsSaisie.Add(brouillon);
        await db.SaveChangesAsync();

        var service = new GestionnaireRetentionService(db, horloge, NullLogger<GestionnaireRetentionService>.Instance);
        var purges = await service.PurgerBrouillonsExpiresAsync();

        Assert.Equal(0, purges);
        Assert.Equal(1, await db.BrouillonsSaisie.CountAsync());
    }

    [Fact]
    public async Task Scenario2_BrouillonPlusDe30Jours_PurgeAvecLot()
    {
        using var db = CreerDbContext();
        var dateRef = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var horloge = new HorlogeTest(dateRef.AddDays(31)); // 31 jours > 30j

        var brouillon = BrouillonSaisie.Creer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "DemandeImport", "{}", dateRef);
        db.BrouillonsSaisie.Add(brouillon);
        await db.SaveChangesAsync();

        var service = new GestionnaireRetentionService(db, horloge, NullLogger<GestionnaireRetentionService>.Instance);
        var purges = await service.PurgerBrouillonsExpiresAsync();

        Assert.Equal(1, purges);
        Assert.Equal(0, await db.BrouillonsSaisie.CountAsync());

        var lot = await db.LotsPurgeRetention.FirstOrDefaultAsync();
        Assert.NotNull(lot);
        Assert.Equal("Brouillon", lot.Categorie);
        Assert.Equal(1, lot.NombreElementsTraites);
        Assert.True(lot.EstSucces);
    }

    [Fact]
    public async Task Scenario3_DossierClotureMoinsDe5Ans_NonAnonymise()
    {
        using var db = CreerDbContext();
        var dateCloture = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var horloge = new HorlogeTest(dateCloture.AddYears(4)); // 4 ans < 5 ans

        var ticket = Ticket.Creer("TCK-001", "BillingImport", "Importation", Guid.NewGuid(), Guid.NewGuid(), dateCloture.AddDays(-10));
        ticket.PrendreEnCharge(Guid.NewGuid(), dateCloture.AddDays(-5));
        ticket.Cloturer(MotifClotureType.Normale, dateCloture);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = new GestionnaireRetentionService(db, horloge, NullLogger<GestionnaireRetentionService>.Instance);
        var anonymises = await service.AnonymiserDossiersExpiresAsync();

        Assert.Equal(0, anonymises);
        var t = await db.Tickets.FirstAsync();
        Assert.False(t.EstAnonymise);
    }

    [Fact]
    public async Task Scenario4_DossierCloturePlusDe5Ans_AnonymiseAvecSucces()
    {
        using var db = CreerDbContext();
        var dateCloture = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var horloge = new HorlogeTest(dateCloture.AddYears(5).AddDays(1)); // > 5 ans

        var ticket = Ticket.Creer("TCK-002", "BillingImport", "Importation", Guid.NewGuid(), Guid.NewGuid(), dateCloture.AddDays(-10));
        ticket.PrendreEnCharge(Guid.NewGuid(), dateCloture.AddDays(-5));
        ticket.Cloturer(MotifClotureType.Normale, dateCloture);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = new GestionnaireRetentionService(db, horloge, NullLogger<GestionnaireRetentionService>.Instance);
        var anonymises = await service.AnonymiserDossiersExpiresAsync();

        Assert.Equal(1, anonymises);
        var t = await db.Tickets.FirstAsync();
        Assert.True(t.EstAnonymise);
        Assert.NotNull(t.DateArchivageUtc);

        var lot = await db.LotsPurgeRetention.FirstOrDefaultAsync(l => l.Categorie == "Dossier");
        Assert.NotNull(lot);
        Assert.Equal(1, lot.NombreElementsTraites);
    }

    [Fact]
    public async Task Scenario5_DossierSousGelLegal_ExempteDAnonymisation()
    {
        using var db = CreerDbContext();
        var dateCloture = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var horloge = new HorlogeTest(dateCloture.AddYears(6)); // 6 ans mais sous gel légal

        var ticket = Ticket.Creer("TCK-003", "BillingImport", "Importation", Guid.NewGuid(), Guid.NewGuid(), dateCloture.AddDays(-10));
        ticket.PrendreEnCharge(Guid.NewGuid(), dateCloture.AddDays(-5));
        ticket.Cloturer(MotifClotureType.Normale, dateCloture);
        ticket.ActiverGelLegal();
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = new GestionnaireRetentionService(db, horloge, NullLogger<GestionnaireRetentionService>.Instance);
        var anonymises = await service.AnonymiserDossiersExpiresAsync();

        Assert.Equal(0, anonymises);
        var t = await db.Tickets.FirstAsync();
        Assert.False(t.EstAnonymise);
        Assert.True(t.EstSousGelLegal);
    }

    [Fact]
    public async Task Scenario6_LeveeDuGelLegal_RedevientEligible()
    {
        using var db = CreerDbContext();
        var dateCloture = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var horloge = new HorlogeTest(dateCloture.AddYears(6));

        var ticket = Ticket.Creer("TCK-004", "BillingImport", "Importation", Guid.NewGuid(), Guid.NewGuid(), dateCloture.AddDays(-10));
        ticket.PrendreEnCharge(Guid.NewGuid(), dateCloture.AddDays(-5));
        ticket.Cloturer(MotifClotureType.Normale, dateCloture);
        ticket.ActiverGelLegal();
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = new GestionnaireRetentionService(db, horloge, NullLogger<GestionnaireRetentionService>.Instance);
        await service.DesactiverGelLegalAsync(ticket.Id, "Fin du litige", Guid.NewGuid());

        var anonymises = await service.AnonymiserDossiersExpiresAsync();
        Assert.Equal(1, anonymises);
        var t = await db.Tickets.FirstAsync();
        Assert.True(t.EstAnonymise);
        Assert.False(t.EstSousGelLegal);
    }

    [Fact]
    public void Scenario7_ComptableMoinsDe10Ans_MaintienScellement()
    {
        var moteur = new MoteurRetentionService();
        var dateEmission = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateEmission.AddYears(8);

        var res = moteur.EvaluerEligibilite(CategorieRetention.Comptable, dateEmission, false, dateCourante);
        Assert.False(res.EstEligible);
        Assert.Equal(ActionRetention.ScellementArchive, res.Action);
    }

    [Fact]
    public void Scenario8_Comptable10AnsEtPlus_ArchiveLegale()
    {
        var moteur = new MoteurRetentionService();
        var dateEmission = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateEmission.AddYears(10).AddDays(1);

        var res = moteur.EvaluerEligibilite(CategorieRetention.Comptable, dateEmission, false, dateCourante);
        Assert.True(res.EstEligible);
        Assert.Equal(ActionRetention.ArchivageInalterable, res.Action);
    }

    [Fact]
    public void Scenario9_Audit3Ans_ConservationInalterable()
    {
        var moteur = new MoteurRetentionService();
        var dateCreation = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateCourante = dateCreation.AddYears(3).AddDays(1);

        var res = moteur.EvaluerEligibilite(CategorieRetention.Audit, dateCreation, false, dateCourante);
        Assert.True(res.EstEligible);
        Assert.Equal(ActionRetention.ArchivageInalterable, res.Action);
    }

    [Fact]
    public async Task Scenario10_IdempotencePurgeEtAnonymisation()
    {
        using var db = CreerDbContext();
        var dateCloture = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var horloge = new HorlogeTest(dateCloture.AddYears(6));

        var ticket = Ticket.Creer("TCK-005", "BillingImport", "Importation", Guid.NewGuid(), Guid.NewGuid(), dateCloture.AddDays(-10));
        ticket.PrendreEnCharge(Guid.NewGuid(), dateCloture.AddDays(-5));
        ticket.Cloturer(MotifClotureType.Normale, dateCloture);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = new GestionnaireRetentionService(db, horloge, NullLogger<GestionnaireRetentionService>.Instance);
        var passe1 = await service.AnonymiserDossiersExpiresAsync();
        var passe2 = await service.AnonymiserDossiersExpiresAsync();

        Assert.Equal(1, passe1);
        Assert.Equal(0, passe2); // Idempotence : second passage = 0
    }

    [Fact]
    public async Task Scenario11_LotPurgeSansPII()
    {
        using var db = CreerDbContext();
        var dateRef = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var horloge = new HorlogeTest(dateRef.AddDays(40));

        var brouillon = BrouillonSaisie.Creer(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "DemandeImport", "{\"secret\":\"confidentiel\"}", dateRef);
        db.BrouillonsSaisie.Add(brouillon);
        await db.SaveChangesAsync();

        var service = new GestionnaireRetentionService(db, horloge, NullLogger<GestionnaireRetentionService>.Instance);
        await service.PurgerBrouillonsExpiresAsync();

        var lot = await db.LotsPurgeRetention.FirstAsync();
        Assert.DoesNotContain("secret", lot.Categorie);
        Assert.DoesNotContain("confidentiel", lot.Categorie);
        Assert.True(lot.NombreElementsTraites > 0);
    }

    [Fact]
    public async Task Scenario12_IntegriteReferentielleApresAnonymisation()
    {
        using var db = CreerDbContext();
        var dateCloture = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var horloge = new HorlogeTest(dateCloture.AddYears(6));
        var dossierId = Guid.NewGuid();

        var ticket = Ticket.Creer("TCK-006", "BillingImport", "Importation", Guid.NewGuid(), dossierId, dateCloture.AddDays(-10));
        ticket.PrendreEnCharge(Guid.NewGuid(), dateCloture.AddDays(-5));
        ticket.Cloturer(MotifClotureType.Normale, dateCloture);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = new GestionnaireRetentionService(db, horloge, NullLogger<GestionnaireRetentionService>.Instance);
        await service.AnonymiserDossiersExpiresAsync();

        var t = await db.Tickets.FirstAsync(tk => tk.Id == ticket.Id);
        Assert.Equal(dossierId, t.DossierId);
        Assert.Equal("TCK-006", t.Numero);
        Assert.True(t.EstAnonymise);
    }
}
