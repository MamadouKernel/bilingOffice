using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Notifications;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

// FEATURE 009 — T070 à T082. CONTRATS C9-02, C9-03, C9-04.
// TEST-NOT-01 cas 1, 2, 3, 6, 7 (cas 4, 5, 8 : sécurité, phase 15).
public class CentreNotificationsTests
{
    private readonly Mock<INotificationRepository> _notifications = new();

    // P2-06 : ces requêtes consultent désormais la garde d'identité. Le dépôt d'infos simulé décrit
    // des comptes ACTIFS — les scénarios éprouvés restent donc exactement les mêmes.
    private readonly Mock<IUtilisateurInfoRepository> _infos = new();

    private GardeIdentiteService Garde()
    {
        _infos.Setup(i => i.ObtenirInfoAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == Guid.Empty
                ? null
                : new UtilisateurInfo(id, "u@test", "u@test", "Utilisateur", true, NatureUtilisateur.Client));
        return new GardeIdentiteService(_infos.Object);
    }
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly Mock<IHorloge> _horloge = new();

    private static readonly Guid UtilisateurA = Guid.NewGuid();
    private static readonly Guid UtilisateurB = Guid.NewGuid();
    private static readonly DateTime Maintenant = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    public CentreNotificationsTests() => _horloge.SetupGet(h => h.MaintenantUtc).Returns(Maintenant);

    private static Notification PourDestinataire(Guid destinataireId, Guid? ticketId = null) =>
        Notification.Creer(Guid.NewGuid(), destinataireId, "TicketCree", Guid.NewGuid(), "Titre", "Corps",
            ticketId ?? Guid.NewGuid(), Maintenant);

    // ---- T070 : cas TEST-NOT-01-1 (contenu de la liste) ----

    [Fact]
    public async Task Liste_ExposeEtatDateTypeTitreContenuEtLienTicket()
    {
        var ticketId = Guid.NewGuid();
        var notification = PourDestinataire(UtilisateurA, ticketId);
        _notifications.Setup(n => n.ListerParDestinataireAsync(UtilisateurA, It.IsAny<CancellationToken>()))
            .ReturnsAsync([notification]);

        var lignes = await new ConsulterMesNotificationsQuery(_notifications.Object, Garde()).ExecuterAsync(UtilisateurA);

        var ligne = Assert.Single(lignes);
        Assert.False(ligne.EstLue);
        Assert.Equal(Maintenant, ligne.CreeeLeUtc);
        Assert.Equal("TicketCree", ligne.TypeEvenement);
        Assert.Equal("Titre", ligne.Titre);
        Assert.Equal("Corps", ligne.Corps);
        Assert.Equal(ticketId, ligne.TicketId);
    }

    // ---- T071 : cas TEST-NOT-01-6 (cloisonnement destinataire, anti-IDOR) ----

