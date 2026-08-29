using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Port de configuration SLA (SLA-01, SLA-05) — paramétrable par module/type/priorité ;
/// retourne <c>null</c> si aucun SLA n'est configuré pour ce ticket (PC-12 non résolu : aucune
/// valeur n'est inventée en son absence).</summary>
public interface IParametresSlaProvider
{
    Task<(ParametresSla Parametres, CalendrierOuvre Calendrier)?> ObtenirPourAsync(Ticket ticket, CancellationToken ct = default);
}

/// <summary>
/// Contrat C-21 — Surveiller les échéances SLA et émettre les alertes. CDC : SLA-01..05,
/// ALE-01..03, TPS-02/04. Logique applicative pure, testable indépendamment de tout
/// <c>BackgroundService</c> (règle 12).
/// </summary>
public sealed class SurveillerEcheancesUseCase(
    ITicketRepository tickets,
    IPeriodeAttenteClientRepository periodesAttenteClient,
    IAlerteRepository alertes,
    IParametresSlaProvider parametresProvider,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<StatutSla?> TraiterTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null || ticket.Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            return null;
        }

        var configuration = await parametresProvider.ObtenirPourAsync(ticket, ct).ConfigureAwait(false);
        if (configuration is null)
        {
            return null; // PC-12 : aucun SLA configuré — rien à surveiller, rien à inventer.
        }

        var (parametresSla, calendrier) = configuration.Value;
        var maintenant = horloge.MaintenantUtc;

        // REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : la durée SLA écoulée est
        // désormais calculée en soustrayant des durées ouvrées à des durées ouvrées — jamais une
        // durée brute à une durée ouvrée (TPS-04, SLA-03). Chaque période d'attente client
        // (PeriodeAttenteClient) est convertie en temps ouvré avec le MÊME calendrier que la
        // durée totale, avant d'être soustraite, ce qui reste correct même si une période
        // chevauche une plage non ouvrée (nuit, week-end, jour férié).
        var dureeTotaleOuvree = calendrier.DureeOuvreeEntre(ticket.DateArriveeEnFileUtc, maintenant);

        var periodes = await periodesAttenteClient.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false);
        var dureeAttenteClienteOuvree = TimeSpan.Zero;
        foreach (var periode in periodes)
        {
            var finPeriode = periode.FinUtc ?? maintenant;
            dureeAttenteClienteOuvree += calendrier.DureeOuvreeEntre(periode.DebutUtc, finPeriode);
        }

        var dureeEffective = dureeTotaleOuvree - dureeAttenteClienteOuvree;
        if (dureeEffective < TimeSpan.Zero)
        {
            dureeEffective = TimeSpan.Zero;
        }

        var statut = EcheanceSla.DeterminerStatut(dureeEffective, parametresSla);

        var typeAlerte = statut switch
        {
            StatutSla.SeuilInformation => TypeAlerte.SlaSeuilInformation,
            StatutSla.SeuilAlerte => TypeAlerte.SlaSeuilAlerte,
            StatutSla.Depasse => TypeAlerte.SlaDepasse,
            _ => (TypeAlerte?)null,
        };

        if (typeAlerte is TypeAlerte type)
        {
            var dejaAlerte = await alertes.ExisteDejaAsync(ticket.Id, type, ct).ConfigureAwait(false);
            if (!dejaAlerte)
            {
                var alerte = Alerte.Creer(type, ticket.Id, ticket.FileCouranteId, maintenant);
                await alertes.AjouterAsync(alerte, ct).ConfigureAwait(false);
                await journalisation.PublierEvenementAsync(type.ToString(), $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
                await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
            }
        }

        return statut;
    }
}
