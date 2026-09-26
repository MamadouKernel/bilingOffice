using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Domain.Guichet;

namespace CIT.Plateforme.Application.Abstractions;

/// <summary>
/// FEATURE 010 (CON-03, research.md D2, data-model.md §1) — port de persistance des rendez-vous guichet.
/// </summary>
public interface IRendezVousRepository
{
    Task AjouterAsync(RendezVous rdv, CancellationToken ct = default);

    Task<RendezVous?> ObtenirParIdAsync(Guid id, CancellationToken ct = default);

    Task<RendezVous?> ObtenirActifParTicketAsync(Guid ticketId, CancellationToken ct = default);

    Task<IReadOnlyList<RendezVous>> ListerParDemandeurAsync(Guid demandeurId, CancellationToken ct = default);

    Task<IReadOnlyList<RendezVous>> ListerParDateAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Rendez-vous d'une période (bornes incluses), triés par date puis heure — vues semaine et mois de l'agenda.</summary>
    Task<IReadOnlyList<RendezVous>> ListerParPeriodeAsync(DateOnly du, DateOnly au, CancellationToken ct = default);

    /// <summary>Compte atomiquement les réservations actives sur un créneau pour vérifier la capacité.</summary>
    Task<int> CompterReservationsCreneauAsync(DateOnly date, TimeSpan heureDebut, CancellationToken ct = default);

    Task<bool> PossedeRdvActifAsync(Guid ticketId, CancellationToken ct = default);
}
