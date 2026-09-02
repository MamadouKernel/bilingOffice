using System.Text;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Infrastructure.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Documents;

/// <summary>
/// TEST-SPEC SEC-CERT-07-04, 07-05, 07-06 : Preuve sur Moteur Antivirus Réel (Microsoft Defender MpCmdRun).
/// Ces tests exécutent réellement le processus antivirus de l'hôte et vérifient son code retour.
/// </summary>
public sealed class ScannerAntivirusIntegrationTests
{
    private static readonly string? CheminMpCmdRun = ScannerAntivirusDefender.ResoudreCheminMpCmdRun();

    [Fact]
    public async Task SecCert07_04_FichierSain_EstAnalyseParMoteurReelEtDeclareSain()
    {
        // Skip condition si exécuté sur un OS sans Defender
        if (CheminMpCmdRun is null)
        {
            Assert.Fail("Moteur Defender (MpCmdRun.exe) introuvable sur la machine hôte : la preuve réelle SEC-07 ne peut être omise.");
            return;
        }

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Securite:Antivirus:CheminExecutable"] = CheminMpCmdRun,
            ["Securite:Antivirus:TimeoutSecondes"] = "30"
        }).Build();

        var scanner = new ScannerAntivirusDefender(NullLogger<ScannerAntivirusDefender>.Instance, config);

        var contenuSain = Encoding.UTF8.GetBytes("Ceci est un fichier texte légitime et parfaitement sain.");
        using var stream = new MemoryStream(contenuSain);

        // Act - Appel du vrai processus MpCmdRun.exe
        var resultat = await scanner.AnalyserAsync(stream);

        // Assert - Code retour 0 attendu de Defender
        Assert.Equal(StatutAnalyseAntivirus.Sain, resultat);
    }

    [Fact]
    public async Task SecCert07_05_FichierEicar_EstDetecteParMoteurReelEtDeclareInfecte()
    {
        if (CheminMpCmdRun is null)
        {
            Assert.Fail("Moteur Defender (MpCmdRun.exe) introuvable sur la machine hôte : la preuve réelle SEC-07 ne peut être omise.");
            return;
        }

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Securite:Antivirus:CheminExecutable"] = CheminMpCmdRun,
            ["Securite:Antivirus:TimeoutSecondes"] = "30"
        }).Build();

        var scanner = new ScannerAntivirusDefender(NullLogger<ScannerAntivirusDefender>.Instance, config);

        // Signature standard EICAR (reconnue par tous les antivirus conformes)
        const string eicarStandard = @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";
        var contenuEicar = Encoding.ASCII.GetBytes(eicarStandard);
        using var stream = new MemoryStream(contenuEicar);

        // Act - Appel du vrai processus MpCmdRun.exe
        var resultat = await scanner.AnalyserAsync(stream);

        // Assert - Code retour 2 (Menace détectée) attendu de Defender
        Assert.Equal(StatutAnalyseAntivirus.Infecte, resultat);
    }

    [Fact]
    public async Task SecCert07_06_MoteurAbsentOuInvalide_AppliquePolitiqueFailClosed()
    {
        // Configuration avec un chemin d'exécutable délibérément introuvable
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Securite:Antivirus:CheminExecutable"] = @"C:\Chemin\Inexistant\FauxAntivirus.exe",
            ["Securite:Antivirus:TimeoutSecondes"] = "5"
        }).Build();

        var scanner = new ScannerAntivirusDefender(NullLogger<ScannerAntivirusDefender>.Instance, config);

        var contenu = Encoding.UTF8.GetBytes("Fichier quelconque");
        using var stream = new MemoryStream(contenu);

        // Act
        var resultat = await scanner.AnalyserAsync(stream);

        // Assert - Doit refuser le fichier par politique Fail-Closed
        Assert.Equal(StatutAnalyseAntivirus.Infecte, resultat);
    }
}
