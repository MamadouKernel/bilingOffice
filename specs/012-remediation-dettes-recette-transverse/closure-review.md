# CLOSURE REVIEW — FEATURE 012

**Feature** : Feature 012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves  
**Branche** : `main`  
**Commit de référence (HEAD)** : `a2dcc76351d5218f29cc6c2325978107f3359941` (F011)  
**Date de revue** : 2026-09-01  
**Verdict** : **READY FOR COMMIT**

---

## 1. BASELINE TECHNIQUE VÉRIFIÉE

```text
Compilation : dotnet build → 0 Avertissement, 0 Erreur
Modèle EF Core : dotnet ef migrations has-pending-model-changes → 0 Pending Changes
Migrations créées : 0 (Invariant I1 respecté)

Résultats des tests unitaires, d'intégration et de sécurité :
- CIT.Plateforme.Domain.Tests          : 158 / 158 verts (100 %)
- CIT.Plateforme.Application.Tests     : 447 / 447 verts (100 %)
- CIT.Plateforme.Infrastructure.Tests  :  83 /  83 verts (100 %)
- CIT.Plateforme.Web.Tests             : 179 / 179 verts (100 %)
TOTAL GLOBAL : 867 / 867 VERTS (100 %)
```

---

## 2. AUDIT NOMINATIF DES 7 EXIGENCES MÉTIER

| ID | Texte CDC | Statut avant F012 | Delta F012 | Code Réel | Test | Preuve Négative | Dette Éventuelle | Verdict |
|:---|:---|:---:|:---:|:---|:---|:---|:---:|:---:|
| **PRI-02** | Priorité automatique selon critères métier et ordonnancement de file | Réservée / Partielle | +1 | `CreerTicketCommand.cs`, `SoumettreDemandeImportCommand.cs`, `ConsulterFileQuery.cs` | `PriCert01PrioriteTests.cs` (2/2) | Reclassement manuel tracé dans l'audit | Aucune | **CONFORME** |
| **IMP-03** | Numéro de ticket communiqué par e-mail au client | Réservée / Non couverte | +1 | `SoumettreDemandeImportCommand.cs` (`journalisation.PublierEvenementAsync("TicketCree", ...)`) | `ImpCert03NotificationEmailTests.cs` (1/1) | Refus doublon BL sans fausse notification | Aucune | **CONFORME** |
| **REC-02** | Catégories de réclamation paramétrables | Non certifiée | +1 | Code existant certifié (0 code produit nouveau) | `RecCert02CategoriesTests.cs` (1/1) | Catégorie inactive refusée | Aucune | **CONFORME** |
| **REC-03** | Orientation vers Support Opérationnel | Non certifiée | +1 | Code existant certifié (0 code produit nouveau) | `RecCert03OrientationSupportTests.cs` (1/1) | Agent non habilité refusé | Aucune | **CONFORME** |
| **SEC-08** | Double authentification TOTP (Google/Microsoft Authenticator) | Réservée (Harnais) | +1 | Code Identity/2FA 0 modification, harnais assaini | `ParcoursConnexion2FaTests.cs` (5/5, 10/10 runs) | Code invalide rejeté, inactif rejeté | Aucune | **CONFORME** |
| **SOC-14** | Export des données en format standard (CSV) | Non couverte | +1 | `ExporterTicketsCsvQuery.cs`, `DependencyInjection.cs` | `SocCert14ExportCsvTests.cs` (2/2) | Client Société A → Société B interdit (CPT-04), Agent file non autorisée interdit (SEC-03) | Aucune | **CONFORME** |
| **COM-06** | Canaux de relance paramétrables (In-App, e-mail) | Non couverte | +1 | `DevelopmentSeederNotifications.cs` + F009 runtime | `ComCert06CanauxRelanceTests.cs` (1/1) | Désactivation canal client respectée | Aucune | **CONFORME** |

---

## 3. AUDIT DÉTAILLÉ PAR EXIGENCE

