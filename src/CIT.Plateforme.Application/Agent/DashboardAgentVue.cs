using System;
using System.Collections.Generic;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Agent;

public sealed record FileStatistiqueVue(
    Guid FileId,
    string Module,
    string Nom,
    int TicketsEnAttente,
    int TicketsEnCours,
    string UrlFile
);

public sealed record TicketPrisEnChargeVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    string FileNom,
    string SocieteNom,
    StatutTicket Statut,
    DateTime DatePriseEnChargeUtc
);

public sealed record DashboardAgentVue(
    Guid AgentId,
    string AgentNom,
    bool EstDisponible,
    IReadOnlyList<FileStatistiqueVue> FilesHabilitees,
    IReadOnlyList<TicketPrisEnChargeVue> MesTicketsEnCours,
    int TotalTicketsEnCoursAgent,
    int TotalTicketsEnAttenteDansPerimetre
);
