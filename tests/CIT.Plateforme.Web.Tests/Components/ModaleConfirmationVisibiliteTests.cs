using Bunit;
using CIT.Plateforme.Web.Components.Shared;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Components;

/// <summary>
/// REMÉDIATION RECETTE UAT (D-01) — LA CONFIRMATION ÉTAIT RENDUE, ET INVISIBLE.
///
/// La modale de confirmation livrée à l'étape UX-P0 fonctionnait : bon texte, bons libellés, bonne
/// logique. Elle était pourtant inutilisable, pour une seule raison — son voile était posé à
/// <c>z-index: 1045</c>, sous la modale Bootstrap qui vaut 1050. Ouverte depuis l'écran des
/// habilitations, elle se retrouvait entièrement recouverte : <c>elementFromPoint</c> au centre du
/// dialogue renvoyait l'en-tête de la modale parente. L'utilisateur cliquait « Tout accorder », ne
/// voyait rien, et n'avait aucun moyen de confirmer ni d'annuler.
///
/// Aucun test unitaire ne pouvait le voir : le DOM était correct. Ce qui manquait, c'était la
/// classe qui place le dialogue sur sa propre couche. Ces tests la verrouillent, et vérifient au
/// passage ce que la recette demandait d'observer : annulation, confirmation, Échap, état occupé.
/// </summary>
public sealed class ModaleConfirmationVisibiliteTests : BunitContext
{
    [Fact]
    public void Fermee_la_modale_ne_rend_rien()
    {
        var rendu = Render<ModaleConfirmation>(p => p
            .Add(m => m.EstOuvert, false));

        Assert.Empty(rendu.Markup.Trim());
    }

    [Fact]
    public void Ouverte_le_voile_porte_la_classe_de_couche_dediee()
    {
        // C'EST LE DÉFAUT D-01. Sans cette classe, le voile retombe sur la couche de navigation
        // (1045), sous la modale Bootstrap (1050), et le dialogue redevient invisible.
        var rendu = Render<ModaleConfirmation>(p => p
            .Add(m => m.EstOuvert, true));

        var voile = rendu.Find(".cit-modal-backdrop");

        Assert.Contains("cit-voile-confirmation", voile.ClassList);
    }

    [Fact]
    public void Ouverte_la_modale_expose_une_semantique_de_dialogue()
    {
        var rendu = Render<ModaleConfirmation>(p => p
            .Add(m => m.EstOuvert, true)
            .Add(m => m.Titre, "Accorder toutes les permissions")
            .Add(m => m.Message, "Ce compte recevra 54 permissions."));

        var dialogue = rendu.Find(".cit-modal-dialog");

        Assert.Equal("dialog", dialogue.GetAttribute("role"));
        Assert.Equal("true", dialogue.GetAttribute("aria-modal"));
        Assert.NotNull(dialogue.GetAttribute("aria-labelledby"));
        Assert.NotNull(dialogue.GetAttribute("aria-describedby"));
        Assert.Contains("Accorder toutes les permissions", rendu.Markup);
        Assert.Contains("Ce compte recevra 54 permissions.", rendu.Markup);
    }

    [Fact]
    public void La_confirmation_declenche_le_rappel_de_confirmation()
    {
        var confirme = false;
        var rendu = Render<ModaleConfirmation>(p => p
            .Add(m => m.EstOuvert, true)
            .Add(m => m.BoutonConfirmerLibelle, "Accorder les permissions")
            .Add(m => m.OnConfirmer, () => confirme = true));

        rendu.Find(".btn-danger").Click();

        Assert.True(confirme);
    }

    [Fact]
    public void L_annulation_declenche_le_rappel_d_annulation()
    {
        var annule = false;
        var rendu = Render<ModaleConfirmation>(p => p
            .Add(m => m.EstOuvert, true)
            .Add(m => m.OnAnnuler, () => annule = true));

        rendu.Find(".btn-outline-secondary").Click();

        Assert.True(annule);
    }

    [Fact]
    public void Echap_annule()
    {
        var annule = false;
        var rendu = Render<ModaleConfirmation>(p => p
            .Add(m => m.EstOuvert, true)
            .Add(m => m.OnAnnuler, () => annule = true));

        rendu.Find(".cit-modal-backdrop").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        Assert.True(annule);
    }

