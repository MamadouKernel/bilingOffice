# Research & Décisions Architecturales — Feature 012 (Remédiation Dettes Métier, Recette Transverse & Clôture Réserves)

> **Document** : `specs/012-remediation-dettes-recette-transverse/research.md`  
> **Feature** : F012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves  
> **Date** : 1er septembre 2026  
> **Statut** : DÉCISIONS ARCHITECTURALES VALIDÉES (Phase 0)

---

## 1. Inventaire et Classification Code-First (D0)

Chaque exigence candidate a fait l'objet d'une inspection préalable dans le code source existant :

| Exigence CDC | Priorité | Famille | Classification Code-First | Diagnostic / Justification |
|:---|:---:|:---:|:---:|:---|
| **PRI-02** | **Must** | Socle | **R3 — Correction minimale** | L'entité `NiveauPriorite`, la commande `ModifierPrioriteCommand` et le champ `Ticket.PrioriteId` existent. Manque : l'attribution initiale par défaut à la création et l'ordonnancement par priorité dans `ConsulterFileQuery`. |
| **IMP-03** | **Must** | Billing Import | **R3 — Correction minimale** | `SoumettreDemandeImportCommand.cs` crée les tickets et persiste l'audit, mais omet l'appel `PublierEvenementAsync("TicketCree", ...)`. F009 est prêt à consommer l'événement. |
| **REC-02** | **Must** | Customer Complaint | **R1 — Preuve existante suffisante** | L'entité `CategorieReclamation`, les commandes CRUD, la page `AdministrationCategories.razor` et la suite `AdministrationCategoriesTests.cs` existent déjà à 100% (remédiation F006). Zéro code produit requis. |
| **REC-03** | **Must** | Customer Complaint | **R1 — Preuve existante suffisante** | `SoumettreReclamationCommand` résout dynamiquement la file cible ("Support Opérationnel") et le cycle d'instruction (demande de complément, fil de discussion) est déjà complet. Zéro code produit requis. |
| **SEC-08** | **Should** | Sécurité | **R3 — Correction harnais de test** | Le composant `ActiverDoubleAuthentification.razor` et la cinématique TOTP sont 100% conformes. L'instabilité provient de la génération de token dans le test et de la concurrence xUnit sur la base LocalDB partagée. |
| **SOC-14** | **Could** | Socle | **R4 — Développement ciblé** | `window.telechargerFichier` existe dans `wwwroot/js/telechargements.js`. Manque : la Query applicative `ExporterTicketsCsvQuery` (génération CSV UTF-8 BOM) et son raccordement UI. |
| **COM-06** | **Could** | Notifications | **R3 — Correction minimale** | L'événement `RelanceAutomatique` existe dans F001/F009. Manque : l'association explicite de la règle de diffusion multicanale (InApp + Email) dans le seeder/registre et la suite de test dédiée. |
| **REC-SCE-14** | — | Recette | **R2 — Tests complémentaires (E2E)** | Les 7 volets contractuels fonctionnent unitairement mais nécessitent un scénario transverse unifié `RecSce14NotificationsE2ETests.cs`. Gain CDC = +0. |

---

## 2. Décisions d'Architecture (D1 à D9)

### D1 — PRI-02 : Déduction initiale de priorité et ordonnancement de file
- **Principe** : Aucune matrice de scoring complexe ou SLA inventé.
- **Règle d'attribution** : À la création d'un ticket, le système résout le `NiveauPriorite` par défaut configuré en base (rang 1 / "Normale").
- **Ordonnancement** : `ConsulterFileQuery` ordonne les tickets de la file par `Priorite.Rang DESC`, puis `DateCreationUtc ASC`.
- **Impact fichiers** : `CIT.Plateforme.Application/Tickets/CreerTicketCommand.cs`, `CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs`, `CIT.Plateforme.Application/Tickets/ConsulterFileQuery.cs`.

