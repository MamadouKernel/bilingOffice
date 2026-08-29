using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Affectation;

/// <summary>Port fournissant les candidats éligibles (habilités sur la file) à la distribution
/// automatique — la notion d'habilitation appartient à Infrastructure/Identity.</summary>
public interface ICandidatsAffectationProvider
{
    Task<IReadOnlyList<CandidatAffectation>> ObtenirCandidatsAsync(Guid fileId, CancellationToken ct = default);
}

/// <summary>
/// Contrat C-23 — Distribuer automatiquement les tickets d'une file. CDC : AFF-01..07, DIS-01.
/// Logique applicative pure, testable indépendamment de tout <c>BackgroundService</c> (règle 12).
/// </summary>
public sealed class DistributionAutomatiqueUseCase(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    ICandidatsAffectationProvider candidatsProvider,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<bool> TraiterTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null || ticket.Statut != StatutTicket.EnAttenteDePriseEnCharge)
        {
            return false;
        }

        var file = await files.ObtenirParIdAsync(ticket.FileCouranteId, ct).ConfigureAwait(false);
        var strategie = ChoisirStrategie(file);
        if (strategie is null)
        {
            return false; // AFF-01 (prise libre) ou AFF-02 (manuelle) : pas de distribution auto.
        }

        var candidats = await candidatsProvider.ObtenirCandidatsAsync(ticket.FileCouranteId, ct).ConfigureAwait(false);
        var operateurChoisi = strategie.Choisir(candidats);
        if (operateurChoisi is not Guid operateurId)
        {
            return false; // Aucun candidat disponible (DIS-01) — le ticket reste en attente.
        }

        ticket.PrendreEnCharge(operateurId, horloge.MaintenantUtc);

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketAffecteAutomatiquement", $"operateur={operateurId}", null, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(null, "TicketAffecteAutomatiquement", nameof(Ticket), ticket.Id, null, operateurId.ToString(), ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketAffecte", $"{{\"ticketId\":\"{ticket.Id}\",\"operateurId\":\"{operateurId}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return true;
    }

    // REMÉDIATION AUDIT (post-implementation-audit.md §3.C) : AFF-06 (compétence) est désormais
    // câblée — la compétence requise est celle administrée sur la file elle-même
    // (FileDeTraitement.CompetenceRequise), jamais une valeur inventée ici. Sans compétence
    // administrée pour une file en stratégie Compétence, aucune distribution n'a lieu (comme pour
    // PriseLibre/AffectationManuelle) plutôt que de deviner un candidat.
    private static IStrategieAffectation? ChoisirStrategie(FileDeTraitement? file) => file?.StrategieActive switch
    {
        StrategieAffectation.RoundRobin => new RoundRobinStrategy(),
        StrategieAffectation.Charge => new ChargeStrategy(),
        StrategieAffectation.Disponibilite => new DisponibiliteStrategy(),
        StrategieAffectation.Competence when !string.IsNullOrWhiteSpace(file!.CompetenceRequise) => new CompetenceStrategy(file.CompetenceRequise),
        _ => null,
    };
}
