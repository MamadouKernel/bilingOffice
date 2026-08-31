using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Contrat C8-07 — Déclencher la convocation. CDC : ENR-03.
/// <para>
/// research.md D6/CLAR-ENR-06 : la convocation est un événement métier tracé, jamais un état
/// <see cref="StatutTicket"/> ni l'implémentation d'un canal (SMS/WhatsApp/email — hors périmètre
/// F008, dépend de PC-16). L'événement publié ne porte aucune donnée de canal — il est structuré
/// pour être consommé par un futur module Notifications sans modification de F008.
/// </para>
/// </summary>
public sealed class DeclencherConvocationCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.Module != "Enrolement")
        {
            throw new InvalidOperationException("Seuls les tickets Enrolement peuvent faire l'objet d'une convocation.");
        }

        await autorisation.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "ConvocationDeclenchee", null, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ConvocationDeclenchee", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("ConvocationDeclenchee", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
