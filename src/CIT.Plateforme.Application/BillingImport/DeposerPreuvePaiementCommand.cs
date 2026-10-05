using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>Contrat C3-10 — Déposer une preuve de paiement. CDC : IMP-12. Sert aussi l'export.
/// La pièce est classée « preuve de paiement » (la caisse la retrouve sur la ligne de sa file), et
/// une nouvelle preuve après un refus vaut réponse du client : le traitement reprend, SLA compris.</summary>
public sealed class DeposerPreuvePaiementCommand(
    DeposerDocumentCommand deposerDocument,
    ITicketRepository tickets,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid clientId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets,
        ReglesDepotDocument reglesEtape, CancellationToken ct = default)
    {
        await deposerDocument.DeposerPourClientAsync(
            clientId, ticketId, nomFichier, typeContenu, contenu, tailleOctets, reglesEtape, CategorieDocumentBillingImport.PreuvePaiement, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        if (ticket.Statut != StatutTicket.EnAttenteClient)
        {
            return;
        }

        ticket.RependreApresReponseClient(horloge.MaintenantUtc);
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "NouvellePreuvePaiementDeposee", nomFichier, clientId, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
