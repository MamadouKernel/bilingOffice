# Audit final UI/UX

> ## ⚠️ AUCUNE INSPECTION NAVIGATEUR N'A ÉTÉ RÉALISÉE
>
> **Tout ce rapport repose sur l'analyse statique du code.** Aucune page n'a été rendue, aucune
> capture n'a été prise, aucune taille d'écran n'a été éprouvée. Les constats de rendu, de contraste
> et de responsive sont des **déductions depuis le CSS et le balisage**, pas des observations.
>
> Le score porte la mention **SCORE PROVISOIRE — ANALYSE STATIQUE**.

*Mode AUDIT / READ ONLY. Aucun fichier n'a été modifié.*

---

## 1. Résumé exécutif

L'application repose sur un **design system réel** — 135 variables CSS dans un `app.css` de
1458 lignes, au-dessus de Bootstrap 5 — et sur une **bibliothèque de dix composants partagés**. Ce
n'est pas un assemblage d'écrans : la cohérence est voulue, et pour l'essentiel tenue.

Trois constats dominent, et le premier est le plus important.

**Deux composants partagés sont construits et jamais utilisés.** `ModaleConfirmation.razor` et
`ToastNotification.razor` ont **zéro référence** dans tout le code. La conséquence est directe et
mesurable : **aucune action destructive de l'application n'est protégée par une confirmation**, alors
que le composant qui devrait la porter existe. C'est la signature exacte que ce projet a rencontrée
quatre fois côté serveur — une règle écrite que rien n'exerce — reproduite ici côté interface.

**Aucune action n'est protégée contre la double soumission.** 233 gestionnaires `@onclick` sont
répartis dans l'application ; **deux pages seulement** portent un indicateur d'action en cours. Les
68 `disabled="@…"` recensés valident la complétude d'un formulaire, pas le fait qu'un envoi soit déjà
parti.

**Aucun tableau n'est paginé.** 55 pages affichent un tableau ; aucune n'implémente de pagination.
En contrepartie, **les 55 sont dans un conteneur `table-responsive`** — sur ce point la couverture
est totale.

Le reste est solide : chargement couvert sur 66 pages sur 81, refus d'accès traité sur 51, formats de
date et de montant cohérents, libellés de statut passant par un référentiel.

---

## 2. Méthode

| Vérification | Réalisée | Moyen |
|---|---|---|
| Inventaire des surfaces routables | ✅ | Analyse des directives `@page` |
| Design system, tokens, styles | ✅ | Lecture de `app.css` + mesure des styles dispersés |
| Composants partagés et leur usage réel | ✅ | Comptage des références par composant |
| États loading / empty / error | ✅ | Recherche des composants et des drapeaux d'état |
| Formulaires, labels, validations | ✅ | Comptage balises et attributs |
| Double soumission | ✅ | Recherche des verrous d'action en cours |
| Tableaux, responsive, modales | ✅ *(statique)* | Classes Bootstrap, media queries |
| Accessibilité | ⚠️ **partiel** | Marqueurs ARIA, labels, focus — **aucun test d'écran** |
| **Rendu visuel** | ❌ | **NON VÉRIFIÉ VISUELLEMENT** |
| **Contraste effectif** | ❌ | **NON VÉRIFIÉ** — aucune mesure de ratio |
| **Responsive réel** | ❌ | **NON VÉRIFIÉ** — aucune taille éprouvée |
| **Parcours de bout en bout** | ❌ | **NON EXÉCUTÉS** — reconstruits par lecture du code |

### Pourquoi l'application n'a pas été lancée

Ce n'est pas un renoncement, c'est une conséquence de la consigne.

`Program.cs` applique **automatiquement les migrations au démarrage** (`MigrateAsync`) et, en
environnement de développement, peut **supprimer puis recréer la base** (`EnsureDeletedAsync`) si
l'historique EF est absent. Or cette mission interdit explicitement de modifier la base et de toucher
aux migrations — et les deux migrations post-audit sont déclarées « non exécutées » depuis trois
rapports.

S'ajoute un obstacle matériel : `ConnectionStrings:PlateformeDb` est **vide** dans les quatre
`appsettings*.json` — par conception. Lancer l'application supposait d'injecter une configuration.

**Lancer l'application aurait violé la règle absolue de cette mission.** Le rapport en porte le coût :
tout ce qui touche au rendu est marqué non vérifié.

---

## 3. Inventaire UI

**100 composants `.razor`**, dont **81 fichiers routables** portant **83 routes** (deux pages
déclarent deux routes). **10 composants partagés**, **9 fichiers de layout**.

### Matrice des surfaces

*Colonne « Responsive » : présence des mécanismes (grille Bootstrap, `table-responsive`) —
**pas** une vérification visuelle. Colonne « État » : couverture loading / empty / error.*

