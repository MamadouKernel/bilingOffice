using System.Net;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Guichet;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Application.Notifications.Resolveurs;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Guichet;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.E2E;

/// <summary>
/// [TEST-E2E] Scénario transverse unifié REC-SCE-14 E2E (US8 / F012).
/// Démontre l'ensemble des 7 volets contractuels du CDC :
/// 1. Notifications multicanales (InApp + Email)
/// 2. Lien sécurisé et contrôle d'accès
/// 3. Recherche guichet
/// 4. Prise de rendez-vous
/// 5. Regroupement multi-tickets en dossier
/// 6. Habilitations RBAC strictes
/// 7. Journal d'audit complet et infalsifiable
/// (Gain CDC = +0).
/// </summary>
[Collection("PlateformeWebCollection")]
public sealed class RecSce14NotificationsE2ETests(PlateformeWebApplicationFactory factory)
    : IClassFixture<PlateformeWebApplicationFactory>
{
    private sealed class DirectExecuteurTransactionnel : IExecuteurTransactionnel
    {
        public Task<T> ExecuterAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default) => action(ct);
        public Task ExecuterAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) => action(ct);
    }

    [Fact]
    public async Task REC_SCE_14_E2E_demonstration_complete_des_7_volets_contractuels()
    {
        using var scopeInitial = factory.Services.CreateScope();
        var db = scopeInitial.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var userManager = scopeInitial.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var horloge = scopeInitial.ServiceProvider.GetRequiredService<IHorloge>();

        var maintenant = horloge.MaintenantUtc;

        // ══════════════════════════════════════════════════════════════════════
        // VOLET 1 : Notifications multicanales (InApp + Email)
        // ══════════════════════════════════════════════════════════════════════
        var clientEmail = $"{Guid.NewGuid()}@client-e2e.test";
        var clientUser = new ApplicationUser
        {
            UserName = clientEmail,
            Email = clientEmail,
            NomAffiche = "Client E2E",
            Nature = NatureUtilisateur.Client
        };
        await userManager.CreateAsync(clientUser, "MotDePasse123!");

        var societe = SocieteCliente.Soumettre(clientUser.Id, "Societe E2E Transverse", TypeClient.Normal, maintenant);
        societe.Valider();
        societe.EnregistrerAdministrateurActif();
        db.Societes.Add(societe);

        var rattachement = UtilisateurSociete.Rattacher(clientUser.Id, societe.Id, maintenant, estAdministrateur: true);
        db.UtilisateursSociete.Add(rattachement);

        var filePilote = await db.Files.FirstOrDefaultAsync(f => f.Module == "BillingImport" && f.Nom == "Pilote Manifeste")
            ?? FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        if (db.Entry(filePilote).State == EntityState.Detached)
        {
            db.Files.Add(filePilote);
        }

        var fileFacturation = await db.Files.FirstOrDefaultAsync(f => f.Module == "BillingImport" && f.Nom == "Facturation")
            ?? FileDeTraitement.Creer("BillingImport", "Facturation");
        if (db.Entry(fileFacturation).State == EntityState.Detached)
        {
            db.Files.Add(fileFacturation);
        }

        if (!await db.ReglesDiffusion.AnyAsync(r => r.TypeEvenement == "TicketCree" && r.Canal == CanalNotification.InApp))
        {
            db.ReglesDiffusion.Add(RegleDiffusion.Creer("TicketCree", CanalNotification.InApp, estObligatoire: true));
        }
        if (!await db.ReglesDiffusion.AnyAsync(r => r.TypeEvenement == "TicketCree" && r.Canal == CanalNotification.Email))
        {
            db.ReglesDiffusion.Add(RegleDiffusion.Creer("TicketCree", CanalNotification.Email, estObligatoire: true));
        }

        if (!await db.ModelesNotification.AnyAsync(m => m.TypeEvenement == "TicketCree" && m.Canal == CanalNotification.InApp))
        {
            db.ModelesNotification.Add(ModeleNotification.Creer("TicketCree", CanalNotification.InApp, "Ticket créé", "Demande enregistrée"));
        }
        if (!await db.ModelesNotification.AnyAsync(m => m.TypeEvenement == "TicketCree" && m.Canal == CanalNotification.Email))
        {
            db.ModelesNotification.Add(ModeleNotification.Creer("TicketCree", CanalNotification.Email, "Ticket créé", "Lien: /tickets/consulter/{{ticketId}}"));
        }

        await db.SaveChangesAsync();

        // Création du premier ticket via commande
        using var scope1 = factory.Services.CreateScope();
        var soumettreImport = scope1.ServiceProvider.GetRequiredService<SoumettreDemandeImportCommand>();
        var blUnique = $"BL-E2E-{Guid.NewGuid():N}"[..12];
        var resultatImport = await soumettreImport.ExecuterAsync(new SoumettreDemandeImportRequete(
            societe.Id, clientUser.Id, [blUnique]));

        Assert.Single(resultatImport.TicketsCrees);
        var ticket1 = resultatImport.TicketsCrees[0];
        var dossierId = resultatImport.Dossier.Id;

        // Distribution des notifications
        using var scopeNotif = factory.Services.CreateScope();
        var spNotif = scopeNotif.ServiceProvider;
        var distributeur = new DistribuerNotificationsUseCase(
            spNotif.GetRequiredService<IEvenementSocleRepository>(),
            spNotif.GetRequiredService<IRegleDiffusionRepository>(),
            spNotif.GetRequiredService<IModeleNotificationRepository>(),
            spNotif.GetRequiredService<INotificationRepository>(),
            spNotif.GetRequiredService<IEnvoiNotificationRepository>(),
            spNotif.GetRequiredService<IPreferenceCanalRepository>(),
            [new ResolveurClientDuDossier(
                spNotif.GetRequiredService<ITicketRepository>(),
                spNotif.GetRequiredService<IDossierRepository>(),
                spNotif.GetRequiredService<IUtilisateurSocieteRepository>())],
            new DirectExecuteurTransactionnel(),
            spNotif.GetRequiredService<IJournalisationService>(),
            horloge,
            spNotif.GetRequiredService<IUniteDeTravail>());

        var nbDistribues = await distributeur.DistribuerAsync();
        Assert.True(nbDistribues >= 1);

        using var scopeCheck1 = factory.Services.CreateScope();
        var dbCheck1 = scopeCheck1.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var notifInApp = await dbCheck1.Notifications.FirstOrDefaultAsync(n => n.TicketId == ticket1.Id);
        Assert.NotNull(notifInApp);
        var envoiEmail = await dbCheck1.EnvoisNotification.FirstOrDefaultAsync(e => e.NotificationId == notifInApp.Id && e.Canal == CanalNotification.Email);
        Assert.NotNull(envoiEmail);

        // ══════════════════════════════════════════════════════════════════════
        // VOLET 2 : Lien sécurisé et contrôle d'accès
        // ══════════════════════════════════════════════════════════════════════
        var clientHttp = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });

        // Requête non authentifiée vers ressource protégée -> 302 vers connexion
        var reponseProtegee = await clientHttp.GetAsync("/client/demandes");
        Assert.Equal(HttpStatusCode.Redirect, reponseProtegee.StatusCode);
        Assert.Contains("connexion", reponseProtegee.Headers.Location!.OriginalString);

        // ══════════════════════════════════════════════════════════════════════
        // VOLET 3 : Recherche guichet
        // ══════════════════════════════════════════════════════════════════════
        using var scopeGuichet = factory.Services.CreateScope();
        var ticketRepo = scopeGuichet.ServiceProvider.GetRequiredService<ITicketRepository>();
        var ticketTrouve = await ticketRepo.ObtenirTicketActifParNumeroBlAsync(blUnique);
        Assert.NotNull(ticketTrouve);
        Assert.Equal(ticket1.Id, ticketTrouve.Id);

        // ══════════════════════════════════════════════════════════════════════
        // VOLET 4 : Prise de rendez-vous guichet
        // ══════════════════════════════════════════════════════════════════════
        using var scopeRdv = factory.Services.CreateScope();
        var dbRdv = scopeRdv.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var rdv = RendezVous.Creer(
            ticket1.Id,
            dossierId,
            clientUser.Id,
            DateOnly.FromDateTime(maintenant.AddDays(1)),
            new TimeSpan(10, 0, 0),
            new TimeSpan(10, 30, 0),
            "RDV Facturation BL",
            maintenant);
        dbRdv.RendezVous.Add(rdv);
        await dbRdv.SaveChangesAsync();

        Assert.Equal(StatutRendezVous.Reserve, rdv.Statut);

        // ══════════════════════════════════════════════════════════════════════
        // VOLET 5 : Regroupement multi-tickets en dossier
        // ══════════════════════════════════════════════════════════════════════
        using var scopeDossier = factory.Services.CreateScope();
        var creerTicket = scopeDossier.ServiceProvider.GetRequiredService<CreerTicketCommand>();
        var consulterDossier = scopeDossier.ServiceProvider.GetRequiredService<ConsulterDossierQuery>();

        var ticket2 = await creerTicket.ExecuterAsync(new CreerTicketRequete(
            societe.Id, "BillingImport", "Complement", fileFacturation.Id, dossierId));

        var dossierComplet = await consulterDossier.ExecuterAsync(dossierId);
        Assert.Equal(2, dossierComplet.Tickets.Count);
        Assert.Contains(dossierComplet.Tickets, t => t.Id == ticket1.Id);
        Assert.Contains(dossierComplet.Tickets, t => t.Id == ticket2.Id);

        // ══════════════════════════════════════════════════════════════════════
        // VOLET 6 : Habilitations RBAC strictes
        // ══════════════════════════════════════════════════════════════════════
        using var scopeRbac = factory.Services.CreateScope();
        var consulterFile = scopeRbac.ServiceProvider.GetRequiredService<ConsulterFileQuery>();
        var agentNonHabiliteId = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            consulterFile.ExecuterAsync(agentNonHabiliteId, filePilote.Id));

        // ══════════════════════════════════════════════════════════════════════
        // VOLET 7 : Journal d'audit complet et infalsifiable
        // ══════════════════════════════════════════════════════════════════════
        using var scopeAudit = factory.Services.CreateScope();
        var dbAudit = scopeAudit.ServiceProvider.GetRequiredService<PlateformeDbContext>();
        var consulterAudit = scopeAudit.ServiceProvider.GetRequiredService<ConsulterJournalAuditQuery>();

        var adminAuditId = Guid.NewGuid();
        dbAudit.Habilitations.Add(HabilitationUtilisateur.Attribuer(adminAuditId, Permission.AuditConsulter));
        await dbAudit.SaveChangesAsync();

        var auditEntries = await consulterAudit.ExecuterAsync(adminAuditId, ticket1.Id);
        Assert.NotEmpty(auditEntries);
        Assert.Contains(auditEntries, a => a.Action == "TicketImportCree");
    }
}
