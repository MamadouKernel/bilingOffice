# Data Model — Modèle conceptuel fonctionnel

**Feature** : `003-billing-office-import` | **Date** : 2026-08-30 | **Phase 1 de `/speckit.plan`**

> **Cadre de ce document** : conformément à la constitution (aucun code, aucun schéma SQL détaillé,
> aucune classe C#) et à `research.md` (D1→D16), ce document décrit le modèle **au niveau
> fonctionnel** : concepts, attributs métier significatifs, relations, invariants, traçabilité CDC.
> Aucun concept n'est introduit lorsque `Ticket`/`Dossier` (Feature 001) porte déjà correctement la
> responsabilité — chaque nouveau concept est justifié individuellement (D1 de `research.md`).

---

## 1. Concepts réutilisés sans extension (Feature 001 / Feature 002)

| Concept | Rôle dans Feature 003 | Source |
|---|---|---|
| **Ticket** | Une unité de travail par BL (IMP-02) | Feature 001, inchangé (hors D2) |
| **Dossier** | Regroupement des tickets d'une même soumission multi-BL (SOC-11) | Feature 001, inchangé |
| **File de traitement** | Pilote Manifeste, Facturation, Validation Paiement, Comptabilité Client, Contrôle Documentaire, File IES — 6 nouvelles **instances de données**, aucun nouveau type | Feature 001, inchangé |
| **Transition autorisée** | Parcours des 4 circuits (D6 de `research.md`) — nouvelles **lignes** de données | Feature 001, inchangé |
| **Document** | Pièces à la soumission, facture, preuve de paiement, BS, BADT | Feature 001, inchangé (D11) |
| **Historique / Journal d'audit** | Traçabilité de toute décision (Manifeste, paiement, lettrage, IES, présentiel) | Feature 001, inchangé |
| **Société cliente / Type de client** | Détermine le circuit (D5) | Feature 002, inchangé |
| **Utilisateur rattaché** | Soumetteur, auteur du dossier | Feature 002, inchangé |
| **Permission / Habilitation** | Nouveaux **codes** (D15), mécanisme inchangé | Feature 001, inchangé |

## 2. Extensions additives (Feature 001, strictement additives)

| Entité étendue | Nouveau champ | Type conceptuel | Nullable | Justification |
|---|---|---|---|---|
| **Ticket** | `NumeroBl` | Texte (identifiant métier fourni par le client) | Oui — uniquement renseigné pour les tickets du module Billing IMPORT | D2 — porte le numéro de BL, distinct du numéro technique de ticket |
| **Ticket** | `DeclarationPresentielle` | Indicateur booléen | Oui, défaut non déclaré | D12 — point d'extension IMP-15 pour la future Feature Continuité |
| **Ticket** | `TypeAnnulationImport` | Indicateur nullable à valeur unique (`RejetManifeste`) | Oui — renseigné **uniquement** lors d'un rejet Pilote Manifeste ; `null` pour toute annulation client ordinaire (CYC-01) ou tout ticket d'un autre module | D13bis (revue du 2026-08-30) — distingue le **résultat métier** « Rejeté » de « Annulé » (audit, reporting, affichage client) sans dupliquer ni modifier `StatutTicket` (qui reste `Annule` dans les deux cas) |

**Aucun autre champ n'est ajouté à `Ticket` ou `Dossier`.** En particulier, aucun nouveau statut
n'est ajouté à `StatutTicket` (D13/D13bis de `research.md`) : Rejeté partage le statut `Annule`
existant avec une annulation client ordinaire (même comportement de workflow — jamais rouvert, ne
bloque jamais l'unicité BL) mais s'en distingue au niveau métier via `TypeAnnulationImport` ; les
allers-retours client (complément, preuve invalide) réutilisent `EnAttenteClient`/`EnCours`
existants.

## 3. Concept nouveau — Suivi de branche IES

| Concept | Description | Attributs métier significatifs | Exigences |
|---|---|---|---|
| **Suivi de branche IES** | Représente la progression indépendante d'un des deux rôles (Pilote Manifeste, Pilote Contrôle Documentaire) sur un même ticket IES — deux enregistrements par ticket IES | Ticket concerné, branche (Manifeste / Contrôle Documentaire), achevée (oui/non), auteur, date d'achèvement | IMP-40, IMP-41 |

**Non-entité, volontairement** : il n'existe **aucun** concept « Jointure IES » distinct — la
jointure est une condition **dérivée** (les deux Suivis de branche sont achevés), jamais un état
persisté séparément (D9 de `research.md`).

**Non-entité, volontairement** : il n'existe **aucun** concept « Validation de paiement » ou
« Lettrage » distinct — ces décisions sont tracées par l'historique/l'audit existant et se déduisent
de la position du ticket dans son parcours de files (D7, D14 de `research.md`).

**Non-entité, volontairement** : il n'existe **aucune** entité « DemandeImport » séparée du couple
Dossier/Ticket — la distinction demande/ticket est conceptuelle (regroupement multi-BL vs unité de
traitement), exactement la méthode déjà appliquée par `spec.md` §Key Entities et par Feature 002 pour
« Société cliente ».

## 4. Relations principales

```text
Société cliente (Feature 002) ──1..N── Dossier (Feature 001, ClientId = société, AuteurId = soumetteur)
                                              │
                                              │ TypeClient figé à la création (D5) — pilote l'aiguillage
                                              │
                                              └──1..N── Ticket (Feature 001, DossierId partagé)
                                                              │
                                                              ├─ NumeroBl (D2) — portée d'unicité globale (D3)
                                                              ├─ FileCouranteId — Manifeste / Facturation /
                                                              │   Validation Paiement / Comptabilité Client /
                                                              │   Contrôle Documentaire / File IES
                                                              ├─ Statut — EnAttenteDePriseEnCharge / EnCours /
                                                              │   EnAttenteClient / Cloture / Annule (Rejeté partage
                                                              │   Annule pour le workflow et l'unicité BL, D13/D13bis)
                                                              ├─ TypeAnnulationImport (D13bis, distingue Rejeté d'un
                                                              │   Annule ordinaire — sans impact sur l'unicité BL)
                                                              ├─ DeclarationPresentielle (D12, IMP-15)
                                                              ├─ 1..N Document (facture, preuve de paiement, BS, BADT)
                                                              ├─ 1..N EntreeHistorique / EntreeJournalAudit
                                                              └─ 0..2 Suivi de branche IES (uniquement si IES)
```

## 5. États et transitions métier (niveau fonctionnel — aucune machine à états technique nouvelle)

> Conformément à §11/§6 de la demande : les états ci-dessous sont exprimés en termes de
> **position du ticket** (quelle file, quel statut générique), jamais comme un nouvel enum technique.

### 5.1 Tronc commun

```text
Soumis (File Pilote Manifeste, Statut EnAttenteDePriseEnCharge)
  ├─ Validé   → aiguillage (D5) → première file du circuit déterminé par le type de client
  ├─ Rejeté   → Statut Annule (D13), motif tracé — terminal, jamais rouvert
  └─ Complément demandé → Statut EnAttenteClient → (client dépose) → Statut EnCours,
                            retour File Pilote Manifeste pour nouvelle décision
```

### 5.2 Circuit Client Normal

```text
File Facturation → (facture éditée) → File Validation Paiement
  → (preuve déposée) → [Validée → File Contrôle Documentaire] | [Invalide → Statut EnAttenteClient
      (CLAR-PAIEMENT-01) → (nouvelle preuve) → File Validation Paiement]
File Contrôle Documentaire → (BS + BADT déposés) → Statut Cloture
```

### 5.3 Circuit Client En Compte

```text
File Facturation → (facture éditée) → File Contrôle Documentaire (transition directe,
  sans passer par Validation Paiement) → (documents déposés) → Statut Cloture
```

### 5.4 Circuit Client Deposit

```text
File Facturation → (facture éditée) → File Comptabilité Client
  → [Lettrage confirmé → File Contrôle Documentaire] | [Lettrage impossible/à reprendre
      (CLAR-DEPOSIT-01) → aucune transition, reste en File Comptabilité Client]
File Contrôle Documentaire → (documents déposés) → Statut Cloture
```

### 5.5 Circuit Client IES

```text
File IES (unique) — deux Suivis de branche créés à l'arrivée :
  Branche Manifeste  : en cours → achevée (indépendamment)
  Branche Contrôle Documentaire : en cours → achevée (indépendamment)
Jointure (dérivée, D9) : les deux branches achevées, quel que soit l'ordre
  → transfert vers finalisation → client termine sur la plateforme IES externe (hors périmètre)
  → remise des documents → Statut Cloture
```

## 6. Invariants fonctionnels (extraits traçables)

1. **Un même numéro de BL n'a jamais plus d'un ticket « actif »** (statut ∈
   {EnAttenteDePriseEnCharge, EnCours, EnAttenteClient} — liste d'inclusion explicite des statuts
   bloquants, D3) sur toute la plateforme — portée **globale**, garantie par un mécanisme SGBD (index
   unique filtré à collation explicite fixe, D3), pas seulement applicatif (UNI-01, CLAR-BL-01).
2. **Un BL redevient disponible dès que son unique ticket est Cloture ou Annule** — Rejeté est
   fonctionnellement équivalent à Annule pour cet invariant (UNI-02, CLAR-BL-02).
3. **Le contrôle d'unicité BL ne porte que sur les tickets du module Billing IMPORT** — un numéro de
   BL réutilisé dans un autre module (ex. Customer Complaint) n'est jamais concerné (UNI-03).
4. **Chaque ticket d'une soumission multi-BL évolue indépendamment des autres** — aucune décision
   (validation, rejet, complément) ni aucun échec de création (doublon BL) ne produit d'effet sur un
   autre ticket du même dossier (IMP-02, D4).
5. **L'aiguillage automatique (IMP-06) utilise exclusivement l'instantané `Dossier.TypeClient` figé à
   la création** — jamais le type courant de la société, même si celui-ci a changé depuis (D5,
   invariant hérité de CPT-06, jamais rouvert par cette feature).
6. **Un ticket IES n'est jamais restitué au client avant que les deux branches (Manifeste, Contrôle
   Documentaire) ne soient toutes deux achevées**, indépendamment de leur ordre d'achèvement (IMP-41,
   D9).
