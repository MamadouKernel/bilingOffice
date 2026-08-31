using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

public class EvaluerReclamationCommandTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<IUtilisateurSocieteRepository> _utilisateurSocieteRepo = new();
    private readonly Mock<IJournalisationService> _journalisation = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uniteDeTravail = new();
    // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.4) : Complaint.Repondre était
    // documentée (spec.md §6, use-cases.md C7-03) mais jamais vérifiée par la Command.
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly EvaluerReclamationCommand _command;

    public EvaluerReclamationCommandTests()
    {
        _command = new EvaluerReclamationCommand(
            _ticketRepo.Object,
            _dossierRepo.Object,
            _utilisateurSocieteRepo.Object,
            _journalisation.Object,
            _horloge.Object,
            _uniteDeTravail.Object,
            _autorisation.Object);

        _horloge.Setup(h => h.MaintenantUtc).Returns(DateTime.UtcNow);
        // Par défaut, l'appelant possède la permission — les tests négatifs de permission
        // (REC-REM-06/08) la retirent explicitement.
        _autorisation.Setup(a => a.ExigerAsync(It.IsAny<Guid>(), Permission.ComplaintRepondre, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Client_peut_evaluer_sa_reclamation_cloturee()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var dossier = Dossier.Creer("DOS-1", societeId, DateTime.UtcNow, clientId);
        
        typeof(Ticket).GetProperty("DossierId")!.SetValue(ticket, dossier.Id);
        
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Réponse", MotifClotureType.Normale, DateTime.UtcNow);
        
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
            
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dossier);
            
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true));

        // Act
        await _command.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticket.Id, 5, "Très bien"), CancellationToken.None);

        // Assert
        Assert.Equal(5, ticket.EvaluationNote);
        Assert.Equal("Très bien", ticket.EvaluationCommentaire);
        
        _uniteDeTravail.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
        _journalisation.Verify(j => j.ConsignerAuditAsync(clientId, "ReclamationEvaluee", nameof(Ticket), ticket.Id, null, "5", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Evaluation_refusee_si_non_cloturee()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var dossier = Dossier.Creer("DOS-1", societeId, DateTime.UtcNow, clientId);
        typeof(Ticket).GetProperty("DossierId")!.SetValue(ticket, dossier.Id);
        
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
            
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dossier);
            
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _command.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticket.Id, 5, null), CancellationToken.None));
    }

    [Fact]
    public async Task Evaluation_refusee_si_mauvaise_societe()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var societeClient = Guid.NewGuid();
        var societeDossier = Guid.NewGuid(); // Différente!
        
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var dossier = Dossier.Creer("DOS-1", societeDossier, DateTime.UtcNow, Guid.NewGuid());
        typeof(Ticket).GetProperty("DossierId")!.SetValue(ticket, dossier.Id);
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Réponse", MotifClotureType.Normale, DateTime.UtcNow);
        
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
            
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dossier);
            
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(clientId, societeClient, DateTime.UtcNow, false, true));

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() => 
            _command.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticket.Id, 5, null), CancellationToken.None));
    }

    [Fact]
    public async Task Evaluation_refusee_si_deja_evaluee()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var dossier = Dossier.Creer("DOS-1", societeId, DateTime.UtcNow, clientId);
        typeof(Ticket).GetProperty("DossierId")!.SetValue(ticket, dossier.Id);
        
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Réponse", MotifClotureType.Normale, DateTime.UtcNow);
        ticket.Evaluer(4, null, DateTime.UtcNow); // Déjà évaluée !
        
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
            
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dossier);
            
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _command.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticket.Id, 5, null), CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Evaluation_refusee_si_note_hors_limites(int note)
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var dossier = Dossier.Creer("DOS-1", societeId, DateTime.UtcNow, clientId);
        typeof(Ticket).GetProperty("DossierId")!.SetValue(ticket, dossier.Id);
        
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Réponse", MotifClotureType.Normale, DateTime.UtcNow);
        
        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ticket);
            
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dossier);
            
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _command.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticket.Id, note, null), CancellationToken.None));
    }

    // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.4) — Écarts 3 (Complaint.Repondre) et
    // 4 (contrôle d'habilitation manquant sur EvaluerReclamationCommand). REC-REM-05/07 : acteur
    // habilité Complaint.Repondre ET dans la bonne société -> évaluation acceptée.
    [Fact]
    public async Task REC_REM_05_07_UtilisateurAvecComplaintRepondre_PeutEvaluer()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var dossier = Dossier.Creer("DOS-1", societeId, DateTime.UtcNow, clientId);
        typeof(Ticket).GetProperty("DossierId")!.SetValue(ticket, dossier.Id);

        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Réponse", MotifClotureType.Normale, DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dossier);
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true));
        // Permission accordée (setup par défaut du constructeur) : ExigerAsync ne lève pas.

        // Act
        await _command.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticket.Id, 4, "Bien"), CancellationToken.None);

        // Assert
        Assert.Equal(4, ticket.EvaluationNote);
        _autorisation.Verify(a => a.ExigerAsync(clientId, Permission.ComplaintRepondre, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    // REC-REM-06/08 : acteur SANS Complaint.Repondre, mais bien dans la bonne société -> refus lié
    // spécifiquement à l'autorisation (isolé du cloisonnement, qui est correct ici).
    [Fact]
    public async Task REC_REM_06_08_UtilisateurSansComplaintRepondre_EstRefuse()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var dossier = Dossier.Creer("DOS-1", societeId, DateTime.UtcNow, clientId);
        typeof(Ticket).GetProperty("DossierId")!.SetValue(ticket, dossier.Id);

        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Réponse", MotifClotureType.Normale, DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dossier);
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(clientId, societeId, DateTime.UtcNow, false, true));
        _autorisation.Setup(a => a.ExigerAsync(clientId, Permission.ComplaintRepondre, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException($"L'utilisateur {clientId} ne dispose pas de la permission 'complaint.repondre'."));

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticket.Id, 4, "Bien"), CancellationToken.None));
        Assert.Null(ticket.EvaluationNote);
    }

    // REC-REM-09 : acteur habilité Complaint.Repondre, mais d'une AUTRE société -> refus lié
    // spécifiquement au cloisonnement (isolé de l'autorisation, qui est correcte ici).
    [Fact]
    public async Task REC_REM_09_UtilisateurAutreSociete_EstRefuseParCloisonnement()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var societeClient = Guid.NewGuid();
        var societeDossier = Guid.NewGuid(); // Différente !
        var ticket = Ticket.Creer("REC-01", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var dossier = Dossier.Creer("DOS-1", societeDossier, DateTime.UtcNow, Guid.NewGuid());
        typeof(Ticket).GetProperty("DossierId")!.SetValue(ticket, dossier.Id);
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Réponse", MotifClotureType.Normale, DateTime.UtcNow);

        _ticketRepo.Setup(r => r.ObtenirParIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _dossierRepo.Setup(r => r.ObtenirParIdAsync(dossier.Id, It.IsAny<CancellationToken>())).ReturnsAsync(dossier);
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(clientId, societeClient, DateTime.UtcNow, false, true));
        // Permission accordée (setup par défaut) : le refus doit venir du cloisonnement, pas de l'autorisation.

        // Act & Assert
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            _command.ExecuterAsync(new EvaluerReclamationRequete(clientId, ticket.Id, 4, "Bien"), CancellationToken.None));
        Assert.Null(ticket.EvaluationNote);
    }
}
