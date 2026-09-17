using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §8.3) : consultation du contenu d'une file
/// Billing IMPORT, désormais soumise à une habilitation serveur — même schéma que le précédent F001
/// <c>ConsulterFileQuery</c> (<c>Tickets/ConsulterFileQuery.cs</c>), qui vérifie déjà
/// <c>IAutorisationService.ExigerAsync</c> avant tout retour de données. Une seule requête,
/// paramétrée par le nom de file et le code de permission attendu pour cette file (chaque file
/// Billing IMPORT a déjà son propre code `billing_import.*` — réutilisé tel quel, aucune nouvelle
/// permission introduite), plutôt que 6 requêtes quasi identiques.
/// </summary>
public sealed class ConsulterFileBillingImportQuery(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    IAutorisationService autorisation,
    IPrioriteRepository? priorites = null)
{
    /// <summary>
    /// Consulte le contenu d'une file, sous réserve que l'appelant détienne **au moins une** des
    /// permissions passées (files à double rôle, ex. File IES — Pilote Manifeste ou Pilote Contrôle
    /// Documentaire, même logique que <see cref="ConstaterJointureIesEtFinaliserCommand"/>).
    ///
    /// SOC-04 : la file supporte un filtre par statut et un tri (par priorité ou par date
    /// d'arrivée, croissant ou décroissant) — l'agent ne dépend plus du seul ordre chronologique.
    /// </summary>
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(
        Guid agentId,
        string nomFile,
        IReadOnlyList<string> permissionsAcceptees,
        StatutTicket? filtreStatut = null,
        TriFileTicket tri = TriFileTicket.DateArriveeCroissante,
        CancellationToken ct = default)
    {
        if (permissionsAcceptees.Count == 0)
        {
            throw new ArgumentException("Au moins une permission acceptée doit être fournie.", nameof(permissionsAcceptees));
        }

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var file = toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeImportCommand.Module && f.Nom == nomFile)
            ?? throw new InvalidOperationException($"File « {nomFile} » (module {SoumettreDemandeImportCommand.Module}) non paramétrée.");

        var autorise = false;
        foreach (var permission in permissionsAcceptees)
        {
            if (await autorisation.EstAutoriseAsync(agentId, permission, file.Id, ct).ConfigureAwait(false))
            {
                autorise = true;
                break;
            }
        }
        if (!autorise)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {agentId} ne détient aucune des permissions requises pour consulter la file « {nomFile} ».");
        }

        var ticketsDeLaFile = await tickets.ListerParFileAsync(file.Id, ct).ConfigureAwait(false);

        var filtres = filtreStatut is StatutTicket statut
            ? ticketsDeLaFile.Where(t => t.Statut == statut).ToList()
            : ticketsDeLaFile.ToList();

        if (tri is TriFileTicket.PrioriteDecroissante or TriFileTicket.PrioriteCroissante && priorites is not null)
        {
            var niveaux = await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false);
            var rangs = niveaux.ToDictionary(n => n.Id, n => n.Rang);
            int RangDe(Ticket t) => t.PrioriteId.HasValue && rangs.TryGetValue(t.PrioriteId.Value, out var r) ? r : 0;

            return tri == TriFileTicket.PrioriteDecroissante
                ? [.. filtres.OrderByDescending(RangDe).ThenBy(t => t.DateArriveeEnFileUtc)]
                : [.. filtres.OrderBy(RangDe).ThenBy(t => t.DateArriveeEnFileUtc)];
        }

        return tri == TriFileTicket.DateArriveeDecroissante
            ? [.. filtres.OrderByDescending(t => t.DateArriveeEnFileUtc)]
            : [.. filtres.OrderBy(t => t.DateArriveeEnFileUtc)];
    }
}

/// <summary>SOC-04 : options de tri disponibles sur les écrans de file agent.</summary>
public enum TriFileTicket
{
    DateArriveeCroissante,
    DateArriveeDecroissante,
    PrioriteCroissante,
    PrioriteDecroissante,
}
