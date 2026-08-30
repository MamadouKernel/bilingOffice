using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

public class TicketExportTests
{
    [Fact]
    public void Creer_AvecNumeroBooking_InitialiseCorrectementLeTicket()
    {
        // Arrange
        var date = DateTime.UtcNow;
        var dossierId = Guid.NewGuid();
        var fileId = Guid.NewGuid();

        // Act
        var ticket = Ticket.Creer(
            numero: "TCK-EXP-2026-00001",
            module: "BillingExport",
            typeDemande: "EXPORT",
            fileCibleId: fileId,
            dossierId: dossierId,
            maintenantUtc: date,
            numeroBl: null,
            numeroBooking: "BKG-2026-001");

        // Assert
        Assert.Equal("TCK-EXP-2026-00001", ticket.Numero);
        Assert.Equal("BillingExport", ticket.Module);
        Assert.Equal("EXPORT", ticket.TypeDemande);
        Assert.Equal("BKG-2026-001", ticket.NumeroBooking);
        Assert.Null(ticket.NumeroBl);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, ticket.Statut);
        Assert.Null(ticket.TypeAnnulationExport);
    }

    [Fact]
    public void MarquerRejetExport_PasseEnStatutAnnuleAvecTypeAnnulationRejetBackOffice()
    {
        // Arrange
        var date = DateTime.UtcNow;
        var ticket = Ticket.Creer(
            numero: "TCK-EXP-2026-00002",
            module: "BillingExport",
            typeDemande: "EXPORT",
            fileCibleId: Guid.NewGuid(),
            dossierId: Guid.NewGuid(),
            maintenantUtc: date,
            numeroBooking: "BKG-2026-002");

        // Act
        ticket.MarquerRejetExport(TypeAnnulationExport.RejetBackOffice, date.AddMinutes(5));

        // Assert
        Assert.Equal(StatutTicket.Annule, ticket.Statut);
        Assert.Equal(TypeAnnulationExport.RejetBackOffice, ticket.TypeAnnulationExport);
        Assert.Equal(date.AddMinutes(5), ticket.DateClotureUtc);
    }
}
