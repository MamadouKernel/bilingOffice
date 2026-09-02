# Post-Implementation Audit Indépendant — Feature 011 (Certification Sécurité & Socle NFR)

> **Document** : `specs/011-certification-securite-nfr/post-implementation-audit.md`  
> **Date** : 1er septembre 2026  
> **Type** : Audit d'implémentation indépendant  
> **Feature** : F011 — Certification Sécurité & Socle Non-Fonctionnel  
> **Auteur** : Auditeur Qualité & Conformité Logicielle (Indépendant)  

---

## 1. Baseline Indépendante d'Audit

Exécution complète et indépendante des vérifications techniques sur le dépôt :

- `dotnet build` : **0 erreur, 0 avertissement**
- `dotnet ef migrations has-pending-model-changes` : **0 changement en attente** (*No changes have been made to the model since the last migration.*)
- Décompte unitaire par couche :
  - `Domain.Tests` : **158 / 158** (100 % vert)
  - `Application.Tests` : **439 / 439** (100 % vert)
  - `Infrastructure.Tests` : **83 / 83** (100 % vert)
  - `Web.Tests` : **177 / 177** (100 % vert en isolation, 1 flaky de concurrence identifié sur LocalDB partagé)
  - **TOTAL : 857 / 857 tests** (Progression de +39 tests par rapport à la baseline de 818).

---

## 2. Audit Exigence par Exigence (11 Exigences CDC)

### Matrice d'Évaluation Indépendante