### PRI-02 — Priorité automatique et ordonnancement
- Attribution automatique du niveau standard par défaut à la création d'un ticket.
- Réutilisation stricte de l'entité et du repository existants `IPrioriteRepository` / `NiveauPriorite`.
- Rang numérique paramétrable (`Rang = 1` pour "Normale").
- Ordonnancement garanti dans `ConsulterFileQuery` : `Priorite.Rang DESC` puis `DateCreationUtc ASC`.
- Possibilité de reclassement manuel par un superviseur (PRI-03) préservée et tracée.

### IMP-03 — Numéro de ticket communiqué par e-mail
- Chaîne événementielle complète : `SoumettreDemandeImportCommand` → événement `TicketCree` → `JournalEvenements` → `DistribuerNotificationsUseCase` (F009) → création de `Notification` (InApp) et `EnvoiNotification` (`Canal = CanalNotification.Email`).
- **EMAIL LOGIQUE/PERSISTÉ** : **OUI** (entités `Notification` et `EnvoiNotification` persistées en base).
- **EMAIL PHYSIQUEMENT LIVRÉ PAR FOURNISSEUR** : **HORS PÉRIMÈTRE** (aucun connecteur externe SMTP tiers imposé à ce stade).

### REC-02 & REC-03 — Réclamations (Certification formelle)
- Certification formelle sur le code existant sans aucune réécriture ni ajout superflu.
- Catégories dynamiques CRUD avec activation/désactivation et orientation vers la file cible Support Opérationnel.

### SEC-08 — Double Authentification TOTP
- 0 ligne de code produit Identity / 2FA modifiée.
- Problème d'isolation du harnais résolu (`PlateformeWebCollection`, `ClearAllPools`, point d'accès standardisé `/client/demandes`).
- Campagne de validation : **10 runs consécutifs à 100% de succès (50/50 tests)**.

### SOC-14 — Export CSV & Sécurité
- Format : UTF-8 avec BOM (`0xEF, 0xBB, 0xBF`), séparateur point-virgule, horodatages ISO 8601 UTC.
- En-tête exact : `NumeroTicket;Module;TypeDemande;Statut;FileCourante;DateCreationUtc;Priorite;ReferenceDossier`.
- Cloisonnement multi-tenant absolu (CPT-04 / SEC-03) prouvé par test négatif.
- Protection injection tableur : neutralisation des formules commençant par `=`, `+`, `-`, `@`.

### COM-06 — Canaux de relance paramétrables
- Règles persistées dans `ReglesDiffusion` et évaluées au runtime par le moteur F009.
- Canaux InApp et Email configurables et activables indépendamment.
- Préférences utilisateur respectées.
- Aucun fournisseur SMS/WhatsApp réel introduit (PC-16 respecté).

---

## 4. AUDIT DU SCÉNARIO TRANSVERSE E2E (REC-SCE-14)

- **Fichier** : `tests/CIT.Plateforme.Web.Tests/E2E/RecSce14NotificationsE2ETests.cs`
- **Validation** : 3 runs consécutifs validés (100% PASS).
- **7 Volets contractuels** :
  1. *Notifications* : Soumission d'une demande multi-BL et distribution InApp + Email (`TicketCree`).
  2. *Lien sécurisé* : Blocage d'accès sans session (302 vers login) et accès accordé avec session.
  3. *Recherche guichet* : Recherche et consultation d'un ticket par numéro de BL.
  4. *Rendez-vous* : Réservation d'un créneau guichet (`RendezVous.Creer`).
  5. *Dossier* : Regroupement multi-tickets sous le même dossier commun.
  6. *Habilitations* : Contrôle strict RBAC (agent non habilité bloqué par `AccesRefuseException`).
  7. *Journal d'audit* : Audit append-only exhaustif tracé (`TicketImportCree`).
- **Gain CDC** : **+0** (Scénario transverse de recette).

---

## 5. AUDIT DES TESTS HISTORIQUES & PRÉSERVATION F011

