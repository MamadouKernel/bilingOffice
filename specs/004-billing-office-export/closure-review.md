# Revue de Fermeture Finale — Feature 004 : Billing Office EXPORT

> **Dossier de référence** : `specs/004-billing-office-export/`  
> **Date de la revue** : 30 Août 2026  
> **Statut de clôture** : **CLOSE** (Validation formelle complète sans réserve)  
> **Auteur** : Antigravity (Pair Programming AI)  
> **Sources de vérité & documents d'audit** :
> - Constitution du projet : `.specify/memory/constitution.md`
> - Analyse CDC & Matrice d'exigences : `docs/00-analyse-cdc.md`, `docs/01-matrice-exigences.md`, `docs/02-points-a-clarifier.md`, `docs/04-matrice-recette.md`, `docs/07-roadmap-post-ui.md`
> - Spécifications & Conception F004 : `specs/004-billing-office-export/spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/use-cases.md`, `quickstart.md`, `tasks.md`, `implementation-report.md`, `post-implementation-audit.md`
> - Revues des Features précédentes fermées : F001, F002, F003, F003.5.

---

## 1. Résumé Exécutif

La **Feature 004 — Billing Office EXPORT** achève le périmètre du Billing Office pour la branche Export maritime. Elle introduit la gestion des numéros de Booking, l'unicité positive active (UNI-04 / CLAR-BOOK-01), l'instruction Back Office avec traçage IPAKI, les circuits de facturation post-Draft IN (Client Normal, En Compte, Deposit), le circuit spécifique Client IES Export avec parallélisme des branches Back Office et Contrôle Documentaire, ainsi que la délivrance finale du Bordereau d'Enlèvement Export (BEE).

L'ensemble des critères de recette, contrats applicatifs C5-01 à C5-08 et scénarios contractuels REC-SCE-05 a été validé sur instance SQL Server réelle. Aucun écart n'a été constaté lors de l'audit indépendant.

---

## 2. Score & Métriques de la Feature 004

- **Exigences CDC couvertes par F004** : **5 / 5** (100 %)
  - `EXP-01` (Must) : **CONFORME**
  - `EXP-02` (Must) : **CONFORME**
  - `EXP-03` (Must) : **CONFORME**
  - `EXP-04` (Must) : **CONFORME**
  - `UNI-04` (Should / Clarifié CLAR-BOOK-01) : **CONFORME**
- **Couverture globale du CDC post-F004** : **115 / 188 exigences (61,17 %)**
- **Suite de tests automatisés** : **352 / 352 tests passants** (0 échec, 0 ignoré, 0 warning)
  - `Domain.Tests` : 40 / 40
  - `Application.Tests` : 194 / 194
  - `Infrastructure.Tests` : 52 / 52 (SQL Server réel)
  - `Web.Tests` : 66 / 66

---

## 3. Matrice de Conformité des 5 Exigences CDC

| Exigence | Priorité | User Story / Contrat | Code Source Réel | Tests de Preuve | Statut Final |
| :--- | :---: | :--- | :--- | :--- | :---: |
| **`EXP-01`** | **Must** | US1 — Soumission multi-bookings (`C5-01`) | `SoumettreDemandeExportCommand.cs`, `Ticket.cs`, `Dossier.cs` | `SoumettreDemandeExportCommandTests.cs`, `MultiBookingsIndependanceTests.cs`, `SoumettreDemandeExport.razor` | **CONFORME** |
| **`EXP-02`** | **Must** | US2 — File Back Office Export (`C5-02`, `C5-03`, `C5-04`) | `ValiderTicketExportCommand.cs`, `RejeterTicketExportCommand.cs`, `GestionComplementExportCommand.cs`, `FileBackOfficeExport.razor` | `InstructionBackOfficeTests.cs`, `ConsulterFileBillingExportQueryTests.cs`, `RecSce05ExportE2ETests.cs` | **CONFORME** |
| **`EXP-03`** | **Must** | US3/US4/US5 — Facturation, Draft IN, Paiement, Lettrage & BEE (`C5-05`, `C5-06`, `C5-07`) | `EditerFactureExportCommand.cs`, `ValiderPreuvePaiementExportCommand.cs`, `ConfirmerLettrageExportCommand.cs`, `FinaliserControleDocumentaireExportCommand.cs` | `CircuitClientNormalExportTests.cs`, `CircuitClientEnCompteExportTests.cs`, `CircuitClientDepositExportTests.cs`, `RecSce05ExportE2ETests.cs` | **CONFORME** |
| **`EXP-04`** | **Must** | US6 — Circuit Spécifique Client IES Export (`C5-08`) | `InstruireBrancheIesExportCommand.cs`, `ConstaterJointureIesExportEtFinaliserCommand.cs`, `FileIesExport.razor` | `CircuitClientIesExportTests.cs`, `NonRegressionIesImportTests.cs`, `ConcurrenceIesExportFinalisationSqlTests.cs`, `RecSce05ExportE2ETests.cs` | **CONFORME** |
| **`UNI-04`** | **Should** | Unicité active sur le Booking (`CLAR-BOOK-01`) | `PlateformeDbContext.cs`, `AjoutSupportBillingExport.cs`, `Repositories.cs` | `UniciteBookingConcurrenceSqlTests.cs` (`CAS-BOOK-01..08`), `NormalisationBookingSqlTests.cs` (`BOOK-NORM-01..05`), `ControleUniciteBookingWebTests.cs` | **CONFORME** |

