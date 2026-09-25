using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

/// <summary>
/// LOT 2 — Enrôlement société (retrait). Une société révoque des accès de sa flotte : carte d'un
/// chauffeur qui a quitté, BAT d'un camion vendu. Même modèle de regroupement que l'ajout (un
/// dossier unique, un ticket par objet), mais sens = Retrait et aucune pièce à déposer (le retrait
/// référence un objet déjà enrôlé, cf. formulaires CIT-CIV-CG-F-0001/F-0002).
/// </summary>
public class SoumettreRetraitEnrolementSocieteCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<IFileDeTraitementRepository> _fileRepo = new();
    private readonly Mock<IUtilisateurSocieteRepository> _utilisateurSocieteRepo = new();
    private readonly Mock<IServiceNumerotation> _numerotation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly SoumettreRetraitEnrolementSocieteCommand _command;
    private readonly FileDeTraitement _fileAgentGate = FileDeTraitement.Creer("Enrolement", "Agent Gate");

    public SoumettreRetraitEnrolementSocieteCommandTests()
    {
        _command = new SoumettreRetraitEnrolementSocieteCommand(
            _ticketRepo.Object,
            _dossierRepo.Object,
            _fileRepo.Object,
            _utilisateurSocieteRepo.Object,
            _numerotation.Object,
            _journalisation.Object,
            _horloge.Object,
            _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 9, 25, 11, 0, 0, DateTimeKind.Utc));
        _numerotation.Setup(n => n.ProchainNumeroDossierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("DOS-ENR-200");
        _fileRepo.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_fileAgentGate]);

        var compteur = 0;
        _numerotation.Setup(n => n.ProchainNumeroTicketAsync("Enrolement", "Enrolement", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"TCK-ENR-R{++compteur:000}");
    }

    private void RattacherAvecDroit(Guid userId, Guid societeId, bool droit = true, bool actif = true)
    {
        var rattachement = UtilisateurSociete.Rattacher(userId, societeId, DateTime.UtcNow, droitSoumission: droit);
        if (!actif)
        {
            rattachement.Desactiver();
        }

        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rattachement);
    }

    private static SoumettreRetraitEnrolementSocieteRequete RequeteRetrait(Guid societeId) => new(
        societeId,
        [
            new ObjetEnrolementSoumission(TypeObjetEnrolement.Chauffeur, "KONE Ali", "A quitté la société."),
            new ObjetEnrolementSoumission(TypeObjetEnrolement.Camion, "AA-123-BC", "Camion vendu."),
        ]);

    [Fact]
    public async Task Societe_habilitee_cree_un_dossier_unique_et_un_ticket_de_retrait_par_objet()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);

        var resultat = await _command.ExecuterAsync(userId, RequeteRetrait(societeId), CancellationToken.None);

        Assert.Equal("DOS-ENR-200", resultat.NumeroDossier);
        Assert.Equal(2, resultat.Tickets.Count);

        _dossierRepo.Verify(r => r.AjouterAsync(
            It.Is<Dossier>(d => d.ClientId == societeId && d.AuteurId == userId),
            It.IsAny<CancellationToken>()), Times.Once);

        _ticketRepo.Verify(r => r.AjouterAsync(
            It.Is<Ticket>(t =>
                t.Module == "Enrolement" &&
                t.DossierId == resultat.DossierId &&
                t.FileCouranteId == _fileAgentGate.Id),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Chaque_ticket_porte_le_sens_retrait_et_le_type_objet()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);

        await _command.ExecuterAsync(userId, RequeteRetrait(societeId), CancellationToken.None);

        _ticketRepo.Verify(r => r.AjouterAsync(
            It.Is<Ticket>(t =>
                t.ObjetEnrolement == TypeObjetEnrolement.Chauffeur &&
                t.SensEnrolement == SensEnrolement.Retrait &&
                t.Objet == "KONE Ali" &&
                t.Description == "A quitté la société."),
            It.IsAny<CancellationToken>()), Times.Once);

        _ticketRepo.Verify(r => r.AjouterAsync(
            It.Is<Ticket>(t =>
                t.ObjetEnrolement == TypeObjetEnrolement.Camion &&
                t.SensEnrolement == SensEnrolement.Retrait &&
                t.Objet == "AA-123-BC"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sans_rattachement_actif_leve_AccesRefuse_et_n_ecrit_rien()
    {
        var userId = Guid.NewGuid();
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UtilisateurSociete?)null);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(userId, RequeteRetrait(Guid.NewGuid()), CancellationToken.None));

        _ticketRepo.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        _dossierRepo.Verify(r => r.AjouterAsync(It.IsAny<Dossier>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Soumission_pour_une_autre_societe_est_refusee_usurpation()
    {
        var userId = Guid.NewGuid();
        var vraieSociete = Guid.NewGuid();
        var autreSociete = Guid.NewGuid();
        RattacherAvecDroit(userId, vraieSociete);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(userId, RequeteRetrait(autreSociete), CancellationToken.None));
    }

    [Fact]
    public async Task Aucun_objet_leve_ArgumentException()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);

        var requete = new SoumettreRetraitEnrolementSocieteRequete(societeId, Array.Empty<ObjetEnrolementSoumission>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(userId, requete, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "Motif valide.")]
    [InlineData("Objet valide", "")]
    public async Task Objet_ou_motif_vide_leve_ArgumentException(string objet, string motif)
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);

        var requete = new SoumettreRetraitEnrolementSocieteRequete(
            societeId,
            [new ObjetEnrolementSoumission(TypeObjetEnrolement.Chauffeur, objet, motif)]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(userId, requete, CancellationToken.None));

        _ticketRepo.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
