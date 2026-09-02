# Modèle de Données — Feature 006

> **Document** : `specs/006-customer-complaint/data-model.md`  
> **Date** : 30 Août 2026

## 1. Entités Réutilisées

- **`Ticket`** (F001) : Porte le cycle de vie principal de la réclamation.
- **`Dossier`** (F001) : Conteneur du ticket.
- **`FileDeTraitement`** (F001) : File de destination "Support Opérationnel".
- **`Message` / `NoteInterne`** (F001) : Fil de discussion et instruction.
- **`DocumentJoint`** (F001) : Preuves du client et documents de réponse.

## 2. Nouvelles Entités

### `CategorieReclamation`
Référentiel des catégories configurables par l'administrateur (REC-02).
- `Id` (Guid, PK)
- `Code` (string, max 50, Index unique)
- `Libelle` (string, max 100)
- `FileCibleDefautId` (Guid?, FK vers `FileDeTraitement`) — Utilisé pour REC-05.
- `EstActif` (bool)

### `EvaluationSatisfaction` (Value Object / Owned Entity)
Pour répondre à REC-06, rattaché directement au `Ticket`.
- `Note` (int, 1-5)
- `Commentaire` (string?, max 500)
- `DateEvaluationUtc` (DateTime)

## 3. Extensions Additives (Ticket)

L'entité `Ticket` existante reçoit les ajouts suivants :

- `CategorieReclamationId` (Guid?, FK) : Catégorie métier de la réclamation.
- `ReponseFormalisee` (string?, max 4000) : Réponse textuelle officielle de CIT saisie par l'agent à la clôture (REC-04, CLAR-REC-01).
- `EvaluationSatisfaction` (Owned Entity, nullable) : Contient l'évaluation du client (REC-06).

*Justification* : Extension purement additive. Les tickets existants des autres modules auront ces champs à `null`, ce qui garantit une stricte rétrocompatibilité.

## 4. Invariants Métier (Domaine)

1. **Clôture** : Si `Ticket.Module == "CustomerComplaint"`, la méthode `CloturerAvecReponseFormalisee` (nouvelle méthode sur `Ticket`) vérifie que le texte de la réponse n'est pas vide avant de changer le statut (REC-04).
2. **Évaluation** : Une `EvaluationSatisfaction` ne peut être ajoutée que si `Statut == Cloture`. Elle ne peut être définie qu'une seule fois (immuable après création).
3. **Création** : Un ticket de réclamation nécessite obligatoirement une `CategorieReclamationId`.

## 5. Script de Migration (Stratégie)

**Nom de la migration** : `AjoutSupportCustomerComplaint`

**Opérations :**
1. Création de la table `CategoriesReclamation` avec index unique sur `Code`.
2. Création de la foreign key `Ticket.CategorieReclamationId` -> `CategoriesReclamation.Id`.
3. Ajout de la colonne `ReponseFormalisee` (nvarchar(max) ou 4000, null) sur `Tickets`.
4. Ajout des colonnes d'évaluation (`Evaluation_Note`, `Evaluation_Commentaire`, `Evaluation_DateUtc`) sur `Tickets` (nullables).

*Note : Les modifications n'ont aucun impact sur les performances des requêtes existantes.*
