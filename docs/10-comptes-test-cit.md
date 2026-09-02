# Comptes de Test — Jeu de Données CIT

> **Document** : `docs/10-comptes-test-cit.md`
> **Date** : 30 août 2026
> **Statut** : Données de test locales

## Objectif

Ce document liste les identifiants d'un jeu de comptes de test créé directement en base de données
(script de seeding ponctuel, pas le seeder automatique au démarrage — voir la distinction ci-dessous).
Il couvre les principaux profils métier (agents CIT et sociétés clientes) pour tester manuellement
les parcours des Features F001 à F005.

## Différence avec `09-comptes-test-development.md`

Deux jeux de comptes de test coexistent dans ce dépôt :

| | Ce document (`10-...`) | `09-comptes-test-development.md` |
| :--- | :--- | :--- |
| **Création** | Script ponctuel exécuté une fois contre la base | `DevelopmentSeeder`, exécuté automatiquement à chaque démarrage de l'app en `Development` |
| **Condition** | Aucune — toujours présent une fois créé | Nécessite la variable d'environnement `CIT_TEST_USERS_PASSWORD` |
| **Domaine e-mail** | `@cit-test.local` (agents), `.test` (sociétés) | `@test.local` |
| **Sociétés** | Actives, avec raison sociale distincte par société | Actives, nommées `<PROFIL> TEST SA` |

Les deux jeux sont indépendants et peuvent coexister sans collision (adresses e-mail différentes).

## Base de données

Ces comptes ont été créés dans la base **`BillingOffice`** du conteneur Docker local
(`billingoffice-sqlserver`, SQL Server 2022, port hôte **14330**). Si votre instance de
l'application pointe vers une autre base (ex. LocalDB), ces comptes n'y seront pas visibles —
vérifiez `ConnectionStrings:PlateformeDb` (`dotnet user-secrets list` ou variable d'environnement
`ConnectionStrings__PlateformeDb`).

---

## 1. Comptes CIT (agents / administration)

Tous rattachés à `Nature = Agent`, sans société (par conception — ces comptes ne passent pas par
la vérification d'accès société de `VerifierAccesCompteQuery`, réservée aux comptes Client).

| Compte (e-mail) | Mot de passe | Persona | Habilitations | Usage de test |
| :--- | :--- | :--- | :--- | :--- |
| `admin.cit@cit-test.local` | `CitAdmin#2026` | Admin CIT | `CompteValider`, `CompteTypeModifier`, `AuditConsulter`, `AdministrationFiles`, `AdministrationSla`, `AdministrationPerimetresSupervision`, `TarificationAdministrer` | Validation des comptes société, audit, administration des files/SLA, **administration tarifaire (F005)** |
| `superviseur@cit-test.local` | `CitSuperviseur#2026` | Superviseur | `SupervisionConsulter`, `SupervisionAgir`, `ActionDeMasse`, `TicketPrendreEnCharge` | Supervision temps réel, actions de masse |
| `agent.manifeste@cit-test.local` | `CitAgent#2026` | Pilote Manifeste | `TicketPrendreEnCharge`, `BillingImportManifesteInstruire` (file *Pilote Manifeste*) | Instruction Billing Import (validation/rejet BL) |
| `agent.facturation@cit-test.local` | `CitAgent#2026` | Facturation & Caisse | `BillingImportFacturationEditer`, `BillingImportPaiementValider`, `BillingExportFacturationEditer`, `BillingExportPaiementValider` | Facturation + validation paiement, Import **et** Export |
| `agent.documentaire@cit-test.local` | `CitAgent#2026` | Contrôle Doc. & Compta | `BillingImportControleDocumentaireTraiter`, `BillingImportComptabiliteConfirmerLettrage`, `BillingExportControleDocumentaireTraiter`, `BillingExportComptabiliteConfirmerLettrage` | Contrôle documentaire + comptabilité client, Import et Export |
| `agent.ies@cit-test.local` | `CitAgent#2026` | IES & Back Office Export | `BillingImportIesManifesteInstruire`, `BillingImportIesControleDocumentaireInstruire`, `BillingExportBackOfficeInstruire`, `BillingExportIesBackOfficeInstruire`, `BillingExportIesControleDocumentaireInstruire` | Traitement IES (parcours parallèle) + Back Office Export |

---

## 2. Comptes Sociétés Clientes

Une société par profil `TypeClient` du CDC, toutes **validées et actives** (pas besoin de passer
par le circuit d'approbation admin pour tester).

| Société | Profil (`TypeClient`) | Compte Administrateur | Compte Collaborateur |
| :--- | :--- | :--- | :--- |
| Import Express SARL | Normal | `admin@import-express.test` | `agent@import-express.test` |
| Trans-Afrique Logistique | En Compte | `admin@trans-afrique.test` | — |
| Ivoire Shipping Co | Deposit | `admin@ivoire-shipping.test` | — |
| Atlantic Freight IES | IES | `admin@atlantic-freight.test` | — |

**Mot de passe commun à tous les comptes société : `Client#2026`**

---

## 3. Files de Traitement Associées

Les 12 files nécessaires aux habilitations ci-dessus ont été créées si absentes :

- **BillingImport** : Pilote Manifeste, Facturation, Validation Paiement, Contrôle Documentaire,
  Comptabilité Client, File IES.
- **BillingExport** : Back Office Export, Facturation, Validation Paiement, Contrôle Documentaire,
  Comptabilité Client, File IES.

---

## 4. Notes

- Après connexion, la page `/comptes/connexion` ne redirige pas automatiquement vers le tableau de
  bord — naviguer via le logo ou une entrée du menu.
- Les comptes agents nécessitaient un correctif (`Connexion.razor`, 30/08/2026) pour se connecter :
  la vérification d'accès société bloquait par erreur tout compte sans société, y compris les
  comptes internes CIT qui n'en ont jamais. Corrigé — un compte agent doit maintenant se connecter
  normalement avec ces identifiants.
