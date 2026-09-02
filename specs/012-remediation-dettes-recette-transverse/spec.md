# Spécification Fonctionnelle — Feature 012 (Remédiation Dettes Métier, Recette Transverse & Clôture Réserves)

> **Document** : `specs/012-remediation-dettes-recette-transverse/spec.md`  
> **Feature** : F012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves  
> **Date** : 1er septembre 2026  
> **Statut** : **CLARIFIÉ (READY FOR `/speckit.plan`)**  
> **Périmètre** : **FIGÉ à 7 exigences CDC + 1 scénario de recette transverse**  
> **Baseline CDC source** : **157 / 188 (83,51 %)** — source `docs/17-baseline-nominative-188-exigences.md` post-F011  
> **Gain maximal projeté** : **+7 exigences** → **164 / 188 (87,23 %)** (non acquis avant Closure Review)

---

## 1. Objectif et Nature de la Feature

La **Feature 012** assainit l'intégralité des réserves et dettes fonctionnelles/techniques historiques issues des Features 001 à 011, certifie les capacités existantes non encore reconnues dans la baseline nominative, et livre la suite de recette transverse de bout en bout pour le scénario contractuel **REC-SCE-14**.

---

## 1 bis. Clarifications Exhaustives des 8 Sujets

### CLAR-01 : PRI-02 — Déduction et influence automatique de la priorité
- **Entrées** : Type de demande / module (`Import`, `Export`, `CustomerComplaint`, `Enrolement`), type de client (`SocieteCliente.Profil`), référentiel des niveaux de priorité (`NiveauPriorite`).
- **Règles métier** : À la création d'un ticket, le système résout le niveau de priorité par défaut configuré pour le système/module. Si un niveau est paramétré comme standard (ex. rang 1 / "Normale"), il est automatiquement assigné au ticket (`Ticket.PrioriteId`).
- **Moment du calcul** : Lors de l'exécution de la commande de création (`CreerTicketCommand`, `SoumettreDemandeImportCommand`, etc.).
- **Recalcul & modification** : Modifiable manuellement par un agent habilité (`ModifierPrioriteCommand`, permission `priorite.modifier`) avec traçage obligatoire dans `JournalAudit` et `HistoriqueTickets` (PRI-03).
- **Effet en file de traitement** : La requête de consultation des files (`ConsulterFileQuery`) intègre le niveau de priorité dans son ordonnancement : `OrderByDescending(t => t.Priorite.Rang).ThenBy(t => t.DateCreationUtc)`. Les tickets de rang supérieur sont présentés en tête de file.
- **Configurabilité** : Les niveaux de priorité et leur rang sont administrables sans développement via `IPrioriteRepository`.
- **Classification** : **B — CORRECTION MINIMALE** (calcul initial à la création + tri prioritaire en file).
- **Statut** : **RÉSOLU**.

