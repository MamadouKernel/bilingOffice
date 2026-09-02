# R0 — Baseline Nominative des 188 Exigences CDC

> **Document** : `docs/17-baseline-nominative-188-exigences.md`
> **Date** : 1er septembre 2026
> **Nature** : consolidation nominative. **Aucun code, aucune migration, aucun workflow Spec Kit, aucun commit, aucun push.**
> **Statut** : **SOURCE DE VÉRITÉ DE COUVERTURE** — remplace tout décompte antérieur.

---

## 1. Méthode

1. **Extraction mécanique** des 188 identifiants depuis `docs/01-matrice-exigences.md` (188 uniques, 0 doublon, 150 Must / 32 Should / 6 Could — conforme aux totaux auto-déclarés du document).
2. **Attribution nominative** : pour chaque Feature, extraction de la liste des exigences réellement revendiquées dans sa `closure-review.md` et sa matrice de couverture `spec.md`.
3. **Recherche de recouvrements** : détection mécanique des identifiants revendiqués par plusieurs Features.
4. **Dénombrement par comptage de lignes** — **jamais par soustraction**. La règle `188 − restantes = conformes` est proscrite ; c'est elle qui avait produit le faux « +4 » de `docs/16`.
5. **Preuve obligatoire pour CONFORME** : rattachement à une closure de Feature qui l'a comptée dans son delta. Le seul fait qu'un comportement existe dans le code **ne suffit pas** ; ce cas produit `PARTIELLE — capacité existante non certifiée`.

### Validation de la méthode

La reconstruction nominative reproduit **exactement** la chaîne historique, sans aucun ajustement :

```
F001 +74 -> 74      F004 +5  -> 115     F009 +8  -> 142
F002 +11 -> 85      F005 +10 -> 125     F010 +6  -> 148
F003 +24 -> 109     F006 +6  -> 131     F007 +4  -> 152
F003.5 +1 -> 110    F008 +3  -> 134
```

Cette correspondance parfaite (notamment `74 + 11 + 24 + 1 = 110`) **valide l'attribution nominative** : les listes reconstruites sont bien celles qu'ont utilisées les Features.

---

## 2. Reconstruction Feature par Feature

### F001 — Socle Tickets & Workflows : **74 exigences**

`SOC-01`→`SOC-14` (14) · `CYC-01`→`CYC-07` (7) · `MSG-01`→`MSG-05` (5) · `WKF-01`→`WKF-03` (3) · `PEC-01`→`PEC-05` (5) · `TRF-01`→`TRF-06` (6) · `TPS-01`→`TPS-05` (5) · `SLA-01`→`SLA-05` (5) · `ALE-01`→`ALE-03` (3) · `SUP-01`→`SUP-05` (5) · `DIS-01`→`DIS-03` (3) · `AFF-01`→`AFF-07` (7) · `PRI-01`→`PRI-04` (4) · `MAS-01`→`MAS-02` (2)

> **Vérification** : la matrice de couverture de `spec.md` contient 84 identifiants, dont 10 `PC-xx` (points à clarifier, pas des exigences). **84 − 10 = 74**, ce qui correspond exactement au « 74 lignes » de la `closure-review.md`.
> **Réserve** : `SOC-14` et `PRI-02` sont classées **« PARTIEL ACCEPTÉ »** par la closure elle-même. **DELTA CERTIFIÉ STRICT F001 = +72.**

### F002 — Comptes Clients & Sociétés : **11 exigences**
`CPT-01`→`CPT-07` (7) · `ENT-01`→`ENT-04` (4) — **DELTA CERTIFIÉ = +11**

### F003 — Billing Office IMPORT : **24 exigences**
`IMP-01`→`06` (6) · `IMP-10`→`15` (6) · `IMP-20`→`22` (3) · `IMP-30`→`32` (3) · `IMP-40`→`42` (3) · `UNI-01`→`UNI-03` (3)
> **Réserve** : la closure déclare `IMP-03` **« PARTIELLE assumée (portion e-mail hors périmètre) »** tout en totalisant 24/24. **DELTA CERTIFIÉ STRICT = +23.**
> `UNI-04` est explicitement déclarée « correctement absente » (réservée à F004) — **aucun double comptage.**

### F003.5 — Consolidation UI/UX : **1 exigence**
`ACR-01` — **DELTA CERTIFIÉ = +1** (les apports NFR-01/NFR-02 ne sont pas comptés, cf. §5)

### F004 — Billing Office EXPORT : **5 exigences**
`EXP-01`→`EXP-04` (4) · `UNI-04` (1) — **DELTA CERTIFIÉ = +5**

### F005 — Simulation Proforma : **10 exigences**
`SIM-01`→`SIM-10` (10) · `SIM-11` **DIFFÉRÉE** — **DELTA CERTIFIÉ = +10**

