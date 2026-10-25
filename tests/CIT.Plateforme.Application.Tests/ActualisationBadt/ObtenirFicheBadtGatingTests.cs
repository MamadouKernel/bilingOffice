using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.ActualisationBadt;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Audit;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

/// <summary>
/// FEATURE 010 — prise en charge explicite : un agent ne qualifie qu'un ticket BADT qu'il s'est
/// affecté (EnCours + assigné à lui), comme sur la fiche ticket générique. Tant que personne ne l'a
/// pris, seul le bouton « Prendre en charge » est offert.
/// </summary>
public class ObtenirFicheBadtGatingTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IDossierRepository> _dossiers = new();
    private readonly Mock<ISocieteClienteRepository> _societes = new();
    private readonly Mock<ILigneActualisationBadtRepository> _lignes = new();
    private readonly Mock<IMessageRepository> _messages = new();
    private readonly Mock<IUtilisateurInfoRepository> _utilisateursInfo = new();
    private readonly Mock<IHistoriqueTicketRepository> _historique = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly ObtenirFicheBadtQuery _query;

    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Ticket _ticket = Ticket.CreerDemandeActualisationBadt(
        "TCK-BADT-001", Guid.NewGuid(), Guid.NewGuid(), "BADT expiré.", DateTime.UtcNow);

    public ObtenirFicheBadtGatingTests()
    {
        _query = new ObtenirFicheBadtQuery(_tickets.Object, _dossiers.Object, _societes.Object,
            _lignes.Object, _messages.Object, _utilisateursInfo.Object, _historique.Object, _autorisation.Object);

        _tickets.Setup(r => r.ObtenirParIdAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_ticket);
        // Habilitation d'instruction accordée quel que soit le périmètre (accès + qualification).
        _autorisation.Setup(a => a.EstAutoriseAsync(_agentId, Permission.ActualisationBadtInstruire, It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _lignes.Setup(r => r.ListerParTicketAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<LigneActualisationBadt>());
        _messages.Setup(r => r.ListerParTicketAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Message>());
        _utilisateursInfo.Setup(r => r.ObtenirInfosAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UtilisateurInfo>());
        _historique.Setup(r => r.ListerParTicketAsync(_ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<EntreeHistorique>());
    }

    [Fact]
    public async Task Ticket_non_pris_en_charge_offre_la_prise_en_charge_pas_la_qualification()
    {
        var fiche = await _query.ExecuterAsync(_agentId, _ticket.Id, CancellationToken.None);

        Assert.True(fiche.PeutPrendreEnCharge);
        Assert.False(fiche.PeutQualifier);
        Assert.Null(fiche.OperateurAssigneId);
    }

    [Fact]
    public async Task Ticket_pris_par_moi_ouvre_la_qualification()
    {
        _ticket.PrendreEnCharge(_agentId, DateTime.UtcNow);

        var fiche = await _query.ExecuterAsync(_agentId, _ticket.Id, CancellationToken.None);

        Assert.True(fiche.PeutQualifier);
        Assert.False(fiche.PeutPrendreEnCharge);
        Assert.Equal(_agentId, fiche.OperateurAssigneId);
    }

    [Fact]
    public async Task Ticket_pris_par_un_autre_agent_ne_laisse_ni_qualifier_ni_reprendre()
    {
        _ticket.PrendreEnCharge(Guid.NewGuid(), DateTime.UtcNow);

        var fiche = await _query.ExecuterAsync(_agentId, _ticket.Id, CancellationToken.None);

        Assert.False(fiche.PeutQualifier);
        Assert.False(fiche.PeutPrendreEnCharge);
    }
}
