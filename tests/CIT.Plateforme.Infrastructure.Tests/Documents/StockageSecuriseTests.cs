using System.Text;
using CIT.Plateforme.Infrastructure.Documents;
using CIT.Plateforme.Infrastructure.Tests.Support;
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
        var storage = new StockageDocumentsFichiers(config, new EnvironnementDeTest("Development"));

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
        var storage = new StockageDocumentsFichiers(config, new EnvironnementDeTest("Development"));

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

    /// <summary>SEC-04 — pièces chiffrées au repos (AES-256-GCM) quand la clé est configurée.</summary>
    [Fact]
    public async Task SecCert04_Piece_chiffree_au_repos_relue_en_clair_et_alteration_detectee()
    {
        var dossier = Path.Combine(Path.GetTempPath(), $"cit-sec-test-{Guid.NewGuid():N}");
        var cleAes = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Stockage:RepertoireDocuments"] = dossier,
            ["Stockage:CleChiffrement"] = cleAes,
        }).Build();
        var stockage = new StockageDocumentsFichiers(config, new EnvironnementDeTest("Development"));

        try
        {
            // Pièce historique déposée AVANT l'activation du chiffrement : toujours lisible.
            Directory.CreateDirectory(dossier);
            await File.WriteAllTextAsync(Path.Combine(dossier, "ancienne-piece.pdf"), "Ancien contenu en clair");
            using (var ancien = new StreamReader(await stockage.LireAsync("ancienne-piece.pdf")))
            {
                Assert.Equal("Ancien contenu en clair", await ancien.ReadToEndAsync());
            }

            var cle = await stockage.EnregistrerAsync(new MemoryStream(Encoding.UTF8.GetBytes("BADT confidentiel")), "badt.pdf");
            var surDisque = await File.ReadAllBytesAsync(Path.Combine(dossier, cle));
            Assert.DoesNotContain("BADT confidentiel", Encoding.UTF8.GetString(surDisque));

            using (var lu = new StreamReader(await stockage.LireAsync(cle)))
            {
                Assert.Equal("BADT confidentiel", await lu.ReadToEndAsync());
            }

            surDisque[^1] ^= 0xFF; // altération d'un octet chiffré
            await File.WriteAllBytesAsync(Path.Combine(dossier, cle), surDisque);
            await Assert.ThrowsAnyAsync<System.Security.Cryptography.CryptographicException>(() => stockage.LireAsync(cle));
        }
        finally
        {
            if (Directory.Exists(dossier))
            {
                Directory.Delete(dossier, recursive: true);
            }
        }
    }
}
