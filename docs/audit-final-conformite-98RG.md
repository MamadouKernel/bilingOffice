# Audit final de conformité

**98 règles de gestion — évaluation indépendante, état figé.**

---

## État Git audité

| | |
|---|---|
| **Commit audité** | **`1c3cd38260c3b196de8641b8ec1d5a89f27a9762`** |
| Branche | `main` |
| Message | `feat: finalise remédiations conformité et sécurité` |
| Worktree | **propre** — `git status --porcelain` : 0 ligne |
| Suite de référence | **1393 / 1393** — 0 échec, 0 test ignoré |
| Push | non effectué (`main...origin/main [ahead 1]`) |
| Date de l'audit | 2026-09-05 |

Les trois conditions préalables ont été vérifiées avant toute analyse. Aucun fichier n'a été modifié
pendant l'audit : les seules opérations conduites sont des lectures.

---

## Méthodologie

Les 98 règles ont été **reconstruites depuis la section 1 du document de référence**
(`Analyse_Merise_CIT_Plateforme_Digitale.docx`), non depuis une annexe ni depuis un audit
antérieur. Le décompte par domaine confirme le total :

```
CPT  8  ·  TCK 15  ·  MSG  4  ·  WKF 11  ·  TPS  3  ·  SLA  4  ·  ALE  2  ·  DIS  2
AFF  1  ·  PRI  2  ·  MAS  1  ·  IMP  9  ·  EXP  4  ·  SIM 12  ·  AVE  2  ·  REC  3
ENR  2  ·  COM  3  ·  NOT  2  ·  CON  2  ·  IA   2  ·  SEC  4         ───────  98
```

**Aucun statut antérieur n'a été recyclé.** Chaque règle a été réévaluée depuis le code de ce commit.

### La règle de preuve appliquée

Conformément à la mission, aucune règle n'est déclarée conforme sur la seule existence d'une
méthode, d'un service, d'un drapeau, d'une table ou d'un test isolé. Pour chaque règle, la question
posée est : **un utilisateur réel atteint-il ce comportement ?**

Cette exigence n'est pas rhétorique. Elle a produit deux constats que les audits précédents n'avaient
pas relevés — l'un porte sur la sécurité (RG-SEC-01), l'autre sur un parcours métier (RG-EXP-03).
Dans les deux cas, le code existe, les tests passent, et le comportement n'est jamais atteint.

---

## Résumé exécutif

**Conformité globale : 96,9 %.** Aucune règle non conforme, aucune règle non implémentée.

| Statut | Nombre |
|---|---|
| ✅ CONFORME | **91** |
| 🟡 PARTIELLEMENT CONFORME | **6** |
| 🔴 NON CONFORME | **0** |
| ⚫ NON IMPLÉMENTÉ | **0** |
| 🔵 N/A / À ARBITRER | **1** |
| ⚠️ NON VÉRIFIABLE | **0** |

Les six partiels se répartissent en deux natures très différentes, et les confondre fausserait la
lecture.

**Deux relèvent d'une décision métier non rendue**, pas d'un défaut technique — RG-COM-01 (PC-16) et
RG-IA-01. RG-CPT-08, classée 🔵, appartient à la même famille. Le code est prêt, ou l'architecture
l'est ; ce qui manque est un arbitrage.

**Quatre sont de vrais écarts fonctionnels**, tous mineurs, aucun bloquant :

- **RG-SEC-01** — le contrôle d'accès *au niveau des champs* qu'exige le CDC pour le traitement
  parallèle IES **n'est câblé nulle part**. `AutorisationChampService` est enregistré dans le
  conteneur, couvert par deux suites de tests, et n'a **aucun appelant en production**. L'exposition
  réelle est nulle — les pages IES n'affichent aucun bloc financier — mais le contrôle nommé par le
  CDC n'est pas appliqué.
- **RG-EXP-03** — le **Draft IN** que la règle exige avant l'édition de la facture export **n'existe
  pas dans le code**. Recherche exhaustive : aucune occurrence.
- **RG-SLA-01** — le paramétrage SLA porte cinq axes sur les six énumérés. La **catégorie de client**
  manque.
- **RG-TCK-06** — un ticket clôturé reste consultable, mais l'archivage n'est pas matérialisé comme
  état.

**La sécurité est saine.** Les quatre défauts d'accès successifs du projet — P0, P0-bis, P0-ter,
consolidation — sont fermés, et la chaîne d'autorisation est unique, ordonnée, et protégée par des
tests de câblage qui lisent les sources.

---

## Matrice 98 RG

