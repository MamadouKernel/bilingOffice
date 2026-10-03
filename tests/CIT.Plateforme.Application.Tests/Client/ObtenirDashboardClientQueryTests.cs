using CIT.Plateforme.Domain.Audit;
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
        TypeClient profil = TypeClient.Normal,
        bool droitSoumission = true)
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var socId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var soc = SocieteCliente.Soumettre(userId, "TRANS-AFRIQUE", profil, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        soc.Valider();
        db.Societes.Add(soc);

        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, estAdmin, droitSoumission);
        userSoc.ModifierDroits(droit, droitSoumission);
        db.UtilisateursSociete.Add(userSoc);

        var dossierRepo = new DossierRepository(db);
        var ticketRepo = new TicketRepository(db);
        var userSocRepo = new UtilisateurSocieteRepository(db);
        var socRepo = new SocieteClienteRepository(db);

        var query = new ObtenirDashboardClientQuery(dossierRepo, ticketRepo, socRepo, userSocRepo, new HistoriqueTicketRepository(db));

        return (db, query, socId, userId);
    }

    [Fact]
    public async Task AM02_CollaborateurAvecDroitSoumission_PeutSoumettre_estVrai()
    {
        var (db, query, socId, userId) = ConstruireContexte(droitSoumission: true);
        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(userId, socId);

        Assert.True(dashboard.PeutSoumettre);
    }

    [Fact]
    public async Task AM02_CollaborateurLimiteSansDroitSoumission_PeutSoumettre_estFaux()
    {
        // Collaborateur limité : rattachement actif mais droit de soumission retiré. Le raccourci
        // de soumission du tableau de bord doit disparaître — il mènerait à un refus serveur.
        var (db, query, socId, userId) = ConstruireContexte(droitSoumission: false);
        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(userId, socId);

        Assert.False(dashboard.PeutSoumettre);
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
    public async Task ACR01_La_carte_de_complement_reprend_les_elements_attendus_par_l_agent()
    {
        var (db, query, socId, userId) = ConstruireContexte();

        var dossier = Dossier.Creer("DOS-2026-002", socId, DateTime.UtcNow, userId);
        var ticket = Ticket.Creer("TCK-002", "BillingImport", "Import", Guid.NewGuid(), dossier.Id, DateTime.UtcNow, "BL-1002");
        var agentId = Guid.NewGuid();
        ticket.PrendreEnCharge(agentId, DateTime.UtcNow);
        ticket.MettreEnAttenteClient(DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.HistoriqueTickets.Add(EntreeHistorique.Creer(ticket.Id, "ComplementDemandeImport", "Copie lisible de la déclaration en douane", agentId, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(userId, socId);

        Assert.Contains("Copie lisible de la déclaration en douane", Assert.Single(dashboard.ActionsRequises).Description);
    }

    [Fact]
    public async Task ACR01_La_carte_reprend_aussi_le_motif_d_une_mise_en_attente_client()
    {
        var (db, query, socId, userId) = ConstruireContexte();

        var dossier = Dossier.Creer("DOS-2026-004", socId, DateTime.UtcNow, userId);
        var ticket = Ticket.Creer("TCK-004", "CustomerComplaint", "Reclamation", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        var agentId = Guid.NewGuid();
        ticket.PrendreEnCharge(agentId, DateTime.UtcNow);
        ticket.MettreEnAttenteClient(DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.HistoriqueTickets.Add(EntreeHistorique.Creer(ticket.Id, "TicketEnAttenteClient", "Merci de joindre la facture contestée", agentId, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var dashboard = await query.ExecuterAsync(userId, socId);

        Assert.Contains("Merci de joindre la facture contestée", Assert.Single(dashboard.ActionsRequises).Description);
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
