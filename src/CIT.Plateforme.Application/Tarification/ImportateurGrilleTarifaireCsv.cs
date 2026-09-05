using System.Globalization;
using System.Text;

namespace CIT.Plateforme.Application.Tarification;

/// <summary>
/// Une ligne du fichier, exprimée en <b>codes</b> de référentiel.
///
/// Le parseur ne résout plus les valeurs lui-même : il produisait auparavant des énumérations et
/// une catégorie en texte libre, ce qui laissait passer n'importe quel libellé. Depuis la
/// normalisation (analyse Merise § 3.3), la résolution appartient à la commande, seule à disposer
/// des référentiels — et un code inconnu devient une erreur d'import nommée, pas une ligne
/// tarifaire fantôme qu'aucune simulation ne retrouverait.
/// </summary>
public sealed record LigneTarifaireCsvModel(
    string SensCode,
    string TailleCode,
    string CategorieCode,
    decimal Acconage,
    decimal Relevage,
    decimal TarifJourBranchement);

public sealed record ResultatAnalyseCsv(
    bool EstValide,
    IReadOnlyList<LigneTarifaireCsvModel> Lignes,
    IReadOnlyList<string> Erreurs);

/// <summary>
/// Service d'analyse syntaxique atomique d'un fichier CSV de grille tarifaire (SIM-10, CLAR-SIM-01).
/// Reste volontairement pur : aucune dépendance, aucun accès aux référentiels.
/// </summary>
public sealed class ImportateurGrilleTarifaireCsv
{
    private const string EnteteAttendue = "Sens;Taille;Categorie;Acconage;Relevage;BranchementJour";

    public ResultatAnalyseCsv AnalyserEtValider(Stream fluxCsv)
    {
        ArgumentNullException.ThrowIfNull(fluxCsv);

        var erreurs = new List<string>();
        var lignes = new List<LigneTarifaireCsvModel>();
        var clesVues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(fluxCsv, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

        var entete = reader.ReadLine()?.Trim();
        if (entete == null || !string.Equals(entete, EnteteAttendue, StringComparison.OrdinalIgnoreCase))
        {
            return new ResultatAnalyseCsv(false, [], [$"En-tête CSV invalide. Attendu : '{EnteteAttendue}', Obtenu : '{entete}'."]);
        }

        var numeroLigne = 1;
        while (!reader.EndOfStream)
        {
            numeroLigne++;
            var ligneBrute = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(ligneBrute)) continue;

            var colonnes = ligneBrute.Split(';');
            if (colonnes.Length != 6)
            {
                erreurs.Add($"Ligne {numeroLigne} : Format invalide (6 colonnes attendues, {colonnes.Length} trouvées).");
                continue;
            }

            var sensCode = colonnes[0].Trim();
            var tailleCode = colonnes[1].Trim();
            var categorieCode = colonnes[2].Trim();
            var acconageBrut = colonnes[3].Trim();
            var relevageBrut = colonnes[4].Trim();
            var brfBrut = colonnes[5].Trim();

            if (string.IsNullOrWhiteSpace(sensCode))
            {
                erreurs.Add($"Ligne {numeroLigne} : Le sens d'opération est vide.");
            }

            if (string.IsNullOrWhiteSpace(tailleCode))
            {
                erreurs.Add($"Ligne {numeroLigne} : La taille de conteneur est vide.");
            }

            if (string.IsNullOrWhiteSpace(categorieCode))
            {
                erreurs.Add($"Ligne {numeroLigne} : La catégorie de marchandise est vide.");
            }

            if (!decimal.TryParse(acconageBrut, NumberStyles.Any, CultureInfo.InvariantCulture, out var acconage) || acconage < 0)
            {
                erreurs.Add($"Ligne {numeroLigne} : Montant d'acconage '{acconageBrut}' invalide ou négatif.");
            }

            if (!decimal.TryParse(relevageBrut, NumberStyles.Any, CultureInfo.InvariantCulture, out var relevage) || relevage < 0)
            {
                erreurs.Add($"Ligne {numeroLigne} : Montant de relevage '{relevageBrut}' invalide ou négatif.");
            }

            if (!decimal.TryParse(brfBrut, NumberStyles.Any, CultureInfo.InvariantCulture, out var brf) || brf < 0)
            {
                erreurs.Add($"Ligne {numeroLigne} : Tarif jour branchement '{brfBrut}' invalide ou négatif.");
            }

            var cle = $"{sensCode}_{tailleCode}_{categorieCode}";
            if (!clesVues.Add(cle))
            {
                erreurs.Add($"Ligne {numeroLigne} : Détection d'un doublon de clé ({sensCode}, {tailleCode}, '{categorieCode}').");
            }

            if (erreurs.Count == 0)
            {
                lignes.Add(new LigneTarifaireCsvModel(sensCode, tailleCode, categorieCode, acconage, relevage, brf));
            }
        }

        if (erreurs.Count > 0)
        {
            return new ResultatAnalyseCsv(false, [], erreurs);
        }

        return new ResultatAnalyseCsv(true, lignes, []);
    }
}
