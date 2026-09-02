# Quickstart / Plan de Test E2E — Feature 006

> **Document** : `specs/006-customer-complaint/quickstart.md`  
> **Date** : 30 Août 2026

Ce document fournit les instructions pour vérifier manuellement l'intégration complète de Customer Complaint (REC-SCE-07), en s'appuyant sur les personas existants et la configuration de l'environnement de développement.

## 1. Pré-requis (Seed)

Au lancement de l'application (profil `https` via Visual Studio ou `dotnet run`), le `DevelopmentSeeder` injecte automatiquement :
- Les catégories de réclamation (Facturation, Qualité de service, Retard...).
- La file "Support Opérationnel".
- Les rôles et permissions associés.

## 2. Personas de test

- **Client Normal** : `client.normal.user@test.local` (Mot de passe défini par `CIT_TEST_USERS_PASSWORD`).
- **Agent Support Op.** : `agent.support@test.local` (Compte à seeder).
- **Admin Fonctionnel** : `superviseur@test.local`.

## 3. Scénario E2E (REC-SCE-07)

### Étape 1 : Soumission (Client)
1. Connectez-vous avec `client.normal.user@test.local`.
2. Naviguez vers **Nouvelle Réclamation** depuis le Dashboard.
3. Sélectionnez une **Catégorie** (ex: "Erreur Facturation").
4. Remplissez l'Objet et la Description. Soumettez.
5. *Validation* : Un ticket est créé et visible dans "Mes Réclamations".

### Étape 2 : Prise en Charge (Agent)
1. Déconnectez le client et connectez-vous avec `agent.support@test.local`.
2. Allez dans le **Tableau de bord Agent** > File "Support Opérationnel".
3. Identifiez le nouveau ticket et cliquez sur **Prendre en charge**.
4. *Validation* : Le ticket passe "En Traitement", vous êtes l'opérateur assigné.

### Étape 3 : Instruction & Demande de Complément (Agent)
1. Sur la fiche ticket, saisissez un message : "Pouvez-vous préciser le numéro de la facture contestée ?".
2. Cliquez sur l'action **Demander un complément**.
3. *Validation* : Le ticket passe au statut "En Attente Client".

### Étape 4 : Réponse au Complément (Client)
1. Re-connectez-vous avec `client.normal.user@test.local`.
2. Observez la section **Actions Requises (ACR-01)** sur le Dashboard : le ticket y figure.
3. Ouvrez le ticket, répondez au message avec le numéro de facture.
4. *Validation* : Le ticket retourne automatiquement au statut "En Traitement". L'alerte ACR-01 disparaît.

### Étape 5 : Clôture Officielle (Agent)
1. Re-connectez-vous en Agent Support.
2. Ouvrez le ticket.
3. Cliquez sur **Clôturer**.
4. Dans la popup, choisissez le motif "Acceptée" ou "Résolue", et rédigez la **Réponse Formalisée**.
5. Validez.
6. *Validation* : Le ticket est clôturé. Le texte de réponse est visible. L'audit de clôture est tracé.

### Étape 6 : Enquête de Satisfaction (Client)
1. Re-connectez-vous en Client.
2. Ouvrez le ticket clôturé.
3. Repérez le composant de feedback. Cliquez sur 5 étoiles.
4. *Validation* : Le vote est enregistré, le composant se verrouille.
