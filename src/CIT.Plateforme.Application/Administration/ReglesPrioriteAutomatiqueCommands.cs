using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Administration;

/// <summary>Une règle d'attribution automatique de priorité telle que présentée à l'administrateur.</summary>
public sealed record ReglePrioriteAutomatiqueVue(
    Guid Id, string? Module, string? TypeDemande, string? TypeClient, Guid PrioriteId, bool EstActif, int Ordre);

/// <summary>
/// PRI-01 — « la priorité peut être attribuée automatiquement selon des règles métier ». Ce
/// référentiel est administrable sans développement, même schéma que
/// <see cref="GererMotifsTransfertCommand"/> (§ 9.10).
///
/// Aucune règle n'est livrée par défaut : le CDC ne fixe aucune correspondance module/type →
/// priorité — en inventer reviendrait à décider à la place du métier.
/// </summary>
public sealed class GererReglesPrioriteAutomatiqueCommand(
    IReglePrioriteAutomatiqueRepository regles,
    IPrioriteRepository priorites,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> DefinirAsync(
        Guid appelantId, string? module, string? typeDemande, string? typeClient, Guid prioriteId, int ordre, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        _ = await priorites.ObtenirNiveauAsync(prioriteId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Niveau de priorité {prioriteId} introuvable.");

        var regle = ReglePrioriteAutomatique.Definir(module, typeDemande, typeClient, prioriteId, ordre);
        await regles.AjouterAsync(regle, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "ReglePrioriteAutomatiqueDefinie", nameof(ReglePrioriteAutomatique), regle.Id,
            null, $"{regle.Module ?? "*"}/{regle.TypeDemande ?? "*"}/{regle.TypeClient?.ToString() ?? "*"} -> {prioriteId}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return regle.Id;
    }

    public async Task ModifierAsync(
        Guid appelantId, Guid regleId, string? module, string? typeDemande, string? typeClient, Guid prioriteId, int ordre, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var regle = await regles.ObtenirParIdAsync(regleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Règle {regleId} introuvable.");

        _ = await priorites.ObtenirNiveauAsync(prioriteId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Niveau de priorité {prioriteId} introuvable.");

        var avant = $"{regle.Module ?? "*"}/{regle.TypeDemande ?? "*"}/{regle.TypeClient?.ToString() ?? "*"} -> {regle.PrioriteId}";
        regle.Modifier(module, typeDemande, typeClient, prioriteId, ordre);

        await journalisation.ConsignerAuditAsync(
            appelantId, "ReglePrioriteAutomatiqueModifiee", nameof(ReglePrioriteAutomatique), regle.Id,
            avant, $"{regle.Module ?? "*"}/{regle.TypeDemande ?? "*"}/{regle.TypeClient?.ToString() ?? "*"} -> {prioriteId}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Désactiver plutôt que supprimer : une règle déjà appliquée à un ticket ne doit pas
    /// disparaître de l'audit (même convention que <c>MotifTransfert</c>).</summary>
    public async Task DefinirActivationAsync(Guid appelantId, Guid regleId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var regle = await regles.ObtenirParIdAsync(regleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Règle {regleId} introuvable.");

        var avant = regle.EstActif ? "actif" : "inactif";
        if (actif)
        {
            regle.Activer();
        }
        else
        {
            regle.Desactiver();
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, actif ? "ReglePrioriteAutomatiqueActivee" : "ReglePrioriteAutomatiqueDesactivee", nameof(ReglePrioriteAutomatique), regle.Id,
            avant, actif ? "actif" : "inactif", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>§ 9.13 — lecture du référentiel des règles d'attribution automatique de priorité.</summary>
public sealed class ListerReglesPrioriteAutomatiqueQuery(
    IReglePrioriteAutomatiqueRepository regles,
    IAutorisationService autorisation)
{
    /// <summary>Écran de paramétrage : voit AUSSI les règles désactivées, sans quoi elles seraient
    /// irrécupérables.</summary>
    public async Task<IReadOnlyList<ReglePrioriteAutomatiqueVue>> ExecuterAsync(Guid administrateurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(administrateurId, Permission.AdministrationFiles, ct: ct).ConfigureAwait(false);

        var toutes = await regles.ListerAsync(activesUniquement: false, ct).ConfigureAwait(false);

        return [.. toutes
            .OrderBy(r => r.Ordre)
            .Select(r => new ReglePrioriteAutomatiqueVue(r.Id, r.Module, r.TypeDemande, r.TypeClient?.ToString(), r.PrioriteId, r.EstActif, r.Ordre))];
    }
}
