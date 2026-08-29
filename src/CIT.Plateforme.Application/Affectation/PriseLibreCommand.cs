using CIT.Plateforme.Application.Tickets;

namespace CIT.Plateforme.Application.Affectation;

/// <summary>AFF-01 : prise libre — un opérateur habilité prend un ticket dans sa file. Sur le
/// plan applicatif, c'est la même transition que la prise en charge (PEC-01/02) déclenchée par
/// l'opérateur lui-même plutôt que par un tiers (superviseur — voir AffecterManuellementCommand,
/// US2) ; le type dédié documente l'intention métier distincte du contrat C-14.</summary>
public sealed class PriseLibreCommand(PrendreEnChargeCommand prendreEnCharge)
{
    public Task ExecuterAsync(Guid operateurId, Guid ticketId, CancellationToken ct = default) =>
        prendreEnCharge.ExecuterAsync(operateurId, ticketId, ct);
}
