using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>
/// Lecture des files de traitement pour l'administration (contrat C-01, volet consultation).
/// Existe pour que l'interface propose des files DÉSIGNÉES PAR LEUR NOM plutôt que de faire
/// saisir un identifiant technique : la couche Web n'accède jamais directement au repository.
/// </summary>
public sealed record FileAdministrable(Guid Id, string Module, string Nom, StrategieAffectation StrategieActive, string? CompetenceRequise);

public sealed class ListerFilesQuery(
    IFileDeTraitementRepository files,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<FileAdministrable>> ExecuterAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        return await ListerInterneAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Variante destinée au choix d'une file CIBLE de transfert : l'habilitation exigée est celle
    /// du transfert lui-même (<c>Permission.TicketTransferer</c>) et non l'administration des files,
    /// pour qu'un opérateur autorisé à transférer puisse désigner la file par son nom.
    /// </summary>
    public async Task<IReadOnlyList<FileAdministrable>> ListerPourTransfertAsync(Guid initiateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(initiateurId, Permission.TicketTransferer, ct: ct).ConfigureAwait(false);

        return await ListerInterneAsync(ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<FileAdministrable>> ListerInterneAsync(CancellationToken ct)
    {
        var toutes = await files.ListerAsync(ct).ConfigureAwait(false);

        return toutes
            .Select(f => new FileAdministrable(f.Id, f.Module, f.Nom, f.StrategieActive, f.CompetenceRequise))
            .OrderBy(f => f.Module)
            .ThenBy(f => f.Nom)
            .ToList();
    }
}
