using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : démontre, de bout en bout (commande
/// d'administration → persistance EF Core → <c>ParametresSlaProvider</c>), que le calendrier
/// ouvré réellement administré est restitué — avant correction, <c>ParametresSlaProvider</c>
/// retournait toujours <see cref="CalendrierOuvre.ToujoursOuvert"/> quelle que soit la
/// configuration (bug du <c>? :</c> à deux branches identiques).
/// </summary>
public sealed class ParametresSlaProviderTests
{
    [Fact]
    public async Task Calendrier_toujours_ouvert_par_defaut_quand_aucun_calendrier_n_est_administre()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationSla));
        await dbContext.SaveChangesAsync();

        var gererSla = new GererParametresSlaCommand(new ParametreSlaRepository(dbContext), new FileDeTraitementRepository(dbContext), new PrioriteRepository(dbContext), autorisation, journalisation, uniteDeTravail);
        await gererSla.DefinirAsync(administrateurId, "BillingOffice", "Import", TimeSpan.FromHours(2), 0.5, 0.8);

        var provider = new ParametresSlaProvider(dbContext);
        var dossier = Dossier.Creer("DOS-CALPROV-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-CALPROV-000001", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);

        var configuration = await provider.ObtenirPourAsync(ticket);

        Assert.NotNull(configuration);
        // Aucun calendrier administré : samedi doit être ouvré (calendrier "toujours ouvert").
        Assert.True(configuration!.Value.Calendrier.EstJourOuvre(new DateOnly(2026, 1, 3))); // samedi
    }

    [Fact]
    public async Task Calendrier_ouvre_administre_est_reellement_restitue_et_exclut_le_week_end()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationSla));
        await dbContext.SaveChangesAsync();

        var gererSla = new GererParametresSlaCommand(new ParametreSlaRepository(dbContext), new FileDeTraitementRepository(dbContext), new PrioriteRepository(dbContext), autorisation, journalisation, uniteDeTravail);
        // Valeurs de test explicites (Lundi-Vendredi 08h-17h) — pas une valeur réelle de PC-12.
        await gererSla.DefinirAvecCalendrierAsync(
            administrateurId, "Gate", "Enrolement", TimeSpan.FromHours(2), 0.5, 0.8,
            new TimeOnly(8, 0), new TimeOnly(17, 0),
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]);

        var provider = new ParametresSlaProvider(dbContext);
        var dossier = Dossier.Creer("DOS-CALPROV-000002", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-CALPROV-000002", "Gate", "Enrolement", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);

        var configuration = await provider.ObtenirPourAsync(ticket);

        Assert.NotNull(configuration);
        var calendrier = configuration!.Value.Calendrier;
        // REMÉDIATION AUDIT : avant correction, cette assertion échouait — le calendrier retourné
        // était toujours "toujours ouvert" (samedi ouvré) quelle que soit la configuration.
        Assert.False(calendrier.EstJourOuvre(new DateOnly(2026, 1, 3))); // samedi — non ouvré
        Assert.True(calendrier.EstJourOuvre(new DateOnly(2026, 1, 5)));  // lundi — ouvré

        // Vendredi 20h -> Lundi 08h : calendrier ouvré exclut intégralement le week-end (0h).
        var duree = calendrier.DureeOuvreeEntre(
            new DateTime(2026, 1, 2, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc));
        Assert.Equal(TimeSpan.Zero, duree);
    }
}