using CIT.Plateforme.Domain.Audit;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Audit;

// FEATURE 009 — T023. Extension additive de EvenementSocle (F001) : une seule propriété
// nullable DistribueLeUtc, curseur de distribution (research.md D2). Aucune propriété
// existante n'est modifiée.
public class EvenementSocleDistributionTests
{
    private static readonly DateTime Maintenant = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Creer_LaisseLeCurseurNull()
    {
        var evenement = EvenementSocle.Creer("TicketCree", "{}", Maintenant);

        Assert.Null(evenement.DistribueLeUtc);
    }

    [Fact]
    public void MarquerDistribue_RenseigneLeCurseur()
    {
        var evenement = EvenementSocle.Creer("TicketCree", "{}", Maintenant);
        var quandDistribue = Maintenant.AddMinutes(2);

        evenement.MarquerDistribue(quandDistribue);

        Assert.Equal(quandDistribue, evenement.DistribueLeUtc);
    }

    [Fact]
    public void MarquerDistribue_DeuxiemeAppel_NEcrasePasLaPremiereValeur()
    {
        // Idempotence du curseur : un second passage ne redate jamais la distribution.
        var evenement = EvenementSocle.Creer("TicketCree", "{}", Maintenant);
        var premierMarquage = Maintenant.AddMinutes(2);
        evenement.MarquerDistribue(premierMarquage);

        evenement.MarquerDistribue(Maintenant.AddHours(5));

        Assert.Equal(premierMarquage, evenement.DistribueLeUtc);
    }

    [Fact]
    public void EvenementSocle_ConserveSesProprietesDOrigine()
    {
        // Garde-fou de non-régression F001 : l'extension est purement additive.
        var evenement = EvenementSocle.Creer("TicketCree", "{\"ticketId\":\"x\"}", Maintenant);

        Assert.Equal("TicketCree", evenement.TypeEvenement);
        Assert.Equal("{\"ticketId\":\"x\"}", evenement.ChargeUtile);
        Assert.Equal(Maintenant, evenement.HorodatageUtc);
        Assert.NotEqual(Guid.Empty, evenement.Id);
    }
}
