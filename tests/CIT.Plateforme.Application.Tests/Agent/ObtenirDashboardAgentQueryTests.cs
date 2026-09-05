using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Agent;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Agent;

public sealed class ObtenirDashboardAgentQueryTests
{
    private static (PlateformeDbContext Db, ObtenirDashboardAgentQuery Query) ConstruireContexte()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var fileRepo = new FileDeTraitementRepository(db);
        var ticketRepo = new TicketRepository(db);
        var habRepo = new HabilitationRepository(db);
        var dispoRepo = new DisponibiliteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);

        var query = new ObtenirDashboardAgentQuery(fileRepo, ticketRepo, habRepo, dispoRepo,
            new CIT.Plateforme.Application.Securite.GardeIdentiteService(new UtilisateurInfoRepository(db)));
        return (db, query);
    }

    [Fact]
    public async Task AgentSansFilesNiTickets_RetourneDashboardVideMaisValide()
    {
        var (db, query) = ConstruireContexte();
        var agentId = Guid.NewGuid();

        var user = new ApplicationUser
        {
            Id = agentId,
            UserName = "agent@cit.ci",
            NomAffiche = "Agent CIT",
            Nature = NatureUtilisateur.Agent,
            EstActif = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(agentId);

        Assert.Equal(agentId, dashboard.AgentId);
        Assert.Equal("Agent CIT", dashboard.AgentNom);
        Assert.True(dashboard.EstDisponible);
        Assert.Empty(dashboard.FilesHabilitees);
        Assert.Empty(dashboard.MesTicketsEnCours);
        Assert.Equal(0, dashboard.TotalTicketsEnCoursAgent);
    }

    [Fact]
    public async Task AgentAvecHabilitationsEtTickets_RetourneStatsEtMesTickets()
    {
        var (db, query) = ConstruireContexte();
        var agentId = Guid.NewGuid();
        var socId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var user = new ApplicationUser
        {
            Id = agentId,
            UserName = "agent@cit.ci",
            NomAffiche = "Opérateur Import",
            Nature = NatureUtilisateur.Agent,
            EstActif = true
        };
        db.Users.Add(user);

        var file = FileDeTraitement.Creer("IMPORT", "Pilote Manifeste");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var hab = HabilitationUtilisateur.Attribuer(agentId, Permission.TicketPrendreEnCharge, fileId);
        db.Habilitations.Add(hab);

        var soc = SocieteCliente.Soumettre(Guid.NewGuid(), "TRANS-AFRIQUE", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        db.Societes.Add(soc);

        var dossier = Dossier.Creer("DOS-1", socId, DateTime.UtcNow, Guid.NewGuid());
        db.Dossiers.Add(dossier);

        // Ticket 1 : pris en charge par l'agent
        var ticket1 = Ticket.Creer("TCK-1", "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-1");
        ticket1.PrendreEnCharge(agentId, DateTime.UtcNow);
        db.Tickets.Add(ticket1);

        // Ticket 2 : en attente dans la file
        var ticket2 = Ticket.Creer("TCK-2", "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-2");
        db.Tickets.Add(ticket2);

        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(agentId);

        Assert.Single(dashboard.FilesHabilitees);
        Assert.Equal("Pilote Manifeste", dashboard.FilesHabilitees[0].Nom);
        Assert.Equal(1, dashboard.FilesHabilitees[0].TicketsEnAttente);
        Assert.Equal(1, dashboard.FilesHabilitees[0].TicketsEnCours);

        Assert.Single(dashboard.MesTicketsEnCours);
        Assert.Equal("TCK-1", dashboard.MesTicketsEnCours[0].NumeroTicket);
        Assert.Equal("BL-1", dashboard.MesTicketsEnCours[0].NumeroBl);
        Assert.Equal(1, dashboard.TotalTicketsEnCoursAgent);
        Assert.Equal(1, dashboard.TotalTicketsEnAttenteDansPerimetre);
    }
}
