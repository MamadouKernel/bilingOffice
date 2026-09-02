using CIT.Plateforme.Application.Supervision;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

public class CalculateurDisponibiliteTests
{
    [Fact]
    public void NfrCert03_03_Disponibilite100Pourcent_QuandAucunePanne()
    {
        // Arrange : 10h ouvrées, 0 panne, 0 maintenance
        var tempsOuvre = TimeSpan.FromHours(10);
        var tempsMaintenance = TimeSpan.Zero;
        var pannes = Array.Empty<TimeSpan>();

        // Act
        var resultat = CalculateurDisponibilite.Calculer(tempsOuvre, tempsMaintenance, pannes);

        // Assert
        Assert.Equal(100.0, resultat.PourcentageDisponibilite);
        Assert.True(resultat.EstConformeSla);
    }

    [Fact]
    public void NfrCert03_04_DisponibiliteExactement99Pourcent_EstConformeSla()
    {
        // Arrange : 100h ouvrées, 0 maintenance, 1h de panne -> 99h disponibles = 99.0%
        var tempsOuvre = TimeSpan.FromHours(100);
        var tempsMaintenance = TimeSpan.Zero;
        var pannes = new[] { TimeSpan.FromHours(1) };

        // Act
        var resultat = CalculateurDisponibilite.Calculer(tempsOuvre, tempsMaintenance, pannes);

        // Assert
        Assert.Equal(99.0, resultat.PourcentageDisponibilite);
        Assert.True(resultat.EstConformeSla);
    }

    [Fact]
    public void NfrCert03_05_DisponibiliteSous99Pourcent_EstNonConforme()
    {
        // Arrange : 100h ouvrées, 0 maintenance, 1h01 (1.01h) de panne -> 98.99%
        var tempsOuvre = TimeSpan.FromHours(100);
        var tempsMaintenance = TimeSpan.Zero;
        var pannes = new[] { TimeSpan.FromMinutes(61) };

        // Act
        var resultat = CalculateurDisponibilite.Calculer(tempsOuvre, tempsMaintenance, pannes);

        // Assert
        Assert.True(resultat.PourcentageDisponibilite < 99.0);
        Assert.False(resultat.EstConformeSla);
    }

    [Fact]
    public void NfrCert03_06_MaintenancePlanifiee_EstDeduiteDuDenominateur()
    {
        // Arrange : 100h ouvrées, 20h de maintenance planifiée -> 80h éligibles.
        // Panne non planifiée de 0.8h -> 79.2h dispo -> 79.2 / 80 = 99.0%
        var tempsOuvre = TimeSpan.FromHours(100);
        var tempsMaintenance = TimeSpan.FromHours(20);
        var pannes = new[] { TimeSpan.FromHours(0.8) };

        // Act
        var resultat = CalculateurDisponibilite.Calculer(tempsOuvre, tempsMaintenance, pannes);

        // Assert
        Assert.Equal(TimeSpan.FromHours(80), resultat.TempsEligible);
        Assert.NotNull(resultat.PourcentageDisponibilite);
        Assert.Equal(99.0, Math.Round(resultat.PourcentageDisponibilite.Value, 2));
        Assert.True(resultat.EstConformeSla);
    }

    [Fact]
    public void NfrCert03_07_TempsEligibleNul_RenvoieNonApplicable_JamaisCentPourcentArtificiel()
    {
        // Arrange : 10h ouvrées intégralement en maintenance planifiée (10h) -> 0h éligibles
        var tempsOuvre = TimeSpan.FromHours(10);
        var tempsMaintenance = TimeSpan.FromHours(10);
        var pannes = Array.Empty<TimeSpan>();

        // Act
        var resultat = CalculateurDisponibilite.Calculer(tempsOuvre, tempsMaintenance, pannes);

        // Assert
        Assert.Equal(TimeSpan.Zero, resultat.TempsEligible);
        Assert.Null(resultat.PourcentageDisponibilite);
        Assert.False(resultat.EstConformeSla);
    }
}
