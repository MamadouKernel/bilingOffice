# Audit de conformité — Plateforme Digitale CIT

> **Document** : `docs/audit-conformite-merise.md`
> **Établi le** : 2026-09-05
> **Référence auditée** : `Analyse_Merise_CIT_Plateforme_Digitale.docx` (créé le 2026-09-04 21:45)
> **Code audité** : arbre de travail sur `75bd614`, **200 fichiers non committés inclus**
> **Mode** : lecture seule — aucune modification de code n'a été faite pendant cet audit

---

## Avertissement de méthode

**L'arbre de travail audité contient des modifications non committées faites plus tôt dans la même
session que cet audit**, portant sur la normalisation des référentiels tarifaires (analyse Merise
§ 3.3). Auditer son propre travail est un biais ; il est signalé ici, et chaque ligne concernée
porte la mention **[lot 3FN]** pour que le commanditaire puisse la relire en priorité.

Avant ce lot, les scores tarifaires étaient sensiblement plus bas : 9 tables du MPD étaient
absentes et RG-SIM-02 / RG-SIM-11 non conformes.

**Une incohérence a été relevée dans le document de référence lui-même** : sa section 1 définit
**98 règles**, tandis que son annexe de traçabilité en recense **97** et l'annonce explicitement
(« Cette matrice recense les 97 règles de gestion formalisées en section 1 »). La règle manquante
est **RG-CPT-08**, qui est aussi la seule règle du document ne citant **aucune** exigence du CDC.
Cet audit retient les 98 règles de la section 1.

---

## 1. Résumé exécutif

La plateforme couvre l'essentiel du modèle décrit par le document. Sur 98 règles de gestion,
**81 sont pleinement conformes**, 10 le sont partiellement, 4 sont contredites et 3 ne sont pas
implémentées. **Conformité globale : 87,76 %.**

Trois constats dominent.

**Un défaut de sécurité de niveau P0.** Les trois points d'entrée de l'API REST implémentent leur
propre contrôle de cloisonnement au lieu d'utiliser `CloisonnementService`, et **ce contrôle échoue
en mode ouvert** : il ne s'applique que si l'appelant possède une société de rattachement. Un
compte transporteur — que `InscrireTransporteurCommand` crée précisément *sans* société — passe
donc au travers et peut lire n'importe quel ticket, numéro de connaissement compris. Les numéros de
ticket étant issus d'une séquence SQL au format `XXX-XXX-000001`, l'énumération est triviale.
Cela contredit RG-CPT-04 et RG-SEC-02.

**Deux exigences restent suspendues à un arbitrage fournisseur**, comme le projet le documente déjà :
l'assistant IA (RG-IA-01/02) n'existe pas du tout, et les canaux e-mail/SMS/WhatsApp (RG-COM-01)
sont câblés sur un adaptateur qui journalise sans envoyer. Aucun client n'est notifié hors portail.

**Le paramétrage « sans développement » reste partiel là où le document l'exige.** Le circuit
nominal de workflow (RG-WKF-01) vit dans des constantes C# et des `switch` sur le type de client ;
la matrice des transitions, elle, est bien administrable. Côté tarifaire, le lot 3FN a rendu les
données administrables mais **aucun écran ne les expose** : les référentiels et les paramètres de
grille ne se modifient aujourd'hui que par migration ou import CSV.

La qualité d'ensemble est par ailleurs élevée : 1 220 tests passent, le journal d'audit est
append-only par garde applicative, le cloisonnement documentaire est correct et fail-closed, et le
projet s'est doté de gardes d'architecture (`ServicesAtteignablesTests`, `SurfaceRoutableTests`)
qui empêchent structurellement la dérive « fonction livrée mais inatteignable ».

---

## 2. Périmètre audité

| Axe du document | Traité |
|:---|:---|
| § 1 — 98 règles de gestion | Intégralement, une ligne de matrice par règle |
| § 2 — MCD, dictionnaire, associations, cardinalités | § 7 de ce rapport |
| § 3 — MLD, règles de passage, vérification 1FN/2FN/3FN | § 8 |
| § 4 — MCT, 7 macro-processus, événements et synchronisations | § 10 |
| § 5 — MOT, 12 postes de travail, traitements automatiques | § 11 |
| § 6 — MPD, 44 tables, PK/FK/index/types/contraintes | § 9 |
| Annexe — matrice de traçabilité RG ↔ CDC | § 6, incohérence signalée |
| Annexes A.1 à A.8 — paramétrage tarifaire | § 15 |

Côté projet : 4 projets .NET 10, 60 tables, 312 fichiers de test, 1 220 tests.

---

## 3. Architecture réellement trouvée

| Élément | Constat |
|:---|:---|
| Solution | `CIT.Plateforme.slnx` — 4 projets : Domain, Application, Infrastructure, Web |
| Architecture | Hexagonale. Domaine pur (aucune référence EF/ASP.NET), ports dans `Application/Abstractions`, adaptateurs dans `Infrastructure` |
| UI | Blazor Server (`@rendermode InteractiveServer`), 79 pages routables |
| Persistance | EF Core sur SQL Server, `PlateformeDbContext`, 45 migrations |
| Authentification | ASP.NET Core Identity (`ApplicationUser : IdentityUser<Guid>`), 2FA présent |
| Autorisation | **Codes de permission**, jamais rôles Identity — `HabilitationUtilisateur(UtilisateurId, PermissionCode, FileId?)`. Choix documenté (PC-03, decision D4) : aucun rôle métier n'est nommé en dur |
| API | 3 endpoints Minimal API sous `/api/v1`, tous `RequireAuthorization()` |
| Traitements de fond | 5 `BackgroundService` : distribution auto, notifications, relances, surveillance SLA, rappel badges |
| Stockage documentaire | `StockageDocumentsFichiers` + scan antivirus (`ScannerAntivirusDefender`) + health check |
| Journalisation | Trois journaux distincts : `HistoriqueTickets` (métier), `JournalAudit` (append-only), `JournalEvenements` (bus de notifications) |

**Divergence de convention assumée face au MPD.** Le document prescrit `dbo.TICKET`, PK
`BIGINT IDENTITY`. Le projet utilise `Tickets`, PK `Guid`, et ASP.NET Identity pour les comptes.
C'est une **divergence acceptable techniquement** (catégorie 2 au sens du § 19 de la commande) :
elle ne retire aucune propriété au modèle. Elle est traitée comme telle dans tout ce rapport, et
jamais comptée comme non-conformité.

---

## 4. Score global

Méthode imposée : CONFORME = 1 · PARTIEL = 0,5 · NON CONFORME = 0 · NON IMPLÉMENTÉ = 0 ·
NON APPLICABLE exclu du dénominateur.

| Statut | Nombre |
|:---|---:|
| ✅ Conforme | 81 |
| 🟡 Partiellement conforme | 10 |
| 🔴 Non conforme | 4 |
| ⚫ Non implémenté | 3 |
| 🔵 Non applicable | 0 |
| ⚠️ Non vérifiable | 0 |
| **Total** | **98** |

**(81 × 1) + (10 × 0,5) = 86,0 / 98 → CONFORMITÉ GLOBALE : 87,76 %**

---

## 5. Scores par domaine

Les quinze premiers domaines sont calculés sur les règles RG-* de la matrice (§ 6). Les quatre
derniers ne dérivent pas de règles RG et sont évalués sur leurs propres critères, explicités.

```
Socle transverse (TCK-01..06, MSG) ....  95,0 %   (9,5 / 10)
Comptes / profils (CPT) ...............  75,0 %   (6,0 / 8)
Tickets / dossiers (TCK-07..15) .......  88,9 %   (8,0 / 9)
Workflow / files (WKF,AFF,DIS,PRI,MAS)   94,1 %   (16,0 / 17)
SLA / temps (TPS, SLA, ALE) ...........  94,4 %   (8,5 / 9)
Billing IMPORT (IMP) ..................  94,4 %   (8,5 / 9)
Billing EXPORT (EXP) ..................  87,5 %   (3,5 / 4)
Simulation Proforma (SIM) .............  95,8 %   (11,5 / 12)
Avoir / Exonération (AVE) .............  75,0 %   (1,5 / 2)
Customer Complaint (REC) .............. 100,0 %   (3,0 / 3)
Enrôlement Transporteurs (ENR) ........  75,0 %   (1,5 / 2)
Notifications (COM, NOT) ..............  80,0 %   (4,0 / 5)
Digital / présentiel (CON) ............ 100,0 %   (2,0 / 2)
Assistant IA (IA) .....................   0,0 %   (0,0 / 2)
Sécurité (SEC) ........................  62,5 %   (2,5 / 4)

Base de données (44 tables du MPD) ....  83,0 %   (36,5 / 44)
Audit / traçabilité (12 actions) ......  91,7 %   (11 / 12)
Interface utilisateur .................  90,0 %   (estimation argumentée, § 24)
Tests .................................  85,0 %   (estimation argumentée, § 25)

CONFORMITÉ GLOBALE ....................  87,76 %
```

