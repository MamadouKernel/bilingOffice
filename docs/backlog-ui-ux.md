# Backlog UI/UX

> ## 🔄 MIS À JOUR APRÈS RECETTE NAVIGATEUR — 2026-09-06
>
> Voir `docs/recette-ui-ux-navigateur.md`. L'application a été exécutée dans un navigateur sur un
> environnement UAT isolé. **Quatre surfaces publiques sur 81 ont été observées** ; aucun parcours
> authentifié n'a pu l'être, faute de comptes.
>
> **Deux défauts nouveaux, confirmés par capture et mesure** — UI-R01 et UI-R02, en tête de table.
>
> **Deux corrections de l'audit statique :**
> - **UI-16** était classé UX-P3 « aucun impact direct ». C'est **la cause racine de UI-R02** : un
>   fond codé en dur avec un texte tokenisé donne du texte invisible en thème sombre. Sévérité
>   sous-estimée.
> - **UI-14** (« mobile incertain ») était **trop pessimiste** : aucun débordement horizontal à
>   390 px sur les trois surfaces observées. La réserve ne porte plus que sur les tableaux, tous
>   inaccessibles sans compte.
>
> Les entrées UI-01 à UI-20 restent **non revérifiées en navigateur**, sauf mention contraire.

*Issu de `docs/audit-final-ui-ux.md`. Aucun code n'a été modifié.*

> ⚠️ **Backlog distinct du périmètre des 98 RG.** Aucune de ces entrées n'affecte la conformité
> métier (99,0 %) ni le backlog de clôture (`docs/backlog-cloture.md`). Deux entrées recoupent
> néanmoins des dettes techniques déjà connues — signalé en colonne *Défaut*.

> ⚠️ **Couverture d'observation.** Les entrées **UI-R0x** sont confirmées en navigateur. Les entrées
> **UI-01 à UI-20** reposent sur l'analyse statique et attendent encore une observation réelle —
> celle-ci n'a pu porter que sur 4 surfaces publiques.

**Effort :** `XS` < 0,5 j · `S` 0,5–2 j · `M` 2–5 j · `L` > 5 j — indicatif.

---

## Table

