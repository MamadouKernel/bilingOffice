# Plan de clôture et préparation production

*Mission de planification. Aucun code, test, migration ou configuration n'a été modifié ou exécuté.*
*Aucun arbitrage métier n'est pris.*

---

## 1. Résumé exécutif

Le projet est **techniquement prêt**. La conformité aux 98 règles atteint **99,0 %**, la suite est à
**1424/1424**, et il ne reste **aucune non-conformité ni fonction manquante**.

Ce qui sépare le projet de la production ne relève plus du développement : **une validation
d'exploitation** (R-15 sur données réelles) et **quatre arbitrages métier**.

Trois constats de cette phase de clôture méritent d'ouvrir le document.

**Le premier inverse une priorité.** SEC-06 est un « Must » du CDC, et la chaîne de rétention est
livrée sans déclencheur. La réaction naturelle serait de câbler le service manquant. **Ce serait une
erreur.** Le CDC classe explicitement les durées de conservation parmi les points « à valider
(durées légales) » — et les valeurs présentes dans le code (30 jours, 5 ans, 10 ans, 3 ans) ne
viennent donc d'aucune source validée. **Déclencher une purge sur des délais inventés détruirait des
données selon des échéances que personne n'a arrêtées.** L'absence de déclencheur est aujourd'hui
une protection ; elle ne doit être levée qu'après la décision juridique.

**Le deuxième éclaire PC-16.** Le CDC porte sa propre mesure de maîtrise : *« Démarrage possible avec
e-mail + notifications portail ; ajout des canaux mobiles sans impact sur le workflow. »* Un
démarrage sans canal mobile est donc explicitement envisagé par le document lui-même. Mais l'e-mail
n'est pas davantage actif que le SMS : les trois canaux externes pointent sur `CanalJournalise`.
**Le minimum sanctionné par le CDC exige donc au moins l'activation de l'e-mail.**

**Le troisième est un quatrième arbitrage, non identifié jusqu'ici** : les durées de conservation.
Le compte d'arbitrages ouverts passe de trois à quatre.

---

## 2. État actuel

| | |
|---|---|
| Commit audité | `1c3cd38260c3b196de8641b8ec1d5a89f27a9762` |
| HEAD | `1c3cd38…` — **inchangé, rien n'a été committé** |
| Worktree | **45 entrées non committées** |
| Suite | **1424 / 1424** (208 · 762 · 139 · 315) |
| Tests créés en remédiation | 31 |
| Migrations créées | 2, **non exécutées** |

**Le worktree non committé est un bloquant en soi** : l'état validé n'est adressable par aucun hash,
et n'est donc ni déployable ni reproductible. C'est la première action du lot A.

---

## 3. Conformité

| Statut | Nombre |
|---|---|
| ✅ CONFORME | **95** |
| 🟡 PARTIELLEMENT CONFORME | **2** — RG-COM-01, RG-IA-01 |
| 🔴 NON CONFORME | **0** |
| ⚫ NON IMPLÉMENTÉ | **0** |
| 🔵 N/A / À ARBITRER | **1** — RG-CPT-08 |

**99,0 %** — 96,0 / 97 (N/A exclu). 20 domaines sur 22 à 100 %.

Les deux partiels et le N/A relèvent tous les trois d'une décision métier. Aucun ne se lève par du
développement seul.

---

## 4. Arbitrages ouverts

**Quatre, et non trois.** Le quatrième est apparu lors de cette clôture.

| ID | Sujet | Règle | Source de l'obligation | Décideur |
|---|---|---|---|---|
| **A-01** | Nature Transporteur distincte ? | RG-CPT-08 🔵 | Merise §1.1 | Métier / PO |
| **A-02** | Quels canaux de notification activer ? | RG-COM-01 🟡 | Merise §1.13 + CDC COM-01/02 | DSI / Métier |
| **A-03** | Moteur et périmètre de l'assistant | RG-IA-01 🟡 | Merise §1.15 | DSI / Métier |
| **A-04** | **Durées légales de conservation** | *hors 98 RG* | **CDC SEC-06 (Must) + §25 « à valider (durées légales) »** | **Juridique / DSI** |

A-04 conditionne D-01 : **on ne câble pas une purge avant d'avoir validé ses échéances.**

---

## 5. Dettes techniques

Onze dettes, reprises de `docs/conformite-finale-98RG-et-dettes.md`. Aucune n'entre au score des
98 RG. Détail complet en § 12 (backlog) ; synthèse ici.