---

## 6. Matrice complète RG-* → implémentation

| ID | Domaine | Exigence (abrégée) | Statut | Implémentation trouvée | Preuve | Écart | Sév. | Action |
|:---|:---|:---|:---|:---|:---|:---|:---|:---|
| RG-CPT-01 | Comptes | Demande de compte validée par CIT | ✅ | `ValiderDemandeCompteCommand` | `Application/Comptes/ValiderDemandeCompteCommand.cs` ; écran `Comptes/ValiderDemandes.razor` | — | — | — |
| RG-CPT-02 | Comptes | Profil fixé parmi 4 valeurs exclusives | ✅ | `enum TypeClient` fermé | `Domain/Comptes/TypeClient.cs` | — | — | — |
| RG-CPT-03 | Comptes | Le profil détermine le workflow | ✅ | Instantané `Dossier.TypeClient` + aiguillage | `Domain/Tickets/Dossier.cs:34` ; `BillingImport/ValiderTicketImportCommand.cs` | — | — | — |
| RG-CPT-04 | Comptes | Client cloisonné à ses propres tickets | 🔴 | `CloisonnementService` (fail-closed) **contourné par l'API** | `Web/Api/ApiEndpoints.cs:43-47` | Contrôle inopérant si `SocieteId` est nul | **P0** | Router l'API via `CloisonnementService` |
| RG-CPT-05 | Comptes | Changement de type tracé, non rétroactif | ✅ | Audit + instantané figé au dossier | `Comptes/ModifierTypeClientCommand.cs:26-28` | — | — | — |
| RG-CPT-06 | Comptes | Société multi-utilisateurs, admin société | ✅ | `UtilisateurSociete.EstAdministrateur` | `Domain/Comptes/UtilisateurSociete.cs:17` ; `Societe/Collaborateurs.razor` | — | — | — |
| RG-CPT-07 | Comptes | Visibilité selon droits accordés | ✅ | `enum DroitConsultation` | `Domain/Comptes/DroitConsultation.cs` | — | — | — |
| RG-CPT-08 | Comptes | Compte transporteur distinct du compte client | ⚫ | Aucune : `NatureUtilisateur` n'a que `Client`/`Agent` | `Domain/Securite/NatureUtilisateur.cs:5-9` | Type de compte absent | P3 | Ajouter la nature `Transporteur` — **règle sans ancrage CDC, absente de l'annexe du document** |
| RG-TCK-01 | Socle | Numéro unique communiqué au client | ✅ | Séquence SQL + événement de notification | `Persistence/ServiceNumerotation.cs:24-30` | — | — | — |
| RG-TCK-02 | Socle | Le ticket centralise tout son traitement | ✅ | Documents, messages, historique rattachés au ticket | `Domain/Tickets/Ticket.cs` ; `Tickets/ObtenirFicheTicketQuery.cs` | — | — | — |
| RG-TCK-03 | Socle | Workflow selon module et type de client | ✅ | Voir RG-CPT-03 | `BillingImport/ValiderTicketImportCommand.cs` | — | — | — |
| RG-TCK-04 | Socle | Regroupement sous un numéro de dossier | ✅ | `Dossier` + `DOS-000000` | `Domain/Tickets/Dossier.cs` ; `ServiceNumerotation.cs:32-36` | — | — | — |
| RG-TCK-05 | Socle | Liaison entre deux tickets | ✅ | `LiaisonTicket` + `LierTicketsCommand` | `Domain/Tickets/LiaisonTicket.cs` ; table `LiaisonsTicket` | — | — | — |
| RG-TCK-06 | Socle | Ticket clôturé archivé automatiquement | 🟡 | `DateArchivageUtc` + `MoteurRetentionService` | `Domain/Tickets/Ticket.cs:62` ; `Securite/MoteurRetentionService.cs` | Rétention légale implémentée ; l'archivage **à la clôture** n'est pas démontré | P2 | Vérifier ou ajouter le déclenchement à la clôture |
| RG-TCK-07 | Cycle de vie | Annulation libre jusqu'à un **seuil paramétrable par workflow** | 🔴 | Paramètre `etapeBloquanteAtteinte` passé par l'appelant | `Tickets/AnnulerTicketCommand.cs:38` ; `Tickets/DetailTicket.razor:235` | Aucun seuil paramétrable n'existe ; l'UI passe **`false` en dur**, la garde serveur ne se déclenche jamais | **P1** | Créer le référentiel de seuil, calculer côté serveur |
| RG-TCK-08 | Cycle de vie | Demande d'annulation au-delà du seuil | ✅ | `DemanderAnnulationAsync` + décision agent | `Tickets/AnnulerTicketCommand.cs:58-92` | — | — | — |
| RG-TCK-09 | Cycle de vie | Annulation motivée, horodatée, historisée | ✅ | Motif obligatoire, historique + audit | `Tickets/AnnulerTicketCommand.cs:31-34, 49-51` | — | — | — |
| RG-TCK-10 | Cycle de vie | Relances automatiques paramétrables | ✅ | `ParametresRelance(NombreRelancesMax, DelaiEntreRelances)` | `Tickets/RelancesAbsenceReponseUseCase.cs:9` | — | — | — |
| RG-TCK-11 | Cycle de vie | Pas de clôture suggérée si état engageant | ✅ | `EstEligibleASuggestionDeCloture` + `EstEnEtatEngageant` | `Domain/Tickets/Ticket.cs:60, 460` | — | — | — |
| RG-TCK-12 | Cycle de vie | Réouverture par CIT seulement, motivée | ✅ | `ReouvrirTicketCommand` + permission `TicketRouvrir` | `Tickets/ReouvrirTicketCommand.cs` | — | — | — |
| RG-TCK-13 | Unicité | Blocage si BL déjà actif | ✅ | `BlDejaActifException` + index unique filtré | `Domain/Tickets/BlDejaActifException.cs` ; migration `IndexUniqueFiltreNumeroBl` | — | — | — |
| RG-TCK-14 | Unicité | Contrôle BL par BL, multi-BL possible | ✅ | Chaque BL traité indépendamment, jamais tout-ou-rien | `BillingImport/SoumettreDemandeImportCommand.cs:24-29` | — | — | — |
| RG-TCK-15 | Unicité | Règle équivalente sur le booking export | ✅ | `BookingDejaActifException` | `BillingExport/BookingDejaActifException.cs` ; `SoumettreDemandeExportCommand.cs:95` | — | — | — |
| RG-MSG-01 | Échanges | Message client/agent vs note interne | ✅ | `enum TypeMessage` + filtrage au dépôt | `Domain/Messagerie/Message.cs:3` | — | — | — |
| RG-MSG-02 | Échanges | Distinction visuelle, horodatage, historisation | ✅ | `EstInterne` porté à la vue ; **filtrage serveur** pour les clients | `Persistence/Repositories.cs:143-148` ; `ObtenirFicheTicketQuery.cs:104-106` | — | — | — |
| RG-MSG-03 | Échanges | Mention d'un collaborateur → notification | ✅ | `MentionnerCollaborateurCommand` publie l'événement | `Messagerie/MentionnerCollaborateurCommand.cs:31-40` | — | — | — |
| RG-MSG-04 | Échanges | Une mention ne réaffecte pas le ticket | ✅ | Invariant explicitement asserté | `Messagerie/MentionnerCollaborateurCommand.cs:29, 43` | — | — | — |
| RG-WKF-01 | Workflow | Circuit nominal **ni figé ni codé en dur** | 🟡 | Files résolues par nom constant, aiguillage par `switch` | `BillingImport/SoumettreDemandeImportCommand.cs:46` ; `BillingExport/EditerFactureExportCommand.cs:47-53` | Le circuit est du code ; seule la matrice de transitions est administrable | P2 | Extraire le circuit en données (étapes ordonnées par module × profil) |
| RG-WKF-02 | Workflow | Dérogation au circuit par les habilités | ✅ | `DerogationWorkflowService` | `Tickets/DerogationWorkflowService.cs` | — | — | — |
| RG-WKF-03 | Workflow | Dérogation tracée (historique + audit) | ✅ | Double consignation systématique | `Tickets/TransfererTicketCommand.cs:70-75` | — | — | — |
| RG-WKF-04 | Prise en charge | État « en attente de prise en charge » | ✅ | `StatutTicket.EnAttenteDePriseEnCharge` | `Domain/Tickets/StatutTicket.cs` | — | — | — |
| RG-WKF-05 | Prise en charge | Prise par opérateur habilité ou affectation superviseur | ✅ | `PrendreEnChargeCommand` + `ActionsSuperviseurCommand` | `Tickets/PrendreEnChargeCommand.cs` ; `Supervision/ActionsSuperviseurCommand.cs` | — | — | — |
| RG-WKF-06 | Prise en charge | Horodatages de file enregistrés, délai calculé | ✅ | `PassageEnFile` (4 bornes + rang) | `Domain/Tickets/PassageEnFile.cs:37-53` | — | — | — |
| RG-WKF-07 | Transfert | Transfert sans recréation, tout conservé | ✅ | `Ticket.Transferer` ne touche ni numéro ni historique | `Tickets/TransfererTicketCommand.cs:67-69` | — | — | — |
| RG-WKF-08 | Transfert | Transfert motivé, opérateur/date/files tracés | ✅ | Historique `de=…;vers=…;motif=…` + audit | `Tickets/TransfererTicketCommand.cs:70-73` | Tracé en **texte**, pas en table structurée (cf. § 9, `TRANSFERT`) | P2 | Table `Transferts` pour l'exploitabilité |
| RG-WKF-09 | Transfert | Réaffectation entre opérateurs, non réservée au superviseur | ✅ | `ReaffecterTicketCommand` + permission dédiée | `Tickets/ReaffecterTicketCommand.cs` | — | — | — |
| RG-WKF-10 | Transfert | Client notifié d'une réorientation | 🟡 | Événement `TicketTransfere` publié et diffusé | `Tickets/TransfererTicketCommand.cs:77-78` | Diffusion réelle bloquée par RG-COM-01 (adaptateur simulé) | P1 | Dépend de l'arbitrage fournisseur |
| RG-WKF-11 | Transfert | Matrice de transitions paramétrable, refus hors matrice | ✅ | `TransitionsAutorisees` + écran d'administration | `Domain/Files/TransitionAutorisee.cs` ; `Administration/MatriceTransitions.razor` | — | — | — |
| RG-TPS-01 | Temps | Attente et traitement **par passage en file** | ✅ | `PassageEnFile.TempsAttente/TempsTraitement` | `Domain/Tickets/PassageEnFile.cs:79-107` | — | — | — |
| RG-TPS-02 | Temps | Attente client mesurée à part, jamais imputée | ✅ | `PeriodeAttenteClient` + `TempsInterneCit = Total − AttenteClient` | `Domain/SuiviTemps/MesureTemps.cs:48-50` | — | — | — |
| RG-TPS-03 | Temps | 3 durées distinguées, total = création → clôture | ✅ | Bornage explicite parcours vs étape | `Domain/SuiviTemps/MesureTemps.cs:45-49` | — | — | — |
| RG-SLA-01 | SLA | Paramétrable par module, type, étape, file, priorité, **catégorie de client** | 🟡 | 4 axes sur 6 : `Module`, `TypeDemande`, `FileId`, `PrioriteId` | `Domain/SuiviTemps/ParametreSlaAdministrable.cs:13-46` | Dimension **catégorie de client absente** | P2 | Ajouter l'axe `TypeClient` et sa spécificité |
| RG-SLA-02 | SLA | Seuils information / alerte / dépassement / escalade | ✅ | 4 seuils, escalade testée avant dépassement | `Domain/SuiviTemps/EcheanceSla.cs:19-56` | — | — | — |
| RG-SLA-03 | SLA | SLA suspendu pendant l'attente client | ✅ | Exclusion en amont du calcul (temps ouvré) | `Domain/SuiviTemps/EcheanceSla.cs:34-37` + `MesureTemps` | — | — | — |
| RG-SLA-04 | SLA | Seuils et durées administrables | ✅ | `ParametresSla` + écran `Administration/ParametresSla.razor` | `Administration/GererParametresSlaCommand.cs` | — | — | — |
| RG-ALE-01 | Alertes | 6 conditions de déclenchement | ✅ | 9 valeurs de `TypeAlerte` couvrant les 6 | `Domain/Supervision/Alerte.cs:4-17` | — | — | — |
| RG-ALE-02 | Alertes | Escalade paramétrable sur dépassement | ✅ | `RegleEscalade` + `SlaEscalade` + écran | `Domain/Supervision/RegleEscalade.cs` ; `Administration/ReglesEscalade.razor` | — | — | — |
| RG-DIS-01 | Disponibilité | Indisponible ⇒ aucune affectation automatique | ✅ | `FiltreDisponibilite` appliqué **avant** toute stratégie | `Domain/Affectation/StrategiesAffectation.cs:15-19` | — | — | — |
| RG-DIS-02 | Disponibilité | Superviseur modifie la dispo, réaffecte en masse | ✅ | `Administration/Disponibilite.razor` + `ActionDeMasseCommand` | `Supervision/ActionDeMasseCommand.cs` | — | — | — |
| RG-AFF-01 | Affectation | 6 modes paramétrables par file | ✅ | Prise libre, manuelle, round-robin, charge, dispo, compétence | `Domain/Affectation/StrategiesAffectation.cs` ; `Administration/StrategieFile.razor` | — | — | — |
| RG-PRI-01 | Priorité | Référentiel paramétrable, modification tracée | ✅ | `NiveauxPriorite` + `ModificationsPriorite` | `Tickets/ModifierPrioriteCommand.cs` | — | — | — |
| RG-PRI-02 | Priorité | Le client ne déclare pas l'urgence | ✅ | Permission `PrioriteModifier` exigée | `Tickets/ModifierPrioriteCommand.cs:26` | — | — | — |
| RG-MAS-01 | Masse | Action de masse tracée **ticket par ticket** | ✅ | Audit individuel dans la boucle, succès **et** échec | `Supervision/ActionDeMasseCommand.cs:33-50` | — | — | — |
| RG-IMP-01 | Import | Soumission de **1 à 5 BL**, un ticket par BL | 🟡 | Multi-BL implémenté, tickets indépendants | `BillingImport/SoumettreDemandeImportCommand.cs` ; `BillingImport/SoumettreDemande.razor:220-227` | **Aucune borne supérieure** : ni la commande ni le formulaire ne limitent à 5 | P2 | Borne paramétrable, validée serveur |
| RG-IMP-02 | Import | File Pilote Manifeste : Validé / Rejeté / Complément | ✅ | 3 issues implémentées | `BillingImport/ValiderTicketImportCommand.cs`, `RejeterTicketImportCommand.cs`, `GestionComplementManifesteCommand.cs` | — | — | — |
| RG-IMP-03 | Import | Retour en file après dépôt du complément | ✅ | `GestionComplementManifesteCommand` | même fichier | — | — | — |
| RG-IMP-04 | Import | Orientation automatique selon le type de client | ✅ | `switch` sur l'instantané `Dossier.TypeClient` | `BillingImport/ValiderTicketImportCommand.cs` | — | — | — |
| RG-IMP-05 | Import | Parcours client **normal** (paiement + preuve + caisse) | ✅ | `DemanderFactureCommand` → `ValidationPaiement.razor` → contrôle doc | `BillingImport/ValiderPreuvePaiementCommand.cs` | — | — | — |
| RG-IMP-06 | Import | Parcours **en compte** (pas de retour client) | ✅ | Transition directe vers Contrôle Documentaire | `BillingImport/EditerFactureCommand.cs` | — | — | — |
| RG-IMP-07 | Import | Parcours **deposit** (lettrage comptabilité) | ✅ | `ConfirmerLettrageCommand` + `FileComptabiliteClient.razor` | `BillingImport/ConfirmerLettrageCommand.cs` | — | — | — |
| RG-IMP-08 | Import | Parcours **IES** parallèle, jointure avant retour | ✅ | `SuiviBrancheIes` + `ConstaterJointureIesEtFinaliserCommand` | `Domain/Tickets/SuiviBrancheIes.cs` ; `JointureIesIncompleteException` | — | — | — |
| RG-IMP-09 | Import | Bascule en présentiel après contrôle BL | ✅ | `DeclarerFinalisationPresentielleCommand` + `DeclarationPresentielle` | `Domain/Tickets/Ticket.cs:97` | — | — | — |
| RG-EXP-01 | Export | Un ticket par booking | ✅ | `SoumettreDemandeExportCommand` | `BillingExport/SoumettreDemandeExportCommand.cs` | — | — | — |
| RG-EXP-02 | Export | Back Office : Codifié / Rejeté / Complément | ✅ | `ValiderTicketExportCommand` + `FileBackOfficeExport.razor` | `BillingExport/ValiderTicketExportCommand.cs` | — | — | — |
| RG-EXP-03 | Export | **Draft IN avant facture**, document final BEE | 🟡 | Facturation et aiguillage par profil présents | `BillingExport/EditerFactureExportCommand.cs:38-53` | **Aucune étape Draft IN** : le seul « Draft » du dépôt est un libellé d'auto-sauvegarde de formulaire | P2 | Ajouter l'étape et son type de document |
| RG-EXP-04 | Export | IES export : facture et BEE côté plateforme externe | ✅ | `FileIesExport.razor` + branches IES | `BillingExport/FileIesExport.razor` | — | — | — |
| RG-SIM-01 | Simulation | Saisie dossier + caractéristiques conteneur | ✅ | `DossierSimulation` porte navire, voyage, BL/booking | `Domain/Tarification/DossierSimulation.cs:24-31` | — | — | — |
| RG-SIM-02 | Simulation | Tarif par clé Sens + Taille + Catégorie, grille administrable | ✅ **[lot 3FN]** | 3 clés étrangères vers référentiels ; catégorie choisie en liste | `Domain/Tarification/LigneTarifaire.cs:19-25` ; `SimulationProformaPage.razor:120` | — | — | — |
| RG-SIM-03 | Simulation | 7 postes calculés par conteneur | ✅ | Les 7 postes dans l'ordre | `Domain/Tarification/MoteurCalculProforma.cs:60-105` | — | — | — |
| RG-SIM-04 | Simulation | Durée de séjour bornes incluses | ✅ | `(Enlèvement − Arrivée) + 1` | `Domain/Tarification/DossierSimulation.cs:31` | — | — | — |
| RG-SIM-05 | Simulation | Branchement Reefer uniquement, franchise paramétrable | ✅ **[lot 3FN]** | Attribut `DeclencheBranchementFrigorifique` du référentiel | `Domain/Tarification/Referentiels/TypeConteneur.cs:26` ; `MoteurCalculProforma.cs:73-79` | — | — | — |
| RG-SIM-06 | Simulation | Majoration lourd, seuil par taille | ✅ **[lot 3FN]** | `SeuilsPoidsMajoration`, une ligne par grille × taille | `Domain/Tarification/Parametres/SeuilPoidsMajoration.cs` | — | — | — |
| RG-SIM-07 | Simulation | Majorations dangereux et OOG, cumulables | ✅ | Cumul additif sur l'acconage, sans cascade | `MoteurCalculProforma.cs:92-101` ; test `Majorations_cumul_additif...` | — | — | — |
| RG-SIM-08 | Simulation | Stationnement par tranches, franchise par sens | ✅ **[lot 3FN]** | `TranchesStationnement` + `FranchisesStationnement` ; l'absence de franchise vaut exclusion | `Domain/Tarification/Parametres/TrancheStationnement.cs:66-76` | — | — | — |
| RG-SIM-09 | Simulation | Répartition entre bénéficiaires paramétrable | ✅ **[lot 3FN]** | `QuotesPartsBeneficiaire` + invariant somme = 1 | `Domain/Tarification/VersionGrilleTarifaire.cs:176-190` | — | — | — |
| RG-SIM-10 | Simulation | Totaux conteneur et dossier, résultat indicatif | ✅ | `TotalLigne`, `TotalGeneralXof`, mention légale | `Domain/Tarification/ResultatSimulationProforma.cs` | — | — | — |
| RG-SIM-11 | Simulation | **Tous** les paramètres administrables sans développement | 🟡 **[lot 3FN]** | Modèle entièrement administrable ; bornes de tranches sorties du code | `MoteurCalculProforma.cs:126-152` ; test « quatrième tranche » | **Aucun écran** n'expose les référentiels ni les paramètres de grille : seul l'import CSV des lignes existe | P2 | Écrans d'administration référentiels + paramètres |
| RG-SIM-12 | Simulation | Grilles versionnées, simulation reproductible | ✅ | `VersionGrilleTarifaire` + instantané figé | `Domain/Tarification/SimulationProforma.cs:9-16` ; test golden master sur SQL réel | — | — | — |
| RG-AVE-01 | Avoir/Exo | Ticket + workflow propre, **étapes et intervenants paramétrables** | 🟡 | Workflow implémenté, écrans dédiés | `AvoirExoneration/DemandeAvoirExoCommands.cs` ; 4 pages `AvoirExoneration/*` | Étapes et intervenants **codés**, non paramétrables | P2 | Même remédiation que RG-WKF-01 |
| RG-AVE-02 | Avoir/Exo | Décision motivée déposée et notifiée | ✅ | `DecisionAvoirExo`, `MotifRejetAvoirExo`, événement | `Domain/Tickets/Ticket.cs:153-178` | — | — | — |
| RG-REC-01 | Complaint | Catégorie paramétrable et dynamique | ✅ | `CategoriesReclamation` + écran d'administration | `Domain/Reclamations/CategorieReclamation.cs` ; `CustomerComplaint/AdministrationCategories.razor` | — | — | — |
| RG-REC-02 | Complaint | Instruction par le Support Opérationnel | ✅ | `FileReclamations.razor` + `ComplaintInstruire` | `Application/CustomerComplaint/*` | — | — | — |
| RG-REC-03 | Complaint | Clôture avec **réponse formalisée obligatoire** | ✅ | `CloturerAvecReponseFormalisee`, réponse requise | `Tickets/CloturerReclamationCommand.cs:11, 36` | — | — | — |
| RG-ENR-01 | Enrôlement | Dossier à distance, pièces paramétrables | ✅ | `SoumettreEnrolementCommand` + `PiecesExigeesEtape` | `Enrolement/SoumettreEnrolementCommand.cs` ; `Administration/PiecesExigeesEtape.razor` | — | — | — |
| RG-ENR-02 | Enrôlement | Convocation et **remise du badge** tracées | 🟡 | Convocation + constat de remise implémentés | `Enrolement/DeclencherConvocationCommand.cs` ; `ConstaterRemiseBadgeCommand.cs:39-41` | **Aucun badge modélisé** : ni numéro, ni date d'émission, ni expiration, ni statut. Choix documenté (CLAR-ENR-05) mais contraire au MCD | P2 | Trancher : modéliser `BADGE` ou amender le document |
| RG-COM-01 | Communication | Notification e-mail + portail + **canal mobile** | 🔴 | Canaux déclarés, tous branchés sur un journaliseur | `Domain/Notifications/CanalNotification.cs` ; `Infrastructure/Notifications/CanalJournalise.cs` ; `DependencyInjection.cs:349-359` | **Aucun envoi réel** hors portail | **P1** | Arbitrage fournisseur (PC-16) puis adaptateur réel |
| RG-COM-02 | Communication | Événements et modèles paramétrables | ✅ | `ModelesNotification` + `ReglesDiffusion` + écran | `Notifications/AdministrationNotifications.razor` | — | — | — |
| RG-COM-03 | Communication | Fil de commentaires = canal de référence | ✅ | Messagerie du ticket | `Domain/Messagerie/Message.cs` | — | — | — |
| RG-NOT-01 | Notification | État Lu/Non lu, accès direct au ticket | ✅ | `EstLue` + `MarquerLue` + centre de notifications | `Domain/Notifications/Notification.cs:45, 102-107` | — | — | — |
| RG-NOT-02 | Notification | Règles de diffusion paramétrables par événement et canal | ✅ | `RegleDiffusion(TypeEvenement, Canal, Module?, FileId?)` | `Domain/Notifications/RegleDiffusion.cs:15-38` | — | — | — |
| RG-CON-01 | Continuité | Reprise au guichet sans ressaisie | ✅ | `RechercherGuichetQuery` + `ConsulterDossierGuichetQuery` | `Guichet/RechercheGuichetQueries.cs:15, 65` ; `Guichet/RechercheGuichet.razor` | — | — | — |
| RG-CON-02 | Continuité | Prise de rendez-vous | ✅ | `RendezVous` + 3 écrans | `Domain/Guichet/RendezVous.cs` ; `Guichet/PrendreRendezVous.razor` | — | — | — |
| RG-IA-01 | Assistant IA | Réponses de premier niveau en français | ⚫ | **Aucune** | Recherche `assistant\|chatbot\|conversation` sur `src/` : 0 résultat | Fonction totalement absente | **P1** | Arbitrage fournisseur (PC-18) |
| RG-IA-02 | Assistant IA | Escalade humaine sans perte de contexte | ⚫ | **Aucune** | idem | Fonction totalement absente | **P1** | idem |
| RG-SEC-01 | Sécurité | Authentification, RBAC, moindre privilège, niveau champ | ✅ | Identity + permissions + `AutorisationChampService` (IES) | `Application/Securite/AutorisationChampService.cs` ; `Domain/Securite/EtatDroitChamp.cs` | — | — | — |
| RG-SEC-02 | Sécurité | Cloisonnement strict client / agent | 🔴 | `CloisonnementService` correct **mais non utilisé par l'API** | `Web/Api/ApiEndpoints.cs:43-47, 83-87` | Fuite inter-locataires par énumération de numéro de ticket | **P0** | Voir § 26 |
| RG-SEC-03 | Sécurité | Journal d'audit inviolable de toute action significative, **connexion incluse** | 🟡 | Journal append-only, 11 familles d'actions tracées | `Persistence/JournalAuditAppendOnlyGuard.cs` | **La connexion n'est jamais consignée** — 0 appel à `ConsignerAuditAsync` dans `Connexion.razor` | P2 | Consigner connexion réussie et échec |
| RG-SEC-04 | Sécurité | Un lien e-mail/SMS ne donne jamais d'accès direct | ✅ | Toutes les pages sous `[Authorize]`, lien vers le ticket | `Tickets/FicheTicket.razor:3` ; `Documents/TelechargerDocumentQuery.cs` | — | — | — |

