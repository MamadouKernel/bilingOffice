# SPÉCIFICATION FONCTIONNELLE & DÉCISIONS CLARIFIÉES — FEATURE 015

## Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement

---

### 1. CONTEXTE MÉTIER & EXIGENCES CDC CONTRACTUELLES

- **Feature** : 015 — Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement
- **Date** : 2026-09-02
- **Baseline CDC d'entrée** : **169 / 188 exigences conformes (89,89 %)**
- **Périmètre fonctionnel** :
  - **`SEC-04` (Must)** : *« Chiffrement des échanges (HTTPS/TLS) et protection des documents stockés »* (§18 du CDC).
  - **`SEC-07` (Must)** : *« Contrôle des fichiers déposés : types autorisés, taille maximale, analyse antivirale »* (§18 du CDC).
  - **`SEC-09` (Should)** : *« Cloisonnement des environnements (production, recette) et gestion des habilitations par environnement »* (§18 du CDC).
- **Gain maximal visé** : **+3 exigences (2 Must, 1 Should)**
- **Projection cible** : **172 / 188 (91,49 %)** *(non acquise avant Closure Review)*

---

### 2. DÉCISIONS DE CLARIFICATION FONCTIONNELLE (CLAR-SEC-015)

| ID | Sujet | Question / Ambiguïté | Décision fonctionnelle validée | Type de décision |
|:---|:---|:---|:---|:---:|
| **DEC-015-01** | TLS & Périmètre applicatif (`SEC-04`) | Quelle est la frontière exacte entre l'application et l'infrastructure pour HTTPS/TLS ? | L'application enforce HTTPS, active HSTS et configure les headers stricts ; la terminaison TLS et la gestion des certificats relèvent du reverse proxy / hébergeur. | CDC / Architecture |
| **DEC-015-02** | Headers de sécurité (`SEC-04`) | Quels headers de sécurité sont intégrés sans casser l'interop Blazor ? | `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, `Strict-Transport-Security`, `Referrer-Policy: strict-origin-when-cross-origin`. CSP progressive adaptée à Blazor. | Décision Produit |
| **DEC-015-03** | Protection documentaire (`SEC-04`) | Comment garantir la protection des documents stockés sans altérer la GED ? | Stockage hermétique hors `wwwroot`, renommage opaque GUID, accès via contrôleur habilité (`SEC-12`) et vérification d'intégrité par empreinte SHA-256. | CDC / Produit |
| **DEC-015-04** | Types autorisés (`SEC-07`) | Les formats de fichiers sont-ils figés ou paramétrables ? | Liste blanche par défaut (PDF, JPEG, PNG, XLSX) configurable par `appsettings.json` sans recompilation. | Décision Produit |
| **DEC-015-05** | Validation Magic Bytes (`SEC-07`) | Comment contrer le masquage d'extensions ? | Double validation : extension déclarée + inspection des signatures binaires (Magic Bytes) d'en-tête côté serveur. | Durcissement Technique |
| **DEC-015-06** | Volumétrie maximale (`SEC-07`) | Quel plafond de taille appliquer ? | Plafond paramétrable fixé par défaut à 10 Mo par pièce jointe, rejet explicite avec message d'erreur localisé. | Décision Produit |
| **DEC-015-07** | Définition d'analyse antivirale réelle (`SEC-07`) | Qu'est-ce qu'une analyse antivirale réelle ? | Inspection effective du flux binaire avec détection de signatures (ex. EICAR), interdiction formelle d'un stub retournant "Sain" sans lecture de flux. | CDC / Invariant |
| **DEC-015-08** | Politique Fail-Closed (`SEC-07`) | Quel comportement si le scanner antivirus est indisponible ? | **Rejet par défaut (Fail-Closed)** : le fichier est refusé avec statut d'erreur d'analyse, interdisant toute acceptation silencieuse. | Décision Sécurité |
| **DEC-015-09** | Flux de scan synchrone (`SEC-07`) | Le scan est-il synchrone ou asynchrone ? | **Synchrone à l'upload** : le fichier n'atteint le stockage protégé que si et seulement si l'analyse retourne `Sain`. Zéro fichier infecté ou non analysé stocké en GED. | Architecture |
| **DEC-015-10** | Traitement des fichiers malveillants (`SEC-07`) | Que devient un fichier détecté dangereux ? | Rejet immédiat, destruction du flux temporaire, non-enregistrement en GED, et journalisation d'un événement d'audit de sécurité sans loguer le contenu binaire. | Décision Sécurité |
| **DEC-015-11** | Agnosticisme du scanner (`SEC-07`) | Quel moteur antivirus utiliser ? | Abstraction `IScannerAntivirus` avec implémentation de scanner local (moteur de signatures locales et détection de motifs malveillants + adaptateur de service local ClamAV/Defender). 0 dépendance cloud SaaS. | Architecture |
| **DEC-015-12** | Environnements contractuels (`SEC-09`) | Quels environnements sont concernés par le cloisonnement ? | `Staging` (Recette) et `Production` (avec profil `Development` pour le poste de travail). | CDC |
| **DEC-015-13** | Secrets de production (`SEC-09`) | Où sont stockés les secrets de production ? | Aucun secret de production dans Git ni dans `appsettings.json` versionné ; injection obligatoire par variables d'environnement en production. | Décision Sécurité |
| **DEC-015-14** | Garde-fous des seeders (`SEC-09`) | Quels seeders sont interdits en production ? | `DevelopmentSeeder` et comptes de démonstration strictement désactivés en profil `Production` ; seuls les référentiels métier indispensables sont initialisés. | Décision Sécurité |
| **DEC-015-15** | Diagnostics et Swagger (`SEC-09`) | Quel statut pour Swagger et DeveloperExceptionPage ? | Swagger et DeveloperExceptionPage strictement désactivés ou restreints en environnement `Production`. | Décision Produit |
| **DEC-015-16** | Habilitations d'environnement (`SEC-09`) | Que recouvre la gestion des habilitations par environnement ? | Comptes et rôles d'administration distincts par environnement, sans réutilisation de privilèges de recette en production. | CDC / Exploitation |
| **DEC-015-17** | Journalisation et données sensibles | Quelles données sont interdites dans les logs ? | Interdiction absolue de loguer les contenus binaires, clés de chiffrement, tokens et mots de passe. | Invariant Sécurité |
| **DEC-015-18** | Impact sur le schéma de données | Une migration EF Core est-elle requise ? | **NON (0 migration)** : les règles s'opèrent au niveau des middlewares, validateurs de domaine et adaptateurs de stockage. | Invariant |

