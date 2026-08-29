using CIT.Plateforme.Domain.SuiviTemps;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.SuiviTemps;

/// <summary>[TEST-TECH] Decision: D7 — moteur SLA/temps pur, sans dépendance infrastructure,
/// unitairement testable.</summary>
public sealed class MoteurSlaTests
{
    [Theory]
    [InlineData(40, StatutSla.EnCours)]      // 40% < seuil info 50%
    [InlineData(60, StatutSla.SeuilInformation)] // 60% >= 50% et < 80%
    [InlineData(85, StatutSla.SeuilAlerte)]  // 85% >= 80% et < 100%
    [InlineData(100, StatutSla.Depasse)]     // 100% >= cible
    [InlineData(150, StatutSla.Depasse)]
    public void DeterminerStatut_applique_les_seuils_50_80_depassement(int pourcentEcoule, StatutSla attendu)
    {
        var parametres = new ParametresSla(DureeCible: TimeSpan.FromHours(10), SeuilInformation: 0.5, SeuilAlerte: 0.8);
        var dureeEcoulee = TimeSpan.FromHours(10 * pourcentEcoule / 100.0);

        Assert.Equal(attendu, EcheanceSla.DeterminerStatut(dureeEcoulee, parametres));
    }

    [Fact]
    public void CalendrierOuvre_ne_compte_que_les_heures_dans_la_plage_configuree()
    {
        var calendrier = new CalendrierOuvre(
            HeureDebut: new TimeOnly(8, 0),
            HeureFin: new TimeOnly(17, 0),
            JoursOuvres: new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            JoursFeries: new HashSet<DateOnly>());

        // Lundi 06/01/2026 08h00 -> Lundi 06/01/2026 20h00 = seulement 08h00->17h00 compte (9h ouvrées)
        var debut = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc); // lundi
        var fin = new DateTime(2026, 1, 5, 20, 0, 0, DateTimeKind.Utc);

        var duree = calendrier.DureeOuvreeEntre(debut, fin);

        Assert.Equal(TimeSpan.FromHours(9), duree);
    }

    [Fact]
    public void CalendrierOuvre_exclut_les_jours_non_ouvres_et_feries()
    {
        var jourFerie = new DateOnly(2026, 1, 6);
        var calendrier = new CalendrierOuvre(
            HeureDebut: new TimeOnly(8, 0),
            HeureFin: new TimeOnly(17, 0),
            JoursOuvres: new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            JoursFeries: new HashSet<DateOnly> { jourFerie });

        // Lundi 05/01 08h -> Mercredi 07/01 17h : mardi 06/01 est férié (exclu), samedi/dimanche n'existent pas ici.
        var debut = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2026, 1, 7, 17, 0, 0, DateTimeKind.Utc);

        var duree = calendrier.DureeOuvreeEntre(debut, fin);

        // Lundi (9h) + mardi férié (0h) + mercredi (9h) = 18h
        Assert.Equal(TimeSpan.FromHours(18), duree);
    }

    [Fact]
    public void CalendrierToujoursOuvert_compte_la_duree_brute_integrale()
    {
        var debut = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc);
        var fin = debut.AddHours(30);

        var duree = CalendrierOuvre.ToujoursOuvert.DureeOuvreeEntre(debut, fin);

        Assert.Equal(TimeSpan.FromHours(30), duree);
    }
}
