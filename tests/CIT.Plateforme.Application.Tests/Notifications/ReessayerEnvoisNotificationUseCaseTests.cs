using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

// FEATURE 009 — T099 à T107, T197. CONTRAT C9-08 (ERR-02).
// TEST-ERR-02 : 6 cas. AUCUNE valeur de reprise n'est affirmée en dur : les paramètres sont
// injectés par la configuration de test (invariant I2, CLAR-NOT-02).
public class ReessayerEnvoisNotificationUseCaseTests
{
    private readonly Mock<IEnvoiNotificationRepository> _envois = new();
    private readonly Mock<INotificationRepository> _notifications = new();
    private readonly Mock<IUtilisateurInfoRepository> _utilisateurs = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly Mock<IHorloge> _horloge = new();

    private static readonly DateTime Maintenant = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid DestinataireId = Guid.NewGuid();

    /// <summary>Paramètres INJECTÉS PAR LE TEST — jamais lus depuis le code de production.</summary>
    private static readonly ParametresReessaiNotification ParametresDeTest = new(NombreTentativesMax: 2, DelaiEntreTentatives: TimeSpan.FromMinutes(1));

    private readonly Notification _notification = Notification.Creer(
        Guid.NewGuid(), DestinataireId, "TicketCree", null, "Titre", "Corps", Guid.NewGuid(), Maintenant);

