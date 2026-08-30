using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-08 — Demander la facture. CDC : IMP-10. Le ticket est déjà en file Facturation
/// (aiguillage automatique, IMP-06/T054) : cette commande enregistre le signal explicite du client,
/// visible par le Pilote Facturation (audit), sans transition de file supplémentaire.
/// </summary>
public sealed class DemanderFactureCommand(
    ITicketRepository tickets,
    Application.Securite.CloisonnementService cloisonnement,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid clientId, Guid ticketId, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "FactureDemandee", null, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "FactureDemandee", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
