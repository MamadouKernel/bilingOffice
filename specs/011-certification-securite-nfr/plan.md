# Implementation Plan: Feature 011 — Certification Sécurité & Socle Non-Fonctionnel

**Branch**: `011-certification-securite-nfr` | **Date**: 1er septembre 2026 | **Spec**: [spec.md](./spec.md)

**Input**: `spec.md` (CLARIFIÉE), `checklists/requirements.md` (107/107), `docs/17-baseline-nominative-188-exigences.md`, `docs/18-preflight-certification-securite-nfr.md` + addendum Final Scope Gate, CDC original, `.specify/memory/constitution.md`, **code réel F001→F010 audité fichier par fichier** (cf. `research.md`).

**Note** : ce plan ne produit aucun code, aucune migration. `/speckit.tasks` n'est pas invoqué.

---

## Summary

F011 certifie **11 exigences CDC** (11 Must) déjà servies par le socle mais jamais comptabilisées comme conformes. C'est une **Feature de certification**, pas de construction.

L'audit du code réel (`research.md` D0) a produit un résultat **plus favorable que l'estimation du preflight** : trois exigences disposent déjà de suites démontrant exactement le texte CDC. `SEC-05` est couverte par une suite complète sur base réelle (modification impossible, suppression impossible, ajout possible) ; `ERR-03` est couverte pour ses volets *type* et *taille* **avec assertion du message d'erreur**. Ces deux exigences relèvent donc du **rattachement nominatif**, pas de l'écriture de tests.

À l'inverse, l'audit a confirmé de véritables trous : `SEC-01` n'a **aucun** test de verrouillage, `SEC-10` **aucun** test, `NFR-01` et `NFR-02` **aucun** test.

**Un seul point de code produit est modifié** : la validation de la configuration de reprise au démarrage du composant (ERR-02, contrat `C11-P01`). Tout le reste est preuve et rattachement.

---

## Technical Context

**Language/Version** : C# 13 / .NET 10 (imposé, Constitution Principe I)

**Primary Dependencies** : ASP.NET Core Blazor Server, Entity Framework Core, ASP.NET Core Identity, xUnit, bUnit, `Microsoft.AspNetCore.Mvc.Testing`, Moq — **aucune nouvelle dépendance**. En particulier : **aucun navigateur automatisé n'est introduit** (research.md D4).

**Storage** : SQL Server — **aucune migration**, **aucune entité**, **aucune colonne** (data-model.md)

**Testing** : xUnit (Domaine/Application/Infrastructure), bUnit (Interface), `Mvc.Testing` (hôte réel), SQL Server réel **uniquement pour SEC-05** (research.md D9)

**Target Platform** : application web multi-couches existante — F011 n'en modifie pas la structure

**Project Type** : Feature de certification transverse, sans module fonctionnel propre

**Performance Goals** : **NON SPÉCIFIÉ** — NFR-03, NFR-04 et NFR-05 sont **hors périmètre** ; le CDC ne fixe aucun seuil pour NFR-01, NFR-02 et NFR-11

**Constraints** : aucune migration · aucune entité · aucun écran nouveau · aucun fournisseur tiers · aucune valeur métier inventée · exécution de test **sérialisée** (instabilité connue hors périmètre)

**Scale/Scope** : 11 exigences · 11 suites TEST-SPEC · 1 point de code produit

---

## Constitution Check

*GATE : évalué avant Phase 0 et re-évalué après Phase 1 (voir §Post-Design).*