---

## 7. Audit MCD

Les **38 entités** du dictionnaire (§ 2.1) et les associations structurantes (§ 2.6) ont été
confrontées au modèle.

**Généralisation ACTEUR.** Le document prescrit une spécialisation exclusive et totale en
`UTILISATEUR_CLIENT` / `AGENT` / `CHAUFFEUR`. Le projet la traduit par `AspNetUsers` +
`NatureUtilisateur` + `UtilisateurSociete`. **CONFORME — IMPLÉMENTATION ÉQUIVALENTE** pour les deux
premiers sous-types ; le troisième (`CHAUFFEUR`) n'existe pas (cf. RG-CPT-08).

**Associations vérifiées conformes** : `RATTACHE` (1,1)-(0,n) via `UtilisateurSociete` ;
`PEUT_TRANSFERER_VERS` réflexive via `TransitionsAutorisees` ; `REGROUPE` (1,n)-(1,1) via
`Dossier` ; `LIE_A` réflexive n-n via `LiaisonsTicket` ; `FAIT_OBJET_PASSAGE` (1,1)-(0,n) via
`PassagesEnFile` ; `CATEG_SCOPED` (catégorie portée par un sens) **[lot 3FN]** ;
`SEUIL_TAILLE` et `FRANCHISE_SENS` **[lot 3FN]** ; `RATTACHEE_TICKET` (0,1)-(0,1) via
`SimulationProforma.TicketId`.