    [Fact]
    public void Le_clic_sur_l_arriere_plan_annule()
    {
        var annule = false;
        var rendu = Render<ModaleConfirmation>(p => p
            .Add(m => m.EstOuvert, true)
            .Add(m => m.OnAnnuler, () => annule = true));

        rendu.Find(".cit-modal-backdrop").Click();

        Assert.True(annule);
    }

    [Fact]
    public void Pendant_le_traitement_les_deux_boutons_sont_neutralises()
    {
        var confirme = 0;
        var annule = 0;
        // Le libellé « en cours de traitement » n'est pas passé par l'appelant : il vient du
        // défaut LOCALISÉ du composant (i18n LOT 1). On fixe donc explicitement la culture pour
        // que l'assertion soit déterministe, quelle que soit la culture de la machine de build.
        using var _ = new CulturePourLeTest("fr-FR");
        var rendu = Render<ModaleConfirmation>(p => p
            .Add(m => m.EstOuvert, true)
            .Add(m => m.Desactive, true)
            .Add(m => m.OnConfirmer, () => confirme++)
            .Add(m => m.OnAnnuler, () => annule++));

        Assert.Contains("spinner-border", rendu.Markup);
        Assert.Contains("Traitement en cours", rendu.Markup);

        rendu.Find(".btn-danger").Click();
        rendu.Find(".btn-outline-secondary").Click();
        rendu.Find(".cit-modal-backdrop").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        // Protection du double-clic ET des fermetures accidentelles pendant une action partie.
        Assert.Equal(0, confirme);
        Assert.Equal(0, annule);
    }

    [Fact]
    public void Les_libelles_par_defaut_suivent_la_culture()
    {
        // i18n LOT 1 — les libellés que la modale possède elle-même (titre, message, Annuler,
        // Confirmer, Traitement) sont rendus dans la langue active quand l'appelant n'en fournit pas.
        using (var _ = new CulturePourLeTest("fr-FR"))
        {
            var fr = Render<ModaleConfirmation>(p => p.Add(m => m.EstOuvert, true));
            Assert.Contains("Confirmer", fr.Markup);
            Assert.Contains("Annuler", fr.Markup);
        }

        using (var _ = new CulturePourLeTest("en-US"))
        {
            var en = Render<ModaleConfirmation>(p => p.Add(m => m.EstOuvert, true));
            Assert.Contains("Confirm", en.Markup);
            Assert.Contains("Cancel", en.Markup);
            // 0 texte FR accidentel : les libellés français par défaut ne doivent PAS fuir en anglais.
            Assert.DoesNotContain("Annuler", en.Markup);
        }
    }

    /// <summary>Fixe temporairement la culture UI courante le temps d'un test, puis la restaure.</summary>
    private sealed class CulturePourLeTest : IDisposable
    {
        private readonly System.Globalization.CultureInfo _precedenteUi = System.Globalization.CultureInfo.CurrentUICulture;
        private readonly System.Globalization.CultureInfo _precedente = System.Globalization.CultureInfo.CurrentCulture;

        public CulturePourLeTest(string culture)
        {
            var c = new System.Globalization.CultureInfo(culture);
            System.Globalization.CultureInfo.CurrentUICulture = c;
            System.Globalization.CultureInfo.CurrentCulture = c;
        }

        public void Dispose()
        {
            System.Globalization.CultureInfo.CurrentUICulture = _precedenteUi;
            System.Globalization.CultureInfo.CurrentCulture = _precedente;
        }
    }

    [Fact]
    public void La_feuille_de_style_place_la_confirmation_au_dessus_de_la_modale_Bootstrap()
    {
        // Le DOM ne dit rien de la superposition : c'est la CSS qui décide, et c'est elle qui
        // portait le défaut. On vérifie donc la déclaration elle-même.
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.NotNull(racine);
        var css = File.ReadAllText(Path.Combine(
            racine!.FullName, "src", "CIT.Plateforme.Web", "wwwroot", "app.css"));

        Assert.Contains(".cit-modal-backdrop.cit-voile-confirmation", css);
        Assert.Contains("--z-confirmation: 1085;", css);
        Assert.Contains("z-index: var(--z-confirmation);", css);

        // Et le voile de navigation reste, lui, sous la modale Bootstrap : les deux usages du même
        // voile n'ont pas les mêmes contraintes.
        Assert.Contains("--z-voile-navigation: 1045;", css);
    }
}
