using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.BillingImport;
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

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] Exigence CDC: IMP-03 → US2 (F012).
/// IMP-03 : « Le client reçoit ses numéros de ticket sur son profil et par e-mail. »
/// Démontre la publication de l'événement TicketCree lors d'une soumission Import multi-BL et la génération
/// persistée de la notification in-app et de l'envoi e-mail via le moteur F009.
/// </summary>
public sealed class ImpCert03NotificationEmailTests
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

            // Marquer distribué en mémoire pour les tests
            typeof(EvenementSocle).GetProperty(nameof(EvenementSocle.DistribueLeUtc))!
                .SetValue(evenement, maintenantUtc);
            await dbContext.SaveChangesAsync(ct);
            return true;
        }
    }

    [Fact]
    public async Task IMP_CERT_03_soumission_import_publie_evenement_et_genere_envoi_email_client()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var ticketRepo = new TicketRepository(dbContext);
        var dossierRepo = new DossierRepository(dbContext);
        var societeRepo = new SocieteClienteRepository(dbContext);
        var utilSocieteRepo = new UtilisateurSocieteRepository(dbContext);
        var fileRepo = new FileDeTraitementRepository(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();

        // 1. Données de référence
        var fileManifeste = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        dbContext.Files.Add(fileManifeste);

        var clientAuteurId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(clientAuteurId, "Informations", TypeClient.Normal, horloge.MaintenantUtc);
        societe.Valider();
        societe.EnregistrerAdministrateurActif();
        dbContext.Societes.Add(societe);

        var rattachement = UtilisateurSociete.Rattacher(clientAuteurId, societe.Id, horloge.MaintenantUtc, estAdministrateur: true);
        dbContext.UtilisateursSociete.Add(rattachement);

        // 2. Règles de diffusion F009 (InApp + Email pour TicketCree)
        var regleInApp = RegleDiffusion.Creer("TicketCree", CanalNotification.InApp, estObligatoire: true);
        var regleEmail = RegleDiffusion.Creer("TicketCree", CanalNotification.Email, estObligatoire: true);
        dbContext.ReglesDiffusion.AddRange(regleInApp, regleEmail);

        var modeleInApp = ModeleNotification.Creer("TicketCree", CanalNotification.InApp, "Ticket créé", "Votre ticket est créé");
        var modeleEmail = ModeleNotification.Creer("TicketCree", CanalNotification.Email, "Confirmation ticket", "Numéro de ticket: {{numero}}");
        dbContext.ModelesNotification.AddRange(modeleInApp, modeleEmail);

        await dbContext.SaveChangesAsync();

        // 3. Soumission Import
        var commande = new SoumettreDemandeImportCommand(
            ticketRepo, dossierRepo, societeRepo, utilSocieteRepo, fileRepo, numerotation, journalisation, horloge, uniteDeTravail);

        var requete = new SoumettreDemandeImportRequete(
            societe.Id,
            clientAuteurId,
            ["BL-IMP-001234"]);

        var resultat = await commande.ExecuterAsync(requete);

        Assert.Single(resultat.TicketsCrees);
        var ticketCree = resultat.TicketsCrees[0];

        // 4. Vérification de l'événement dans le journal
        var evenementTicketCree = dbContext.JournalEvenements.SingleOrDefault(e => e.TypeEvenement == "TicketCree");
        Assert.NotNull(evenementTicketCree);
        Assert.Contains(ticketCree.Id.ToString(), evenementTicketCree.ChargeUtile);

        // 5. Distribution par le moteur F009
        var evenementRepo = new InMemoryEvenementSocleRepository(dbContext);
        var regleRepo = new RegleDiffusionRepository(dbContext);
        var modeleRepo = new ModeleNotificationRepository(dbContext);
        var notificationRepo = new NotificationRepository(dbContext);
        var envoiRepo = new EnvoiNotificationRepository(dbContext);
        var prefRepo = new PreferenceCanalRepository(dbContext);
        var executeur = new DirectExecuteurTransactionnel();

        var resolveurs = new IResolveurDestinataires[]
        {
            new ResolveurClientDuDossier(ticketRepo, dossierRepo, utilSocieteRepo)
        };

        var distributeur = new DistribuerNotificationsUseCase(
            evenementRepo, regleRepo, modeleRepo, notificationRepo, envoiRepo, prefRepo,
            resolveurs, executeur, journalisation, horloge, uniteDeTravail);

        var nbDistribues = await distributeur.DistribuerAsync();
        Assert.Equal(1, nbDistribues);

        // 6. Vérification de la notification et de l'envoi Email
        var notif = dbContext.Notifications.SingleOrDefault(n => n.DestinataireId == clientAuteurId || n.DestinataireId == societe.Id);
        Assert.NotNull(notif);
        Assert.Equal(ticketCree.Id, notif.TicketId);

        var envois = dbContext.EnvoisNotification.Where(e => e.NotificationId == notif.Id).ToList();
        Assert.Single(envois);
        Assert.Equal(CanalNotification.Email, envois[0].Canal);
    }
}
