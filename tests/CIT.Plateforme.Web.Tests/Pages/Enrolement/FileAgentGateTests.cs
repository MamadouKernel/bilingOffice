using System;
using System.Linq;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Enrolement;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T058-T059. C8-02, C8-10, mirroring FileReclamationsTests.cs.
public sealed class FileAgentGateTests : BunitContext
{
    public FileAgentGateTests() => LocalisationDeTest.Enregistrer(Services);

    private static (PlateformeDbContext Db, Guid AgentId, Guid FileId, Guid TicketId) ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new PlateformeDbContext(options);

        var agentId = Guid.NewGuid();
        var transporteurId = Guid.NewGuid();

        var file = FileDeTraitement.Creer("Enrolement", "Agent Gate");
        db.Files.Add(file);

        var dossier = Dossier.Creer("DOS-ENR-01", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, transporteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), DateTime.UtcNow);
        var ticket = Ticket.Creer("ENR-2026-0001", "Enrolement", "Enrolement", file.Id, dossier.Id, DateTime.UtcNow);
        ticket.DefinirInformationsEnrolement("Enrôlement chauffeur", "Camion frigorifique.");
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.SaveChanges();

        return (db, agentId, file.Id, ticket.Id);
    }

    private void EnregistrerServices(PlateformeDbContext db)
    {
        var fileRepo = new FileDeTraitementRepository(db);
        var ticketRepo = new TicketRepository(db);
        var autorisation = new AutorisationService(db);
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);

        Services.AddSingleton(new ConsulterFileAgentGateQuery(ticketRepo, fileRepo, autorisation));
        Services.AddScoped<CIT.Plateforme.Web.Services.AnnuaireClients>();
        Services.AddSingleton(new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow));
    }

    [Fact]
    public void AgentHabilite_AccedeALaFileEtVoitLesDossiers()
    {
        var (db, agentId, fileId, ticketId) = ConstruireBase();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.EnrolementInstruire, fileId));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent,
            Permissions = [Permission.EnrolementInstruire]
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<FileAgentGate>();

        Assert.Contains("File Agent Gate", cut.Markup);
        Assert.Contains("ENR-2026-0001", cut.Markup);
        Assert.Contains("Prendre en charge", cut.Markup);
    }

    [Fact]
    public void AgentNonHabilite_AffichePermissionDenied()
    {
        var (db, agentId, _, _) = ConstruireBase();
        // Aucune habilitation EnrolementInstruire attribuée.

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<FileAgentGate>();

        Assert.Contains("403", cut.Markup);
    }

    [Fact]
    public void PriseEnCharge_MetAJourLaFile()
    {
        var (db, agentId, fileId, ticketId) = ConstruireBase();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.EnrolementInstruire, fileId));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent,
            Permissions = [Permission.EnrolementInstruire]
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<FileAgentGate>();
        cut.Find("button.btn-outline-success").Click();

        cut.WaitForState(() => cut.Markup.Contains("pris en charge avec succès"), TimeSpan.FromSeconds(5));

        var ticketMisAJour = db.Tickets.Single(t => t.Id == ticketId);
        Assert.Equal(StatutTicket.EnCours, ticketMisAJour.Statut);
    }
}
