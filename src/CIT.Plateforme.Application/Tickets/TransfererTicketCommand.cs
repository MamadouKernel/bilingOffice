using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Levée quand la file cible n'est pas une destination autorisée par la matrice
/// (TRF-06) : aucun transfert hors matrice n'est possible, quelle que soit l'habilitation.</summary>
public sealed class TransitionNonAutoriseeException(string message) : Exception(message);

/// <summary>
/// Levée quand le motif fourni ne figure pas parmi les motifs actifs du référentiel (§ 9.10).
/// Ne survient QUE si le référentiel est peuplé : tant qu'il est vide, un motif libre est
/// recevable, faute de quoi la plateforme serait intransférable avant tout paramétrage.
/// </summary>
public sealed class MotifTransfertInconnuException(string message) : Exception(message);

/// <summary>
/// Contrat C-15 — Transférer un ticket vers une autre file. CDC : TRF-01, TRF-02, TRF-03, TRF-05,
/// TRF-06. Applique CLAR-03 (ticket non affecté après transfert sauf destinataire désigné).
/// </summary>
public sealed class TransfererTicketCommand(
    ITicketRepository tickets,
    ITransitionAutoriseeRepository transitions,
    IMotifTransfertRepository motifsTransfert,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid initiateurId, Guid ticketId, Guid fileCibleId, string motif, Guid? destinataireId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de transfert est obligatoire (TRF-03).", nameof(motif));
        }

        // § 9.10 — « les motifs de transfert » sont un référentiel administrable. Dès qu'il porte au
        // moins un motif actif, il fait autorité : le texte libre cesse d'être accepté, sans quoi le
        // paramétrage n'aurait aucun effet et deux opérateurs continueraient de nommer différemment
        // la même situation.
        //
        // Tant qu'il est vide, un motif libre reste recevable. C'est ce qui permet de livrer la
        // plateforme sans avoir inventé une nomenclature que le CDC ne donne pas.
        var motifNormalise = motif.Trim();
        if (await motifsTransfert.ContientAuMoinsUnMotifActifAsync(ct).ConfigureAwait(false)
            && !await motifsTransfert.CodeActifExisteAsync(motifNormalise, ct).ConfigureAwait(false))
        {
            throw new MotifTransfertInconnuException(
                $"Le motif « {motifNormalise} » ne figure pas parmi les motifs de transfert actifs du référentiel (§ 9.10).");
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(initiateurId, Permission.TicketTransferer, ticket.FileCouranteId, ct).ConfigureAwait(false);
        await global::CIT.Plateforme.Application.Securite.GardeAgentAffecte.ExigerAsync(autorisation, initiateurId, ticket, ct).ConfigureAwait(false);

        var transitionAutorisee = await transitions.ObtenirAsync(ticket.FileCouranteId, fileCibleId, ct).ConfigureAwait(false);
        if (transitionAutorisee is null || !transitionAutorisee.AutoriseLeMotif(motif))
        {
            throw new TransitionNonAutoriseeException(
                $"La transition de la file {ticket.FileCouranteId} vers {fileCibleId} n'est pas autorisée par la matrice (TRF-06).");
        }

        var fileSource = ticket.FileCouranteId;
        ticket.Transferer(fileCibleId, destinataireId, horloge.MaintenantUtc);

        // TRF-02 : le ticket conserve son numéro, son dossier, son historique, ses documents et
        // ses commentaires — aucune de ces données n'est touchée par Transferer().
        await journalisation.ConsignerHistoriqueAsync(
            ticket.Id, "TicketTransfere", $"de={fileSource};vers={fileCibleId};motif={motif}", initiateurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            initiateurId, "TicketTransfere", nameof(Ticket), ticket.Id, fileSource.ToString(), fileCibleId.ToString(), ct).ConfigureAwait(false);

        // TRF-05 : le client est notifié — le socle publie l'événement, le canal effectif relève
        // de la future feature Notifications (D10, PC-25, hors périmètre).
        await journalisation.PublierEvenementAsync(
            "TicketTransfere", $"{{\"ticketId\":\"{ticket.Id}\",\"fileCibleId\":\"{fileCibleId}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
