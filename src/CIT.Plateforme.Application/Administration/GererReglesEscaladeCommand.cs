using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Vue d'une règle d'escalade, avec les libellés résolus — l'écran ne manipule jamais
/// d'identifiants techniques bruts.</summary>
public sealed record RegleEscaladeVue(
    Guid Id,
    ActionEscalade Action,
    string? Module,
    string? TypeDemande,
    Guid? FileId,
    string NomFile,
    Guid? PrioriteCibleId,
    string LibellePrioriteCible,
    bool EstActive,
    int Ordre);

/// <summary>
/// ALE-03 / § 17.3 — administration du référentiel des règles d'escalade, sans développement.
///
/// L'habilitation exigée est <see cref="Permission.AdministrationSla"/> : l'escalade est le
/// prolongement du SLA, et les deux écrans s'administrent ensemble.
/// </summary>
public sealed class GererReglesEscaladeCommand(
    IRegleEscaladeRepository regles,
    IFileDeTraitementRepository files,
    IPrioriteRepository priorites,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<IReadOnlyList<RegleEscaladeVue>> ListerAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var toutes = await regles.ListerAsync(ct).ConfigureAwait(false);
        var nomsFiles = (await files.ListerAsync(ct).ConfigureAwait(false)).ToDictionary(f => f.Id, f => f.Nom);
        var libellesPriorites = (await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false)).ToDictionary(n => n.Id, n => n.Libelle);

        return toutes.Select(r => new RegleEscaladeVue(
            r.Id,
            r.Action,
            r.Module,
            r.TypeDemande,
            r.FileId,
            r.FileId is Guid f && nomsFiles.TryGetValue(f, out var nom) ? nom : "Toutes",
            r.PrioriteCibleId,
            r.PrioriteCibleId is Guid p && libellesPriorites.TryGetValue(p, out var lib) ? lib : "—",
            r.EstActive,
            r.Ordre)).ToList();
    }

    public async Task<Guid> DefinirAsync(
        Guid administrateurId,
        ActionEscalade action,
        string? module = null,
        string? typeDemande = null,
        Guid? fileId = null,
        Guid? prioriteCibleId = null,
        int ordre = 0,
        CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var regle = RegleEscalade.Definir(action, module, typeDemande, fileId, prioriteCibleId, ordre);
        await regles.AjouterAsync(regle, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            administrateurId, "RegleEscaladeDefinie", nameof(RegleEscalade), regle.Id,
            null, $"{action} (module={module ?? "tous"}, file={fileId?.ToString() ?? "toutes"})", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return regle.Id;
    }

    /// <summary>Activation/désactivation plutôt que suppression : les escalades déjà appliquées
    /// référencent la règle, et le journal d'audit est append-only (SEC-05).</summary>
    public async Task DefinirActivationAsync(Guid administrateurId, Guid regleId, bool active, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationSla, ct: ct).ConfigureAwait(false);

        var regle = await regles.ObtenirAsync(regleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Règle d'escalade {regleId} introuvable.");

        if (active)
        {
            regle.Activer();
        }
        else
        {
            regle.Desactiver();
        }

        await journalisation.ConsignerAuditAsync(
            administrateurId, active ? "RegleEscaladeActivee" : "RegleEscaladeDesactivee", nameof(RegleEscalade), regleId,
            null, regle.Action.ToString(), ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
