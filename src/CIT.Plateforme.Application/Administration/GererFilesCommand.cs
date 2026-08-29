using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>
/// Contrat C-01 — Gérer les files (créer/modifier une file, sa stratégie d'affectation active).
/// CDC : SOC-04, WKF-01, AFF-07.
/// </summary>
public sealed record CreerFileRequete(Guid AdministrateurId, string Module, string Nom, StrategieAffectation StrategieInitiale);

public sealed class GererFilesCommand(
    IFileDeTraitementRepository files,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> CreerAsync(CreerFileRequete requete, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(requete.AdministrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var file = FileDeTraitement.Creer(requete.Module, requete.Nom);
        file.DefinirStrategie(requete.StrategieInitiale);

        await files.AjouterAsync(file, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            requete.AdministrateurId, "FileCreee", nameof(FileDeTraitement), file.Id, null,
            $"module={requete.Module};nom={requete.Nom};strategie={requete.StrategieInitiale}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return file.Id;
    }

    public async Task ChangerStrategieAsync(
        Guid administrateurId, Guid fileId, StrategieAffectation nouvelleStrategie, string? competenceRequise = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var file = await files.ObtenirParIdAsync(fileId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"File {fileId} introuvable.");

        var ancienne = file.StrategieActive;
        file.DefinirStrategie(nouvelleStrategie, competenceRequise);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "FileStrategieModifiee", nameof(FileDeTraitement), file.Id,
            ancienne.ToString(), nouvelleStrategie.ToString(), ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
