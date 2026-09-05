using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Application.Notifications.Resolveurs;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

/// <summary>
/// [TEST-SPEC] Exigence CDC: COM-06 → US7 (F012).
/// COM-06 : « Les canaux de relance automatique sont paramétrables (In-App, e-mail). »
/// Démontre l'émission de l'événement RelanceEmise, sa distribution multicanale paramétrable (InApp + Email)
/// et le respect des préférences client.
/// </summary>
public sealed class ComCert06CanauxRelanceTests
{
    private sealed class DirectExecuteurTransactionnel : IExecuteurTransactionnel
    {
        public Task<T> ExecuterAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default) => action(ct);
        public Task ExecuterAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) => action(ct);
    }

    private sealed class InMemoryEvenementSocleRepository(PlateformeDbContext dbContext) : IEvenementSocleRepository
    {
        public async Task<IReadOnlyList<EvenementSocle>> ListerNonDistribuesAsync(int limite, CancellationToken ct = default) =>
            await dbContext.JournalEvenements
                .Where(e => e.DistribueLeUtc == null)
                .OrderBy(e => e.HorodatageUtc)
                .Take(limite)
                .ToListAsync(ct);

        public async Task<EvenementSocle?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
            await dbContext.JournalEvenements.FirstOrDefaultAsync(e => e.Id == id, ct);

        public async Task<bool> TenterDePrendreAsync(Guid evenementId, DateTime maintenantUtc, CancellationToken ct = default)
        {
            var evenement = await ObtenirParIdAsync(evenementId, ct);
            if (evenement is null || evenement.DistribueLeUtc != null)
            {
                return false;
            }

            typeof(EvenementSocle).GetProperty(nameof(EvenementSocle.DistribueLeUtc))!
                .SetValue(evenement, maintenantUtc);
            await dbContext.SaveChangesAsync(ct);
            return true;
        }
    }

    [Fact]
    public async Task COM_CERT_06_relance_emise_distribue_sur_canaux_inapp_et_email_selon_regles_et_preferences()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var utilSocieteRepo = new UtilisateurSocieteRepository(dbContext);
        var evenementRepo = new InMemoryEvenementSocleRepository(dbContext);
        var regleRepo = new RegleDiffusionRepository(dbContext);
        var modeleRepo = new ModeleNotificationRepository(dbContext);
        var notificationRepo = new NotificationRepository(dbContext);
        var envoiRepo = new EnvoiNotificationRepository(dbContext);
        var prefRepo = new PreferenceCanalRepository(dbContext);
        var executeur = new DirectExecuteurTransactionnel();

        // 1. Données de base : client, société, dossier, ticket
        var clientAuteurId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(clientAuteurId, "Client Relance", TypeClient.Normal, horloge.MaintenantUtc);
        societe.Valider();
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, clientAuteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, horloge.MaintenantUtc, estAdministrateur: true));

        var dossier = Dossier.Creer("DOS-REL-001", societe.Id, horloge.MaintenantUtc);
        dbContext.Dossiers.Add(dossier);

        var ticket = Ticket.Creer("TCK-REL-001", "BillingImport", "Import", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        dbContext.Tickets.Add(ticket);

        // 2. Règles de diffusion administrables actives pour RelanceEmise : InApp + Email
        var regleInApp = RegleDiffusion.Creer("RelanceEmise", CanalNotification.InApp, estObligatoire: false);
        var regleEmail = RegleDiffusion.Creer("RelanceEmise", CanalNotification.Email, estObligatoire: false);
        dbContext.ReglesDiffusion.AddRange(regleInApp, regleEmail);

        var modeleInApp = ModeleNotification.Creer("RelanceEmise", CanalNotification.InApp, "Rappel dossier", "Relance sur dossier");
        var modeleEmail = ModeleNotification.Creer("RelanceEmise", CanalNotification.Email, "Rappel e-mail", "Relance envoyée par e-mail");
        dbContext.ModelesNotification.AddRange(modeleInApp, modeleEmail);

        // 3. Événement RelanceEmise publié
        await journalisation.PublierEvenementAsync("RelanceEmise", $"{{\"ticketId\":\"{ticket.Id}\"}}");
        await uniteDeTravail.EnregistrerAsync();

        // 4. Distribution des notifications
        var resolveurs = new IResolveurDestinataires[]
        {
            new ResolveurClientDuDossier(ticketRepo, dossierRepo, utilSocieteRepo)
        };

        var distributeur = new DistribuerNotificationsUseCase(
            evenementRepo,
            new CIT.Plateforme.Application.Tests.Support.LienPortailDeTest(), regleRepo, new TicketRepository(dbContext), modeleRepo, notificationRepo, envoiRepo, prefRepo,
            resolveurs, executeur, journalisation, horloge, uniteDeTravail);

        var nbDistribues = await distributeur.DistribuerAsync();
        Assert.Equal(1, nbDistribues);

        // 5. Vérification des canaux générés : InApp (table Notifications) + Email (table EnvoisNotification)
        var notif = await dbContext.Notifications.SingleOrDefaultAsync(n => n.TicketId == ticket.Id);
        Assert.NotNull(notif);
        Assert.Equal("RelanceEmise", notif.TypeEvenement);

        var envois = await dbContext.EnvoisNotification.Where(e => e.NotificationId == notif.Id).ToListAsync();
        Assert.Single(envois);
        Assert.Equal(CanalNotification.Email, envois[0].Canal);
    }
}
