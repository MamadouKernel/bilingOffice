using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3, contrôle exhaustif des lectures) : la page
/// <c>FileIes.razor</c> injectait <c>ISuiviBrancheIesRepository</c> directement pour afficher l'état
/// des deux branches — même schéma d'autorisation que <see cref="ConsulterFileBillingImportQuery"/>,
/// exigé ici aussi (au moins une des deux permissions de branche IES, scopée à la file du ticket).
///
/// REMÉDIATION AUDIT FINAL (RG-SEC-01) : la requête rendait la liste des suivis et rien d'autre.
/// Détenir <b>une</b> des deux permissions donnait donc accès à l'écran entier, actions des deux
/// branches comprises — alors qu'IMP-40 exige que chacun n'agisse « que sur les champs/actions de
/// son rôle ». La requête résout désormais aussi les droits au niveau champ, côté serveur, et les
/// rend avec les données.
/// </summary>
public sealed class ConsulterSuivisBranchesIesQuery(
    ITicketRepository tickets,
    ISuiviBrancheIesRepository suivisBranchesIes,
    IAutorisationService autorisation,
    PorteeChampsIesService porteeChamps)
{
    public async Task<VueBranchesIes> ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var autoriseBrancheManifeste = await autorisation.EstAutoriseAsync(
            agentId, Permission.BillingImportIesManifesteInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        var autoriseBrancheControleDocumentaire = await autorisation.EstAutoriseAsync(
            agentId, Permission.BillingImportIesControleDocumentaireInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        if (!autoriseBrancheManifeste && !autoriseBrancheControleDocumentaire)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {agentId} ne détient aucune des permissions de branche IES requises pour consulter le ticket {ticketId}.");
        }

        // RG-SEC-01 — les droits au niveau champ accompagnent la donnée. Ils sont résolus depuis les
        // habilitations, jamais transmis par l'appelant.
        var droits = await porteeChamps
            .ResoudreAsync(agentId, ticket.FileCouranteId, ModuleIes.Import, ct)
            .ConfigureAwait(false);

        var suivis = await suivisBranchesIes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        return new VueBranchesIes(suivis, droits);
    }
}
