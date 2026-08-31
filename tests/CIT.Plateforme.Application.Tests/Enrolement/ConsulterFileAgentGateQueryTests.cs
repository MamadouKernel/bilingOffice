using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — glue UI (C8-10), mirroring ConsulterFileReclamationsQuery.
public class ConsulterFileAgentGateQueryTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IFileDeTraitementRepository> _fileRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly ConsulterFileAgentGateQuery _query;
    private readonly FileDeTraitement _fileAgentGate = FileDeTraitement.Creer("Enrolement", "Agent Gate");

    public ConsulterFileAgentGateQueryTests()
    {
        _query = new ConsulterFileAgentGateQuery(_ticketRepo.Object, _fileRepo.Object, _autorisation.Object);
        _fileRepo.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([_fileAgentGate]);
    }

    [Fact]
    public async Task Agent_habilite_recoit_les_tickets_en_attente_et_en_cours()
    {
        var agentId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.EnrolementInstruire, _fileAgentGate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ticket = Ticket.Creer("ENR-01", "Enrolement", "Enrolement", _fileAgentGate.Id, Guid.NewGuid(), DateTime.UtcNow);
        _ticketRepo.Setup(r => r.ListerParFileAsync(_fileAgentGate.Id, It.IsAny<CancellationToken>())).ReturnsAsync([ticket]);

        var resultat = await _query.ExecuterAsync(agentId);

        Assert.Single(resultat);
    }

    [Fact]
    public async Task Agent_non_habilite_est_refuse()
    {
        var agentId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.EnrolementInstruire, _fileAgentGate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _autorisation.Setup(a => a.EstAutoriseAsync(agentId, Permission.TicketPrendreEnCharge, _fileAgentGate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<AccesRefuseException>(() => _query.ExecuterAsync(agentId));
    }
}
