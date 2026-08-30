using System;
using System.Collections.Generic;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Dossiers;

public sealed record TicketLigneDossierVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    string Module,
    string TypeDemande,
    StatutTicket Statut,
    string FileNom,
    string? OperateurAssigneNom,
    DateTime DateCreationUtc,
    DateTime DateDerniereActiviteUtc
);

public sealed record FicheDossierVue(
    Guid DossierId,
    string NumeroDossier,
    Guid SocieteId,
    string SocieteNom,
    TypeClient SocieteProfil,
    Guid? AuteurId,
    string AuteurNom,
    DateTime DateCreationUtc,
    StatutVisuelDossier StatutGlobal,
    IReadOnlyList<TicketLigneDossierVue> Tickets,
    int TotalTickets,
    int TotalTicketsEnCours,
    int TotalTicketsClotures
);