| ID | Dette | Sévérité | Priorité | Moment | Bloque prod |
|---|---|---|---|---|---|
| D-01 | Chaîne de rétention SEC-06 sans déclencheur | HIGH | P1 | AVANT PILOTE *(planification)* / APRÈS PROD *(exécution)* | NON |
| D-02 | Actions en masse `ComptesAgents` non atomiques | MEDIUM | P1 | AVANT PILOTE | NON |
| D-03 | `AccesRefuseException` non gérée sur ces actions | MEDIUM | P1 | AVANT PILOTE | NON |
| D-04 | Couplage `ValidationInterval` ↔ clôture de compte | MEDIUM | P2 | AVANT PILOTE *(documentation)* | NON |
| D-05 | `workflow.deroger` non attribuable | LOW | P3 | BACKLOG | NON |
| D-06 | `DerogationWorkflowService` inatteignable | LOW | P3 | BACKLOG | NON |
| D-07 | `CreerVersionGrilleTarifaireCommandHandler` doublonné | LOW | P3 | BACKLOG | NON |
| D-08 | 2FA non éprouvé de bout en bout | LOW | P2 | APRÈS PROD | NON |
| D-09 | Clôture de compte en deux transactions | LOW | P2 | APRÈS PROD | NON |
| D-10 | Parallélisme de la suite (contention LocalDB) | LOW | P3 | BACKLOG | NON |
| D-11 | `ComptesAgents.razor` sans test de rendu | LOW | P2 | APRÈS PROD | NON |

**Aucune dette ne bloque la production.**

---

## 6. SEC-06 — chaîne de rétention

### L'obligation, citée du CDC

> **SEC-06 · Must** — « Protection des données personnelles (clients, chauffeurs) conformément à la
> réglementation ivoirienne en vigueur (ARTCI) : minimisation, **durées de conservation**, droits des
> personnes. »

> **§25, points à arbitrer** — « Politique de conservation et d'archivage des tickets et documents
> **à valider (durées légales)**. »

Le CDC exige donc la rétention **et** déclare ses durées non arrêtées. Les deux phrases se lisent
ensemble.

### État constaté

| Élément | État |
|---|---|
| `MoteurRetentionService` — moteur de décision | ✅ livré, testé (`MoteurRetentionServiceTests`) |
| `GestionnaireRetentionService` — orchestration | ✅ livré, transactionnel, idempotent, journalisé en `LotPurgeRetention` |
| Enregistrement DI | ✅ |
| Tests de certification | ✅ `SecCert06RetentionTests` |
| **Déclencheur production** | ❌ **aucun** — ni service hébergé, ni commande d'administration |
| **Durées de conservation** | ⚠️ **codées en dur, non validées** |

Six `BackgroundService` existent et sont enregistrés par `AddHostedService` ; **aucun ne concerne la
rétention.** Le motif est établi et suivi partout ailleurs — il n'a pas été appliqué ici.

### Le point qui inverse la priorité

`MoteurRetentionService` porte quatre échéances **en dur** :

| Catégorie | Délai codé | Action à échéance | Source |
|---|---|---|---|
| Brouillon | 30 jours | Purge | **non sourcée** |
| Dossier | 5 ans | Anonymisation | **non sourcée** |
| Comptable | 10 ans | Archivage inaltérable | **non sourcée** *(cohérent avec OHADA, à confirmer)* |
| Audit | 3 ans | Archivage inaltérable | **non sourcée** |

Le CDC ne fixe aucune de ces valeurs — il les renvoie explicitement à validation. Elles violent au
passage le principe suivi ailleurs dans le socle : `RappelExpirationBadgesService` documente que la
cadence technique vit dans le code, mais que **les valeurs métier restent en configuration et ne
sont pas inventées par le code (PC-12)**.

**Conséquence pratique : l'absence de déclencheur protège aujourd'hui les données.** Câbler le
service avant A-04 exécuterait des purges et des anonymisations sur des échéances que personne n'a
arrêtées — et l'anonymisation est irréversible.

### Spécification cible

| Point | Recommandation |
|---|---|
| **Déclencheur** | `BackgroundService` sur le modèle des six existants — enveloppe d'hébergement sans règle métier |
| **Fréquence** | Cadence technique quotidienne (hors heures ouvrées), à porter en configuration |
| **Données concernées** | Brouillons de saisie · dossiers/tickets clôturés · pièces comptables · entrées de journal d'audit |
| **Durées de conservation** | **DECISION REQUIRED (A-04)** — à porter en configuration, jamais en dur |
| **Anonymisation** | `Ticket.MarquerAnonymise` existe ; ne déplace plus la date d'archivage depuis RG-TCK-06 |
| **Suppression** | Réservée aux brouillons. Le journal d'audit reste append-only : archivage inaltérable, jamais suppression |
| **Gel légal** | `ActiverGelLegal` / `DesactiverGelLegal` existent au domaine ; l'orchestration doit les respecter comme condition d'exclusion |
| **Audit** | `LotPurgeRetention` est déjà écrit par lot — à conserver, avec 0 donnée personnelle dans les logs |
| **Dry-run** | **À ajouter** — mode « évaluer sans muter », indispensable avant la première exécution réelle |
| **Reprise sur erreur** | Transactionnel par lot, déjà en place |
| **Idempotence** | Déjà en place — à couvrir par un test de double exécution |
| **Observabilité** | Compteur d'éléments traités par catégorie, durée du lot, échecs — sans PII |

