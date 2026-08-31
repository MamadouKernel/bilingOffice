using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

public class CustomerComplaintTicketTests
{
    private Ticket CreerTicketReclamation()
    {
        return Ticket.Creer("REC-001", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
    }

    [Fact]
    public void CloturerAvecReponseFormalisee_SansReponse_LeveException()
    {
        // Arrange
        var ticket = CreerTicketReclamation();
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ticket.CloturerAvecReponseFormalisee(null!, MotifClotureType.Normale, DateTime.UtcNow));
        Assert.Contains("réponse formalisée", exception.Message);
    }

    [Fact]
    public void CloturerAvecReponseFormalisee_AvecReponse_ClotureLeTicket()
    {
        // Arrange
        var ticket = CreerTicketReclamation();
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        var maintenant = DateTime.UtcNow;

        // Act
        ticket.CloturerAvecReponseFormalisee("Nous avons corrigé l'erreur.", MotifClotureType.Normale, maintenant);

        // Assert
        Assert.Equal(StatutTicket.Cloture, ticket.Statut);
        Assert.Equal("Nous avons corrigé l'erreur.", ticket.ReponseFormalisee);
        Assert.Equal(maintenant, ticket.DateClotureUtc);
    }

    [Fact]
    public void Evaluer_TicketNonCloture_LeveException()
    {
        // Arrange
        var ticket = CreerTicketReclamation();

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ticket.Evaluer(5, "Parfait", DateTime.UtcNow));
        Assert.Contains("clôturé", exception.Message);
    }

    [Fact]
    public void Evaluer_TicketCloture_EnregistreEvaluation()
    {
        // Arrange
        var ticket = CreerTicketReclamation();
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Reponse", MotifClotureType.Normale, DateTime.UtcNow);
        var dateEval = DateTime.UtcNow;

        // Act
        ticket.Evaluer(4, "Très bien", dateEval);

        // Assert
        Assert.Equal(4, ticket.EvaluationNote);
        Assert.Equal("Très bien", ticket.EvaluationCommentaire);
        Assert.Equal(dateEval, ticket.EvaluationDateUtc);
    }

    [Fact]
    public void Evaluer_TicketDejaEvalue_LeveException()
    {
        // Arrange
        var ticket = CreerTicketReclamation();
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.CloturerAvecReponseFormalisee("Reponse", MotifClotureType.Normale, DateTime.UtcNow);
        ticket.Evaluer(4, "Très bien", DateTime.UtcNow);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ticket.Evaluer(5, "Nouveau", DateTime.UtcNow));
        Assert.Contains("déjà été évalué", exception.Message);
    }

    [Fact]
    public void DefinirCategorieReclamation_DefinitLId()
    {
        // Arrange
        var ticket = CreerTicketReclamation();
        var categorieId = Guid.NewGuid();

        // Act
        ticket.DefinirCategorieReclamation(categorieId);

        // Assert
        Assert.Equal(categorieId, ticket.CategorieReclamationId);
    }

    // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.3) : use-cases.md C7-01 promet
    // Objet et Description en entrée obligatoire — jamais capturés avant cette remédiation.
    [Fact]
    public void REC_REM_01_DefinirContenuReclamation_DefinitObjetEtDescription()
    {
        // Arrange
        var ticket = CreerTicketReclamation();

        // Act
        ticket.DefinirContenuReclamation("Facture erronée", "Le montant facturé ne correspond pas au devis signé.");

        // Assert
        Assert.Equal("Facture erronée", ticket.Objet);
        Assert.Equal("Le montant facturé ne correspond pas au devis signé.", ticket.Description);
    }

    [Fact]
    public void REC_REM_02_DefinirContenuReclamation_ObjetVide_LeveException()
    {
        // Arrange
        var ticket = CreerTicketReclamation();

        // Assert
        Assert.Throws<ArgumentException>(() => ticket.DefinirContenuReclamation("  ", "Description valide."));
    }

    [Fact]
    public void REC_REM_02_DefinirContenuReclamation_DescriptionVide_LeveException()
    {
        // Arrange
        var ticket = CreerTicketReclamation();

        // Assert
        Assert.Throws<ArgumentException>(() => ticket.DefinirContenuReclamation("Objet valide", ""));
    }
}