**Écarts de cardinalité relevés :**

| Association | Attendu | Constaté | Statut |
|:---|:---|:---|:---|
| `MENTIONNE` (ECHANGE — AGENT) | (0,n) — (0,n) | `Message.CollaborateurMentionneId`, **une seule mention par message** | 🟡 |
| `HABILITE` (AGENT — ROLE) | (0,n) — (0,n) porteuse | Remplacée par des codes de permission portés par `Habilitations` | ✅ équivalent |
| `FAIT_OBJET_TRANSFERT` | (1,1) — (0,n) | Aucune entité `TRANSFERT` ; trace textuelle | ⚫ |
| `ABOUTIT_BADGE` (TICKET — BADGE) | (1,1) — (0,1) | Aucune entité `BADGE` | ⚫ |

---

## 8. Audit MLD

Les cinq règles de passage du § 3.1 sont respectées : entité → table, association (0,1)-(0,n) →
clé étrangère côté « n », association porteuse → table à part entière, association n-n → table de
liaison à clé composite, généralisation → table par sous-type.

**Vérification 1FN / 2FN / 3FN.** Le document décrit en § 3.3 deux corrections apportées au MLD.
Au moment de l'audit :

| Correction § 3.3 | État |
|:---|:---|
| n° 1 — référentiels normalisés (`SENS_OPERATION`, `TAILLE_CONTENEUR`, `TYPE_CONTENEUR`, `CATEGORIE_MARCHANDISE`) | ✅ appliquée **[lot 3FN]** |
| n° 2 — extraction des repeating groups (`SEUIL_POIDS_MAJORATION`, `FRANCHISE_STATIONNEMENT`, et par extension `TRANCHE_STATIONNEMENT`, `QUOTE_PART_BENEFICIAIRE`, `PARAMETRE_MAJORATION`) | ✅ appliquée **[lot 3FN]** |

