using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Supervision;

/// <summary>Contrat C-24 — Consulter la supervision temps réel (vue par file). CDC : SUP-01,
/// SUP-02. Filtrée par le périmètre du superviseur (CLAR-05).</summary>
/// <summary>Le nom de la file accompagne son identifiant : l'écran de supervision présente un
/// libellé métier, l'identifiant technique n'étant conservé que pour la navigation.
/// SUP-02 : ancienneté du ticket le plus ancien et délai moyen sont calculés sur les tickets
/// actifs (ni clôturés ni annulés) — un backlog vide rend les deux <c>null</c> plutôt qu'un
/// zéro qui laisserait croire à un traitement instantané. ProcheDepassementSla et
/// EnDepassementSla réutilisent le moteur SLA existant (SurveillerEcheancesUseCase /
/// EcheanceSla) : aucun seuil n'est réinventé ici.</summary>
public sealed record VueFile(
    Guid FileId,
    int NonAffectes,
    int EnCours,
    int EnAttenteClient,
    int TotalActifs,
    string Nom = "",
    int ProcheDepassementSla = 0,
    int EnDepassementSla = 0,
    int SansActivite = 0,
    TimeSpan? AncienneteTicketLePlusAncien = null,
    TimeSpan? DelaiMoyenTraitement = null);

public sealed class VueFilesQuery(
    IFileDeTraitementRepository files,
    ITicketRepository tickets,
    IPerimetreSupervisionRepository perimetres,
    IAutorisationService autorisation,
    SurveillerEcheancesUseCase surveillerEcheances,
    IHorloge horloge,
    TimeSpan? seuilSansActivite = null)
{
    /// <summary>SUP-01 : « sans activité depuis une durée définie ». Cette durée est paramétrable
    /// par appel (tests, futur écran d'administration) ; à défaut, 24h — une valeur de repli
    /// documentée plutôt qu'une constante muette.</summary>
    private readonly TimeSpan _seuilSansActivite = seuilSansActivite ?? TimeSpan.FromHours(24);

    public async Task<IReadOnlyList<VueFile>> ExecuterAsync(Guid superviseurId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(superviseurId, Permission.SupervisionConsulter, ct: ct).ConfigureAwait(false);

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(superviseurId, ct).ConfigureAwait(false);
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        // CLAR-05 : périmètre borné et configurable — un superviseur sans périmètre défini n'a
        // accès à rien (par défaut restrictif, jamais un accès global implicite).
        var filesDuPerimetre = toutesLesFiles.Where(f => perimetre is not null && perimetre.CouvreLaFile(f.Id));

        var maintenant = horloge.MaintenantUtc;
        var vues = new List<VueFile>();
        foreach (var file in filesDuPerimetre)
        {
            var ticketsDeLaFile = await tickets.ListerParFileAsync(file.Id, ct).ConfigureAwait(false);
            var actifs = ticketsDeLaFile.Where(t => t.Statut is not (StatutTicket.Cloture or StatutTicket.Annule)).ToList();

            // Anti-N+1 : statut SLA de tous les tickets actifs calculé en lot (paramètres + périodes
            // pré-chargés), au lieu de ~3 requêtes par ticket. Même calcul que la variante unitaire.
            var statutsSla = await surveillerEcheances.DeterminerStatutsSlaAsync(actifs, ct).ConfigureAwait(false);
            var procheDepassement = actifs.Count(t =>
                statutsSla.GetValueOrDefault(t.Id) is StatutSla.SeuilInformation or StatutSla.SeuilAlerte);
            var enDepassement = actifs.Count(t =>
                statutsSla.GetValueOrDefault(t.Id) is StatutSla.Depasse or StatutSla.Escalade);

            var sansActivite = actifs.Count(t => maintenant - t.DateDerniereActiviteUtc >= _seuilSansActivite);

            TimeSpan? ancienneteTicketLePlusAncien = actifs.Count == 0
                ? null
                : maintenant - actifs.Min(t => t.DateArriveeEnFileUtc);

            TimeSpan? delaiMoyenTraitement = actifs.Count == 0
                ? null
                : TimeSpan.FromTicks((long)actifs.Average(t => (maintenant - t.DateArriveeEnFileUtc).Ticks));

            vues.Add(new VueFile(
                file.Id,
                NonAffectes: ticketsDeLaFile.Count(t => t.Statut == StatutTicket.EnAttenteDePriseEnCharge),
                EnCours: ticketsDeLaFile.Count(t => t.Statut == StatutTicket.EnCours),
                EnAttenteClient: ticketsDeLaFile.Count(t => t.Statut == StatutTicket.EnAttenteClient),
                TotalActifs: actifs.Count,
                Nom: file.Nom,
                ProcheDepassementSla: procheDepassement,
                EnDepassementSla: enDepassement,
                SansActivite: sansActivite,
                AncienneteTicketLePlusAncien: ancienneteTicketLePlusAncien,
                DelaiMoyenTraitement: delaiMoyenTraitement));
        }

        return vues;
    }
}
