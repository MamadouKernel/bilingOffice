# Parcours par rôle — création de compte, connexion, actions possibles

> **Document** : `docs/94-parcours-par-role.md`
> **Établi le** : 2026-09-17
> **Pour qui** : équipe CIT (métier + support) — comprendre, pour chaque profil d'utilisateur,
> comment son compte est créé, comment il se connecte, et ce qu'il peut réellement faire une fois
> connecté. Le détail technique (permissions, routes) vient de
> `src/CIT.Plateforme.Application/Web/CatalogueNavigation.cs`, la source unique de vérité du menu :
> ce document en est la traduction lisible, pas une redocumentation séparée qui pourrait diverger.
>
> La connexion elle-même (`/comptes/connexion`, e-mail + mot de passe, 2FA disponible pour tous)
> est identique pour tous les profils authentifiés — ce qui change d'un rôle à l'autre, c'est
> **comment le compte existe** et **où l'utilisateur atterrit après connexion**.

---

## Vue d'ensemble

| Rôle | Comment le compte existe | Espace après connexion | Rattaché à une société ? |
|:---|:---|:---|:---|
| Visiteur anonyme | — (pas de compte) | Page d'accueil publique | — |
| Client (société) | Dossier société validé par CIT | `/client/dashboard` | Oui |
| Collaborateur d'entreprise | Invité par l'administrateur de sa société | `/client/dashboard` | Oui |
| Administrateur de société | Premier compte du dossier société validé | `/societe/dashboard` | Oui |
| Transporteur / Chauffeur | Auto-inscription publique | `/enrolement/mes-enrolements` | **Non, jamais** |
| Agent (Billing Office / Guichet) | Créé par un administrateur CIT | `/agent/dashboard` | Non |
| Superviseur | Créé par un administrateur CIT, permissions de supervision | `/supervision` | Non |
| Administration CIT | Créé par un administrateur CIT, permission `CompteValider` | `/comptes/valider` | Non |

---

## 1. Visiteur anonyme

**Compte** : aucun.

**Ce qu'il peut faire** depuis la page d'accueil (`/`) :
- Simuler une proforma tarifaire sans créer de compte (`/simulation/proforma`) — le seul écran
  entièrement public de la plateforme (voir `docs/90` et le correctif du lien retour accueil).
- Se connecter (`/comptes/connexion`) s'il a déjà un compte.
- Créer un compte société (`/comptes/soumettre`).
- S'inscrire comme transporteur/chauffeur (`/comptes/inscription-transporteur`).

---

## 2. Client — société (demandeur validé)

**Création du compte** :
1. Le demandeur dépose un dossier de société sur `/comptes/soumettre` (Raison Sociale, RCCM,
   Compte Contribuable, pièces justificatives).
2. Un administrateur CIT (`Permission.CompteValider`) instruit et valide le dossier sur
   `/comptes/valider`.
3. Une fois validé, le premier compte devient automatiquement **administrateur de la société**
   (voir section 4) — un simple « client » collaborateur, lui, est ensuite invité par cet
   administrateur (section 3).

**Connexion** : `/comptes/connexion`, e-mail + mot de passe. Redirection automatique vers
`/client/dashboard`.

**Ce qu'il peut faire** (menu « Espace Structure / Gestion des Dossiers / Facturation & Simulation
/ Administration & Sécurité ») :
- Consulter son tableau de bord et le profil de sa société.
- Suivre ses demandes (`/client/demandes`) et ses rendez-vous guichet.
- Soumettre une demande IMPORT (BL) ou EXPORT (Booking), une demande d'Avoir/Exonération.
- Simuler une proforma et consulter son historique de simulations.
- Déposer une réclamation et suivre ses réclamations.
- S'il détient en plus `Permission.EnrolementSoumettre` (rare pour un compte société, réservé au
  parcours transporteur), soumettre un enrôlement.
- Gérer ses préférences de notification et activer la double authentification (2FA).

---

## 3. Collaborateur d'entreprise

**Création du compte** : un administrateur de société l'invite depuis `/comptes/collaborateurs`
(ou `/societe/collaborateurs`) — aucune auto-inscription possible pour ce profil, contrairement au
premier compte de la société.

**Connexion** : identique, redirection vers `/client/dashboard`.