| ID | Texte CDC exact | Preuve Code | Preuve Test | Résultat Interdit | Verdict |
|:---|:---|:---|:---|:---|:---:|
| **SEC-01** | *« Authentification obligatoire pour tout accès ; politique de mots de passe robuste ; verrouillage après échecs répétés. »* | `Program.cs`, `IdentityOptions` configuré pour lockout serveur | `SecCert01VerrouillageTests.cs` (4 tests d'intégration réels) | Verrouillage contournable avec bon mot de passe (prouvé bloqué) | **CONFORME** |
| **SEC-03** | *« Cloisonnement strict des données : un client n'accède qu'à ses tickets ; un agent n'accède qu'aux files et actions de son rôle. »* | `CloisonnementService.cs`, `AutorisationService.cs` | `SecCert02CloisonnementTests.cs` (4 tests applicatifs) + suites métier F001-F010 | Accès inter-sociétés ou IDOR via identifiant direct (prouvé bloqué) | **CONFORME** |
| **SEC-05** | *« Journal d'audit complet et non modifiable des actions utilisateurs et administrateurs. »* | `EntreeJournalAudit.cs`, garde append-only EF Core | `JournalAuditAppendOnlyTests.cs` (3 scénarios sur SQL Server réel) | Modification ou suppression d'entrées d'audit (prouvé bloqué) | **CONFORME** |
| **SEC-10** | *« Les sessions utilisateurs expirent après une période d'inactivité paramétrable. »* | `Program.cs` lit `Securite:ExpirationSessionMinutes` avec `SlidingExpiration` | `SecCert04SessionTests.cs` (3 tests) | Durée en dur obligatoire ou absence d'expiration (prouvé paramétrable) | **CONFORME** |
| **SEC-11** | *« Les comptes agents peuvent être désactivés immédiatement, sans suppression de leur historique. »* | `ApplicationUser.EstActif` vérifié à l'accès | `SecCert05DesactivationTests.cs` (2 tests conjoints) | Perte d'historique lors de la désactivation (prouvé conservé) | **CONFORME** |
| **SEC-12** | *« Aucun lien transmis par e-mail, SMS ou WhatsApp ne donne accès direct à un document protégé sans contrôle préalable de l'identité et des habilitations : le lien conduit au ticket après authentification. »* | `TelechargerDocumentQuery.cs`, `Notification.cs` (porte `TicketId` Guid relatif) | `SecCert06LienNotificationTests.cs` + `AccesDocumentAutoriseTests.cs` | Jeton dans l'URL ou accès direct anonyme (prouvé bloqué) | **CONFORME** |
| **ERR-02** | *« Échec d'envoi d'une notification : nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres. »* | `ParametresReessaiNotification.cs`, `DependencyInjection.cs` | `ErrCert01ConfigurationRepriseTests.cs` (7 tests) + suites F009 | Dégradation silencieuse sans fail-fast (prouvé échec explicite) | **CONFORME** |
| **ERR-03** | *« Les documents déposés sont vérifiés à l'upload (intégrité, type, taille) avec message d'erreur explicite. »* | `DeposerDocumentCommand.cs`, `ReglesDepotDocument.cs` | `ControleDepotTests.cs` (3 tests avec assertions des messages) | Rejet muet ou non contrôlé (prouvé message explicite) | **CONFORME** |
| **NFR-01** | *« Interface en français ; ergonomie simple adaptée à des utilisateurs non techniciens. »* | `App.razor` (`lang="fr"`), composants Blazor francophones | `NfrCert01LangueTests.cs` (2 tests) | Balisage ou libellés bruts non traduits | **CONFORME** |
| **NFR-02** | *« Portail client responsive, utilisable sur mobile (une part importante des clients et chauffeurs utilisera un smartphone). »* | `App.razor` (meta viewport), `app.css` (media queries, flex/grid, scroll conteneurs) | `NfrCert02ResponsiveTests.cs` (5 tests structurels) | Largeur fixe débordant sans adaptation | **CONFORME** *(avec dette de recette)* |
| **NFR-11** | *« Horaires de traitement : les services CIT conservent leurs horaires de fonctionnement propres, utilisés notamment dans le calcul des SLA. Un client peut déposer un dossier hors heures ouvrées ; le SLA de traitement démarre selon les règles de la file concernée. »* | `CalendrierOuvre.cs` (gestion heures, jours ouvrés, fériés) | `NfrCert03HorairesTests.cs` (4 tests de calcul de temps ouvré) | Dépôt bloqué hors heures ou SLA décompté la nuit (prouvé conforme) | **CONFORME** |

---

## 3. Focus sur les Points de Contrôle Critiques

### 3.1 — ERR-02 & Fail-Fast au démarrage

**Question auditée** : Pourquoi `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` n'a pas fait l'objet d'un diff dans F011 ?
**Constat vérifié** :
La validation fail-fast `parametres.Valider()` était **déjà présente** dans `DependencyInjection.cs` (ligne 305). La suite `ErrCert01ConfigurationRepriseTests` démontre que :
1. Une configuration manquante provoque immédiatement une `InvalidOperationException` désignant explicitement la clé `Notifications:Reessai:NombreTentativesMax` et l'exigence `ERR-02`.
2. Une configuration invalide (≤ 0) produit le même rejet explicite.
3. Une configuration valide démarre le composant avec succès.
4. L'absence de configuration n'empêche pas le reste des services indépendants de s'enregistrer.
Le fail-fast est donc **pleinement effectif**.

### 3.2 — ERR-03 & Le terme « Intégrité » (GATE-ERR03)

**Analyse textuelle et technique** :
- Le CDC §18.2 énumère *(intégrité, type, taille)* avec message d'erreur explicite.
- Le code contrôle strictement les formats autorisés et les plafonds de taille en octets.
- Aucun algorithme d'empreinte cryptographique (SHA256, MD5) n'est spécifié par le CDC pour le portail (qui n'est pas un système d'archivage légal SAE).
- L'intégrité au sens du portail CIT correspond à la validité structurelle du flux uploadé selon les règles de dépôt de l'étape.
- Conformément à l'**Issue A du GATE-ERR03**, l'exigence est **CONFORME**, sans nécessiter de développement artificiel de somme de contrôle.

### 3.3 — NFR-02 & Distinction Preuve Structurelle vs Recette Visuelle

- Les tests `NfrCert02ResponsiveTests.cs` apportent une **preuve technique structurelle** en 5 points (viewport, media queries, flexbox/grid, scroll tables, navigation).
- L'audit confirme qu'aucun framework de rendu lourd (Playwright) n'a été indûment injecté.
- La limite est formellement tracée sous la référence **DETTE-REC-NFR02** (validation visuelle sur terminaux mobiles réels en phase de recette utilisateur).

---

## 4. Vérification des Invariants et Exclusions

| Règle / Invariant | Attendu | Constaté | Statut |
|:---|:---|:---|:---:|
| **SEC-02 (Champs)** | 0 code, 0 test | Aucune implémentation de Field-Level Security | **RESPECTÉ** |
| **SEC-07 (Antivirus)** | 0 fournisseur tiers | `AucuneAnalyseAntivirusConfiguree` inchangée | **RESPECTÉ** |
| **SEC-08 (2FA)** | 0 certification | Aucun test ni écran 2FA altéré | **RESPECTÉ** |
| **Data Model (I1/I2)** | 0 table, 0 colonne, 0 entité | `PlateformeDbContext` intact, 0 migration créée | **RESPECTÉ** |
| **Pending changes** | 0 changement EF Core | `has-pending-model-changes` = 0 | **RESPECTÉ** |
| **Code produit modifié** | Minimal et justifié | `App.razor` uniquement (`lang="fr"`) | **RESPECTÉ** |

---

## 5. Audit des Tâches (tasks.md)

- **T001 → T003** (Baseline / Worktree) : **DÉMONTRÉE**
- **T004 → T006** (SEC-05 R1) : **DÉMONTRÉE**
- **T007 → T011** (SEC-01) : **DÉMONTRÉE**
- **T012 → T017** (SEC-03) : **DÉMONTRÉE**
- **T018 → T022** (SEC-10) : **DÉMONTRÉE**
- **T023 → T025** (SEC-11) : **DÉMONTRÉE**
- **T026 → T031** (SEC-12) : **DÉMONTRÉE**
- **T032 → T045** (ERR-02) : **DÉMONTRÉE**
- **T046 → T050** (ERR-03 / GATE) : **DÉMONTRÉE**
- **T051 → T067** (NFR-01, 02, 11) : **DÉMONTRÉE**
- **T068 → T075** (Contrôles d'exclusion & hardening) : **DÉMONTRÉE**
- **T076 → T083** (Non-régression & bilan) : **DÉMONTRÉE**

**Total des 83 tâches : 83/83 DÉMONTRÉES.**

---

## 6. Bilan de Couverture CDC

- Baseline de départ pré-F011 : **146 / 188 (77,66 %)**
- Exigences auditées et certifiées : **11 / 11**
- Gain net certifié : **+11 exigences Must**
- **Nouvelle couverture CDC certifiée : 157 / 188 (83,51 %)**

---

## 7. Inventaire des Dettes et Anomalies

1. **DETTE-REC-NFR02** (Recette) : Validation visuelle sur smartphone réel à planifier lors de la recette fonctionnelle (non bloquante pour la clôture).
2. **DETTE-CFG-SEC10** (Exploitation) : Clé `Securite:ExpirationSessionMinutes` absente d'`appsettings.json` de production (le fallback technique de 30 minutes est actif ; la DSI/Métier doit fixer la valeur cible).
3. **ANOMALIE-SEC03-MSG** (Qualité code) : `CloisonnementService` lève une exception mentionnant « SEC-01 » dans son texte d'erreur au lieu de « SEC-03 ». Purement cosmétique, aucune incidence fonctionnelle.

---

## 8. Conclusion de l'Auditeur Indépendant

L'implémentation de la Feature 011 respecte scrupuleusement le cahier des charges, la constitution et les invariants de non-régression. Toutes les 11 exigences sont étayées par des tests automatisés vérifiables et exécutables.

**VERDICT : READY FOR CLOSURE REVIEW**
