using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>
/// WKF-02/WKF-03 : le circuit n'est jamais rigide — un utilisateur habilité peut déroger au
/// circuit nominal (matrice des transitions) lorsque la situation opérationnelle l'exige. À la
/// différence de <see cref="TransfererTicketCommand"/>, cette dérogation NE vérifie PAS la
/// matrice — c'est précisément sa raison d'être — mais exige une permission distincte et plus
/// forte, et trace explicitement qu'il s'agit d'une dérogation (jamais silencieuse).
/// </summary>
public sealed class DerogationWorkflowService(
    ITicketRepository tickets,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public const string PermissionDeroger = "workflow.deroger";

    public async Task DerogerTransfertAsync(Guid initiateurId, Guid ticketId, Guid fileCibleId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de dérogation est obligatoire (WKF-03).", nameof(motif));
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");

        await autorisation.ExigerAsync(initiateurId, PermissionDeroger, ticket.FileCouranteId, ct).ConfigureAwait(false);

        var fileSource = ticket.FileCouranteId;
        ticket.Transferer(fileCibleId, destinataireId: null, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(
            ticket.Id, "DerogationCircuitNominal", $"de={fileSource};vers={fileCibleId};motif={motif}", initiateurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(
            initiateurId, "DerogationCircuitNominal", nameof(Ticket), ticket.Id, fileSource.ToString(), fileCibleId.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
