# Audit de conformité actualisé

*Contre-audit complet après fermeture P0-bis — recalcul intégral des 98 règles et reclassification P2/P3.*
*Mode AUDIT / READ ONLY : aucun code modifié, aucune migration, aucun script R-15, aucun P2/P3 traité.*

---

## 1. Résumé exécutif

**P0 est RÉOUVERT.** Une chaîne complète et exploitable a été établie pendant cet audit : la
désactivation d'un compte interne (agent ou administrateur CIT) **n'a aucun effet sur ses
permissions**. Le compte peut se reconnecter, atteindre les écrans d'administration et y agir —
jusqu'à se réactiver lui-même. Ce défaut ne se trouve sur aucune des surfaces corrigées en P0 et
P0-bis : il vit dans le pipeline d'authentification et dans le service d'autorisation, que ni l'une
ni l'autre remédiation n'avait examinés. Il est désigné ici **P0-ter**.

Hors ce point, l'état du dépôt est bon. Les remédiations P0, P0-bis et P1 tiennent, le lot 3FN est
confirmé conforme, la suite complète est verte (1 294 tests). La conformité recalculée depuis zéro
s'établit à **95,9 %** — mais ce chiffre doit être lu avec le P0-ter en tête : il pèse sur trois
règles seulement, alors que sa portée opérationnelle est celle d'un contrôle de sécurité inopérant.

Deux constats de méthode sortent également de cet audit, et tous deux me concernent :

- **Mon propre rapport P0-bis contient un faux positif.** Son § 10 conclut « aucune autre lecture de
  ressource exposée à l'utilisateur n'est dépourvue de décision d'autorisation ». Le balayage n'avait
  pas couvert `ObtenirFicheEnrolementQuery` ni `ObtenirFicheAvoirExonerationQuery` — deux fiches
  complètes, chacune avec sa propre implémentation du contrôle d'accès. Elles se révèlent
  fonctionnellement correctes après vérification, mais la conclusion était trop large pour ce qui
  avait réellement été inspecté.
- **La certification SEC-11 reposait sur un test qui ne démontrait rien.** Le fichier le reconnaît
  désormais lui-même : l'assertion écrivait l'indicateur puis vérifiait qu'il valait ce qu'on venait
  d'y mettre, sans exercer le moindre chemin d'accès. C'est très exactement l'angle mort par lequel
  le P0-ter a survécu à trois audits.

---

## 2. État du dépôt

| Élément | État |
|---|---|
| Branche | `main` |
| Suite complète | **1 294 / 1 294 — 0 échec** (Domain 208 · Application 651 · Infrastructure 139 · Web 296) |
| Build | 0 erreur, 1 avertissement résiduel (`CS9113` sur `AssistantCommands.journalisation`) |
| Remédiation P0 (API `/api/v1`) | Tenue — 12 tests verts |
| Remédiation P0-bis (fiches ticket/dossier) | Tenue — 17 + 9 tests verts |
| Remédiation P1 (01/02/03) | Tenue |
| Lot 3FN | Conforme, inchangé |
| R-15 | Ouvert |
| **P0-ter (nouveau)** | **Ouvert — bloquant** |

Sources relues : le document de référence, `audit-conformite-merise.md`,
`remediation-P0-securite.md`, `remediation-P0-bis-agent-cloisonnement.md`, `remediation-P1.md`,
`contre-audit-P0-P1-3FN.md`.

---

## 3. Méthode

Les 98 règles ont été reconstruites **depuis la section 1 du document de référence**, par extraction
du XML du `.docx` — et non depuis la matrice de 97 lignes de l'annexe. Le décompte par famille
confirme le total : CPT 8 (RG-CPT-08 incluse), TCK 15, MSG 4, WKF 11, TPS 3, SLA 4, ALE 2, DIS 2,
AFF 1, PRI 2, MAS 1, IMP 9, EXP 4, SIM 12, AVE 2, REC 3, ENR 2, COM 3, NOT 2, CON 2, IA 2, SEC 4.

Aucun statut antérieur n'a été réutilisé. Chaque règle a été réévaluée depuis le code.

**Trois profondeurs de vérification ont été appliquées, et il faut les distinguer** — c'est la
condition pour que ce rapport ne reproduise pas les faux positifs qu'il dénonce :

| Profondeur | Ce qui a été fait | Règles concernées |
|---|---|---|
| **A — tracée de bout en bout** | Texte de la règle → implémentation lue → appelants recensés → consommation de l'effet vérifiée → tests lus → tentative de mise en défaut | Les 4 RG-SEC, RG-CPT-04/06/07, RG-TCK-06/07/11, RG-MSG-01/04, RG-WKF-11, RG-SLA-03, RG-SIM-12, RG-IA-01/02, RG-COM-01, RG-ENR-01/02, RG-AVE-01 |
| **B — implémentation lue + point de consommation vérifié** | Implémentation lue, effet consommé quelque part de démontré, tests recensés sans être tous lus | RG-CPT-01/02/03/05/08, RG-TCK-01/12/13/15, RG-ALE-01/02, RG-DIS-01/02, RG-AFF-01, RG-PRI-01/02, RG-MAS-01, RG-TPS-01/03, RG-NOT-01/02, RG-CON-01/02 |
| **C — inventaire + sondage** | Présence de l'implémentation et d'une couverture de test établie, chemin de consommation non retracé individuellement | Le reste : RG-TCK-02/03/04/05/08/09/10/14, RG-MSG-02/03, RG-WKF-01→10, RG-TPS-02, RG-SLA-01/02/04, RG-IMP-01→09, RG-EXP-01→04, RG-SIM-01→11, RG-AVE-02, RG-REC-01→03 |

Les statuts de niveau C sont donnés de bonne foi mais **n'ont pas la même valeur probante** que ceux
de niveau A. Un audit ultérieur portant sur eux pourrait en dégrader certains — c'est exactement ce
qui vient de se produire pour SEC-01 et SEC-02, classées conformes par les passes précédentes.
Cette réserve est reprise en § 13.

Conformément au § 2 de la mission, aucune règle n'est déclarée conforme sur la seule présence d'une
méthode, d'un service, d'un drapeau, d'une classe, d'une table ou d'un test unitaire isolé.

---

## 4. Validation P0

### 4.1 Ce qui tient

| Surface | Deny-by-default | Agent sans habilitation | Agent mauvaise file | Agent désactivé | Compte inconnu | Client non propriétaire | Transporteur | Notes internes |
|---|---|---|---|---|---|---|---|---|
| `ControleAccesRessourceService` | ✅ | ✅ refusé | ✅ refusé | ✅ refusé | ✅ refusé | ✅ refusé | ✅ cloisonné | — |
| `ObtenirFicheTicketQuery` | ✅ délègue | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ agent habilité seul |
| `ObtenirFicheDossierQuery` | ✅ délègue | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | — |
| `IndicateursTemps.razor` | ✅ délègue | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | — |
| `ObtenirFicheEnrolementQuery` | 🟡 propre impl. | ✅ refusé | ✅ file vérifiée | **🔴 non vérifié** | ✅ refusé | ✅ refusé | ✅ propriétaire | — |
| `ObtenirFicheAvoirExonerationQuery` | 🟡 propre impl. | ✅ refusé | 🟡 sans portée file | **🔴 non vérifié** | ✅ refusé | ✅ refusé | — | ✅ sur permission |
| Requêtes tarification | 🟡 `SocieteId` fourni par l'appelant | — | — | 🔴 non vérifié | — | ✅ refusé | — | — |

