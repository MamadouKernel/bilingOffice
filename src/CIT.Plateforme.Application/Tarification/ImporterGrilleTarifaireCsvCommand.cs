using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record ImporterGrilleTarifaireCsvCommande(
    Guid AgentId,
    string NumeroVersion,
    DateTime DateEffetUtc,
    ParametresTarification Parametres,
    Stream FluxCsv);

public sealed record ResultatImportCsvVue(
    bool Succes,
    int NombreLignesImportees,
    Guid? VersionId,
    IReadOnlyList<string> Erreurs);

public sealed class ImporterGrilleTarifaireCsvCommandHandler(
    IVersionGrilleTarifaireRepository repository,
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

        var version = VersionGrilleTarifaire.Creer(
            commande.NumeroVersion,
            commande.DateEffetUtc,
            commande.AgentId,
            DateTime.UtcNow,
            commande.Parametres);

        foreach (var l in analyse.Lignes)
        {
            version.AjouterLigne(l.Sens, l.Taille, l.CategorieMarchandise, l.Acconage, l.Relevage, l.TarifJourBranchement);
        }

        await repository.AjouterAsync(version, ct);

        return new ResultatImportCsvVue(true, analyse.Lignes.Count, version.Id, []);
    }
}
