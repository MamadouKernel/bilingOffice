using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Nfr;

/// <summary>
/// FEATURE 011 — CERTIFICATION NFR-02 · suite TEST-SPEC « NFR-CERT-02 » (T054→T060).
///
/// Texte CDC (NFR-02, Must) : « Portail client responsive, utilisable sur mobile (une part
/// importante des clients et chauffeurs utilisera un smartphone). »
///
/// Preuve STRUCTURELLE en 5 points (research.md D4) :
///   Point 1 (T054) : directive viewport mobile déclarée dans App.razor ;
///   Point 2 (T055) : points de rupture CSS (@media) définis dans app.css ;
///   Point 3 (T056) : primitives de mise en page adaptative (flexbox / grid / pourcentages) ;
///   Point 4 (T057) : conteneurs défilants ou adaptatifs pour les éléments tabulaires ;
///   Point 5 (T058) : navigation et boutons atteignables dans la variante réduite.
///
/// CONTRÔLE (I9) : aucune largeur de test (320, 375, 768, 1024 px) n'est un seuil CDC contractuel.
///
/// ══════════════════════════════════════════════════════════════════════════════════════════════
/// DETTE DE RECETTE (T060) :
/// VALIDATION VISUELLE NAVIGATEUR : NON COUVERTE PAR F011 — DETTE RECETTE.
/// La certification prouve que le contrat technique responsive est en place dans le balisage
/// et les feuilles de style, sans présumer du rendu visuel au pixel près sur terminal réel.
/// ══════════════════════════════════════════════════════════════════════════════════════════════
/// </summary>
public sealed class NfrCert02ResponsiveTests
{
    private static string TrouverRacineProjetWeb()
    {
        var rep = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(rep))
        {
            var cheminCible = Path.Combine(rep, "src", "CIT.Plateforme.Web");
            if (Directory.Exists(cheminCible))
            {
                return cheminCible;
            }

            var parent = Directory.GetParent(rep);
            rep = parent?.FullName;
        }

        var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "CIT.Plateforme.Web"));
        return Directory.Exists(fallback) ? fallback : throw new DirectoryNotFoundException("Impossible de localiser src/CIT.Plateforme.Web");
    }

    // ---- Point 1 (T054) : directive viewport mobile déclarée ----

    [Fact]
    public void Directive_viewport_mobile_est_declaree_dans_le_document_principal()
    {
        var racineWeb = TrouverRacineProjetWeb();
        var cheminApp = Path.Combine(racineWeb, "Components", "App.razor");
        Assert.True(File.Exists(cheminApp), $"App.razor introuvable à {cheminApp}");

        var contenu = File.ReadAllText(cheminApp);

        // La balise <meta name="viewport" content="width=device-width, initial-scale=1.0" /> est obligatoire
        // pour que les navigateurs mobiles adaptent la zone de rendu.
        Assert.Contains("name=\"viewport\"", contenu, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("width=device-width", contenu, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Point 2 (T055) : points de rupture CSS définis ----

    [Fact]
    public void Points_de_rupture_css_sont_definis_dans_la_feuille_de_styles()
    {
        var racineWeb = TrouverRacineProjetWeb();
        var cheminCss = Path.Combine(racineWeb, "wwwroot", "app.css");
        Assert.True(File.Exists(cheminCss), $"app.css introuvable à {cheminCss}");

        var contenuCss = File.ReadAllText(cheminCss);

        // La feuille de style doit contenir des règles @media pour adapter la disposition
        Assert.Contains("@media", contenuCss, StringComparison.OrdinalIgnoreCase);
    }

    // ---- Point 3 (T056) : primitives de mise en page adaptative ----

    [Fact]
    public void Primitives_de_mise_en_page_adaptative_sont_utilisees()
    {
        var racineWeb = TrouverRacineProjetWeb();
        var cheminCss = Path.Combine(racineWeb, "wwwroot", "app.css");
        var contenuCss = File.ReadAllText(cheminCss);

        // Vérifier l'usage de Flexbox ou CSS Grid pour des dispositions non rigides
        var utiliseFlexOuGrid = contenuCss.Contains("display: flex", StringComparison.OrdinalIgnoreCase)
                             || contenuCss.Contains("display:flex", StringComparison.OrdinalIgnoreCase)
                             || contenuCss.Contains("display: grid", StringComparison.OrdinalIgnoreCase)
                             || contenuCss.Contains("display:grid", StringComparison.OrdinalIgnoreCase);

        Assert.True(utiliseFlexOuGrid, "La feuille de style doit employer flex ou grid pour la disposition responsive (NFR-02).");
    }

    // ---- Point 4 (T057) : conteneurs adaptés pour les tableaux ----

    [Fact]
    public void Regles_de_defilement_ou_conteneurs_responsives_existent()
    {
        var racineWeb = TrouverRacineProjetWeb();
        var cheminCss = Path.Combine(racineWeb, "wwwroot", "app.css");
        var contenuCss = File.ReadAllText(cheminCss);

        // Présence de règles de gestion du débordement (overflow, max-width, etc.)
        var gereDebordement = contenuCss.Contains("overflow", StringComparison.OrdinalIgnoreCase)
                           || contenuCss.Contains("max-width", StringComparison.OrdinalIgnoreCase);

        Assert.True(gereDebordement, "Le CSS doit contenir des règles de gestion de débordement adaptatif (NFR-02).");
    }

    // ---- Point 5 (T058) : navigation et actions utilisables ----

    [Fact]
    public void Navigation_principale_est_structuree_de_facon_accessible()
    {
        var racineWeb = TrouverRacineProjetWeb();
        var cheminNav = Path.Combine(racineWeb, "Components", "Layout", "NavMenu.razor");
        Assert.True(File.Exists(cheminNav), $"NavMenu.razor introuvable à {cheminNav}");

        var contenuNav = File.ReadAllText(cheminNav);

        // Le menu de navigation contient les liens fonctionnels principaux
        Assert.Contains("NavLink", contenuNav, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=", contenuNav, StringComparison.OrdinalIgnoreCase);
    }
}
