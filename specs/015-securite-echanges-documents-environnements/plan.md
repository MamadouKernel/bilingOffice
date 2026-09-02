# PLAN TECHNIQUE D'IMPLÉMENTATION DURCI — FEATURE 015

## Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement

---

### 1. OBJECTIF TECHNIQUE & CADRE D'EXÉCUTION

- **Feature** : 015 — Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement
- **Date** : 2026-09-02
- **Baseline CDC d'entrée** : **169 / 188 exigences conformes (89,89 %)**
- **Baseline technique** : **914 / 914 tests verts (0 warning, 0 pending model change)**
- **Périmètre contractuel** :
  - **`SEC-04` (Must)** : Chiffrement des échanges (HTTPS/TLS) et protection des documents stockés
  - **`SEC-07` (Must)** : Contrôle des fichiers déposés : types autorisés, taille maximale, analyse antivirale réelle
  - **`SEC-09` (Should)** : Cloisonnement des environnements (production, recette) et gestion des habilitations
- **Gain visé** : **+3 exigences (2 Must, 1 Should)**
- **Projection CDC** : **172 / 188 (91,49 %)** *(non acquise avant Closure Review)*
- **Impact Schéma Relationnel** : **0 migration EF Core**, **0 nouvelle table**, **0 nouvelle entité**.

---

### 2. STRATÉGIE DURCIE SEC-07 — PREUVE MOTEUR ANTIVIRUS RÉEL

#### 2.1 Règle Fondamentale d'Éligibilité (Condition de Closure)
> [!IMPORTANT]
> **SEC-07 ne peut pas être certifiée ni apporter +1 à la couverture CDC si aucun test d'intégration exécutant un moteur antivirus réel n'est PASS.**
> Un composant maison qui inspecte des expressions régulières ou une signature EICAR codée en dur **NE CONSTITUE PAS** un moteur antivirus et est **STRICTEMENT INTERDIT** comme preuve de conformité.

#### 2.2 Moteur Réel Retenu & Architecture de l'Adaptateur
- **Moteur Réel Primaire** : **Microsoft Defender CLI (`MpCmdRun.exe`)** sur environnement Windows / Windows Server, et adaptabilité **ClamAV CLI/Daemon (`clamscan` / `clamd`)** sur environnement Linux/Docker.
- **Rôle de `ScannerAntivirusLocal`** :
  - Il s'agit exclusivement d'un **ADAPTATEUR D'INFRASTRUCTURE** (`IAnalyseAntivirus`), et en aucun cas d'un moteur antivirus fait maison.
  - Il écrit temporairement le flux dans un espace privé sécurisé, invoque le processus du moteur antivirus réel du système hôte (`MpCmdRun.exe -Scan -ScanType 3 -File "<path>"` ou `clamscan`), lit le code de sortie officiel du processus, détruit immédiatement le fichier temporaire, et mappe le résultat vers `StatutAnalyseAntivirus`.
- **Codes de retour réels Microsoft Defender** :
  - Code `0` : **Sain** (Aucune menace détectée).
  - Code `2` : **Infecté / Menace Détectée** (Fichier dangereux identifié).
  - Tout autre code (`1`, timeout, exécutable absent, code inconnu) : **ErreurAnalyse** -> **FAIL-CLOSED** (Rejet immédiat du fichier).

#### 2.3 Matrice des Niveaux de Preuve Antivirale
1. **Niveau 1 — Tests d'Orchestration (Mock / Test Double)** :
   - Valide l'enchaînement des étapes : `DeposerDocumentCommand` réagit correctement à `Sain`, `Infecte`, et `ErreurAnalyse`.
   - Ne certifie PAS le moteur réel.
2. **Niveau 2 — Tests d'Intégration Moteur Réel (Obligatoire pour Closure)** :
   - Exécution réelle du moteur OS (`MpCmdRun.exe` / `clamscan`) sur :
     - 1. Un fichier sain réel (résultat attendu : `StatutAnalyseAntivirus.Sain`, code sortie 0).
     - 2. Le fichier standard de test EICAR réel (résultat attendu : `StatutAnalyseAntivirus.Infecte`, code sortie 2).
     - 3. Une condition d'erreur / chemin invalide (résultat attendu : `StatutAnalyseAntivirus.Infecte` / Fail-Closed, rejet du dépôt).

---

### 3. CONCEPTION DÉTAILLÉE DES COMPOSANTS (SEC-04, SEC-07, SEC-09)

