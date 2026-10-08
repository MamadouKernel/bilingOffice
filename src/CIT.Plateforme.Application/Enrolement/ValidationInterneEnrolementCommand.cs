using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// Validation interne « 4-yeux » (07/10/2026) — un VALIDATEUR de société approuve ou rejette un
/// dossier d'enrôlement préparé par un soumissionnaire. Séparation des devoirs STRICTE : le
/// validateur ne peut jamais valider/rejeter ce qu'il a préparé lui-même. À l'approbation, les
/// tickets sont libérés et deviennent visibles de l'Agent Gate (alerte émise) ; au rejet, ils sont
/// annulés avec motif.
/// </summary>
public sealed class ValidationInterneEnrolementCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IUtilisateurSocieteRepository rattachements,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ApprouverAsync(Guid validateurId, Guid dossierId, CancellationToken ct = default)
    {
        var parques = await ChargerParquesAsync(validateurId, dossierId, ct).ConfigureAwait(false);

        foreach (var ticket in parques)
        {
            ticket.LibererValidationInterne();
            await journalisation.ConsignerHistoriqueAsync(ticket.Id, "EnrolementValideEnInterne", "Validé en interne (4-yeux) — transmis à l'Agent Gate", validateurId, ct).ConfigureAwait(false);
            await journalisation.ConsignerAuditAsync(validateurId, "EnrolementValideEnInterne", nameof(Ticket), ticket.Id, null, null, ct).ConfigureAwait(false);
            await journalisation.PublierEvenementAsync("TicketArriveeEnFile", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task RejeterAsync(Guid validateurId, Guid dossierId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif du rejet est obligatoire.", nameof(motif));
        }

        var parques = await ChargerParquesAsync(validateurId, dossierId, ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        foreach (var ticket in parques)
        {
            ticket.Annuler(maintenant);
            var detail = $"Rejeté en interne (4-yeux) : {motif.Trim()}";
            await journalisation.ConsignerHistoriqueAsync(ticket.Id, "EnrolementRejeteEnInterne", detail, validateurId, ct).ConfigureAwait(false);
            await journalisation.ConsignerAuditAsync(validateurId, "EnrolementRejeteEnInterne", nameof(Ticket), ticket.Id, null, detail, ct).ConfigureAwait(false);
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Contrôles communs (validateur habilité, dossier de sa société, tickets parqués, SoD).</summary>
    private async Task<IReadOnlyList<Ticket>> ChargerParquesAsync(Guid validateurId, Guid dossierId, CancellationToken ct)
    {
        var rattachement = await rattachements.ObtenirParUtilisateurAsync(validateurId, ct).ConfigureAwait(false);
        if (rattachement is null || !rattachement.EstActif || !rattachement.DroitValidation)
        {
            throw new AccesRefuseException($"L'utilisateur {validateurId} n'est pas validateur d'une société (droit requis).");
        }

        var dossier = await dossiers.ObtenirParIdAsync(dossierId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Dossier {dossierId} introuvable.");
        if (dossier.ClientId != rattachement.SocieteId)
        {
            throw new AccesRefuseException($"Le dossier {dossierId} ne relève pas de la société du validateur.");
        }

        var parques = (await tickets.ListerParDossierAsync(dossierId, ct).ConfigureAwait(false))
            .Where(t => t.EnAttenteValidationInterneParId is not null)
            .ToList();
        if (parques.Count == 0)
        {
            throw new InvalidOperationException("Aucune demande en attente de validation interne pour ce dossier.");
        }

        // Séparation des devoirs STRICTE : on ne valide/rejette jamais sa propre préparation.
        if (parques.Any(t => t.EnAttenteValidationInterneParId == validateurId))
        {
            throw new AccesRefuseException("Un validateur ne peut pas valider ses propres soumissions (séparation des devoirs).");
        }

        return parques;
    }
}
