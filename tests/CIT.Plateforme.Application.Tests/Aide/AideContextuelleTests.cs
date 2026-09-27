using CIT.Plateforme.Application.Aide;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Aide;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Aide;

/// <summary>Aide contextuelle (bouton « ? ») : correspondance d'écran, balisage, provisionnement.</summary>
public sealed class AideContextuelleTests
{
    [Theory]
    [InlineData("/billing-import/soumettre", true)]
    [InlineData("/Billing-Import/Soumettre/", true)]
    [InlineData("/billing-import/soumettre?bl=123#pieces", true)]
    [InlineData("/billing-import/soumettre/etape-2", true)]
    [InlineData("/billing-import/soumettre-bis", false)]
    [InlineData("/billing-import", false)]
    public void Un_article_couvre_son_ecran_et_ses_sous_ecrans_uniquement(string chemin, bool attendu)
    {
        var article = ArticleAide.Creer("/billing-import/soumettre", "T", "T", "C", "C");

        Assert.Equal(attendu, article.Couvre(chemin));
    }

    [Fact]
    public void Le_balisage_distingue_intertitres_puces_etapes_astuces_et_paragraphes()
    {
        var blocs = BlocAide.Analyser("Intro\r\n## Titre\n- puce\n1. étape\n12. autre étape\n! astuce\n\n2026. pas une étape");

        Assert.Equal(
            [TypeBlocAide.Paragraphe, TypeBlocAide.Intertitre, TypeBlocAide.Puce, TypeBlocAide.Etape, TypeBlocAide.Etape, TypeBlocAide.Astuce, TypeBlocAide.Paragraphe],
            blocs.Select(b => b.Type));
        Assert.Equal("étape", blocs[3].Texte);
    }

    [Fact]
    public void Un_article_sans_titre_est_refuse()
    {
        Assert.Throws<ArgumentException>(() => ArticleAide.Creer("/x", " ", "T", "C", "C"));
    }

    [Fact]
    public async Task L_article_le_plus_precis_l_emporte_et_la_langue_est_respectee()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        db.ArticlesAide.AddRange(
            ArticleAide.Creer("/tickets", "Dossier", "File", "Général", "General"),
            ArticleAide.Creer("/tickets/abc/documents", "Pièces", "Documents", "## Détail", "## Detail"));
        await db.SaveChangesAsync();
        var query = new ObtenirAideContextuelleQuery(new ArticleAideRepository(db));

        var precis = await query.ExecuterAsync("/tickets/abc/documents", "en-US");
        var general = await query.ExecuterAsync("/tickets/xyz", "fr-FR");
        var aucun = await query.ExecuterAsync("/inconnu", "fr-FR");

        Assert.Equal("Documents", precis!.Titre);
        Assert.Equal(TypeBlocAide.Intertitre, precis.Blocs[0].Type);
        Assert.Equal("Dossier", general!.Titre);
        Assert.Null(aucun);
    }

    [Fact]
    public async Task Un_article_desactive_n_est_plus_servi()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var article = ArticleAide.Creer("/supervision", "S", "S", "C", "C");
        article.Modifier("S", "S", "C", "C", actif: false);
        db.ArticlesAide.Add(article);
        await db.SaveChangesAsync();

        Assert.Null(await new ObtenirAideContextuelleQuery(new ArticleAideRepository(db)).ExecuterAsync("/supervision", "fr"));
    }

    [Fact]
    public async Task Le_provisionnement_installe_tout_le_contenu_sans_ecraser_un_article_retouche()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var retouche = ArticleAide.Creer("/supervision", "Titre maison", "Custom title", "Contenu maison", "Custom content");
        db.ArticlesAide.Add(retouche);
        await db.SaveChangesAsync();

        await new ProvisionnementAide(db, NullLogger<ProvisionnementAide>.Instance).ExecuterAsync();
        await new ProvisionnementAide(db, NullLogger<ProvisionnementAide>.Instance).ExecuterAsync();

        Assert.Equal(ContenuAideInitial.Articles.Count, await db.ArticlesAide.CountAsync());
        Assert.Equal("Titre maison", (await db.ArticlesAide.SingleAsync(a => a.Route == "/supervision")).TitreFr);
    }

    [Fact]
    public void Le_contenu_de_depart_a_des_routes_uniques_et_chaque_article_est_bilingue()
    {
        var routes = ContenuAideInitial.Articles.Select(a => ArticleAide.NormaliserRoute(a.Route)).ToList();

        Assert.Equal(routes.Count, routes.Distinct().Count());
        Assert.All(ContenuAideInitial.Articles, a =>
            Assert.Equal(BlocAide.Analyser(a.ContenuFr).Count, BlocAide.Analyser(a.ContenuEn).Count));
    }
}
