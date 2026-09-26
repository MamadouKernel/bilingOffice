using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.SupportTechnique;
using CIT.Plateforme.Domain.Incidents;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.SupportTechnique;

/// <summary>
/// [TEST-SPEC — SUPPORT IT] Déclaration d'un incident technique. À la différence d'une réclamation,
/// la déclaration n'exige aucune société : tout utilisateur authentifié peut signaler une panne.
/// L'incident appartient à son déclarant (client = auteur = appelant), et route vers la file cible
/// de sa catégorie.
/// </summary>
public class SoumettreIncidentCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<ICategorieIncidentRepository> _categorieRepo = new();
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IServiceNumerotation> _numerotation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();

    private readonly SoumettreIncidentCommand _command;

    public SoumettreIncidentCommandTests()
    {
        _command = new SoumettreIncidentCommand(
            _ticketRepo.Object,
            _categorieRepo.Object,
            _dossierRepo.Object,
            _uniteDeTravail.Object,
            _horloge.Object,
            _numerotation.Object,
            _journalisation.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc));
        _numerotation.Setup(n => n.ProchainNumeroTicketAsync("SupportTechnique", "Incident", It.IsAny<CancellationToken>()))
            .ReturnsAsync("TCK-INC-001");
        _numerotation.Setup(n => n.ProchainNumeroDossierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("DOS-INC-001");
    }

    [Fact]
    public async Task Utilisateur_authentifie_avec_categorie_active_cree_incident_sans_societe()
    {
        var userId = Guid.NewGuid();
        var fileCibleId = Guid.NewGuid();
        var categorie = CategorieIncident.Creer("CONNEXION", "Connexion", fileCibleId);

        _categorieRepo.Setup(r => r.ObtenirParIdAsync(categorie.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorie);

        var requete = new SoumettreIncidentRequete(categorie.Id, GraviteIncident.Critique, "Connexion impossible", "Le code 2FA n'arrive jamais.");

        var resultat = await _command.ExecuterAsync(userId, requete, CancellationToken.None);

        Assert.Equal("TCK-INC-001", resultat.NumeroTicket);

        // Incident rattaché au déclarant, sans société : client = auteur = appelant.
        _dossierRepo.Verify(r => r.AjouterAsync(It.Is<Dossier>(d => d.ClientId == userId && d.AuteurId == userId), It.IsAny<CancellationToken>()), Times.Once);
        _ticketRepo.Verify(r => r.AjouterAsync(It.Is<Ticket>(t =>
            t.Module == "SupportTechnique" &&
            t.CategorieIncidentId == categorie.Id &&
            t.Gravite == GraviteIncident.Critique &&
            t.FileCouranteId == fileCibleId &&
            t.Objet == "Connexion impossible"), It.IsAny<CancellationToken>()), Times.Once);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Appelant_non_authentifie_est_refuse()
    {
        var requete = new SoumettreIncidentRequete(Guid.NewGuid(), GraviteIncident.Mineure, "Objet", "Description");
        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(Guid.Empty, requete, CancellationToken.None));
    }

    [Fact]
    public async Task Categorie_inexistante_est_refusee()
    {
        _categorieRepo.Setup(r => r.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategorieIncident?)null);
        var requete = new SoumettreIncidentRequete(Guid.NewGuid(), GraviteIncident.Majeure, "Objet", "Description");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(Guid.NewGuid(), requete, CancellationToken.None));
    }

    [Fact]
    public async Task Categorie_inactive_est_refusee()
    {
        var categorie = CategorieIncident.Creer("AUTRE", "Autre", Guid.NewGuid());
        categorie.Desactiver();
        _categorieRepo.Setup(r => r.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorie);
        var requete = new SoumettreIncidentRequete(categorie.Id, GraviteIncident.Mineure, "Objet", "Description");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(Guid.NewGuid(), requete, CancellationToken.None));
    }

    [Fact]
    public async Task Categorie_sans_file_cible_est_refusee()
    {
        var categorie = CategorieIncident.Creer("AUTRE", "Autre", null);
        _categorieRepo.Setup(r => r.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorie);
        var requete = new SoumettreIncidentRequete(categorie.Id, GraviteIncident.Mineure, "Objet", "Description");
        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(Guid.NewGuid(), requete, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "Description valide.")]
    [InlineData("Objet valide", "")]
    public async Task Contenu_obligatoire_absent_leve_une_validation_et_n_ecrit_rien(string objet, string description)
    {
        var categorie = CategorieIncident.Creer("CONNEXION", "Connexion", Guid.NewGuid());
        _categorieRepo.Setup(r => r.ObtenirParIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categorie);
        var requete = new SoumettreIncidentRequete(categorie.Id, GraviteIncident.Mineure, objet, description);

        await Assert.ThrowsAsync<ArgumentException>(() => _command.ExecuterAsync(Guid.NewGuid(), requete, CancellationToken.None));
        _ticketRepo.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
