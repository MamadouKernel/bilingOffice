using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Agent;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Components.Pages.Agent;
using CIT.Plateforme.Web.Services;
using CIT.Plateforme.Web.Tests.Components.Layout;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Pages.Agent;

public sealed class DashboardAgentTests : BunitContext
{
    [Fact]
    public void DashboardAgent_RenduComplet_FilesEtTickets()
    {
        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var fakeContexte = new FakeContexteUtilisateurWeb();
        fakeContexte.Contexte = fakeContexte.Contexte with
        {
            UtilisateurId = agentId,
            NomAffiche = "Opérateur Test",
            Nature = NatureUtilisateur.Agent,
            EstAuthentifie = true,
            EspaceActif = EspaceFonctionnel.Agent
        };
        Services.AddSingleton<IContexteUtilisateurWeb>(fakeContexte);

        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var user = new ApplicationUser
        {
            Id = agentId,
            UserName = "agent@cit.ci",
            NomAffiche = "Opérateur Test",
            Nature = NatureUtilisateur.Agent,
            EstActif = true
        };
        db.Users.Add(user);

        var file = FileDeTraitement.Creer("IMPORT", "Pilote Manifeste");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var hab = HabilitationUtilisateur.Attribuer(agentId, Permission.TicketPrendreEnCharge, fileId);
        db.Habilitations.Add(hab);
        db.SaveChanges();

        var fileRepo = new FileDeTraitementRepository(db);
        var ticketRepo = new TicketRepository(db);
        var habRepo = new HabilitationRepository(db);
        var dispoRepo = new DisponibiliteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var query = new ObtenirDashboardAgentQuery(fileRepo, ticketRepo, habRepo, dispoRepo, userInfoRepo);
        Services.AddSingleton(query);

        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var autorisation = new AutorisationService(db);
        var dispoCommand = new GererDisponibiliteCommand(dispoRepo, autorisation, journalisation, horloge, uow);
        Services.AddSingleton(dispoCommand);

        var cut = Render<DashboardAgent>();

        Assert.Contains("Tableau de bord Opérateur", cut.Markup);
        Assert.Contains("Opérateur Test", cut.Markup);
        Assert.Contains("Pilote Manifeste", cut.Markup);
        Assert.Contains("/billing-import/manifeste", cut.Markup);
    }
}
