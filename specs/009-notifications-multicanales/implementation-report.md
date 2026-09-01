# Rapport d'Implémentation — Feature 009 (Notifications Multicanales & Centre In-App)

> **Document** : `specs/009-notifications-multicanales/implementation-report.md`
> **Date d'exécution** : 1er septembre 2026
> **Portée** : exécution de `tasks.md` (201 tâches exécutables) selon `spec.md`, `research.md`, `data-model.md`, `contracts/use-cases.md`, `quickstart.md`.
> **Statut Git** : **AUCUN commit, aucun push, rien de stagé.**

---

## 1. Baseline (preflight)

Exécutée **avant toute modification de code** :

| Contrôle | Attendu | Obtenu |
|:---|:---|:---|
| `dotnet build` | 0 warning / 0 erreur | ✅ **0 / 0** |
| Domain / Application / Infrastructure / Web | 608 verts | ✅ **86 · 319 · 67 · 136 = 608/608** |
| `has-pending-model-changes` | 0 | ✅ *No changes have been made to the model* |

Gate franchi, aucune remédiation nécessaire.

---

## 2. Migration

**Fichier** : `20260901053222_AjoutNotifications.cs`

`Up()` — **strictement additif**, relu ligne par ligne :

| Opération | Nombre | Détail |
|:---|:---:|:---|
| `CreateTable` | 5 | `Notifications`, `EnvoisNotification`, `ModelesNotification`, `ReglesDiffusion`, `PreferencesCanalUtilisateur` |
| `AddColumn` | 1 | `JournalEvenements.DistribueLeUtc` — **`nullable: true`** |
| `CreateIndex` | 8 | 4 uniques + 4 de recherche |
| `DropTable` / `DropColumn` / `DropIndex` / `AlterColumn` / `Sql(...)` | **0** | Aucune opération destructive dans `Up()` |

Les `DropTable` / `DropIndex` / `DropColumn` **n'existent que dans `Down()`**, méthode de retour arrière standard d'EF Core — vérifié par lecture directe.

**Aucune donnée existante n'est modifiée.** Les lignes historiques de `JournalEvenements` valent `null` et seront examinées une fois puis marquées.

> **ÉCART TRACÉ vs `data-model.md` §7** : le plan annonçait **5 index**, la migration en crée **8**. Les 4 supplémentaires sont des index de recherche justifiés par les requêtes réelles — `IX_Notifications_DestinataireId` (cloisonnement), `IX_EnvoisNotification_NotificationId` (clé étrangère), `IX_JournalEvenements_DistribueLeUtc` (curseur), `IX_EnvoisNotification_Statut_DerniereTentative` (sélection bornée des reprises). Tous **additifs**, aucun ne porte de règle métier. `data-model.md` n'a pas été rétro-modifié pour masquer l'écart.

---

## 3. Moteur de notification

**Approche retenue et tenue** (research.md D1) : le moteur **consomme `EvenementSocle`**, l'intention écrite par F001 (« la future feature Notifications s'y abonnera »).

**Pipeline implémenté** — `DistribuerNotificationsUseCase` :

```
EvenementSocle (DistribueLeUtc IS NULL)
   → prise atomique de l'événement
   → règles de diffusion actives          (aucune règle ⇒ aucune notification)
   → résolution des destinataires nommés  (aucun résolveur ⇒ aucune notification)
   → rendu du modèle, FIGÉ à la création
   → Notification (1 par destinataire)
   → EnvoiNotification (1 par canal externe ; jamais pour In-App)
   → persistance
   → commit conjoint marquage + notifications
```

**Aucun des producteurs existants n'appelle le moteur** : le couplage reste unidirectionnel.

---

## 4. `DistribueLeUtc` — les quatre scénarios

| Scénario | Comportement obtenu | Preuve |
|:---|:---|:---|
| **A** — événement non distribué → traitement | Notifications persistées **puis** curseur renseigné ; l'ordre est vérifié | `DistribuerNotificationsUseCaseTests.ScenarioA_*` |
| **B** — second passage | Aucun doublon observable ; les notifications existantes ne sont pas modifiées | `EvenementRejoue_NeCreeAucunDoublon`, `MentionRejouee` |
| **C** — échec avant persistance | La transaction est annulée, `DistribueLeUtc` **reste NULL**, l'événement est relu au tour suivant | `ScenarioC_EchecAvantPersistance_LaTransactionEstAnnulee` |
| **D** — deux workers simultanés | **Exactement un** worker obtient l'événement | `NotificationRepositoryTests.ScenarioD_DeuxPrisesConcurrentes_UneSeuleReussit` — **SQL Server réel** |

