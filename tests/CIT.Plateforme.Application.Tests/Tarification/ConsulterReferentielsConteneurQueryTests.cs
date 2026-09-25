using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification.Referentiels;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

/// <summary>
/// F3 (audit sécurité) — la consultation des référentiels conteneur passe par un cas d'usage
/// cloisonné : l'habilitation <see cref="Permission.TarificationAdministrer"/> est EXIGÉE avant toute
/// lecture, et les référentiels inactifs sont inclus (réactivation, historique).
/// </summary>
public sealed class ConsulterReferentielsConteneurQueryTests
{
    private readonly Mock<IReferentielsTarifairesRepository> _repo = new();
    private readonly Mock<IAutorisationService> _autorisation = new();
    private readonly ConsulterReferentielsConteneurQuery _query;

    public ConsulterReferentielsConteneurQueryTests()
    {
        _query = new ConsulterReferentielsConteneurQuery(_repo.Object, _autorisation.Object);
    }

    [Fact]
    public async Task Sans_habilitation_leve_AccesRefuse_et_ne_lit_aucun_referentiel()
    {
        var agentId = Guid.NewGuid();
        _autorisation
            .Setup(a => a.ExigerAsync(agentId, Permission.TarificationAdministrer, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesRefuseException("refusé"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => _query.ExecuterAsync(agentId));

        _repo.Verify(r => r.ListerSensAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.ListerTypesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.ListerTaillesAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.ListerCategoriesAsync(It.IsAny<Guid?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Avec_habilitation_retourne_les_quatre_listes_inactifs_inclus()
    {
        var agentId = Guid.NewGuid();
        _autorisation
            .Setup(a => a.ExigerAsync(agentId, Permission.TarificationAdministrer, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repo.Setup(r => r.ListerSensAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SensOperation>());
        _repo.Setup(r => r.ListerTypesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TypeConteneur>());
        _repo.Setup(r => r.ListerTaillesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TailleConteneur>());
        _repo.Setup(r => r.ListerCategoriesAsync(null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CategorieMarchandise>());

        var vue = await _query.ExecuterAsync(agentId);

        Assert.NotNull(vue);
        // inactifs inclus (actifsSeulement == false) sur chaque référentiel.
        _repo.Verify(r => r.ListerSensAsync(false, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.ListerTypesAsync(false, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.ListerTaillesAsync(false, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.ListerCategoriesAsync(null, false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
