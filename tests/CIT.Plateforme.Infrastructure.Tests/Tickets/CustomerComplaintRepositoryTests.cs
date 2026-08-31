using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Tickets;

public class CustomerComplaintRepositoryTests
{
    [Fact]
    public async Task Peut_Sauvegarder_Et_Recharger_Une_CategorieReclamation()
    {
        // Arrange
        await using var context = BaseDeTestSql.CreerContexte();
        var categorie = CategorieReclamation.Creer("REC-TEST-01", "Catégorie Test", null);

        // Act
        context.Set<CategorieReclamation>().Add(categorie);
        await context.SaveChangesAsync();

        // Assert
        await using var verificationContext = BaseDeTestSql.CreerContexte();
        var categorieChargee = await verificationContext.Set<CategorieReclamation>().FindAsync(categorie.Id);
        
        Assert.NotNull(categorieChargee);
        Assert.Equal("REC-TEST-01", categorieChargee.Code);
        Assert.Equal("Catégorie Test", categorieChargee.Libelle);
        Assert.Equal(categorie.FileCibleDefautId, categorieChargee.FileCibleDefautId);
        Assert.True(categorieChargee.EstActif);
    }

    [Fact]
    public async Task Peut_Sauvegarder_Et_Recharger_Ticket_Avec_Extensions_Complaint()
    {
        // Arrange
        await using var context = BaseDeTestSql.CreerContexte();
        
        var categorie = CategorieReclamation.Creer("REC-TEST-02", "Catégorie Test 2", null);
        context.Set<CategorieReclamation>().Add(categorie);
        
        var ticket = Ticket.Creer("TCK-REC-001", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.DefinirCategorieReclamation(categorie.Id);
        
        // Simuler tout le cycle pour tester l'évaluation et la réponse formalisée
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Ceci est la réponse formelle.", MotifClotureType.Normale, DateTime.UtcNow);
        
        var dateEval = DateTime.UtcNow;
        ticket.Evaluer(5, "Parfait !", dateEval);

        // Act
        context.Set<Ticket>().Add(ticket);
        await context.SaveChangesAsync();

        // Assert
        await using var verificationContext = BaseDeTestSql.CreerContexte();
        var ticketCharge = await verificationContext.Set<Ticket>().FindAsync(ticket.Id);
        
        Assert.NotNull(ticketCharge);
        Assert.Equal("TCK-REC-001", ticketCharge.Numero);
        Assert.Equal(categorie.Id, ticketCharge.CategorieReclamationId);
        Assert.Equal("Ceci est la réponse formelle.", ticketCharge.ReponseFormalisee);
        Assert.Equal(5, ticketCharge.EvaluationNote);
        Assert.Equal("Parfait !", ticketCharge.EvaluationCommentaire);
        // Tolérance pour les DateTime en base
        Assert.Equal(dateEval, ticketCharge.EvaluationDateUtc!.Value, TimeSpan.FromMilliseconds(500));
    }
}