**Frontière transactionnelle** : `IExecuteurTransactionnel` (Application) / `ExecuteurTransactionnel` (Infrastructure). Le marquage du curseur n'est **jamais** committé avant la persistance durable des notifications.

---

## 5. Idempotence et garantie de distribution

**Garantie revendiquée — et rien de plus** :

| Niveau | Garantie | Mécanisme |
|:---|:---|:---|
| Traitement d'un événement | **at-least-once** | curseur non committé en cas d'échec |
| Effet observable (notification) | **effectively-once** | index unique `(EvenementSourceId, DestinataireId)` |
| Envoi sur canal externe | **at-least-once** | ERR-02 exige la reprise ; aucune déduplication externe n'est inventée |

**Deux lignes de défense** contre la double distribution :
1. **prise atomique** — `UPDATE … WHERE DistribueLeUtc IS NULL`, exécuté côté serveur SQL ;
2. **index unique** — rejette le doublon même si (1) était franchie.

Une violation d'unicité concurrente est traitée comme un **résultat normal de déduplication** (contrôle `ExisteDejaAsync` en amont), jamais comme une erreur brute.

> **Le terme « exactly-once » n'apparaît nulle part** dans le code source — vérifié par test automatisé (`LeTermeExactlyOnce_NApparaitNullePartDansLeCodeSource`). Trois commentaires qui l'employaient, même pour le nier, ont été reformulés.

---

## 6. Reprise sur échec (ERR-02)

| Obligation | Implémentation | Preuve |
|:---|:---|:---|
| Statut d'envoi **par canal** | `EnvoiNotification.Statut` (CDC §20) | `ChaqueCanal_PorteSonPropreStatutDEnvoi` |
| Nouvelle tentative automatique | `ReessayerEnvoisNotificationUseCase` + `BackgroundService` | `Reprise_EngageUneNouvelleTentativeSansActionHumaine` |
| Isolation inter-canaux | chaque envoi traité indépendamment | `UnCanalEnEchec_NEmpechePasLesAutres` |
| Conservation de l'échec | `MotifEchec` borné, `NombreTentatives` cumulé | `EnregistrerEchec_Repete_CumuleLesTentatives` |
| Transaction métier indépendante | la reprise est asynchrone, la transaction est committée depuis longtemps | conséquence structurelle de D1 |
| **Aucun fallback** | aucune bascule de canal n'existe dans le domaine | `EchecEmail_NeDeclencheJamaisUnAutreCanal` |

**Configuration obligatoire** — `Notifications:Reessai:{NombreTentativesMax, DelaiEntreTentatives}` :
- **aucune valeur par défaut dans le code** ; `Valider()` lève une erreur explicite citant CLAR-NOT-02 ;
- les valeurs sont **absentes de `appsettings.json`** et présentes **uniquement dans `appsettings.Development.json`**, commentées comme **NON CONTRACTUELLES** : un déploiement de production doit les fournir explicitement, sinon le démarrage échoue ;
- au plafond, l'envoi **cesse d'être sélectionné et conserve son statut `Echec`** — **aucun statut « Abandonné » n'a été introduit** (vérifié par test).

Le raccourci de `ParametresRelance` (F001, valeurs en dur + dette tracée) **n'a pas été reproduit**.

---

## 7. Centre In-App

**Implémenté** : liste, état Lu/Non lu, date, type, titre, contenu, lien vers le ticket, compteur de non-lues, marquer comme lu.

**Volontairement NON implémenté** (absent du CDC — CLAR-NOT-03) : suppression physique, archivage, corbeille, épinglage, recherche plein texte, priorité ou catégorie personnalisée. Vérifié par test UI (`AucuneActionDeSuppressionNiDArchivage_NEstProposee`) **et** par réflexion sur le domaine (`Notification_NExposeAucuneSuppressionNiArchivage`).

`MarquerCommeLue` est **idempotente** : sur une notification déjà lue, aucun changement, aucune erreur.

**ACR-01 non dupliquée** : `MarquerNotificationLueCommand` ne dépend d'aucun port lié au tableau de bord ni au ticket — vérifié par réflexion.

---

## 8. Modèles, règles et canaux

