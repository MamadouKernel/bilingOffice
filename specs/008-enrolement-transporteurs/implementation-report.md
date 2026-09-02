# Rapport d'Implémentation — Feature 008 (Enrôlement Transporteurs)

> **Document** : `specs/008-enrolement-transporteurs/implementation-report.md`
> **Date** : 31 août 2026
> **Exécution** : `/speckit.implement` sur `tasks.md` (T001→T080), discipline TEST-FIRST stricte.

---

## 1. Baseline

**Avant remédiation** (préflight réel, PowerShell natif) : Domain 82/82 · Application 263/263 · Infrastructure 65/65 · Web 115/116 (1 échec pré-existant, hors périmètre F008 — `DashboardAgentTests`, cause = race de rendu asynchrone dans un test bUnit, sans lien avec la refonte UI en cours ni avec F008).

**Remédiation ciblée** (avant toute tâche F008, validée par l'utilisateur) : ajout d'un `cut.WaitForState(...)` dans `DashboardAgentTests.cs` (patron déjà en usage ailleurs). Classification **CAS B**. `DashboardAgent.razor` non modifié.

**Baseline remédiée** : **526/526**, 0 warning, 0 pending model change. Reconfirmée réellement en tout début de session d'implémentation (§1 des instructions) : identique, 526/526.

**Baseline finale, après implémentation complète de F008** : **595/595**, 0 échec, 0 warning, 0 pending model change (Domain 86 + Application 309 + Infrastructure 67 + Web 133).

## 2. Tâches

80/80 tâches exécutées (T001-T080), à l'exception de **T080** (parcours manuel `quickstart.md` sur application démarrée) — non exécutée dans cette session, dette mineure documentée en §22. Détail complet : `tasks.md`.

## 3. Domaine

Une seule extension : `Ticket.DefinirInformationsEnrolement(string objet, string description)` (`src/CIT.Plateforme.Domain/Tickets/Ticket.cs`), réutilisant les colonnes génériques `Objet`/`Description` (ajoutées en F006, jamais spécifiques à Reclamation dans leur définition EF). **Aucune nouvelle propriété, aucune nouvelle entité, aucune migration.** `StatutTicket` inchangé — convocation/validation restent des jalons d'historique, pas des états (research.md D6, confirmé pendant l'implémentation).

## 4. Permissions

Deux constantes ajoutées à `Domain.Securite.Permission` : `EnrolementSoumettre` (`"enrolement.soumettre"`), `EnrolementInstruire` (`"enrolement.instruire"`, permission bundlée unique pour l'Agent Gate — mirroring `ComplaintInstruire`, choix justifié dans research.md D3 face au modèle granulaire de Billing Import/Export). Aucun nouveau rôle Identity — RBAC uniquement via `HabilitationUtilisateur` (mécanisme F002 générique, inchangé).

## 5. File Agent Gate

`FileDeTraitement.Creer("Enrolement", "Agent Gate")` — donnée de configuration pure (`DevelopmentSeeder.SeedEnrolementAsync`), réutilisant `FileDeTraitement` (F001) sans extension. Aucun Guid codé en dur — résolution systématique par `(Module, Nom)`.

## 6. Soumission (C8-01)

`SoumettreEnrolementCommand` (nouvelle) : vérifie `EnrolementSoumettre`, valide `Objet`/`Description` non vides, résout la file par nom, crée `Dossier(ClientId=appelantId, AuteurId=null)` + `Ticket(Module="Enrolement", ...)`, appelle `DefinirInformationsEnrolement`, historique + audit + événement. Identité toujours dérivée de `appelantId` (paramètre serveur), jamais d'un champ de la requête (`SoumettreEnrolementRequete` ne porte aucun champ d'identité — vérifié par réflexion, `EnrolementSecuriteTests`). 7 tests (unitaires + intégration EF réelle pour le dépôt de pièce associé).

## 7. Prise en charge (C8-02, C8-10)

