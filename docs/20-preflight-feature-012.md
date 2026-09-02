# Preflight Feature 012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves

> **Document** : `docs/20-preflight-feature-012.md`  
> **Date** : 1er septembre 2026  
> **Auteur** : Scrum Master senior, Business Analyst & Quality Gate  
> **Contexte** : Préparation du périmètre définitif de la Feature 012 avant lancement de `/speckit.specify`  
> **Statut** : **PRÉPARATION DE CADRAGE OFFICIELLE** — Zéro code, zéro test, zéro migration.

---

## 1. Baseline Technique et CDC

### Baseline Technique Réelle
- **Build .NET** : 0 erreur, 0 avertissement
- **Tests unitaires et intégration** : **858 / 858 verts**
  - `Domain.Tests` : 158 / 158
  - `Application.Tests` : 439 / 439
  - `Infrastructure.Tests` : 83 / 83
  - `Web.Tests` (sérialisé) : 178 / 178
- **EF Migrations** : 0 pending model change

### Baseline CDC Nominative Post-F011
- **Conformes** : **157 / 188 (83,51 %)**
- **Partielles** : 8
- **Différées** : 4
- **Absentes** : 11
- **À clarifier** : 8
- **Total de contrôle** : 188 / 188

### Projection Maximale F012
- **Gain maximal ciblé** : **+7 exigences** (4 Must, 1 Should, 2 Could)
- **Baseline projetée post-F012** : **164 / 188 (87,23 %)**
- *(Rappel : REC-SCE-14 est une preuve de recette transverse et ne compte pas comme un identifiant d'exigence supplémentaire).*

---

## 2. Audit Indépendant des Sujets Candidats

### 1. PRI-02 — Déduction et influence de la priorité
- **Texte CDC exact** : *« La priorité peut influencer l'ordre du ticket dans la file, le SLA applicable, les alertes et les règles d'escalade »* (Must, §7 / CDC).
- **Existant dans le code** :
  - Entité `NiveauPriorite` et `ModificationPriorite` (`Domain/Tickets/Priorite.cs`).
  - Commande `ModifierPrioriteCommand` avec contrôle d'habilitation et traçage d'audit (`Application/Tickets/ModifierPrioriteCommand.cs`).
  - Priorité stockée sur `Ticket.PrioriteId`.
- **Écart constaté** :
  - Le calcul initial de priorité à la création de ticket n'est pas automatisé (laissée à null ou valeur par défaut).
  - Aucune suite de tests dédiée n'a été rattachée nominativement à PRI-02 dans les features précédentes.
- **Code requis** : Raccordement d'une règle de déduction / attribution initiale de priorité lors de la création d'un ticket et démonstration de l'influence sur le tri des files (`ConsulterFileQuery`).
- **Tests** : Suite `PriCert01PrioriteTests.cs` (calcul initial, modification tracée, tri de file).
- **Risque** : Faible.
- **Classification** : **B — CORRECTION MINIMALE**
- **Verdict** : **GO**

---

### 2. IMP-03 — Numéro de ticket communiqué sur le profil et par e-mail
- **Texte CDC exact** : *« Le client reçoit ses numéros de ticket sur son profil et par e-mail »* (Must, §11 / IMP-03).
- **Existant dans le code** :
  - Le profil et la vue mes demandes affichent déjà les numéros de ticket (SOC-01 / SOC-03).
  - Le moteur de notifications F009 dispose de l'événement `TicketCree` avec règles de diffusion `InApp` et `Email` (`DevelopmentSeederNotifications.cs:43-44`).
- **Écart constaté** :
  - `SoumettreDemandeImportCommand.cs` enregistrait l'historique et l'audit mais omettait d'appeler `journalisation.PublierEvenementAsync("TicketCree", ...)` vers `JournalEvenements`.
  - La portion e-mail était donc restée « PARTIELLE assumée » depuis F003.
- **Dépendance F009** : Résolue (F009 est 100% en place).
- **Fournisseur externe requis** : **NON** (l'abstraction d'envoi et la boîte d'envois F009 certifient la génération de l'e-mail sans dépendre d'un SMTP externe de production).
- **Code requis** : Publication de l'événement `TicketCree` dans `SoumettreDemandeImportCommand`.
- **Test** : Preuve automatisée démontrant la création de la notification et de l'envoi e-mail suite à une soumission Import.
- **Classification** : **B — CORRECTION MINIMALE**
- **Verdict** : **GO**

---

### 3. REC-02 — Catégories de réclamations paramétrables sans développement
- **Texte CDC exact** : *« Les catégories sont paramétrables et dynamiques (ajout/modification sans développement) »* (Must, §14 / REC-02).
- **Existant dans le code** :
  - Entité `CategorieReclamation` avec `Code`, `Libelle`, `FileCibleDefautId`, `EstActif` (`Domain/Reclamations/CategorieReclamation.cs`).
  - Commandes `CreerCategorieReclamationCommand` et `ModifierCategoriesReclamationCommand` (`Application/CustomerComplaint/`).
  - Page Blazor complète `AdministrationCategories.razor` avec création, modification en ligne, activation/désactivation.
  - Suite de tests `AdministrationCategoriesTests.cs` (162 lignes).
- **Écart constaté** :
  - L'exigence a été intégralement développée lors de la remédiation post-audit F006.
  - La baseline nominative R0 l'a classée PARTIELLE uniquement en raison de la réserve historique de F006.
- **Code requis** : **AUCUN NOUVEAU DÉVELOPPEMENT** (code déjà en place et fonctionnel).
- **Test** : Suite de certification TEST-SPEC nominative `RecCert01CategoriesTests.cs` capitalisant sur l'existant.
- **Classification** : **A — DÉJÀ CONFORME, CERTIFICATION SEULE**
- **Verdict** : **GO**

---

### 4. REC-03 — Orientation automatique Support Opérationnel et instruction
- **Texte CDC exact** : *« Les tickets sont orientés vers le Support Opérationnel, qui peut instruire, demander des compléments, joindre des documents, échanger avec le client et orienter vers les services concernés »* (Must, §14 / REC-03).
- **Existant dans le code** :
  - `SoumettreReclamationCommand.cs` route automatiquement le ticket vers la file cible paramétrée de la catégorie (`categorie.FileCibleDefautId` qui pointe par défaut vers la file « Support Opérationnel »).
  - Le fil de discussion, le dépôt de pièces jointes, la demande de complément et la clôture avec évaluation sont déjà implémentés.
- **Écart constaté** :
  - Même cause que REC-02 : réserve de clôture initiale F006 non formellement recertifiée.
- **Code requis** : **AUCUN NOUVEAU DÉVELOPPEMENT**.
- **Test** : Suite de certification TEST-SPEC `RecCert02InstructionSupportTests.cs`.
- **Classification** : **A — DÉJÀ CONFORME, CERTIFICATION SEULE**
- **Verdict** : **GO**

---

### 5. SEC-08 — Double authentification (2FA) pour agents et administrateurs
- **Texte CDC exact** : *« Second facteur d'authentification pour les profils agents et administrateurs »* (Should, §18.1 / SEC-08).
- **Audit approfondi du harnais de test** :
  - Le code applicatif (`ActiverDoubleAuthentification.razor`, génération TOTP RFC 6238) est 100% conforme.
  - En exécution isolée de la classe de test : **0 échec sur 10 runs consécutifs**.
  - En exécution globale concurrente sous xUnit : échec intermittent dû au fait que toutes les classes de test Web partagent la même base SQL Server LocalDB `CitPlateformeTestsWeb` sans isolation de collection.
  - **Diagnostic** : **Défaut exclusif de harnais de test (D-T1)**, aucun défaut produit.
- **Correction requise** :
  - Configurer l'assembly `CIT.Plateforme.Web.Tests` avec `[assembly: CollectionBehavior(DisableTestParallelization = true)]` ou isoler la collection de test Web.
  - Exécuter 10 fois consécutivement la suite 2FA pour garantir zéro régression.
- **Test** : `SecCert08DoubleAuthentificationTests.cs` (ou requalification de `ParcoursConnexion2FaTests`).
- **Classification** : **B — CORRECTION HARNAIS DE TEST**
- **Verdict** : **GO**

---

### 6. SOC-14 — Export des données
- **Texte CDC exact** : *« Export des données »* (Could, §8 / SOC-14).
- **Existant dans le code** :
  - `window.telechargerFichier` existe dans `wwwroot/js/telechargements.js`.
  - Des exports spécialisés existent (ex. PDF Proforma SIM-09).
- **Écart constaté** :
  - Absence d'un endpoint / service d'export tabulaire universel (format CSV standard) permettant d'extraire la liste des tickets / dossiers selon les filtres de l'utilisateur.
- **Portée raisonnable** :
  - Une query `ExporterTicketsCsvQuery` générant un flux CSV (séparateur point-virgule / virgule, encodage UTF-8) avec respect strict du cloisonnement (CPT-04).
  - Un bouton d'export dans les vues tabulaires existantes.
- **Code requis** : Service de génération CSV dans Application + intégration UI.
- **Test** : Tests unitaires sur le format CSV et tests d'autorisation/cloisonnement de l'export.
- **Classification** : **C — DÉVELOPPEMENT MÉTIER CIBLÉ**
- **Verdict** : **GO**

---

### 7. COM-06 — Canaux des relances automatiques
- **Texte CDC exact** : *« Relances automatiques paramétrables lorsque le ticket est en attente d'une action client au-delà d'un délai défini »* (Could, §16 / COM-06).
- **Existant dans le code** :
  - Le mécanisme de relance automatique est déjà implémenté et certifié (CYC-04).
  - L'événement `RelanceAutomatique` est déjà prévu dans le modèle de notification F009.
- **Écart constaté** :
  - Les règles de diffusion par défaut pour `RelanceAutomatique` n'étaient pas associées à une suite de test validant le choix paramétrable des canaux (InApp + Email).
- **Fournisseur externe requis** : **NON** (bloqué pour SMS/WhatsApp par PC-16, mais certifiable sur les canaux disponibles InApp et Email).
- **Code requis** : Raccordement explicite de la règle de relance sur les canaux activés.
- **Test** : `ComCert06CanauxRelanceTests.cs` validant le paramétrage multicanal de la relance.
- **Classification** : **B — CORRECTION MINIMALE**
- **Verdict** : **GO**

---

### 8. REC-SCE-14 — Preuve transverse de bout en bout
- **Texte CDC exact** : Recette transverse couvrant 7 volets sans numérotation d'étapes :
  1. *Notifications sur chaque canal configuré*
  2. *Lien vers le ticket avec authentification préalable*
  3. *Continuité digital/présentiel par recherche de ticket au guichet*
  4. *Rendez-vous donnant accès au bureau de facturation*
  5. *Regroupement des tickets d'une même demande en dossier*
  6. *Contrôle des habilitations*
  7. *Journal d'audit inviolable*
- **Écart constaté** :
  - Les 7 volets fonctionnent et sont testés unitairement/intégration.
  - Il manque une suite E2E unique reliant l'ensemble du parcours utilisateur.
- **Livrable F012** :
  - Création de `tests/CIT.Plateforme.Web.Tests/E2E/RecSce14NotificationsE2ETests.cs`.
- **Classification** : **E2E RECETTE TRANSVERSE (Zéro gain CDC additionnel)**
- **Verdict** : **GO**

---

## 3. Anti-Scope Strict de la Feature 012

Les éléments suivants sont **strictement exclus** du périmètre de la Feature 012 :

- ❌ **SEC-02** (RBAC au niveau des champs IES — bloqué par PC-05)
- ❌ **SEC-07** (Scan antivirus tiers — bloqué par absence de fournisseur DSI)
- ❌ **IA-01 à IA-07** (Module Assistant IA — bloqué par PC-18)
- ❌ **ERR-01** (Gestion des brouillons — lot F013 dédié)
- ❌ **ENR-04** / SMS & WhatsApp réels (bloqué par PC-16)
- ❌ **SEC-04, SEC-06, SEC-09, NFR-03→10** (Production Readiness / Hébergement — PC-01/13/14)
- ❌ **Refonte visuelle UI** ou modifications d'ergonomie hors boutons d'export

---

## 4. Synthèse des Classifications

| Exigence / Sujet | Priorité | Famille | Classification | Gain CDC | Action F012 |
|:---|:---:|:---:|:---:|:---:|:---|
| **PRI-02** | Must | Socle | **B — Correction minimale** | +1 | Déduction initiale priorité + test tri file |
| **IMP-03** | Must | Billing Import | **B — Correction minimale** | +1 | Publication `TicketCree` dans `SoumettreDemandeImport` |
| **REC-02** | Must | Customer Complaint | **A — Certification seule** | +1 | Suite TEST-SPEC sur l'admin catégories existante |
| **REC-03** | Must | Customer Complaint | **A — Certification seule** | +1 | Suite TEST-SPEC sur l'orientation Support existante |
| **SEC-08** | Should | Sécurité | **B — Correction harnais** | +1 | Stabilisation xUnit + certification 2FA |
| **SOC-14** | Could | Socle | **C — Dév. métier ciblé** | +1 | Query export CSV + intégration Blazor |
| **COM-06** | Could | Notifications | **B — Correction minimale** | +1 | Raccordement relances multicanales |
| **REC-SCE-14** | — | Recette | **Preuve E2E** | +0 | Test E2E `RecSce14NotificationsE2ETests.cs` |

---

## 5. Bilan du Périmètre et Couverture

```
PÉRIMÈTRE FINAL RETENU (7 exigences) :
  PRI-02 (Must)
  IMP-03 (Must)
  REC-02 (Must)
  REC-03 (Must)
  SEC-08 (Should)
  SOC-14 (Could)
  COM-06 (Could)
  + REC-SCE-14 (Preuve E2E transverse)

RÉPARTITION PAR PRIORITÉ :
  MUST   : 4  (PRI-02, IMP-03, REC-02, REC-03)
  SHOULD : 1  (SEC-08)
  COULD  : 2  (SOC-14, COM-06)

GAIN STRICT CDC : +7 exigences
BASELINE ACTUELLE : 157 / 188 (83,51 %)
BASELINE PROJETÉE : 164 / 188 (87,23 %)
```

---

## 6. Definition of Done (DoD) F012

1. **Preuve nominative obligatoire** : Chaque exigence retenue dispose d'une classe de test TEST-SPEC dédiée avec traçabilité CDC.
2. **Cycle TDD respecté** : Preuve rouge avant modification, verte après.
3. **Stabilisation harnais** : `ParcoursConnexion2FaTests` passe 10 fois consécutivement sans échec.
4. **Scénario E2E REC-SCE-14** : `RecSce14NotificationsE2ETests.cs` est vert et rejoue les 7 volets sans mock destructif.
5. **Aucune régression** : 100% de la baseline technique existante (858 tests) reste verte.
6. **Zéro warning** : Compilateur .NET à 0 warning.
7. **Modèle de données** : 0 pending model change (aucune migration ou migration strictement isolée si export CSV requiert une entité, sinon 0 migration).
8. **Zéro tiers externe** : Aucun compte tiers réel (Twilio, SendGrid) requis pour valider les tests.
9. **Audit & Closure** : Production de `implementation-report.md`, `post-implementation-audit.md` et `closure-review.md`.
10. **Commit sélectif** : Uniquement les fichiers de la Feature 012, exclusion stricte de tout fichier étranger.

---

## 7. Registre des Risques F012

| Risque | Probabilité | Impact | Stratégie de mitigation |
|:---|:---:|:---:|:---|
| **Régression sur les flux de réclamations F006** | Faible | Élevé | REC-02 et REC-03 sont déjà implémentées ; ne toucher à aucun code produit existant, produire uniquement les tests de certification. |
| **Instabilité résiduelle du test 2FA (SEC-08)** | Moyenne | Moyen | Désactiver la parallélisation sur la collection de tests Web xUnit pour éliminer la contention sur la base LocalDB partagée. |
| **Sur-développement sur l'export de données (SOC-14)** | Faible | Moyen | Se limiter strictement à une génération CSV simple des listes existantes sans introduire de moteur BI ou d'agrégation complexe. |
| **Faux test E2E sur REC-SCE-14** | Faible | Élevé | Utiliser les composants Bunit/TestServer réels traversant le pipeline authentique de bout en bout. |

---

## 8. Conclusion et Validation de Scope

Le périmètre de la **Feature 012** est parfaitement délimité, documenté, autonome et exempt de tout point de blocage métier ou technique.

**VERDICT QUALITY GATE : READY FOR `/speckit.specify`**
