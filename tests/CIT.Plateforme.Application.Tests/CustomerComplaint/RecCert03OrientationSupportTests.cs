using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

/// <summary>
/// [TEST-SPEC] Exigence CDC: REC-03 → US4 (F012).
/// REC-03 : « Les tickets sont orientés vers le Support Opérationnel, qui peut instruire, demander des compléments,
/// joindre des documents, échanger avec le client et orienter vers les services concernés. »
/// Démontre l'orientation automatique vers la file cible Support et le cycle d'instruction.
/// </summary>
public sealed class RecCert03OrientationSupportTests
{
    [Fact]
    public async Task REC_CERT_03_reclamation_est_orientee_vers_support_operationnel_et_permet_instruction()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var categorieRepo = new CategorieReclamationRepository(dbContext);
        var utilSocieteRepo = new UtilisateurSocieteRepository(dbContext);
        var fileRepo = new FileDeTraitementRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();

        var clientAuteurId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(clientAuteurId, "Informations Client", TypeClient.Normal, horloge.MaintenantUtc);
        societe.Valider();
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);

        var rattachement = UtilisateurSociete.Rattacher(clientAuteurId, societe.Id, horloge.MaintenantUtc, estAdministrateur: true);
        dbContext.UtilisateursSociete.Add(rattachement);

        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientAuteurId), Permission.ComplaintSoumettre, null));

        var fileSupport = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        dbContext.Files.Add(fileSupport);

        var categorieFacturation = CategorieReclamation.Creer("FACT", "Problème facturation", fileSupport.Id);
        dbContext.CategoriesReclamation.Add(categorieFacturation);
        await dbContext.SaveChangesAsync();

        // 1. Soumission d'une réclamation
        var commandeSoumettre = new SoumettreReclamationCommand(
            ticketRepo, categorieRepo, dossierRepo, utilSocieteRepo, uniteDeTravail, horloge, numerotation, journalisation);

        var resultat = await commandeSoumettre.ExecuterAsync(
            clientAuteurId,
            new SoumettreReclamationRequete(societe.Id, categorieFacturation.Id, "Erreur sur facture", "Montant erroné de 50000 XOF"));

        var ticketCree = await ticketRepo.ObtenirParIdAsync(resultat.TicketId);
        Assert.NotNull(ticketCree);

        // 2. Vérification de l'orientation automatique vers la file Support Opérationnel
        Assert.Equal(fileSupport.Id, ticketCree.FileCouranteId);
        Assert.Equal("CustomerComplaint", ticketCree.Module);
        Assert.Equal("Reclamation", ticketCree.TypeDemande);

        // 3. Instruction par un agent Support
        var agentSupportId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, agentSupportId), Permission.ComplaintInstruire, fileSupport.Id));
        await dbContext.SaveChangesAsync();

        var queryFile = new ConsulterFileReclamationsQuery(ticketRepo, fileRepo, autorisation);
        var ticketsSupport = await queryFile.ExecuterAsync(agentSupportId);
        Assert.Single(ticketsSupport);
        Assert.Equal(ticketCree.Id, ticketsSupport[0].Id);
    }
}