| ID | Priorité | Page | Défaut | Impact utilisateur | Recommandation | Effort |
|---|---|---|---|---|---|---|
| **UI-R01** | **UX-P1** | `Home.razor` → `/simulation/proforma` | **CONFIRMÉ EN RECETTE.** Le CTA principal « Simuler une Proforma » mène à « Accès Refusé » pour un visiteur anonyme. La route est publique (allowlist : *« SIM-01 : simulation accessible avant tout parcours »*) mais la requête refuse l'anonyme | Le principal appel à l'action de la vitrine est une impasse. Un prospect venu estimer un coût avant de créer un compte se heurte à un mur | **Arbitrage métier** : soit masquer le CTA aux anonymes et retirer la page de l'allowlist, soit ouvrir réellement la simulation sans compte | S |
| **UI-R02** | **UX-P1** | `Home.razor` | **CONFIRMÉ EN RECETTE (capture + styles calculés).** En thème sombre, les cartes gardent un fond en dur (`#ffffff`, `#f8fafc`) tandis que le texte suit le token `--ink` : contraste **1,17:1** sur les 3 titres d'orientation, **1,12:1** sur un titre de section. En clair : 17,64:1 | Les trois cartes qui orientent un nouvel utilisateur — dont « Transporteur / Chauffeur » — sont sans titre lisible. C'est la porte d'entrée de la plateforme | Remplacer les fonds codés en dur par les tokens de thème sur cette page | S |
| **UI-R03** | UX-P2 | `SimulationProformaPage`, `Connexion` | **CONFIRMÉ EN RECETTE.** Zones tactiles sous 44 px : 2 boutons sur 4 (simulation), 1 sur 6 (connexion) | Cibles difficiles à atteindre au doigt sur mobile | Porter la hauteur minimale des boutons à 44 px | XS |
| **UI-01** | **UX-P0** | `Administration/ComptesAgents.razor` | « ⭐ Tout accorder (SuperAdmin) » et « Tout retirer » : action irréversible d'un clic, sans confirmation, non atomique, hors du wrapper d'erreur *(recoupe D-02 / D-03)* | Un clic mal placé confère ~60 permissions à un compte, ou les retire toutes. En cas d'échec en cours de boucle, l'état reste partiel et muet | Brancher `ModaleConfirmation` (le composant existe) en nommant le compte cible et le nombre de permissions ; router par `ExecuterAsync` ; rendre la boucle atomique | **XS** *(confirmation seule)* · **S** *(avec atomicité)* |
| **UI-02** | UX-P1 | Transversal — 233 `@onclick` | Aucune protection contre la double soumission : 2 pages seulement portent un verrou d'action en cours. Les 68 `disabled="@…"` valident la complétude du formulaire, pas l'envoi en cours | Un double-clic sur « Soumettre » crée deux demandes ; sur « Prendre en charge », le serveur protège par jeton optimiste mais l'utilisateur ne le voit pas | Drapeau `_enCours` désactivant le bouton pendant l'appel, avec libellé « Envoi en cours… ». Commencer par les 10 boutons de soumission des parcours principaux | **S** |
| **UI-03** | UX-P1 | Transversal — 12 « Désactiver », 6 « Retirer », 5 « Supprimer », 7 « Clôturer », 10 « Rejeter » | `ModaleConfirmation.razor` existe et a **zéro référence** dans tout le code. Aucune action destructive n'est confirmée | L'utilisateur déclenche une action irréversible sans point d'arrêt ; le seul recours est le support | Brancher le composant existant sur les actions destructives, en expliquant la conséquence plutôt qu'en demandant « Confirmer ? » | **M** |
| **UI-04** | UX-P1 | 55 pages avec tableau | Aucune pagination. `JournalAudit`, files agent et `ComptesAgents` chargent l'intégralité des lignes | Sur volume réel, page lente et liste illisible. Le CDC signale lui-même un risque de sous-estimation de la volumétrie | Pagination serveur sur les surfaces à croissance non bornée en priorité : journal d'audit, files agent | **M** |
| ~~**UI-05**~~ | ⚪ **SANS OBJET** | `Components/Layout/NavMenu.razor` | **FAUX POSITIF DE L'AUDIT — retiré.** Le menu **est** filtré, par 4 `@switch` sur `EspaceActif` et 18 branches `case` ; l'audit avait compté les `@if` et conclu à tort. `EspacesDisponibles` est calculé côté serveur depuis les permissions réelles (`ObtenirContexteUtilisateurQuery`) | Aucun | **Aucune action.** Voir `docs/remediation-ui-ux-p0-p1.md` § 5 | — |
| **UI-06** | UX-P1 | Transversal — 68 pages | `_erreur = ex.Message` affiche le message de **toute** exception, y compris une `SqlException` ou une erreur d'infrastructure | L'utilisateur reçoit un message technique incompréhensible, potentiellement révélateur de la structure interne | Filtrer : messages métier connus affichés tels quels, le reste remplacé par un libellé générique + identifiant de corrélation journalisé | **S** |
| **UI-07** | UX-P2 | 26 pages *(dont `MesDemandesAvoirExo`, `MesRendezVous`)* | Tableau affiché sans `EtatVide` alors que le composant est utilisé sur 31 autres pages | Un client sans demande voit un tableau vide sans savoir s'il n'a rien déposé, si un filtre l'exclut, ou si le chargement a échoué | Ajouter `EtatVide` avec un titre, une explication et l'action suivante. **Priorité aux 2 surfaces client** | **S** |
| **UI-08** | UX-P2 | Transversal | Icônes emoji (🏠 🔑 ⭐ ✅ ⏳) sans `aria-hidden`. 12 `aria-label` pour 233 boutons | Les lecteurs d'écran annoncent « étoile blanche », « clé » sur des éléments purement décoratifs. Certains boutons peuvent n'avoir aucun nom accessible | `aria-hidden="true"` sur les emoji décoratifs ; `aria-label` sur les boutons dont le libellé est porté par l'icône seule | **XS** *(aria-hidden)* · **S** *(audit des boutons)* |
| **UI-09** | UX-P2 | `Components/Layout/MainLayout.razor` | Aucun lien d'évitement vers le contenu principal | Un utilisateur au clavier traverse 146 entrées de menu avant d'atteindre le contenu, à chaque page | Ajouter un lien « Aller au contenu » visible au focus, ciblant le conteneur principal | **XS** |
| **UI-10** | UX-P2 | `Components/Layout/NavMenu.razor` | 8 `Match="NavLinkMatch"` sur 146 `NavLink` ; les autres retombent sur `Prefix` | Plusieurs entrées peuvent apparaître actives en même temps ; l'utilisateur ne sait pas où il est | Déclarer `NavLinkMatch.All` sur les routes qui sont préfixes d'autres routes | **S** |
| **UI-11** | UX-P2 | Files agent *(12 pages)* | Ni tri de colonne, ni filtre. Seules `ComptesAgents` et `RechercheGuichet` en proposent | Un agent avec 80 tickets en file ne peut pas prioriser autrement qu'à l'œil | Tri sur les colonnes de date et de statut ; filtre sur le statut | **M** |
| **UI-12** | UX-P2 | `Pages/Tickets/IndicateursTemps.razor` | Seule page de l'application à référencer directement `PlateformeDbContext`, contournant la couche applicative | Aucun pour l'utilisateur. Risque de maintenance : cette lecture échappe aux contrôles d'accès centralisés que toutes les autres pages traversent | Introduire une requête applicative dédiée, comme les 25 autres pages | **S** |
| **UI-13** | UX-P2 | `Administration/ComptesAgents.razor` | Seule page à définir ses propres classes dans un bloc `<style>` ; introduit un vocabulaire visuel non partagé | Rupture perceptible avec le reste de l'administration | Promouvoir les classes utiles dans `app.css` avec le préfixe `cit-`, ou aligner sur les cartes existantes | **S** |
| **UI-14** | UX-P2 🔽 *(revu à la baisse — recette)* | Transversal — 55 tableaux | Rendu mobile non vérifié : un seul point de rupture propre sous 991.98px (`575.98px`), 12 `d-none`, et des tableaux à 5-6 colonnes | Sur une application destinée à des transporteurs et chauffeurs s'enrôlant à distance, un mobile inutilisable bloque le module Enrôlement | **Vérifier réellement** en 390×844 avant toute correction. Puis, si confirmé : vue « carte » sur mobile pour les listes principales | **S** *(vérification)* · **M–L** *(correction)* |
| **UI-15** | UX-P3 | `Components/Shared/ToastNotification.razor` | Composant construit, **zéro référence**. Le retour d'action passe partout par une alerte inline | Aucun impact direct — le retour existe. Coût : un composant maintenu sans usage | Brancher sur les actions rapides *(bascule de permission, marquage de notification lue)*, ou supprimer | **S** |
| **UI-16** | **UX-P1** 🔼 *(revu à la hausse — cause de UI-R02)* | 5 pages *(Home 34, SoumettreDemande 29, InscriptionTransporteur 19, ComptesAgents 16, Connexion 13)* | 215 `style=` en ligne et ~40 couleurs hexadécimales dont plusieurs doublonnent des tokens (`#196b9f`, `#173f82`, `#0f2b5c`) | Aucun impact direct. Un changement de charte demanderait de reprendre ces pages une par une | Remplacer les couleurs par les variables existantes ; promouvoir les motifs récurrents en classes `cit-` | **M** |
| **UI-17** | UX-P3 | `wwwroot/app.css` | 80 `!important` — symptôme d'une lutte de spécificité contre Bootstrap | Aucun impact direct. Toute surcharge future devient difficile | Réduire par montée de spécificité des sélecteurs `cit-`, sans réécriture globale | **M** |
| **UI-18** | UX-P3 | Transversal | Formats de date secondaires : `HH:mm:ss` (1), `dd/MM/yyyy à HH:mm` (2), face à `dd/MM/yyyy HH:mm` (43) | Incohérence perceptible d'un écran à l'autre | Aligner sur `dd/MM/yyyy HH:mm`. *Note : l'UTC affiché est correct — la Côte d'Ivoire est à UTC+0* | **XS** |
| **UI-19** | UX-P3 | Transversal | Aucun fil d'Ariane sur les fiches et les pages de détail | Sur une fiche ticket atteinte depuis un dossier, l'utilisateur ne voit pas la hiérarchie ni le chemin de retour | Fil d'Ariane sur les pages de détail : Dossier → Ticket → Documents | **M** |
| **UI-20** | UX-P3 | `BillingExport/FileFacturationExport.razor` | Date d'émission du Draft IN portée par un attribut `title` seul | Invisible au tactile et aux lecteurs d'écran ; l'agent ne sait pas quand le Draft IN a été émis | Afficher la date sous le badge, ou l'ajouter au libellé | **XS** |

