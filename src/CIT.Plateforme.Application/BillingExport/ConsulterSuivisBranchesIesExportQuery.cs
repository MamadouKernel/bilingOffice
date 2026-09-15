using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Consultation autorisée des suivis de branches IES Export pour un ticket.
///
/// REMÉDIATION AUDIT FINAL (RG-SEC-01) : symétrique de
/// <see cref="CIT.Plateforme.Application.BillingImport.ConsulterSuivisBranchesIesQuery"/> — les
/// droits au niveau champ sont résolus côté serveur et rendus avec les données, de sorte que
/// l'écran puisse n'offrir que les actions du rôle de l'appelant (EXP-04, IMP-40).
/// </summary>
public sealed class ConsulterSuivisBranchesIesExportQuery(
    ITicketRepository tickets,
    ISuiviBrancheIesRepository suivisBranchesIes,
    IAutorisationService autorisation,
    PorteeChampsIesService porteeChamps)
{
    public async Task<VueBranchesIes> ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var autoriseBO = await autorisation.EstAutoriseAsync(
            agentId, Permission.BillingExportIesBackOfficeInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        var autoriseDoc = await autorisation.EstAutoriseAsync(
            agentId, Permission.BillingExportIesControleDocumentaireInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        if (!autoriseBO && !autoriseDoc)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {agentId} ne détient aucune des permissions de branche IES requises pour consulter le ticket {ticketId}.");
        }

        var droits = await porteeChamps
            .ResoudreAsync(agentId, ticket.FileCouranteId, ModuleIes.Export, ct)
            .ConfigureAwait(false);

        var suivis = await suivisBranchesIes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        return new VueBranchesIes(suivis, droits);
    }
}
