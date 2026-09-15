using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Client;

public sealed class ObtenirDashboardClientQuery(
    IDossierRepository dossierRepository,
    ITicketRepository ticketRepository,
    ISocieteClienteRepository societeRepository,
    IUtilisateurSocieteRepository utilisateurSocieteRepository)
{
    public async Task<DashboardClientVue> ExecuterAsync(Guid utilisateurId, Guid societeId, CancellationToken ct = default)
    {
        // 1. Contrôle de cloisonnement serveur (Principe VII)
        var userSoc = await utilisateurSocieteRepository.ObtenirParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (userSoc is null || !userSoc.EstActif || userSoc.SocieteId != societeId)
        {
            throw new AccesRefuseException($"L'utilisateur {utilisateurId} n'est pas autorisé à accéder aux données de la société {societeId}.");
        }

        // 2. Informations société
        var societe = await societeRepository.ObtenirParIdAsync(societeId, ct).ConfigureAwait(false);
        if (societe is null)
        {
            throw new InvalidOperationException($"Société introuvable {societeId}.");
        }

        // 3. Filtrage du scope selon ENT-03 / CLAR-02
        var dossiersSociete = await dossierRepository.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);
        if (userSoc.DroitConsultation == DroitConsultation.PropresDemandesUniquement && !userSoc.EstAdministrateur)
        {
            dossiersSociete = dossiersSociete.Where(d => d.AuteurId == utilisateurId).ToList();
        }

        var dossiersRecents = dossiersSociete
            .OrderByDescending(d => d.DateCreationUtc)
            .Take(10)
            .ToList();

        var dossierIds = dossiersRecents.Select(d => d.Id).ToList();

        var ticketsConformes = await ticketRepository.ListerParDossiersAsync(dossierIds, ct).ConfigureAwait(false);
        var ticketsParDossier = ticketsConformes.GroupBy(t => t.DossierId).ToDictionary(g => g.Key, g => g.ToList());

        // 4. Calcul de l'exigence ACR-01 ("Mes actions requises")
        var actionsRequises = new List<ActionRequiseItemVue>();
        int totalEnAttenteCit = 0;

        foreach (var ticket in ticketsConformes)
        {
            if (ticket.Statut == StatutTicket.EnAttenteClient)
            {
                var estExport = ticket.Module == "BillingExport";
                var estComplaint = ticket.Module == "CustomerComplaint";
                
                var typeAction = estComplaint ? TypeActionRequise.ComplementReclamation :
                                 estExport ? TypeActionRequise.ComplementExport : 
                                 TypeActionRequise.ComplementManifeste;
                                 
                var referenceMetier = estComplaint ? $"Réclamation {ticket.Numero}" :
                                      estExport ? $"Booking {ticket.NumeroBooking ?? ticket.Numero}" : 
                                      $"BL {ticket.NumeroBl ?? ticket.Numero}";
                                      
                var roleInstructeur = estComplaint ? "Le Service Client" :
                                      estExport ? "Le Back Office Export" : 
                                      "Le Pilote Manifeste";

                var titre = estComplaint ? $"Complément demandé pour votre {referenceMetier}" :
                            $"Complément demandé pour le {referenceMetier}";

                actionsRequises.Add(new ActionRequiseItemVue(
                    TicketId: ticket.Id,
                    NumeroTicket: ticket.Numero,
                    NumeroBl: ticket.NumeroBl,
                    TypeAction: typeAction,
                    Titre: titre,
                    Description: $"{roleInstructeur} a sollicité un complément d'information ou des pièces justificatives.",
                    ActionLibelle: "Déposer le complément",
                    UrlCible: $"/tickets/{ticket.Id}#complement",
                    DateDeclenchementUtc: ticket.DateCreationUtc,
                    NumeroBooking: ticket.NumeroBooking
                ));
            }
            else if (ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge || ticket.Statut == StatutTicket.EnCours)
            {
                totalEnAttenteCit++;
            }
        }

        // 5. Projection des dossiers récents avec état dérivé
        var dossiersVues = new List<DossierResumeVue>();
        foreach (var d in dossiersRecents)
        {
            ticketsParDossier.TryGetValue(d.Id, out var ticketsDossier);
            ticketsDossier ??= new List<Ticket>();

            var ticketsResume = ticketsDossier.Select(t => new TicketResumeVue(
                TicketId: t.Id,
                NumeroTicket: t.Numero,
                NumeroBl: t.NumeroBl,
                Statut: t.Statut,
                DateDerniereActiviteUtc: t.DateCreationUtc,
                NumeroBooking: t.NumeroBooking
            )).ToList();

            var premierTicket = ticketsDossier.FirstOrDefault();
            var module = premierTicket?.Module ?? "BillingImport";
            var typeDemande = premierTicket?.TypeDemande ?? "IMPORT";
            var etatGlobal = DeterminerEtatGlobalDossier(ticketsDossier);

            dossiersVues.Add(new DossierResumeVue(
                DossierId: d.Id,
                NumeroDossier: d.Numero,
                Module: module,
                TypeDemande: typeDemande,
                DateCreationUtc: d.DateCreationUtc,
                EtatGlobal: etatGlobal,
                NombreTickets: ticketsDossier.Count,
                Tickets: ticketsResume
            ));
        }

        var nomSociete = societe.InformationsDeclarees;
        if (nomSociete.Contains("RaisonSociale"))
        {
            nomSociete = ExtraireNom(nomSociete);
        }

        return new DashboardClientVue(
            SocieteId: societe.Id,
            SocieteNom: nomSociete,
            SocieteProfil: societe.Profil,
            SocieteStatut: societe.Etat,
            ActionsRequises: actionsRequises,
            DemandesRecentes: dossiersVues,
            TotalDemandesActives: dossiersVues.Count(d => d.EtatGlobal != StatutVisuelDossier.ClotureTermine && d.EtatGlobal != StatutVisuelDossier.Annule),
            TotalEnAttenteCit: totalEnAttenteCit,
            TotalActionsRequises: actionsRequises.Count,
            // AM-02 — même prédicat que SoumettreDemandeImportCommand/SoumettreDemandeExportCommand
            // (« refusé si !EstActif || !DroitSoumission »). Le cloisonnement société est déjà
            // garanti par le contrôle §1 ci-dessus (userSoc.SocieteId == societeId).
            PeutSoumettre: userSoc.EstActif && userSoc.DroitSoumission
        );
    }

    private static StatutVisuelDossier DeterminerEtatGlobalDossier(List<Ticket> tickets)
    {
        if (tickets.Count == 0) return StatutVisuelDossier.EnAttentePriseEnCharge;
        if (tickets.All(t => t.Statut == StatutTicket.Cloture)) return StatutVisuelDossier.ClotureTermine;
        if (tickets.All(t => t.Statut == StatutTicket.Annule)) return StatutVisuelDossier.Annule;
        if (tickets.Any(t => t.Statut == StatutTicket.EnAttenteClient)) return StatutVisuelDossier.EnAttenteClient;
        if (tickets.Any(t => t.Statut == StatutTicket.EnCours)) return StatutVisuelDossier.EnCoursTraitement;
        return StatutVisuelDossier.EnAttentePriseEnCharge;
    }

    private static string ExtraireNom(string raw)
    {
        try
        {
            var parts = raw.Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Contains("RaisonSociale", StringComparison.OrdinalIgnoreCase))
                {
                    var kv = part.Split(':');
                    if (kv.Length > 1) return kv[1].Trim().Trim('"', '{', '}', ' ');
                }
            }
        }
        catch { }
        return raw;
    }
}
