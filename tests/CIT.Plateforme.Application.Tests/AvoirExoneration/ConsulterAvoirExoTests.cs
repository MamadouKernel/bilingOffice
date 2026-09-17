using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.AvoirExoneration;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.AvoirExoneration;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;

namespace CIT.Plateforme.Application.Tests.AvoirExoneration;

public sealed class ConsulterAvoirExoTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<ILiaisonTicketRepository> _liaisonRepo = new();
    private readonly Mock<IMessageRepository> _messageRepo = new();
    private readonly Mock<IHistoriqueTicketRepository> _historiqueRepo = new();
    private readonly Mock<IUtilisateurSocieteRepository> _utilisateurSocieteRepo = new();
    private readonly Mock<ISocieteClienteRepository> _societeRepo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IHabilitationRepository> _habilitationRepo = new();
    private readonly Mock<IPerimetreSupervisionRepository> _perimetreRepo = new();
    private readonly Mock<IUtilisateurInfoRepository> _utilisateurInfoRepo = new();

    private readonly ListerMesDemandesAvoirExonerationQuery _mesDemandesQuery;
    private readonly ConsulterFileAvoirExonerationQuery _fileQuery;
    private readonly ObtenirFicheAvoirExonerationQuery _ficheQuery;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _societeId = Guid.NewGuid();
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly DateTime _maintenantUtc = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    public ConsulterAvoirExoTests()
    {
        _mesDemandesQuery = new ListerMesDemandesAvoirExonerationQuery(
            _ticketRepo.Object,
            _dossierRepo.Object,
            _utilisateurSocieteRepo.Object);

        _fileQuery = new ConsulterFileAvoirExonerationQuery(
            _ticketRepo.Object,
            _dossierRepo.Object,
            _societeRepo.Object,
            _autorisation.Object);

        _ficheQuery = new ObtenirFicheAvoirExonerationQuery(
            _ticketRepo.Object,
            _dossierRepo.Object,
            _societeRepo.Object,
            _documentRepo.Object,
            _liaisonRepo.Object,
            _messageRepo.Object,
            _historiqueRepo.Object,
            _utilisateurSocieteRepo.Object,
            _autorisation.Object,
            new ControleAccesRessourceService(
                _dossierRepo.Object,
                _ticketRepo.Object,
                _utilisateurSocieteRepo.Object,
                _habilitationRepo.Object,
                _perimetreRepo.Object,
                new GardeIdentiteService(_utilisateurInfoRepo.Object)));

        // CENTRALISATION P2-01 — la fiche délègue désormais le contrôle générique. L'appelant doit
        // donc être un compte RÉEL et ACTIF pour que le refus testé plus bas porte bien sur la
        // propriété du dossier, et non sur une identité que le montage aurait oublié de décrire.
        _utilisateurInfoRepo.Setup(u => u.ObtenirInfoAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UtilisateurInfo(_userId, "client@avoirexo.test", "client@avoirexo.test", "Client", true, NatureUtilisateur.Client));
        _habilitationRepo.Setup(h => h.ListerParUtilisateurAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HabilitationUtilisateur>());

        _utilisateurSocieteRepo.Setup(u => u.ObtenirParUtilisateurAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(_userId, _societeId, _maintenantUtc, true));
    }

    [Fact]
    public async Task ListerMesDemandes_FiltreStrictementSurSocieteDuDemandeur()
    {
        // Arrange
        var dossierClient = Dossier.Creer("DOS-001", _societeId, _maintenantUtc, _userId);
        var tckAvoir = Ticket.CreerDemandeAvoirExo("AVO-001", "Avoir", Guid.NewGuid(), dossierClient.Id, "Obj1", "Desc1", 100m, _maintenantUtc);

        _dossierRepo.Setup(d => d.ListerParSocieteAsync(_societeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([dossierClient]);
        _ticketRepo.Setup(t => t.ListerParDossierAsync(dossierClient.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([tckAvoir]);

        // Act
        var resultats = await _mesDemandesQuery.ExecuterAsync(_userId, CancellationToken.None);

        // Assert
        Assert.Single(resultats);
        Assert.Equal("AVO-001", resultats[0].NumeroTicket);
        Assert.Equal("Avoir", resultats[0].TypeDemande);
    }

    [Fact]
    public async Task ConsulterFile_AgentNonHabilite_LeveAccesRefuseException()
    {
        // Arrange (SEC-AVE-03)
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.AvoirInstruire, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var criteres = new CriteresFiltreFileAvoirExo(null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _fileQuery.ExecuterAsync(_agentId, criteres, CancellationToken.None));
    }

    [Fact]
    public async Task ObtenirFiche_ClientAutreSociete_LeveAccesRefuseException()
    {
        // Arrange (SEC-AVE-01)
        var autreSocieteId = Guid.NewGuid();
        var dossierAutre = Dossier.Creer("DOS-AUTRE", autreSocieteId, _maintenantUtc, Guid.NewGuid());
        var ticket = Ticket.CreerDemandeAvoirExo("AVO-002", "Avoir", Guid.NewGuid(), dossierAutre.Id, "Obj", "Desc", null, _maintenantUtc);

        _ticketRepo.Setup(t => t.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
        _dossierRepo.Setup(d => d.ObtenirParIdAsync(ticket.DossierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dossierAutre);
        _autorisation.Setup(a => a.EstAutoriseAsync(_userId, Permission.AvoirInstruire, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _ficheQuery.ExecuterAsync(_userId, ticket.Id, CancellationToken.None));
    }
}
