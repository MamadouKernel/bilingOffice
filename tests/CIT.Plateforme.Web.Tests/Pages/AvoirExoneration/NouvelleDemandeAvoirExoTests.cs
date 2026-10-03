using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.AvoirExoneration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.AvoirExoneration;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.AvoirExoneration;

public sealed class FakeStockageDocuments : IStockageDocuments
{
    public Task<string> EnregistrerAsync(Stream contenu, string nomFichier, CancellationToken ct = default)
        => Task.FromResult($"documents/{nomFichier}");

    public Task<Stream> LireAsync(string cleStockage, CancellationToken ct = default)
        => Task.FromResult<Stream>(new MemoryStream());
}

public sealed class FakeNumerotationAvoirExoService : IServiceNumerotation
{
    public Task<string> ProchainNumeroTicketAsync(string module, string typeDemande, CancellationToken ct = default)
        => Task.FromResult(typeDemande == "Avoir" ? "AVO-2026-00001" : "EXO-2026-00001");

    public Task<string> ProchainNumeroDossierAsync(CancellationToken ct = default)
        => Task.FromResult("DOS-2026-00001");
}

public sealed class FakeHorlogeAvoirExo : IHorloge
{
    public DateTime MaintenantUtc => new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
}

public sealed class NouvelleDemandeAvoirExoTests : BunitContext
{
    public NouvelleDemandeAvoirExoTests() => LocalisationDeTest.Enregistrer(Services);

    private static (PlateformeDbContext Db, Guid ClientId, Guid SocieteId) ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(clientId, "BOLLORE AFRICA", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);

        var userSoc = UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);

        var file = FileDeTraitement.Creer("AvoirExoneration", "Avoir & Exonération");
        db.Files.Add(file);

        db.SaveChanges();
        return (db, clientId, societeId);
    }

    private void EnregistrerServices(PlateformeDbContext db, Guid clientId, Guid societeId)
    {
        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var liaisonRepo = new LiaisonTicketRepository(db);
        var docRepo = new DocumentRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var uow = new UniteDeTravail(db);
        var stockage = new FakeStockageDocuments();
        var numerotation = new FakeNumerotationAvoirExoService();
        var horloge = new FakeHorlogeAvoirExo();
        var journalisation = new JournalisationService(db, horloge);
        var auto = new AutorisationService(db);

        var soumettreCmd = new SoumettreDemandeAvoirExonerationCommand(
            ticketRepo, dossierRepo, fileRepo, liaisonRepo, docRepo,
            stockage, Moq.Mock.Of<CIT.Plateforme.Domain.Documents.IAnalyseAntivirus>(), userSocRepo, numerotation, journalisation, horloge, auto, uow);

        Services.AddSingleton(soumettreCmd);

        // REMÉDIATION D-18 : la page charge désormais les demandes du client pour alimenter le
        // sélecteur de liaison — le harnais doit fournir la requête correspondante.
        Services.AddSingleton(new CIT.Plateforme.Application.Client.ObtenirDashboardClientQuery(
            dossierRepo, ticketRepo, new SocieteClienteRepository(db), userSocRepo));

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = clientId,
            SocieteId = societeId,
            SocieteNom = "BOLLORE AFRICA",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
    }

    [Fact]
    public void Formulaire_RenduInitial_AfficheChampsObligatoiresEtBoutonDesactiveSansPiece()
    {
        var (db, clientId, societeId) = ConstruireBase();
        EnregistrerServices(db, clientId, societeId);

        var cut = Render<NouvelleDemandeAvoirExo>();

        Assert.Contains("Demande d'Avoir ou d'Exonération", cut.Markup);
        Assert.Contains("Type de demande", cut.Markup);
        Assert.Contains("Objet de la demande", cut.Markup);
        Assert.Contains("Exposé des motifs", cut.Markup);

        var btn = cut.Find("#btnSoumettre");
        Assert.True(btn.HasAttribute("disabled"));
    }
}
