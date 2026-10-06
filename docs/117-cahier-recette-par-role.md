# Cahier de recette par rôle — YABA (06/10/2026)

> Complète le cahier global (`docs/96`, `docs/110`) et le PV §22 (`docs/114`) par une recette
> **organisée par rôle** : chaque profil rejoue uniquement ses écrans et ses actions, en vérifiant
> aussi ce qu'il **ne doit pas** pouvoir faire (cloisonnement). À remplir et signer.
>
> Statuts : **OK** · **KO** · **Réserve** · **N/T**. Hors périmètre : SMS / WhatsApp.

## Comptes de test
Voir `docs/09`/`docs/10`. Chaque testeur se connecte avec le compte du rôle concerné.

---

## 1. Client (normal / en compte / deposit / IES)

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| C1 | Créer un compte société + déposer RCCM/DFE/CNI | Compte soumis, validation CIT requise | | | |
| C2 | Se connecter, activer la 2FA | Accès refusé tant que 2FA non activée | | | |
| C3 | Soumettre un dossier import multi-BL (≤5) | 1 ticket par BL, n° reçus (portail + e-mail) | | | |
| C4 | Lancer une simulation de proforma, l'exporter PDF | Totaux au franc (Annexe A), mention légale | | | |
| C5 | Suivre un ticket, déposer un complément demandé | Statut mis à jour, fil d'échange | | | |
| C6 | Demander la facture, joindre la preuve de paiement | Statuts En attente paiement → validation | | | |
| C7 | Modifier la date d'enlèvement avant facture | Proforma repasse en correction si déjà émise | | | |
| C8 | Télécharger BS/BADT à la clôture | Documents disponibles sur le profil | | | |
| C9 | Demander une annulation / réouverture | Selon étape : annulation directe ou demande | | | |
| C10 | **Cloisonnement** : tenter d'accéder au ticket d'une autre société | **Refusé** | | | |

## 2. Transporteur / Chauffeur

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| T1 | Soumettre un dossier d'enrôlement + pièces (agrément) | Ticket créé, file Agent Gate | | | |
| T2 | Déposer un complément demandé | Dossier revient en instruction | | | |
| T3 | Recevoir la convocation photo/badge | Notification portail + e-mail | | | |
| T4 | Suivre l'échéance permis/assurance/visite | Rappel avant expiration | | | |

## 3. Pilote Manifeste (Contrôle BL import, Front Office)

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| M1 | Voir sa file, prendre en charge un ticket | Badge « pris en charge par » ; horodatages | | | |
| M2 | Valider / Rejeter (motif obligatoire) / Demander complément | Statut + notification client | | | |
| M3 | Transférer un ticket mal orienté (motif obligatoire) | Conforme à la matrice ; refus hors matrice | | | |
| M4 | **Cloisonnement** : agir sur un ticket pris par un autre agent | **Lecture seule** (sauf superviseur) | | | |
| M5 | **IES** : instruire le bloc **Manifeste** d'un ticket IES | Écrit le bloc Manifeste ; bloc Douane **masqué/lecture** | | | |

## 4. Back Office Export (Codification)

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| BO1 | Recevoir un ticket booking, codifier dans IPAKI (hors portail) puis tracer | Codifié / Rejeté / Complément | | | |
| BO2 | **IES export** : instruire le bloc Manifeste/Back Office | Cloisonné du bloc Douane | | | |

## 5. Pilote Facturation Terre

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| F1 | Recevoir le ticket validé, contrôler, éditer la facture (jointe) | Statut Facturé, client notifié | | | |
| F2 | Export : créer le Draft IN avant la facture | Draft IN tracé | | | |
| F3 | Déposer / corriger la proforma | Client valide ou demande correction | | | |

## 6. Comptable Client (Lettrage — clients deposit)

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| CP1 | Recevoir un ticket deposit, lettrer dans IRIS (hors portail), confirmer | Passage au Contrôle Documentaire | | | |

## 7. Caisse / Finance (Validation du paiement)

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| CA1 | Valider une preuve de paiement (moyen + référence) | Statut Payé confirmé ; sinon refus motivé | | | |

## 8. Pilote Contrôle Documentaire

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| D1 | Recevoir le ticket payé, vérifier, déposer BS + BADT (import) / BEE (export) | Documents joints, ticket clôturé + archivé | | | |
| D2 | **IES** : instruire le bloc **Douane** | Écrit le bloc Douane ; bloc Manifeste **masqué/lecture** ; jointure requise avant retour client | | | |

## 9. Support Opérationnel (Customer Complaint)

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| SO1 | Instruire une réclamation par catégorie, demander complément, orienter | Fil tracé | | | |
| SO2 | Clôturer avec réponse formalisée | Réponse conservée dans le ticket | | | |
| SO3 | Instruire un incident technique (Support IT N1/N2) | Selon habilitation | | | |

## 10. Agent Gate (Enrôlement)

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| G1 | Instruire un dossier d'enrôlement, demander complément, valider | Statut mis à jour | | | |
| G2 | Convoquer pour photo, constater la remise du badge | Ticket tracé, clôture | | | |

## 11. Superviseur

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| S1 | Vue temps réel des files / charge / disponibilité | Indicateurs à jour | | | |
| S2 | Affecter un ticket non pris en charge / réaffecter / transférer | Action tracée | | | |
| S3 | Débloquer un ticket pris par un autre agent | Autorisé (superviseur) | | | |
| S4 | Action de masse (réaffecter les tickets d'un absent) | Tracée ticket par ticket | | | |
| S5 | Escalade SLA → notification superviseur | Déclenchée selon règle | | | |

## 12. Administrateur fonctionnel

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| AF1 | Valider / typer un compte société | Compte activé | | | |
| AF2 | Gérer comptes agents + habilitations (granulaires) | Effet immédiat, audité | | | |
| AF3 | Paramétrer files, matrice transitions, motifs, SLA, priorités | Sans développement | | | |
| AF4 | CRUD référentiels (pièces, priorités, motifs, notifications) | Créer/éditer/désactiver (soft-delete) | | | |
| AF5 | Consulter le journal d'audit | Actions tracées (auteur/date/détail) | | | |

## 13. Administrateur technique

| # | Scénario | Attendu | Statut | Date | Visa |
|---|---|---|---|---|---|
| AT1 | Vérifier `/ready` (DB, stockage, antivirus, chiffrement, maintenance) | 5× Healthy | | | |
| AT2 | Sauvegarde + test de restauration | Restauration prouvée | | | |
| AT3 | Désactiver immédiatement un compte agent | Accès coupé, historique conservé | | | |

---

## Cloisonnement IES — matrice des blocs (référence D2/M5)

4 blocs de données : **Manifeste**, **Douane**, **Financier**, **Chauffeur**. Droit par bloc :
écriture (`AllowWrite`), lecture seule, ou **masqué** (défaut = masqué, refus par défaut).

| Rôle | Bloc Manifeste | Bloc Douane |
|---|---|---|
| Pilote Manifeste | **Écriture** | masqué / lecture |
| Pilote Contrôle Documentaire | masqué / lecture | **Écriture** |

Double contrôle serveur : permission de branche (`BillingImportIesManifesteInstruire` /
`…ControleDocumentaireInstruire`) **et** matrice des champs (`PeutEcrire(bloc)`). Le ticket IES
n'est rendu au client qu'après **jointure** des deux branches.

## Décision par rôle

| Rôle | Scénarios OK | Réserves | Décision |
|---|---|---|---|
| (un par ligne) | | | |

Signatures : Recetteur métier · Responsable CIT · DSI.