7. **Chaque branche IES n'est modifiable que par l'utilisateur habilité à cette branche précise** —
   aucun accès en écriture (et, par défaut, en lecture des champs réservés) à la branche opposée sans
   la permission correspondante (IMP-40, D10).
8. **Toute décision de gestion sensible** (validation/rejet Manifeste, édition de facture, validation
   ou refus de paiement, confirmation de lettrage, achèvement d'une branche IES, déclaration
   présentielle) produit **au minimum** une entrée d'historique et/ou de journal d'audit — aucun
   second système d'audit (ENT-04 réutilisé, D1).
9. **Un ticket Rejeté n'est jamais rouvert** — cohérent avec `Ticket.Rouvrir()` qui n'autorise déjà
   que les tickets `Cloture` (CLAR-02 de Feature 001, jamais modifié).
10. **Aucun élément historisé n'est supprimé physiquement** — même principe que Feature 001/002
    (Principe VIII).

## 7. Données paramétrables (administrées, jamais codées en dur)

Conformément aux Principes VI et XIII :

| Paramètre | Portée | Point ouvert |
|---|---|---|
| Types de pièces exigées par étape et par profil client | Par étape/profil | PC-08 (non bloquant pour le plan — D11) |
| Limite du nombre de BL par soumission (« jusqu'à 5 ») | Globale | PC-19 (valeur par défaut, non figée) |
| Rôle habilité à valider la preuve de paiement | Global | PC-03 (habilitation générique retenue, D15) |
| Liste exacte des champs/actions réservés à chaque branche IES | Par branche | PC-05 (mécanisme spécifié, contenu différé, D10) |

**Immuable, jamais paramétrable** : les 4 profils de client (Normal / En Compte / Deposit / IES —
Feature 002, liste fermée par le CDC) ; la portée globale du contrôle d'unicité BL (CLAR-BL-01,
décision validée, pas un paramètre) ; la règle de normalisation du numéro de BL — insensibilité à la
casse, trim des espaces externes, préservation des tirets et espaces internes (CLARIFICATION PROJET
validée le 2026-08-30, `research.md` D2 — pas une exigence CDC, mais une décision fermée, pas un
paramètre configurable par le métier).

