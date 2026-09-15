# Clôture finale UI/UX — rapport principal

**Date** : 7 septembre 2026.
**Règle appliquée** : aucun PASS sans preuve ; aucune décision métier inventée ; aucune sécurité
affaiblie pour l'UX ; aucune dette masquée ; **aucun commit**.

---

## 1. Baseline confirmée avant modification

- HEAD : `1c3cd38`, branche `main`. `git diff --check` propre (hors avertissements CRLF).
- Suite de référence rejouée **par projet** : Domain 231, Application 837, Infrastructure 175,
  Web 377 → **1620/1620, 0 échec, 0 ignoré**. Baseline intègre déjà la garde `ContrasteChampsWcagTests`.

## 2. Modifications de ce cycle

| Fichier | Nature |
|---|---|
| `Application/Client/DashboardClientVue.cs` | AM-02 — champ serveur `PeutSoumettre` |
| `Application/Client/ObtenirDashboardClientQuery.cs` | AM-02 — calcul `EstActif && DroitSoumission` |
| `Web/Components/Pages/Client/DashboardClient.razor` | AM-02 — 2 CTA de soumission gardés |
| `Web/Components/Pages/Tickets/DetailTicket.razor` | AM-03 — confirmation sur Annuler + Clôturer |
| `tests/.../Client/ObtenirDashboardClientQueryTests.cs` | +2 tests AM-02 |
| `tests/.../Pages/Client/DashboardClientTests.cs` | +1 test AM-02 (rendu) |
| `tests/.../Pages/Tickets/DetailTicketConfirmationTests.cs` | +3 tests AM-03 (nouveau) |

Aucune modification du moteur de facturation, du pipeline de sécurité, ni de la palette hors jeton
`--field-border` (cycle précédent).

## 3. Décisions et implémentations

Détail complet dans [`decisions-finales-ui-ux.md`](decisions-finales-ui-ux.md). Synthèse :

| Verrou | Décision | Statut |
|---|---|---|
| AM-01 durée de séjour | Convention **bornes incluses** déjà unique et faisant autorité (Annexe A.8, golden master) | **CONFORME** — aucune modif |
| AM-02 collaborateur limité | Visibilité dérivée serveur (`EstActif && DroitSoumission`) | **CONFORME** (menu + dashboard client) |
| AM-03 confirmations | Stratégie B — C/D uniquement | **APPLIQUÉ au cœur** (clôture, annulation) ; rollout résiduel documenté |
| D-04 DataProtection | Persistance clés = décision de topologie | **CONDITION INFRA / ACTION DÉPLOIEMENT** |
| A11Y lecteur d'écran | NVDA/JAWS indisponibles | **NON EXÉCUTABLE** — 9/10 maintenu |

## 4. Tests

Suite complète **par projet**, après modifications :

| Projet | Avant | Après | Δ |
|---|---|---|---|
| Domain | 231 | 231 | 0 |
| Application | 837 | 839 | +2 (AM-02) |
| Infrastructure | 175 | 175 | 0 |
| Web | 377 | 381 | +4 (1 AM-02 rendu + 3 AM-03) |
| **Total** | **1620** | **1626** | **+6** |

**0 échec, 0 ignoré.** Aucun test supprimé, aucun `Skip`, aucune assertion affaiblie. `1620 + 6 = 1626`.

## 5. Mutations (chacune restaurée, diff re-vérifié)

| Cible | Mutation | Effet |
|---|---|---|
| AM-01 | durée `+1 → +2` | 4 golden masters tombent |
| AM-02 | `PeutSoumettre: true` en dur | garde de rendu tombe |
| AM-03 | `@onclick="ClorerAsync"` (court-circuit modale) | 3 gardes tombent |

## 6. Recette navigateur / responsive / dark mode

Détail dans [`recette-finale-ui-ux.md`](recette-finale-ui-ux.md).

- **WCAG 1.4.11** : vérifié en **navigateur**, clair + sombre, valeurs `getComputedStyle` ≥ 3:1
  (tous états). Basculement de thème à chaud OK.
- **AM-02 / AM-03** : vérifiés au **rendu composant** (pages réelles montées avec requêtes/commandes
  réelles sur base en mémoire).
- **Recette authentifiée multi-persona (8 profils) + responsive 320→1920 par page authentifiée +
  dark par page authentifiée** : **NON REJOUÉE** ce cycle (coût d'authentification hors périmètre).
  Non comptée comme PASS nouveau ; aucune régression structurelle introduite.

## 7. Accessibilité

Détail dans [`audit-accessibilite-final.md`](audit-accessibilite-final.md). WCAG 1.4.11 = PASS
technique. Le 10ᵉ point exige la passe lecteur d'écran, **non exécutable** ici → **9/10 maintenu**.

## 8. Risques résiduels

1. **D-04 (infra)** — clés DataProtection par défaut ; robustesse antiforgery/session au redémarrage
   / scale-out dépend d'une décision de déploiement. **Non résolu par code** (délibérément).
2. **Audit lecteur d'écran** — requis pour toute revendication WCAG AA formelle ; non exécutable ici.
3. **AM-03 résiduel** — étendre le patron de confirmation aux autres actions destructives
   (ex. désactivation de catégorie). Non bloquant : contrôle serveur en place.
4. **Recette multi-persona non rejouée** ce cycle (voir §6).

## 9. Score — recalcul traçable

Base : **95/100**, 5 points manquants ventilés (audit a11y 1 · infra 1 · métier 1 · risque
accepté/faux positif 2).

| Point manquant | Avant | Après | Preuve | Justification |
|---|---|---|---|---|
| Métier (AM-01/02/03) | −1 | **0** | AM-01 mutation ; AM-02 3 tests + mutation ; AM-03 3 tests + mutation | Arbitrages **rendus** (autorisés) et **appliqués+prouvés** sur le cœur. Blocage métier levé. |
| Audit a11y formel | −1 | −1 | — | Lecteur d'écran **non exécutable** ici. Jamais de PASS sans preuve. |
| Infra D-04 | −1 | −1 | — | Décision de topologie, non résolue par code. |
| Risque accepté / faux positif (CSV + `!important`) | −2 | −2 | — | Non re-vérifiés ce cycle ; pas de PASS nouveau sans preuve. |

**Score final : 96/100** (+1, métier).

> Lecture stricte : exiger le rollout AM-03 **exhaustif** avant de créditer le point maintiendrait
> 95/100. Le point est attribué car le **verrou** (arbitrage métier) est levé et la stratégie
> décidée est appliquée et prouvée sur les transitions les plus engageantes. **97+ n'est pas
> revendiqué** : il exige l'audit lecteur d'écran, la décision infra D-04, et les points de risque
> accepté — aucun démontré ce cycle.

## 10. Verdicts

- **UI prête pilote** : **OUI** — changements additifs, testés, sans régression (1626/1626).
- **UI prête production** : **SOUS CONDITIONS** — décision infra D-04 ; audit lecteur d'écran pour
  revendication WCAG AA formelle.
- **Application prête production** : **SOUS CONDITIONS** — mêmes conditions (D-04 déploiement +
  audit a11y externe).
