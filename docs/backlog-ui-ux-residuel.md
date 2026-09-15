# Backlog UI/UX résiduel

**Date** : 7 septembre 2026. Après fermeture P1/P2/P3 — **1584/1584**.

> **Dette UI/UX technique corrigeable sans décision : 0.**
> Chaque ligne ci-dessous relève d'une décision (métier/infra), d'un risque explicitement accepté,
> d'un faux positif démontré, ou d'une amélioration produit post-production. Aucune ne bloque le pilote.

---

## A. Décisions métier requises (3)

Non tranchables sans le métier — jamais inventées.

| ID | Question | Comportement actuel | Options | Recommandation |
|---|---|---|---|---|
| **AM-01** | La durée de séjour facturée : borne incluse ou exclue ? Le simulateur affiche « 14 jours », l'historique « 15 j », le branchement frigo est facturé 13 j — pour le même intervalle (06/09 → 20/09). | Trois affichages divergents pour une même simulation. | (a) borne exclue (14) ; (b) borne incluse (15). | Uniformiser sur la règle du CDC/facturation réelle une fois tranchée. |
| **AM-02** | Quelle restriction pour un collaborateur « limité » ? `collab.limite` présente exactement le même menu et les mêmes accès que `client.normal` (mesuré). | Aucune restriction visible. | Définir le périmètre réduit attendu. | Restreindre après décision, puis test de cloisonnement dédié. |
| **AM-03** | « Valider Booking » fait avancer le workflow **sans confirmation**, alors qu'une attribution d'habilitation en exige une. Où placer la frontière ? | Action de workflow directe. | (a) confirmer toute transition ; (b) ne confirmer que le destructif. | Harmoniser après décision. |

---

## B. Décision d'infrastructure / déploiement (1)

| ID | Sujet | État |
|---|---|---|
| **D-04** | Session/antiforgery : message spécifique « session expirée » + **persistance des clés DataProtection** (magasin partagé, `SetApplicationName`) pour le mono- et le multi-instance. | Symptôme brut **dev-only** ; production rend déjà l'écran de marque. Comportements et recommandation détaillés dans [`remediation-ui-ux-complete.md`](remediation-ui-ux-complete.md) §4. La part applicative est une modification du pipeline de sécurité, à réaliser **avec** la décision d'infrastructure. |

---

## C. Risques explicitement acceptés (5)

| ID | Motif de l'acceptation |
|---|---|
| **numérotation** (`AVO-AVO`, `ENR-ENR`) | Migration de format sur données existantes (mélange ancien/nouveau, jeux d'essai) ; bénéfice cosmétique ; risque disproportionné. |
| **UI-17** (80 `!important`) | Retrait à haut risque de régression de spécificité sur toute l'application ; bénéfice utilisateur nul. |
| **UI-16 (refactor de fond)** | Part critique (fonds sombres) corrigée **et gardée** ; le refactor des ~200 `style=` inline restants est sans impact fonctionnel — chantier de fond. |
| **D-19** (succès sous la ligne de flottaison) | Le panneau **est** rendu ; défilement interop pour un gain marginal. |
| **UI-08 (résidu)** (emojis décoratifs) | Contrôles interactifs icône déjà étiquetés ; masquer chaque emoji adjacent à du texte est un balayage de masse à faible valeur. |

---

## D. Améliorations produit — post-production (3)

Fonctionnalités, non défauts. Les pages concernées sont exploitables en l'état.

| ID | Amélioration |
|---|---|
| **UI-11** | Tri / filtre / recherche sur les 12 files agent (utile au-delà d'un certain volume par file). |
| **UI-19** | Fil d'Ariane sur les fiches et pages de détail. |
| **UI-12** | Faire passer `IndicateursTemps` par une requête Application (retrait de l'accès direct `PlateformeDbContext`) — dette d'architecture, invisible à l'utilisateur. |

---

## E. Faux positifs démontrés (5)

| ID | Preuve |
|---|---|
| **UI-07** (états vides) | Présents partout où utiles — `EtatVide` partagé ou markup custom (`MesRendezVous`, `MesDemandesAvoirExo`…). |
| **UI-10** (`NavLinkMatch`) | Résolu par la refonte de la sidebar : chaque entrée porte sa correspondance (`CorrespondanceExacte`). |
| **UI-13** (styles locaux ComptesAgents) | Styles Blazor **scoped** (pattern valide) ; couleurs tokenisées (UI-R02). |
| **UI-15** (`ToastNotification` orphelin) | Câblé et utilisé (retours d'action). |
| **D-22** (CSV) | Le parseur cumule toutes les erreurs de syntaxe ; le constructeur toutes les erreurs de référentiel. Seul l'ordre des phases les sépare — une ligne mal formée **ne peut pas** être vérifiée sémantiquement. Comportement standard. |

---

## Résumé

| Catégorie | Nb |
|---|---|
| Décisions métier | 3 |
| Décision d'infrastructure | 1 |
| Risques acceptés | 5 |
| Post-production (features) | 3 |
| Faux positifs | 5 |
| **Dette UI/UX technique corrigeable sans décision** | **0** |
