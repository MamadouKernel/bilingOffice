using System.Net;
using System.Text.Json;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Formulaires;
using CIT.Plateforme.Domain.Formulaires;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Formulaires;

[Collection("PlateformeWebCollection")]
public sealed class ErrCert01WebBrouillonsTests : IClassFixture<PlateformeWebApplicationFactory>
{
    private readonly PlateformeWebApplicationFactory _factory;

    public ErrCert01WebBrouillonsTests(PlateformeWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Brouillon_PersistanceEtRestauration_NeContientAucunFluxBinaire()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var enregistrerCmd = scope.ServiceProvider.GetRequiredService<EnregistrerBrouillonCommand>();
        var obtenirQuery = scope.ServiceProvider.GetRequiredService<ObtenirBrouillonQuery>();
        var supprimerCmd = scope.ServiceProvider.GetRequiredService<SupprimerBrouillonCommand>();

        var userId = Guid.NewGuid();
        var societeId = Guid.NewGuid();
        var typeFormulaire = "DemandeImport";

        var payload = new
        {
            NumeroBl = "BL-TEST-999",
            TypeClient = "ClientNormal",
            FichiersMetadonnees = new[]
            {
                new { Nom = "connaissement.pdf", Taille = 1048576, Type = "application/pdf" }
            }
        };

        var json = JsonSerializer.Serialize(payload);

        // Act - 1. Enregistrement
        var brouillonId = await enregistrerCmd.ExecuterAsync(userId, societeId, typeFormulaire, json);
        Assert.NotEqual(Guid.Empty, brouillonId);

        // Act - 2. Restauration
        var restaure = await obtenirQuery.ExecuterAsync(userId, typeFormulaire);

        // Assert - 2. Vérification
        Assert.NotNull(restaure);
        Assert.Equal(typeFormulaire, restaure.TypeFormulaire);
        Assert.Contains("BL-TEST-999", restaure.DonneesJson);
        Assert.Contains("connaissement.pdf", restaure.DonneesJson);
        // Invariant I6 : Aucun binaire brut / base64 lourd
        Assert.DoesNotContain("base64", restaure.DonneesJson, StringComparison.OrdinalIgnoreCase);

        // Act - 3. Purge
        await supprimerCmd.ExecuterAsync(userId, typeFormulaire);

        // Assert - 3. Vérification purge
        var apresPurge = await obtenirQuery.ExecuterAsync(userId, typeFormulaire);
        Assert.Null(apresPurge);
    }

    [Fact]
    public async Task Brouillon_CloisonnementMultiTenant_AutreUtilisateurNeVoitPasLeBrouillon()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var enregistrerCmd = scope.ServiceProvider.GetRequiredService<EnregistrerBrouillonCommand>();
        var obtenirQuery = scope.ServiceProvider.GetRequiredService<ObtenirBrouillonQuery>();

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var societeA = Guid.NewGuid();
        var typeFormulaire = "DemandeExport";

        await enregistrerCmd.ExecuterAsync(userA, societeA, typeFormulaire, "{\"booking\":\"BK-001\"}");

        // Act
        var brouillonB = await obtenirQuery.ExecuterAsync(userB, typeFormulaire);

        // Assert
        Assert.Null(brouillonB);
    }
}