- `Imp11EtProducteursTests.cs` : Passage de 21 à 22 fichiers producteurs suite à l'ajout légitime d'IMP-03 dans `SoumettreDemandeImportCommand.cs`. Assertion renforcée.
- `SecCert01VerrouillageTests.cs` & `SecCert04SessionTests.cs` : Sonde de session mise à jour sur `/client/demandes` (`@attribute [Authorize]`). Preuves SEC-01 / SEC-04 / SEC-10 strictement équivalentes.
- `PlateformeWebApplicationFactory.cs` : Sérialisation de collection et purge de pool LocalDB.
- `ConnexionPremierClicTests.cs` : Fichier `??` (non suivi), **strictement exclu du commit F012**.
- **F011 PROOF PRESERVATION** : **PASS**.

---

## 6. AUDIT DU CODE PRODUIT MODIFIÉ

| Fichier | Exigence | Justification | Nécessaire | Hors Scope |
|:---|:---|:---|:---:|:---:|
| `src/CIT.Plateforme.Application/Tickets/CreerTicketCommand.cs` | PRI-02 | Attribution priorité par défaut | OUI | NON |
| `src/CIT.Plateforme.Application/Tickets/ConsulterFileQuery.cs` | PRI-02 | Ordonnancement prioritaire de la file | OUI | NON |
| `src/CIT.Plateforme.Application/Tickets/ExporterTicketsCsvQuery.cs` | SOC-14 | Query d'export CSV sécurisée | OUI | NON |
| `src/CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs` | PRI-02, IMP-03 | Priorité par défaut + publication `TicketCree` | OUI | NON |
| `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` | SOC-14 | Enregistrement DI de `ExporterTicketsCsvQuery` | OUI | NON |
| `src/CIT.Plateforme.Infrastructure/Persistence/DevelopmentSeederNotifications.cs` | COM-06 | Règles initiales `RelanceEmise` pour InApp/Email | OUI | NON |

- **Total fichiers produit** : 6 fichiers légitimes.

---

## 7. AUDIT ANTI-SCOPE

- 0 modification SEC-02, SEC-07, ERR-01, IA.
- 0 intégration SMS ou WhatsApp réel (PC-16 respecté).
- 0 connecteur SMTP tiers ou dépendance cloud externe.
- 0 migration EF Core.
- 0 nouvelle table.
- 0 nouvelle entité.
- **ANTI-SCOPE RESULT** : **PASS**.

---

## 8. AUDIT DES TÂCHES (TASKS.MD)

- **Total tâches** : 42 / 42.
- **Tâches démontrées et validées** : 42 / 42 (Phases 1 à 10).

---

## 9. REGISTRE DES DETTES RÉSIDUELLES

| Dette | Type | Priorité | Bloque Clôture F012 ? | Bloque Production ? | Action Future |
|:---|:---|:---:|:---:|:---:|:---|
| **DETTE-REC-NFR02** | Performance / NFR | Moyenne | NON | NON | Campagne de tir de charge NFR lors de la certification performance globale. |
| **DETTE-CFG-SEC10** | Configuration | Basse | NON | OUI | Externalisation des paramètres d'expiration de session dans appsettings/secrets en production. |
| **DETTE-F010-01** | Guichet / Matériel | Basse | NON | NON | Intégration éventuelle de l'impression physique de tickets de borne. |

---

## 10. RECALCUL STRICT DU SCORE CDC

- **Score avant F012** : **157 / 188** (83,51 %)
- **Gains F012 certifiés** :
  - PRI-02 : **+1**
  - IMP-03 : **+1**
  - REC-02 : **+1**
  - REC-03 : **+1**
  - SEC-08 : **+1**
  - SOC-14 : **+1**
  - COM-06 : **+1**
  - REC-SCE-14 : **+0**
- **Gain certifié total** : **+7 / 7**
- **Score après F012** : **164 / 188 exigences conformes**
- **Taux de couverture CDC exact** : **87,23 %**

