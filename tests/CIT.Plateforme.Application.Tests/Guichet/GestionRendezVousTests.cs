using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Securite;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Guichet;

public class GestionRendezVousTests
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

    private sealed class FakeJournalisationService : IJournalisationService
    {
        public List<(string Type, string Payload)> Evenements { get; } = [];

        public Task ConsignerHistoriqueAsync(Guid ticketId, string evenement, string? detail, Guid? auteurId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task ConsignerAuditAsync(Guid? auteurId, string action, string entiteType, Guid entiteId, string? avant, string? apres, CancellationToken ct = default)
            => Task.CompletedTask;

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
    public async Task Annuler_ClientProprietaire_PasseAAnnuleEtEmetEvenement()
    {
        var rdvRepo = new FakeRendezVousRepository();
        var journalisation = new FakeJournalisationService();
        var auto = new FakeAutorisationService();
        var uow = new FakeUniteDeTravail();
        var horloge = new FakeHorloge();
        var demandeurId = Guid.NewGuid();

        var rdv = RendezVous.Creer(Guid.NewGuid(), null, demandeurId, new DateOnly(2026, 9, 2), new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0), "Motif", horloge.MaintenantUtc);
        rdvRepo.RendezVousList.Add(rdv);

        var command = new AnnulerRendezVousCommand(rdvRepo, auto, journalisation, uow, horloge);
        await command.ExecuterAsync(new AnnulerRendezVousRequest(demandeurId, rdv.Id, "Empêchement"));

        Assert.Equal(StatutRendezVous.Annule, rdv.Statut);
        Assert.Single(journalisation.Evenements);
        Assert.Equal("RendezVousAnnule", journalisation.Evenements[0].Type);
    }

    [Fact]
    public async Task Reporter_CreneauDisponible_DeplaceRdvEtEmetEvenement()
    {
        var rdvRepo = new FakeRendezVousRepository();
        var journalisation = new FakeJournalisationService();
        var auto = new FakeAutorisationService();
        var uow = new FakeUniteDeTravail();
        var parametres = new ParametresRendezVous(CapaciteParCreneau: 5);
        var horloge = new FakeHorloge();
        var demandeurId = Guid.NewGuid();

        var rdv = RendezVous.Creer(Guid.NewGuid(), null, demandeurId, new DateOnly(2026, 9, 2), new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0), "Motif", horloge.MaintenantUtc);
        rdvRepo.RendezVousList.Add(rdv);

        var nouvelleDate = new DateOnly(2026, 9, 3);
        var nouvelleHeure = new TimeSpan(11, 0, 0);

        var command = new ReporterRendezVousCommand(
            rdvRepo, auto, new DirectExecuteurTransactionnel(), journalisation, uow, parametres, horloge);

        await command.ExecuterAsync(new ReporterRendezVousRequest(demandeurId, rdv.Id, nouvelleDate, nouvelleHeure));

        Assert.Equal(nouvelleDate, rdv.Date);
        Assert.Equal(nouvelleHeure, rdv.HeureDebut);
        Assert.Single(journalisation.Evenements);
    }

    [Fact]
    public async Task MarquerHonore_AgentHabilite_PasseAHonoreEtEmetEvenement()
    {
        var rdvRepo = new FakeRendezVousRepository();
        var journalisation = new FakeJournalisationService();
        var auto = new FakeAutorisationService();
        var uow = new FakeUniteDeTravail();
        var agentId = Guid.NewGuid();
        auto.Habilitations.Add((agentId, Permission.RendezVousGerer));
        var horloge = new FakeHorloge();

        var rdv = RendezVous.Creer(Guid.NewGuid(), null, Guid.NewGuid(), new DateOnly(2026, 9, 2), new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0), "Motif", horloge.MaintenantUtc);
        rdvRepo.RendezVousList.Add(rdv);

        var command = new MarquerRendezVousHonoreCommand(rdvRepo, auto, journalisation, uow, horloge);
        await command.ExecuterAsync(new MarquerHonoreRequest(agentId, rdv.Id, "Présence validée"));

        Assert.Equal(StatutRendezVous.Honore, rdv.Statut);
        Assert.Single(journalisation.Evenements);
        Assert.Equal("RendezVousHonore", journalisation.Evenements[0].Type);
    }
}