### Classification

**C — fonctionnalité obligatoire non câblée**, au titre du CDC (Must), hors des 98 RG.
Sévérité **HIGH**. **Ne bloque pas la mise en service** — rien n'est perdu, tout est conservé.

---

## 7. RG-CPT-08 — fiche de décision

> « Un compte transporteur/chauffeur est un type de compte distinct du compte client, dédié au module
> Enrôlement Transporteurs. »

### État actuel

`NatureUtilisateur` = { `Client`, `Agent` }. Le transporteur est un `Client` sans rattachement
société. La distinction **existe fonctionnellement** : parcours d'inscription dédié
(`InscrireTransporteurCommand`), absence de rattachement reconnue explicitement, module dédié,
dossiers personnels admis par la chaîne d'autorisation (`dossier.ClientId == utilisateurId`).

Ce qui manque, au regard du texte, c'est qu'elle ne soit pas **nommée** : elle se déduit d'une
absence.

### Impact d'un ajout de `Transporteur` — 13 sites

| Domaine | Sites | Effet d'une addition naïve |
|---|---|---|
| **Authentification** | `RegleAccesConnexion` | aucun — la règle porte sur `EstActif` et la nature Client |
| **Autorisation** | `ControleAccesRessourceService` ×2 | aucun — les tests portent sur `!= Agent` |
| **Notes internes** | `ObtenirFicheTicketQuery`, `ObtenirFicheEnrolementQuery` | 🔴 **le transporteur recevrait la vue AGENT, notes internes comprises** |
| **Pages agent** | 5 × `BillingExport/File*.razor` | 🔴 **accès à cinq files agent** |
| **Fiches (IHM)** | `FicheTicket`, `FicheDossier`, `FicheEnrolement` | traité comme agent à l'écran |
| **ENT-03** | `DeterminerVisibiliteDemandesQuery` | aucun — porte sur le rattachement, pas la nature |
| **Dossiers / tickets** | propriété nominative | aucun — déjà reconnue |
| **Société** | `ObtenirContexteUtilisateurQuery` | l'espace Client ne lui serait plus proposé |
| **Création de compte** | `InscrireTransporteurCommand` | à adapter |
| **Permissions / habilitations** | — | aucun — le modèle est par code de permission, pas par nature |
| **Clôture de compte** | `CloturerMonCompteCommand` | il ne pourrait plus clôturer son compte |
| **Mentions** | `MentionnerCollaborateurCommand` | il deviendrait mentionnable en note interne |
| **Tests** | suites de cloisonnement | à étendre |

**7 des 13 sites ont une conséquence de sécurité.**

### Cause racine

Le code n'exprime nulle part la notion d'**usager externe**. Il écrit `Nature == Client`, ce qui
confond « n'est pas un agent » et « est un client d'entreprise ». **Ajouter une nature ne crée pas le
problème : il le révèle.**

### Les deux options

#### OPTION A — Le transporteur reste `Client`

| | |
|---|---|
| **Avantages** | Aucun changement de modèle. Aucun risque de régression. La distinction opérationnelle existe déjà et fonctionne. Coût nul |
| **Inconvénients** | Le texte de RG-CPT-08 parle d'un « type de compte distinct » — la lettre n'est pas satisfaite. La règle reste 🔵 |
| **Risques** | Un futur besoin de différenciation (droits, parcours, reporting) exigera le lot de toute façon |
| **Effort** | Nul — éventuellement une note de justification |
| **Impacts sécurité** | Aucun |

#### OPTION B — Nature `Transporteur` distincte

| | |
|---|---|
| **Avantages** | Le modèle dit ce que le CDC décrit. Ouvre une différenciation fine (droits, écrans, reporting) |
| **Inconvénients** | Lot dédié, 13 sites, dont 7 sensibles |
| **Risques** | 🔴 Élevés si conduit naïvement : fuite de notes internes vers les transporteurs, accès à cinq files agent |
| **Effort** | **L** — en deux temps obligatoires : (1) introduire `EstUsagerExterne` et l'appliquer aux 13 sites **à comportement constant**, vérifiable par la suite existante ; (2) seulement ensuite ajouter la valeur d'énumération, avec tests de cloisonnement dédiés et validation par mutation |
| **Impacts sécurité** | Maîtrisables **à condition** de respecter l'ordre en deux temps. L'inverser est la manière sûre d'introduire une régression |

