using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Web.Components.Pages.Guichet;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Guichet;

public sealed class MesRendezVousTests : BunitContext
{
    public MesRendezVousTests() => LocalisationDeTest.Enregistrer(Services);

    private sealed class FakeRendezVousRepository : IRendezVousRepository
    {
        public List<RendezVous> RendezVousList { get; } = [];
        public Task AjouterAsync(RendezVous rdv, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<RendezVous?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(RendezVousList.Find(r => r.Id == id));
        public Task<RendezVous?> ObtenirActifParTicketAsync(Guid ticketId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<RendezVous>> ListerParDemandeurAsync(Guid demandeurId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RendezVous>>(RendezVousList.FindAll(r => r.DemandeurId == demandeurId));
        public Task<IReadOnlyList<RendezVous>> ListerParDateAsync(DateOnly date, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CompterReservationsCreneauAsync(DateOnly date, TimeSpan heureDebut, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> PossedeRdvActifAsync(Guid ticketId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeTicketRepository : ITicketRepository
    {
        public Task<Domain.Tickets.Ticket?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Domain.Tickets.Ticket?>(null);
        public Task AjouterAsync(Domain.Tickets.Ticket ticket, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Domain.Tickets.Ticket?> ObtenirParNumeroAsync(string numero, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Domain.Tickets.Ticket>> ListerParDossierAsync(Guid dossierId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Domain.Tickets.Ticket>> ListerParDossiersAsync(IEnumerable<Guid> dossierIds, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Domain.Tickets.Ticket>> ListerParFileAsync(Guid fileId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Domain.Tickets.Ticket>> ListerParOperateurAsync(Guid operateurId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Domain.Tickets.Ticket>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Domain.Tickets.Ticket>> ListerTousAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Domain.Tickets.Ticket?> ObtenirTicketActifParNumeroBlAsync(string numeroBl, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Domain.Tickets.Ticket?> ObtenirTicketActifParNumeroBookingAsync(string numeroBooking, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeJournalisationService : IJournalisationService
    {
        public Task ConsignerHistoriqueAsync(Guid ticketId, string evenement, string? detail, Guid? auteurId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ConsignerAuditAsync(Guid? auteurId, string action, string entiteType, Guid entiteId, string? avant, string? apres, CancellationToken ct = default) => Task.CompletedTask;
        public Task PublierEvenementAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeAutorisationService : IAutorisationService
    {
        public Task<bool> EstAutoriseAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default) => Task.FromResult(true);
        public Task ExigerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeUniteDeTravail : IUniteDeTravail
    {
        public Task<int> EnregistrerAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    private sealed class FakeHorloge : IHorloge
    {
        public DateTime MaintenantUtc => DateTime.UtcNow;
    }

    private sealed class FakeExecuteurTransactionnel : IExecuteurTransactionnel
    {
        public Task<T> ExecuterAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default) => action(ct);
    }

    [Fact]
    public void SansRdv_AfficheEtatVide()
    {
        var clientId = Guid.NewGuid();
        var fakeContexte = new FakeContexteUtilisateurWeb
        {
            Contexte = new ContexteUtilisateurVue(
                clientId, "client@test.local", "Client Test", NatureUtilisateur.Client,
                true, null, null, null, false, null, null,
                [EspaceFonctionnel.Client], EspaceFonctionnel.Client, [], [], false)
        };

        var rdvRepo = new FakeRendezVousRepository();
        var ticketRepo = new FakeTicketRepository();

        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        Services.AddSingleton<IRendezVousRepository>(rdvRepo);
        Services.AddSingleton<ITicketRepository>(ticketRepo);
        Services.AddSingleton<IAutorisationService>(new FakeAutorisationService());
        Services.AddSingleton<IJournalisationService>(new FakeJournalisationService());
        Services.AddSingleton<IUniteDeTravail>(new FakeUniteDeTravail());
        Services.AddSingleton<IHorloge>(new FakeHorloge());
        Services.AddSingleton<IExecuteurTransactionnel>(new FakeExecuteurTransactionnel());
        Services.AddSingleton(new ParametresRendezVous(5, TimeSpan.FromMinutes(30), new TimeSpan(8, 0, 0), new TimeSpan(16, 30, 0), 50));

        Services.AddSingleton<ListerMesRendezVousQuery>();
        Services.AddSingleton<AnnulerRendezVousCommand>();
        Services.AddSingleton<ReporterRendezVousCommand>();
        Services.AddSingleton<ListerCreneauxDisponiblesQuery>();

        var cut = Render<MesRendezVous>();
        cut.WaitForState(() => cut.Markup.Contains("aucun rendez-vous"), TimeSpan.FromSeconds(5));
        Assert.Contains("Vous n'avez aucun rendez-vous guichet enregistré", cut.Markup);
    }
}