### F006 — Customer Complaint : **6 revendiquées, 4 certifiées**
`REC-01`→`REC-06` (6)
> **Contradiction interne** : la closure additionne +6 alors que son propre bilan écrit *« Bilan REC : 4/6 CONFORME, 2/6 PARTIEL (REC-02, REC-03) — pas 6/6 comme annoncé »*. **DELTA CERTIFIÉ STRICT = +4.**

### F007 — Avoir & Exonération : **4 exigences**
`AVE-01`→`AVE-04` (4) — **DELTA CERTIFIÉ = +4**

### F008 — Enrôlement Transporteurs : **3 exigences**
`ENR-01`→`ENR-03` (3) · `ENR-04`, `ENR-05` **DIFFÉRÉES** — **DELTA CERTIFIÉ = +3**

### F009 — Notifications & Centre In-App : **8 exigences**
`COM-01`→`COM-05` (5) · `NOT-01`→`NOT-03` (3) · `COM-06` **DIFFÉRÉE** — **DELTA CERTIFIÉ = +8**

### F010 — Continuité Guichet & Rendez-vous : **6 revendiquées, 5 nouvelles**
`CON-01`→`CON-04` (4) · `ERR-04` (1) · **`IMP-15` (1) — DÉJÀ COMPTÉE PAR F003**
> **DELTA CERTIFIÉ STRICT = +5.**

---

## 3. Les 188 exigences — table nominative

> Légende des preuves : `CL` = closure-review · `AU` = audit post-implémentation · `T` = test automatisé · `C` = code identifié · `REC` = scénario de recette

### Famille SOC — Socle (14)

| ID | Prio | Description courte | Feature | Statut | Preuve | Dette / Clarification |
|:---|:---:|:---|:---:|:---|:---|:---|
| SOC-01 | Must | Ticket à numéro unique communiqué (portail + e-mail) | F001 | **CONFORME** | CL F001 · T | — |
| SOC-02 | Must | Le ticket centralise documents, statut, historique | F001 | **CONFORME** | CL F001 · T | — |
| SOC-03 | Must | Suivi du ticket par le client | F001 | **CONFORME** | CL F001 · T | — |
| SOC-04 | Must | Files de traitement par rôle | F001 | **CONFORME** | CL F001 · T | — |
| SOC-05 | Must | Fil de commentaires horodaté par ticket | F001 | **CONFORME** | CL F001 · T | — |
| SOC-06 | Must | Actions workflow : valider, rejeter, complément | F001 | **CONFORME** | CL F001 · T | — |
| SOC-07 | Must | Dépôt de documents paramétrable par étape | F001 | **CONFORME** | CL F001 · T | — |
| SOC-08 | Must | Statuts visibles en temps réel par le client | F001 | **CONFORME** | CL F001 · T | — |
| SOC-09 | Must | Archivage automatique à la clôture | F001 | **CONFORME** | CL F001 · T | — |
| SOC-10 | Must | Liaison entre tickets | F001 | **CONFORME** | CL F001 · T | — |
| SOC-11 | Must | Dossier regroupant les tickets d'une demande | F001 | **CONFORME** | CL F001 · T | — |
| SOC-12 | Must | Distinction arrivée en file / prise en charge | F001 | **CONFORME** | CL F001 · T | — |
| SOC-13 | Must | Journal d'audit inviolable | F001 | **CONFORME** | CL F001 · T | Dette D-A1 (append-only applicatif) |
| **SOC-14** | Could | Export des données | F001 | **PARTIELLE** | CL F001 : « PARTIEL ACCEPTÉ, sans test dédié » | Dette D-F1 |

### Famille CYC — Cycle de vie (7) · WKF (3) · PEC (5) · MSG (5)

| ID | Prio | Description courte | Feature | Statut | Preuve |
|:---|:---:|:---|:---:|:---|:---|
| CYC-01→CYC-07 | Must ×5, Should ×2 | Annulation, relances, suggestion de clôture, réouverture | F001 | **CONFORME** (7) | CL F001 · T |
| WKF-01→WKF-03 | Must | Workflow par type de client, transitions, traçabilité | F001 | **CONFORME** (3) | CL F001 · T |
| PEC-01→PEC-05 | Must ×4, Should ×1 | Prise en charge, horodatages, délai de PEC | F001 | **CONFORME** (5) | CL F001 · T |
| MSG-01→MSG-05 | Must ×4, Should ×1 | Messages client/agent, notes internes, mentions | F001 | **CONFORME** (5) | CL F001 · T · AU F009 (MSG-04 raccordée) |

### Famille TRF (6) · TPS (5) · SLA (5) · ALE (3) · SUP (5) · DIS (3) · AFF (7) · PRI (4) · MAS (2)

