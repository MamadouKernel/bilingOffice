# Matrice de traçabilité UAT — 188 exigences

> **Document** : `docs/37-uat-requirements-traceability-matrix.md`  
> **Établi le** : 2026-09-02, sur le commit `d80a126`  
> **Question à laquelle ce document répond** : *comment prouve-t-on cette exigence ?*

---

## 1. Méthode

Le rattachement d'une exigence à un test est établi par **recherche de son identifiant** dans
les fichiers de test. C'est une mesure de traçabilité **déclarée**, pas de couverture : un test
peut couvrir une exigence sans la nommer, et nommer une exigence ne prouve pas qu'on la couvre.

Ce document dit donc où la preuve est **revendiquée**, pas où elle est **acquise**.

## 2. Résultat

| Indicateur | Valeur |
|:---|---:|
| Exigences totales | 188 |
| Citées dans au moins un fichier de test | **153** |
| Non citées par identifiant | **35** |
| Tests automatisés exécutés | 1041 / 1041 |

### Les 35 exigences sans citation d'identifiant

Leur absence de citation **ne signifie pas absence de test**. Elle signifie qu'aucun test ne
porte l'identifiant, donc qu'aucune preuve n'est *opposable par recherche*. Pour une recette
contractuelle, c'est la même chose qu'une preuve manquante : elle ne se démontre pas.