`PrendreEnChargeCommand`/`ConsulterFileQuery` (F001, génériques) étendues d'une 3ᵉ branche modulaire (`Module == "Enrolement" → EnrolementInstruire`), exactement comme la branche `CustomerComplaint` déjà existante. **Aucun second moteur de file créé.** Les suites de régression complètes des 2 fichiers ont été rejouées après chaque extension (protection F001/F003/F004/F006).

## 8. Compléments (C8-03, C8-04)

`AttenteClientCommand.MettreEnAttenteAsync` étendue (même patron 3 branches). `RependreApresReponseAsync`, `PublierMessageCommand.PublierMessageClientAsync`, `DeposerDocumentCommand.DeposerPourClientAsync` fonctionnent **sans aucune ligne modifiée** pour un ticket Enrolement — démontré par un test de cycle complet réel (`CycleCompletEnrolementIntegrationTests`, SQL Server réel) : soumission → prise en charge → demande de complément → réponse (message + pièce) → reprise → visible en file. Aucun second moteur de complément (CLAR-ENR-03 confirmé par le code).

## 9. Validation (C8-05)

`ValiderEnrolementCommand` (nouvelle) : vérifie le module et `EnrolementInstruire`, enregistre l'historique `"EnrolementValide"`. `StatutTicket` reste `EnCours` (aucune transition — cohérent avec research.md D6). Ne réutilise délibérément pas `ActionsWorkflowStandardCommand` (générique, non modulé — cf. research.md D5, confirmé pertinent pendant l'implémentation).

## 10. Convocation (C8-07)

`DeclencherConvocationCommand` (nouvelle) : historique `"ConvocationDeclenchee"`, événement publié **sans aucune donnée de canal** (assertion négative testée explicitement — ni "sms", ni "whatsapp", ni "email" dans le payload). Aucune implémentation SMS/WhatsApp/email (CLAR-ENR-06).

## 11. Badge (C8-08)

`ConstaterRemiseBadgeCommand` (nouvelle) : appelle `Ticket.Cloturer` (générique, garde existante contre la double clôture), historique `"BadgeRemisEtDossierCloture"`. **Aucune propriété "numéro de badge"/"technologie" sur `Ticket`** — vérifié par réflexion dans les tests. Aucune techno inventée (QR/RFID/NFC/API).

## 12. Clôture

Déclenchée exclusivement par `ConstaterRemiseBadgeCommand` — `DateClotureUtc` renseignée, ticket jamais supprimé (SOC-09, principe transverse). Confirmée end-to-end par le test REC-SCE-08.

## 13. UI

4 pages Blazor créées (`src/CIT.Plateforme.Web/Components/Pages/Enrolement/`) : `NouvelEnrolement.razor`, `MesEnrolements.razor`, `FileAgentGate.razor`, `FicheEnrolement.razor`. Contraintes respectées : zéro GUID manuel (identité systématiquement dérivée de `IContexteUtilisateurWeb`), zéro `DbContext`/`Repository` injecté dans un composant (uniquement des Commands/Queries Application), permissions revérifiées côté serveur par chaque commande indépendamment de l'affichage. `NavMenu.razor` modifié par exactement 2 insertions isolées (liens Client et Agent), le reste du fichier — refonte UI préexistante — non touché. 15 tests bUnit (dont un ajouté en hardening pour couvrir explicitement le cas non-authentifié, gap identifié en cours de route).

**Écart découvert** : `ObtenirFicheTicketQuery` (F001/F006) n'est pas réutilisable pour Enrolement — elle exige un `Dossier.ClientId` résolvable en `SocieteCliente`, systématiquement absent pour un Transporteur/Chauffeur (personne physique, D1). Une query dédiée `ObtenirFicheEnrolementQuery` a été créée, TEST-FIRST, mirroring la structure de l'originale sans la dépendance société. Une seconde query de glue (`ConsulterFileAgentGateQuery`, mirroring `ConsulterFileReclamationsQuery` de F006) a également été nécessaire pour résoudre la file "Agent Gate" par nom côté UI.

## 14. Sécurité

