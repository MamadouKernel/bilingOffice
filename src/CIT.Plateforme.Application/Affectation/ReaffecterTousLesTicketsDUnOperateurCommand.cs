using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Affectation;

public sealed record ResultatReaffectationDeMasse(Guid TicketId, bool Succes, string? Erreur);

/// <summary>
/// Contrat DIS-02 — « Le superviseur peut […] identifier les tickets encore affectés à un
/// opérateur absent, réaffecter ces tickets en masse […]. »
/// <para>
/// Ne réimplémente pas la réaffectation : chaque ticket passe par
/// <see cref="ReaffecterTicketCommand"/>, qui applique déjà le contrôle TRF-04 (l'opérateur
/// cible doit être habilité sur la file du ticket). Comme <see cref="Supervision.ActionDeMasseCommand"/>,
/// chaque ticket de la sélection est traité individuellement et le résultat rend le détail des
/// succès et des échecs plutôt qu'un verdict global — un opérateur cible non habilité sur une
/// seule des files concernées ne doit pas faire échouer les réaffectations valides des autres.
/// </para>
/// </summary>
public sealed class ReaffecterTousLesTicketsDUnOperateurCommand(
    ITicketRepository tickets,
    IPerimetreSupervisionRepository perimetres,
    IAutorisationService autorisation,
    ReaffecterTicketCommand reaffecterTicket,
    IJournalisationService journalisation)
{
    public async Task<IReadOnlyList<ResultatReaffectationDeMasse>> ExecuterAsync(
        Guid initiateurId, Guid operateurAbsentId, Guid operateurCibleId, CancellationToken ct = default)
    {
        // DIS-02 : « selon ses habilitations » — la même habilitation que celle qui autorise déjà
        // un superviseur à changer la disponibilité d'un tiers gouverne le déclenchement en masse.
        await autorisation.ExigerAsync(initiateurId, Permission.DisponibiliteGerer, ct: ct).ConfigureAwait(false);

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(initiateurId, ct).ConfigureAwait(false);

        var ticketsDeLOperateur = await tickets.ListerParOperateurAsync(operateurAbsentId, ct).ConfigureAwait(false);
        var ticketsActifsDuPerimetre = ticketsDeLOperateur
            .Where(t => t.Statut is not (StatutTicket.Cloture or StatutTicket.Annule))
            .Where(t => perimetre is not null && perimetre.CouvreLaFile(t.FileCouranteId))
            .ToList();

        var resultats = new List<ResultatReaffectationDeMasse>();
        foreach (var ticket in ticketsActifsDuPerimetre)
        {
            try
            {
                await reaffecterTicket.ExecuterAsync(initiateurId, ticket.Id, operateurCibleId, ct).ConfigureAwait(false);
                resultats.Add(new ResultatReaffectationDeMasse(ticket.Id, Succes: true, Erreur: null));
            }
            catch (Exception ex)
            {
                // Traçage individuel de l'échec, à l'image de MAS-02 pour les autres actions de
                // masse : un échec partiel doit se lire ticket par ticket, pas se deviner.
                await journalisation.ConsignerAuditAsync(
                    initiateurId, "ReaffectationDeMasseEchec", nameof(Ticket), ticket.Id,
                    operateurAbsentId.ToString(), $"échec: {ex.Message}", ct).ConfigureAwait(false);
                resultats.Add(new ResultatReaffectationDeMasse(ticket.Id, Succes: false, Erreur: ex.Message));
            }
        }

        return resultats;
    }
}
