using CIT.Plateforme.Domain.Tickets;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Tickets;

/// <summary>
/// [TEST-TECH] Décision D5/D11 de research.md (Feature 002) : extension additive de
/// <see cref="Dossier"/> avec <see cref="Dossier.AuteurId"/>, sans régression sur Feature 001.
/// </summary>
public sealed class DossierAuteurTests
{
    [Fact]
    public void Creer_sans_auteurId_signature_a_trois_arguments_laisse_AuteurId_null()
    {
        var clientId = Guid.NewGuid();
        var maintenant = DateTime.UtcNow;

        var dossier = Dossier.Creer("DOS-001", clientId, maintenant);

        Assert.Null(dossier.AuteurId);
        Assert.Equal(clientId, dossier.ClientId);
    }

    [Fact]
    public void Creer_avec_auteurId_fourni_initialise_AuteurId()
    {
        var societeId = Guid.NewGuid();
        var auteurId = Guid.NewGuid();
        var maintenant = DateTime.UtcNow;

        var dossier = Dossier.Creer("DOS-002", societeId, maintenant, auteurId);

        Assert.Equal(auteurId, dossier.AuteurId);
        Assert.Equal(societeId, dossier.ClientId);
    }
}
