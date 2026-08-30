using System.Globalization;
using System.Text;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record LigneTarifaireCsvModel(
    SensTrafic Sens,
    TailleConteneur Taille,
    string CategorieMarchandise,
    decimal Acconage,
    decimal Relevage,
    decimal TarifJourBranchement);

public sealed record ResultatAnalyseCsv(
    bool EstValide,
    IReadOnlyList<LigneTarifaireCsvModel> Lignes,
    IReadOnlyList<string> Erreurs);

/// <summary>
/// Service d'analyse syntaxique et métier atomique d'un fichier CSV de grille tarifaire (SIM-10, CLAR-SIM-01).
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

            var sensBrut = colonnes[0].Trim();
            var tailleBrut = colonnes[1].Trim();
            var categorie = colonnes[2].Trim();
            var acconageBrut = colonnes[3].Trim();
            var relevageBrut = colonnes[4].Trim();
            var brfBrut = colonnes[5].Trim();

            if (!Enum.TryParse<SensTrafic>(sensBrut, ignoreCase: true, out var sens))
            {
                erreurs.Add($"Ligne {numeroLigne} : Sens de trafic '{sensBrut}' inconnu.");
            }

            if (!int.TryParse(tailleBrut, out var tailleInt) || (tailleInt != 20 && tailleInt != 40))
            {
                erreurs.Add($"Ligne {numeroLigne} : Taille '{tailleBrut}' invalide (seules 20 ou 40 sont autorisées).");
            }
            var taille = tailleInt == 20 ? TailleConteneur.VingtPieds : TailleConteneur.QuarantePieds;

            if (string.IsNullOrWhiteSpace(categorie))
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

            var cle = $"{sens}_{taille}_{categorie}";
            if (!clesVues.Add(cle))
            {
                erreurs.Add($"Ligne {numeroLigne} : Détection d'un doublon de clé ({sens}, {tailleInt}\", '{categorie}').");
            }

            if (erreurs.Count == 0)
            {
                lignes.Add(new LigneTarifaireCsvModel(sens, taille, categorie, acconage, relevage, brf));
            }
        }

        if (erreurs.Count > 0)
        {
            return new ResultatAnalyseCsv(false, [], erreurs);
        }

        return new ResultatAnalyseCsv(true, lignes, []);
    }
}
