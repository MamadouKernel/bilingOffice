# Fiche d'atelier « valeurs métier » (J2) + cartographie des écrans de saisie (J3) — 10/10/2026

> Objectif J2 : faire **arrêter par les responsables** les valeurs paramétrables du CDC (§25).
> Objectif J3 : les **saisir dans l'administration sans développement**.
> Cette fiche sert de support d'atelier (à remplir) **et** de checklist de couverture des écrans.
>
> Convention : la colonne « Valeur validée » reste vide jusqu'à l'atelier. La colonne « Écran »
> indique où saisir sans code. ⚠️ = point qui demande encore un petit développement avant J3.

---

## PARTIE A — Questionnaire d'atelier (à remplir avec le métier)

### A1. SLA — durées cibles et seuils (§9.6, SLA-05) · *Billing, Gate, Finance, Support*

Pour **chaque** combinaison Module / Type de demande / Étape / Priorité, arrêter :

| Module | Type demande | Étape | Priorité | Durée cible | Seuil info (déf. 50 %) | Seuil alerte (déf. 80 %) | Seuil escalade (déf. 100 %) | Valeur validée |
|---|---|---|---|---|---|---|---|---|
| Import | | | Standard | | | | | |
| Import | | | Urgent | | | | | |
| Export | | | Standard | | | | | |
| … | | | | | | | | |

> Questions de cadrage : quelles étapes ont un délai contractuel ? Les seuils 50/80/100 % conviennent-ils
> ou faut-il les ajuster par étape ? Le SLA se suspend-il en « attente client » (oui par défaut) ?

### A2. Règles d'escalade (§9.7) · *Superviseurs*

| Module | Étape | Priorité | Déclencheur (état) | Action (Notifier / Élever priorité / Réaffecter) | Destinataire | Valeur validée |
|---|---|---|---|---|---|---|
| | | | Seuil escalade atteint | | | |

### A3. Matrice des transitions de files (§9.4, TRF-06) · *Billing, Gate*

Confirmer / compléter les transitions autorisées et les motifs obligatoires :

| Module | File source | File cible | Motifs autorisés | Garder (O/N) ? | À ajouter |
|---|---|---|---|---|---|
| Import | Pilote Manifeste | Facturation | Anomalie documentaire ; Erreur d'orientation | | |
| Import | Facturation | Contrôle Documentaire | Prestation complémentaire ; Facture à corriger ; Erreur d'orientation | | |
| Import | Facturation | Validation Paiement | Facture à corriger ; Erreur d'orientation | | |
| Export | Back Office Export | Facturation | Anomalie documentaire ; Erreur d'orientation | | |
| Export | Facturation | Contrôle Documentaire | Prestation complémentaire ; Facture à corriger ; Erreur d'orientation | | |
| Export | Facturation | Validation Paiement | Facture à corriger ; Erreur d'orientation | | |
| … | *(transitions illustratives du CDC à trancher : Contrôle BL ↔ Facturation, Comptabilité → …)* | | | | |

### A4. Motifs de transfert (§9.10) · *Billing, Gate*

Amorcés : **Anomalie documentaire · Erreur d'orientation · Prestation complémentaire · Facture à corriger**.
Motifs à ajouter / retirer : _________________________________________________

### A5. Priorités et attribution automatique (PRI-01) · *Superviseurs*

- Niveaux amorcés : **Standard (10) · Urgent (20) · Critique (30)**. Modifier libellés/rangs ? _______
- Règles d'attribution automatique de priorité (ex. « marchandise dangereuse → Urgent ») :

| Condition (module / type / critère) | Priorité attribuée | Valeur validée |
|---|---|---|
| | | |

### A6. Pièces exigées à l'ouverture de compte (CPT-01) · *Finance, Support*

| Pièce | Obligatoire ? | Profil(s) concerné(s) | Valeur validée |
|---|---|---|---|
| RCCM | Oui (déf.) | Tous | |
| DFE | Oui (déf.) | Tous | |
| CNI représentant | Oui (déf.) | Tous | |
| RIB | Facultatif (déf.) | Tous | |
| Agrément de transport | Facultatif (déf.) | Transporteur | |
| *(autre)* | | | |

### A7. Pièces exigées par étape de workflow (SOC-07) · *Billing, Gate*

| Parcours | Étape | Pièce exigée | Format accepté | Taille max | Obligatoire ? | Valeur validée |
|---|---|---|---|---|---|---|
| Import | | copie BL / déclaration douane / preuve paiement… | | | | |
| Export | | | | | | |

### A8. Relances « absence de réponse » (CYC-04) · *Support* — **valeurs actées par défaut (10/10)**

| Paramètre | Valeur actée | Modifiable sans dev ? |
|---|---|---|
| Nombre de relances avant suggestion de clôture | **3** | Non (codé) — à rouvrir seulement si le métier l'exige |
| Délai entre deux relances | **2 jours** | Non (codé) |

> Décision du 10/10 : on conserve 3 relances / 2 jours par défaut (0 développement). À ne rendre
> administrable que si le métier demande explicitement de pouvoir les régler.

### A9. Horaires de fonctionnement (pour le calcul SLA, SLA-04) · *Exploitation*

