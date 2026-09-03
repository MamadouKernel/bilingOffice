using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Un motif de transfert tel que présenté à l'administrateur et à l'opérateur.</summary>
public sealed record MotifTransfertVue(Guid Id, string Code, string Libelle, bool EstActif, int Ordre);

/// <summary>
/// § 9.10 / § 17.3 — « les motifs de transfert » sont administrables sans développement.
///
/// Aucun motif n'est livré par défaut : le CDC n'en nomme aucun, et en inventer reviendrait à
/// décider à la place du métier. Tant que le référentiel est vide, un motif libre reste recevable —
/// même convention que les pièces exigées à l'ouverture d'un compte.
/// </summary>
public sealed class GererMotifsTransfertCommand(
    IMotifTransfertRepository motifs,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> DefinirAsync(
        Guid appelantId, string code, string libelle, int ordre, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        if (await motifs.CodeExisteAsync(code.Trim(), ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Un motif portant le code « {code.Trim()} » existe déjà.");
        }

        var motif = MotifTransfert.Definir(code, libelle, ordre);
        await motifs.AjouterAsync(motif, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "MotifTransfertDefini", nameof(MotifTransfert), motif.Id, null, motif.Code, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return motif.Id;
    }

    public async Task ModifierAsync(
        Guid appelantId, Guid motifId, string libelle, int ordre, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var motif = await motifs.ObtenirParIdAsync(motifId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Motif {motifId} introuvable.");

        var avant = motif.Libelle;
        motif.Modifier(libelle, ordre);

        await journalisation.ConsignerAuditAsync(
            appelantId, "MotifTransfertModifie", nameof(MotifTransfert), motif.Id,
            avant, motif.Libelle, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Désactiver plutôt que supprimer : l'historique des transferts déjà tracés référence ce code,
    /// et le journal d'audit est append-only (SEC-05).
    /// </summary>
    public async Task DefinirActivationAsync(Guid appelantId, Guid motifId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var motif = await motifs.ObtenirParIdAsync(motifId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Motif {motifId} introuvable.");

        var avant = motif.EstActif ? "actif" : "inactif";
        if (actif)
        {
            motif.Activer();
        }
        else
        {
            motif.Desactiver();
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, actif ? "MotifTransfertActive" : "MotifTransfertDesactive", nameof(MotifTransfert), motif.Id,
            avant, actif ? "actif" : "inactif", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>§ 9.10 — lecture du référentiel des motifs de transfert.</summary>
public sealed class ListerMotifsTransfertQuery(
    IMotifTransfertRepository motifs,
    IAutorisationService autorisation)
{
    /// <summary>Écran de paramétrage : voit AUSSI les motifs désactivés, sans quoi ils seraient
    /// irrécupérables.</summary>
    public async Task<IReadOnlyList<MotifTransfertVue>> ExecuterAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        return await ListerInterneAsync(activesUniquement: false, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Variante destinée au choix du motif lors d'un transfert : l'habilitation exigée est celle du
    /// transfert lui-même, pour qu'un opérateur autorisé à transférer puisse désigner le motif par
    /// son libellé. Même distinction que <see cref="ListerFilesQuery.ListerPourTransfertAsync"/>.
    /// </summary>
    public async Task<IReadOnlyList<MotifTransfertVue>> ListerPourTransfertAsync(Guid initiateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(initiateurId, Permission.TicketTransferer, ct: ct).ConfigureAwait(false);

        return await ListerInterneAsync(activesUniquement: true, ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<MotifTransfertVue>> ListerInterneAsync(bool activesUniquement, CancellationToken ct)
    {
        var tous = await motifs.ListerAsync(activesUniquement, ct).ConfigureAwait(false);

        return tous
            .OrderBy(m => m.Ordre)
            .ThenBy(m => m.Libelle, StringComparer.CurrentCulture)
            .Select(m => new MotifTransfertVue(m.Id, m.Code, m.Libelle, m.EstActif, m.Ordre))
            .ToList();
    }
}