| RG | Domaine | Statut | Preuve code | Preuve test | Commentaire |
|---|---|---|---|---|---|
| RG-CPT-01 | Comptes | ✅ | `SoumettreDemandeCompteCommand` + `ValiderOuRejeterDemandeCompteCommand`, `Permission.CompteValider` | `ValiderDemandeCompteTests`, `ConcurrenceValidationCompteSqlTests` | Activation impossible sans décision d'un collaborateur habilité |
| RG-CPT-02 | Comptes | ✅ | `TypeClient` — énumération fermée à 4 valeurs | `SoumettreDemandeCompteTests` | Aucune évolutivité, conforme au CDC |
| RG-CPT-03 | Comptes | ✅ | `ValiderTicketImportCommand:45`, `EditerFactureCommand:40` | `AiguillageTypeClientTests`, `CircuitClient*Tests` (×8) | Le profil pilote l'aiguillage des 4 circuits |
| RG-CPT-04 | Comptes | ✅ | `ControleAccesRessourceService.ClientEstAdmisAsync` — décision unique | `SecCert02CloisonnementTests`, `ParcoursCloisonnementMultiTenantE2ETests` | Deux formes de propriété admises, tout le reste refusé |
| RG-CPT-05 | Comptes | ✅ | `Dossier.TypeClient` — instantané figé à la création ; audit `TypeClientModifie` | `NonRetroactiviteTypeClientSqlTests`, `SnapshotTypeClientExportTests` | L'aiguillage ne relit **jamais** le profil courant |
| RG-CPT-06 | Comptes | ✅ | `UtilisateurSociete`, `GererRoleAdministrateurCommand`, invariant CLAR-03 | `DesactivationDernierAdministrateurTests`, `ConcurrenceDernierAdministrateurTests` | La société ne peut jamais devenir orpheline |
| RG-CPT-07 | Comptes | ✅ | `DroitConsultation` + `VisibiliteDemandesFiltre`, appliqué dans `ControleAccesRessourceService:280` | `DeterminerVisibiliteDemandesTests`, `PorteeFileAvoirExonerationTests` | ENT-03 appliqué **dans** la décision d'accès, pas à côté |
| RG-CPT-08 | Comptes | 🔵 | `NatureUtilisateur` = { Client, Agent } | `InscrireTransporteurCommandTests` | Distinction **fonctionnelle** réelle (parcours dédié, absence de société, module dédié) mais **non nommée** dans le modèle — arbitrage métier |
| RG-TCK-01 | Ticket | ✅ | `INumerotationService.ProchainNumeroTicketAsync`, unicité en base | `NumerotationTests`, `TicketSocleTests` | Numéro unique communiqué au client |
| RG-TCK-02 | Ticket | ✅ | `ObtenirFicheTicketQuery` — documents, statut, historique, messages, décisions | `ObtenirFicheTicketQueryTests`, `FicheTicketTests` | Le ticket centralise effectivement tout son traitement |
| RG-TCK-03 | Ticket | ✅ | Module + aiguillage par `dossier.TypeClient` | `CircuitClient*Tests` | Module × type de client |
| RG-TCK-04 | Ticket | ✅ | `Dossier` + `SoumettreDemandeImportCommand` multi-BL | `SoumettreDemandeImportTests`, `AtomiciteMultiBlSqlTests` | 1 à 5 BL → n tickets, 1 dossier |
| RG-TCK-05 | Ticket | ✅ | `LiaisonTicket`, `LierTicketsCommand` | `LiaisonInterTicketsTests` | Liaison inter-modules supportée |
| RG-TCK-06 | Ticket | 🟡 | Ticket clôturé consultable ; `DateArchivageUtc` alimenté par `MarquerAnonymise` (rétention) **seulement** | `ClotureArchivageTests`, `ConsulterDocumentsTicketClotureQuery` | **Consultable : oui. Archivé : pas comme état** |
| RG-TCK-07 | Ticket | ✅ | `AnnulerTicketCommand.MotifDeBlocageAsync` — `EstEnEtatEngageant` **et** `EtapeBloquanteAnnulation` paramétrable par workflow | `AnnulationSeuilBloquantTests`, `AnnulationTests` | Le seuil est administrable, pas codé |
| RG-TCK-08 | Ticket | ✅ | `AnnulerTicketCommand` — `DemandeAnnulationSoumise` puis décision agent | `AnnulationTests` | Voie de repli complète |
| RG-TCK-09 | Ticket | ✅ | Motif obligatoire (`ArgumentException`), `ConsignerHistoriqueAsync` + `ConsignerAuditAsync` | `AuditDistinctionAnnulationRejetTests` | Motivé, horodaté, conservé |
| RG-TCK-10 | Ticket | ✅ | `RelancesAbsenceReponseUseCase` + service de fond ; clôture = `Permission.TicketClotureAbsenceReponse` | `RelancesAbsenceReponseTests` | La suggestion est produite, la clôture reste manuelle |
| RG-TCK-11 | Ticket | ✅ | `Ticket.EstEligibleASuggestionDeCloture` : `&& !EstEnEtatEngageant` ; `MarquerEtatEngageant()` appelé par **6 commandes** de production | `CablageEtatEngageantTests` (9 cas) | Le drapeau était autrefois sans appelant — il en a six, vérifiés par câblage |
| RG-TCK-12 | Ticket | ✅ | `ReouvrirTicketCommand` — `Permission.TicketRouvrir` + motif obligatoire ; le client ne peut que **demander** | `ReouvertureTests` | Numéro, historique, documents conservés |
| RG-TCK-13 | Ticket | ✅ | `BlDejaActifException`, index unique filtré sur tickets actifs | `ControleUniciteBlTests`, `ConcurrenceUniciteBlSqlTests`, `ControleUniciteBlWebTests` | Le contrôle ne porte pas sur l'historique |
| RG-TCK-14 | Ticket | ✅ | Contrôle appliqué BL par BL dans la boucle de soumission | `SoumettreDemandeImportTests`, `AtomiciteMultiBlSqlTests` | Réutilisation du BL possible hors module |
| RG-TCK-15 | Ticket | ✅ | Unicité booking implémentée à l'export | `UniciteBookingConcurrenceSqlTests`, `ControleUniciteBookingWebTests` | La règle dit « pourra s'appliquer » : elle **est** appliquée |
| RG-MSG-01 | Échanges | ✅ | `TypeMessage.NoteInterne` ; `ListerVisiblesParClientAsync` vs `ListerParTicketAsync` (`ObtenirFicheTicketQuery:112`) | `NotesInternesTests`, `MessagerieFicheTicketNonRegressionTests` | Deux natures, filtrage côté serveur |
| RG-MSG-02 | Échanges | ✅ | `MessageItemVue.EstInterne` ; rendu distinct `FicheTicket.razor:394` | `MessagesTests` | Horodatés et historisés |
| RG-MSG-03 | Échanges | ✅ | `MentionnerCollaborateurCommand` + `ResolveurCollaborateurMentionne` | `RaccordementMentionTests` | Notification effective |
| RG-MSG-04 | Échanges | ✅ | `MentionnerCollaborateurCommand:43` — assertion explicite sur `OperateurAssigneId` | `MentionSansReaffectationTests` | La mention ne réaffecte jamais |
| RG-WKF-01 | Workflow | ✅ | Files en base, aiguillage par module + type de demande + profil | `MatriceTransitionsTests`, `CircuitClient*Tests` | Ni figé ni codé en dur |
| RG-WKF-02 | Workflow | ✅ | `DerogationWorkflowService` | `DerogationWorkflowTests` | Dérogation soumise aux habilitations |
| RG-WKF-03 | Workflow | ✅ | `ConsignerAuditAsync` sur chaque dérogation | `DerogationWorkflowTests` | Historique + journal d'audit |
| RG-WKF-04 | Workflow | ✅ | `StatutTicket.EnAttenteDePriseEnCharge` | `ConsulterFileEtPriseLibreTests` | État initial en file |
| RG-WKF-05 | Workflow | ✅ | `PrendreEnChargeCommand` (permission bornée file) + `AffecterManuellementCommand` | `RecSce09PriseEnChargeTests`, `PriseEnChargeConcurrenceTests` | Concurrence maîtrisée par jeton optimiste |
| RG-WKF-06 | Workflow | ✅ | `PassageEnFile` — arrivée, prise en charge, opérateur, dernière activité | `SuiviPassagesEnFileTests`, `MesureTempsParEtapeTests` | Délai de prise en charge calculé |
| RG-WKF-07 | Workflow | ✅ | `TransfererTicketCommand` — le ticket conserve numéro, dossier, historique | `RecSce10TransfertTests` | Le client ne recrée rien |
| RG-WKF-08 | Workflow | ✅ | Motif obligatoire + référentiel `MotifTransfert` ; audit origine/destination | `MotifsTransfertReferentielTests`, `MotifTransfertTests` | Motif contraint à un référentiel |
| RG-WKF-09 | Workflow | ✅ | `ReaffecterTicketCommand` — `Permission.TicketReaffecter`, non réservée au superviseur | `RecSce11ReaffectationTests`, `TransfertSansAffectationTests` | Conforme à TRF-04 |
| RG-WKF-10 | Workflow | ✅ | `TransfererTicketCommand:78` publie l'événement, consommé par `DistribuerNotificationsUseCase` | `DistribuerNotificationsUseCaseTests` | Notification portail effective ; canaux externes → RG-COM-01 |
| RG-WKF-11 | Workflow | ✅ | `TransitionAutorisee` + `TransitionNonAutoriseeException` | `MatriceTransitionsTests` | Destination non autorisée = refus |
| RG-TPS-01 | Temps | ✅ | `MesureTemps.Calculer` — attente et traitement bornés par la file courante | `MesureTempsParEtapeTests` | Mesure par étape |
| RG-TPS-02 | Temps | ✅ | `DureeAttenteClienteCumulee`, `PeriodeAttenteClient` | `AttenteClientTests`, `RecSce12TempsSlaTests` | Jamais imputée à l'opérateur |
| RG-TPS-03 | Temps | ✅ | `TempsTotal` = création → clôture ; `TempsInterneCit` = total − attente client | `MesureTempsParEtapeTests` | Défaut corrigé : le total repartait autrefois de l'arrivée en file |
| RG-SLA-01 | SLA | 🟡 | `ParametreSlaAdministrable` : `Module`, `TypeDemande`, `FileId`, `PrioriteId` ; l'« étape » est assumée comme la file, justifié en commentaire | `ParametresSlaAxesTests`, `ParametreSlaDureeCibleTests` | **L'axe « catégorie de client » manque** — 5 axes sur 6 |
| RG-SLA-02 | SLA | ✅ | `SeuilInformation`, `SeuilAlerte`, dépassement = durée cible, `SeuilEscalade` contraint > 100 % | `MoteurSlaTests` | Les quatre niveaux du CDC |
| RG-SLA-03 | SLA | ✅ | SLA suspendu pendant `EnAttenteClient` | `SurveillerEcheancesCalendrierTests`, `RecSce12TempsSlaTests` | Ne dégrade pas la mesure |
| RG-SLA-04 | SLA | ✅ | `GererParametresSlaCommand` + page `ParametresSla.razor` | `AdministrationRedefinitionTests` | Administrable sans développement |
| RG-ALE-01 | Alertes | ✅ | `TypeAlerte` — 8 valeurs couvrant les 6 cas du CDC | `NfrCert03DisponibiliteTests`, `SurveillerEcheancesCalendrierTests` | Non prise en charge, sans activité, SLA ×3, backlog, volume, opérateur indisponible |
| RG-ALE-02 | Alertes | ✅ | `RegleEscalade` + `ActionEscalade`, administrable | `RegleEscaladeTests` | Notification, remontée de priorité, réaffectation |
| RG-DIS-01 | Dispo | ✅ | `FiltreDisponibilite.SeulementDisponibles` appliqué par toutes les stratégies | `DisponibiliteExclusionTests`, `DisponibiliteTests` | Exclusion structurelle, pas conditionnelle |
| RG-DIS-02 | Dispo | ✅ | `GererDisponibiliteCommand`, `ActionsSuperviseurCommand`, `ActionDeMasseCommand` | `ActionsDeMasseTests`, `ActionsSuperviseurCablageTests` | Identification + réaffectation en masse |
| RG-AFF-01 | Affectation | ✅ | `RoundRobinStrategy`, `ChargeStrategy`, `DisponibiliteStrategy`, `CompetenceStrategy` + prise libre + manuelle ; mode porté par la file | `StrategieAffectationParametrableTests`, `DistributionCompetenceTests` | 6 modes, paramétrables par file |
| RG-PRI-01 | Priorité | ✅ | `Priorite` (référentiel), `ModifierPrioriteCommand` avec audit ancienne/nouvelle valeur | `PriCert01PrioriteTests`, `PrioriteTests` | Auteur et motif tracés |
| RG-PRI-02 | Priorité | ✅ | `ModifierPrioriteCommand:26` — `Permission.PrioriteModifier` exigée ; aucun champ priorité dans les formulaires client | `PrioriteTests` | Le client ne peut pas se déclarer urgent |
| RG-MAS-01 | Masse | ✅ | `ActionDeMasseCommand:33` — boucle avec `ConsignerAuditAsync` **par ticket**, y compris en cas d'échec | `ActionsDeMasseTests` | Tracé ticket par ticket |
| RG-IMP-01 | Import | ✅ | `SoumettreDemandeImportCommand` — 1 à 5 BL, un ticket indépendant par BL | `SoumettreDemandeImportTests`, `AtomiciteMultiBlSqlTests` | Documents, historique, statut propres |
| RG-IMP-02 | Import | ✅ | `ValiderTicketImportCommand`, `RejeterTicketImportCommand` (motif obligatoire), `GestionComplementManifesteCommand` | `InstructionManifesteTests` | Les trois issues du Pilote Manifeste |
| RG-IMP-03 | Import | ✅ | `GestionComplementManifesteCommand` — retour en file Manifeste après dépôt | `InstructionManifesteTests` | Boucle de complément fermée |
| RG-IMP-04 | Import | ✅ | `ValiderTicketImportCommand:45-66` — aiguillage sur l'instantané | `AiguillageTypeClientTests` | Les 4 profils orientés |
| RG-IMP-05 | Import | ✅ | `DemanderFactureCommand` → `EditerFactureCommand` → `DeposerPreuvePaiementCommand` → `ValiderPreuvePaiementCommand` → contrôle documentaire → clôture | `CircuitClientNormalTests`, `RecSce01ImportNormalTests` | Parcours complet |
| RG-IMP-06 | Import | ✅ | `EditerFactureCommand:47` — `EnCompte` → contrôle documentaire directement | `CircuitClientEnCompteTests`, `RecSce02ImportEnCompteTests` | Pas de renvoi au client |
| RG-IMP-07 | Import | ✅ | `EditerFactureCommand:48` → comptabilité ; `ConfirmerLettrageCommand` | `CircuitClientDepositTests`, `RecSce03ImportDepositTests` | Lettrage confirmé dans le ticket |
| RG-IMP-08 | Import | ✅ | `SuiviBrancheIes` ×2 dès la validation ; `ConstaterJointureIesEtFinaliserCommand` ; `JointureIesIncompleteException` | `CircuitClientIesTests`, `ConcurrenceIesFinalisationSqlTests`, `RecSce04ImportIesTests` | Retour client impossible avant jointure |
| RG-IMP-09 | Import | ✅ | `DeclarerFinalisationPresentielleCommand`, `Ticket.DeclarationPresentielle` | `DeclarationPresentielleTests`, `AlternativePresentielleTests` | Bascule tracée |
| RG-EXP-01 | Export | ✅ | `SoumettreDemandeExportCommand` — un ticket par booking | `SoumettreDemandeExportCommandTests`, `MultiBookingsIndependanceTests` | Indépendance vérifiée |
| RG-EXP-02 | Export | ✅ | `ValiderTicketExportCommand`, `RejeterTicketExportCommand`, `GestionComplementExportCommand` | `InstructionBackOfficeTests` | Codifié / rejeté / complément |
| RG-EXP-03 | Export | 🟡 | Aiguillage par type ✅ (`EditerFactureExportCommand:49-51`) ; **BEE** ✅ (`FinaliserControleDocumentaireExportCommand`) ; **Draft IN : aucune occurrence dans le code** | `CircuitClient*ExportTests`, `RecSce05ExportE2ETests` | **Le Draft IN exigé avant l'édition de la facture n'existe pas** |
| RG-EXP-04 | Export | ✅ | `ConstaterJointureIesExportEtFinaliserCommand` ; facture et BEE hors plateforme | `CircuitClientIesExportTests`, `ConcurrenceIesExportFinalisationSqlTests` | Traitement parallèle puis remise |
| RG-SIM-01 | Simulation | ✅ | `DossierSimulation` + conteneurs (numéro, sens, taille, type, poids, OOG, dangereux, catégorie) | `CalculerSimulationProformaQueryTests`, `SimulationProformaWebTests` | Saisie complète |
| RG-SIM-02 | Simulation | ✅ | `LigneTarifaire` indexée Sens + Taille + Catégorie | `TarificationSqlPersistenceTests`, `SimCert11ValidationsTests` | Clé exacte du CDC |
| RG-SIM-03 | Simulation | ✅ | `MoteurCalculProforma` — acconage, relevage, branchement, lourd, dangereux, OOG, stationnement + répartition | `GoldenMasterProformaTests`, `MajorationsTarifairesTests` | Les 7 composantes |
| RG-SIM-04 | Simulation | ✅ | `DureeSejourJours`, bornes incluses | `StationnementEtReeferTests` | Alimente branchement et stationnement |
| RG-SIM-05 | Simulation | ✅ | `MoteurCalculProforma:72-75` — Reefer uniquement, franchise 2 j paramétrable | `StationnementEtReeferTests` | Conforme Annexe A.3 |
| RG-SIM-06 | Simulation | ✅ | `ParametrageInitialAnnexeA:124,129-130` — 30 %, seuils 15 t / 26 t | `MajorationsTarifairesTests` | Indépendant de la catégorie |
| RG-SIM-07 | Simulation | ✅ | `ParametrageInitialAnnexeA:125-126` — 50 % / 50 %, cumulables | `MajorationsTarifairesTests` | Cumul vérifié |
| RG-SIM-08 | Simulation | ✅ | Tranches 850 / 1 250 / 1 850 ; franchises 11 j local, 21 j transit ; export non applicable | `StationnementEtReeferTests`, `GoldenMasterProformaTests` | Conforme Annexe A.4 |
| RG-SIM-09 | Simulation | ✅ | `QuotePartBeneficiaire` — CIT 30 %, OIC 20 %, PAA 50 % | `GoldenMasterProformaTests` | Quotes-parts paramétrables |
| RG-SIM-10 | Simulation | ✅ | Somme des composantes ; total dossier ; mention non engageante | `GoldenMasterProformaTests`, `AuditPreuveArrondiFrancTests` | Arrondi au franc, exact |
| RG-SIM-11 | Simulation | ✅ | `ConsulterReferentielsTarifairesQuery`, `ImporterGrilleTarifaireCsvCommand`, `Permission.TarificationAdministrer` | `ImportGrilleTarifaireCsvTests`, `AdministrationTarifaireAutorisationTests` | Administrable sans développement |
| RG-SIM-12 | Simulation | ✅ | `VersionGrilleTarifaire.DateEffetUtc` ; `SimulationProforma.VersionGrilleTarifaireId` + libellé | `ReproductibiliteHistoriqueSimulationTests` | Une simulation historique recalcule à l'identique |
| RG-AVE-01 | Avoir/Exo | ✅ | `SoumettreDemandeAvoirExonerationCommand`, workflow propre, `PieceExigeeEtape` paramétrable | `SoumettreDemandeAvoirExoTests`, `InstructionAvoirExoTests` | Étapes et intervenants paramétrables |
| RG-AVE-02 | Avoir/Exo | ✅ | `DeciderDemandeAvoirExoCommand` — `DecisionAvoirExo`, motif de rejet, notification | `DeciderAvoirExoTests`, `AveSpecTests` | Décision et documents dans le ticket |
| RG-REC-01 | Complaint | ✅ | `CategorieReclamation` (référentiel dynamique), `SoumettreReclamationCommand` | `RecCert02CategoriesTests`, `SoumettreReclamationCommandTests` | Catégories administrables |
| RG-REC-02 | Complaint | ✅ | `ConsulterFileReclamationsQuery`, `Permission.ComplaintInstruire`, transfert vers autres files | `RecCert03OrientationSupportTests`, `FileSupportTests` | Instruction complète |
| RG-REC-03 | Complaint | ✅ | `CloturerReclamationCommand` — `ReponseFormalisee` obligatoire dans la requête | `CloturerReclamationCommandTests` | Conservée et restituée dans la fiche |
| RG-ENR-01 | Enrôlement | ✅ | `InscrireTransporteurCommand`, `SoumettreEnrolementCommand`, pièces paramétrables | `SoumettreEnrolementCommandTests`, `DepotPieceEnrolementIntegrationTests` | Saisie à distance |
| RG-ENR-02 | Enrôlement | ✅ | `DeclencherConvocationCommand`, `ConstaterRemiseBadgeCommand` | `CycleCompletEnrolementIntegrationTests`, `RecSce08EnrolementTransporteurE2ETests` | Convocation et remise tracées |
| RG-COM-01 | Notifications | 🟡 | 3 canaux enregistrés en DI — **tous `CanalJournalise`** (`DependencyInjection:377-385`) ; notification portail effective ; `ILienPortail` garantit le lien | `ComCert06CanauxRelanceTests`, `SecCert06LienNotificationTests` | **Aucun canal mobile n'atteint réellement le client** — PC-16 ouvert |
| RG-COM-02 | Notifications | ✅ | `ModeleNotification` + `AdministrerModelesNotificationCommand`, ciblage module/étape/canal | `AdministrationEtPreferencesTests` | Paramétrable |
| RG-COM-03 | Notifications | ✅ | `PublierMessageCommand` — fil du ticket canal de référence | `MessagesTests` | Conforme |
| RG-NOT-01 | Notifications | ✅ | `Notification.EstLue` / `MarquerCommeLue` ; `TicketId` porté | `NotificationTests`, `CentreNotificationsTests` | Accès direct au ticket |
| RG-NOT-02 | Notifications | ✅ | `RegleDiffusion` — événement × canal, ciblage module/file, activable | `ReferentielsNotificationTests`, `AdministrationEtPreferencesTests` | Paramétrable par événement et canal |
| RG-CON-01 | Continuité | ✅ | `RechercherGuichetQuery` par numéro ; `ConsulterDossierGuichetQuery` restitue historique et étape | `RechercheGuichetTests`, `RecSce14ContinuiteGuichetRendezVousE2ETests` | Reprise sans ressaisie |
| RG-CON-02 | Continuité | ✅ | `ReserverRendezVousCommand`, `RendezVous`, agenda guichet | `ReserverRendezVousTests`, `GestionRendezVousTests` | Indépendant de la file physique |
| RG-IA-01 | Assistant | 🟡 | `AssistantSimule` — **8 sujets** par appariement de sous-chaînes ; aucun accès au dossier | `AssistantApplicationTests` | Répond et oriente sur 8 sujets ; **n'aide pas à compléter les dossiers** |
| RG-IA-02 | Assistant | ✅ | `EscaladerAssistantVersOperateurCommand` — fil intégral transmis, conversation rattachée au ticket | `ConversationAssistantTests`, `AssistantApplicationTests` | Escalade sans perte de contexte |
| RG-SEC-01 | Sécurité | 🟡 | Authentification ✅ ; RBAC par codes de permission ✅ (garde avant permission) ; **niveau champ : `AutorisationChampService` enregistré en DI, aucun appelant en production** | `AutorisationServeurTests`, `EtatCompteEtAutorisationTests` ; `AutorisationChampServiceTests` et `SecCert02RbacChampTests` instancient le service **directement** | **Le contrôle au niveau des champs n'est jamais appliqué** — exposition réelle nulle |
| RG-SEC-02 | Sécurité | ✅ | `ControleAccesRessourceService` — décision unique ; périmètres de supervision pour les agents | `SecCert02CloisonnementTests`, `CloisonnementAgentFichesTests`, `ApiCloisonnementTests`, `CablageControleAccesTests` (23 cas) | Cloisonnement client **et** agent |
| RG-SEC-03 | Sécurité | ✅ | `ConsignerAuditAsync` sur toutes les actions listées ; `JournalConnexionService` ; `JournalAuditAppendOnlyGuard` | `JournalAuditAppendOnlyTests`, `JournalConnexionTests`, `CablageJournalConnexionTests` | Journal append-only garanti applicativement |
| RG-SEC-04 | Sécurité | ✅ | `LienPortailConfigure.VersTicket` → `/tickets/{id}`, page `[Authorize]` + `ExigerConsultationTicketAsync` | `SecCert06LienNotificationTests` | Jamais d'accès direct au document |

