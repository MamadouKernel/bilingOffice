using System;
using System.Collections.Generic;
using CIT.Plateforme.Domain.SuiviTemps;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.SuiviTemps;

/// <summary>
/// FEATURE 011 — CERTIFICATION NFR-11 · suite TEST-SPEC « NFR-CERT-03 » (T061→T066).
///
/// Texte CDC (NFR-11, Must) : « Horaires de traitement : les services CIT conservent leurs
/// horaires de fonctionnement propres, utilisés notamment dans le calcul des SLA. Un client
/// peut déposer un dossier hors heures ouvrées ; le SLA de traitement démarre selon les règles
/// de la file concernée. »
///
/// Cette suite certifie :
///   - T062 (cas décisif) : un dépôt hors heures ouvrées (ex: nuit ou week-end) n'impute aucun
///     temps ouvré SLA tant que les heures ouvrées ne sont pas ouvertes ;
///   - T063 : frontières de début et de fin de journée ouvrée ;
///   - T064 : jours non ouvrés et jours fériés exclus du décompte ;
///   - T065 : mode « ToujoursOuvert » comptabilise le temps calendaire sans inventer d'horaires.
///
/// CONTRÔLE (I8/I9) : aucun calendrier national imposé en dur, aucun service externe requis.
/// </summary>
public sealed class NfrCert03HorairesTests
{
    private static readonly CalendrierOuvre CalendrierStandard = new(
        HeureDebut: new TimeOnly(8, 0),
        HeureFin: new TimeOnly(17, 0),
        JoursOuvres: new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
        JoursFeries: new HashSet<DateOnly> { new DateOnly(2026, 1, 1) }); // 1er Janvier 2026

    // ---- T062 (Cas décisif CDC NFR-11) : dépôt hors heures ouvrées ----

    [Fact]
    public void Depot_hors_heures_ouvrees_ne_consomme_aucun_temps_sla_avant_l_ouverture()
    {
        // Dépôt effectué un dimanche à 22h00 UTC (hors heures ouvrées)
        var depotDimancheNuit = new DateTime(2026, 8, 30, 22, 0, 0, DateTimeKind.Utc); // Dimanche 30 Août 2026

        // Constat à 06h00 le lundi matin (toujours avant ouverture à 08h00)
        var lundiMatinAvantOuverture = new DateTime(2026, 8, 31, 6, 0, 0, DateTimeKind.Utc);

        var tempsOuvreEcoule = CalendrierStandard.DureeOuvreeEntre(depotDimancheNuit, lundiMatinAvantOuverture);

        Assert.Equal(TimeSpan.Zero, tempsOuvreEcoule);

        // Constat à 09h00 le lundi matin (1h après ouverture à 08h00)
        var lundiApresOuverture = new DateTime(2026, 8, 31, 9, 0, 0, DateTimeKind.Utc);

        var tempsApres1h = CalendrierStandard.DureeOuvreeEntre(depotDimancheNuit, lundiApresOuverture);

        // Seule l'heure entre 08h00 et 09h00 est imputée au SLA
        Assert.Equal(TimeSpan.FromHours(1), tempsApres1h);
    }

    // ---- T063 : frontière de début et fin de journée ouvrée ----

    [Fact]
    public void Temps_ecoule_hors_plage_journaliere_est_exclu()
    {
        // Période couvrant une soirée entière : Lundi 17h00 → Mardi 08h00 (15h de nuit)
        var lundiFermeture = new DateTime(2026, 8, 31, 17, 0, 0, DateTimeKind.Utc);
        var mardiOuverture = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        var dureeNuit = CalendrierStandard.DureeOuvreeEntre(lundiFermeture, mardiOuverture);

        Assert.Equal(TimeSpan.Zero, dureeNuit);
    }

    // ---- T064 : jour férié configuré est exclu du décompte ----

    [Fact]
    public void Jour_ferie_parametre_est_exclu_du_calcul_sla()
    {
        // Jeudi 1er Janvier 2026 (férié configuré dans CalendrierStandard) de 08h00 à 17h00
        var debutFerie = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var finFerie = new DateTime(2026, 1, 1, 17, 0, 0, DateTimeKind.Utc);

        var dureeFerie = CalendrierStandard.DureeOuvreeEntre(debutFerie, finFerie);

        Assert.Equal(TimeSpan.Zero, dureeFerie);
    }

    // ---- T065 : mode ToujoursOuvert applique un calcul 24/7 ----

    [Fact]
    public void Mode_toujours_ouvert_calcule_la_duree_calendaire_sans_restriction()
    {
        var debut = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2026, 9, 1, 12, 30, 0, DateTimeKind.Utc);

        var duree = CalendrierOuvre.ToujoursOuvert.DureeOuvreeEntre(debut, fin);

        Assert.Equal(TimeSpan.FromMinutes(750), duree);
    }
}