| ID | Prio | Description courte | Feature | Statut | Preuve |
|:---|:---:|:---|:---:|:---|:---|
| TRF-01→TRF-06 | Must ×5, Should ×1 | Transferts de file, réaffectation, motif obligatoire | F001 | **CONFORME** (6) | CL F001 · T · REC-SCE-10 |
| TPS-01→TPS-05 | Must ×4, Should ×1 | Mesure des temps, temps interne / temps client | F001 | **CONFORME** (5) | CL F001 · T · REC-SCE-12 |
| SLA-01→SLA-05 | Must ×4, Should ×1 | SLA paramétrables, seuils, suspension | F001 | **CONFORME** (5) | CL F001 · T · REC-SCE-12 |
| ALE-01→ALE-03 | Must | Alertes, seuils, escalade | F001 | **CONFORME** (3) | CL F001 · T · REC-SCE-13 |
| SUP-01→SUP-05 | Must | Vues temps réel, actions superviseur | F001 | **CONFORME** (5) | CL F001 · T · REC-SCE-13 |
| DIS-01→DIS-03 | Must ×2, Should ×1 | Disponibilité des opérateurs | F001 | **CONFORME** (3) | CL F001 · T |
| AFF-01→AFF-07 | Must ×5, Should ×2 | Règles d'affectation, compétences | F001 | **CONFORME** (7) | CL F001 · T |
| PRI-01, PRI-03, PRI-04 | Must ×3 | Niveaux de priorité, modification tracée | F001 | **CONFORME** (3) | CL F001 · T |
| **PRI-02** | **Must** | Priorité déductible de critères | F001 | **PARTIELLE** | CL F001 : « PARTIEL ACCEPTÉ, comportement non démontré » — Dette D-F2 |
| MAS-01, MAS-02 | Should ×1, Could ×1 | Actions de masse | F001 | **CONFORME** (2) | CL F001 · T |

### Famille CPT (7) · ENT (4)

| ID | Prio | Description courte | Feature | Statut | Preuve |
|:---|:---:|:---|:---:|:---|:---|
| CPT-01→CPT-07 | Must ×5, Should ×1, Could ×1 | Création validée, profil, authentification, cloisonnement | F002 | **CONFORME** (7) | CL F002 · T |
| ENT-01→ENT-04 | Must ×2, Should ×2 | Compte entreprise multi-utilisateurs, droits | F002 | **CONFORME** (4) | CL F002 · T |

### Famille IMP (21) · UNI (4) · EXP (4)

| ID | Prio | Description courte | Feature | Statut | Preuve |
|:---|:---:|:---|:---:|:---|:---|
| IMP-01, IMP-02, IMP-04→IMP-06 | Must | Soumission, unicité BL, contrôle manifeste | F003 | **CONFORME** (5) | CL F003 · T |
| **IMP-03** | **Must** | Numéros de ticket sur profil **et par e-mail** | F003 | **PARTIELLE** | CL F003 : « PARTIELLE assumée, portion e-mail hors périmètre ». Le canal e-mail existe depuis F009 mais **n'a jamais été recertifié sur IMP-03** |
| IMP-10→IMP-14 | Must | Circuit client normal complet | F003 | **CONFORME** (5) | CL F003 · T |
| **IMP-15** | Should | Alternative présentielle | **F003** | **CONFORME** | CL F003 (`DeclarationPresentielleTests`, 3 tests). **Re-revendiquée par F010 — double comptage, cf. §4** |
| IMP-20→IMP-22, IMP-30→IMP-32, IMP-40→IMP-42 | Must | Circuits en compte / deposit / IES | F003 | **CONFORME** (9) | CL F003 · T |
| UNI-01→UNI-03 | Must ×3 | Unicité BL actif | F003 | **CONFORME** (3) | CL F003 · T |
| UNI-04 | Should | Unicité booking actif | F004 | **CONFORME** | CL F004 (CLAR-BOOK-01) |
| EXP-01→EXP-04 | Must | Parcours EXPORT complet | F004 | **CONFORME** (4) | CL F004 · T |

### Famille SIM (11) · REC (6) · ENR (5) · AVE (4) · ACR (1)

| ID | Prio | Description courte | Feature | Statut | Preuve |
|:---|:---:|:---|:---:|:---|:---|
| SIM-01→SIM-10 | Must ×8, Should ×2 | Simulation proforma, grilles tarifaires | F005 | **CONFORME** (10) | CL F005 · T |
| **SIM-11** | Could | Validations croisées avancées | — | **DIFFÉRÉE** | CL F005 §14 : « non implémentée, tâche différée » |
| REC-01, REC-04, REC-05, REC-06 | Must ×3, Should ×1 | Réclamations : soumission, suivi, clôture, évaluation | F006 | **CONFORME** (4) | CL F006 · T · REC-SCE-07 |
| **REC-02** | **Must** | Catégories paramétrables sans développement | F006 | **PARTIELLE** | CL F006 : « 4/6 CONFORME, 2/6 PARTIEL » — Dette D-F3 |
| **REC-03** | **Must** | Orientation Support Opérationnel, instruction | F006 | **PARTIELLE** | idem |
| ENR-01→ENR-03 | Must | Dossier à distance, instruction, convocation | F008 | **CONFORME** (3) | CL F008 · T · REC-SCE-08 |
| **ENR-04** | Should | Notification chauffeur SMS/WhatsApp | — | **DIFFÉRÉE** | CL F008 — bloquée par **PC-16** |
| **ENR-05** | Could | Renouvellement de badge avec rappel | — | **DIFFÉRÉE** | CL F008 |
| AVE-01→AVE-04 | Must ×3, Should ×1 | Demande, validation, décision, traçabilité Avoir/Exo | F007 | **CONFORME** (4) | CL F007 · T |
| ACR-01 | Must | Bloc « Mes actions requises » | F003.5 | **CONFORME** | CL F003.5 · T |