| Page | Route | Profil | Fonction | Layout | Responsive | État |
|---|---|---|---|---|---|---|
| Home | `/` | Public | Accueil | Main | grille | L— |
| Connexion | `/comptes/connexion` | Public | Authentification | Main | grille | L·E |
| VerifierCode | `/comptes/verifier-code` | Public | 2FA | Main | grille | L·E |
| MotDePasseOublie | `/comptes/mot-de-passe-oublie` | Public | Récupération | Main | grille | L·E |
| ReinitialiserMotDePasse | `/comptes/reinitialiser-mot-de-passe` | Public | Récupération | Main | grille | L·E |
| SoumettreDemande *(compte)* | `/comptes/soumettre` | Public | Création société | Main | grille | L·E |
| InscriptionTransporteur | `/comptes/inscription-transporteur` | Public | Enrôlement | Main | grille | L·E |
| AccepterInvitation | `/comptes/invitation/{Jeton}` | Public | Invitation | Main | grille | L·E |
| CompteCloture | `/comptes/compte-cloture` | Public | Confirmation | Main | statique | — |
| AccesRefuse | `/comptes/acces-refuse` · `/acces-refuse` | Public | Refus | Main | statique | — |
| Error | `/Error` | Public | Erreur | Main | statique | — |
| NotFound | `/not-found` | Public | 404 | Main | statique | — |
| SimulationProformaPage | `/simulation/proforma` | Public | Simulation | Main | grille | L·E·Er |
| DashboardClient | `/client/dashboard` | Client | Tableau de bord | Main | grille | L·E·Er |
| MesDemandes | `/client/demandes` | Client | Liste | Main | table-resp | L·E·Er |
| SoumettreDemande *(import)* | `/billing-import/soumettre` | Client | Dépôt IMPORT | Main | grille | L·Er |
| SoumettreDemandeExport | `/billing-export/soumettre` | Client | Dépôt EXPORT | Main | grille | L·Er |
| MesReclamations | `/reclamations/mes-reclamations` | Client | Liste | Main | table-resp | L·E·Er |
| NouvelleReclamation | `/reclamations/soumettre` | Client | Formulaire | Main | grille | L·Er |
| MesEnrolements | `/enrolement/mes-enrolements` | Transporteur | Liste | Main | table-resp | L·E·Er |
| NouvelEnrolement | `/enrolement/soumettre` | Transporteur | Formulaire | Main | grille | L·Er |
| MesDemandesAvoirExo | `/avoir-exoneration/mes-demandes` | Client | Liste | Main | table-resp | L·**—**·Er |
| NouvelleDemandeAvoirExo | `/avoir-exoneration/soumettre` | Client | Formulaire | Main | grille | L·Er |
| MesRendezVous | `/rendez-vous/mes-rendez-vous` | Client | Liste | Main | table-resp | L·**—**·Er |
| PrendreRendezVous | `/rendez-vous/prendre/{TicketId}` | Client | Formulaire | Main | grille | L·Er |
| MonProfil | `/comptes/mon-profil` | Tous | Profil + clôture | Main | table-resp | L·**—**·Er |
| ActiverDoubleAuthentification | `/comptes/double-authentification` | Tous | 2FA | Main | grille | L·Er |
| CentreNotifications | `/notifications` | Tous | Liste | Main | grille | L·E·Er |
| MesPreferencesNotification | `/notifications/preferences` | Tous | Préférences | Main | grille | L·Er |
| AssistantVirtuel | `/assistant` | Client | Assistant | Main | grille | L·Er |
| FicheTicket | *(via dossiers/tickets)* | Tous | Fiche 723 l. | Main | table-resp | L·**—**·Er |
| FicheDossier | `/dossiers/{Id}` | Tous | Fiche | Main | table-resp | L·**—**·Er |
| DetailDossier | `/dossiers/{DossierId}` | Tous | Détail | Main | table-resp | L·**—**·Er |
| Documents | *(via fiche)* | Tous | Pièces | Main | table-resp | L·**—**·Er |
| ConsulterTicketCloture | `/billing-import/ticket-cloture[/{TicketId}]` | Tous | Archive | Main | table-resp | L·E·Er |
| DashboardAgent | `/agent/dashboard` | Agent | Tableau de bord | Main | grille | L·E·Er |
| FilePiloteManifeste | `/billing-import/manifeste` | Agent | File | Main | table-resp | L·E·Er |
| FileFacturation | `/billing-import/facturation` | Agent | File | Main | table-resp | L·E·Er |
| ValidationPaiement | `/billing-import/validation-paiement` | Agent | File | Main | table-resp | L·E·Er |
| FileComptabiliteClient | `/billing-import/comptabilite-client` | Agent | File | Main | table-resp | L·E·Er |
| FileControleDocumentaire | `/billing-import/controle-documentaire` | Agent | File | Main | table-resp | L·E·Er |
| FileIes | `/billing-import/ies` | Agent | File IES | Main | table-resp | L·E·Er |
| FileBackOfficeExport | `/billing-export/back-office` | Agent | File | Main | table-resp | L·E·Er |
| FileFacturationExport | `/billing-export/facturation` | Agent | **Draft IN + facture** | Main | table-resp | L·E·Er |
| FileValidationPaiementExport | `/billing-export/validation-paiement` | Agent | File | Main | table-resp | L·E·Er |
| FileComptabiliteExport | `/billing-export/comptabilite` | Agent | File | Main | table-resp | L·E·Er |
| FileControleDocumentaireExport | `/billing-export/controle-documentaire` | Agent | File | Main | table-resp | L·E·Er |
| FileIesExport | `/billing-export/ies` | Agent | File IES | Main | table-resp | L·E·Er |
| FileReclamations | `/reclamations/file` | Agent | File | Main | table-resp | L·E·Er |
| FileAgentGate | `/enrolement/file-agent-gate` | Agent | File | Main | table-resp | L·E·Er |
| FicheEnrolement | `/enrolement/{TicketId}` | Agent | Fiche | Main | table-resp | L·**—**·Er |
| FileAvoirExoneration | `/avoir-exoneration/file` | Agent | File | Main | table-resp | L·**—**·Er |
| FicheAvoirExoneration | `/avoir-exoneration/fiche/{TicketId}` | Agent | Fiche 409 l. | Main | table-resp | L·Er |
| RechercheGuichet | `/guichet/recherche` | Agent | Recherche | Main | table-resp | L·**—**·Er |
| AgendaGuichet | `/guichet/agenda` | Agent | Agenda | Main | table-resp | L·**—**·Er |
| IndicateursTemps | *(via fiche)* | Agent | KPI temps | Main | grille | L·Er |
| TableauDeBord | `/supervision` | Superviseur | Supervision 531 l. | Main | table-resp | L·**—**·Er |
| Disponibilite | `/administration/disponibilite` | Superviseur | Disponibilité | Main | table-resp | L·E·Er |
| DashboardSociete | `/societe/dashboard` | Admin société | Tableau de bord | Main | table-resp | L·**—**·Er |
| Collaborateurs | `/societe/collaborateurs` · `/comptes/collaborateurs` | Admin société | Gestion 460 l. | Main | table-resp | L·**—**·Er |
| ProfilSociete | `/comptes/profil` | Admin société | Profil | Main | grille | L·Er |
| ComptesAgents | `/administration/comptes-agents` | Admin CIT | **Comptes + habilitations 830 l.** | Main | table-resp | L·**—**·Er |
| ValiderDemandes | `/comptes/valider` | Admin CIT | Validation | Main | table-resp | L·**—**·Er |
| ModifierTypeClient | `/comptes/modifier-type` | Admin CIT | Typage | Main | grille | L·Er |
| JournalAudit | `/administration/audit` | Admin CIT | Audit | Main | table-resp | L·E·Er |
| ParametresSla | `/administration/parametres-sla` | Admin CIT | SLA | Main | table-resp | L·**—**·Er |
| MatriceTransitions | `/administration/matrice-transitions` | Admin CIT | Workflow | Main | table-resp | L·**—**·Er |
| MotifsTransfert | `/administration/motifs-transfert` | Admin CIT | Référentiel | Main | table-resp | L·**—**·Er |
| ReglesEscalade | `/administration/regles-escalade` | Admin CIT | Escalade | Main | table-resp | L·**—**·Er |
| PerimetresSupervision | `/administration/perimetres-supervision` | Admin CIT | Périmètres | Main | table-resp | L·**—**·Er |
| CompetencesOperateur | `/administration/competences-operateur` | Admin CIT | Compétences | Main | table-resp | L·**—**·Er |
| EtapesBloquantesAnnulation | `/administration/etapes-bloquantes-annulation` | Admin CIT | Seuils | Main | table-resp | L·E·Er |
| PiecesExigeesCompte | `/administration/pieces-exigees-compte` | Admin CIT | Référentiel | Main | table-resp | L·**—**·Er |
| PiecesExigeesEtape | `/administration/pieces-exigees-etape` | Admin CIT | Référentiel | Main | table-resp | L·**—**·Er |
| StrategieFile | `/administration/strategie-file` | Admin CIT | Affectation | Main | grille | L·E·Er |
| AdministrationNotifications | `/administration/notifications` | Admin CIT | Modèles | Main | table-resp | L·**—**·Er |
| AdministrationCategories | `/reclamations/administration-categories` | Admin CIT | Catégories | Main | table-resp | L·**—**·Er |
| GestionGrillesTarifaires | `/admin/tarification` | Admin CIT | Grilles | Main | table-resp | L·E·Er |
| HistoriqueSimulationsPage | `/simulation/historique` | Client/Agent | Historique | Main | table-resp | L·E·Er |
| DetailSimulationPage | `/simulation/{Id}` | Client/Agent | Détail | Main | grille | L·Er |

