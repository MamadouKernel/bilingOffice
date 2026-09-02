# Comptes de Test (Environnement Development)

> **Document** : `docs/09-comptes-test-development.md`  
> **Date** : 30 Août 2026  
> **Statut** : Données de test locales

## Objectif

Ce document liste les identifiants générés par le mécanisme de *seeding* (injection de données) exécuté automatiquement au démarrage de l'application en environnement `Development`.
Il permet de tester manuellement l'intégralité des parcours (Features F001 à F005) localement.

## Avertissement de Sécurité

> [!WARNING]
> **Aucun mot de passe n'est stocké dans le code source ni dans la base de données de production.**  
> Le seeding nécessite la définition préalable de la variable d'environnement locale :  
> `CIT_TEST_USERS_PASSWORD`  
> Si cette variable est absente, la création des comptes de test est ignorée.

### Comment définir le mot de passe localement :
En PowerShell (avant de faire `dotnet run`) :
```powershell
$env:CIT_TEST_USERS_PASSWORD="VotreMotDePasseTestFort!"
```

---

## 1. Comptes Clients (Sociétés)

| Compte (Email) | Persona | Société | Profil (TypeClient) | Permissions Spécifiques | Usage de test |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `client.normal.admin@test.local` | Admin Société | NORMAL TEST SA | Normal | `CompteGererCollaborateurs` | Gestion, accès total (Import/Export/Proforma) |
| `client.normal.user@test.local` | Client Standard | NORMAL TEST SA | Normal | *(Aucune)* | Soumission et consultation limitées |
| `client.encompte.admin@test.local` | Admin Société | EN COMPTE TEST SA | EnCompte | `CompteGererCollaborateurs` | Workflow sans paiement |
| `client.deposit.admin@test.local` | Admin Société | DEPOSIT TEST SA | Deposit | `CompteGererCollaborateurs` | Workflow lettrage |
| `client.ies.admin@test.local` | Admin Société | IES TEST SA | IES | `CompteGererCollaborateurs` | Workflow double guichet |
| `client.sansdroit@test.local` | Client Sans Droit | NORMAL TEST SA | Normal | *(Aucune)* | Test d'accès refusé (403) sur actions spécifiques |

---

## 2. Comptes Internes CIT (Agents)

| Compte (Email) | Persona | Files Habilitées | Permissions | Usage de test |
| :--- | :--- | :--- | :--- | :--- |
| `agent.import@test.local` | Agent Import | Pilote Manifeste | `BillingImportSoumettre`<br>`BillingImportManifesteInstruire` | Valider / Rejeter BL |
| `agent.export@test.local` | Agent Export | Back Office Export | `BillingExportSoumettre`<br>`BillingExportBackOfficeInstruire` | Saisir Draft IN |
| `agent.facturation@test.local` | Agent Facturation | Facturation | `BillingImportFacturationEditer`<br>`BillingExportFacturationEditer` | Éditer factures |
| `agent.paiement@test.local` | Agent Paiement | Validation Paiement | `BillingImportPaiementValider`<br>`BillingExportPaiementValider` | Valider preuve de paiement |
| `agent.cd@test.local` | Agent CD | Contrôle Documentaire | `BillingImportControleDocumentaireTraiter`<br>`BillingExportControleDocumentaireTraiter` | Délivrer BADT / BEE |
| `agent.compta@test.local` | Agent Compta | Comptabilité Client | `BillingImportComptabiliteConfirmerLettrage`<br>`BillingExportComptabiliteConfirmerLettrage` | Traiter les clients Deposit |
| `agent.ies@test.local` | Agent IES | IES | `BillingImportIes...`<br>`BillingExportIes...` | Traitement parallèle IES |
| `superviseur@test.local` | Superviseur | *(Toutes via permissions)* | `SupervisionConsulter`<br>`SupervisionAgir`<br>`ActionDeMasse`<br>`AdministrationFiles` | Tableau de bord de supervision |
| `tarification@test.local` | Admin Tarification | *(Aucune)* | `TarificationAdministrer` | Uploader grilles CSV F005 |
| `agent.sansdroit@test.local` | Agent Négatif | *(Aucune)* | *(Aucune)* | Test de sécurité (403) |
