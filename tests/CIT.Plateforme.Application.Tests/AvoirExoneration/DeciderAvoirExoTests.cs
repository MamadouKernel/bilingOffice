using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.AvoirExoneration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.AvoirExoneration;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;

namespace CIT.Plateforme.Application.Tests.AvoirExoneration;

public sealed class DeciderAvoirExoTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IStockageDocuments> _stockage = new();
    private readonly Mock<IAnalyseAntivirus> _antivirus = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly DeciderDemandeAvoirExoCommand _command;

    private readonly Guid _agentId = Guid.NewGuid();
    private readonly DateTime _maintenantUtc = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    public DeciderAvoirExoTests()
    {
        _command = new DeciderDemandeAvoirExoCommand(
            _ticketRepo.Object,
            _documentRepo.Object,
            _stockage.Object,
            _antivirus.Object,
            _journalisation.Object,
            _horloge.Object,
            _autorisation.Object,
            _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(_maintenantUtc);
        _antivirus.Setup(a => a.AnalyserAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>())).ReturnsAsync(StatutAnalyseAntivirus.Sain);
        // AVE-02 (remédiation) : la permission de décision est propre au type de demande.
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.AvoirDecider, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.ExonerationDecider, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _stockage.Setup(s => s.EnregistrerAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("documents/decisions/dec.pdf");
    }

    [Fact]
    public async Task Decider_Accord_Nominal_EnregistreDecisionEtClotureEtPublieEvenement()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo("AVO-001", "Avoir", Guid.NewGuid(), Guid.NewGuid(), "Obj", "Desc", 100000m, _maintenantUtc);
        ticket.PrendreEnCharge(_agentId, _maintenantUtc);

        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var docDecision = new PieceJointeSoumission("note_avoir.pdf", "application/pdf", [1, 2, 3]);
        var requete = new DeciderDemandeAvoirExoRequete(
            TicketId: ticket.Id,
            Decision: DecisionAvoirExo.Accorde,
            MotifRejet: null,
            MontantAccorde: 95000m,
            DocumentDecision: docDecision);

        // Act
        await _command.ExecuterAsync(_agentId, requete, CancellationToken.None);

        // Assert
        Assert.Equal(StatutTicket.Cloture, ticket.Statut);
        Assert.Equal(DecisionAvoirExo.Accorde, ticket.DecisionAvoirExo);
        Assert.Equal(95000m, ticket.MontantAccorde);
        Assert.Equal(_agentId, ticket.DecideurAvoirExoId);

        _documentRepo.Verify(d => d.AjouterAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
        _journalisation.Verify(j => j.PublierEvenementAsync("AvoirAccorde", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Decider_Rejet_AvecMotif_EnregistreRejetEtClotureEtPublieEvenement()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo("EXO-001", "Exoneration", Guid.NewGuid(), Guid.NewGuid(), "Obj", "Desc", null, _maintenantUtc);
        ticket.PrendreEnCharge(_agentId, _maintenantUtc);

        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var requete = new DeciderDemandeAvoirExoRequete(
            TicketId: ticket.Id,
            Decision: DecisionAvoirExo.Rejete,
            MotifRejet: "Document périmé",
            MontantAccorde: null,
            DocumentDecision: null);

        // Act
        await _command.ExecuterAsync(_agentId, requete, CancellationToken.None);

        // Assert
        Assert.Equal(StatutTicket.Cloture, ticket.Statut);
        Assert.Equal(DecisionAvoirExo.Rejete, ticket.DecisionAvoirExo);
        Assert.Equal("Document périmé", ticket.MotifRejetAvoirExo);
        _journalisation.Verify(j => j.PublierEvenementAsync("ExonerationRejetee", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Decider_Rejet_SansMotif_LeveArgumentException()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo("AVO-001", "Avoir", Guid.NewGuid(), Guid.NewGuid(), "Obj", "Desc", 100000m, _maintenantUtc);
        ticket.PrendreEnCharge(_agentId, _maintenantUtc);

        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);

        var requete = new DeciderDemandeAvoirExoRequete(
            TicketId: ticket.Id,
            Decision: DecisionAvoirExo.Rejete,
            MotifRejet: "",
            MontantAccorde: null,
            DocumentDecision: null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(_agentId, requete, CancellationToken.None));
    }

    [Fact]
    public async Task Decider_AgentNonHabilite_LeveAccesRefuseException()
    {
        // Arrange (SEC-AVE-04)
        var ticket = Ticket.CreerDemandeAvoirExo("AVO-002", "Avoir", Guid.NewGuid(), Guid.NewGuid(), "Obj", "Desc", 100m, _maintenantUtc);
        ticket.PrendreEnCharge(_agentId, _maintenantUtc);
        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.AvoirDecider, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var requete = new DeciderDemandeAvoirExoRequete(
            TicketId: ticket.Id,
            Decision: DecisionAvoirExo.Accorde,
            MotifRejet: null,
            MontantAccorde: 100m,
            DocumentDecision: null);

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(_agentId, requete, CancellationToken.None));
    }

    /// <summary>
    /// AVE-02 (remédiation) : Avoir et Exonération sont des workflows séparément habilitables. Un
    /// agent habilité à décider des Avoirs mais pas des Exonérations doit être refusé sur ces
    /// dernières, et réciproquement.
    /// </summary>
    [Fact]
    public async Task Decider_HabiliteAvoirSeulement_RefuseSurExoneration()
    {
        // Arrange
        var ticket = Ticket.CreerDemandeAvoirExo("EXO-002", "Exoneration", Guid.NewGuid(), Guid.NewGuid(), "Obj", "Desc", null, _maintenantUtc);
        ticket.PrendreEnCharge(_agentId, _maintenantUtc);
        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.ExonerationDecider, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var requete = new DeciderDemandeAvoirExoRequete(
            TicketId: ticket.Id,
            Decision: DecisionAvoirExo.Accorde,
            MotifRejet: null,
            MontantAccorde: null,
            DocumentDecision: null);

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(_agentId, requete, CancellationToken.None));
    }
}