**Légende État** — `L` indicateur de chargement · `E` état vide · `Er` gestion d'erreur ·
**—** état vide absent alors que la page affiche un tableau.

### Cohérence avec `SurfaceRoutableTests`

La garde d'architecture énumère **13 pages publiques assumées** ; l'inventaire ci-dessus en recense
exactement **13** sans `[Authorize]`. **Aucun écart.** Toutes les autres pages routables portent
l'attribut, et la garde le vérifie à chaque exécution.

---

## 4. Design system

### Ce qui existe réellement

| Élément | Constat |
|---|---|
| **Base** | Bootstrap 5 (`lib/bootstrap/css/bootstrap.min.css`) |
| **Surcouche** | `wwwroot/app.css` — **1458 lignes**, **135 variables CSS** |
| **Tokens** | Palette nommée (`--azure-50/100/500/600/700`, `--deep-500→900`, `--ink`, `--ink-2`, `--ink-3`, `--ground`, `--accent`, `--action`), élévations (`--lift-1`), variables Bootstrap surchargées (`--bs-primary`, `--bs-border-radius`, `--bs-body-font-family`) |
| **Nommage** | Préfixe applicatif cohérent : `cit-page-header`, `cit-page-title`, `cit-card`, `cit-nav-item`, `cit-accordion-group`, `cit-user-dropdown` |
| **Composants partagés** | 10 — voir § 5 |

**C'est un vrai design system**, pas une feuille de style accumulée : la palette est nommée et
dérivée, les variables Bootstrap sont surchargées au lieu d'être combattues, et le préfixe `cit-`
délimite proprement la surcouche.

### Ce qui l'affaiblit

| Signal | Mesure | Lecture |
|---|---|---|
| `style="…"` en ligne | **215 occurrences** | Concentrées : Home (34), SoumettreDemande compte (29), InscriptionTransporteur (19), ComptesAgents (16), Connexion (13) |
| `!important` | **80 dans `app.css`**, 10 dans les pages | Volume élevé — symptôme d'une lutte contre la spécificité Bootstrap |
| Bloc `<style>` en page | **1** — `ComptesAgents.razor` | Seule page à définir ses propres classes localement |
| Couleurs hexadécimales en `.razor` | ~40 occurrences, 12 teintes | `#ffffff` (19×) est bénin ; `#196b9f`, `#173f82`, `#0f2b5c`, `#f59e0b` doublonnent des tokens existants |
| Tailles fixes | présentes dans les blocs inline | Non tokenisées |

Les 215 styles en ligne se concentrent sur **cinq pages** — dont quatre sont des surfaces publiques
au traitement graphique particulier (accueil, création de compte, inscription, connexion). Ce n'est
pas une dispersion générale : c'est une exception assumée sur les vitrines, plus une page
d'administration récemment refondue.

> ### COHÉRENCE DESIGN : **BONNE**
>
> Un système existe, il est nommé, il est appliqué. Il n'atteint pas « excellente » à cause des
> 80 `!important`, des 215 styles en ligne et de la page qui introduit ses propres classes.

---

## 5. Cohérence

### Les dix composants partagés, et leur usage réel

| Composant | Pages | Verdict |
|---|---|---|
| `IndicateurChargement` | **66** | ✅ excellent |
| `PermissionDenied` | **51** | ✅ excellent |
| `EtatVide` | **31** | 🟡 partiel |
| `StatutBadge` | **17** | ✅ bon |
| `ChampMotDePasse` | **6** | ✅ cohérent |
| `ChronologieTicket` | 2 | ✅ usage spécifique |
| `ZoneActionsRequises` | 1 | 🟡 usage unique |
| `BandeauDisponibiliteAgent` | 1 | 🟡 usage unique |
| **`ModaleConfirmation`** | **0** | 🔴 **construit, jamais utilisé** |
| **`ToastNotification`** | **0** | 🔴 **construit, jamais utilisé** |

**Les deux composants morts ne sont pas décoratifs.** `ModaleConfirmation` est exactement ce qui
manque aux actions destructives (§ 11 de la mission), et `ToastNotification` exactement ce qui
manque au retour d'action. Ils ont été écrits pour ces besoins et n'ont jamais été branchés.

### Ruptures visuelles identifiées

| Rupture | Constat |
|---|---|
| **`ComptesAgents.razor`** | Seule page avec un bloc `<style>` propre (cartes de statistiques, badge « super-admin », icônes rondes). Elle introduit un vocabulaire visuel que nulle autre page ne partage |
| **Pages publiques vs applicatives** | Accueil, connexion, création de compte, inscription transporteur concentrent 95 des 215 styles en ligne — traitement « vitrine » distinct du reste |
| **Retour d'action** | Uniforme partout — alerte Bootstrap inline (`_message` / `_succes` / `_erreur`, 68 pages, 128 alertes). Cohérent, mais rudimentaire, et sans le composant toast prévu |

Hors ces trois points, les en-têtes (`cit-page-header` / `cit-page-title` / `cit-page-subtitle`), les
cartes (`cit-card`), les badges de statut et les boutons Bootstrap sont homogènes d'un module à
l'autre. **L'utilisateur a bien affaire à une seule application.**

---

## 6. Navigation

| Élément | Constat |
|---|---|
| Structure | Menu latéral en accordéons `<details>`, groupés par **espace fonctionnel** |
| `PersonaSwitcher` | Bascule entre espaces disponibles — Client, Entreprise, Opérateur CIT, Supervision, Administration CIT — affiché seulement si `EspacesDisponibles.Count > 1` ✅ |
| `HeaderBar` | Nom affiché, espace actif, société et profil, menu utilisateur, déconnexion ✅ |
| Page active | **8 `Match="NavLinkMatch"` sur 146 `NavLink`** — les 138 autres retombent sur `Prefix`, qui peut allumer plusieurs entrées simultanément |
| **Filtrage par permission** | **Absent.** Un seul `@if` de visibilité dans 383 lignes ; il distingue *anonyme / authentifié*, pas les profils |
| Breadcrumbs | **Aucun** — aucune page n'en porte |
| Liens morts | Aucun lien vers une route inexistante détecté |

### Le point à retenir

Une fois authentifié, **l'utilisateur voit l'ensemble des groupes du menu**, quel que soit son
profil. Un client voit les entrées d'administration ; il les clique ; le serveur refuse ; la page
`PermissionDenied` s'affiche.

