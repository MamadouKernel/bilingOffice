using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>
/// FEATURE 010 (Actualisation BADT) — clôture du ticket une fois le fichier Navis intégré. Dernière
/// étape du cycle : toutes les lignes doivent être statuées (plus aucune en attente), sans quoi la
/// demande n'est pas traitée. La clôture déclenche l'archivage (SOC-09).
/// </summary>
public sealed class CloturerTicketBadtCommand(
    ITicketRepository tickets,
    ILigneActualisationBadtRepository lignes,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await QualifierLigneBadtCommand.ChargerTicketBadtAsync(tickets, ticketId, ct).ConfigureAwait(false);

        if (!await autorisation.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException("Permission d'instruction des demandes d'actualisation BADT requise.");
        }

        var toutesLignes = await lignes.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false);
        if (toutesLignes.Count == 0)
        {
            throw new InvalidOperationException("La demande ne porte aucune ligne : rien à clôturer.");
        }

        // Un BL non encore déplié (ligne sans conteneur) reste « en attente » : on l'explicite pour
        // éviter le message générique « conteneurs à statuer » qui masque la vraie cause.
        if (toutesLignes.Any(l => l.NumeroConteneur.Length == 0))
        {
            throw new InvalidOperationException("Certains BL n'ont pas encore été dépliés en conteneurs : ajoutez les conteneurs (ou retirez le BL) avant la clôture.");
        }

        if (toutesLignes.Any(l => l.Decision == DecisionLigneBadt.EnAttente))
        {
            throw new InvalidOperationException("Tous les conteneurs doivent être statués (accordés ou refusés) avant la clôture.");
        }

        var maintenant = horloge.MaintenantUtc;
        ticket.Cloturer(MotifClotureType.Normale, maintenant);

        await journalisation.ConsignerAuditAsync(agentId, "ActualisationBadt.Cloture", nameof(Ticket), ticket.Id, null, "clôturé", ct).ConfigureAwait(false);
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketCloture", "Fichier Navis intégré, demande traitée.", agentId, ct).ConfigureAwait(false);
        // Le client est informé que sa demande d'actualisation est traitée et clôturée (résultat
        // par conteneur consultable sur le ticket). Événement déjà câblé au ResolveurClientDuDossier.
        await journalisation.PublierEvenementAsync("TicketCloture", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