| Paramètre | Valeur actuelle | Valeur à arrêter |
|---|---|---|
| Heure d'ouverture | 7 h 30 | |
| Heure de fermeture | 17 h 30 | |
| Samedi ouvré ? | Non | |
| Jours fériés exclus du calcul | *(liste à saisir)* ✅ | |

> ✅ Depuis le 10/10, les heures, le samedi **et la liste des jours fériés** sont administrables (écran dédié, voir Partie B).

### A10. Autres paramètres §25 · *Exploitation / DSI*

| Paramètre | Valeur actuelle (déf.) | Valeur à arrêter |
|---|---|---|
| Nombre max de BL par demande | 5 | |
| Nombre max de pièces par dépôt | 10 | |
| Conservation brouillons | 30 jours | |
| Conservation dossiers | 5 ans | |
| Conservation pièces comptables | 10 ans | |
| Conservation journal d'audit | 3 ans | |
| Objectif de disponibilité | 99 % | |
| Proforma obligatoire pour facturer | Oui | |
| Mode de confirmation de paiement | Preuve + validation caisse | |
| Canaux de notification actifs | E-mail + portail | *(SMS/WhatsApp exclus — décision actée)* |
| Catégories de réclamation | *(référentiel ouvert)* | |

---

## PARTIE B — Cartographie des écrans d'administration (vérification J3)

Chaque bloc de la Partie A a bien un écran de saisie **sans code**, sauf les deux ⚠️.

| Paramètre (Partie A) | Écran | Route | État |
|---|---|---|---|
| A1 SLA (durée, seuils, par module/type/étape/priorité) | Paramètres SLA | `/administration/parametres-sla` | ✅ |
| A2 Règles d'escalade | Règles d'escalade | `/administration/regles-escalade` | ✅ |
| A3 Matrice des transitions | Matrice des transitions | `/administration/matrice-transitions` | ✅ |
| A4 Motifs de transfert | Motifs de transfert | `/administration/motifs-transfert` | ✅ |
| A5 Niveaux de priorité | Priorités | `/administration/priorites` | ✅ |
| A5 Attribution automatique de priorité | Règles de priorité automatique | `/administration/regles-priorite-automatique` | ✅ |
| A6 Pièces exigées à l'ouverture de compte | Pièces exigées (compte) | `/administration/pieces-exigees-compte` | ✅ (édition inline) |
| A7 Pièces exigées par étape | Pièces exigées (étape) | `/administration/pieces-exigees-etape` | ✅ |
| A8 Relances absence de réponse | — | — | 🔒 Valeurs **actées par défaut** (3 relances / 2 j). Non administrables (décision 10/10). |
| A9 Heures d'ouverture / fermeture / samedi | Paramètres plateforme (groupe Disponibilité) | `/administration/parametres-plateforme` | ✅ |
| A9 Liste des jours fériés | **Jours fériés (SLA)** | `/administration/jours-feries` | ✅ **livré le 10/10** |
| A10 Volumétrie / conservation / disponibilité / facturation | Paramètres plateforme | `/administration/parametres-plateforme` | ✅ |
| A10 Canaux + modèles de notification | Administration des notifications | `/administration/notifications` | ✅ |
| A10 Catégories de réclamation + référentiels (libellés/ordre) | Référentiels | `/administration/referentiels` | ✅ |
| (bonus) Stratégie de distribution par file (libre / round-robin / charge) | Stratégie de file | `/administration/strategie-file` | ✅ |
| (bonus) Compétences des opérateurs (qui travaille quelle file) | Compétences opérateur | `/administration/competences-operateur` | ✅ |
| (bonus) Périmètres de supervision | Périmètres de supervision | `/administration/perimetres-supervision` | ✅ |
| (bonus) Types de client | Types de client | `/administration/types-client` | ✅ |
| (bonus) Libellés de statut (FR/EN) | Libellés de statut | `/administration/libelles-statut` | ✅ |
| (bonus) Champs du formulaire d'enrôlement | Champs d'enrôlement | `/administration/champs-formulaire-enrolement` | ✅ |
| (bonus) Étapes bloquant l'annulation libre | Étapes bloquantes | `/administration/etapes-bloquantes-annulation` | ✅ |
| Grilles tarifaires proforma (versionnage, import xlsx/CSV) | Module Tarification (hors dossier Administration) | — | ✅ import tableur |

### Les 2 points ⚠️ — tranchés le 10/10

1. **Relances absence de réponse (A8).** ✅ Décision : valeurs par défaut **actées** (3 relances / 2 jours),
   0 développement. À rouvrir seulement si le métier demande explicitement de les régler.
2. **Jours fériés (A9).** ✅ **Livré.** Écran `/administration/jours-feries` (CRUD, soft delete, audité).
   Les jours déclarés actifs sont exclus du décompte des délais SLA (chargés au démarrage, rafraîchis à
   chaque modification). Tout est désormais 100 % administrable sans développement avant l'atelier.

---

## Marche à suivre

1. **Atelier (J2)** : parcourir la Partie A domaine par domaine, remplir la colonne « Valeur validée »,
   trancher les 2 points ⚠️.
2. **Saisie (J3)** : reporter les valeurs via les écrans de la Partie B. Les 2 ⚠️ font l'objet soit d'un
   petit dev, soit d'une décision « valeurs par défaut conservées ».
3. **Preuve** : exporter / capturer le paramétrage saisi et archiver cette fiche remplie (clôture J2+J3).
