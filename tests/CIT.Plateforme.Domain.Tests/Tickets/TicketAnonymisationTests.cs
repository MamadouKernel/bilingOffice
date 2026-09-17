using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

/// <summary>SEC-06 (« droits des personnes », droit à l'effacement) — MarquerAnonymise doit
/// réellement effacer/masquer le contenu personnel, pas seulement poser un drapeau.</summary>
public class TicketAnonymisationTests
{
    private static Ticket CreerTicketReclamation()
    {
        var ticket = Ticket.Creer("REC-ANO-001", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        ticket.DefinirContenuReclamation("Objet personnel du client", "Description personnelle détaillée");
        return ticket;
    }

    [Fact]
    public void MarquerAnonymise_RemplacePapierPersonnel_ParValeurAnonymisee()
    {
        var ticket = CreerTicketReclamation();

        ticket.MarquerAnonymise(DateTime.UtcNow);

        Assert.True(ticket.EstAnonymise);
        Assert.Equal(Ticket.ValeurAnonymisee, ticket.Objet);
        Assert.Equal(Ticket.ValeurAnonymisee, ticket.Description);
    }

    [Fact]
    public void MarquerAnonymise_ChampsNonRenseignes_RestentNuls()
    {
        var ticket = Ticket.Creer("REC-ANO-002", "CustomerComplaint", "Reclamation", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        ticket.MarquerAnonymise(DateTime.UtcNow);

        Assert.True(ticket.EstAnonymise);
        Assert.Null(ticket.Objet);
        Assert.Null(ticket.Description);
    }

    [Fact]
    public void MarquerAnonymise_EstIdempotent_NeReecritPasLaDateDarchivage()
    {
        var ticket = CreerTicketReclamation();
        var premiereDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        ticket.MarquerAnonymise(premiereDate);
        ticket.MarquerAnonymise(new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(premiereDate, ticket.DateArchivageUtc);
        Assert.Equal(Ticket.ValeurAnonymisee, ticket.Objet);
    }

    [Fact]
    public void MarquerAnonymise_NeChangePasLeNumeroNiLeModule()
    {
        var ticket = CreerTicketReclamation();

        ticket.MarquerAnonymise(DateTime.UtcNow);

        Assert.Equal("REC-ANO-001", ticket.Numero);
        Assert.Equal("CustomerComplaint", ticket.Module);
    }
}
