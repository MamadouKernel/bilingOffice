using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [TEST-SPEC] Exigence CDC: EXP-01, UNI-04 → US1, US2 → Contrat C5-01.
/// </summary>
public sealed class SoumettreDemandeExportCommandTests
{
    private static (PlateformeDbContext DbContext, SoumettreDemandeExportCommand Soumettre) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();

        var file = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, SoumettreDemandeExportCommand.FileBackOfficeExport);
        dbContext.Files.Add(file);
        dbContext.SaveChanges();

        var soumettre = new SoumettreDemandeExportCommand(
            new TicketRepository(dbContext),
            new DossierRepository(dbContext),
            new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext),
            new FileDeTraitementRepository(dbContext),
            numerotation,
            journalisation,
            horloge,
            uniteDeTravail);

        return (dbContext, soumettre);
    }

    [Fact]
    public async Task Soumission_de_deux_bookings_valides_cree_deux_tickets_independants_sous_un_meme_dossier()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExport", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(societe.Id, auteurId, ["BKG-001", "BKG-002"]));

        Assert.Equal(2, resultat.TicketsCrees.Count);
        Assert.All(resultat.TicketsCrees, t => Assert.Equal(resultat.Dossier.Id, t.DossierId));
        Assert.Equal(["BKG-001", "BKG-002"], resultat.TicketsCrees.Select(t => t.NumeroBooking));
        Assert.Empty(resultat.BookingRefuses);
    }

    [Fact]
    public async Task Utilisateur_sans_droit_de_soumission_est_refuse()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExport", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var utilisateurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: false));
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(societe.Id, utilisateurId, ["BKG-003"])));
    }

    [Fact]
    public async Task Booking_actif_existant_est_signale_en_refus_sans_bloquer_autres_bookings()
    {
        var (dbContext, soumettre) = ConstruireContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExport", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        // 1ère soumission
        await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(societe.Id, auteurId, ["BKG-EXIST"]));

        // 2ème soumission avec un doublon et un nouveau booking
        var resultat2 = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(societe.Id, auteurId, ["BKG-EXIST", "BKG-NOUVEAU"]));

        Assert.Single(resultat2.TicketsCrees);
        Assert.Equal("BKG-NOUVEAU", resultat2.TicketsCrees[0].NumeroBooking);
        Assert.Single(resultat2.BookingRefuses);
        Assert.Equal("BKG-EXIST", resultat2.BookingRefuses[0].NumeroBooking);
        Assert.NotNull(resultat2.BookingRefuses[0].TicketExistantId); // Même société -> ID fourni
    }
}
