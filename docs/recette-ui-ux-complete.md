# Recette UI/UX complète — navigateur

**Date** : 7 septembre 2026 · Environnement : `CIT_UAT_RECETTE` sur `https://localhost:7097`.
Comptes UAT exclusivement (`@local.test`), aucune donnée personnelle réelle.

> Les tests automatisés (1584/1584) ne remplacent pas cette étape : chaque item ci-dessous a été
> **exécuté dans le navigateur**.

## 1. Corrections vérifiées

| Item | Preuve navigateur | Verdict |
|---|---|---|
| **D-07** connexion (authentifié) | `admin` → `/comptes/connexion` **redirige** vers `/comptes/valider` ; plus de 500 | ✅ |
| **D-07** connexion (anonyme) | après déconnexion, le formulaire s'affiche, avec `<h1>` | ✅ |
| **D-06** 403 sans code | `superviseur` → `/administration/parametres-sla` : *« Vous devez disposer de la permission d'administration des SLA pour accéder à cette interface. »* — **0 code, 0 GUID** | ✅ |
| **D-18** sélecteur ticket parent | `client.normal` → avoir/exo : `<select>` de **12** demandes (« BIL-IMP-000008 — BL … (DOS-000008) »), **plus de champ UUID** | ✅ |
| **D-16** titre `<h1>` | présent sur connexion, dashboard, simulation… (81/81 en garde) | ✅ |
| **Simulation publique** (non-régression) | anonyme : page ouverte, calcul Dry = **78 550 XOF**, résultat visible, CTA « Connectez-vous pour enregistrer » | ✅ |
| **Boundaries anonyme** | `/tickets/…`, `/simulation/historique`, `/admin/tarification`, `/billing-import/facturation`, `/administration/audit` → **redirigés vers la connexion** | ✅ |

## 2. Mode sombre (balayage réel, contrastes calculés)

| Surface | Constat | Verdict |
|---|---|---|
| Accueil (cartes d'orientation) | titres **15,07:1**, paragraphes **5,64:1** | ✅ AA |
| Tableau de bord client (badges/liens) | badge `bg-info text-dark` **7,78:1** (était 1,94) · lien `text-primary` **5,58:1** (était 4,23) · **0 texte sous le seuil** hors boutons/emojis sur dégradé | ✅ AA |
| Correctif ciblé | `bg-light text-dark` (fond sombre en thème sombre) laissé à `--ink` (clair) — vérifié lisible | ✅ |

*Aucune conformité WCAG globale revendiquée. Les faux positifs du scanner (dégradés, fonds
translucides des boutons) ont été confirmés lisibles.*

## 3. Responsive (débordement horizontal global)

| Taille | Pages testées | Débordement |
|---|---|---|
| 390×844 (mobile) | dashboard, simulation, avoir/exo, import, mes-réclamations | **0** partout |
| 768×1024 · 1024×768 | dashboard client porteur d'un nom de société (cas D-17) | **0** (était +160 px) |
| 1366×768 · 1920×1080 | dashboard, audit | **0** |

## 4. Sidebar — non-régression (lien visible ⇒ destination autorisée)

| Profil | Liens sidebar | Liens interdits |
|---|---|---|
| `superviseur` | 2 | **0** |
| `agent.sansdroits` | 2 | **0** |
| `transporteur` | 7 | **0** |

Complété par 20 tests `NavigationParPermissions` + garde de câblage, et la recette 8 profils du tour
précédent (0 interdit chacun). **Les contrôles serveur restent intacts** — l'absence de lien n'est
jamais une sécurité.

## 5. Éléments vérifiés au build/source (non rejouables isolément en navigateur)

| Item | Vérification |
|---|---|
| **D-02** toast auto-fermeture | source + garde `FinitionsUiTests` (DureeMs, IDisposable, Task.Delay) |
| **UI-09** lien d'évitement | présent dans MainLayout, ancré `#cit-contenu-principal` (garde) |
| **UI-R03** cibles tactiles | `@media (pointer: coarse)` min 44 px |
| **D-20** état vide catégories | `EtatVide` monté (déclenchable seulement base vide) |
| **UI-20** date Draft IN visible | affichée sous le badge « ✅ Émis » |

## 6. Parcours critiques (rejoués ou couverts)

Client Import (D-10 tout-doublon = refus, pas de dossier vide) · Client Export/Draft IN/BEE (tour
précédent, dépôt réel) · Simulation (Dry/Reefer/dangereux/OOG) · Réclamation (catégorie configurable)
· Avoir/Exonération (sélecteur ticket) · Administration (comptes, catégories, SLA refus propre) ·
Journal d'audit (auteurs résolus, 0 « Utilisateur »).

**Recette : conforme.** Aucun défaut technique nouveau observé.
