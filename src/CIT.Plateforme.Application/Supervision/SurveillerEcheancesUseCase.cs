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
    IRegleEscaladeRepository reglesEscalade,
    IPerimetreSupervisionRepository perimetres,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>
    /// SUP-01/SUP-02 — calcul du statut SLA d'un ticket, réutilisé tel quel par la supervision
    /// (<see cref="VueFilesQuery"/>) : aucune réimplémentation du calcul temps ouvré / attente
    /// client, seule <see cref="TraiterTicketAsync"/> ajoute par-dessus l'émission d'alertes et
    /// l'escalade. Retourne <c>null</c> si aucun SLA n'est configuré (PC-12).
    /// </summary>
    public async Task<StatutSla?> DeterminerStatutSlaAsync(Ticket ticket, CancellationToken ct = default)
    {
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

        return EcheanceSla.DeterminerStatut(dureeEffective, parametresSla);
    }

    public async Task<StatutSla?> TraiterTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null || ticket.Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            return null;
        }

        var statut = await DeterminerStatutSlaAsync(ticket, ct).ConfigureAwait(false);
        if (statut is null)
        {
            return null;
        }

        var maintenant = horloge.MaintenantUtc;

        var typeAlerte = statut switch
        {
            StatutSla.SeuilInformation => TypeAlerte.SlaSeuilInformation,
            StatutSla.SeuilAlerte => TypeAlerte.SlaSeuilAlerte,
            StatutSla.Depasse => TypeAlerte.SlaDepasse,
            StatutSla.Escalade => TypeAlerte.SlaEscalade,
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

        if (statut == StatutSla.Escalade)
        {
            await AppliquerEscaladeAsync(ticket, maintenant, ct).ConfigureAwait(false);
        }

        return statut;
    }

    /// <summary>
    /// ALE-03 — applique les règles d'escalade administrées. <b>Tant que le référentiel est vide,
    /// cette méthode ne fait rien</b> : le comportement reste celui d'avant, le dépassement est
    /// signalé sans être escaladé. C'est ce qui permet de livrer le mécanisme sans avoir inventé
    /// la politique d'escalade que le CDC ne donne pas.
    /// </summary>
    private async Task AppliquerEscaladeAsync(Ticket ticket, DateTime maintenant, CancellationToken ct)
    {
        var regles = await reglesEscalade.ListerAsync(ct).ConfigureAwait(false);
        var applicables = regles
            .Where(r => r.SApplique(ticket.Module, ticket.TypeDemande, ticket.FileCouranteId))
            .OrderBy(r => r.Ordre)
            .ToList();

        if (applicables.Count == 0)
        {
            return;
        }

        var modifie = false;

        foreach (var regle in applicables)
        {
            switch (regle.Action)
            {
                case ActionEscalade.NotifierSuperviseur:
                    // L'événement publié est lui aussi une écriture : sans le signaler, il ne
                    // serait jamais enregistré quand la notification est la SEULE action de la
                    // règle — le cas le plus courant.
                    modifie |= await NotifierSuperviseursAsync(ticket, ct).ConfigureAwait(false);
                    break;

                case ActionEscalade.RemonterPriorite when regle.PrioriteCibleId is Guid cible:
                    ticket.DefinirPriorite(cible);
                    modifie = true;
                    await journalisation.ConsignerAuditAsync(
                        ticket.OperateurAssigneId ?? Guid.Empty, "EscaladePrioriteRemontee", nameof(Ticket), ticket.Id,
                        null, $"règle {regle.Id} → priorité {cible}", ct).ConfigureAwait(false);
                    break;

                case ActionEscalade.Reaffecter:
                    ticket.RemettreEnDistribution(maintenant);
                    modifie = true;
                    await journalisation.ConsignerAuditAsync(
                        ticket.OperateurAssigneId ?? Guid.Empty, "EscaladeReaffectation", nameof(Ticket), ticket.Id,
                        null, $"règle {regle.Id} → ticket remis en distribution", ct).ConfigureAwait(false);
                    break;
            }
        }

        if (modifie)
        {
            await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// ALE-02 — les superviseurs destinataires sont ceux dont le PÉRIMÈTRE ADMINISTRÉ couvre la
    /// file du ticket. Le destinataire n'est donc jamais codé : en ajouter un est un acte
    /// d'administration, pas un développement — ce que le CDC exige.
    /// </summary>
    private async Task<bool> NotifierSuperviseursAsync(Ticket ticket, CancellationToken ct)
    {
        var tous = await perimetres.ListerAsync(ct).ConfigureAwait(false);
        var concernes = tous
            .Where(p => p.CouvreLaFile(ticket.FileCouranteId))
            .Select(p => p.SuperviseurId)
            .Distinct()
            .ToList();

        if (concernes.Count == 0)
        {
            // Aucun périmètre administré ne couvre cette file : rien n'est notifié, et surtout
            // aucun destinataire de repli n'est deviné.
            return false;
        }

        var destinataires = string.Join(",", concernes.Select(id => $"\"{id}\""));
        await journalisation.PublierEvenementAsync(
            TypeAlerte.SlaEscalade.ToString(),
            $"{{\"ticketId\":\"{ticket.Id}\",\"superviseurs\":[{destinataires}]}}",
            ct).ConfigureAwait(false);

        return true;
    }
}