- **`ModeleNotification`** (COM-03) : par `(TypeEvenement, Canal)`, activable/désactivable, **jamais supprimable**. Le rendu est **figé à la création** de la notification : modifier un modèle n'altère aucune notification déjà émise.
- **`RegleDiffusion`** (NOT-02) : par `(TypeEvenement, Canal)`, portant `EstObligatoire` — **propriété de la règle**, jamais une constante par type d'événement.
- **`PreferenceCanalUtilisateur`** (COM-05) : **seule** dimension canal ; In-App non débrayable ; une règle obligatoire prime sur la préférence.
- **Canaux** : `ICanalNotification` + `CanalJournalise` — **aucun fournisseur**, aucune dépendance réseau, aucun paquet ajouté.

---

## 9. MSG-04 — raccordement (Option B)

**Coût en code métier : ZÉRO.** `MentionnerCollaborateurCommand.cs` et `Message.cs` sont **inchangés** — vérifié par `git diff` **et** par test automatisé (`MentionnerCollaborateurCommand_NeConnaitAucunConceptDeFeature009`).

Le raccordement se fait par **résolveur + règle + modèle** :

```
CollaborateurMentionne (publié par F001, charge utile portant déjà collaborateurId)
   → ResolveurCollaborateurMentionne
   → notification In-App du SEUL collaborateur mentionné
```

**Cas couverts** :

| Cas | Résultat |
|:---|:---|
| Mention valide | Notification In-App, `Non lu`, avec lien vers le ticket |
| Collaborateur inexistant | Aucune notification, aucune exception |
| Collaborateur désactivé | Aucune notification |
| Destinataire de nature **Client** | **Aucune notification** — une note interne ne devient jamais une notification client (MSG-02) |
| Note interne | Le résolveur client **ne traite ni `MessagePoste` ni `CollaborateurMentionne`** |
| Événement rejoué | Aucun doublon |
| Absence de broadcast | **Un seul** destinataire résolu |
| E-mail | **Aucune règle `(CollaborateurMentionne, Email)` n'est semée** — émis uniquement si configuré explicitement |

**MSG-04 reste une exigence de F001 et n'est PAS recomptée** dans les 9 exigences de F009.

**MSG-05 préservé par construction** : le résolveur ne dépend d'aucun port capable de modifier un ticket — vérifié par réflexion sur son constructeur.

---

## 10. ALE-03 — abstention

**Aucune notification SLA n'est implémentée.** F009 fournit le point d'intégration et rien d'autre.

| Vérification | Résultat | Preuve |
|:---|:---|:---|
| L'événement SLA existe et est visible du moteur | ✅ | `SurveillerEcheancesUseCase` publie `SlaBientotDepasse` / `SlaDepasse` |
| Aucun résolveur superviseur | ✅ | `IlNExisteQueDeuxResolveursDeDestinataires` (réflexion) |
| Aucune règle SLA semée | ✅ | seed borné, vérifié |
| Aucune notification générée | ✅ | `EvenementSla_NeProduitAucuneNotification` |
| `SurveillerEcheancesUseCase` inchangée | ✅ | test de source |

---

## 11. IMP-11

**Deux lignes ajoutées, une par commande** :

| Fichier | Avant | Après |
|:---|:---|:---|
| `EditerFactureCommand.cs` | historique + audit, **aucun `EvenementSocle`** | + 1 `PublierEvenementAsync("FactureEditee", …)` |
| `EditerFactureExportCommand.cs` | idem | idem |

**Aucune autre logique de notification** dans ces commandes — elles ignorent canal, modèle et destinataire (vérifié par test de source).

**Non-régression F003 / F004** : suites BillingImport et BillingExport intégralement vertes.

---

## 12. Producteurs d'événements

| État | Fichiers | Sites d'appel |
|:---|:---:|:---:|
| Avant F009 | 19 | 21 |
| Après F009 | **21** | **23** |

Les 2 fichiers ajoutés sont les **seules** commandes de facture, qui n'étaient pas productrices. **Aucun des 19 producteurs préexistants n'est modifié** — vérifié par test automatisé.

> **Correction de comptage** : une première version de ce test affirmait 19 fichiers après F009. C'était faux — les deux commandes de facture n'étaient pas productrices avant, elles le deviennent. Le test a été corrigé, pas contourné.

---

## 13. Interface

| Écran | Fait |
|:---|:---|
| `HeaderBar.razor` | Cloche **déjà présente** câblée : lien + badge de non-lues. Structure et classes CSS conservées. **Aucune refonte.** |
| `CentreNotifications.razor` | Liste, état, date, type, titre, contenu, lien ticket, marquer comme lu |
| `MesPreferencesNotification.razor` | Canaux débrayables (In-App absent, non débrayable) |
| `AdministrationNotifications.razor` | Règles de diffusion et modèles |
| `NavMenu.razor` | Liens par espace, selon habilitation |