### Famille COM (6) · NOT (3) · CON (4)

| ID | Prio | Description courte | Feature | Statut | Preuve |
|:---|:---:|:---|:---:|:---|:---|
| COM-01→COM-05 | Must ×4, Should ×1 | Notification multicanale, lien, modèles, chat, préférences | F009 | **CONFORME** (5) | CL F009 · AU F009 · T (TEST-NOT-01/02, TEST-MSG-04) |
| **COM-06** | Could | Canaux des relances automatiques | — | **DIFFÉRÉE** | AU F009 : aucune règle semée, `TEST-COM-06` (4 cas) non écrits |
| NOT-01→NOT-03 | Must ×2, Should ×1 | État Lu/Non lu, règles de diffusion, indicateur | F009 | **CONFORME** (3) | CL F009 · AU F009 · T |
| CON-01→CON-04 | Must ×3, Should ×1 | Reprise guichet, recherche, rendez-vous, file physique | F010 | **CONFORME** (4) | CL F010 · T |

### Famille SEC (12) — **AUCUNE n'a jamais été comptée dans un delta de Feature**

| ID | Prio | Description courte | Feature | Statut | Preuve / Constat |
|:---|:---:|:---|:---:|:---|:---|
| **SEC-01** | Must | Authentification obligatoire, politique de mot de passe | *(transverse)* | **PARTIELLE** | **C + 4 tests nommés** — jamais comptée |
| **SEC-02** | Must | RBAC, moindre privilège | *(transverse)* | **PARTIELLE** | **C + 1 test nommé** — jamais comptée |
| **SEC-03** | Must | Cloisonnement strict client / agent | *(transverse)* | **PARTIELLE** | **C**, massivement testé sous l'angle CPT-04 — jamais comptée sous SEC-03 |
| **SEC-04** | Must | Chiffrement des échanges (HTTPS/TLS) | — | **À CLARIFIER** | Relève de l'hébergement — **PC-13** |
| **SEC-05** | Must | Journal d'audit non modifiable | *(transverse)* | **PARTIELLE** | **C + 2 tests** · `JournalAuditAppendOnlyGuard` — Dette D-A1 |
| **SEC-06** | Must | Protection des données personnelles | *(transverse)* | **À CLARIFIER** | **PC-14** (conservation, ARTCI) |
| **SEC-07** | Must | Contrôle des fichiers déposés (type, taille, antivirus) | *(transverse)* | **PARTIELLE** | **C** : `ReglesDepotDocument` + abstraction antivirus, consommés par F003/F004/F006/F008 |
| **SEC-08** | Should | Second facteur agents/administrateurs | *(transverse)* | **PARTIELLE** | **C** : `ActiverDoubleAuthentification.razor`, `ParcoursConnexion2FaTests` — Dette D-T1 (instable en parallèle) |
| **SEC-09** | Should | Cloisonnement des environnements | — | **ABSENTE** | Aucun artefact |
| **SEC-10** | Must | Expiration de session paramétrable | *(transverse)* | **PARTIELLE** | **C** : `Program.cs:30-31` — `ExpireTimeSpan` piloté par `Securite:ExpirationSessionMinutes` |
| **SEC-11** | Must | Désactivation immédiate d'un compte agent | *(transverse)* | **PARTIELLE** | **C** : `ApplicationUser.EstActif` + 1 test |
| **SEC-12** | Must | Lien de notification jamais un accès direct | *(transverse)* | **PARTIELLE** | **C + 2 tests** · certifiée par l'AU F009 mais **explicitement non comptée** |

### Famille ERR (4)

| ID | Prio | Description courte | Feature | Statut | Preuve / Constat |
|:---|:---:|:---|:---:|:---|:---|
| **ERR-01** | Must | Aucune perte de saisie (brouillon / reprise) | — | **ABSENTE** | **0 occurrence de « brouillon »** dans `src/` |
| **ERR-02** | Must | Reprise d'envoi de notification, traçage, isolation | *(transverse)* | **PARTIELLE** | **C + 6 tests** (TEST-ERR-02) mais **fail-fast absent** — Dette D9-6, BLOCKER PROD |
| **ERR-03** | Must | Vérification des documents à l'upload | *(transverse)* | **PARTIELLE** | **C** F001/F003 — jamais comptée |
| ERR-04 | Should | Mode dégradé documenté | F010 | **CONFORME** | CL F010 · T |