## 8. Traçabilité concepts → familles d'exigences

| Famille | Concepts principaux |
|---|---|
| IMP-01, IMP-02, IMP-03 | Ticket (NumeroBl), Dossier, Document |
| IMP-04, IMP-05 | Ticket (Statut, File Pilote Manifeste), EntreeHistorique |
| IMP-06 | Dossier (TypeClient), TransitionAutorisee |
| IMP-10 à IMP-14 | Ticket (Files Facturation/Validation Paiement/Contrôle Documentaire), Document |
| IMP-15 | Ticket (DeclarationPresentielle) |
| IMP-20 à IMP-22 | Ticket (Files Facturation/Contrôle Documentaire), Document |
| IMP-30 à IMP-32 | Ticket (Files Facturation/Comptabilité Client/Contrôle Documentaire) |
| IMP-40, IMP-41, IMP-42 | Ticket (File IES), Suivi de branche IES |
| UNI-01, UNI-02, UNI-03 | Ticket (NumeroBl, index unique filtré) |

> **Limite assumée** : ce modèle reste à un niveau d'abstraction fonctionnel — attributs techniques
> (types SQL précis, index filtré exact, clés) relèvent de `/speckit.tasks`/`/speckit.implement` sous
> le contrôle de D2/D3 et du Principe VIII.
