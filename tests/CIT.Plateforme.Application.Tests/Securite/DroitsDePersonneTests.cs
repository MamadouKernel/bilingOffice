using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// SEC-06 (« droits des personnes ») : accès (export) et effacement (anonymisation) exercés en
/// self-service par un administrateur de société, sur le même modèle d'autorisation que
/// <c>SoumettreReclamationCommand</c> — le droit est individuel, porté par le rattachement
/// (<see cref="UtilisateurSociete.EstAdministrateur"/>), jamais par une permission RBAC globale.
/// </summary>
public sealed class DroitsDePersonneTests
{
    private readonly Mock<IUtilisateurSocieteRepository> _utilisateurSocieteRepo = new();
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IDocumentRepository> _documentRepo = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    private readonly Mock<IJournalisationService> _journalisation = new();

    private static readonly DateTime Maintenant = new(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);

    public DroitsDePersonneTests() => _horloge.Setup(h => h.MaintenantUtc).Returns(Maintenant);

    private ExporterDonneesSocieteQuery ConstruireExportQuery() => new(
        _utilisateurSocieteRepo.Object, _dossierRepo.Object, _ticketRepo.Object, _documentRepo.Object, _horloge.Object);

    private AnonymiserDossierCommand ConstruireAnonymiserCommand() => new(
        _utilisateurSocieteRepo.Object, _dossierRepo.Object, _ticketRepo.Object, _horloge.Object, _uniteDeTravail.Object, _journalisation.Object);

    [Fact]
    public async Task Export_Administrateur_societe_obtient_ses_dossiers_tickets_et_documents()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, Maintenant, estAdministrateur: true));

        var dossier = Dossier.Creer("DOS-000001", societeId, Maintenant);
        _dossierRepo.Setup(r => r.ListerParSocieteAsync(societeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([dossier]);

        var ticket = Ticket.Creer("TCK-000001", "CustomerComplaint", "Reclamation", Guid.NewGuid(), dossier.Id, Maintenant);
        ticket.DefinirContenuReclamation("Objet test", "Description test");
        _ticketRepo.Setup(r => r.ListerParDossierAsync(dossier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([ticket]);

        var document = Document.Deposer(ticket.Id, userId, "facture.pdf", "application/pdf", 1024, "cle-1", Maintenant);
        _documentRepo.Setup(r => r.ListerParTicketAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([document]);

        var resultat = await ConstruireExportQuery().ExecuterAsync(userId, CancellationToken.None);

        Assert.Equal(societeId, resultat.SocieteId);
        var dossierVue = Assert.Single(resultat.Dossiers);
        Assert.Equal("DOS-000001", dossierVue.Numero);
        var ticketVue = Assert.Single(dossierVue.Tickets);
        Assert.Equal("TCK-000001", ticketVue.Numero);
        Assert.Equal("Objet test", ticketVue.Objet);
        var documentVue = Assert.Single(ticketVue.Documents);
        Assert.Equal("facture.pdf", documentVue.NomFichier);
    }

    [Fact]
    public async Task Export_Utilisateur_non_administrateur_est_refuse()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, Maintenant, estAdministrateur: false));

        await Assert.ThrowsAsync<AccesRefuseException>(() => ConstruireExportQuery().ExecuterAsync(userId, CancellationToken.None));
    }

    [Fact]
    public async Task Anonymiser_Administrateur_societe_anonymise_un_dossier_clos_de_sa_societe()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, Maintenant, estAdministrateur: true));

        var dossier = Dossier.Creer("DOS-000002", societeId, Maintenant);
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dossier);

        var ticket = Ticket.Creer("TCK-000002", "CustomerComplaint", "Reclamation", Guid.NewGuid(), dossier.Id, Maintenant);
        ticket.DefinirContenuReclamation("Objet personnel", "Description personnelle");
        ticket.Cloturer(MotifClotureType.Normale, Maintenant);
        _ticketRepo.Setup(r => r.ListerParDossierAsync(dossier.Id, It.IsAny<CancellationToken>())).ReturnsAsync([ticket]);

        var nombre = await ConstruireAnonymiserCommand().ExecuterAsync(userId, dossier.Id, CancellationToken.None);

        Assert.Equal(1, nombre);
        Assert.True(ticket.EstAnonymise);
        Assert.Equal(Ticket.ValeurAnonymisee, ticket.Objet);
        Assert.Equal(Ticket.ValeurAnonymisee, ticket.Description);
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Anonymiser_Dossier_dune_autre_societe_est_refuse_cloisonnement()
    {
        var userId = Guid.NewGuid();
        var societeAppelant = Guid.NewGuid();
        var societeProprietaire = Guid.NewGuid();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeAppelant, Maintenant, estAdministrateur: true));

        var dossierDuTiers = Dossier.Creer("DOS-000003", societeProprietaire, Maintenant);
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossierDuTiers.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dossierDuTiers);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => ConstruireAnonymiserCommand().ExecuterAsync(userId, dossierDuTiers.Id, CancellationToken.None));

        _ticketRepo.Verify(r => r.ListerParDossierAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Anonymiser_Dossier_non_entierement_cloture_est_refuse()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, Maintenant, estAdministrateur: true));

        var dossier = Dossier.Creer("DOS-000004", societeId, Maintenant);
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dossier);

        var ticketEnCours = Ticket.Creer("TCK-000004", "CustomerComplaint", "Reclamation", Guid.NewGuid(), dossier.Id, Maintenant);
        _ticketRepo.Setup(r => r.ListerParDossierAsync(dossier.Id, It.IsAny<CancellationToken>())).ReturnsAsync([ticketEnCours]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ConstruireAnonymiserCommand().ExecuterAsync(userId, dossier.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Anonymiser_Utilisateur_non_administrateur_est_refuse()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(userId, societeId, Maintenant, estAdministrateur: false));

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => ConstruireAnonymiserCommand().ExecuterAsync(userId, Guid.NewGuid(), CancellationToken.None));
    }
}
