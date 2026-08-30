using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingExport;

/// <summary>
/// Contrat C5-02 — Consultation d'une file Billing EXPORT avec contrôle d'habilitation.
/// CDC : EXP-02, EXP-03, EXP-04.
/// </summary>
public sealed class ConsulterFileBillingExportQuery(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<Ticket>> ExecuterAsync(
        Guid agentId,
        string nomFile,
        IReadOnlyList<string> permissionsAcceptees,
        CancellationToken ct = default)
    {
        if (permissionsAcceptees.Count == 0)
        {
            throw new ArgumentException("Au moins une permission acceptée doit être fournie.", nameof(permissionsAcceptees));
        }

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var file = toutesLesFiles.SingleOrDefault(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == nomFile)
            ?? throw new InvalidOperationException($"File « {nomFile} » (module {SoumettreDemandeExportCommand.Module}) non paramétrée.");

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

        return await tickets.ListerParFileAsync(file.Id, ct).ConfigureAwait(false);
    }
}
