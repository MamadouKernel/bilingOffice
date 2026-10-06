# Comparaison projet ↔ documents 06/10 (Parcours Client + Complément CDC + 3 infographies)

Sources comparées :
- `Parcours Client - YABA.docx`
- `Complement_CDC_YABA_Parcours_Billing_Office_Import_Export.docx` (Rév.01, 06/10/2026)
- 3 infographies : (1) Gestion dossiers/tickets/documents, (2) Parcours IMPORT, (3) Parcours EXPORT.

État projet : `main` = `6f106dd` (prod, `/ready` Healthy).

Légende : ✅ conforme · 🟡 partiel · ❌ manquant.

---

## §2 — Dossier / Ticket / BL-Booking / Documents (infographie 1)

| Règle du doc | État | Détail |
|---|---|---|
| 1 soumission multi-BL/booking = 1 dossier | ✅ | `Dossier` regroupe les tickets, numéro unique. |
| 1 BL / 1 booking = 1 ticket distinct | ✅ | `SoumettreDemandeImportCommand` / `...ExportCommand` : un ticket par référence. |
| Chaque document associé au BL/booking → rattaché au ticket | ✅ | Dépôt par ticket (`DeposerDocumentCommand`). |
| Vue consolidée du dossier | ✅ | Dashboard client + fiche dossier. |
| **NB : document commun à plusieurs références, un seul dépôt** | 🟡 | **Application : OK** (`DeposerDocumentCommunPourClientAsync`, 1 upload → N tickets, cloisonnement par ticket, testé). **UI de multi-sélection « Associer à » (image 1) non branchée** sur l'écran de soumission (ni IMPORT ni EXPORT). Aujourd'hui les pièces du formulaire sont rattachées à **tous** les tickets du dossier, sans case à cocher par référence. |

## §3 — Parcours IMPORT

| Étape du doc | État | Détail |
|---|---|---|
| 3.1 Manifestes (nominal interface / dégradé e-mail ou dépôt YABA → Pilote Manifeste) | ✅ | File Pilote Manifeste, création auto de ticket. |
| 3.2 Création demande, dépôt docs, file Contrôle BL/Booking | ✅ | — |
| 3.3 Contrôle BL/Booking (valider / complément / rejet / transfert) | ✅ | — |
| 3.4 Proforma (dépôt, validation client, demande correction) | ✅ | `GestionProformaCommand`. |
| 3.5 Facture définitive + **Demande d'annulation de facture** (avant règlement, motif obligatoire) | ✅ | `EditerFactureCommand` ; annulation = CYC-02 (facture ⇒ ticket engageant ⇒ bascule auto en demande d'annulation, motif obligatoire, décision agent). |
| 3.6 Client normal : **« Règlement effectué » → Facturation « En attente de confirmation du règlement » → facturier confirme → BADT/Contrôle Doc** | ✅ (paramétrable) | Écart #2 : param `Facturation.ModeConfirmationPaiement`. **Mode déclaratif = exactement le doc.** Défaut actuel prod = preuve+caisse (historique). → **Mettre le paramètre à 1 pour coller au doc.** |
| 3.7 Client deposit (lettrage IRIS → BADT) | ✅ | File Comptabilité Client. |
| 3.8 Client en compte (passage direct BADT) | ✅ | — |
| 3.9 BADT + Bon de Sortie, notification, clôture | ✅ | — |
| 3.10 Règles transverses (n° ticket stable, transferts tracés, SLA, supervision…) | ✅ | — |

## §4 — Parcours EXPORT

| Étape du doc | État | Détail |
|---|---|---|
| 4.1 Création demande booking, docs, file Back Office Export | ✅ | — |
| 4.2 Création/codification booking IPAKI (complément/rejet/transfert) | ✅ | — |
| 4.3 Proforma | ✅ | — |
| 4.4 Validation proforma client | ✅ | — |
| 4.5 Facture définitive + Demande d'annulation de facture | ✅ | — |
| 4.6 Client normal : **« Règlement effectué » → Facturation confirmation → Contrôle Documentaire** | ❌ | **Non implémenté côté EXPORT.** `ReglementDeclaratifCommand` est **IMPORT-only** (module + file Contrôle Documentaire codés import). L'export reste en preuve+caisse (`ValiderPreuvePaiementExportCommand`, `FileValidationPaiementExport`). |
| 4.7 / 4.8 deposit / en compte | ✅ | — |
| 4.9 Contrôle documentaire + BEE, clôture | ✅ | — |
| 4.10 Règles transverses | ✅ | — |

