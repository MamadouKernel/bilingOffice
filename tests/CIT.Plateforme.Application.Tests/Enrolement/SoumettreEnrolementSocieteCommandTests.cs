using System;
using System.Collections.Generic;
using System.Linq;
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
/// LOT 2 — Enrôlement société (ajout). Une société cliente monte un dossier unique pour sa flotte
/// et soumet, via l'application, un ticket par objet (chauffeur / camion). Le chauffeur ne se
/// déplace jamais : les pièces (permis + photo / carte grise) sont déposées dans le dossier.
/// Modèle d'autorisation ENT-02 (identique à la réclamation) : le droit de soumission est porté par
/// le rattachement société lui-même, jamais par une permission RBAC globale distincte.
/// </summary>
public class SoumettreEnrolementSocieteCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<IFileDeTraitementRepository> _fileRepo = new();
    private readonly Mock<IUtilisateurSocieteRepository> _utilisateurSocieteRepo = new();
    private readonly Mock<IServiceNumerotation> _numerotation = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();

    private readonly SoumettreEnrolementSocieteCommand _command;
    private readonly FileDeTraitement _fileAgentGate = FileDeTraitement.Creer("Enrolement", "Agent Gate");

    public SoumettreEnrolementSocieteCommandTests()
    {
        _command = new SoumettreEnrolementSocieteCommand(
            _ticketRepo.Object,
            _dossierRepo.Object,
            _fileRepo.Object,
            _utilisateurSocieteRepo.Object,
            _numerotation.Object,
            _journalisation.Object,
            _horloge.Object,
            _uniteDeTravail.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc));
        _numerotation.Setup(n => n.ProchainNumeroDossierAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("DOS-ENR-100");
        _fileRepo.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([_fileAgentGate]);

        // Numérotation séquentielle : un numéro distinct par ticket.
        var compteur = 0;
        _numerotation.Setup(n => n.ProchainNumeroTicketAsync("Enrolement", "Enrolement", It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => $"TCK-ENR-{++compteur:000}");
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

    private static SoumettreEnrolementSocieteRequete RequeteFlotte(Guid societeId) => new(
        societeId,
        [
            new ObjetEnrolementSoumission(TypeObjetEnrolement.Chauffeur, "KONE Ali", "Permis B, dépôt Abidjan.", Permis),
            new ObjetEnrolementSoumission(TypeObjetEnrolement.Chauffeur, "DIALLO Sekou", "Permis C, dépôt San-Pédro.", Permis),
            new ObjetEnrolementSoumission(TypeObjetEnrolement.Camion, "AA-123-BC", "Semi-remorque frigorifique.", DocumentsCamion),
        ]);

    private static readonly EcheanceDocumentSaisie[] Permis =
        [new(DocumentEnrolement.PermisConduire, new DateOnly(2028, 1, 31))];

    private static readonly EcheanceDocumentSaisie[] DocumentsCamion =
    [
        new(DocumentEnrolement.AssuranceVehicule, new DateOnly(2027, 6, 30)),
        new(DocumentEnrolement.VisiteTechnique, new DateOnly(2027, 3, 31)),
    ];

    /// <summary>03/10/2026 : les dates d'expiration des documents sont obligatoires — un camion
    /// sans date de visite technique est refusé, et rien n'est écrit.</summary>
    [Fact]
    public async Task Un_document_sans_date_d_expiration_est_refuse_sans_rien_creer()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);
        var requete = new SoumettreEnrolementSocieteRequete(societeId,
        [
            new ObjetEnrolementSoumission(TypeObjetEnrolement.Camion, "AA-123-BC", "Semi-remorque.",
                [new(DocumentEnrolement.AssuranceVehicule, new DateOnly(2027, 6, 30))]),
        ]);

        var erreur = await Assert.ThrowsAsync<ArgumentException>(() => _command.ExecuterAsync(userId, requete, CancellationToken.None));

        Assert.Contains("VisiteTechnique", erreur.Message);
        _ticketRepo.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Societe_habilitee_cree_un_dossier_unique_et_un_ticket_par_objet()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);

        var resultat = await _command.ExecuterAsync(userId, RequeteFlotte(societeId), CancellationToken.None);

        Assert.Equal("DOS-ENR-100", resultat.NumeroDossier);
        Assert.Equal(3, resultat.Tickets.Count);

        // Un seul dossier, rattaché à la société (ClientId = société, AuteurId = représentant).
        _dossierRepo.Verify(r => r.AjouterAsync(
            It.Is<Dossier>(d => d.ClientId == societeId && d.AuteurId == userId),
            It.IsAny<CancellationToken>()), Times.Once);

        // Trois tickets, tous dans le même dossier, routés vers l'Agent Gate.
        _ticketRepo.Verify(r => r.AjouterAsync(
            It.Is<Ticket>(t =>
                t.Module == "Enrolement" &&
                t.DossierId == resultat.DossierId &&
                t.FileCouranteId == _fileAgentGate.Id),
            It.IsAny<CancellationToken>()), Times.Exactly(3));

        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Chaque_ticket_porte_le_type_objet_le_sens_ajout_et_les_informations()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);

        await _command.ExecuterAsync(userId, RequeteFlotte(societeId), CancellationToken.None);

        // Deux chauffeurs typés Chauffeur/Ajout, avec objet + description repris.
        _ticketRepo.Verify(r => r.AjouterAsync(
            It.Is<Ticket>(t =>
                t.ObjetEnrolement == TypeObjetEnrolement.Chauffeur &&
                t.SensEnrolement == SensEnrolement.Ajout &&
                t.Objet == "KONE Ali" &&
                t.Description == "Permis B, dépôt Abidjan."),
            It.IsAny<CancellationToken>()), Times.Once);

        // Le camion est typé Camion/Ajout.
        _ticketRepo.Verify(r => r.AjouterAsync(
            It.Is<Ticket>(t =>
                t.ObjetEnrolement == TypeObjetEnrolement.Camion &&
                t.SensEnrolement == SensEnrolement.Ajout &&
                t.Objet == "AA-123-BC"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Chaque_ticket_est_audite_a_la_creation()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);

        await _command.ExecuterAsync(userId, RequeteFlotte(societeId), CancellationToken.None);

        _journalisation.Verify(j => j.ConsignerHistoriqueAsync(
            It.IsAny<Guid>(), "TicketCree", It.IsAny<string?>(), userId, It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Sans_rattachement_actif_leve_AccesRefuse_et_n_ecrit_rien()
    {
        var userId = Guid.NewGuid();
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UtilisateurSociete?)null);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(userId, RequeteFlotte(Guid.NewGuid()), CancellationToken.None));

        _ticketRepo.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        _dossierRepo.Verify(r => r.AjouterAsync(It.IsAny<Dossier>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Rattachement_inactif_leve_AccesRefuse()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId, actif: false);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(userId, RequeteFlotte(societeId), CancellationToken.None));
    }

    [Fact]
    public async Task Sans_droit_de_soumission_leve_AccesRefuse()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId, droit: false);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(userId, RequeteFlotte(societeId), CancellationToken.None));
    }

    [Fact]
    public async Task Soumission_pour_une_autre_societe_est_refusee_usurpation()
    {
        var userId = Guid.NewGuid();
        var vraieSociete = Guid.NewGuid();
        var autreSociete = Guid.NewGuid();
        RattacherAvecDroit(userId, vraieSociete);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(userId, RequeteFlotte(autreSociete), CancellationToken.None));
    }

    [Fact]
    public async Task Aucun_objet_leve_ArgumentException()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);

        var requete = new SoumettreEnrolementSocieteRequete(societeId, Array.Empty<ObjetEnrolementSoumission>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(userId, requete, CancellationToken.None));
    }

    [Theory]
    [InlineData("", "Description valide.")]
    [InlineData("   ", "Description valide.")]
    [InlineData("Objet valide", "")]
    [InlineData("Objet valide", "   ")]
    public async Task Objet_ou_description_vide_leve_ArgumentException(string objet, string description)
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);

        var requete = new SoumettreEnrolementSocieteRequete(
            societeId,
            [new ObjetEnrolementSoumission(TypeObjetEnrolement.Chauffeur, objet, description)]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _command.ExecuterAsync(userId, requete, CancellationToken.None));

        _ticketRepo.Verify(r => r.AjouterAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task File_agent_gate_non_parametree_leve_InvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        RattacherAvecDroit(userId, societeId);
        _fileRepo.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _command.ExecuterAsync(userId, RequeteFlotte(societeId), CancellationToken.None));
    }
}
