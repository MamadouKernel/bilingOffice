using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Découverte à l'implémentation (documentée dans implementation-report.md, "Écarts") :
/// <see cref="ObtenirFicheTicketQuery"/> (F001/F006) exige que <c>Dossier.ClientId</c> soit
/// résolvable en <c>SocieteCliente</c> — toujours faux pour un ticket Enrolement, puisque
/// <c>ClientId</c> y est l'identifiant du Transporteur/Chauffeur lui-même (research.md D1). Cette
/// query dédiée reprend la même structure (cloisonnement, chronologie, documents, messages) sans
/// la dépendance société, et calcule les actions spécifiques à F008 (compléter, valider, convoquer,
/// constater le badge).
/// </summary>
public sealed record FicheEnrolementVue(
    Guid TicketId,
    string NumeroTicket,
    StatutTicket Statut,
    Guid DossierId,
    string NumeroDossier,
    Guid ClientId,
    Guid FileId,
    string FileNom,
    Guid? OperateurAssigneId,
    string? OperateurAssigneNom,
    DateTime DateCreationUtc,
    DateTime? DatePriseEnChargeUtc,
    DateTime? DateClotureUtc,
    string? Objet,
    string? Description,
    bool PeutPrendreEnCharge,
    bool PeutDemanderComplement,
    bool PeutValider,
    bool PeutConvoquer,
    bool PeutConstaterBadge,
    bool PeutRepondreComplement,
    bool PeutDeposerDocument,
    bool PeutAjouterMessage,
    IReadOnlyList<ChronologieItemVue> Chronologie,
    IReadOnlyList<DocumentItemVue> Documents,
    IReadOnlyList<MessageItemVue> Messages);