### Famille NFR (11)

| ID | Prio | Description courte | Feature | Statut | Preuve / Constat |
|:---|:---:|:---|:---:|:---|:---|
| **NFR-01** | Must | Interface en français, ergonomie simple | *(F003.5)* | **PARTIELLE** | **C** — UI intégralement livrée, **jamais certifiée sous NFR-01** |
| **NFR-02** | Must | Portail responsive mobile | *(F003.5)* | **PARTIELLE** | **C** — livré, `docs/06` — jamais certifiée sous NFR-02 |
| **NFR-03** | Must | Disponibilité ≥ 99 % en heures ouvrées | — | **À CLARIFIER** | Exploitation — non vérifiable en code |
| **NFR-04** | Must | Temps de réponse < 3 s | — | **À CLARIFIER** | **PC-01** — aucun test de charge |
| **NFR-05** | Must | Volumétrie dimensionnante | — | **À CLARIFIER** | **PC-01** — « à confirmer en cadrage » |
| **NFR-06** | Must | Sauvegardes et restauration testée | — | **À CLARIFIER** | Exploitation — **PC-13** |
| **NFR-07** | Should | Interface en anglais | — | **ABSENTE** | Aucune localisation |
| **NFR-08** | Should | Architecture ouverte exposant des API | — | **ABSENTE** | Aucune API publique |
| **NFR-09** | Should | Hébergement conforme au groupe | — | **À CLARIFIER** | **PC-13** |
| **NFR-10** | Must | Accessible 24/7 hors maintenance | — | **À CLARIFIER** | Exploitation |
| **NFR-11** | Must | Horaires de fonctionnement pour le calcul des SLA | *(F001)* | **PARTIELLE** | **C** : `Domain/SuiviTemps/CalendrierOuvre.cs`, `GererParametresSlaCommand` — jamais certifiée |

### Famille IA (7)

| ID | Prio | Statut | Constat |
|:---|:---:|:---|:---|
| IA-01, IA-02, IA-03, IA-06, IA-07 | Must (5) | **ABSENTE** | Aucun code. Bloquée par **PC-18** (souveraineté, gouvernance) |
| IA-04, IA-05 | Should (2) | **ABSENTE** | idem |

---

## 4. Analyse de l'écart 152 / 146 — **RÉSOLU**

Le document `docs/16` annonçait un écart **+4** (156 vs 152). **Ce chiffre était faux** : il provenait d'un calcul par soustraction (`188 − 32 non-conformes`), méthode explicitement proscrite par R0. Le dénombrement par comptage de lignes donne un résultat **inverse** : la baseline stricte est **inférieure** à la baseline historique.

**ÉCART RÉEL : 152 − 146 = −6 exigences.** Les six sont nommément identifiées.

| # | ID | Priorité | Statut historique | Statut R0 | Preuve | Cause |
|:--|:---|:---:|:---|:---|:---|:---|
| **ÉCART-01** | **IMP-15** | Should | CONFORME ×2 | **CONFORME ×1** | CL F003 (`DeclarationPresentielleTests`) **et** CL F010 | **Double comptage.** F003 la certifie CONFORME ; F010 la recompte dans son delta +6. Le delta réel de F010 est **+5** |
| **ÉCART-02** | **SOC-14** | Could | comptée CONFORME | **PARTIELLE** | CL F001 §2 : « PARTIEL ACCEPTÉ — sans test dédié, différable » | Exigence comptée dans les 74 malgré son propre classement PARTIEL |
| **ÉCART-03** | **PRI-02** | **Must** | comptée CONFORME | **PARTIELLE** | CL F001 §2 : « PARTIEL ACCEPTÉ — libellé permissif *peut*, comportement non démontré » | idem |
| **ÉCART-04** | **IMP-03** | **Must** | comptée CONFORME | **PARTIELLE** | CL F003 : « 1 exigence PARTIELLE assumée (IMP-03, portion e-mail hors périmètre) » | Comptée dans les 24/24 malgré la réserve. Le canal e-mail existe depuis F009 mais IMP-03 n'a jamais été recertifiée |
| **ÉCART-05** | **REC-02** | **Must** | comptée CONFORME | **PARTIELLE** | CL F006 : « Bilan REC : 4/6 CONFORME, 2/6 PARTIEL — **pas 6/6 comme annoncé** » | Delta +6 additionné alors que le bilan interne en démontre 4 |
| **ÉCART-06** | **REC-03** | **Must** | comptée CONFORME | **PARTIELLE** | idem | idem |

**ÉCART RÉSOLU : OUI.** Les six exigences responsables sont **IMP-15, SOC-14, PRI-02, IMP-03, REC-02, REC-03**.

> **Nature de l'écart** : ce n'est ni une fraude ni une erreur de développement. Cinq cas sur six sont des **réserves correctement documentées par les Features elles-mêmes**, mais **agrégées comme conformes** dans le cumul. Le sixième (IMP-15) est un authentique double comptage. Aucune Feature n'est requalifiée par ce document.

