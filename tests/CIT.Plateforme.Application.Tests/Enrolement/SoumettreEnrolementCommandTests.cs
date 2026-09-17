using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — T015-T017. C8-01 (contracts/use-cases.md).
public class SoumettreEnrolementCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<IFileDeTraitementRepository> _fileRepo = new();
    private readonly Mock<IChampFormulaireEnrolementRepository> _champsFormulaireRepo = new();
    private readonly Mock<IValeurChampEnrolementRepository> _valeursChampRepo = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IServiceNumerotation> _numerotation = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();

    private readonly SoumettreEnrolementCommand _command;
    private readonly FileDeTraitement _fileAgentGate = FileDeTraitement.Creer("Enrolement", "Agent Gate");

    public SoumettreEnrolementCommandTests()
    {
        _command = new SoumettreEnrolementCommand(
            _ticketRepo.Object,
            _dossierRepo.Object,
            _fileRepo.Object,
            _champsFormulaireRepo.Object,
            _valeursChampRepo.Object,
            _numerotation.Object,
            _journalisation.Object,
            _horloge.Object,
            _autorisation.Object,
            _uniteDeTravail.Object);

        _champsFormulaireRepo.Setup(c => c.ListerAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CIT.Plateforme.Domain.Enrolement.ChampFormulaireEnrolement>());
        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 8, 31, 10, 0, 0, DateTimeKind.Utc));
        _numerotation.Setup(n => n.ProchainNumeroTicketAsync("Enrolement", "Enrolement", It.IsAny<CancellationToken>()))
            .ReturnsAsync("TCK-ENR-001");
        _numerotation.Setup(n => n.ProchainNumeroDossierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("DOS-ENR-001");
        _fileRepo.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_fileAgentGate]);
    }

    [Fact]
    public async Task A_Utilisateur_authentifie_et_habilite_soumission_reussie()
    {
        var appelantId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(appelantId, Permission.EnrolementSoumettre, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var requete = new SoumettreEnrolementRequete("Enrôlement chauffeur", "Camion frigorifique, dépôt d'Abidjan.");

        var resultat = await _command.ExecuterAsync(appelantId, requete, CancellationToken.None);

        Assert.NotNull(resultat);
        Assert.Equal("TCK-ENR-001", resultat.NumeroTicket);

        // B. Identité dérivée du contexte serveur (appelantId), jamais d'un champ de la requête.
        _dossierRepo.Verify(r => r.AjouterAsync(
            It.Is<Dossier>(d => d.ClientId == appelantId && d.AuteurId == null),
            It.IsAny<CancellationToken>()), Times.Once);

        // F. Routage vers la file Agent Gate (Module="Enrolement").
        _ticketRepo.Verify(r => r.AjouterAsync(
            It.Is<Ticket>(t =>
                t.Module == "Enrolement" &&
                t.TypeDemande == "Enrolement" &&
                t.FileCouranteId == _fileAgentGate.Id &&
                t.Objet == "Enrôlement chauffeur" &&
                t.Description == "Camion frigorifique, dépôt d'Abidjan."),
            It.IsAny<CancellationToken>()), Times.Once);

        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);

        // G. Audit.
        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(It.IsAny<Guid>(), "TicketCree", It.IsAny<string?>(), appelantId, It.IsAny<CancellationToken>()), Times.Once);
        _journalisation.Verify(j => j.ConsignerAuditAsync(appelantId, "TicketCree", nameof(Ticket), It.IsAny<Guid>(), null, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task B_Utilisateur_sans_permission_EnrolementSoumettre_leve_AccesRefuse()
    {
        var appelantId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(appelantId, Permission.EnrolementSoumettre, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var requete = new SoumettreEnrolementRequete("Objet", "Description");

        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ExecuterAsync(appelantId, requete, CancellationToken.None));

        _ticketRepo.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", "Description valide.")]
    [InlineData("   ", "Description valide.")]
    [InlineData("Objet valide", "")]
    [InlineData("Objet valide", "   ")]
    public async Task D_Objet_ou_description_vide_leve_ArgumentException(string objet, string description)
    {
        var appelantId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(appelantId, Permission.EnrolementSoumettre, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var requete = new SoumettreEnrolementRequete(objet, description);

        await Assert.ThrowsAsync<ArgumentException>(() => _command.ExecuterAsync(appelantId, requete, CancellationToken.None));

        _ticketRepo.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FileAgentGate_NonParametree_LeveInvalidOperationException()
    {
        var appelantId = Guid.NewGuid();
        _autorisation.Setup(a => a.EstAutoriseAsync(appelantId, Permission.EnrolementSoumettre, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileRepo.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var requete = new SoumettreEnrolementRequete("Objet", "Description");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _command.ExecuterAsync(appelantId, requete, CancellationToken.None));
    }
}