**Aucune décision n'est prise ici.**

---

## 8. PC-16 / RG-COM-01 — fiche d'arbitrage

### État constaté

| Élément | Techniquement prêt | Activé en production |
|---|---|---|
| Résolution des destinataires | ✅ | ✅ |
| Gabarits et interpolation | ✅ | ✅ |
| Lien direct vers le ticket (`GabaritNotification.GarantirLienVersTicket`) | ✅ | ✅ |
| Règles de diffusion paramétrables | ✅ | ✅ |
| File de reprise sur échec | ✅ | ✅ |
| **Notification portail (In-App)** | ✅ | ✅ **— seul canal réellement actif** |
| **E-mail** | ✅ | ❌ `CanalJournalise` |
| **SMS** | ✅ | ❌ `CanalJournalise` |
| **WhatsApp** | ✅ | ❌ `CanalJournalise` |

**Rien n'est en attente de développement.** Ce qui manque est le choix d'un fournisseur, son contrat
et ses coûts.

### L'élément sourcé qui éclaire la décision

Le CDC porte sa propre mesure de maîtrise du risque « canaux mobiles » :

> « **Démarrage possible avec e-mail + notifications portail ; ajout des canaux mobiles sans impact
> sur le workflow.** »

Trois conséquences :

1. **Un démarrage sans canal mobile est explicitement envisagé par le CDC.** RG-COM-01 resterait 🟡
   au regard des 98 règles, mais le phasage est prévu par le document source.
2. **Le minimum sanctionné exige néanmoins l'e-mail** — qui n'est pas actif aujourd'hui. Le portail
   seul est en deçà du démarrage que le CDC décrit.
3. **L'ajout ultérieur est sans impact sur le workflow** — le CDC l'affirme, et l'architecture le
   confirme : brancher un canal est une substitution d'adaptateur derrière `ICanalNotification`.

### Décision attendue

| Question | Portée |
|---|---|
| E-mail activé au démarrage ? | **Recommandé par le CDC** — minimum du démarrage décrit |
| SMS activé ? | Fournisseur, coût au message, couverture opérateurs CI |
| WhatsApp activé ? | Fournisseur (WhatsApp Business API), conformité, coût |
| Phasage | Démarrage e-mail + portail, mobile en vague 2 ? |

**Aucun fournisseur n'est configuré, et aucun ne l'est ici.**

---

## 9. Assistant IA — RG-IA-01 / RG-IA-02

### État constaté

**RG-IA-02 est ✅** : `EscaladerAssistantVersOperateurCommand` transmet le fil intégral en message
client, la conversation est persistée et rattachée au ticket. Aucune perte de contexte.

**RG-IA-01 est 🟡** :

| Exigence | État |
|---|---|
| Répond en français aux questions de premier niveau | 🟡 **8 sujets**, appariement de sous-chaînes |
| Oriente le client dans ses démarches | 🟡 dans ce périmètre |
| **L'aide à compléter ses dossiers** | ❌ aucun accès au dossier, aucune assistance à la saisie |

Hors de ces huit sujets, l'assistant escalade avec un motif plutôt que de deviner — comportement
honnête et délibéré.

Le port `IAssistantConversationnel` est propre : substituer un moteur n'exige aucune refonte.
**L'aide à la complétion, en revanche, est une capacité nouvelle** — elle suppose un accès au dossier
et une compréhension de son état, pas seulement un adaptateur.

### Les trois options

| Critère | **A — Règles enrichies** | **B — Moteur IA externe** | **C — Modèle hébergé/interne** |
|---|---|---|---|
| **Qualité** | Prévisible, bornée. Ne généralise pas | Élevée sur la langue et l'intention | Variable selon le modèle retenu |
| **Coût** | Faible, surtout en conception | Au volume — à modéliser sur le trafic attendu | Investissement initial + exploitation GPU |
| **Sécurité** | Maximale — aucune surface externe | Surface réseau sortante à encadrer | Maîtrisée, sous responsabilité CIT |
| **Données** | Ne quittent jamais la plateforme | **Sortent** — question centrale | Ne sortent pas |
| **Confidentialité** | Totale | Dépend du contrat et de la localisation (ARTCI) | Totale |
| **Maintenance** | Élevée — chaque sujet est écrit à la main | Faible côté modèle | Élevée — MCO du modèle et de l'infrastructure |
| **Disponibilité** | Celle de la plateforme | Dépendance externe, SLA tiers | Celle de l'infrastructure CIT |
| **Auditabilité** | Totale — la réponse est déterministe | Faible — réponse non reproductible | Moyenne selon la journalisation |
| **Effort d'intégration** | **S** — le port existe | **M** — adaptateur + garde-fous + limites de coût | **L** — infrastructure, MCO, montée de version |

