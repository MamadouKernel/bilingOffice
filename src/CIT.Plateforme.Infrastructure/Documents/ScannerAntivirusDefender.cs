using System.Diagnostics;
using CIT.Plateforme.Domain.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Documents;

/// <summary>
/// Adaptateur vers le moteur antivirus Microsoft Defender CLI (MpCmdRun.exe) (SEC-07).
/// Exécute l'analyse en mode réel avec politique stricte Fail-Closed.
/// </summary>
public sealed class ScannerAntivirusDefender : IAnalyseAntivirus
{
    private readonly ILogger<ScannerAntivirusDefender> _logger;
    private readonly string? _cheminExecutableConfigure;
    private readonly int _timeoutSecondes;

    public ScannerAntivirusDefender(
        ILogger<ScannerAntivirusDefender> logger,
        IConfiguration? configuration = null)
    {
        _logger = logger;
        _cheminExecutableConfigure = configuration?["Securite:Antivirus:CheminExecutable"];
        _timeoutSecondes = configuration?.GetValue<int>("Securite:Antivirus:TimeoutSecondes") ?? 15;
    }

    public static string? ResoudreCheminMpCmdRun(string? cheminConfigure = null)
    {
        if (!string.IsNullOrWhiteSpace(cheminConfigure))
        {
            return File.Exists(cheminConfigure) ? cheminConfigure : null;
        }

        // Emplacements standards sur Windows
        var emplacementsStandards = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Defender", "MpCmdRun.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Defender", "MpCmdRun.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft", "Windows Defender", "Platform")
        };

        foreach (var emp in emplacementsStandards)
        {
            if (File.Exists(emp))
            {
                return emp;
            }

            // Cas du sous-dossier de version sous ProgramData\Microsoft\Windows Defender\Platform\<version>\MpCmdRun.exe
            if (Directory.Exists(emp))
            {
                var fichiers = Directory.GetFiles(emp, "MpCmdRun.exe", SearchOption.AllDirectories);
                if (fichiers.Length > 0)
                {
                    return fichiers.OrderByDescending(f => f).First();
                }
            }
        }

        return null;
    }

    public async Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default)
    {
        var cheminMpCmdRun = ResoudreCheminMpCmdRun(_cheminExecutableConfigure);
        if (cheminMpCmdRun is null)
        {
            _logger.LogWarning("Moteur Microsoft Defender introuvable sur le système hôte. Application de la politique Fail-Closed (rejet).");
            return StatutAnalyseAntivirus.Infecte;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), "cit-antivirus-scans");
        Directory.CreateDirectory(tempDirectory);
        var tempFilePath = Path.Combine(tempDirectory, $"scan-{Guid.NewGuid():N}.tmp");

        try
        {
            // Écriture du flux dans un fichier temporaire isolé
            contenu.Position = 0;
            await using (var fs = File.Create(tempFilePath))
            {
                await contenu.CopyToAsync(fs, ct).ConfigureAwait(false);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = cheminMpCmdRun,
                Arguments = $"-Scan -ScanType 3 -File \"{tempFilePath}\" -DisableRemediation",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_timeoutSecondes));

            try
            {
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Timeout ({Timeout}s) dépassé lors de l'analyse Defender. Rejet Fail-Closed.", _timeoutSecondes);
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return StatutAnalyseAntivirus.Infecte;
            }

            var exitCode = process.ExitCode;
            _logger.LogInformation("Analyse Defender terminée pour {Fichier} avec code de sortie {ExitCode}.", tempFilePath, exitCode);

            return exitCode switch
            {
                0 => StatutAnalyseAntivirus.Sain,
                2 => StatutAnalyseAntivirus.Infecte,
                _ => StatutAnalyseAntivirus.Infecte // Tout autre code est traité en Fail-Closed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'appel au processus Defender. Rejet Fail-Closed.");
            return StatutAnalyseAntivirus.Infecte;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try { File.Delete(tempFilePath); } catch { /* ignore */ }
            }
        }
    }
}
