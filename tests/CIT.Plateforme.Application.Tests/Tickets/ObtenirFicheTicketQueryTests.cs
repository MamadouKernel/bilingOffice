using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

public sealed class ObtenirFicheTicketQueryTests
{
    private static (PlateformeDbContext Db, ObtenirFicheTicketQuery Query, Guid SocId, Guid ClientUserId, Guid AgentUserId) ConstruireContexte()
    {
        var options = new DbContextOptionsBuilder<PlateformeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new PlateformeDbContext(options);

        var socId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();
        var agentUserId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(clientUserId, "SOCIETE CIV", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var userClient = new ApplicationUser
        {
            Id = clientUserId,
            UserName = "client@civ.ci",
            NomAffiche = "Client Civ",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        var userAgent = new ApplicationUser
        {
            Id = agentUserId,
            UserName = "agent@cit.ci",
            NomAffiche = "Agent CIT",
            Nature = NatureUtilisateur.Agent,
            EstActif = true
        };
        db.Users.AddRange(userClient, userAgent);

        var userSoc = UtilisateurSociete.Rattacher(clientUserId, socId, DateTime.UtcNow, true, true);
        db.UtilisateursSociete.Add(userSoc);

        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var socRepo = new SocieteClienteRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var userInfoRepo = new UtilisateurInfoRepository(db);
        var histRepo = new HistoriqueTicketRepository(db);
        var docRepo = new DocumentRepository(db);
        var msgRepo = new MessageRepository(db);
        var habRepo = new HabilitationRepository(db);

        var query = new ObtenirFicheTicketQuery(
            ticketRepo, dossierRepo, fileRepo, socRepo, userSocRepo,
            userInfoRepo, histRepo, docRepo, msgRepo, habRepo);

        return (db, query, socId, clientUserId, agentUserId);
    }

    [Fact]
    public async Task FicheTicket_ClientAutorise_RetourneSyntheseEtMessagesClients()
    {
        var (db, query, socId, clientUserId, agentUserId) = ConstruireContexte();

        var fileId = Guid.NewGuid();
        var file = FileDeTraitement.Creer("IMPORT", "Pilote Manifeste");
        typeof(FileDeTraitement).GetProperty("Id")!.SetValue(file, fileId);
        db.Files.Add(file);

        var dossier = Dossier.Creer("DOS-2026-10", socId, DateTime.UtcNow, clientUserId);
        var ticket = Ticket.Creer("TCK-2026-10", "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-10");
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);

        // Ajouter une entrée historique
        var hist = EntreeHistorique.Creer(ticket.Id, "Creation", "Ticket créé avec succès", clientUserId, DateTime.UtcNow);
        db.HistoriqueTickets.Add(hist);

        // Ajouter messages (1 visible client, 1 interne)
        var msgPublic = Message.Publier(ticket.Id, clientUserId, TypeMessage.Externe, "Bonjour, voici mon BL", DateTime.UtcNow);
        var msgInterne = Message.Publier(ticket.Id, agentUserId, TypeMessage.NoteInterne, "Note interne agent", DateTime.UtcNow);
        db.Messages.AddRange(msgPublic, msgInterne);

        await db.SaveChangesAsync();

        var fiche = await query.ExecuterAsync(clientUserId, ticket.Id);

        Assert.Equal(ticket.Id, fiche.TicketId);
        Assert.Equal("TCK-2026-10", fiche.NumeroTicket);
        Assert.Equal("BL-10", fiche.NumeroBl);
        Assert.Equal("Pilote Manifeste", fiche.FileNom);
        Assert.Single(fiche.Chronologie);
        Assert.Single(fiche.Messages); // Uniquement le message public pour le client !
        Assert.Equal("Bonjour, voici mon BL", fiche.Messages[0].Contenu);
    }

    [Fact]
    public async Task FicheTicket_ClientSocieteEtrangere_LeveAccesRefuseException()
    {
        var (db, query, _, clientUserId, _) = ConstruireContexte();

        var autreSocId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var dossierAutre = Dossier.Creer("DOS-AUTRE", autreSocId, DateTime.UtcNow, Guid.NewGuid());
        var ticketAutre = Ticket.Creer("TCK-AUTRE", "IMPORT", "STANDARD", fileId, dossierAutre.Id, DateTime.UtcNow);
        var socAutre = SocieteCliente.Soumettre(Guid.NewGuid(), "AUTRE SOC", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(socAutre, autreSocId);
        socAutre.Valider();

        db.Societes.Add(socAutre);
        db.Dossiers.Add(dossierAutre);
        db.Tickets.Add(ticketAutre);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() => query.ExecuterAsync(clientUserId, ticketAutre.Id));
    }
}
