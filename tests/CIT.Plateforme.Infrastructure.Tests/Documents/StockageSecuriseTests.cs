using System.Text;
using CIT.Plateforme.Infrastructure.Documents;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Documents;

/// <summary>
/// TEST-SPEC SEC-CERT-04-03 : Isolation Physique hors wwwroot et Protection GED.
/// </summary>
public sealed class StockageSecuriseTests
{
    [Fact]
    public async Task SecCert04_03_StockageEstHorsWwwroot_EtCleEstOpaque()
    {
        // Arrange
        var tempStorageDir = Path.Combine(Path.GetTempPath(), $"cit-sec-test-{Guid.NewGuid():N}");
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Stockage:RepertoireDocuments"] = tempStorageDir
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var storage = new StockageDocumentsFichiers(config);

        var contenu = new MemoryStream(Encoding.UTF8.GetBytes("Contenu confidentiel GED"));
        var nomFichierOriginal = "dossier_douane_2026.pdf";

        try
        {
            // Act
            var cle = await storage.EnregistrerAsync(contenu, nomFichierOriginal);

            // Assert
            Assert.False(cle.Contains(".."), "La clé ne doit pas permettre de traversée de répertoire (Path Traversal).");
            Assert.DoesNotContain("wwwroot", tempStorageDir.ToLowerInvariant());

            var fichierPhysique = Path.Combine(tempStorageDir, cle);
            Assert.True(File.Exists(fichierPhysique), "Le fichier doit exister dans l'espace de stockage sécurisé.");

            // Vérification de la lecture
            using var streamLu = await storage.LireAsync(cle);
            using var reader = new StreamReader(streamLu);
            var texteLu = await reader.ReadToEndAsync();
            Assert.Equal("Contenu confidentiel GED", texteLu);
        }
        finally
        {
            if (Directory.Exists(tempStorageDir))
            {
                Directory.Delete(tempStorageDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SecCert04_03_TentativePathTraversal_EstSanitisee()
    {
        // Arrange
        var tempStorageDir = Path.Combine(Path.GetTempPath(), $"cit-sec-test-{Guid.NewGuid():N}");
        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["Stockage:RepertoireDocuments"] = tempStorageDir
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var storage = new StockageDocumentsFichiers(config);

        var contenu = new MemoryStream(Encoding.UTF8.GetBytes("Attaque traversal"));
        var nomFichierMalveillant = "../../../etc/passwd.pdf";

        try
        {
            // Act
            var cle = await storage.EnregistrerAsync(contenu, nomFichierMalveillant);

            // Assert
            Assert.False(cle.StartsWith(".."), "Le chemin doit être sanitisé.");
            Assert.DoesNotContain("/", cle);
            Assert.DoesNotContain("\\", cle);

            var fichierPhysique = Path.Combine(tempStorageDir, cle);
            Assert.True(File.Exists(fichierPhysique));
        }
        finally
        {
            if (Directory.Exists(tempStorageDir))
            {
                Directory.Delete(tempStorageDir, recursive: true);
            }
        }
    }
}