**Ce n'est pas une faille.** Le refus est serveur, correct, et couvert par 51 pages utilisant
`PermissionDenied` — l'audit de sécurité l'a établi. C'est un **défaut d'expérience** : un menu qui
propose beaucoup d'impasses apprend à l'utilisateur à s'en méfier.

*La visibilité UI n'est pas une frontière de sécurité — et l'ajouter ici ne changerait rien à la
sécurité, seulement au confort.*

---

## 7. Formulaires

| Mesure | Valeur | Lecture |
|---|---|---|
| `<label>` | **137** | ✅ |
| Champs de saisie | **109** | Ratio label/champ > 1 — bon signe |
| `ChampMotDePasse` partagé | 6 pages | ✅ traitement homogène des mots de passe |
| `disabled="@…"` | 68 | Majoritairement conditionné à la complétude du formulaire |
| Retour après succès | 68 pages avec `_message`/`_succes` | ✅ |

### Points faibles

- **Aucun verrou d'action en cours** (voir § 8) : le bouton reste actif pendant l'appel.
- **Validation** : côté serveur systématiquement (les commandes lèvent `ArgumentException` /
  `AccesRefuseException`) ; côté client, la couverture n'a pas pu être établie sans rendu — les
  formulaires utilisent un mélange de `@bind` natif et de composants `Input*`.
- **Messages d'erreur** : proviennent des exceptions applicatives (`_erreur = ex.Message`). Ils sont
  rédigés en français métier dans les commandes — mais une exception non anticipée exposerait son
  message technique. Voir § 9.

---

## 8. Feedback

| Mécanisme | Présence |
|---|---|
| Alerte inline (`alert alert-success` / `alert-danger`) | **128 occurrences, 68 pages** ✅ |
| Message de succès explicite après action | ✅ largement présent |
| **Toast** | ❌ composant existant, **0 usage** |
| **Modale de confirmation** | ❌ composant existant, **0 usage** |
| `confirm()` JavaScript | **0** |
| Redirection après action | présente sur les parcours de création |

**Le retour d'action existe et il est cohérent** — c'est le point fort. Ce qui manque est
l'**avant** : rien ne demande confirmation avant une action irréversible.

---

## 9. Loading / Empty / Error

| État | Couverture | Détail |
|---|---|---|
| **LOADING** | **66 / 81 pages** ✅ | `IndicateurChargement` + drapeau `_chargement` (67 pages) |
| **EMPTY** | **31 pages** 🟡 | **26 pages affichant un tableau n'ont aucun état vide** |
| **ERROR** | **68 pages** ✅ | `try/catch` distinguant `AccesRefuseException` (→ `PermissionDenied`) des autres |

### Les 26 tableaux sans état vide

`CompetencesOperateur` · `ComptesAgents` · `MatriceTransitions` · `MotifsTransfert` ·
`ParametresSla` · `PerimetresSupervision` · `PiecesExigeesCompte` · `PiecesExigeesEtape` ·
`ReglesEscalade` · `FileAvoirExoneration` · `MesDemandesAvoirExo` · `MonProfil` · `ValiderDemandes` ·
`AdministrationCategories` · `DetailDossier` · `FicheDossier` · `FicheEnrolement` · `AgendaGuichet` ·
`MesRendezVous` · `RechercheGuichet` · `AdministrationNotifications` · `Collaborateurs` ·
`DashboardSociete` · `TableauDeBord` · `Documents` · `FicheTicket`

Deux d'entre elles sont des surfaces client (`MesDemandesAvoirExo`, `MesRendezVous`) où l'absence
d'explication est la plus coûteuse : un client sans demande voit un tableau vide sans savoir s'il n'a
rien déposé ou si un filtre l'exclut.

### Classement des erreurs

| Catégorie | Constat |
|---|---|
| **ERREUR BIEN GÉRÉE** | Le cas dominant — `AccesRefuseException` → `PermissionDenied` (51 pages) ; erreurs métier → alerte inline |
| **ERREUR TECHNIQUE EXPOSÉE** | **Risque réel** : `_erreur = ex.Message` affiche le message de **toute** exception, y compris une `SqlException` ou une `InvalidOperationException` d'infrastructure |
| **ERREUR SILENCIEUSE** | **4 méthodes** — les actions en masse de `ComptesAgents` ne passent pas par le wrapper de gestion d'erreur (constat déjà porté au backlog technique, D-03) |

---

## 10. Tableaux

| Critère | Constat |
|---|---|
| Pages avec `<table>` | **55** |
| **`table-responsive`** | **55 / 55 — couverture totale** ✅ |
| **Pagination** | **0 page** 🔴 |
| Tri | Aucun tri de colonne détecté |
| Filtres | Présents ponctuellement (`ComptesAgents` : recherche + statut ; `RechercheGuichet`) |
| Badges de statut | `StatutBadge` sur 17 pages ✅ |
| Alignement des nombres | Non vérifiable sans rendu |

**L'absence totale de pagination est le défaut structurant de cette partie.** Les files agent, le
journal d'audit et la liste des comptes chargent l'intégralité des lignes. Sur un volume de
production — le CDC signale d'ailleurs un risque de « sous-estimation de la volumétrie documentaire »
— cela dégrade le rendu et le temps de réponse.

En regard, **la couverture `table-responsive` à 100 %** est un résultat rare et mérite d'être noté :
aucun tableau ne débordera horizontalement de son conteneur.

---

## 11. Responsive

> **NON VÉRIFIÉ VISUELLEMENT.** Aucune taille d'écran n'a été éprouvée.

### Ce que le code met en place

| Mécanisme | Mesure |
|---|---|
| Grille Bootstrap | `col-md-` **167** · `col-lg-` 25 · `col-sm-` 11 |
| Masquage adaptatif | `d-none` 12 · `d-md-` 4 |
| Tableaux | `table-responsive` **55 / 55** |
| Points de rupture propres | **2** — `max-width: 991.98px`, `max-width: 575.98px` |
| Préférences système | `prefers-reduced-motion` ✅ · `@media print` ✅ |

### Évaluation par classe — **déduite, non observée**

| Classe | Résolutions | Appréciation statique |
|---|---|---|
| **DESKTOP** | 1920×1080 · 1366×768 | Favorable — grille `col-md` conçue pour cette largeur |
| **TABLETTE** | 1024×768 · 768×1024 | Plausible — le point de rupture 991.98px cible cette bascule |
| **MOBILE** | 390×844 | **Incertain** — un seul point de rupture (575.98px), 12 `d-none` seulement, et des tableaux à 5-6 colonnes qui, même en `table-responsive`, imposeront un défilement horizontal permanent |

**Le mobile est le point d'interrogation le plus sérieux de cet audit**, et c'est précisément celui
qu'une analyse statique ne peut pas trancher.

---

## 12. Accessibilité

> Audit **partiel**. Aucun lecteur d'écran, aucune mesure de contraste, aucun parcours clavier réel.
> **Aucune conclusion de conformité WCAG n'est possible sur cette base.**

