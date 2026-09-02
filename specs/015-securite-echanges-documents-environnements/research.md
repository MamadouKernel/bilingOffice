# RECHERCHE TECHNIQUE & ANALYSE FORENSIQUE DURCIE — FEATURE 015

## Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement

---

### 1. ANALYSE DU MOTEUR ANTIVIRUS RÉEL — MICROSOFT DEFENDER CLI (`MpCmdRun.exe`)

- **Chemin système standard** : `C:\Program Files\Windows Defender\MpCmdRun.exe` (ou via dossier Platform sous `%ProgramData%`).
- **Commande d'analyse d'un fichier unique** :
  ```cmd
  "C:\Program Files\Windows Defender\MpCmdRun.exe" -Scan -ScanType 3 -File "<CheminFichierTemporaire>"
  ```
- **Codes de sortie officiels Microsoft Defender** :
  - **`0`** : *No threats found* -> `StatutAnalyseAntivirus.Sain`.
  - **`2`** : *Threats found (or remediated/quarantined)* -> `StatutAnalyseAntivirus.Infecte`.
  - **Autre** (`1`, `error`, etc.) : *Scan failed or aborted* -> `StatutAnalyseAntivirus.Infecte` / Fail-Closed.
- **Support de la signature standard EICAR** :
  - Chaîne EICAR standard : `X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*`
  - Defender détecte immédiatement cette signature et renvoie le code de sortie `2` (`Threat detected: Virus:DOS/EICAR_Test_File`).
- **Performance & Isolation** :
  - Temps d'analyse pour un fichier unitaire : < 300 ms.
  - Aucune donnée n'est transmise hors de la machine (100 % local, souverain, 0 SaaS cloud).

---

### 2. ANALYSE DE L'ADAPTABILITÉ LINUX / CLAMAV

- **Commande standard ClamAV** : `clamscan --no-summary "<CheminFichier>"` ou via le socket local `clamd` (commande `SCAN / chemin`).
- **Codes de sortie ClamAV** :
  - `0` : Pas de virus détecté (Sain).
  - `1` : Virus détecté (Infecté).
  - `2` : Erreur (Fail-Closed).

---

### 3. CONCEPTION DE L'ADAPTATEUR D'INFRASTRUCTURE

```csharp
public sealed class ScannerAntivirusDefender(ILogger<ScannerAntivirusDefender> logger) : IAnalyseAntivirus
{
    private static readonly string CheminMpCmdRun =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Defender", "MpCmdRun.exe");

    public async Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default)
    {
        if (!File.Exists(CheminMpCmdRun))
        {
            logger.LogWarning("Moteur Microsoft Defender (MpCmdRun.exe) introuvable. Application de la politique Fail-Closed.");
            return StatutAnalyseAntivirus.Infecte;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"cit-scan-{Guid.NewGuid():N}.tmp");
        try
        {
            contenu.Position = 0;
            await using (var fs = File.Create(tempFile))
            {
                await contenu.CopyToAsync(fs, ct).ConfigureAwait(false);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = CheminMpCmdRun,
                Arguments = $"-Scan -ScanType 3 -File \"{tempFile}\" -DisableRemediation",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null) return StatutAnalyseAntivirus.Infecte;

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            return process.ExitCode switch
            {
                0 => StatutAnalyseAntivirus.Sain,
                2 => StatutAnalyseAntivirus.Infecte,
                _ => StatutAnalyseAntivirus.Infecte // Fail-Closed
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur lors de l'exécution du scan antivirus Defender. Rejet Fail-Closed.");
            return StatutAnalyseAntivirus.Infecte;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { /* ignore */ }
            }
        }
    }
}
```