---

## 5. Capacités existantes non certifiées

> Fonctionnalités présentes dans le code, souvent testées, **jamais comptées dans le delta d'une Feature**.

| Exigence | Prio | Code existant | Test existant | Recette | Feature d'origine | Déjà comptée dans les 152 ? | Certifiable sans code ? | Travail manquant |
|:---|:---:|:---|:---|:---|:---|:---:|:---:|:---|
| **SEC-01** | **Must** | Identity + politique de mot de passe | **4 tests nommés** | REC-SCE-04/14 | F001/F002 | **NON** | **OUI** | Certification nominative |
| **SEC-02** | **Must** | `IAutorisationService`, `Permission` | **1 test nommé** | REC-SCE-14 | F001 | **NON** | **OUI** | Certification |
| **SEC-03** | **Must** | `CloisonnementService` | testé sous CPT-04 | REC-SCE-14 | F001/F002 | **NON** | **OUI** | Rattachement + certification |
| **SEC-05** | **Must** | `JournalAuditAppendOnlyGuard` | **2 tests** | REC-SCE-14 | F001 | **NON** | **OUI** | Certification (dette D-A1 à part) |
| **SEC-07** | **Must** | `ReglesDepotDocument` + antivirus | indirect | — | F001 | **NON** | **OUI** | Test dédié + certification |
| **SEC-08** | Should | 2FA Identity + écran | `ParcoursConnexion2FaTests` | — | hors Feature | **NON** | **OUI** | Stabilisation (D-T1) + certification |
| **SEC-10** | **Must** | `Program.cs:30-31` (config) | aucun | — | hors Feature | **NON** | **OUI** | Test dédié + certification |
| **SEC-11** | **Must** | `ApplicationUser.EstActif` | **1 test** | — | F002 | **NON** | **OUI** | Test « sans perte d'historique » |
| **SEC-12** | **Must** | Lien relatif sans jeton | **2 tests** | REC-SCE-14 | F009 | **NON** (explicitement) | **OUI** | Certification |
| **ERR-02** | **Must** | Moteur de reprise F009 | **6 tests** | — | F009 | **NON** (explicitement) | **NON** | Fail-fast (D9-6) puis certification |
| **ERR-03** | **Must** | Contrôle upload F001/F003 | indirect | — | F001 | **NON** | **OUI** | Test dédié + certification |
| **NFR-01** | **Must** | UI française intégrale | indirect | — | F003.5 | **NON** | **OUI** | Certification |
| **NFR-02** | **Must** | Responsive | `docs/06` | — | F003.5 | **NON** | **OUI** | Preuve responsive + certification |
| **NFR-11** | **Must** | `CalendrierOuvre` + SLA | indirect | — | F001 | **NON** | **OUI** | Test « dépôt hors heures ouvrées » |

### Correction de l'affirmation de `docs/16`

`docs/16` affirmait : *« cinq Must sont déjà servies »*. **Ce nombre est faux — il est sous-estimé.**

**Nombre exact de Must déjà implémentées mais non certifiées : 12**
`SEC-01`, `SEC-02`, `SEC-03`, `SEC-05`, `SEC-07`, `SEC-10`, `SEC-11`, `SEC-12`, `ERR-02`, `ERR-03`, `NFR-01`, `NFR-02`, `NFR-11` → **13 au total**, dont **12 Must** et 1 Should (`SEC-08`).

*(Décompte : 13 exigences listées, dont `SEC-08` est Should ; les 12 autres sont Must.)*

---

## 6. REC-SCE — réconciliation F009 / F010

Le CDC ne numérote **aucune étape** dans REC-SCE-14 : il énonce **cinq volets** séparés par des points-virgules. Toute mention d'un « 14/14 » repose donc sur une numérotation qui **n'existe pas au CDC**.

| Volet (citation littérale) | Rattachement | A. Fonctionnel | B. Tests unitaires/intégration | C. E2E transverse |
|:---|:---|:---:|:---:|:---:|
| *« Notifications sur chaque canal configuré »* | F009 | ✅ | ✅ | ❌ |
| *« …avec lien vers le ticket »* | F009 | ✅ | ✅ | ❌ |
| *« continuité digital/présentiel par recherche de ticket au guichet »* | F010 | ✅ | ✅ | ✅ |
| *« …et rendez-vous donnant accès au bureau de facturation »* | F010 | ✅ | ✅ | ✅ |
| *« regroupement des tickets d'une même demande en dossier »* | F001 | ✅ | ✅ | ✅ |
| *« contrôle des habilitations »* | F009 + socle | ✅ | ✅ | ⚠️ partiel (volet notifications absent) |
| *« journal d'audit »* | F009 + socle | ✅ | ✅ | ⚠️ partiel (volet notifications absent) |

**Constat vérifié** : `RecSce14NotificationsE2ETests.cs` **n'existe pas**.

