<#
.SYNOPSIS
  NFR-06 — Sauvegarde YABA (base + pièces au même instant) et test de restauration vérifié.

.DESCRIPTION
  Mode Sauvegarder : BACKUP COPY_ONLY + CHECKSUM de la base, puis archive zip du répertoire des
  pièces, horodatés à l'identique (point de cohérence, docs/41 § 2).
  Mode Tester : restaure la dernière sauvegarde dans une base DISTINCTE (<Base>_TestRestauration),
  contrôle l'intégrité (RESTORE VERIFYONLY), compare les volumes des tables clés avec la base
  source, relève le dernier horodatage d'audit, vérifie qu'un échantillon de pièces référencées
  existe dans l'archive, puis supprime la base de test. Code de sortie 0 = restauration vérifiée.

  La base source n'est que lue.

.EXAMPLE
  .\sauvegarde-restauration.ps1 -Serveur . -Base DbCITplateforme -DossierPieces "$env:TEMP\cit-plateforme-documents"
#>
param(
    [ValidateSet("Sauvegarder", "Tester", "Les deux")] [string] $Mode = "Les deux",
    [string] $Serveur = ".",
    [string] $Base = "yaba",
    [string] $DossierSauvegarde = "C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\Backup",
    [string] $DossierPieces = "",
    # Archive des pièces : dossier accessible au compte qui lance le script (le dossier de
    # sauvegarde SQL n'est en général lisible que par le service SQL Server).
    [string] $DossierArchive = (Join-Path $env:PUBLIC "yaba-sauvegardes"),
    [int] $EchantillonPieces = 20
)
$ErrorActionPreference = "Stop"

function Sql([string] $requete, [string] $base = "master") {
    $sortie = & sqlcmd -S $Serveur -E -d $base -b -h -1 -W -s "|" -Q "SET NOCOUNT ON; $requete"
    if ($LASTEXITCODE -ne 0) { throw "Échec SQL : $requete`n$sortie" }
    # La virgule empêche PowerShell de « dérouler » un résultat d'une seule ligne en chaîne
    # (sans elle, [0] renverrait le premier caractère au lieu de la première ligne).
    return ,@($sortie | Where-Object { $_ -and $_.Trim() } | ForEach-Object { $_.Trim() })
}

function Sauvegarder {
    $horodatage = Get-Date -Format "yyyyMMdd-HHmm"
    $bak = Join-Path $DossierSauvegarde "$Base-$horodatage.bak"
    Write-Host "-> Sauvegarde de la base $Base vers $bak"
    Sql "BACKUP DATABASE [$Base] TO DISK = N'$bak' WITH COPY_ONLY, CHECKSUM, INIT;" | Out-Null
    if ($DossierPieces) {
        New-Item -ItemType Directory -Force $DossierArchive | Out-Null
        $zip = Join-Path $DossierArchive "$Base-pieces-$horodatage.zip"
        Write-Host "-> Archive des pièces ($DossierPieces) vers $zip"
        Compress-Archive -Path (Join-Path $DossierPieces "*") -DestinationPath $zip -Force
    }
    Write-Host "OK Sauvegarde $horodatage terminée"
}

function Tester {
    # Dernière sauvegarde complète d'après l'historique officiel de SQL Server (msdb).
    $chemin = (Sql "SELECT TOP 1 mf.physical_device_name FROM msdb.dbo.backupset bs JOIN msdb.dbo.backupmediafamily mf ON mf.media_set_id = bs.media_set_id WHERE bs.database_name = N'$Base' AND bs.type = 'D' AND mf.physical_device_name LIKE N'%\$Base-[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9].bak' ORDER BY bs.backup_finish_date DESC;")[0]
    if (-not $chemin) { throw "Aucune sauvegarde complète de $Base dans l'historique msdb" }
    $bak = [pscustomobject]@{ FullName = $chemin; Name = [IO.Path]::GetFileName($chemin); BaseName = [IO.Path]::GetFileNameWithoutExtension($chemin) }
    $horodatage = $bak.BaseName.Substring($Base.Length + 1)
    $test = "${Base}_TestRestauration"
    Write-Host "-> Vérification d'intégrité de $($bak.Name)"
    Sql "RESTORE VERIFYONLY FROM DISK = N'$($bak.FullName)' WITH CHECKSUM;" | Out-Null

    $deplacements = foreach ($ligne in (Sql "RESTORE FILELISTONLY FROM DISK = N'$($bak.FullName)';")) {
        $colonnes = $ligne -split "\|"
        $cible = Join-Path (Split-Path $colonnes[1]) ("${test}_" + (Split-Path $colonnes[1] -Leaf))
        "MOVE N'$($colonnes[0])' TO N'$cible'"
    }
    Write-Host "-> Restauration dans la base distincte $test"
    Sql "IF DB_ID(N'$test') IS NOT NULL BEGIN ALTER DATABASE [$test] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$test]; END; RESTORE DATABASE [$test] FROM DISK = N'$($bak.FullName)' WITH $($deplacements -join ', '), RECOVERY, CHECKSUM;" | Out-Null

    $erreurs = 0
    try {
        Write-Host "-> Contrôles de cohérence (restauré = état à l'instant de la sauvegarde, donc <= source)"
        foreach ($table in "Tickets", "Dossiers", "Documents", "HistoriqueTickets", "JournalAudit", "AspNetUsers") {
            $restaure = [int](Sql "SELECT COUNT(*) FROM [$table];" $test)[0]
            $source = [int](Sql "SELECT COUNT(*) FROM [$table];" $Base)[0]
            if ($restaure -gt 0 -and $restaure -le $source) { $etat = "OK" } else { $etat = "ECART"; $erreurs++ }
            "   {0,-20} restauré {1,7}   source {2,7}   {3}" -f $table, $restaure, $source, $etat | Write-Host
        }
        Write-Host ("   Dernière entrée d'audit restaurée : {0} UTC" -f (Sql "SELECT CONVERT(varchar(19), MAX(HorodatageUtc), 120) FROM JournalAudit;" $test)[0])
        Write-Host ("   Migrations présentes : {0}" -f (Sql "SELECT COUNT(*) FROM __EFMigrationsHistory;" $test)[0])

        $zip = Join-Path $DossierArchive "$Base-pieces-$horodatage.zip"
        if (Test-Path $zip) {
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            $archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
            $entrees = @{}
            foreach ($e in $archive.Entries) { $entrees[$e.FullName.Replace('\', '/')] = $true }
            $archive.Dispose()
            $cles = Sql "SELECT TOP ($EchantillonPieces) CleStockage FROM Documents ORDER BY NEWID();" $test
            $manquantes = @($cles | Where-Object { -not $entrees.ContainsKey($_.Replace('\', '/')) })
            if ($manquantes.Count) { $erreurs++ }
            Write-Host ("   Pièces : {0}/{1} de l'échantillon présentes dans l'archive" -f ($cles.Count - $manquantes.Count), $cles.Count)
        }
        else {
            Write-Host "   (aucune archive de pièces datée $horodatage : contrôle des pièces non effectué)"
        }
    }
    finally {
        Sql "ALTER DATABASE [$test] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$test];" | Out-Null
        Write-Host "-> Base de test supprimée"
    }

    if ($erreurs) { Write-Host "ECHEC Restauration NON vérifiée ($erreurs écart(s))"; exit 1 }
    Write-Host "OK Restauration vérifiée : sauvegarde $horodatage exploitable"
}

if ($Mode -in "Sauvegarder", "Les deux") { Sauvegarder }
if ($Mode -in "Tester", "Les deux") { Tester }
