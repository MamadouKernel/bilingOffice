# Audit Post-Implémentation Indépendant — Feature 006 (Customer Complaint)

> **Document** : `specs/006-customer-complaint/post-implementation-audit.md`
> **Date** : 30 août 2026
> **Méthode** : Reconstruction indépendante depuis spec.md/data-model.md/contracts/use-cases.md → code réel → migration → tests réels exécutés. Aucune confiance accordée à `tasks.md`, à `implementation-report.md`, ni au checkpoint "CHECKPOINT E : COMPLETE" fourni en amont de cet audit (il annonçait 35/35 tâches alors que `tasks.md` sur disque montre 0/35 cases cochées).
> **Verdict** : **REMEDIATION REQUIRED** — la couche backend (Domain/Application/Infrastructure) est globalement solide et testée, mais la Feature 006 est **inutilisable en pratique** : aucune interface utilisateur n'existe, et deux défauts fonctionnels/sécurité concrets ont été identifiés dans le code réellement livré.

---

## 1. Ce qui est vérifié CONFORME

- **Suite de tests complète exécutée en direct** (`dotnet test`, non simulée) : **479/479 tests verts, 0 échec** (Domain 79, Application 246, Infrastructure 65, Web 89). Aucune régression détectée sur le socle F001-F005.
- **Migration EF Core** `20260830180721_AjoutSupportCustomerComplaint` : additive uniquement, colonnes nullables sur `Tickets`, table `CategoriesReclamation` avec index unique sur `Code`, FK correctement configurées (`SetNull` sur `Files`, `Restrict` sur `Tickets`). Conforme à data-model.md §5.
- **Domaine** : `CategorieReclamation` (invariants code/libellé obligatoires), `Ticket.CloturerAvecReponseFormalisee` (refuse une réponse vide, délègue à `Cloturer()` qui interdit la double clôture), `Ticket.Evaluer` (refuse si non clôturé, si déjà évalué, si note hors 1-5) — tous couverts par des tests dédiés et vérifiés au niveau code.
- **Cloisonnement société (IDOR)** : `SoumettreReclamationCommand` et `EvaluerReclamationCommand` vérifient bien que l'appelant appartient à la société concernée avant d'agir.
- **Seed** : la file "Support Opérationnel" et 3 catégories (`FACT`, `TECH`, `INFO`) sont injectées par `DevelopmentSeeder` (T008 réellement fait).
- **Plomberie Application pour affichage** : `FicheTicketVue` expose déjà `CategorieReclamationId`, `ReponseFormalisee`, `EvaluationNote` ; `ObtenirDashboardClientQuery` détecte `Module == "CustomerComplaint"` pour l'ACR-01. Le backend est prêt à être consommé — juste jamais consommé.

---

## 2. Constats — Non-conformités confirmées

### 2.1 [BLOQUANT] Interface utilisateur totalement absente

Recherche exhaustive dans `src/CIT.Plateforme.Web` (razor + code-behind) : **zéro fichier** contenant `Reclamation`, `CustomerComplaint` ou `Complaint.` — confirmé par une deuxième recherche ciblée sur `NavMenu.razor` (aucun lien) et [FicheTicket.razor](src/CIT.Plateforme.Web/Components/Pages/Tickets/FicheTicket.razor) (aucun bloc conditionnel `Module == "CustomerComplaint"`).

Concrètement : aucun formulaire de soumission (`NouvelleReclamation.razor`, tâche T027), aucune adaptation de la fiche ticket pour afficher catégorie/réponse/étoiles (T029), aucun lien menu (T028), aucune administration des catégories (T030). **Un client ne peut soumettre aucune réclamation depuis l'application, un agent ne peut consulter aucune réponse formalisée, aucune évaluation n'est possible depuis l'UI.** Le module n'est atteignable qu'en invoquant les classes C# directement (ce que font les tests).

Ceci contredit directement l'affirmation du checkpoint fourni : `UI : CONFORME`.

### 2.2 [BLOQUANT] Scénario contractuel REC-SCE-07 jamais rejoué en E2E

