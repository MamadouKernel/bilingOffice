using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Societe;

public sealed class ObtenirDashboardSocieteQuery(
    IDossierRepository dossierRepository,
    ITicketRepository ticketRepository,
    ISocieteClienteRepository societeRepository,
    IUtilisateurSocieteRepository utilisateurSocieteRepository,
    IUtilisateurInfoRepository utilisateurInfoRepository)
{
    public async Task<DashboardSocieteVue> ExecuterAsync(Guid demandeurId, Guid societeId, CancellationToken ct = default)
    {
        // 1. Contrôle d'autorisation Admin Société (Principes VII, Contrat C4-04)
        var userDemandeur = await utilisateurSocieteRepository.ObtenirParUtilisateurAsync(demandeurId, ct).ConfigureAwait(false);
        if (userDemandeur is null || !userDemandeur.EstActif || userDemandeur.SocieteId != societeId || !userDemandeur.EstAdministrateur)
        {
            throw new AccesRefuseException($"L'utilisateur {demandeurId} n'a pas les droits d'administration sur la société {societeId}.");
        }

        var societe = await societeRepository.ObtenirParIdAsync(societeId, ct).ConfigureAwait(false);
        if (societe is null)
        {
            throw new InvalidOperationException($"Société introuvable {societeId}.");
        }

        // 2. Liste de tous les collaborateurs de la société
        var rattachements = await utilisateurSocieteRepository.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);

        var collaborateurs = new List<CollaborateurVue>();
        foreach (var r in rattachements.OrderByDescending(u => u.EstAdministrateur).ThenBy(u => u.DateRattachementUtc))
        {
            var info = await utilisateurInfoRepository.ObtenirInfoAsync(r.UtilisateurId, ct).ConfigureAwait(false);
            collaborateurs.Add(new CollaborateurVue(
                Id: r.Id,
                UtilisateurId: r.UtilisateurId,
                NomAffiche: info?.NomAffiche ?? "Collaborateur",
                Email: info?.Email ?? string.Empty,
                EstAdministrateur: r.EstAdministrateur,
                DroitSoumission: r.DroitSoumission,
                DroitConsultation: r.DroitConsultation,
                EstActif: r.EstActif,
                DateRattachementUtc: r.DateRattachementUtc,
                DroitValidation: r.DroitValidation
            ));
        }

        // 3. Métriques globales société
        var dossiers = await dossierRepository.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);
        var dossierIds = dossiers.Select(d => d.Id).ToList();
        var tickets = await ticketRepository.ListerParDossiersAsync(dossierIds, ct).ConfigureAwait(false);

        var totalTicketsEnCours = tickets.Count(t =>
            t.Statut == StatutTicket.EnAttenteDePriseEnCharge ||
            t.Statut == StatutTicket.EnCours ||
            t.Statut == StatutTicket.EnAttenteClient);

        var nomSociete = societe.NomAffichable;

        return new DashboardSocieteVue(
            SocieteId: societe.Id,
            SocieteNom: nomSociete,
            SocieteProfil: societe.Profil,
            SocieteStatut: societe.Etat,
            NombreAdministrateursActifs: societe.NombreAdministrateursActifs,
            Collaborateurs: collaborateurs,
            TotalDossiersSociete: dossiers.Count,
            TotalTicketsEnCours: totalTicketsEnCours
        );
    }

}
