# Plan Technique d'Implémentation — Feature 004 : Billing Office EXPORT

> **Dossier** : `specs/004-billing-office-export/`  
> **Date** : 30 Août 2026  
> **Version** : 1.3 (Précision Technique CLAR-BOOK-01)  
> **Statut** : **VALIDÉ POUR /speckit.tasks**  
> **Auteur** : Antigravity Implementation Planning Agent  
> **Sources de vérité** : CIT-CIV-DSI-RFP-XXXX v1.0, `.specify/memory/constitution.md`, `specs/004-billing-office-export/spec.md`, `research.md`, `data-model.md`, `contracts/use-cases.md`

---

## 1. Synthèse Exécutive & Objectifs

La **Feature 004 — Billing Office EXPORT** met en œuvre le parcours complet de gestion des dossiers d'embarquement export au terminal à conteneurs de CIT.
Elle couvre **5 exigences contractuelles du CDC** (`EXP-01`, `EXP-02`, `EXP-03`, `EXP-04` et `UNI-04`) et valide le scénario de recette contractuel **`REC-SCE-05`**.

### Objectifs Clés :
1. **Soumission Multi-Bookings & Traitement Indépendant** : Permettre à un client de soumettre 1 à N bookings dans une même demande générant un dossier unique et des tickets indépendants (`EXP-01`).
2. **Unicité Globale du Booking Actif (`UNI-04`, `PC-09` & `CLAR-BOOK-01`)** : Empêcher les doublons actifs sous forte concurrence via un index unique filtré SQL Server avec libération instantanée à la clôture ou au rejet.
3. **File Back Office & Codification IPAKI (`EXP-02`)** : Assurer la prise en charge, la validation, le rejet motivé ou la demande de complément (déclenchant **ACR-01**).
4. **Orientation par Type Client, Draft IN & Émission du BEE (`EXP-03`)** : Aiguiller selon le profil (`Normal`, `EnCompte`, `Deposit`, `Ies`), déposer le Draft IN en facturation et délivrer le **BEE** (*Bordereau d'Embarquement Électronique*) en Contrôle Documentaire.
5. **Circuit Spécifique Client IES Export (`EXP-04`)** : Traitement parallèle Back Office / Contrôle Documentaire et finalisation IES.
6. **Ergonomie Consolidée & Zéro GUID Manuel** : Intégration transparente dans la coquille Blazor issue de Feature 003.5.

---

## 2. Table Comparative des Circuits par Profil Client

| Étape de Traitement | Client Normal | Client En Compte | Client Deposit | Client IES Export | Source CDC |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **1. Soumission** | Multi-bookings (1..N) | Multi-bookings (1..N) | Multi-bookings (1..N) | Multi-bookings (1..N) | `EXP-01` |
| **2. Unicité Booking** | Index filtré SQL actif | Index filtré SQL actif | Index filtré SQL actif | Index filtré SQL actif | `UNI-04`, `CLAR-BOOK-01` |
| **3. Contrôle Initial** | Back Office (IPAKI) | Back Office (IPAKI) | Back Office (IPAKI) | Traitement Parallèle BO/CD | `EXP-02`, `EXP-04` |
| **4. Facturation / Draft IN**| Dépôt Draft IN + Facture | Dépôt Draft IN + Facture | Dépôt Draft IN + Facture | Sur plateforme IES | `EXP-03`, `EXP-04` |
| **5. Paiement / Encaissement**| Preuve client + Caisse | Sans étape paiement | Lettrage IRIS (hors portail)| Sur plateforme IES | `EXP-03`, `EXP-04` |
| **6. Document Final** | **BEE** (Contrôle Doc) | **BEE** (Contrôle Doc) | **BEE** (Contrôle Doc) | **BEE** (Plateforme IES) | `EXP-03`, `EXP-04` |
| **7. Clôture** | Remise BEE & Clôture | Remise BEE & Clôture | Remise BEE & Clôture | Remise doc & Clôture | `EXP-03`, `EXP-04` |

---

## 3. Matrice de Traçabilité des Documents

| Document | Source CDC Exacte | Obligatoire ? | Déposé Par | Visible Par | Étape du Circuit |
| :--- | :--- | :---: | :--- | :--- | :--- |
| **ConfirmationBooking** | `EXP-01` (§11.3) | **Oui** | Client | Client, Agent BO | Soumission initiale |
| **DeclarationDouaneExport**| `EXP-01` (§11.3) | Optionnel | Client | Client, Agent BO/CD | Soumission / Complément |
| **DraftIn** | `EXP-03` (§11.3) | **Oui** | Facturation | Client, Agent Facturation | Facturation |
| **FactureExport** | `EXP-03` (§11.3) | **Oui** | Facturation | Client, Agent Facturation | Facturation |
| **PreuvePaiement** | `EXP-03` (§11.3) | Pour `Normal` | Client | Client, Caisse/Finance | Paiement |
| **Bee** | `EXP-03`, `EXP-04` (§11.3)| **Oui (doc final)**| Contrôle Doc / IES | Client, Agent CD | Contrôle Documentaire / Clôture |

---

## 4. Matrice des Cas de Tests SQL Server d'Unicité & Normalisation

### Cas de Concurrence & Cycle de Vie (`CAS-BOOK-01..08`)
| Cas de Test | Description du Scénario Concurrence | Résultat Attendu |
| :--- | :--- | :--- |
| **CAS-BOOK-01** | Deux sociétés distinctes soumettent simultanément le même numéro `BKG-100` | Exactement **1 création réussie**, 1 exception de conflit d'unicité. |
| **CAS-BOOK-02** | Même société, deux collaborateurs soumettent simultanément `BKG-100` | Exactement **1 création réussie**, 1 exception de conflit. |
| **CAS-BOOK-03** | Soumission d'un booking déjà en statut actif (`0`, `1` ou `2`) | **Blocage strict** avec message explicite. |
| **CAS-BOOK-04** | Soumission d'un booking dont le ticket précédent est `Cloture` (`3`) | **Création acceptée avec succès** (booking libéré). |
| **CAS-BOOK-05** | Soumission d'un booking dont le ticket précédent est `Annule` (`4`) | **Création acceptée avec succès** (booking libéré). |
| **CAS-BOOK-06** | Soumission d'un booking dont le ticket précédent a été rejeté par le Back Office | **Création acceptée avec succès** (rejet libère le booking). |
| **CAS-BOOK-07** | Échec transactionnel lors de la création d'un ticket | **Rollback complet**, aucun verrou fantôme ou résidu orphelin. |
| **CAS-BOOK-08** | Multi-bookings (`BKG-A` valide, `BKG-B` en doublon actif) | `BKG-A` est créé avec succès, `BKG-B` est signalé en conflit sans bloquer `BKG-A`. |

### Cas de Normalisation & Comparaison (`BOOK-NORM-01..05` — CLAR-BOOK-01)
| Cas de Test | Scénario de Normalisation | Résultat Attendu |
| :--- | :--- | :--- |
| **BOOK-NORM-01** | Soumission de `ABC123` puis tentative de soumission active de `abc123` | **Conflit d'unicité / Doublon détecté** (insensibilité à la casse). |
| **BOOK-NORM-02** | Soumission de `" ABC123 "` puis tentative de soumission active de `ABC123` | **Conflit d'unicité / Doublon détecté** (espaces externes nettoyés). |
| **BOOK-NORM-03** | Soumission de `ABC-123` puis soumission active de `ABC123` | **Deux créations réussies** (conservation stricte des tirets). |
| **BOOK-NORM-04** | Soumission de `ABC 123` puis soumission active de `ABC123` | **Deux créations réussies** (conservation stricte des espaces internes). |
| **BOOK-NORM-05** | Saisie de `" abc123 "` | **Valeur persistée et affichée = `"abc123"`** (espaces externes retirés, casse de saisie préservée). |

---

## 5. Matrice des Contrats Applicatifs (C5-01 à C5-08)

| Identifiant | Cas d'Usage / Commande / Requête | Exigences Couvertes | Acteur Principal | Rôle / Permission Exigée |
| :--- | :--- | :--- | :--- | :--- |
| **C5-01** | `SoumettreDemandeExportCommand` | `EXP-01`, `UNI-04` | Client | `DroitSoumission == true` |
| **C5-02** | `ConsulterFileBillingExportQuery` | `EXP-02` | Opérateur Back Office | `BillingExportBackOfficeInstruire` |
| **C5-03** | `ValiderTicketExportCommand` | `EXP-02` | Opérateur Back Office | `BillingExportBackOfficeInstruire` |
| **C5-04** | `GestionComplementExportCommand` | `EXP-02`, `ACR-01` | Client | Propriétaire de la demande |
| **C5-05** | `EmettreFactureExportCommand` | `EXP-03` | Pilote Facturation | `BillingFacturationInstruire` |
| **C5-06** | `ValiderPaiementExportCommand` | `EXP-03` | Caisse / Finance | `BillingPaiementValider` |
| **C5-07** | `FinaliserControleDocumentaireExportCommand` | `EXP-03` | Pilote Contrôle Doc | `BillingControleDocumentaireInstruire` |
| **C5-08** | `AcheverBrancheIesExportCommand` | `EXP-04` | Back Office / CD | Habilitation selon la branche |

---

## 6. Actions Requises Export (ACR-01)

| État / Condition du Ticket | Action Dérivée Client | Destination UI / Ancre | Source Exigence |
| :--- | :--- | :--- | :--- |
| `Statut == EnAttenteClient` & Motif complément BO renseigné | "Fournir les pièces complémentaires demandées par le Back Office" | `/tickets/{id}#complement` | `EXP-02`, `ACR-01` |
| `Statut == EnAttenteClient` & Facture Export émise (Client Normal) | "Régler la facture Export et déposer la quittance" | `/tickets/{id}#paiement` | `EXP-03`, `ACR-01` |
| `Statut == EnAttenteClient` & Preuve paiement rejetée par Caisse | "Déposer une nouvelle preuve de règlement" | `/tickets/{id}#paiement` | `EXP-03`, `ACR-01` |

---

## 7. Constitution Check & Non-Régression

- **Baseline 310 Tests Verts** : Aucune régression sur Import (`REC-SCE-01..04`), Comptes (`F002`) et Socle (`F001`).
- **Principe I (Clean Architecture)** : Modèle plat, Use Cases étanches.
- **Principe II (Test-First)** : Tests unitaires, SQL Server réel et E2E (`ParcoursExportE2ETests.cs`).
- **Principe X (Zéro Code Mort)** : Traçage IPAKI manuel/opérateur conformément au CDC sans connecteur API fictif.
- **Principe XVII (Zéro GUID Manuel)** : Toutes les pages connectées exploitent `IContexteUtilisateurWeb`.

---

### Statut

# **`READY FOR /speckit.tasks`**
