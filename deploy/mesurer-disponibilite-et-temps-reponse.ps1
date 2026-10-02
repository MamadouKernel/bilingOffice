<#
  NFR-03 / NFR-04 / NFR-10 — preuve mesurée de la disponibilité et du temps de réponse.

  Interroge des pages publiques et la sonde /ready, et ajoute une ligne par mesure dans un CSV
  (HorodatageUtc, Url, Statut, Ms). À planifier en tâche Windows sur un poste du réseau CIT
  (pas sur le serveur lui-même : on mesure ce que voit l'utilisateur).

  Mesure (à planifier toutes les 5 min, 24/7) :
    powershell -File mesurer-disponibilite-et-temps-reponse.ps1 -Base https://10.88.179.103:8443 -Sortie D:\Mesures\yaba.csv
  Rapport (taux de disponibilité, cible NFR-03 >= 99 % ; p95, cible NFR-04 < 3 000 ms) :
    powershell -File mesurer-disponibilite-et-temps-reponse.ps1 -Sortie D:\Mesures\yaba.csv -Rapport
#>
param(
    [string]$Base = 'https://10.88.179.103:8443',
    [string]$Sortie = '.\mesures-yaba.csv',
    [string[]]$Chemins = @('/ready', '/comptes/connexion', '/aide'),
    [switch]$Rapport
)

$SeuilMs = 3000

if ($Rapport) {
    $lignes = @(Import-Csv $Sortie)
    if ($lignes.Count -eq 0) { Write-Output 'Aucune mesure.'; return }
    $ok = @($lignes | Where-Object { $_.Statut -eq '200' })
    $dispo = [math]::Round(100 * $ok.Count / $lignes.Count, 2)
    $tri = @($ok | ForEach-Object { [int]$_.Ms } | Sort-Object)
    $p95 = if ($tri.Count) { $tri[[math]::Min($tri.Count - 1, [math]::Floor(0.95 * $tri.Count))] } else { 0 }
    Write-Output ("Période : {0} -> {1}" -f $lignes[0].HorodatageUtc, $lignes[-1].HorodatageUtc)
    Write-Output ("Mesures : {0}  | Disponibilité : {1} % (cible >= 99 %)" -f $lignes.Count, $dispo)
    Write-Output ("Temps de réponse p95 : {0} ms (cible < {1} ms)" -f $p95, $SeuilMs)
    return
}

# Certificat interne non reconnu (écart SEC-04) : la mesure ne doit pas échouer pour cette raison.
if (-not ([System.Management.Automation.PSTypeName]'ToutCertificat').Type) {
    Add-Type 'public class ToutCertificat : System.Net.ICertificatePolicy { public bool CheckValidationResult(System.Net.ServicePoint s, System.Security.Cryptography.X509Certificates.X509Certificate c, System.Net.WebRequest r, int p) { return true; } }'
}
[System.Net.ServicePointManager]::CertificatePolicy = New-Object ToutCertificat
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

foreach ($chemin in $Chemins) {
    $url = $Base.TrimEnd('/') + $chemin
    $chrono = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $statut = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30).StatusCode
    } catch {
        $statut = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    }
    $chrono.Stop()
    [pscustomobject]@{
        HorodatageUtc = (Get-Date).ToUniversalTime().ToString('o')
        Url           = $url
        Statut        = $statut
        Ms            = $chrono.ElapsedMilliseconds
    } | Export-Csv -Path $Sortie -Append -NoTypeInformation -Encoding UTF8
}
