using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Contrat C-16 — Réaffecter un ticket. CDC : TRF-04 — droit configurable, non réservé
/// au superviseur (la permission peut être attribuée à tout agent habilité).</summary>
public sealed class ReaffecterTicketCommand(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid initiateurId, Guid ticketId, Guid nouvelOperateurId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(initiateurId, Permission.TicketReaffecter, ticket.FileCouranteId, ct).ConfigureAwait(false);

        // TRF-04 (audit) : « un autre opérateur habilité » — le nouvel opérateur doit lui-même
        // pouvoir traiter des tickets de cette file, sans quoi la réaffectation orpheline le
        // ticket vers un compte qui ne le verra jamais dans son propre périmètre.
        var cibleHabilitee = await autorisation.EstAutoriseAsync(nouvelOperateurId, Permission.TicketPrendreEnCharge, ticket.FileCouranteId, ct).ConfigureAwait(false);
        if (!cibleHabilitee)
        {
            throw new AccesRefuseException($"L'opérateur cible {nouvelOperateurId} n'est pas habilité sur la file {ticket.FileCouranteId} (TRF-04).");
        }

        var ancienOperateur = ticket.OperateurAssigneId;
        ticket.Reaffecter(nouvelOperateurId, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(
            ticket.Id, "TicketReaffecte", $"de={ancienOperateur};vers={nouvelOperateurId}", initiateurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            initiateurId, "TicketReaffecte", nameof(Ticket), ticket.Id, ancienOperateur?.ToString(), nouvelOperateurId.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
