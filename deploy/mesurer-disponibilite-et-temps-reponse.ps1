<#
  NFR-03 / NFR-04 / NFR-10 — preuve mesurée de la disponibilité et du temps de réponse.

  Interroge des pages publiques et la sonde /ready, et ajoute une ligne par mesure dans un CSV
  (HorodatageUtc, Url, Statut, Ms). À planifier en tâche Windows sur un poste du réseau CIT
  (pas sur le serveur lui-même : on mesure ce que voit l'utilisateur).

  Mesure (à planifier toutes les 5 min, 24/7) :
    powershell -File mesurer-disponibilite-et-temps-reponse.ps1 -Base https://10.88.179.103:8443 -Sortie D:\Mesures\yaba.csv
  Rapport (taux de disponibilité, cible NFR-03 >= 99 % ; p95, cible NFR-04 < 3 000 ms) :
    powershell -File mesurer-disponibilite-et-temps-reponse.ps1 -Sortie D:\Mesures\yaba.csv -Rapport

  ALERTE si le site tombe : après -SeuilEchecs mesures consécutives de /ready en échec, un e-mail
  part vers -AlerteA ; un second annonce le rétablissement. Un seul message par panne (fichier
  d'état à côté du CSV).
  Serveur SMTP : -ConfigYaba reprend la section Notifications:Smtp de l'appsettings.Production.json
  de YABA (relais, port, SSL, expéditeur, compte, mot de passe) : rien à ressaisir. Sinon, paramètres
  -Smtp* et mot de passe en variable d'environnement MACHINE YABA_SMTP_MOTDEPASSE (jamais en clair ici).
    powershell -File mesurer-disponibilite-et-temps-reponse.ps1 -Sortie D:\Mesures\yaba.csv -AlerteA exploitation@cotedivoireterminal.com -ConfigYaba C:\inetpub\yaba\appsettings.Production.json
  Vérifier l'envoi : mêmes paramètres + -TesterAlerte (envoie un e-mail de test).
  Installation de la tâche planifiée (toutes les 5 min, 24/7, compte SYSTEM ; console administrateur) :
  mêmes paramètres + -Installer.
#>
param(
    [string]$Base = 'https://10.88.179.103:8443',
    [string]$Sortie = '.\mesures-yaba.csv',
    [string[]]$Chemins = @('/ready', '/comptes/connexion', '/aide'),
    [switch]$Rapport,
    [string[]]$AlerteA = @(),
    [string]$SmtpHote = '',
    [int]$SmtpPort = 587,
    [string]$SmtpUtilisateur = '',
    [string]$Expediteur = 'no-reply@cotedivoireterminal.com',
    [bool]$SmtpSsl = $true,
    [string]$ConfigYaba = '',
    [int]$SeuilEchecs = 2,
    [switch]$Installer,
    [switch]$TesterAlerte
)

# Lancé par la tâche planifiée (-File), "-AlerteA a@x,b@y" arrive en une seule chaîne.
$AlerteA = @($AlerteA | ForEach-Object { $_ -split '[,;]' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$Sortie = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Sortie)

$SmtpMotDePasse = $env:YABA_SMTP_MOTDEPASSE
if ($ConfigYaba) {
    $ConfigYaba = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ConfigYaba)
    # Un paramètre passé explicitement l'emporte sur la configuration de YABA.
    $smtp = (Get-Content $ConfigYaba -Raw -Encoding UTF8 | ConvertFrom-Json).Notifications.Smtp
    if (-not $smtp) { throw "Section Notifications:Smtp absente de $ConfigYaba" }
    if (-not $PSBoundParameters.ContainsKey('SmtpHote')) { $SmtpHote = $smtp.Hote }
    if (-not $PSBoundParameters.ContainsKey('SmtpPort') -and $smtp.Port) { $SmtpPort = [int]$smtp.Port }
    if (-not $PSBoundParameters.ContainsKey('SmtpSsl') -and $null -ne $smtp.UtiliserSsl) { $SmtpSsl = [bool]$smtp.UtiliserSsl }
    if (-not $PSBoundParameters.ContainsKey('SmtpUtilisateur')) { $SmtpUtilisateur = $smtp.Utilisateur }
    if (-not $PSBoundParameters.ContainsKey('Expediteur') -and $smtp.Expediteur) { $Expediteur = $smtp.Expediteur }
    if (-not $SmtpMotDePasse) { $SmtpMotDePasse = $smtp.MotDePasse }
}

if ($Installer) {
    # Reprend les mêmes paramètres (sauf -Installer) dans une tâche lancée toutes les 5 minutes.
    $PSBoundParameters['Sortie'] = $Sortie   # chemin absolu : SYSTEM démarre dans System32
    if ($ConfigYaba) { $PSBoundParameters['ConfigYaba'] = $ConfigYaba }
    $arguments = ($PSBoundParameters.GetEnumerator() | Where-Object { $_.Key -notin 'Installer', 'TesterAlerte' } | ForEach-Object {
        $valeur = if ($_.Value -is [array]) { ($_.Value -join ',') } else { $_.Value }
        "-$($_.Key) `"$valeur`"" }) -join ' '
    $action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" $arguments"
    $declencheur = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 5)
    $compte = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
    Register-ScheduledTask -TaskName 'YABA - Surveillance disponibilite' -Action $action -Trigger $declencheur -Principal $compte -Force | Out-Null
    if (-not $ConfigYaba -and -not [Environment]::GetEnvironmentVariable('YABA_SMTP_MOTDEPASSE', 'Machine') -and $SmtpUtilisateur) {
        Write-Output 'ATTENTION : YABA_SMTP_MOTDEPASSE absente au niveau Machine, la tâche ne pourra pas envoyer d''alerte.'
    }
    Write-Output 'Tâche planifiée « YABA - Surveillance disponibilite » installée (toutes les 5 min).'
    return
}

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

$readyOk = $true
foreach ($chemin in $Chemins) {
    $url = $Base.TrimEnd('/') + $chemin
    $chrono = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $statut = (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 30).StatusCode
    } catch {
        $statut = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
    }
    $chrono.Stop()
    if ($chemin -eq '/ready' -and $statut -ne 200) { $readyOk = $false }
    [pscustomobject]@{
        HorodatageUtc = (Get-Date).ToUniversalTime().ToString('o')
        Url           = $url
        Statut        = $statut
        Ms            = $chrono.ElapsedMilliseconds
    } | Export-Csv -Path $Sortie -Append -NoTypeInformation -Encoding UTF8
}

# --- Alerte de panne et de rétablissement (un message par changement d'état) -------------------
$fichierEtat = [IO.Path]::ChangeExtension($Sortie, '.etat.json')
$etat = if (Test-Path $fichierEtat) { Get-Content $fichierEtat -Raw | ConvertFrom-Json } else { [pscustomobject]@{ echecsConsecutifs = 0; enPanne = $false; depuisUtc = $null } }
$maintenant = (Get-Date).ToUniversalTime().ToString('o')
$message = $null

if ($readyOk) {
    if ($etat.enPanne) { $message = @{ Sujet = "[YABA] Rétabli - $Base répond de nouveau"; Corps = "YABA ($Base) répond de nouveau depuis $maintenant UTC. Panne commencée le $($etat.depuisUtc) UTC." } }
    $etat = [pscustomobject]@{ echecsConsecutifs = 0; enPanne = $false; depuisUtc = $null }
}
else {
    $echecs = [int]$etat.echecsConsecutifs + 1
    $depuis = if ($etat.depuisUtc) { $etat.depuisUtc } else { $maintenant }
    $enPanne = $echecs -ge $SeuilEchecs
    if ($enPanne -and -not $etat.enPanne) { $message = @{ Sujet = "[YABA] PANNE - $Base ne répond pas"; Corps = "La sonde $Base/ready échoue depuis $depuis UTC ($echecs mesures consécutives). Vérifier le pool IIS yaba, la base SQL et /ready." } }
    $etat = [pscustomobject]@{ echecsConsecutifs = $echecs; enPanne = $enPanne; depuisUtc = $depuis }
}
$etat | ConvertTo-Json | Set-Content $fichierEtat -Encoding UTF8

if ($TesterAlerte) { $message = @{ Sujet = "[YABA] Test d'alerte de disponibilité"; Corps = "Message de test : la surveillance de $Base peut envoyer ses alertes à $($AlerteA -join ', ')." } }

if ($message) {
    Write-Output $message.Sujet
    if ($AlerteA.Count -and $SmtpHote) {
        try {
            $client = New-Object System.Net.Mail.SmtpClient($SmtpHote, $SmtpPort)
            $client.EnableSsl = $SmtpSsl
            $client.Timeout = 20000
            if ($SmtpUtilisateur -and $SmtpMotDePasse) { $client.Credentials = New-Object System.Net.NetworkCredential($SmtpUtilisateur, $SmtpMotDePasse) }
            $mail = New-Object System.Net.Mail.MailMessage
            $mail.From = $Expediteur
            foreach ($a in $AlerteA) { $mail.To.Add($a) }
            $mail.Subject = $message.Sujet
            $mail.Body = $message.Corps
            $client.Send($mail)
            Write-Output "Alerte envoyée à $($AlerteA -join ', ')"
        }
        catch { Write-Output "ECHEC d'envoi de l'alerte : $($_.Exception.Message)" }
    }
    else { Write-Output '(aucun destinataire/serveur SMTP configuré : alerte seulement journalisée ici)' }
}