Les deux dérogations documentées par le document sont respectées : les montants figés de
`CONTENEUR_SIMULE` sont bien conservés (fait daté, SIM-09), et les durées calculées le sont — mais
**en C# plutôt qu'en colonnes `PERSISTED`**. Divergence acceptable techniquement : le document
justifiait `PERSISTED` par l'indexation des tableaux de bord ; le calcul applicatif reste correct
mais n'est pas indexable.

---

## 9. Audit MPD — 44 tables

| # | Table du document | Implémentation | Statut |
|---:|:---|:---|:---|
| 1 | `ACTEUR` | `AspNetUsers` (+ `NomAffiche`, `Nature`, `EstActif`) | 🟡 pas de `type_acteur` à 3 valeurs |
| 2 | `COMPTE_CLIENT` | `Societes` | ✅ |
| 3 | `UTILISATEUR_CLIENT` | `UtilisateursSociete` + `AspNetUsers` | ✅ |
| 4 | `AGENT` | `AspNetUsers` (Nature=Agent) + `Disponibilites` | 🟡 pas de `matricule` |
| 5 | `CHAUFFEUR` | — | ⚫ |
| 6 | `ROLE` | `AspNetRoles` présente mais **inutilisée** ; `Habilitations` tient le rôle | 🟡 choix documenté (PC-03) |
| 7 | `FILE_TRAITEMENT` | `Files` | ✅ |
| 8 | `HABILITE` | `Habilitations` | ✅ |
| 9 | `RATTACHE_A_FILE` | absorbée par `Habilitations.FileId` | 🟡 équivalent fonctionnel |
| 10 | `MEMBRE_DE` | absorbée par `Habilitations.FileId` | 🟡 équivalent fonctionnel |
| 11 | `TRANSITION_AUTORISEE` | `TransitionsAutorisees` | ✅ |
| 12 | `DOSSIER` | `Dossiers` | ✅ |
| 13 | `STATUT` | `LibellesStatutTicket` (libellés seulement ; l'énum reste l'identité) | 🟡 écart résiduel assumé |
| 14 | `PRIORITE` | `NiveauxPriorite` | ✅ |
| 15 | `CATEGORIE_RECLAMATION` | `CategoriesReclamation` | ✅ |
| 16 | `TYPE_DOCUMENT` | `PiecesExigeesEtape` — **non rattachée aux documents déposés** | 🟡 |
| 17 | `TICKET` | `Tickets` | ✅ |
| 18 | `TICKET_LIE` | `LiaisonsTicket` | ✅ |
| 19 | `PASSAGE_FILE` | `PassagesEnFile` (+ `Rang`, non prévu au MPD) | ✅ supérieur |
| 20 | `TRANSFERT` | — trace textuelle dans `HistoriqueTickets` | ⚫ |
| 21 | `DOCUMENT` | `Documents` (+ antivirus, scellement SHA-256) | ✅ supérieur |
| 22 | `ECHANGE` | `Messages` | ✅ |
| 23 | `MENTIONNE` | `Message.CollaborateurMentionneId` (1 seule mention) | 🟡 |
| 24 | `HISTORIQUE_EVENEMENT` | `HistoriqueTickets` | ✅ |
| 25 | `ENTREE_AUDIT` | `JournalAudit` (+ garde append-only) | ✅ supérieur |
| 26 | `MODELE_NOTIFICATION` | `ModelesNotification` | ✅ |
| 27 | `NOTIFICATION` | `Notifications` + `EnvoisNotification` | ✅ supérieur |
| 28 | `SLA` | `ParametresSla` | 🟡 axe catégorie client manquant |
| 29 | `ALERTE` | `Alertes` | ✅ |
| 30 | `GRILLE_TARIFAIRE` | `VersionsGrillesTarifaires` | ✅ |
| 31 | `SENS_OPERATION` | `SensOperations` | ✅ **[lot 3FN]** |
| 32 | `TAILLE_CONTENEUR` | `TaillesConteneur` | ✅ **[lot 3FN]** |
| 33 | `TYPE_CONTENEUR` | `TypesConteneur` | ✅ **[lot 3FN]** |
| 34 | `CATEGORIE_MARCHANDISE` | `CategoriesMarchandise` (unicité code+sens) | ✅ **[lot 3FN]** |
| 35 | `LIGNE_TARIF` | `LignesTarifaires` (3 FK) | ✅ **[lot 3FN]** |
| 36 | `PARAMETRE_MAJORATION` | `ParametresMajoration` | ✅ **[lot 3FN]** |
| 37 | `SEUIL_POIDS_MAJORATION` | `SeuilsPoidsMajoration` | ✅ **[lot 3FN]** |
| 38 | `TRANCHE_STATIONNEMENT` | `TranchesStationnement` (bornes min/max) | ✅ **[lot 3FN]** |
| 39 | `FRANCHISE_STATIONNEMENT` | `FranchisesStationnement` | ✅ **[lot 3FN]** |
| 40 | `QUOTE_PART_BENEFICIAIRE` | `QuotesPartsBeneficiaire` | ✅ **[lot 3FN]** |
| 41 | `SIMULATION` | `SimulationsProforma` | ✅ |
| 42 | `CONTENEUR_SIMULE` | `LignesResultatsSimulation` + `PartsStationnementSimulees` | ✅ supérieur |
| 43 | `BADGE` | — | ⚫ |
| 44 | `RENDEZ_VOUS` | `RendezVous` | ✅ |

