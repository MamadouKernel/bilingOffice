# Audit Global Post-F007 — Plateforme Digitale CIT

> **Document** : `docs/16-audit-global-post-f007.md`
> **Date** : 1er septembre 2026
> **Nature** : audit de traçabilité, de couverture et de pilotage. **Aucune modification de code, de test, de migration ni de configuration. Aucun commit, aucun push. Aucune Feature lancée.**
> **Hiérarchie des sources appliquée** : **CDC > analyse > matrice > spec > code.** Toute contradiction est documentée, jamais corrigée silencieusement.

---

## 1. Executive Summary

La plateforme a clôturé onze Features (F001 → F010, dont F003.5). La baseline technique est **saine et reproductible en exécution sérialisée** : 818/818 tests verts, 0 avertissement, 0 changement de modèle en attente.

**Trois constats structurent cet audit :**

1. **La couverture annoncée de 152/188 n'est pas mécaniquement reconstructible.** La chaîne de progression documentée (110 → 115 → 125 → 131 → 134 → 142 → 148 → 152) est cohérente et traçable dans les `closure-review.md`, mais les formats de déclaration sont hétérogènes d'une Feature à l'autre : **aucun artefact ne porte la liste nominative des 152 exigences conformes**. Mon dénombrement indépendant des non-conformes en identifie **32**, ce qui donnerait 156 — soit un **écart de 4 exigences non expliqué**, dans le sens *favorable* au projet.

2. **Cinq exigences Must sont techniquement présentes dans le code mais n'ont jamais été certifiées** contre le CDC : `SEC-07`, `SEC-10`, `SEC-11`, `NFR-11` et, dans une moindre mesure, `SEC-08` (Should). Ce sont des **capacités fantômes** : le travail est fait, la preuve contractuelle manque. C'est le gisement de couverture le moins cher du projet.

3. **Quatre exigences comptées comme conformes sont classées PARTIEL par leurs propres audits** : `REC-02`, `REC-03` (F006), `SOC-14`, `PRI-02` (F001). Elles ne doivent pas être requalifiées ici sans preuve, mais elles fragilisent le chiffre de 152.

**Recommandation** : la prochaine Feature **n'est pas l'Assistant IA**, malgré ses 5 Must. C'est une **Feature de certification Sécurité & Socle NFR**, qui transforme du code existant non certifié en couverture démontrée, à effort faible et risque quasi nul.

---

## 2. Baseline technique — vérifiée par exécution réelle

