using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.ActualisationBadt;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

public class TraitementLigneBadtCommandsTests
{
    private readonly Mock<ILigneActualisationBadtRepository> _lignes = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly Ticket _ticket = Ticket.CreerDemandeActualisationBadt(
        "TCK-BADT-001", Guid.NewGuid(), Guid.NewGuid(), "BADT expiré.", new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc));

    private readonly QualifierLigneBadtCommand _qualifier;
    private readonly DeciderLigneBadtCommand _decider;

    public TraitementLigneBadtCommandsTests()
    {
        _qualifier = new QualifierLigneBadtCommand(_lignes.Object, _tickets.Object, _autorisation.Object, _journalisation.Object, _uniteDeTravail.Object);
        _decider = new DeciderLigneBadtCommand(_lignes.Object, _tickets.Object, _autorisation.Object, _journalisation.Object, _horloge.Object, _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc));
        _tickets.Setup(r => r.ObtenirParIdAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_ticket);
    }

    private LigneActualisationBadt UneLigne()
    {
        var ligne = LigneActualisationBadt.Creer(_ticket.Id, "BL123", "TCLU1234567");
        _lignes.Setup(r => r.ObtenirParIdAsync(ligne.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ligne);
        return ligne;
    }

    [Fact]
    public async Task Qualifier_pose_cause_et_motif_puis_enregistre()
    {
        var ligne = UneLigne();
        var agentId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.ActualisationBadtInstruire, _ticket.FileCouranteId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _qualifier.ExecuterAsync(agentId, ligne.Id, CauseExpirationBadt.Terminale, "Retard terminal.", CancellationToken.None);

        Assert.Equal(CauseExpirationBadt.Terminale, ligne.Cause);
        Assert.Equal("Retard terminal.", ligne.MotifCause);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Qualifier_sans_permission_refuse()
    {
        var ligne = UneLigne();
        var agentId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _qualifier.ExecuterAsync(agentId, ligne.Id, CauseExpirationBadt.Terminale, "Motif.", CancellationToken.None));

        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Decider_accord_calcule_la_nouvelle_date_a_decision_plus_N()
    {
        var ligne = UneLigne();
        ligne.Qualifier(CauseExpirationBadt.Terminale, "Retard terminal.");
        var decideurId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(decideurId, Permission.ActualisationBadtDecider, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _decider.ExecuterAsync(decideurId, new DeciderLigneBadtRequete(ligne.Id, DecisionLigneBadt.Accordee, NombreJoursAccordes: 7), CancellationToken.None);

        Assert.Equal(DecisionLigneBadt.Accordee, ligne.Decision);
        Assert.Equal(7, ligne.NombreJoursAccordes);
        Assert.Equal(new DateOnly(2026, 10, 22), ligne.NouvelleDateValidite);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Decider_accord_sans_jours_leve_une_erreur()
    {
        var ligne = UneLigne();
        ligne.Qualifier(CauseExpirationBadt.Terminale, "Retard terminal.");
        var decideurId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(decideurId, Permission.ActualisationBadtDecider, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _decider.ExecuterAsync(decideurId, new DeciderLigneBadtRequete(ligne.Id, DecisionLigneBadt.Accordee), CancellationToken.None));
    }

    [Fact]
    public async Task Decider_refus_pose_le_motif()
    {
        var ligne = UneLigne();
        var decideurId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(decideurId, Permission.ActualisationBadtDecider, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _decider.ExecuterAsync(decideurId, new DeciderLigneBadtRequete(ligne.Id, DecisionLigneBadt.Refusee, MotifRefus: "Hors délai."), CancellationToken.None);

        Assert.Equal(DecisionLigneBadt.Refusee, ligne.Decision);
        Assert.Equal("Hors délai.", ligne.MotifRefus);
    }

    [Fact]
    public async Task Decider_sans_permission_refuse()
    {
        var ligne = UneLigne();
        var decideurId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _decider.ExecuterAsync(decideurId, new DeciderLigneBadtRequete(ligne.Id, DecisionLigneBadt.Refusee, MotifRefus: "x"), CancellationToken.None));
    }
}
