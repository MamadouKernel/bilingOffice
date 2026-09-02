using System;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.CustomerComplaint;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.CustomerComplaint;

public sealed class FileReclamationsTests : BunitContext
{
    private static (PlateformeDbContext Db, Guid AgentId, Guid FileId, Guid TicketId) ConstruireBase()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var agentId = Guid.NewGuid();
        var socId = Guid.NewGuid();

        var file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        db.Files.Add(file);

        var dossier = Dossier.Creer("DOS-REC-01", socId, DateTime.UtcNow, Guid.NewGuid());
        var ticket = Ticket.Creer("REC-2026-0001", "CustomerComplaint", "Reclamation", file.Id, dossier.Id, DateTime.UtcNow);
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

        Services.AddSingleton(new ConsulterFileReclamationsQuery(ticketRepo, fileRepo, autorisation));
        Services.AddSingleton(new PrendreEnChargeCommand(ticketRepo, autorisation, journalisation, horloge, uow));
    }

    [Fact]
    public void AgentHabilite_AccedeALaFileEtVoitTickets()
    {
        var (db, agentId, fileId, ticketId) = ConstruireBase();

        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.ComplaintInstruire, fileId));
        db.SaveChanges();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent,
            Permissions = new[] { Permission.ComplaintInstruire }
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);
        EnregistrerServices(db);

        var cut = Render<FileReclamations>();

        Assert.Contains("File Réclamations — Support Opérationnel", cut.Markup);
        Assert.Contains("REC-2026-0001", cut.Markup);
        Assert.Contains("Prendre en charge", cut.Markup);
    }

    [Fact]
    public void AgentNonHabilite_AffichePermissionDenied()
    {
        var (db, agentId, _, _) = ConstruireBase();
        // Pas d'habilitation ComplaintInstruire

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

        var cut = Render<FileReclamations>();

        Assert.Contains("403", cut.Markup);
        Assert.Contains("Accès Refusé", cut.Markup);
    }
}