```
dotnet build
dotnet test --no-build -- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=1
dotnet ef migrations has-pending-model-changes --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

| Contrôle | Attendu | **Mesuré** | Statut |
|:---|:---|:---:|:---:|
| BUILD | succès | **succès** | ✅ |
| WARNINGS | 0 | **0** | ✅ |
| TESTS DOMAIN | — | **154 / 154** | ✅ |
| TESTS APPLICATION | — | **425 / 425** | ✅ |
| TESTS INFRASTRUCTURE | — | **83 / 83** | ✅ |
| TESTS WEB | — | **156 / 156** | ✅ |
| **TOTAL** | ≥ 818 | **818 / 818** | ✅ |
| PENDING MODEL CHANGES | 0 | **0** | ✅ |

**Gate franchi.** L'audit peut se poursuivre.

> **Observation reportée de l'audit F009** : la suite était instable en exécution **parallèle** (`ParcoursConnexion2FaTests` échouait 2 fois sur 3 en run solution). L'exécution **sérialisée** demandée ici est **stable**. L'instabilité parallèle n'a pas disparu — elle est contournée par le mode d'exécution. **Voir dette D-T1.**

---

## 3. Méthode

1. Extraction **mécanique** des identifiants d'exigences depuis `docs/01-matrice-exigences.md` (source de traçabilité officielle).
2. Contrôle du dénombrement contre les totaux auto-déclarés du document.
3. Rattachement Feature par Feature via les `closure-review.md`, `post-implementation-audit.md` et `spec.md`.
4. **Vérification par le code** des exigences suspectes ou jamais revendiquées (`grep` ciblé sur `src/`).
5. Recalcul indépendant, **sans reprendre 152/188 comme acquis**.

**Limite assumée de cet audit** : la certification nominative des ~150 exigences déclarées conformes n'a **pas** été refaite une par une. L'audit vérifie la **baseline**, les **non-conformes**, les **capacités non certifiées** et les **conformités suspectes** — pas la totalité du socle déjà clôturé et audité Feature par Feature. Ce périmètre est explicite et ne doit pas être présenté comme une recertification intégrale.

---

## 4. Reconstruction des 188 exigences

### 4.1 Dénombrement — **confirmé**

Extraction mécanique : **188 identifiants uniques**, aucun doublon.

| Priorité | Nombre |
|:---|:---:|
| **Must** | **150** |
| **Should** | **32** |
| **Could** | **6** |
| **TOTAL** | **188** |

Ces totaux correspondent exactement à ceux auto-déclarés par `docs/01-matrice-exigences.md` (§ recompte intégral). **Le chiffre de 188 est solide.**

### 4.2 Répartition par famille (30 familles)

| Famille | N | Famille | N | Famille | N |
|:---|:---:|:---|:---:|:---|:---:|
| IMP | 21 | CPT | 7 | ERR | 4 |
| SOC | 14 | AFF | 7 | ENT | 4 |
| SEC | 12 | TRF | 6 | CON | 4 |
| SIM | 11 | REC | 6 | AVE | 4 |
| NFR | 11 | COM | 6 | WKF | 3 |
| CYC | 7 | TPS · SLA · PEC · MSG · ENR · SUP | 5 ×6 | NOT · DIS · ALE | 3 ×3 |
| IA | 7 | UNI · PRI · EXP | 4 ×3 | MAS 2 · ACR 1 | 3 |

### 4.3 Exigences NON CONFORMES — établies par preuve

> Chaque ligne ci-dessous est étayée par une vérification directe (code, test, ou déclaration explicite d'un audit de Feature).

| ID | Famille | Priorité | Statut | Preuve |
|:---|:---|:---:|:---|:---|
| **IA-01** | IA | Must | **ABSENTE** | Aucun code. `spec.md` F009 §24 : « module distinct, hors périmètre » |
| **IA-02** | IA | Must | **ABSENTE** | idem |
| **IA-03** | IA | Must | **ABSENTE** | idem |
| **IA-04** | IA | Should | **ABSENTE** | idem |
| **IA-05** | IA | Should | **ABSENTE** | cité uniquement comme hors périmètre |
| **IA-06** | IA | Must | **ABSENTE** | Aucun code |
| **IA-07** | IA | Must | **ABSENTE** | Aucun code |
| **ERR-01** | ERR | Must | **ABSENTE** | *« Aucune perte de données saisies… brouillon ou reprise de saisie »* — **aucune occurrence de « brouillon »** dans `src/` |
| **SEC-09** | SEC | Should | **ABSENTE** | Cloisonnement des environnements — aucun artefact, jamais revendiquée |
| **NFR-07** | NFR | Should | **ABSENTE** | Interface en anglais — aucune localisation |
| **SEC-07** | SEC | **Must** | **PARTIELLE** | `ReglesDepotDocument` + abstraction antivirus **présents et utilisés** (F001, consommés par F003/F004/F006/F008) — **jamais certifiée** |
| **SEC-08** | SEC | Should | **PARTIELLE** | 2FA agents : `ActiverDoubleAuthentification.razor`, `ParcoursConnexion2FaTests`, Identity TwoFactor — **présente, jamais certifiée** |
| **SEC-10** | SEC | **Must** | **PARTIELLE** | `Program.cs:30-31` — `ExpireTimeSpan` **paramétrable** (`Securite:ExpirationSessionMinutes`) + `SlidingExpiration` — **jamais certifiée** |
| **SEC-11** | SEC | **Must** | **PARTIELLE** | `ApplicationUser.EstActif` — désactivation sans suppression d'historique — **jamais certifiée** |
| **NFR-01** | NFR | **Must** | **PARTIELLE** | Interface française livrée par F003.5 — **jamais certifiée comme NFR-01** |
| **NFR-02** | NFR | **Must** | **PARTIELLE** | Responsive livré par F003.5 — **jamais certifié comme NFR-02** |
| **NFR-11** | NFR | **Must** | **PARTIELLE** | `Domain/SuiviTemps/CalendrierOuvre.cs` + `GererParametresSlaCommand` — heures ouvrées SLA **implémentées**, **jamais certifiées** |
| **SOC-14** | SOC | Could | **PARTIELLE** | `closure-review.md` F001 §2 : « PARTIEL ACCEPTÉ — sans test dédié, différable » |
| **PRI-02** | PRI | **Must** | **PARTIELLE** | `closure-review.md` F001 §2 : « PARTIEL ACCEPTÉ — libellé permissif *peut*, donnée disponible » |
| **REC-02** | REC | **Must** | **PARTIELLE** | `closure-review.md` F006 : *« Bilan REC : 4/6 CONFORME, 2/6 PARTIEL (REC-02, REC-03) — pas 6/6 »* |
| **REC-03** | REC | **Must** | **PARTIELLE** | idem |
| **COM-06** | COM | Could | **DIFFÉRÉE** | Audit F009 : capacité présente, **aucune règle semée**, `TEST-COM-06` (4 cas) non écrits |
| **ENR-04** | ENR | Should | **DIFFÉRÉE** | Closure F008 — canal mobile, dépend de PC-16 |
| **ENR-05** | ENR | Could | **DIFFÉRÉE** | Closure F008 |
| **SIM-11** | SIM | Could | **DIFFÉRÉE** | `closure-review.md` F005 §14 : « non implémentée, tâche différée » |
| **NFR-03** | NFR | **Must** | **À CLARIFIER** | Disponibilité ≥ 99 % en heures ouvrées — **exigence d'exploitation**, non vérifiable en code |
| **NFR-04** | NFR | **Must** | **À CLARIFIER** | Temps de réponse < 3 s — aucun test de charge (PC-01) |
| **NFR-05** | NFR | **Must** | **À CLARIFIER** | Volumétrie *« à confirmer en phase de cadrage »* — **PC-01 ouvert** |
| **NFR-06** | NFR | **Must** | **À CLARIFIER** | Sauvegardes et restauration testée — relève de l'exploitation (PC-13) |
| **NFR-08** | NFR | Should | **À CLARIFIER** | Architecture ouverte / API — aucune API publique exposée à ce jour |
| **NFR-09** | NFR | Should | **À CLARIFIER** | Hébergement — **PC-13 ouvert** |
| **NFR-10** | NFR | **Must** | **À CLARIFIER** | Disponibilité 24/7 — exploitation |

**Sous-totaux établis** : ABSENTE **10** · PARTIELLE **11** · DIFFÉRÉE **4** · À CLARIFIER **7** → **32 non conformes**.

---

## 5. Couverture recalculée — **écart documenté**

| Catégorie | Nombre |
|:---|:---:|
| **CONFORMES** (par déduction) | **156** |
| PARTIELLES | 11 |
| DIFFÉRÉES | 4 |
| ABSENTES | 10 |
| À CLARIFIER | 7 |
| **TOTAL** | **188** ✅ |

**Couverture stricte recalculée : 156 / 188 = 82,98 %**
**Couverture précédemment annoncée : 152 / 188 = 80,85 %**

### ÉCART DE COUVERTURE : **+4 exigences (en faveur du projet)**

**CAUSE** — l'écart n'est **pas** une erreur détectée dans un sens ou dans l'autre, mais une **impossibilité de réconciliation mécanique** :

1. **Aucun artefact ne porte la liste nominative des 152 conformes.** Le chiffre est obtenu par *addition de deltas* de closure en closure (110 → … → 152), jamais par énumération.
2. **Les formats de déclaration sont hétérogènes** : F001 raisonne sur « 74 lignes », F005 sur « SIM-01→11 », F007 sur un tableau à 4 lignes, F009/F010 sur des matrices de couverture. Une extraction automatique ne retrouve que **42 identifiants** en format normalisé sur 188.
3. Mon chiffre de 156 est obtenu **par soustraction** (188 − 32 non-conformes prouvés). Il est donc **majorant** : toute exigence conforme « sur le papier » mais non démontrée serait comptée à tort.

**EXIGENCES CONCERNÉES** : les 4 de l'écart ne sont **pas identifiables** avec les artefacts actuels. Les candidates les plus probables sont des exigences que les Features ont exclues de leur décompte sans le tracer nominativement (typiquement dans les familles `IMP` — 21 exigences avec numérotation discontinue 01-06, 10-15, 20-22, 30-32, 40-42 — ou `SOC`).

> **Je ne corrige pas artificiellement ce résultat.** Le chiffre prudent à retenir pour le pilotage reste **152/188**, tant qu'une **liste nominative des conformes** n'a pas été produite. **C'est la première action recommandée** (voir §16, action R0).

---

## 6. État des Features F001 → F010

| Feature | Domaine | Exigences | Conformes | Partielles | Différées | Dette | Closure |
|:---|:---|:---:|:---:|:---:|:---:|:---|:---|
| **F001** | Socle tickets & workflows | 74 | 72 | 2 (SOC-14, PRI-02) | 0 | Audit append-only applicatif ; 2 PARTIEL acceptés | **CLOSED** |
| **F002** | Comptes clients & sociétés | ~15 | 15 | 0 | 0 | — | **CLOSED** |
| **F003** | Billing Office IMPORT | ~20 | 20 | 0 | 0 | — | **CLOSED** |
| **F003.5** | Consolidation UI/UX | — (→ 110/188) | — | 0 | 0 | NFR-01/02 livrés mais **non certifiés** | **CLOSED** |
| **F004** | Billing Office EXPORT | 5 (→ 115) | 5 | 0 | 0 | — | **CLOSED** |
| **F005** | Simulation Proforma | 11 (→ 125) | 10 | 0 | 1 (SIM-11) | SIM-11 Could différée | **CLOSED** |
| **F006** | Customer Complaint | 6 (→ 131) | 4 | **2 (REC-02, REC-03)** | 0 | Accessibilité paramétrage catégories | **CLOSE WITH ACCEPTED DEBT** |
| **F007** | Avoir & Exonération | 4 (→ 152) | 4 | 0 | 0 | Aucune réserve déclarée | **CLOSED** |
| **F008** | Enrôlement Transporteurs | 5 (→ 134) | 3 | 0 | 2 (ENR-04, ENR-05) | Canaux mobiles (PC-16) | **CLOSE WITH ACCEPTED DEBT** |
| **F009** | Notifications & Centre In-App | 9 (→ 142) | 8 | 0 | 1 (COM-06) | 12 dettes (D1→D12) + 7 anomalies | **CLOSE WITH ACCEPTED DEBT** |
| **F010** | Continuité Guichet & RDV | 6 (→ 148) | 6 | 0 | 0 | Dettes déclarées | **CLOSE WITH ACCEPTED DEBT** |

> Une Feature `CLOSED` **peut** porter une dette acceptée. Aucun statut historique n'est requalifié rétroactivement par cet audit.

**Réserve sur F006** : la closure annonce +6 (131/188) alors que son propre audit conclut **4/6 CONFORME**. Voir §12.

---

## 7. Inventaire des dettes ouvertes

| Dette | Feature | Type | Exigence | Impact | Bloquante future ? | Action recommandée |
|:---|:---|:---|:---|:---|:---|:---|
| **D-T1** Instabilité des tests en parallèle | F009 (constat) | **TEST** | — | Baseline non déterministe hors mode sérialisé | **Oui** — fiabilité CI | Stabiliser `PlateformeWebApplicationFactory` (booléen statique + xUnit parallèle) |
| **D-A1** Audit append-only applicatif seulement | F001 | **SÉCURITÉ** | SOC-13 / SEC-05 | Renfort SGBD absent | Non | Renfort recommandé avant production |
| **D-F1** `SOC-14` sans test dédié | F001 | **TEST** | SOC-14 (Could) | Faible | Non | Différable |
| **D-F2** `PRI-02` donnée disponible, comportement non démontré | F001 | **TEST** | PRI-02 (**Must**) | Moyen | **Oui** | Test dédié |
| **D-F3** `REC-02`/`REC-03` PARTIEL | F006 | **RECETTE** | REC-02, REC-03 (**Must**) | Accessibilité du paramétrage | **Oui** | Compléter avant recette |
| **D-F4** `SIM-11` non implémentée | F005 | **ARCHITECTURE** | SIM-11 (Could) | Faible | Non | Différable |
| **D-F5** `ENR-04`/`ENR-05` canaux mobiles | F008 | **INTÉGRATION** | ENR-04, ENR-05 | Dépend de PC-16 | **Oui (production)** | Après décision fournisseur |
| **D9-1** 3 index non annoncés, dont 1 spéculatif | F009 | **PERFORMANCE** | — | `ListerParNotificationAsync` morte | Non | Nettoyer ou acter |
| **D9-2** `TEST-COM-06` (4 cas) non écrits | F009 | **TEST** | COM-06 | COM-06 non démontrée | Non (Could) | Avant clôture de COM-06 |
| **D9-3** Aucun E2E REC-SCE-14 côté F009 | F009 | **RECETTE** | COM-01/02/03 | Volets non rejoués de bout en bout | **Oui** | **Avant recette** |
| **D9-4** Aucun bUnit administration/préférences | F009 | **TEST** | COM-03, COM-05 | UI non démontrée | **Oui** | **Avant recette** |
| **D9-5** Édition des modèles absente de l'UI | F009 | **ARCHITECTURE** | COM-03 | Paramétrage possible en API seulement | **Oui** | Compléter l'écran |
| **D9-6** Pas de fail-fast sur config de reprise | F009 | **CONFIGURATION** | ERR-02 (**Must**) | **ERR-02 silencieusement inopérante en production sans configuration** | **OUI — BLOCKER PROD** | `ValidateOnStart` |
| **D9-7** Violation d'unicité concurrente non capturée | F009 | **ARCHITECTURE** | — | Risque de boucle de rejeu | Moyen | `catch (DbUpdateException)` |
| **D9-8** Scénario C non exercé en intégration | F009 | **TEST** | ERR-02 | `RollbackAsync` réel non testé | Non | Test d'intégration |
| **D9-9** Canaux mobiles non branchés | F009 | **INTÉGRATION** | COM-01 | PC-16 | **OUI — BLOCKER PROD** | Décision DSI/Finance |
| **D9-10** Textes des modèles non contractuels | F009 | **DOCUMENTATION** | CLAR-NOT-05 | Contenus à fournir | **Oui** | **Avant recette** |
| **D9-11** Couplage `app.css` non signalé | F009 | **DOCUMENTATION** | NOT-03 | Badge non stylé si commit sélectif | Non | Arbitrer au commit |
| **D-NC** 5 capacités Must/Should non certifiées | transverse | **RECETTE** | SEC-07/08/10/11, NFR-11 | Couverture sous-évaluée | **Oui** | **Feature de certification** |

**Total : 19 dettes ouvertes. 2 BLOCKERS PRODUCTION** (D9-6, D9-9).

---

## 8. Clarifications

| PC / CLAR | Sujet | Statut | Feature | Bloque quoi ? | Décision |
|:---|:---|:---|:---|:---|:---|
| **PC-01** | Volumétrie opérationnelle cible | **OUVERT** | — | **PRODUCTION** (NFR-04, NFR-05) | Cadrage métier attendu |
| **PC-03** | Rôles Caisse/Finance, Guichet, Avoir/Exo | **RÉSOLU** | F007, F010 | — | Porté par les habilitations existantes |
| **PC-07** | Matrice des transitions | **RÉSOLU** | F001 | — | Matrice paramétrable livrée |
| **PC-08** | Pièces exigées par étape | **RÉSOLU** | F003 | — | `ReglesDepotDocument` paramétrable |
| **PC-10** | Bascule présentielle (IMP-15) | **RÉSOLU** | F010 | — | `BasculerAlternativePresentielleCommand` |
| **PC-12** | Valeurs cibles des SLA | **OUVERT** | F001 | **RECETTE** | Valeurs métier attendues |
| **PC-13** | Choix d'hébergement | **OUVERT** | — | **PRODUCTION** (NFR-09, NFR-06) | Décision DSI |
| **PC-14** | Conservation et archivage | **OUVERT** | — | **PRODUCTION** | Validation juridique |
| **PC-15** | Workflow Avoir/Exonération Finance | **RÉSOLU** | F007 | — | Formalisé et livré |
| **PC-16** | Souscription SMS/WhatsApp | **OUVERT** | F008, F009 | **PRODUCTION** (COM-01, ENR-04) | Décision DSI/Finance |
| **PC-17** | Liste initiale des déclencheurs | **PARTIEL** | F009 | **IMPLEMENTATION** | Seules les règles CDC-EXIGENCE semées |
| **PC-18** | Assistant IA — gouvernance et souveraineté | **OUVERT** | — | **SPÉCIFICATION** (IA-01→07) | **Bloque le lancement d'une Feature IA** |
| **PC-19** | Limite BL par soumission | **RÉSOLU** | F003 | — | — |
| **PC-24** | Cycle de vie du dossier | **OUVERT** | — | IMPLEMENTATION | Modèle de données |
| **PC-25** | Notifications internes agents | **PARTIEL** | F009 | IMPLEMENTATION | MSG-04 raccordée, ALE-03 non |
| **PC-26** | Navigation IES | **OUVERT** | F003 | RECETTE | — |
| **CLAR-NOT-02** | Valeurs de reprise ERR-02 | **OUVERT** | F009 | **PRODUCTION** | Métier/DSI |
| **CLAR-NOT-05** | Textes des modèles | **OUVERT** | F009 | **RECETTE** | Métier |
| **CLAR-COM-04** | Coordonnées destinataires | **OUVERT** | F009 | RECETTE / PRODUCTION | Métier |

> **Règle appliquée** : une clarification « bloque PRODUCTION » **n'empêche pas** une Feature de développement dès lors que l'architecture l'abstrait. C'est le cas de PC-16 (abstraction `ICanalNotification` livrée). **PC-18 est différent** : il bloque la **spécification** de l'Assistant IA — impossible de spécifier sans arbitrage de souveraineté.

---

## 9. Scénarios de recette REC-SCE

| REC-SCE | Exigences principales | Feature(s) | Test automatisé | Statut |
|:---|:---|:---|:---|:---|
| REC-SCE-01 → 06 | Socle, comptes, IMPORT | F001, F002, F003 | Oui | **COUVERT** |
| REC-SCE-07 | Réclamations | F006 | `RecSce07CustomerComplaintE2ETests` | **COUVERT** |
| REC-SCE-08 | Enrôlement | F008 | `RecSce08EnrolementTransporteurE2ETests` | **COUVERT** |
| REC-SCE-09 → 13 | PEC, transferts, SLA, supervision | F001 | Oui | **COUVERT** |
| **REC-SCE-14** | Transverse : notifications · continuité · dossier · habilitations · audit | **F009 + F010 + F001** | **Aucun test E2E dédié** | **PARTIEL** |

### REC-SCE-14 — revérification indépendante

F010 annonce ce scénario **14/14**. **Ce n'est pas démontré.**

| Volet | Rattachement | Test E2E dédié |
|:---|:---|:---|
| Notifications sur chaque canal configuré | F009 | ❌ |
| Lien vers le ticket | F009 | ❌ |
| Recherche guichet | F010 | ✅ (F010) |
| Rendez-vous | F010 | ✅ (F010) |
| Regroupement en dossier | F001 | ✅ acquis |
| Contrôle des habilitations | F009 + socle | ❌ pour le volet notifications |
| Journal d'audit | F009 + socle | ❌ pour le volet notifications |

`RecSce14NotificationsE2ETests.cs` **n'existe pas** (vérifié). Les volets F009 sont couverts **unitairement**, jamais rejoués de bout en bout.

**REC-SCE : 13 COUVERTS · 1 PARTIEL (REC-SCE-14) · 0 ABSENT.**

---

## 10. Exigences restantes, par famille

| Famille | Restantes | Must | Should | Could | Dépendances | Risque | Effort |
|:---|:---:|:---:|:---:|:---:|:---|:---|:---|
| **NFR** | 11 | 8 | 3 | 0 | PC-01, PC-13 · exploitation | **Élevé** (moitié non testable en code) | Moyen à élevé |
| **IA** | 7 | 5 | 2 | 0 | **PC-18 bloquant SPEC** · socle F001 | **Élevé** (souveraineté, gouvernance) | **Élevé** |
| **SEC** | 5 | 3 | 2 | 0 | **Aucune** — code déjà présent | **Faible** | **Très faible** |
| **REC** | 2 | 2 | 0 | 0 | F006 livrée | Faible | Faible |
| **ENR** | 2 | 0 | 1 | 1 | **PC-16** | Moyen | Faible |
| **SOC** | 1 | 0 | 0 | 1 | F001 livrée | Très faible | Très faible |
| **PRI** | 1 | 1 | 0 | 0 | F001 livrée | Faible | Très faible |
| **COM** | 1 | 0 | 0 | 1 | F009 livrée | Faible | Faible |
| **SIM** | 1 | 0 | 0 | 1 | F005 livrée | Très faible | Faible |
| **ERR** | 1 | 1 | 0 | 0 | Aucune | Moyen | Moyen |
| **TOTAL** | **32** | **20** | **8** | **4** | | | |

---

## 11. Capacités existantes NON CERTIFIÉES

> Fonctionnalités **présentes dans le code**, parfois testées, **jamais certifiées contre le CDC**.

| Exigence | Priorité | Preuve de présence dans le code | Ce qui manque |
|:---|:---:|:---|:---|
| **SEC-07** Contrôle des fichiers déposés | **Must** | `ReglesDepotDocument` (types, taille) + abstraction antivirus, consommés par F003/F004/F006/F008 | Certification nominative + scénario de recette |
| **SEC-10** Expiration de session paramétrable | **Must** | `Program.cs:30-31` — `ExpireTimeSpan` piloté par `Securite:ExpirationSessionMinutes`, `SlidingExpiration = true` | Test dédié + certification |
| **SEC-11** Désactivation immédiate d'un compte agent | **Must** | `ApplicationUser.EstActif` ; utilisé par le résolveur F009 | Test « désactivation sans perte d'historique » + certification |
| **NFR-11** Horaires de fonctionnement pour les SLA | **Must** | `Domain/SuiviTemps/CalendrierOuvre.cs`, `GererParametresSlaCommand`, `SurveillerEcheancesUseCase` | Certification + test de dépôt hors heures ouvrées |
| **SEC-08** Second facteur agents/admins | Should | `ActiverDoubleAuthentification.razor`, `ParcoursConnexion2FaTests`, Identity TwoFactor | Certification (et stabilisation, cf. D-T1) |
| **NFR-01** Interface en français | **Must** | Intégralité de l'UI livrée par F003.5 | Certification formelle |
| **NFR-02** Portail responsive | **Must** | Livré par F003.5 (audit UI/UX `docs/06`) | Certification + preuve responsive |

**7 exigences — dont 5 Must — sont à portée immédiate de certification.** C'est le gisement le plus rentable du projet.

---

## 12. Conformités suspectes

> Exigences comptées comme conformes alors que la preuve est incomplète. **Aucune n'est requalifiée ici** — elles sont signalées pour arbitrage.

| Exigence | Priorité | Comptée par | Contradiction relevée |
|:---|:---:|:---|:---|
| **REC-02** | **Must** | F006 (+6 → 131/188) | `closure-review.md` F006 : *« Bilan REC : 4/6 CONFORME, 2/6 PARTIEL (REC-02, REC-03) — pas 6/6 comme annoncé »* |
| **REC-03** | **Must** | F006 | idem |
| **SOC-14** | Could | F001 (dans les 74) | `closure-review.md` F001 : « PARTIEL ACCEPTÉ, sans test dédié » |
| **PRI-02** | **Must** | F001 (dans les 74) | « PARTIEL ACCEPTÉ, comportement non démontré » |
| **ERR-02** | **Must** | F009 (contrainte transverse, non comptée) | Fonctionnellement **inopérante en production sans configuration** (D9-6) — non comptée dans les 152, mais annoncée conforme |

> **Contradiction documentaire majeure** : F006 additionne +6 exigences à la couverture globale alors que son propre bilan en démontre 4. Si REC-02 et REC-03 sont reclassées PARTIELLES, **la couverture réelle descend à 150/188**. Cet arbitrage appartient au métier, pas à cet audit.

---

## 13. Candidats pour la prochaine Feature

### Candidat A — Certification Sécurité & Socle NFR

- **EXIGENCES** : SEC-07, SEC-08, SEC-10, SEC-11, NFR-01, NFR-02, NFR-11, PRI-02, SOC-14, ERR-01 — **10 exigences**
- **MUST** : 6 (SEC-07, SEC-10, SEC-11, NFR-01, NFR-02, NFR-11) + PRI-02 = **7** · **SHOULD** : 1 (SEC-08) · **COULD** : 1 (SOC-14) · **+ ERR-01 (Must, à construire)**
- **VALEUR MÉTIER** : transforme du travail déjà payé en couverture contractuelle démontrée. Ferme le risque « capacité fantôme ».
- **DÉPENDANCES** : **aucune** — tout le code existe, sauf ERR-01 (brouillon).
- **RISQUE** : **très faible** — essentiellement des tests et de la certification.
- **CLARIFICATIONS** : aucune bloquante.
- **EFFORT** : **faible** (~1 sprint court). ERR-01 est la seule construction réelle.
- **GAIN DE COUVERTURE** : **+9 à +10** → **161-162/188 (≈ 86 %)**
- **DETTE RÉSOLUE** : D-NC, D-F2, D-F1, partiellement D-T1
- **POURQUOI MAINTENANT** : c'est le meilleur rapport couverture/effort du projet, et il assainit la traçabilité **avant** d'ajouter de nouveaux modules.

### Candidat B — Assistant IA

- **EXIGENCES** : IA-01 → IA-07 — **7 exigences**
- **MUST** : 5 · **SHOULD** : 2 · **COULD** : 0
- **VALEUR MÉTIER** : élevée (réduction des sollicitations agents), mais **non urgente** — aucun parcours métier n'en dépend.
- **DÉPENDANCES** : socle F001 (escalade, tickets) disponible ; **PC-18 OUVERT et bloquant la SPÉCIFICATION**.
- **RISQUE** : **élevé** — souveraineté des données, gouvernance de la base de connaissances, choix de moteur, coût d'exploitation.
- **CLARIFICATIONS** : **PC-18 doit être tranché avant `/speckit.specify`.**
- **EFFORT** : **élevé** — module entier, intégration externe probable.
- **GAIN DE COUVERTURE** : +7 → 159/188
- **DETTE RÉSOLUE** : aucune ; en crée probablement.
- **POURQUOI MAINTENANT** : **rien ne l'impose.** Lancer maintenant reviendrait à spécifier sur un point à clarifier bloquant.

### Candidat C — Production Readiness & Exploitation

- **EXIGENCES** : NFR-03, NFR-04, NFR-05, NFR-06, NFR-08, NFR-09, NFR-10, SEC-09 — **8 exigences**
- **MUST** : 5 · **SHOULD** : 3 · **COULD** : 0
- **VALEUR MÉTIER** : indispensable **avant mise en production**, nulle sur le plan fonctionnel.
- **DÉPENDANCES** : **PC-01 et PC-13 ouverts** — volumétrie et hébergement non arbitrés.
- **RISQUE** : moyen — dépend de décisions externes (DSI).
- **CLARIFICATIONS** : PC-01, PC-13, PC-14 bloquants.
- **EFFORT** : moyen à élevé, largement **hors code** (exploitation, sauvegardes, tests de charge).
- **GAIN DE COUVERTURE** : +8 → 160/188, mais **conditionné à des décisions non prises**.
- **DETTE RÉSOLUE** : D9-6, D9-9 (les 2 blockers production)
- **POURQUOI MAINTENANT** : prématuré tant que PC-01 et PC-13 ne sont pas tranchés.

### Candidat D — Remédiation de dette F009/F010 et recette transverse

- **EXIGENCES** : COM-06, REC-02, REC-03 + REC-SCE-14 complet — **3 exigences + 1 scénario**
- **MUST** : 2 · **COULD** : 1
- **VALEUR MÉTIER** : faible en fonctionnel, **forte en fiabilité de recette**.
- **DÉPENDANCES** : aucune.
- **RISQUE** : très faible.
- **EFFORT** : faible.
- **GAIN DE COUVERTURE** : +3 → 155/188
- **DETTE RÉSOLUE** : D9-2, D9-3, D9-4, D9-5, D9-7, D-F3
- **POURQUOI MAINTENANT** : fermerait REC-SCE-14, seul scénario contractuel encore partiel.

---

## 14. Scoring

| Critère (pondération) | **A — Certification SEC/NFR** | **B — Assistant IA** | **C — Production Readiness** | **D — Remédiation dette** |
|:---|:---:|:---:|:---:|:---:|
| Criticité Must (30) | **26** (7 Must) | 22 (5 Must) | 18 (5 Must, non testables) | 12 (2 Must) |
| Valeur opérationnelle (20) | **15** | 14 | 18 | 11 |
| Dépendances disponibles (15) | **15** (tout existe) | 4 (PC-18 bloquant) | 4 (PC-01/PC-13) | **15** |
| Réduction dette/risque (15) | **13** | 0 | **14** | **14** |
| Gain de couverture CDC (10) | **10** (+9/+10) | 7 (+7) | 8 (+8) | 3 (+3) |
| Effort raisonnable (10) | **9** | 3 | 5 | **9** |
| **TOTAL /100** | **88** | **50** | **67** | **64** |

---

## 15. Roadmap jusqu'à 188/188

```
R0  Liste nominative des 152 conformes            (préalable de traçabilité, 0 code)
        ↓
