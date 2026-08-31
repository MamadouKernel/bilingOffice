using System;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.Dossiers;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.BillingImport;
using CIT.Plateforme.Web.Components.Pages.Client;
using CIT.Plateforme.Web.Components.Pages.Dossiers;
using CIT.Plateforme.Web.Components.Pages.Tickets;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.E2E;

public sealed class ParcoursClientE2ETests : BunitContext
{
    [Fact]
    public async Task ParcoursClient_DeLaSoumissionAuSuiviFicheTicket()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var userId = Guid.NewGuid();
        var socId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = userId,
            SocieteId = socId,
            SocieteNom = "TRANS-IVOIRE-LOGISTICS",
            NomAffiche = "Jean Dupont",
            Email = "jean@transivoire.ci",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            DroitSoumission = true,
            DroitConsultation = DroitConsultation.ToutesDemandesSociete,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var soc = SocieteCliente.Soumettre(userId, "TRANS-IVOIRE-LOGISTICS", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "jean@transivoire.ci",
            Email = "jean@transivoire.ci",
            NomAffiche = "Jean Dupont",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(user);

        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, true, true);
        db.UtilisateursSociete.Add(userSoc);

        var file = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);
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
        var numService = new FakeNumerotationService();
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketRepo, dossierRepo);

        var soumettreCmd = new SoumettreDemandeImportCommand(ticketRepo, dossierRepo, socRepo, userSocRepo, fileRepo, numService, journalisation, horloge, uow);
        var dashboardClientQuery = new ObtenirDashboardClientQuery(dossierRepo, ticketRepo, socRepo, userSocRepo);
        var ficheDossierQuery = new ObtenirFicheDossierQuery(dossierRepo, ticketRepo, fileRepo, socRepo, userSocRepo, userInfoRepo);
        var ficheTicketQuery = new ObtenirFicheTicketQuery(ticketRepo, dossierRepo, fileRepo, socRepo, userSocRepo, userInfoRepo, histRepo, docRepo, msgRepo, habRepo, catRepo);

        var prendreEnCharge = new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var attenteClient = new AttenteClientCommand(ticketRepo, new PeriodeAttenteClientRepository(db), autorisation, journalisation, horloge, uow);
        var publierMessage = new PublierMessageCommand(msgRepo, ticketRepo, autorisation, cloisonnement, journalisation, horloge, uow);
        // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.1) : FicheTicket.razor injecte
        // désormais ces deux contrats pour afficher/clôturer/évaluer une réclamation.
        var clotureReclamation = new CloturerReclamationCommand(ticketRepo, autorisation, journalisation, horloge, uow);
        var evaluerReclamation = new EvaluerReclamationCommand(ticketRepo, dossierRepo, userSocRepo, journalisation, horloge, uow, autorisation);

        Services.AddSingleton(soumettreCmd);
        Services.AddSingleton(dashboardClientQuery);
        Services.AddSingleton(ficheDossierQuery);
        Services.AddSingleton(ficheTicketQuery);
        Services.AddSingleton(prendreEnCharge);
        Services.AddSingleton(attenteClient);
        Services.AddSingleton(publierMessage);
        Services.AddSingleton(clotureReclamation);
        Services.AddSingleton(evaluerReclamation);

        // 1. Étape 1 : Rendu Soumission (Zéro champ GUID)
        var cutSoumission = Render<SoumettreDemande>();
        Assert.Contains("TRANS-IVOIRE-LOGISTICS", cutSoumission.Markup);
        Assert.Contains("Jean Dupont", cutSoumission.Markup);
        Assert.DoesNotContain("Identifiant de la société (démonstration)", cutSoumission.Markup);

        // 2. Étape 2 : Création de la demande
        var resultat = await soumettreCmd.ExecuterAsync(new SoumettreDemandeImportRequete(socId, userId, ["BL-E2E-001", "BL-E2E-002"]));
        Assert.Equal(2, resultat.TicketsCrees.Count);
        var dossierId = resultat.Dossier.Id;
        var ticket1Id = resultat.TicketsCrees[0].Id;

        // 3. Étape 3 : Consultation du Dashboard Client
        var cutDashboard = Render<DashboardClient>();
        Assert.Contains("TRANS-IVOIRE-LOGISTICS", cutDashboard.Markup);

        // 4. Étape 4 : Consultation Fiche Dossier
        var cutDossier = Render<FicheDossier>(p => p.Add(x => x.Id, dossierId));
        Assert.Contains("TRANS-IVOIRE-LOGISTICS", cutDossier.Markup);
        Assert.Contains("BL-E2E-001", cutDossier.Markup);
        Assert.Contains("BL-E2E-002", cutDossier.Markup);

        // 5. Étape 5 : Consultation Fiche Ticket
        var cutTicket = Render<FicheTicket>(p => p.Add(x => x.Id, ticket1Id));
        Assert.Contains("BL-E2E-001", cutTicket.Markup);
        Assert.Contains("Pilote Manifeste", cutTicket.Markup);
    }

    private sealed class FakeNumerotationService : IServiceNumerotation
    {
        private int _ticketCompteur;
        private int _dossierCompteur;

        public Task<string> ProchainNumeroTicketAsync(string module, string typeDemande, CancellationToken ct = default)
        {
            var val = Interlocked.Increment(ref _ticketCompteur);
            return Task.FromResult($"TCK-{val:D6}");
        }

        public Task<string> ProchainNumeroDossierAsync(CancellationToken ct = default)
        {
            var val = Interlocked.Increment(ref _dossierCompteur);
            return Task.FromResult($"DOS-{val:D6}");
        }
    }
}
