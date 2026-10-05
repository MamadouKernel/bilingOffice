using System;
using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

public sealed class ModifierDateEnlevementTests
{
    private static Ticket Nouveau() =>
        Ticket.Creer("TKT-1", "BillingImport", "Import", Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

    private static readonly DateOnly Futur = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);

    [Fact]
    public void SansProforma_ChangeLaDate_SansRefaireLaProforma()
    {
        var ticket = Nouveau();

        var proformaARefaire = ticket.ModifierDateEnlevement(Futur, DateTime.UtcNow);

        Assert.False(proformaARefaire);
        Assert.Equal(Futur, ticket.DateEnlevementSouhaitee);
    }

    [Fact]
    public void ProformaEnAttente_RepasseEnCorrectionDemandee()
    {
        var ticket = Nouveau();
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.EmettreProforma(DateTime.UtcNow);

        var proformaARefaire = ticket.ModifierDateEnlevement(Futur, DateTime.UtcNow);

        Assert.True(proformaARefaire);
        Assert.Equal(EtatProforma.CorrectionDemandee, ticket.EtatProforma);
    }

    [Fact]
    public void ProformaValidee_RepasseEnCorrectionDemandee()
    {
        var ticket = Nouveau();
        ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);
        ticket.EmettreProforma(DateTime.UtcNow);
        ticket.ValiderProforma(DateTime.UtcNow);

        var proformaARefaire = ticket.ModifierDateEnlevement(Futur, DateTime.UtcNow);

        Assert.True(proformaARefaire);
        Assert.Equal(EtatProforma.CorrectionDemandee, ticket.EtatProforma);
    }
}
