using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Agent;

public sealed class ObtenirDashboardAgentQuery(
    IFileDeTraitementRepository fileRepository,
    ITicketRepository ticketRepository,
    IHabilitationRepository habilitationRepository,
    IDisponibiliteRepository disponibiliteRepository,
    IUtilisateurInfoRepository utilisateurInfoRepository)
{
    public async Task<DashboardAgentVue> ExecuterAsync(Guid agentId, CancellationToken ct = default)
    {
        var userInfo = await utilisateurInfoRepository.ObtenirInfoAsync(agentId, ct).ConfigureAwait(false);
        var nomAgent = userInfo?.NomAffiche ?? "Opérateur CIT";

        var dispo = await disponibiliteRepository.ObtenirParOperateurAsync(agentId, ct).ConfigureAwait(false);
        bool estDisponible = dispo is null || dispo.Statut == StatutDisponibilite.Disponible;

        var habilitations = await habilitationRepository.ListerParUtilisateurAsync(agentId, ct).ConfigureAwait(false);
        var fileIdsHabilitees = habilitations
            .Where(h => h.FileId.HasValue)
            .Select(h => h.FileId!.Value)
            .Distinct()
            .ToHashSet();

        var toutesFiles = await fileRepository.ListerAsync(ct).ConfigureAwait(false);
        var filesHabilitees = toutesFiles.Where(f => fileIdsHabilitees.Contains(f.Id)).ToList();

        var tousTickets = await ticketRepository.ListerTousAsync(ct).ConfigureAwait(false);
        var ticketsEnCoursAgent = tousTickets
            .Where(t => t.OperateurAssigneId == agentId && t.Statut == StatutTicket.EnCours)
            .OrderByDescending(t => t.DateCreationUtc)
            .ToList();

        // Stats par file
        var fileStats = new List<FileStatistiqueVue>();
        int totalAttentePerimetre = 0;

        foreach (var file in filesHabilitees)
        {
            var enAttente = tousTickets.Count(t => t.FileCouranteId == file.Id && t.Statut == StatutTicket.EnAttenteDePriseEnCharge);
            var enCours = tousTickets.Count(t => t.FileCouranteId == file.Id && t.Statut == StatutTicket.EnCours);

            totalAttentePerimetre += enAttente;

            var url = DeterminerUrlFile(file.Nom);

            fileStats.Add(new FileStatistiqueVue(
                FileId: file.Id,
                Module: file.Module,
                Nom: file.Nom,
                TicketsEnAttente: enAttente,
                TicketsEnCours: enCours,
                UrlFile: url
            ));
        }

        var mesTicketsVues = ticketsEnCoursAgent.Select(t =>
        {
            var file = filesHabilitees.FirstOrDefault(f => f.Id == t.FileCouranteId);

            return new TicketPrisEnChargeVue(
                TicketId: t.Id,
                NumeroTicket: t.Numero,
                NumeroBl: t.NumeroBl,
                FileNom: file?.Nom ?? "File générale",
                SocieteNom: "Client",
                Statut: t.Statut,
                DatePriseEnChargeUtc: t.DateCreationUtc
            );
        }).ToList();

        return new DashboardAgentVue(
            AgentId: agentId,
            AgentNom: nomAgent,
            EstDisponible: estDisponible,
            FilesHabilitees: fileStats,
            MesTicketsEnCours: mesTicketsVues,
            TotalTicketsEnCoursAgent: mesTicketsVues.Count,
            TotalTicketsEnAttenteDansPerimetre: totalAttentePerimetre
        );
    }

    private static string DeterminerUrlFile(string nomFile) => nomFile.ToLowerInvariant() switch
    {
        var n when n.Contains("manifeste") => "/billing-import/manifeste",
        var n when n.Contains("facturation") => "/billing-import/facturation",
        var n when n.Contains("paiement") || n.Contains("caisse") => "/billing-import/validation-paiement",
        var n when n.Contains("documentaire") => "/billing-import/controle-documentaire",
        var n when n.Contains("comptabilit") => "/billing-import/comptabilite-client",
        var n when n.Contains("ies") => "/billing-import/ies",
        _ => "/billing-import/manifeste"
    };
}
