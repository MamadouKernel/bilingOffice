using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

/// <summary>Validation interne « 4-yeux » — approbation / rejet / séparation des devoirs stricte.</summary>
public sealed class ValidationInterneEnrolementCommandTests
{
    private static readonly DateTime Maintenant = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Societe = Guid.NewGuid();
    private static readonly Guid Preparateur = Guid.NewGuid();
    private static readonly Guid Validateur = Guid.NewGuid();

    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IDossierRepository> _dossiers = new();
    private readonly Mock<IUtilisateurSocieteRepository> _rattachements = new();
    private readonly Mock<IJournalisationService> _journal = new();
    private readonly Mock<IHorloge> _horloge = new();
    private readonly Mock<IUniteDeTravail> _uow = new();
    private readonly ValidationInterneEnrolementCommand _command;
    private readonly Guid _dossierId;
    private readonly Ticket _ticket;

    public ValidationInterneEnrolementCommandTests()
    {
        _horloge.Setup(h => h.MaintenantUtc).Returns(Maintenant);
        _command = new ValidationInterneEnrolementCommand(_tickets.Object, _dossiers.Object, _rattachements.Object, _journal.Object, _horloge.Object, _uow.Object);

        var dossier = Dossier.Creer("DOS-VAL-0001", Societe, Maintenant, Preparateur);
        _dossierId = dossier.Id;

        _ticket = Ticket.Creer("ENR-VAL-0001", "Enrolement", "Enrolement", Guid.NewGuid(), _dossierId, Maintenant);
        _ticket.MarquerAValiderEnInterne(Preparateur);

        _dossiers.Setup(d => d.ObtenirParIdAsync(_dossierId, It.IsAny<CancellationToken>())).ReturnsAsync(dossier);
        _tickets.Setup(t => t.ListerParDossierAsync(_dossierId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Ticket> { _ticket });
        _rattachements.Setup(r => r.ObtenirParUtilisateurAsync(Validateur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(Validateur, Societe, Maintenant, droitValidation: true));
    }

    [Fact]
    public async Task Approuver_libere_le_ticket_et_alerte_l_Agent_Gate()
    {
        await _command.ApprouverAsync(Validateur, _dossierId);

        Assert.Null(_ticket.EnAttenteValidationInterneParId); // libéré -> visible Agent Gate
        _journal.Verify(j => j.PublierEvenementAsync("TicketArriveeEnFile", It.Is<string>(s => s.Contains(_ticket.Id.ToString())), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.EnregistrerAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rejeter_annule_le_ticket_sans_alerter_l_Agent_Gate()
    {
        await _command.RejeterAsync(Validateur, _dossierId, "Pièces non conformes");

        Assert.Equal(StatutTicket.Annule, _ticket.Statut);
        _journal.Verify(j => j.PublierEvenementAsync("TicketArriveeEnFile", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Le_preparateur_ne_peut_pas_valider_sa_propre_soumission()
    {
        // Le validateur EST le préparateur -> SoD strict : refus.
        _rattachements.Setup(r => r.ObtenirParUtilisateurAsync(Preparateur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UtilisateurSociete.Rattacher(Preparateur, Societe, Maintenant, droitValidation: true));

        await Assert.ThrowsAsync<AccesRefuseException>(() => _command.ApprouverAsync(Preparateur, _dossierId));
    }
}