---

## Synthèse

| Priorité | Nombre |
|---|---|
| **UX-P0** | **1** |
| **UX-P1** | **5** |
| **UX-P2** | **8** |
| **UX-P3** | **6** |
| **Total** | **20** |

### Par nature

| Nature | Entrées |
|---|---|
| **Composant existant non branché** | UI-01, UI-03, UI-15 |
| **Protection d'action manquante** | UI-01, UI-02, UI-03 |
| **Volume / passage à l'échelle** | UI-04, UI-11 |
| **Accessibilité** | UI-08, UI-09, UI-20 |
| **Cohérence du design system** | UI-13, UI-16, UI-17, UI-18 |
| **Non vérifiable sans navigateur** | UI-14 |

### Quick wins — faible effort, fort impact, faible risque

| Ordre | ID | Effort | Gain |
|---|---|---|---|
| 1 | **UI-01** *(confirmation seule)* | XS | **Ferme le seul UX-P0** |
| 2 | UI-08 *(`aria-hidden`)* | XS | Restitution lecteur d'écran |
| 3 | UI-09 *(lien d'évitement)* | XS | Navigation clavier |
| 4 | UI-07 *(2 surfaces client)* | XS | Compréhension client |
| 5 | UI-18 *(format de date)* | XS | Cohérence |
| 6 | UI-20 *(date Draft IN)* | XS | Lisibilité de la nouveauté |
| 7 | **UI-R02** *(fonds en dur → tokens de thème)* | S | Rend lisibles les titres d'orientation en thème sombre |
| 8 | UI-02 *(verrou sur 10 boutons)* | S | Couvre l'essentiel de la double soumission |

**Les six premiers sont des ajouts, pas des refontes.** Le premier, seul, supprime le défaut classé
P0.

---

## Séquencement proposé

| Lot | Contenu | Critère de sortie |
|---|---|---|
| **UI-A — avant pilote** | UI-01 · UI-02 · UI-05 · quick wins 2 à 6 | Aucun UX-P0 · confirmation sur toute action destructive en masse · menu sans impasse |
| **UI-B — avant production** | **Inspection navigateur réelle** *(préalable à UI-14)* · UI-03 · UI-04 · UI-06 | Rendu vérifié sur 5 surfaces et en 390×844 · pagination sur les surfaces non bornées |
| **UI-C — court terme** | UI-07 · UI-10 · UI-11 · UI-12 · UI-13 | États vides complets · navigation lisible · couche applicative respectée |
| **UI-D — polish** | UI-15 · UI-16 · UI-17 · UI-19 | Design system resserré · fil d'Ariane |

**UI-B commence par une vérification, pas par une correction.** Aucune décision de responsive
mobile ne devrait être prise sur la base de cet audit : il n'a rien vu.
