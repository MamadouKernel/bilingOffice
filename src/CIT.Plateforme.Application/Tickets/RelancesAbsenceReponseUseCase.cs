using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

/// <summary>Paramètres de relance — administrables sans développement (CYC-04), jamais fixés en
/// dur dans le code (Principe VI/XIII). Aucune valeur par défaut métier n'est imposée : ce type
/// porte uniquement la forme du paramètre.</summary>
public sealed record ParametresRelance(int NombreRelancesMax, TimeSpan DelaiEntreRelances);

/// <summary>
/// Contrat C-22 — Relancer le client et suggérer la clôture. CDC : CYC-04, CYC-05.
/// <para>
/// Règle métier critique « attente client » : logique applicative pure, testable indépendamment
/// de tout <c>BackgroundService</c> (règle 12) — le service hébergé (<c>RelancesAbsenceReponseService</c>,
/// Infrastructure) ne fait qu'appeler <see cref="TraiterAsync"/> à intervalle configurable.
/// </para>
/// </summary>
public sealed class RelancesAbsenceReponseUseCase(
    ITicketRepository tickets,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>Traite un ticket en attente client : émet une relance si le délai est écoulé, ou
    /// signale son éligibilité à une suggestion de clôture (jamais une clôture automatique —
    /// CYC-04) si le nombre maximal de relances est atteint sans réponse et sans état engageant
    /// (CYC-05).</summary>
    public async Task<bool> TraiterTicketAsync(Guid ticketId, ParametresRelance parametres, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null || ticket.Statut != StatutTicket.EnAttenteClient)
        {
            return false;
        }

        if (ticket.EstEligibleASuggestionDeCloture(parametres.NombreRelancesMax))
        {
            await journalisation.ConsignerHistoriqueAsync(ticket.Id, "SuggestionClotureAbsenceReponse", null, null, ct).ConfigureAwait(false);
            await journalisation.PublierEvenementAsync("SuggestionClotureAbsenceReponse", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
            await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
            return true;
        }

        ticket.EnregistrerRelance(horloge.MaintenantUtc);
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "RelanceEmise", $"numero={ticket.NombreRelancesEnvoyees}", null, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("RelanceEmise", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return true;
    }
}
