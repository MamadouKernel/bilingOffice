using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// Tests unitaires pour la commande CloturerMonCompteCommand (Self-Delete / Clôture en self-service).
/// </summary>
public sealed class CloturerMonCompteCommandTests
{
    private readonly Mock<IUtilisateurInfoRepository> _utilisateurs = new();
    private readonly Mock<IAdministrationComptesRepository> _comptes = new();
    private readonly Mock<IUtilisateurSocieteRepository> _rattachements = new();
    private readonly Mock<ISocieteClienteRepository> _societes = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly CloturerMonCompteCommand _command;
    private static readonly Guid UtilisateurId = Guid.NewGuid();
    private static readonly Guid SocieteId = Guid.NewGuid();

    public CloturerMonCompteCommandTests()
    {
        _command = new CloturerMonCompteCommand(
            _utilisateurs.Object,
            _comptes.Object,
            _rattachements.Object,
            _societes.Object,
            _journalisation.Object,
            _uniteDeTravail.Object);

        _utilisateurs.Setup(r => r.ObtenirInfoAsync(UtilisateurId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UtilisateurInfo(
                UtilisateurId, "user@client.local", "user@client.local", "Client Test", true, NatureUtilisateur.Client));

        _comptes.Setup(c => c.VerifierMotDePasseAsync(UtilisateurId, "MonMotDePasse123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task Nominal_CollaborateurClient_DesactiveCompteEtRattachementEtAudite()
    {
        // Collaborateur simple (non-administrateur de société)
        var rattachement = UtilisateurSociete.Rattacher(UtilisateurId, SocieteId, DateTime.UtcNow, estAdministrateur: false);
        _rattachements.Setup(r => r.ObtenirParUtilisateurAsync(UtilisateurId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rattachement);

        await _command.ExecuterAsync(UtilisateurId, new CloturerMonCompteRequete("MonMotDePasse123!", "Départ de l'entreprise"));

        // Rattachement désactivé
        Assert.False(rattachement.EstActif);

        // Audit consigné
        _journalisation.Verify(j => j.ConsignerAuditAsync(
            UtilisateurId,
            "CompteClotureParUsager",
            "ApplicationUser",
            UtilisateurId,
            "Actif",
            It.Is<string>(s => s.Contains("Départ de l'entreprise")),
            It.IsAny<CancellationToken>()), Times.Once);

        // Unité de travail enregistrée
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Désactivation Identity et révocation sessions
        _comptes.Verify(c => c.DefinirActivationAsync(UtilisateurId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Nominal_TransporteurSansSociete_DesactiveCompteEtAudite()
    {
        // Transporteur sans société rattachée
        _rattachements.Setup(r => r.ObtenirParUtilisateurAsync(UtilisateurId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UtilisateurSociete?)null);

        await _command.ExecuterAsync(UtilisateurId, new CloturerMonCompteRequete("MonMotDePasse123!", null));

        // Audit consigné
        _journalisation.Verify(j => j.ConsignerAuditAsync(
            UtilisateurId,
            "CompteClotureParUsager",
            "ApplicationUser",
            UtilisateurId,
            "Actif",
            "Clôturé par l'usager",
            It.IsAny<CancellationToken>()), Times.Once);

        // Désactivation Identity
        _comptes.Verify(c => c.DefinirActivationAsync(UtilisateurId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MotDePasseVide_EstRefuse(string mdp)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(UtilisateurId, new CloturerMonCompteRequete(mdp, null)));

        _comptes.Verify(c => c.DefinirActivationAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MotDePasseIncorrect_EstRefuseAvecAccesRefuseException()
    {
        _comptes.Setup(c => c.VerifierMotDePasseAsync(UtilisateurId, "MauvaisMdp", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(UtilisateurId, new CloturerMonCompteRequete("MauvaisMdp", null)));

        _comptes.Verify(c => c.DefinirActivationAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DernierAdministrateurActif_EstRefuseAvecDernierAdministrateurActifException()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Info", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        societe.EnregistrerAdministrateurActif(); // NombreAdministrateursActifs = 1

        var adminUnique = UtilisateurSociete.Rattacher(UtilisateurId, societe.Id, DateTime.UtcNow, estAdministrateur: true);

        _rattachements.Setup(r => r.ObtenirParUtilisateurAsync(UtilisateurId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUnique);
        _societes.Setup(s => s.ObtenirParIdAsync(societe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(societe);

        // Tentative de clôture -> doit lever DernierAdministrateurActifException (CLAR-03)
        await Assert.ThrowsAsync<DernierAdministrateurActifException>(() =>
            _command.ExecuterAsync(UtilisateurId, new CloturerMonCompteRequete("MonMotDePasse123!", null)));

        // Le rattachement et le compte restent actifs
        Assert.True(adminUnique.EstActif);
        _comptes.Verify(c => c.DefinirActivationAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdministrateurSocieteAvecAutreAdminActif_ReussitEtDecrementeCompteur()
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Info", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        societe.EnregistrerAdministrateurActif();
        societe.EnregistrerAdministrateurActif(); // NombreAdministrateursActifs = 2

        var admin = UtilisateurSociete.Rattacher(UtilisateurId, societe.Id, DateTime.UtcNow, estAdministrateur: true);

        _rattachements.Setup(r => r.ObtenirParUtilisateurAsync(UtilisateurId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        _societes.Setup(s => s.ObtenirParIdAsync(societe.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(societe);

        await _command.ExecuterAsync(UtilisateurId, new CloturerMonCompteRequete("MonMotDePasse123!", null));

        Assert.False(admin.EstActif);
        Assert.Equal(1, societe.NombreAdministrateursActifs);
        _comptes.Verify(c => c.DefinirActivationAsync(UtilisateurId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AgentCit_EstRefuseEnLibreService()
    {
        var agentId = Guid.NewGuid();
        _utilisateurs.Setup(r => r.ObtenirInfoAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UtilisateurInfo(
                agentId, "agent@cit.local", "agent@cit.local", "Agent CIT", true, NatureUtilisateur.Agent));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _command.ExecuterAsync(agentId, new CloturerMonCompteRequete("MonMotDePasse123!", null)));

        _comptes.Verify(c => c.DefinirActivationAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompteDejaInactif_EstRefuse()
    {
        var inactifId = Guid.NewGuid();
        _utilisateurs.Setup(r => r.ObtenirInfoAsync(inactifId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UtilisateurInfo(
                inactifId, "inactif@client.local", "inactif@client.local", "Client Inactif", false, NatureUtilisateur.Client));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _command.ExecuterAsync(inactifId, new CloturerMonCompteRequete("MonMotDePasse123!", null)));

        _comptes.Verify(c => c.DefinirActivationAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UtilisateurInexistant_EstRefuseAvecAccesRefuseException()
    {
        var inconnuId = Guid.NewGuid();
        _utilisateurs.Setup(r => r.ObtenirInfoAsync(inconnuId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UtilisateurInfo?)null);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(inconnuId, new CloturerMonCompteRequete("MonMotDePasse123!", null)));

        _comptes.Verify(c => c.DefinirActivationAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
