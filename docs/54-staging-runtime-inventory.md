# Inventaire d'exécution pour Staging

> **Document** : `docs/54-staging-runtime-inventory.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Méthode** : relevé **dans le code**, pas dans une documentation dérivée. Chaque ligne renvoie
> au fichier qui l'impose.

---

## ⚠ Point d'attention prioritaire

**`Stockage:RepertoireDocuments` n'est déclaré dans aucun `appsettings.json`.**

```csharp
configuration["Stockage:RepertoireDocuments"]
    ?? Path.Combine(Path.GetTempPath(), "cit-plateforme-documents")
```
*(`Infrastructure/Documents/StockageDocumentsFichiers.cs:14`)*

Non renseignée, l'application écrit **les documents déposés dans le répertoire temporaire du
système**. Sur un serveur, ce répertoire est purgé — par le système, par une tâche de ménage, ou
au redémarrage selon la configuration.

Les conséquences ne sont pas immédiatement visibles : le dépôt réussit, la base enregistre la
référence, et les fichiers disparaissent plus tard. On obtient des dossiers pointant vers des
pièces absentes.

**Cette clé doit être renseignée explicitement avant tout déploiement.** Elle figure en tête de la
matrice de configuration (`docs/55`).

---

## 1. Runtime

| Élément | Valeur | Source |
|:---|:---|:---|
| Framework cible | **.NET 10** (`net10.0`) | Les quatre `.csproj` |
| Modèle d'hébergement | ASP.NET Core — Blazor Server | `Program.cs` |
| Système d'exploitation | **Indifférent** — .NET 10 est multiplateforme | — |

**Aucune dépendance native au système**, à une exception : l'antivirus (§ 5).

### Paquets NuGet — surface de dépendance complète

| Paquet | Version |
|:---|:---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.11 |
| `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.11 |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.11 |
| `Microsoft.AspNetCore.OpenApi` | 10.0.11 |
| `QRCoder` | 1.8.0 |

**Cinq paquets seulement**, tous Microsoft à l'exception de `QRCoder`, utilisé pour générer
localement le QR code d'enrôlement 2FA. Aucune dépendance SaaS, aucun appel réseau sortant vers un
service tiers.

---

## 2. Base de données

| Élément | Exigence | Source |
|:---|:---|:---|
| Moteur | **SQL Server** | `UseSqlServer` — `DependencyInjection.cs` |
| Version | Compatible EF Core 10 | Paquet `EntityFrameworkCore.SqlServer` 10.0.11 |
| Chaîne de connexion | `ConnectionStrings:PlateformeDb` | `appsettings.json` — **livrée vide** |
| Migrations | 25, appliquées **hors démarrage** | `docs/57` |

### Permissions requises

| Phase | Droits |
|:---|:---|
| Exploitation courante | Lecture / écriture sur la base |
| Migration | Droits de modification de schéma, **temporairement** |

**Le compte applicatif ne doit pas être un compte d'administration d'instance.** Un compte limité
à cette seule base réduit la portée d'une injection ou d'une fuite de configuration. Les droits de
schéma s'accordent le temps de la migration, puis se retirent.

---

## 3. Système de fichiers — stockage documentaire

| Élément | Valeur |
|:---|:---|
| Clé de configuration | `Stockage:RepertoireDocuments` — **non déclarée**, voir l'avertissement |
| Repli si absente | `%TEMP%/cit-plateforme-documents` — **inadapté à un serveur** |
| Droits requis | Lecture, écriture, création de répertoire, par le compte de service |
| Capacité | Fonction du volume documentaire — `TBD_DSI` (PC-01) |
| Nature | Système de fichiers local ou monté ; aucun stockage objet n'est requis |

**Contrainte de cohérence.** La base contient les références, le système de fichiers contient les
octets. Sauvegarde et restauration doivent porter sur **les deux, dans la même fenêtre** — sans
quoi la restauration produit un système incohérent.

---

## 4. Réseau

### Flux entrants

| Port | Protocole | Usage | Requis |
|:---|:---|:---|:---|
| 443 | HTTPS | Accès applicatif | **Oui** |
| 80 | HTTP | Redirection vers HTTPS | Recommandé |

`UseHttpsRedirection` est actif. Derrière un proxy terminant TLS, **les en-têtes de transfert
doivent être propagés**, faute de quoi l'application redirige en boucle.

### Flux sortants

| Destination | Port | Usage | Requis |
|:---|:---|:---|:---|
| SQL Server | 1433 par défaut | Persistance | **Oui** |
| Système de fichiers | — | Stockage documentaire | **Oui** |
| Antivirus local | — | Analyse des dépôts | **Oui** |
| SMTP | — | **Aucun** — aucun fournisseur d'e-mail n'est configuré | Non |
| API externes | — | **Aucune** | Non |

