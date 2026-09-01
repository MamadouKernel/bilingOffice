using CIT.Plateforme.Domain.AvoirExoneration;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Domain.Tests.AvoirExoneration;

public sealed class DemandeAvoirExoTests
{
    private readonly Guid _dossierId = Guid.NewGuid();
    private readonly Guid _fileCibleId = Guid.NewGuid();
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly DateTime _maintenantUtc = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreerDemandeAvoirExo_Avoir_Nominal_CreeTicketValide()
    {
        // Act
        var ticket = Ticket.CreerDemandeAvoirExo(
            numero: "AVO-2026-00001",
            typeDemande: "Avoir",
            fileCibleId: _fileCibleId,
            dossierId: _dossierId,
            objet: "Erreur facturation acconage",
            description: "Double facturation constatée sur le BL 12345",
            montantConteste: 150000m,
            maintenantUtc: _maintenantUtc);

        // Assert
        Assert.NotEqual(Guid.Empty, ticket.Id);
        Assert.Equal("AVO-2026-00001", ticket.Numero);
        Assert.Equal("AvoirExoneration", ticket.Module);
        Assert.Equal("Avoir", ticket.TypeDemande);
        Assert.Equal(_dossierId, ticket.DossierId);
        Assert.Equal(_fileCibleId, ticket.FileCouranteId);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticket.Statut);
        Assert.Equal("Erreur facturation acconage", ticket.Objet);
        Assert.Equal("Double facturation constatée sur le BL 12345", ticket.Description);
        Assert.Equal(150000m, ticket.MontantConteste);
        Assert.Null(ticket.MontantAccorde);
        Assert.Null(ticket.DecisionAvoirExo);
        Assert.Null(ticket.MotifRejetAvoirExo);
        Assert.Equal(_maintenantUtc, ticket.DateCreationUtc);
    }

    [Fact]
    public void CreerDemandeAvoirExo_Exoneration_Nominal_CreeTicketValide()
    {
        // Act
        var ticket = Ticket.CreerDemandeAvoirExo(
            numero: "EXO-2026-00001",
            typeDemande: "Exoneration",
            fileCibleId: _fileCibleId,
            dossierId: _dossierId,
            objet: "Exonération conventionnelle",
            description: "Application de la franchise diplomatique",
            montantConteste: null,
            maintenantUtc: _maintenantUtc);

        // Assert
        Assert.Equal("EXO-2026-00001", ticket.Numero);
        Assert.Equal("AvoirExoneration", ticket.Module);
        Assert.Equal("Exoneration", ticket.TypeDemande);
        Assert.Null(ticket.MontantConteste);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticket.Statut);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreerDemandeAvoirExo_NumeroInvalide_LeveArgumentException(string? numeroInvalide)
    {
        Assert.Throws<ArgumentException>(() =>
            Ticket.CreerDemandeAvoirExo(
                numero: numeroInvalide!,
                typeDemande: "Avoir",
                fileCibleId: _fileCibleId,
                dossierId: _dossierId,
                objet: "Objet",
                description: "Desc",
                montantConteste: 100m,
                maintenantUtc: _maintenantUtc));
    }

    [Theory]
    [InlineData("Autre")]
    [InlineData("Litige")]
    [InlineData("")]
    public void CreerDemandeAvoirExo_TypeInvalide_LeveArgumentException(string typeInvalide)
    {
        Assert.Throws<ArgumentException>(() =>
            Ticket.CreerDemandeAvoirExo(
                numero: "AVO-2026-00001",
                typeDemande: typeInvalide,
                fileCibleId: _fileCibleId,
                dossierId: _dossierId,
                objet: "Objet",
                description: "Desc",
                montantConteste: 100m,
                maintenantUtc: _maintenantUtc));
    }

    [Fact]
    public void CreerDemandeAvoirExo_MontantNegatif_LeveArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            Ticket.CreerDemandeAvoirExo(
                numero: "AVO-2026-00001",
                typeDemande: "Avoir",
                fileCibleId: _fileCibleId,
                dossierId: _dossierId,
                objet: "Objet",
                description: "Desc",
                montantConteste: -50m,
                maintenantUtc: _maintenantUtc));
    }

    [Fact]
    public void EnregistrerDecisionAvoirExo_Accord_Nominal_PasseAValideEtCloture()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo(
            numero: "AVO-2026-00001",
            typeDemande: "Avoir",
            fileCibleId: _fileCibleId,
            dossierId: _dossierId,
            objet: "Objet",
            description: "Desc",
            montantConteste: 200000m,
            maintenantUtc: _maintenantUtc);

        ticket.PrendreEnCharge(_agentId, _maintenantUtc);

        var dateDecision = _maintenantUtc.AddHours(2);

        // Act
        ticket.EnregistrerDecisionAvoirExo(
            decision: DecisionAvoirExo.Accorde,
            motifRejet: null,
            montantAccorde: 180000m,
            decideurId: _agentId,
            maintenantUtc: dateDecision);

        // Assert
        Assert.Equal(StatutTicket.Cloture, ticket.Statut);
        Assert.Equal(DecisionAvoirExo.Accorde, ticket.DecisionAvoirExo);
        Assert.Null(ticket.MotifRejetAvoirExo);
        Assert.Equal(180000m, ticket.MontantAccorde);
        Assert.Equal(_agentId, ticket.DecideurAvoirExoId);
        Assert.Equal(dateDecision, ticket.DateDecisionAvoirExoUtc);
        Assert.Equal(dateDecision, ticket.DateClotureUtc);
    }

    [Fact]
    public void EnregistrerDecisionAvoirExo_Rejet_AvecMotif_PasseARejeteEtCloture()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo(
            numero: "AVO-2026-00001",
            typeDemande: "Avoir",
            fileCibleId: _fileCibleId,
            dossierId: _dossierId,
            objet: "Objet",
            description: "Desc",
            montantConteste: 200000m,
            maintenantUtc: _maintenantUtc);

        ticket.PrendreEnCharge(_agentId, _maintenantUtc);

        var dateDecision = _maintenantUtc.AddHours(2);

        // Act
        ticket.EnregistrerDecisionAvoirExo(
            decision: DecisionAvoirExo.Rejete,
            motifRejet: "Délai de contestation de 30 jours dépassé",
            montantAccorde: null,
            decideurId: _agentId,
            maintenantUtc: dateDecision);

        // Assert
        Assert.Equal(StatutTicket.Cloture, ticket.Statut);
        Assert.Equal(DecisionAvoirExo.Rejete, ticket.DecisionAvoirExo);
        Assert.Equal("Délai de contestation de 30 jours dépassé", ticket.MotifRejetAvoirExo);
        Assert.Equal(0m, ticket.MontantAccorde);
        Assert.Equal(_agentId, ticket.DecideurAvoirExoId);
        Assert.Equal(dateDecision, ticket.DateDecisionAvoirExoUtc);
        Assert.Equal(dateDecision, ticket.DateClotureUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnregistrerDecisionAvoirExo_Rejet_SansMotif_LeveArgumentException(string? motifInvalide)
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo(
            numero: "AVO-2026-00001",
            typeDemande: "Avoir",
            fileCibleId: _fileCibleId,
            dossierId: _dossierId,
            objet: "Objet",
            description: "Desc",
            montantConteste: 200000m,
            maintenantUtc: _maintenantUtc);

        ticket.PrendreEnCharge(_agentId, _maintenantUtc);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ticket.EnregistrerDecisionAvoirExo(
                decision: DecisionAvoirExo.Rejete,
                motifRejet: motifInvalide,
                montantAccorde: null,
                decideurId: _agentId,
                maintenantUtc: _maintenantUtc));
    }

    [Fact]
    public void EnregistrerDecisionAvoirExo_TicketDejaCloture_LeveInvalidOperationException()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo(
            numero: "AVO-2026-00001",
            typeDemande: "Avoir",
            fileCibleId: _fileCibleId,
            dossierId: _dossierId,
            objet: "Objet",
            description: "Desc",
            montantConteste: 200000m,
            maintenantUtc: _maintenantUtc);

        ticket.PrendreEnCharge(_agentId, _maintenantUtc);
        ticket.EnregistrerDecisionAvoirExo(DecisionAvoirExo.Accorde, null, 100m, _agentId, _maintenantUtc);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            ticket.EnregistrerDecisionAvoirExo(
                decision: DecisionAvoirExo.Rejete,
                motifRejet: "Nouveau motif",
                montantAccorde: null,
                decideurId: _agentId,
                maintenantUtc: _maintenantUtc.AddDays(1)));
    }
}
