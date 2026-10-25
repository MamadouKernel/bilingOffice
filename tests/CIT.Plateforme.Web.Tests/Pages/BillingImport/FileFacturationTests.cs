using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.BillingImport;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.BillingImport;

/// <summary>
/// [TEST-SPEC] SOC-04 — la file « Facturation » (Billing IMPORT) doit permettre à l'agent de
/// filtrer par statut et de trier par priorité, pas seulement de subir l'ordre chronologique
/// d'arrivée. Ce test couvre l'écran représentatif choisi pour porter le correctif.
/// </summary>
public sealed class FileFacturationTests : BunitContext
{
    public FileFacturationTests() => LocalisationDeTest.Enregistrer(Services);

    private sealed class FakeAntivirus : IAnalyseAntivirus
    {
        public Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default) =>
            Task.FromResult(StatutAnalyseAntivirus.Sain);
    }

    private sealed class FakeStockage : IStockageDocuments
    {
        public Task<string> EnregistrerAsync(Stream contenu, string nomFichier, CancellationToken ct = default) =>
            Task.FromResult($"documents/{nomFichier}");

        public Task<Stream> LireAsync(string cleStockage, CancellationToken ct = default) =>
            Task.FromResult<Stream>(new MemoryStream());
    }

    private static (PlateformeDbContext Db, Guid AgentId, Guid FileId) ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var agentId = Guid.NewGuid();
        var file = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation);
        db.Files.Add(file);

        var dossier = Dossier.Creer("DOS-FF-01", Guid.NewGuid(), DateTime.UtcNow);
        db.Dossiers.Add(dossier);

        var enAttente = Ticket.Creer("TCK-FF-001", "BillingImport", "Import", file.Id, dossier.Id, DateTime.UtcNow.AddHours(-2), "BL-FF-1");
        var enCours = Ticket.Creer("TCK-FF-002", "BillingImport", "Import", file.Id, dossier.Id, DateTime.UtcNow, "BL-FF-2");
        enCours.PrendreEnCharge(agentId, DateTime.UtcNow);
        db.Tickets.AddRange(enAttente, enCours);
        db.SaveChanges();

        return (db, agentId, file.Id);
    }

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var ticketRepo = new TicketRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var dossierRepo = new DossierRepository(db);
        var documentRepo = new DocumentRepository(db);
        var utilisateurSocieteRepo = new UtilisateurSocieteRepository(db);
        var habilitationRepo = new HabilitationRepository(db);
        var perimetreRepo = new PerimetreSupervisionRepository(db);
        var utilisateurInfoRepo = new UtilisateurInfoRepository(db);
        var autorisation = new AutorisationService(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var priorites = new PrioriteRepository(db);

        var garde = new GardeIdentiteService(utilisateurInfoRepo);
        var controleAcces = new ControleAccesRessourceService(dossierRepo, ticketRepo, utilisateurSocieteRepo, habilitationRepo, perimetreRepo, garde);
        var cloisonnement = new CloisonnementService(ticketRepo, controleAcces);
        var deposerDocument = new DeposerDocumentCommand(
            documentRepo, ticketRepo, new FakeStockage(), new FakeAntivirus(), autorisation, cloisonnement, journalisation, horloge, uow);

        Services.AddSingleton(new ConsulterFileBillingImportQuery(ticketRepo, fileRepo, autorisation, priorites));
        Services.AddSingleton(new CIT.Plateforme.Application.Tickets.PrendreEnChargeEtapeBillingCommand(ticketRepo, autorisation, journalisation, horloge, uow));
        Services.AddScoped<CIT.Plateforme.Web.Services.AnnuaireClients>();
        Services.AddSingleton(new EditerFactureCommand(ticketRepo, dossierRepo, fileRepo, deposerDocument, autorisation, journalisation, horloge, uow));
        Services.AddSingleton(new ReglementDeclaratifCommand(ticketRepo, fileRepo, cloisonnement, autorisation, journalisation, horloge, uow));
    }

    private static FakeContexteUtilisateurWeb ContexteAgent(Guid agentId)
    {
        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent,
            Permissions = [Permission.BillingImportFacturationEditer]
        };
        return fakeContexte;
    }

    [Fact]
    public void LaFile_ExposeUnFiltreDeStatutEtUnTriDePriorite()
    {
        var (db, agentId, fileId) = ConstruireBase();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.BillingImportFacturationEditer, fileId));
        db.SaveChanges();

        Services.AddSingleton<IContexteUtilisateurWeb>(ContexteAgent(agentId));
        EnregistrerServices(db);

        var cut = Render<FileFacturation>();

        // SOC-04 : les contrôles de filtre/tri existent et affichent les deux tickets attendus.
        Assert.NotNull(cut.Find("#filtre-statut"));
        Assert.NotNull(cut.Find("#tri-file"));
        Assert.Contains("TCK-FF-001", cut.Markup);
        Assert.Contains("TCK-FF-002", cut.Markup);
    }

    [Fact]
    public void FiltrerParStatutEnCours_NeGardeQueLeTicketEnCours()
    {
        var (db, agentId, fileId) = ConstruireBase();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.BillingImportFacturationEditer, fileId));
        db.SaveChanges();

        Services.AddSingleton<IContexteUtilisateurWeb>(ContexteAgent(agentId));
        EnregistrerServices(db);

        var cut = Render<FileFacturation>();
        cut.Find("#filtre-statut").Change(nameof(StatutTicket.EnCours));

        cut.WaitForState(() => !cut.Markup.Contains("TCK-FF-001"), TimeSpan.FromSeconds(5));

        Assert.DoesNotContain("TCK-FF-001", cut.Markup);
        Assert.Contains("TCK-FF-002", cut.Markup);
    }
}
