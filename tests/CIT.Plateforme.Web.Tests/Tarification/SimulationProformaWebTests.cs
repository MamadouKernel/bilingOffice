using System.Runtime.CompilerServices;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Tarification;

/// <summary>
/// [TEST-SPEC] (brief d'implémentation §25, §26, §33) : vérifie mécaniquement, sur le code source
/// réel des 4 pages Blazor de la Feature 005, le respect des principes constitutionnels déjà établis
/// pour les Features 001-004 : zéro GUID saisi manuellement (l'identité provient exclusivement de
/// <c>IContexteUtilisateurWeb</c>), et zéro accès direct à <c>PlateformeDbContext</c> ou à un
/// repository Infrastructure depuis une page Razor (toute lecture passe par un handler Application
/// autorisé, cf. précédent <c>ConsulterFileBillingImportQuery</c> de la remédiation Feature 003).
/// </summary>
public sealed class SimulationProformaWebTests
{
    private static string CheminSource([CallerFilePath] string ici = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ici)!, "..", "..", "..", "src", "CIT.Plateforme.Web", "Components", "Pages", "Tarification"));

    public static TheoryData<string> PagesTarification => new()
    {
        "SimulationProformaPage.razor",
        "HistoriqueSimulationsPage.razor",
        "DetailSimulationPage.razor",
        "GestionGrillesTarifaires.razor",
    };

    [Theory]
    [MemberData(nameof(PagesTarification))]
    public void Page_ne_contient_aucune_saisie_manuelle_de_Guid(string nomFichier)
    {
        var chemin = Path.Combine(CheminSource(), nomFichier);
        Assert.True(File.Exists(chemin), $"Fichier introuvable : {chemin}");
        var contenu = File.ReadAllText(chemin);

        Assert.DoesNotContain("Guid.Parse", contenu);
        Assert.DoesNotContain("Guid.TryParse", contenu);
        // « Votre identifiant » / « identifiant (démonstration) » sont les libellés historiques des
        // anciennes pages exigeant une saisie manuelle de GUID (cf. audit UI/UX F001-F003) — leur
        // absence confirme que l'identité provient du contexte serveur, jamais d'un champ libre.
        Assert.DoesNotContain("identifiant (démonstration)", contenu, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(PagesTarification))]
    public void Page_utilise_IContexteUtilisateurWeb_pour_resoudre_l_identite(string nomFichier)
    {
        var chemin = Path.Combine(CheminSource(), nomFichier);
        var contenu = File.ReadAllText(chemin);

        Assert.Contains("IContexteUtilisateurWeb", contenu);
        Assert.Contains("ContexteService.ObtenirContexteAsync", contenu);
    }

    [Theory]
    [MemberData(nameof(PagesTarification))]
    public void Page_n_accede_jamais_directement_a_PlateformeDbContext_ou_a_un_repository(string nomFichier)
    {
        var chemin = Path.Combine(CheminSource(), nomFichier);
        var contenu = File.ReadAllText(chemin);

        Assert.DoesNotContain("PlateformeDbContext", contenu);

        var lignesInjection = contenu.Split('\n').Where(l => l.TrimStart().StartsWith("@inject"));
        Assert.DoesNotContain(lignesInjection, l => l.Contains("Repository"));
    }

    [Fact]
    public void Page_admin_affiche_un_etat_non_autorise_en_cas_d_AccesRefuseException()
    {
        var chemin = Path.Combine(CheminSource(), "GestionGrillesTarifaires.razor");
        var contenu = File.ReadAllText(chemin);

        Assert.Contains("AccesRefuseException", contenu);
        Assert.Contains("PermissionDenied", contenu);
    }
}
