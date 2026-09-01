using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Guichet;

public class AlternativePresentielleTests
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

    private sealed class FakeDossierRepository : IDossierRepository
    {
        public Dictionary<Guid, Dossier> Dossiers { get; } = [];
        public Task<Dossier?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Dossiers.GetValueOrDefault(id));
        public Task AjouterAsync(Dossier dossier, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Dossier>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Dossier>> ListerParClientAsync(Guid clientId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeJournalisationService : IJournalisationService
    {
        public List<(string Type, string Payload)> Evenements { get; } = [];
        public List<(Guid TicketId, string Action, string? Detail)> Historiques { get; } = [];

        public Task ConsignerHistoriqueAsync(Guid ticketId, string evenement, string? detail, Guid? auteurId, CancellationToken ct = default)
        {
            Historiques.Add((ticketId, evenement, detail));
            return Task.CompletedTask;
        }

        public Task ConsignerAuditAsync(Guid? auteurId, string action, string entiteType, Guid entiteId, string? avant, string? apres, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task PublierEvenementAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default)
        {
            Evenements.Add((typeEvenement, chargeUtile));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHorloge : IHorloge
    {
        public DateTime MaintenantUtc => new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Basculer_ClientProprietaire_ConsigneHistoriqueEtEmetEvenement()
    {
        var ticketRepo = new FakeTicketRepository();
        var dossierRepo = new FakeDossierRepository();
        var journal = new FakeJournalisationService();
        var uow = new FakeUniteDeTravail();
        var horloge = new FakeHorloge();
        var demandeurId = Guid.NewGuid();

        var dossier = Dossier.Creer("DOS-001", demandeurId, horloge.MaintenantUtc, demandeurId);
        dossierRepo.Dossiers[dossier.Id] = dossier;

        var ticket = Ticket.Creer("CIT-IMP-2026-0001", "BillingImport", "DemandeImport", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc, "MEDU12345");
        ticketRepo.Tickets[ticket.Id] = ticket;

        var command = new BasculerAlternativePresentielleCommand(ticketRepo, dossierRepo, journal, uow, horloge);
        await command.ExecuterAsync(demandeurId, ticket.Id);

        Assert.Single(journal.Historiques);
        Assert.Equal("BasculeAlternativePresentielle", journal.Historiques[0].Action);
        Assert.Single(journal.Evenements);
        Assert.Equal("BasculePresentielleEffectuee", journal.Evenements[0].Type);
    }
}
