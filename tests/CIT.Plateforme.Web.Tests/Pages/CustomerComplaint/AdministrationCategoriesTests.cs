using System;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Application.Web;
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

/// <summary>
/// T030 (closure-review.md "DÉCISION FINALE T030 / REC-02") — accès à l'écran d'administration des
/// catégories de réclamation.
/// </summary>
public sealed class AdministrationCategoriesTests : BunitContext
{
    private static (PlateformeDbContext Db, Guid AdminId) ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var adminId = Guid.NewGuid();
        db.Set<CategorieReclamation>().Add(CategorieReclamation.Creer("FACT", "Facturation", Guid.NewGuid()));
        db.SaveChanges();

        return (db, adminId);
    }

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var catRepo = new CategorieReclamationRepository(db);
        var autorisation = new AutorisationService(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);

        // REMÉDIATION RECETTE UAT (D-14) : la page propose désormais un choix de file cible, et les
        // deux commandes vérifient ce choix. Les deux services correspondants doivent donc être
        // résolvables, sans quoi la page ne s'afficherait plus.
        var fileRepo = new FileDeTraitementRepository(db);
        var garde = new GardeFileCibleReclamation(fileRepo);

        Services.AddSingleton(new ListerToutesCategoriesReclamationQuery(catRepo, autorisation));
        Services.AddSingleton(new ListerFilesCibleReclamationQuery(fileRepo, autorisation));
        Services.AddSingleton(new ModifierCategoriesReclamationCommand(catRepo, garde, autorisation, journalisation, uow));
        Services.AddSingleton(new CreerCategorieReclamationCommand(catRepo, garde, autorisation, journalisation, uow));
        Services.AddSingleton(new ListerCategoriesReclamationQuery(catRepo));
    }

    [Fact]
    public void A_AdminHabilite_AccedeALaPage()
    {
        var (db, adminId) = ConstruireBase();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AdministrationCategoriesReclamation));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = adminId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<AdministrationCategories>();

        Assert.Contains("FACT", cut.Markup);
        Assert.DoesNotContain("403", cut.Markup);
    }

    [Fact]
    public void B_UtilisateurNonHabilite_AffichePermissionDenied()
    {
        var (db, adminId) = ConstruireBase();
        // Aucune habilitation AdministrationCategoriesReclamation attribuée.

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = adminId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<AdministrationCategories>();

        Assert.Contains("403", cut.Markup);
        Assert.Contains("Accès Refusé", cut.Markup);
        Assert.DoesNotContain("FACT", cut.Markup);
    }

    // REMÉDIATION FINALE REC-02 — admin habilité voit le bouton d'ajout.
    [Fact]
    public void AdminHabilite_VoitLeBoutonNouvelleCategorie()
    {
        var (db, adminId) = ConstruireBase();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AdministrationCategoriesReclamation));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = adminId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<AdministrationCategories>();

        Assert.Contains("Nouvelle catégorie", cut.Markup);
    }

    // Création valide via l'écran réel -> la catégorie apparaît dans le tableau, ET devient
    // disponible pour ListerCategoriesReclamationQuery (utilisée par NouvelleReclamation.razor).
    [Fact]
    public async Task CreationValide_CategorieAffichee_EtDisponiblePourNouvelleReclamation()
    {
        var (db, adminId) = ConstruireBase();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AdministrationCategoriesReclamation));

        // REMEDIATION RECETTE UAT (D-14) : une categorie sans file cible refuse ensuite TOUTE
        // reclamation. L'ecran exige donc desormais ce choix, et le referentiel doit proposer au
        // moins une file du module. C'etait precisement ce qui manquait en recette : la creation
        // reussissait, et le module restait inatteignable.
        var fileSupport = CIT.Plateforme.Domain.Files.FileDeTraitement.Creer(
            CIT.Plateforme.Application.CustomerComplaint.SoumettreReclamationCommand.Module,
            "Support Operationnel");
        db.Files.Add(fileSupport);
        db.SaveChanges();
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = adminId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.AdministrationCit
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<AdministrationCategories>();
        cut.Find("button").Click(); // "➕ Nouvelle catégorie" (premier bouton, formulaire fermé)

        cut.Find("#nouveauCode").Input("URGE");
        cut.Find("#nouveauLibelle").Input("Urgence");
        cut.Find("#nouvelleFile").Change(fileSupport.Id.ToString());
        cut.Find("button.btn-success").Click();

        Assert.Contains("URGE", cut.Markup);
        Assert.Contains("Urgence", cut.Markup);

        var listerCategories = Services.BuildServiceProvider().GetRequiredService<ListerCategoriesReclamationQuery>();
        var categoriesDisponibles = await listerCategories.ExecuterAsync();
        Assert.Contains(categoriesDisponibles, c => c.Code == "URGE");
    }
}