| Principe | Évaluation | Justification |
|:---|:---:|:---|
| **I. Stack technique imposée** | ✅ PASS | Aucune technologie nouvelle ; aucune dépendance ajoutée ; navigateur automatisé explicitement écarté (D4) |
| **II. Source de vérité et traçabilité** | ✅ PASS | Chaque contrat cite le **texte CDC intégral**. Précédent appliqué : le CDC prime sur la matrice tronquée (cas SEC-02 au preflight) |
| **III. Priorisation Must/Should/Could** | ✅ PASS | 11 Must, aucune Should ni Could. Aucune exigence exclue n'est réintroduite |
| **IV. Principes d'architecture** | ✅ PASS | **Non-duplication démontrée par l'audit** : SEC-05 et ERR-03 ne reçoivent aucun test nouveau. Navigateur automatisé écarté comme disproportionné (YAGNI) |
| **V. Domaine métier** | ✅ PASS | Aucune entité, aucune règle métier nouvelle. Les mécanismes existants sont certifiés, jamais réimplémentés |
| **VI. Workflows paramétrables** | ✅ PASS | Durée de session, règles de dépôt et calendrier ouvré restent **paramétrables** ; aucune valeur n'est figée en dur (D7, D8) |
| **VII. Sécurité (NON-NÉGOCIABLE)** | ✅ PASS | Chaque fiche SEC exige un **contrôle serveur** ; principe transverse écrit : *une protection uniquement UI est insuffisante*. Aucun mécanisme de sécurité dupliqué |
| **VIII. Données et persistance** | ✅ PASS | **0 migration, 0 table, 0 colonne** — démontré exigence par exigence (data-model.md §2) |
| **IX. Audit et traçabilité** | ✅ PASS | SEC-05 certifie l'inviolabilité du journal **unique** ; aucun second journal |
| **X. Gestion des documents** | ✅ PASS | ERR-03 certifie les contrôles existants. Le volet antivirus (SEC-07) est **exclu**, jamais requalifié |
| **XI. Intégrations et périmètre autonome** | ✅ PASS | Aucun fournisseur, aucune API externe, **aucun service externe de jours fériés** (D8) |
| **XII. Tests et stratégie de validation** | ✅ PASS | Test-First sur les preuves manquantes ; ordre contraignant harnais → produit pour ERR-02 (D6) ; niveaux choisis au plus juste (D9) |
| **XIII. Performance et exploitation** | ✅ PASS | Aucune valeur de performance inventée ; NFR-03/04/05 hors périmètre |
| **XIV. Observabilité** | ✅ PASS | Le message de fail-fast désigne le paramètre manquant sans exposer de donnée sensible |
| **XV. Gestion des points non résolus** | ✅ PASS | **CLAR-NOT-02 reste ouverte** (D3) · **GATE-ERR03 posé, non tranché** (D5) · PC-05 respecté (SEC-02 exclue) · aucune hypothèse promue en règle |
| **XVI. Definition of Done** | ✅ PASS (à vérifier en implémentation) | Grille applicable ; aucune exigence Must silencieusement ignorée — les exclusions sont nommées et motivées |
| **XVII. Gouvernance des écarts** | ✅ PASS | Le seul écart au socle (`C11-P01`) est explicite, justifié, borné à un point unique, et ne modifie aucune règle métier |

**17/17 PASS.** Aucune violation — la section « Complexity Tracking » reste vide.

---

## Project Structure

### Documentation (this feature)

```text
specs/011-certification-securite-nfr/
├── spec.md                # Entrée (CLARIFIÉE — 8 clarifications)
├── checklists/requirements.md   # 107/107
├── plan.md                # Ce fichier
├── research.md            # Phase 0 — 10 décisions (D0-D9)
├── data-model.md          # Phase 1 — démonstration : 0 entité, 0 table, 0 migration
├── contracts/
│   └── use-cases.md       # Phase 1 — 11 contrats de certification + 1 contrat produit
├── quickstart.md          # Phase 1 — 9 parcours de certification (A→I)
└── tasks.md               # Phase 2 (/speckit.tasks — NON produit ici)
```

### Source Code — surface d'impact réelle

```text
src/
└── CIT.Plateforme.Infrastructure/
    └── DependencyInjection.cs        # SEUL fichier produit modifié — C11-P01
                                      # validation de la configuration de reprise au
                                      # démarrage du composant (ERR-02)
                                      # AUCUNE valeur de reprise introduite

tests/
├── CIT.Plateforme.Domain.Tests/
│   └── SuiviTemps/                   # NFR-CERT-03 — scénario limite hors heures ouvrées
│
├── CIT.Plateforme.Application.Tests/
│   ├── Securite/                     # SEC-CERT-02 (cloisonnement nominatif)
│   │                                 # SEC-CERT-05 (désactivation + historique)
│   │                                 # SEC-CERT-06 (lien sans authentification)
│   └── Documents/                    # ERR-CERT-02 — RATTACHEMENT de la suite existante
│                                     # (aucun test nouveau pour type/taille)
│
├── CIT.Plateforme.Infrastructure.Tests/
│   └── Persistence/                  # SEC-CERT-03 — RATTACHEMENT de la suite existante
│                                     # (aucun test nouveau)
│
└── CIT.Plateforme.Web.Tests/
    ├── Support/                      # HARNAIS — configuration de reprise valide (ÉTAPE 1)
    ├── Securite/                     # SEC-CERT-01 (verrouillage) · SEC-CERT-04 (session)
    │                                 # ERR-CERT-01 (démarrage sans configuration)
    └── Nfr/                          # NFR-CERT-01 (langue) · NFR-CERT-02 (responsive structurel)
```

**Structure Decision** : F011 n'introduit **aucun projet, aucune couche, aucun dossier applicatif**. Un seul fichier de produit est touché. Les tests s'insèrent dans les projets existants, dans des dossiers thématiques alignés sur les conventions en place.

