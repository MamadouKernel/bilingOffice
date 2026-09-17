using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// IMP-15 : créneau optionnel accompagnant la bascule présentielle. Renseigné, un véritable
/// rendez-vous est créé et lié au ticket via <see cref="ReserverRendezVousCommand"/> (F010,
/// CON-03) — la « trace de cette bascule » devient un rendez-vous réel, pas seulement un drapeau.
/// </summary>
public sealed record CreneauPresentielRequete(
    DateOnly Date,
    TimeSpan HeureDebut,
    string Motif = "Finalisation présentielle Billing Import (IMP-15)"
);

/// <summary>
/// Contrat C3-19 — Déclarer une finalisation présentielle. CDC : IMP-15 (Should).
/// <para>
/// REMÉDIATION (post-implementation-audit.md) : à l'origine, seul le drapeau
/// <c>DeclarationPresentielle</c> était posé sur le ticket — « le ticket porte la trace de cette
/// bascule » restait un simple booléen, sans rendez-vous réel. F010 (CON-03) existe désormais et
/// porte la réservation/gestion de rendez-vous : cette commande lui délègue la création d'un
/// rendez-vous réel quand un créneau est fourni. Le paramètre reste optionnel : les appelants
/// existants (drapeau seul, sans rendez-vous) continuent de fonctionner à l'identique.
/// </para>
/// </summary>
public sealed class DeclarerFinalisationPresentielleCommand(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    CloisonnementService cloisonnement,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail,
    ReserverRendezVousCommand? reserverRendezVous = null)
{
    public async Task ExecuterAsync(Guid clientId, Guid ticketId, CreneauPresentielRequete? creneau = null, CancellationToken ct = default)
    {
        await cloisonnement.ExigerProprieteClientAsync(clientId, ticketId, ct).ConfigureAwait(false);

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var filePiloteManifeste = toutesLesFiles.SingleOrDefault(f =>
            f.Module == SoumettreDemandeImportCommand.Module && f.Nom == SoumettreDemandeImportCommand.FilePiloteManifeste);

        // C3-19 : précondition « ticket validé par le Pilote Manifeste (à l'issue du contrôle BL) ».
        // Un ticket rejeté ou en attente de complément n'a jamais quitté la file Pilote Manifeste
        // (RejeterTicketImportCommand/GestionComplementManifesteCommand n'y transfèrent pas le
        // ticket) — seule la validation (aiguillage, D5) en fait sortir le ticket.
        if (filePiloteManifeste is not null && ticket.FileCouranteId == filePiloteManifeste.Id)
        {
            throw new InvalidOperationException(
                $"Le ticket {ticketId} n'a pas encore été validé par le Pilote Manifeste (IMP-15).");
        }

        ticket.DeclarerFinalisationPresentielle();

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "FinalisationPresentielleDeclaree", null, clientId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(clientId, "FinalisationPresentielleDeclaree", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        // IMP-15 : créneau fourni → un véritable rendez-vous est réservé et lié au ticket. La
        // réservation retombe sur ses propres règles (unicité de créneau, capacité, etc.) : un
        // échec de réservation ne doit pas effacer la bascule présentielle déjà actée ci-dessus.
        if (creneau is not null)
        {
            if (reserverRendezVous is null)
            {
                throw new InvalidOperationException(
                    "Un créneau a été fourni mais aucun service de réservation de rendez-vous n'est configuré (IMP-15).");
            }

            await reserverRendezVous.ExecuterAsync(new ReserverRendezVousRequest(
                clientId, ticketId, creneau.Date, creneau.HeureDebut, creneau.Motif), ct).ConfigureAwait(false);
        }
    }
}
