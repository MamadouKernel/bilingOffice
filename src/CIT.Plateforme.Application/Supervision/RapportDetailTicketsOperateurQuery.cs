using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Détail d'un ticket traité par un opérateur, avec les temps mesurés.</summary>
public sealed record LigneTicketTraiteVue(
    Guid TicketId,
    string NumeroTicket,
    string Module,
    string? Objet,
    StatutTicket Statut,
    DateTime DateArriveeUtc,
    DateTime? DatePriseEnChargeUtc,
    DateTime? DateSortieUtc,
    TimeSpan TempsAttente,
    TimeSpan TempsTraitement);

/// <summary>
/// §9.9.3 (détail) — pour un opérateur donné, la LISTE des tickets qu'il a pris en charge sur une
/// période, avec le temps passé sur chacun (attente puis traitement). Complète le rapport agrégé
/// <see cref="RapportActiviteOperateurQuery"/> par le détail « ticket par ticket » demandé par la
/// supervision. BORNÉ AU PÉRIMÈTRE du superviseur (SEC-02/03), <see cref="Permission.SupervisionConsulter"/>.
/// Lecture seule, finalité pilotage (non notation individuelle).
/// </summary>
public sealed class RapportDetailTicketsOperateurQuery(
    IPassageEnFileRepository passages,
    IFileDeTraitementRepository files,
    IPerimetreSupervisionRepository perimetres,
    ITicketRepository tickets,
    IHorloge horloge,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<LigneTicketTraiteVue>> ExecuterAsync(
        Guid superviseurId, Guid operateurId, DateTime debutUtc, DateTime finUtc, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(superviseurId, ct).ConfigureAwait(false);
        if (perimetre is null)
        {
            return [];
        }

        var filesDuPerimetre = (await files.ListerAsync(ct).ConfigureAwait(false))
            .Where(f => perimetre.CouvreLaFile(f.Id))
            .Select(f => f.Id)
            .ToList();
        if (filesDuPerimetre.Count == 0)
        {
            return [];
        }

        // On repart des mêmes passages que le rapport agrégé, puis on garde ceux pris en charge par
        // l'opérateur demandé. Le temps de traitement est mesuré à la sortie (ou à maintenant si le
        // ticket est encore en cours).
        var passagesOperateur = (await passages.ListerParFilesEtPeriodeAsync(filesDuPerimetre, debutUtc, finUtc, ct).ConfigureAwait(false))
            .Where(p => p.OperateurId == operateurId && p.DatePriseEnChargeUtc is not null)
            .OrderByDescending(p => p.DatePriseEnChargeUtc)
            .ToList();
        if (passagesOperateur.Count == 0)
        {
            return [];
        }

        var maintenant = horloge.MaintenantUtc;
        var lignes = new List<LigneTicketTraiteVue>(passagesOperateur.Count);
        foreach (var p in passagesOperateur)
        {
            // ponytail: un chargement de ticket par passage — volume borné (tickets d'un agent sur
            // une période) ; passer à un chargement par lot si le volume le justifie.
            var ticket = await tickets.ObtenirParIdAsync(p.TicketId, ct).ConfigureAwait(false);
            if (ticket is null)
            {
                continue;
            }

            lignes.Add(new LigneTicketTraiteVue(
                ticket.Id,
                ticket.Numero,
                ticket.Module,
                ticket.Objet,
                ticket.Statut,
                p.DateArriveeUtc,
                p.DatePriseEnChargeUtc,
                p.DateSortieUtc,
                p.TempsAttente(maintenant),
                p.TempsTraitement(maintenant)));
        }

        return lignes;
    }
}
