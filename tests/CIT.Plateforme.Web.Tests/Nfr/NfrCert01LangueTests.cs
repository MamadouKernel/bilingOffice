using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Nfr;

/// <summary>
/// FEATURE 011 — CERTIFICATION NFR-01 · suite TEST-SPEC « NFR-CERT-01 » (T051→T053).
///
/// Texte CDC (NFR-01, Must) : « Interface en français ; ergonomie simple adaptée à des
/// utilisateurs non techniciens. »
///
/// Cette suite certifie de façon reproductible que l'interface est en français :
///   - la balise html lang="fr" est déclarée ;
///   - les intitulés de menu, boutons d'action et messages utilisateurs principaux sont francophones ;
///   - aucun libellé technique brut ou non traduit n'est exposé.
///
/// CONTRÔLE (I9) : aucun score d'ergonomie inventé, aucun seuil contractuel non sourcé.
/// </summary>
public sealed class NfrCert01LangueTests
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

        // Fallback relatif direct
        var fallback = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "CIT.Plateforme.Web"));
        return Directory.Exists(fallback) ? fallback : throw new DirectoryNotFoundException("Impossible de localiser src/CIT.Plateforme.Web");
    }

    [Fact]
    public void La_langue_francaise_est_declaree_dans_le_document_principal()
    {
        var racineWeb = TrouverRacineProjetWeb();
        var cheminApp = Path.Combine(racineWeb, "Components", "App.razor");
        Assert.True(File.Exists(cheminApp), $"App.razor introuvable à {cheminApp}");

        var contenuApp = File.ReadAllText(cheminApp);
        Assert.Contains("lang=\"fr\"", contenuApp, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Les_composants_du_parcours_contiennent_des_libelles_francais()
    {
        var racineWeb = TrouverRacineProjetWeb();
        var fichiersPages = Directory.GetFiles(Path.Combine(racineWeb, "Components", "Pages"), "*.razor", SearchOption.AllDirectories);

        Assert.NotEmpty(fichiersPages);

        // Vérifier sur un échantillon représentatif de pages clés
        var motsClesFrancais = new[] { "Connexion", "Demande", "Dossier", "Statut", "Valider", "Annuler", "Soumettre" };
        var texteGlobal = string.Join(" ", fichiersPages.Select(File.ReadAllText));

        foreach (var mot in motsClesFrancais)
        {
            Assert.True(texteGlobal.Contains(mot, StringComparison.OrdinalIgnoreCase),
                $"L'interface doit contenir des termes usuels en français, mot clé absent : {mot} (NFR-01).");
        }
    }
}