| Critère | Mesure | Appréciation |
|---|---|---|
| Association label / champ | 137 labels / 109 champs | ✅ favorable |
| Attribut `alt` | **16 `alt` pour 16 `<img>`** | ✅ couverture complète |
| `role=` | 50 | ✅ présent |
| `aria-*` | 38, dont **12 `aria-label`** | 🟡 faible au regard de 233 boutons |
| Styles `:focus` / `:focus-visible` | **23 règles** dans `app.css` | ✅ le focus est traité |
| `prefers-reduced-motion` | présent | ✅ |
| **Lien d'évitement** | **absent** de `MainLayout` | 🔴 navigation clavier pénible |
| **Icônes** | **emoji** (🏠 🔑 📝 ⚡ 🔍 ✅ ⏳ ⭐) | 🔴 voir ci-dessous |

### Le problème des icônes emoji

L'application utilise des **emoji comme iconographie**, y compris dans les boutons et le menu. Deux
conséquences :

1. **Les lecteurs d'écran les annoncent** — « bâtiment de bureaux », « clé », « étoile blanche » —
   ce qui pollue la restitution quand l'emoji est purement décoratif et n'est pas masqué par
   `aria-hidden`.
2. **Leur rendu varie** selon le système et la police, ce qui fragilise la cohérence visuelle.

Avec **12 `aria-label` pour 233 gestionnaires de clic**, une part des boutons combinant emoji et
texte court reste probablement sans nom accessible explicite — mais **seul un test réel le
confirmerait**.

---

## 13. Textes

| Recherche | Résultat |
|---|---|
| Identifiants techniques affichés | 63 occurrences de `.Id`, **dont 30 dans des `href=`** — routage, pas affichage |
| `Enum.ToString()` brut sur un statut | **1 seule occurrence** — le reste passe par `StatutBadge` et le référentiel `LibelleStatutTicket` ✅ |
| Anglais résiduel | Aucun dans les textes visibles ; les classes CSS anglaises ne sont pas affichées |
| Messages développeur | Aucun message technique codé en dur détecté ; le risque vient de `ex.Message` (§ 9) |
| Vouvoiement | Cohérent sur les échantillons lus — « Votre compte a été clôturé », « vos sessions ont été déconnectées » |
| Vocabulaire métier | Aligné sur le CDC — Pilote Manifeste, Back Office, Contrôle Documentaire, BEE, Draft IN, avoir, exonération |

**La qualité rédactionnelle est un point fort.** Les libellés d'espace (« Espace Opérateur CIT »,
« Administration CIT ») sont explicites, et les messages de succès disent ce qui s'est produit plutôt
que « Opération réussie ».

---

## 14. Dates et montants

### Dates

| Format | Occurrences | Verdict |
|---|---|---|
| `dd/MM/yyyy HH:mm` | **43** | ✅ format français dominant |
| `dd/MM/yyyy` | 9 | ✅ |
| `yyyy-MM-dd` | 5 | ✅ légitime — valeurs de `<input type="date">` |
| `dd/MM/yyyy à HH:mm` | 2 | 🟡 variante |
| `dd/MM/yyyy HH:mm:ss` | 1 | 🟡 secondes inutiles |
| `dddd dd MMMM yyyy` | 1 | ✅ affichage long |

**Aucune ambiguïté jour/mois** : le format américain est absent.

**Sur l'UTC** — 41 affichages proviennent directement de propriétés `…Utc`. Ce serait un défaut
ailleurs ; **la Côte d'Ivoire est à UTC+0 toute l'année**, donc l'heure affichée est l'heure locale
correcte. Le nommage reste trompeur pour un lecteur du code, et le comportement deviendrait faux si
l'application servait un autre fuseau.

### Montants

| Critère | Constat |
|---|---|
| Devise | **XOF**, 26 occurrences, uniforme |
| Format numérique | **`ToString("N0")`**, 22 occurrences — séparateur de milliers, zéro décimale |
| Cohérence | Le franc CFA n'ayant pas de subdivision usuelle, `N0` est le bon choix |
| Arrondi | Le moteur arrondit au franc et le test `AuditPreuveArrondiFrancTests` le garantit ✅ |

**Tarification, simulation et facturation partagent le même format.** C'est cohérent.

---

## 15. Permissions et UX

| Profil | Module | Voir | Créer | Modifier | Action métier |
|---|---|---|---|---|---|
| **Client** | Billing IMPORT/EXPORT | ✅ ses demandes | ✅ | — | Demander facture, déposer preuve, annuler |
| Client | Réclamations | ✅ siennes | ✅ | — | Évaluer |
| Client | Avoir / Exonération | ✅ siennes | ✅ | — | Compléter |
| Client | Simulation | ✅ | ✅ | — | Historique, détail |
| Client | Rendez-vous | ✅ | ✅ | — | Annuler, reporter |
| Client | Profil | ✅ | — | ✅ | **Clôturer son compte** |
| **Transporteur** | Enrôlement | ✅ siens | ✅ | — | Renouveler badge |
| **Admin société** | Collaborateurs | ✅ | ✅ | ✅ | Activer / désactiver, droits ENT-03 |
| Admin société | Dashboard société | ✅ | — | — | — |
| **Agent** | Files de son périmètre | ✅ | — | ✅ | Prendre en charge, transférer, instruire, clôturer |
| Agent | Fiches | ✅ selon habilitation | — | ✅ | Notes internes, documents, décision |
| Agent | Guichet | ✅ | ✅ | ✅ | Rechercher, honorer un rendez-vous |
| **Superviseur** | Supervision | ✅ | — | ✅ | Affectation, actions de masse, disponibilité |
| **Admin CIT** | Comptes agents | ✅ | ✅ | ✅ | Habilitations, **actions en masse** |
| Admin CIT | Référentiels | ✅ | ✅ | ✅ | SLA, transitions, escalade, pièces, catégories |
| Admin CIT | Tarification | ✅ | ✅ | ✅ | Import CSV, versions de grille |
| Admin CIT | Audit | ✅ | — | — | Consultation seule |

### Traduction UX du refus

**Le refus est bien traduit** : `PermissionDenied` sur 51 pages, avec un message explicite plutôt
qu'une redirection silencieuse — les tests `AuthorizationUxTests` vérifient d'ailleurs que le refus
s'affiche **sans redirection** pour un utilisateur authentifié, et redirige avec `returnUrl` pour un
anonyme.

**Le défaut est en amont** : le menu ne filtre pas, donc l'utilisateur découvre le refus après avoir
cliqué plutôt que de ne pas voir l'entrée.

---

## 16. Parcours Client

> **NON EXÉCUTÉ.** Reconstruit par lecture du code.

```
Connexion → [2FA] → Dashboard Client
  → Soumettre demande (IMPORT 1-5 BL / EXPORT bookings)
  → Mes demandes → Fiche ticket
  → Documents (dépôt) · Messages (fil) · Chronologie
  → Demander facture → Déposer preuve de paiement
  → Suivi jusqu'à clôture → Ticket archivé, consultable
```

