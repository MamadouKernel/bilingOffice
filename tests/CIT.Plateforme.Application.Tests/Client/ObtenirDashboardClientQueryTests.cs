using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Client;

public sealed class ObtenirDashboardClientQueryTests
{
    private static (PlateformeDbContext Db, ObtenirDashboardClientQuery Query, Guid SocieteId, Guid UtilisateurId) ConstruireContexte(
        DroitConsultation droit = DroitConsultation.PropresDemandesUniquement,
        bool estAdmin = false,
        TypeClient profil = TypeClient.Normal)
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var socId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(userId, "TRANS-AFRIQUE", profil, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, estAdmin, true);
        userSoc.ModifierDroits(droit, true);
        db.UtilisateursSociete.Add(userSoc);

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var socRepo = new SocieteClienteRepository(db);

        var query = new ObtenirDashboardClientQuery(dossierRepo, ticketRepo, socRepo, userSocRepo);

        return (db, query, socId, userId);
    }

    [Fact]
    public async Task ACR01_TicketEnAttenteClient_GenereActionComplementManifeste()
    {
        var (db, query, socId, userId) = ConstruireContexte();

        var dossier = Dossier.Creer("DOS-2026-001", socId, DateTime.UtcNow, userId);
        var fileId = Guid.NewGuid();
        var ticket = Ticket.Creer("TCK-001", "IMPORT", "STANDARD", fileId, dossier.Id, DateTime.UtcNow, "BL-1001");
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.MettreEnAttenteClient(DateTime.UtcNow);

        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(userId, socId);

        Assert.Single(dashboard.ActionsRequises);
        var action = dashboard.ActionsRequises[0];
        Assert.Equal(ticket.Id, action.TicketId);
        Assert.Equal(TypeActionRequise.ComplementManifeste, action.TypeAction);
        Assert.Equal("BL-1001", action.NumeroBl);
        Assert.Contains("BL-1001", action.Titre);
        Assert.Equal($"/tickets/{ticket.Id}#complement", action.UrlCible);
    }

    [Fact]
    public async Task ACR01_TicketSansActionBloquante_ActionsRequisesEstVide()
    {
        var (db, query, socId, userId) = ConstruireContexte();

        var dossier = Dossier.Creer("DOS-2026-003", socId, DateTime.UtcNow, userId);
        var ticket = Ticket.Creer("TCK-003", "IMPORT", "STANDARD", Guid.NewGuid(), dossier.Id, DateTime.UtcNow, "BL-1003");
        // Statut EnAttenteDePriseEnCharge (action côté CIT, pas côté client)

        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(userId, socId);

        Assert.Empty(dashboard.ActionsRequises);
        Assert.Single(dashboard.DemandesRecentes);
        Assert.Equal(1, dashboard.TotalEnAttenteCit);
    }

    [Fact]
    public async Task Scope_PropresDemandes_MasqueDemandesDUnAutreCollaborateur()
    {
        var (db, query, socId, userId) = ConstruireContexte(droit: DroitConsultation.PropresDemandesUniquement);

        var autreUtilisateurId = Guid.NewGuid();
        var dossierPerso = Dossier.Creer("DOS-MINE", socId, DateTime.UtcNow, userId);
        var dossierAutre = Dossier.Creer("DOS-OTHER", socId, DateTime.UtcNow, autreUtilisateurId);

        db.Dossiers.AddRange(dossierPerso, dossierAutre);
        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(userId, socId);

        Assert.Single(dashboard.DemandesRecentes);
        Assert.Equal("DOS-MINE", dashboard.DemandesRecentes[0].NumeroDossier);
    }

    [Fact]
    public async Task Scope_ToutesDemandesSociete_AfficheDemandesDeTousLesCollaborateurs()
    {
        var (db, query, socId, userId) = ConstruireContexte(droit: DroitConsultation.ToutesDemandesSociete);

        var autreUtilisateurId = Guid.NewGuid();
        var dossierPerso = Dossier.Creer("DOS-MINE", socId, DateTime.UtcNow, userId);
        var dossierAutre = Dossier.Creer("DOS-OTHER", socId, DateTime.UtcNow, autreUtilisateurId);

        db.Dossiers.AddRange(dossierPerso, dossierAutre);
        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(userId, socId);

        Assert.Equal(2, dashboard.DemandesRecentes.Count);
    }

    [Fact]
    public async Task Securite_AccesSocieteEtrangere_LeveAccesRefuseException()
    {
        var (db, query, _, userId) = ConstruireContexte();
        var autreSocieteId = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() => query.ExecuterAsync(userId, autreSocieteId));
    }
}