Tous les contrôles vérifiés côté serveur, jamais seulement côté UI : permission (`EnrolementSoumettre`/`EnrolementInstruire`) systématiquement vérifiée par chaque commande ; cloisonnement (`CloisonnementService`, générique, inchangé) pour les documents et messages client ; anti-IDOR structurel (`appelantId` toujours dérivé du contexte authentifié, jamais un paramètre de requête) ; accès croisé consolidé (un Transporteur ne peut jamais appeler les 3 commandes Agent Gate) ; transitions invalides gardées par le Domain existant (`Ticket.PrendreEnCharge`, `Ticket.Cloturer`) ; double validation documentée comme sans effet de bord dangereux (comportement identique à `ActionsWorkflowStandardCommand`, assumé, pas une lacune) ; double remise de badge refusée. 7 tests consolidés dans `EnrolementSecuriteTests.cs`, en complément des tests déjà présents dans les autres fichiers.

## 15. REC-SCE-08

Rejoué intégralement (5/5 étapes) via un test E2E bUnit (`RecSce08EnrolementTransporteurE2ETests`) exerçant le chemin UI réel pour chaque étape significative (rendu + `.Input()`/`.Click()`, jamais d'appel direct de commande là où l'UI est censée être démontrée), contre SQL Server réel (LocalDB), stable sur 3 exécutions consécutives. Deux écarts techniques découverts et corrigés pendant cette phase (détaillés en §22 et dans `tasks.md`) : nécessité d'une `IServiceScope`/`PlateformeDbContext` fraîche pour chaque relecture d'état post-action (contournement du change-tracker EF Core de portée longue), et nécessité d'une `BunitContext` distincte par étape (bUnit v2 verrouille sa collection de services dès le premier rendu).

## 16. Absence de rejet

Confirmée à trois niveaux indépendants : (1) aucune tâche de `tasks.md` n'a créé de commande de rejet ; (2) grep de contrôle sur `src/CIT.Plateforme.Application/Enrolement/` : 0 résultat pour "Rejeter"/"Rejet" ; (3) test automatisé par réflexion (`AucuneClasseDeRejetDansLAssemblage`) qui échouerait si une telle classe apparaissait un jour dans ce namespace.

## 17. ENR-04 (Should)

Tracée, non implémentée. L'événement générique `"ConvocationDeclenchee"` est structurellement consommable par un futur module Notifications sans modification de F008 (vérifié : le payload ne porte aucune donnée de canal). L'implémentation effective du canal SMS/WhatsApp dépend de PC-16, hors périmètre F008.

## 18. ENR-05 (Could)

Tracée, non implémentée. Aucune entité "expiration de badge" ni tâche de rappel automatique créée. **CLAR-ENR-07 reste explicitement DIFFÉRÉE — jamais présentée comme résolue.** Si retenue ultérieurement, nécessite une clarification dédiée (durée) avant toute conception.

## 19. Scans architecture

- **Zéro GUID manuel / zéro DbContext / zéro Repository dans les pages F008** : vérifié par relecture systématique des 4 fichiers `.razor` (uniquement des Commands/Queries Application injectées, identité dérivée de `IContexteUtilisateurWeb`).
- **Dépendances interdites** (Billing/Simulation/Assistant/SMS/WhatsApp) : grep sur `src/CIT.Plateforme.Application/Enrolement/` et `src/CIT.Plateforme.Web/Components/Pages/Enrolement/` → 1 seule occurrence (commentaire XML dans `DeclencherConvocationCommand.cs` documentant explicitement l'absence d'implémentation SMS/WhatsApp/email — conforme, pas une violation).
- **AVE** : 0 occurrence de "AVE"/"Avoir"/"Exoneration" dans le namespace Enrolement (test automatisé par réflexion).

## 20. Pending model changes

`dotnet ef migrations has-pending-model-changes` → *"No changes have been made to the model since the last migration."* — confirmé avant (baseline) et après (non-régression finale) l'implémentation complète. **Aucune migration créée**, conforme à plan.md "MIGRATION PRÉVUE : NON".

