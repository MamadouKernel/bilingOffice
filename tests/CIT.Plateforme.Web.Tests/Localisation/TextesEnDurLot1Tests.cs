using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

/// <summary>
/// LOT 1 i18n — GARDE « TEXTES EN DUR » CIBLÉE SUR LA COQUILLE PARTAGÉE.
///
/// Portée volontairement MINIMALE et explicite : les composants de la coquille (barre supérieure,
/// menu latéral, bascule d'espace, modale de confirmation, toast, état vide, indicateur de
/// chargement, accès refusé, fil d'Ariane, superposition de reconnexion). Pour chacun, on vérifie
/// deux choses :
///   - POSITIF : le texte utilisateur passe bien par la source de traduction (présence de
///     <c>Loc[</c> ou <c>SharedResourcesLocalizer.Get(</c>) — si on retire le câblage i18n, le test
///     tombe ;
///   - NÉGATIF : un littéral français précis que ce lot a retiré ne réapparaît pas en dur.
///
/// Les littéraux « négatifs » sont choisis pour ne PAS apparaître dans les commentaires du fichier
/// (sinon la garde deviendrait un faux positif). Cette garde ne prétend pas couvrir les pages hors
/// LOT 1 : celles-ci sont traitées, et gardées, dans leurs lots respectifs.
/// </summary>
public sealed class TextesEnDurLot1Tests
{
    private const string RepertoireComposants = "Components";

    [Theory]
    // (chemin relatif sous Components, marqueur i18n attendu, littéral FR interdit en dur)
    [InlineData("Layout/NavMenu.razor", "@Loc[lien.Libelle]", "Chargement du menu...")]
    [InlineData("Layout/HeaderBar.razor", "@Loc[\"Header.ThemeToggleTitle\"]", "Basculer le thème")]
    [InlineData("Layout/PersonaSwitcher.razor", "SharedResourcesLocalizer.Get(\"Header.SpaceClient\")", "=> \"Espace Client\"")]
    [InlineData("Layout/ReconnectModal.razor", "SharedResourcesLocalizer.Get(\"Reconnect.Rejoining\")", "Rejoining the server")]
    [InlineData("Shared/ModaleConfirmation.razor", "SharedResourcesLocalizer.Get(\"Common.Processing\")", "= \"Traitement en cours")]
    [InlineData("Shared/ToastNotification.razor", "SharedResourcesLocalizer.Get(\"Common.CloseNotification\")", "aria-label=\"Fermer la notification\"")]
    [InlineData("Shared/EtatVide.razor", "SharedResourcesLocalizer.Get(\"Common.EmptyTitle\")", "= \"Aucun élément\"")]
    [InlineData("Shared/IndicateurChargement.razor", "SharedResourcesLocalizer.Get(\"Common.LoadingData\")", "Chargement des données en cours")]
    [InlineData("Shared/PermissionDenied.razor", "SharedResourcesLocalizer.Get(\"AccessDenied.Title\")", "Accès Refusé <span")]
    [InlineData("Shared/FilAriane.razor", "SharedResourcesLocalizer.Get(\"Common.Breadcrumb\")", "Fil d'Ariane\" class=")]
    public void La_coquille_partagee_passe_par_le_localizer_et_ne_recode_pas_le_texte_en_dur(
        string cheminRelatif, string marqueurI18n, string litteralInterdit)
    {
        var source = File.ReadAllText(CheminComposant(cheminRelatif));

        Assert.Contains(marqueurI18n, source);
        Assert.DoesNotContain(litteralInterdit, source);
    }

    private static string CheminComposant(string cheminRelatif)
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.True(racine is not null, "Racine du dépôt introuvable depuis " + AppContext.BaseDirectory);
        return Path.Combine(
            racine!.FullName, "src", "CIT.Plateforme.Web", RepertoireComposants,
            cheminRelatif.Replace('/', Path.DirectorySeparatorChar));
    }
}
