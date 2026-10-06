using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Vue d'une pièce exigée, avec le nom de l'étape résolu — l'écran ne manipule jamais
/// d'identifiant technique brut.</summary>
public sealed record PieceExigeeEtapeVue(
    Guid Id,
    string Code,
    string Libelle,
    Guid FileId,
    string NomEtape,
    string? Module,
    bool Obligatoire,
    bool EstActif,
    int Ordre);

/// <summary>
/// SOC-07 / § 17.3 — administration des pièces exigées par étape, sans développement.
///
/// L'habilitation exigée est <see cref="Permission.AdministrationFiles"/> : les pièces attendues
/// se rattachent à une étape du parcours, et c'est le même rôle qui administre les files.
/// </summary>
public sealed class GererPiecesExigeesEtapeCommand(
    IPieceExigeeEtapeRepository pieces,
    IFileDeTraitementRepository files,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<IReadOnlyList<PieceExigeeEtapeVue>> ListerAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var toutes = await pieces.ListerAsync(ct).ConfigureAwait(false);
        var noms = (await files.ListerAsync(ct).ConfigureAwait(false)).ToDictionary(f => f.Id, f => f.Nom);

        return toutes.Select(p => new PieceExigeeEtapeVue(
            p.Id, p.Code, p.Libelle, p.FileId,
            noms.TryGetValue(p.FileId, out var nom) ? nom : p.FileId.ToString(),
            p.Module, p.Obligatoire, p.EstActif, p.Ordre)).ToList();
    }

    public async Task<Guid> DefinirAsync(
        Guid administrateurId, string code, string libelle, Guid fileId, bool obligatoire, int ordre,
        string? module = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var piece = PieceExigeeEtape.Definir(code, libelle, fileId, obligatoire, ordre, module);
        await pieces.AjouterAsync(piece, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "PieceExigeeEtapeDefinie", nameof(PieceExigeeEtape), piece.Id,
            null, $"{code} à l'étape {fileId} (module={module ?? "tous"}, obligatoire={obligatoire})", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return piece.Id;
    }

    public async Task ModifierAsync(
        Guid administrateurId, Guid pieceId, string libelle, bool obligatoire, int ordre, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var piece = await pieces.ObtenirAsync(pieceId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Pièce exigée {pieceId} introuvable.");

        var avant = $"{piece.Libelle} (obligatoire={piece.Obligatoire}, ordre={piece.Ordre})";
        piece.Modifier(libelle, obligatoire, ordre);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "PieceExigeeEtapeModifiee", nameof(PieceExigeeEtape), pieceId,
            avant, $"{piece.Libelle} (obligatoire={piece.Obligatoire}, ordre={piece.Ordre})", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Activation/désactivation plutôt que suppression : les documents déjà déposés
    /// référencent ce code, et leur historique doit rester lisible (SEC-05).</summary>
    public async Task DefinirActivationAsync(Guid administrateurId, Guid pieceId, bool active, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var piece = await pieces.ObtenirAsync(pieceId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Pièce exigée {pieceId} introuvable.");

        if (active)
        {
            piece.Activer();
        }
        else
        {
            piece.Desactiver();
        }

        await journalisation.ConsignerAuditAsync(
            administrateurId, active ? "PieceExigeeEtapeActivee" : "PieceExigeeEtapeDesactivee",
            nameof(PieceExigeeEtape), pieceId, null, piece.Code, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// SOC-07 — les pièces attendues à une étape donnée, pour l'écran de dépôt. Seules les pièces
    /// actives sont retournées : une pièce désactivée n'est plus exigée, mais reste lisible dans
    /// l'historique des documents qui la référencent.
    /// </summary>
    public async Task<IReadOnlyList<PieceExigeeEtape>> ListerAttenduesAsync(
        string module, Guid fileId, CancellationToken ct = default)
    {
        var toutes = await pieces.ListerAsync(ct).ConfigureAwait(false);
        return toutes.Where(p => p.EstAttendue(module, fileId)).OrderBy(p => p.Ordre).ToList();
    }
}