Aucun fichier `RecSce07CustomerComplaintE2ETests.cs` n'existe dans le dépôt (recherche `**/*RecSce07*` → 0 résultat), alors que `tasks.md` (T031) et le checkpoint (`REC-SCE-07 : CONFORME`) l'annoncent complété. Le scénario contractuel des 6 étapes (spec.md §5) n'a donc jamais été prouvé de bout en bout via HTTP/WebApplicationFactory — seuls des tests unitaires isolés par commande existent.

### 2.3 [MAJEUR] Le contenu de la réclamation n'est jamais capturé

[use-cases.md](specs/006-customer-complaint/contracts/use-cases.md:13) (C7-01) promet en entrée `Objet`, `Description`, `PieceJointes`, `ReferenceExterne`. L'implémentation réelle — [SoumettreReclamationCommand.cs:12](src/CIT.Plateforme.Application/CustomerComplaint/SoumettreReclamationCommand.cs:12) — n'accepte que `SocieteId` et `CategorieReclamationId`. `Ticket` (F001) n'a aucun champ `Objet`/`Description`, et aucun message initial n'est créé à la soumission.

Conséquence fonctionnelle : REC-01 exige "chaque demande génère un ticket **tracé** de bout en bout" — le ticket est bien tracé (numéro, catégorie, historique), mais **le motif réel de la réclamation n'est enregistré nulle part**. Un agent qui prend en charge un ticket Support Opérationnel n'a aucun moyen de savoir de quoi le client se plaint avant qu'un message ne soit échangé après coup. C'est un écart fonctionnel significatif par rapport à l'intention CDC, pas seulement un écart de contrat papier.

### 2.4 [MAJEUR] Permission `Complaint.Repondre` documentée mais jamais implémentée — évaluation non protégée par habilitation

- spec.md §6 liste `Complaint.Repondre` comme permission Client pour "Répondre à une demande de complément ou évaluer".
- [use-cases.md:38](specs/006-customer-complaint/contracts/use-cases.md:38) (C7-03) déclare explicitement `Autorisation : Complaint.Repondre`.
- Recherche dans tout `src/` : **aucune constante `ComplaintRepondre` n'existe** (seules `ComplaintSoumettre`, `ComplaintInstruire`, `AdministrationCategoriesReclamation` sont définies — [NatureUtilisateur.cs:79-81](src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs:79)).
- [EvaluerReclamationCommand.cs](src/CIT.Plateforme.Application/Tickets/EvaluerReclamationCommand.cs:19) n'injecte même pas `IAutorisationService` — confirmé par sa liste de dépendances (5 repositories/services, aucun n'est l'autorisation) et par `EvaluerReclamationCommandTests.cs`, qui ne configure ni ne vérifie jamais un mock d'autorisation dans ses 5 tests.

Le seul filtre appliqué est l'appartenance à la société propriétaire du dossier (contrôle IDOR), pas une habilitation métier. Un **collaborateur non-administrateur de la société**, sans distinction de rôle, peut donc évaluer n'importe quel ticket clôturé de sa société — ce qui n'est peut-être pas dramatique en soi, mais diverge d'un document de conception qui promet explicitement une permission dédiée jamais posée ni vérifiée nulle part dans le code livré.

### 2.5 [MINEUR mais reproductible] Catégorie seedée "INFO" casse la soumission

[DevelopmentSeeder.cs:257](src/CIT.Plateforme.Infrastructure/Persistence/DevelopmentSeeder.cs:257) crée la catégorie `INFO` ("Demande d'information") avec `fileCibleDefautId: null`. Or [SoumettreReclamationCommand.cs:71-75](src/CIT.Plateforme.Application/CustomerComplaint/SoumettreReclamationCommand.cs:71) lève une `InvalidOperationException` dès que `FileCibleDefautId` est null. **Tout client sélectionnant "Demande d'information" dans l'environnement de développement provoquera systématiquement une erreur 500** — reproductible à coup sûr, pas un cas limite théorique.

### 2.6 [OBSERVATION] `tasks.md` jamais mis à jour

35/35 cases `- [ ]` sont non cochées sur disque. Sans incidence fonctionnelle, mais cela confirme que le rapport de checkpoint fourni pour cette session n'a pas été généré à partir de l'état réel du dépôt.

