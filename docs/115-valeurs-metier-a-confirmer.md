# Valeurs métier à confirmer (placeholders) — 05/10/2026

> Sincérité (voir `docs/113`) : plusieurs référentiels tournent aujourd'hui sur des **valeurs par
> défaut raisonnables**, pas sur des valeurs **arrêtées par le métier**. Le CDC lui-même les liste
> comme « à valider » (§25). Tout est **administrable sans développement** : il s'agit de décider,
> pas de coder. Ce document liste ce qui est en base et ce qui reste à trancher.

## 1. SLA (seuils et durées cibles) — §9.6, SLA-05, §25

- **État : aucune valeur SLA amorcée.** Les écrans (`Paramètres SLA`) et le moteur existent, mais
  aucune durée cible ni seuil n'est défini par défaut.
- **À décider avec le métier** : par module / type de demande / étape / file / priorité —
  durée cible, seuil d'information (ex. 50 %), seuil d'alerte (ex. 80 %), dépassement (100 %), escalade.
- Tant qu'aucun SLA n'est saisi, aucune alerte de délai ni escalade SLA ne se déclenche.

## 2. Matrice des transitions autorisées — §9.4, TRF-06, §25

Valeurs **par défaut amorcées** (file source → file cible, motifs autorisés) :

| Module | De | Vers | Motifs |
|---|---|---|---|
| Import | Pilote Manifeste | Facturation | Anomalie documentaire, Erreur d'orientation |
| Import | Facturation | Contrôle Documentaire | Prestation complémentaire, Facture à corriger, Erreur d'orientation |
| Import | Facturation | Validation Paiement | Facture à corriger, Erreur d'orientation |
| Export | Back Office Export | Facturation | Anomalie documentaire, Erreur d'orientation |
| Export | Facturation | Contrôle Documentaire | Prestation complémentaire, Facture à corriger, Erreur d'orientation |
| Export | Facturation | Validation Paiement | Facture à corriger, Erreur d'orientation |

**À confirmer / compléter** : le CDC (§9.4) donne d'autres transitions à titre d'illustration
(Contrôle BL ↔ Facturation, Contrôle documentaire → Facturation, Comptabilité → …). Arrêter la
liste définitive avec les responsables métier.

## 3. Motifs de transfert — §9.10

Amorcés : **Anomalie documentaire, Erreur d'orientation, Prestation complémentaire, Facture à corriger**.
À confirmer/compléter avec le métier.

## 4. Priorités — PRI-01

Amorcées : **Standard (10), Urgent (20), Critique (30)**. Libellés/rangs confirmables ;
**aucune règle d'attribution automatique** n'est définie par défaut (écran dédié).

## 5. Pièces exigées à l'ouverture de compte — CPT-01, §25

Amorcées : **RCCM, DFE, CNI du représentant** (obligatoires par défaut), **RIB** (facultatif),
**Agrément de transport** (facultatif, profil Transporteur). À valider : liste exacte et caractère
obligatoire par profil. *(Sur les sociétés déjà en base, exécuter `deploy/sql/pieces-legales-obligatoires.sql`.)*

## 6. Pièces exigées par étape de workflow — SOC-07, §25

À valider : la liste exacte des types de pièces exigées à chaque étape de chaque parcours
(déclaration douane, copie BL, preuve de paiement, etc.), ainsi que formats et taille max.

## 7. Autres paramètres à arrêter (§25)

- **Relances « absence de réponse »** (CYC-04) : nombre et cadence des relances avant suggestion de clôture.
- **Horaires de fonctionnement** utilisés dans le calcul des SLA (SLA-04).
- **Étapes bloquant l'annulation libre** (CYC-01) par workflow.
- **Durée de validité des badges** et **délai de rappel avant expiration** (enrôlement).
- **Politique de conservation / archivage** (durées légales).
- **Canaux de notification actifs** au lancement : e-mail + portail (SMS/WhatsApp exclus par décision DSI).

## Comment procéder

1. Atelier métier par domaine (Billing, Gate, Finance, Support) pour fixer les valeurs.
2. Saisie dans les écrans d'administration (aucun développement requis).
3. Consignation des valeurs retenues dans ce document (colonne « valeur validée » à ajouter).