### CLAR-02 : IMP-03 — Numéro de ticket communiqué sur le profil et par e-mail
- **Événement émis** : `TicketCree` avec charge utile JSON `{"ticketId": "<GUID>", "module": "BillingImport", "numero": "<NUMERO>"}` publié via `IJournalisationService.PublierEvenementAsync`.
- **Destinataire** : L'auteur de la demande résolu par `ResolveurClientDuDossier`.
- **Contenu minimal** : Numéro unique du ticket, nature du parcours (Import), statut initial ("Soumis"), lien relatif vers le ticket.
- **Canaux générés** : InApp (`CanalNotification.InApp`) et Email (`CanalNotification.Email`).
- **Fournisseur réel de production** : **HORS PÉRIMÈTRE**. F012 valide la création de la notification et de l'enregistrement `EnvoiNotification` pour le canal Email dans la base de données. L'intégration d'un serveur SMTP réel relève de l'infrastructure de production.
- **Bloque Implémentation** : **NON**.
- **Bloque Production** : **NON**.
- **Classification** : **B — CORRECTION MINIMALE** (ajout de l'appel `PublierEvenementAsync` dans `SoumettreDemandeImportCommand`).
- **Statut** : **RÉSOLU**.

### CLAR-03 : REC-02 — Administration des catégories de réclamations
- **Audit du code existant** :
  - Entité `CategorieReclamation` dans `Domain/Reclamations/` (avec `Code`, `Libelle`, `FileCibleDefautId`, `EstActif`, méthodes `Activer()`, `Desactiver()`, `MettreAJour()`).
  - Commandes `CreerCategorieReclamationCommand` et `ModifierCategoriesReclamationCommand` dans `Application/CustomerComplaint/`.
  - Interface Blazor interactive `AdministrationCategories.razor` permettant la création, la modification du libellé et l'activation/désactivation.
  - Sécurité protégée par `Permission.ComplaintCategoriesModifier`.
  - Suite de tests unitaire/composant existante `AdministrationCategoriesTests.cs`.
- **Code produit manquant** : **AUCUN**. Tout le comportement attendu est déjà implémenté depuis la remédiation post-audit F006.
- **Classification** : **A — DÉJÀ CONFORME, CERTIFICATION SEULE** (Suite TEST-SPEC `REC-CERT-02`).
- **Statut** : **RÉSOLU**.

### CLAR-04 : REC-03 — Orientation automatique vers le Support Opérationnel et instruction
- **Audit du code existant** :
  - `SoumettreReclamationCommand` résout la file cible configurée sur la catégorie (`categorie.FileCibleDefautId`) qui pointe par défaut vers la file "Support Opérationnel".
  - Les agents habilités de la file Support Opérationnel peuvent instruire, demander des compléments, joindre des documents, échanger dans le fil de commentaires et clôturer avec évaluation.
- **Code produit manquant** : **AUCUN**.
- **Classification** : **A — DÉJÀ CONFORME, CERTIFICATION SEULE** (Suite TEST-SPEC `REC-CERT-03`).
- **Statut** : **RÉSOLU**.

### CLAR-05 : SEC-08 — Double authentification (2FA) et stabilisation du harnais
- **Diagnostic** : Le code applicatif (`ActiverDoubleAuthentification.razor`, génération TOTP RFC 6238) est 100% conforme. En exécution concurrente xUnit, les différentes classes de test Web écrivent simultanément sur la même base SQL Server LocalDB `CitPlateformeTestsWeb`, causant des collisions d'état de session et d'utilisateurs.
- **Action harnais** : Isoler la collection de test Web xUnit (`[assembly: CollectionBehavior(DisableTestParallelization = true)]` ou collection partagée explicite) et garantir que le contexte de test réinitialise proprement son état.
- **Critère de certification** : Exécution de `ParcoursConnexion2FaTests` / `SecCert08DoubleAuthentificationTests` **10 fois consécutives avec 0 échec**.
- **Code produit altéré** : **AUCUN** (sauf si un bug de code produit venait à être formellement isolé).
- **Classification** : **B — CORRECTION HARNAIS + CERTIFICATION SEULE**.
- **Statut** : **RÉSOLU**.

### CLAR-06 : SOC-14 — Export de données tabulaire (CSV)
- **Portée** : Export tabulaire standardisé au format CSV des tickets visibles par l'utilisateur courant.
- **Colonnes exportées** : `NumeroTicket`, `Module`, `TypeDemande`, `Statut`, `FileCourante`, `DateCreationUtc`, `Priorite`, `ReferenceDossier`.
- **Format et encodage** : Format texte CSV avec séparateur point-virgule (`;`), encodage UTF-8 avec BOM (compatible Excel sans altération des caractères accentués francophones), format de date `yyyy-MM-dd HH:mm:ss`.
- **Cloisonnement & Sécurité** : Filtrage strict par société cliente pour les utilisateurs clients (CPT-04) et par périmètre de file pour les agents (SEC-03).
- **Nom du fichier** : `export-tickets-AAAA-MM-JJ-HHmm.csv`.
- **Intégration UI** : Bouton d'export déclenchant le téléchargement via `window.telechargerFichier` (`wwwroot/js/telechargements.js`).
- **Classification** : **C — DÉVELOPPEMENT MÉTIER CIBLÉ** (Query `ExporterTicketsCsvQuery` + intégration Blazor).
- **Statut** : **RÉSOLU**.

### CLAR-07 : COM-06 — Canaux configurables pour les relances automatiques
- **Portée** : L'événement de relance automatique (`RelanceAutomatique`) utilise la configuration des règles de diffusion (`IRegleDiffusionRepository`) et des préférences clients (`IPreferenceCanalRepository`).
- **Canaux supportés dans F012** : `InApp` et `Email`. (SMS et WhatsApp restent exclus tant que PC-16 n'est pas instruit).
- **Comportement en cas de canal désactivé** : Si un canal est désactivé, la notification est générée sur les canaux actifs restants sans échec global.
- **Journalisation** : Chaque tentative d'envoi par canal est tracée dans `EnvoisNotification` et auditée.
- **Classification** : **B — CORRECTION MINIMALE** (raccordement de la règle de diffusion `RelanceAutomatique` + tests).
- **Statut** : **RÉSOLU**.

### CLAR-08 : REC-SCE-14 — Scénario de recette transverse E2E (7 volets)
- **Volet 1 (Notifications)** : Notification émise sur les canaux configurés (InApp/Email) lors d'un événement métier.
- **Volet 2 (Lien sécurisé)** : Le lien de notification renvoie vers le ticket avec authentification préalable obligatoire.
- **Volet 3 (Recherche guichet)** : L'agent de guichet retrouve le ticket par numéro de suivi ou numéro de BL/booking (`RechercherTicketsGuichetQuery`).
- **Volet 4 (Rendez-vous)** : Prise de rendez-vous rattachée au ticket pour passage physique au bureau de facturation (`PrendreRendezVousCommand`).
- **Volet 5 (Dossier)** : Regroupement cohérent des tickets sous une même référence dossier (`Dossier.Numero`).
- **Volet 6 (Habilitations)** : Contrôle d'accès strict selon le rôle (l'agent ne peut agir que dans son périmètre autorisé).
- **Volet 7 (Journal d'audit)** : Traçage inviolable de l'ensemble des actions dans `EntreeJournalAudit`.
- **Livrable E2E** : `tests/CIT.Plateforme.Web.Tests/E2E/RecSce14NotificationsE2ETests.cs`.
- **Gain CDC** : **+0** (Validation de recette transverse).
- **Classification** : **E2E RECETTE TRANSVERSE**.
- **Statut** : **RÉSOLU**.

---

## 2. Périmètre Contractuel — 7 Exigences

| Réf CDC | Priorité | Famille | Description Littérale CDC |
|:---|:---:|:---:|:---|
| **PRI-02** | **Must** | Socle | *« La priorité peut influencer l'ordre du ticket dans la file, le SLA applicable, les alertes et les règles d'escalade. »* |
| **IMP-03** | **Must** | Billing Import | *« Le client reçoit ses numéros de ticket sur son profil et par e-mail. »* |
| **REC-02** | **Must** | Customer Complaint | *« Les catégories sont paramétrables et dynamiques (ajout/modification sans développement). »* |
| **REC-03** | **Must** | Customer Complaint | *« Les tickets sont orientés vers le Support Opérationnel, qui peut instruire, demander des compléments, joindre des documents, échanger avec le client et orienter vers les services concernés. »* |
| **SEC-08** | **Should** | Sécurité | *« Second facteur d'authentification pour les profils agents et administrateurs. »* |
| **SOC-14** | **Could** | Socle | *« Export des données. »* |
| **COM-06** | **Could** | Notifications | *« Relances automatiques paramétrables lorsque le ticket est en attente d'une action client au-delà d'un délai défini. »* |

---

## 3. Hors Périmètre (Anti-Scope Strict)

- ❌ **SEC-02** (RBAC au niveau des champs IES — bloqué par PC-05) ;
- ❌ **SEC-07** (Scan antivirus externe — bloqué par absence de fournisseur DSI) ;
- ❌ **IA-01 à IA-07** (Module Assistant IA — bloqué par PC-18) ;
- ❌ **ERR-01** (Gestion des brouillons — lot F013 dédié) ;
- ❌ **ENR-04 / SMS & WhatsApp réels** (bloqué par PC-16) ;
- ❌ **Production Readiness / Exploitation** (SEC-04, SEC-06, SEC-09, NFR-03 à NFR-10) ;
- ❌ **Refonte visuelle UI / Moteur BI complexe**.

---

## 4. Spécification des User Stories

### US1 — Déduction et influence automatique de la priorité (PRI-02, Must)
- **Acteur** : Système / Client / Agent
- **Description** : À la création d'un ticket, le système assigne le niveau de priorité par défaut configuré (`NiveauPriorite`). Les requêtes de consultation de file (`ConsulterFileQuery`) trient les tickets par priorité décroissante (`Rang`), puis par date de création croissante.
- **Critères d'acceptation** :
  - `CA-01.1` : Tout nouveau ticket reçoit automatiquement la priorité par défaut paramétrée.
  - `CA-01.2` : La liste des tickets retournée par `ConsulterFileQuery` affiche les tickets urgents/prioritaires avant les tickets normaux.
  - `CA-01.3` : La modification de priorité par un agent habilité est enregistrée et auditée (PRI-03).

### US2 — Communication du numéro de ticket Import par e-mail (IMP-03, Must)
- **Acteur** : Client / Moteur de notification
- **Description** : Lors de la soumission d'une demande Import multi-BL (`SoumettreDemandeImportCommand`), le système publie `TicketCree`, ce qui déclenche la création d'une `Notification` et d'un `EnvoiNotification` pour le canal `Email`.
- **Critères d'acceptation** :
  - `CA-02.1` : Pour chaque BL valide accepté, `TicketCree` est persisté dans `JournalEvenements`.
  - `CA-02.2` : Le moteur de distribution génère un `EnvoiNotification` avec `Canal = Email` à destination du client.
  - `CA-02.3` : Le numéro de ticket est disponible immédiatement sur le profil et dans la boîte de notifications.

### US3 — Certification des catégories de réclamations dynamiques (REC-02, Must)
- **Acteur** : Administrateur fonctionnel
- **Description** : L'administrateur gère les catégories de réclamation via l'interface dédiée sans développement ni redéploiement.
- **Critères d'acceptation** :
  - `CA-03.1` : Création dynamique d'une catégorie avec code unique et libellé.
  - `CA-03.2` : Modification du libellé et de la file cible par défaut.
  - `CA-03.3` : Désactivation d'une catégorie (empêche son utilisation pour de nouvelles demandes sans altérer l'historique).

### US4 — Certification de l'orientation Support Opérationnel et instruction (REC-03, Must)
- **Acteur** : Agent Support Opérationnel / Client
- **Description** : Les réclamations sont orientées automatiquement vers la file Support Opérationnel et peuvent être instruites de bout en bout.
- **Critères d'acceptation** :
  - `CA-04.1` : Toute nouvelle réclamation est affectée à la file Support Opérationnel par défaut.
  - `CA-04.2` : L'agent Support peut demander un complément d'information (statut « En attente client »).
  - `CA-04.3` : Le client dépose son complément et le dossier revient en traitement chez l'agent Support.

### US5 — Stabilisation du harnais et certification 2FA (SEC-08, Should)
- **Acteur** : Agent / Administrateur
- **Description** : La validation du second facteur TOTP s'exécute de manière déterministe et stable.
- **Critères d'acceptation** :
  - `CA-05.1` : Connexion mot de passe redirige vers la saisie du code 2FA.
  - `CA-05.2` : Code TOTP valide autorise l'accès complet.
  - `CA-05.3` : Code TOTP invalide est rejeté avec message explicite.
  - `CA-05.4` : La suite de test 2FA s'exécute 10 fois consécutivement avec 100% de succès.

### US6 — Export tabulaire au format CSV (SOC-14, Could)
- **Acteur** : Client / Agent / Superviseur
- **Description** : L'utilisateur télécharge la liste de ses tickets au format CSV dans le respect strict de son périmètre d'autorisation.
- **Critères d'acceptation** :
  - `CA-06.1` : L'export génère un flux CSV UTF-8 (avec BOM, séparateur point-virgule).
  - `CA-06.2` : Le flux inclut : Numéro, Module, Type, Statut, Date, Priorité, Référence.
  - `CA-06.3` : Un client ne peut exporter que les tickets de sa propre société cliente (CPT-04 / SEC-03).
  - `CA-06.4` : Le déclenchement s'effectue via le composant Blazor et `telechargements.js`.

### US7 — Canaux configurables pour les relances automatiques (COM-06, Could)
- **Acteur** : Administrateur fonctionnel / Système
- **Description** : Les relances automatiques diffusent les notifications sur les canaux configurés actifs (InApp, Email).
- **Critères d'acceptation** :
  - `CA-07.1` : L'événement `RelanceAutomatique` génère des notifications sur les canaux actifs paramétrés.
  - `CA-07.2` : La désactivation d'un canal dans les préférences n'empêche pas l'envoi sur les autres canaux.

### US8 — Recette transverse E2E REC-SCE-14
- **Acteur** : Parcours usager / agent / guichet
- **Description** : Scénario transverse E2E rejouant les 7 volets contractuels.
- **Critères d'acceptation** :
  - `CA-08.1` : Le test E2E `RecSce14NotificationsE2ETests.cs` valide l'enchaînement complet des 7 volets sans mock destructif.

---

## 5. Fiches TEST-SPEC Nominatives

| Identifiant TEST-SPEC | Exigence CDC | Classe de test cible | Niveau de test | Preuve pour Closure Review |
|:---|:---:|:---|:---:|:---|
| **PRI-CERT-01** | `PRI-02` (Must) | `PriCert01PrioriteTests.cs` | Application / Domain | Calcul automatique + ordre de file vérifié |
| **IMP-CERT-03** | `IMP-03` (Must) | `ImpCert03NotificationEmailTests.cs` | Application / Integration | Événement `TicketCree` + `EnvoiNotification(Email)` |
| **REC-CERT-02** | `REC-02` (Must) | `RecCert02CategoriesTests.cs` | Application / Web | CRUD dynamique catégories sans code |
| **REC-CERT-03** | `REC-03` (Must) | `RecCert03OrientationSupportTests.cs` | Application | Aiguillage Support Opérationnel + instruction |
| **SEC-CERT-08** | `SEC-08` (Should) | `SecCert08DoubleAuthentificationTests.cs` | Web / Integration | 10 runs consécutifs 2FA sans échec |
| **SOC-CERT-14** | `SOC-14` (Could) | `SocCert14ExportCsvTests.cs` | Application / Web | Flux CSV conforme + cloisonnement prouvé |
| **COM-CERT-06** | `COM-06` (Could) | `ComCert06CanauxRelanceTests.cs` | Application | Diffusion relance multicanale paramétrable |
| **REC-SCE-14-E2E** | REC-SCE-14 | `RecSce14NotificationsE2ETests.cs` | Web E2E | Parcours complet 7 volets contractuels |

---

## 6. Synthèse des Modifications Attendues

| Exigence | Nature du Travail | Périmètre des Fichiers Concernés |
|:---|:---:|:---|
| **PRI-02** | **Correction minimale** | `Soumettre...Command.cs`, `ConsulterFileQuery.cs` |
| **IMP-03** | **Correction minimale** | `SoumettreDemandeImportCommand.cs` |
| **REC-02** | **Certification seule** | Tests uniquement (`RecCert02CategoriesTests.cs`) |
| **REC-03** | **Certification seule** | Tests uniquement (`RecCert03OrientationSupportTests.cs`) |
| **SEC-08** | **Harnais de test** | Configuration collection xUnit dans `Web.Tests` |
| **SOC-14** | **Développement ciblé** | `ExporterTicketsCsvQuery.cs`, composant Blazor |
| **COM-06** | **Correction minimale** | Seeding / configuration règle de relance |
| **REC-SCE-14** | **Recette E2E** | `RecSce14NotificationsE2ETests.cs` |

---

## 7. Traçabilité et Couverture CDC

```
BASELINE AVANT F012 : 157 / 188 (83,51 %)

EXIGENCES CIBLÉES (7) :
- PRI-02 : Must (+1)
- IMP-03 : Must (+1)
- REC-02 : Must (+1)
- REC-03 : Must (+1)
- SEC-08 : Should (+1)
- SOC-14 : Could (+1)
- COM-06 : Could (+1)
- REC-SCE-14 : Recette transverse (+0)

GAIN MAXIMAL PROJETÉ : +7
BASELINE CIBLE F012  : 164 / 188 (87,23 %) — NON ACQUISE AVANT CLOSURE REVIEW
```

---

## 8. Definition of Done (DoD)

1. **7 suites TEST-SPEC nominatives 100% vertes**.
2. **1 scénario E2E REC-SCE-14 100% vert**.
3. **10 runs consécutifs 2FA stables sans échec**.
4. **100% de la baseline existante (858 tests) préservée**.
5. **0 warning de compilation, 0 pending model change, 0 migration**.
6. **0 fournisseur externe requis**.
7. **Rapports Spec Kit complets et commit sélectif exclusif**.