### Question à trancher en plus du moteur

**« Aider à compléter ses dossiers » entre-t-il dans le périmètre livré ?** C'est une capacité, pas
un adaptateur : quelle que soit l'option retenue, elle demande un accès aux données du dossier et
une conception propre — donc un chiffrage distinct.

**Aucune option n'est choisie ici.**

---

## 10. R-15 — procédure de répétition sur copie de production

> **Statut : GO SOUS CONDITIONS** — inchangé.

Le lot 3FN est conforme (RG-SIM-11 et RG-SIM-12 ✅). Le script est écrit, transactionnel, idempotent,
et déjà répété sur SQL Server : nominal, idempotence, rollback sur données incohérentes, continuation
de la chaîne EF.

**Il n'a pas été confronté à une copie des données de production.** La répétition précédente avait
révélé **trois erreurs que trois relectures n'avaient pas vues** — c'est pourquoi cette confrontation
ne se présume pas.

### Procédure — 17 étapes

| # | Étape | Responsable | Point de contrôle |
|---|---|---|---|
| 1 | Sauvegarde complète de production | DBA | Sauvegarde vérifiée et restaurable |
| 2 | Restauration en environnement **isolé** | DBA / Infra | Aucune connectivité vers la production |
| 3 | Contrôle de la version de schéma | DBA | `__EFMigrationsHistory` relevé et consigné |
| 4 | Inventaire des données | DBA / Tech Lead | Comptages par table, **valeurs distinctes de Sens / Taille / Type** |
| 5 | **Confrontation aux correspondances d'ordinaux (section E)** | Tech Lead | **Toute valeur non prévue = NO-GO immédiat** |
| 6 | Dry-run si disponible | Tech Lead | Journal de reprise lu intégralement |
| 7 | Exécution R-15 | DBA | Transaction validée |
| 8 | Vérification des lignes non résolues | Tech Lead | **0 ligne** |
| 9 | Vérification des comptages | DBA | Identiques avant/après |
| 10 | Vérification des clés étrangères | DBA | **0 violation** |
| 11 | Vérification des index | DBA | Index attendus présents, anciens supprimés |
| 12 | Vérification d'unicité | DBA | **0 violation** |
| 13 | Poursuite des migrations EF | Tech Lead | `database update` va au bout |
| 14 | Démarrage applicatif | Infra | Application démarre, health checks verts |
| 15 | Tests applicatifs critiques | QA | Suites sécurité + Billing + Simulation |
| 16 | **Simulations historiques** | QA / Métier | Recalcul **identique au franc** sur au moins 2 simulations antérieures |
| 17 | Test de rollback | DBA | Restauration de la sauvegarde vérifiée |

**Rapport de répétition** consigné et signé avant toute exécution réelle.

### Critères GO / NO-GO

**GO uniquement si les huit conditions sont réunies :**

```
[ ] 0 ligne non résolue
[ ] 0 violation de clé étrangère
[ ] 0 violation d'unicité
[ ] comptages cohérents avant/après
[ ] migrations EF passent jusqu'au bout
[ ] l'application démarre
[ ] tests critiques verts
[ ] simulations historiques reproductibles au franc
```

**Une seule condition non remplie = NO-GO.** Aucune ne se compense.

---

## 11. Migrations

### Séquence complète

| Ordre | Migration | Nature | Statut |
|---|---|---|---|
| … | *(migrations antérieures appliquées)* | — | appliquées |
| 1 | `20260905033506_NormalisationReferentielsTarifaires` | **lot 3FN — objet de R-15** | à appliquer **via R-15** |
| 2 | `20260905075121_AjoutSeuilAnnulationParWorkflow` | additive | à appliquer |
| 3 | `20260905080609_AjoutAssistantVirtuel` | additive | à appliquer |
| 4 | `20260906034753_AjoutDraftInExport` | additive, nullable, `Tickets` | **non exécutée** |
| 5 | `20260906035408_AjoutAxeTypeClientSla` | additive, nullable, `ParametresSla` | **non exécutée** |

### Ordre par rapport à R-15

**R-15 vient en premier.** Le script de reprise prend en charge la migration n° 1 et l'inscrit
lui-même dans `__EFMigrationsHistory` ; `dotnet ef database update` poursuit ensuite la chaîne à
partir de la n° 2 — comportement validé lors de la répétition.

Les migrations n° 4 et 5 :

- sont **additives** (`AddColumn` seul), **nullables**, **réversibles** (`DropColumn` symétrique) ;
- portent sur `Tickets` et `ParametresSla`, **tables disjointes** de celles que R-15 remanie ;
- **n'ont aucune dépendance à R-15** et ne peuvent pas interférer avec lui.

