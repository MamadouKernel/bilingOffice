# AM-03 — confirmation des actions destructives / irréversibles

**Date** : 12 septembre 2026. Décision métier rendue : *les transitions/actions destructives ou
irréversibles doivent demander confirmation.* Aucun commit.

## 1. Inventaire complet des actions destructives (recalculé depuis le code)

| Action | Fichier | Réversible ? | Confirmation | Statut |
|---|---|---|---|---|
| Clôture / annulation ticket | `Tickets/DetailTicket.razor` | Non (archivage) | ✅ (existante) | Couvert |
| Désactivation compte agent / retrait de toutes les habilitations | `Administration/ComptesAgents.razor` | Oui / — | ✅ (existante) | Couvert |
| Rejet de dossier (file) | `BillingImport/FilePiloteManifeste`, `BillingExport/FileBackOfficeExport` | Non | ✅ (modale dédiée existante) | Couvert |
| Clôture de mon compte | `Comptes/MonProfil.razor` | Non | ✅ (existante) | Couvert |
| Annulation de rendez-vous | `Guichet/MesRendezVous.razor` | Non | ✅ (existante) | Couvert |
| **Suppression d'une transition (matrice files)** | `Administration/MatriceTransitions.razor` | **Non (config perdue)** | ✅ **ajoutée** | **Couvert (ce tour)** |
| **Retrait d'un paramètre SLA** | `Administration/ParametresSla.razor` | **Non (config perdue)** | ✅ **ajoutée** | **Couvert (ce tour)** |
| **Révocation d'invitation** | `Societe/Collaborateurs.razor` | **Non (lien invalidé)** | ✅ **ajoutée** | **Couvert (ce tour)** |
| **Désactivation d'un collaborateur** | `Societe/Collaborateurs.razor` | Oui (réactivable) mais **révoque l'accès immédiatement** | ✅ **ajoutée** | **Couvert (ce tour)** |

## 2. Actions volontairement NON confirmées (réversibles / non destructives)

| Action | Raison |
|---|---|
| Retrait du rôle administrateur (`Collaborateurs`) | Réversible (re-attribuable) ; le collaborateur **garde son accès**, seul le privilège admin change. Impact moindre qu'une désactivation. |
| Retrait d'une compétence opérateur (`CompetencesOperateur`) | Réversible : la compétence se ré-attribue immédiatement. |
| Suppression d'une ligne de conteneur (`SimulationProformaPage`) | Édition d'un **formulaire non soumis** — aucune donnée persistée. |
| Suppression d'un fichier joint avant dépôt (`NouvelleDemandeAvoirExo`) | Édition d'un formulaire non soumis — le fichier se re-joint. |
| « Annuler ma demande » (`FicheTicket`) | **Placeholder** : le handler ne fait qu'afficher un message et recharger — il n'exécute AUCUNE commande. C'est un **manque fonctionnel** (à implémenter), pas un défaut de confirmation. L'annulation réelle d'un ticket est confirmée dans `DetailTicket`. |

## 3. Implémentation

Chaque action ajoutée suit le patron existant, **sans dupliquer la logique** : un enregistrement
partagé [`ConfirmationEnAttente`](../src/CIT.Plateforme.Web/Components/Shared/ConfirmationEnAttente.cs)
(titre, message, libellé, classe, action différée) + le composant `ModaleConfirmation` déjà en place.

- Le bouton n'appelle plus la commande destructive : il appelle `DemanderConfirmation…`, qui **prépare**
  l'action sans l'exécuter.
- L'action réelle n'est lancée que par `OnConfirmer`.
- `OnAnnuler` / Échap / clic hors modale referment sans effet.

**Accessibilité conservée** (couverte par la modale et ses tests) : `role="dialog"`, `aria-modal`,
`aria-labelledby`/`describedby`, focus initial sur *Annuler*, fermeture Échap, retour du focus au
déclencheur (WCAG 2.4.3). La modale n'est **jamais** une frontière de sécurité : chaque commande
revérifie l'autorisation côté serveur (inchangé).

## 4. Vérification

- **Garde de câblage** : `Am03ConfirmationCablageTests` — pour chaque action, le bouton ne peut pas
  appeler directement la commande destructive et la page monte une `<ModaleConfirmation>`.
  **Mutation vérifiée** : revenir à l'appel direct fait tomber la garde.
- La modale elle-même (dialogue, focus, Échap, retour focus) reste couverte par
  `ModaleConfirmationVisibiliteTests` / `ModaleConfirmationFocusTests`.
- Preuve navigateur : voir le rapport de session.

## 5. Verdict AM-03

**OK pour le périmètre de la décision** (actions destructives/irréversibles couvertes). Les actions
réversibles restent directes, **par choix documenté** ci-dessus — pas par oubli. Le seul cas ambigu
(`FicheTicket` « Annuler ma demande ») est un **placeholder fonctionnel** sans commande, signalé comme
tel : le confirmer serait confirmer un no-op.
