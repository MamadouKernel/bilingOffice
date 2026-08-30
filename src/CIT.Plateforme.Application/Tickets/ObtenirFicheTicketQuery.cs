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
    IUtilisateurSocieteRepository utilisateurSocieteRepository,
    IUtilisateurInfoRepository utilisateurInfoRepository,
    IHistoriqueTicketRepository historiqueRepository,
    IDocumentRepository documentRepository,
    IMessageRepository messageRepository,
    IHabilitationRepository habilitationRepository)
{
    public async Task<FicheTicketVue> ExecuterAsync(Guid appelantId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var dossier = await dossierRepository.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {ticket.DossierId} introuvable.");

        var societe = await societeRepository.ObtenirParIdAsync(dossier.ClientId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Société {dossier.ClientId} introuvable.");

        var appelantInfo = await utilisateurInfoRepository.ObtenirInfoAsync(appelantId, ct).ConfigureAwait(false);
        bool estClient = appelantInfo?.Nature == NatureUtilisateur.Client;

        // 1. Contrôle de cloisonnement serveur (Principe VII)
        if (estClient)
        {
            var userSoc = await utilisateurSocieteRepository.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
            if (userSoc is null || !userSoc.EstActif || userSoc.SocieteId != dossier.ClientId)
            {
                throw new AccesRefuseException($"L'utilisateur client {appelantId} n'est pas autorisé à consulter le ticket {ticket.Numero}.");
            }

            if (userSoc.DroitConsultation == DroitConsultation.PropresDemandesUniquement && !userSoc.EstAdministrateur && dossier.AuteurId != appelantId)
            {
                throw new AccesRefuseException($"L'utilisateur {appelantId} ne peut consulter que ses propres demandes.");
            }
        }

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
            StatutAntivirus: d.StatutAntivirus
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
        bool aDroitPriseEnCharge = habilitations.Any(h => h.PermissionCode == Permission.TicketPrendreEnCharge && (h.FileId == null || h.FileId == ticket.FileCouranteId));
        bool aDroitTransferer = habilitations.Any(h => h.PermissionCode == Permission.TicketTransferer && (h.FileId == null || h.FileId == ticket.FileCouranteId));
        bool aDroitCloturer = habilitations.Any(h => h.PermissionCode == Permission.TicketCloturer && (h.FileId == null || h.FileId == ticket.FileCouranteId));

        bool peutPrendreEnCharge = !estClient && ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge && aDroitPriseEnCharge;
        bool peutTransferer = !estClient && ticket.Statut == StatutTicket.EnCours && aDroitTransferer;
        bool peutMettreEnAttenteClient = !estClient && ticket.Statut == StatutTicket.EnCours && ticket.OperateurAssigneId == appelantId;
        bool peutReprendreApresClient = (!estClient || ticket.Statut == StatutTicket.EnAttenteClient) && ticket.Statut == StatutTicket.EnAttenteClient;
        bool peutCloturer = !estClient && ticket.Statut == StatutTicket.EnCours && aDroitCloturer;
        bool peutAnnuler = estClient && ticket.Statut != StatutTicket.Cloture && ticket.Statut != StatutTicket.Annule && !ticket.EstEnEtatEngageant;
        bool peutAjouterMessage = ticket.Statut != StatutTicket.Cloture && ticket.Statut != StatutTicket.Annule;
        bool peutDeposerDocument = ticket.Statut != StatutTicket.Cloture && ticket.Statut != StatutTicket.Annule;

        var societeNom = societe.InformationsDeclarees;
        if (societeNom.Contains("RaisonSociale"))
        {
            societeNom = ExtraireNom(societeNom);
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
            SocieteId: societe.Id,
            SocieteNom: societeNom,
            DateCreationUtc: ticket.DateCreationUtc,
            DatePriseEnChargeUtc: ticket.DatePriseEnChargeUtc,
            DateClotureUtc: ticket.DateClotureUtc,
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
            TypeAnnulationExport: ticket.TypeAnnulationExport
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
