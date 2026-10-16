using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications.Resolveurs;
using CIT.Plateforme.Domain.Securite;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.ActualisationBadt;

// FEATURE 010 — l'escalade (BadtLigneEscaladee) cible les détenteurs de ActualisationBadtDecider.
public class ResolveurDecideursBadtTests
{
    private readonly Mock<IHabilitationRepository> _habilitations = new();
    private readonly ResolveurDecideursBadt _resolveur;

    public ResolveurDecideursBadtTests()
    {
        _resolveur = new ResolveurDecideursBadt(_habilitations.Object);
    }

    [Fact]
    public void Ne_traite_que_l_evenement_escalade()
    {
        Assert.True(_resolveur.PeutResoudre("BadtLigneEscaladee"));
        Assert.False(_resolveur.PeutResoudre("TicketCree"));
    }

    [Fact]
    public async Task Resout_les_decideurs_habilites()
    {
        var decideur = Guid.NewGuid();
        _habilitations
            .Setup(h => h.ListerUtilisateursAvecPermissionAsync(Permission.ActualisationBadtDecider, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { decideur });

        var destinataires = await _resolveur.ResoudreAsync("BadtLigneEscaladee", "{\"ticketId\":\"" + Guid.NewGuid() + "\"}", CancellationToken.None);

        Assert.Contains(decideur, destinataires);
    }

    [Fact]
    public async Task Autre_evenement_ne_resout_personne()
    {
        var destinataires = await _resolveur.ResoudreAsync("TicketCree", "{}", CancellationToken.None);
        Assert.Empty(destinataires);
    }
}
