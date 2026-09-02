# Précontrôles Staging

Vérifie qu'un environnement réunit ce que la plateforme exige, **avant** tout déploiement.

## Nature du script

**Lecture seule**, à une exception documentée : le contrôle d'écriture crée un fichier témoin dans
le répertoire de stockage et **le supprime immédiatement**. Aucune autre écriture, aucune
modification de configuration, aucun accès à la base au-delà d'un test de port.

**Aucun secret n'est contenu dans ce script.** La chaîne de connexion est lue depuis
l'environnement pour vérifier sa seule *présence* — sa valeur n'est jamais affichée ni journalisée.

## Usage

```powershell
./precheck.ps1 -BaseUrl https://<staging> -RepertoireDocuments <chemin> -ServeurSql <instance>
```

Tous les paramètres sont facultatifs : un paramètre absent fait ignorer les contrôles associés,
signalés comme « à voir ».

| Paramètre | Rôle |
|:---|:---|
| `-BaseUrl` | Sondes applicatives, TLS, `Server-Timing` |
| `-RepertoireDocuments` | Existence, droits d'écriture, espace disque |
| `-ServeurSql` | Joignabilité du serveur SQL |
| `-EspaceDisqueMinimumGo` | Seuil d'alerte, défaut 10 Go |

## Ce qui est contrôlé

| # | Contrôle | Bloquant |
|---:|:---|:---|
| 1 | Runtime .NET présent, ASP.NET Core **10** installé | **Oui** |
| 2 | Répertoire de stockage : existence, écriture, espace | **Oui** |
| 3 | Serveur SQL joignable sur 1433 | **Oui** |
| 4 | Microsoft Defender CLI présent | À voir |
| 5 | `ASPNETCORE_ENVIRONMENT` positionné, **et différent de `Development`** | **Oui** |
| 6 | Chaîne de connexion fournie — présence seule | À voir |
| 7 | `Notifications:Reessai` fourni — présence seule | À voir |
| 8 | `/healthz`, `/ready`, TLS, `Server-Timing` | À voir |

## Trois contrôles qui méritent une explication

**`ASPNETCORE_ENVIRONMENT` ne doit pas valoir `Development`.** Dans ce mode, l'application exécute
le jeu de données de test et désactive HSTS. Un Staging ainsi configuré serait à la fois peuplé de
comptes fictifs et dégradé en sécurité. Le script traite ce cas comme un **échec**, pas comme un
avertissement.

**Le répertoire de stockage doit être fourni.** Sans la clé `Stockage:RepertoireDocuments`,
l'application écrit les documents dans le répertoire temporaire du système, qui est purgé. La
panne est différée et silencieuse : le dépôt réussit, la référence est enregistrée, le fichier
disparaît plus tard.

**`Notifications:Reessai` empêche le démarrage s'il manque.** Ces deux valeurs ne sont déclarées que
dans la configuration de développement : le CDC (ERR-02) exige une nouvelle tentative d'envoi sans
en fixer ni le nombre ni la cadence, et le code refuse d'inventer une valeur métier. En leur
absence, l'application échoue au lancement avec un message nommant la clé.

Le contrôle est classé « à voir » et non « échec », parce que ces valeurs peuvent légitimement
provenir d'un fichier `appsettings` propre à l'environnement plutôt que d'une variable — le script
ne lit pas les fichiers de configuration de l'application.

## Code de sortie

| Valeur | Signification |
|---:|:---|
| `0` | Aucun échec — le déploiement peut être envisagé |
| `1` | Au moins un échec — **ne pas déployer en l'état** |

Les points « à voir » ne bloquent pas mais doivent être examinés : ils correspondent soit à un
paramètre non fourni, soit à un contrôle non concluant.

## Portabilité

Écrit en PowerShell, cohérent avec l'option de déploiement Windows / IIS (`docs/58`). Si l'option
conteneur est retenue, les contrôles restent valables mais devront être transposés.