**Le parcours est complet et sans rupture apparente.** Chaque étape a son écran, la fiche centralise
documents, messages et chronologie, et le ticket archivé reste consultable.

### Frictions identifiées

| # | Friction | Gravité |
|---|---|---|
| 1 | Le menu propose des entrées d'administration que le client ne peut pas ouvrir | UX-P1 |
| 2 | Aucune confirmation avant l'annulation d'un ticket | UX-P1 |
| 3 | `MesDemandesAvoirExo` et `MesRendezVous` : tableau vide sans explication | UX-P2 |
| 4 | Double-clic possible sur « Soumettre » — deux demandes | UX-P1 |
| 5 | Aucun fil d'Ariane pour situer une fiche dans son dossier | UX-P3 |

---

## 17. Parcours Agent

> **NON EXÉCUTÉ.**

```
Connexion → Dashboard Agent (files habilitées, tickets en cours, disponibilité)
  → File (Manifeste / Facturation / Contrôle documentaire / IES …)
  → Prendre en charge → Fiche ticket
  → Instruire : valider · rejeter (motif) · demander complément
  → Notes internes · messages client · documents
  → Décision → Transfert ou clôture
```

**Le tableau de bord agent est bien conçu** : il n'affiche que les files habilitées, les tickets en
cours de l'agent, et un bandeau de disponibilité — la garde `GardeTableauDeBordAgentTests` garantit
qu'un compte désactivé n'obtient rien.

### Frictions

| # | Friction | Gravité |
|---|---|---|
| 1 | Files sans pagination — la totalité des tickets est chargée | UX-P1 |
| 2 | Aucune confirmation avant clôture ou rejet | UX-P1 |
| 3 | Ni tri ni filtre sur les files | UX-P2 |
| 4 | Double-clic sur « Prendre en charge » — protégé côté serveur par le jeton de concurrence optimiste ✅, mais sans retour visuel immédiat | UX-P2 |

---

## 18. Parcours Administrateur

> **NON EXÉCUTÉ.**

```
Connexion → Comptes agents → Créer un compte
  → Modale d'habilitations → attribuer / retirer / groupes / TOUT accorder
  → Référentiels (SLA, transitions, escalade, pièces, catégories, motifs)
  → Tarification (import CSV de grille)
  → Journal d'audit
```

### Frictions

| # | Friction | Gravité |
|---|---|---|
| 1 | **« ⭐ Tout accorder (SuperAdmin) » sans aucune confirmation** | **UX-P0** |
| 2 | « Tout retirer » sans confirmation, non atomique | **UX-P0** |
| 3 | Aucun état vide sur 9 écrans de référentiel | UX-P2 |
| 4 | Journal d'audit sans pagination — croissance illimitée | UX-P1 |

---

## 19. Export — Draft IN

> **NON EXÉCUTÉ.**

Le parcours ajouté par la remédiation RG-EXP-03 a été relu :

```
File Facturation Export
  → colonne « Draft IN » :  [dépôt de fichier]  ou  badge « ✅ Émis »
  → colonne « Déposer facture Export » :
        si Draft IN émis   → champ de dépôt
        sinon              → mention « Draft IN requis »
```

**L'ergonomie de cette addition est correcte** et respecte le principe de l'application : le serveur
décide, l'écran reflète. L'utilisateur comprend l'ordre imposé sans avoir à essayer — le libellé
« Draft IN requis » remplace le champ plutôt que de le désactiver silencieusement.

| Point | Appréciation |
|---|---|
| Ordre visible avant l'action | ✅ |
| État « Émis » horodaté en `title` | ✅ *(infobulle — invisible au tactile)* 🟡 |
| Message de succès explicite | ✅ « Le Draft IN … a été émis. La facture peut maintenant être éditée. » |
| Confirmation avant dépôt | ❌ absente, comme partout |

---

## 20. Simulation

> **NON EXÉCUTÉ.**

`SimulationProformaPage` (430 lignes) est **publique** — décision assumée (SIM-01, inscrite dans la
garde des pages publiques).

| Critère | Appréciation statique |
|---|---|
| Saisie dossier + conteneurs | Formulaire structuré, grille Bootstrap |
| Détail du calcul | Le domaine produit les composantes (acconage, relevage, branchement, majorations, stationnement, quotes-parts) ; la page les restitue |
| Compréhension métier | **Favorable** — le résultat est ventilé par composante, pas un total opaque |
| Montants | `XOF` + `N0` cohérents |
| Reproductibilité | La version de grille est référencée et son libellé conservé ✅ |
| Pagination de l'historique | Absente |

**C'est l'un des écrans les mieux traités** du point de vue de la compréhension : le CDC exige un
résultat « indicatif », et l'affichage ventilé le rend vérifiable par un utilisateur métier.

---

## 21. Avoir / Exonération

> **NON EXÉCUTÉ.**

```
NouvelleDemandeAvoirExo → MesDemandesAvoirExo
  → FileAvoirExoneration (agent) → FicheAvoirExoneration (409 lignes)
  → instruction · demande de complément · décision (accord / rejet motivé)
```

| Friction | Gravité |
|---|---|
| `MesDemandesAvoirExo` : tableau sans état vide — surface **client** | UX-P2 |
| `FileAvoirExoneration` : idem, sans état vide | UX-P2 |
| Décision (accord/rejet) sans confirmation | UX-P1 |

---

## 22. ComptesAgents

**830 lignes — le plus gros composant de l'application**, devant `FicheTicket` (723).

*L'audit de sécurité précédent a établi que cette refonte concurrente ne dégrade rien : même route,
même `[Authorize]`, mêmes services, identité résolue au serveur. Ce qui suit ne porte que sur l'UI.*

| Critère | Appréciation |
|---|---|
| **Lisibilité** | ✅ Bonne — cartes de statistiques, recherche, filtre par statut, groupement des permissions par domaine |
| **Densité** | 🟡 Élevée — statistiques + tableau + modale d'habilitations avec ~60 permissions |
| **Hiérarchie** | ✅ Claire — l'entrée dans la modale se fait par compte, puis par groupe |
| **Actions en masse** | 🔴 « Tout accorder (SuperAdmin) », « Tout retirer », bascule de groupe |
| **Confirmation** | 🔴 **Aucune** sur les trois |
| **Feedback** | 🟡 Message de succès présent, mais **hors du wrapper d'erreur** — un refus serveur ne produit pas le panneau attendu |
| **Risque d'erreur utilisateur** | 🔴 **Élevé** — un clic accorde ~60 permissions à un compte ; un autre les retire toutes |
| **Cohérence** | 🟡 Seule page à définir ses propres classes dans un bloc `<style>` |
| **Mobile / tablette** | ⚠️ **NON VÉRIFIÉ** — modale dense + tableau à colonnes multiples : risque à confirmer |

### Le point central

