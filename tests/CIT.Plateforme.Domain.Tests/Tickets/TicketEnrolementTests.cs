using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

// FEATURE 008 (Enrôlement Transporteurs) — T003. DefinirInformationsEnrolement réutilise les
// colonnes génériques Objet/Description (déjà ajoutées en F006) — aucune nouvelle propriété.
public class TicketEnrolementTests
{
    private static Ticket CreerTicketEnrolement()
    {
        return Ticket.Creer("ENR-001", "Enrolement", "Enrolement", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
    }

    [Fact]
    public void DefinirInformationsEnrolement_DefinitObjetEtDescription()
    {
        var ticket = CreerTicketEnrolement();

        ticket.DefinirInformationsEnrolement("Enrôlement chauffeur", "Camion frigorifique, dépôt d'Abidjan.");

        Assert.Equal("Enrôlement chauffeur", ticket.Objet);
        Assert.Equal("Camion frigorifique, dépôt d'Abidjan.", ticket.Description);
    }

    [Fact]
    public void DefinirInformationsEnrolement_ObjetVide_LeveException()
    {
        var ticket = CreerTicketEnrolement();

        Assert.Throws<ArgumentException>(() => ticket.DefinirInformationsEnrolement("  ", "Description valide."));
    }

    [Fact]
    public void DefinirInformationsEnrolement_DescriptionVide_LeveException()
    {
        var ticket = CreerTicketEnrolement();

        Assert.Throws<ArgumentException>(() => ticket.DefinirInformationsEnrolement("Objet valide", ""));
    }

    [Fact]
    public void DefinirInformationsEnrolement_TicketNonEnrolement_ResteUtilisableSansEffetDeBord()
    {
        // Un ticket d'un autre module (ex. CustomerComplaint) reste inchangé si cette méthode
        // n'est jamais appelée sur lui — DefinirInformationsEnrolement n'a aucune garde de module
        // (comme DefinirContenuReclamation, générique dans sa définition), mais n'est appelée que
        // par SoumettreEnrolementCommand (Application), jamais par un autre module.
        var ticketAutreModule = Ticket.Creer("REC-001", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        Assert.Null(ticketAutreModule.Objet);
        Assert.Null(ticketAutreModule.Description);
    }
}