A   Certification Sécurité & Socle NFR            → ~161/188   (SEC-07/08/10/11, NFR-01/02/11, PRI-02, SOC-14, ERR-01)
        ↓
D   Remédiation dette & recette transverse        → ~164/188   (COM-06, REC-02, REC-03, REC-SCE-14 complet)
        ↓
B   Assistant IA           [après arbitrage PC-18] → ~171/188   (IA-01→07)
        ↓
E   Canaux mobiles         [après arbitrage PC-16] → ~173/188   (ENR-04, ENR-05) + SIM-11 (Could)
        ↓
C   Production Readiness   [après PC-01, PC-13]    → ~181/188   (NFR-03/04/05/06/08/09/10, SEC-09)
        ↓
    Hardening & Recette finale CDC                 →  188/188
```

> Les Features sont construites **à partir des exigences réellement restantes**, pas pour préserver une numérotation. Le libellé « F011 » n'est pas présupposé : le candidat A regroupe des exigences transverses SEC/NFR et pourrait tout aussi bien s'appeler « F011 — Certification Sécurité & Socle Non-Fonctionnel ».

---

## 16. Production Readiness

| Sujet | Constat | Classement |
|:---|:---|:---|
| **Configuration de reprise ERR-02** | Aucun fail-fast : l'application démarre sans configuration, ERR-02 est **silencieusement inopérante** | **BLOCKER PROD** |
| **Fournisseurs SMS/WhatsApp** | Abstraction livrée, **aucun fournisseur souscrit** (PC-16) | **BLOCKER PROD** |
| **Fournisseur e-mail** | Abstraction seule — `CanalJournalise` n'envoie rien | **BLOCKER PROD** |
| **Sauvegardes et restauration** | NFR-06 — aucune procédure testée | **BLOCKER PROD** |
| **Hébergement** | PC-13 non tranché (NFR-09) | **BLOCKER PROD** |
| **Volumétrie et performance** | PC-01 non tranché ; NFR-04 (<3 s) et NFR-05 non mesurés | **IMPORTANT PROD** |
| **Conservation et archivage** | PC-14 non tranché | **IMPORTANT PROD** |
| **Valeurs cibles SLA** | PC-12 non tranché | **IMPORTANT PROD** |
| **Secrets** | Aucun secret en dur détecté ; `ConnectionStrings:PlateformeDb` vide en `appsettings.json` | **IMPORTANT PROD** (approvisionnement) |
| **Cloisonnement des environnements** | SEC-09 — absent | **IMPORTANT PROD** |
| **Observabilité** | Journalisation applicative présente ; pas de métriques ni de traces | **IMPORTANT PROD** |
| **Index et performance BDD** | 1 index spéculatif F009 sur une méthode morte | **AMÉLIORATION** |
| **Tests E2E manquants** | REC-SCE-14 (volets F009), bUnit administration | **IMPORTANT PROD** |
| **Stabilité de la suite de tests** | Instable en parallèle, stable en sérialisé | **IMPORTANT PROD** |
| **Recettes manuelles** | `quickstart.md` F009 parcours 2, 3, 7 non rejoués | **IMPORTANT PROD** |
| **Jeux de données** | Seed de développement borné et non contractuel | **AMÉLIORATION** |
| **Permissions opérationnelles** | RBAC complet et vérifié côté serveur | ✅ conforme |

**5 BLOCKERS PRODUCTION · 9 IMPORTANT PROD · 2 AMÉLIORATION.**

---

## 17. Recommandation finale

**Lancer le Candidat A — Certification Sécurité & Socle Non-Fonctionnel**, précédé de l'action **R0**.

**R0 (préalable, sans code)** : produire la **liste nominative des exigences conformes**. Tant qu'elle n'existe pas, aucun chiffre de couverture n'est auditable, et l'écart de 4 exigences relevé au §5 restera irréductible. C'est un travail de traçabilité, pas de développement.

**Pourquoi A plutôt que l'Assistant IA** — malgré ses 5 Must :
1. **PC-18 bloque la spécification** de l'IA. Lancer `/speckit.specify` maintenant violerait le Principe XV de la constitution (ne jamais résoudre arbitrairement un point ouvert).
2. Le candidat A ne dépend d'**aucune** clarification.
3. A rapporte **plus de couverture** (+9/+10 contre +7) pour un effort **bien moindre**.
4. A supprime un risque de gouvernance réel : **cinq exigences Must dont le travail est fait mais la preuve absente**. Ce risque grandit à chaque Feature ajoutée par-dessus.

**Ce que A ne fait pas** : il ne débloque aucun des 5 blockers production, qui dépendent tous de décisions externes (PC-13, PC-16, PC-01). Ces arbitrages devraient être sollicités **en parallèle** du développement de A, pour ne pas les découvrir au moment de la mise en production.

---

```
AUDIT GLOBAL POST-F007