    [Fact]
    public async Task Liste_NeRemonteJamaisLesNotificationsDUnAutre()
    {
        // Le filtre est appliqué DANS la requête : la query n'accepte aucun identifiant de
        // destinataire fourni par l'appelant.
        _notifications.Setup(n => n.ListerParDestinataireAsync(UtilisateurB, It.IsAny<CancellationToken>()))
            .ReturnsAsync([PourDestinataire(UtilisateurB)]);
        _notifications.Setup(n => n.ListerParDestinataireAsync(UtilisateurA, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var lignes = await new ConsulterMesNotificationsQuery(_notifications.Object, Garde()).ExecuterAsync(UtilisateurA);

        Assert.Empty(lignes);
        _notifications.Verify(n => n.ListerParDestinataireAsync(UtilisateurA, It.IsAny<CancellationToken>()), Times.Once);
        _notifications.Verify(n => n.ListerParDestinataireAsync(UtilisateurB, It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- T074 : identité dérivée du contexte authentifié ----

    [Fact]
    public async Task Liste_NonAuthentifie_EstRefusee()
    {
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => new ConsulterMesNotificationsQuery(_notifications.Object, Garde()).ExecuterAsync(Guid.Empty));
    }

    [Fact]
    public void Query_NExposeAucunParametreDeDestinataireArbitraire()
    {
        // La signature ne comporte QUE l'identité authentifiée : impossible de viser un tiers.
        var parametres = typeof(ConsulterMesNotificationsQuery)
            .GetMethod(nameof(ConsulterMesNotificationsQuery.ExecuterAsync))!
            .GetParameters().Select(p => p.Name).ToList();

        Assert.Equal(["utilisateurAuthentifieId", "ct"], parametres);
    }

    // ---- T075 : compteur (NOT-03) ----

    [Fact]
    public async Task Compteur_RetourneLeNombreExactDeNonLues()
    {
        _notifications.Setup(n => n.CompterNonLuesAsync(UtilisateurA, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var compte = await new CompterNonLuesQuery(_notifications.Object, Garde()).ExecuterAsync(UtilisateurA);

        Assert.Equal(3, compte);
    }

    [Fact]
    public async Task Compteur_ZeroEstUnResultatNormal()
    {
        _notifications.Setup(n => n.CompterNonLuesAsync(UtilisateurA, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var compte = await new CompterNonLuesQuery(_notifications.Object, Garde()).ExecuterAsync(UtilisateurA);

        Assert.Equal(0, compte);
    }

    [Fact]
    public async Task Compteur_NonAuthentifie_EstRefuse()
    {
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => new CompterNonLuesQuery(_notifications.Object, Garde()).ExecuterAsync(Guid.Empty));
    }

    // ---- T076 : cas TEST-NOT-01-2 (marquage) ----

    private MarquerNotificationLueCommand Commande() =>
        new(_notifications.Object, _journalisation.Object, _horloge.Object, _uniteDeTravail.Object);

    [Fact]
    public async Task Marquage_PasseAEtatLuEtEstAudite()
    {
        var notification = PourDestinataire(UtilisateurA);
        _notifications.Setup(n => n.ObtenirParIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        await Commande().ExecuterAsync(UtilisateurA, notification.Id);

        Assert.True(notification.EstLue);
        Assert.Equal(Maintenant, notification.LueLeUtc);
        _journalisation.Verify(j => j.ConsignerAuditAsync(
            UtilisateurA, "NotificationLue", "Notification", notification.Id, "NonLue", "Lue", It.IsAny<CancellationToken>()), Times.Once);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- T077 : cas TEST-NOT-01-3 (idempotence) ----

    [Fact]
    public async Task Marquage_DejaLue_NeChangeRienEtNeLevePas()
    {
        var notification = PourDestinataire(UtilisateurA);
        notification.MarquerCommeLue(Maintenant.AddMinutes(-10));
        _notifications.Setup(n => n.ObtenirParIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        var exception = await Record.ExceptionAsync(() => Commande().ExecuterAsync(UtilisateurA, notification.Id));

        Assert.Null(exception);
        Assert.Equal(Maintenant.AddMinutes(-10), notification.LueLeUtc);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- T078 : cas TEST-NOT-01-6 (marquage par un non-destinataire) ----

    [Fact]
    public async Task Marquage_ParUnNonDestinataire_EstRefuseCoteServeur()
    {
        var notification = PourDestinataire(UtilisateurB);
        _notifications.Setup(n => n.ObtenirParIdAsync(notification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(notification);

        await Assert.ThrowsAsync<AccesRefuseException>(() => Commande().ExecuterAsync(UtilisateurA, notification.Id));

        Assert.False(notification.EstLue);
    }

    [Fact]
    public async Task Marquage_NotificationInexistanteEtNotificationDAutrui_DonnentLeMemeMessage()
    {
        // Anti-divulgation : connaître un identifiant ne doit jamais permettre de déduire
        // l'existence d'une notification (spec.md §20, US1 « cloisonnement »).
        var notificationAutrui = PourDestinataire(UtilisateurB);
        _notifications.Setup(n => n.ObtenirParIdAsync(notificationAutrui.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notificationAutrui);
        var idInexistant = Guid.NewGuid();
        _notifications.Setup(n => n.ObtenirParIdAsync(idInexistant, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        var refusAutrui = await Assert.ThrowsAsync<AccesRefuseException>(
            () => Commande().ExecuterAsync(UtilisateurA, notificationAutrui.Id));
        var refusInexistant = await Assert.ThrowsAsync<AccesRefuseException>(
            () => Commande().ExecuterAsync(UtilisateurA, idInexistant));

        Assert.Equal(refusInexistant.Message, refusAutrui.Message);
    }

    // ---- T082 : abstention ACR-01 ----

    [Fact]
    public void MarquerCommeLue_NeToucheAucunMecanismeDActionRequise()
    {
        // spec.md §11 : marquer une notification comme lue ne retire JAMAIS l'action requise
        // correspondante. La commande ne dépend d'aucun port lié au tableau de bord client.
        var parametres = typeof(MarquerNotificationLueCommand)
            .GetConstructors().Single().GetParameters()
            .Select(p => p.ParameterType.Name).ToList();

        Assert.DoesNotContain(parametres, t => t.Contains("Action", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(parametres, t => t.Contains("Dashboard", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(parametres, t => t.Contains("Ticket", StringComparison.OrdinalIgnoreCase));
    }
}
