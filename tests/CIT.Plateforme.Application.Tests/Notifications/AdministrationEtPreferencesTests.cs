using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

// FEATURE 009 — T108 à T115. CONTRATS C9-06 (modèles) et C9-07 (règles). TEST-NOT-02 cas 1→6.
public class AdministrationNotificationsTests
{
    private readonly Mock<IModeleNotificationRepository> _modeles = new();
    private readonly Mock<IRegleDiffusionRepository> _regles = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private static readonly Guid AdminId = Guid.NewGuid();
    private static readonly Guid ClientId = Guid.NewGuid();

    private AdministrerModelesNotificationCommand CommandeModeles() =>
        new(_modeles.Object, _autorisation.Object, _journalisation.Object, _uniteDeTravail.Object);

    private AdministrerReglesDiffusionCommand CommandeRegles() =>
        new(_regles.Object, _autorisation.Object, _journalisation.Object, _uniteDeTravail.Object);

    // ---- T108 / T109 : cas TEST-NOT-02-1 et -2 (ajout/retrait de canal, sans redéploiement) ----

    [Fact]
    public async Task AjouterUnCanal_CreeLaRegleCorrespondante()
    {
        RegleDiffusion? creee = null;
        _regles.Setup(r => r.AjouterAsync(It.IsAny<RegleDiffusion>(), It.IsAny<CancellationToken>()))
            .Callback<RegleDiffusion, CancellationToken>((r, _) => creee = r)
            .Returns(Task.CompletedTask);

        await CommandeRegles().CreerAsync(AdminId, "FactureEditee", CanalNotification.Email, estObligatoire: false);

        Assert.NotNull(creee);
        Assert.Equal("FactureEditee", creee!.TypeEvenement);
        Assert.Equal(CanalNotification.Email, creee.Canal);
        Assert.True(creee.EstActif);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RetirerUnCanal_DesactiveLaRegleSansSuppressionPhysique()
    {
        var regle = RegleDiffusion.Creer("FactureEditee", CanalNotification.Email, false);
        _regles.Setup(r => r.ObtenirParIdAsync(regle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(regle);

        await CommandeRegles().BasculerActivationAsync(AdminId, regle.Id, actif: false);

        Assert.False(regle.EstActif);
        // Aucune suppression physique : le référentiel n'expose aucune méthode de suppression.
        Assert.DoesNotContain(
            typeof(IRegleDiffusionRepository).GetMethods().Select(m => m.Name),
            n => n.Contains("Supprim", StringComparison.OrdinalIgnoreCase));
    }

    // ---- T110 : cas TEST-NOT-02-3 (non-rétroactivité) ----

    [Fact]
    public async Task ModifierUnModele_NAltereAucuneNotificationDejaEmise()
    {
        var modele = ModeleNotification.Creer("TicketCree", CanalNotification.InApp, "Ancien titre", "Ancien corps");
        var dejaEmise = Notification.Creer(
            Guid.NewGuid(), Guid.NewGuid(), "TicketCree", modele.Id, "Ancien titre", "Ancien corps", null, DateTime.UtcNow);
        _modeles.Setup(m => m.ObtenirParIdAsync(modele.Id, It.IsAny<CancellationToken>())).ReturnsAsync(modele);

        await CommandeModeles().ModifierAsync(AdminId, modele.Id, "Nouveau titre", "Nouveau corps");

        Assert.Equal("Nouveau titre", modele.GabaritTitre);
        // Le rendu de la notification déjà émise est FIGÉ.
        Assert.Equal("Ancien titre", dejaEmise.Titre);
        Assert.Equal("Ancien corps", dejaEmise.Corps);
    }

    // ---- T111 : cas TEST-NOT-02-4 (granularité par événement) ----

    [Fact]
    public async Task ModifierLaRegleDUnEvenement_NAffectePasUnAutre()
    {
        var regleE1 = RegleDiffusion.Creer("TicketCree", CanalNotification.Email, false);
        var regleE2 = RegleDiffusion.Creer("FactureEditee", CanalNotification.Email, false);
        _regles.Setup(r => r.ObtenirParIdAsync(regleE1.Id, It.IsAny<CancellationToken>())).ReturnsAsync(regleE1);

        await CommandeRegles().BasculerActivationAsync(AdminId, regleE1.Id, actif: false);

        Assert.False(regleE1.EstActif);
        Assert.True(regleE2.EstActif);
    }

    // ---- T112 : cas TEST-NOT-02-5 (autorisation côté serveur) ----

    [Fact]
    public async Task SansHabilitation_LAdministrationEstRefuseeCoteServeur()
    {
        _autorisation.Setup(a => a.ExigerAsync(ClientId, Permission.AdministrationNotifications, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => CommandeRegles().CreerAsync(ClientId, "TicketCree", CanalNotification.Email, false));
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => CommandeModeles().CreerAsync(ClientId, "TicketCree", CanalNotification.Email, "t", "c"));
        await Assert.ThrowsAsync<AccesRefuseException>(() => CommandeRegles().ListerAsync(ClientId));

        _regles.Verify(r => r.AjouterAsync(It.IsAny<RegleDiffusion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- T113 : cas TEST-NOT-02-6 (traçabilité avant/après) ----

    [Fact]
    public async Task ModificationDeRegle_EstTraceeAvecAuteurEtValeursAvantApres()
    {
        var regle = RegleDiffusion.Creer("TicketCree", CanalNotification.Email, estObligatoire: false);
        _regles.Setup(r => r.ObtenirParIdAsync(regle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(regle);

        await CommandeRegles().DefinirCaractereObligatoireAsync(AdminId, regle.Id, estObligatoire: true);

        _journalisation.Verify(j => j.ConsignerAuditAsync(
            AdminId, "RegleDiffusionModifiee", nameof(RegleDiffusion), regle.Id,
            "obligatoire=False", "obligatoire=True", It.IsAny<CancellationToken>()), Times.Once);
    }
}

// FEATURE 009 — T116 à T120. CONTRAT C9-09 (COM-05). TEST-NOT-02 cas 7.
public class PreferencesCanauxTests
{
    private readonly Mock<IPreferenceCanalRepository> _preferences = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private static readonly Guid ClientId = Guid.NewGuid();

    private GererPreferencesCanauxCommand Commande() =>
        new(_preferences.Object, _journalisation.Object, _uniteDeTravail.Object);

    [Fact]
    public async Task DesactiverUnCanal_EnregistreEtTraceLaPreference()
    {
        PreferenceCanalUtilisateur? creee = null;
        _preferences.Setup(p => p.AjouterAsync(It.IsAny<PreferenceCanalUtilisateur>(), It.IsAny<CancellationToken>()))
            .Callback<PreferenceCanalUtilisateur, CancellationToken>((p, _) => creee = p)
            .Returns(Task.CompletedTask);

        await Commande().DefinirAsync(ClientId, CanalNotification.Email, actif: false);

        Assert.NotNull(creee);
        Assert.False(creee!.EstActif);
        _journalisation.Verify(j => j.ConsignerAuditAsync(
            ClientId, "PreferenceCanalModifiee", nameof(PreferenceCanalUtilisateur), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- T117 : le canal In-App n'est pas débrayable ----

    [Fact]
    public async Task DesactiverLeCanalInApp_EstImpossible()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => Commande().DefinirAsync(ClientId, CanalNotification.InApp, actif: false));

        _preferences.Verify(p => p.AjouterAsync(It.IsAny<PreferenceCanalUtilisateur>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- T118 : un utilisateur ne modifie que ses propres préférences ----

    [Fact]
    public void LaCommande_NExposeAucunParametreDUtilisateurTiers()
    {
        var parametres = typeof(GererPreferencesCanauxCommand)
            .GetMethod(nameof(GererPreferencesCanauxCommand.DefinirAsync))!
            .GetParameters().Select(p => p.Name).ToList();

        Assert.Equal(["utilisateurAuthentifieId", "canal", "actif", "ct"], parametres);
    }

    [Fact]
    public async Task NonAuthentifie_EstRefuse()
    {
        await Assert.ThrowsAsync<AccesRefuseException>(
            () => Commande().DefinirAsync(Guid.Empty, CanalNotification.Email, actif: false));
    }

    // ---- T119 : aucune préférence au-delà du canal ----

    [Fact]
    public void AucuneDimensionDePreferenceAuDelaDuCanal()
    {
        // spec.md §21 : ni fréquence, ni langue, ni type d'événement, ni plage horaire.
        var proprietes = typeof(PreferenceCanalUtilisateur).GetProperties().Select(p => p.Name).ToList();

        Assert.Equal(["Id", "UtilisateurId", "Canal", "EstActif"], proprietes);
    }
}
