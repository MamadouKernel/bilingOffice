using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Web;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Web;

public sealed class ObtenirContexteUtilisateurQueryTests
{
    private static (PlateformeDbContext DbContext, ObtenirContexteUtilisateurQuery Query) ConstruireContexte()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var query = new ObtenirContexteUtilisateurQuery(
            new UtilisateurInfoRepository(db),
            new UtilisateurSocieteRepository(db),
            new SocieteClienteRepository(db),
            new HabilitationRepository(db),
            new PerimetreSupervisionRepository(db));
        return (db, query);
    }

    [Fact]
    public async Task UtilisateurInconnu_RetourneContexteNonAuthentifie()
    {
        var (_, query) = ConstruireContexte();
        var userId = Guid.NewGuid();

        var ctx = await query.ExecuterAsync(userId);

        Assert.False(ctx.EstAuthentifie);
        Assert.Equal(userId, ctx.UtilisateurId);
        Assert.Empty(ctx.EspacesDisponibles);
    }

    [Fact]
    public async Task UtilisateurDesactive_RetourneContexteNonAuthentifie()
    {
        var (db, query) = ConstruireContexte();
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "test@test.ci",
            Email = "test@test.ci",
            NomAffiche = "Test User",
            EstActif = false,
            Nature = NatureUtilisateur.Client
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var ctx = await query.ExecuterAsync(userId);

        Assert.False(ctx.EstAuthentifie);
    }

    [Fact]
    public async Task ClientStandard_RetourneEspaceClientEtSociete()
    {
        var (db, query) = ConstruireContexte();
        var userId = Guid.NewGuid();
        var socId = Guid.NewGuid();

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "client@trans.ci",
            Email = "client@trans.ci",
            NomAffiche = "Client Trans",
            EstActif = true,
            Nature = NatureUtilisateur.Client
        };
        db.Users.Add(user);

        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);

        var soc = SocieteCliente.Soumettre(userId, "TRANS-AFRIQUE", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        db.Societes.Add(soc);

        await db.SaveChangesAsync();

        var ctx = await query.ExecuterAsync(userId);

        Assert.True(ctx.EstAuthentifie);
        Assert.Equal(socId, ctx.SocieteId);
        Assert.Equal("TRANS-AFRIQUE", ctx.SocieteNom);
        Assert.Equal(TypeClient.Normal, ctx.SocieteProfil);
        Assert.False(ctx.EstAdministrateurSociete);
        Assert.Contains(EspaceFonctionnel.Client, ctx.EspacesDisponibles);
        Assert.DoesNotContain(EspaceFonctionnel.AdministrateurSociete, ctx.EspacesDisponibles);
        Assert.Equal(EspaceFonctionnel.Client, ctx.EspaceActif);
    }

    [Fact]
    public async Task AdminSociete_RetourneEspacesClientEtAdminSociete_EspaceActifAdmin()
    {
        var (db, query) = ConstruireContexte();
        var userId = Guid.NewGuid();
        var socId = Guid.NewGuid();

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "admin@trans.ci",
            Email = "admin@trans.ci",
            NomAffiche = "Admin Trans",
            EstActif = true,
            Nature = NatureUtilisateur.Client
        };
        db.Users.Add(user);

        var userSoc = UtilisateurSociete.Rattacher(userId, socId, DateTime.UtcNow, true, true);
        userSoc.ModifierDroits(DroitConsultation.ToutesDemandesSociete, true);
        db.UtilisateursSociete.Add(userSoc);

        var soc = SocieteCliente.Soumettre(userId, "TRANS-AFRIQUE", TypeClient.EnCompte, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, socId);
        db.Societes.Add(soc);

        await db.SaveChangesAsync();

        var ctx = await query.ExecuterAsync(userId);

        Assert.True(ctx.EstAuthentifie);
        Assert.True(ctx.EstAdministrateurSociete);
        Assert.Contains(EspaceFonctionnel.AdministrateurSociete, ctx.EspacesDisponibles);
        Assert.Contains(EspaceFonctionnel.Client, ctx.EspacesDisponibles);
        Assert.Equal(EspaceFonctionnel.AdministrateurSociete, ctx.EspaceActif);
    }

    [Fact]
    public async Task AgentCit_RetourneEspaceAgent_AvecFilesHabilitees()
    {
        var (db, query) = ConstruireContexte();
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "agent@cit.ci",
            Email = "agent@cit.ci",
            NomAffiche = "Agent CIT",
            EstActif = true,
            Nature = NatureUtilisateur.Agent
        };
        db.Users.Add(user);

        var hab = HabilitationUtilisateur.Attribuer(userId, Permission.TicketPrendreEnCharge, fileId);
        db.Habilitations.Add(hab);

        await db.SaveChangesAsync();

        var ctx = await query.ExecuterAsync(userId);

        Assert.True(ctx.EstAuthentifie);
        Assert.Contains(EspaceFonctionnel.Agent, ctx.EspacesDisponibles);
        Assert.Equal(EspaceFonctionnel.Agent, ctx.EspaceActif);
        Assert.Contains(fileId, ctx.FilesHabilitees);
        Assert.Contains(Permission.TicketPrendreEnCharge, ctx.Permissions);
    }

    [Fact]
    public async Task SuperviseurEtAgent_PrioriseEspaceSupervision_MultiEspaces()
    {
        var (db, query) = ConstruireContexte();
        var userId = Guid.NewGuid();

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "sup@cit.ci",
            Email = "sup@cit.ci",
            NomAffiche = "Superviseur CIT",
            EstActif = true,
            Nature = NatureUtilisateur.Agent
        };
        db.Users.Add(user);

        var hab = HabilitationUtilisateur.Attribuer(userId, Permission.TicketPrendreEnCharge, Guid.NewGuid());
        db.Habilitations.Add(hab);

        var perim = PerimetreSupervision.Definir(userId, new List<Guid> { Guid.NewGuid() });
        db.PerimetresSupervision.Add(perim);

        await db.SaveChangesAsync();

        var ctx = await query.ExecuterAsync(userId);

        Assert.True(ctx.EstAuthentifie);
        Assert.True(ctx.EstSuperviseur);
        Assert.Contains(EspaceFonctionnel.Supervision, ctx.EspacesDisponibles);
        Assert.Contains(EspaceFonctionnel.Agent, ctx.EspacesDisponibles);
        Assert.Equal(EspaceFonctionnel.Supervision, ctx.EspaceActif);
    }
}
