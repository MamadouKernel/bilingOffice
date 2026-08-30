# Contracts — Contrats fonctionnels des cas d'usage de Feature 003

**Feature** : `003-billing-office-import` | **Date** : 2026-08-30 | **Phase 1 de `/speckit.plan`**

> **Cadre** : descriptions **fonctionnelles** des cas d'usage (acteurs, préconditions, effet, règles,
> exigences couvertes, audit, erreurs/refus). **Aucun code, aucune signature C#, aucun contrat d'API
> technique** — la déclinaison technique appartient à `/speckit.tasks`/`/speckit.implement`. Chaque
> contrôle d'autorisation est **vérifié côté serveur** (Principe VII), via les codes de permission
> définis en `research.md` D15. Numérotation locale à cette feature (`C3-xx`), distincte de `C-xx`
> (Feature 001) et `C2-xx` (Feature 002).

---

## A. Soumission et contrôle d'unicité (IMP-01, IMP-02, IMP-03, UNI-01, UNI-02, UNI-03)

### C3-01 — Soumettre une demande IMPORT multi-BL
- **Acteur** : utilisateur rattaché à une société cliente, disposant du droit de soumission
  (Feature 002, ENT-02).
- **Préconditions** : société active ; rattachement actif ; droit de soumission accordé.
- **Effet** : création d'un dossier (société propriétaire = `Dossier.ClientId`, auteur =
  `Dossier.AuteurId`, instantané du type de client = `Dossier.TypeClient`) ; pour chaque BL soumis
  (jusqu'à 5 par défaut, paramétrable — PC-19), tentative indépendante de création d'un ticket
  (voir C3-02) ; les tickets créés sont regroupés sous ce dossier (SOC-11).
- **Règles** : chaque tentative de création par BL est **indépendante** (`research.md` D4) — un échec
  d'unicité sur un BL ne bloque, n'annule, ni ne retarde les autres BL de la même soumission.
- **Exigences** : IMP-01, IMP-02, IMP-03 (FR-001, FR-006, FR-007 — US1).
- **Audit** : une entrée par ticket créé (auteur = soumetteur, action = « ticket IMPORT créé »,
  détail = numéro de BL).
- **Erreurs/refus** : utilisateur non habilité à soumettre → refus serveur systématique
  (`AccesRefuseException`), quel que soit l'état de l'interface.

### C3-02 — Contrôler l'unicité d'un BL à la création d'un ticket
- **Acteur** : système (déclenché par C3-01, pour chaque BL individuellement).
- **Préconditions** : aucune.
- **Effet** : si aucun ticket actif (statut ∉ {Clôturé, Annulé — Rejeté inclus, `research.md` D13})
  n'existe pour ce numéro de BL sur **toute la plateforme** (`CLAR-BL-01`), le ticket est créé ; sinon
  la création est refusée pour ce BL précis.
- **Règles** : la garantie sous concurrence réelle est assurée au niveau SGBD (index unique filtré,
  `research.md` D3) — jamais par une simple lecture applicative préalable.
- **Exigences** : UNI-01, UNI-02, UNI-03 (FR-002 à FR-005).
- **Audit** : si refus, une entrée d'historique côté client (« BL déjà en cours de traitement »).
- **Erreurs/refus** : BL déjà actif → refus, accès au ticket existant proposé **si et seulement si**
  le client dispose des droits de le consulter (cloisonnement société, CPT-04, absolu) ; sinon message
  générique sans divulgation d'appartenance.

## B. Instruction Pilote Manifeste (IMP-04, IMP-05)

### C3-03 — Valider un ticket au contrôle BL
- **Acteur** : Pilote Manifeste (DEFINED_ROLE, CDC §4).
- **Préconditions** : ticket en file Pilote Manifeste, en attente de prise en charge ou pris en charge
  par cet agent.
- **Effet** : le ticket devient éligible à l'aiguillage automatique (C3-07).
- **Règles** : décision indépendante de celle des autres tickets du même dossier (`research.md` D1).
- **Exigences** : IMP-04 (FR-008 — US3 scénario 1).
- **Audit** : entrée journal (auteur = Pilote Manifeste, action = « ticket validé »).
- **Erreurs/refus** : agent non habilité sur cette file → refus serveur.

### C3-04 — Rejeter un ticket au contrôle BL
- **Acteur** : Pilote Manifeste.
- **Préconditions** : idem C3-03 ; motif de rejet obligatoire.
- **Effet** : le ticket passe au statut `Annule` (`research.md` D13 — jamais `Cloture`, pour interdire
  toute réouverture ultérieure), marqué distinctement `TypeAnnulationImport = RejetManifeste`
  (`research.md` D13bis — préserve la distinction métier avec une annulation client ordinaire sans
  affecter l'unicité BL, qui reste dérivée du seul `Statut`) ; le client est notifié ; le BL redevient
  disponible pour une nouvelle demande (UNI-02, `CLAR-BL-02`).
- **Règles** : le motif est obligatoire et tracé (`EntreeHistorique.Detail`).
- **Exigences** : IMP-04 (FR-008 — US3 scénario 2).
- **Audit** : entrée journal (auteur, action = « ticket rejeté », détail = motif).
- **Erreurs/refus** : motif absent → refus de l'opération (validation applicative) ; agent non habilité
  → refus serveur.

### C3-05 — Demander un complément
- **Acteur** : Pilote Manifeste.
- **Préconditions** : idem C3-03.
- **Effet** : le ticket passe en attente client (`Ticket.MettreEnAttenteClient`) ; le client est notifié.
- **Exigences** : IMP-05 (FR-009 — US3 scénario 3).
- **Audit** : entrée journal (auteur, action = « complément demandé », détail = éléments attendus).
- **Erreurs/refus** : agent non habilité → refus serveur.

### C3-06 — Déposer les éléments de complément (client)
- **Acteur** : le client (auteur du dossier ou utilisateur habilité à consulter/agir sur cette demande,
  Feature 002 ENT-03/CLAR-01/02).
- **Préconditions** : ticket en attente client suite à C3-05.
- **Effet** : les documents/éléments sont déposés (réutilisation `Document`, D11) ; le ticket reprend
  (`Ticket.RependreApresReponseClient`) et retourne en file Pilote Manifeste pour une nouvelle décision.
- **Exigences** : IMP-05 (FR-009 — US3 scénario 3).
- **Audit** : entrée journal (auteur = client, action = « complément déposé »).
- **Erreurs/refus** : client non propriétaire du ticket (cloisonnement, CPT-04) → refus serveur.

## C. Aiguillage automatique (IMP-06)

### C3-07 — Aiguiller automatiquement un ticket validé
- **Acteur** : système (déclenché immédiatement après C3-03).
- **Préconditions** : ticket validé par le Pilote Manifeste.
- **Effet** : le ticket est transféré vers la première file du circuit correspondant au type de client
  **figé à la création du dossier** (`Dossier.TypeClient`, `research.md` D5) : Facturation (Normal, En
  Compte, Deposit) ou File IES (IES).
- **Règles** : **jamais** de lecture du type de client courant de la société (`SocieteCliente.Profil`)
  — un changement de type intervenu après la création du dossier n'a **aucun** effet sur ce ticket.
- **Exigences** : IMP-06 (FR-011, FR-012 — US4).
- **Audit** : entrée journal (action = « ticket aiguillé », détail = circuit déterminé).
- **Erreurs/refus** : aucun — opération système automatique, sans intervention humaine.

## D. Client Normal (IMP-10 à IMP-14)

### C3-08 — Demander la facture
- **Acteur** : client (auteur ou utilisateur habilité de la société).
- **Préconditions** : ticket aiguillé vers le circuit Normal, en file Facturation.
- **Effet** : la demande est enregistrée (déclenche la prise en charge par le Pilote Facturation).
- **Exigences** : IMP-10 (FR-013 — US5 scénario 1).
- **Audit** : entrée journal (auteur = client, action = « facture demandée »).
- **Erreurs/refus** : client non propriétaire → refus serveur.

### C3-09 — Éditer et joindre la facture
- **Acteur** : Pilote Facturation (Terre) (DEFINED_ROLE).
- **Préconditions** : ticket en file Facturation, contrôles réalisés hors portail.
- **Effet** : la facture (`Document`) est jointe au ticket ; le client est notifié ; le ticket rejoint
  la file Validation Paiement (Normal) ou est transféré directement au Contrôle Documentaire (En
  Compte, C3-13) ou à la Comptabilité Client (Deposit, C3-15).
- **Exigences** : IMP-11 (FR-014 — US5 scénario 2).
- **Audit** : entrée journal (auteur = Pilote Facturation, action = « facture éditée »).
- **Erreurs/refus** : agent non habilité sur cette file → refus serveur.

### C3-10 — Déposer une preuve de paiement
- **Acteur** : client.
- **Préconditions** : facture éditée, ticket en file Validation Paiement, règlement effectué hors
  portail par le client.
- **Effet** : la preuve (`Document`) est déposée sur le ticket, en attente de validation.
- **Exigences** : IMP-12 (FR-015 — US5 scénario 3).
- **Audit** : entrée journal (auteur = client, action = « preuve de paiement déposée »).
- **Erreurs/refus** : client non propriétaire → refus serveur.

### C3-11 — Valider ou refuser la preuve de paiement
- **Acteur** : utilisateur habilité à valider le paiement (PC-03, formulation neutre — `research.md`
  D15, permission `billing_import.paiement.valider`).
- **Préconditions** : preuve de paiement déposée (C3-10).
- **Effet** : *validation* → statuts internes En attente paiement → En attente validation → Payé
  confirmé, ticket transféré vers le Contrôle Documentaire. *Refus* (`CLAR-PAIEMENT-01`) → le ticket
  retourne en attente client (`Ticket.MettreEnAttenteClient`), motif tracé, le client peut déposer une
  nouvelle preuve (retour à C3-10).
- **Exigences** : IMP-13 (FR-016 — US5 scénarios 4 et 6).
- **Audit** : entrée journal (auteur, action = « paiement validé » ou « preuve refusée », détail =
  motif si refus).
- **Erreurs/refus** : agent non habilité (permission `billing_import.paiement.valider` manquante) →
  refus serveur, quel que soit l'état de l'interface.

### C3-12 — Déposer BS/BADT et clôturer (Contrôle Documentaire)
- **Acteur** : Pilote Contrôle Documentaire (DEFINED_ROLE).
- **Préconditions** : ticket en file Contrôle Documentaire (Normal : après C3-11 ; En Compte : après
  C3-13 ; Deposit : après C3-15).
- **Effet** : vérification du dossier, dépôt de Bon de Sortie et BADT (`Document`), retour au client,
  clôture (`Ticket.Cloturer`).
- **Exigences** : IMP-14 (FR-017 — US5 scénario 5), IMP-22, IMP-32 (variantes En Compte/Deposit).
- **Audit** : entrée journal (auteur = Pilote CD, action = « documents déposés et ticket clôturé »).
- **Erreurs/refus** : agent non habilité → refus serveur.

## E. Client En Compte (IMP-20 à IMP-22)

### C3-13 — Transmettre directement au Contrôle Documentaire (variante En Compte)
- **Acteur** : système, déclenché immédiatement après C3-09 lorsque le type de client figé du dossier
  est « En Compte ».
- **Préconditions** : facture éditée pour un dossier En Compte.
- **Effet** : le ticket est transféré directement au Contrôle Documentaire — **aucune** file de
  Validation Paiement n'est empruntée (aucune étape de paiement, `research.md` D6).
- **Exigences** : IMP-20, IMP-21 (FR-018, FR-019 — US6 scénario 1).
- **Audit** : entrée journal (action = « transmission directe En Compte »).
- **Erreurs/refus** : aucun — transition système automatique.

### C3-14 — Récupérer le ticket clôturé avec documents (En Compte)
- **Acteur** : client en compte.
- **Préconditions** : ticket clôturé (C3-12).
- **Effet** : consultation de l'ensemble des documents (facture, BS, BADT).
- **Exigences** : IMP-22 (FR-020 — US6 scénario 2).
- **Audit** : hors périmètre (consultation, pas de mutation).
- **Erreurs/refus** : client non propriétaire → refus serveur (cloisonnement).

## F. Client Deposit (IMP-30 à IMP-32)

### C3-15 — Confirmer le lettrage (Comptabilité Client)
- **Acteur** : Comptable Client (DEFINED_ROLE), permission `billing_import.comptabilite.confirmer_lettrage`.
- **Préconditions** : ticket en file Comptabilité Client (après édition de facture, dossier Deposit) ;
  lettrage effectué dans IRIS, hors portail (`research.md` D7 — aucune intégration).
- **Effet** : *confirmation* → le ticket est transféré vers le Contrôle Documentaire. *Lettrage
  impossible ou à reprendre* (`CLAR-DEPOSIT-01`) → **aucune action** ; le ticket reste dans la file
  Comptabilité Client jusqu'à résolution manuelle, sans clôture automatique ni retour vers la
  Facturation.
- **Exigences** : IMP-31 (FR-022 — US7 scénarios 2 et 4).
- **Audit** : entrée journal si confirmation (auteur = Comptable Client, action = « lettrage confirmé »).
- **Erreurs/refus** : agent non habilité → refus serveur.

## G. Client IES (IMP-40 à IMP-42)

### C3-16 — Instruire une branche IES (Manifeste)
- **Acteur** : Pilote Manifeste, permission `billing_import.ies.manifeste.instruire`.
- **Préconditions** : ticket IES en File IES ; branche Manifeste non encore achevée.
- **Effet** : traitement des champs/actions réservés à cette branche (portée exacte différée, PC-05,
  `research.md` D10) ; à l'issue, la branche Manifeste est marquée achevée.
- **Règles** : progression **indépendante** de la branche Contrôle Documentaire, dans n'importe quel
  ordre.
- **Exigences** : IMP-40 (FR-024 — US8 scénario 1).
- **Audit** : entrée journal (auteur, action = « branche Manifeste IES achevée »).
- **Erreurs/refus** : agent ne détenant que la permission de la branche opposée → refus serveur, aucun
  accès aux champs réservés à cette branche.

### C3-17 — Instruire une branche IES (Contrôle Documentaire)
- **Acteur** : Pilote Contrôle Documentaire, permission
  `billing_import.ies.controle_documentaire.instruire`.
- **Préconditions** : ticket IES en File IES ; branche Contrôle Documentaire non encore achevée.
- **Effet** : symétrique de C3-16 pour la branche Contrôle Documentaire.
- **Exigences** : IMP-40 (FR-024 — US8 scénario 1).
- **Audit** : entrée journal (auteur, action = « branche Contrôle Documentaire IES achevée »).
- **Erreurs/refus** : symétrique de C3-16.

### C3-18 — Constater la jointure et finaliser (IES)
- **Acteur** : système, déclenché à l'achèvement de la seconde branche (C3-16 ou C3-17, quel que soit
  l'ordre).
- **Préconditions** : les deux branches sont achevées (`research.md` D9).
- **Effet** : le ticket est rendu disponible pour que le client finalise (facturation, paiement,
  documents) sur la plateforme IES externe (hors périmètre) ; à la remise des documents, le ticket
  portail est clôturé.
- **Exigences** : IMP-41, IMP-42 (FR-025, FR-026 — US8 scénarios 2 et 3).
- **Audit** : entrée journal (action = « jointure IES constatée » puis « ticket IES clôturé »).
- **Erreurs/refus** : tentative de finalisation avant jointure complète → refus serveur systématique.

## H. Alternative présentielle (IMP-15)

### C3-19 — Déclarer une finalisation présentielle
- **Acteur** : client, auteur ou utilisateur habilité de la société.
- **Préconditions** : ticket validé par le Pilote Manifeste (à l'issue du contrôle BL).
- **Effet** : `Ticket.DeclarationPresentielle` est positionné à vrai (`research.md` D12) ; le ticket
  porte la trace de cette bascule, disponible pour la future Feature « Continuité Digital/Présentiel »
  (CON-01/02/03) — **aucun** mécanisme de rendez-vous, de recherche de guichet, ou de créneau n'est
  implémenté par ce contrat.
- **Exigences** : IMP-15 (FR-027 — US9, Should).
- **Audit** : entrée journal (auteur = client, action = « bascule présentielle déclarée »).
- **Erreurs/refus** : client non propriétaire → refus serveur ; ticket non encore validé par le Pilote
  Manifeste → refus (précondition non remplie).

---

## Couverture — tous les FR de `spec.md` sont couverts

| FR | Contrat(s) |
|---|---|
| FR-001, FR-006, FR-007 | C3-01 |
| FR-002 à FR-005 | C3-02 |
| FR-008 | C3-03, C3-04 |
| FR-009 | C3-05, C3-06 |
| FR-011, FR-012 | C3-07 |
| FR-013 | C3-08 |
| FR-014 | C3-09 |
| FR-015 | C3-10 |
| FR-016 | C3-11 |
| FR-017 | C3-12 |
| FR-018, FR-019 | C3-13 |
| FR-020 | C3-14 |
| FR-021 | C3-09 (variante Deposit) |
| FR-022 | C3-15 |
| FR-023 | C3-12 (variante Deposit) |
| FR-024 | C3-16, C3-17 |
| FR-025, FR-026 | C3-18 |
| FR-027 | C3-19 |

**19 contrats, 27/27 FR couverts, aucun orphelin.**