Le bouton **« ⭐ Tout accorder (SuperAdmin) »** est, du point de vue de l'expérience, **l'action la
plus dangereuse de l'application** : un clic unique, sans confirmation, qui confère l'intégralité des
droits — y compris l'administration des comptes, la tarification et l'audit.

Elle est **autorisée** (le serveur exige `administration.comptes`), **tracée** (chaque attribution est
auditée individuellement), et donc parfaitement légitime. Mais elle est à un clic d'une erreur de
manipulation, et `ModaleConfirmation` existe déjà dans le projet pour exactement ce cas.

---

## 23. Performance perçue

> Aucune mesure. Ce qui suit est une **analyse de risque**, pas un benchmark.

| Signal | Constat | Classement |
|---|---|---|
| **Pagination absente** | 0 page sur 55 tableaux | **RISQUE DE PERFORMANCE** — journal d'audit et files agent croissent sans borne |
| Chargements séquentiels | `FileIes` charge la file, **puis boucle** une requête de suivi par ticket (N+1 applicatif) | **RISQUE** |
| Lecture par requête | `ValidationInterval = TimeSpan.Zero` impose une lecture du compte à chaque requête | **RISQUE** *(déjà au backlog technique, D-04)* |
| `IServiceScopeFactory` | 25 pages — un scope par opération | Choix délibéré de sûreté concurrentielle, coût assumé |
| Re-render | `SurChangementContexte` déclenche `StateHasChanged` sur le menu | Faible |

**PERFORMANCE MESURÉE : aucune.** L'application n'ayant pas été lancée, aucun chiffre n'est produit.

---

## 24. Qualité frontend

| Critère | Constat |
|---|---|
| Volume total | 20 544 lignes de `.razor` sur 100 composants |
| Composants > 400 lignes | **7** — ComptesAgents 830, FicheTicket 723, TableauDeBord 531, Collaborateurs 460, SimulationProforma 430, SoumettreDemande 411, FicheAvoirExoneration 409 |
| JavaScript en ligne | **Aucun** ✅ — 4 fichiers externes, chargés dans `App.razor` |
| `IJSRuntime` | 4 pages seulement ✅ |
| CSS en ligne | 215 `style=` 🟡, 1 bloc `<style>` 🟡 |
| `IServiceScopeFactory` | 25 pages — **délibéré**, issu des correctifs de concurrence |
| **`PlateformeDbContext` dans une page** | **1 — `Tickets/IndicateursTemps.razor`** 🔴 |
| Logique métier en UI | Ponctuelle — calculs d'affichage, pas de règle métier |

### Le seul vrai manquement d'architecture

**`IndicateursTemps.razor` référence directement `PlateformeDbContext`.** C'est la seule page de
l'application à franchir la couche applicative, et cela contredit le principe suivi partout
ailleurs — les 25 autres pages résolvent des cas d'usage via le conteneur.

### Pages méritant une extraction de composants

`ComptesAgents` (modale d'habilitations extractible), `FicheTicket` (onglets documents / messages /
chronologie), `TableauDeBord` (cartes KPI). **Aucune extraction n'a été faite.**

---

## 25. Défauts

### UX-P0 — bloque un parcours ou provoque une action dangereuse

| ID | Défaut |
|---|---|
| **UI-01** | `ComptesAgents` — « Tout accorder (SuperAdmin) » et « Tout retirer » : action irréversible d'un clic, **sans confirmation**, **non atomique**, hors du wrapper de gestion d'erreur |

### UX-P1 — dégradation importante d'un parcours principal

| ID | Défaut |
|---|---|
| **UI-02** | Aucune protection contre la double soumission : 233 `@onclick`, 2 pages avec verrou |
| **UI-03** | `ModaleConfirmation` existe et n'est **jamais utilisée** — aucune action destructive n'est confirmée |
| **UI-04** | Aucune pagination sur 55 tableaux |
| **UI-05** | Menu non filtré par permission — impasses pour tous les profils non-administrateurs |
| **UI-06** | `_erreur = ex.Message` peut exposer un message technique d'infrastructure |

### UX-P2 — problème réel mais contournable

| ID | Défaut |
|---|---|
| **UI-07** | 26 tableaux sans état vide, dont 2 surfaces client |
| **UI-08** | Icônes emoji sans `aria-hidden` ; 12 `aria-label` pour 233 boutons |
| **UI-09** | Absence de lien d'évitement dans `MainLayout` |
| **UI-10** | 138 `NavLink` sur 146 sans `NavLinkMatch` explicite — plusieurs entrées actives simultanément |
| **UI-11** | Ni tri ni filtre sur les files agent |
| **UI-12** | `IndicateursTemps.razor` accède directement au `DbContext` |
| **UI-13** | `ComptesAgents` — bloc `<style>` propre, rupture du design system |
| **UI-14** | Mobile non vérifié : tableaux à 5-6 colonnes, un seul point de rupture |

### UX-P3 — polish et cohérence

| ID | Défaut |
|---|---|
| **UI-15** | `ToastNotification` existe et n'est jamais utilisé |
| **UI-16** | 215 `style=` en ligne, ~40 couleurs hexadécimales doublonnant des tokens |
| **UI-17** | 80 `!important` dans `app.css` |
| **UI-18** | Formats de date secondaires : `HH:mm:ss` (1), `à HH:mm` (2) |
| **UI-19** | Aucun fil d'Ariane |
| **UI-20** | Draft IN : date d'émission en `title` seulement — invisible au tactile |

**Total : 1 UX-P0 · 5 UX-P1 · 8 UX-P2 · 6 UX-P3 = 20 défauts.**

---

## 26. Quick wins

Faible effort, fort impact, faible risque — **non implémentés**.

| # | Action | Effort | Impact |
|---|---|---|---|
| 1 | Brancher `ModaleConfirmation` sur les trois actions en masse de `ComptesAgents` | **XS** | **Ferme l'unique UX-P0** |
| 2 | Ajouter `aria-hidden="true"` sur les emoji décoratifs | XS | Restitution lecteur d'écran nettement améliorée |
| 3 | Ajouter un lien d'évitement dans `MainLayout` | XS | Navigation clavier |
| 4 | Ajouter `EtatVide` aux 2 surfaces client concernées | XS | Compréhension client |
| 5 | Verrou `_enCours` sur les 10 boutons de soumission principaux | S | Couvre l'essentiel de UI-02 |
| 6 | Filtrer les groupes du menu selon `EspacesDisponibles` — déjà calculé côté serveur | S | Supprime les impasses |
| 7 | Uniformiser `HH:mm:ss` → `HH:mm` | XS | Cohérence |

Les six premiers relèvent de l'ajout, pas de la refonte. Le premier, à lui seul, supprime le seul
défaut classé P0.

---

## 27. Score UI/UX

> ### SCORE PROVISOIRE — ANALYSE STATIQUE
>
> Aucune inspection navigateur. Les postes *Responsive* et *Accessibilité* sont plafonnés faute
> d'observation réelle.