---

## 4. Contrats d'Application C5-01 à C5-08

| Contrat | Implémentation | Contrôle d'Accès & Habilitation | Tests Associés | Preuve & Comportement | Statut |
| :--- | :--- | :--- | :--- | :--- | :---: |
| **`C5-01`** | `SoumettreDemandeExportCommand` | `IUtilisateurSocieteRepository` (`DroitSoumission == true`) | `SoumettreDemandeExportCommandTests.cs` | Génère 1 dossier et N tickets indépendants ; rejette les collaborateurs non autorisés. | **CONFORME** |
| **`C5-02`** | `ConsulterFileBillingExportQuery` | `IAutorisationService` (permission de file) | `ConsulterFileBillingExportQueryTests.cs` | Filtre les dossiers accessibles selon les habilitations de l'agent. | **CONFORME** |
| **`C5-03`** | `ValiderTicketExportCommand` & `RejeterTicketExportCommand` | `Permission.BillingExportBackOfficeInstruire` | `InstructionBackOfficeTests.cs` | Valide avec traçage IPAKI et aiguillage snapshot `TypeClient` ; rejet motivé libère le booking. | **CONFORME** |
| **`C5-04`** | `GestionComplementExportCommand` | `BillingExportBackOfficeInstruire` (agent) / `CloisonnementService` (client) | `InstructionBackOfficeTests.cs`, `RecSce05ExportE2ETests.cs` | Gère l'aller-retour complément avec suspension du SLA interne et reprise client sécurisée. | **CONFORME** |
| **`C5-05`** | `EditerFactureExportCommand` | `Permission.BillingExportFacturationEditer` | `CircuitClient*ExportTests.cs` | Dépôt obligatoire de la facture et routage immédiat (Normal ➔ Paiement, En Compte ➔ CD, Deposit ➔ Comptabilité). | **CONFORME** |
| **`C5-06`** | `ValiderPreuvePaiementExportCommand` & `ConfirmerLettrageExportCommand` | `BillingExportPaiementValider` / `BillingExportComptabiliteConfirmerLettrage` | `CircuitClientNormalExportTests.cs`, `CircuitClientDepositExportTests.cs` | Contrôle d'encaissement caisse ou rapprochement comptable avant Contrôle Documentaire. | **CONFORME** |
| **`C5-07`** | `FinaliserControleDocumentaireExportCommand` | `Permission.BillingExportControleDocumentaireTraiter` | `CircuitClientNormalExportTests.cs`, `RecSce05ExportE2ETests.cs` | Dépôt du BEE, passage à l'état terminal `Cloture` et libération de la clé UNI-04. | **CONFORME** |
| **`C5-08`** | `InstruireBrancheIesExportCommand` & `ConstaterJointureIesExportEtFinaliserCommand` | `BillingExportIesBackOfficeInstruire` / `BillingExportIesControleDocumentaireInstruire` | `CircuitClientIesExportTests.cs`, `ConcurrenceIesExportFinalisationSqlTests.cs` | Instruction parallèle Back Office (2) / Contrôle Doc (1) et constatation atomique de jointure. | **CONFORME** |

---

## 5. Unicité & Normalisation Booking (UNI-04 / CLAR-BOOK-01)

- **Portée** : Globale sur la plateforme.
- **Stockage** : `NumeroBooking = NumeroBooking.Trim()`. La casse saisie par l'utilisateur est fidèlement préservée à l'affichage (aucun `ToUpperInvariant()` forcé).
- **Comparaison SQL** : Collation déterministe `Latin1_General_CI_AS`.
- **Règles validées sur SQL Server** :
  - `ABC123` == `abc123` (insensible à la casse)
  - `" ABC123 "` == `ABC123` (espaces externes éliminés)
  - `ABC-123` != `ABC123` (tirets préservés)
  - `ABC 123` != `ABC123` (espaces internes préservés)
