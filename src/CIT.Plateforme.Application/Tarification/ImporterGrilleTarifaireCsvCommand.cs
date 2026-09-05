using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Tarification;

/// <summary>
/// Import CSV d'une grille. <c>Parametres</c> non renseigné applique le paramétrage initial de
/// l'Annexe A — c'est le cas courant : le fichier porte les tarifs, pas les taux ni les tranches.
/// </summary>
public sealed record ImporterGrilleTarifaireCsvCommande(
    Guid AgentId,
    string NumeroVersion,
    DateTime DateEffetUtc,
    Stream FluxCsv,
    ParametresGrilleSaisie? Parametres = null);

public sealed record ResultatImportCsvVue(
    bool Succes,
    int NombreLignesImportees,
    Guid? VersionId,
    IReadOnlyList<string> Erreurs);

public sealed class ImporterGrilleTarifaireCsvCommandHandler(
    IVersionGrilleTarifaireRepository repository,
    IReferentielsTarifairesRepository referentielsRepository,
    ImportateurGrilleTarifaireCsv importateur,
    IAutorisationService autorisation)
{
    public async Task<ResultatImportCsvVue> HandleAsync(ImporterGrilleTarifaireCsvCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);

        // FEATURE 005 (spec.md §20, research.md D15) : l'import CSV atomique modifie la tarification
        // au même titre qu'une création manuelle — même permission exigée.
        await autorisation.ExigerAsync(commande.AgentId, Permission.TarificationAdministrer, ct: ct);

        var analyse = importateur.AnalyserEtValider(commande.FluxCsv);
        if (!analyse.EstValide)
        {
            return new ResultatImportCsvVue(false, 0, null, analyse.Erreurs);
        }

        var referentiels = await referentielsRepository.ChargerAsync(ct);

        var version = ConstructeurGrilleTarifaire.Construire(
            commande.NumeroVersion,
            commande.DateEffetUtc,
            commande.AgentId,
            DateTime.UtcNow,
            commande.Parametres,
            [.. analyse.Lignes.Select(l => new LigneTarifaireSaisie(
                l.SensCode, l.TailleCode, l.CategorieCode, l.Acconage, l.Relevage, l.TarifJourBranchement))],
            referentiels,
            out var erreurs);

        // ATOMICITÉ (CLAR-SIM-01) : un seul code inconnu annule tout l'import. Créer la grille
        // amputée des lignes fautives donnerait une tarification silencieusement incomplète —
        // et « tarif non trouvé » se manifesterait bien plus tard, chez un client.
        if (erreurs.Count > 0)
        {
            return new ResultatImportCsvVue(false, 0, null, erreurs);
        }

        await repository.AjouterAsync(version, ct);

        return new ResultatImportCsvVue(true, analyse.Lignes.Count, version.Id, []);
    }
}