**Ce qu'il peut faire** : les mêmes actions que le Client de la section 2 — un collaborateur est un
compte rattaché à la même société, sans les écrans de gestion de la société elle-même (ceux-là
restent réservés à l'administrateur de société, section 4).

---

## 4. Administrateur de société

**Création du compte** : automatique, pour le tout premier compte associé à un dossier de société
validé (voir section 2). Un administrateur de société peut aussi être désigné explicitement dans
`/comptes/collaborateurs`.

**Connexion** : redirection vers `/societe/dashboard`.

**Ce qu'il peut faire**, en plus de tout ce que peut faire un Client (section 2) :
- Gérer les collaborateurs de sa société (inviter, retirer, désigner un autre administrateur).
- Consulter les demandes de **toute** la société, pas seulement les siennes
  (`/client/demandes?vue=societe`).
- Gérer le profil de l'entreprise (`/comptes/profil`).

---

## 5. Transporteur / Chauffeur

**Création du compte** : auto-inscription publique sur `/comptes/inscription-transporteur`
(e-mail, nom, mot de passe). **Aucune validation CIT n'est requise** — le compte est actif dès sa
création, avec pour seul droit `Permission.EnrolementSoumettre`.

**Ce profil n'est structurellement rattaché à aucune société** — c'est la distinction du CDC entre
un compte client (transactionnel, facturation) et un compte transporteur/chauffeur (opérationnel,
gate physique). `InscrireTransporteurCommand` refuse explicitement d'agir sur un compte déjà
rattaché à une société ou déjà demandeur d'un dossier société : les deux parcours ne se mélangent
jamais.

**Connexion** : redirection vers `/enrolement/mes-enrolements` (corrigé le 2026-09-17 — voir
ci-dessous « Bug corrigé »).

**Ce qu'il peut faire** :
- Déposer un dossier d'enrôlement (permis, carte grise, immatriculation) sur
  `/enrolement/soumettre`.
- Suivre ses dossiers d'enrôlement sur `/enrolement/mes-enrolements`.
- Activer la double authentification (2FA).

**Ce qu'il ne peut PAS faire** : soumettre une demande IMPORT/EXPORT, une demande d'Avoir/
Exonération, une réclamation, ou consulter la simulation de proforma en tant que compte connecté —
tous ces écrans exigent une société rattachée (`ExigeRattachementSociete: true`), que ce profil n'a
jamais. C'est voulu : le CDC ne prévoit pas de facturation pour un compte transporteur.

**Bug corrigé (2026-09-17)** : jusqu'à ce correctif, la page d'accueil redirigeait tout compte sans
espace fonctionnel dédié — donc un transporteur — vers `/client/dashboard`, qui exige un
`SocieteId` et échouait avec « Impossible de charger le tableau de bord : aucun compte société
associé » dès la première connexion. `Home.razor` détecte désormais ce profil (aucune société +
`Permission.EnrolementSoumettre`) et le redirige vers `/enrolement/mes-enrolements`.

---

## 6. Agent (Billing Office / Guichet)

**Création du compte** : exclusivement par un administrateur CIT, sur
`/administration/comptes-agents` (`Permission.AdministrationComptes`). Aucune auto-inscription.
L'administrateur y attribue une ou plusieurs habilitations précises (jamais un « rôle » nommé —
voir `NatureUtilisateur.cs`, PC-03).

**Connexion** : redirection vers `/agent/dashboard`.

**Ce qu'il peut faire** dépend **entièrement** des permissions qui lui ont été attribuées — un
agent sans aucune habilitation ne voit qu'un tableau de bord vide (la recette UAT avait mesuré
jusqu'à 16 liens sur 22 menant à un refus d'accès avant la remédiation du menu). Selon les
habilitations reçues, un agent peut par exemple :
- Rechercher un dossier au Guichet Billing Office, gérer l'agenda de rendez-vous.
- Instruire le Pilote Manifeste ou le Contrôle Documentaire IMPORT.
- Instruire le Back-Office ou le Contrôle Documentaire EXPORT.
- Traiter les dossiers IES (Import/Export).
- Instruire les réclamations, la file Agent Gate (enrôlement), ou les demandes d'Avoir/Exonération.
- Éditer la facturation, valider les paiements en caisse, confirmer le lettrage comptable —
  IMPORT comme EXPORT.

Chaque lien du menu n'apparaît que si l'agent détient réellement la permission correspondante,
éventuellement bornée à une file de traitement précise.

---

## 7. Superviseur

**Création du compte** : par un administrateur CIT, avec `Permission.SupervisionConsulter` et,
selon le périmètre, les permissions d'administration des files/SLA.

**Connexion** : redirection vers `/supervision`.

**Ce qu'il peut faire** :
- Consulter la supervision en direct (tickets, charge par file, alertes SLA).
- Consulter la disponibilité des agents.
- Administrer la stratégie des files, les motifs de transfert, les champs d'enrôlement, les
  paramètres SLA, les règles d'escalade, les niveaux de priorité, les libellés de statut, les
  pièces exigées par étape, la matrice des transitions, les périmètres de supervision, les
  compétences des opérateurs.
- Selon les permissions reçues en plus : valider des demandes de compte société, consulter le
  journal d'audit technique.

---

## 8. Administration CIT (administrateur fonctionnel)

**Création du compte** : par un autre administrateur CIT déjà en place, ou lors de l'amorçage
initial de la plateforme.

**Connexion** : redirection vers `/comptes/valider`.

**Ce qu'il peut faire** :
- Instruire et valider les demandes de compte société.
- Modifier le type de client d'une société (Normal, Grand Compte…).
- Administrer les catégories de réclamation, les modèles/règles de notification.
- Créer et administrer les comptes agents et leurs habilitations.
- Administrer les pièces exigées à l'ouverture d'un compte.
- Administrer les grilles tarifaires et les référentiels conteneur (types, tailles, catégories —
  voir `docs/92`).
- Consulter le journal d'audit.

---

## Synthèse — ce qui distingue vraiment les rôles

Le système ne connaît **aucun rôle nommé en dur** (PC-03, non résolu par choix : « nommer un rôle
serait décider à la place de la DSI »). Ce qu'un compte peut faire dépend de deux choses
seulement, combinées :

1. **Le rattachement à une société** (`SocieteId`) — porte les écrans transactionnels (BL/Booking,
   Avoir/Exonération, réclamations, proforma en tant que compte connecté). Le compte transporteur
   n'en a jamais, par construction.
2. **Les permissions individuellement attribuées** (`Permission.*`, table `HabilitationUtilisateur`)
   — portent chaque écran d'instruction, d'administration ou de supervision. Deux agents peuvent
   avoir des permissions totalement différentes, y compris à l'intérieur du même « espace
   fonctionnel ».

L'« espace fonctionnel » (`EspaceFonctionnel`) ne sert qu'à choisir la destination après connexion
et le jeu de groupes de menu proposé — jamais à accorder un droit : `ConstruireNavigationQuery`
et le contrôle serveur de chaque page appliquent toujours la même règle d'habilitation, qu'importe
l'espace affiché.
