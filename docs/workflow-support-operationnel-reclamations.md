# Workflow Support Opérationnel — Réclamations client (support de formation)

> Comment un utilisateur **non-CIT (client)** entre en contact avec le **Support Opérationnel**
> pour ses préoccupations, et comment la réclamation est traitée de bout en bout.

Canal : la plateforme YABA (pas de téléphone/chat dans l'application). Le client dépose une
réclamation **catégorisée**, puis un **échange tracé** s'ouvre sur le ticket, avec notifications
des deux côtés (in-app **et** e-mail).

## 1. Côté CLIENT (non-CIT)

| Étape | Écran | Action |
|------|-------|--------|
| 1. Déposer | Menu → Réclamations → **Soumettre** (`/reclamations/soumettre`) | Choisit une **Catégorie**, saisit **Objet** + **Description**, joint des **pièces** (photo/PDF), puis soumet. Un **numéro de suivi** s'affiche. |
| 2. Confirmation | Notification | « Votre demande a été enregistrée » (in-app + e-mail) avec le numéro de suivi. |
| 3. Suivre / échanger | **Mes réclamations** (`/reclamations/mes-reclamations`) → ouvre le ticket | Lit les **messages** du support, **répond**, **dépose un complément** si demandé. |
| 4. Résolution | Notification | « **Votre réclamation a été traitée** » (in-app + e-mail) → consulte la **réponse formalisée** ; peut évaluer, ou soumettre une nouvelle réclamation si la réponse ne convient pas. |

## 2. Côté SUPPORT OPÉRATIONNEL (agent CIT)

| Étape | Écran | Action |
|------|-------|--------|
| 1. Alerte | Notification | À chaque dépôt : « Nouvelle demande dans votre file » (in-app **+ e-mail**). |
| 2. File | Réclamations → **File** (`/reclamations/file`) | Voit les réclamations de **son périmètre** (catégorie/file), ouvre un ticket (`/tickets/{id}`). |
| 3. Prise en charge | Fiche ticket | **« Prendre en charge »** → le ticket lui est réservé. |
| 4. Instruire | Fiche ticket | **Messages** (échange avec le client), **pièces** (consulte/dépose), **« Demander un complément »** (→ *En attente client*, le client est notifié). |
| 5. Clôturer | Fiche ticket | **« Clôturer »** avec une **réponse formalisée** obligatoire → le client est notifié de la résolution. |

## 3. Paramétrage préalable (administrateur)

- Réclamations → **Catégories** (`/reclamations/administration-categories`) : créer les **catégories**
  et leur **file cible**. C'est la catégorie qui **route** la réclamation vers la bonne file support.
  Sans file cible configurée, la soumission est refusée.
- **Comptes agents** : le profil « Support Opérationnel » doit détenir les droits d'instruction
  (prise en charge, messages, clôture) sur la file des réclamations.

## 4. Boucle de notifications (résumé)

- **Dépôt** → client : « demande enregistrée » ; agents : « nouvelle demande dans votre file » (in-app + e-mail).
- **Complément demandé** → client notifié.
- **Clôture** → client : « réclamation traitée » (in-app + e-mail).

## 5. Traçabilité

- Chaque réclamation porte une **origine** (Portail par défaut). Si un client contacte par un autre
  canal, l'agent peut consigner la réclamation en indiquant l'origine, pour garder l'historique.
- Toutes les actions (prise en charge, messages, complément, clôture) sont **horodatées** dans
  l'historique du ticket.

## 6. Points de vérification pour la recette / formation

1. Un client sans droit de soumission est refusé (habilitation portée par le rattachement société).
2. Une réclamation apparaît uniquement dans la file de sa catégorie (cloisonnement).
3. L'agent reçoit l'alerte **in-app + e-mail** (sous réserve d'une adresse e-mail sur son compte).
4. Le client reçoit la notification de **clôture** avec la réponse formalisée.