```
SÉQUENCE :   sauvegarde → R-15 (migration 1) → ef database update (2 → 5) → démarrage → smoke tests
```

---

## 12. Backlog final

Le détail exploitable est dans **`docs/backlog-cloture.md`**. Synthèse par catégorie :

| Catégorie | Nombre | IDs |
|---|---|---|
| **A. Arbitrages métier** | 4 | A-01 → A-04 |
| **B. Conditions obligatoires avant production** | 2 | C-01, C-02 |
| **C. Dettes techniques** | 11 | D-01 → D-11 |
| **D. Améliorations recommandées** | 3 | I-01 → I-03 |
| **E. Risques acceptés** | 2 | R-ACC-01, R-ACC-02 |
| **F. Travaux post-production** | 4 | PP-01 → PP-04 |

---

## 13. Priorisation

### LOT A — Bloquants production

| | |
|---|---|
| **Contenu** | **C-01** répétition R-15 sur copie de production · **C-02** commit de l'état validé et worktree propre |
| **Dépendances** | C-02 ne dépend de rien et conditionne tout le reste — un état non committé n'est pas déployable |
| **Effort** | C-02 : **S** · C-01 : **M** (mobilisation DBA + environnement isolé) |
| **Responsable** | C-02 : Tech Lead · C-01 : DBA (A) + Tech Lead (R) |
| **Critère de sortie** | Hash committé, `git status` vide, suite 1424/1424 sur ce hash · Rapport de répétition R-15 avec les 8 critères satisfaits |

### LOT B — Arbitrages avant pilote

| | |
|---|---|
| **Contenu** | **A-01** RG-CPT-08 · **A-02** PC-16 · **A-03** assistant IA · **A-04** durées légales de conservation |
| **Dépendances** | A-04 **précède** D-01. Les trois autres sont indépendants |
| **Effort** | Décisionnel. Le chiffrage de mise en œuvre suit la décision |
| **Responsable** | A-01 : Métier/PO · A-02 : DSI · A-03 : DSI/Métier · A-04 : **Juridique** |
| **Critère de sortie** | Décision écrite, datée, et statut de règle mis à jour |

### LOT C — Dettes à court terme

| | |
|---|---|
| **Contenu** | **D-01** planification de la rétention (après A-04) · **D-02** atomicité des actions en masse · **D-03** wrapper d'erreur · **D-04** documentation du couplage `ValidationInterval` |
| **Dépendances** | D-01 ⇠ A-04. D-02 et D-03 se traitent ensemble, même fichier, même geste |
| **Effort** | D-01 : **M** · D-02+D-03 : **S** · D-04 : **XS** |
| **Responsable** | Tech Lead (A), développeur (R), sécurité (C) |
| **Critère de sortie** | Voir critères individuels au backlog |

### LOT D — Améliorations post-production

| | |
|---|---|
| **Contenu** | **D-05 → D-11**, **I-01 → I-03**, **PP-01 → PP-04** |
| **Dépendances** | D-05 et D-06 se décident ensemble (même service, même permission) |
| **Effort** | Majoritairement **S** ; D-08 et D-11 : **M** |
| **Responsable** | Tech Lead (A), développeur (R), QA (C) |
| **Critère de sortie** | Chaque entrée sortie du backlog avec son critère propre |

---

## 14. RACI

| Action | DSI | Métier / PO | Tech Lead | Dév | DBA | Sécurité | Infra | QA | Juridique |
|---|---|---|---|---|---|---|---|---|---|
| C-01 R-15 sur copie prod | I | I | **R** | C | **A** | C | C | C | — |
| C-02 Commit état validé | I | — | **A/R** | C | — | — | — | I | — |
| A-01 RG-CPT-08 | C | **A** | C | — | — | C | — | I | — |
| A-02 PC-16 canaux | **A** | C | C | — | — | C | C | — | I |
| A-03 Assistant IA | **A** | C | C | — | — | C | C | — | C |
| A-04 Durées légales | C | C | I | — | C | C | — | — | **A/R** |
| D-01 Rétention SEC-06 | I | I | **A** | **R** | C | C | C | C | C |
| D-02 / D-03 ComptesAgents | — | I | **A** | **R** | — | C | — | C | — |
| D-04 Couplage `ValidationInterval` | I | — | **A/R** | C | — | C | C | — | — |
| D-05 → D-07 Code mort | — | I | **A** | **R** | — | — | — | I | — |
| D-08 2FA bout en bout | I | — | **A** | **R** | — | C | — | **R** | — |
| D-09 Clôture transactionnelle | — | I | **A** | **R** | C | — | — | C | — |
| D-10 Parallélisme suite | — | — | **A** | **R** | — | — | — | C | — |
| D-11 Test de rendu | — | — | **A** | **R** | — | — | — | **R** | — |
| MEP | **A** | I | **R** | C | **R** | C | **R** | **R** | — |
| Rollback | **A** | I | **R** | C | **R** | I | **R** | C | — |

