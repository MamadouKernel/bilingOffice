using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CIT.Plateforme.Application.Abstractions;

public sealed record CriteresRechercheGuichet(
    string? Terme,
    string? Module,
    int Limite = 50
);

public sealed record ResultatRechercheGuichetDto(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroDossier,
    string Module,
    string ClientNom,
    string? ReferenceBlOuBooking,
    string EtapeActuelle,
    string StatutTicket,
    DateTime DateCreation,
    bool PossedeRendezVousActif
);

/// <summary>
/// FEATURE 010 (CON-02, research.md D1) — port de recherche rapide multicritère au guichet.
/// </summary>
public interface IRechercheGuichetRepository
{
    Task<IReadOnlyList<ResultatRechercheGuichetDto>> RechercherAsync(
        CriteresRechercheGuichet criteres,
        IEnumerable<Guid>? filesAutorisees = null,
        CancellationToken ct = default);
}
