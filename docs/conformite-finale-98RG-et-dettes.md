# Conformité finale — 98 RG et dettes hors score

*Mode AUDIT / READ ONLY. Aucun code, test, migration ou configuration n'a été modifié.*

| | |
|---|---|
| Base d'audit | `docs/audit-final-conformite-98RG.md` — commit `1c3cd38260c3b196de8641b8ec1d5a89f27a9762` |
| Remédiations | `docs/remediation-post-audit-final.md` |
| Recertification | `docs/recertification-post-remediation.md` |
| HEAD | `1c3cd38260c3b196de8641b8ec1d5a89f27a9762` *(inchangé — rien n'a été committé)* |
| Suite | **1424 / 1424** |

**Périmètre du recalcul : les 98 règles RG-* du document de référence, et rien d'autre.** SEC-06, la
chaîne de rétention, `workflow.deroger`, les services orphelins et les doublons techniques sont hors
de ce périmètre : ils figurent en section *Dettes techniques*, sans effet sur le score.

**Quatre statuts changent, aucun autre.** Les quatre remédiations validées passent de 🟡 à ✅. Tous
les autres statuts sont repris tels quels : aucune preuve nouvelle n'est venue en modifier un.

---

## 1. Matrice finale — 98 RG

| RG | Statut | Preuve finale | Arbitrage ouvert |
|---|---|---|---|
| RG-CPT-01 | ✅ | `ValiderOuRejeterDemandeCompteCommand` + `Permission.CompteValider` · `ValiderDemandeCompteTests` | — |
| RG-CPT-02 | ✅ | `TypeClient` — 4 valeurs fermées · `SoumettreDemandeCompteTests` | — |
| RG-CPT-03 | ✅ | Aiguillage sur `dossier.TypeClient` · `AiguillageTypeClientTests` | — |
| RG-CPT-04 | ✅ | `ControleAccesRessourceService.ClientEstAdmisAsync` · `ParcoursCloisonnementMultiTenantE2ETests` | — |
| RG-CPT-05 | ✅ | Instantané `Dossier.TypeClient` · `NonRetroactiviteTypeClientSqlTests` | — |
| RG-CPT-06 | ✅ | `UtilisateurSociete` + invariant CLAR-03 · `ConcurrenceDernierAdministrateurTests` | — |
| RG-CPT-07 | ✅ | `VisibiliteDemandesFiltre` appliqué dans le contrôle d'accès · `DeterminerVisibiliteDemandesTests` | — |
| RG-CPT-08 | 🔵 | `NatureUtilisateur` = { Client, Agent } — distinction fonctionnelle réelle, non nommée dans le modèle | **OUI** — modèle ou opérationnel |
| RG-TCK-01 | ✅ | `INumerotationService` + unicité en base · `NumerotationTests` | — |
| RG-TCK-02 | ✅ | `ObtenirFicheTicketQuery` centralise tout le traitement · `ObtenirFicheTicketQueryTests` | — |
| RG-TCK-03 | ✅ | Module × type de client · `CircuitClient*Tests` | — |
| RG-TCK-04 | ✅ | `Dossier` + multi-BL · `AtomiciteMultiBlSqlTests` | — |
| RG-TCK-05 | ✅ | `LiaisonTicket` · `LiaisonInterTicketsTests` | — |
| **RG-TCK-06** | **✅** | **`Ticket.Cloturer` → `Archiver` ; `EstArchive` restitué par la fiche · `ArchivageAutomatiqueTests` (9)** | — |
| RG-TCK-07 | ✅ | `EstEnEtatEngageant` + `EtapeBloquanteAnnulation` paramétrable · `AnnulationSeuilBloquantTests` | — |
| RG-TCK-08 | ✅ | `DemandeAnnulationSoumise` puis décision agent · `AnnulationTests` | — |
| RG-TCK-09 | ✅ | Motif obligatoire, historique + audit · `AuditDistinctionAnnulationRejetTests` | — |
| RG-TCK-10 | ✅ | `RelancesAbsenceReponseUseCase` ; clôture manuelle · `RelancesAbsenceReponseTests` | — |
| RG-TCK-11 | ✅ | `EstEligibleASuggestionDeCloture` avec `!EstEnEtatEngageant` ; 6 appelants · `CablageEtatEngageantTests` | — |
| RG-TCK-12 | ✅ | `ReouvrirTicketCommand` — permission + motif · `ReouvertureTests` | — |
| RG-TCK-13 | ✅ | `BlDejaActifException` + index filtré · `ConcurrenceUniciteBlSqlTests` | — |
| RG-TCK-14 | ✅ | Contrôle BL par BL · `SoumettreDemandeImportTests` | — |
| RG-TCK-15 | ✅ | Unicité booking · `UniciteBookingConcurrenceSqlTests` | — |
| RG-MSG-01 | ✅ | `ListerVisiblesParClientAsync` vs `ListerParTicketAsync` · `NotesInternesTests` | — |
| RG-MSG-02 | ✅ | `MessageItemVue.EstInterne` + rendu distinct · `MessagesTests` | — |
| RG-MSG-03 | ✅ | `MentionnerCollaborateurCommand` + résolveur · `RaccordementMentionTests` | — |
| RG-MSG-04 | ✅ | Assertion sur `OperateurAssigneId` · `MentionSansReaffectationTests` | — |
| RG-WKF-01 | ✅ | Files en base, aiguillage paramétré · `MatriceTransitionsTests` | — |
| RG-WKF-02 | ✅ | Dérogation servie par `TransfererTicketCommand` / `ReaffecterTicketCommand` · `DerogationWorkflowTests` | — |
| RG-WKF-03 | ✅ | Audit sur chaque dérogation · `DerogationWorkflowTests` | — |
| RG-WKF-04 | ✅ | `StatutTicket.EnAttenteDePriseEnCharge` · `ConsulterFileEtPriseLibreTests` | — |
| RG-WKF-05 | ✅ | Permission bornée file + affectation superviseur · `PriseEnChargeConcurrenceTests` | — |
| RG-WKF-06 | ✅ | `PassageEnFile` · `SuiviPassagesEnFileTests` | — |
| RG-WKF-07 | ✅ | Le ticket conserve numéro, dossier, historique · `RecSce10TransfertTests` | — |
| RG-WKF-08 | ✅ | Motif obligatoire + référentiel · `MotifsTransfertReferentielTests` | — |
| RG-WKF-09 | ✅ | `Permission.TicketReaffecter` non réservée au superviseur · `RecSce11ReaffectationTests` | — |
| RG-WKF-10 | ✅ | Événement publié, consommé par la distribution · `DistribuerNotificationsUseCaseTests` | — |
| RG-WKF-11 | ✅ | `TransitionNonAutoriseeException` · `MatriceTransitionsTests` | — |
| RG-TPS-01 | ✅ | `MesureTemps.Calculer` bornée par la file · `MesureTempsParEtapeTests` | — |
| RG-TPS-02 | ✅ | `DureeAttenteClienteCumulee` · `AttenteClientTests` | — |
| RG-TPS-03 | ✅ | Total = création → clôture · `MesureTempsParEtapeTests` | — |
| **RG-SLA-01** | **✅** | **6 axes : module, type, file (étape), priorité, catégorie de client — filtrés dans `ParametresSlaProvider` · `AxeCategorieClientSlaTests` (5)** | — |
| RG-SLA-02 | ✅ | 4 niveaux, `SeuilEscalade` contraint > 100 % · `MoteurSlaTests` | — |
| RG-SLA-03 | ✅ | SLA suspendu en attente client · `SurveillerEcheancesCalendrierTests` | — |
| RG-SLA-04 | ✅ | `GererParametresSlaCommand` + écran · `AdministrationRedefinitionTests` | — |
| RG-ALE-01 | ✅ | `TypeAlerte` — 8 valeurs couvrant les 6 cas · `NfrCert03DisponibiliteTests` | — |
| RG-ALE-02 | ✅ | `RegleEscalade` + `ActionEscalade` · `RegleEscaladeTests` | — |
| RG-DIS-01 | ✅ | `FiltreDisponibilite.SeulementDisponibles` · `DisponibiliteExclusionTests` | — |
| RG-DIS-02 | ✅ | Réaffectation en masse · `ActionsDeMasseTests` | — |
| RG-AFF-01 | ✅ | 6 modes paramétrables par file · `StrategieAffectationParametrableTests` | — |
| RG-PRI-01 | ✅ | Référentiel + audit ancienne/nouvelle valeur · `PriCert01PrioriteTests` | — |
| RG-PRI-02 | ✅ | `Permission.PrioriteModifier` exigée · `PrioriteTests` | — |
| RG-MAS-01 | ✅ | Audit **par ticket**, y compris en cas d'échec · `ActionsDeMasseTests` | — |
| RG-IMP-01 | ✅ | 1 à 5 BL, tickets indépendants · `AtomiciteMultiBlSqlTests` | — |
| RG-IMP-02 | ✅ | Validé / rejeté / complément · `InstructionManifesteTests` | — |
| RG-IMP-03 | ✅ | Retour en file Manifeste · `InstructionManifesteTests` | — |
| RG-IMP-04 | ✅ | Aiguillage sur l'instantané · `AiguillageTypeClientTests` | — |
| RG-IMP-05 | ✅ | Parcours complet · `RecSce01ImportNormalTests` | — |
| RG-IMP-06 | ✅ | Transition directe contrôle doc · `RecSce02ImportEnCompteTests` | — |
| RG-IMP-07 | ✅ | Lettrage confirmé · `RecSce03ImportDepositTests` | — |
| RG-IMP-08 | ✅ | Double branche + `JointureIesIncompleteException` · `ConcurrenceIesFinalisationSqlTests` | — |
| RG-IMP-09 | ✅ | `DeclarerFinalisationPresentielleCommand` · `DeclarationPresentielleTests` | — |
| RG-EXP-01 | ✅ | Un ticket par booking · `MultiBookingsIndependanceTests` | — |
| RG-EXP-02 | ✅ | Codifié / rejeté / complément · `InstructionBackOfficeTests` | — |
| **RG-EXP-03** | **✅** | **Draft IN avant facture (`DraftInRequisException`) + BEE ; refus IES prioritaire · `DraftInExportTests` (7)** | — |
| RG-EXP-04 | ✅ | Jointure IES export, facture hors plateforme · `ConcurrenceIesExportFinalisationSqlTests` | — |
| RG-SIM-01 | ✅ | Saisie complète du dossier et des conteneurs · `CalculerSimulationProformaQueryTests` | — |
| RG-SIM-02 | ✅ | Clé Sens + Taille + Catégorie · `TarificationSqlPersistenceTests` | — |
| RG-SIM-03 | ✅ | 7 composantes · `GoldenMasterProformaTests` | — |
| RG-SIM-04 | ✅ | Durée de séjour, bornes incluses · `StationnementEtReeferTests` | — |
| RG-SIM-05 | ✅ | Reefer uniquement, franchise 2 j · `StationnementEtReeferTests` | — |
| RG-SIM-06 | ✅ | 30 %, seuils 15 t / 26 t · `MajorationsTarifairesTests` | — |
| RG-SIM-07 | ✅ | 50 % / 50 %, cumulables · `MajorationsTarifairesTests` | — |
| RG-SIM-08 | ✅ | Tranches 850 / 1 250 / 1 850 ; franchises 11 j et 21 j · `GoldenMasterProformaTests` | — |
| RG-SIM-09 | ✅ | CIT 30 % · OIC 20 % · PAA 50 % · `GoldenMasterProformaTests` | — |
| RG-SIM-10 | ✅ | Total conteneur et dossier, au franc · `AuditPreuveArrondiFrancTests` | — |
| RG-SIM-11 | ✅ | Paramètres administrables + import CSV · `ImportGrilleTarifaireCsvTests` | — |
| RG-SIM-12 | ✅ | Version + libellé référencés · `ReproductibiliteHistoriqueSimulationTests` | — |
| RG-AVE-01 | ✅ | Workflow propre, pièces paramétrables · `InstructionAvoirExoTests` | — |
| RG-AVE-02 | ✅ | Décision + documents + notification · `DeciderAvoirExoTests` | — |
| RG-REC-01 | ✅ | Catégories dynamiques · `RecCert02CategoriesTests` | — |
| RG-REC-02 | ✅ | Instruction et orientation · `RecCert03OrientationSupportTests` | — |
| RG-REC-03 | ✅ | `ReponseFormalisee` obligatoire · `CloturerReclamationCommandTests` | — |
| RG-ENR-01 | ✅ | Saisie à distance, pièces paramétrables · `DepotPieceEnrolementIntegrationTests` | — |
| RG-ENR-02 | ✅ | Convocation et remise de badge tracées · `RecSce08EnrolementTransporteurE2ETests` | — |
| RG-COM-01 | 🟡 | Portail effectif et lien garanti ; **e-mail, SMS, WhatsApp = `CanalJournalise`, aucun envoi réel** · `ComCert06CanauxRelanceTests` | **OUI** — PC-16 |
| RG-COM-02 | ✅ | Modèles paramétrables par module/étape/canal · `AdministrationEtPreferencesTests` | — |
| RG-COM-03 | ✅ | Fil du ticket canal de référence · `MessagesTests` | — |
| RG-NOT-01 | ✅ | `EstLue` / `MarquerCommeLue` + `TicketId` · `NotificationTests` | — |
| RG-NOT-02 | ✅ | `RegleDiffusion` événement × canal · `ReferentielsNotificationTests` | — |
| RG-CON-01 | ✅ | Recherche guichet, historique et étape restitués · `RecSce14ContinuiteGuichetRendezVousE2ETests` | — |
| RG-CON-02 | ✅ | `RendezVous` + agenda · `GestionRendezVousTests` | — |
| RG-IA-01 | 🟡 | **8 sujets par mots-clés ; aucune aide à la complétion des dossiers** · `AssistantApplicationTests` | **OUI** — moteur et périmètre |
| RG-IA-02 | ✅ | Escalade avec fil intégral rattaché au ticket · `ConversationAssistantTests` | — |
| **RG-SEC-01** | **✅** | **RBAC par permissions + contrôle au niveau champ câblé (`PorteeChampsIesService` → matrice) · `ControleChampIesTests` (9)** | — |
| RG-SEC-02 | ✅ | Décision unique + périmètres de supervision · `CablageControleAccesTests` (23) | — |
| RG-SEC-03 | ✅ | Audit de toutes les actions listées, connexion comprise ; journal append-only · `JournalAuditAppendOnlyTests` | — |
| RG-SEC-04 | ✅ | Lien → ticket `[Authorize]` + contrôle d'accès · `SecCert06LienNotificationTests` | — |

**98 lignes.**

---

## 2. Recalcul

```
Dénominateur = 98 − 1 (N/A) = 97 règles évaluées

CONFORME        95 × 1,0  =  95,0
PARTIEL          2 × 0,5  =   1,0
NON CONFORME     0 × 0    =   0,0
NON IMPLÉMENTÉ   0 × 0    =   0,0
                            ──────
                             96,0 / 97

CONFORMITÉ FINALE = 99,0 %
```

| Statut | Nombre |
|---|---|
| ✅ CONFORME | **95** |
| 🟡 PARTIELLEMENT CONFORME | **2** |
| 🔴 NON CONFORME | **0** |
| ⚫ NON IMPLÉMENTÉ | **0** |
| 🔵 N/A / À ARBITRER | **1** |
| ⚠️ NON VÉRIFIABLE | **0** |

### Progression

| | Audit final | Après remédiation |
|---|---|---|
| Conformité | 96,9 % | **99,0 %** |
| Conformes | 91 | **95** |
| Partiels | 6 | **2** |

Les quatre règles passées de 🟡 à ✅ : **RG-SEC-01**, **RG-EXP-03**, **RG-SLA-01**, **RG-TCK-06**.
Aucun autre statut n'a bougé.

### Par domaine

| Domaine | Règles | ✅ | 🟡 | 🔵 | Score | % |
|---|---|---|---|---|---|---|
| Comptes (CPT) | 8 | 7 | 0 | 1 | 7,0 / 7 | **100 %** |
| Ticket (TCK) | 15 | 15 | 0 | 0 | 15,0 / 15 | **100 %** |
| Échanges (MSG) | 4 | 4 | 0 | 0 | 4,0 / 4 | **100 %** |
| Workflow (WKF) | 11 | 11 | 0 | 0 | 11,0 / 11 | **100 %** |
| Temps (TPS) | 3 | 3 | 0 | 0 | 3,0 / 3 | **100 %** |
| SLA | 4 | 4 | 0 | 0 | 4,0 / 4 | **100 %** |
| Alertes (ALE) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Disponibilité (DIS) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Affectation (AFF) | 1 | 1 | 0 | 0 | 1,0 / 1 | **100 %** |
| Priorité (PRI) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Actions de masse (MAS) | 1 | 1 | 0 | 0 | 1,0 / 1 | **100 %** |
| Billing IMPORT (IMP) | 9 | 9 | 0 | 0 | 9,0 / 9 | **100 %** |
| Billing EXPORT (EXP) | 4 | 4 | 0 | 0 | 4,0 / 4 | **100 %** |
| Simulation (SIM) | 12 | 12 | 0 | 0 | 12,0 / 12 | **100 %** |
| Avoir/Exonération (AVE) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Customer Complaint (REC) | 3 | 3 | 0 | 0 | 3,0 / 3 | **100 %** |
| Enrôlement (ENR) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Communication (COM) | 3 | 2 | 1 | 0 | 2,5 / 3 | 83,3 % |
| Notifications (NOT) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Continuité (CON) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Assistant IA (IA) | 2 | 1 | 1 | 0 | 1,5 / 2 | 75,0 % |
| Sécurité (SEC) | 4 | 4 | 0 | 0 | 4,0 / 4 | **100 %** |
| **TOTAL** | **98** | **95** | **2** | **1** | **96,0 / 97** | **99,0 %** |

**20 domaines sur 22 sont à 100 %.** Les deux qui ne le sont pas — Communication et Assistant IA —
le sont pour la même raison : une décision métier non rendue, pas un défaut technique.

---

## 3. Arbitrages toujours ouverts

Aucun n'est fermé artificiellement. Chacun conserve le statut que la preuve disponible autorise.

### RG-CPT-08 — 🔵 N/A / À ARBITRER

> « Un compte transporteur/chauffeur est un type de compte distinct du compte client, dédié au module
> Enrôlement Transporteurs. »

**Preuve disponible.** `NatureUtilisateur` = { Client, Agent } — vérifié au moment du recalcul. La
distinction existe **fonctionnellement** (parcours d'inscription dédié, absence de rattachement
société, module dédié, dossiers personnels reconnus par la chaîne d'autorisation) mais n'est pas
**nommée** dans le modèle.

**Question métier.** Le CDC exige-t-il une nature distincte *dans le modèle*, ou la distinction
opérationnelle suffit-elle ?

Si « dans le modèle » : lot dédié, **13 sites impactés dont 7 à conséquence de sécurité**. Le
préalable technique reste de nommer la notion d'**usager externe** — les 13 sites écrivent
`Nature == Client`, confondant « n'est pas un agent » et « est un client d'entreprise ».

### RG-COM-01 — 🟡 PARTIELLEMENT CONFORME

**Preuve disponible.** Six occurrences de `CanalJournalise` dans `DependencyInjection.cs` — vérifié :
les trois canaux externes (e-mail, SMS, WhatsApp) pointent tous sur une implémentation qui journalise
et n'envoie rien. La notification portail, elle, atteint réellement le client, et
`GabaritNotification.GarantirLienVersTicket` assure le lien.

**Ce qui manque n'est pas du code** : résolution des destinataires, gabarits, règles de diffusion, file
de reprise sont livrés et testés. Il manque le **choix d'un fournisseur — PC-16**.

Tant que la décision n'est pas prise, **aucun canal mobile n'atteint le client**, et la règle ne peut
pas être notée ✅.

### RG-IA-01 — 🟡 PARTIELLEMENT CONFORME

**Preuve disponible.** `AssistantSimule` porte 8 sujets déclenchés par appariement de sous-chaînes —
vérifié. Hors de ces sujets, l'assistant escalade avec un motif plutôt que de deviner.

| Exigence | État |
|---|---|
| Répond en français aux questions de premier niveau | 🟡 sur 8 sujets |
| Oriente le client | 🟡 dans ce périmètre |
| **L'aide à compléter ses dossiers** | ❌ aucun accès au dossier |

**Deux décisions** : le moteur et le fournisseur, puis le périmètre livré — « aider à compléter ses
dossiers » est une capacité nouvelle, pas un adaptateur.

*RG-IA-02 reste ✅ : l'escalade transmet le fil intégral, rattaché au ticket.*

---

## 4. Dettes techniques hors 98 RG

**Aucune de ces dettes n'entre au dénominateur.** Elles sont réelles, elles sont documentées, et elles
ne relèvent d'aucune des 98 règles.

| # | Dette | Sévérité | Impact | Recommandation | Bloque production |
|---|---|---|---|---|---|
| 1 | **Chaîne de rétention SEC-06 non déclenchée** — `GestionnaireRetentionService` → `MoteurRetentionService`, enregistrés, testés, aucun service hébergé ne les appelle | **HIGH** | Aucune purge, aucune anonymisation, aucun gel légal ne s'exécute jamais. Exposition réglementaire : on conserve au-delà des délais. **Aucune perte de donnée possible** — c'est l'effacement qui n'a pas lieu, pas la conservation | Écrire le `BackgroundService` manquant sur le modèle des six existants, ou exposer une commande d'administration. Décision à porter au CDC | **NON** |
| 2 | **Actions en masse `ComptesAgents` non atomiques** — `AttribuerToutesAsync`, `RetirerToutesAsync`, `BasculerGroupeAsync` bouclent N appels sans transaction | **MEDIUM** | Un échec au n-ième appel laisse les n−1 précédents validés. Cas concret : « tout retirer » sur soi-même dépouille l'administrateur puis lève sur `administration.comptes` | Encadrer la boucle par une unité de travail, ou refuser l'action sur soi-même en amont | **NON** |
| 3 | **Absence de wrapper `AccesRefuseException` sur ces actions** — les quatre méthodes n'utilisent pas `ExecuterAsync` | **MEDIUM** | Un refus serveur remonte en exception non traitée dans le circuit Blazor au lieu du panneau de refus | Router les quatre méthodes par le wrapper existant | **NON** |
| 4 | **Couplage `ValidationInterval = TimeSpan.Zero` ↔ clôture de compte** | **MEDIUM** | La promesse affichée « vos sessions ont été déconnectées » dépend de ce réglage. Le relever pour raisons de performance la rendrait fausse | Documenter la dépendance à l'endroit du réglage avant toute optimisation | **NON** |
| 5 | **`workflow.deroger` impossible à attribuer** — constante locale à `DerogationWorkflowService`, absente de `Permission.Tous` | **LOW** | `GererHabilitationAgentCommand` refuse les codes inconnus : personne ne peut détenir cette permission | Traiter avec la dette n° 6 — même décision | **NON** |
| 6 | **`DerogationWorkflowService` inatteignable** | **LOW** | Nul — RG-WKF-02/03 sont servies par `TransfererTicketCommand` et `ReaffecterTicketCommand`, matrice comprise | Supprimer, ou câbler et déclarer la permission | **NON** |
| 7 | **`CreerVersionGrilleTarifaireCommandHandler` doublonné et non câblé** | **LOW** | Nul — RG-SIM-11 est servie par l'import CSV, seul exposé à l'écran | Supprimer, ou exposer la saisie structurée | **NON** |
| 8 | **2FA d'Identity non éprouvé de bout en bout** | **LOW** | La **règle** d'accès l'est (`RegleAccesConnexionTests`) ; le mécanisme tiers ne l'est pas | Test d'intégration sur le parcours complet, ou acceptation formelle | **NON** |
| 9 | **Clôture de compte en deux transactions** — audit et rattachement validés avant la désactivation Identity | **LOW** | Un échec intermédiaire enregistre une clôture qui n'a pas eu lieu | Unifier la transaction | **NON** |
| 10 | **Parallélisme de la suite** — contention LocalDB entre projets exécutés simultanément | **LOW** | Échecs transitoires trompeurs. Vérifié : les projets exécutés seuls rendent 1424/1424 | `xunit.runner.json` limitant le parallélisme | **NON** |
| 11 | **`ComptesAgents.razor` sans test de rendu** | **LOW** | La page a doublé de taille sans couverture d'écran. Les commandes sous-jacentes sont testées | Test bUnit sur les actions en masse | **NON** |

**Total : 11 dettes — 1 HIGH, 3 MEDIUM, 7 LOW. Dettes bloquantes : 0.**

La dette n° 1 est la seule qui touche une obligation externe. Elle ne bloque pas la mise en service —
rien n'est perdu, tout est conservé — mais elle doit être planifiée, et non laissée en l'état.

---

## 5. GO / NO-GO

> ### A. Conformité 98 RG : 🟠 **GO AVEC RÉSERVES**

**99,0 %. Zéro non-conformité, zéro règle non implémentée, 20 domaines sur 22 à 100 %.**

Réserves — les trois arbitrages non rendus, et eux seuls :

1. **RG-COM-01** — aucun canal mobile n'atteint réellement le client tant que PC-16 n'est pas tranché.
2. **RG-IA-01** — l'assistant rend 8 réponses par mots-clés ; l'aide à la complétion n'existe pas.
3. **RG-CPT-08** — la nature transporteur n'est pas nommée dans le modèle.

Aucune de ces réserves n'est technique. Aucune ne se lève par du développement seul.

> ### B. Dette technique : 🟠 **À TRAITER**

Onze dettes, aucune bloquante. La chaîne de rétention SEC-06 domine l'ensemble : c'est la seule qui
engage une obligation externe, et la seule classée HIGH.

Les dix autres sont des corrections bornées — deux suppressions de code mort, deux robustesses
d'interface, un couplage à documenter, quatre points de test et de transaction.

> ### C. Production : 🟠 **GO SOUS CONDITIONS**

Aucun obstacle de sécurité. La chaîne d'autorisation est unique, ordonnée, protégée par des tests de
câblage, et le contrôle au niveau champ — dernier écart de sécurité de l'audit — est désormais câblé
et éprouvé.

```
[ ] 1. R-15 sur copie de production              BLOQUANT (base peuplée)
[ ] 2. Décision PC-16                            canaux mobiles
[ ] 3. Décision assistant IA                     moteur + périmètre
[ ] 4. Décision RG-CPT-08                        modèle ou opérationnel
[ ] 5. Planifier le déclenchement de la rétention SEC-06     non bloquant
```

### R-15 — évalué séparément

> **GO SOUS CONDITIONS**

Le code du lot 3FN est conforme (RG-SIM-11 et RG-SIM-12 ✅). Le script de reprise est écrit,
transactionnel, idempotent, et répété sur SQL Server — nominal, idempotence, rollback, continuation
de la chaîne EF.

**Il n'a pas été confronté à une copie des données de production.** La condition est inchangée :
confronter les valeurs réellement présentes aux correspondances d'ordinaux de la section E, sur une
copie restaurée, avant toute exécution réelle. La répétition avait révélé trois erreurs que trois
relectures n'avaient pas vues — c'est pourquoi cette confrontation ne se présume pas.

Les deux migrations post-audit (`AjoutDraftInExport`, `AjoutAxeTypeClientSla`) sont additives,
nullables, réversibles, postérieures au lot 3FN et sur des tables disjointes de R-15 :
**aucune interférence**.

---

## Conclusion

La plateforme atteint **99,0 % de conformité aux 98 règles de gestion du modèle Merise**, sans
aucune non-conformité et sans aucune fonction manquante. Vingt domaines sur vingt-deux sont
intégralement conformes.

Ce qui reste tient à **trois décisions métier** — un fournisseur de notification, un moteur d'IA, une
nature de compte — et à **une validation d'exploitation**, R-15 sur données réelles. Aucune ne se
règle par du code.

Le trajet mérite d'être noté pour ce qu'il dit du reste. Le score est passé de 96,9 % à 99,0 %, et
les quatre règles gagnées avaient toutes la même signature : **une règle écrite que rien n'exerçait**.
`AutorisationChampService` existait sans appelant ; le Draft IN n'existait qu'en toutes lettres dans
le CDC ; l'axe SLA manquait au moteur autant qu'au modèle ; l'archivage était affirmé en commentaire.
Dans les quatre cas, la suite de tests était verte.

C'est aussi ce que rappelle la principale dette restante. La chaîne de rétention SEC-06 est complète,
testée, enregistrée — et rien ne la déclenche. Elle n'entre pas dans ce score parce qu'elle
n'appartient pas aux 98 règles ; elle mérite d'être traitée pour exactement la même raison que les
quatre précédentes.

**L'existence d'un code n'est pas la preuve d'un comportement.** Les quatre points gagnés viennent
d'avoir appliqué cette phrase plutôt que de l'avoir écrite.
