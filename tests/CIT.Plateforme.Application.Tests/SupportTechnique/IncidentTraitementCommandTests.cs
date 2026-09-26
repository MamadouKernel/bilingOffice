using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.SupportTechnique;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.SupportTechnique;

/// <summary>
/// [TEST-SPEC — SUPPORT IT] Traitement d'un incident par le N1 : résolution (clôture avec réponse
/// formalisée) et escalade N1→N2 (action tracée, motif obligatoire). Les deux exigent
/// <see cref="Permission.SupportIncidentInstruire"/> sur la file de l'incident et refusent les
/// tickets d'un autre module.
/// </summary>
public class IncidentTraitementCommandTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journal = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uow = new();

    public IncidentTraitementCommandTests()
    {
        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc));
    }

    private static Ticket IncidentEnCours()
    {
        var ticket = Ticket.Creer("TCK-INC-010", SupportItConstantes.Module, SupportItConstantes.TypeDemande,
            Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 9, 26, 9, 0, 0, DateTimeKind.Utc));
        ticket.PrendreEnCharge(Guid.NewGuid(), new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc));
        return ticket;
    }

    private ResoudreIncidentCommand Resoudre() =>
        new(_tickets.Object, _autorisation.Object, _journal.Object, _horloge.Object, _uow.Object);

    private EscaladerIncidentCommand Escalader() =>
        new(_tickets.Object, _autorisation.Object, _journal.Object, _uow.Object);

    [Fact]
    public async Task Resoudre_cloture_avec_reponse_et_enregistre()
    {
        var ticket = IncidentEnCours();
        _tickets.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await Resoudre().ExecuterAsync(new ResoudreIncidentRequete(Guid.NewGuid(), ticket.Id, "Cache vidé, service redémarré."), CancellationToken.None);

        Assert.Equal(StatutTicket.Cloture, ticket.Statut);
        Assert.Equal("Cache vidé, service redémarré.", ticket.ReponseFormalisee);
        _uow.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Resoudre_refuse_un_ticket_d_un_autre_module()
    {
        var autre = Ticket.Creer("TCK-REC-1", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        _tickets.Setup(r => r.ObtenirParIdAsync(autre.Id, It.IsAny<CancellationToken>())).ReturnsAsync(autre);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Resoudre().ExecuterAsync(new ResoudreIncidentRequete(Guid.NewGuid(), autre.Id, "x"), CancellationToken.None));
    }

    [Fact]
    public async Task Escalader_trace_et_publie_un_evenement()
    {
        var ticket = IncidentEnCours();
        _tickets.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        var agent = Guid.NewGuid();

        await Escalader().ExecuterAsync(new EscaladerIncidentRequete(agent, ticket.Id, "Nécessite un correctif logiciel."), CancellationToken.None);

        _journal.Verify(j => j.ConsignerHistoriqueAsync(ticket.Id, "IncidentEscalade", "Nécessite un correctif logiciel.", agent, It.IsAny<CancellationToken>()), Times.Once);
        _journal.Verify(j => j.PublierEvenementAsync("TicketIncidentEscalade", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Escalader_sans_motif_est_refusee(string motif)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => Escalader().ExecuterAsync(new EscaladerIncidentRequete(Guid.NewGuid(), Guid.NewGuid(), motif), CancellationToken.None));
        _uow.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