---

## Mise à jour — lot A+B livré (06/10/2026, TDD)

- **A — EXPORT paiement déclaratif : ✅ implémenté.** `ReglementDeclaratifExportCommand` (Declarer/Confirmer) + bouton facturier dans `FileFacturationExport` + bouton client aiguillé par module dans la fiche. **Garde de module** ajoutée aux deux commandes (import refuse un ticket export et inversement) → le risque de mauvais aiguillage est fermé. 2 tests export verts.
- **B — UI « document commun » multi-références : ✅ implémentée.** Composant partagé `DocumentsCommunsFormulaire` (cases « Associer à : BL/booking ») sur les écrans de soumission IMPORT et EXPORT ; dépôt unique via `DepotFichiers.JoindreCommunAsync` → `DeposerDocumentCommunPourClientAsync` (1 upload → tickets cochés). Conforme à l'infographie 1.

Suites : Domain 306 · Application 1111 · Infrastructure 210 · Web 546 — 0 échec.

> Pour être **iso-doc** en prod : passer `Facturation.ModeConfirmationPaiement = 1` (déclaratif), désormais disponible IMPORT **et** EXPORT.

### Correctifs de fidélité au document (06/10/2026)
- **Retour en file Facturation** : à la déclaration « Règlement effectué », le ticket **revient en file Facturation** (doc §3.6/§4.6) — le facturier le voit et le confirme depuis sa file. IMPORT + EXPORT. (Avant, il restait en Validation Paiement et échappait à l'écran facturier.)
- **Libellé** : un ticket en Facturation avec règlement déclaré affiche **« En attente de confirmation du règlement »** (`LibellePaiementCdcHelper`).
- Il ne reste **aucun écart fonctionnel** vs les documents (hors SMS/WhatsApp, hors périmètre). Reste : recette signée (PV) et le réglage du paramètre en prod.

---

## Écarts résiduels (état initial — désormais résorbés par le lot A+B ci-dessus)

1. **EXPORT — mode de paiement déclaratif : ❌ manquant.** Le doc §4.6 décrit le même « Règlement effectué » déclaratif qu'à l'import. Il n'existe pas côté export.
   - *Risque latent* : le bouton « Règlement effectué » (ajouté dans la fiche ticket commune) s'affiche dès que `PeutDeposerPreuvePaiement` est vrai — ce qui est aussi le cas pour un ticket **export** en file « Validation Paiement ». En mode déclaratif activé, un clic appellerait la commande **import** et aiguillerait vers le Contrôle Documentaire **import** (mauvaise file). **Inoffensif tant que le mode déclaratif est OFF (défaut prod).**
   - *Correctif proposé* : garde de module dans `ReglementDeclaratifCommand` (refuser un ticket non-import) + commande/portée export symétrique (`ReglementDeclaratifExportCommand`) + bouton facturier dans `FileFacturationExport`.

2. **Document commun multi-références — UI de sélection : 🟡 partiel.** La capacité applicative existe et est testée ; l'écran de soumission n'expose pas encore les cases « Associer à : BL/booking » de l'infographie 1. Aujourd'hui une pièce du formulaire va à **tous** les tickets du dossier (pas de choix par référence).

3. **Mode de paiement par défaut.** Les documents décrivent **uniquement** le flux déclaratif. Le projet le propose en paramètre, par défaut sur preuve+caisse. Pour être iso-doc : passer `Facturation.ModeConfirmationPaiement = 1` (IMPORT disponible ; EXPORT à implémenter — écart n°1 ci-dessus).

## Déjà conforme (rappel)
Tout le reste des deux parcours (files, transferts tracés, proforma, facture, annulation de facture, deposit/lettrage/en compte, BADT/Bon de Sortie/BEE, notifications, SLA, supervision, traçabilité) est implémenté et couvert par les suites de tests.

> Hors périmètre explicite du client : **SMS / WhatsApp** (non pris en compte) — les notifications e-mail + in-app YABA sont en place.
