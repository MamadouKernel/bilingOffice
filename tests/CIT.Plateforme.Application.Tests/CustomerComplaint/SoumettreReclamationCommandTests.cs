using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.CustomerComplaint;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

public class SoumettreReclamationCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<ICategorieReclamationRepository> _categorieRepo = new();
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<IUtilisateurSocieteRepository> _utilisateurSocieteRepo = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IServiceNumerotation> _numerotation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();

    private readonly SoumettreReclamationCommand _command;

    public SoumettreReclamationCommandTests()
    {
        _command = new SoumettreReclamationCommand(
            _ticketRepo.Object,
            _categorieRepo.Object,
            _dossierRepo.Object,
            _utilisateurSocieteRepo.Object,
            _uniteDeTravail.Object,
            _horloge.Object,
            _numerotation.Object,
            _journalisation.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc));
        _numerotation.Setup(n => n.ProchainNumeroTicketAsync("CustomerComplaint", "Reclamation", It.IsAny<CancellationToken>()))
            .ReturnsAsync("TCK-REC-001");
        _numerotation.Setup(n => n.ProchainNumeroDossierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("DOS-REC-001");
    }

    [Fact]
    public async Task A_Client_authentifie_avec_droit_et_categorie_active_cree_reclamation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var categorieId = Guid.NewGuid();
        var categorie = CategorieReclamation.Creer("FACT", "Facturation", Guid.NewGuid());

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, DateTime.UtcNow, droitSoumission: true));

        _categorieRepo.Setup(r => r.ObtenirParIdAsync(categorieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorie);

        var request = new SoumettreReclamationRequete(societeId, categorieId, "Facture erronée", "Le montant facturé ne correspond pas au devis signé.");

        // Act
        var resultat = await _command.ExecuterAsync(userId, request, CancellationToken.None);

        // Assert
        Assert.NotNull(resultat);
        Assert.Equal("TCK-REC-001", resultat.NumeroTicket);

        _dossierRepo.Verify(r => r.AjouterAsync(It.Is<Dossier>(d => d.ClientId == societeId && d.AuteurId == userId), It.IsAny<CancellationToken>()), Times.Once);
        _ticketRepo.Verify(r => r.AjouterAsync(It.Is<Ticket>(t =>
            t.Module == "CustomerComplaint" &&
            t.CategorieReclamationId == categorie.Id &&
            t.FileCouranteId == categorie.FileCibleDefautId &&
            t.Objet == "Facture erronée" &&
            t.Description == "Le montant facturé ne correspond pas au devis signé."), It.IsAny<CancellationToken>()), Times.Once);

        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);

        // H. Audit de création présent
        // L'origine de la réclamation (canal de dépôt) est tracée à l'historique et à l'audit.
        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(It.IsAny<Guid>(), "TicketCree", "Réclamation soumise (origine : Portail)", userId, It.IsAny<CancellationToken>()), Times.Once);
        _journalisation.Verify(j => j.ConsignerAuditAsync(userId, "TicketCree", "Ticket", It.IsAny<Guid>(), null, "Portail", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task B_Categorie_inexistante_retourne_refus_metier()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, DateTime.UtcNow, droitSoumission: true));

        _categorieRepo.Setup(r => r.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategorieReclamation?)null);

        var request = new SoumettreReclamationRequete(societeId, Guid.NewGuid(), "Objet", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(userId, request, CancellationToken.None));
    }

    [Fact]
    public async Task C_Categorie_inactive_retourne_refus_metier()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var categorie = CategorieReclamation.Creer("FACT", "Facturation", null);
        categorie.Desactiver();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, DateTime.UtcNow, droitSoumission: true));

        _categorieRepo.Setup(r => r.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorie);

        var request = new SoumettreReclamationRequete(societeId, categorie.Id, "Objet", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(userId, request, CancellationToken.None));
    }

    [Fact]
    public async Task E_F_Societe_issue_du_contexte_interdit_usurpation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var vraieSocieteId = Guid.NewGuid();
        var fausseSocieteId = Guid.NewGuid(); // L'attaquant essaie de soumettre pour fausseSocieteId
        var categorie = CategorieReclamation.Creer("FACT", "Facturation", Guid.NewGuid());

        // Le repository retourne le VRAI rattachement (vraieSocieteId)
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, vraieSocieteId, DateTime.UtcNow, droitSoumission: true));

        _categorieRepo.Setup(r => r.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorie);

        var request = new SoumettreReclamationRequete(fausseSocieteId, categorie.Id, "Objet", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(userId, request, CancellationToken.None));
    }

    /// <summary>
    /// RÉGRESSION DÉCOUVERTE EN TEST MANUEL EN DIRECT (pas par lecture de code) : la commande
    /// vérifiait auparavant une permission RBAC (Permission.ComplaintSoumettre) qui n'était jamais
    /// accordée par le parcours réel de validation de compte société — aucune société ne pouvait
    /// donc jamais soumettre de réclamation en conditions réelles, malgré une suite de tests
    /// entièrement verte (chaque test simulait la permission comme toujours accordée). Corrigé en
    /// alignant sur ENT-02 : le droit de soumission est porté par le rattachement lui-même
    /// (UtilisateurSociete.DroitSoumission), comme pour BillingImport/BillingExport.
    /// </summary>
    [Fact]
    public async Task Utilisateur_sans_droit_de_soumission_retourne_AccesRefuse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, DateTime.UtcNow, droitSoumission: false));

        var request = new SoumettreReclamationRequete(societeId, Guid.NewGuid(), "Objet", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(userId, request, CancellationToken.None));
    }

    [Fact]
    public async Task Utilisateur_sans_rattachement_actif_retourne_AccesRefuse()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UtilisateurSociete?)null);

        var request = new SoumettreReclamationRequete(Guid.NewGuid(), Guid.NewGuid(), "Objet", "Description");

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(userId, request, CancellationToken.None));
    }

    // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.3) — REC-REM-02 : la validation
    // métier du contenu doit être appliquée par la Command, pas seulement par le Domain (défense
    // en profondeur avant toute écriture en base).
    [Theory]
    [InlineData("", "Description valide.")]
    [InlineData("   ", "Description valide.")]
    [InlineData("Objet valide", "")]
    [InlineData("Objet valide", "   ")]
    public async Task REC_REM_02_Contenu_obligatoire_absent_leve_une_validation_metier(string objet, string description)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var categorieId = Guid.NewGuid();
        var categorie = CategorieReclamation.Creer("FACT", "Facturation", Guid.NewGuid());

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, DateTime.UtcNow, droitSoumission: true));

        _categorieRepo.Setup(r => r.ObtenirParIdAsync(categorieId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorie);

        var request = new SoumettreReclamationRequete(societeId, categorieId, objet, description);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _command.ExecuterAsync(userId, request, CancellationToken.None));

        _ticketRepo.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
