using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Guichet;

public class ModeDegradeTests
{
    private sealed class FakeUniteDeTravail : IUniteDeTravail
    {
        public Task<int> EnregistrerAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    private sealed class FakeTicketRepository : ITicketRepository
    {
        public Dictionary<Guid, Ticket> Tickets { get; } = [];
        public Task<Ticket?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Tickets.GetValueOrDefault(id));
        public Task AjouterAsync(Ticket ticket, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Ticket?> ObtenirParNumeroAsync(string numero, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParDossierAsync(Guid dossierId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParDossiersAsync(IEnumerable<Guid> dossierIds, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParFileAsync(Guid fileId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParOperateurAsync(Guid operateurId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Ticket>> ListerTousAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Ticket?> ObtenirTicketActifParNumeroBlAsync(string numeroBl, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Ticket?> ObtenirTicketActifParNumeroBookingAsync(string numeroBooking, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeJournalisationService : IJournalisationService
    {
        public List<(string Type, string Payload)> Evenements { get; } = [];
        public List<(Guid TicketId, string Action, string? Detail)> Historiques { get; } = [];
        public List<(Guid? AuteurId, string Action, string Details)> Audits { get; } = [];

        public Task ConsignerHistoriqueAsync(Guid ticketId, string evenement, string? detail, Guid? auteurId, CancellationToken ct = default)
        {
            Historiques.Add((ticketId, evenement, detail));
            return Task.CompletedTask;
        }

        public Task ConsignerAuditAsync(Guid? auteurId, string action, string entiteType, Guid entiteId, string? avant, string? apres, CancellationToken ct = default)
        {
            Audits.Add((auteurId, action, apres ?? string.Empty));
            return Task.CompletedTask;
        }

        public Task PublierEvenementAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
        {
            Evenements.Add((typeEvenement, chargeUtile));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAutorisationService : IAutorisationService
    {
        public HashSet<(Guid User, string Permission)> Habilitations { get; } = [];
        public Task<bool> EstAutoriseAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
            => Task.FromResult(Habilitations.Contains((utilisateurId, permissionCode)));
        public Task ExigerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
        {
            if (!Habilitations.Contains((utilisateurId, permissionCode)))
                throw new AccesRefuseException($"Permission {permissionCode} refusée.");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHorloge : IHorloge
    {
        public DateTime MaintenantUtc => new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Regulariser_AgentHabilite_ConsigneHistoriqueAuditEtEvenement()
    {
        var ticketRepo = new FakeTicketRepository();
        var auto = new FakeAutorisationService();
        var journal = new FakeJournalisationService();
        var uow = new FakeUniteDeTravail();
        var horloge = new FakeHorloge();

        var agentId = Guid.NewGuid();
        auto.Habilitations.Add((agentId, Permission.GuichetRecherche));

        var ticket = Ticket.Creer("CIT-IMP-2026-0001", "BillingImport", "DemandeImport", Guid.NewGuid(), Guid.NewGuid(), horloge.MaintenantUtc, "MEDU12345");
        ticketRepo.Tickets[ticket.Id] = ticket;

        var command = new RegulariserModeDegradeCommand(ticketRepo, auto, journal, uow, horloge);
        var dateReelle = new DateTime(2026, 9, 1, 8, 30, 0, DateTimeKind.Utc);

        await command.ExecuterAsync(new RegulariserModeDegradeRequest(
            agentId, ticket.Id, dateReelle, "Encaissement espèces au guichet n°2"));

        Assert.Single(journal.Historiques);
        Assert.Equal("ModeDegradeRegularise", journal.Historiques[0].Action);
        Assert.Single(journal.Audits);
        Assert.Equal("ModeDegrade.Regularisation", journal.Audits[0].Action);
        Assert.Single(journal.Evenements);
        Assert.Equal("ModeDegradeRegularise", journal.Evenements[0].Type);
    }

    [Fact]
    public async Task Regulariser_AgentNonHabilite_LeveAccesRefuseException()
    {
        var ticketRepo = new FakeTicketRepository();
        var auto = new FakeAutorisationService();
        var journal = new FakeJournalisationService();
        var uow = new FakeUniteDeTravail();
        var horloge = new FakeHorloge();

        var agentId = Guid.NewGuid();
        var command = new RegulariserModeDegradeCommand(ticketRepo, auto, journal, uow, horloge);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            command.ExecuterAsync(new RegulariserModeDegradeRequest(
                agentId, Guid.NewGuid(), DateTime.UtcNow, "Opération")));
    }
}