*R = Responsible · A = Accountable · C = Consulted · I = Informed.*
*Aucun nom de personne n'est cité.*

---

## 15. Matrice de risques

**Un risque n'est pas une dette.** Une dette est un travail à faire ; un risque est un événement qui
peut survenir. Certaines dettes engendrent un risque, la plupart non.

| ID | Risque | Probabilité | Impact | Niveau | Mitigation | Bloque prod |
|---|---|---|---|---|---|---|
| **RQ-01** | R-15 rencontre en production une valeur d'ordinal non prévue et corrompt les données tarifaires | Moyenne | **Critique** | **ÉLEVÉ** | Répétition obligatoire sur copie (C-01), étape 5 en NO-GO immédiat, sauvegarde préalable, rollback testé | **OUI** |
| **RQ-02** | La rétention est câblée avant validation juridique et **anonymise irréversiblement** des données selon des délais inventés | Faible | **Critique** | **ÉLEVÉ** | **Ordonnancement A-04 → D-01, non négociable.** Mode dry-run avant première exécution réelle | NON |
| **RQ-03** | Déploiement d'un état non committé : impossible de reproduire ou de revenir en arrière | Moyenne | Élevé | **ÉLEVÉ** | C-02 en préalable absolu | **OUI** |
| **RQ-04** | Un administrateur se dépouille de ses permissions via « tout retirer » et laisse un état partiel | Faible | Moyen | MOYEN | D-02 / D-03 ; invariant serveur déjà présent sur `administration.comptes` | NON |
| **RQ-05** | `ValidationInterval` relevé pour raisons de performance : la promesse « vos sessions ont été déconnectées » devient fausse | Faible | Moyen | MOYEN | D-04 — documenter la dépendance à l'endroit du réglage | NON |
| **RQ-06** | Mise en service sans aucun canal externe : le client n'est notifié que s'il se connecte au portail | **Élevée** | Moyen | **MOYEN** | A-02 — le CDC sanctionne un démarrage e-mail + portail, ce qui suppose d'activer l'e-mail | NON |
| **RQ-07** | Le 2FA d'Identity dysfonctionne en production sans avoir été éprouvé de bout en bout | Faible | Élevé | MOYEN | D-08 ; smoke test d'authentification en MEP | NON |
| **RQ-08** | Un futur lot RG-CPT-08 conduit naïvement expose les notes internes aux transporteurs | Moyenne | **Élevé** | **MOYEN** | Ordre en deux temps imposé (§7, option B) ; tests de cloisonnement dédiés | NON |
| **RQ-09** | Contention LocalDB produit des échecs trompeurs et masque une vraie régression en CI | Moyenne | Faible | FAIBLE | D-10 ; exécution séquentielle par projet en attendant | NON |

**Deux risques bloquent la production : RQ-01 et RQ-03.** Tous deux sont couverts par le lot A.

---

## 16. Checklist avant pilote

```
[ ] Commit de l'état post-remédiation créé              C-02
[ ] Worktree propre (git status vide)                   C-02
[ ] Suite 1424/1424 vérifiée SUR CE HASH                C-02
[ ] Séquence de migrations validée sur environnement de recette
[ ] R-15 répété sur copie de production, 8 critères OK  C-01
[ ] Rapport de répétition R-15 consigné                 C-01
[ ] Arbitrage RG-CPT-08 rendu                           A-01
[ ] Arbitrage PC-16 rendu (canaux activés au démarrage) A-02
[ ] Arbitrage assistant IA rendu                        A-03
[ ] Durées légales de conservation validées             A-04
[ ] SEC-06 planifié (déclencheur + durées en config)    D-01
[ ] Sauvegarde et rollback validés en conditions réelles
[ ] Supervision et alerting prêts (SLA, files, échecs de notification)
[ ] Journal d'audit vérifié : append-only actif en base
[ ] Comptes et habilitations d'exploitation revus
[ ] Tests sécurité verts (232/232)
[ ] Jeu de smoke tests préparé et revu
```

---

## 17. Checklist mise en production

### PRE-MEP

```
[ ] Fenêtre de maintenance annoncée
[ ] Sauvegarde complète vérifiée ET restaurable
[ ] Hash à déployer confirmé, identique à celui recetté
[ ] Secrets et chaînes de connexion en place — appsettings de production vides par conception
[ ] Configuration des durées de rétention en place (si D-01 livré)
[ ] Fournisseurs de notification configurés (selon A-02)
[ ] Plan de rollback relu et responsable désigné
```

### MEP