```
REC-SCE-14 FONCTIONNEL : COUVERT (7 volets sur 7)
REC-SCE-14 TESTS       : COUVERT (unitaire + intégration, toutes couches)
REC-SCE-14 E2E         : PARTIEL — 3 volets sur 7 rejoués en E2E (tous côté F010)
STATUT CONTRACTUEL     : PARTIEL
DETTE                  : D9-3 — aucun parcours E2E transverse ne rejoue les volets
                         notifications. L'annonce « 14/14 » de F010 n'est pas soutenable :
                         elle confond couverture fonctionnelle et couverture E2E, et
                         s'appuie sur une numérotation d'étapes absente du CDC.
```

---

## 7. Baseline officielle R0

| Statut | Nombre | Vérification |
|:---|:---:|:---|
| **CONFORME** | **146** | comptage de lignes |
| **PARTIELLE** | **19** | comptage de lignes |
| **DIFFÉRÉE** | **4** | SIM-11, ENR-04, ENR-05, COM-06 |
| **ABSENTE** | **11** | IA-01→07 (7), ERR-01, SEC-09, NFR-07, NFR-08 |
| **À CLARIFIER** | **8** | SEC-04, SEC-06, NFR-03, NFR-04, NFR-05, NFR-06, NFR-09, NFR-10 |
| **TOTAL** | **188** | ✅ 146 + 19 + 4 + 11 + 8 = 188 |

**Détail des 19 PARTIELLE** : SOC-14 · PRI-02 · IMP-03 · REC-02 · REC-03 · SEC-01 · SEC-02 · SEC-03 · SEC-05 · SEC-07 · SEC-08 · SEC-10 · SEC-11 · SEC-12 · ERR-02 · ERR-03 · NFR-01 · NFR-02 · NFR-11

### **BASELINE OFFICIELLE : 146 / 188 = 77,66 %**

> La baseline historique de **152/188 est retirée**. Elle surévaluait la couverture de 6 exigences, toutes nommément identifiées au §4.

---

## 8. Exigences restantes — 42

| Statut | Nb | Must | Should | Could |
|:---|:---:|:---:|:---:|:---:|
| PARTIELLE | 19 | 16 | 2 | 1 |
| DIFFÉRÉE | 4 | 0 | 1 | 3 |
| ABSENTE | 11 | 6 | 5 | 0 |
| À CLARIFIER | 8 | 6 | 2 | 0 |
| **TOTAL** | **42** | **28** | **10** | **4** |

---

## 9. Périmètre reconstruit de la Feature candidate

Le périmètre n'est **pas** présupposé à 10 exigences. Il est construit par filtrage strict : exigences **non conformes**, **certifiables**, **cohérentes fonctionnellement**, **non déjà comptées**, **non bloquées par une clarification**.

**Exclusions motivées** :
- `SEC-04`, `SEC-06`, `NFR-03`→`NFR-06`, `NFR-09`, `NFR-10` → **bloquées** par PC-13, PC-14, PC-01
- `ERR-02` → nécessite d'abord la correction du fail-fast (D9-6) — **inclus, avec code à produire**
- `REC-02`, `REC-03` → cohérence fonctionnelle différente (module Réclamations) → **feature de remédiation séparée**
- `SOC-14`, `PRI-02`, `IMP-03` → dettes ponctuelles hétérogènes → **feature de remédiation séparée**
- `IA-*`, `ENR-04/05`, `COM-06`, `SIM-11` → bloquées ou différées

```
FEATURE CANDIDATE : Certification Sécurité & Socle Non-Fonctionnel

EXIGENCES EXACTES (14) :
  SEC-01  SEC-02  SEC-03  SEC-05  SEC-07  SEC-08  SEC-10  SEC-11  SEC-12
  ERR-02  ERR-03
  NFR-01  NFR-02  NFR-11

MUST   : 13  (SEC-01, 02, 03, 05, 07, 10, 11, 12 · ERR-02, ERR-03 · NFR-01, 02, 11)
SHOULD :  1  (SEC-08)
COULD  :  0

DÉJÀ IMPLÉMENTÉES : 14 / 14  (aucune construction fonctionnelle nouvelle)

CODE À PRODUIRE :
  - fail-fast de la configuration de reprise (ERR-02, dette D9-6) — ValidateOnStart
  - stabilisation du harnais 2FA (SEC-08, dette D-T1)
  → 2 corrections ciblées, aucune fonctionnalité nouvelle

TESTS À PRODUIRE :
  - SEC-03 : rattachement nominatif du cloisonnement (aujourd'hui testé sous CPT-04)
  - SEC-07 : test dédié type/taille/antivirus
  - SEC-10 : test d'expiration de session
  - SEC-11 : test « désactivation sans perte d'historique »
  - ERR-03 : test dédié de vérification à l'upload
  - NFR-11 : test « dépôt hors heures ouvrées »
  - NFR-02 : preuve responsive automatisée

RECETTES À PRODUIRE :
  - scénario TEST-SPEC de certification sécurité transverse
  - preuve NFR-01 (français) et NFR-02 (responsive)

GAIN STRICT DE COUVERTURE : +14
COUVERTURE APRÈS FEATURE  : 160 / 188 = 85,11 %
```