- **États bloquants** : `EnAttenteDePriseEnCharge` (0), `EnCours` (1), `EnAttenteClient` (2).
- **États libératoires** : `Cloture` (3), `Annule` (4) y compris `RejetBackOffice`.
- **Index SQL Server réel** :
  ```sql
  CREATE UNIQUE INDEX [IX_Tickets_NumeroBooking_Actif]
  ON [Tickets] ([NumeroBooking])
  WHERE [NumeroBooking] IS NOT NULL AND [Statut] IN (0, 1, 2);
  ```
  *(Aucun `NOT IN` utilisé)*.

---

## 6. Soumission Multi-Bookings (EXP-01)

- **1 Dossier EXPORT** regroupe **N Tickets indépendants** (1 ticket par numéro de booking).
- **Isolation transactionnelle** : Chaque ticket est persisté individuellement. En cas de doublon sur un booking (ex. Booking B), les bookings valides (Bookings A et C) sont créés et persistés avec succès. Aucune transaction globale tout-ou-rien n'a été introduite.

---

## 7. Back Office & Traçage IPAKI (EXP-02)

- File de travail dédiée `Back Office Export`.
- Traçage des contrôles et de la codification IPAKI dans l'historique et l'audit.
- **Vérification d'absence de connecteur externe** : Aucun client HTTP, API REST fictive ou tâche de synchronisation automatique vers IPAKI n'a été codé. Le système gère l'enregistrement et la traçabilité de la codification manuelle de l'opérateur.

---

## 8. Traitement du Rejet Export

- **Annulation standard** : `Statut = Annule`, `TypeAnnulationExport = null`.
- **Rejet Back Office** : `Statut = Annule`, `TypeAnnulationExport = TypeAnnulationExport.RejetBackOffice` (valeur 0).
- Le motif obligatoire est consigné dans `HistoriqueTicket` et `JournalAudit`. La fiche ticket affiche le badge `"❌ Rejet Back Office"` et le numéro de booking est immédiatement libéré.

---

## 9 à 11. Circuits Clients (Normal, En Compte, Deposit — EXP-03)

1. **Client Normal** : Back Office ➔ Facturation (Facture déposée) ➔ Preuve de paiement client ➔ Validation Caisse ➔ Contrôle Documentaire (dépôt BEE) ➔ Clôture.
2. **Client En Compte** : Back Office ➔ Facturation ➔ Transition directe vers Contrôle Documentaire (dépôt BEE) ➔ Clôture (aucune validation caisse).
3. **Client Deposit** : Back Office ➔ Facturation ➔ File Comptabilité Client (lettrage comptable) ➔ Contrôle Documentaire (BEE) ➔ Clôture (aucune API IRIS inventée).

---

## 12 & 13. Circuit IES Export (EXP-04) & Non-Régression IES Import

- **Extension `BrancheIes`** : Ajout de `BackOffice = 2` tout en préservant l'intégrité de `Manifeste = 0` et `ControleDocumentaire = 1`.
- **Instruction parallèle** : Les branches Back Office et Contrôle Documentaire avancent de façon autonome.
- **Jointure & Concurrence optimiste** : `ConstaterJointureIesExportEtFinaliserCommand` empêche toute double clôture concurrente grâce au jeton `Ticket.VersionConcurrence` (`ConcurrenceIesExportFinalisationSqlTests.cs`).
- **Non-Régression F003** : Tous les tests `RecSce04ImportIesTests.cs` restent 100% verts.

---

## 14. Documents & Bordereau d'Enlèvement Export (BEE)

- Les documents d'instruction (`ConfirmationBooking`, `FactureExport`, `PreuvePaiement`, `Bee`) transitent par le composant centralisé et audité `DeposerDocumentCommand`.
- La délivrance du **BEE** constitue la pièce maîtresse du Contrôle Documentaire déclenchant la clôture du ticket.
- Tout téléchargement est cloisonné à l'entreprise émettrice.

---

## 15. Intégration ACR-01 Export

- L'extension de `ObtenirDashboardClientQuery` prend en charge les compléments Export (`TypeActionRequise.ComplementExport`).
- L'action requise pointe directement vers `/tickets/{id}#complement` et disparaît dès que le client soumet son complément. Aucun second moteur ACR-01 n'a été créé.

