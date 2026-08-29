using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Contrat C-06 — Gérer les règles de priorité / modifier la priorité d'un ticket. CDC :
/// PRI-01, PRI-02, PRI-03, PRI-04. PRI-04 : le client ne peut jamais s'auto-déclarer urgent —
/// cette commande n'est accessible qu'aux utilisateurs habilités (jamais au client).</summary>
public sealed class ModifierPrioriteCommand(
    ITicketRepository tickets,
    IPrioriteRepository priorites,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid auteurId, Guid ticketId, Guid nouveauNiveauId, string motif, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        // PRI-04 : seul un utilisateur habilité peut modifier la priorité — jamais le client
        // directement (aucune permission client n'est jamais attribuée pour ce code).
        await autorisation.ExigerAsync(auteurId, Permission.PrioriteModifier, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var ancienNiveauId = ticket.PrioriteId;
        var modification = ModificationPriorite.Creer(ticketId, ancienNiveauId, nouveauNiveauId, auteurId, motif, horloge.MaintenantUtc);
        await priorites.AjouterModificationAsync(modification, ct).ConfigureAwait(false);

        ticket.DefinirPriorite(nouveauNiveauId);

        await journalisation.ConsignerHistoriqueAsync(ticketId, "PrioriteModifiee", motif, auteurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(auteurId, "PrioriteModifiee", nameof(Ticket), ticketId, ancienNiveauId?.ToString(), nouveauNiveauId.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
