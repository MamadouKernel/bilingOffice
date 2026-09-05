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
)
{
    /// <summary>
    /// Tableau de bord d'un appelant que la garde d'identité refuse : compte inconnu, désactivé,
    /// ou qui n'est pas un agent. Aucune donnée n'y transite — ni nom, ni file, ni ticket, ni
    /// compteur. C'est la forme que prend le refus sur un écran d'accueil.
    /// </summary>
    public static DashboardAgentVue Vide(Guid agentId) => new(
        AgentId: agentId,
        AgentNom: string.Empty,
        EstDisponible: false,
        FilesHabilitees: Array.Empty<FileStatistiqueVue>(),
        MesTicketsEnCours: Array.Empty<TicketPrisEnChargeVue>(),
        TotalTicketsEnCoursAgent: 0,
        TotalTicketsEnAttenteDansPerimetre: 0);
}
