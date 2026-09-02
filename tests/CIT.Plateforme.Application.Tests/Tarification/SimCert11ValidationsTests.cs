using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Domain.Tarification;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

public sealed class SimCert11ValidationsTests
{
    private readonly ValiderCoherenceSimulationQuery _validateur = new();

    [Fact]
    public void Valider_DateEnlevementAnterieureArrivee_RetourneAlerte()
    {
        // Arrange - Règle 1 CDC : Dates de séjour
        var dateArrivee = new DateOnly(2026, 9, 10);
        var dateEnlevement = new DateOnly(2026, 9, 5);

        // Act
        var resultat = _validateur.Executer(
            SensTrafic.ImportLocal,
            dateArrivee,
            dateEnlevement,
            [
                new ConteneurValidationRequete(TailleConteneur.VingtPieds, TypeConteneur.Dry, "Standard", false, 15m)
            ]);

        // Assert
        Assert.False(resultat.EstValide);
        Assert.Contains(resultat.Alertes, a => a.Contains("date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valider_BranchementFrigorifiqueSurConteneurDry_RetourneAlerte()
    {
        // Arrange - Règle 2 CDC : Exclusivité Reefer
        var dateArrivee = new DateOnly(2026, 9, 1);
        var dateEnlevement = new DateOnly(2026, 9, 10);

        // Act
        var resultat = _validateur.Executer(
            SensTrafic.ImportLocal,
            dateArrivee,
            dateEnlevement,
            [
                new ConteneurValidationRequete(TailleConteneur.VingtPieds, TypeConteneur.Dry, "Standard", true, 15m) // Branchement sur Dry
            ]);

        // Assert
        Assert.False(resultat.EstValide);
        Assert.Contains(resultat.Alertes, a => a.Contains("Reefer", StringComparison.OrdinalIgnoreCase) || a.Contains("frigorifique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valider_CategorieIncompatibleAvecSens_RetourneAlerte()
    {
        // Arrange - Règle 3 CDC : Catégorie compatible avec le sens
        var dateArrivee = new DateOnly(2026, 9, 1);
        var dateEnlevement = new DateOnly(2026, 9, 10);

        // Act
        var resultat = _validateur.Executer(
            SensTrafic.ImportLocal,
            dateArrivee,
            dateEnlevement,
            [
                new ConteneurValidationRequete(TailleConteneur.VingtPieds, TypeConteneur.Dry, "Marchandise Export Specifique", false, 15m)
            ]);

        // Assert
        Assert.False(resultat.EstValide);
        Assert.Contains(resultat.Alertes, a => a.Contains("incompatible", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valider_TailleOuTypeNonReconnu_RetourneAlerte()
    {
        // Arrange - Règle 4 CDC : Taille et type reconnus
        var dateArrivee = new DateOnly(2026, 9, 1);
        var dateEnlevement = new DateOnly(2026, 9, 10);

        // Act
        var resultat = _validateur.Executer(
            SensTrafic.ImportLocal,
            dateArrivee,
            dateEnlevement,
            [
                new ConteneurValidationRequete((TailleConteneur)99, TypeConteneur.Dry, "Standard", false, 15m)
            ]);

        // Assert
        Assert.False(resultat.EstValide);
        Assert.Contains(resultat.Alertes, a => a.Contains("non reconnu", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valider_SimulationCoherente_RetourneSuccesEtMessagesAide()
    {
        // Arrange - Simulation entièrement valide
        var dateArrivee = new DateOnly(2026, 9, 1);
        var dateEnlevement = new DateOnly(2026, 9, 8);

        // Act
        var resultat = _validateur.Executer(
            SensTrafic.ImportLocal,
            dateArrivee,
            dateEnlevement,
            [
                new ConteneurValidationRequete(TailleConteneur.QuarantePieds, TypeConteneur.Reefer, "Standard", true, 25m)
            ]);

        // Assert
        Assert.True(resultat.EstValide);
        Assert.Empty(resultat.Alertes);
        Assert.NotEmpty(resultat.MessagesAide);
    }
}