**98 lignes.**

---

## Score global

```
Dénominateur = 98 − 1 (N/A) = 97 règles évaluées

CONFORME        91 × 1,0  =  91,0
PARTIEL          6 × 0,5  =   3,0
NON CONFORME     0 × 0    =   0,0
NON IMPLÉMENTÉ   0 × 0    =   0,0
                            ──────
                             94,0 / 97

CONFORMITÉ GLOBALE = 96,9 %
```

## Scores par domaine

| Domaine | Règles | ✅ | 🟡 | 🔵 | Score | % |
|---|---|---|---|---|---|---|
| Comptes et profils (CPT) | 8 | 7 | 0 | 1 | 7,0 / 7 | **100 %** |
| Ticket et cycle de vie (TCK) | 15 | 14 | 1 | 0 | 14,5 / 15 | 96,7 % |
| Échanges et notes (MSG) | 4 | 4 | 0 | 0 | 4,0 / 4 | **100 %** |
| Workflow et files (WKF) | 11 | 11 | 0 | 0 | 11,0 / 11 | **100 %** |
| Mesure des temps (TPS) | 3 | 3 | 0 | 0 | 3,0 / 3 | **100 %** |
| SLA | 4 | 3 | 1 | 0 | 3,5 / 4 | 87,5 % |
| Alertes (ALE) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Disponibilité (DIS) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Affectation (AFF) | 1 | 1 | 0 | 0 | 1,0 / 1 | **100 %** |
| Priorité (PRI) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Actions de masse (MAS) | 1 | 1 | 0 | 0 | 1,0 / 1 | **100 %** |
| Billing IMPORT (IMP) | 9 | 9 | 0 | 0 | 9,0 / 9 | **100 %** |
| Billing EXPORT (EXP) | 4 | 3 | 1 | 0 | 3,5 / 4 | 87,5 % |
| Simulation (SIM) | 12 | 12 | 0 | 0 | 12,0 / 12 | **100 %** |
| Avoir et Exonération (AVE) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Customer Complaint (REC) | 3 | 3 | 0 | 0 | 3,0 / 3 | **100 %** |
| Enrôlement (ENR) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Communication (COM) | 3 | 2 | 1 | 0 | 2,5 / 3 | 83,3 % |
| Notifications (NOT) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Continuité (CON) | 2 | 2 | 0 | 0 | 2,0 / 2 | **100 %** |
| Assistant IA (IA) | 2 | 1 | 1 | 0 | 1,5 / 2 | 75,0 % |
| Sécurité (SEC) | 4 | 3 | 1 | 0 | 3,5 / 4 | 87,5 % |
| **TOTAL** | **98** | **91** | **6** | **1** | **94,0 / 97** | **96,9 %** |

