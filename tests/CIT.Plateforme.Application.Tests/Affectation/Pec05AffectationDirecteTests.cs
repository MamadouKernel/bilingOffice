using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Affectation;

/// <summary>
/// PEC-05 — affectation directe : un superviseur affecte un ticket NON pris en charge à un
/// opérateur précis ; l'opérateur doit être habilité sur la file, l'action est tracée.
/// </summary>
public sealed class Pec05AffectationDirecteTests
{
    private static readonly DateTime Maintenant = new(2026, 10, 4, 9, 0, 0, DateTimeKind.Utc);
    private readonly Guid _superviseur = Guid.NewGuid();
    private readonly Guid _operateur = Guid.NewGuid();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly Mock<IJournalisationService> _journal = new();
    private readonly Ticket _ticket = Ticket.Creer("BIL-IMP-PEC05", "BillingImport", "Import", Guid.NewGuid(), Guid.NewGuid(), Maintenant.AddHours(-1));

    private AffecterManuellementCommand Commande()
    {
        _tickets.Setup(t => t.ObtenirParIdAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_ticket);
        var horloge = new Mock<IHorloge>();
        horloge.Setup(h => h.MaintenantUtc).Returns(Maintenant);
        return new AffecterManuellementCommand(_tickets.Object, _autorisation.Object, _journal.Object, horloge.Object, new Mock<IUniteDeTravail>().Object);
    }

    [Fact]
    public async Task PEC05_le_superviseur_affecte_un_ticket_non_pris_en_charge_a_un_operateur_precis()
    {
        _autorisation.Setup(a => a.EstAutoriseAsync(_operateur, Permission.TicketPrendreEnCharge, _ticket.FileCouranteId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, _ticket.Statut);

        await Commande().ExecuterAsync(_superviseur, _ticket.Id, _operateur);

        Assert.Equal(_operateur, _ticket.OperateurAssigneId);
        Assert.Equal(StatutTicket.EnCours, _ticket.Statut);
        Assert.Equal(Maintenant, _ticket.DatePriseEnChargeUtc);
        _autorisation.Verify(a => a.ExigerAsync(_superviseur, Permission.TicketAffecterManuellement, _ticket.FileCouranteId, It.IsAny<CancellationToken>()), Times.Once);
        _journal.Verify(j => j.ConsignerHistoriqueAsync(_ticket.Id, "TicketAffecteManuellement", It.IsAny<string?>(), _superviseur, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PEC05_un_operateur_non_habilite_sur_la_file_est_refuse()
    {
        _autorisation.Setup(a => a.EstAutoriseAsync(_operateur, Permission.TicketPrendreEnCharge, _ticket.FileCouranteId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<AccesRefuseException>(() => Commande().ExecuterAsync(_superviseur, _ticket.Id, _operateur));

        Assert.Null(_ticket.OperateurAssigneId);
        Assert.Equal(StatutTicket.EnAttenteDePriseEnCharge, _ticket.Statut);
    }
}
