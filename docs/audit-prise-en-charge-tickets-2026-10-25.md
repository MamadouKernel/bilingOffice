# Audit — Prise en charge avant traitement (tous modules, focus Import/Export)

**Date :** 2026-10-25 · **Méthode :** parcours statique du code (Domain / Application / Web) ·
**Question :** chaque parcours exige-t-il de **s'affecter** le ticket (prise en charge) avant de le traiter ?

## Deux modèles coexistent

- **Affectation individuelle (prise en charge explicite)** : un agent clique « Prendre en charge »
  (statut `EnAttenteDePriseEnCharge → EnCours`, `OperateurAssigneId = agent`) **avant** de pouvoir
  agir. Un seul agent possède le ticket.
- **File par étape, gating par permission (rôle)** : tout agent ayant la permission de l'étape traite
  directement la file. Pas d'affectation individuelle préalable. C'est le modèle **Import/Export**.

## Tableau 1 — Modèle par module

| Module | Écran de traitement | Prise en charge avant traitement ? | Modèle | Preuve |
|---|---|---|---|---|
| Tickets génériques | `FicheTicket.razor` | ✅ Oui, bouton explicite | Affectation individuelle | `PeutPrendreEnCharge`, `PrendreEnChargeCommand` |
| Enrôlement | `FicheEnrolement.razor` | ✅ Oui | Affectation individuelle | `PrendreEnCharge` |
| Réclamation client | `FileReclamations.razor` | ✅ Oui | Affectation individuelle | `PrendreEnCharge` |
| Incident IT (support) | `FileIncidentsIT.razor` | ✅ Oui | Affectation individuelle | `PrendreEnCharge` |
| Avoir / Exonération | `FicheAvoirExoneration.razor` | ✅ Oui | Affectation individuelle | `PrendreEnCharge` |
| **Actualisation BADT** | `FicheActualisationBadt.razor` | ✅ Oui (ajouté le 25/10, `8351afc`) | Affectation individuelle | `PeutPrendreEnCharge` |
| **Import (Billing)** | `BillingImport/File*.razor` | ⚠️ **Non** — aucune prise en charge explicite | File par étape (permission) | aucun `PrendreEnCharge` dans les écrans |
| **Export (Billing)** | `BillingExport/File*.razor` | ⚠️ **Non** — aucune prise en charge explicite | File par étape (permission) | aucun `PrendreEnCharge` dans les écrans |

## Tableau 2 — Parcours IMPORT complet

Soumission client → file par étape, chaque étape protégée par **sa** permission. Aucune étape ne
demande de « s'affecter » : l'agent habilité traite directement. 3 actions posent une prise en charge
**implicite** (statut `EnAttente → EnCours`) en effet de bord du 1ᵉʳ acte.

| Étape | Commande | Permission (rôle) | Prise en charge |
|---|---|---|---|
| Soumission (client) | `SoumettreDemandeImportCommand` | — (client) | — |
| Manifeste / Pilote | `ValiderTicketImportCommand`, `RejeterTicketImportCommand` | `BillingImportManifesteInstruire` | Non |
| Manifeste — complément | `GestionComplementManifesteCommand` | `BillingImportManifesteInstruire` | **Implicite (auto)** |
| Facturation | `EditerFactureCommand`, `ReglementDeclaratifCommand`, `DemanderFactureCommand` | `BillingImportFacturationEditer` | Non |
| Facturation — proforma | `GestionProformaCommand` | `BillingImportFacturationEditer` | **Implicite (auto)** |
| Contrôle documentaire | `DeposerDocumentsControleEtCloturerCommand` | `BillingImportControleDocumentaireTraiter` | Non |
| Comptabilité (lettrage) | `ConfirmerLettrageCommand` | `BillingImportComptabiliteConfirmerLettrage` | Non |
| IES / jointure | `InstruireBrancheIesCommand`, `ConstaterJointureIesEtFinaliserCommand` | `BillingImportIesManifesteInstruire` | Non |
| Validation paiement | `ValiderPreuvePaiementCommand` | `BillingImportPaiementValider` | **Implicite (auto)** |

## Tableau 3 — Parcours EXPORT complet

Même structure : file par étape, gating par permission, 2 actions avec prise en charge implicite.

| Étape | Commande | Permission (rôle) | Prise en charge |
|---|---|---|---|
| Soumission (client) | `SoumettreDemandeExportCommand` | — (client) | — |
| Back Office | `ValiderTicketExportCommand`, `RejeterTicketExportCommand` | `BillingExportBackOfficeInstruire` | Non |
| Back Office — complément | `GestionComplementExportCommand` | `BillingExportBackOfficeInstruire` | **Implicite (auto)** |
| Facturation | `EditerFactureExportCommand`, `EmettreDraftInExportCommand`, `ReglementDeclaratifExportCommand` | `BillingExportFacturationEditer` | Non |
| Contrôle documentaire | `FinaliserControleDocumentaireExportCommand` | `BillingExportControleDocumentaireTraiter` | Non |
| Comptabilité (lettrage) | `ConfirmerLettrageExportCommand` | `BillingExportComptabiliteConfirmerLettrage` | Non |
| IES / jointure | `InstruireBrancheIesExportCommand`, `ConstaterJointureIesExportEtFinaliserCommand` | `BillingExportIesBackOfficeInstruire` | Non |
| Validation paiement | `ValiderPreuvePaiementExportCommand` | `BillingExportPaiementValider` | **Implicite (auto)** |

## Constat

- **Les modules « ticket » (SAV)** — réclamation, enrôlement, incident IT, avoir, et désormais BADT —
  imposent une **prise en charge explicite** avant traitement : un agent s'affecte, puis traite.
- **Import et Export** fonctionnent en **files par étape, gating par permission** : l'agent habilité
  traite directement, sans s'affecter. La prise en charge n'existe qu'**implicitement** sur 3 actions
  import + 2 export (flip de statut, pas de choix d'agent).

Ce n'est pas une anomalie technique : c'est un **choix de modèle** (pipeline collaboratif multi-rôles
pour la facturation, vs dossier possédé par un agent pour le SAV). La vraie question est **métier**.

## Décision à prendre (métier)

| Option | Effet |
|---|---|
| A — Laisser Import/Export en file par étape | Statu quo ; rapide, mais pas de « qui traite quoi » individuel |
| B — Imposer une prise en charge explicite par étape Import/Export | Cohérent avec le SAV ; ajoute un clic + un verrou d'agent par étape (chantier notable) |
| C — Rendre l'auto-affectation implicite systématique | Chaque 1ᵉʳ acte d'une étape affecte l'agent, sans bouton ; traçabilité « qui a traité » sans friction |

Aucun changement de code n'a été fait : ce document est un état des lieux. Dis-moi l'option retenue.
