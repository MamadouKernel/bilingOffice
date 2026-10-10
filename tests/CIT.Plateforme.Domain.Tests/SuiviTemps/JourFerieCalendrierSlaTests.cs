using CIT.Plateforme.Domain.SuiviTemps;

namespace CIT.Plateforme.Domain.Tests.SuiviTemps;

/// <summary>SLA-04 — un jour férié déclaré est exclu du décompte des délais SLA.</summary>
public sealed class JourFerieCalendrierSlaTests
{
    private static ParametreSlaAdministrable Parametre8h17hLunVen()
    {
        var p = ParametreSlaAdministrable.Definir("Import", "Facturation", TimeSpan.FromHours(4), 0.5, 0.8);
        p.AppliquerCalendrierOuvre(
            new TimeOnly(8, 0), new TimeOnly(17, 0),
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]);
        return p;
    }

    [Fact]
    public void Un_jour_ferie_un_jour_ouvre_ne_compte_pas_dans_la_duree_ouvree()
    {
        // Arrange — mercredi 5 août 2026 déclaré férié.
        var ferie = new DateOnly(2026, 8, 5); // mercredi
        var calendrierAvec = Parametre8h17hLunVen().ConstruireCalendrierOuvre(new HashSet<DateOnly> { ferie });
        var calendrierSans = Parametre8h17hLunVen().ConstruireCalendrierOuvre(new HashSet<DateOnly>());

        // Du mardi 8h au jeudi 17h (UTC+0, Abidjan).
        var debut = new DateTime(2026, 8, 4, 8, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2026, 8, 6, 17, 0, 0, DateTimeKind.Utc);

        // Act
        var avecFerie = calendrierAvec.DureeOuvreeEntre(debut, fin);
        var sansFerie = calendrierSans.DureeOuvreeEntre(debut, fin);

        // Assert — le mercredi (9 h ouvrées) disparaît quand il est férié.
        Assert.Equal(TimeSpan.FromHours(27), sansFerie); // mar + mer + jeu = 3 × 9 h
        Assert.Equal(TimeSpan.FromHours(18), avecFerie);  // mar + jeu = 2 × 9 h
        Assert.False(calendrierAvec.EstJourOuvre(ferie));
    }

    [Fact]
    public void Calendrier_toujours_ouvert_exclut_aussi_un_ferie()
    {
        var ferie = new DateOnly(2026, 8, 5);
        // Un paramètre sans calendrier administré = « toujours ouvert » (24/7).
        var p = ParametreSlaAdministrable.Definir("Import", "Facturation", TimeSpan.FromHours(4), 0.5, 0.8);
        var calendrier = p.ConstruireCalendrierOuvre(new HashSet<DateOnly> { ferie });

        Assert.False(calendrier.EstJourOuvre(ferie));
        Assert.True(calendrier.EstJourOuvre(ferie.AddDays(1)));
    }
}
