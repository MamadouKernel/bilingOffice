using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Client;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.CustomerComplaint;

public class DashboardClientAcrTests
{
    private readonly Mock<IDossierRepository> _dossierRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<ISocieteClienteRepository> _societeRepo = new();
    private readonly Mock<IUtilisateurSocieteRepository> _utilisateurSocieteRepo = new();
    
    private readonly ObtenirDashboardClientQuery _query;

    public DashboardClientAcrTests()
    {
        _query = new ObtenirDashboardClientQuery(
            _dossierRepo.Object,
            _ticketRepo.Object,
            _societeRepo.Object,
            _utilisateurSocieteRepo.Object);
    }

    [Fact]
    public async Task Ticket_Complaint_en_attente_client_genere_une_action_requise()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        
        var societe = SocieteCliente.Soumettre(userId, "TEST", TypeClient.Normal, DateTime.UtcNow);
        typeof(SocieteCliente).GetProperty("Id")!.SetValue(societe, societeId);
        
        var userSoc = UtilisateurSociete.Rattacher(userId, societeId, DateTime.UtcNow, false, true);
        
        var dossier = Dossier.Creer("DOS-1", societeId, DateTime.UtcNow, userId);
        
        var ticket = Ticket.Creer("REC-1", "CustomerComplaint", "Reclamation", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.MettreEnAttenteClient(DateTime.UtcNow);
        
        _utilisateurSocieteRepo.Setup(r => r.ObtenirParUtilisateurAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userSoc);
            
        _societeRepo.Setup(r => r.ObtenirParIdAsync(societeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(societe);
            
        _dossierRepo.Setup(r => r.ListerParSocieteAsync(societeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { dossier }.ToList());
            
        _ticketRepo.Setup(r => r.ListerParDossiersAsync(It.IsAny<System.Collections.Generic.IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ticket }.ToList());

        // Act
        var result = await _query.ExecuterAsync(userId, societeId, CancellationToken.None);

        // Assert
        Assert.Single(result.ActionsRequises);
        var action = result.ActionsRequises.First();
        Assert.Equal(TypeActionRequise.ComplementReclamation, action.TypeAction);
        Assert.Contains("Complément demandé pour votre Réclamation REC-1", action.Titre);
        Assert.Contains("Le Service Client", action.Description);
    }
}
