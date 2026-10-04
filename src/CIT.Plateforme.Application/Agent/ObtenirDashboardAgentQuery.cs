using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Agent;

public sealed class ObtenirDashboardAgentQuery(
    IFileDeTraitementRepository fileRepository,
    ITicketRepository ticketRepository,
    IHabilitationRepository habilitationRepository,
    IDisponibiliteRepository disponibiliteRepository,
    // P3 — `IUtilisateurInfoRepository` retiré : la garde d'identité renvoie déjà le compte,
    // et le relire aurait rouvert un écart entre l'identité contrôlée et celle affichée.
    GardeIdentiteService garde)
{
    public async Task<DashboardAgentVue> ExecuterAsync(Guid agentId, CancellationToken ct = default)
    {
        // CONSOLIDATION SÉCURITÉ — LA GARDE EST ICI, PAS DANS LA PAGE.
        //
        // Cette requête déterminait seule le périmètre de files de l'agent, à partir de ses
        // habilitations, sans jamais vérifier que le compte existait ni qu'il était actif. Sa seule
        // protection était la garde `EstAuthentifie` de DashboardAgent.razor — une garde
        // d'interface, donc jamais une frontière de sécurité.
        //
        // Le refus renvoie un tableau de bord VIDE plutôt qu'une exception : cette requête alimente
        // un écran d'accueil, et un espace vide y est la réponse juste. Rien n'y est exposé — ni
        // nom, ni file, ni ticket, ni compteur.
        var userInfo = await garde.ObtenirCompteUtilisableAsync(agentId, ct).ConfigureAwait(false);
        if (userInfo is null || userInfo.Nature != NatureUtilisateur.Agent)
        {
            return DashboardAgentVue.Vide(agentId);
        }

        var nomAgent = userInfo.NomAffiche ?? "Opérateur CIT";

        var dispo = await disponibiliteRepository.ObtenirParOperateurAsync(agentId, ct).ConfigureAwait(false);
        bool estDisponible = dispo is null || dispo.Statut == StatutDisponibilite.Disponible;

        var habilitations = await habilitationRepository.ListerParUtilisateurAsync(agentId, ct).ConfigureAwait(false);
        var fileIdsHabilitees = habilitations
            .Where(h => h.FileId.HasValue)
            .Select(h => h.FileId!.Value)
            .Distinct()
            .ToHashSet();

        var toutesFiles = await fileRepository.ListerAsync(ct).ConfigureAwait(false);

        // Revue UX du 04/10/2026 : une habilitation GLOBALE (sans file) ouvre aussi les files dont
        // l'écran figure au menu de l'agent — même règle que le menu (catalogue + RegleHabilitation).
        // Sans cela, l'agent lisait « aucune file habilitée » en traitant sa file. Le lien vient du
        // catalogue : c'est l'écran réel de la file.
        var urlsParFile = new Dictionary<Guid, string>();
        foreach (var entree in Web.CatalogueNavigation.Pour(Web.EspaceFonctionnel.Agent).SelectMany(g => g.Entrees))
        {
            var file = entree.NomFile is null ? null : toutesFiles.FirstOrDefault(f =>
                string.Equals(f.Module, entree.ModuleFile, StringComparison.Ordinal)
                && string.Equals(f.Nom, entree.NomFile, StringComparison.Ordinal));
            if (file is null || !entree.PermissionsRequises.Any(p => RegleHabilitation.Couvre(habilitations, p, file.Id)))
            {
                continue;
            }

            fileIdsHabilitees.Add(file.Id);
            urlsParFile.TryAdd(file.Id, entree.Route);
        }

        var filesHabilitees = toutesFiles.Where(f => fileIdsHabilitees.Contains(f.Id)).ToList();

        // Anti-scan complet : uniquement les tickets de l'agent + ceux des files de son périmètre,
        // au lieu de charger toute la table Tickets en mémoire.
        var mesTickets = await ticketRepository.ListerParOperateurAsync(agentId, ct).ConfigureAwait(false);
        var ticketsEnCoursAgent = mesTickets
            .Where(t => t.Statut == StatutTicket.EnCours)
            .OrderByDescending(t => t.DateCreationUtc)
            .ToList();

        var ticketsDuPerimetre = await ticketRepository.ListerParFilesAsync(fileIdsHabilitees, ct).ConfigureAwait(false);

        // Stats par file
        var fileStats = new List<FileStatistiqueVue>();
        int totalAttentePerimetre = 0;

        foreach (var file in filesHabilitees)
        {
            var enAttente = ticketsDuPerimetre.Count(t => t.FileCouranteId == file.Id && t.Statut == StatutTicket.EnAttenteDePriseEnCharge);
            var enCours = ticketsDuPerimetre.Count(t => t.FileCouranteId == file.Id && t.Statut == StatutTicket.EnCours);

            totalAttentePerimetre += enAttente;

            var url = urlsParFile.TryGetValue(file.Id, out var route) ? route : DeterminerUrlFile(file.Nom);

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
            var file = toutesFiles.FirstOrDefault(f => f.Id == t.FileCouranteId);

            return new TicketPrisEnChargeVue(
                TicketId: t.Id,
                NumeroTicket: t.Numero,
                NumeroBl: t.NumeroBl,
                FileNom: file?.Nom ?? "File générale",
                SocieteNom: "Client",
                Statut: t.Statut,
                DatePriseEnChargeUtc: t.DatePriseEnChargeUtc ?? t.DateCreationUtc
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
