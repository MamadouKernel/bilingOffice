using System;
using System.Net;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Agent;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Dossiers;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Societe;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Agent;
using CIT.Plateforme.Web.Components.Pages.BillingImport;
using CIT.Plateforme.Web.Components.Pages.Dossiers;
using CIT.Plateforme.Web.Components.Pages.Tickets;
using CIT.Plateforme.Web.Components.Shared;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages;

public sealed class AuthorizationUxTests : BunitContext, IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public AuthorizationUxTests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
        LocalisationDeTest.Enregistrer(Services);
    }

    [Fact]
    public async Task CasD_UtilisateurAnonyme_RedirigeVersConnexionAvecReturnUrl()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var reponse = await client.GetAsync("/client/dashboard");

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        var location = reponse.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/comptes/connexion", location);
        Assert.Contains("ReturnUrl=%2Fclient%2Fdashboard", location);
    }

    // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.1) — REC-SEC-01 : les 3 nouvelles
    // routes F006 (absentes avant remédiation) doivent exiger une authentification.
    [Theory]
    [InlineData("/reclamations/soumettre")]
    [InlineData("/reclamations/mes-reclamations")]
    [InlineData("/reclamations/file")]
    public async Task RecSec01_RoutesReclamations_UtilisateurAnonyme_RedirigeVersConnexion(string route)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var reponse = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.Redirect, reponse.StatusCode);
        var location = reponse.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/comptes/connexion", location);
    }

    [Fact]
    public void CasA_ClientAuthentifie_SurDashboardAgent_AffichePermissionDeniedSansRedirection()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = Guid.NewGuid(),
            NomAffiche = "Client Test",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);
        var fileRepo = new FileDeTraitementRepository(db);
        var ticketRepo = new TicketRepository(db);
        var habRepo = new HabilitationRepository(db);
        var dispoRepo = new DisponibiliteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var dispoCmd = new GererDisponibiliteCommand(dispoRepo, autorisation, journalisation, horloge, uow);
        var query = new ObtenirDashboardAgentQuery(fileRepo, ticketRepo, habRepo, dispoRepo,
            new CIT.Plateforme.Application.Securite.GardeIdentiteService(new UtilisateurInfoRepository(db)));
        Services.AddSingleton(query);
        Services.AddSingleton(dispoCmd);

        var cut = Render<DashboardAgent>();

        // Doit afficher un refus d'accès 403 propre et PAS de formulaire de connexion
        Assert.Contains("403", cut.Markup);
        Assert.Contains("Accès Refusé", cut.Markup);
        Assert.DoesNotContain("Se connecter", cut.Markup);
    }

    [Fact]
    public void CasB_AgentNonHabilite_SurFileBillingImport_AffichePermissionDeniedSansRedirection()
    {
        var agentId = Guid.NewGuid();
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            NomAffiche = "Agent Non Habilité",
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);
        var fileRepo = new FileDeTraitementRepository(db);
        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var suiviRepo = new SuiviBrancheIesRepository(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Web.Tests.Support.FabriqueSecurite.ControleAcces(db));

        var file = FileDeTraitement.Creer("BillingImport", SoumettreDemandeImportCommand.FilePiloteManifeste);
        db.Files.Add(file);
        db.SaveChanges();

        var validerCmd = new ValiderTicketImportCommand(ticketRepo, dossierRepo, fileRepo, suiviRepo, autorisation, journalisation, horloge, uow);
        var rejeterCmd = new RejeterTicketImportCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var complementCmd = new GestionComplementManifesteCommand(ticketRepo, cloisonnement, autorisation, journalisation, horloge, uow);
        var consulterFileQuery = new ConsulterFileBillingImportQuery(ticketRepo, fileRepo, autorisation);

        Services.AddSingleton(validerCmd);
        Services.AddSingleton(rejeterCmd);
        Services.AddSingleton(complementCmd);
        Services.AddSingleton(consulterFileQuery);

        var cut = Render<FilePiloteManifeste>();

        // L'agent n'a pas la permission BillingImportManifesteInstruire
        Assert.Contains("403", cut.Markup);
        Assert.Contains("Accès Refusé", cut.Markup);
        Assert.DoesNotContain("Se connecter", cut.Markup);
    }

    [Fact]
    public void CasC_UtilisateurSocieteA_SurTicketSocieteB_AffichePermissionDeniedSansRedirection()
    {
        var userAId = Guid.NewGuid();
        var socAId = Guid.NewGuid();
        var socBId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = userAId,
            SocieteId = socAId,
            SocieteNom = "Société A",
            NomAffiche = "Utilisateur A",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var socA = SocieteCliente.Soumettre(userAId, "Société A", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(socA, socAId);
        socA.Valider();

        var socB = SocieteCliente.Soumettre(Guid.NewGuid(), "Société B", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(socB, socBId);
        socB.Valider();
        db.Societes.AddRange(socA, socB);

        var userA = new ApplicationUser { Id = userAId, UserName = "userA@a.ci", NomAffiche = "Utilisateur A", Nature = NatureUtilisateur.Client, EstActif = true };
        db.Users.Add(userA);

        var userSocA = UtilisateurSociete.Rattacher(userAId, socAId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSocA);

        var file = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var dossierB = Dossier.Creer("DOS-B", socBId, DateTime.UtcNow, Guid.NewGuid());
        var ticketB = Ticket.Creer("TCK-B", "BillingImport", "IMPORT", fileId, dossierB.Id, DateTime.UtcNow, "BL-B");
        db.Dossiers.Add(dossierB);
        db.Tickets.Add(ticketB);
        db.SaveChanges();

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
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Web.Tests.Support.FabriqueSecurite.ControleAcces(db));
        var catRepo = new CategorieReclamationRepository(db);

        var queryTicket = new ObtenirFicheTicketQuery(ticketRepo, dossierRepo, fileRepo, socRepo, userInfoRepo, histRepo, docRepo, msgRepo, habRepo, catRepo, new LiaisonTicketRepository(db),
            new CIT.Plateforme.Application.Securite.ControleAccesRessourceService(
                new DossierRepository(db), new TicketRepository(db),
                new UtilisateurSocieteRepository(db), new HabilitationRepository(db),
                new PerimetreSupervisionRepository(db), new CIT.Plateforme.Application.Securite.GardeIdentiteService(new UtilisateurInfoRepository(db))));
        var prendreEnCharge = new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var attenteClient = new AttenteClientCommand(ticketRepo, new PeriodeAttenteClientRepository(db), autorisation, journalisation, horloge, uow);
        var publierMessage = new PublierMessageCommand(msgRepo, ticketRepo, autorisation, cloisonnement, journalisation, horloge, uow);
        // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.1) : FicheTicket.razor injecte
        // désormais ces deux contrats pour afficher/clôturer/évaluer une réclamation.
        var clotureReclamation = new CloturerReclamationCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var evaluerReclamation = new EvaluerReclamationCommand(ticketRepo, dossierRepo, userSocRepo, journalisation, horloge, uow, autorisation);

        Services.AddSingleton(queryTicket);
        Services.AddSingleton(prendreEnCharge);
        Services.AddSingleton(attenteClient);
        Services.AddSingleton(publierMessage);
        Services.AddSingleton(clotureReclamation);
        Services.AddSingleton(evaluerReclamation);

        var cut = Render<FicheTicket>(p => p.Add(x => x.Id, ticketB.Id));

        // Refus cloisonnement multi-tenant sans fuite
        Assert.Contains("403", cut.Markup);
        Assert.Contains("Accès Refusé", cut.Markup);
        Assert.DoesNotContain("BL-B", cut.Markup); // Aucune fuite de données
    }
}