#### 3.1 Module SEC-04 : Transport Sécurisé & Protection Documentaire GED
- `SecurityHeadersMiddleware.cs` : Middleware injectant `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy: strict-origin-when-cross-origin`, et Content-Security-Policy Blazor-compatible (`frame-ancestors 'self'`).
- `Program.cs` : Activation de `UseHttpsRedirection()` et `UseHsts()`.
- `StockageDocumentsFichiers.cs` : Stockage hermétique hors `wwwroot`, renommage opaque GUID, calcul d'empreinte SHA-256 à l'ingestion sans altération de la base SQL.

#### 3.2 Module SEC-07 : Pipeline d'Admission & Analyse Antivirale
- `ValidateurFichierDocument.cs` : Double contrôle d'admission (Extensions autorisées + Magic Bytes d'en-tête : PDF `%PDF-`, PNG `\x89PNG`, JPEG `\xFF\xD8\xFF`, XLSX `PK\x03\x04`).
- `ScannerAntivirusDefender.cs` : Adaptateur invoquant le moteur antivirus réel `MpCmdRun.exe`.
- `DeposerDocumentCommand.cs` : Enchaînement strict : Validation type/taille -> Magic Bytes -> Scanner Antivirus Réel -> Stockage final uniquement si `Sain`.

#### 3.3 Module SEC-09 : Cloisonnement d'Environnement & Secrets
- `Program.cs` : Conditionnement strict `if (!app.Environment.IsProduction())` pour `DevelopmentSeeder` et Swagger.
- `appsettings.Staging.json` & `appsettings.Production.json` : Ségrégation des profils et interdiction des secrets de production dans Git.

---

### 4. SUITE CONTRACTUELLE TEST-SPEC (11 SCÉNARIOS)

| Test-Spec | Scénario | Objectif & Preuve | Projet de Test | Type de Preuve |
|:---|:---|:---|:---|:---:|
| **`SEC-CERT-04-01`** | HTTPS & HSTS | Redirection HTTPS et présence HSTS | `Web.Tests` | Applicative |
| **`SEC-CERT-04-02`** | Security Headers | Présence headers nosniff, SAMEORIGIN, Referrer-Policy | `Web.Tests` | Applicative |
| **`SEC-CERT-04-03`** | Isolation GED | Rejet accès direct URL hors wwwroot | `Infrastructure.Tests` | Applicative |
| **`SEC-CERT-07-01`** | Format valide | Admission PDF/JPEG conforme | `Application.Tests` | Orchestration |
| **`SEC-CERT-07-02`** | Rejet Magic Bytes | Rejet .exe maquillé en .pdf | `Application.Tests` | Orchestration |
| **`SEC-CERT-07-03`** | Plafond taille | Rejet si > 10 Mo | `Application.Tests` | Orchestration |
| **`SEC-CERT-07-04`** | Fichier sain réel | Scan fichier sain par vrai moteur `MpCmdRun.exe` (Exit Code 0) | `Infrastructure.Tests` | **MOTEUR RÉEL** |
| **`SEC-CERT-07-05`** | Menace EICAR réelle | Scan fichier EICAR par vrai moteur `MpCmdRun.exe` (Exit Code 2) | `Infrastructure.Tests` | **MOTEUR RÉEL** |
| **`SEC-CERT-07-06`** | Scanner Fail-Closed | Rejet automatique si moteur indisponible/erreur | `Infrastructure.Tests` | **MOTEUR RÉEL** |
| **`SEC-CERT-09-01`** | Gardes Production | `DevelopmentSeeder` inactif en mode `Production` | `Web.Tests` | Environnement |
| **`SEC-CERT-09-02`** | 0 secret versionné | Scan statique des configs versionnées sans secrets | `Web.Tests` | Environnement |

---

### 5. CONSTITUTION CHECK F015 (17/17 PASS)

1. Périmètre strict à SEC-04, SEC-07, SEC-09 : **PASS**
2. Zéro migration EF Core : **PASS**
3. Zéro table SQL : **PASS**
4. Zéro entité Domaine : **PASS**
5. Aucune donnée métier modifiée : **PASS**
6. Contrats d'API NFR-08 100% préservés : **PASS**
7. Authentification et 2FA préservés : **PASS**
8. Notifications F009/F010 hors scope : **PASS**
9. Zéro dépendance cloud SaaS externe : **PASS**
10. Analyse antivirus réelle avec moteur OS obligatoire : **PASS**
11. Preuve EICAR sur moteur réel obligatoire : **PASS**
12. Politique de sécurité Fail-Closed garantie : **PASS**
13. Double contrôle d'admission (Extensions + Magic Bytes) : **PASS**
14. Isolation physique GED hors `wwwroot` : **PASS**
15. Cloisonnement profils Staging / Production étanche : **PASS**
16. `DevelopmentSeeder` bridé en Production : **PASS**
17. Anti-scope respecté (SEC-02, SEC-06, ENR-04, IA, NFR-03..10 exclus) : **PASS**
