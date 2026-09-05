using CIT.Plateforme.Application.Exports;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Exports;

/// <summary>
/// [TEST-SPEC] SOC-14 — le format des exports, et surtout ce qu'il neutralise.
///
/// Une cellule CSV commençant par <c>=</c>, <c>+</c>, <c>-</c> ou <c>@</c> est exécutée comme une
/// formule à l'ouverture par Excel et LibreOffice. Comme certaines colonnes exportées portent des
/// valeurs saisies par des tiers (numéro, libellé de priorité, référence de dossier), l'export
/// serait un vecteur d'injection sans cette neutralisation.
///
/// Ces règles vivaient dans <c>ExporterTicketsCsvQuery</c> seule. Le second export SOC-14 les
/// aurait réécrites de son côté — ou oubliées. Elles sont désormais partagées, et testées ici.
/// </summary>
public sealed class FormatCsvTests
{
    [Theory]
    [InlineData("=1+1")]
    [InlineData("+33600000000")]
    [InlineData("-2")]
    [InlineData("@SUM(A1:A9)")]
    public void Une_cellule_qui_ressemble_a_une_formule_est_neutralisee(string valeur)
    {
        var echappee = FormatCsv.Echapper(valeur);

        Assert.StartsWith("'", echappee, StringComparison.Ordinal);
        Assert.EndsWith(valeur, echappee, StringComparison.Ordinal);
    }

    [Fact]
    public void Un_separateur_dans_une_cellule_ne_casse_pas_la_colonne()
    {
        Assert.Equal("\"Contrôle BL; urgent\"", FormatCsv.Echapper("Contrôle BL; urgent"));
    }

    [Fact]
    public void Un_guillemet_est_double_selon_la_convention_csv()
    {
        Assert.Equal("\"BL dit \"\"ferme\"\"\"", FormatCsv.Echapper("BL dit \"ferme\""));
    }

    [Fact]
    public void Une_cellule_ordinaire_traverse_sans_alteration()
    {
        Assert.Equal("TCK-001", FormatCsv.Echapper("TCK-001"));
        Assert.Equal(string.Empty, FormatCsv.Echapper(null));
    }

    [Fact]
    public void Le_fichier_porte_le_bom_utf8_et_une_ligne_par_enregistrement()
    {
        var octets = FormatCsv.Composer(
            ["Numero", "Libelle"],
            [["TCK-001", "Contrôle BL"], ["TCK-002", "Réclamation"]]);

        Assert.Equal([0xEF, 0xBB, 0xBF], octets[..3]);

        var texte = System.Text.Encoding.UTF8.GetString(octets[3..]);
        var lignes = texte.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, lignes.Length);
        Assert.Equal("Numero;Libelle", lignes[0]);
        Assert.Equal("TCK-002;Réclamation", lignes[2]);
    }

    [Fact]
    public void L_horodatage_est_iso_8601_utc_independant_de_la_culture()
    {
        Assert.Equal(
            "2026-09-04T08:30:00Z",
            FormatCsv.Horodatage(new DateTime(2026, 9, 4, 8, 30, 0, DateTimeKind.Utc)));
    }
}
