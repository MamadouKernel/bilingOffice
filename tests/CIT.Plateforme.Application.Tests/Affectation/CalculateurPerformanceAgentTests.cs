using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Domain.Affectation;

namespace CIT.Plateforme.Application.Tests.Affectation;

public sealed class CalculateurPerformanceAgentTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Compte_les_indisponibilites_et_leur_duree_dans_la_fenetre()
    {
        // Arrange — disponible, 1h30 après passe indisponible 30 min, puis redevient disponible.
        var historique = new List<(StatutDisponibilite, DateTime)>
        {
            (StatutDisponibilite.Disponible, T0),
            (StatutDisponibilite.Indisponible, T0.AddHours(1.5)),
            (StatutDisponibilite.Disponible, T0.AddHours(2)),
        };

        // Act — fenêtre de 4 heures.
        var perf = CalculateurPerformanceAgent.Calculer(historique, T0, T0.AddHours(4));

        // Assert
        Assert.Equal(1, perf.NombreIndisponibilites);
        Assert.Equal(TimeSpan.FromMinutes(30), perf.TempsIndisponible);
        Assert.Equal(TimeSpan.FromHours(3.5), perf.TempsDisponible);
        Assert.Equal(TimeSpan.Zero, perf.TempsAbsent);
        Assert.Equal(87.5, perf.PourcentageDisponible); // 3.5h / 4h
    }

    [Fact]
    public void Un_agent_deja_indisponible_au_debut_nest_pas_recompte()
    {
        // Arrange — l'unique transition dans la fenêtre fait SORTIR d'indisponible : aucun passage
        // EN indisponible n'y survient, donc le compteur reste à zéro.
        var historique = new List<(StatutDisponibilite, DateTime)>
        {
            (StatutDisponibilite.Indisponible, T0),
            (StatutDisponibilite.Disponible, T0.AddHours(1)),
        };

        var perf = CalculateurPerformanceAgent.Calculer(historique, T0, T0.AddHours(2));

        Assert.Equal(0, perf.NombreIndisponibilites);
        Assert.Equal(TimeSpan.FromHours(1), perf.TempsIndisponible);
    }
}