## 21. Non-régression

`dotnet build` solution complète → 0 avertissement, 0 erreur. `dotnet test` solution complète (PowerShell natif) → **595/595 verts, 0 échec, 0 ignoré**. Les suites de régression F001-F006 (dont `RecSce07CustomerComplaintE2ETests`, `FileSupportTests`, `PublierMessageComplaintTests`, `AttenteClientComplaintTests`) font partie intégrante de ce total et sont toutes vertes, confirmant l'absence de régression sur les 4 fichiers génériques étendus.

## 22. Dettes / Écarts

**Dette** :
- **T080 non exécutée** : parcours manuel de `quickstart.md` sur l'application démarrée (`dotnet run` + navigateur). La validation automatisée équivalente (REC-SCE-08 E2E, rendu Blazor réel + SQL Server réel) constitue une preuve substantiellement similaire, mais un passage manuel réel reste recommandé avant mise en production.

**Écarts techniques découverts et résolus pendant l'implémentation** (aucun n'a modifié une règle métier ni introduit une exigence non prévue par spec.md/contracts/use-cases.md) :
1. `ObtenirFicheTicketQuery` non réutilisable pour Enrolement → `ObtenirFicheEnrolementQuery` créée (TEST-FIRST).
2. Glue UI `ConsulterFileAgentGateQuery` nécessaire pour résoudre la file par nom côté page Blazor.
3. Change-tracker EF Core de portée longue masquant les écritures inter-scopes dans le test E2E → lecture systématique via scope frais.
4. Verrouillage de la collection de services bUnit v2 dès le premier `Render` → une `BunitContext` par étape UI dans le test E2E.
5. Warning de compilation sur un paramètre `IHorloge` non utilisé dans `ValiderEnrolementCommand` → paramètre retiré (validation ne nécessite aucun horodatage, cohérent avec l'absence de nouvel état).
6. Gap de couverture hardening identifié en cours de route (test "utilisateur non authentifié" manquant pour l'UI F008) → comblé avant la clôture de la phase.

Aucune dette fonctionnelle. Aucune exigence Must silencieusement ignorée.

---

## VERDICT FINAL

```
FEATURE : 008 — Enrôlement Transporteurs
TÂCHES : 79/80 (T080 non exécutée — dette mineure documentée, non bloquante)
ENR : 5/5 (ENR-01/02/03 implémentées ; ENR-04/05 tracées et différées, aucune silencieusement ignorée)
MUST : 3/3
SHOULD : 1/1 (différée — ENR-04, dépend PC-16)
COULD : 1/1 (différée — ENR-05, CLAR-ENR-07 non résolue)
C8 : 10/10
CLAR-ENR-01→06 : 6/6
CLAR-ENR-07 : DIFFÉRÉE
REC-SCE-08 : CONFORME (5/5 étapes, test E2E UI réel, stable ×3)
ENR-SEC : 6/6 (anti-IDOR, permission manquante, accès croisé, non-authentifié, manipulation identifiant, absence rejet — tous couverts)
UI CLIENT : NouvelEnrolement.razor, MesEnrolements.razor, FicheEnrolement.razor (vue Transporteur) — opérationnels, testés
UI AGENT GATE : FileAgentGate.razor, FicheEnrolement.razor (vue Agent Gate) — opérationnels, testés
REJET DEFINITIF : ABSENT (vérifié à 3 niveaux : tasks.md, grep, test par réflexion)
MIGRATION : AUCUNE (0 pending model change avant et après)
PENDING MODEL CHANGES : 0
TESTS : 595/595 (Domain 86, Application 309, Infrastructure 67, Web 133)
BUILD WARNINGS : 0
AVE : ABSENT (vérifié par test par réflexion)
DETTE : T080 (parcours manuel quickstart.md non exécuté) — mineure, non bloquante
VERDICT : READY FOR POST-IMPLEMENTATION AUDIT
```

STOP après implémentation. Aucun audit lancé automatiquement. Aucun commit. Aucun push. Feature 009 non commencée.