---

### 3. PÉRIMÈTRE DÉTAILLÉ DES SURFACES & COMPOSANTS

#### 3.1 Surfaces applicatives et d'infrastructure
1. **Pipeline HTTP & Sécurité Transport (`SEC-04`)** :
   - Middleware de redirection HTTPS et activation HSTS (`UseHsts`, `UseHttpsRedirection`).
   - Middleware de headers de sécurité applicatifs (`UseSecurityHeaders`).
2. **Gestionnaire de Stockage Sécurisé & Intégrité GED (`SEC-04`)** :
   - Service `IStockageDocumentService` stockant hors `wwwroot` avec nommage sécurisé et empreinte SHA-256.
   - Endpoint de téléchargement sécurisé vérifiant systématiquement l'authentification et l'autorisation (`SEC-12`).
3. **Validateur de Fichiers & Scanner Antivirus (`SEC-07`)** :
   - Service `IValidateurFichierService` avec validation liste blanche + Magic Bytes.
   - Moteur `IScannerAntivirus` avec analyse effective du flux et détection de signature EICAR.
   - Intégration dans les pages de téléversement (Billing Import, Export, Enrôlement, Customer Complaint).
4. **Configuration & Gardes-Fous d'Environnement (`SEC-09`)** :
   - `Program.cs` avec détection stricte du profil d'environnement (`IsProduction()`, `IsStaging()`, `IsDevelopment()`).
   - `DevelopmentSeeder` conditionné à `!app.Environment.IsProduction()`.
   - Documentation et structure des profils `appsettings.*.json`.

---

### 4. USER STORIES CONTRACTUELLES

#### US1 (SEC-04) — Navigation sécurisée et protection contre l'interception
- **En tant qu'** usager accédant au portail CIT Digital Platform,
- **Je veux** que toutes mes communications transitent obligatoirement par un canal chiffré HTTPS avec des en-têtes défensifs,
- **Afin d'** empêcher l'interception, le rejeu ou l'altération de mes démarches portuaires.

#### US2 (SEC-04) — Confidentialité et intégrité des documents GED
- **En tant que** transitaire maritime téléversant des documents confidentiels,
- **Je veux** que mes pièces jointes soient isolées hors de la racine web publique et protégées par une empreinte d'intégrité,
- **Afin qu'** aucun utilisateur non autorisé ne puisse accéder directement à mes fichiers par URL.

