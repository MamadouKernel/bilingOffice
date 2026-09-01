using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Domain.Notifications;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

// FEATURE 009 — T053 à T063, T189 à T193. CONTRAT C9-01 (moteur de distribution).
// Garantie I9 : at-least-once au traitement, effectively-once à l'effet observable.
public class DistribuerNotificationsUseCaseTests
{
    private readonly Mock<IEvenementSocleRepository> _evenements = new();
    private readonly Mock<IRegleDiffusionRepository> _regles = new();
    private readonly Mock<IModeleNotificationRepository> _modeles = new();
    private readonly Mock<INotificationRepository> _notifications = new();
    private readonly Mock<IEnvoiNotificationRepository> _envois = new();
    private readonly Mock<IPreferenceCanalRepository> _preferences = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IExecuteurTransactionnel> _executeur = new();
    private readonly Mock<IResolveurDestinataires> _resolveur = new();

    private static readonly Guid EvenementId = Guid.NewGuid();
    private static readonly Guid DestinataireId = Guid.NewGuid();
    private static readonly Guid TicketId = Guid.NewGuid();
    private const string Type = "TicketCree";
    private const string Charge = "{\"ticketId\":\"x\"}";

    private readonly List<Notification> _notificationsAjoutees = [];
    private readonly List<EnvoiNotification> _envoisAjoutes = [];

    public DistribuerNotificationsUseCaseTests()
    {
        _horloge.SetupGet(h => h.MaintenantUtc).Returns(new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc));

