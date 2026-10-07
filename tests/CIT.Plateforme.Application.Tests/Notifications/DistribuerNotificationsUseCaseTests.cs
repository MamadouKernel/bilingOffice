using CIT.Plateforme.Domain.Tickets;
﻿using CIT.Plateforme.Application.Abstractions;
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

    /// <summary>COM-03 : le distributeur lit désormais le module et l'étape du ticket rattaché
    /// pour filtrer les règles. Le substitut ne renvoie aucun ticket — les règles qui ne
    /// discriminent pas, seules utilisées ici, restent donc applicables.</summary>
    private readonly Mock<ITicketRepository> _tickets = new();

    private DistribuerNotificationsUseCase Creer() => new(
        _evenements.Object, new CIT.Plateforme.Application.Tests.Support.LienPortailDeTest(), _regles.Object, _tickets.Object, _modeles.Object, _notifications.Object, _envois.Object,
        _preferences.Object, [_resolveur.Object], _executeur.Object, _journalisation.Object,
        _horloge.Object, _uniteDeTravail.Object);

    private void AvecRegles(params RegleDiffusion[] regles) =>
        _regles.Setup(r => r.ListerActivesParEvenementAsync(Type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(regles);

    /// <summary>Rattache un ticket réel à l'événement, pour que le distributeur puisse lire son
    /// module et sa file — le contexte sur lequel COM-03 fait porter le filtrage.</summary>
    private Ticket AvecTicketEnFile(string module, Guid fileId)
    {
        var ticket = Ticket.Creer("TCK-COM03-000001", module, "Import", fileId, Guid.NewGuid(), DateTime.UtcNow);
        _tickets.Setup(t => t.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        return ticket;
    }

    // ---- COM-03 : paramétrage par module et par étape ----

    [Fact]
    public async Task RegleSansModuleNiEtape_ResteApplicable()
    {
        // RÉTROCOMPATIBILITÉ : c'est la forme de toutes les règles administrées avant l'ouverture
        // de ces axes. Les rendre inopérantes couperait silencieusement les notifications.
        AvecTicketEnFile("BillingOffice", Guid.NewGuid());
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));

        var crees = await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Equal(1, crees);
    }

    [Fact]
    public async Task RegleBorneeAUnAutreModule_NeSApplique_Pas()
    {
        AvecTicketEnFile("BillingOffice", Guid.NewGuid());
        var regle = RegleDiffusion.Creer(Type, CanalNotification.InApp, false);
        regle.Cibler("Enrolement", null);
        AvecRegles(regle);

        var crees = await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Equal(0, crees);
        Assert.Empty(_notificationsAjoutees);
    }

    [Fact]
    public async Task RegleBorneeALEtapeDuTicket_SApplique()
    {
        var file = Guid.NewGuid();
        AvecTicketEnFile("BillingOffice", file);
        var regle = RegleDiffusion.Creer(Type, CanalNotification.InApp, false);
        regle.Cibler("BillingOffice", file);
        AvecRegles(regle);

        var crees = await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Equal(1, crees);
    }

    [Fact]
    public async Task RegleBorneeAUneAutreEtape_NeSApplique_Pas()
    {
        AvecTicketEnFile("BillingOffice", Guid.NewGuid());
        var regle = RegleDiffusion.Creer(Type, CanalNotification.InApp, false);
        regle.Cibler(null, Guid.NewGuid());
        AvecRegles(regle);

        var crees = await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Equal(0, crees);
    }

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
        // Depuis le retrait de SMS/WhatsApp, l'e-mail est le seul canal EXTERNE (l'In-App n'émet
        // aucun envoi : la notification EST sa diffusion).
        AvecRegles(
            RegleDiffusion.Creer(Type, CanalNotification.InApp, false),
            RegleDiffusion.Creer(Type, CanalNotification.Email, false));

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        Assert.Single(_envoisAjoutes);
        Assert.All(_envoisAjoutes, e => Assert.Equal(StatutEnvoi.EnAttente, e.Statut));
        Assert.DoesNotContain(_envoisAjoutes, e => e.Canal == CanalNotification.InApp);
        Assert.Contains(_envoisAjoutes, e => e.Canal == CanalNotification.Email);
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
        // Sans ticket rattaché, un lien de repli vers le portail est ajouté pour garantir un bouton
        // d'action dans l'e-mail ; le rendu figé porte sur le texte capté à la création.
        Assert.StartsWith("Corps du modèle", notification.Corps);
        Assert.Contains($"{Support.LienPortailDeTest.UrlBase}/comptes/connexion", notification.Corps);
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

    // ---- P1-02 / RG-COM-01 : interpolation du gabarit et lien direct vers le ticket ----

    /// <summary>
    /// DÉFAUT CORRIGÉ : le distributeur recopiait le gabarit verbatim. Un modèle portant
    /// « {numeroTicket} » restituait ce texte littéral au client.
    /// </summary>
    [Fact]
    public async Task Le_gabarit_est_interpole_avec_les_donnees_du_ticket()
    {
        var ticket = AvecTicketEnFile("BillingImport", Guid.NewGuid());
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));
        _modeles.Setup(m => m.ObtenirActifAsync(Type, CanalNotification.InApp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModeleNotification.Creer(Type, CanalNotification.InApp,
                "Ticket {numeroTicket}", "Votre demande {module} a évolué."));

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        var notification = Assert.Single(_notificationsAjoutees);
        Assert.Equal($"Ticket {ticket.Numero}", notification.Titre);
        Assert.Contains("Votre demande BillingImport a évolué.", notification.Corps);
    }

    /// <summary>
    /// RG-COM-01 — « chaque notification comporte un lien direct vers le ticket ». Le lien est
    /// garanti même lorsque le gabarit administré ne l'a pas prévu : la règle ne peut pas dépendre
    /// du fait qu'un administrateur ait pensé à écrire un jeton.
    /// </summary>
    [Fact]
    public async Task Le_lien_direct_vers_le_ticket_est_garanti_meme_si_le_gabarit_l_omet()
    {
        var ticket = AvecTicketEnFile("BillingImport", Guid.NewGuid());
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));
        _modeles.Setup(m => m.ObtenirActifAsync(Type, CanalNotification.InApp, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ModeleNotification.Creer(Type, CanalNotification.InApp, "Titre", "Corps sans lien."));

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        var notification = Assert.Single(_notificationsAjoutees);
        Assert.Contains($"/tickets/{ticket.Id}", notification.Corps);
    }

    /// <summary>NON-RÉGRESSION : sans modèle administré, le comportement de repli est conservé —
    /// le titre reste le type d'événement.</summary>
    [Fact]
    public async Task Sans_modele_administre_le_type_d_evenement_reste_le_titre()
    {
        AvecTicketEnFile("BillingImport", Guid.NewGuid());
        AvecRegles(RegleDiffusion.Creer(Type, CanalNotification.InApp, false));

        await Creer().DistribuerUnEvenementAsync(EvenementId, Type, Charge);

        var notification = Assert.Single(_notificationsAjoutees);
        Assert.Equal(Type, notification.Titre);
    }
}