#### US3 (SEC-07) — Contrôle d'admission et rejet des fichiers non conformes
- **En tant qu'** utilisateur soumettant un dossier,
- **Je veux** être immédiatement notifié si un fichier dépasse la taille maximale ou s'il s'agit d'un format interdit masqué sous une fausse extension,
- **Afin de** corriger mon dépôt avec un justificatif valide.

#### US4 (SEC-07) — Détection de menaces antivirales et politique Fail-Closed
- **En tant que** responsable de la sécurité CIT,
- **Je veux** que chaque flux de fichier soit réellement analysé par un moteur antivirus et rejeté en cas de menace ou d'indisponibilité du scanner,
- **Afin de** préserver l'intégrité du système d'information contre les virus et chevaux de Troie.

#### US5 (SEC-09) — Ségrégation hermétique des environnements et secrets
- **En tant qu'** administrateur de plateforme,
- **Je veux** que les environnements de recette et de production soient strictement cloisonnés, avec des secrets dédiés et la désactivation des seeders de test en production,
- **Afin d'** éviter toute fuite de données ou compromission d'environnement.

---

### 5. SUITE CONTRACTUELLE TEST-SPEC (11 SCÉNARIOS)

| Identifiant | Exigence | Précondition | Action / Déclencheur | Résultat attendu (PASS) | Résultat interdit (FAIL) |
|:---|:---:|:---|:---|:---|:---|
| **`SEC-CERT-04-01`** | `SEC-04` | Serveur en profil sécurisé | Requête HTTP vers le portail | Redirection vers HTTPS + Header HSTS présent | Requête servie en HTTP sans redirection |
| **`SEC-CERT-04-02`** | `SEC-04` | Application initialisée | Inspection des headers de réponse | Headers `nosniff`, `SAMEORIGIN`, `Referrer-Policy` présents | Absence des headers de protection |
| **`SEC-CERT-04-03`** | `SEC-04` | Document stocké avec SHA-256 | Tentative d'accès direct URL / Altération | Accès direct URL refusé (404/403) ; vérification intégrité | Fichier accessible publiquement |
| **`SEC-CERT-07-01`** | `SEC-07` | Formulaire upload | Téléversement d'un PDF valide (2 Mo) | Fichier validé et accepté | Rejet injustifié |
| **`SEC-CERT-07-02`** | `SEC-07` | Formulaire upload | Téléversement d'un `.exe` renommé `.pdf` | Rejet par validation Magic Bytes | Acceptation sur seule foi de l'extension |
| **`SEC-CERT-07-03`** | `SEC-07` | Limite à 10 Mo | Téléversement d'un fichier de 15 Mo | Rejet immédiat avec message d'erreur de taille | Dépassement accepté ou crash |
| **`SEC-CERT-07-04`** | `SEC-07` | Scanner antivirus actif | Téléversement d'un fichier sain | Analyse réelle exécutée, statut `Sain` consigné | Acceptation aveugle sans appel scanner |
| **`SEC-CERT-07-05`** | `SEC-07` | Scanner antivirus actif | Téléversement chaîne standard EICAR | Détection de menace, rejet immédiat, alerte consignée | Fichier infecté accepté |
| **`SEC-CERT-07-06`** | `SEC-07` | Scanner indisponible | Téléversement d'un fichier | Rejet par politique Fail-Closed (`ErreurAnalyse`) | Fichier accepté par défaut quand scanner HS |
| **`SEC-CERT-09-01`** | `SEC-09` | Profil Production | Démarrage de l'application | `DevelopmentSeeder` désactivé, Swagger restreint | Exécution de comptes de test en prod |
| **`SEC-CERT-09-02`** | `SEC-09` | Configuration | Scan statique `appsettings.json` | 0 secret / mot de passe de prod en clair dans Git | Présence de mots de passe de prod |

---

### 6. INVARIANTS & ANTI-SCOPE

- **0 Migration EF Core** : Couche purement middleware, validation et adaptateurs de stockage.
- **0 Dépendance SaaS Cloud externe** : Fonctionnement 100 % local et souverain.
- **0 Absorption de scope tiers** : `SEC-02` (PC-05), `SEC-06` (PC-14), `ENR-04` (PC-16), `IA-01..07` (PC-18), `NFR-03..10` strictement exclus.
- **Préservation NFR-08 (API REST)** et **ERR-01 (Brouillons)**.