        // Par défaut, la prise atomique réussit et la transaction exécute l'opération.
        _evenements.Setup(e => e.TenterDePrendreAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _executeur.Setup(x => x.ExecuterAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<int>>, CancellationToken>((op, ct) => op(ct));

        _resolveur.Setup(r => r.PeutResoudre(Type)).Returns(true);
        _resolveur.Setup(r => r.ResoudreAsync(Type, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([DestinataireId]);
        _resolveur.Setup(r => r.ResoudreTicketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TicketId);

        _notifications.Setup(n => n.AjouterAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => _notificationsAjoutees.Add(n))
            .Returns(Task.CompletedTask);
        _envois.Setup(e => e.AjouterAsync(It.IsAny<EnvoiNotification>(), It.IsAny<CancellationToken>()))
            .Callback<EnvoiNotification, CancellationToken>((e, _) => _envoisAjoutes.Add(e))
            .Returns(Task.CompletedTask);
    }

    private DistribuerNotificationsUseCase Creer() => new(
        _evenements.Object, _regles.Object, _modeles.Object, _notifications.Object, _envois.Object,
        _preferences.Object, [_resolveur.Object], _executeur.Object, _journalisation.Object,
        _horloge.Object, _uniteDeTravail.Object);

    private void AvecRegles(params RegleDiffusion[] regles) =>
        _regles.Setup(r => r.ListerActivesParEvenementAsync(Type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(regles);

    // ---- T053 : silence du paramétrage ----

    [Fact]
    public async Task AucuneRegle_NeProduitAucuneNotification()
    {
        AvecRegles();

        var crees = await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Equal(0, crees);
        Assert.Empty(_notificationsAjoutees);
        // L'événement est néanmoins marqué distribué : il a bien été examiné (spec.md §10).
        _evenements.Verify(e => e.TenterDePrendreAsync(EvenementId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- T054 : nominal In-App (cas TEST-NOT-02-1, volet In-App) ----

    [Fact]
    public async Task RegleInApp_ProduitUneNotificationNonLueAvecLienTicket()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));

        var crees = await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Equal(1, crees);
        var notification = Assert.Single(_notificationsAjoutees);
        Assert.Equal(DestinataireId, notification.DestinataireId);
        Assert.Equal(EvenementId, notification.EvenementSourceId);
        Assert.Equal(TicketId, notification.TicketId);
        Assert.False(notification.EstLue);
    }

    // ---- T055 : le canal In-App ne produit aucun envoi ----

    [Fact]
    public async Task CanalInApp_NeCreeAucunEnvoi()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Empty(_envoisAjoutes);
    }

    // ---- T056 : un envoi par canal externe (cas TEST-NOT-02-1) ----

    [Fact]
    public async Task ReglesMultiCanaux_CreentUnEnvoiParCanalExterne()
    {
        AvecRegles(
            RegleDiffusion.Creer(Type, CanalNotification.InApp, false),
            RegleDiffusion.Creer(Type, CanalNotification.Email, false),
            RegleDiffusion.Creer(Type, CanalNotification.Sms, false));

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Equal(2, _envoisAjoutes.Count);
        Assert.All(_envoisAjoutes, e => Assert.Equal(StatutEnvoi.EnAttente, e.Statut));
        Assert.DoesNotContain(_envoisAjoutes, e => e.Canal == CanalNotification.InApp);
    }

    // ---- T057 / T193 : idempotence (scénario B, cas TEST-MSG-04-8 générique) ----

    [Fact]
    public async Task EvenementRejoue_NeCreeAucunDoublon()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));
        _notifications.Setup(n => n.ExisteDejaAsync(EvenementId, DestinataireId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var crees = await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Equal(0, crees);
        Assert.Empty(_notificationsAjoutees);
    }

    // ---- T058 : rendu figé depuis le modèle ----

    [Fact]
    public async Task Rendu_EstFigeDepuisLeModeleInApp()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));
        var modele = ModeleNotification.Creer(Type, CanalNotification.InApp, "Titre du modèle", "Corps du modèle");
        _modeles.Setup(m => m.ObtenirActifAsync(Type, CanalNotification.InApp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(modele);

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        var notification = Assert.Single(_notificationsAjoutees);
        Assert.Equal("Titre du modèle", notification.Titre);
        Assert.Equal("Corps du modèle", notification.Corps);
        Assert.Equal(modele.Id, notification.ModeleId);
    }

    // ---- T059 : préférence vs notification obligatoire (cas TEST-NOT-02-7) ----

    [Fact]
    public async Task CanalDesactiveEtRegleNonObligatoire_NeCreePasLEnvoi()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.Email, estObligatoire: false));
        var preference = PreferenceCanalUtilisateur.Creer(DestinataireId, CanalNotification.Email);
        preference.Desactiver();
        _preferences.Setup(p => p.ObtenirAsync(DestinataireId, CanalNotification.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Empty(_envoisAjoutes);
        Assert.Single(_notificationsAjoutees);
    }

    [Fact]
    public async Task CanalDesactiveMaisRegleObligatoire_CreeQuandMemeLEnvoi()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.Email, estObligatoire: true));
        var preference = PreferenceCanalUtilisateur.Creer(DestinataireId, CanalNotification.Email);
        preference.Desactiver();
        _preferences.Setup(p => p.ObtenirAsync(DestinataireId, CanalNotification.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        var envoi = Assert.Single(_envoisAjoutes);
        Assert.Equal(CanalNotification.Email, envoi.Canal);
    }

    // ---- T061 : étanchéité des notes internes (MSG-02) ----

    [Fact]
    public async Task AucunResolveurClient_PourUnEvenementDeNoteInterne()
    {
        // Le résolveur client ne déclare PAS savoir traiter MessagePoste ni
        // CollaborateurMentionne : une note interne ne peut jamais notifier un client.
        var resolveurClient = new CIT.Plateforme.Application.Notifications.Resolveurs.ResolveurClientDuDossier(
            Mock.Of<ITicketRepository>(), Mock.Of<IDossierRepository>(), Mock.Of<IUtilisateurSocieteRepository>());

        Assert.False(resolveurClient.PeutResoudre("MessagePoste"));
        Assert.False(resolveurClient.PeutResoudre("CollaborateurMentionne"));
    }

    // ---- T062 : isolation entre événements ----

    [Fact]
    public async Task EchecSurUnEvenement_NInterrompPasLesSuivants()
    {
        var evenementKo = CIT.Plateforme.Domain.Audit.EvenementSocle.Creer(Type, Charge, DateTime.UtcNow);
        var evenementOk = CIT.Plateforme.Domain.Audit.EvenementSocle.Creer(Type, Charge, DateTime.UtcNow);
        _evenements.Setup(e => e.ListerNonDistribuesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([evenementKo, evenementOk]);
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));

        _evenements.Setup(e => e.TenterDePrendreAsync(evenementKo.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("panne simulée"));
        _evenements.Setup(e => e.TenterDePrendreAsync(evenementOk.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var crees = await Creer().DistribuerAsync();

        Assert.Equal(1, crees);
        Assert.Single(_notificationsAjoutees);
    }

    // ---- T063 : absence de broadcast ----

    [Fact]
    public async Task UnDestinataireResolu_ProduitExactementUneNotification()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Single(_notificationsAjoutees);
    }

    // ---- T189 : scénario A — persistance AVANT marquage observable ----

    [Fact]
    public async Task ScenarioA_NotificationsPersisteesEtEvenementMarque()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Single(_notificationsAjoutees);
        _evenements.Verify(e => e.TenterDePrendreAsync(EvenementId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        // Le marquage et la persistance sont committés ensemble par IExecuteurTransactionnel :
        // l'enregistrement de l'unité de travail a bien lieu.
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- T190 : scénario C — échec avant persistance ----

    [Fact]
    public async Task ScenarioC_EchecAvantPersistance_LaTransactionEstAnnulee()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));
        _notifications.Setup(n => n.AjouterAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("panne de persistance"));

        // L'exception traverse le use case ; c'est IExecuteurTransactionnel qui annule la
        // transaction, laissant DistribueLeUtc à null (garantie at-least-once).
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge));

        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- T191 : scénario D — concurrence, prise atomique perdue ----

    [Fact]
    public async Task ScenarioD_PriseAtomiquePerdue_NeProduitAucuneNotification()
    {
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));
        // Un autre worker a déjà pris l'événement.
        _evenements.Setup(e => e.TenterDePrendreAsync(EvenementId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var crees = await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Equal(0, crees);
        Assert.Empty(_notificationsAjoutees);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Abstention ALE-03 (research.md D6) ----

    [Fact]
    public async Task EvenementSla_NeProduitAucuneNotification()
    {
        // Aucun résolveur ne déclare traiter les événements SLA : ALE-03 n'est pas raccordée.
        _regles.Setup(r => r.ListerActivesParEvenementAsync("SlaDepasse", It.IsAny<CancellationToken>()))
            .ReturnsAsync([RegleDiffusion.Creer("SlaDepasse", CanalNotification.InApp, false)]);

        var crees = await Creer().DistribuerUnEvenementAsync(EvenementId, "SlaDepasse", "{}");

        Assert.Equal(0, crees);
        Assert.Empty(_notificationsAjoutees);
    }
}
