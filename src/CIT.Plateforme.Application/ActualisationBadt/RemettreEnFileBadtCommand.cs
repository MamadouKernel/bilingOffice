using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — l'agent qui instruit un ticket le REMET DANS LA FILE avec un
/// commentaire, pour qu'un collègue le reprenne. Le ticket repasse « en attente de prise en charge »
/// (<see cref="Domain.Tickets.Ticket.RemettreEnDistribution"/>) ; le remplaçant retrouve sur la fiche
/// l'historique des actions déjà faites, l'état des lignes en cours et ce commentaire de passation.
/// Le commentaire est obligatoire. Les agents de la file sont notifiés qu'un ticket est à reprendre.
/// </summary>
public sealed class RemettreEnFileBadtCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, string commentaire, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(commentaire))
        {
            throw new ArgumentException("Un commentaire de passation est obligatoire pour remettre le ticket dans la file.", nameof(commentaire));
        }

        var ticket = await QualifierLigneBadtCommand.ChargerTicketBadtAsync(tickets, ticketId, ct).ConfigureAwait(false);

        if (!await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Permission d'instruction des demandes d'actualisation BADT requise.");
        }

        ticket.RemettreEnDistribution(horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketRemisEnFile",
            commentaire.Trim(), agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, "ActualisationBadt.RemiseEnFile", nameof(Domain.Tickets.Ticket),
            ticket.Id, null, commentaire.Trim(), ct).ConfigureAwait(false);
        // Un ticket est de nouveau à prendre : prévenir les agents de la file (in-app + e-mail).
        await journalisation.PublierEvenementAsync("BadtTicketRemisEnFile", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
