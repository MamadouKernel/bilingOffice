using System.Globalization;
using System.Linq;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Web.Resources;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Localisation;

/// <summary>
/// LOT 1 i18n — MENU LATÉRAL FR/EN.
///
/// Le menu résout ses libellés par leur texte français canonique (clé) via la source de traduction
/// partagée : <c>@Loc[groupe.Titre]</c> et <c>@Loc[lien.Libelle]</c>. Ces gardes verrouillent trois
/// propriétés :
///   1. tout titre de groupe et tout libellé d'entrée du catalogue a une traduction FR ET EN ;
///   2. les traductions EN attendues sont bien celles publiées (cible de mutation : changer une
///      traduction EN fait tomber le test) ;
///   3. les ROUTES du menu ne dépendent pas de la culture — pour un même utilisateur, routes FR ==
///      routes EN. La langue ne change que des libellés, jamais une destination ni une permission.
/// </summary>
public sealed class NavigationI18nTests
{
    private static readonly EspaceFonctionnel[] EspacesAvecMenu =
    [
        EspaceFonctionnel.Client,
        EspaceFonctionnel.AdministrateurSociete,
        EspaceFonctionnel.Agent,
        EspaceFonctionnel.Supervision,
        EspaceFonctionnel.AdministrationCit,
    ];

    private static IEnumerable<string> TousLesLibelles()
    {
        foreach (var espace in EspacesAvecMenu)
        {
            foreach (var groupe in CatalogueNavigation.Pour(espace))
            {
                yield return groupe.Titre;
                foreach (var entree in groupe.Entrees)
                {
                    yield return entree.Libelle;
                }
            }
        }
    }

    [Fact]
    public void Chaque_libelle_du_menu_a_une_traduction_FR_et_EN()
    {
        var fr = SharedResourcesLocalizer.GetFrenchDictionary();
        var en = SharedResourcesLocalizer.GetEnglishDictionary();

        var sansTraduction = TousLesLibelles()
            .Distinct()
            .Where(libelle => !fr.ContainsKey(libelle) || !en.ContainsKey(libelle))
            .OrderBy(l => l)
            .ToList();

        Assert.True(
            sansTraduction.Count == 0,
            "Libellés de menu sans clé de traduction (FR et/ou EN manquante) : "
            + string.Join(", ", sansTraduction));
    }

    [Fact]
    public void Aucun_libelle_de_menu_ne_reste_en_francais_en_anglais()
    {
        // En anglais, un libellé rendu à l'identique du français signale une traduction oubliée.
        // Exceptions légitimes : noms propres / sigles identiques dans les deux langues.
        var identiquesTolerees = new HashSet<string>(System.StringComparer.Ordinal)
        {
            "Notifications",          // identique FR/EN
            "Sécurité (2FA)",         // rendu "Security (2FA)" — voir assertion ciblée ci-dessous
        };

        var localizer = new SharedResourcesLocalizer();
        var precedente = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            var restentEnFrancais = TousLesLibelles()
                .Distinct()
                .Where(libelle => !identiquesTolerees.Contains(libelle))
                .Where(libelle => string.Equals(localizer[libelle].Value, libelle, System.StringComparison.Ordinal))
                .OrderBy(l => l)
                .ToList();

            Assert.True(
                restentEnFrancais.Count == 0,
                "Libellés de menu non traduits en anglais (valeur EN == texte FR) : "
                + string.Join(", ", restentEnFrancais));
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }

    [Theory]
    // Cible de mutation : chaque paire (clé FR canonique, traduction EN attendue). Changer une
    // traduction EN dans le dictionnaire fait tomber ce test.
    [InlineData("Tableau de bord", "Dashboard")]
    [InlineData("Mes demandes", "My Requests")]
    [InlineData("Comptes agents", "Agent Accounts")]
    [InlineData("Journal d'audit", "Audit Log")]
    [InlineData("Sécurité (2FA)", "Security (2FA)")]
    [InlineData("Simulation de Proforma", "Proforma Simulation")]
    [InlineData("Pilote Manifeste", "Manifest Pilot")]
    [InlineData("Contrôle Doc Import", "Import Doc Control")]
    [InlineData("Validation Caisse Export", "Export Cashier Validation")]
    [InlineData("Vue Supervision", "Supervision View")]
    [InlineData("Espace Structure", "Organisation")]
    [InlineData("Administration & Sécurité", "Administration & Security")]
    public void Les_traductions_EN_du_menu_sont_celles_publiees(string cleFr, string attenduEn)
    {
        var localizer = new SharedResourcesLocalizer();
        var precedente = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Equal(attenduEn, localizer[cleFr].Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }

    [Fact]
    public void Les_routes_du_menu_sont_identiques_quelle_que_soit_la_culture()
    {
        // Les permissions et les routes sont déclarées dans le catalogue, indépendamment de toute
        // culture. On le VÉRIFIE : la même énumération sous fr-FR et sous en-US doit rendre
        // exactement les mêmes routes (et les mêmes permissions requises), dans le même ordre.
        var precedente = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var routesFr = EmpreinteRoutesEtPermissions();

            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var routesEn = EmpreinteRoutesEtPermissions();

            Assert.Equal(routesFr, routesEn);
        }
        finally
        {
            CultureInfo.CurrentUICulture = precedente;
        }
    }

    private static List<string> EmpreinteRoutesEtPermissions()
    {
        var empreinte = new List<string>();
        foreach (var espace in EspacesAvecMenu)
        {
            foreach (var groupe in CatalogueNavigation.Pour(espace))
            {
                foreach (var entree in groupe.Entrees)
                {
                    empreinte.Add(
                        $"{espace}|{entree.Route}|{string.Join('+', entree.PermissionsRequises)}");
                }
            }
        }

        return empreinte;
    }

    [Fact]
    public void NavMenu_localise_les_libelles_mais_jamais_les_routes()
    {
        var source = File.ReadAllText(CheminNavMenu());

        // Les libellés passent par le localizer…
        Assert.Contains("@Loc[groupe.Titre]", source);
        Assert.Contains("@Loc[lien.Libelle]", source);

        // …mais la destination reste la route brute du catalogue (jamais localisée).
        Assert.Contains("href=\"@lien.Route\"", source);
        Assert.DoesNotContain("Loc[lien.Route]", source);
    }

    private static string CheminNavMenu()
    {
        var racine = new DirectoryInfo(AppContext.BaseDirectory);
        while (racine is not null && !Directory.Exists(Path.Combine(racine.FullName, "src")))
        {
            racine = racine.Parent;
        }

        Assert.NotNull(racine);
        return Path.Combine(
            racine!.FullName, "src", "CIT.Plateforme.Web", "Components", "Layout", "NavMenu.razor");
    }
}