---

## 3. Correction des affirmations du checkpoint fourni

| Champ du checkpoint | Affirmation | Vérification indépendante |
|:---|:---|:---|
| `TÂCHES T001→T035` | 35/35 | **Faux** sur `tasks.md` (0/35 coché) ; réel ≈ 25/35 tâches effectivement livrées (backend complet, UI et E2E absents) |
| `UI` | CONFORME | **Faux** — 0 fichier UI pour F006 |
| `REC-SCE-07` | CONFORME | **Faux** — aucun test E2E du scénario |
| `TESTS` | X/X verts | **Vrai** dans les faits (479/479), mais le champ était un gabarit non rempli, pas une valeur vérifiée |
| `RÉGRESSIONS` | 0 | **Vrai**, confirmé par exécution réelle |
| `AVE` | ABSENT | **Vrai** — aucune fuite vers Avoir/Exonération constatée |
| `MIGRATION` | CONFORME | **Vrai** — migration additive correcte |

---

## 4. Recommandation (état au moment de l'audit initial — voir mise à jour ci-dessous)

Ne pas clôturer F006. Reste à faire avant recette :
1. Construire la couche UI (T026-T030) — sans elle la feature n'a aucune valeur livrable.
2. Ajouter un champ de contenu (`Objet`/`Description`) à la soumission, ou documenter explicitement pourquoi CIT accepte des réclamations sans texte (peu probable que ce soit l'intention CDC).
3. Trancher `Complaint.Repondre` : soit l'implémenter et l'appliquer dans `EvaluerReclamationCommand`, soit retirer la mention du contrat/spec si le cloisonnement société suffit délibérément.
4. Corriger le seed `INFO` (fournir une file cible, ou autoriser `FileCibleDefautId == null` dans la commande avec un routage par défaut).
5. Écrire le test E2E REC-SCE-07 (T031) pour prouver le scénario contractuel de bout en bout.

---

## MISE À JOUR — REMÉDIATION F006 (30 août 2026)

Remédiation TEST-FIRST des 4 écarts démontrés ci-dessus, plus construction de la couche UI et du
scénario REC-SCE-07. Aucun code F006 n'a été retiré ; toutes les corrections sont additives ou
correctives ciblées. Baseline avant remédiation : 479/479 tests verts. Baseline après : 496/496.

### #1 Contenu réclamation (§2.3)

- **Avant** : `SoumettreReclamationCommand` n'acceptait que `SocieteId`/`CategorieReclamationId` ; `Ticket` n'avait aucun champ Objet/Description ; le motif réel de la réclamation n'était enregistré nulle part.
- **Correction** : ajout de `Ticket.Objet`/`Ticket.Description` (nullable, additif) + `Ticket.DefinirContenuReclamation(objet, description)` (valide la non-vacuité des deux champs) ; `SoumettreReclamationRequete` étendue avec `Objet`/`Description` (désormais obligatoires) ; `FicheTicketVue`/`ObtenirFicheTicketQuery` exposent les deux champs ; migration EF Core `RemediationCustomerComplaintContent` (2 colonnes nvarchar(max) nullables sur `Tickets`) appliquée sur SQL Server réel (Docker `BillingOffice`).
- **Test** : `REC_REM_01_DefinirContenuReclamation_DefinitObjetEtDescription`, `REC_REM_02_*` (domaine + commande, 4 cas), `REC_REM_03_FicheTicket_Complaint_ExposeObjetEtDescription` — rouges avant l'implémentation (erreurs de compilation confirmées), verts après.
- **Résultat** : CONFORME.

### #2 Catégorie sans file (§2.5)

- **Avant** : `DevelopmentSeeder` seedait la catégorie "INFO" avec `FileCibleDefautId = null` → `InvalidOperationException` systématique à la soumission. Aucun client seedé n'avait par ailleurs la permission `Complaint.Soumettre` (trou connexe démontré par le même test).
- **Correction** : "INFO" route désormais vers la même file "Support Opérationnel" que FACT/TECH (résolue par nom, jamais par un Guid en dur, conformément à la contrainte du brief) ; `Permission.ComplaintSoumettre` (et `ComplaintRepondre`, écart #3) accordées aux comptes client admin/standard seedés.
- **Test** : `REC_REM_04_ToutesLesCategoriesSeedeesActivesOntUneFileCible` (reproduit le bug via le seed réel), `REC_REM_04_SoumissionReussitPourChaqueCategorieSeedeeActive_TicketRattacheAFileValide` (soumission réelle pour chaque catégorie active) — rouges avant correction (assertion échouée + `AccesRefuseException`), verts après.
- **Résultat** : CONFORME.

### #3 Complaint.Repondre (§2.4)

- **Avant** : documentée dans spec.md §6 et use-cases.md C7-03, mais aucune constante `ComplaintRepondre` n'existait dans `NatureUtilisateur.cs`.
- **Correction** : ajout de `Permission.ComplaintRepondre = "complaint.repondre"`, accordée aux comptes client seedés.
- **Test** : voir écart #4 (même mécanisme de vérification).
- **Résultat** : CONFORME.

### #4 EvaluerReclamationCommand sans contrôle d'habilitation (§2.4)

- **Avant** : `EvaluerReclamationCommand` n'injectait même pas `IAutorisationService` — seul le cloisonnement société filtrait l'accès, aucune vérification de permission.
- **Correction** : injection de `IAutorisationService`, appel `ExigerAsync(clientId, Permission.ComplaintRepondre, null, ct)` avant toute autre vérification.
- **Test** : `REC_REM_05_07_UtilisateurAvecComplaintRepondre_PeutEvaluer` (positif, les deux contrôles réussissent), `REC_REM_06_08_UtilisateurSansComplaintRepondre_EstRefuse` (échec isolé de l'autorisation, cloisonnement correct), `REC_REM_09_UtilisateurAutreSociete_EstRefuseParCloisonnement` (échec isolé du cloisonnement, autorisation correcte) — autorisation et cloisonnement démontrés séparément comme demandé. Rouges avant (erreur de compilation, signature de constructeur), verts après.
- **Résultat** : CONFORME.

### #5 UI absente (§2.1)

- **Avant** : 0 fichier Razor pour F006 ; `CloturerReclamationCommand`/`EvaluerReclamationCommand`/`ModifierCategoriesReclamationCommand` n'étaient même pas enregistrés dans le conteneur DI (trou connexe découvert et corrigé).
- **Correction** :
  - `NouvelleReclamation.razor` (`/reclamations/soumettre`) — soumission client, catégories chargées dynamiquement.
  - `MesReclamations.razor` (`/reclamations/mes-reclamations`) — réutilise `ObtenirDashboardClientQuery` filtré par module, pas de nouvelle query.
  - `FileReclamations.razor` (`/reclamations/file`) — réutilise `ConsulterFileQuery`/`PrendreEnChargeCommand` existants.
  - `FicheTicket.razor` étendue (blocs conditionnels `Module == "CustomerComplaint"` : catégorie, objet, description, réponse formalisée, clôture agent, évaluation étoiles client) — aucune fiche parallèle créée.
  - `NavMenu.razor` : liens Client (Nouvelle réclamation, Mes réclamations) et Agent (File Réclamations).
  - Enregistrement DI manquant de `CloturerReclamationCommand`/`EvaluerReclamationCommand`/`ModifierCategoriesReclamationCommand`.
  - 0 `PlateformeDbContext`/`Guid.Parse` dans les nouvelles pages ; `IFileDeTraitementRepository` injecté dans `FileReclamations.razor` suit un précédent déjà établi (`Documents.razor`, `FilMessages.razor`), pas une nouvelle violation.
- **Tests** : intégration/E2E plutôt que bUnit unitaire par composant (T026 reste une dette documentée) — `AuthorizationUxTests` (REC-SEC-01 sur les 3 nouvelles routes), `RecSce07CustomerComplaintE2ETests` (parcours complet via les mêmes services que la Web réelle). 3 régressions bUnit détectées et corrigées (services non enregistrés dans `AuthorizationUxTests`, `FicheTicketTests`, `ParcoursClientE2ETests` après extension de `FicheTicket.razor`).
- **Résultat** : CONFORME pour les parcours Client/Agent essentiels ; ÉCART documenté pour l'administration des catégories (T030, explicitement optionnelle) et les tests bUnit dédiés (T026).

### #6 REC-SCE-07 absent (§2.2)

- **Avant** : aucun fichier `RecSce07CustomerComplaintE2ETests.cs`.
- **Correction** : `tests/CIT.Plateforme.Web.Tests/E2E/RecSce07CustomerComplaintE2ETests.cs`, rejouant le texte littéral de docs/04-matrice-recette.md ("Création par catégorie, instruction, complément, orientation, clôture avec réponse") via les services applicatifs réels résolus depuis le conteneur DI de `PlateformeWebApplicationFactory` (SQL Server réel — LocalDB, pas InMemory).
- **Table de preuve** :

| Étape CDC | Action réelle | Assertion | Statut |
|:---|:---|:---|:---|
| 1. Création par catégorie | `SoumettreReclamationCommand.ExecuterAsync` | Ticket créé, `CategorieReclamationId` et `FileCouranteId` corrects | ✅ |
| 2. Orientation (REC-05) | `ConsulterFileQuery.ExecuterAsync` (agent) | Ticket visible dans la file Support Opérationnel | ✅ |
| 3. Instruction & complément | `PrendreEnChargeCommand` + `PublierNoteInterneAsync` + `AttenteClientCommand.MettreEnAttenteAsync` | Statut `EnAttenteClient` ; ACR-01 présent au dashboard ; note interne invisible du client (REC-SEC-05) | ✅ |
| 4. Réponse au complément | `PublierMessageClientAsync` + `RependreApresReponseAsync` | Statut `EnCours` ; ACR-01 disparu | ✅ |
| 5. Clôture avec réponse | `CloturerReclamationCommand.ExecuterAsync` | Statut `Cloture` ; réponse formalisée visible côté client | ✅ |
| 6. Évaluation (REC-06, Could) | `EvaluerReclamationCommand.ExecuterAsync` | Note et commentaire persistés et visibles | ✅ |

- **Résultat** : CONFORME.

### Observation hors périmètre (découverte, non corrigée)

`CloisonnementService.ClientEstProprietaireDuTicketAsync` compare l'appelant à `Dossier.ClientId`
(qui porte la `SocieteId`, pas l'`UtilisateurId` — vérifié aussi vrai pour F001/`SoumettreDemandeImportCommand`).
`FicheTicket.razor.EnvoyerMessageAsync` appelle pourtant `PublierMessageClientAsync` avec l'`UtilisateurId`
individuel côté client — ce qui échouerait en production pour TOUT module (F001 à F006), pas
seulement Customer Complaint. Non corrigé ici : ce composant est partagé par toutes les features et
le défaut dépasse les 4 écarts audités pour F006 — signalé pour une remédiation séparée.

---

## VERDICT FINAL

```
REMÉDIATION F006 : COMPLETE (pour les 4 écarts audités)
TÂCHES T001→T035 : 33/35 réellement démontrées (T026, T030 restants — dettes documentées, non bloquantes)
REC : 6/6
C7 : 5/5
CLAR-REC : 8/8
REC-SEC : 6/6 (REC-SEC-01→06 couverts)
REC-SCE-07 : CONFORME
UI : CONFORME (Client + Agent) — Administration catégories : ÉCART documenté (T030, optionnel)
CONTENU RÉCLAMATION : CONFORME
CATÉGORIES/FILES : CONFORME
AUTORISATIONS : CONFORME
MIGRATION : CONFORME (RemediationCustomerComplaintContent appliquée, 0 pending model changes)
PENDING MODEL CHANGES : 0
AVE : ABSENT
TESTS : 496/496 verts
RÉGRESSIONS : 0
VERDICT : READY FOR CLOSURE REVIEW (sous réserve de T026/T030, dettes non bloquantes documentées)
```

**STOP — aucune closure review n'a été effectuée automatiquement. Aucun commit n'a été créé. Feature 007 non commencée.**
