using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — backfill du reporting (approche B). Reconstruit les mesures
/// des demandes antérieures à la mise en service du reporting, depuis l'historique des tickets.
/// Idempotent : les tickets ayant déjà des mesures sont ignorés (relançable sans doublon).
/// Réservé à un responsable BADT (<see cref="Permission.ActualisationBadtDecider"/>).
/// </summary>
public sealed class ReconstruireMesuresBadtCommand(
    ITicketRepository tickets,
    ILigneActualisationBadtRepository lignes,
    IHistoriqueTicketRepository historique,
    IMesureActualisationBadtRepository mesures,
    IAutorisationService autorisation,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>Nombre de mesures insérées.</summary>
    public async Task<int> ExecuterAsync(Guid demandeurId, CancellationToken ct = default)
    {
        if (!await autorisation.EstAutoriseAsync(demandeurId, Permission.ActualisationBadtDecider, null, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Reconstruction du reporting BADT réservée à un responsable (décideur).");
        }

        var dejaTraites = (await mesures.TicketsAvecMesuresAsync(ct).ConfigureAwait(false)).ToHashSet();
        var ticketsBadt = await tickets.ListerParModuleAsync(Ticket.ModuleActualisationBadt, ct).ConfigureAwait(false);

        var inseres = 0;
        foreach (var ticket in ticketsBadt.Where(t => !dejaTraites.Contains(t.Id)))
        {
            var entrees = await historique.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false);
            var lignesTicket = await lignes.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false);

            foreach (var mesure in ReconstructeurMesuresBadt.Reconstruire(ticket.Id, entrees, lignesTicket))
            {
                await mesures.AjouterAsync(mesure, ct).ConfigureAwait(false);
                inseres++;
            }
        }

        if (inseres > 0)
        {
            await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        }

        return inseres;
    }
}
