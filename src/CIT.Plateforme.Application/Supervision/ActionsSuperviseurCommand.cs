using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>
/// Contrat C-24 — Actions superviseur. CDC : SUP-04 (affecter un ticket non pris en charge,
/// réaffecter, transférer, débloquer). Réutilise les cas d'usage déjà existants (US2/US9) — le
/// superviseur est un initiateur habilité comme un autre, jamais un rôle métier nommé en dur
/// (PC-03/PC-04 non résolus).
/// </summary>
public sealed class ActionsSuperviseurCommand(
    AffecterManuellementCommand affecterManuellement,
    ReaffecterTicketCommand reaffecter,
    TransfererTicketCommand transferer)
{
    public Task AffecterTicketNonPrisEnChargeAsync(Guid superviseurId, Guid ticketId, Guid operateurCibleId, CancellationToken ct = default) =>
        affecterManuellement.ExecuterAsync(superviseurId, ticketId, operateurCibleId, ct);

    public Task ReaffecterAsync(Guid superviseurId, Guid ticketId, Guid nouvelOperateurId, CancellationToken ct = default) =>
        reaffecter.ExecuterAsync(superviseurId, ticketId, nouvelOperateurId, ct);

    public Task DebloquerParTransfertAsync(Guid superviseurId, Guid ticketId, Guid fileCibleId, string motif, CancellationToken ct = default) =>
        transferer.ExecuterAsync(superviseurId, ticketId, fileCibleId, motif, destinataireId: null, ct);
}
