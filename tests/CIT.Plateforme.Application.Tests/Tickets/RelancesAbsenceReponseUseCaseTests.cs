using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tickets;

/// <summary>
/// CYC-04 — le service passe toutes les 5 minutes : une relance n'est émise qu'une fois le délai
/// entre relances écoulé depuis la dernière activité, et la suggestion de clôture ne se répète pas
/// à chaque passage (constat de recette du 04/10/2026 : 3 relances en 15 min, puis une suggestion
/// toutes les 5 min).
/// </summary>
public sealed class RelancesAbsenceReponseUseCaseTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly ParametresRelance Parametres = new(NombreRelancesMax: 3, DelaiEntreRelances: TimeSpan.FromDays(2));

    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IJournalisationService> _journal = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Ticket _ticket;

    public RelancesAbsenceReponseUseCaseTests()
    {
        _ticket = Ticket.Creer("BIL-IMP-1", "BillingImport", "Import", Guid.NewGuid(), Guid.NewGuid(), T0);
        _ticket.PrendreEnCharge(Guid.NewGuid(), T0);
        _ticket.MettreEnAttenteClient(T0);
        _tickets.Setup(t => t.ObtenirParIdAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_ticket);
    }

    private Task<bool> TraiterA(DateTime maintenant)
    {
        _horloge.Setup(h => h.MaintenantUtc).Returns(maintenant);
        return new RelancesAbsenceReponseUseCase(_tickets.Object, _journal.Object, _horloge.Object, new Mock<IUniteDeTravail>().Object)
            .TraiterTicketAsync(_ticket.Id, Parametres);
    }

    private int Emis(string evenement) => _journal.Invocations
        .Count(i => i.Method.Name == nameof(IJournalisationService.ConsignerHistoriqueAsync) && (string)i.Arguments[1] == evenement);

    [Fact]
    public async Task Aucune_relance_avant_le_delai()
    {
        Assert.False(await TraiterA(T0.AddMinutes(5)));
        Assert.False(await TraiterA(T0.AddDays(1)));

        Assert.Equal(0, _ticket.NombreRelancesEnvoyees);
    }

    [Fact]
    public async Task Une_relance_par_delai_puis_une_seule_suggestion_par_delai()
    {
        // Passages toutes les 5 minutes pendant 10 jours.
        for (var t = T0; t <= T0.AddDays(10); t = t.AddMinutes(5))
        {
            await TraiterA(t);
        }

        // Relances à J+2, J+4, J+6 ; suggestions à J+8 et J+10 seulement.
        Assert.Equal(3, Emis("RelanceEmise"));
        Assert.Equal(2, Emis("SuggestionClotureAbsenceReponse"));
    }
}