### D2 — IMP-03 : Raccordement de l'événement `TicketCree` dans Billing Import
- **Principe** : Respect strict du modèle événementiel F009 (consommation du `JournalEvenements` par `DistribuerNotificationsUseCase`).
- **Action** : Dans `SoumettreDemandeImportCommand.cs`, après la création du ticket, ajout de :
  ```csharp
  await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"{Module}\"}}", ct).ConfigureAwait(false);
  ```
- **Fournisseur externe** : Aucun serveur SMTP réel n'est requis ; la persistance dans `EnvoiNotification(Canal = Email)` valide la conformité CDC.

### D3 — REC-02 & REC-03 : Certification stricte sans duplication de code
- **Principe** : Ne réécrire aucune logique métier ni page Blazor existante.
- **Action** : Écriture de suites TEST-SPEC dédiées (`RecCert02CategoriesTests.cs` et `RecCert03OrientationSupportTests.cs`) rattachant nominativement les tests aux exigences CDC.

### D4 — SEC-08 : Stratégie d'assainissement et stabilisation du harnais de test 2FA
- **Cause d'instabilité isolée** :
  1. La méthode de test calculait le TOTP avec un helper `Base32Decode` / HMAC custom sensible aux fenêtres de temps.
  2. xUnit exécutait `ParcoursConnexion2FaTests` en parallèle d'autres suites Web écrivant sur la même base LocalDB `CitPlateformeTestsWeb`.
- **Résolution** :
  1. Utilisation de l'API standard `UserManager.GenerateTwoFactorTokenAsync(utilisateur, TokenOptions.DefaultAuthenticatorProvider)` dans les tests.
  2. Configuration de l'isolation xUnit dans `CIT.Plateforme.Web.Tests` (`[Collection("PlateformeWebCollection")]` ou désactivation de la parallélisation de collection).
  3. Validation formelle sur 10 exécutions consécutives à 100% de succès.

### D5 — SOC-14 : Conception de l'export CSV tabulaire léger
- **Format** : Texte CSV, encodage `UTF-8 avec BOM` (permet l'ouverture sans corruption d'accents dans Excel et LibreOffice), séparateur point-virgule (`;`).
- **Structure** :
  `NumeroTicket;Module;TypeDemande;Statut;FileCourante;DateCreationUtc;Priorite;ReferenceDossier`
- **Sécurité et Cloisonnement** :
  - Client : extraction limitée strictement aux tickets de sa société cliente (`ClientId == utilisateur.SocieteId`).
  - Agent / Superviseur : extraction limitée aux tickets de sa file ou de son périmètre d'autorisation.
- **Composant UI** : Bouton d'export réutilisant `telechargerFichier` (`wwwroot/js/telechargements.js`).

### D6 — COM-06 : Relances automatiques multicanales (InApp + Email)
- **Principe** : Réutilisation directe du moteur de notification F009.
- **Règles de diffusion** : Enregistrement des règles pour `RelanceAutomatique` avec `CanalNotification.InApp` et `CanalNotification.Email`.
- **Exclusion** : SMS et WhatsApp restent exclus tant que le point de clarification **PC-16** n'est pas instruit.

### D7 — REC-SCE-14 : Scénario transverse E2E sans mock destructif
- **Structure** : Suite unique `tests/CIT.Plateforme.Web.Tests/E2E/RecSce14NotificationsE2ETests.cs` démontrant les 7 volets contractuels :
  1. *Notification multicanale*
  2. *Lien sécurisé vers le ticket*
  3. *Recherche guichet*
  4. *Rendez-vous bureau facturation*
  5. *Regroupement en dossier*
  6. *Contrôle des habilitations*
  7. *Journal d'audit inviolable*
- **Gain CDC** : +0 exigence (scénario transverse d'intégration).

### D8 — Invariant de Persistance et Modèle de Données
- **0 nouvelle entité dans Domain**.
- **0 nouvelle table dans la base de données**.
- **0 migration Entity Framework Core**.

### D9 — Stratégie de Commit Sélectif
- Exclusion stricte de toute modification préexistante dans le worktree (`App.razor`, thèmes UI, etc.).
- Commit exclusif des fichiers de spécification, de code applicatif F012 et des nouvelles suites TEST-SPEC.
