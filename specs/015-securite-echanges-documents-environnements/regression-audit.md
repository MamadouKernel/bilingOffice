# RAPPORT D'AUDIT DE NON-RÉGRESSION (REGRESSION GATE) — FEATURE 015

## Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement

---

### 1. BASELINE TECHNIQUE & INTÉGRITÉ DU SYSTÈME

- **Compilation** : `dotnet build /p:UseSharedCompilation=false` -> **0 Erreur, 0 Avertissement**
- **Pending EF Model Changes** : `dotnet ef migrations has-pending-model-changes` -> **0 pending model changes (0 migration)**
- **Suite de tests automatisés** : `dotnet test` (sérialisé) -> **945 / 945 tests verts (100 %)**
  - `CIT.Plateforme.Domain.Tests` : 158 / 158
  - `CIT.Plateforme.Application.Tests` : 493 / 493 (+6 tests F015)
  - `CIT.Plateforme.Infrastructure.Tests` : 94 / 94 (+5 tests F015)
  - `CIT.Plateforme.Web.Tests` : 200 / 200 (+4 tests F015)
- **Régressions constatées** : **0**

---

### 2. AUDIT FORENSIQUE DES PREUVES PAR EXIGENCE

#### 2.1 SEC-04 (Must) — Transport Sécurisé & Protection GED : CONFORME
- **HTTPS & HSTS** : Redirection HTTPS active et HSTS conditionné aux profils non-développement (`SecCert04TransportTests.SecCert04_01`).
- **Security Headers** : `SecurityHeadersMiddleware` actif avec `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Referrer-Policy: strict-origin-when-cross-origin` et `Content-Security-Policy` Blazor-compatible (`SecCert04TransportTests.SecCert04_02`).
- **Protection GED** : Stockage hermétique hors `wwwroot`, clé anonymisée GUID, protection anti-path-traversal (`StockageSecuriseTests.SecCert04_03`).
- **Intégrité SHA-256** : Intégrité garantie sans migration relationnelle opportuniste.

#### 2.2 SEC-07 (Must) — Contrôle des Fichiers & Analyse Antivirale Réelle : CONFORME
- **Admission des fichiers** : Validateur centralisé `ValidateurFichierDocument` (PDF, JPEG, PNG, XLSX, plafond 10 Mo) (`ControleFichiersTests.SecCert07_01..03`).
- **Magic Bytes** : Inspection binaire d'en-tête bloquant les faux PDF / exécutables maquillés (`ControleFichiersTests.SecCert07_02`).
- **Adaptateur Antivirus Réel** : `ScannerAntivirusDefender` invoquant le CLI système `MpCmdRun.exe`. Zéro regex / signature maison.
- **Preuve Moteur Réel Microsoft Defender** :
  - Scan de fichier sain réel validé avec code retour `0` (`ScannerAntivirusIntegrationTests.SecCert07_04`).
  - Scan de la signature standard EICAR réelle validé avec détection et code retour `2` (`ScannerAntivirusIntegrationTests.SecCert07_05`).
  - Politique **Fail-Closed** validée : tout chemin introuvable ou erreur de scan entraîne le refus immédiat du fichier (`ScannerAntivirusIntegrationTests.SecCert07_06`).
  - Nettoyage garanti des fichiers temporaires privés dans le bloc `finally`.

#### 2.3 SEC-09 (Should) — Cloisonnement des Environnements & Secrets : CONFORME
- **Profils d'environnement** : `appsettings.Staging.json` et `appsettings.Production.json` étanches (`SecCert09EnvironnementTests.SecCert09_01`).
- **Zéro secret dans Git** : Aucun mot de passe ni secret de production en clair versionné (`SecCert09EnvironnementTests.SecCert09_02`).
- **Garde-fous des seeders** : `DevelopmentSeeder` (comptes démo) formellement désactivé en mode `Production`.

---

### 3. MATRICE DE TRAÇABILITÉ CONTRATS & TEST-SPEC

| Identifiant Contrat / Test-Spec | Exigence | Statut Audit | Preuve d'Exécution |
|:---|:---:|:---:|:---|
| **`C15-01` / `SEC-CERT-04-01..02`** | `SEC-04` | ✅ PASS | Headers HTTP & HSTS vérifiés |
| **`C15-02` / `SEC-CERT-04-03`** | `SEC-04` | ✅ PASS | GED isolée hors wwwroot & Anti-Traversal |
| **`C15-03` / `SEC-CERT-07-01..03`** | `SEC-07` | ✅ PASS | Validation MIME, taille et Magic Bytes |
| **`C15-04` / `SEC-CERT-07-04..06`** | `SEC-07` | ✅ PASS | Moteur réel Defender, EICAR & Fail-Closed |
| **`C15-05` / `SEC-CERT-09-01..02`** | `SEC-09` | ✅ PASS | Profils Staging/Prod et 0 secret versionné |

---

### 4. ANTI-SCOPE & IMPACTS DE SCHÉMA

- **`SEC-02` (PC-05)** : Non touché (différé à F018).
- **`SEC-06` (PC-14)** : Non touché (différé à F018).
- **`ENR-04` (PC-16)** : Non touché (différé à F018).
- **`IA-01..07` (PC-18)** : Non touché (différé à F017).
- **`NFR-03..10`** : Non touché.
- **Nouvelles tables / entités / colonnes SQL** : **0**.

---

### 5. PROJECTION CDC

- **Baseline CDC avant F015** : **169 / 188 (89,89 %)**
- **Gain potentiel audité** : **+3** (`SEC-04`, `SEC-07`, `SEC-09`)
- **Projection cible** : **172 / 188 (91,49 %)** *(non acquise avant Closure Review)*
- **Statut Regression Gate** : **PASS**
