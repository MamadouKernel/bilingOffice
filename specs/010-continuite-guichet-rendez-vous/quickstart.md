# Guide de Démarrage Rapide & Validation — Feature 010
## Continuité Guichet & Rendez-vous

> **Document** : `specs/010-continuite-guichet-rendez-vous/quickstart.md`  
> **Statut** : Validé pour implémentation  
> **Date** : 1er septembre 2026

---

## 1. Vue d'Ensemble & Fonctionnalités Clés

La **Feature 010** apporte la continuité entre l'espace digital et le guichet physique :
1. **Recherche Rapide au Guichet (`/guichet/recherche`)** : Retrouve instantanément un dossier par N° ticket, N° dossier, N° BL/booking ou client.
2. **Prise de Rendez-vous Client (`/rendez-vous/prendre/{ticketId}`)** : Réservation de créneaux avec gestion stricte de la capacité et prévention des conflits.
3. **Mes Rendez-vous Client (`/rendez-vous/mes-rendez-vous`)** : Consultation et annulation de mes créneaux réservés.
4. **Agenda Guichet (`/guichet/agenda`)** : Vue globale des créneaux pour les agents avec constatation de présence (`Honore`).

---

## 2. Exécution des Suites de Tests

### 2.1 Validation Complète de la Solution
```powershell
# Exécution de tous les tests avec limitation du parallélisme
dotnet test -- NUnit.NumberOfTestWorkers=1 xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=1
```

### 2.2 Tests Spécifiques à la Feature 010
```powershell
# Tests du Domaine Guichet
dotnet test --filter "FullyQualifiedName~Guichet" tests/CIT.Plateforme.Domain.Tests

# Tests Applicatifs (Recherche, Réservation, Annulation, Concurrence)
dotnet test --filter "FullyQualifiedName~Guichet" tests/CIT.Plateforme.Application.Tests

# Tests d'Intégration & Concurrence SQL Server Réel
dotnet test --filter "FullyQualifiedName~RendezVousRepositoryTests" tests/CIT.Plateforme.Infrastructure.Tests

# Test E2E de Clôture REC-SCE-14
dotnet test --filter "FullyQualifiedName~RecSce14E2ETests" tests/CIT.Plateforme.Web.Tests
```

---

## 3. Comptes de Test et Profils

| Profil | Identifiant e-mail | Rôle / Espace | Usage F010 |
|:---|:---|:---|:---|
| **Client Standard** | `client@sococim.ci` | Espace Client | Prise de RDV sur ses tickets, consultation, annulation |
| **Agent Billing** | `agent.billing@cotedivoireterminal.com` | Espace Agent | Recherche guichet, reprise sans ressaisie, validation présence |
| **Agent Gate** | `agent.gate@cotedivoireterminal.com` | Espace Agent | Recherche enrôlements, accueil chauffeurs |
| **Superviseur CIT** | `superviseur@cotedivoireterminal.com` | Supervision | Consultation agenda global, gestion charge |
| **Admin CIT** | `m.konate@cotedivoireterminal.com` | Administration | Accès complet, paramétrage créneaux |

---

## 4. Scénario de Démonstration Manuel (Parcours Pas-à-Pas)

1. **Connexion Client** :
   - Se connecter avec `client@sococim.ci`.
   - Ouvrir un ticket Billing Import (`/billing-import/soumettre` ou ticket existant).
   - Cliquer sur « Prendre un rendez-vous au guichet ».
   - Sélectionner une date (ex. demain) et un créneau (ex. 09:00).
   - Valider la réservation. Le statut passe à `Reserve`.
2. **Connexion Agent Guichet** :
   - Se connecter avec `agent.billing@cotedivoireterminal.com`.
   - Naviguer vers `/guichet/recherche`.
   - Saisir le numéro de BL ou le numéro de ticket.
   - Cliquer sur le ticket trouvé pour ouvrir la fiche complète sans ressaisie.
   - Constater le rendez-vous et cliquer sur « Valider présence usager » (`Honore`).