Les endpoints `/api/v1` (3 routes) délèguent au contrôle central pour ticket et dossier ; la
condition fail-open d'origine (`ctx.SocieteId.HasValue &&`) n'est pas réapparue.

### 4.2 P0-ter — la désactivation d'un compte interne est sans effet

**La divergence attendue au § 4 de la mission est confirmée : « agent désactivé refusé » ne tient
pas hors des surfaces corrigées.** La chaîne est complète et chacun de ses maillons a été lu :

1. **Connexion** — `Components/Pages/Comptes/Connexion.razor:175` :
   ```csharp
   private async Task<bool> EstAccesAutoriseAsync(ApplicationUser utilisateur) =>
       utilisateur.Nature != NatureUtilisateur.Client || await VerifierAcces.AccesAutoriseAsync(utilisateur.Id);
   ```
   Pour un compte **Agent**, la première branche est vraie et court-circuite tout. Le contrôle
   `EstActif` que porte `VerifierAccesCompteQuery` n'est jamais atteint. `EstActif` étant une
   propriété applicative, aucune primitive Identity ne la connaît, et aucun
   `SecurityStampValidator` n'est configuré. **Un agent désactivé obtient donc une session neuve.**

2. **Autorisation** — `Infrastructure/Identity/AutorisationService.cs:22-33` : la décision consulte
   `db.Habilitations` et **rien d'autre**. L'état du compte n'entre pas dans le calcul. Les
   habilitations d'un compte désactivé restent donc pleinement effectives.

3. **Surface** — 19 pages routables sur les 69 qui utilisent le contexte ne vérifient jamais
   `contexte.EstAuthentifie`, dont **les dix pages d'administration**. Elles portent `[Authorize]`,
   que le cookie satisfait.

4. **Effet** — `Comptes/AdministrationComptesAgentsCommands.cs:72` :
   `await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ...)`. Aucun autre
   contrôle. Un administrateur désactivé conservant cette habilitation peut ouvrir
   `/administration/comptes-agents` et **se réactiver lui-même**, ou désactiver des tiers.

**Ce qui atténue, et qu'il faut dire honnêtement :** `ObtenirContexteUtilisateurQuery:22` refuse bien
un compte inactif et renvoie un contexte mort (`EstAuthentifie: false`, aucune permission, aucune
file). Les 50 pages qui consultent cette valeur masquent donc leurs actions, et la fiche Enrôlement
est protégée par ce biais malgré l'absence de contrôle dans sa requête. **La protection réelle vit
donc dans l'interface, pas dans la couche d'autorisation** — et là où l'interface ne la porte pas,
il n'y a rien.

**Pourquoi ce défaut a traversé trois audits.** `SecCert05DesactivationTests` certifiait SEC-11.
Son assertion écrivait `agent.EstActif = false` puis vérifiait `Assert.False(agent.EstActif)` :
aucun chemin d'accès n'était exercé. Le fichier porte aujourd'hui ce constat en commentaire et
renvoie vers `AdministrationComptesAgentsTests.Un_compte_desactive_perd_effectivement_tout_acces` —
lequel éprouve `ObtenirContexteUtilisateurQuery`, donc **le seul maillon qui fonctionne**, et aucun
des trois qui ne fonctionnent pas.

**Règles impactées :** RG-SEC-01 (🔴), RG-SEC-02 (🟡). **Priorité : P0.**

---

## 5. Validation P1

### 5.1 P1-01 — état engageant (RG-TCK-07, 10, 11, 12)

`Ticket.MarquerEtatEngageant()` compte désormais **six appelants en production**, et ils
correspondent bien aux événements métier engageants au sens de RG-TCK-11 (« paiement confirmé,
document officiel produit ») :

| Appelant | Événement | Correspondance CDC |
|---|---|---|
| `BillingImport/EditerFactureCommand:59` | Facture éditée | document officiel |
| `BillingExport/EditerFactureExportCommand:63` | Facture éditée | document officiel |
| `BillingImport/ValiderPreuvePaiementCommand:33` | Preuve de paiement validée | paiement confirmé |
| `BillingExport/ValiderPreuvePaiementExportCommand:32` | idem EXPORT | paiement confirmé |
| `BillingImport/ConfirmerLettrageCommand:41` | Lettrage confirmé | paiement confirmé |
| `BillingExport/ConfirmerLettrageExportCommand:32` | idem EXPORT | paiement confirmé |

Le drapeau est consommé en trois points : `AnnulerTicketCommand:101` (RG-TCK-07),
`Ticket.cs:461` `PeutSuggererCloture` (RG-TCK-11), `ObtenirFicheTicketQuery:141` `peutAnnuler`. Il
est persisté (colonne présente depuis la migration `20260829064258_AjoutCycleDeVieTicket`) et posé
avant le transfert et l'enregistrement. **Le comportement de production est correct.**

**Mais le câblage n'est protégé par aucun test.** Aucun test de la solution n'assert
`EstEnEtatEngageant` après exécution de l'une des six commandes : les deux seuls tests qui
manipulent le drapeau (`AnnulationSeuilBloquantTests`, `RelancesAbsenceReponseTests`) l'appellent
**à la main**, exactement comme le faisaient les tests qui n'avaient pas vu que la méthode n'avait
aucun appelant. **Supprimer les six appels laisserait la suite entièrement verte.**

Conformément au § 5 de la mission, les règles sont classées selon le comportement réel (✅), et le
test manquant est inscrit comme risque prioritaire (§ 13, **P2-04**).

### 5.2 P1-02 — notifications (RG-COM-01/02/03, RG-NOT-01/02)

| Aspect | État | Preuve |
|---|---|---|
| Interpolation des gabarits | ✅ | `GabaritNotification.Interpoler` appelé en production, `DistribuerNotificationsUseCase:183-185` |
| Lien vers le ticket | ✅ | `LienPortailConfigure` (`ILienPortail`), `TicketId` porté par chaque notification |
| Repli si coordonnée absente | ✅ | `CanalJournalise:33` — traité comme échec, `ReessayerEnvoisNotificationUseCase:98` |
| Portail (InApp) | ✅ | Canal réel : l'existence de la notification **est** sa diffusion |
| Lu / non lu | ✅ | `EstLue`, `CentreNotificationsQueries:38` |
| Paramétrage événement × canal | ✅ | `ModeleNotification.Creer(typeEvenement, canal, …)` |
| E-mail | 🟡 | `CanalJournalise` — adaptateur **journalisant**, pas d'envoi réel |
| SMS | 🟡 | idem |
| WhatsApp | 🟡 | idem |
| Sécurité des liens (RG-SEC-04) | ✅ | Le lien conduit au ticket ; `TelechargerDocumentQuery` exige l'autorisation et audite |

**Distinction demandée au § 6.** L'implémentation **technique** est complète et correctement câblée :
port `ICanalNotification`, trois canaux enregistrés en DI, résolution des destinataires, file de
reprise. L'**activation métier** relève de PC-16, explicitement documenté comme « bloque :
PRODUCTION » et réservé à la DSI/Finance. Les trois canaux externes sont donc servis par un
adaptateur journalisant assumé, non par un défaut.