> **Option complémentaire** — une Feature de remédiation (`SOC-14`, `PRI-02`, `IMP-03`, `REC-02`, `REC-03` + `COM-06` + REC-SCE-14 E2E) apporterait **+6** supplémentaires → **166/188 (88,3 %)**.

---

## 10. Roadmap actualisée jusqu'à 188/188

```
Certification Sécurité & Socle NFR        +14  → 160/188  (85,1 %)
        v
Remédiation dettes & recette transverse    +6  → 166/188  (88,3 %)
        v
Assistant IA          [après PC-18]        +7  → 173/188  (92,0 %)
        v
Canaux mobiles        [après PC-16]        +2  → 175/188  (93,1 %)
   + SIM-11 (Could)                        +1  → 176/188
        v
Production Readiness  [après PC-01/13/14] +10  → 186/188  (98,9 %)
   (SEC-04, SEC-06, SEC-09, NFR-03→10)
        v
Reliquat : NFR-07 (anglais), NFR-08 (API)  +2  → 188/188  (100 %)
```

---

```
R0 — BASELINE NOMINATIVE CDC

TOTAL IDS : 188

CONFORMES : 146
PARTIELLES : 19
DIFFÉRÉES : 4
ABSENTES : 11
À CLARIFIER : 8

TOTAL CONTRÔLE : 146 + 19 + 4 + 11 + 8 = 188 ✅

BASELINE HISTORIQUE :
152/188 (80,85 %)

BASELINE RECONSTRUITE :
146/188 (77,66 %)

ÉCART : −6 exigences (la baseline historique SURÉVALUAIT la couverture)
RÉSOLU : OUI

IDS RESPONSABLES DE L'ÉCART :
- IMP-15  — double comptage F003 (CONFORME) + F010 (recomptée dans son delta +6)
- SOC-14  — « PARTIEL ACCEPTÉ » en F001, agrégée comme conforme
- PRI-02  — « PARTIEL ACCEPTÉ » en F001, agrégée comme conforme
- IMP-03  — « PARTIELLE assumée » en F003, agrégée comme conforme
- REC-02  — PARTIEL selon le bilan interne de F006, agrégée comme conforme
- REC-03  — PARTIEL selon le bilan interne de F006, agrégée comme conforme

NOTE : le « +4 » annoncé par docs/16 était FAUX. Il provenait d'un calcul par
       soustraction (188 − 32), méthode proscrite par R0. Le comptage par ligne
       donne un écart de sens inverse.

F006 :
DELTA HISTORIQUE : +6 (REC-01 → REC-06)
DELTA STRICT : +4 (REC-01, REC-04, REC-05, REC-06)
ÉCART : −2 (REC-02, REC-03)
CAUSE : la closure additionne 6 alors que son propre bilan écrit
        « 4/6 CONFORME, 2/6 PARTIEL — pas 6/6 comme annoncé »

REC-SCE-14 :
FONCTIONNEL : COUVERT (7 volets / 7)
TESTS : COUVERT (unitaire + intégration)
E2E : PARTIEL (3 volets / 7 — tous côté F010)
STATUT : PARTIEL — l'annonce « 14/14 » n'est pas soutenable ; le CDC ne définit
         aucune numérotation d'étapes pour ce scénario

CAPACITÉS EXISTANTES NON CERTIFIÉES :
- SEC-01, SEC-02, SEC-03, SEC-05, SEC-07, SEC-08, SEC-10, SEC-11, SEC-12
- ERR-02, ERR-03
- NFR-01, NFR-02, NFR-11

NOMBRE EXACT DE MUST DÉJÀ IMPLÉMENTÉES MAIS NON CERTIFIÉES : 12
  (13 exigences au total, dont SEC-08 qui est Should)
  L'affirmation « cinq Must » de docs/16 était SOUS-ESTIMÉE.

EXIGENCES RESTANTES STRICTES : 42
  PARTIELLE 19 · DIFFÉRÉE 4 · ABSENTE 11 · À CLARIFIER 8
  (28 Must, 10 Should, 4 Could)

PROCHAINE FEATURE RECOMMANDÉE : Certification Sécurité & Socle Non-Fonctionnel
EXIGENCES : SEC-01, SEC-02, SEC-03, SEC-05, SEC-07, SEC-08, SEC-10, SEC-11,
            SEC-12, ERR-02, ERR-03, NFR-01, NFR-02, NFR-11  (14 — dont 13 Must)
GAIN : +14
COUVERTURE PROJETÉE : 160/188 (85,11 %)

DOCUMENT :
docs/17-baseline-nominative-188-exigences.md

CODE MODIFIÉ :
NON

MIGRATION :
NON

COMMIT :
NON

PUSH :
NON

VERDICT :
READY FOR FEATURE SCOPE VALIDATION
```
