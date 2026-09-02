using System;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.CustomerComplaint;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.CustomerComplaint;

public sealed class NouvelleReclamationTests : BunitContext
{
    private static (PlateformeDbContext Db, Guid ClientId, Guid SocieteId, Guid CategorieId) ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(clientId, "TRANS-AFRIQUE", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);

        var userSoc = UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);

        var file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        db.Files.Add(file);

        var cat = CategorieReclamation.Creer("FACT", "Erreur facturation", file.Id);
        db.Set<CategorieReclamation>().Add(cat);

        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(clientId, Permission.ComplaintSoumettre));
        db.SaveChanges();

        return (db, clientId, societeId, cat.Id);
    }

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var catRepo = new CategorieReclamationRepository(db);
        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var autorisation = new AutorisationService(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var numerotation = new FakeNumerotationService();

        Services.AddSingleton(new ListerCategoriesReclamationQuery(catRepo));
        Services.AddSingleton(new SoumettreReclamationCommand(
            ticketRepo, catRepo, dossierRepo, userSocRepo, uow, horloge, numerotation, autorisation, journalisation));
    }

    [Fact]
    public void ClientHabilite_AccedeAuFormulaireEtVoitCategories()
    {
        var (db, clientId, societeId, catId) = ConstruireBase();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = clientId,
            SocieteId = societeId,
            SocieteNom = "TRANS-AFRIQUE",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<NouvelleReclamation>();

        Assert.Contains("Nouvelle réclamation", cut.Markup);
        Assert.Contains("Erreur facturation", cut.Markup);
        Assert.DoesNotContain("alert-danger", cut.Markup);
    }

    [Fact]
    public void UtilisateurNonAuthentifie_AfficheMessageNonAutorise()
    {
        var (db, _, _, _) = ConstruireBase();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            EstAuthentifie = false,
            SocieteId = null
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<NouvelleReclamation>();

        Assert.Contains("Veuillez vous connecter", cut.Markup);
    }

    [Fact]
    public void SoumissionValide_CreeReclamationEtAfficheConfirmation()
    {
        var (db, clientId, societeId, catId) = ConstruireBase();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = clientId,
            SocieteId = societeId,
            SocieteNom = "TRANS-AFRIQUE",
            Nature = NatureUtilisateur.Client,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Client
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<NouvelleReclamation>();

        cut.Find("#categorie").Change(catId.ToString());
        cut.Find("#objet").Input("Contestation facture F-1234");
        cut.Find("#description").Input("La facture contient un montant de stationnement indu.");

        var bouton = cut.Find("button.btn-primary");
        bouton.Click();

        cut.WaitForState(() => cut.Markup.Contains("Réclamation soumise avec succès"), TimeSpan.FromSeconds(5));

        Assert.Contains("Réclamation soumise avec succès", cut.Markup);
        Assert.Contains("Voir mes réclamations", cut.Markup);
    }

    private sealed class FakeNumerotationService : IServiceNumerotation
    {
        private int _ticketCompteur;
        private int _dossierCompteur;

        public Task<string> ProchainNumeroTicketAsync(string module, string typeDemande, CancellationToken ct = default)
        {
            var val = Interlocked.Increment(ref _ticketCompteur);
            return Task.FromResult($"REC-{val:D6}");
        }

        public Task<string> ProchainNumeroDossierAsync(CancellationToken ct = default)
        {
            var val = Interlocked.Increment(ref _dossierCompteur);
            return Task.FromResult($"DOS-{val:D6}");
        }
    }
}