**L'application n'a besoin d'aucun accès Internet sortant.** C'est une propriété assumée : aucune
ressource externe n'est chargée, et un test automatisé le vérifie à chaque exécution.

---

## 5. Antivirus

| Élément | Valeur | Source |
|:---|:---|:---|
| Adaptateur | Microsoft Defender CLI (`MpCmdRun.exe`) | `ScannerAntivirusDefender.cs` |
| Chemin de l'exécutable | `Securite:Antivirus:CheminExecutable` | Configurable |
| Mode | `Securite:Antivirus:Mode` — `"Defender"` | `appsettings.json` |
| Comportement en échec | `Securite:Antivirus:FailClosed` — **`true`** | `appsettings.json` |
| Délai maximal | `Securite:Antivirus:TimeoutSecondes` — 15 | `appsettings.json` |

**C'est la seule dépendance liée au système d'exploitation.** L'adaptateur cible Defender, donc
Windows. Sur une cible Linux, cette dépendance devra être remplacée — l'abstraction
`IAnalyseAntivirus` le permet sans toucher au reste.

`FailClosed = true` signifie qu'**un antivirus indisponible bloque le dépôt** plutôt que de le
laisser passer non analysé. C'est le comportement voulu ; il implique que l'indisponibilité de
l'antivirus dégrade la plateforme.

---

## 6. Sécurité

| Élément | État | Source |
|:---|:---|:---|
| HTTPS | Redirection active | `UseHttpsRedirection` |
| HSTS | Actif hors développement | `UseHsts` |
| En-têtes de sécurité | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, CSP | `SecurityHeadersMiddleware` |
| Antiforgery | Actif | `UseAntiforgery` |
| Authentification | Cookie ASP.NET Core Identity | `Program.cs` |
| Expiration de session | `Securite:ExpirationSessionMinutes` — repli 30 min, glissante | `Program.cs` |
| Second facteur | TOTP, **optionnel** par utilisateur | — |
| Mot de passe | 10 caractères minimum | `DependencyInjection.cs` |
| Verrouillage | 5 tentatives, 15 minutes | `DependencyInjection.cs` |

### Secrets attendus

| Clé | Nature |
|:---|:---|
| `ConnectionStrings:PlateformeDb` | **Secret** |
| `Administration:AdministrateurInitial:MotDePasse` | **Secret** |

**Aucun secret ne figure dans le dépôt.** Le mécanisme de fourniture en Staging est à arrêter —
`TBD_DSI`.

### Amorçage du premier administrateur

`Administration:AdministrateurInitial:Email` et `:MotDePasse`. **Sans ces clés, aucun compte ne
peut administrer les comptes agents** au premier démarrage. Le mécanisme est inerte dès qu'un
administrateur existe.

---

## 7. Traitements de fond

Cinq services hébergés, démarrés avec l'application :

| Service | Objet |
|:---|:---|
| `RelancesAbsenceReponseService` | Relances en cas d'absence de réponse client |
| `SurveillanceSlaService` | Surveillance des échéances et alertes |
| `DistributionAutomatiqueService` | Affectation automatique des tickets |
| `DistributionNotificationsService` | Distribution des notifications |
| `ReessaiEnvoisNotificationService` | Réessai des envois en échec |

**Implication d'exploitation.** Ces services écrivent en base. Une intervention sur la base
suppose de vérifier qu'ils sont au repos — le mode maintenance **ne les interrompt pas**.

**Implication de mise à l'échelle.** Plusieurs instances de l'application exécuteraient chacune
ces traitements. La conception protège des doublons (prise atomique, index unique), mais ce point
mérite d'être vérifié avant toute décision de répartition de charge.

---

## 8. Observabilité

| Point | Chemin | Ce qu'il atteste |
|:---|:---|:---|
| Vivacité | `GET /healthz` | Le processus répond |
| Disponibilité | `GET /ready` | L'application **et sa base** sont prêtes |
| Temps applicatif | En-tête `Server-Timing` | Durée de traitement serveur |
| Journaux | Sortie standard | Niveau via `Logging:LogLevel` |
| Journal d'audit | Base de données | Actions sensibles, en ajout seul |

**Superviser `/healthz` seul est insuffisant** : il peut répondre `200` alors que la base est
inatteignable. C'est `/ready` qui atteste la capacité à servir.

---

## 9. Récapitulatif des dépendances

| Dépendance | Requise | Bloquante au démarrage |
|:---|:---|:---|
| Runtime .NET 10 | Oui | **Oui** |
| SQL Server accessible | Oui | **Oui** — `/ready` échoue sinon |
| Répertoire de stockage accessible en écriture | Oui | Non — échoue au premier dépôt |
| Antivirus | Oui | Non — bloque les dépôts (`FailClosed`) |
| Certificat TLS | Oui | Selon l'hébergement |
| Accès Internet sortant | **Non** | Non |
| SMTP | **Non** | Non |
