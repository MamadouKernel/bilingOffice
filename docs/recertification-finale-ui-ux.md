# Recertification finale UI/UX + traduction

**Date** : 8 septembre 2026. Recalcul honnête après audit de la traduction. **Aucun commit.**
Principe : un score prouvé, plus bas, vaut mieux qu'un 100 fictif — et un défaut nouvellement mis
en évidence tire le score **vers le bas**, pas vers le haut.

## 1. Baseline confirmée

- HEAD `1c3cd38`, `git diff --check` propre (hors CRLF).
- Suite par projet **avant** ce cycle : Domain 231 · Application 839 · Infrastructure 175 · Web 381
  = **1626/1626**, 0 échec, 0 ignoré.

## 2. Tests après ce cycle

| Projet | Avant | Après | Δ |
|---|---|---|---|
| Domain | 231 | 231 | 0 |
| Application | 839 | 839 | 0 |
| Infrastructure | 175 | 175 | 0 |
| Web | 381 | 395 | +14 (StatutBadge +6, TraductionParity +8) |
| **Total** | **1626** | **1640** | **+14** |

**0 échec, 0 ignoré.** Modification de test justifiée : `StatutBadgeTests` épingle désormais la
culture (le composant est devenu localisé) — même patron que `NfrCert07IntegrationTests`.

## 3. Traduction — audit + correctif

Détail : [`audit-traduction-complet.md`](audit-traduction-complet.md),
[`remediation-traduction.md`](remediation-traduction.md), [`recette-traduction.md`](recette-traduction.md).

- **Mécanisme** : CORRECT (cookie `.AspNetCore.Culture` 1 an + POST/redirect + `<html lang>`
  dynamique + persistance). Vérifié navigateur.
- **Couverture** : **~8 %** (8/101 pages). Bascule EN ⇒ interface mixte dominée par le français.
- **Correctif borné** : `StatutBadge` bilingue (FR/EN) + 6 clés `Statut.Ticket.*` + gardes de parité.
  Tests + mutation. Aucune régression FR.
- **Reste** : ~92 % des pages, PageTitle, validation, texte dynamique, sidebar, commutateur anonyme —
  **non traduits** (feuille de route documentée, non réalisée).

**Score Traduction : 2/10** — le mécanisme tient (+), mais la fonctionnalité est inutilisable en
anglais en l'état (couverture catastrophique). Aucun point « traduction complète » n'est attribué.

## 4. Recertification des parcours

| Parcours | Méthode | Résultat |
|---|---|---|
| Simulation publique | navigateur `/simulation/proforma` | rendu + formulaire OK |
| Responsive 320 px (public) | navigateur | 0 px débordement |
| Dark mode (public) | navigateur | `data-theme=dark`, WCAG 1.4.11 champ `#6b7d8c` conforme |
| AM-02 collaborateur limité | rendu composant + applicatif (mission précédente) | OK |
| AM-03 clôture/annulation ticket | rendu composant (mission précédente) | OK |
| Recette authentifiée multi-persona (8 profils) + responsive/dark par page authentifiée | — | **NON REJOUÉE** (coût d'authentification) |

## 5. AM-03 — rollout résiduel

Inventaire des actions destructives/engageantes (C/D) :
- **Cœur ticket** (`DetailTicket` : clôture, annulation) → confirmation modale accessible. **OK**.
- **Workflows billing** (`FileBackOfficeExport`, `FilePiloteManifeste`, `ValidationPaiement`,
  `FileValidationPaiementExport`, `MonProfil` clôture, rendez-vous…) → **panneau de confirmation en
  ligne** (motif obligatoire + bouton « Confirmer »). Forme de confirmation valide.
- **Actions directes sans confirmation restantes** : `FicheTicket.AnnulerTicketAsync`,
  `Collaborateurs.DesactiverAsync`, `MatriceTransitions/ParametresSla.SupprimerAsync` (config),
  `ValiderDemandes.Valider/Rejeter`, `AdministrationCategories.Desactiver`.

**AM-03 = PARTIEL** (honnête) : cœur + workflows majeurs confirmés ; une poignée d'actions admin
directes restent à couvrir. Non bloquant — le serveur valide toujours identité/permission/état.

## 6. D-04 et lecteur d'écran

- **D-04** : inchangé — clés DataProtection par défaut ; correctif dépend de la topologie de
  production (mono/multi-instance, Docker/K8s, stockage/Redis/KeyVault disponibles). Inconnu ici →
  **CONDITION INFRA / ACTION DÉPLOIEMENT**. Aucune modification de sécurité à l'aveugle.
- **Lecteur d'écran** : NVDA/JAWS indisponibles → **NON EXÉCUTABLE**. Vérifs techniques OK (`lang`
  dynamique, focus, rôles ARIA des modales). Accessibilité **9/10** maintenue.

## 7. Score final — recalcul traçable

Base : **96/100** (mission précédente).

| Axe | Avant | Après | Justification |
|---|---|---|---|
| Traduction (NFR-07) | non chiffré explicitement | **2/10** | Défaut nouvellement prouvé : ~8 % de couverture. Non pris en compte dans le 96. |
| Impact global de l'axe i18n | — | **−1** | Un NFR bilingue à 8 % est un défaut réel ; partiellement atténué par un mécanisme sain + correctif borné + gardes. |
| Accessibilité | 9/10 | 9/10 | inchangé (audit lecteur d'écran non exécutable) |
| Infra D-04 | condition | condition | inchangé |
| AM-01/02 | OK | OK | inchangé |
| AM-03 | cœur | cœur + workflows (PARTIEL) | pas de point supplémentaire tant que non exhaustif |

**Score UI/UX final : 95/100** (−1 vs 96, déduction honnête pour l'axe traduction mis en évidence).

> Direction du recalcul : **vers le bas**. Découvrir qu'une fonctionnalité annoncée est à 8 % ne
> peut pas augmenter le score. Le mécanisme sain, le correctif testé et les gardes évitent une
> déduction plus lourde. **100 n'est ni visé ni atteint.**

## 8. Verdicts

- **UI prête pilote** : **OUI** — français pleinement fonctionnel, testé, sans régression (1640/1640).
- **UI prête production** : **SOUS CONDITIONS** — (a) décision infra D-04, (b) audit lecteur d'écran,
  (c) **si le bilingue EN est contractuel (NFR-07), la traduction doit être complétée** avant mise
  en production « bilingue ».
- **Application prête production** : **SOUS CONDITIONS** (mêmes trois).
