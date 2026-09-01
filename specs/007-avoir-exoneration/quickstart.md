# Guide de Démarrage & Validation Rapide — Feature 007
## Module Avoir & Exonération

> **Document** : `specs/007-avoir-exoneration/quickstart.md`  
> **Date** : 1er septembre 2026  
> **Nature** : Guide d'exécution et de validation des contrats de la Feature 007.

---

## 1. Vue d'Ensemble

Le module Avoir & Exonération (F007) implémente les exigences `AVE-01` à `AVE-04` du Cahier des Charges.
Il permet aux clients de soumettre des demandes d'avoirs et d'exonérations avec pièces jointes et liaison parent, et aux agents habilités d'instruire les dossiers et de consigner la décision finale (accord ou rejet motivé).

---

## 2. Parcours Utilisateurs Démonstrateurs

### 2.1 Parcours Client — Soumission d'une Demande d'Avoir (AVE-01, AVE-04)
1. Se connecter avec un compte client (ex: `client@sococim.ci`).
2. Naviguer vers `/billing-office/avoir-exoneration/demande`.
3. Sélectionner le type **Avoir**.
4. Renseigner l'objet (*« Erreur de facturation acconage »*), la description explicative, le montant contesté (ex: `150000` XOF).
5. (Optionnel) Sélectionner un ticket de facturation Billing Import existant dans la liste déroulante (`AVE-04`).
6. Téléverser la facture contestée (PDF).
7. Cliquer sur **Soumettre la demande**.
8. **Résultat attendu** :
   - Redirection vers `/billing-office/avoir-exoneration/mes-demandes`.
   - Ticket créé avec préfixe `AVO-2026-XXXXX` et statut `Cree`.
   - Événement `DemandeAvoirSoumise` consigné dans `JournalEvenements`.

---

### 2.2 Parcours Agent — Instruction & Demande de Complément (AVE-02)
1. Se connecter avec un compte agent habilité (ex: `agent.facturation@test.local`, porteur de `Permission.AvoirExonerationInstruire`).
2. Naviguer vers `/billing-office/avoir-exoneration/file`.
3. Cliquer sur la demande soumise pour ouvrir la fiche `/billing-office/avoir-exoneration/ticket/{id}`.
4. Cliquer sur **Prendre en charge** -> Statut passe à `EnCours`.
5. Constater une pièce manquante et cliquer sur **Demander un complément** :
   - Saisir le motif : *« Veuillez joindre le reçu de paiement original »*.
6. **Résultat attendu** :
   - Statut du ticket passe à `EnAttenteClient`.
   - Action requise `ACR-01` visible sur le tableau de bord client.
   - Événement `AvoirComplementDemande` émis.

---

### 2.3 Parcours Client — Fourniture du Complément
1. Le client accède à sa fiche ticket.
2. Dépose le document manquant via la zone de téléversement et clique sur **Marquer complément fourni**.
3. **Résultat attendu** :
   - Le ticket repasse automatiquement au statut `EnCours` dans la file de l'agent.

---

### 2.4 Parcours Agent Décideur — Accord & Dépôt de Décision (AVE-03)
1. Se connecter avec un agent décideur (porteur de `Permission.AvoirExonerationDecider`).
2. Ouvrir le ticket instruit.
3. Cliquer sur **Prononcer un Accord** :
   - Montant accordé : `150000` XOF.
   - Joindre le document officiel de note d'avoir émis par la Finance.
4. Cliquer sur **Confirmer l'accord**.
5. **Résultat attendu** :
   - Le statut passe à `Valide` (affiché *Accordé*) puis `Cloture`.
   - Événement `AvoirAccorde` émis pour notification client.
   - Le document de décision est accessible au client dans l'onglet *Documents*.

---

### 2.5 Parcours Agent Décideur — Rejet Motivé Obligatoire (AVE-03)
1. Ouvrir une demande d'exonération non éligible.
2. Cliquer sur **Rejeter la demande** :
   - Laisser le motif vide -> Message d'erreur de validation (motif obligatoire).
   - Renseigner le motif : *« Attestation d'exonération expirée à la date de déclaration »*.
3. Cliquer sur **Confirmer le rejet**.
4. **Résultat attendu** :
   - Le statut passe à `Rejete` puis `Cloture`.
   - Événement `ExonerationRejetee` émis avec le motif textuel.
   - Le motif de rejet est affiché de façon proéminente sur l'espace client.

---

## 3. Commandes de Validation Automatisée

```bash
# 1. Vérification de la compilation
dotnet build

# 2. Exécution des tests unitaires du domaine Avoir & Exonération
dotnet test tests/CIT.Plateforme.Domain.Tests --filter "FullyQualifiedName~AvoirExoneration"

# 3. Exécution des tests applicatifs use-cases F007
dotnet test tests/CIT.Plateforme.Application.Tests --filter "FullyQualifiedName~AvoirExoneration"

# 4. Exécution des tests Web / bUnit et scénarios TEST-SPEC
dotnet test tests/CIT.Plateforme.Web.Tests --filter "FullyQualifiedName~AvoirExoneration"

# 5. Vérification des changements de modèle EF
dotnet ef migrations has-pending-model-changes --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```
