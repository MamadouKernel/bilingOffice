using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [TEST-SPEC] EXP-01 / Décision A2 : Indépendance totale des tickets multi-bookings.
/// </summary>
public sealed class MultiBookingsIndependanceTests
{
    [Fact]
    public async Task Multi_bookings_partiellement_doublon_cree_les_tickets_valides_sans_annuler_le_dossier()
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

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteExport", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        // 1er booking existant
        await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(societe.Id, auteurId, ["BKG-A"]));

        // Soumission de BKG-A (doublon), BKG-B (valide), BKG-C (valide)
        var resultat = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(societe.Id, auteurId, ["BKG-A", "BKG-B", "BKG-C"]));

        Assert.NotNull(resultat.Dossier);
        Assert.Equal(2, resultat.TicketsCrees.Count);
        Assert.Contains(resultat.TicketsCrees, t => t.NumeroBooking == "BKG-B");
        Assert.Contains(resultat.TicketsCrees, t => t.NumeroBooking == "BKG-C");
        Assert.Single(resultat.BookingRefuses);
        Assert.Equal("BKG-A", resultat.BookingRefuses[0].NumeroBooking);
    }
}