Conséquence sur le statut : RG-COM-01 exige « e-mail, notification portail, **et au moins un canal
mobile** ». Le portail est réel, les trois autres sont simulés. La règle est donc **🟡**, non ✅ :
le service rendu au client ne comporte aujourd'hui aucun canal mobile effectif. RG-COM-02, RG-COM-03,
RG-NOT-01 et RG-NOT-02 restent ✅ — le paramétrage, le fil de commentaires et le centre de
notifications fonctionnent réellement.

### 5.3 P1-03 — assistant virtuel (RG-IA-01, RG-IA-02)

Les trois plans demandés au § 7 se distinguent nettement :

| Plan | État |
|---|---|
| **Architecture** | ✅ Port `IAssistantConversationnel`, adaptateur substituable, conversation persistée (`ConversationAssistant`, migration `20260905080609_AjoutAssistantVirtuel`), page `AssistantVirtuel.razor` |
| **Adaptateur** | `AssistantSimule` — moteur à mots-clés : `mots.Any(m => normalisee.Contains(m, StringComparison.Ordinal))` (`AssistantSimule.cs:83`) |
| **Capacité fonctionnelle réelle** | Reconnaissance de mots-clés et réponses préparées |

RG-IA-01 exige que l'assistant « répond[e] en français aux questions de premier niveau, oriente le
client dans ses démarches et **l'aide à compléter ses dossiers** ». Un appariement de sous-chaînes
ne rend pas ce service. Le statut doit refléter le service rendu : **🟡**. Ce n'est pas un défaut de
réalisation — l'adaptateur simulé a été explicitement retenu en arbitrage — mais l'arbitrage ne
change pas ce que le client obtient.

RG-IA-02 (escalade sans perte de contexte) est en revanche **✅** : `EscaladerAssistantVersOperateurCommand`
existe, est enregistrée en DI, est appelée par `AssistantVirtuel.razor:202` et couverte par
`AssistantApplicationTests`. Le fil est transmis comme message client — le transcript étant les
propres mots du client, il n'a rien d'interne.

---

## 6. Validation lot 3FN

Les conclusions du contre-audit précédent sont **confirmées, sans réouverture artificielle**. Rien
n'a changé dans ce lot depuis, et les vérifications de contrôle repassent :

| Objet | État |
|---|---|
| MCD / MLD / MPD | Conformes — référentiels tarifaires éclatés en entités propres |
| Normalisation | 3FN atteinte : `SensOperation`, `TailleConteneur`, `TypeConteneur`, `CategorieMarchandise` sortis des lignes tarifaires |
| Clés étrangères | Posées ; la navigation calculée `TranchesOrdonnees` est ignorée (`b.Ignore`), ce qui a supprimé la FK fantôme `VersionGrilleTarifaireId1` |
| Index / contraintes | En place sur les clés de grille et la clé tarifaire Sens+Taille+Catégorie |
| Référentiels | `ParametrageInitialAnnexeA` — paramétrage administrable, sans développement |
| Simulation | Montants figés à la création (`MontantTotalXof`, `LignesConteneurs`) |
| Snapshot | Cohérent avec les migrations |
| Versionnement | `VersionGrilleTarifaire` avec date d'effet ; `SimulationProforma.VersionGrilleTarifaireId` |
| Golden masters | `GoldenMasterProformaTests`, `AuditPreuveArrondiFrancTests` — exactitude au franc préservée |

**Verdict : CONFORME.**

---

## 7. Matrice des 98 règles

Statuts : ✅ conforme · 🟡 partiel · 🔴 non conforme · ⚫ non implémenté · 🔵 N/A · ⚠️ non vérifiable

