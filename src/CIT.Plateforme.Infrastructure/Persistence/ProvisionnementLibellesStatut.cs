using CIT.Plateforme.Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// §11.4 / §9.10 — garantit qu'un libellé existe pour chaque statut du cycle de vie.
///
/// STRICTEMENT ADDITIF. Un libellé déjà présent n'est <b>jamais</b> réécrit : il a pu être modifié
/// par l'administrateur fonctionnel, et c'est précisément ce que le CDC demande de rendre
/// possible. Un approvisionnement qui « remettrait les valeurs par défaut » à chaque démarrage
/// annulerait silencieusement son paramétrage — le même piège que celui rencontré sur les mots de
/// passe de l'amorçage.
///
/// Les libellés initiaux reprennent ceux du CDC §11.4. Ce sont des valeurs de départ, non des
/// constantes : leur modification est le comportement attendu.
/// </summary>
public sealed class ProvisionnementLibellesStatut(
    PlateformeDbContext contexte,
    ILogger<ProvisionnementLibellesStatut> journal)
{
    /// <summary>
    /// Libellés de départ, repris du référentiel du CDC §11.4.
    /// <c>EstEngageant</c> suit CYC-05 : un statut engageant interdit la suggestion de clôture
    /// pour absence de réponse.
    /// </summary>
    private static readonly (StatutTicket Statut, string Fr, string En, int Ordre, bool Engageant)[] Initiaux =
    [
        (StatutTicket.EnAttenteDePriseEnCharge, "En attente de prise en charge", "Awaiting assignment", 10, false),
        (StatutTicket.EnCours,                  "En cours de traitement",        "In progress",         20, false),
        (StatutTicket.EnAttenteClient,          "En attente client",             "Awaiting customer",   30, false),
        (StatutTicket.Cloture,                  "Clôturé",                       "Closed",              40, true),
        (StatutTicket.Annule,                   "Annulé",                        "Cancelled",           50, true),
    ];

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        var codesPresents = await contexte.LibellesStatutTicket
            .Select(l => l.Code)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var manquants = Initiaux
            .Where(i => !codesPresents.Contains(i.Statut.ToString()))
            .ToList();

        if (manquants.Count == 0)
        {
            return;
        }

        foreach (var (statut, fr, en, ordre, engageant) in manquants)
        {
            contexte.LibellesStatutTicket.Add(
                LibelleStatutTicket.Creer(statut.ToString(), fr, en, ordre, engageant));
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);

        journal.LogInformation(
            "Référentiel des libellés de statut complété : {Nombre} libellé(s) ajouté(s). "
            + "Les libellés déjà présents ont été laissés intacts.",
            manquants.Count);
    }
}
