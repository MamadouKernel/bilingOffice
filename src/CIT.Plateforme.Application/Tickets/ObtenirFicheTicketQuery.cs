using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

public sealed class ObtenirFicheTicketQuery(
    ITicketRepository ticketRepository,
    IDossierRepository dossierRepository,
    IFileDeTraitementRepository fileRepository,
    ISocieteClienteRepository societeRepository,
    IUtilisateurInfoRepository utilisateurInfoRepository,
    IHistoriqueTicketRepository historiqueRepository,
    IDocumentRepository documentRepository,
    IMessageRepository messageRepository,
    IHabilitationRepository habilitationRepository,
    ICategorieReclamationRepository categorieReclamationRepository,
    ILiaisonTicketRepository liaisonRepository,
    ControleAccesRessourceService controleAcces)
{
    public async Task<FicheTicketVue> ExecuterAsync(Guid appelantId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var dossier = await dossierRepository.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {ticket.DossierId} introuvable.");

        // ORDRE (remédiation P0-bis) : l'autorisation précède TOUTE autre lecture. Elle
        // s'exécutait auparavant après la résolution de la société, si bien qu'un appelant non
        // autorisé déclenchait une requête et recevait un message citant un identifiant interne.
        // Un refus ne doit rien apprendre.
        await controleAcces.ExigerConsultationTicketAsync(appelantId, ticket, ct).ConfigureAwait(false);

        // Un dossier PERSONNEL (Enrôlement) porte un utilisateur dans ClientId, non une société :
        // l'absence de société y est normale et ne doit pas faire échouer la lecture.
        var societe = await societeRepository.ObtenirParIdAsync(dossier.ClientId, ct).ConfigureAwait(false);

        var appelantInfo = await utilisateurInfoRepository.ObtenirInfoAsync(appelantId, ct).ConfigureAwait(false);
        // P3 — FAIL-CLOSED. Cette expression valait `appelantInfo?.Nature == Client` : un compte
        // introuvable donnait `false`, donc la vue AGENT, notes internes comprises. Le chemin est
        // aujourd'hui inatteignable — la garde d'identité refuse en amont — mais la protection
        // reposait alors sur l'ordre des opérations plutôt que sur l'expression elle-même.
        // En cas d'absence d'information, c'est la vue la PLUS RESTRICTIVE qui s'applique.
        bool estClient = appelantInfo is null || appelantInfo.Nature == NatureUtilisateur.Client;

        // REMÉDIATION P0-bis. La garde était intégralement enveloppée dans `if (estClient)` : la
        // branche AGENT n'existait pas. Un compte agent sans la moindre habilitation — et même un
        // identifiant d'utilisateur inconnu, puisqu'il n'était pas « client » non plus — obtenait
        // la fiche complète, notes internes comprises. Le raisonnement « ce n'est pas un client,
        // donc on laisse passer » est exactement ce que le refus par défaut interdit.
        //
        // La décision n'est plus réécrite ici : c'est la quatrième réimplémentation qu'il fallait
        // éviter, et la troisième qui avait déjà divergé. Elle est appliquée plus haut, avant
        // toute lecture.

        // 2. Récupération métadonnées complémentaires
        var file = await fileRepository.ObtenirParIdAsync(ticket.FileCouranteId, ct).ConfigureAwait(false);
        var fileNom = file?.Nom ?? "File générale";

        string? operateurNom = null;
        if (ticket.OperateurAssigneId is Guid opId)
        {
            var opInfo = await utilisateurInfoRepository.ObtenirInfoAsync(opId, ct).ConfigureAwait(false);
            operateurNom = opInfo?.NomAffiche ?? "Opérateur assigné";
        }

        // 3. Chronologie / Timeline
        var historique = await historiqueRepository.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var auteurIds = historique.Where(h => h.AuteurId.HasValue).Select(h => h.AuteurId!.Value).Distinct().ToList();
        var auteursInfo = await utilisateurInfoRepository.ObtenirInfosAsync(auteurIds, ct).ConfigureAwait(false);

        var chronologie = historique.Select(h =>
        {
            string auteurNom = "Système CIT";
            if (h.AuteurId.HasValue && auteursInfo.TryGetValue(h.AuteurId.Value, out var a))
            {
                auteurNom = a.NomAffiche ?? a.Email;
            }

            return new ChronologieItemVue(
                Id: h.Id,
                Evenement: h.Evenement,
                Detail: h.Detail,
                AuteurId: h.AuteurId,
                AuteurNom: auteurNom,
                HorodatageUtc: h.HorodatageUtc
            );
        }).ToList();

        // 4. Documents attachés
        var docs = await documentRepository.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var documents = docs.Select(d => new DocumentItemVue(
            Id: d.Id,
            NomFichier: d.NomFichier,
            TypeContenu: d.TypeContenu,
            TailleOctets: d.TailleOctets,
            DateDepotUtc: d.DateDepotUtc,
            StatutAntivirus: d.StatutAntivirus,
            Categorie: d.Categorie
        )).ToList();

        // 5. Messages / Échanges
        IReadOnlyList<Message> msgs = estClient
            ? await messageRepository.ListerVisiblesParClientAsync(ticketId, ct).ConfigureAwait(false)
            : await messageRepository.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);

        var msgAuteurIds = msgs.Select(m => m.AuteurId).Distinct().ToList();
        var msgAuteursInfo = await utilisateurInfoRepository.ObtenirInfosAsync(msgAuteurIds, ct).ConfigureAwait(false);

        var messages = msgs.Select(m =>
        {
            string auteur = "Utilisateur";
            if (msgAuteursInfo.TryGetValue(m.AuteurId, out var inf))
            {
                auteur = inf.NomAffiche ?? inf.Email;
            }

            return new MessageItemVue(
                Id: m.Id,
                Contenu: m.Contenu,
                AuteurNom: auteur,
                EstInterne: m.Type == TypeMessage.NoteInterne,
                DateEnvoiUtc: m.HorodatageUtc
            );
        }).ToList();

        // 6. Détermination des permissions contextuelles
        var habilitations = await habilitationRepository.ListerParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        bool aDroitPriseEnCharge = habilitations.Any(h => (h.PermissionCode == Permission.TicketPrendreEnCharge || h.PermissionCode == Permission.ComplaintInstruire) && (h.FileId == null || h.FileId == ticket.FileCouranteId));
        bool aDroitTransferer = habilitations.Any(h => (h.PermissionCode == Permission.TicketTransferer || h.PermissionCode == Permission.ComplaintInstruire) && (h.FileId == null || h.FileId == ticket.FileCouranteId));
        bool aDroitCloturer = habilitations.Any(h => (h.PermissionCode == Permission.TicketCloturer || h.PermissionCode == Permission.ComplaintInstruire) && (h.FileId == null || h.FileId == ticket.FileCouranteId));

        bool peutPrendreEnCharge = !estClient && ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge && aDroitPriseEnCharge;
        bool peutTransferer = !estClient && ticket.Statut == StatutTicket.EnCours && aDroitTransferer;
        bool peutMettreEnAttenteClient = !estClient && ticket.Statut == StatutTicket.EnCours && ticket.OperateurAssigneId == appelantId;
        bool peutReprendreApresClient = (!estClient || ticket.Statut == StatutTicket.EnAttenteClient) && ticket.Statut == StatutTicket.EnAttenteClient;
        bool peutCloturer = !estClient && ticket.Statut == StatutTicket.EnCours && aDroitCloturer;
        bool peutAnnuler = estClient && ticket.Statut != StatutTicket.Cloture && ticket.Statut != StatutTicket.Annule && !ticket.EstEnEtatEngageant;
        bool peutAjouterMessage = ticket.Statut != StatutTicket.Cloture && ticket.Statut != StatutTicket.Annule;
        bool peutDeposerDocument = ticket.Statut != StatutTicket.Cloture && ticket.Statut != StatutTicket.Annule;

        // IMP-10 — la demande de facture appartient au CLIENT, sur un ticket Billing encore actif.
        // La commande vérifie en outre la propriété du ticket (cloisonnement) : ce droit ne fait
        // qu'afficher ou masquer l'action, il ne la remplace pas.
        bool peutDemanderFacture = estClient
            && (ticket.Module == "BillingImport" || ticket.Module == "BillingExport")
            && ticket.Statut != StatutTicket.Cloture
            && ticket.Statut != StatutTicket.Annule;

        var societeNom = societe?.InformationsDeclarees ?? string.Empty;
        if (societeNom.Contains("RaisonSociale"))
        {
            societeNom = ExtraireNom(societeNom);
        }

        // SOC-10 — les tickets liés. La liaison se lit dans les deux sens (le repository couvre
        // les deux côtés) : un ticket créé plus tard et rattaché à celui-ci doit apparaître ici
        // aussi, sans quoi la liaison ne serait visible que depuis la fiche qui l'a posée.
        var liaisonsBrutes = await liaisonRepository.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var liaisons = new List<LiaisonItemVue>(liaisonsBrutes.Count);
        foreach (var liaison in liaisonsBrutes)
        {
            var autreId = liaison.TicketId == ticketId ? liaison.TicketLieId : liaison.TicketId;
            var autre = await ticketRepository.ObtenirParIdAsync(autreId, ct).ConfigureAwait(false);
            if (autre is not null)
            {
                liaisons.Add(new LiaisonItemVue(autre.Id, autre.Numero, autre.Module, autre.Statut, liaison.DateCreationUtc));
            }
        }

        // Poser une liaison est un acte d'agent instruisant la file (même habilitation que la
        // prise en charge, cf. LierTicketsCommand) : le client la consulte, il ne la crée pas.
        bool peutLierTickets = !estClient && aDroitPriseEnCharge;

        // 7. Métadonnées spécifiques Customer Complaint (REC-01, REC-02, REC-04, REC-06)
        string? categorieLibelle = null;
        if (ticket.CategorieReclamationId.HasValue)
        {
            var categorie = await categorieReclamationRepository.ObtenirParIdAsync(ticket.CategorieReclamationId.Value, ct).ConfigureAwait(false);
            categorieLibelle = categorie?.Libelle;
        }

        return new FicheTicketVue(
            TicketId: ticket.Id,
            NumeroTicket: ticket.Numero,
            NumeroBl: ticket.NumeroBl,
            Module: ticket.Module,
            TypeDemande: ticket.TypeDemande,
            Statut: ticket.Statut,
            DossierId: dossier.Id,
            NumeroDossier: dossier.Numero,
            FileId: ticket.FileCouranteId,
            FileNom: fileNom,
            OperateurAssigneId: ticket.OperateurAssigneId,
            OperateurAssigneNom: operateurNom,
            SocieteId: societe?.Id ?? Guid.Empty,
            SocieteNom: societeNom,
            DateCreationUtc: ticket.DateCreationUtc,
            DatePriseEnChargeUtc: ticket.DatePriseEnChargeUtc,
            DateClotureUtc: ticket.DateClotureUtc,
            EstArchive: ticket.EstArchive,
            DateArchivageUtc: ticket.DateArchivageUtc,
            EstEnEtatEngageant: ticket.EstEnEtatEngageant,
            DeclarationPresentielle: ticket.DeclarationPresentielle,
            PeutPrendreEnCharge: peutPrendreEnCharge,
            PeutTransferer: peutTransferer,
            PeutMettreEnAttenteClient: peutMettreEnAttenteClient,
            PeutReprendreApresClient: peutReprendreApresClient,
            PeutCloturer: peutCloturer,
            PeutAnnuler: peutAnnuler,
            PeutAjouterMessage: peutAjouterMessage,
            PeutDeposerDocument: peutDeposerDocument,
            Chronologie: chronologie,
            Documents: documents,
            Messages: messages,
            NumeroBooking: ticket.NumeroBooking,
            TypeAnnulationExport: ticket.TypeAnnulationExport,
            CategorieReclamationId: ticket.CategorieReclamationId,
            CategorieReclamationLibelle: categorieLibelle,
            Objet: ticket.Objet,
            Description: ticket.Description,
            ReponseFormalisee: ticket.ReponseFormalisee,
            EvaluationNote: ticket.EvaluationNote,
            EvaluationCommentaire: ticket.EvaluationCommentaire,
            EvaluationDateUtc: ticket.EvaluationDateUtc,
            PeutDemanderFacture: peutDemanderFacture,
            Liaisons: liaisons,
            PeutLierTickets: peutLierTickets,
            LibellePaiementCdc: CIT.Plateforme.Application.BillingImport.LibellePaiementCdcHelper.PourFile(ticket.Module, fileNom)
        );
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