| RG | Domaine | Statut | Preuve code | Preuve test | Écart | Priorité |
|---|---|---|---|---|---|---|
| RG-CPT-01 | Comptes | ✅ | `SoumettreDemandeCompteCommand`, `ValiderDemandeCompteCommand`, `VerifierAccesCompteQuery` (demande non tranchée ⇒ refus) | Suites Comptes | — | — |
| RG-CPT-02 | Comptes | ✅ | `TypeClient` (4 valeurs), fixé par `SocieteCliente.Soumettre` | Suites Comptes | — | — |
| RG-CPT-03 | Comptes | ✅ | Orientation par `TypeClient` après contrôle BL | `CircuitClient{Normal,EnCompte,Deposit,Ies}Tests` | — | — |
| RG-CPT-04 | Comptes | ✅ | `ControleAccesRessourceService.ClientEstAdmisAsync` | `CloisonnementAgentFichesTests`, `ApiCloisonnementTests` | — | — |
| RG-CPT-05 | Comptes | ✅ | `ModifierTypeClientCommand:28` audit ; `Dossier.ClientId` jamais réécrit | Suites Comptes | — | — |
| RG-CPT-06 | Comptes | ✅ | `ActiverDesactiverUtilisateurCommand` → `UtilisateurSociete.EstActif`, honoré par `ControleAccesRessourceService:202` et `VerifierAccesCompteQuery:53` | `AdministrationComptesAgentsTests` | Côté **société** uniquement ; la désactivation des comptes **internes** relève de P0-ter | — |
| RG-CPT-07 | Comptes | ✅ | `DroitConsultation` + `VisibiliteDemandesFiltre` (`ControleAccesRessourceService:213`) | `CloisonnementAgentFichesTests` | — | — |
| RG-CPT-08 | Comptes | 🟡 | `InscrireTransporteurCommand`, module Enrôlement dédié | `EnrolementSecuriteTests` | `NatureUtilisateur` ne comporte que `Client`/`Agent` : le transporteur n'est pas un **type distinct**, il est un client sans société | P3 |
| RG-TCK-01 | Ticket | ✅ | `CreerTicketCommand` + service de numérotation | Suites Tickets | — | — |
| RG-TCK-02 | Ticket | ✅ | `ObtenirFicheTicketQuery` agrège historique, documents, messages, décisions | `FicheTicketTests` | Niveau C | — |
| RG-TCK-03 | Ticket | ✅ | Workflow par module + `TypeClient` | Suites Circuit* | Niveau C | — |
| RG-TCK-04 | Ticket | ✅ | `Dossier` regroupe les tickets d'une soumission multi-BL | `SoumettreDemandeImportCommand` tests | Niveau C | — |
| RG-TCK-05 | Ticket | ✅ | `LierTicketsCommand`, `ILiaisonTicketRepository` | Suites Tickets | Niveau C | — |
| RG-TCK-06 | Ticket | 🟡 | `CloturerTicketCommand:36` → `ticket.Cloturer(...)` ; consultation post-clôture préservée | 52 fichiers de test touchent la clôture | **L'archivage automatique n'est pas matérialisé** : `DateArchivageUtc` n'est écrit que par `MarquerAnonymise`, appelé par le moteur de rétention, jamais à la clôture | P3 |
| RG-TCK-07 | Ticket | ✅ | `AnnulerTicketCommand:101` lit `EstEnEtatEngageant` ; seuil paramétrable (`GererEtapesBloquantesAnnulationCommand`) | `AnnulationSeuilBloquantTests` | Câblage non protégé par test → P2-04 | — |
| RG-TCK-08 | Ticket | ✅ | Demande d'annulation transmise à la file habilitée | Suites Tickets | Niveau C | — |
| RG-TCK-09 | Ticket | ✅ | Motif obligatoire, horodatage, historique | Suites Tickets | Niveau C | — |
| RG-TCK-10 | Ticket | ✅ | `RelancesAbsenceReponseUseCase`, `NombreRelancesEnvoyees` | `RelancesAbsenceReponseTests` | Câblage engageant → P2-04 | — |
| RG-TCK-11 | Ticket | ✅ | `Ticket.cs:461` `PeutSuggererCloture` exclut l'état engageant ; 6 appelants de `MarquerEtatEngageant` | `RelancesAbsenceReponseTests` (drapeau posé à la main) | **Aucun test ne tomberait si les 6 appels étaient retirés** | P2-04 |
| RG-TCK-12 | Ticket | ✅ | `ReouvrirTicketCommand` + DI + motif tracé | Suites Tickets | — | — |
| RG-TCK-13 | Ticket | ✅ | Contrôle d'unicité BL à la création IMPORT | Suites unicité (12 fichiers) | — | — |
| RG-TCK-14 | Ticket | ✅ | Unicité appliquée BL par BL, multi-BL préservé | idem | Niveau C | — |
| RG-TCK-15 | Ticket | ✅ | `BookingDejaActifException`, `SoumettreDemandeExportCommand:110` | Suites Export | Règle conditionnelle (« pourra s'appliquer ») — implémentée | — |
| RG-MSG-01 | Échanges | ✅ | `TypeMessage.NoteInterne` ; `ListerVisiblesParClientAsync` vs `ListerParTicketAsync` | `CloisonnementAgentFichesTests` Cas09/09bis/09ter | — | — |
| RG-MSG-02 | Échanges | ✅ | `EstInterne` porté jusqu'à la vue ; horodatage et historisation | Suites Messagerie | Niveau C | — |
| RG-MSG-03 | Échanges | ✅ | `MentionnerCollaborateurCommand:38` publie l'événement de notification | Suites Messagerie | Niveau C | — |
| RG-MSG-04 | Échanges | ✅ | `MentionnerCollaborateurCommand:29-43` — `OperateurAssigneId` capturé puis vérifié inchangé | Suites Messagerie | — | — |
| RG-WKF-01 | Workflow | ✅ | Circuit déterminé par module + type de demande + profil | Suites Circuit* | Niveau C | — |
| RG-WKF-02 | Workflow | ✅ | `DerogationWorkflowService`, habilitations paramétrées | Suites Tickets | Niveau C | — |
| RG-WKF-03 | Workflow | ✅ | Dérogation tracée historique + journal d'audit | Suites Tickets | Niveau C | — |
| RG-WKF-04 | Workflow | ✅ | État « en attente de prise en charge » à l'entrée en file | Suites Tickets | Niveau C | — |
| RG-WKF-05 | Workflow | ✅ | `PrendreEnChargeCommand` + `AutorisationService.ExigerAsync` scopé file | Suites Tickets | Habilitation vérifiée, **état du compte non** (P0-ter) | — |
| RG-WKF-06 | Workflow | ✅ | Horodatages : arrivée, prise en charge, opérateur, dernière activité | `RecSce12TempsSlaTests` | Niveau C | — |
| RG-WKF-07 | Workflow | ✅ | `TransfererTicketCommand` — pas de recréation côté client | `RecSce10TransfertTests` | — | — |
| RG-WKF-08 | Workflow | ✅ | `TransfererTicketCommand:36` motif obligatoire ; origine, destination, date tracées | `RecSce10TransfertTests` | — | — |
| RG-WKF-09 | Workflow | ✅ | `ReaffecterTicketCommand` — non réservée au superviseur | Suites Tickets | Niveau C | — |
| RG-WKF-10 | Workflow | ✅ | Événement `TicketTransfere` → `ResolveurClientDuDossier:35` | Suites Notifications | Canal mobile simulé (cf. RG-COM-01) | — |
| RG-WKF-11 | Workflow | ✅ | `TransfererTicketCommand:59-62` → `TransitionNonAutoriseeException` | `MatriceTransitionsTests`, `RecSce10TransfertTests` | — | — |
| RG-TPS-01 | Temps | ✅ | `MesureTemps.Calculer` — attente et traitement | `RecSce12TempsSlaTests` | — | — |
| RG-TPS-02 | Temps | ✅ | `PeriodeAttenteClient` mesurée séparément, jamais imputée | `SurveillerEcheancesCalendrierTests` | Niveau C | — |
| RG-TPS-03 | Temps | ✅ | `IndicateursTemps.razor:113` — total depuis la **création**, non depuis l'arrivée en file | `RecSce12TempsSlaTests` | — | — |
| RG-SLA-01 | SLA | ✅ | `GererParametresSlaCommand` — module, type, étape, file, priorité, catégorie | Suites Supervision | Niveau C | — |
| RG-SLA-02 | SLA | ✅ | `StatutSla` : information, alerte, dépassement, escalade | `SurveillerEcheancesUseCase:75-78` | Niveau C | — |
| RG-SLA-03 | SLA | ✅ | `SurveillerEcheancesUseCase:65` — `dureeEffective = dureeTotaleOuvree − dureeAttenteClienteOuvree` | `RecSce12TempsSlaTests:57` (`Assert.Null` sur l'alerte), `SurveillerEcheancesCalendrierTests` | — | — |
| RG-SLA-04 | SLA | ✅ | Seuils et durées administrables par écran | Suites Administration | Niveau C | — |
| RG-ALE-01 | Alertes | ✅ | `TypeAlerte` : `NonPriseEnCharge`, `SansActivite`, `BacklogFile`, `VolumeAnormal` | Suites Supervision | — | — |
| RG-ALE-02 | Alertes | ✅ | `SurveillerEcheancesUseCase:94-96` `AppliquerEscaladeAsync` + `IRegleEscaladeRepository` | Suites Supervision | — | — |
| RG-DIS-01 | Disponibilité | ✅ | `StrategiesAffectation.cs:18` — candidats filtrés sur `StatutDisponibilite.Disponible` | Suites Affectation | — | — |
| RG-DIS-02 | Disponibilité | ✅ | `GererDisponibiliteCommand`, alerte `OperateurIndisponibleAvecTickets`, `ReaffecterTicketCommand` | Suites Affectation | — | — |
| RG-AFF-01 | Affectation | ✅ | `DistributionAutomatiqueUseCase:66` — stratégies dont `RoundRobinStrategy` ; `GererCompetencesOperateurCommand` | Suites Affectation | — | — |
| RG-PRI-01 | Priorité | ✅ | `ModifierPrioriteCommand`, référentiel paramétrable | Suites Tickets | — | — |
| RG-PRI-02 | Priorité | ✅ | `ModifierPrioriteCommand:9` — PRI-04, le client ne s'auto-déclare jamais urgent | Suites Tickets | — | — |
| RG-MAS-01 | Actions de masse | ✅ | `ActionDeMasseCommand` + DI | Suites Supervision | Niveau B — trace ticket par ticket implémentée | — |
| RG-IMP-01 | IMPORT | ✅ | 1 à 5 BL, un ticket indépendant par BL | Suites BillingImport | Niveau C | — |
| RG-IMP-02 | IMPORT | ✅ | File Pilote Manifeste ; Validé / Rejeté (motif) / Complément | Suites BillingImport | Niveau C | — |
| RG-IMP-03 | IMPORT | ✅ | Retour en file après dépôt du complément | Suites BillingImport | Niveau C | — |
| RG-IMP-04 | IMPORT | ✅ | Orientation automatique par `TypeClient` | Suites Circuit* | Niveau C | — |
| RG-IMP-05 | IMPORT | ✅ | Circuit client normal | `CircuitClientNormalTests` | Niveau C | — |
| RG-IMP-06 | IMPORT | ✅ | Circuit client en compte | `CircuitClientEnCompteTests` | Niveau C | — |
| RG-IMP-07 | IMPORT | ✅ | Circuit client deposit, lettrage | `CircuitClientDepositTests`, `ConfirmerLettrageCommand` | Niveau C | — |
| RG-IMP-08 | IMPORT | ✅ | Traitement parallèle IES ; `ConsulterSuivisBranchesIesQuery:28` et `ConstaterJointureIesEtFinaliserCommand:49` exigent une permission de branche | `CircuitClientIesTests` | Portée **branche**, non **champ** (cf. RG-SEC-01) | — |
| RG-IMP-09 | IMPORT | ✅ | Bascule présentiel via `Guichet` / `PrendreRendezVous` | Suites Guichet | Niveau C | — |
| RG-EXP-01 | EXPORT | ✅ | Un ticket par booking | Suites BillingExport | Niveau C | — |
| RG-EXP-02 | EXPORT | ✅ | File Back Office ; Codifié / Rejeté | `InstructionBackOfficeTests` | Niveau C | — |
| RG-EXP-03 | EXPORT | ✅ | Variantes par type de client, adaptations EXPORT | Suites Circuit*Export | Niveau C | — |
| RG-EXP-04 | EXPORT | ✅ | IES export — facture et BEE côté plateforme externe | `CircuitClientIesExportTests` | Niveau C | — |
| RG-SIM-01 | Simulation | ✅ | `CalculerSimulationProformaQuery` — saisie dossier + conteneurs | `CalculerSimulationProformaQueryTests` | Niveau C | — |
| RG-SIM-02 | Simulation | ✅ | Clé Sens + Taille + Catégorie, `LigneTarifaire` | `GoldenMasterProformaTests` | — | — |
| RG-SIM-03 | Simulation | ✅ | `MoteurCalculProforma` — postes par conteneur | `GoldenMasterProformaTests` | — | — |
| RG-SIM-04 | Simulation | ✅ | Durée de séjour bornes incluses | `GoldenMasterProformaTests` | Niveau C | — |
| RG-SIM-05 | Simulation | ✅ | Branchement Reefer, franchise 2 j paramétrable | `GoldenMasterProformaTests` | Niveau C | — |
| RG-SIM-06 | Simulation | ✅ | `SeuilPoidsMajoration` par taille, 30 % par défaut | `GoldenMasterProformaTests` | Niveau C | — |
| RG-SIM-07 | Simulation | ✅ | Majorations dangereux / hors gabarit, `ParametreMajoration` | `GoldenMasterProformaTests` | Niveau C | — |
| RG-SIM-08 | Simulation | ✅ | `TrancheStationnement`, `FranchiseStationnement` | `GoldenMasterProformaTests` | Niveau C | — |
| RG-SIM-09 | Simulation | ✅ | `QuotePartBeneficiaire` — CIT/OIC/PAA paramétrables | `GoldenMasterProformaTests` | Niveau C | — |
| RG-SIM-10 | Simulation | ✅ | Total conteneur = somme des postes | `GoldenMasterProformaTests`, `AuditPreuveArrondiFrancTests` | — | — |
| RG-SIM-11 | Simulation | ✅ | `ParametrageInitialAnnexeA` + écrans d'administration tarifaire | `AdministrationTarifaireAutorisationTests` | Niveau C | — |
| RG-SIM-12 | Simulation | ✅ | Version + date d'effet ; `SimulationProforma.VersionGrilleTarifaireId`, `MontantTotalXof` figés ; `ConsulterSimulationProformaQuery:62,96` **relit** sans recalculer | `ConsulterSimulationProformaQueryTests` (scénario V1→S1→V2→consultation S1) | Le test assert la **version** mais **jamais les montants** | P2-05 |
| RG-AVE-01 | Avoir/Exo | ✅ | `DemandeAvoirExoQueries` + workflow propre, justificatifs | `FicheAvoirExonerationTests`, `NouvelleDemandeAvoirExoTests` | — | — |
| RG-AVE-02 | Avoir/Exo | ✅ | Décision et documents déposés, client notifié | Suites AvoirExoneration | Niveau C | — |
| RG-REC-01 | Réclamation | ✅ | `ICategorieReclamationRepository`, `AdministrationCategories.razor` | `MesReclamationsTests`, `NouvelleReclamationTests` | Niveau C | — |
| RG-REC-02 | Réclamation | ✅ | Orientation Support Opérationnel, instruction, compléments | Suites CustomerComplaint | Niveau C | — |
| RG-REC-03 | Réclamation | ✅ | `CloturerReclamationCommand` — réponse formalisée obligatoire | Suites CustomerComplaint | Niveau C | — |
| RG-ENR-01 | Enrôlement | ✅ | `SoumettreEnrolementCommand`, `IParametresEnrolementProvider` (liste paramétrable) | `SoumettreEnrolementCommandTests`, `CycleCompletEnrolementIntegrationTests` | — | — |
| RG-ENR-02 | Enrôlement | ✅ | `DeclencherConvocationCommand`, `ConstaterRemiseBadge` — convocation tracée | `CycleCompletEnrolementIntegrationTests` | — | — |
| RG-COM-01 | Communication | 🟡 | 3 canaux enregistrés en DI ; `ILienPortail` fournit le lien direct | Suites Notifications | **Aucun canal mobile effectif** : e-mail, SMS et WhatsApp sont servis par `CanalJournalise` (journalisation). Portail réel. Activation = PC-16 | P3 |
| RG-COM-02 | Communication | ✅ | `ModeleNotification.Creer(typeEvenement, canal, …)` — paramétrable par module, étape, canal | Suites Notifications | — | — |
| RG-COM-03 | Communication | ✅ | Fil de commentaires = canal de référence (`PublierMessageCommand`) | Suites Messagerie | — | — |
| RG-NOT-01 | Notifications | ✅ | `EstLue` + `TicketId` (`CentreNotificationsQueries:38`) | Suites Notifications | — | — |
| RG-NOT-02 | Notifications | ✅ | Règles de diffusion par événement et par canal (`ModeleNotification`) | Suites Notifications | — | — |
| RG-CON-01 | Continuité | ✅ | Numéro de ticket conservé, parcours poursuivi au guichet | Suites Guichet | Niveau B | — |
| RG-CON-02 | Continuité | ✅ | `RendezVousUseCases`, `PrendreRendezVous.razor`, `AgendaGuichet` | Suites Guichet | Niveau B | — |
| RG-IA-01 | Assistant | 🟡 | `IAssistantConversationnel` + `AssistantSimule` | `AssistantApplicationTests` | Moteur à **mots-clés** (`AssistantSimule:83`) — n'oriente ni n'aide à compléter un dossier au sens de la règle | P3 |
| RG-IA-02 | Assistant | ✅ | `EscaladerAssistantVersOperateurCommand`, appelée par `AssistantVirtuel.razor:202` | `AssistantApplicationTests` | — | — |
| RG-SEC-01 | Sécurité | 🔴 | RBAC par codes de permission (`AutorisationService`) ; authentification Identity | `AdministrationComptesAgentsTests` (contexte seul) | **Moindre privilège violé** : un compte interne désactivé se reconnecte (`Connexion.razor:175`) et conserve toutes ses permissions (`AutorisationService:22-33`). Aucun contrôle **au niveau des champs** | **P0** |
| RG-SEC-02 | Sécurité | 🟡 | `ControleAccesRessourceService` — client et agent cloisonnés en **lecture** | `CloisonnementAgentFichesTests`, `ApiCloisonnementTests` | Le cloisonnement en lecture tient ; un agent désactivé conserve en revanche ses **actions** (P0-ter). 5 implémentations distinctes de la décision d'accès subsistent | **P0** |
| RG-SEC-03 | Sécurité | 🟡 | `JournalAuditAppendOnlyGuard` (inviolabilité) ; `ConsignerAuditAsync` sur création, prise en charge, statut, dépôt, décision, transfert, réaffectation, **téléchargement** (`TelechargerDocumentQuery:55`) | Suites Audit | La règle énumère explicitement la **connexion** : aucun événement d'audit n'est produit à l'authentification | P2-03 |
| RG-SEC-04 | Sécurité | ✅ | `LienPortailConfigure` — le lien conduit au ticket ; `TelechargerDocumentQuery` exige l'autorisation puis audite | Suites Documents | — | — |

---

## 8. Score global

| Statut | Nombre | Pondération | Points |
|---|---|---|---|
| ✅ Conforme | 91 | × 1 | 91 |
| 🟡 Partiellement conforme | 6 | × 0,5 | 3 |
| 🔴 Non conforme | 1 | × 0 | 0 |
| ⚫ Non implémenté | 0 | × 0 | 0 |
| 🔵 N/A (exclu) | 0 | — | — |
| ⚠️ Non vérifiable | 0 | — | — |
| **Total** | **98** | | **94** |

**Conformité globale : 94 / 98 = 95,9 %**

**Ce chiffre ne doit pas être lu seul.** Le P0-ter ne pèse que sur trois règles, alors qu'il rend
inopérant un contrôle de sécurité entier. Un score de conformité par règle mesure la couverture
fonctionnelle, jamais la gravité. La décision de mise en production se prend sur le § 14, pas sur ce
pourcentage.

---

## 9. Scores par domaine

| Domaine | Règles | Points | Score | Commentaire |
|---|---|---|---|---|
| CPT — Comptes et profils | 8 | 7,5 | **93,8 %** | RG-CPT-08 : transporteur non distinct |
| TCK — Ticket et cycle de vie | 15 | 14,5 | **96,7 %** | RG-TCK-06 : archivage non matérialisé |
| MSG — Échanges et notes internes | 4 | 4 | **100 %** | |
| WKF — Workflow, files, transferts | 11 | 11 | **100 %** | |
| TPS — Mesure des temps | 3 | 3 | **100 %** | |
| SLA — Niveaux de service | 4 | 4 | **100 %** | Suspension attente client démontrée |
| ALE — Alertes | 2 | 2 | **100 %** | |
| DIS — Disponibilité | 2 | 2 | **100 %** | |
| AFF — Affectation | 1 | 1 | **100 %** | |
| PRI — Priorité | 2 | 2 | **100 %** | |
| MAS — Actions de masse | 1 | 1 | **100 %** | |
| IMP — Billing IMPORT | 9 | 9 | **100 %** | |
| EXP — Billing EXPORT | 4 | 4 | **100 %** | |
| SIM — Simulation Proforma | 12 | 12 | **100 %** | Lot 3FN confirmé |
| AVE — Avoir et Exonération | 2 | 2 | **100 %** | |
| REC — Customer Complaint | 3 | 3 | **100 %** | |
| ENR — Enrôlement | 2 | 2 | **100 %** | |
| COM — Communication | 3 | 2,5 | **83,3 %** | Canaux externes simulés (PC-16) |
| NOT — Notifications | 2 | 2 | **100 %** | |
| CON — Continuité | 2 | 2 | **100 %** | |
| IA — Assistant virtuel | 2 | 1,5 | **75 %** | Moteur à mots-clés |
| **SEC — Sécurité et conformité** | **4** | **2** | **50 %** | **P0-ter — le domaine le plus dégradé** |

Le classement est net : la sécurité est le seul domaine sous 75 %, et c'est celui qui décide.

---

## 10. Faux positifs de l'audit initial

| # | Règle / affirmation | Biais à l'origine de l'erreur |
|---|---|---|
| FP-1 | **RG-TCK-11 déclarée ✅** | *Méthode existante mais jamais appelée.* `MarquerEtatEngageant()` était lue, testée unitairement, et n'avait aucun appelant en production. L'audit a conclu sur la présence de la méthode et de son test, sans jamais rechercher ses appelants. |
| FP-2 | **Fiches ticket/dossier « correctement gardées » (rapport P0)** | *Garde présente mais branche incomplète.* Je n'avais lu que la branche `if (estClient)`, qui contenait bien une `AccesRefuseException`, et j'ai conclu que la garde existait. Aucune lecture de ce que faisait le code quand la condition était fausse. |
| FP-3 | **SEC-11 / désactivation certifiée conforme** | *Test qui ne teste rien.* L'assertion écrivait `agent.EstActif = false` puis vérifiait `Assert.False(agent.EstActif)`. Un test tautologique passe toujours ; il a donné à la règle une apparence de couverture. C'est le vecteur direct du P0-ter. |
| FP-4 | **RG-SEC-01 / RG-SEC-02 déclarées conformes aux passes précédentes** | *Cloisonnement confondu avec autorisation.* Les passes ont vérifié le cloisonnement en **lecture** (qui voit quoi) et en ont déduit le contrôle d'**accès** au sens large, sans examiner l'état du compte ni le pipeline de connexion. |
| FP-5 | **« Aucune autre lecture sans décision d'autorisation » (mon rapport P0-bis, § 10)** | *Conclusion plus large que le balayage.* Le balayage portait sur `ObtenirFiche*`/`Consulter*`/accès DbContext direct dans `Application` et `Web`. `ObtenirFicheEnrolementQuery` et `ObtenirFicheAvoirExonerationQuery` — cette dernière logée dans un fichier nommé `DemandeAvoirExoQueries.cs` — n'ont pas été atteintes, et la conclusion a été énoncée comme exhaustive. |
| FP-6 | **R-15 qualifié de « perte de données »** | *Extrapolation du symptôme.* La migration **échoue** à la création des FK ; elle ne corrompt rien. La qualification exagérait le risque tout en désignant mal la remédiation. |

---

## 11. Faux négatifs de l'audit initial

| # | Élément | Réalité |
|---|---|---|
| FN-1 | « L'escalade IA du § 4.8 manque » | Le § 4.8 décrit le cycle de vie **transverse** du ticket, pas l'escalade de l'assistant. Écart inventé par mauvaise lecture du document. |
| FN-2 | `ObtenirFicheAvoirExonerationQuery` suspectée à la lecture de `if (!estAgent && …)` | Forme identique au défaut P0-bis, **mais** `estAgent` y est une véritable vérification de permission (`EstAutoriseAsync(…, AvoirExonerationInstruire)`), non la nature de l'utilisateur. La règle est correcte, et les notes internes y sont filtrées sur la permission — plus rigoureux que la fiche ticket, qui les filtre sur la nature. |
| FN-3 | Fiche Enrôlement supposée non gardée en pratique | La requête ne contrôle pas `EstActif`, mais `FicheEnrolement.razor:190` refuse un contexte non authentifié, ce que renvoie `ObtenirContexteUtilisateurQuery` pour un compte désactivé. Le risque est architectural, non exploitable en l'état. |

**Règles dont le statut a changé grâce aux remédiations :** RG-CPT-04, RG-CPT-07, RG-MSG-01,
RG-TCK-07, RG-TCK-10, RG-TCK-11 (P0/P0-bis/P1-01), RG-COM-02, RG-NOT-01, RG-NOT-02 (P1-02),
RG-IA-02 (P1-03).

**Règles dont le statut a été corrigé grâce aux tests de câblage :** RG-CPT-04 et RG-SEC-02 — les
neuf tests de `CablageControleAccesTests` sont aujourd'hui le seul mécanisme qui empêche la
réintroduction silencieuse de la garde réservée à la branche client.

---

## 12. Tests de câblage

**Existants et validés par mutation :**

| Aspect | Test | Mutation vérifiée |
|---|---|---|
| Cloisonnement agent / client / transporteur | `CablageControleAccesTests` (9 cas) | Retrait de `ExigerConsultationTicketAsync` ⇒ 2 tests de câblage + 8 fonctionnels tombent |
| Fail-closed du contrôle central | `Le_controle_central_reste_fail_closed` | Les 4 refus par défaut sont assertés textuellement |
| Non-retour du fail-open API | `Les_endpoints_api_delèguent_au_controle_central` | `ctx.SocieteId.HasValue &&` interdit de réapparaître |
| Suspension SLA | `RecSce12TempsSlaTests:57` (`Assert.Null`) | Retirer la soustraction de l'attente client ⇒ l'alerte se déclenche ⇒ le test tombe |
| Transitions workflow | `MatriceTransitionsTests`, `RecSce10TransfertTests` | `TransitionNonAutoriseeException` assertée |
| Notes internes | `CloisonnementAgentFichesTests` Cas09 / 09bis / 09ter | Les trois faces de la règle |
| Reproductibilité tarifaire (version) | `ConsulterSimulationProformaQueryTests` | Scénario V1→S1→V2→S1 |

**Manquants — priorité forte, listés au § 3 de la mission :**

| Aspect | Situation | Conséquence |
|---|---|---|
| **État engageant** | Aucun test n'assert `EstEnEtatEngageant` après exécution d'une des 6 commandes | Supprimer les 6 appels laisse la suite verte. C'est le défaut P1-01 à l'identique, prêt à revenir |
| **Reproductibilité tarifaire (montants)** | La version est assertée, les montants ne le sont jamais | Un recalcul introduit à la consultation ne serait détecté par aucun test |
| **Désactivation de compte** | Aucun test n'exerce la connexion ni `AutorisationService` avec un compte inactif | C'est ce trou qui a laissé passer le P0-ter |
| **Archivage** | Aucun test d'archivage à la clôture (il n'y a rien à tester) | Cf. RG-TCK-06 |
| **Annulation / réouverture** | Comportement testé, câblage vers l'état engageant non testé | Même risque que P1-01 |
| **Notifications** | Distribution testée ; aucun test ne tomberait si un événement cessait d'être publié par une commande | Régression silencieuse possible |

---

## 13. Risques non couverts par tests

1. **P0-ter** — aucun test n'exerce un chemin d'accès avec un compte désactivé.
2. **Câblage de l'état engageant** — six appels de production sans filet.
3. **Montants d'une simulation historique** — figés par construction, non assertés.
4. **19 pages routables sans garde `EstAuthentifie`** — aucune ne fait l'objet d'un test de garde.
5. **Cinq implémentations distinctes de la décision d'accès** — trois d'entre elles
   (`Enrolement`, `AvoirExoneration`, tarification) ne sont couvertes par aucun test de câblage.
6. **Publication des événements de notification** — chaque commande publie son événement ; rien ne
   vérifie qu'elle continue de le faire.
7. **Règles de niveau C (§ 3)** — statuts établis par inventaire et sondage. Le P0-ter démontre
   qu'un statut ainsi établi peut être faux.

---

## 14. P0 restants

### P0-ter — La désactivation d'un compte interne est sans effet sur ses permissions

| | |
|---|---|
| **Gravité** | Bloquante — contrôle de sécurité inopérant |
| **Règles** | RG-SEC-01 (🔴), RG-SEC-02 (🟡) |
| **Preuves** | `Connexion.razor:175` · `AutorisationService.cs:22-33` · 19 pages sans garde `EstAuthentifie` · `AdministrationComptesAgentsCommands.cs:72` |
| **Exploitabilité** | Un administrateur désactivé qui conserve `administration.comptes` peut se reconnecter, ouvrir `/administration/comptes-agents` et **se réactiver**. Aucune donnée client n'est exposée (les fiches sont protégées par P0/P0-bis) ; c'est l'intégrité du contrôle d'accès qui est atteinte |
| **Atténuation existante** | `ObtenirContexteUtilisateurQuery:22` renvoie un contexte mort ; 50 pages sur 69 le respectent |
| **Nature du défaut** | La décision de sécurité vit dans l'interface, pas dans la couche d'autorisation — même cause racine que P0 et P0-bis |

*Aucune correction n'a été apportée : cet audit est en lecture seule.*

---

## 15. P1 restants

**Aucun.** P1-01, P1-02 et P1-03 sont fermés au niveau du comportement de production. Les réserves
qui subsistent sont des dettes de test (P2-04) ou des décisions métier assumées (P3-01, P3-02).

---

## 16. P2 reclassés

Reconstruits depuis zéro sur impact métier, impact sécurité, dette d'architecture, probabilité de
régression, couverture de test, dépendances et complexité. Les anciens P2 n'ont pas été repris tels
quels.

| # | Écart | Impact | Régression | Complexité | Justification du rang |
|---|---|---|---|---|---|
| **P2-01** | **Duplication résiduelle des décisions d'accès — 5 implémentations** (`ControleAccesRessourceService`, `ObtenirFicheEnrolementQuery`, `ObtenirFicheAvoirExonerationQuery`, `ConsulterSimulationProformaQuery`, `GenererDocumentProformaPdfQuery`) | Sécurité élevé | **Très élevée** | Moyenne | C'est la cause racine commune de P0, P0-bis et P0-ter. Trois défauts nés du même motif : tant qu'il subsiste, un quatrième est probable. À traiter **avec** le P0-ter, pas après |
| **P2-02** | **`SocieteId` fourni par l'appelant côté tarification** | Sécurité moyen | Moyenne | Faible | Non exploitable depuis l'IHM (les pages le tirent du contexte serveur), mais c'est le motif exact du P0 d'origine : une décision fondée sur une donnée de l'appelant |
| **P2-03** | **Journalisation des connexions absente** (RG-SEC-03) | Conformité élevé | Faible | Faible | La règle nomme explicitement la connexion. Sans elle, aucune détection possible d'un accès par compte désactivé — c'est aussi ce qui a rendu le P0-ter invisible en exploitation |
| **P2-04** | **Test de câblage de l'état engageant** | Métier élevé | **Très élevée** | Faible | Le défaut P1-01 est reproductible à l'identique aujourd'hui. Coût faible, valeur immédiate |
| **P2-05** | **Test RG-SIM-12 sur les montants** | Métier élevé | Moyenne | Faible | Une simulation opposable au client doit rester reproductible. Le comportement est correct ; seule l'assertion manque |
| **P2-06** | **19 pages routables sans garde `EstAuthentifie`** | Sécurité moyen | Élevée | Moyenne | Chacune est un P0-ter en puissance. À traiter par un mécanisme unique, non page par page |
| **P2-07** | **R-15 — script de reprise de données** | Exploitation élevé | Faible | Élevée | Bloque toute mise en production sur base peuplée (§ 18) |

---

## 17. P3 reclassés

| # | Écart | Justification du rang |
|---|---|---|
| **P3-01** | **Canaux mobiles simulés** (RG-COM-01) | L'architecture est complète ; l'activation relève de PC-16, arbitrage DSI/Finance. Rien à corriger côté code tant que la décision n'est pas prise |
| **P3-02** | **Assistant à mots-clés** (RG-IA-01) | Adaptateur simulé retenu en arbitrage explicite. Le port est prêt : substituer un vrai moteur n'exige aucune refonte |
| **P3-03** | **Archivage non matérialisé à la clôture** (RG-TCK-06) | La consultation post-clôture, qui est l'effet attendu par le client, fonctionne. Il manque un marqueur d'état, pas une capacité |
| **P3-04** | **Nature « transporteur » non distincte** (RG-CPT-08) | Distinction portée aujourd'hui par l'absence de rattachement société. Fonctionne, mais repose sur une absence — fragile à terme |
| **P3-05** | **Avertissement `CS9113`** (`AssistantCommands.journalisation`) | Paramètre non lu. Dette cosmétique |
| **P3-06** | **Portée file absente sur la fiche Avoir/Exonération** | `EstAutoriseAsync(…, fileId: null)` : tout porteur de la permission accède, quelle que soit la file. Module mono-file aujourd'hui |

---

## 18. R-15

**La conclusion précédente est maintenue, et sa qualification est celle qui avait été corrigée :**

La migration de normalisation 3FN (`20260905033506_NormalisationReferentielsTarifaires`) **n'est pas
applicable telle quelle sur une base contenant des données réelles**. Elle ajoute des clés
étrangères `NOT NULL` vers les nouveaux référentiels sans reprise préalable des lignes existantes.

**Le risque est un ÉCHEC DE MIGRATION / ROLLBACK SUR BASE PEUPLÉE — pas une perte de données.**
La transaction échoue à la création des contraintes et la base revient à son état antérieur. Rien
n'est corrompu ; le déploiement est simplement impossible.

Sur une base vierge, la migration s'applique sans difficulté — c'est le cas de tous les
environnements de test actuels, ce qui explique qu'aucune suite ne la signale.

**Décision : GO SOUS CONDITIONS**, tant que le script de reprise n'est pas conçu, testé sur une
copie de production et validé. Le script n'a pas été créé — la mission l'interdit explicitement.

---

## 19. Ordre optimal de remédiation

L'ordre suit les dépendances réelles, pas la numérotation.

**Phase 1 — Fermer le P0-ter et sa cause racine ensemble.** Les traiter séparément reviendrait à
répéter le cycle P0 → P0-bis → P0-ter, où chaque correction ferme une surface et en laisse d'autres
ouvertes.

1. **P0-ter** — porter l'état du compte dans la couche d'autorisation : contrôle `EstActif` dans
   `AutorisationService` (la décision commune à toutes les commandes) et dans le pipeline de
   connexion pour tous les comptes, pas les seuls clients.
2. **P2-01** — rallier les cinq implémentations de la décision d'accès à `ControleAccesRessourceService`.
   À faire dans le même mouvement : c'est ce service qui portera le contrôle ajouté en 1.
3. **P2-06** — remplacer les 19 gardes manquantes par un mécanisme unique plutôt que page par page.
4. **Tests de câblage** sur ces trois points, validés par mutation, avant de déclarer la phase close.

**Phase 2 — Reconstituer les filets manquants.** Peu coûteux, forte valeur de non-régression.

5. **P2-04** — test de câblage de l'état engageant.
6. **P2-05** — assertion des montants sur RG-SIM-12.
7. **P2-03** — journalisation des connexions (referme aussi la détection du P0-ter).

**Phase 3 — Rendre le déploiement possible.**

8. **P2-07 / R-15** — concevoir le script de reprise, le tester sur une copie de production, le faire
   valider. Sans lui, rien ne part en production sur base peuplée.
9. **P2-02** — dériver le `SocieteId` de l'identité côté tarification.

**Phase 4 — Décisions métier et dette résiduelle.**

10. **P3-01** (PC-16) et **P3-02** (assistant) : arbitrages, pas corrections.
11. **P3-03 → P3-06** : dette de faible impact.

---

## 20. Conclusion

Le projet est fonctionnellement mûr : 91 règles sur 98 sont pleinement conformes, les quatre modules
métier tiennent de bout en bout, la tarification est exacte au franc et normalisée en 3FN, et 1 294
tests passent. Les remédiations P0, P0-bis et P1 ont toutes tenu au contre-audit.

**Mais la sécurité est à 50 %, et c'est le seul chiffre qui décide aujourd'hui.** La désactivation
d'un compte interne — un contrôle que trois audits successifs ont déclaré conforme — ne fonctionne
pas. Un administrateur désactivé peut se reconnecter et se réactiver.

Ce qui frappe, dans ce troisième défaut d'accès, c'est qu'il partage **exactement** la cause racine
des deux précédents : la décision de sécurité est réécrite là où elle est utilisée, jamais posée à
un seul endroit. Les endpoints API avaient leur copie ; les fiches ticket et dossier avaient la
leur ; aujourd'hui ce sont la page de connexion, le service d'autorisation et 19 pages qui portent
chacun un fragment de la décision — et les fragments ne se recouvrent pas. Corriger le P0-ter sans
rallier les cinq implémentations restantes produirait un P0-quater.

Trois enseignements, que je formule aussi contre mon propre travail :

- **Un test tautologique est pire que pas de test.** `Assert.False(agent.EstActif)` après avoir
  écrit `false` a donné à SEC-11 une apparence de couverture qui a survécu à trois audits.
- **Une conclusion doit être bornée par ce qui a réellement été inspecté.** Mon rapport P0-bis a
  écrit « aucune autre lecture sans décision d'autorisation » alors que deux fiches complètes
  n'avaient pas été atteintes. La conclusion était plus large que le balayage.
- **Un score de conformité par règle ne mesure pas la gravité.** 95,9 % et un contrôle d'accès
  inopérant coexistent sans se contredire, parce qu'une règle vaut un point quelle que soit sa
  portée.

**Recommandation : NO-GO en production** tant que le P0-ter n'est pas fermé — indépendamment de
R-15, qui bloque de son côté tout déploiement sur base peuplée.
