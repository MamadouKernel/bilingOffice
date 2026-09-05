using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Un ticket tel que le superviseur a besoin de le voir pour agir : de quoi décider, pas
/// davantage.</summary>
public sealed record TicketSupervise(
    Guid TicketId,
    string Numero,
    string Module,
    StatutTicket Statut,
    Guid? OperateurAssigneId,
    string OperateurAssigneNom,
    Guid? PrioriteId,
    string PrioriteLibelle,
    DateTime DateArriveeEnFileUtc);

/// <summary>
/// Ce que le superviseur doit avoir sous les yeux pour agir sur une file : les tickets, et les
/// niveaux de priorité vers lesquels il peut les déplacer.
///
/// Les niveaux voyagent AVEC les tickets à dessein. Les faire venir de
/// <c>GererParametresSlaCommand</c> aurait exigé du superviseur une habilitation d'administration
/// des SLA qu'il n'a aucune raison d'avoir : <c>PRI-03</c> serait resté hors de portée pour un
/// superviseur ordinaire.
/// </summary>
public sealed record FileSupervisee(
    IReadOnlyList<TicketSupervise> Tickets,
    IReadOnlyList<(Guid Id, string Libelle)> NiveauxPriorite);

/// <summary>
/// SUP-04 — « Le superviseur peut : affecter un ticket non pris en charge, réaffecter, transférer,
/// équilibrer la charge, intervenir sur un ticket bloqué. »
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-04, <c>docs/89</c>) : l'écran de supervision n'injectait que
/// <c>VueFilesQuery</c> et <c>VueChargeQuery</c> — deux requêtes de comptage. Le superviseur voyait
/// combien de tickets attendaient, sans pouvoir en désigner un seul. Les commandes d'action
/// existaient, testées, et aucun écran ne les appelait.
///
/// Agir suppose de pouvoir nommer les tickets : c'est ce que cette requête apporte. Elle applique
/// le MÊME périmètre que <see cref="VueFilesQuery"/> (CLAR-05) — un superviseur sans périmètre
/// administré n'accède à rien, jamais à tout par défaut.
/// </summary>
public sealed class ListerTicketsDeLaFileQuery(
    ITicketRepository tickets,
    IPerimetreSupervisionRepository perimetres,
    IPrioriteRepository priorites,
    IUtilisateurInfoRepository utilisateurs,
    IAutorisationService autorisation)
{
    public async Task<FileSupervisee> ExecuterAsync(
        Guid superviseurId, Guid fileId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(superviseurId, ct).ConfigureAwait(false);
        if (perimetre is null || !perimetre.CouvreLaFile(fileId))
        {
            throw new AccesRefuseException(
                $"La file {fileId} ne figure pas dans le périmètre de supervision de {superviseurId}.");
        }

        var deLaFile = await tickets.ListerParFileAsync(fileId, ct).ConfigureAwait(false);
        var actifs = deLaFile
            .Where(t => t.Statut is not (StatutTicket.Cloture or StatutTicket.Annule))
            .OrderBy(t => t.DateArriveeEnFileUtc)
            .ToList();

        var niveaux = await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false);
        var libelles = niveaux.ToDictionary(n => n.Id, n => n.Libelle);

        var vues = new List<TicketSupervise>(actifs.Count);
        foreach (var t in actifs)
        {
            var nomOperateur = string.Empty;
            if (t.OperateurAssigneId is Guid operateurId)
            {
                var info = await utilisateurs.ObtenirInfoAsync(operateurId, ct).ConfigureAwait(false);
                nomOperateur = info?.NomAffiche ?? operateurId.ToString();
            }

            vues.Add(new TicketSupervise(
                t.Id,
                t.Numero,
                t.Module,
                t.Statut,
                t.OperateurAssigneId,
                nomOperateur,
                t.PrioriteId,
                t.PrioriteId is Guid p && libelles.TryGetValue(p, out var lib) ? lib : "—",
                t.DateArriveeEnFileUtc));
        }

        return new FileSupervisee(vues, [.. niveaux.Select(n => (n.Id, n.Libelle))]);
    }
}