public sealed class ObtenirFicheEnrolementQuery(
    ITicketRepository ticketRepository,
    IDossierRepository dossierRepository,
    IFileDeTraitementRepository fileRepository,
    IHistoriqueTicketRepository historiqueRepository,
    IDocumentRepository documentRepository,
    IMessageRepository messageRepository,
    IHabilitationRepository habilitationRepository,
    IUtilisateurInfoRepository utilisateurInfoRepository,
    ControleAccesRessourceService controleAcces)
{
    public async Task<FicheEnrolementVue> ExecuterAsync(Guid appelantId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await ticketRepository.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        if (ticket.Module != "Enrolement")
        {
            throw new InvalidOperationException("Ce contrat ne s'applique qu'aux tickets Enrolement.");
        }

        var dossier = await dossierRepository.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {ticket.DossierId} introuvable.");

        // ── 1. CONTRÔLE GÉNÉRIQUE — délégué (centralisation P2-01) ────────────────────────
        //
        // Cette requête portait sa propre implémentation du cloisonnement : quatrième copie d'une
        // décision qui en comptait cinq. Elle traitait correctement la propriété du transporteur
        // et l'habilitation de l'agent, mais ignorait l'état du compte — la faille P0-ter — et sa
        // seule protection contre un compte désactivé était la garde d'interface de
        // FicheEnrolement.razor. Une frontière de sécurité ne vit pas dans une page.
        //
        // Identité valide, compte actif, propriété du dossier, portée de file : plus rien de tout
        // cela n'est réécrit ici.
        await controleAcces.ExigerConsultationTicketAsync(appelantId, ticket, ct).ConfigureAwait(false);

        var appelantInfo = await utilisateurInfoRepository.ObtenirInfoAsync(appelantId, ct).ConfigureAwait(false);
        // P3 — FAIL-CLOSED. Cette expression valait `appelantInfo?.Nature == Client` : un compte
        // introuvable donnait `false`, donc la vue AGENT, notes internes comprises. Le chemin est
        // aujourd'hui inatteignable — la garde d'identité refuse en amont — mais la protection
        // reposait alors sur l'ordre des opérations plutôt que sur l'expression elle-même.
        // En cas d'absence d'information, c'est la vue la PLUS RESTRICTIVE qui s'applique.
        var estClient = appelantInfo is null || appelantInfo.Nature == NatureUtilisateur.Client;

        var habilitations = await habilitationRepository.ListerParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        var aDroitInstruction = habilitations.Any(h => h.PermissionCode == Permission.EnrolementInstruire && (h.FileId == null || h.FileId == ticket.FileCouranteId));

        // ── 2. CONTRÔLE MÉTIER SPÉCIFIQUE — conservé ──────────────────────────────────────
        //
        // Le contrôle central admet un agent porteur d'une habilitation QUELCONQUE couvrant la
        // file. L'Enrôlement est plus strict : il exige `enrolement.instruire` (F008). Cette
        // exigence est propre au module, elle n'a donc pas à remonter au contrôle central — mais
        // elle s'ajoute à lui, elle ne le remplace pas.
        if (!estClient && !aDroitInstruction)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} n'est pas habilité à consulter ce dossier d'enrôlement.");
        }

        var file = await fileRepository.ObtenirParIdAsync(ticket.FileCouranteId, ct).ConfigureAwait(false);

        string? operateurNom = null;
        if (ticket.OperateurAssigneId is Guid opId)
        {
            var opInfo = await utilisateurInfoRepository.ObtenirInfoAsync(opId, ct).ConfigureAwait(false);
            operateurNom = opInfo?.NomAffiche ?? "Agent Gate";
        }

        var historique = await historiqueRepository.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var auteurIds = historique.Where(h => h.AuteurId.HasValue).Select(h => h.AuteurId!.Value).Distinct().ToList();
        var auteursInfo = await utilisateurInfoRepository.ObtenirInfosAsync(auteurIds, ct).ConfigureAwait(false);
        var chronologie = historique.Select(h => new ChronologieItemVue(
            Id: h.Id,
            Evenement: h.Evenement,
            Detail: h.Detail,
            AuteurId: h.AuteurId,
            AuteurNom: h.AuteurId.HasValue && auteursInfo.TryGetValue(h.AuteurId.Value, out var a) ? (a.NomAffiche ?? a.Email) : "Système CIT",
            HorodatageUtc: h.HorodatageUtc)).ToList();

        var docs = await documentRepository.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var documents = docs.Select(d => new DocumentItemVue(d.Id, d.NomFichier, d.TypeContenu, d.TailleOctets, d.DateDepotUtc, d.StatutAntivirus)).ToList();

        IReadOnlyList<Message> msgs = estClient
            ? await messageRepository.ListerVisiblesParClientAsync(ticketId, ct).ConfigureAwait(false)
            : await messageRepository.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        var msgAuteurIds = msgs.Select(m => m.AuteurId).Distinct().ToList();
        var msgAuteursInfo = await utilisateurInfoRepository.ObtenirInfosAsync(msgAuteurIds, ct).ConfigureAwait(false);
        var messages = msgs.Select(m => new MessageItemVue(
            m.Id, m.Contenu,
            msgAuteursInfo.TryGetValue(m.AuteurId, out var inf) ? (inf.NomAffiche ?? inf.Email) : "Utilisateur",
            m.Type == TypeMessage.NoteInterne, m.HorodatageUtc)).ToList();

        var peutPrendreEnCharge = !estClient && aDroitInstruction && ticket.Statut == StatutTicket.EnAttenteDePriseEnCharge;
        var peutDemanderComplement = !estClient && aDroitInstruction && ticket.Statut == StatutTicket.EnCours;
        var peutValider = !estClient && aDroitInstruction && ticket.Statut == StatutTicket.EnCours;
        var peutConvoquer = !estClient && aDroitInstruction && ticket.Statut == StatutTicket.EnCours;
        var peutConstaterBadge = !estClient && aDroitInstruction && ticket.Statut == StatutTicket.EnCours;
        var peutRepondreComplement = estClient && ticket.Statut == StatutTicket.EnAttenteClient;
        var peutDeposerDocument = ticket.Statut != StatutTicket.Cloture && ticket.Statut != StatutTicket.Annule;
        var peutAjouterMessage = ticket.Statut != StatutTicket.Cloture && ticket.Statut != StatutTicket.Annule;

        return new FicheEnrolementVue(
            TicketId: ticket.Id,
            NumeroTicket: ticket.Numero,
            Statut: ticket.Statut,
            DossierId: dossier.Id,
            NumeroDossier: dossier.Numero,
            ClientId: dossier.ClientId,
            FileId: ticket.FileCouranteId,
            FileNom: file?.Nom ?? "Agent Gate",
            OperateurAssigneId: ticket.OperateurAssigneId,
            OperateurAssigneNom: operateurNom,
            DateCreationUtc: ticket.DateCreationUtc,
            DatePriseEnChargeUtc: ticket.DatePriseEnChargeUtc,
            DateClotureUtc: ticket.DateClotureUtc,
            Objet: ticket.Objet,
            Description: ticket.Description,
            PeutPrendreEnCharge: peutPrendreEnCharge,
            PeutDemanderComplement: peutDemanderComplement,
            PeutValider: peutValider,
            PeutConvoquer: peutConvoquer,
            PeutConstaterBadge: peutConstaterBadge,
            PeutRepondreComplement: peutRepondreComplement,
            PeutDeposerDocument: peutDeposerDocument,
            PeutAjouterMessage: peutAjouterMessage,
            Chronologie: chronologie,
            Documents: documents,
            Messages: messages);
    }
}