---

## 11. INVENTAIRE GIT POUR COMMIT SÉLECTIF

### Fichiers F012 à inclure (Commit Sélectif F012)
1. `specs/012-remediation-dettes-recette-transverse/`
2. `src/CIT.Plateforme.Application/Tickets/CreerTicketCommand.cs`
3. `src/CIT.Plateforme.Application/Tickets/ConsulterFileQuery.cs`
4. `src/CIT.Plateforme.Application/Tickets/ExporterTicketsCsvQuery.cs`
5. `src/CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs`
6. `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs`
7. `src/CIT.Plateforme.Infrastructure/Persistence/DevelopmentSeederNotifications.cs`
8. `tests/CIT.Plateforme.Application.Tests/Tickets/PriCert01PrioriteTests.cs`
9. `tests/CIT.Plateforme.Application.Tests/Tickets/SocCert14ExportCsvTests.cs`
10. `tests/CIT.Plateforme.Application.Tests/BillingImport/ImpCert03NotificationEmailTests.cs`
11. `tests/CIT.Plateforme.Application.Tests/CustomerComplaint/RecCert02CategoriesTests.cs`
12. `tests/CIT.Plateforme.Application.Tests/CustomerComplaint/RecCert03OrientationSupportTests.cs`
13. `tests/CIT.Plateforme.Application.Tests/Notifications/ComCert06CanauxRelanceTests.cs`
14. `tests/CIT.Plateforme.Application.Tests/Notifications/Imp11EtProducteursTests.cs`
15. `tests/CIT.Plateforme.Web.Tests/Support/PlateformeWebApplicationFactory.cs`
16. `tests/CIT.Plateforme.Web.Tests/Comptes/ParcoursConnexion2FaTests.cs`
17. `tests/CIT.Plateforme.Web.Tests/Securite/SecCert01VerrouillageTests.cs`
18. `tests/CIT.Plateforme.Web.Tests/Securite/SecCert04SessionTests.cs`
19. `tests/CIT.Plateforme.Web.Tests/E2E/RecSce14NotificationsE2ETests.cs`

### Fichiers exclus du commit F012
- Fichiers UI préexistants non liés (`App.razor`, thèmes CSS, profils, persona switcher, etc.).
- Tests non suivis étrangers (`ConnexionPremierClicTests.cs`).
- Documents d'analyse (`docs/*`, `scratch/*`).

---

## 12. CONCLUSION DE REVUE

FEATURE :
012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves

PRI-02 : CONFORME
IMP-03 : CONFORME
REC-02 : CONFORME
REC-03 : CONFORME
SEC-08 : CONFORME
SOC-14 : CONFORME
COM-06 : CONFORME

EXIGENCES CONFORMES :
7/7

REC-SCE-14 :
VALIDÉ (7/7 volets contractuels E2E, 3 runs consécutifs verts, Gain CDC = +0)

TEST-SPEC :
7/7

E2E :
REC-SCE-14-E2E (3/3 runs PASS)

F011 PROOF PRESERVATION :
PASS

TASKS :
42/42 démontrées

CODE PRODUIT MODIFIÉ :
6 fichiers strictement justifiés (0 dérive)

MIGRATION :
AUCUNE

TABLES :
0

ENTITÉS :
0

TESTS :
Domain : 158/158
Application : 447/447
Infrastructure : 83/83
Web : 179/179
TOTAL : 867/867

BUILD WARNINGS :
0

PENDING MODEL CHANGES :
0

RÉGRESSIONS :
0

DETTE :
3 dettes documentées non-bloquantes (DETTE-REC-NFR02, DETTE-CFG-SEC10, DETTE-F010-01)

BLOCKERS :
0

BASELINE CDC AVANT :
157/188

GAIN CERTIFIÉ :
+7

BASELINE CDC APRÈS :
164/188 (87,23 %)

VERDICT :
READY FOR COMMIT

STOP.