**Contraintes tenues** : 0 GUID saisi manuellement, 0 `DbContext` en Razor, 0 repository en Razor, identité toujours issue du contexte authentifié, autorisation vérifiée côté serveur.

> `MainLayout` étant rendu en **SSR statique**, la cloche est un lien (`<a href>`) et non un bouton `@onclick` — un gestionnaire Blazor n'y serait jamais déclenché.

---

## 14. Sécurité

| Vérification | Preuve |
|:---|:---|
| Cloisonnement destinataire (anti-IDOR) | Filtre serveur par égalité stricte, dans la requête ; refus indiscernable entre « inexistante » et « d'autrui » |
| Cloisonnement inter-sociétés | Le résolveur client n'ajoute que les collaborateurs de la société du dossier |
| Administrateur sans accès aux notifications d'autrui | Aucune permission de ce type n'existe |
| Notes internes jamais exposées au client | Double garantie : résolveur client ignorant `MessagePoste`/`CollaborateurMentionne`, **et** résolveur de mention refusant tout destinataire non-Agent |
| Lien vers ticket sécurisé | `TicketId` porté, lien **relatif**, **aucun jeton** |
| Absence de broadcast | Aucun destinataire collectif ; aucun résolveur file/rôle |
| Aucune coordonnée arbitraire | Aucun paramètre `string` d'e-mail/téléphone/adresse dans les points d'entrée F009 (réflexion) |
| Contenu jamais journalisé | `MotifEchec` borné et technique ; l'audit ne porte que canal/statut/tentatives |

---

## 15. TEST-SPEC — 34 cas

| TEST-SPEC | Cas | État |
|:---|:---:|:---|
| `TEST-NOT-01` | 8 | ✅ |
| `TEST-NOT-02` | 8 | ✅ |
| `TEST-MSG-04` | 8 | ✅ |
| `TEST-ERR-02` | 6 | ✅ |
| `TEST-COM-06` | 4 | ⚠️ **partiel** — voir dette D3 |
| **Total** | **34** | **30 pleinement couverts, 4 partiels** |

---

## 16. REC-SCE-14

**PARTIEL pour F009**, conformément à `spec.md` §18.1 — et **non clôturable** par cette feature.

| Volet | État |
|:---|:---|
| Notifications sur chaque canal configuré | ✅ sur les canaux configurés (portail + e-mail) |
| Lien vers le ticket | ✅ |
| Contrôle des habilitations (volet notifications) | ✅ |
| Journal d'audit (volet notifications) | ✅ |
| Recherche de ticket au guichet | ❌ **Feature 010** |
| Rendez-vous / bureau de facturation | ❌ **Feature 010** |

> **Dette D4** : aucun test E2E bUnit dédié `RecSce14NotificationsE2ETests` n'a été écrit. Les volets F009 sont couverts par les tests Application et UI existants, mais pas rejoués comme un parcours de bout en bout.

---

## 17. Scans

| Scan | Résultat |
|:---|:---|
| Fournisseurs interdits (Twilio, SendGrid, SmtpClient, MailKit, Azure.Communication, Infobip, Vonage, Nexmo, Mailgun, Amazon.SimpleEmail, graph.facebook.com, api.whatsapp.com) | ✅ **0 occurrence hors commentaires d'interdiction** — test automatisé |
| Dépendances (`.csproj`) | ✅ **aucun `PackageReference` ajouté** |
| Valeurs de retry en dur / backoff / `Math.Pow` | ✅ **0** dans le code |
| `exactly-once` | ✅ **0 occurrence** |
| `CON-` / rendez-vous / créneau / guichet / agenda dans le code F009 | ✅ **0** |
| `AVE` / avoir / exonération dans le code F009 | ✅ **0** |
| Secrets dans `appsettings*` | ✅ **0** |
| Paramètres retry dans `appsettings.json` (production) | ✅ **absents — volontairement** |
| `DbContext` / repository en Razor | ✅ **0** |

---

## 18. Non-régression