---

## Régimes de certification

L'audit du code (research.md D2) répartit les 11 exigences en trois régimes — **ce n'est pas une hypothèse uniforme, c'est le résultat de l'inspection**.

| Régime | Exigences | Nb | Travail |
|:---|:---|:---:|:---|
| **R1 — Rattachement nominatif seul** | SEC-05 · ERR-03 | **2** | La preuve existe et démontre le texte CDC. **Aucun test nouveau** — rattacher, ne pas dupliquer |
| **R2 — Complément de preuve** | SEC-01 · SEC-03 · SEC-10 · SEC-11 · SEC-12 · NFR-01 · NFR-02 · NFR-11 | **8** | Mécanisme en place ; scénarios manquants, principalement négatifs |
| **R3 — Correction produit minimale** | ERR-02 | **1** | Un point unique de validation au démarrage |

---

## Matrice TEST-SPEC — 11 suites

| ID | Exigence | Niveau | Fixture | Action | Assertion | Résultat interdit | Preuve Closure |
|:---|:---|:---|:---|:---|:---|:---|:---|
| **SEC-CERT-01** | SEC-01 | Intégration | Hôte réel, compte actif, plafond configuré | Échecs répétés au-delà du plafond, puis **bon** mot de passe | Compte verrouillé ; accès **toujours refusé** | Accès accordé après verrouillage | Le verrouillage prime sur la validité du mot de passe |
| **SEC-CERT-02** | SEC-03 | Application + Intégration | 2 clients de sociétés distinctes, 2 agents de files distinctes | Accès direct par identifiant ; action hors file | Refus **serveur**, sans divulgation | Ressource retournée ; existence révélée | Refus serveur sur les **deux axes** |
| **SEC-CERT-03** | SEC-05 | Infrastructure (SQL réel) | Entrée d'audit existante | Modifier, puis supprimer | Les deux rejetées ; journal intact | Entrée altérée | **R1** — rattachement de la suite existante |
| **SEC-CERT-04** | SEC-10 | Intégration | Hôte réel, durée fournie par la **configuration de test** | Session dépassée → ressource protégée | Valeur configurée appliquée ; accès refusé | Session perpétuelle ; durée en production | Paramétrabilité **et** effet |
| **SEC-CERT-05** | SEC-11 | Application + Intégration | Compte agent actif ayant produit de l'historique | Désactiver, puis (a) accéder (b) consulter l'historique | (a) refus immédiat (b) historique intact | Accès maintenu ; trace supprimée | Les **deux volets** dans le même scénario |
| **SEC-CERT-06** | SEC-12 | Application | Notification portant un lien vers un ticket | Ouvrir **sans authentification** ; habilitation retirée | Renvoi au contrôle d'identité ; refus sans divulgation | Document servi ; jeton présent | Absence de jeton **et** d'accès direct |
| **ERR-CERT-01** | ERR-02 | Intégration (démarrage) | Environnement **sans** configuration de reprise | Démarrer le composant | Échec explicite, paramètre désigné | Démarrage puis reprise inopérante ; défaut substitué ; **application entière en échec** | Fail-fast + portée **composant** |
| **ERR-CERT-02** | ERR-03 | Application | Règles de dépôt configurées | Type non autorisé ; taille dépassée | Refus **avec message** désignant le critère | Fichier accepté ; refus sans motif | **R1** — rattachement. ⚠️ **GATE-ERR03** |
| **NFR-CERT-01** | NFR-01 | Interface | Écrans du parcours utilisateur | Inspecter les libellés | Libellés **en français** | Libellé non francophone exposé | Vérification reproductible, **sans seuil inventé** |
| **NFR-CERT-02** | NFR-02 | Interface (**structurel**) | Écrans du parcours client | Vérifier les 5 points structurels | Contrat responsive appliqué | Largeur présentée comme seuil CDC | **Limite déclarée** : pas de validation visuelle |
| **NFR-CERT-03** | NFR-11 | Domaine + Application | File avec calendrier ouvré paramétré | Dépôt **hors heures ouvrées** | Dépôt accepté ; décompte selon les règles de la file | Dépôt refusé ; décompte calendaire | Les **deux propriétés** du texte CDC |

---

## Phases d'implémentation prévues (TEST-FIRST) — indicatif pour `/speckit.tasks`

> Ce plan ne produit pas `tasks.md`. Les phases ci-dessous cadrent l'ordre logique.

