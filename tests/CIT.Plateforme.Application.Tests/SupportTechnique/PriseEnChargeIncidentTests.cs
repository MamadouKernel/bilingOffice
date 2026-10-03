using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.SupportTechnique;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.SupportTechnique;

/// <summary>
/// La file Support IT propose « Prendre en charge » à l'agent habilité à instruire les incidents :
/// la prise en charge doit donc reposer sur cette habilitation (N1 ou N2), et non sur la
/// permission générique des files métier, que le profil Support IT ne détient pas.
/// </summary>
public sealed class PriseEnChargeIncidentTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly PrendreEnChargeCommand _commande;

    public PriseEnChargeIncidentTests()
    {
        _horloge.Setup(h => h.MaintenantUtc).Returns(DateTime.UtcNow);
        _commande = new PrendreEnChargeCommand(_tickets.Object, _autorisation.Object,
            new Mock<IJournalisationService>().Object, _horloge.Object, new Mock<IUniteDeTravail>().Object);
    }

    private Ticket Incident()
    {
        var ticket = Ticket.Creer("SUP-INC-01", SupportItConstantes.Module, "Incident", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        _tickets.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        return ticket;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Un_agent_Support_IT_N1_ou_N2_prend_en_charge_un_incident(bool n1, bool n2)
    {
        var agentId = Guid.NewGuid();
        var ticket = Incident();
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.SupportIncidentInstruire, ticket.FileCouranteId, It.IsAny<CancellationToken>())).ReturnsAsync(n1);
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.SupportIncidentInstruireN2, ticket.FileCouranteId, It.IsAny<CancellationToken>())).ReturnsAsync(n2);

        await _commande.ExecuterAsync(agentId, ticket.Id);

        Assert.Equal(StatutTicket.EnCours, ticket.Statut);
        _autorisation.Verify(a => a.ExigerAsync(It.IsAny<Guid>(), Permission.TicketPrendreEnCharge, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Sans_habilitation_d_instruction_la_prise_en_charge_d_un_incident_est_refusee()
    {
        var agentId = Guid.NewGuid();
        var ticket = Incident();

        await Assert.ThrowsAsync<AccesRefuseException>(() => _commande.ExecuterAsync(agentId, ticket.Id));
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticket.Statut);
    }
}