**Total : 32 ✅ · 9 🟡 · 3 ⚫ → 36,5 / 44 = 83,0 %**

**Au-delà du MPD**, l'implémentation compte 16 tables supplémentaires qui n'y figurent pas :
`PeriodesAttenteClient`, `ReglesEscalade`, `CompetencesOperateur`, `PerimetresSupervision`,
`MotifsTransfert`, `LotsPurgeRetention`, `PreferencesCanalUtilisateur`, `SuivisBranchesIes`,
`ModificationsPriorite`, `BrouillonsSaisie`, `InvitationsCollaborateur`,
`JustificatifsDemandeCompte`, `PiecesExigeesCompte`, `PiecesExigeesEtape`, `JournalEvenements`,
`ReglesDiffusion`. Ce sont des **améliorations d'architecture** (catégorie 3), pas des écarts.

### Conventions SQL (§ 6.1 du document)

| Convention prescrite | Constaté | Classement |
|:---|:---|:---|
| Schéma `dbo`, noms en MAJUSCULES | PascalCase français (`Tickets`) | Divergence acceptable |
| PK `BIGINT`/`INT IDENTITY` | `uniqueidentifier` (Guid) | Divergence acceptable |
| `NVARCHAR` dimensionné | Respecté (`HasMaxLength` systématique) | ✅ |
| `DATETIME2` | Respecté | ✅ |
| `DECIMAL(14,2)` montants | `decimal(18,2)`, taux en `(5,4)` | ✅ (plus précis) |
| `BIT` booléens | Respecté | ✅ |
| CHECK sur énumérations fermées | **Absent** — les énumérations sont converties en `int` ou `string` sans CHECK | 🟡 divergence acceptable (garde applicative) |
| Index sur chaque FK et colonne de recherche | Respecté, y compris index **filtrés** (BL actif) | ✅ supérieur |
| `ON DELETE NO ACTION` par défaut | `Restrict` sur les FK de référentiel, `Cascade` sur les agrégats | ✅ |
| Colonnes calculées `PERSISTED` | Calcul applicatif | 🟡 non indexable |

---

## 10. Audit MCT — 7 macro-processus

| Processus | Statut | Preuve |
|:---|:---|:---|
| P1 — Billing Office IMPORT | 🟡 | Tronc commun + 4 variantes de profil présents ; borne 1–5 BL absente (RG-IMP-01) |
| P2 — Billing Office EXPORT | 🟡 | Codification, profils, BEE présents ; **étape Draft IN absente** (RG-EXP-03) |
| P3 — Simulation de Proforma | ✅ | Calcul, enregistrement, export PDF, rattachement ticket (`SimulationProforma.TicketId`) |
| P4 — Avoir et Exonération | 🟡 | Point de décision unique implémenté ; étapes non paramétrables (RG-AVE-01) |
| P5 — Customer Complaint | ✅ | Rebouclage complément + réponse formalisée obligatoire |
| P6 — Enrôlement Transporteurs | 🟡 | Synchronisation convocation ⊗ présence physique présente ; badge non modélisé |
| P7 — Cycle de vie transverse | ✅ | Prise en charge (OU libre/superviseur), transfert (ET habilitation ⊗ matrice), horloge SLA |

Les événements et résultats du dictionnaire § 4.1 sont tous représentés, sauf « Chauffeur présent
(photo/badge) » qui n'a pas de porteur de données (cf. `BADGE`).

---

## 11. Audit MOT — 12 postes de travail

| Poste | Implémenté | Preuve |
|:---|:---|:---|
| Client (4 profils) | ✅ | `Client/DashboardClient.razor`, `MesDemandes.razor` |
| Transporteur / Chauffeur | 🟡 | Écrans présents (`Enrolement/*`, `Comptes/InscriptionTransporteur.razor`) mais **pas de nature de compte distincte** |
| Pilote Manifeste | ✅ | `BillingImport/FilePiloteManifeste.razor` |
| Back Office Export | ✅ | `BillingExport/FileBackOfficeExport.razor` |
| Pilote Facturation Terre | ✅ | `FileFacturation.razor`, `FileFacturationExport.razor` |
| Comptable Client | ✅ | `FileComptabiliteClient.razor`, `FileComptabiliteExport.razor` |
| Pilote Contrôle Documentaire | ✅ | `FileControleDocumentaire.razor` (+ export) |
| Support Opérationnel | ✅ | `CustomerComplaint/FileReclamations.razor` |
| Agent Gate | ✅ | `Enrolement/FileAgentGate.razor` |
| Superviseur | ✅ | `Supervision/TableauDeBord.razor` + périmètres |
| Administrateur fonctionnel | ✅ | 12 écrans sous `Administration/` |
| Plateforme (automatique) | ✅ | 5 `BackgroundService` |

Les 10 traitements annoncés « Automatique » au tableau § 5.3 ont tous une implémentation. La
dimension proprement organisationnelle (lieux, horaires, charge des postes) **n'est pas vérifiable
depuis le code** et relève de la recette.

---

## 12. Audit du socle Ticket

Vérifié conforme : numéro unique séquencé, numéro de dossier, regroupement multi-BL, liaison de
tickets, documents, échanges, historique horodaté, statut, priorité, file courante, opérateur
assigné, cycle de vie complet, motifs obligatoires, conservation de l'historique au transfert.

**Cas limites contrôlés :**
- Multi-BL dont certains refusés → un dossier est **toujours** créé, les BL valides passent
  (`SoumettreDemandeImportCommand.cs:24-29`) : jamais de tout-ou-rien.
- BL refusé appartenant à une autre société → l'identifiant du ticket existant **n'est pas révélé**
  (`BlRefuseInfo`, commentaire lignes 18-22). Bonne pratique de non-divulgation.
- Double prise en charge concurrente → `VersionConcurrence` (`rowversion`) +
  `ConflitDeConcurrenceException`, testé sur SQL Server réel.
- Double clôture → garde dans `Ticket.Cloturer`.

**Écart :** RG-TCK-07, détaillé en § 27.

---

## 13–18. Audits par module

Le détail figure dans la matrice § 6. Synthèse des écarts uniquement :

- **Billing IMPORT** : borne 1–5 BL absente.
- **Billing EXPORT** : étape Draft IN absente.
- **Simulation** : modèle conforme et vérifié au franc près (golden masters Annexe A.8 en mémoire
  et après rechargement SQL) ; **aucun écran d'administration** des référentiels et paramètres.
  Le « PDF » produit n'est pas un PDF valide au sens ISO 32000 — c'est un texte avec en-tête
  `%PDF-1.4`, ce que le projet documente lui-même par un test de preuve
  (`AuditPreuveValiditePdfTests`). P2.
- **Avoir / Exonération** : étapes et intervenants non paramétrables.
- **Customer Complaint** : conforme.
- **Enrôlement** : badge non modélisé.

---

## 19–21. Workflow, SLA, Notifications

Voir matrice. Points saillants : circuit nominal codé (RG-WKF-01), axe catégorie client absent du
SLA (RG-SLA-01), canaux externes en adaptateur simulé (RG-COM-01).

---

## 22. Audit Sécurité

