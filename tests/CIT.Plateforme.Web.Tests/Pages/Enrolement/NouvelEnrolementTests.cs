using System;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Enrolement;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CIT.Plateforme.Web.Tests.Support;

namespace CIT.Plateforme.Web.Tests.Pages.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T052-T053. C8-01.
public sealed class NouvelEnrolementTests : BunitContext
{
    public NouvelEnrolementTests() => LocalisationDeTest.Enregistrer(Services);

    private static PlateformeDbContext ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new PlateformeDbContext(options);
        db.Files.Add(FileDeTraitement.Creer("Enrolement", "Agent Gate"));
        db.SaveChanges();
        return db;
    }

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var autorisation = new AutorisationService(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var numerotation = new FakeNumerotationService();

        var champsFormulaireRepo = new ChampFormulaireEnrolementRepository(db);
        var valeursChampRepo = new ValeurChampEnrolementRepository(db);
        Services.AddSingleton(new SoumettreEnrolementCommand(ticketRepo, dossierRepo, fileRepo, champsFormulaireRepo, valeursChampRepo, numerotation, journalisation, horloge, autorisation, uow));
        Services.AddSingleton(new CIT.Plateforme.Application.Administration.ListerChampsFormulaireEnrolementQuery(champsFormulaireRepo, autorisation));

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Services.AddSingleton(new DeposerDocumentCommand(
            new DocumentRepository(db), ticketRepo, new StockageDocumentsFichiers(configuration, new EnvironnementDeTest("Development")), new AucuneAnalyseAntivirusConfiguree(),
            autorisation, new CloisonnementService(ticketRepo, CIT.Plateforme.Web.Tests.Support.FabriqueSecurite.ControleAcces(db)), journalisation, horloge, uow));
    }

    [Fact]
    public void TransporteurHabilite_AccedeAuFormulaire()
    {
        var db = ConstruireBase();
        var transporteurId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = transporteurId,
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client,
            Permissions = [Permission.EnrolementSoumettre]
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<NouvelEnrolement>();

        Assert.Contains("Nouvel enrôlement", cut.Markup);
        Assert.DoesNotContain("alert-danger", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonAuthentifie_AfficheMessageNonAutorise()
    {
        // ENR-SEC-01 : un utilisateur anonyme ne doit jamais accéder au formulaire de soumission.
        var db = ConstruireBase();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with { EstAuthentifie = false, Permissions = [] };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<NouvelEnrolement>();

        Assert.Contains("Veuillez vous connecter", cut.Markup);
    }

    [Fact]
    public void TransporteurSansPermission_AffichePermissionDenied()
    {
        var db = ConstruireBase();
        var transporteurId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = transporteurId,
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client,
            Permissions = []
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<NouvelEnrolement>();

        Assert.Contains("403", cut.Markup);
    }

    [Fact]
    public void SoumissionValide_CreeEnrolementEtAfficheConfirmation()
    {
        var db = ConstruireBase();
        var transporteurId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, transporteurId), Permission.EnrolementSoumettre));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = transporteurId,
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client,
            Permissions = [Permission.EnrolementSoumettre]
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<NouvelEnrolement>();

        cut.Find("#objet").Input("Enrôlement chauffeur");
        cut.Find("#description").Input("Camion frigorifique, dépôt d'Abidjan.");

        var bouton = cut.Find("button.btn-primary");
        bouton.Click();

        cut.WaitForState(() => cut.Markup.Contains("Enrôlement soumis avec succès"), TimeSpan.FromSeconds(5));

        Assert.Contains("Enrôlement soumis avec succès", cut.Markup);
    }

    private sealed class FakeNumerotationService : IServiceNumerotation
    {
        private int _ticketCompteur;
        private int _dossierCompteur;

        public Task<string> ProchainNumeroTicketAsync(string module, string typeDemande, CancellationToken ct = default) =>
            Task.FromResult($"ENR-{Interlocked.Increment(ref _ticketCompteur):D6}");

        public Task<string> ProchainNumeroDossierAsync(CancellationToken ct = default) =>
            Task.FromResult($"DOS-{Interlocked.Increment(ref _dossierCompteur):D6}");
    }
}
