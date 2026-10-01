using ClosedXML.Excel;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Tarification;

/// <summary>Contenu d'un export de grille tarifaire, prêt à être téléchargé.</summary>
public sealed record ExportGrilleTarifaire(byte[] Contenu, string NomFichier);

/// <summary>
/// SIM-10 (réciproque de l'import) — export d'une version de grille tarifaire au format Excel
/// (.xlsx). Les colonnes sont exactement celles attendues par l'import CSV
/// (<c>Sens;Taille;Categorie;Acconage;Relevage;BranchementJour</c>), si bien que le fichier
/// exporté peut être corrigé puis réimporté sans ressaisie. Même habilitation que l'import
/// (<see cref="Permission.TarificationAdministrer"/>).
/// </summary>
public sealed class ExporterGrilleTarifaireExcelQuery(
    IVersionGrilleTarifaireRepository versions,
    IReferentielsTarifairesRepository referentiels,
    IAutorisationService autorisation)
{
    private static readonly string[] Entetes =
        ["Sens", "Taille", "Categorie", "Acconage", "Relevage", "BranchementJour"];

    public async Task<ExportGrilleTarifaire> ExecuterAsync(Guid agentId, Guid versionId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(agentId, Permission.TarificationAdministrer, ct: ct).ConfigureAwait(false);

        var version = await versions.ObtenirParIdAsync(versionId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Version de grille {versionId} introuvable.");

        // Référentiels chargés en entier (actifs comme retirés) : une version ancienne peut
        // référencer un code désactivé depuis, et doit rester exportable lisiblement.
        var sens = (await referentiels.ListerSensAsync(actifsSeulement: false, ct).ConfigureAwait(false))
            .ToDictionary(s => s.Id, s => s.Code);
        var tailles = (await referentiels.ListerTaillesAsync(actifsSeulement: false, ct).ConfigureAwait(false))
            .ToDictionary(t => t.Id, t => t.Code);
        var categories = (await referentiels.ListerCategoriesAsync(sensOperationId: null, actifsSeulement: false, ct).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Code);

        using var classeur = new XLWorkbook();
        var feuille = classeur.Worksheets.Add("Grille tarifaire");

        for (var c = 0; c < Entetes.Length; c++)
        {
            feuille.Cell(1, c + 1).Value = Entetes[c];
        }
        feuille.Row(1).Style.Font.Bold = true;

        var ligneExcel = 2;
        var lignesTriees = version.Lignes
            .OrderBy(l => sens.GetValueOrDefault(l.SensOperationId, ""), StringComparer.Ordinal)
            .ThenBy(l => tailles.GetValueOrDefault(l.TailleConteneurId, ""), StringComparer.Ordinal)
            .ThenBy(l => categories.GetValueOrDefault(l.CategorieMarchandiseId, ""), StringComparer.Ordinal);

        foreach (var ligne in lignesTriees)
        {
            feuille.Cell(ligneExcel, 1).Value = sens.GetValueOrDefault(ligne.SensOperationId, "?");
            feuille.Cell(ligneExcel, 2).Value = tailles.GetValueOrDefault(ligne.TailleConteneurId, "?");
            feuille.Cell(ligneExcel, 3).Value = categories.GetValueOrDefault(ligne.CategorieMarchandiseId, "?");
            feuille.Cell(ligneExcel, 4).Value = ligne.Acconage;
            feuille.Cell(ligneExcel, 5).Value = ligne.Relevage;
            feuille.Cell(ligneExcel, 6).Value = ligne.TarifJourBranchement;
            ligneExcel++;
        }

        feuille.Columns().AdjustToContents();

        using var memoire = new MemoryStream();
        classeur.SaveAs(memoire);

        var nom = $"grille-tarifaire-{version.NumeroVersion}.xlsx";
        return new ExportGrilleTarifaire(memoire.ToArray(), nom);
    }
}
