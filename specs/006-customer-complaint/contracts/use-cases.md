# Contrats Applicatifs (Use Cases) — Feature 006

> **Document** : `specs/006-customer-complaint/contracts/use-cases.md`  
> **Date** : 30 Août 2026

Afin de respecter la directive de réutilisation (Principe IV et V), la majorité des actions du cycle de vie de la réclamation exploitent **directement** les Commandes et Queries du module Ticketing F001 (prise en charge, demande de complément, réponse client, transfert, ajout de documents, messages internes).

Seuls les cas d'usage strictement spécifiques à Customer Complaint font l'objet de nouveaux contrats.

## C7-01 : SoumettreReclamationCommand

- **Acteur** : Client (Standard ou Admin Société)
- **Entrée** : `CategorieId` (Guid), `Objet` (string), `Description` (string), `PieceJointes` (fichiers), `ReferenceExterne` (string optionnelle).
- **Sortie** : `TicketId` (Guid), `NumeroTicket` (string).
- **Préconditions** : La catégorie existe et est active.
- **Autorisation** : `Complaint.Soumettre`
- **Cloisonnement** : Le ticket est automatiquement affecté à la `SocieteId` issue de `IContexteUtilisateurWeb`. L'identifiant utilisateur est capturé côté serveur.
- **Effets** : Création d'un `Ticket` (Module = "CustomerComplaint"). Affectation à la file cible définie par la catégorie. Déclenchement de l'audit et de la notification de confirmation (COM-01).
- **Exigence REC** : REC-01, REC-05.

## C7-02 : CloturerReclamationCommand

- **Acteur** : Agent Support Opérationnel
- **Entrée** : `TicketId` (Guid), `ReponseFormalisee` (string), `MotifCloture` (MotifClotureType).
- **Sortie** : Void
- **Préconditions** : Ticket en traitement. Non clôturé. `ReponseFormalisee` non vide.
- **Autorisation** : `Complaint.Instruire`
- **Cloisonnement** : L'agent doit appartenir à la file courante du ticket.
- **Effets** : Passage à l'état Clôturé. Sauvegarde de la réponse. Déclenchement d'une notification client contenant la réponse officielle.
- **Exigence REC** : REC-04 (CLAR-REC-01).

## C7-03 : EvaluerReclamationCommand

- **Acteur** : Client (auteur ou admin de la société)
- **Entrée** : `TicketId` (Guid), `Note` (int 1-5), `Commentaire` (string optionnel).
- **Sortie** : Void
- **Préconditions** : Ticket clôturé. Évaluation non existante.
- **Autorisation** : `Complaint.Repondre`
- **Cloisonnement** : Le ticket doit appartenir à la société de l'utilisateur (vérifié par `SocieteId`).
- **Effets** : Ajout de l'objet `EvaluationSatisfaction` sur le ticket.
- **Exigence REC** : REC-06.

## C7-04 : ListerCategoriesReclamationQuery

- **Acteur** : Tous (Client, Agent, Admin)
- **Entrée** : `InclureInactives` (bool - false par défaut)
- **Sortie** : Liste des `CategorieDto` (Id, Code, Libelle).
- **Préconditions** : Aucune
- **Autorisation** : Authentifié.
- **Exigence REC** : REC-01, REC-02.

## C7-05 : ModifierCategoriesReclamationCommand

- **Acteur** : Admin Fonctionnel
- **Entrée** : Liste complète ou unitaire des catégories (Code, Libelle, FileCible, Actif).
- **Sortie** : Void
- **Autorisation** : `Complaint.Parametrer`
- **Effets** : Mise à jour du référentiel en base.
- **Exigence REC** : REC-02.

---

## Contrats F001 Réutilisés Directement

- `ObtenirDetailTicketQuery` : Modifié au niveau Application pour inclure la catégorie et la réponse formalisée si le module est "CustomerComplaint".
- `ListerTicketsClientQuery` / `ListerTicketsFileQuery` : Filtre natif sur `Module` disponible.
- `PrendreEnChargeTicketCommand` : Fonctionne nativement.
- `MettreEnAttenteClientCommand` : Utilisé pour la **Demande de complément** (REC-03).
- `ReprendreTraitementTicketCommand` : Utilisé pour la **Réponse au complément** (REC-03).
- `TransfererTicketCommand` : Utilisé pour l'orientation manuelle.
- `AjouterMessageCommand` : Utilisé pour les échanges.