---

## 16. Immuabilité du Snapshot `Dossier.TypeClient`

- Le routage des tickets existants est piloté exclusivement par `Dossier.TypeClient` (instantané figé à la création).
- Une modification ultérieure du profil de la société n'affecte en rien les dossiers en cours d'instruction (`SnapshotTypeClientExportTests.cs`).

---

## 17. Sécurité & Cloisonnement Multi-Tenant (Anti-IDOR)

- Toutes les commandes clientes vérifient `CloisonnementService.ExigerProprieteClientAsync`. Un client de l'entreprise A est systématiquement rejeté s'il tente d'interagir avec un ticket de l'entreprise B.
- Les actions opératrices sont protégées par les permissions granulaires `BillingExport*`.

---

## 18. Interface Utilisateur Blazor & UX F003.5

- **Zéro GUID manuel** (résolution automatique via `IContexteUtilisateurWeb`).
- **Zéro DbContext / Repository direct dans Razor** (utilisation exclusive de Commandes et Requêtes applicatives).
- Pages opérationnelles et intégrées dans `NavMenu.razor` et `FicheTicket.razor`.

---

## 19. Scénario de Recette Contractuel REC-SCE-05

| Volet REC-SCE-05 | Test Automatisé | Statut |
| :--- | :--- | :---: |
| **EXPORT — Client Normal** | `RecSce05ExportE2ETests.REC_SCE_05_Circuit_Client_Normal_Export_Complet` | **DIRECTEMENT COUVERT** |
| **EXPORT — Client En Compte** | `RecSce05ExportE2ETests.REC_SCE_05_Circuit_Client_EnCompte_Export_Complet` | **DIRECTEMENT COUVERT** |
| **EXPORT — Client Deposit** | `RecSce05ExportE2ETests.REC_SCE_05_Circuit_Client_Deposit_Export_Complet` | **DIRECTEMENT COUVERT** |
| **EXPORT — Client IES Export** | `RecSce05ExportE2ETests.REC_SCE_05_Circuit_Client_IES_Export_Complet` | **DIRECTEMENT COUVERT** |
| **EXPORT — Multi-Bookings** | `RecSce05ExportE2ETests.REC_SCE_05_Multi_Bookings_Independance_Instruction` | **DIRECTEMENT COUVERT** |

---

## 20. Migrations & Modèle EF Core

- Migration [`20260830101722_AjoutSupportBillingExport.cs`](file:///c:/Users/KERNELMK/Documents/cit/cit-digital-platform/src/CIT.Plateforme.Infrastructure/Persistence/Migrations/20260830101722_AjoutSupportBillingExport.cs) présente et exécutable sur base vierge.
- `dotnet ef migrations has-pending-model-changes` confirme : `No changes have been made to the model since the last migration`.

---

## 21. Périmètre Négatif & Points de Clarification (PC)

- **Périmètre négatif respecté** : Aucune absorption des features futures (`SIM`, `REC`, `AVE`, `ENR`, `COM`, `NOT`, `IA`).
- **Points PC** :
  - `PC-09` : Unicité booking active clarifiée et validée (`CLAR-BOOK-01`).
  - `PC-03` : Rôle Caisse/Finance réutilisé sans dérive.
  - `PC-08` : Stockage déclaratif société préservé.

---

## 22. Dette Technique

- **Aucune dette technique résiduelle bloquante ou non planifiée.**
- Tous les tests de concurrence, de normalisation et d'intégration ont été exécutés sur base SQL Server réelle.

---

## 23. État Git & Diff

- **Fichiers modifiés** : Exclusivement les extensions additives légitimes (`Ticket.cs`, `BrancheIes.cs`, `PlateformeDbContext.cs`, `Repositories.cs`, `DependencyInjection.cs`, `NavMenu.razor`, `FicheTicket.razor`, `ObtenirDashboardClientQuery.cs`).
- **Nouveaux fichiers** : Modules `BillingExport` (Application, Web, Tests), migration EF Core, et spécifications associées.
- **Aucun secret, token, mot de passe ou fichier temporaire** n'est présent.

---

## 24. DÉCISION FINALE

```
================================================================================
DÉCISION : CLOSE
================================================================================
La Feature 004 — Billing Office EXPORT est déclarée OFFICIELLEMENT FERMÉE.
Tous les critères de recette, exigences CDC, tests d'infrastructure et
normes architecturales sont satisfaits à 100%.
================================================================================
```