    public ReessayerEnvoisNotificationUseCaseTests()
    {
        _horloge.SetupGet(h => h.MaintenantUtc).Returns(Maintenant);
        _notifications.Setup(n => n.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_notification);
        _utilisateurs.Setup(u => u.ObtenirProfilAsync(DestinataireId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfilUtilisateur(DestinataireId, "client@test.local", "Client", "+2250000000", true, NatureUtilisateur.Client));
    }

    private sealed class CanalDeTest(CanalNotification canal, bool abouti) : ICanalNotification
    {
        public CanalNotification Canal { get; } = canal;
        public int Appels { get; private set; }

        public Task<ResultatEnvoi> EnvoyerAsync(DestinationEnvoi destination, string titre, string corps, CancellationToken ct = default)
        {
            Appels++;
            return Task.FromResult(abouti ? ResultatEnvoi.Succes() : ResultatEnvoi.Echec("passerelle indisponible"));
        }
    }

    private ReessayerEnvoisNotificationUseCase Creer(IEnumerable<ICanalNotification> canaux, ParametresReessaiNotification? parametres = null) =>
        new(_envois.Object, _notifications.Object, _utilisateurs.Object, canaux,
            parametres ?? ParametresDeTest, _journalisation.Object, _horloge.Object, _uniteDeTravail.Object);

    private void AvecEnvois(params EnvoiNotification[] envois) =>
        _envois.Setup(e => e.ListerAReessayerAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(envois);

    // ---- T099 : cas TEST-ERR-02-1 (isolation inter-canaux) ----

    [Fact]
    public async Task UnCanalEnEchec_NEmpechePasLesAutres()
    {
        var email = EnvoiNotification.Creer(_notification.Id, CanalNotification.Email);
        var sms = EnvoiNotification.Creer(_notification.Id, CanalNotification.Sms);
        var whatsApp = EnvoiNotification.Creer(_notification.Id, CanalNotification.WhatsApp);
        AvecEnvois(email, sms, whatsApp);

        await Creer([
            new CanalDeTest(CanalNotification.Email, abouti: true),
            new CanalDeTest(CanalNotification.Sms, abouti: false),
            new CanalDeTest(CanalNotification.WhatsApp, abouti: true),
        ]).ReessayerAsync();

        Assert.Equal(StatutEnvoi.Envoye, email.Statut);
        Assert.Equal(StatutEnvoi.Echec, sms.Statut);
        Assert.Equal(StatutEnvoi.Envoye, whatsApp.Statut);
    }

    // ---- T100 : cas TEST-ERR-02-2 (statut d'envoi distinct par canal — CDC §20) ----

    [Fact]
    public async Task ChaqueCanal_PorteSonPropreStatutDEnvoi()
    {
        var email = EnvoiNotification.Creer(_notification.Id, CanalNotification.Email);
        var sms = EnvoiNotification.Creer(_notification.Id, CanalNotification.Sms);
        AvecEnvois(email, sms);

        await Creer([
            new CanalDeTest(CanalNotification.Email, abouti: true),
            new CanalDeTest(CanalNotification.Sms, abouti: false),
        ]).ReessayerAsync();

        Assert.NotEqual(email.Statut, sms.Statut);
        Assert.NotNull(sms.MotifEchec);
        Assert.Null(email.MotifEchec);
    }

    // ---- T101 : cas TEST-ERR-02-3 (traçage sans contenu) ----

    [Fact]
    public async Task Echec_EstTraceSansLeContenuDuMessage()
    {
        var email = EnvoiNotification.Creer(_notification.Id, CanalNotification.Email);
        AvecEnvois(email);

        await Creer([new CanalDeTest(CanalNotification.Email, abouti: false)]).ReessayerAsync();

        _journalisation.Verify(j => j.ConsignerAuditAsync(
            null, "EnvoiNotification", nameof(EnvoiNotification), email.Id, null,
            It.Is<string>(d => d.Contains("canal=") && d.Contains("statut=")
                               && !d.Contains("Titre") && !d.Contains("Corps")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- T102 : cas TEST-ERR-02-4 (nouvelle tentative automatique) ----

    [Fact]
    public async Task Reprise_EngageUneNouvelleTentativeSansActionHumaine()
    {
        var email = EnvoiNotification.Creer(_notification.Id, CanalNotification.Email);
        email.EnregistrerEchec("premier échec", Maintenant.AddMinutes(-10));
        AvecEnvois(email);
        var canal = new CanalDeTest(CanalNotification.Email, abouti: true);

        await Creer([canal]).ReessayerAsync();

        Assert.Equal(1, canal.Appels);
        Assert.Equal(2, email.NombreTentatives);
        Assert.Equal(StatutEnvoi.Envoye, email.Statut);
    }

    // ---- T197 : condition d'arrêt (plafond configuré) ----

    [Fact]
    public async Task AuPlafondConfigure_LEnvoiNEstPlusSelectionneEtResteEnEchec()
    {
        // Le plafond est LU DEPUIS LA CONFIGURATION injectée : le test n'affirme aucune valeur
        // en dur, il vérifie que la valeur configurée est bien celle transmise au dépôt.
        var parametres = new ParametresReessaiNotification(NombreTentativesMax: 5, DelaiEntreTentatives: TimeSpan.FromMinutes(7));
        AvecEnvois();

        await Creer([], parametres).ReessayerAsync();

        _envois.Verify(e => e.ListerAReessayerAsync(
            parametres.NombreTentativesMax,
            Maintenant - parametres.DelaiEntreTentatives,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void StatutEnvoi_NeComporteAucunStatutDAbandon()
    {
        // CLAR-NOT-02 : au plafond, l'envoi conserve son statut Echec tracé. Aucun statut de
        // terminaison n'est inventé.
        Assert.DoesNotContain(Enum.GetNames<StatutEnvoi>(), n => n.Contains("Abandon", StringComparison.OrdinalIgnoreCase));
    }

    // ---- T105 : configuration obligatoire ----

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    public void ConfigurationAbsenteOuInvalide_FaitEchouerExplicitement(int tentatives, int minutes)
    {
        var parametres = new ParametresReessaiNotification(tentatives, TimeSpan.FromMinutes(minutes));

        var exception = Assert.Throws<InvalidOperationException>(parametres.Valider);

        Assert.Contains("NombreTentativesMax", exception.Message, StringComparison.Ordinal);
        Assert.Contains("CLAR-NOT-02", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DelaiAbsent_FaitEchouerExplicitement()
    {
        var parametres = new ParametresReessaiNotification(3, TimeSpan.Zero);

        var exception = Assert.Throws<InvalidOperationException>(parametres.Valider);

        Assert.Contains("DelaiEntreTentatives", exception.Message, StringComparison.Ordinal);
    }

    // ---- T104 : cas TEST-ERR-02-6 (aucun fallback) ----

    [Fact]
    public async Task EchecEmail_NeDeclencheJamaisUnAutreCanal()
    {
        var email = EnvoiNotification.Creer(_notification.Id, CanalNotification.Email);
        AvecEnvois(email);
        var canalSms = new CanalDeTest(CanalNotification.Sms, abouti: true);

        await Creer([new CanalDeTest(CanalNotification.Email, abouti: false), canalSms]).ReessayerAsync();

        // Aucune substitution : le canal SMS n'a jamais été sollicité.
        Assert.Equal(0, canalSms.Appels);
        Assert.Equal(CanalNotification.Email, email.Canal);
    }

    // ---- CLAR-COM-04 : coordonnée absente → NonDelivrable, distinct d'un échec ----

    [Fact]
    public async Task CoordonneeAbsente_MarqueNonDelivrableEtNonEchec()
    {
        _utilisateurs.Setup(u => u.ObtenirProfilAsync(DestinataireId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfilUtilisateur(DestinataireId, "client@test.local", "Client", null, true, NatureUtilisateur.Client));
        var sms = EnvoiNotification.Creer(_notification.Id, CanalNotification.Sms);
        AvecEnvois(sms);

        await Creer([new CanalDeTest(CanalNotification.Sms, abouti: true)]).ReessayerAsync();

        Assert.Equal(StatutEnvoi.NonDelivrable, sms.Statut);
        Assert.NotEqual(StatutEnvoi.Echec, sms.Statut);
    }

    [Fact]
    public async Task CanalNonConfigure_MarqueNonDelivrableSansFaireEchouerLesAutres()
    {
        // PC-16 : un canal mobile non souscrit ne rend aucun Must de F009 inatteignable.
        var email = EnvoiNotification.Creer(_notification.Id, CanalNotification.Email);
        var whatsApp = EnvoiNotification.Creer(_notification.Id, CanalNotification.WhatsApp);
        AvecEnvois(email, whatsApp);

        await Creer([new CanalDeTest(CanalNotification.Email, abouti: true)]).ReessayerAsync();

        Assert.Equal(StatutEnvoi.Envoye, email.Statut);
        Assert.Equal(StatutEnvoi.NonDelivrable, whatsApp.Statut);
    }
}
