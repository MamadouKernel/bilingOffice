using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-18 — Constater la jointure et finaliser (IES). CDC : IMP-41, IMP-42. research.md D9 :
/// la jointure est une condition **dérivée** des deux <see cref="SuiviBrancheIes"/>, jamais un état
/// persisté séparément — jamais de transfert/clôture avant que les deux branches ne soient achevées.
/// La finalisation métier (facturation, paiement, documents) se déroule sur la plateforme IES
/// externe (hors périmètre, comme le lettrage IRIS du circuit Deposit — research.md D7) ; seule la
/// remise des documents est représentée ici par la clôture du ticket portail.
/// <para>
/// REMÉDIATION AUDIT (post-implementation-audit.md §8.2) : avant correction, cette commande
/// n'exigeait aucune habilitation — n'importe quel appelant, y compris <c>auteurId: null</c>,
/// pouvait clôturer n'importe quel ticket IES dès que les deux branches étaient achevées par
/// d'autres. Le contrat cite « Acteur : système, déclenché à l'achèvement de la seconde branche »,
/// mais aucun mécanisme d'appel système distinct n'est spécifié ailleurs (research.md, data-model.md)
/// — inventer une identité système dédiée serait une nouvelle règle métier non demandée. La
/// correction retenue reste donc dans le mécanisme d'habilitation déjà établi (D10) : l'appelant
/// doit être un agent réel, habilité sur **au moins une** des deux permissions de branche IES (celle
/// du Pilote Manifeste ou celle du Pilote Contrôle Documentaire — l'un ou l'autre a légitimement pu
/// achever la seconde branche et souhaiter constater la jointure), scopée à la file courante du
/// ticket. <c>auteurId</c> n'est plus nullable : aucun contournement anonyme.
/// </para>
/// </summary>
public sealed class ConstaterJointureIesEtFinaliserCommand(
    ITicketRepository tickets,
    ISuiviBrancheIesRepository suivisBranchesIes,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid auteurId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        var autoriseBrancheManifeste = await autorisation.EstAutoriseAsync(
            auteurId, Permission.BillingImportIesManifesteInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        var autoriseBrancheControleDocumentaire = await autorisation.EstAutoriseAsync(
            auteurId, Permission.BillingImportIesControleDocumentaireInstruire, ticket.FileCouranteId, ct).ConfigureAwait(false);
        if (!autoriseBrancheManifeste && !autoriseBrancheControleDocumentaire)
        {
            throw new AccesRefuseException(
                $"L'utilisateur {auteurId} ne détient aucune des permissions de branche IES requises pour finaliser le ticket {ticketId}.");
        }

        var suivis = await suivisBranchesIes.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
        if (suivis.Count == 0 || suivis.Any(s => !s.EstTerminee))
        {
            throw new JointureIesIncompleteException(ticketId);
        }

        var maintenant = horloge.MaintenantUtc;
        await journalisation.ConsignerHistoriqueAsync(ticketId, "JointureIesConstatee", null, auteurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(auteurId, "JointureIesConstatee", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        ticket.Cloturer(MotifClotureType.Normale, maintenant);

        await journalisation.ConsignerHistoriqueAsync(ticketId, "TicketIesCloture", null, auteurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(auteurId, "TicketIesCloture", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
