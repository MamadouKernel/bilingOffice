using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Guichet;

public class ReserverRendezVousTests
{
    private sealed class DirectExecuteurTransactionnel : IExecuteurTransactionnel
    {
        public Task<T> ExecuterAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
            => operation(ct);
    }

    private sealed class FakeUniteDeTravail : IUniteDeTravail
    {
        public Task<int> EnregistrerAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    private sealed class FakeRendezVousRepository : IRendezVousRepository
    {
        public List<RendezVous> RendezVousList { get; } = [];

        public Task AjouterAsync(RendezVous rdv, CancellationToken ct = default)
        {
            RendezVousList.Add(rdv);
            return Task.CompletedTask;
        }

        public Task<RendezVous?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(RendezVousList.Find(r => r.Id == id));

        public Task<RendezVous?> ObtenirActifParTicketAsync(Guid ticketId, CancellationToken ct = default)
            => Task.FromResult(RendezVousList.Find(r => r.TicketId == ticketId && r.Statut == StatutRendezVous.Reserve));

        public Task<IReadOnlyList<RendezVous>> ListerParDemandeurAsync(Guid demandeurId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RendezVous>>(RendezVousList.FindAll(r => r.DemandeurId == demandeurId));

        public Task<IReadOnlyList<RendezVous>> ListerParPeriodeAsync(DateOnly du, DateOnly au, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<RendezVous>> ListerParDateAsync(DateOnly date, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RendezVous>>(RendezVousList.FindAll(r => r.Date == date));

        public Task<int> CompterReservationsCreneauAsync(DateOnly date, TimeSpan heureDebut, CancellationToken ct = default)
            => Task.FromResult(RendezVousList.Count(r => r.Date == date && r.HeureDebut == heureDebut && r.Statut == StatutRendezVous.Reserve));

        public Task<bool> PossedeRdvActifAsync(Guid ticketId, CancellationToken ct = default)
            => Task.FromResult(RendezVousList.Any(r => r.TicketId == ticketId && r.Statut == StatutRendezVous.Reserve));
    }

    private sealed class FakeTicketRepository : ITicketRepository
    {
        public Dictionary<Guid, Ticket> Tickets { get; } = [];

        public Task<Ticket?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Tickets.GetValueOrDefault(id));

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

        public Task<Dossier?> ObtenirParIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Dossiers.GetValueOrDefault(id));

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
    public async Task Reserver_Nominal_CreeRdvEtPublieEvenement()
    {
        var rdvRepo = new FakeRendezVousRepository();
        var ticketRepo = new FakeTicketRepository();
        var dossierRepo = new FakeDossierRepository();
        var journalisation = new FakeJournalisationService();
        var uow = new FakeUniteDeTravail();
        var parametres = new ParametresRendezVous(CapaciteParCreneau: 5);
        var horloge = new FakeHorloge();
        var demandeurId = Guid.NewGuid();

        var dossier = Dossier.Creer("DOS-001", demandeurId, horloge.MaintenantUtc, demandeurId);
        dossierRepo.Dossiers[dossier.Id] = dossier;

        var ticket = Ticket.Creer("CIT-IMP-2026-0001", "BillingImport", "DemandeImport", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc, "MEDU12345");
        ticketRepo.Tickets[ticket.Id] = ticket;

        var command = new ReserverRendezVousCommand(
            rdvRepo, ticketRepo, dossierRepo, new DirectExecuteurTransactionnel(), journalisation, uow, parametres, horloge);

        var rdvId = await command.ExecuterAsync(new ReserverRendezVousRequest(
            demandeurId, ticket.Id, new DateOnly(2026, 9, 2), new TimeSpan(9, 0, 0), "Paiement"));

        Assert.NotEqual(Guid.Empty, rdvId);
        Assert.Single(rdvRepo.RendezVousList);
        Assert.Single(journalisation.Evenements);
        Assert.Equal("RendezVousReserve", journalisation.Evenements[0].Type);
    }

    [Fact]
    public async Task Reserver_CreneauPlein_LeveCreneauCompletException()
    {
        var rdvRepo = new FakeRendezVousRepository();
        var ticketRepo = new FakeTicketRepository();
        var dossierRepo = new FakeDossierRepository();
        var journalisation = new FakeJournalisationService();
        var uow = new FakeUniteDeTravail();
        var parametres = new ParametresRendezVous(CapaciteParCreneau: 2);
        var horloge = new FakeHorloge();
        var demandeurId = Guid.NewGuid();

        var date = new DateOnly(2026, 9, 2);
        var heure = new TimeSpan(9, 0, 0);

        // Remplir la capacité
        rdvRepo.RendezVousList.Add(RendezVous.Creer(Guid.NewGuid(), null, Guid.NewGuid(), date, heure, new TimeSpan(9, 30, 0), "Motif 1", horloge.MaintenantUtc));
        rdvRepo.RendezVousList.Add(RendezVous.Creer(Guid.NewGuid(), null, Guid.NewGuid(), date, heure, new TimeSpan(9, 30, 0), "Motif 2", horloge.MaintenantUtc));

        var dossier = Dossier.Creer("DOS-002", demandeurId, horloge.MaintenantUtc, demandeurId);
        dossierRepo.Dossiers[dossier.Id] = dossier;

        var ticket = Ticket.Creer("CIT-IMP-2026-0002", "BillingImport", "DemandeImport", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc, "MEDU12346");
        ticketRepo.Tickets[ticket.Id] = ticket;

        var command = new ReserverRendezVousCommand(
            rdvRepo, ticketRepo, dossierRepo, new DirectExecuteurTransactionnel(), journalisation, uow, parametres, horloge);

        await Assert.ThrowsAsync<CreneauCompletException>(() =>
            command.ExecuterAsync(new ReserverRendezVousRequest(demandeurId, ticket.Id, date, heure, "Paiement")));
    }
}
