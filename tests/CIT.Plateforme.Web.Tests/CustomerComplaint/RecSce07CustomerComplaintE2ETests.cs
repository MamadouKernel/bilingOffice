using System;
using System.Linq;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.CustomerComplaint;

/// <summary>
/// [TEST-CDC] Rejeu intégral de REC-SCE-07 (docs/04-matrice-recette.md) :
/// Parcours Customer Complaint complet :
/// 1. Soumission par catégorie
/// 2. Orientation File Support Opérationnel
/// 3. Prise en charge Agent
/// 4. Demande de complément (Statut EnAttenteClient, ACR-01 généré)
/// 5. Réponse au complément par le client (reprise traitement, disparition ACR-01)
/// 6. Transfert de file (orientation)
/// 7. Clôture avec réponse formalisée obligatoire
/// 8. Évaluation de satisfaction client
/// Exigences : REC-01, REC-02, REC-03, REC-04, REC-05, REC-06.
/// </summary>
public sealed class RecSce07CustomerComplaintE2ETests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory = factory;

    [Fact]
    public async Task RecSce07_ParcoursContractuelComplet_Reussite()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();

        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreReclamationCommand>();
        var consulterFileQuery = scope.ServiceProvider.GetRequiredService<ConsulterFileQuery>();
        var prendreEnChargeCmd = scope.ServiceProvider.GetRequiredService<PrendreEnChargeCommand>();
        var attenteClientCmd = scope.ServiceProvider.GetRequiredService<AttenteClientCommand>();
        var dashboardClientQuery = scope.ServiceProvider.GetRequiredService<ObtenirDashboardClientQuery>();
        var publierMessageCmd = scope.ServiceProvider.GetRequiredService<PublierMessageCommand>();
        var transfererCmd = scope.ServiceProvider.GetRequiredService<TransfererTicketCommand>();
        var clotureCmd = scope.ServiceProvider.GetRequiredService<CloturerReclamationCommand>();
        var evaluerCmd = scope.ServiceProvider.GetRequiredService<EvaluerReclamationCommand>();
        var ficheQuery = scope.ServiceProvider.GetRequiredService<ObtenirFicheTicketQuery>();

        // 0. Initialisation Acteurs & Files
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var agentSupportId = Guid.NewGuid();
        var agentFacturationId = Guid.NewGuid();

        var clientUser = new ApplicationUser
        {
            Id = clientId,
            UserName = $"client_e2e_{clientId}@test.local",
            Email = $"client_e2e_{clientId}@test.local",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(clientUser);

        var agentSupportUser = new ApplicationUser
        {
            Id = agentSupportId,
            UserName = $"agent_supp_{agentSupportId}@test.local",
            Email = $"agent_supp_{agentSupportId}@test.local",
            Nature = NatureUtilisateur.Agent,
            EstActif = true
        };
        db.Users.Add(agentSupportUser);

        var agentFactUser = new ApplicationUser
        {
            Id = agentFacturationId,
            UserName = $"agent_fact_{agentFacturationId}@test.local",
            Email = $"agent_fact_{agentFacturationId}@test.local",
            Nature = NatureUtilisateur.Agent,
            EstActif = true
        };
        db.Users.Add(agentFactUser);

        var soc = SocieteCliente.Soumettre(clientId, "SOC-REC-E2E", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);

        var userSoc = UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);

        var fileSupport = await db.Files.FirstOrDefaultAsync(f => f.Nom == "Support Opérationnel");
        if (fileSupport is null)
        {
            fileSupport = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
            db.Files.Add(fileSupport);
        }

        var fileFacturation = await db.Files.FirstOrDefaultAsync(f => f.Nom == "Facturation");
        if (fileFacturation is null)
        {
            fileFacturation = FileDeTraitement.Creer("BillingImport", "Facturation");
            db.Files.Add(fileFacturation);
        }

        // Autoriser la transition de fileSupport vers fileFacturation
        if (!await db.TransitionsAutorisees.AnyAsync(t => t.FileSourceId == fileSupport.Id && t.FileCibleId == fileFacturation.Id))
        {
            db.TransitionsAutorisees.Add(TransitionAutorisee.Definir(fileSupport.Id, fileFacturation.Id, new List<string>()));
        }

        var catCode = $"FACT_{Guid.NewGuid():N}"[..10];
        var categorie = CategorieReclamation.Creer(catCode, "Erreur facturation", fileSupport.Id);
        db.Set<CategorieReclamation>().Add(categorie);

        // Habilitations
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId), Permission.ComplaintSoumettre));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId), Permission.ComplaintRepondre));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentSupportId), Permission.ComplaintInstruire, fileSupport.Id));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentSupportId), Permission.TicketTransferer, fileSupport.Id));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentFacturationId), Permission.ComplaintInstruire, fileFacturation.Id));
        await db.SaveChangesAsync();

        // 1. Étape 1 : Soumission par catégorie (REC-01, REC-02)
        var soumissionResult = await soumettreCmd.ExecuterAsync(
            clientId,
            new SoumettreReclamationRequete(societeId, categorie.Id, "Facture erronée", "Stationnement indûment facturé sur BL-999"));

        Assert.NotNull(soumissionResult);
        Assert.NotEmpty(soumissionResult.NumeroTicket);
        var ticketId = soumissionResult.TicketId;

        // 2. Étape 2 : Orientation File Support Opérationnel (REC-05)
        var ticketsEnFile = await consulterFileQuery.ExecuterAsync(agentSupportId, fileSupport.Id);
        var ticketEnFile = ticketsEnFile.FirstOrDefault(t => t.Id == ticketId);
        Assert.NotNull(ticketEnFile);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticketEnFile.Statut);

        // 3. Étape 3 : Prise en charge par l'agent (REC-03)
        await prendreEnChargeCmd.ExecuterAsync(agentSupportId, ticketId);
        var ticketPrisEnCharge = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(ticketPrisEnCharge);
        Assert.Equal(StatutTicket.EnCours, ticketPrisEnCharge.Statut);
        Assert.Equal(agentSupportId, ticketPrisEnCharge.OperateurAssigneId);

        // 4. Étape 4 : Demande de complément (REC-03, ACR-01)
        await attenteClientCmd.MettreEnAttenteAsync(agentSupportId, ticketId, "Veuillez fournir le reçu de paiement de caution.");
        var ticketEnAttente = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(ticketEnAttente);
        Assert.Equal(StatutTicket.EnAttenteClient, ticketEnAttente.Statut);

        var dashboardClient = await dashboardClientQuery.ExecuterAsync(clientId, societeId);
        var actionRequise = dashboardClient.ActionsRequises.FirstOrDefault(a => a.TicketId == ticketId);
        Assert.NotNull(actionRequise);
        Assert.Equal(TypeActionRequise.ComplementReclamation, actionRequise.TypeAction);

        // 5. Étape 5 : Réponse au complément par le client
        await publierMessageCmd.PublierMessageClientAsync(clientId, ticketId, "Voici la pièce justificative.");
        await attenteClientCmd.RependreApresReponseAsync(ticketId);

        var ticketRepris = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(ticketRepris);
        Assert.Equal(StatutTicket.EnCours, ticketRepris.Statut);

        var dashboardApresReponse = await dashboardClientQuery.ExecuterAsync(clientId, societeId);
        Assert.DoesNotContain(dashboardApresReponse.ActionsRequises, a => a.TicketId == ticketId);

        // 6. Étape 6 : Transfert d'orientation (optionnel dans flux, testé)
        await transfererCmd.ExecuterAsync(agentSupportId, ticketId, fileFacturation.Id, "Transmission pour vérification comptable");
        var ticketTransfere = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(ticketTransfere);
        Assert.Equal(fileFacturation.Id, ticketTransfere.FileCouranteId);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticketTransfere.Statut);

        // Reprise en charge par agent Facturation
        await prendreEnChargeCmd.ExecuterAsync(agentFacturationId, ticketId);

        // 7. Étape 7 : Clôture avec réponse formalisée (REC-04, CLAR-REC-01)
        var reponseTexte = "Après vérification, un dégrèvement a été appliqué sur votre dossier.";
        await clotureCmd.ExecuterAsync(new CloturerReclamationRequete(agentFacturationId, ticketId, reponseTexte));

        var ticketCloture = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(ticketCloture);
        Assert.Equal(StatutTicket.Cloture, ticketCloture.Statut);
        Assert.Equal(reponseTexte, ticketCloture.ReponseFormalisee);

        // Vérification affichage fiche
        var ficheVue = await ficheQuery.ExecuterAsync(clientId, ticketId);
        Assert.Equal(reponseTexte, ficheVue.ReponseFormalisee);
        Assert.Equal(StatutTicket.Cloture, ficheVue.Statut);

        // 8. Étape 8 : Évaluation de satisfaction client (REC-06, CLAR-REC-02)
        await evaluerCmd.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticketId, 5, "Service très réactif, merci."));

        var ticketEvalue = await db.Tickets.FindAsync(ticketId);
        Assert.NotNull(ticketEvalue);
        Assert.Equal(5, ticketEvalue.EvaluationNote);
        Assert.Equal("Service très réactif, merci.", ticketEvalue.EvaluationCommentaire);
    }
}