| Réf. | Prio. | Statut | Exigence |
|:---|:---|:---|:---|
| `PEC-05` | Should | `CERTIFIED` | Affectation directe : un superviseur ou une règle d'affectation paramétrée peut affecter un tic |
| `TPS-05` | Should | `CERTIFIED` | L'historique du ticket restitue la chronologie détaillée par étape (attente + traitement pour c |
| `SLA-04` | Should | `CERTIFIED` | Les horaires de fonctionnement (heures ouvrées du service) peuvent être pris en compte dans le  |
| `AFF-03` | Should | `CERTIFIED` | Distribution automatique Round-Robin |
| `AFF-04` | Should | `CERTIFIED` | Distribution selon la charge actuelle des agents |
| `AFF-05` | Should | `CERTIFIED` | Distribution selon la disponibilité des agents |
| `IMP-12` | Must | `CERTIFIED` | Le client procède au paiement (hors portail) puis joint sa preuve de paiement au ticket |
| `IMP-13` | Must | `CERTIFIED` | La preuve de paiement fait l'objet d'une validation interne par le rôle habilité (Caisse/Financ |
| `IMP-14` | Must | `CERTIFIED` | Le Pilote Contrôle Documentaire vérifie le dossier, dépose les documents de sortie (BS, BADT) d |
| `SIM-01` | Must | `CERTIFIED` | Le client peut lancer une simulation de proforma **avant, si nécessaire**, de démarrer son parc |
| `SIM-03` | Must | `CERTIFIED` | Le tarif applicable est déterminé par la clé Sens + Taille + Catégorie de marchandise, à partir |
| `SIM-05` | Must | `CERTIFIED` | Le résultat présente le détail des postes par conteneur, un total général et la mention légale  |
| `SIM-07` | Must | `CERTIFIED` | Les grilles tarifaires sont versionnées avec une date d'effet ; une simulation référence la ver |
| `SIM-08` | Should | `CERTIFIED` | Le client peut enregistrer une simulation, la retrouver sur son profil et la rattacher ultérieu |
| `SIM-10` | Should | `CERTIFIED` | L'administrateur peut charger une nouvelle grille tarifaire par import (ex. tableur) en plus de |
| `SIM-11` | Could | `CERTIFIED` | Contrôles de cohérence à la saisie (taille et type reconnus, catégorie compatible avec le sens) |
| `COM-01` | Must | `CERTIFIED` | La plateforme notifie le client via : e-mail, notifications portail, et au moins un canal de me |
| `IA-01` | Must | `NOT_IMPLEMENTED` | L'assistant répond aux questions simples des clients (fonctionnement du portail, pièces à fourn |
| `IA-02` | Must | `NOT_IMPLEMENTED` | L'assistant oriente le client dans ses démarches (choix du parcours, du module, de la catégorie |
| `IA-03` | Must | `NOT_IMPLEMENTED` | L'assistant escalade vers un opérateur humain lorsque cela est nécessaire ; le passage se fait  |
| `IA-04` | Should | `NOT_IMPLEMENTED` | L'assistant s'appuie sur une base de connaissances administrable (FAQ, procédures) ; il cite la |
| `IA-05` | Should | `NOT_IMPLEMENTED` | L'assistant est accessible depuis le portail et, si possible, depuis les canaux de messagerie c |
| `IA-06` | Must | `NOT_IMPLEMENTED` | L'assistant identifie les situations nécessitant une intervention humaine et permet le transfer |
| `IA-07` | Must | `NOT_IMPLEMENTED` | Le client peut demander une intervention humaine explicitement ; l'assistant réduit les sollici |
| `SEC-04` | Must | `CERTIFIED` | Chiffrement des échanges (HTTPS/TLS) et protection des documents stockés |
| `SEC-09` | Should | `CERTIFIED` | Cloisonnement des environnements (production, recette) et gestion des habilitations par environ |
| `NFR-03` | Must | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | Disponibilité cible en heures ouvrées ≥ 99 % ; maintenance planifiée hors heures de pointe |
| `NFR-04` | Must | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | Temps de réponse < 3 secondes pour les écrans courants dans des conditions normales de charge |
| `NFR-05` | Must | `NOT_IMPLEMENTED` | Volumétrie dimensionnante à confirmer en phase de cadrage : tickets/jour, taille des pièces joi |
| `NFR-06` | Must | `NOT_IMPLEMENTED` | Sauvegardes régulières (données et documents) et procédure de restauration testée |
| `NFR-07` | Should | `CERTIFIED` | Interface disponible en anglais (clients internationaux) |
| `NFR-08` | Should | `CERTIFIED` | Architecture ouverte exposant des API pour faciliter d'éventuelles intégrations futures |
| `NFR-09` | Should | `NOT_IMPLEMENTED` | Hébergement conforme aux standards du groupe ; le choix (infrastructure CIT/AGL ou cloud) sera  |
| `NFR-10` | Must | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | La plateforme est accessible 24h/24 et 7j/7, hors périodes de maintenance planifiées |
| `ERR-01` | Must | `CERTIFIED` | Aucune perte de données saisies en cas d'incident lors d'une soumission (brouillon ou reprise d |

**Recommandation.** Ces exigences doivent être couvertes par un **scénario UAT nommé**, à
défaut d'un test automatisé qui les cite. C'est l'objet du plan de recette `docs/36`.

---

## 3. Matrice complète

Colonnes : identifiant · priorité CDC · feature · fichiers de test citant l'exigence · statut ·
preuve UAT attendue · bloquant.

| Réf. | Prio. | Feature | Tests citant l'exigence | Statut | Preuve UAT | Bloquant |
|:---|:---|:---|:---|:---|:---|:---|
| `SOC-01` | Must | 001, 003, 006, 007, 008, 009, 010 | NumerotationTests.cs, SecCert06LienNotificationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-02` | Must | 001, 002, 003, 008, 009 | TicketSocleTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-03` | Must | 001 | TicketSocleTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-04` | Must | 001, 006, 008, 009 | ConsulterFileEtPriseLibreTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-05` | Must | 001, 008, 009 | MessagesTests.cs, RaccordementMentionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-06` | Must | 001, 002, 003, 006, 008, 009 | TicketSocleTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-07` | Must | 001, 003, 006, 007, 008 | ControleDepotTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-08` | Must | 001, 009 | TicketSocleTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-09` | Must | 001, 003, 006, 009, 010 | ClotureArchivageTests.cs, NumerotationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-10` | Must | 001, 006, 007, 008 | LiaisonInterTicketsTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-11` | Must | 001, 003, 009 | DossierDeriveTests.cs, RecSce14TransverseTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-12` | Must | 001 | PriseEnChargeConcurrenceTests.cs, RecSce09PriseEnChargeTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SOC-13` | Must | 001, 002, 003, 008, 009, 010 | JournalAuditAppendOnlyTests.cs, RecSce09PriseEnChargeTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `SOC-14` | Could | 001, 011, 012 | SocCert14ExportCsvTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CYC-01` | Must | 001 | AnnulationTests.cs, AuditDistinctionAnnulationRejetTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CYC-02` | Must | 001 | AnnulationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CYC-03` | Must | 001 | AnnulationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CYC-04` | Must | 001, 009 | RelancesAbsenceReponseTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CYC-05` | Must | 001 | RelancesAbsenceReponseTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CYC-06` | Should | 001 | ReouvertureTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CYC-07` | Should | 001 | ReouvertureTests.cs | `CERTIFIED` | Scénario de recette | — |
| `MSG-01` | Must | 001 | MessagesTests.cs | `CERTIFIED` | Scénario de recette | — |
| `MSG-02` | Must | 001, 003, 009 | DistribuerNotificationsUseCaseTests.cs, NotesInternesTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `MSG-03` | Must | 001 | NotesInternesTests.cs | `CERTIFIED` | Scénario de recette | — |
| `MSG-04` | Should | 001, 009 | DistribuerNotificationsUseCaseTests.cs, Imp11EtProducteursTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `MSG-05` | Must | 001, 009 | MentionSansReaffectationTests.cs, RaccordementMentionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `WKF-01` | Must | 001, 002, 003, 006 | TicketSocleTests.cs | `CERTIFIED` | Scénario de recette | — |
| `WKF-02` | Must | 001 | DerogationWorkflowTests.cs | `CERTIFIED` | Scénario de recette | — |
| `WKF-03` | Must | 001 | DerogationWorkflowTests.cs, RecSce10TransfertTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `PEC-01` | Must | 001 | RecSce09PriseEnChargeTests.cs, TransfertSansAffectationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `PEC-02` | Must | 001 | PriseEnChargeConcurrenceTests.cs, RecSce09PriseEnChargeTests.cs | `CERTIFIED` | Scénario de recette | — |
| `PEC-03` | Must | 001 | RecSce09PriseEnChargeTests.cs | `CERTIFIED` | Scénario de recette | — |
| `PEC-04` | Must | 001 | RecSce09PriseEnChargeTests.cs | `CERTIFIED` | Scénario de recette | — |
| `PEC-05` | Should | 001 | — | `CERTIFIED` | Scénario de recette | — |
| `TRF-01` | Must | 001 | RecSce10TransfertTests.cs, TransfertSansAffectationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TRF-02` | Must | 001 | RecSce10TransfertTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TRF-03` | Must | 001 | RecSce10TransfertTests.cs, TransfertSansAffectationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TRF-04` | Must | 001 | RecSce11ReaffectationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TRF-05` | Must | 001, 009 | RecSce10TransfertTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TRF-06` | Must | 001 | MatriceTransitionsTests.cs, RecSce10TransfertTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TPS-01` | Must | 001 | RecSce09PriseEnChargeTests.cs, RecSce12TempsSlaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TPS-02` | Must | 001, 008 | AttenteClientTests.cs, RecSce12TempsSlaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TPS-03` | Must | 001 | RecSce12TempsSlaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TPS-04` | Must | 001, 008 | AttenteClientTests.cs, RecSce12TempsSlaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `TPS-05` | Should | 001 | — | `CERTIFIED` | Scénario de recette | — |
| `SLA-01` | Must | 001, 006 | RecSce12TempsSlaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SLA-02` | Must | 001 | RecSce12TempsSlaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SLA-03` | Must | 001 | RecSce12TempsSlaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SLA-04` | Should | 001 | — | `CERTIFIED` | Scénario de recette | — |
| `SLA-05` | Must | 001 | RecSce12TempsSlaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `ALE-01` | Must | 001, 009 | RecSce12TempsSlaTests.cs, RecSce13SupervisionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `ALE-02` | Must | 001, 009 | RecSce12TempsSlaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `ALE-03` | Must | 001, 009 | DistribuerNotificationsUseCaseTests.cs, Imp11EtProducteursTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `SUP-01` | Must | 001, 009 | RecSce13SupervisionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SUP-02` | Must | 001, 009 | RecSce13SupervisionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SUP-03` | Must | 001 | VueChargeTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SUP-04` | Must | 001 | RecSce13SupervisionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SUP-05` | Must | 001 | VueChargeTests.cs | `CERTIFIED` | Scénario de recette | — |
| `DIS-01` | Must | 001, 003 | DisponibiliteAutorisationTests.cs, DisponibiliteExclusionTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `DIS-02` | Must | 001 | DisponibiliteAutorisationTests.cs, VueChargeTests.cs | `CERTIFIED` | Scénario de recette | — |
| `DIS-03` | Must | 001 | DisponibiliteTests.cs | `CERTIFIED` | Scénario de recette | — |
| `AFF-01` | Must | 001 | ConsulterFileEtPriseLibreTests.cs | `CERTIFIED` | Scénario de recette | — |
| `AFF-02` | Must | 001 | RecSce13SupervisionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `AFF-03` | Should | 001 | — | `CERTIFIED` | Scénario de recette | — |
| `AFF-04` | Should | 001 | — | `CERTIFIED` | Scénario de recette | — |
| `AFF-05` | Should | 001 | — | `CERTIFIED` | Scénario de recette | — |
| `AFF-06` | Should | 001 | DistributionCompetenceTests.cs | `CERTIFIED` | Scénario de recette | — |
| `AFF-07` | Must | 001 | StrategieAffectationParametrableTests.cs | `CERTIFIED` | Scénario de recette | — |
| `PRI-01` | Must | 001 | PrioriteTests.cs | `CERTIFIED` | Scénario de recette | — |
| `PRI-02` | Must | 001, 011, 012 | PriCert01PrioriteTests.cs, PrioriteTests.cs | `CERTIFIED` | Scénario de recette | — |
| `PRI-03` | Must | 001, 012 | PrioriteTests.cs | `CERTIFIED` | Scénario de recette | — |
| `PRI-04` | Must | 001 | PrioriteTests.cs | `CERTIFIED` | Scénario de recette | — |
| `MAS-01` | Should | 001 | ActionsDeMasseTests.cs, RecSce13SupervisionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `MAS-02` | Must | 001 | ActionsDeMasseTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CPT-01` | Must | 002 | CircuitClientEnCompteExportTests.cs, Imp11EtProducteursTests.cs (+9) | `CERTIFIED` | Scénario de recette | — |
| `CPT-02` | Must | 002, 003 | PiecesExigeesEtInvitationTests.cs, SocieteClienteTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `CPT-03` | Must | 002 | ParcoursConnexion2FaTests.cs, RecuperationMotDePasseTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `CPT-04` | Must | 001, 002, 003, 009, 011, 012, 013 | ConsulterProfilSocieteTests.cs, ControleUniciteBlTests.cs (+3) | `CERTIFIED` | Scénario de recette | — |
| `CPT-05` | Must | 002 | RattacherUtilisateurTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CPT-06` | Should | 002, 003 | AiguillageTypeClientTests.cs, ModifierTypeClientTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `CPT-07` | Could | 002 | DoubleAuthentificationTests.cs, ParcoursConnexion2FaTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `ENT-01` | Must | 002 | ConsulterProfilSocieteTests.cs, PiecesExigeesEtInvitationTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `ENT-02` | Must | 002, 003 | ConcurrenceDernierAdministrateurTests.cs, DesactivationDernierAdministrateurTests.cs (+6) | `CERTIFIED` | Scénario de recette | — |
| `ENT-03` | Must | 002, 003, 003 | ConsulterProfilSocieteTests.cs, DeterminerVisibiliteDemandesTests.cs | `CERTIFIED` | Scénario de recette | — |
| `ENT-04` | Should | 002, 003 | AuditGestionCollaborateursTests.cs, InscrireTransporteurCommandTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-01` | Must | 003 | RecSce01ImportNormalTests.cs, SoumettreDemandeImportTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-02` | Must | 003 | AtomiciteMultiBlSqlTests.cs, RecSce01ImportNormalTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `IMP-03` | Must | 003, 009, 011, 012 | Imp11EtProducteursTests.cs, ImpCert03NotificationEmailTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `IMP-04` | Must | 002, 003, 008 | AuditDistinctionAnnulationRejetTests.cs, InstructionManifesteTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `IMP-05` | Must | 003, 009 | InstructionManifesteTests.cs, RecSce01ImportNormalTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-06` | Must | 002, 003 | AiguillageTypeClientTests.cs, RecSce01ImportNormalTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-10` | Must | 002, 003 | CircuitClientNormalTests.cs, RecSce01ImportNormalTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-11` | Must | 003, 009 | Imp11EtProducteursTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-12` | Must | 003, 009 | — | `CERTIFIED` | Scénario de recette | — |
| `IMP-13` | Must | 003, 009 | — | `CERTIFIED` | Scénario de recette | — |
| `IMP-14` | Must | 003, 009 | — | `CERTIFIED` | Scénario de recette | — |
| `IMP-15` | Should | 003, 010 | DeclarationPresentielleTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-20` | Must | 002, 003 | CircuitClientEnCompteTests.cs, RecSce02ImportEnCompteTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-21` | Must | 003 | CircuitClientEnCompteTests.cs, RecSce02ImportEnCompteTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-22` | Must | 003 | CircuitClientEnCompteTests.cs, RecSce02ImportEnCompteTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-30` | Must | 002, 003 | CircuitClientDepositTests.cs, RecSce03ImportDepositTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-31` | Must | 003 | CircuitClientDepositTests.cs, RecSce03ImportDepositTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-32` | Must | 003 | CircuitClientDepositTests.cs, RecSce03ImportDepositTests.cs | `CERTIFIED` | Scénario de recette | — |
| `IMP-40` | Must | 002, 003, 011, 017 | CircuitClientIesTests.cs, RecSce04ImportIesTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `IMP-41` | Must | 003, 017 | CircuitClientIesTests.cs, ConstaterJointureIesAutorisationTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `IMP-42` | Must | 003 | ConstaterJointureIesAutorisationTests.cs, RecSce04ImportIesTests.cs | `CERTIFIED` | Scénario de recette | — |
| `UNI-01` | Must | 003 | AuditDistinctionAnnulationRejetTests.cs, ControleUniciteBlTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `UNI-02` | Must | 003 | ControleUniciteBlTests.cs | `CERTIFIED` | Scénario de recette | — |
| `UNI-03` | Must | 003 | ControleUniciteBlTests.cs | `CERTIFIED` | Scénario de recette | — |
| `UNI-04` | Should | 003, 004 | ControleUniciteBookingWebTests.cs, RecSce05ExportE2ETests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `EXP-01` | Must | 003, 004 | ErrCert01BrouillonsTests.cs, MultiBookingsIndependanceTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `EXP-02` | Must | 004 | InstructionBackOfficeTests.cs | `CERTIFIED` | Scénario de recette | — |
| `EXP-03` | Must | 004 | CircuitClientDepositExportTests.cs, CircuitClientEnCompteExportTests.cs (+3) | `CERTIFIED` | Scénario de recette | — |
| `EXP-04` | Must | 003, 004 | CircuitClientIesExportTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SIM-01` | Must | 004, 005 | — | `CERTIFIED` | Scénario de recette | — |
| `SIM-02` | Must | 005 | MajorationsTarifairesTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SIM-03` | Must | 005 | — | `CERTIFIED` | Scénario de recette | — |
| `SIM-04` | Must | 005 | StationnementEtReeferTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SIM-05` | Must | 005 | — | `CERTIFIED` | Scénario de recette | — |
| `SIM-06` | Must | 005 | AuditPreuveArrondiFrancTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SIM-07` | Must | 005 | — | `CERTIFIED` | Scénario de recette | — |
| `SIM-08` | Should | 005 | — | `CERTIFIED` | Scénario de recette | — |
| `SIM-09` | Should | 005 | AuditPreuveValiditePdfTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SIM-10` | Should | 005 | — | `CERTIFIED` | Scénario de recette | — |
| `SIM-11` | Could | 005, 013 | — | `CERTIFIED` | Scénario de recette | — |
| `AVE-01` | Must | 004, 005, 007, 009 | AveSpecTests.cs, AvoirExonerationPersistenceTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `AVE-02` | Must | 007 | AveSpecTests.cs, SoumettreDemandeAvoirExoTests.cs | `CERTIFIED` | Scénario de recette | — |
| `AVE-03` | Must | 007, 009 | ConsulterAvoirExoTests.cs | `CERTIFIED` | Scénario de recette | — |
| `AVE-04` | Should | 005, 007 | DeciderAvoirExoTests.cs | `CERTIFIED` | Scénario de recette | — |
| `REC-01` | Must | 004, 005, 006 | AttenteClientComplaintTests.cs, CloturerReclamationCommandTests.cs (+10) | `CERTIFIED` | Scénario de recette | — |
| `REC-02` | Must | 006, 008, 011, 012 | AdministrationCategoriesTests.cs, CreerCategorieReclamationCommandTests.cs (+5) | `CERTIFIED` | Scénario de recette | — |
| `REC-03` | Must | 006, 008, 011, 012 | RecCert03OrientationSupportTests.cs, RecSce07CustomerComplaintE2ETests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `REC-04` | Must | 006 | RecSce07CustomerComplaintE2ETests.cs | `CERTIFIED` | Scénario de recette | — |
| `REC-05` | Should | 006 | CustomerComplaintSecurityTests.cs, RecSce07CustomerComplaintE2ETests.cs | `CERTIFIED` | Scénario de recette | — |
| `REC-06` | Could | 005, 006 | CustomerComplaintSecurityTests.cs, RecSce07CustomerComplaintE2ETests.cs | `CERTIFIED` | Scénario de recette | — |
| `ENR-01` | Must | 004, 005, 008 | AttenteClientEnrolementTests.cs, ConstaterRemiseBadgeCommandTests.cs (+11) | `CERTIFIED` | Scénario de recette | — |
| `ENR-02` | Must | 008 | CycleCompletEnrolementIntegrationTests.cs, EnrCert05RenouvellementTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `ENR-03` | Must | 008, 009 | FicheEnrolementTests.cs, RecSce08EnrolementTransporteurE2ETests.cs | `CERTIFIED` | Scénario de recette | — |
| `ENR-04` | Should | — | FicheEnrolementTests.cs | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_PROVIDER` |
| `ENR-05` | Could | 005, 008, 009, 013 | ConstaterRemiseBadgeCommandTests.cs, Imp11EtProducteursTests.cs | `CERTIFIED` | Scénario de recette | — |
| `COM-01` | Must | 002, 003, 004, 007, 008, 009 | — | `CERTIFIED` | Scénario de recette | — |
| `COM-02` | Must | 003, 009 | RaccordementMentionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `COM-03` | Must | 009 | ReferentielsNotificationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `COM-04` | Must | 009 | EnvoiNotificationTests.cs, RaccordementMentionTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `COM-05` | Should | 009, 013 | AdministrationEtPreferencesTests.cs | `CERTIFIED` | Scénario de recette | — |
| `COM-06` | Could | 009, 011, 012 | ComCert06CanauxRelanceTests.cs | `CERTIFIED` | Scénario de recette | — |
| `NOT-01` | Must | 009 | CentreNotificationsTests.cs, NotificationTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `NOT-02` | Must | 009, 011 | AdministrationEtPreferencesTests.cs, DistribuerNotificationsUseCaseTests.cs (+6) | `CERTIFIED` | Scénario de recette | — |
| `NOT-03` | Should | 009 | CentreNotificationsTests.cs, MainLayoutIntegrationTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `CON-01` | Must | 003, 009, 010 | Imp11EtProducteursTests.cs, RecSce14ContinuiteGuichetRendezVousE2ETests.cs | `CERTIFIED` | Scénario de recette | — |
| `CON-02` | Must | 003, 009, 010 | RecSce14ContinuiteGuichetRendezVousE2ETests.cs | `CERTIFIED` | Scénario de recette | — |
| `CON-03` | Must | 003, 009, 010 | RecSce14ContinuiteGuichetRendezVousE2ETests.cs, RendezVousRepositoryTests.cs | `CERTIFIED` | Scénario de recette | — |
| `CON-04` | Should | 003, 009, 010 | RecSce14ContinuiteGuichetRendezVousE2ETests.cs | `CERTIFIED` | Scénario de recette | — |
| `IA-01` | Must | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_DSI` |
| `IA-02` | Must | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_DSI` |
| `IA-03` | Must | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_DSI` |
| `IA-04` | Should | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_DSI` |
| `IA-05` | Should | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_DSI` |
| `IA-06` | Must | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_DSI` |
| `IA-07` | Must | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_DSI` |
| `SEC-01` | Must | 001, 002, 007, 008, 009, 011 | AuthorizationUxTests.cs, AutorisationServeurTests.cs (+3) | `CERTIFIED` | Scénario de recette | — |
| `SEC-02` | Must | 001, 002, 003, 009, 011, 012, 013, 015, 016, 017 | RecSce04ImportIesTests.cs, SecCert02RbacChampTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SEC-03` | Must | 001, 002, 009, 011, 012, 013 | SecCert02CloisonnementTests.cs, SocCert14ExportCsvTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SEC-04` | Must | 002, 009, 011, 012, 015 | — | `CERTIFIED` | Scénario de recette | — |
| `SEC-05` | Must | 001, 002, 009, 011, 017 | CustomerComplaintSecurityTests.cs, JournalAuditAppendOnlyTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `SEC-06` | Must | 002, 011, 012, 015, 016, 017 | CustomerComplaintSecurityTests.cs, SecCert06RetentionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SEC-07` | Must | 001, 009, 011, 012, 013, 015 | ControleDepotTests.cs, ScannerAntivirusIntegrationTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SEC-08` | Should | 001, 002, 011, 012 | ParcoursConnexion2FaTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SEC-09` | Should | 011, 012, 015 | — | `CERTIFIED` | Scénario de recette | — |
| `SEC-10` | Must | 009, 011 | SecCert04SessionTests.cs | `CERTIFIED` | Scénario de recette | — |
| `SEC-11` | Must | 001, 002, 009, 011 | AdministrationComptesAgentsTests.cs, ConstaterJointureIesAutorisationTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `SEC-12` | Must | 009, 011, 015 | AccesDocumentAutoriseTests.cs, RaccordementMentionTests.cs (+1) | `CERTIFIED` | Scénario de recette | — |
| `NFR-01` | Must | 003, 011, 016 | NfrCert01LangueTests.cs | `CERTIFIED` | Scénario de recette | — |
| `NFR-02` | Must | 003, 008, 011, 016 | NfrCert02ResponsiveTests.cs | `CERTIFIED` | Scénario de recette | — |
| `NFR-03` | Must | F016 | — | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | Campagne environnementale | `BLOCKED_ENVIRONMENT` |
| `NFR-04` | Must | F016 | — | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | Campagne environnementale | `BLOCKED_ENVIRONMENT` |
| `NFR-05` | Must | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_DSI` |
| `NFR-06` | Must | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_ENVIRONMENT` |
| `NFR-07` | Should | 011, 013, 014 | — | `CERTIFIED` | Scénario de recette | — |
| `NFR-08` | Should | 011, 013, 014, 015, 016, 017 | — | `CERTIFIED` | Scénario de recette | — |
| `NFR-09` | Should | — | — | `NOT_IMPLEMENTED` | Non applicable à ce stade | `BLOCKED_DSI` |
| `NFR-10` | Must | F016 | — | `IMPLEMENTED_NOT_ENVIRONMENT_PROVEN` | Campagne environnementale | `BLOCKED_ENVIRONMENT` |
| `NFR-11` | Must | 011, 016 | NfrCert03HorairesTests.cs | `CERTIFIED` | Scénario de recette | — |
| `ERR-01` | Must | 012, 013, 014, 015, 017 | — | `CERTIFIED` | Scénario de recette | — |
| `ERR-02` | Must | 009, 011 | EnvoiNotificationTests.cs, ErrCert01ConfigurationRepriseTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |
| `ERR-03` | Must | 001, 011 | ControleDepotTests.cs | `CERTIFIED` | Scénario de recette | — |
| `ERR-04` | Should | 009, 010 | Imp11EtProducteursTests.cs, RecSce14ContinuiteGuichetRendezVousE2ETests.cs | `CERTIFIED` | Scénario de recette | — |
| `ACR-01` | Must | 001, 003, 004, 006, 007, 009, 010 | Acr01ExportDashboardTests.cs, CentreNotificationsTests.cs (+2) | `CERTIFIED` | Scénario de recette | — |

---

## 4. Réserve

La colonne « Tests citant l'exigence » est produite par recherche textuelle. Elle ne mesure ni
la pertinence du test, ni sa profondeur. Un audit de couverture réel supposerait de relire
chaque test au regard de l'exigence qu'il revendique — travail non couvert ici.