---

## Sécurité

### Ce qui est confirmé fermé

| Point | État | Preuve |
|---|---|---|
| **Source unique d'autorisation** | ✅ | `ControleAccesRessourceService` — une seule implémentation générique ; `CablageControleAccesTests` (23 cas) empêche qu'une règle soit réécrite localement |
| **Compte désactivé** | ✅ | `GardeIdentiteService` consulté **avant** les habilitations dans `AutorisationService.DeciderAsync` ; l'ordre lui-même est testé |
| **Session existante** | ✅ | `UpdateSecurityStampAsync` sur désactivation + `ValidationInterval = TimeSpan.Zero` — le cookie est rejeté à la requête suivante |
| **2FA** | ✅ | `RegleAccesConnexion` — règle unique appliquée par `Connexion.razor` **et** `VerifierCode.razor` ; elle a vécu en deux exemplaires dont l'un sans contrôle d'activité |
| **Agent** | ✅ | `CloisonnementAgentFichesTests`, `GardeTableauDeBordAgentTests` — un agent désactivé n'obtient pas même son nom |
| **Client** | ✅ | `ClientEstAdmisAsync` — deux formes de propriété, tout le reste refusé |
| **Transporteur** | ✅ | Dossier personnel reconnu (`dossier.ClientId == utilisateurId`), sans société |
| **Dossier / Ticket** | ✅ | `ExigerConsultationDossierAsync` / `ExigerConsultationTicketAsync`, autorisation **avant** toute lecture |
| **Documents** | ✅ | `AccesDocumentAutoriseTests`, `StockageDocumentsSecuriteTests` |
| **Notes internes** | ✅ | Filtrage côté serveur, fail-closed (`appelantInfo is null` → traité comme client) |
| **IDOR / BOLA** | ✅ | `ApiCloisonnementTests`, `ParcoursCloisonnementMultiTenantE2ETests` ; identité **jamais** issue de l'appelant |
| **Administration** | ✅ | Les pages d'administration passent par `AutorisationService` ; `SurfaceRoutableTests` interdit une page routable non protégée |

