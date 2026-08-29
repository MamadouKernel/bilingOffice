using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Supervision;

public enum TypeActionDeMasse
{
    Affecter,
    Reaffecter,
    Transferer,
    ModifierPriorite,
}

public sealed record ResultatActionDeMasse(Guid TicketId, bool Succes, string? Erreur);

/// <summary>
/// MAS-01 (Should) / MAS-02 (Must) — actions de masse superviseur. Chaque ticket de la sélection
/// est traité individuellement et **tracé ticket par ticket** (MAS-02), même en cas d'échec
/// partiel sur certains tickets de la sélection.
/// </summary>
public sealed class ActionDeMasseCommand(
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<IReadOnlyList<ResultatActionDeMasse>> ExecuterAsync(
        Guid superviseurId, IReadOnlyCollection<Guid> ticketIds, TypeActionDeMasse type, Func<Guid, CancellationToken, Task> actionParTicket, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.ActionDeMasse, ct: ct).ConfigureAwait(false);

        var resultats = new List<ResultatActionDeMasse>();
        foreach (var ticketId in ticketIds)
        {
            try
            {
                await actionParTicket(ticketId, ct).ConfigureAwait(false);

                // MAS-02 : traçage individuel, un par ticket de la sélection.
                await journalisation.ConsignerAuditAsync(
                    superviseurId, $"ActionDeMasse:{type}", "Ticket", ticketId, null, "succès", ct).ConfigureAwait(false);
                resultats.Add(new ResultatActionDeMasse(ticketId, Succes: true, Erreur: null));
            }
            catch (Exception ex)
            {
                await journalisation.ConsignerAuditAsync(
                    superviseurId, $"ActionDeMasse:{type}", "Ticket", ticketId, null, $"échec: {ex.Message}", ct).ConfigureAwait(false);
                resultats.Add(new ResultatActionDeMasse(ticketId, Succes: false, Erreur: ex.Message));
            }
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return resultats;
    }
}
