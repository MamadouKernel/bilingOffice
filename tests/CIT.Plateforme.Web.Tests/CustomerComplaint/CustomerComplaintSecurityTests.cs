using System;
using System.Net;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.CustomerComplaint;

/// <summary>
/// Preuves de sécurité REC-SEC-01 à REC-SEC-06 pour Feature 006 (Customer Complaint).
/// </summary>
public sealed class CustomerComplaintSecurityTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory = factory;

    [Fact]
    public async Task REC_SEC_01_Anonyme_RedirigeVersLoginAvecReturnUrl()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/reclamations/soumettre");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/comptes/connexion", response.Headers.Location.ToString());
        Assert.Contains("ReturnUrl", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task REC_SEC_02_ClientSansPermission_RefusSoumission()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var soumettreCmd = scope.ServiceProvider.GetRequiredService<SoumettreReclamationCommand>();

        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        var clientUser = new ApplicationUser
        {
            Id = clientId,
            UserName = $"client_noperm_{clientId}@test.local",
            Email = $"client_noperm_{clientId}@test.local",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(clientUser);

        var soc = SocieteCliente.Soumettre(clientId, "SOC-NO-PERM", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);

        var userSoc = UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true);
        db.UtilisateursSociete.Add(userSoc);

        var file = await db.Files.FirstOrDefaultAsync(f => f.Nom == "Support Opérationnel");
        if (file is null)
        {
            file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
            db.Files.Add(file);
        }

        var cat = CategorieReclamation.Creer($"SEC2_{Guid.NewGuid():N}"[..10], "Général", file.Id);
        db.Set<CategorieReclamation>().Add(cat);
        // AUCUNE habilitation ComplaintSoumettre attribuée
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            soumettreCmd.ExecuterAsync(
                clientId,
                new SoumettreReclamationRequete(societeId, cat.Id, "Test objet", "Test description")));
    }

    [Fact]
    public async Task REC_SEC_03_SocieteA_NePeutPasConsulterReclamationSocieteB()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var ficheQuery = scope.ServiceProvider.GetRequiredService<ObtenirFicheTicketQuery>();

        var clientAId = Guid.NewGuid();
        var societeAId = Guid.NewGuid();
        var userA = new ApplicationUser
        {
            Id = clientAId,
            UserName = $"clientA_{clientAId}@test.local",
            Email = $"clientA_{clientAId}@test.local",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(userA);

        var socA = SocieteCliente.Soumettre(clientAId, "SOC-A", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(socA, societeAId);
        socA.Valider();
        db.Societes.Add(socA);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(clientAId, societeAId, DateTime.UtcNow, false, true));

        var clientBId = Guid.NewGuid();
        var societeBId = Guid.NewGuid();
        var userB = new ApplicationUser
        {
            Id = clientBId,
            UserName = $"clientB_{clientBId}@test.local",
            Email = $"clientB_{clientBId}@test.local",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(userB);

        var socB = SocieteCliente.Soumettre(clientBId, "SOC-B", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(socB, societeBId);
        socB.Valider();
        db.Societes.Add(socB);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(clientBId, societeBId, DateTime.UtcNow, false, true));

        var file = await db.Files.FirstOrDefaultAsync(f => f.Nom == "Support Opérationnel");
        if (file is null)
        {
            file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
            db.Files.Add(file);
        }

        var dossierB = Dossier.Creer("DOS-B", societeBId, DateTime.UtcNow, clientBId);
        var ticketB = Ticket.Creer("REC-B-01", "CustomerComplaint", "Reclamation", file.Id, dossierB.Id, DateTime.UtcNow);
        db.Dossiers.Add(dossierB);
        db.Tickets.Add(ticketB);
        await db.SaveChangesAsync();

        // Client A tente de consulter le ticket de Société B -> Refus d'accès
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            ficheQuery.ExecuterAsync(clientAId, ticketB.Id));
    }

    [Fact]
    public async Task REC_SEC_04_AgentSansComplaintInstruire_NePeutPasConsulterLaFile()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var consulterFileQuery = scope.ServiceProvider.GetRequiredService<ConsulterFileQuery>();

        var agentId = Guid.NewGuid();
        var agentUser = new ApplicationUser
        {
            Id = agentId,
            UserName = $"agent_{agentId}@test.local",
            Email = $"agent_{agentId}@test.local",
            Nature = NatureUtilisateur.Agent,
            EstActif = true
        };
        db.Users.Add(agentUser);

        var file = await db.Files.FirstOrDefaultAsync(f => f.Nom == "Support Opérationnel");
        if (file is null)
        {
            file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
            db.Files.Add(file);
        }
        // Agent sans habilitation sur cette file
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            consulterFileQuery.ExecuterAsync(agentId, file.Id));
    }

    [Fact]
    public async Task REC_SEC_05_NoteInterne_NonVisibleParLeClient()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var publierMessageCmd = scope.ServiceProvider.GetRequiredService<PublierMessageCommand>();
        var ficheQuery = scope.ServiceProvider.GetRequiredService<ObtenirFicheTicketQuery>();

        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var clientUser = new ApplicationUser
        {
            Id = clientId,
            UserName = $"client05_{clientId}@test.local",
            Email = $"client05_{clientId}@test.local",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(clientUser);

        var soc = SocieteCliente.Soumettre(clientId, "SOC-SEC-05", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true));

        var agentId = Guid.NewGuid();
        var agentUser = new ApplicationUser
        {
            Id = agentId,
            UserName = $"agent05_{agentId}@test.local",
            Email = $"agent05_{agentId}@test.local",
            Nature = NatureUtilisateur.Agent,
            EstActif = true
        };
        db.Users.Add(agentUser);

        var file = await db.Files.FirstOrDefaultAsync(f => f.Nom == "Support Opérationnel");
        if (file is null)
        {
            file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
            db.Files.Add(file);
        }
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.ComplaintInstruire, file.Id));

        var dossier = Dossier.Creer("DOS-05", societeId, DateTime.UtcNow, clientId);
        var ticket = Ticket.Creer("REC-05-01", "CustomerComplaint", "Reclamation", file.Id, dossier.Id, DateTime.UtcNow);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Agent publie une note interne
        await publierMessageCmd.PublierNoteInterneAsync(agentId, ticket.Id, "Note interne confidentielle d'instruction");

        // Client consulte la fiche
        var ficheVue = await ficheQuery.ExecuterAsync(clientId, ticket.Id);

        // La note interne ne doit absolument pas figurer dans les messages retournés au client
        Assert.DoesNotContain(ficheVue.Messages, m => m.Contenu.Contains("confidentielle"));
        Assert.DoesNotContain(ficheVue.Messages, m => m.EstInterne);
    }

    [Fact]
    public async Task REC_SEC_06_BypassDirectCommande_RefuseCoteServeur()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var evaluerCmd = scope.ServiceProvider.GetRequiredService<EvaluerReclamationCommand>();
        var clotureCmd = scope.ServiceProvider.GetRequiredService<CloturerReclamationCommand>();

        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var clientUser = new ApplicationUser
        {
            Id = clientId,
            UserName = $"client06_{clientId}@test.local",
            Email = $"client06_{clientId}@test.local",
            Nature = NatureUtilisateur.Client,
            EstActif = true
        };
        db.Users.Add(clientUser);

        var soc = SocieteCliente.Soumettre(clientId, "SOC-SEC-06", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(soc, societeId);
        soc.Valider();
        db.Societes.Add(soc);
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true));
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(clientId, Permission.ComplaintRepondre));

        var file = await db.Files.FirstOrDefaultAsync(f => f.Nom == "Support Opérationnel");
        if (file is null)
        {
            file = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
            db.Files.Add(file);
        }

        var dossier = Dossier.Creer("DOS-06", societeId, DateTime.UtcNow, clientId);
        var ticket = Ticket.Creer("REC-06-01", "CustomerComplaint", "Reclamation", file.Id, dossier.Id, DateTime.UtcNow);
        // Ticket EnAttenteDePriseEnCharge (NON clôturé)
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        // Tentative d'évaluation directe sur ticket non clôturé -> Refus
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            evaluerCmd.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticket.Id, 5, null)));

        // Tentative de clôture par un utilisateur non habilité -> Refus
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            clotureCmd.ExecuterAsync(new CloturerReclamationRequete(clientId, ticket.Id, "Tentative de clôture frauduleuse")));
    }
}
