using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.ActualisationBadt;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

public class CreerDemandeActualisationBadtCommandTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IDossierRepository> _dossiers = new();
    private readonly Mock<ILigneActualisationBadtRepository> _lignes = new();
    private readonly Mock<IFileDeTraitementRepository> _files = new();
    private readonly Mock<IUtilisateurSocieteRepository> _utilisateurSocietes = new();
    private readonly Mock<IServiceNumerotation> _numerotation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IMesureActualisationBadtRepository> _mesures = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly CreerDemandeActualisationBadtCommand _command;

    public CreerDemandeActualisationBadtCommandTests()
    {
        _command = new CreerDemandeActualisationBadtCommand(
            _tickets.Object, _dossiers.Object, _lignes.Object, _files.Object,
            _utilisateurSocietes.Object, _numerotation.Object, _journalisation.Object,
            _horloge.Object, _mesures.Object, _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Utc));
        _numerotation.Setup(n => n.ProchainNumeroDossierAsync(It.IsAny<CancellationToken>())).ReturnsAsync("DOS-BADT-001");
        _numerotation.Setup(n => n.ProchainNumeroTicketAsync(Ticket.ModuleActualisationBadt, Ticket.ModuleActualisationBadt, It.IsAny<CancellationToken>()))
            .ReturnsAsync("TCK-BADT-001");
    }

    private static IReadOnlyList<LigneDemandeBadt> UneLigne() => [new("BL123", "TCLU1234567")];

    [Fact]
    public async Task Cree_dossier_ticket_et_une_ligne_par_conteneur_dans_la_file_BADT()
    {
        var demandeurId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var fileBadt = FileDeTraitement.Creer(Ticket.ModuleActualisationBadt, CreerDemandeActualisationBadtCommand.NomFile);

        _utilisateurSocietes.Setup(r => r.ObtenirParUtilisateurAsync(demandeurId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(demandeurId, societeId, DateTime.UtcNow, droitSoumission: true));
        _files.Setup(r => r.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([fileBadt]);

        var requete = new CreerDemandeActualisationBadtRequete(demandeurId, "BADT expiré sur ce conteneur.",
            [new("BL123", "TCLU1234567"), new("BL123", "TCLU7654321")]);

        var ticket = await _command.ExecuterAsync(requete, CancellationToken.None);

        Assert.Equal("TCK-BADT-001", ticket.Numero);
        Assert.Equal(Ticket.ModuleActualisationBadt, ticket.Module);
        _dossiers.Verify(r => r.AjouterAsync(It.Is<Dossier>(d => d.ClientId == societeId && d.AuteurId == demandeurId), It.IsAny<CancellationToken>()), Times.Once);
        _tickets.Verify(r => r.AjouterAsync(It.Is<Ticket>(t => t.FileCouranteId == fileBadt.Id), It.IsAny<CancellationToken>()), Times.Once);
        _lignes.Verify(r => r.AjouterAsync(It.IsAny<LigneActualisationBadt>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Le CLIENT est notifié (TicketCree) ET les AGENTS de la file BADT (TicketArriveeEnFile) :
        // sans ce second événement, le Support Client ne reçoit rien (régression du 16/10/2026).
        _journalisation.Verify(j => j.PublierEvenementAsync("TicketCree", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _journalisation.Verify(j => j.PublierEvenementAsync("TicketArriveeEnFile", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sans_rattachement_le_demandeur_fait_office_de_societe()
    {
        var demandeurId = Guid.NewGuid();
        var fileBadt = FileDeTraitement.Creer(Ticket.ModuleActualisationBadt, CreerDemandeActualisationBadtCommand.NomFile);

        _utilisateurSocietes.Setup(r => r.ObtenirParUtilisateurAsync(demandeurId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UtilisateurSociete?)null);
        _files.Setup(r => r.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([fileBadt]);

        await _command.ExecuterAsync(new CreerDemandeActualisationBadtRequete(demandeurId, "Récit.", UneLigne()), CancellationToken.None);

        _dossiers.Verify(r => r.AjouterAsync(It.Is<Dossier>(d => d.ClientId == demandeurId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task File_non_approvisionnee_leve_une_erreur_avant_toute_ecriture()
    {
        var demandeurId = Guid.NewGuid();
        _utilisateurSocietes.Setup(r => r.ObtenirParUtilisateurAsync(demandeurId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UtilisateurSociete?)null);
        _files.Setup(r => r.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _command.ExecuterAsync(new CreerDemandeActualisationBadtRequete(demandeurId, "Récit.", UneLigne()), CancellationToken.None));

        _tickets.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Aucun_conteneur_leve_une_validation_metier()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(new CreerDemandeActualisationBadtRequete(Guid.NewGuid(), "Récit.", []), CancellationToken.None));
    }
}
