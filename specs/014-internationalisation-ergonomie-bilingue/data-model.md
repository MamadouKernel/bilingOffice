# MODÈLE DE DONNÉES & MATRICE DES RESSOURCES — FEATURE 014

**Feature** : Feature 014 — Internationalisation & Ergonomie Bilingue (Français / Anglais)  
**Périmètre** : **NFR-07** (Should)  

---

## 1. IMPACT SUR LE MODÈLE DE DONNÉES

```text
NOUVELLE ENTITÉ DE DOMAINE : 0
NOUVELLE TABLE SQL         : 0
NOUVELLE COLONNE           : 0
MIGRATION ENTITY FRAMEWORK : AUCUNE (0)
```

La persistance de la préférence linguistique est assurée au niveau HTTP client via le cookie standard `.AspNetCore.Culture`. Aucune modification du schéma relationnel `PlateformeDbContext` n'est requise.

---

## 2. STRUCTURE DES RESSOURCES DE LOCALISATION

Les chaînes localisées sont organisées sous `src/CIT.Plateforme.Web/Resources/` au sein de la classe marqueur `SharedResources` avec support bilingue :
- `SharedResources.fr-FR.resx` (ou dictionnaire bilingue typé)
- `SharedResources.en-US.resx`

### 2.1 Convention de nommage des clés de ressources
Les clés suivent une hiérarchie sémantique stricte :
`<Module>.<Section>.<Element>`

- `Common.AppTitle` / `Common.Save` / `Common.Cancel` / `Common.Back`
- `Nav.Home` / `Nav.BillingImport` / `Nav.BillingExport` / `Nav.Complaints` / `Nav.Enrollment` / `Nav.Simulation`
- `Auth.LoginTitle` / `Auth.Email` / `Auth.Password` / `Auth.Submit` / `Auth.TwoFactorTitle`
- `BillingImport.Title` / `BillingImport.BlNumber` / `BillingImport.Submit`
- `BillingExport.Title` / `BillingExport.BookingNumber` / `BillingExport.Submit`
- `Complaint.Title` / `Complaint.Category` / `Complaint.Description` / `Complaint.Submit`
- `Enrollment.Title` / `Enrollment.TransportCompany` / `Enrollment.Submit`
- `Simulation.Title` / `Simulation.ContainerSize` / `Simulation.Calculate`
- `Status.EnCours` / `Status.Valide` / `Status.Rejete` / `Status.Cloture`

---

## 3. MATRICE DES SURFACES ET COMPOSANTS CERTIFIÉS

| Surface | Composant Razor | Clés majeures | FR (Exemple) | EN (Exemple) |
|:---|:---|:---|:---|:---|
| **Layout & Nav** | `MainLayout.razor`, `PersonaSwitcher.razor` | `Nav.*`, `Common.*` | "Accueil", "Mes Démarches" | "Home", "My Applications" |
| **Auth & 2FA** | `Connexion.razor`, `VerifierCode.razor` | `Auth.*` | "Connexion sécurisée" | "Secure Login" |
| **Demande Import** | `SoumettreDemande.razor` | `BillingImport.*` | "Demande de Facturation Import" | "Import Billing Application" |
| **Demande Export** | `SoumettreDemandeExport.razor` | `BillingExport.*` | "Demande de Facturation Export" | "Export Billing Application" |
| **Réclamation** | `NouvelleReclamation.razor` | `Complaint.*` | "Déposer une réclamation" | "Submit a Complaint" |
| **Enrôlement** | `SoumettreDemande.razor` (Enrôlement) | `Enrollment.*` | "Enrôlement Transporteur" | "Carrier Enrollment" |
| **Simulation** | `SimulationProformaPage.razor` | `Simulation.*` | "Simulation de facturation" | "Proforma Billing Simulation" |
| **Suivi Demarches**| `DetailTicket.razor`, `DetailDossier.razor` | `Status.*`, `Ticket.*` | "Détail de la démarche" | "Application Details" |
