using System.Text.Json;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.AvoirExoneration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;

namespace CIT.Plateforme.Application.Tests.AvoirExoneration;

public sealed class SoumettreDemandeAvoirExoTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<IFileDeTraitementRepository> _fileRepo = new();
    private readonly Mock<ILiaisonTicketRepository> _liaisonRepo = new();
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IStockageDocuments> _stockage = new();
    private readonly Mock<IUtilisateurSocieteRepository> _utilisateurSocieteRepo = new();
    private readonly Mock<IServiceNumerotation> _numerotation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly SoumettreDemandeAvoirExonerationCommand _command;
    private readonly FileDeTraitement _fileAvoirExo = FileDeTraitement.Creer("AvoirExoneration", "Avoir & Exonération");
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _societeId = Guid.NewGuid();
    private readonly DateTime _maintenantUtc = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    public SoumettreDemandeAvoirExoTests()
    {
        _command = new SoumettreDemandeAvoirExonerationCommand(
            _ticketRepo.Object,
            _dossierRepo.Object,
            _fileRepo.Object,
            _liaisonRepo.Object,
            _documentRepo.Object,
            _stockage.Object,
            _utilisateurSocieteRepo.Object,
            _numerotation.Object,
            _journalisation.Object,
            _horloge.Object,
            _autorisation.Object,
            _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(_maintenantUtc);
        _numerotation.Setup(n => n.ProchainNumeroTicketAsync("AvoirExoneration", "Avoir", It.IsAny<CancellationToken>()))
            .ReturnsAsync("AVO-2026-00001");
        _numerotation.Setup(n => n.ProchainNumeroTicketAsync("AvoirExoneration", "Exoneration", It.IsAny<CancellationToken>()))
            .ReturnsAsync("EXO-2026-00001");
        _numerotation.Setup(n => n.ProchainNumeroDossierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("DOS-2026-00001");
        _fileRepo.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_fileAvoirExo]);
        _utilisateurSocieteRepo.Setup(u => u.ObtenirParUtilisateurAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(_userId, _societeId, _maintenantUtc, true));
        _stockage.Setup(s => s.EnregistrerAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("documents/avo/test.pdf");
    }

    [Fact]
    public async Task Soumettre_Avoir_Nominal_CreeTicketEtDossierEtPublieEvenement()
    {
        // Arrange
        var piece = new PieceJointeSoumission("facture.pdf", "application/pdf", [1, 2, 3]);
        var requete = new SoumettreDemandeAvoirExoRequete(
            TypeDemande: "Avoir",
            Objet: "Contestation facturation",
            Description: "Erreur montant manutention",
            MontantConteste: 150000m,
            ReferenceMetier: "BL-998877",
            TicketParentId: null,
            PiecesJointes: [piece]);

        // Act
        var reponse = await _command.ExecuterAsync(_userId, requete, CancellationToken.None);

        // Assert
        Assert.NotNull(reponse);
        Assert.Equal("AVO-2026-00001", reponse.NumeroTicket);
        Assert.Equal("DOS-2026-00001", reponse.NumeroDossier);

        _dossierRepo.Verify(d => d.AjouterAsync(
            It.Is<Dossier>(dos => dos.ClientId == _societeId && dos.AuteurId == _userId),
            It.IsAny<CancellationToken>()), Times.Once);

        _ticketRepo.Verify(t => t.AjouterAsync(
            It.Is<Ticket>(tck =>
                tck.Module == "AvoirExoneration" &&
                tck.TypeDemande == "Avoir" &&
                tck.Objet == "Contestation facturation" &&
                tck.MontantConteste == 150000m &&
                tck.Statut == StatutTicket.EnAttenteDePriseEnCharge),
            It.IsAny<CancellationToken>()), Times.Once);

        _documentRepo.Verify(d => d.AjouterAsync(
            It.IsAny<Document>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _journalisation.Verify(j => j.PublierEvenementAsync(
            "DemandeAvoirSoumise",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Soumettre_Exoneration_Nominal_CreeTicketExo()
    {
        // Arrange
        var piece = new PieceJointeSoumission("attestation.pdf", "application/pdf", [1, 2, 3]);
        var requete = new SoumettreDemandeAvoirExoRequete(
            TypeDemande: "Exoneration",
            Objet: "Demande franchise",
            Description: "Exonération TVA accord ministériel",
            MontantConteste: null,
            ReferenceMetier: null,
            TicketParentId: null,
            PiecesJointes: [piece]);

        // Act
        var reponse = await _command.ExecuterAsync(_userId, requete, CancellationToken.None);

        // Assert
        Assert.Equal("EXO-2026-00001", reponse.NumeroTicket);
        _journalisation.Verify(j => j.PublierEvenementAsync(
            "DemandeExonerationSoumise",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Soumettre_AvecLiaisonParent_MemeSociete_CreeLiaisonTicket()
    {
        // Arrange
        var parentTicketId = Guid.NewGuid();
        var parentDossierId = Guid.NewGuid();
        var parentDossier = Dossier.Creer("DOS-PAR-001", _societeId, _maintenantUtc, _userId);
        var parentTicket = Ticket.Creer("TCK-IMP-001", "BillingImport", "Import", _fileAvoirExo.Id, parentDossier.Id, _maintenantUtc);

        _ticketRepo.Setup(t => t.ObtenirParIdAsync(parentTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentTicket);
        _dossierRepo.Setup(d => d.ObtenirParIdAsync(parentTicket.DossierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentDossier);

        var piece = new PieceJointeSoumission("doc.pdf", "application/pdf", [1, 2, 3]);
        var requete = new SoumettreDemandeAvoirExoRequete(
            TypeDemande: "Avoir",
            Objet: "Avoir lié à l'import",
            Description: "Détail",
            MontantConteste: 50000m,
            ReferenceMetier: null,
            TicketParentId: parentTicketId,
            PiecesJointes: [piece]);

        // Act
        var reponse = await _command.ExecuterAsync(_userId, requete, CancellationToken.None);

        // Assert
        Assert.NotNull(reponse);
        _liaisonRepo.Verify(l => l.AjouterAsync(
            It.Is<LiaisonTicket>(lt => lt.TicketLieId == parentTicketId && lt.CreeParId == _userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Soumettre_LiaisonParent_AutreSociete_LeveAccesRefuseException()
    {
        // Arrange (Parent appartenant à une autre société)
        var autreSocieteId = Guid.NewGuid();
        var parentTicketId = Guid.NewGuid();
        var parentDossier = Dossier.Creer("DOS-AUTRE-001", autreSocieteId, _maintenantUtc, Guid.NewGuid());
        var parentTicket = Ticket.Creer("TCK-EXP-001", "BillingExport", "Export", _fileAvoirExo.Id, parentDossier.Id, _maintenantUtc);

        _ticketRepo.Setup(t => t.ObtenirParIdAsync(parentTicketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentTicket);
        _dossierRepo.Setup(d => d.ObtenirParIdAsync(parentTicket.DossierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentDossier);

        var piece = new PieceJointeSoumission("doc.pdf", "application/pdf", [1, 2, 3]);
        var requete = new SoumettreDemandeAvoirExoRequete(
            TypeDemande: "Avoir",
            Objet: "Avoir illégitime",
            Description: "Tentative liaison",
            MontantConteste: 50000m,
            ReferenceMetier: null,
            TicketParentId: parentTicketId,
            PiecesJointes: [piece]);

        // Act & Assert (SEC-AVE-02)
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(_userId, requete, CancellationToken.None));
    }

    [Fact]
    public async Task Soumettre_SansPieceJointe_LeveArgumentException()
    {
        var requete = new SoumettreDemandeAvoirExoRequete(
            TypeDemande: "Avoir",
            Objet: "Objet",
            Description: "Desc",
            MontantConteste: 1000m,
            ReferenceMetier: null,
            TicketParentId: null,
            PiecesJointes: []);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(_userId, requete, CancellationToken.None));
    }
}
