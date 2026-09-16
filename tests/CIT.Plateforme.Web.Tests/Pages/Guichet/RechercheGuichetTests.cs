using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Web.Components.Pages.Guichet;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Guichet;

public sealed class RechercheGuichetTests : BunitContext
{
    public RechercheGuichetTests() => LocalisationDeTest.Enregistrer(Services);

    private sealed class FakeRechercheGuichetRepository : IRechercheGuichetRepository
    {
        public List<ResultatRechercheGuichetDto> Resultats { get; } = [];
        public Task<IReadOnlyList<ResultatRechercheGuichetDto>> RechercherAsync(CriteresRechercheGuichet criteres, IEnumerable<Guid>? filesAutorisees = null, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ResultatRechercheGuichetDto>>(Resultats);
    }

    private sealed class FakeTicketRepository : ITicketRepository
    {
        public Task<Ticket?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Ticket?>(null);
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
        public Task<Dossier?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Dossier?>(null);
        public Task AjouterAsync(Dossier dossier, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Dossier>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Dossier>> ListerParClientAsync(Guid clientId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeDocumentRepository : IDocumentRepository
    {
        public Task<IReadOnlyList<Document>> ListerParTicketAsync(Guid ticketId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Document>>([]);
        public Task<Document?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<Document?>(null);
        public Task AjouterAsync(Document document, CancellationToken ct = default) => throw new NotImplementedException();
        public Task SupprimerAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeRendezVousRepository : IRendezVousRepository
    {
        public Task AjouterAsync(RendezVous rdv, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<RendezVous?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<RendezVous?> ObtenirActifParTicketAsync(Guid ticketId, CancellationToken ct = default) => Task.FromResult<RendezVous?>(null);
        public Task<IReadOnlyList<RendezVous>> ListerParDemandeurAsync(Guid demandeurId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<RendezVous>> ListerParDateAsync(DateOnly date, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CompterReservationsCreneauAsync(DateOnly date, TimeSpan heureDebut, CancellationToken ct = default) => Task.FromResult(0);
        public Task<bool> PossedeRdvActifAsync(Guid ticketId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeUtilisateurInfoRepository : IUtilisateurInfoRepository
    {
        public Task<UtilisateurInfo?> ObtenirInfoAsync(Guid utilisateurId, CancellationToken ct = default) => Task.FromResult<UtilisateurInfo?>(null);
        public Task<IReadOnlyDictionary<Guid, UtilisateurInfo>> ObtenirInfosAsync(IEnumerable<Guid> utilisateursIds, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UtilisateurInfo?> ObtenirParEmailAsync(string email, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ProfilUtilisateur?> ObtenirProfilAsync(Guid utilisateurId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> MettreAJourProfilAsync(Guid utilisateurId, string nomAffiche, string? telephone, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeAutorisationService : IAutorisationService
    {
        public bool Autorise { get; set; } = true;
        public Task<bool> EstAutoriseAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
            => Task.FromResult(Autorise);
        public Task ExigerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
        {
            if (!Autorise) throw new AccesRefuseException("Accès refusé");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeJournalisationService : IJournalisationService
    {
        public Task ConsignerHistoriqueAsync(Guid ticketId, string evenement, string? detail, Guid? auteurId, CancellationToken ct = default) => Task.CompletedTask;
        public Task ConsignerAuditAsync(Guid? auteurId, string action, string entiteType, Guid entiteId, string? avant, string? apres, CancellationToken ct = default) => Task.CompletedTask;
        public Task PublierEvenementAsync(string typeEvenement, string chargeUtile, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeUniteDeTravail : IUniteDeTravail
    {
        public Task<int> EnregistrerAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    private sealed class FakeHorloge : IHorloge
    {
        public DateTime MaintenantUtc => DateTime.UtcNow;
    }

    private sealed class FakeStockageDocuments : IStockageDocuments
    {
        public Task<string> EnregistrerAsync(System.IO.Stream contenu, string nomFichier, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<System.IO.Stream> LireAsync(string cleStockage, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeHabilitationRepository : IHabilitationRepository
    {
        public Task AjouterAsync(HabilitationUtilisateur habilitation, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<HabilitationUtilisateur>> ListerParUtilisateurAsync(Guid utilisateurId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HabilitationUtilisateur>>([]);
        public Task RetirerAsync(Guid utilisateurId, string permissionCode, Guid? fileId = null, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeUtilisateurSocieteRepository : IUtilisateurSocieteRepository
    {
        public Task AjouterAsync(UtilisateurSociete rattachement, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UtilisateurSociete?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<UtilisateurSociete?>(null);
        public Task<UtilisateurSociete?> ObtenirParUtilisateurAsync(Guid utilisateurId, CancellationToken ct = default) => Task.FromResult<UtilisateurSociete?>(null);
        public Task<IReadOnlyList<UtilisateurSociete>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakePerimetreSupervisionRepository : IPerimetreSupervisionRepository
    {
        public Task AjouterAsync(PerimetreSupervision perimetre, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PerimetreSupervision?> ObtenirParSuperviseurAsync(Guid superviseurId, CancellationToken ct = default) => Task.FromResult<PerimetreSupervision?>(null);
        public Task<IReadOnlyList<PerimetreSupervision>> ListerAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PerimetreSupervision>>([]);
    }

    private void ConfigurerServices(IContexteUtilisateurWeb contexte, FakeRechercheGuichetRepository repo, FakeAutorisationService auto)
    {
        Services.AddSingleton(contexte);
        Services.AddSingleton<IRechercheGuichetRepository>(repo);
        Services.AddSingleton<ITicketRepository>(new FakeTicketRepository());
        Services.AddSingleton<IDossierRepository>(new FakeDossierRepository());
        Services.AddSingleton<IDocumentRepository>(new FakeDocumentRepository());
        Services.AddSingleton<IRendezVousRepository>(new FakeRendezVousRepository());
        Services.AddSingleton<IUtilisateurInfoRepository>(new FakeUtilisateurInfoRepository());
        Services.AddSingleton<IAutorisationService>(auto);
        Services.AddSingleton<IJournalisationService>(new FakeJournalisationService());
        Services.AddSingleton<IUniteDeTravail>(new FakeUniteDeTravail());
        Services.AddSingleton<IHorloge>(new FakeHorloge());

        // SEC-12 — RechercheGuichet injecte désormais TelechargerDocumentQuery (téléchargement de
        // document depuis le dossier Guichet, avec contrôle d'habilitation). Chaîne de dépendances
        // reconstruite ici avec des doublures inertes : ces deux tests n'exercent jamais le
        // téléchargement lui-même, seulement le rendu de l'écran.
        Services.AddSingleton<IStockageDocuments>(new FakeStockageDocuments());
        Services.AddSingleton<IHabilitationRepository>(new FakeHabilitationRepository());
        Services.AddSingleton<IUtilisateurSocieteRepository>(new FakeUtilisateurSocieteRepository());
        Services.AddSingleton<IPerimetreSupervisionRepository>(new FakePerimetreSupervisionRepository());
        Services.AddSingleton<GardeIdentiteService>();
        Services.AddSingleton<ControleAccesRessourceService>();
        Services.AddSingleton<CloisonnementService>();
        Services.AddSingleton<TelechargerDocumentQuery>();

        Services.AddSingleton<RechercherGuichetQuery>();
        Services.AddSingleton<ConsulterDossierGuichetQuery>();
        Services.AddSingleton<RegulariserModeDegradeCommand>();
    }

    [Fact]
    public void NonHabilite_AfficheMessageAccesRefuse()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb
        {
            Contexte = new ContexteUtilisateurVue(
                Guid.NewGuid(), "agent.sansdroit@test.local", "Agent Sans Droit", NatureUtilisateur.Agent,
                false, null, null, null, false, null, null,
                [EspaceFonctionnel.Agent], EspaceFonctionnel.Agent, [], [], false)
        };

        ConfigurerServices(fakeContexte, new FakeRechercheGuichetRepository(), new FakeAutorisationService { Autorise = false });

        var cut = Render<RechercheGuichet>();
        cut.WaitForState(() => cut.Markup.Contains("Accès refusé"), TimeSpan.FromSeconds(5));
        Assert.Contains("GuichetRecherche", cut.Markup);
    }

    [Fact]
    public void Habilite_AfficheZoneRechercheMulticritere()
    {
        var fakeContexte = new FakeContexteUtilisateurWeb
        {
            Contexte = new ContexteUtilisateurVue(
                Guid.NewGuid(), "agent.billing@test.local", "Agent Billing", NatureUtilisateur.Agent,
                true, null, null, null, false, null, null,
                [EspaceFonctionnel.Agent], EspaceFonctionnel.Agent,
                [Permission.GuichetRecherche, Permission.RendezVousGerer], [], false)
        };

        var fakeRepo = new FakeRechercheGuichetRepository();
        fakeRepo.Resultats.Add(new ResultatRechercheGuichetDto(
            Guid.NewGuid(), "CIT-IMP-2026-0001", "DOS-001", "BillingImport", "SOCOCIM CI", "COSU123456", "Valide", "EnAttenteDePriseEnCharge", DateTime.UtcNow, false));

        ConfigurerServices(fakeContexte, fakeRepo, new FakeAutorisationService { Autorise = true });

        var cut = Render<RechercheGuichet>();
        cut.WaitForState(() => cut.Markup.Contains("Recherche Multicritère Rapide"), TimeSpan.FromSeconds(5));
        Assert.Contains("Recherche Multicritère Rapide", cut.Markup);
    }
}