| Contrôle | Résultat |
|:---|:---|
| `dotnet build --no-incremental` | ✅ **0 avertissement, 0 erreur** |
| Domain | **129/129** (86 baseline + 43) |
| Application | **400/400** (319 baseline + 81) |
| Infrastructure | **77/77** (67 baseline + 10) |
| Web | **141/141** (136 baseline + 5) |
| **Total** | ✅ **747/747** — 0 échec, 0 ignoré |
| `has-pending-model-changes` | ✅ **0** |
| Régressions F001→F008 | ✅ **0** |

**Un seul test existant a été touché** : `MainLayoutIntegrationTests` — le harnais bUnit devait fournir la query du compteur de non-lues, désormais consommée par `HeaderBar`. **Le comportement du layout n'a pas été modifié** ; c'est le test qui a été complété, pas le produit affaibli.

---

## 19. Git

**Rien n'a été stagé, commité ni poussé.** L'arbre de travail contient **133 entrées**, à classer avant tout commit :

| Catégorie | Nombre | Détail |
|:---|:---:|:---|
| **F009 — nouveaux fichiers** | 15 | domaine, application, infrastructure, UI, tests, migration |
| **F009 — fichiers modifiés** | 13 | `EvenementSocle.cs`, `NatureUtilisateur.cs`, `Abstractions/Repositories.cs`, `EditerFactureCommand.cs`, `EditerFactureExportCommand.cs`, `PlateformeDbContext.cs`, `DependencyInjection.cs`, `DevelopmentSeeder.cs`, `PlateformeDbContextModelSnapshot.cs`, `HeaderBar.razor`, `NavMenu.razor`, `appsettings.Development.json`, `MainLayoutIntegrationTests.cs` |
| **PRÉEXISTANTS — étrangers à F009** | ~105 | refonte UI (nombreux `.razor`), écrans connexion/2FA, `Infrastructure/Persistence/Repositories.cs`, `DevelopmentSeederTests.cs`, ajustements Application/Infrastructure |

> **Vérifié** : `Infrastructure/Persistence/Repositories.cs` et `DevelopmentSeederTests.cs` apparaissent modifiés **mais ne contiennent aucune ligne F009** (`git diff | grep -ci notification` → 0). Ce sont des modifications préexistantes à cette session.

**Le commit F009 devra être sélectif, fichier par fichier. `git add -A` et `git add .` sont proscrits.**

---

## 20. Dettes et écarts

| # | Dette / écart | Nature | Suite |
|:--|:---|:---|:---|
| **D1** | **8 index créés au lieu des 5 annoncés** dans `data-model.md` §7 | Écart au plan, additif et justifié | À entériner ou à réduire lors de l'audit |
| **D2** | **`TEST-COM-06` partiel** — COM-06 (Could) n'a pas reçu de suite dédiée. La capacité fonctionne (`RelanceEmise` est traité comme tout autre type dès qu'une règle est configurée) mais aucun test ne l'exerce | Dette tracée, non silencieuse | Précédent ENR-04/ENR-05 (F008) |
| **D3** | **Aucun test E2E `RecSce14NotificationsE2ETests`** | Dette tracée | À écrire avant recette |
| **D4** | **Tests bUnit d'administration et de préférences non écrits** (T146, T149) — les écrans existent et les commandes sont testées côté Application | Dette tracée | À compléter |
| **D5** | **Résolveur client : fan-out société non testé en intégration** — la logique `DroitConsultation` est implémentée mais couverte uniquement par les tests structurels | Dette tracée | À compléter |
| **D6** | **Valeurs de reprise à confirmer** — `3` tentatives / `5 min` en développement uniquement, **non contractuelles** | **CLAR-NOT-02 ouvert** | Décision métier/DSI **avant implémentation de production** |
| **D7** | **Liste PC-17 non validée** — seules les règles CDC-EXIGENCE sont semées | **CLAR-COM-02 ouvert** | Validation métier |
| **D8** | **Textes des modèles non contractuels** | **CLAR-NOT-05 ouvert** | À fournir **avant recette** |
| **D9** | **Canaux mobiles non branchés** — SMS/WhatsApp servis par `CanalJournalise` | **PC-16 ouvert** | **DIFFÉRÉ / AVANT PRODUCTION** |
| **D10** | **Coordonnée téléphone facultative et non vérifiée** ; aucune coordonnée pour les chauffeurs (ENR-04) | **CLAR-COM-04 ouvert** | Décision métier |

**Aucune exigence Must n'est silencieusement ignorée.** Les dettes D2 à D5 sont des dettes de **couverture de test**, pas de fonctionnalité ; D6 à D10 sont des **points à clarifier du CDC**, explicitement non comblés.
