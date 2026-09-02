# QUICKSTART & SCÉNARIOS DE RECETTE — FEATURE 013

**Feature** : Feature 013 — Continuité Opérationnelle, Brouillons de Saisie, Exposition d'API & Clôture Démarches  
**Date** : 2026-09-01  

---

## 1. SCÉNARIOS DE RECETTE NOMINATIFS

### Scénario 1 : Continuité de saisie sur incident (ERR-01)
1. L'utilisateur ouvre le formulaire de soumission Import (`/billing-import/soumettre`) et renseigne un numéro de BL et des données.
2. L'utilisateur simule une interruption (fermeture d'onglet, rechargement F5, expiration de session).
3. L'utilisateur se reconnecte et rouvre la page.
4. **Vérification** : Un bandeau de reprise s'affiche, les données saisies sont restaurées à l'identique (0 perte de données).

---

### Scénario 2 : Purge automatique du brouillon sur soumission réussie (ERR-01)
1. L'utilisateur reprend son brouillon et clique sur « Soumettre définitivement ».
2. La commande de soumission réussit et génère le ticket.
3. L'utilisateur revient sur la page de soumission.
4. **Vérification** : Le formulaire est réinitialisé vierge, aucun ancien brouillon résiduel ne réapparaît.

---

### Scénario 3 : Cloisonnement multi-tenant des brouillons (ERR-01 / CPT-04)
1. L'utilisateur de la Société A saisit un brouillon.
2. L'utilisateur de la Société B tente d'accéder au brouillon de la Société A via requête directe.
3. **Vérification** : L'accès est strictement rejeté avec `AccesRefuseException` (ou code HTTP 403).

---

### Scénario 4 : Interrogation de statut de ticket par API sécurisée (NFR-08)
1. Un client/partenaire authentifié émet une requête `GET /api/v1/tickets/{numero}`.
2. **Vérification** : L'API retourne le code `200 OK` avec le statut, le module et les horodatages en JSON.
3. Une requête sans jeton/session retourne `401 Unauthorized`.
4. Une tentative de lecture d'un ticket appartenant à une société tierce retourne `403 Forbidden`.

---

### Scénario 5 : Consultation de la documentation OpenAPI / Swagger (NFR-08)
1. L'utilisateur accède à la route de documentation `/swagger`.
2. **Vérification** : L'interface interactive OpenAPI présente la liste exhaustive des endpoints, des paramètres et des schémas de réponse.

---

### Scénario 6 : Contrôles de cohérence et messages d'aide en simulation (SIM-11)
1. L'utilisateur configure un conteneur "Dry" avec option "Branchement frigorifique".
2. **Vérification** : Un message d'aide contextuel explicite signale que le branchement est réservé aux conteneurs Reefer.
3. L'utilisateur saisit une date d'enlèvement antérieure à la date d'arrivée.
4. **Vérification** : Un blocage préventif invite l'utilisateur à corriger la chronologie des dates.

---

### Scénario 7 : Rappel automatique d'expiration de badge (ENR-05)
1. Un badge enrôlé arrive à échéance sous le préavis de rappel configuré.
2. Le traitement `RappelerExpirationBadgesCommand` est exécuté.
3. **Vérification** : L'événement `RappelExpirationBadge` est émis dans `JournalEvenements` et distribué au chauffeur/transporteur.

---

### Scénario 8 : Renouvellement de badge sans ressaisie (ENR-05)
1. Le transporteur déclenche la demande de renouvellement sur son dossier.
2. **Vérification** : Un ticket de renouvellement est créé dans le même `DossierId`, l'historique du badge précédent est conservé, l'audit `DemandeRenouvellementInitiee` est consigné.
