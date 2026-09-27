using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Aide;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Aide;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Aide;

/// <summary>Centre d'aide : périmètre = menu du compte, recherche, écrans transverses, administration.</summary>
public sealed class CentreAideTests
{
    private static readonly Guid Utilisateur = Guid.NewGuid();

    private static ConsulterCentreAideQuery Construire(PlateformeDbContext db, params string[] permissions)
    {
        var habilitations = new Mock<IHabilitationRepository>();
        habilitations.Setup(r => r.ListerParUtilisateurAsync(Utilisateur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions.Select(p => HabilitationUtilisateur.Attribuer(Utilisateur, p)).ToList());
        var files = new Mock<IFileDeTraitementRepository>();
        files.Setup(r => r.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<FileDeTraitement>());

        return new ConsulterCentreAideQuery(
            new ArticleAideRepository(db), new ConstruireNavigationQuery(habilitations.Object, files.Object));
    }

    private static PlateformeDbContext AvecArticles()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        db.ArticlesAide.AddRange(
            ArticleAide.Creer("/administration/referentiels", "Référentiels métier", "Reference data", "Libellés et ordre.", "Labels and order."),
            ArticleAide.Creer("/billing-import/soumettre", "Demande IMPORT", "IMPORT request", "Saisissez le BL.", "Enter the B/L."),
            ArticleAide.Creer("/comptes/double-authentification", "Sécurité 2FA", "2FA security", "Code à 6 chiffres.", "6-digit code."),
            ArticleAide.Creer("/tickets", "Dossier", "File", "Pièces jointes et échanges.", "Attachments.", transverse: true));
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Le_guide_ne_montre_que_les_ecrans_du_menu_du_compte_plus_les_transverses()
    {
        using var db = AvecArticles();

        var sections = await Construire(db, Permission.AdministrationReferentiels)
            .ExecuterAsync(Utilisateur, EspaceFonctionnel.AdministrationCit, aUneSocieteRattachee: false, "fr-FR", null);

        var titres = sections.SelectMany(s => s.Articles).Select(a => a.Titre).ToList();
        Assert.Contains("Référentiels métier", titres);
        Assert.Contains("Sécurité 2FA", titres);
        Assert.Contains("Dossier", titres);
        Assert.DoesNotContain("Demande IMPORT", titres);
        Assert.Null(sections[^1].Titre);
    }

    [Fact]
    public async Task Sans_la_permission_l_aide_de_l_ecran_n_est_pas_proposee()
    {
        using var db = AvecArticles();

        var sections = await Construire(db)
            .ExecuterAsync(Utilisateur, EspaceFonctionnel.AdministrationCit, aUneSocieteRattachee: false, "fr-FR", null);

        Assert.DoesNotContain(sections.SelectMany(s => s.Articles), a => a.Titre == "Référentiels métier");
    }

    [Fact]
    public async Task La_recherche_ignore_la_casse_et_les_accents_et_suit_la_langue()
    {
        using var db = AvecArticles();
        var query = Construire(db, Permission.AdministrationReferentiels);

        var fr = await query.ExecuterAsync(Utilisateur, EspaceFonctionnel.AdministrationCit, false, "fr-FR", "LIBELLES");
        var en = await query.ExecuterAsync(Utilisateur, EspaceFonctionnel.AdministrationCit, false, "en-US", "labels");

        Assert.Equal("Référentiels métier", Assert.Single(fr.SelectMany(s => s.Articles)).Titre);
        Assert.Equal("Reference data", Assert.Single(en.SelectMany(s => s.Articles)).Titre);
    }

    [Fact]
    public async Task L_administration_cree_puis_modifie_une_fiche_et_trace_l_audit()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var adminId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, adminId), Permission.AdministrationAide));
        db.SaveChanges();
        var commande = new EnregistrerArticleAideCommand(
            new ArticleAideRepository(db), new AutorisationService(db), new JournalisationService(db, HorlogeDeTest.Defaut()), new UniteDeTravail(db));

        var id = await commande.ExecuterAsync(adminId, null, "/Supervision/", "Titre", "Title", "Texte", "Text", actif: true, transverse: false);
        await commande.ExecuterAsync(adminId, id, "ignorée", "Nouveau titre", "New title", "Texte", "Text", actif: false, transverse: false);

        var article = await db.ArticlesAide.SingleAsync();
        Assert.Equal("/supervision", article.Route);
        Assert.Equal("Nouveau titre", article.TitreFr);
        Assert.False(article.Actif);
        Assert.Contains(db.JournalAudit, e => e.Action == "ArticleAideCree");
        Assert.Contains(db.JournalAudit, e => e.Action == "ArticleAideModifie");
        await Assert.ThrowsAsync<RouteAideDejaUtiliseeException>(() =>
            commande.ExecuterAsync(adminId, null, "/supervision", "T", "T", "C", "C", actif: true, transverse: false));
    }

    [Fact]
    public async Task Sans_habilitation_l_administration_des_fiches_est_refusee()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var query = new ListerArticlesAideQuery(new ArticleAideRepository(db), new AutorisationService(db));

        await Assert.ThrowsAsync<AccesRefuseException>(() => query.ExecuterAsync(Guid.NewGuid()));
    }
}
