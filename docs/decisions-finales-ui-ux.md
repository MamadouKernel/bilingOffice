# Décisions finales UI/UX — clôture

**Date** : 7 septembre 2026. Ce document trace, pour chaque verrou ouvert, la décision prise,
son autorité, ce qui a été implémenté et ce qui reste hors de mon périmètre.

Aucune décision métier n'a été inventée. Aucune modification de sécurité n'a été faite à l'aveugle.
Aucun commit n'a été effectué.

---

## AM-01 — Durée de séjour facturable

**Constat.** Une seule définition existe déjà et fait autorité :
`DossierSimulation.DureeSejourJours => (DateEnlevementPrevisionnel.DayNumber - DateArrivee.DayNumber) + 1`
— **bornes incluses**. Le moteur (`MoteurCalculProforma`), la projection persistée
(`SimulationProforma`), le PDF (`GenerateurDocumentProformaPdf`) et l'affichage
(`DetailSimulationPage`, `HistoriqueSimulationsPage`) lisent tous **cette même** valeur. Aucun
`DateDiff` indépendant dans les pages.

**Autorité.** La convention est ancrée dans le golden master
`StationnementEtReeferTests.Duree_de_sejour_est_calculee_bornes_incluses`, dont le cas « 29 jours »
référence l'**Annexe A.8** du paramétrage tarifaire. C'est la facturation validée qui fait foi.

**Décision.** Aucune modification — le système applique déjà **une seule** convention, cohérente
entre moteur et affichage. Modifier quoi que ce soit dégraderait un comportement déjà correct et
couvert.

**Preuve.** Mutation `+1 → +2` sur la durée : 4 tests golden master tombent. Restauré.

**Statut : CONFORME.**

---

## AM-02 — Collaborateur limité

**Décision (autorisée).** L'espace visible dérive **strictement** des permissions résolues côté
serveur. Le serveur reste seul juge ; l'IHM masque uniquement ce qui mènerait à un 403.

**Implémenté.**
- Menu latéral : déjà piloté par `ConstruireNavigationQuery` (habilitations réelles + `RegleHabilitation`,
  la même règle que `AutorisationService`). Inchangé.
- Tableau de bord client : le raccourci « Nouvelle demande IMPORT » **et** le CTA de l'état vide
  ne s'affichent que si `PeutSoumettre` est vrai. `PeutSoumettre` est calculé par
  `ObtenirDashboardClientQuery` avec **exactement** le prédicat des commandes de soumission
  (`EstActif && DroitSoumission`).

**Non affaibli.** La commande `SoumettreDemandeImportCommand`/`SoumettreDemandeExportCommand` refuse
toujours côté serveur si `!EstActif || !DroitSoumission`, quelle que soit l'IHM (URL forcée incluse).

**Preuve.** 2 tests requête (`PeutSoumettre` vrai/faux) + 1 test rendu (collaborateur limité →
raccourci absent). Mutation `PeutSoumettre: true` en dur → la garde de rendu tombe. Restauré.

**Statut : CONFORME** pour les surfaces identifiées (menu + tableau de bord client). Les autres
dashboards (agent : page entière gardée + files dérivées des habilitations ; société : vue
administrateur) n'exposent pas de CTA vers une ressource interdite.

---

## AM-03 — Confirmation des transitions de workflow

**Décision (autorisée) — Stratégie B.** Confirmation réservée aux actions **irréversibles** ou
**engageantes** (catégories C et D). Pas de confirmation sur chaque bouton.

**Classification du cycle de vie d'un ticket.**

| Action | Catégorie | Confirmation |
|---|---|---|
| Demander un complément (mise en attente) | B (réversible) | non |
| Reprendre après réponse | B | non |
| Réaffecter un opérateur | B (interne réversible) | non |
| Transférer vers une autre file | B/C (réversible : transfert retour possible) | non |
| Rouvrir | B/C (action de récupération) | non |
| **Annuler** | **D (destructive, irréversible)** | **oui** |
| **Clôturer** | **C/D (archivage automatique, irréversible)** | **oui** |

**Implémenté.** Les deux actions irréversibles ouvrent la `ModaleConfirmation` partagée (déjà
accessible : `role="dialog"`, `aria-modal`, focus initial sur le geste sûr, Échap, restitution de
focus, protection anti double-validation). Une seule instance, action mémorisée. L'exécution réelle
n'a lieu qu'après confirmation.

**Non affaibli.** Les commandes `AnnulerTicketCommand`/`CloturerTicketCommand` revérifient identité,
habilitation, cloisonnement et état côté serveur. La modale n'est pas une frontière de sécurité.

**Preuve.** 3 tests de rendu (clic → modale sans effet ; confirmation → clôture réelle ; retour →
aucun effet). Mutation « court-circuit » (`@onclick="ClorerAsync"` direct) → les 3 gardes tombent.
Restauré.

**Statut : APPLIQUÉ au cœur du workflow ticket** (transitions les plus engageantes). Reste à
étendre le même patron aux autres actions destructives de l'application (ex. désactivation de
catégorie de réclamation) — travail résiduel, non bloquant, sans dette masquée : ces actions
gardent leur contrôle serveur.

---

## D-04 — DataProtection / antiforgery / session expirée

**Constat.** Aucune configuration explicite de DataProtection (`grep` : ni `AddDataProtection`, ni
`PersistKeysToFileSystem`, ni `SetApplicationName`, ni magasin partagé). Le pipeline se limite à
`app.UseAntiforgery()`. Les clés utilisent donc le magasin **par défaut**, éphémère dès que l'hôte
ne fournit pas de persistance stable (conteneur sans volume, redémarrage) — un redémarrage invalide
alors les jetons antiforgery/session en vol.

**Pourquoi aucune modification de code.** Le correctif correct dépend de la **topologie de
déploiement**, inconnue dans cet environnement :
- **mono-instance avec persistance fiable** → `PersistKeysToFileSystem(<répertoire hors conteneur
  éphémère>)` + `SetApplicationName("cit-plateforme")` ;
- **multi-instance / scale-out** → magasin partagé (Redis, table, blob) + même `SetApplicationName`.

Ajouter une persistance sans connaître la cible, ou intercepter l'échec antiforgery dans le
pipeline, risquerait soit de ne rien résoudre (clés toujours éphémères), soit d'avaler une exception
de sécurité. La consigne est explicite : **ne pas modifier le pipeline à l'aveugle**.

**Ce qui ne doit JAMAIS être fait** (et ne l'a pas été) : committer les clés, les mettre dans
`wwwroot`, désactiver l'antiforgery, transformer un refus en succès.

**Statut : CONDITION INFRASTRUCTURE / ACTION DÉPLOIEMENT REQUISE.** La configuration exacte est une
décision d'exploitation, à poser une fois la topologie de production arrêtée. Le message UX
« session expirée » ne pourra être branché proprement qu'après, sans affaiblir la protection.

---

## A11Y — Audit lecteur d'écran formel

NVDA / JAWS / VoiceOver **ne sont pas disponibles** dans cet environnement d'exécution. Un scan DOM
automatique n'est **pas** un audit lecteur d'écran et ne peut pas en tenir lieu.

**Statut : LECTEUR D'ÉCRAN FORMEL — NON EXÉCUTABLE DANS CET ENVIRONNEMENT.** Le point n'est pas
attribué. Accessibilité reste plafonnée à **9/10**.