| Poste | Note | Justification |
|---|---|---|
| **Design visuel** | **11 / 15** | 135 tokens, palette nommée, préfixe cohérent. Pénalisé par 215 styles en ligne et 80 `!important` |
| **Cohérence** | **7 / 10** | En-têtes, cartes, badges homogènes. Pénalisé par 2 composants morts et la rupture `ComptesAgents` |
| **Navigation** | **6 / 10** | Accordéons structurés, `PersonaSwitcher`, déconnexion claire. Pénalisé par l'absence de filtrage et 8/146 `NavLinkMatch` |
| **Formulaires** | **7 / 10** | 137 labels / 109 champs, composant mot de passe partagé. Pénalisé par l'absence de verrou d'envoi |
| **Feedback** | **6 / 10** | 68 pages avec retour explicite. Pénalisé par l'absence totale de confirmation préalable |
| **Responsive** | **6 / 10** | `table-responsive` 55/55, grille Bootstrap. **Plafonné — non vérifié**, mobile incertain |
| **Accessibilité** | **5 / 10** | Labels, `alt` complets, 23 règles de focus. Pénalisé par emoji non masqués, aria rare, pas de lien d'évitement. **Plafonné — non vérifié** |
| **Gestion des erreurs** | **7 / 10** | `PermissionDenied` sur 51 pages, distinction des exceptions. Pénalisé par `ex.Message` et les 4 méthodes hors wrapper |
| **Parcours utilisateur** | **7 / 10** | Parcours complets, sans rupture. Pénalisé par confirmations et pagination absentes |
| **Qualité frontend** | **3 / 5** | Aucun JS en ligne, composants partagés réels. Pénalisé par le `DbContext` en page et 7 composants > 400 lignes |
| **TOTAL** | **65 / 100** | |

---

## 28. Recommandations

**Avant pilote** — ferme le P0 et l'essentiel des P1 :

1. Brancher `ModaleConfirmation` sur les actions en masse *(quick win n° 1)*.
2. Rendre ces actions atomiques et les router par le wrapper d'erreur *(déjà D-02 / D-03 au backlog technique)*.
3. Ajouter un verrou d'action en cours sur les boutons de soumission principaux.
4. Filtrer le menu selon les espaces disponibles.

**Avant production** :

5. Paginer le journal d'audit et les files agent — les deux surfaces à croissance non bornée.
6. **Conduire une inspection navigateur réelle**, en particulier sur mobile. Cet audit ne peut pas s'y substituer.
7. Encadrer `ex.Message` pour ne pas exposer un message d'infrastructure.

**Ensuite** :

8. Compléter les états vides, l'accessibilité des icônes, le lien d'évitement, les `NavLinkMatch`.
9. Extraire les composants des trois plus gros écrans.
10. Résorber `IndicateursTemps.razor`.

---

## 29. GO / NO-GO UI

> ### DESIGN : 🟢 **VALIDÉ AVEC RÉSERVES**
> Un design system réel, nommé et appliqué. Réserves : 215 styles en ligne, 80 `!important`, une page
> qui introduit son propre vocabulaire visuel.

> ### UX : 🟠 **À REVOIR**
> Les parcours sont complets et cohérents, mais **une action destructive majeure s'exécute d'un clic
> sans confirmation**, aucune action n'est protégée contre la double soumission, et aucun tableau
> n'est paginé. Le composant qui résoudrait le premier point existe déjà et n'est pas branché.

> ### RESPONSIVE : ⚠️ **NON VÉRIFIÉ**
> Les mécanismes sont en place — `table-responsive` à 100 %, grille Bootstrap — mais **aucune taille
> d'écran n'a été éprouvée**. Un seul point de rupture mobile et des tableaux à 5-6 colonnes rendent
> le verdict impossible à rendre sans observation.

> ### ACCESSIBILITÉ : 🟠 **À REVOIR**
> Les fondamentaux sont là (labels, `alt`, focus, `prefers-reduced-motion`). Manquent le lien
> d'évitement, le masquage des icônes décoratives et les noms accessibles des boutons. **Aucune
> conclusion WCAG n'est possible sans test réel.**

---

> ### UI PRÊTE POUR PILOTE : 🟠 **OUI SOUS CONDITIONS**
>
> Conditions : fermer UI-01 *(confirmation sur les actions en masse)*, et conduire une inspection
> navigateur au moins sur les cinq surfaces principales. Un pilote encadré, avec des utilisateurs
> accompagnés, absorbe le reste.

> ### UI PRÊTE POUR PRODUCTION : 🔴 **NON**
>
> Trois raisons, dans l'ordre :
> 1. **UI-01 non fermé** — une action irréversible d'un clic sans confirmation ;
> 2. **Aucune vérification visuelle n'a jamais été conduite** — le responsive mobile est un
>    inconnu total sur une application destinée à des transporteurs et des chauffeurs ;
> 3. **Aucune pagination** sur des surfaces à croissance non bornée.
>
> Aucune de ces trois n'est coûteuse à lever. La deuxième est la plus importante, et c'est la seule
> que ce rapport ne pouvait pas traiter.

---

## 30. Conclusion

L'interface de cette plateforme est **meilleure que ce à quoi on pourrait s'attendre** d'un projet
dont les audits précédents ne portaient que sur le métier et la sécurité. Un design system existe,
avec 135 tokens et un préfixe applicatif discipliné. Dix composants partagés ont été écrits. Les
états de chargement couvrent 66 pages sur 81, les refus d'accès 51, et **les 55 tableaux sont tous
dans un conteneur adaptatif** — une couverture totale qu'on rencontre rarement.

Ce qui la retient est de même nature que ce que les audits précédents ont trouvé côté serveur, et
c'est le constat que je retiens de ce travail.

**`ModaleConfirmation.razor` et `ToastNotification.razor` ont été écrits, et n'ont jamais été
branchés.** Zéro référence, l'un comme l'autre. Ce ne sont pas des composants superflus : le premier
est exactement ce qui manque aux douze actions de désactivation, aux cinq suppressions et au bouton
« Tout accorder » ; le second exactement ce qui manque au retour d'action. Ils ont été conçus pour
les besoins qu'on constate aujourd'hui non couverts.

C'est la cinquième fois que ce projet produit cette figure — après `MarquerEtatEngageant` sans
appelant, la garde de fiche sans branche agent, `AutorisationChampService` sans consommateur, et la
chaîne de rétention sans déclencheur. **La différence, ici, est qu'aucun test ne pouvait la
signaler** : il n'existe pas de garde d'atteignabilité pour les composants d'interface.

Enfin, ce rapport doit être lu pour ce qu'il est. Il n'a **rien vu**. Il a mesuré, compté, croisé et
déduit — mais aucune page n'a été rendue, aucun contraste mesuré, aucun écran de téléphone éprouvé.
Sur une application destinée à des chauffeurs et des transporteurs qui s'enrôlent à distance, le
responsive mobile n'est pas un détail de finition, et c'est précisément ce qu'une analyse statique ne
peut pas trancher.

**Le score de 65/100 est provisoire, et il le restera jusqu'à ce que quelqu'un ouvre l'application.**
