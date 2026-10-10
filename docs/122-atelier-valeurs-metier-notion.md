# Atelier valeurs métier YABA — suivi de validation

Collez cette page dans Notion. Chaque tableau se transforme en base de données via **⋯ → Turn into database**. La colonne **Statut** est prévue pour une propriété *Select* ; **Responsable** pour une propriété *Personne* ou *Select*.

Valeurs possibles de **Statut** : `À discuter` · `Validé` · `Acté par défaut` · `Livré`

---

## 1 · Paramètres simples à arrêter (base de données principale)

Une ligne = un paramètre. Transformez ce tableau en base de données, puis groupez par **Domaine** ou filtrez par **Statut**.

| Paramètre | Domaine | Responsable | Valeur par défaut | Valeur validée | Statut | Écran de saisie |
| --- | --- | --- | --- | --- | --- | --- |
| Motifs de transfert | Workflow | Billing / Gate | Anomalie doc · Erreur orientation · Prestation complémentaire · Facture à corriger |  | À discuter | /administration/motifs-transfert |
| Niveaux de priorité | Workflow | Superviseurs | Standard (10) · Urgent (20) · Critique (30) |  | À discuter | /administration/priorites |
| Nombre de relances avant clôture | Socle | Support | 3 |  | Acté par défaut | (codé — non administrable) |
| Délai entre deux relances | Socle | Support | 2 jours |  | Acté par défaut | (codé — non administrable) |
| Heure d'ouverture | Exploitation | Exploitation | 7 h 30 |  | À discuter | /administration/parametres-plateforme |
| Heure de fermeture | Exploitation | Exploitation | 17 h 30 |  | À discuter | /administration/parametres-plateforme |
| Samedi ouvré | Exploitation | Exploitation | Non |  | À discuter | /administration/parametres-plateforme |
| Jours fériés exclus du SLA | Exploitation | Exploitation | (liste à saisir) |  | Livré | /administration/jours-feries |
| Nombre max de BL par demande | §25 | DSI / Métier | 5 |  | À discuter | /administration/parametres-plateforme |
| Nombre max de pièces par dépôt | §25 | DSI / Métier | 10 |  | À discuter | /administration/parametres-plateforme |
| Conservation brouillons | §25 | DSI | 30 jours |  | À discuter | /administration/parametres-plateforme |
| Conservation dossiers | §25 | DSI | 5 ans |  | À discuter | /administration/parametres-plateforme |
| Conservation pièces comptables | §25 | DSI | 10 ans |  | À discuter | /administration/parametres-plateforme |
| Conservation journal d'audit | §25 | DSI | 3 ans |  | À discuter | /administration/parametres-plateforme |
| Objectif de disponibilité | §25 | DSI | 99 % |  | À discuter | /administration/parametres-plateforme |
| Proforma obligatoire pour facturer | Facturation | Finance | Oui |  | À discuter | /administration/parametres-plateforme |
| Mode de confirmation de paiement | Facturation | Finance | Preuve + validation caisse |  | À discuter | /administration/parametres-plateforme |
| Canaux de notification actifs | Communication | DSI | E-mail + portail (SMS/WhatsApp exclus) |  | Acté par défaut | /administration/notifications |

---

## 2 · SLA — durées et seuils (base de données dédiée)

Une ligne = une combinaison Module / Type / Étape / Priorité. Écran de saisie : `/administration/parametres-sla`.

| Module | Type de demande | Étape | Priorité | Durée cible | Seuil info (déf. 50 %) | Seuil alerte (déf. 80 %) | Seuil escalade (déf. 100 %) | Statut |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Import |  |  | Standard |  | 50 % | 80 % | 100 % | À discuter |
| Import |  |  | Urgent |  | 50 % | 80 % | 100 % | À discuter |
| Export |  |  | Standard |  | 50 % | 80 % | 100 % | À discuter |
| Export |  |  | Urgent |  | 50 % | 80 % | 100 % | À discuter |

---

## 3 · Règles d'escalade (base de données dédiée)

Écran de saisie : `/administration/regles-escalade`.

| Module | Étape | Priorité | Déclencheur | Action | Destinataire | Statut |
| --- | --- | --- | --- | --- | --- | --- |
|  |  |  | Seuil escalade atteint | Notifier / Élever priorité / Réaffecter |  | À discuter |

---

## 4 · Matrice des transitions de files (base de données dédiée)

Écran de saisie : `/administration/matrice-transitions`. Une ligne = une transition autorisée.

| Module | File source | File cible | Motifs autorisés | Garder | Statut |
| --- | --- | --- | --- | --- | --- |
| Import | Pilote Manifeste | Facturation | Anomalie doc · Erreur orientation | Oui | À discuter |
| Import | Facturation | Contrôle Documentaire | Prestation complémentaire · Facture à corriger · Erreur orientation | Oui | À discuter |
| Import | Facturation | Validation Paiement | Facture à corriger · Erreur orientation | Oui | À discuter |
| Export | Back Office Export | Facturation | Anomalie doc · Erreur orientation | Oui | À discuter |
| Export | Facturation | Contrôle Documentaire | Prestation complémentaire · Facture à corriger · Erreur orientation | Oui | À discuter |
| Export | Facturation | Validation Paiement | Facture à corriger · Erreur orientation | Oui | À discuter |

---

## 5 · Pièces exigées à l'ouverture de compte (base de données dédiée)

Écran de saisie : `/administration/pieces-exigees-compte`.

| Pièce | Obligatoire | Profil(s) | Statut |
| --- | --- | --- | --- |
| RCCM | Oui | Tous | À discuter |
| DFE | Oui | Tous | À discuter |
| CNI représentant | Oui | Tous | À discuter |
| RIB | Non | Tous | À discuter |
| Agrément de transport | Non | Transporteur | À discuter |

---

## 6 · Pièces exigées par étape de workflow (base de données dédiée)

Écran de saisie : `/administration/pieces-exigees-etape`.

| Parcours | Étape | Pièce exigée | Format accepté | Taille max | Obligatoire | Statut |
| --- | --- | --- | --- | --- | --- | --- |
| Import |  |  |  |  |  | À discuter |
| Export |  |  |  |  |  | À discuter |

---

## 7 · Liste de tâches de clôture (à transformer en To-do list)

- [ ] Atelier SLA + seuils arrêtés (tableau 2)
- [ ] Règles d'escalade arrêtées (tableau 3)
- [ ] Matrice des transitions confirmée (tableau 4)
- [ ] Pièces par compte validées (tableau 5)
- [ ] Pièces par étape validées (tableau 6)
- [ ] Paramètres simples renseignés (tableau 1)
- [ ] Jours fériés saisis dans l'application
- [ ] Saisie complète dans l'administration (J3)
- [ ] Export / captures du paramétrage archivés (preuve J2+J3)
