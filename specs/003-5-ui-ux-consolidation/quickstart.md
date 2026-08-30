# Quickstart & Manuel de Validation UI/UX: Consolidation F001-F003

**Feature Branch**: `003-5-ui-ux-consolidation`  
**Date**: 2026-08-30  
**Phase**: `/speckit.plan`

---

## 1. Objectif du Guide
Ce guide décrit la procédure de vérification pas-à-pas des parcours utilisateurs dans le navigateur web, **garantissant qu'aucun identifiant technique (GUID) n'est jamais saisi manuellement par l'utilisateur**.

---

## 2. Parcours 1 : Persona Client Société (Demandeur Import)

1. **Connexion** :
   - Ouvrir l'application sur `http://localhost:5000/`.
   - Constater la redirection vers `/comptes/connexion` (ou clic sur "Se connecter" sur la page d'accueil).
   - Saisir l'e-mail client (ex. `client.import@transafrique.ci`) et son mot de passe.
   - Constater la redirection automatique vers `/client/dashboard`.
2. **Consultation du Tableau de Bord & Actions Requises (`ACR-01`)** :
   - Observer le bandeau supérieur avec le nom de l'entreprise et son profil (*TRANS-AFRIQUE | Profil Normal*).
   - Si un ticket est en attente de complément ou de paiement, observer le widget « Mes actions requises » avec badge d'urgence.
3. **Soumission d'une Nouvelle Demande Import** :
   - Cliquer sur le bouton « Nouvelle demande IMPORT » (ou via la sidebar).
   - Sur la page `/billing-import/soumettre`, constater que la société est déjà renseignée automatiquement.
   - Saisir 2 numéros de BL (ex. `BL-2026-001`, `BL-2026-002`) et cliquer sur « Soumettre ».
   - Constater la redirection automatique vers la vue du dossier créé (`/dossiers/{id}`).
4. **Consultation du Dossier & des Tickets** :
   - Observer la liste des 2 tickets générés de façon indépendante (`IMP-02`).
   - Cliquer sur l'un des tickets pour ouvrir sa fiche détaillée (`/tickets/{id}`).
   - Constater l'affichage complet : numéro, BL, statut "En attente de prise en charge", historique et documents.

---

## 3. Parcours 2 : Persona Pilote Manifeste (Agent CIT)

1. **Connexion Agent** :
   - Se connecter avec un compte Pilote Manifeste (ex. `manifeste@cit.ci`).
   - Constater la redirection automatique vers `/agent/dashboard`.
2. **Tableau de Bord Agent & Disponibilité** :
   - Observer la liste de ses files de compétences avec le compteur de tickets en instance.
   - Basculer son statut de disponibilité sur « Disponible » via le sélecteur d'en-tête (`DIS-01`).
3. **Traitement d'une File Métier** :
   - Cliquer sur la file « Pilote Manifeste » dans le dashboard (ou via la sidebar).
   - Sur la page `/billing-import/manifeste`, constater que la liste des tickets en instance se charge automatiquement sans saisie d'identifiant d'agent.
   - Sur la ligne du ticket `BL-2026-001`, cliquer sur « Valider ».
   - Constater le message de succès et la disparition du ticket de la file.
   - Sur la ligne du ticket `BL-2026-002`, cliquer sur « Demander un complément » (saisir le motif dans la modale).
   - Constater la validation immédiate.

---

## 4. Parcours 3 : Déblocage par le Client (`ACR-01` E2E)

1. **Reconnexion Client** :
   - Se reconnecter avec le compte client `client.import@transafrique.ci`.
2. **Résolution de l'Action Requise** :
   - Constater que le widget « Mes actions requises » affiche désormais : *« Complément demandé pour le BL BL-2026-002 »*.
   - Cliquer sur le bouton d'action « Déposer le complément ».
   - La fiche du ticket s'ouvre directement avec la zone de dépôt mise en évidence.
   - Téléverser le document justificatif demandé.
   - Revenir sur `/client/dashboard` : constater que l'action requise a automatiquement disparu.

---

## 5. Parcours 4 : Persona Administrateur Société

1. **Connexion Administrateur Entreprise** :
   - Se connecter avec le compte `admin@transafrique.ci`.
   - Constater la redirection automatique vers `/societe/dashboard`.
2. **Espace Entreprise & Gestion des Collaborateurs** :
   - Visualiser le résumé de la société, le nombre d'utilisateurs actifs et d'administrateurs.
   - Cliquer sur « Gérer les collaborateurs ».
   - Sur `/societe/collaborateurs`, constater que la liste se charge automatiquement sans saisie de `societeId`.
   - Cliquer sur « Basculer visibilité » pour un collaborateur : constater la mise à jour instantanée sans saisie manuelle.

---

## 6. Parcours 5 : Persona Superviseur / Admin CIT

1. **Connexion Superviseur** :
   - Se connecter avec le compte `superviseur@cit.ci`.
   - Constater la redirection automatique vers `/supervision`.
2. **Supervision des Files & Audit** :
   - Visualiser le backlog par file et la charge par opérateur.
   - Naviguer vers `/administration/audit` via la sidebar : constater l'affichage direct du journal d'audit sans saisie d'identifiant.
   - Utiliser le sélecteur d'espace dans le Header pour basculer vers le Dashboard Agent et tester une file opérationnelle.

---
*(Fin du quickstart).*