1. **Preflight** — baseline **818/818** en exécution sérialisée, 0 avertissement, 0 changement de modèle en attente. Tout écart bloque.
2. **Rattachement R1** — rattacher nominativement les suites existantes de `SEC-05` et `ERR-03` (volets type/taille). **Aucun test nouveau.** Vérifier que chaque suite démontre bien le texte CDC avant de la revendiquer.
3. **Certifications sans dépendance produit** — `SEC-CERT-02`, `SEC-CERT-05`, `SEC-CERT-06`, `NFR-CERT-03` : Application et Domaine, indépendantes de l'hôte.
4. **Certifications sur hôte réel** — `SEC-CERT-01` (verrouillage), `SEC-CERT-04` (session).
5. **Certifications d'interface** — `NFR-CERT-01`, `NFR-CERT-02`.
6. **ERR-02 — ordre contraignant** (research.md D6) :
   1. adapter le **harnais** pour fournir une configuration de reprise valide ;
   2. **baseline verte** ;
   3. activer le **fail-fast** dans le produit ;
   4. tester la configuration absente/invalide ;
   5. tester la configuration valide ;
   6. non-régression F009 + suite Web complète.
   > **Ne jamais inverser 1 et 3.**
7. **Non-régression finale** — build 0 avertissement, suite complète ≥ 818, `has-pending-model-changes` à 0, diff nul sur entités et contexte de persistance.
8. **Contrôles d'exclusion** — 0 occurrence de SEC-02/SEC-07/SEC-08 hors « Hors périmètre » ; 0 valeur de reprise en dur ; 0 largeur présentée comme seuil CDC ; aucun écran modifié sans anomalie démontrée.

---

## Points à clarifier avant `/speckit.tasks`

| # | Point | Nature | Effet |
|:--|:---|:---|:---|
| **1** | **GATE-ERR03** — que recouvre « intégrité » au sens du CDC ? | **Gate conditionnel** | Si un mécanisme distinct est exigé → **ERR-03 sort**, total **10**, projection **156/188**. **Ne bloque pas `/speckit.tasks`** : les 10 autres exigences sont indépendantes |
| **2** | **NFR-02 — validation visuelle** | **Dette de recette** | Ne bloque pas la certification CDC (aucun seuil au CDC). À porter en Closure Review |
| **3** | **CLAR-NOT-02** — valeurs de reprise | **Hors périmètre** | Reste ouverte. F011 signale l'absence, ne la comble pas |
| **4** | **SEC-10** — durée d'exploitation | **Hors périmètre** | Décision métier/DSI. F011 certifie la paramétrabilité, pas la valeur |
| **5** | **SEC-02, SEC-07, SEC-08** | **Hors périmètre** | Feature de remédiation ultérieure |

**Aucun BLOCKER pour `/speckit.tasks`.**

---

## Risques

| # | Risque | Probabilité | Mitigation |
|:--|:---|:---:|:---|
| **R1** | Activer le fail-fast avant d'adapter le harnais fait tomber **toute la suite Web** | **Élevée** | Ordre contraignant D6, inscrit dans le plan et opposable aux tâches |
| **R2** | Un test de certification **duplique** une preuve existante | Moyenne | Régime R1 identifié par l'audit ; l'étape 2 impose le rattachement avant l'écriture |
| **R3** | GATE-ERR03 tranché défavorablement en cours d'implémentation | Moyenne | ERR-03 est isolée ; sa sortie retire une suite sans toucher aux autres |
| **R4** | Absorption involontaire des 83 fichiers préexistants (refonte UI/connexion/2FA) | Moyenne | Commit sélectif ; piège `app.css` documenté depuis l'audit F009 |
| **R5** | Un test NFR conduit à modifier un écran pour « faire passer » la certification | Faible | Interdiction explicite : ne modifier un écran qu'en cas d'**anomalie démontrée** |
| **R6** | Un test F011 dépend indirectement du harnais 2FA instable | Faible | **STOP sur ce sous-test** ; ne pas absorber SEC-08, ne pas diagnostiquer |

---

## Post-Design Constitution Check

Re-évaluation après Phase 1 (`research.md`, `data-model.md`, `contracts/use-cases.md`, `quickstart.md`) : **aucune nouvelle violation**.

Le constat le plus significatif de cette phase — **deux exigences n'appellent aucun test nouveau** (régime R1) — **renforce** le Principe IV (non-duplication) au-delà du niveau visé initialement. Le refus d'introduire un navigateur automatisé pour NFR-02, en assumant et déclarant la limite de preuve, sert le même principe.

Le seul écart au socle (`C11-P01`) est **explicite, justifié, borné à un point unique**, et ne modifie **aucune règle métier** — conforme au Principe XVII.

**17/17 PASS maintenu.**

---

## Complexity Tracking

*Aucune violation du Constitution Check — section laissée vide conformément au gabarit.*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
