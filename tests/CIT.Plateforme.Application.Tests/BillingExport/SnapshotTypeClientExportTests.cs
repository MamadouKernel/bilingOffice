using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingExport;

/// <summary>
/// [TEST-PROOF] Snapshot TypeClient : Le routage d'un dossier/ticket existant dépend exclusivement
/// de Dossier.TypeClient (snapshot figé à la création) et reste insensible aux modifications ultérieures
/// de SocieteCliente.TypeClient.
/// </summary>
public sealed class SnapshotTypeClientExportTests
{
    [Fact]
    public async Task Routage_export_reste_fidele_au_snapshot_dossier_meme_si_societe_modifiee()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();
        var ticketRepository = new TicketRepository(dbContext);
        var dossierRepository = new DossierRepository(dbContext);
        var fileRepository = new FileDeTraitementRepository(dbContext);
        var suiviRepository = new SuiviBrancheIesRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var cloisonnement = new CloisonnementService(ticketRepository, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(dbContext));

        var fileBO = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.BackOfficeExport);
        var fileFactu = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.Facturation);
        var fileIes = FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, NomsFilesExport.FileIes);
        dbContext.Files.AddRange(fileBO, fileFactu, fileIes);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "SocieteSnapshotTest", TypeClient.IES, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentId), Permission.BillingExportBackOfficeInstruire));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeExportCommand(
            ticketRepository, dossierRepository, new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), fileRepository, numerotation, journalisation, horloge, uniteDeTravail);

        // 1. Soumission sous profil IES -> Dossier.TypeClient = IES
        var res = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(societe.Id, auteurId, ["BKG-SNAP-01"]));
        var ticketId = res.TicketsCrees[0].Id;

        // 2. Altération a posteriori du profil courant de la société vers Normal
        // (Simulation d'une évolution de compte qui ne doit pas affecter les dossiers en cours)
        dbContext.Entry(societe).Property(s => s.Profil).CurrentValue = TypeClient.Normal;
        await dbContext.SaveChangesAsync();

        // 3. Validation Back Office -> Doit router vers File IES grâce au snapshot
        var validerBO = new ValiderTicketExportCommand(
            ticketRepository, dossierRepository, fileRepository, suiviRepository, autorisation, journalisation, horloge, uniteDeTravail);

        await validerBO.ExecuterAsync(agentId, ticketId);

        var ticketApresValidation = await ticketRepository.ObtenirParIdAsync(ticketId);
        Assert.Equal(fileIes.Id, ticketApresValidation!.FileCouranteId);
    }
}