| Contrôle | Résultat |
|:---|:---|
| Authentification | ✅ Identity, 2FA, verrouillage (`SecCert01VerrouillageTests`) |
| Autorisation | ✅ permissions bornées par file, jamais de rôle en dur |
| Moindre privilège | ✅ `DroitConsultation` par défaut = ses seules demandes |
| Cloisonnement écrans | ✅ `CloisonnementService`, fail-closed |
| **Cloisonnement API** | 🔴 **fail-open — voir P0** |
| Accès document | ✅ propriété vérifiée à **chaque** accès, pas seulement au dépôt |
| Antivirus | ✅ document inaccessible tant que non `Sain` |
| Upload | ✅ extension + taille (10 Mo) + **magic bytes** contre le masquage d'exécutable |
| Injection SQL | ✅ EF paramétré ; le seul ADO.NET direct (`NEXT VALUE FOR`) n'interpole que des constantes internes |
| En-têtes de sécurité | ✅ `EntetesSecuriteDurciesTests` |
| Ressources externes | ✅ `AucuneRessourceExterneTests` |
| Divulgation de jeton | ✅ `SecResetDivulgationJetonTests` |
| Liens de notification | ✅ toutes les pages `[Authorize]` |
| Secrets | ✅ audité par ailleurs (`docs/70`, `docs/71`) |

---

## 23. Audit Traçabilité

11 des 12 familles d'actions listées par RG-SEC-03 sont consignées : création, prise en charge,
changement de statut, dépôt, **téléchargement**, décision, transfert, réaffectation, changement de
priorité, annulation, réouverture, action administrateur.

**La connexion ne l'est pas** — `Connexion.razor` ne contient aucun appel à `ConsignerAuditAsync`.
Le journal est par ailleurs inviolable : `JournalAuditAppendOnlyGuard` bloque tout `Modified` /
`Deleted` avant `SaveChanges`.

---

## 24. Audit Interface utilisateur

79 pages routables couvrant les 5 profils. Deux gardes d'architecture verrouillent la cohérence
UI ↔ backend, et c'est remarquable :

- `ServicesAtteignablesTests` — aucune commande applicative ne peut être livrée sans être
  atteignable depuis un écran ou un traitement de fond. Sa liste de dette « ne doit que
  rétrécir ».
- `SurfaceRoutableTests` — aucune page routable n'injecte un dépôt ou le `DbContext`.

**Écart UI relevé** : aucun écran n'expose les référentiels tarifaires ni les paramètres de grille
(cf. RG-SIM-11). C'est aujourd'hui la seule fonction dont le modèle est administrable sans que
l'utilisateur puisse l'atteindre — exactement le motif pour lequel `ServicesAtteignablesTests`
existe, mais il ne couvre pas les entités, seulement les services.

Aucune interface ne propose une action interdite : les droits contextuels de `FicheTicketVue`
masquent les actions, **et** la commande revérifie systématiquement côté serveur.

---

## 25. Audit Tests

1 220 tests, 312 fichiers, 4 projets. Répartition : Domain 187 · Application 619 ·
Infrastructure 139 · Web 275.

**Points forts :** golden masters tarifaires au franc près, rejoués sur SQL Server réel ; tests de
concurrence sur vraie base ; tests d'autorisation par commande ; tests de recette CDC nommés
(`RecSce06…`, `SecCert…`, `NfrCert…`) ; tests d'architecture.

**Règles critiques sans couverture :**

| Règle | Test manquant |
|:---|:---|
| RG-CPT-04 / RG-SEC-02 | **Aucun test de cloisonnement sur les endpoints API** — c'est ce qui a laissé passer le P0 |
| RG-SEC-03 | Aucun test n'exige la consignation de la connexion |
| RG-IMP-01 | Aucun test sur la borne 1–5 BL |
| RG-EXP-03 | Aucun test sur l'étape Draft IN |
| RG-TCK-06 | Aucun test sur l'archivage à la clôture |
| RG-TCK-07 | Aucun test ne vérifie que l'annulation est refusée au-delà du seuil |
| RG-SLA-01 | Aucun test sur l'axe catégorie de client |

---

## 26. Non-conformités P0

### P0-01 — Fuite inter-locataires par l'API REST

**Problème.** Les trois endpoints de `Web/Api/ApiEndpoints.cs` implémentent un contrôle de
cloisonnement local au lieu d'appeler `CloisonnementService`. La condition
`ctx.Nature == Client && ctx.SocieteId.HasValue && ctx.SocieteId.Value != dossier.ClientId`
n'interdit l'accès **que si l'appelant possède une société**. Quand `SocieteId` est nul, aucune
vérification n'a lieu et la requête aboutit.

`ObtenirContexteUtilisateurQuery.cs:110` pose `SocieteId: societe?.Id` — nul pour tout compte sans
rattachement. Or `InscrireTransporteurCommand` crée précisément des comptes **sans société**, et
refuse explicitement ceux qui en ont une.

**Impact.** Tout compte transporteur authentifié — parcours d'inscription public — peut lire
n'importe quel ticket : module, type de demande, statut, date de création et **numéro de
connaissement**. Les numéros suivent le format `XXX-XXX-000001` issu d'une séquence SQL
(`ServiceNumerotation.cs:26-30`) : l'énumération est immédiate. Le second endpoint expose en outre
la liste complète des tickets d'un dossier.

**Règles violées.** RG-CPT-04, RG-SEC-02. Exigences CDC : CPT-04, SEC-03.

**Fichiers.** `src/CIT.Plateforme.Web/Api/ApiEndpoints.cs` lignes 43-47 et 83-87.

