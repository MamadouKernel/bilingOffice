using System;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Agent;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Agent;
using CIT.Plateforme.Web.Components.Pages.BillingImport;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.E2E;

public sealed class ParcoursAgentE2ETests : BunitContext
{
    public ParcoursAgentE2ETests() => LocalisationDeTest.Enregistrer(Services);

    [Fact]
    public async Task ParcoursAgent_DashboardDisponibiliteEtFileManifeste()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var socId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            NomAffiche = "Agent Manifeste",
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var userAgent = new ApplicationUser
        {
            Id = agentId,
            UserName = "agent@cit.ci",
            Email = "agent@cit.ci",
            NomAffiche = "Agent Manifeste",
            Nature = NatureUtilisateur.Agent,
            EstActif = true
        };
        db.Users.Add(userAgent);

        var file = FileDeTraitement.Creer("BillingImport", SoumettreDemandeImportCommand.FilePiloteManifeste);
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var hab = HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportManifesteInstruire, fileId);
        db.Habilitations.Add(hab);

        var soc = SocieteCliente.Soumettre(Guid.NewGuid(), "CLIENT TEST", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var dossier = Dossier.Creer("DOS-E2E-1", socId, DateTime.UtcNow, Guid.NewGuid());
        var ticket = Ticket.Creer("TCK-E2E-1", "BillingImport", "IMPORT", fileId, dossier.Id, DateTime.UtcNow, "BL-E2E-999");
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var fileRepo = new FileDeTraitementRepository(db);
        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var habRepo = new HabilitationRepository(db);
        var dispoRepo = new DisponibiliteRepository(db);
        var suiviRepo = new SuiviBrancheIesRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Web.Tests.Support.FabriqueSecurite.ControleAcces(db));

        var queryDashboard = new ObtenirDashboardAgentQuery(fileRepo, ticketRepo, habRepo, dispoRepo,
            new CIT.Plateforme.Application.Securite.GardeIdentiteService(new UtilisateurInfoRepository(db)));
        var dispoCommand = new GererDisponibiliteCommand(dispoRepo, autorisation, journalisation, horloge, uow);
        var validerCmd = new ValiderTicketImportCommand(ticketRepo, dossierRepo, fileRepo, suiviRepo, autorisation, journalisation, horloge, uow);
        var rejeterCmd = new RejeterTicketImportCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var complementCmd = new GestionComplementManifesteCommand(ticketRepo, cloisonnement, autorisation, journalisation, horloge, uow);
        var consulterFileQuery = new ConsulterFileBillingImportQuery(ticketRepo, fileRepo, autorisation);

        Services.AddSingleton(queryDashboard);
        Services.AddScoped<CIT.Plateforme.Web.Services.AnnuaireClients>();
        Services.AddSingleton(dispoCommand);
        Services.AddSingleton(validerCmd);
        Services.AddSingleton(rejeterCmd);
        Services.AddSingleton(complementCmd);
        Services.AddSingleton(consulterFileQuery);
        Services.AddSingleton(new CIT.Plateforme.Application.Tickets.PrendreEnChargeEtapeBillingCommand(ticketRepo, autorisation, journalisation, horloge, uow));

        // 1. Dashboard Agent
        var cutDashboard = Render<DashboardAgent>();
        Assert.Contains("Agent Manifeste", cutDashboard.Markup);
        Assert.Contains("Pilote Manifeste", cutDashboard.Markup);

        // 2. File Pilote Manifeste
        var cutFile = Render<FilePiloteManifeste>();
        Assert.Contains("BL-E2E-999", cutFile.Markup);
        Assert.Contains("TCK-E2E-1", cutFile.Markup);
    }
}
