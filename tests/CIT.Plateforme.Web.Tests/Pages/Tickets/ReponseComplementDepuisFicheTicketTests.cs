using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Tickets;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Tickets;

/// <summary>
/// REMÉDIATION FINALE UI (closure-review.md §9) : preuve que la réponse client à une demande de
/// complément fonctionne réellement à travers le composant <see cref="FicheTicket"/> — pas en
/// appelant directement la Command avec le bon identifiant (ce que faisait
/// RecSce07CustomerComplaintE2ETests avant cette remédiation, contournant ainsi le bug).
/// </summary>
public sealed class ReponseComplementDepuisFicheTicketTests : BunitContext
{
    public ReponseComplementDepuisFicheTicketTests() => LocalisationDeTest.Enregistrer(Services);

    [Fact]
    public async Task Client_Repond_Depuis_FicheTicket_Publie_Le_Message_Et_Reprend_Le_Traitement()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var socId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = clientId,
            SocieteId = socId,
            SocieteNom = "SOCIETE REC-03",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var soc = SocieteCliente.Soumettre(clientId, "SOCIETE REC-03", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var userClient = new ApplicationUser { Id = clientId, UserName = "client@rec03.ci", NomAffiche = "Client REC-03", Nature = NatureUtilisateur.Client, EstActif = true };
        var userAgent = new ApplicationUser { Id = agentId, UserName = "agent@rec03.ci", NomAffiche = "Agent Support", Nature = NatureUtilisateur.Agent, EstActif = true };
        db.Users.AddRange(userClient, userAgent);

        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), socId, DateTime.UtcNow, true, true));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.ComplaintInstruire, fileId));

        var file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var categorie = CategorieReclamation.Creer("FACT", "Facturation", fileId);
        db.Set<CategorieReclamation>().Add(categorie);

        var dossier = Dossier.Creer("DOS-REC03-01", socId, DateTime.UtcNow, clientId);
        var ticket = Ticket.Creer("TCK-REC03-01", "CustomerComplaint", "Reclamation", fileId, dossier.Id, DateTime.UtcNow);
        ticket.DefinirCategorieReclamation(categorie.Id);
        ticket.DefinirContenuReclamation("Facture erronée", "Le montant ne correspond pas au devis.");
        ticket.PrendreEnCharge(agentId, DateTime.UtcNow);
        // Étape 1 du scénario : l'agent demande un complément (REC-03).
        ticket.MettreEnAttenteClient(DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var histRepo = new HistoriqueTicketRepository(db);
        var docRepo = new DocumentRepository(db);
        var msgRepo = new MessageRepository(db);
        var habRepo = new HabilitationRepository(db);
        var catRepo = new CategorieReclamationRepository(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Web.Tests.Support.FabriqueSecurite.ControleAcces(db));

        var ficheQuery = new ObtenirFicheTicketQuery(ticketRepo, dossierRepo, fileRepo, socRepo, userInfoRepo, histRepo, docRepo, msgRepo, habRepo, catRepo, new LiaisonTicketRepository(db),
            new CIT.Plateforme.Application.Securite.ControleAccesRessourceService(
                new DossierRepository(db), new TicketRepository(db),
                new UtilisateurSocieteRepository(db), new HabilitationRepository(db),
                new PerimetreSupervisionRepository(db), new CIT.Plateforme.Application.Securite.GardeIdentiteService(new UtilisateurInfoRepository(db))));
        var prendreEnCharge = new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var attenteClient = new AttenteClientCommand(ticketRepo, new PeriodeAttenteClientRepository(db), autorisation, journalisation, horloge, uow);
        var publierMessage = new PublierMessageCommand(msgRepo, ticketRepo, autorisation, cloisonnement, journalisation, horloge, uow);
        var clotureReclamation = new CloturerReclamationCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var evaluerReclamation = new EvaluerReclamationCommand(ticketRepo, dossierRepo, userSocRepo, journalisation, horloge, uow, autorisation);
        var dashboardQuery = new ObtenirDashboardClientQuery(dossierRepo, ticketRepo, socRepo, userSocRepo);

        Services.AddSingleton(ficheQuery);
        Services.AddSingleton(prendreEnCharge);
        Services.AddSingleton(attenteClient);
        Services.AddSingleton(publierMessage);
        Services.AddSingleton(clotureReclamation);
        Services.AddSingleton(evaluerReclamation);

        // Précondition : ACR-01 présent avant la réponse.
        var dashboardAvant = await dashboardQuery.ExecuterAsync(clientId, socId);
        Assert.Contains(dashboardAvant.ActionsRequises, a => a.TicketId == ticket.Id);

        // Act — Étape 2 : le client ouvre la fiche RÉELLE et répond via le composant UI.
        var cut = Render<FicheTicket>(p => p.Add(x => x.Id, ticket.Id));

        var champMessage = cut.Find("input[placeholder='Rédigez votre message ou complément...']");
        champMessage.Change("Voici le numéro de facture contesté : FCT-2026-042.");

        var boutonEnvoyer = cut.Find("button.btn-primary");
        Assert.Contains("Envoyer", boutonEnvoyer.TextContent);
        boutonEnvoyer.Click();

        // Étape "reprise" : le client clique sur l'action de reprise du traitement.
        var boutonReprendre = cut.Find("button.btn-outline-success");
        Assert.Contains("Reprendre le traitement", boutonReprendre.TextContent);
        boutonReprendre.Click();

        // Assert
        // 4. La réponse doit être publiée sur le bon ticket.
        var messagesDuTicket = await msgRepo.ListerParTicketAsync(ticket.Id);
        Assert.Contains(messagesDuTicket, m => m.Contenu.Contains("FCT-2026-042"));

        // 5. Le ticket doit reprendre son cycle de traitement.
        var ticketRecharge = await ticketRepo.ObtenirParIdAsync(ticket.Id);
        Assert.Equal(StatutTicket.EnCours, ticketRecharge!.Statut);

        // 6. ACR-01 doit disparaître.
        var dashboardApres = await dashboardQuery.ExecuterAsync(clientId, socId);
        Assert.DoesNotContain(dashboardApres.ActionsRequises, a => a.TicketId == ticket.Id);
    }
}
