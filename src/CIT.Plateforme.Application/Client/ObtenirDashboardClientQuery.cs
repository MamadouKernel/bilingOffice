using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Client;

public sealed class ObtenirDashboardClientQuery(
    IDossierRepository dossierRepository,
    ITicketRepository ticketRepository,
    ISocieteClienteRepository societeRepository,
    IUtilisateurSocieteRepository utilisateurSocieteRepository,
    IHistoriqueTicketRepository? historique = null,
    IFileDeTraitementRepository? files = null,
    IDocumentRepository? documents = null)
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
            if (ticket.Statut == StatutTicket.EnAttenteClient && await ActionBillingSpecifiqueAsync(ticket, ct).ConfigureAwait(false) is { } actionBilling)
            {
                actionsRequises.Add(actionBilling);
            }
            else if (ticket.Statut == StatutTicket.EnAttenteClient)
            {
                var estExport = ticket.Module == "BillingExport";
                var estComplaint = ticket.Module == "CustomerComplaint";
                
                var typeAction = estComplaint ? TypeActionRequise.ComplementReclamation :
                                 estExport ? TypeActionRequise.ComplementExport : 
                                 TypeActionRequise.ComplementManifeste;
                                 
                var referenceMetier = estComplaint ? LangueCourante.T($"Réclamation {ticket.Numero}", $"complaint {ticket.Numero}") :
                                      estExport ? $"Booking {ticket.NumeroBooking ?? ticket.Numero}" : 
                                      $"BL {ticket.NumeroBl ?? ticket.Numero}";
                                      
                var roleInstructeur = estComplaint ? LangueCourante.T("Le Service Client", "Customer Service") :
                                      estExport ? LangueCourante.T("Le Back Office Export", "The Export Back Office") : 
                                      LangueCourante.T("Le Pilote Manifeste", "The Manifest Pilot");

                var titre = estComplaint ? LangueCourante.T($"Complément demandé pour votre {referenceMetier}", $"Additional information requested for your {referenceMetier}") :
                            LangueCourante.T($"Complément demandé pour le {referenceMetier}", $"Additional information requested for {referenceMetier}");

                actionsRequises.Add(new ActionRequiseItemVue(
                    TicketId: ticket.Id,
                    NumeroTicket: ticket.Numero,
                    NumeroBl: ticket.NumeroBl,
                    TypeAction: typeAction,
                    Titre: titre,
                    Description: await DescriptionComplementAsync(ticket.Id, roleInstructeur, ct).ConfigureAwait(false),
                    ActionLibelle: LangueCourante.T("Déposer le complément", "Submit the additional information"),
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

        // 5. Projection des dossiers récents avec état dérivé (et étape métier de chaque BL / booking)
        var nomsFiles = files is null ? new Dictionary<Guid, string>() : (await files.ListerAsync(ct).ConfigureAwait(false)).ToDictionary(f => f.Id, f => f.Nom);
        var etapes = new Dictionary<Guid, string?>();
        foreach (var t in ticketsConformes)
        {
            etapes[t.Id] = nomsFiles.TryGetValue(t.FileCouranteId, out var nomFile)
                ? EtapeParcoursClient.Determiner(t.Module, nomFile, t.Statut, t.EtatProforma,
                    t.TypeAnnulationImport == TypeAnnulationImport.RejetManifeste || t.TypeAnnulationExport == TypeAnnulationExport.RejetBackOffice,
                    await PreuveDeposeeAsync(t, nomFile, ct).ConfigureAwait(false))
                : null;
        }

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
                NumeroBooking: t.NumeroBooking,
                Etape: etapes.GetValueOrDefault(t.Id)
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

        var nomSociete = societe.NomAffichable;

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

    /// <summary>Preuve de paiement déposée depuis l'arrivée en file Validation Paiement (seule file concernée).</summary>
    private async Task<bool> PreuveDeposeeAsync(Ticket ticket, string nomFile, CancellationToken ct) =>
        documents is not null && nomFile == BillingImport.NomsFiles.ValidationPaiement
        && (await documents.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false)).Any(d =>
            d.Categorie == Domain.Documents.CategorieDocumentBillingImport.PreuvePaiement && d.DateDepotUtc >= ticket.DateArriveeEnFileUtc);

    /// <summary>Parcours Billing : une proforma à valider ou une preuve de paiement refusée ne sont pas
    /// des « compléments » — l'action proposée au client doit dire exactement quoi faire.</summary>
    private async Task<ActionRequiseItemVue?> ActionBillingSpecifiqueAsync(Ticket ticket, CancellationToken ct)
    {
        var reference = ticket.NumeroBooking is { } booking ? $"Booking {booking}" : $"BL {ticket.NumeroBl ?? ticket.Numero}";
        if (ticket.EtatProforma == EtatProforma.EnAttenteValidation)
        {
            var jusquau = ticket.DateEnlevementSouhaitee is DateOnly d ? d.ToString("dd/MM/yyyy") : null;
            return new ActionRequiseItemVue(
                TicketId: ticket.Id, NumeroTicket: ticket.Numero, NumeroBl: ticket.NumeroBl, TypeAction: TypeActionRequise.ProformaAValider,
                Titre: LangueCourante.T($"Proforma à valider pour le {reference}", $"Proforma to approve for {reference}"),
                Description: jusquau is null
                    ? LangueCourante.T("Le Pilote Facturation a déposé votre proforma : validez-la ou demandez une correction.", "The Billing Pilot uploaded your proforma: approve it or request a correction.")
                    : LangueCourante.T($"Le Pilote Facturation a déposé votre proforma, calculée jusqu'au {jusquau} : validez-la ou demandez une correction.", $"The Billing Pilot uploaded your proforma, calculated up to {jusquau}: approve it or request a correction."),
                ActionLibelle: LangueCourante.T("Répondre à la proforma", "Respond to the proforma"),
                UrlCible: $"/tickets/{ticket.Id}", DateDeclenchementUtc: ticket.DateCreationUtc, NumeroBooking: ticket.NumeroBooking);
        }

        var refus = historique is null ? null : (await historique.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false))
            .Where(e => e.Evenement is "PreuvePaiementRefusee" or "ComplementDemandeImport" or "ComplementDemandeExport" or "TicketEnAttenteClient")
            .MaxBy(e => e.HorodatageUtc);
        if (refus?.Evenement != "PreuvePaiementRefusee")
        {
            return null;
        }

        return new ActionRequiseItemVue(
            TicketId: ticket.Id, NumeroTicket: ticket.Numero, NumeroBl: ticket.NumeroBl, TypeAction: TypeActionRequise.NouvellePreuveRequise,
            Titre: LangueCourante.T($"Nouvelle preuve de paiement attendue pour le {reference}", $"New proof of payment expected for {reference}"),
            Description: LangueCourante.T($"La caisse a refusé la preuve déposée : {refus.Detail}", $"The cashier refused the uploaded proof: {refus.Detail}"),
            ActionLibelle: LangueCourante.T("Déposer une nouvelle preuve", "Upload a new proof"),
            UrlCible: $"/tickets/{ticket.Id}", DateDeclenchementUtc: ticket.DateCreationUtc, NumeroBooking: ticket.NumeroBooking);
    }

    /// <summary>Ce que l'agent attend du client (dernière demande de complément ou mise en attente consignée), à défaut
    /// une mention générique. Lu ticket par ticket : seuls les tickets en attente client sont concernés.</summary>
    private async Task<string> DescriptionComplementAsync(Guid ticketId, string roleInstructeur, CancellationToken ct)
    {
        var demande = historique is null ? null : (await historique.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false))
            .Where(e => (e.Evenement.StartsWith("ComplementDemande", StringComparison.Ordinal) || e.Evenement == "TicketEnAttenteClient")
                && !string.IsNullOrWhiteSpace(e.Detail))
            .MaxBy(e => e.HorodatageUtc);

        return demande is null
            ? LangueCourante.T($"{roleInstructeur} a sollicité un complément d'information ou des pièces justificatives.", $"{roleInstructeur} has requested additional information or supporting documents.")
            : LangueCourante.T($"{roleInstructeur} attend : {demande.Detail}", $"{roleInstructeur} is waiting for: {demande.Detail}");
    }
}