### Le constat nouveau — RG-SEC-01, niveau champ

Le CDC exige un contrôle d'accès « **y compris au niveau des champs (traitement parallèle IES)** ».
`AutorisationChampService` implémente exactement cette matrice — quatre blocs (`Manifeste`,
`Chauffeur`, `Douane`, `Financier`) × cinq rôles, avec un défaut `Masked` fail-secure. Il est
enregistré dans le conteneur (`DependencyInjection:474`).

**Il n'a aucun appelant en production.** Recherche exhaustive sur `src/` et `tests/` :

- aucune page Razor ne l'injecte ;
- aucun cas d'usage applicatif ne le référence ;
- les deux suites qui le couvrent — `AutorisationChampServiceTests`, `SecCert02RbacChampTests` —
  l'instancient **directement** (`new AutorisationChampService()`), sans passer par le conteneur.

C'est la forme exacte du défaut que le projet a déjà rencontré deux fois : une règle écrite, testée,
déclarée conforme, et sans effet. `ServicesAtteignablesTests` ne la détecte pas, parce que son corpus
d'exigence porte sur les classes `Command`, `Query` et `UseCase` — un `Service` en est hors.

**L'exposition réelle est nulle** : les deux pages IES (`FileIes.razor`, `FileIesExport.razor`)
n'affichent aucun champ financier, et tous les acteurs concernés sont des agents internes CIT. Le
risque est **latent**, non actif : il se matérialiserait le jour où l'un de ces écrans afficherait un
montant.