**Correction recommandée.** Remplacer les deux contrôles locaux par
`CloisonnementService.ExigerProprieteClientAsync`, qui est fail-closed par construction, et
statuer explicitement sur le cas agent (habilitation de file plutôt qu'absence de contrôle).

**Dépendances.** Aucune.

**Risque de régression.** Faible. Le durcissement peut refuser des appels aujourd'hui acceptés
pour des comptes sans société — ce qui est précisément l'objectif. Ajouter au préalable un test de
cloisonnement API (client A → ticket de client B → 403, transporteur → ticket quelconque → 403).

---

## 27. Non-conformités P1

### P1-01 — RG-TCK-07 : la garde d'annulation est désactivée depuis l'interface

`Tickets/DetailTicket.razor:235` appelle
`AnnulerLibrementAsync(…, etapeBloquanteAtteinte: false)` — **valeur littérale**. La garde
`CYC-02` de `AnnulerTicketCommand.cs:38` ne peut donc jamais se déclencher depuis l'application.
Seul le masquage du bouton (`peutAnnuler`, calculé sur `EstEnEtatEngageant`) protège aujourd'hui,
c'est-à-dire une protection d'affichage et non de commande. Par ailleurs, le « seuil paramétrable
par workflow » qu'exige la règle **n'existe pas** : `EstEnEtatEngageant` est un booléen posé par
les commandes métier, pas un seuil configurable.
**Correction** : calculer l'état bloquant côté serveur dans la commande, et créer le référentiel
de seuil par workflow.

### P1-02 — RG-COM-01 : aucun client n'est notifié hors du portail

`DependencyInjection.cs:349-359` branche `Email`, `Sms` et `WhatsApp` sur `CanalJournalise`, qui
journalise sans envoyer. Le CDC exige e-mail **et** au moins un canal mobile.
**Correction** : arbitrage fournisseur (PC-16) puis adaptateur réel. L'architecture est prête —
`ICanalNotification`, règles de diffusion, modèles, préférences par utilisateur et file de
réessai (`ReessayerEnvoisNotificationUseCase`) existent déjà.

### P1-03 — RG-IA-01 / RG-IA-02 : assistant IA totalement absent

Aucune trace dans `src/`. Le § 1.15 du document et les exigences IA-01, IA-02, IA-03, IA-06 sont
sans implémentation.
**Correction** : arbitrage fournisseur (PC-18). À noter que le MCD du document ne prévoit
**aucune table** pour l'IA ; RG-IA-02 (« le fil de conversation reste attaché au ticket ») en
suppose pourtant une.

---

## 28. Non-conformités P2

| ID | Règle | Écart |
|:---|:---|:---|
| P2-01 | RG-SIM-11 | Aucun écran d'administration des référentiels tarifaires ni des paramètres de grille |
| P2-02 | RG-WKF-01 / RG-AVE-01 | Circuit nominal et étapes de workflow codés en dur |
| P2-03 | RG-SEC-03 | Connexion non consignée au journal d'audit |
| P2-04 | RG-IMP-01 | Borne 1–5 BL non appliquée |
| P2-05 | RG-EXP-03 | Étape Draft IN absente |
| P2-06 | RG-SLA-01 | Axe « catégorie de client » absent du paramétrage SLA |
| P2-07 | RG-ENR-02 | Entités `CHAUFFEUR` et `BADGE` absentes |
| P2-08 | RG-WKF-08 | Transferts tracés en texte, pas en table structurée |
| P2-09 | RG-TCK-06 | Archivage à la clôture non démontré |
| P2-10 | § 2.6 | `MENTIONNE` limitée à une mention par message au lieu d'une n-n |
| P2-11 | SIM-09 | Le document « PDF » n'est pas un PDF valide (ISO 32000) |
| P2-12 | § 6.2 | `TYPE_DOCUMENT` non rattachée aux documents déposés |

---

## 29. Améliorations P3

- **P3-01** — RG-CPT-08 : nature de compte `Transporteur` absente. Règle sans ancrage CDC et
  absente de l'annexe du document : à trancher avec le commanditaire avant développement.
- **P3-02** — `STATUT` pleinement paramétrable (ajout d'un statut sans déploiement). Écart
  résiduel déjà assumé et documenté dans le code même.
- **P3-03** — Colonnes calculées `PERSISTED` pour l'indexation des tableaux de bord.
- **P3-04** — Contraintes `CHECK` sur les énumérations fermées, comme le prescrit le § 6.1.
- **P3-05** — `AspNetRoles` présente mais jamais utilisée : code mort à qualifier.

---

## 30. Fonctionnalités totalement absentes

1. Assistant virtuel IA (RG-IA-01, RG-IA-02).
2. Envoi effectif sur e-mail, SMS et WhatsApp (RG-COM-01).
3. Entité `CHAUFFEUR` et type de compte transporteur (RG-CPT-08).
4. Entité `BADGE` — numéro, émission, expiration, statut (RG-ENR-02).
5. Table `TRANSFERT` structurée (§ 6.2 du MPD).
6. Étape Draft IN du parcours export (RG-EXP-03).
7. Écrans d'administration des référentiels et paramètres tarifaires (RG-SIM-11).

---

## 31. Éléments présents mais non utilisés

- `AspNetRoles` / `AspNetUserRoles` — peuplées par Identity, jamais consultées pour autoriser.
- 8 services applicatifs listés en dette connue par `ServicesAtteignablesTests` (code mort
  documenté : `ActionsWorkflowStandardCommand`, `BasculerAlternativePresentielleCommand`,
  `ConsulterFileQuery`, `PriseLibreCommand`, `CreerTicketCommand`, etc.).
- `PiecesExigeesEtape` administrable, mais aucun document déposé n'y est rattaché.

---

## 32. Tests manquants (à écrire, non codés)

| ID | Type | Objet |
|:---|:---|:---|
| T-01 | Sécurité / intégration | Client A → API ticket de client B → 403 |
| T-02 | Sécurité / intégration | **Transporteur sans société → API ticket quelconque → 403** (couvre P0-01) |
| T-03 | Sécurité / intégration | Énumération de numéros de ticket via l'API → aucun résultat hors périmètre |
| T-04 | Audit | Connexion réussie et échouée → entrée au journal d'audit |
| T-05 | Workflow | Annulation refusée au-delà du seuil, garde serveur déclenchée |
| T-06 | Métier | Soumission de 6 BL → refus |
| T-07 | Workflow | Draft IN obligatoire avant édition de facture export |
| T-08 | SLA | SLA différencié par catégorie de client |
| T-09 | Cycle de vie | Archivage automatique constaté à la clôture |
| T-10 | Base de données | Reprise de données de la migration `NormalisationReferentielsTarifaires` |
| T-11 | Concurrence | Deux transferts simultanés sur le même ticket |
| T-12 | Métier | Mention de plusieurs collaborateurs dans une même note |

---

## 33. Plan de remédiation recommandé

| Phase | ID | Exigences | Fichiers | Migration | Tests | Dépend. | Prio | Complexité |
|:---|:---|:---|:---|:---|:---|:---|:---|:---|
| **0 — Critique** | R-00 | RG-CPT-04, RG-SEC-02 | `Web/Api/ApiEndpoints.cs` | non | T-01, T-02, T-03 | — | P0 | Faible |
| **1 — Modèle** | R-11 | § 6.2 `TRANSFERT` | Domain/Files, DbContext | oui | T-11 | — | P2 | Moyenne |
| | R-12 | RG-ENR-02, RG-CPT-08 | `CHAUFFEUR`, `BADGE`, `NatureUtilisateur` | oui | — | Arbitrage P3-01 | P2 | Moyenne |
| | R-13 | § 2.6 `MENTIONNE` | Domain/Messagerie | oui | T-12 | — | P2 | Faible |
| | R-14 | § 6.2 `TYPE_DOCUMENT` | Domain/Documents | oui | — | — | P2 | Faible |
| | R-15 | Reprise de données du lot 3FN | script SQL | — | T-10 | **Avant tout déploiement** | P1 | Moyenne |
| **2 — Socle** | R-21 | RG-TCK-07 | `AnnulerTicketCommand`, `DetailTicket.razor`, référentiel de seuil | oui | T-05 | — | P1 | Moyenne |
| | R-22 | RG-SEC-03 | `Connexion.razor`, `VerifierCode.razor` | non | T-04 | — | P2 | Faible |
| | R-23 | RG-TCK-06 | `MoteurRetentionService` | non | T-09 | — | P2 | Faible |
| **3 — Workflows** | R-31 | RG-WKF-01, RG-AVE-01 | Extraction du circuit en données | oui | — | R-11 | P2 | **Élevée** |
| **4 — Billing** | R-41 | RG-IMP-01 | `SoumettreDemandeImportCommand` + formulaire | non | T-06 | — | P2 | Faible |
| | R-42 | RG-EXP-03 | Étape Draft IN | oui | T-07 | R-14 | P2 | Moyenne |
| **5 — Simulation** | R-51 | RG-SIM-11 | Écrans d'administration référentiels + paramètres | non | — | lot 3FN | P2 | Moyenne |
| | R-52 | SIM-09 | Vrai générateur PDF | non | — | — | P2 | Moyenne |
| **8 — SLA / Notif.** | R-81 | RG-SLA-01 | `ParametreSlaAdministrable` + axe TypeClient | oui | T-08 | — | P2 | Faible |
| | R-82 | RG-COM-01, RG-WKF-10 | Adaptateur fournisseur réel | non | — | **Arbitrage PC-16** | P1 | Moyenne |
| **9 — IA** | R-91 | RG-IA-01, RG-IA-02 | Port + adaptateur + écran + persistance du fil | oui | — | **Arbitrage PC-18** | P1 | Élevée |
| **11 — Tests** | R-111 | Couverture | T-01 à T-12 | non | — | phases 0-9 | P1 | Moyenne |
| **12 — Validation** | R-121 | Re-audit complet | — | — | suite complète | tout | — | Faible |

**Ordre imposé par les dépendances** : R-00 immédiatement (sécurité) · R-15 avant tout déploiement
de la migration du lot 3FN · R-31 après R-11 · R-42 après R-14.

---

## 34. Conclusion

La plateforme est à **87,76 %** de conformité au document Merise. Le socle transverse, le cycle de
vie du ticket, la mesure des temps, le moteur de workflow et le calcul tarifaire sont solides,
testés et — pour la tarification — vérifiés au franc près contre les jeux d'essai de l'Annexe A.8.

Trois choses doivent être traitées avant toute mise en service.

La première est un **défaut de sécurité réel et exploitable** : l'API laisse un compte
transporteur lire les tickets de n'importe quel client. Il ne demande pourtant qu'une correction
de quelques lignes — le service de cloisonnement correct existe déjà et est utilisé partout
ailleurs. C'est l'illustration du risque que fait courir un contrôle réécrit localement plutôt que
réutilisé.

La deuxième est que **deux exigences fonctionnelles entières restent suspendues à un arbitrage
DSI** — assistant IA et canaux de notification externes. Elles ne relèvent pas du développement
mais de la décision.

La troisième est que le **paramétrage « sans développement » reste incomplet** là où le CDC
l'impose : le circuit de workflow et l'administration tarifaire. Le lot 3FN a rendu les données
tarifaires administrables ; il manque encore les écrans pour les administrer.

Enfin, l'audit a relevé une **incohérence dans le document de référence lui-même** (98 règles
définies, 97 tracées, RG-CPT-08 orpheline et sans ancrage CDC). Ce point mérite d'être arbitré
avec le commanditaire avant d'engager le développement correspondant.