BASELINE :
BUILD : SUCCÈS
WARNINGS : 0
TESTS : Domain 154/154 · Application 425/425 · Infrastructure 83/83 · Web 156/156 — TOTAL 818/818
PENDING MODEL CHANGES : 0

CDC :
TOTAL : 188 (vérifié mécaniquement — 150 Must, 32 Should, 6 Could)
CONFORMES : 156 (par soustraction — chiffre MAJORANT, non énuméré nominativement)
PARTIELLES : 11
DIFFÉRÉES : 4
ABSENTES : 10
À CLARIFIER : 7
CONTRÔLE : 156 + 11 + 4 + 10 + 7 = 188 ✅

COUVERTURE STRICTE : 156/188 (82,98 %) — non consolidée
COUVERTURE PRÉCÉDEMMENT ANNONCÉE : 152/188 (80,85 %)
ÉCART : +4 exigences NON EXPLIQUÉ — aucun artefact ne porte la liste nominative des
        conformes ; réconciliation mécanique impossible. Chiffre prudent à retenir : 152/188.
        Si REC-02 et REC-03 sont reclassées PARTIELLES, la couverture descend à 150/188.

FEATURES AUDITÉES :
F001 : CLOSED (2 PARTIEL acceptés : SOC-14, PRI-02)
F002 : CLOSED
F003 : CLOSED
F003.5 : CLOSED (NFR-01/NFR-02 livrés mais NON CERTIFIÉS)
F004 : CLOSED
F005 : CLOSED (SIM-11 différée)
F006 : CLOSE WITH ACCEPTED DEBT (REC-02, REC-03 PARTIEL — contradiction avec le +6 annoncé)
F007 : CLOSED
F008 : CLOSE WITH ACCEPTED DEBT (ENR-04, ENR-05 différées)
F009 : CLOSE WITH ACCEPTED DEBT (COM-06 différée, 12 dettes, 7 anomalies)
F010 : CLOSE WITH ACCEPTED DEBT