**Aucune régression de sécurité n'est constatée par ailleurs. Aucun P0, aucun P1.**

---

## MCD / MLD / MPD

| Point | État | Constat |
|---|---|---|
| **MCD** | ✅ | Les entités du domaine couvrent les 44 tables du modèle ; agrégats cohérents avec les règles |
| **MLD / MPD** | ✅ | EF Core 10, `PlateformeDbContext` explicite (pas de convention implicite sur les clés d'audit) |
| **Normalisation 3FN** | ✅ | Lot achevé : `Referentiels/` (SensOperation, TailleConteneur, TypeConteneur, CategorieMarchandise) et `Parametres/` (5 entités) remplacent les 4 fichiers supprimés dans ce commit |
| **Référentiels** | ✅ | `ProvisionnementReferentielsTarifaires` approvisionne l'Annexe A ; `ReferentielsTarifaires` en lecture |
| **Contraintes / FK / index** | ✅ | Index unique filtré sur BL actif et booking actif ; FK contraintes sauf journal d'audit (append-only, délibéré) |
| **Versionnement** | ✅ | `VersionGrilleTarifaire` — `NumeroVersion`, `DateEffetUtc`, `EstActive`, historique conservé |
| **Snapshots** | ✅ | `Dossier.TypeClient` (non-rétroactivité CPT-06) ; `SimulationProforma.VersionGrilleTarifaireId` + libellé |
| **Reproductibilité RG-SIM-12** | ✅ | `ReproductibiliteHistoriqueSimulationTests` — une simulation historique recalcule à l'identique après changement de grille |

---

## Workflows

Les onze règles WKF sont conformes. Trois points méritent d'être notés :

- La **matrice de transitions** est une donnée, pas du code : un transfert vers une destination non
  inscrite lève `TransitionNonAutoriseeException`.
- Les **motifs de transfert** sont contraints à un référentiel (`MotifTransfertInconnuException`) —
  un motif libre ne peut pas contourner la traçabilité.
- L'**action de masse** trace ticket par ticket **y compris quand un ticket échoue**, ce qui est la
  seule lecture honnête de MAS-02.

---

## Billing IMPORT

Les neuf règles sont conformes. Les quatre circuits (normal, en compte, deposit, IES) sont couverts
chacun par une suite applicative **et** un scénario de recette de bout en bout
(`RecSce01` → `RecSce04`).

Le point le plus solide est l'aiguillage : il lit `dossier.TypeClient`, l'instantané figé à la
création, et **jamais** le profil courant de la société. La non-rétroactivité de CPT-06 est donc une
propriété du modèle, pas une précaution ajoutée après coup — et elle est vérifiée sur SQL Server réel.

Le traitement parallèle IES refuse le retour client tant que les deux branches ne sont pas jointes
(`JointureIesIncompleteException`), et cette jointure est éprouvée en concurrence.

---

## Billing EXPORT

Trois règles sur quatre sont conformes.

**RG-EXP-03 est partielle.** La règle énonce deux adaptations par rapport à l'IMPORT :

| Adaptation exigée | État |
|---|---|
| Le document final du Contrôle Documentaire est le **BEE** | ✅ `FinaliserControleDocumentaireExportCommand` |
| Le Pilote Facturation crée un **Draft IN** avant l'édition de la facture | ❌ **aucune occurrence dans le code** |

`EditerFactureExportCommand` édite directement la facture et aiguille selon le type de client. Il
n'existe ni entité, ni commande, ni étape portant le Draft IN. Une recherche insensible à la casse
sur `src/` ne retourne que la chaîne `Common.DraftSaved`, sans rapport (sauvegarde de brouillon de
formulaire).

L'écart est **fonctionnel, pas structurel** : le parcours export fonctionne de bout en bout, un
document intermédiaire manque. Il ne bloque aucun autre traitement.

---

## Simulation

**Les douze règles sont conformes** — c'est le domaine le mieux tenu du projet.

Le moteur est un calcul pur, découplé de toute infrastructure, et son exactitude est établie par
`GoldenMasterProformaTests` contre les cas de l'Annexe A.8, au franc près
(`AuditPreuveArrondiFrancTests`). Tous les paramètres — grilles, taux, seuils, franchises, tranches,
quotes-parts — sont des données administrables, et le paramétrage initial de l'Annexe A est
approvisionné explicitement plutôt que codé dans le moteur.

RG-SIM-12 mérite une mention : une simulation ne conserve pas seulement l'identifiant de sa version
de grille mais aussi son **libellé**, et le test de reproductibilité vérifie qu'un recalcul après
publication d'une nouvelle grille rend le même montant.

---

## Customer Complaint

Les trois règles sont conformes. Les catégories sont un référentiel administrable et non une
énumération, ce que REC-01 exige explicitement (« paramétrable et dynamique »). La réponse formalisée
est un paramètre **obligatoire** de la requête de clôture — elle ne peut pas être oubliée.

---

## Enrôlement

Les deux règles sont conformes, avec un cycle complet éprouvé
(`CycleCompletEnrolementIntegrationTests`, `RecSce08EnrolementTransporteurE2ETests`) : soumission à
distance, pièces paramétrables, convocation, remise de badge, clôture.

C'est ce module qui rend RG-CPT-08 tangible : le transporteur y dispose de **dossiers personnels**
(`Dossier.ClientId` porte l'utilisateur, non une société), forme de propriété que la chaîne
d'autorisation reconnaît nommément.

---

## Notifications

Quatre règles sur cinq (COM + NOT) sont conformes. L'infrastructure est complète : résolution des
destinataires, gabarits interpolés, garantie du lien vers le ticket
(`GabaritNotification.GarantirLienVersTicket`), état lu/non lu, règles de diffusion paramétrables par
événement et par canal, file de reprise sur échec.

**RG-COM-01 reste partielle pour une seule raison** : les trois canaux externes sont enregistrés, mais
les trois pointent sur `CanalJournalise` — une implémentation qui écrit dans le journal et n'envoie
rien. La notification portail, elle, atteint réellement le client (elle **est** sa propre diffusion).

Aucun développement n'est en attente. Ce qui manque est le choix d'un fournisseur — **PC-16**.

---

## Assistant IA

**RG-IA-02 est conforme.** L'escalade transmet le fil intégral en message client, la conversation est
persistée et rattachée au ticket : il n'y a pas de perte de contexte.

**RG-IA-01 est partielle**, et il faut être précis sur ce qui manque :

| Exigence | État |
|---|---|
| Répond en français aux questions de premier niveau | 🟡 8 sujets, déclenchés par appariement de sous-chaînes |
| Oriente le client dans ses démarches | 🟡 dans le périmètre de ces 8 sujets |
| **L'aide à compléter ses dossiers** | ❌ aucun accès au dossier, aucune assistance à la saisie |

Hors de ces huit sujets, l'assistant ne devine pas : il escalade avec un motif. C'est un comportement
honnête, et délibéré. Mais **architecture prête ≠ service rendu** : le port
`IAssistantConversationnel` est propre et substituer un vrai moteur n'exigerait aucune refonte — ce
que le client obtient aujourd'hui reste huit réponses par mots-clés.

---

## Audit / traçabilité

**RG-SEC-03 est conforme, et c'est l'un des points les plus solides du système.**

Toutes les actions énumérées par la règle sont consignées : création, prise en charge, changement de
statut, dépôt, décision, transfert, réaffectation, téléchargement, **et connexion** — cette dernière
depuis `JournalConnexionService`, avec six actions distinctes et un motif de refus fermé par
énumération. Aucun mot de passe, code 2FA, jeton ou cookie n'entre dans le journal.

L'inviolabilité est garantie applicativement par `JournalAuditAppendOnlyGuard`, qui lève
`JournalAuditImmuableException` sur toute tentative de modification ou de suppression d'une entrée
existante — en complément des droits base, pas à leur place.

---

## R-15

**Le code est conforme.** L'évaluation du déploiement est distincte et ne le remet pas en cause.

| Aspect | Statut |
|---|---|
| **Conformité applicative** | ✅ Lot 3FN complet et cohérent : référentiels et paramètres normalisés, entités remplacées, migration `20260905033506_NormalisationReferentielsTarifaires` présente, persistance vérifiée sur SQL Server |
| Script de reprise | ✅ `scripts/reprise-r15/reprise-r15.sql` — 823 lignes, transactionnel, idempotent, journalisé |
| Répétition sur SQL Server | ✅ nominal, idempotence, rollback sur données incohérentes, poursuite de la chaîne EF |
| **Exécution sur copie de production** | ❌ **non réalisée** |

> ### R-15 — Déploiement : **GO SOUS CONDITIONS**

La condition est unique et précise : confronter les valeurs réellement présentes en base aux
correspondances d'ordinaux de la section E du script, sur une copie restaurée, **avant** toute
exécution réelle. La répétition a déjà révélé trois erreurs que trois relectures n'avaient pas vues —
c'est précisément pourquoi cette confrontation ne peut pas être présumée.

**Cette condition ne dégrade aucun statut de conformité applicative.**

---

## Risques résiduels

### 1. Conformité fonctionnelle

Écarts au CDC, indépendants de toute dette ou décision.

| Réf | Écart | Impact |
|---|---|---|
| RG-SEC-01 | Contrôle d'accès au niveau champ non câblé | Latent — aucun écran n'affiche aujourd'hui les blocs concernés |
| RG-EXP-03 | Draft IN absent du parcours export | Un document intermédiaire manque ; le parcours aboutit |
| RG-SLA-01 | Axe « catégorie de client » absent du paramétrage SLA | Un SLA ne peut pas être différencié par profil client |
| RG-TCK-06 | Archivage non matérialisé comme état | Le ticket reste consultable ; la promesse d'archivage n'est pas portée par le modèle |

### 2. Dette technique

| Sujet | Nature |
|---|---|
| `ValidationInterval = TimeSpan.Zero` | Une lecture du compte par requête, non mesurée. **Couplée à la clôture de compte**, dont la promesse « vos sessions ont été déconnectées » en dépend |
| 2FA d'Identity non éprouvé de bout en bout | La **règle** l'est (`RegleAccesConnexionTests`) ; le mécanisme tiers ne l'est pas |
| Clôture de compte en deux transactions | Audit et rattachement validés avant la désactivation Identity |
| Parallélisme de la suite | Contention LocalDB entre projets en exécution simultanée ; propre en série |
| `ServicesAtteignablesTests` ne couvre pas les `Service` | C'est ce qui a laissé `AutorisationChampService` inatteignable sans qu'aucun test ne tombe |

### 3. Risques résiduels classés

| Niveau | Nombre | Détail |
|---|---|---|
| **BLOCKER** | **1** | R-15 non confronté à une copie de production — **déploiement uniquement** |
| **HIGH** | **0** | — |
| **MEDIUM** | **3** | RG-SEC-01 (contrôle champ latent) ; couplage `ValidationInterval` ↔ clôture de compte ; RG-TCK-06 (promesse d'archivage non portée par le modèle) |
| **LOW** | **3** | 2FA non éprouvé bout-en-bout ; clôture en deux transactions ; parallélisme de la suite |
| **ACCEPTED** | **2** | `fileId: null` sur `ConsulterFileAvoirExonerationQuery` (sans effet, module mono-file) ; asymétrie nature/permission du filtre des notes internes (délibérée) |
| **DECISION REQUIRED** | **3** | RG-CPT-08 ; PC-16 / RG-COM-01 ; RG-IA-01 |

### 4. Décisions métier ouvertes

**RG-CPT-08 — nature transporteur.** La distinction existe fonctionnellement ; elle n'est pas nommée
dans le modèle. La question à trancher : *le CDC exige-t-il une nature distincte dans le modèle, ou
la distinction opérationnelle suffit-elle ?*

Si « nommée dans le modèle » : **lot dédié, 13 sites impactés dont 7 à conséquence de sécurité**. Une
addition naïve de `NatureUtilisateur.Transporteur` servirait les notes internes aux transporteurs sur
les fiches ticket et enrôlement, et leur ouvrirait cinq pages de file agent. Le préalable technique
est de nommer la notion d'**usager externe** — les 13 sites écrivent aujourd'hui `Nature == Client`,
confondant « n'est pas un agent » et « est un client d'entreprise ».

**PC-16 — canaux de notification.** Fournisseur e-mail / SMS / WhatsApp. Rien de technique n'est en
attente. Sans cette décision, RG-COM-01 reste 🟡.

**RG-IA-01 — assistant.** Choix du moteur et du fournisseur ; et décision sur le périmètre livré —
« aider à compléter ses dossiers » est une capacité nouvelle, pas un adaptateur.

### 5. Conditions de déploiement

```
[ ] 1. R-15 sur copie de production                      BLOQUANT
       Restaurer, confronter les valeurs réelles aux correspondances
       d'ordinaux (section E), exécuter, lire le journal, contrôler
       deux simulations historiques.

[ ] 2. Décision PC-16                                    canaux mobiles
[ ] 3. Décision assistant IA                             périmètre + moteur
[ ] 4. Décision RG-CPT-08                                modèle ou opérationnel
```

Ne figurent **pas** parmi les conditions, et sont documentés comme dette ou comme écart fonctionnel à
planifier : la mesure de charge de `ValidationInterval`, la limitation du parallélisme des tests, le
câblage du contrôle au niveau champ, le Draft IN, l'axe SLA manquant, l'archivage matérialisé.

---

## GO / NO-GO conformité

> ### 🟠 **GO AVEC RÉSERVES**

**96,9 % de conformité. Zéro règle non conforme. Zéro règle non implémentée.**

Les 98 règles ont été réévaluées depuis le code du commit
`1c3cd38260c3b196de8641b8ec1d5a89f27a9762`, sans recyclage de statut antérieur, sur une suite
intégralement verte.

**Réserves :**

1. **Deux écarts que ce commit ne corrige pas et qu'aucun audit antérieur n'avait relevés** —
   RG-SEC-01 (contrôle au niveau champ jamais appelé) et RG-EXP-03 (Draft IN absent). Aucun n'est
   bloquant ; les deux doivent être portés au plan de correction.
2. **Trois règles dépendent d'arbitrages non rendus** — RG-CPT-08, RG-COM-01, RG-IA-01. Elles ne
   peuvent pas être notées autrement tant que les décisions ne sont pas prises.
3. **Deux écarts fonctionnels mineurs** — RG-SLA-01 (axe manquant), RG-TCK-06 (archivage non
   matérialisé).

---

## GO / NO-GO production

> ### 🟠 **GO SOUS CONDITIONS**

Aucun obstacle de sécurité ne subsiste : la chaîne d'autorisation est unique, ordonnée, protégée par
des tests de câblage, et les quatre défauts d'accès successifs du projet sont fermés et vérifiés.

Ce qui reste tient à **une validation non faite et trois arbitrages en attente**. Aucun n'exige de
développement.

La condition n° 1 est **bloquante pour tout déploiement sur base peuplée**. Les trois autres
conditionnent la complétude fonctionnelle, non la sûreté de l'exploitation.

---

## Conclusion

Le système est conforme à 96,9 % des règles de gestion du modèle Merise, sans aucune non-conformité
et sans aucune fonction manquante. Les domaines les plus critiques — sécurité, cloisonnement,
workflows, simulation tarifaire, traçabilité — sont tenus, et tenus par des mécanismes vérifiables
plutôt que par des intentions.

Deux constats de cet audit méritent d'être retenus au-delà de leur portée immédiate.

**Le premier** : `AutorisationChampService` est écrit, enregistré, testé par deux suites, et n'a aucun
appelant. C'est la troisième fois que ce projet produit une règle conforme sur le papier et sans effet
dans le produit — après `MarquerEtatEngageant()` sans appelant et la garde de fiche sans branche
agent. Les deux premières ont été fermées par des tests qui lisent les sources ; la troisième a
échappé au filet parce que le corpus surveillé s'arrête aux `Command`, `Query` et `UseCase`. **Un
filet a toujours une maille, et c'est là que passe le défaut suivant.**

**Le second** : le Draft IN de RG-EXP-03 n'existe nulle part. Aucun test ne le réclamait, aucune revue
ne l'avait relevé, et le parcours export passe de bout en bout sans lui. Une exigence qui n'est portée
par aucun test n'est pas surveillée — elle est seulement écrite.

Ces deux constats ne remettent pas en cause l'ouvrage. Ils rappellent ce que cet audit a cherché à
appliquer partout : **l'existence d'un code n'est pas la preuve d'un comportement.**

---

*Audit conduit en lecture seule sur le commit `1c3cd38260c3b196de8641b8ec1d5a89f27a9762`.*
*Aucun code, test, migration, script, configuration ni documentation existante n'a été modifié.*
