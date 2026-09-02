<#
.SYNOPSIS
    Precontroles NON DESTRUCTIFS d'un environnement Staging pour la plateforme CIT.

.DESCRIPTION
    Verifie que l'environnement reunit ce que l'application exige AVANT tout deploiement.
    Le script est en LECTURE SEULE : il ne cree, ne modifie et ne supprime rien, a une
    exception documentee (test d'ecriture dans le repertoire de stockage, qui supprime
    immediatement le fichier temoin qu'il vient de creer).

    AUCUN SECRET N'EST CONTENU DANS CE SCRIPT.
    La chaine de connexion et les autres secrets sont passes en parametre ou lus dans
    l'environnement ; ils ne sont jamais affiches.

.EXAMPLE
    ./precheck.ps1 -BaseUrl https://staging.exemple -RepertoireDocuments D:\cit\documents
#>
[CmdletBinding()]
param(
    [string] $BaseUrl,
    [string] $RepertoireDocuments,
    [string] $ServeurSql,
    [int]    $EspaceDisqueMinimumGo = 10
)

$script:Echecs = 0
$script:Avertissements = 0

function Test-Point {
    param([string] $Libelle, [scriptblock] $Controle)
    try {
        $r = & $Controle
        if ($r -eq $true)      { Write-Host ("  [ OK ]      {0}" -f $Libelle) -ForegroundColor Green }
        elseif ($r -eq $false) { Write-Host ("  [ ECHEC ]   {0}" -f $Libelle) -ForegroundColor Red; $script:Echecs++ }
        else                   { Write-Host ("  [ INFO ]    {0} : {1}" -f $Libelle, $r) }
    } catch {
        Write-Host ("  [ ECHEC ]   {0} : {1}" -f $Libelle, $_.Exception.Message) -ForegroundColor Red
        $script:Echecs++
    }
}

function Test-Optionnel {
    param([string] $Libelle, [scriptblock] $Controle)
    try {
        $r = & $Controle
        if ($r -eq $true) { Write-Host ("  [ OK ]      {0}" -f $Libelle) -ForegroundColor Green }
        else { Write-Host ("  [ A VOIR ]  {0}" -f $Libelle) -ForegroundColor Yellow; $script:Avertissements++ }
    } catch {
        Write-Host ("  [ A VOIR ]  {0} : {1}" -f $Libelle, $_.Exception.Message) -ForegroundColor Yellow
        $script:Avertissements++
    }
}

Write-Host ""
Write-Host "PRECONTROLES STAGING - Plateforme CIT" -ForegroundColor Cyan
Write-Host "======================================"
Write-Host ""

# --- 1. Runtime ---------------------------------------------------------------
Write-Host "1. Runtime"
Test-Point ".NET SDK ou runtime present" {
    $null -ne (Get-Command dotnet -ErrorAction SilentlyContinue)
}
Test-Point "Runtime ASP.NET Core 10 installe" {
    $r = & dotnet --list-runtimes 2>$null
    [bool]($r | Select-String -SimpleMatch "Microsoft.AspNetCore.App 10.")
}

# --- 2. Stockage documentaire -------------------------------------------------
Write-Host ""
Write-Host "2. Stockage documentaire"
if ($RepertoireDocuments) {
    Test-Point "Repertoire de stockage existant" { Test-Path -Path $RepertoireDocuments }
    Test-Point "Ecriture autorisee (fichier temoin supprime aussitot)" {
        $t = Join-Path $RepertoireDocuments ("precheck-{0}.tmp" -f ([guid]::NewGuid()))
        try { "precheck" | Out-File -FilePath $t -Encoding utf8 -ErrorAction Stop; $true }
        finally { if (Test-Path $t) { Remove-Item $t -Force -ErrorAction SilentlyContinue } }
    }
    Test-Point "Espace disque disponible" {
        $d = (Get-Item $RepertoireDocuments).PSDrive
        $go = [math]::Round($d.Free / 1GB, 1)
        if ($go -lt $EspaceDisqueMinimumGo) { $false } else { "$go Go libres" }
    }
} else {
    Write-Host "  [ A VOIR ]  RepertoireDocuments non fourni - controle ignore" -ForegroundColor Yellow
    Write-Host "              Rappel : non configure, l'application ecrit dans %TEMP%." -ForegroundColor Yellow
    $script:Avertissements++
}

# --- 3. Base de donnees -------------------------------------------------------
Write-Host ""
Write-Host "3. Base de donnees"
if ($ServeurSql) {
    Test-Point "Serveur SQL joignable (port 1433)" {
        $h = $ServeurSql.Split(',')[0].Split('\')[0]
        (Test-NetConnection -ComputerName $h -Port 1433 -WarningAction SilentlyContinue).TcpTestSucceeded
    }
} else {
    Write-Host "  [ A VOIR ]  ServeurSql non fourni - controle ignore" -ForegroundColor Yellow
    $script:Avertissements++
}

# --- 4. Antivirus -------------------------------------------------------------
Write-Host ""
Write-Host "4. Antivirus"
Test-Optionnel "Microsoft Defender CLI (MpCmdRun.exe) present" {
    Test-Path "$env:ProgramFiles\Windows Defender\MpCmdRun.exe"
}
Write-Host "              Rappel : FailClosed=true - un antivirus absent BLOQUE les depots." -ForegroundColor DarkGray

# --- 5. Variables d'environnement --------------------------------------------
Write-Host ""
Write-Host "5. Configuration"
Test-Point "ASPNETCORE_ENVIRONMENT positionne" {
    if ([string]::IsNullOrWhiteSpace($env:ASPNETCORE_ENVIRONMENT)) { $false }
    elseif ($env:ASPNETCORE_ENVIRONMENT -eq 'Development') {
        Write-Host "              ATTENTION : 'Development' active le jeu de donnees de test." -ForegroundColor Red
        $false
    } else { $env:ASPNETCORE_ENVIRONMENT }
}
Test-Optionnel "Chaine de connexion fournie (presence seule, valeur non affichee)" {
    -not [string]::IsNullOrWhiteSpace($env:ConnectionStrings__PlateformeDb)
}
Test-Optionnel "Notifications:Reessai fourni par variable d'environnement" {
    (-not [string]::IsNullOrWhiteSpace($env:Notifications__Reessai__NombreTentativesMax)) -and
    (-not [string]::IsNullOrWhiteSpace($env:Notifications__Reessai__DelaiEntreTentatives))
}
Write-Host "              Rappel : sans ces deux valeurs, l'application NE DEMARRE PAS." -ForegroundColor DarkGray
Write-Host "              Elles peuvent aussi venir d'un appsettings propre a l'environnement." -ForegroundColor DarkGray

# --- 6. Sondes applicatives ---------------------------------------------------
Write-Host ""
Write-Host "6. Sondes applicatives (si l'application est deja deployee)"
if ($BaseUrl) {
    Test-Optionnel "/healthz repond 200" {
        (Invoke-WebRequest -Uri "$BaseUrl/healthz" -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200
    }
    Test-Optionnel "/ready repond 200 (atteste l'acces a la base)" {
        (Invoke-WebRequest -Uri "$BaseUrl/ready" -UseBasicParsing -TimeoutSec 10).StatusCode -eq 200
    }
    Test-Optionnel "Certificat TLS valide" {
        try { Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -TimeoutSec 10 | Out-Null; $true } catch { $false }
    }
    Test-Optionnel "En-tete Server-Timing present (requis pour NFR-04)" {
        $r = Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -TimeoutSec 10
        $null -ne $r.Headers['Server-Timing']
    }
} else {
    Write-Host "  [ INFO ]    BaseUrl non fournie - controles applicatifs ignores"
}

# --- Synthese -----------------------------------------------------------------
Write-Host ""
Write-Host "======================================"
Write-Host ("Echecs : {0}   Points a voir : {1}" -f $script:Echecs, $script:Avertissements)
if ($script:Echecs -gt 0) {
    Write-Host "PRECONTROLES : ECHEC - ne pas deployer en l'etat." -ForegroundColor Red
    exit 1
}
Write-Host "PRECONTROLES : OK" -ForegroundColor Green
exit 0