```
[ ] Mise en maintenance de l'application
[ ] Sauvegarde de bascule
[ ] Exécution R-15 (migration 3FN)
[ ] Lecture intégrale du journal de reprise
[ ] dotnet ef database update — migrations 2 → 5
[ ] Vérification __EFMigrationsHistory
[ ] Déploiement applicatif
[ ] Health checks
[ ] Sortie de maintenance
```

### POST-MEP — smoke tests

```
[ ] Authentification : connexion, 2FA, déconnexion
[ ] Autorisation : compte désactivé refusé ; cloisonnement inter-société refusé
[ ] Billing IMPORT : soumission, unicité BL, aiguillage par profil
[ ] Billing EXPORT : soumission, Draft IN, facture, BEE
[ ] Simulation : un calcul contrôlé au franc contre l'Annexe A
[ ] SLA : un ticket franchit un seuil, l'alerte est produite
[ ] Notifications : une notification portail arrive et son lien ouvre le ticket
[ ] Audit : les actions ci-dessus figurent au journal
[ ] Archivage : un ticket clôturé est archivé et reste consultable
[ ] Supervision : métriques et logs remontent
```

---

## 18. Rollback

**Déclencheurs :** échec de R-15 · échec des migrations EF · application ne démarre pas · un smoke
test critique échoue (authentification, autorisation, simulation).

```
[ ] Décision de rollback prononcée par le responsable désigné (DSI accountable)
[ ] Remise en maintenance
[ ] Restauration de la sauvegarde de bascule
[ ] Redéploiement de la version applicative précédente
[ ] Vérification __EFMigrationsHistory cohérente avec la version restaurée
[ ] Health checks
[ ] Smoke tests réduits : authentification + consultation d'un ticket
[ ] Sortie de maintenance
[ ] Post-mortem consigné avant toute nouvelle tentative
```

**Point d'attention.** R-15 est transactionnel et refuse d'opérer sur données incohérentes ; le
rollback par restauration reste néanmoins la voie sûre, parce que les migrations EF qui le suivent ne
sont pas, elles, regroupées dans une transaction unique.

---

## 19. GO / NO-GO

> ### 🟠 **GO SOUS CONDITIONS**

**Les faits qui plaident pour :**

- conformité **99,0 %** — 95 conformes, 0 non conforme, 0 non implémentée ;
- **P0 : 0 · P1 : 0** ;
- **1424/1424**, dont 232 tests de sécurité ;
- les quatre défauts d'accès successifs du projet sont fermés et vérifiés par mutation ;
- 20 domaines fonctionnels sur 22 à 100 %.

**Les faits qui imposent des conditions :**

- **R-15 n'a pas été confronté aux données de production** (RQ-01, bloquant). La répétition
  précédente avait révélé trois erreurs invisibles à la relecture ;
- **l'état validé n'est pas committé** (RQ-03, bloquant) — 45 entrées non versionnées, aucun hash
  déployable ;
- **quatre arbitrages métier ouverts**, dont un — les durées légales — conditionne une opération
  irréversible ;
- **onze dettes techniques**, aucune bloquante, dont une HIGH.

**Ni GO sec, ni NO-GO.** Un NO-GO serait injustifié : rien dans le produit ne s'y oppose. Un GO sec
serait imprudent : deux conditions bloquantes ne relèvent pas du produit mais de l'exploitation, et
elles ne se contournent pas.

---

## 20. Conclusion

Le projet arrive à sa clôture technique dans un état qu'il n'avait pas au début de ce cycle : **zéro
non-conformité, zéro fonction manquante, une suite entièrement verte, et une chaîne d'autorisation
unique protégée par des tests qui lisent les sources.**

Ce plan ne demande plus de développement pour la mise en service. Il demande **une répétition, un
commit, et quatre décisions.**

Une remarque, parce qu'elle vaut au-delà de ce document. La tentation naturelle, en lisant que
SEC-06 est un « Must » du CDC livré sans déclencheur, est de câbler le service manquant — c'est ce
que le projet a fait, à juste titre, pour `MarquerEtatEngageant`, pour la garde de fiche, pour
`AutorisationChampService`. **Ici, ce serait une faute.** Le même CDC qui exige la rétention renvoie
ses durées à validation juridique, et les valeurs présentes dans le code ne viennent d'aucune source.
Déclencher une purge sur des délais inventés détruirait des données selon des échéances que personne
n'a arrêtées — et l'anonymisation ne se défait pas.

**L'absence de déclencheur, ici, protège.** C'est le seul endroit de tout ce cycle où « la règle
existe mais rien ne l'exécute » est le bon état provisoire — et c'est précisément pour cela qu'il
fallait le vérifier à la source plutôt que de le déduire du motif.

Le reste est de l'ordonnancement.