REC-SCE :
COUVERTS : 13
PARTIELS : 1 (REC-SCE-14 — volets F009 sans test E2E, contrairement au 14/14 annoncé)
ABSENTS : 0

DETTES OUVERTES : 19
BLOCKERS PRODUCTION : 5 (config reprise ERR-02 · fournisseurs SMS/WhatsApp · fournisseur
                        e-mail · sauvegardes NFR-06 · hébergement PC-13)

EXIGENCES RESTANTES : 32 (20 Must, 8 Should, 4 Could)
  NFR 11 · IA 7 · SEC 5 · REC 2 · ENR 2 · SOC 1 · PRI 1 · COM 1 · SIM 1 · ERR 1

TOP 3 CANDIDATS :
1. Certification Sécurité & Socle NFR — 88/100
2. Production Readiness & Exploitation — 67/100
3. Remédiation dette & recette transverse — 64/100
   (Assistant IA : 50/100 — écarté à ce stade, PC-18 bloque la spécification)

FEATURE RECOMMANDÉE : Certification Sécurité & Socle Non-Fonctionnel
EXIGENCES CIBLÉES : SEC-07, SEC-08, SEC-10, SEC-11, NFR-01, NFR-02, NFR-11, PRI-02,
                    SOC-14, ERR-01
GAIN DE COUVERTURE : +9 à +10 → ≈ 161-162/188 (≈ 86 %)
JUSTIFICATION : cinq exigences Must sont déjà servies par du code en production mais n'ont
                jamais été certifiées ; aucune clarification ne bloque ; effort faible,
                risque quasi nul, meilleur rapport couverture/effort du projet. L'Assistant
                IA est écarté car PC-18 bloque sa spécification (Constitution, Principe XV).

ROADMAP RESTANTE :
1. R0 — liste nominative des conformes (traçabilité, sans code)
2. Certification Sécurité & Socle NFR → ≈161/188
3. Remédiation dette & recette transverse (REC-SCE-14 complet) → ≈164/188
4. Assistant IA [après PC-18] → ≈171/188, puis canaux mobiles [PC-16], puis Production
   Readiness [PC-01, PC-13] → 188/188

DOCUMENT :
docs/16-audit-global-post-f007.md

MODIFICATIONS CODE :
AUCUNE

MIGRATION :
AUCUNE

COMMIT :
AUCUN

PUSH :
NON

VERDICT :
READY FOR NEXT FEATURE SELECTION
```
