# SPÉCIFICATION FONCTIONNELLE & CONTRACTUELLE CLARIFIÉE — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### 1. RÉFÉRENTIEL CONTRACTUEL & SOURCES DE VÉRITÉ

- **`SEC-02`** (Must) :
  > *« Contrôle d'accès par rôles (RBAC) appliquant le moindre privilège, y compris au niveau des champs pour les traitements parallèles. »*
  - *Source* : CDC § 17.1, PC-05 (docs/30-dsi-pc-clarification-gate.md).
- **`SEC-06`** (Must) :
  > *« Protection des données personnelles (clients, chauffeurs) conformément à la réglementation ivoirienne en vigueur (ARTCI) : minimisation, durées de conservation, droits des personnes. »*
  - *Source* : CDC § 17.1, PC-14 (Loi n° 2013-450 / ARTCI, Code de Commerce & OHADA).

---

### 2. DÉCISIONS DE CLARIFICATION FORMELLES (DEC-017-01 À DEC-017-08)

#### DEC-017-01 : Matrice de Contrôle RBAC par Champ (Contexte IES)
Les règles de contrôle d'accès au niveau champ s'appliquent lors du traitement parallèle des dossiers IES (`IMP-40`, `IMP-41`, `REC-SCE-04`) :

| Bloc de Données | Champs Nominatifs | Pilote Manifeste | Pilote CD | Facturation / Caisse | Superviseur | Admin |
|:---|:---|:---:|:---:|:---:|:---:|:---:|
| **Données Manifeste** | N° BL, Navire, Armateur, Poids brut, Port | **Édition** | Lecture seule | Lecture seule | Édition | Lecture seule |
| **Données Douane & CD** | N° Déclaration, Statut conformité, Pièces CD | Lecture seule | **Édition** | Lecture seule | Édition | Lecture seule |
| **Données Financières** | Montant HT/TTC, Lignes tarifaires, Avoirs | **Masqué** | **Masqué** | **Édition** | Édition | Lecture seule |
| **Données Chauffeur** | Nom chauffeur, N° Permis, N° Téléphone | **Édition** | Lecture seule | Masqué | Édition | Lecture seule |

#### DEC-017-02 : Comportement du Masquage Côté Serveur, API & Export
- **UI Blazor** : Les champs non autorisés sont masqués (`[Confidentiel / Accès restreint]` ou désactivés selon la matrice).
- **API REST (NFR-08)** : Les champs masqués sont retournés avec valeur `null` ou omis dans les DTOs projetés ; toute tentative de mutation (`POST`/`PUT`) sur un champ non autorisé renvoie immédiatement `403 Forbidden`.
- **Exports CSV / Excel** : Les colonnes financières sont masquées (`[RESTREINT]`) pour les profils opérationnels non habilités.
- **Fail-Secure** : Le contrôle est opéré au niveau des commandes et des validateurs d'application (`CIT.Plateforme.Application`).

#### DEC-017-03 : Dates de Référence pour les Délais de Conservation Légale
- **Pièces comptables & Factures** : Date d'émission de la facture (`DateEmissionUtc`) $\implies$ **10 ans** (Archivage scellé avec calcul SHA-256).
- **Dossiers opérationnels clôturés** : Date de clôture définitive (`DateClotureUtc`) $\implies$ **5 ans**.
- **Journaux d'audit de sécurité (`SEC-05`)** : Date de génération de l'événement (`DateCreationUtc`) $\implies$ **3 ans** (Inaltérable).
- **Brouillons abandonnés (`ERR-01`)** : Date de dernière modification (`DerniereModificationUtc`) $\implies$ **30 jours**.

#### DEC-017-04 : Actions à l'Échéance du Délai
- **Dossiers $> 5$ ans** : Anonymisation irréversible des données à caractère personnel (nom, prénom, email, téléphone, n° de pièce d'identité remplacés par `ANONYMISE`).
- **Brouillons $> 30$ jours** : Purge physique et logique définitive.
- **Factures décennales** : Conservation scellée en lecture seule d'archive.

#### DEC-017-05 : Mécanisme de Gel Légal (*Legal Hold*)
- Le Superviseur ou l'Administrateur peut activer un drapeau `EstSousGelLegal = true` sur un ticket ou dossier en litige, suspendant temporairement toute opération automatique de purge ou d'anonymisation.

#### DEC-017-06 : Idempotence & Certificat d'Audit de Purge
- L'exécution de la purge est strictement idempotente.
- Chaque exécution génère un événement d'audit (`SEC-05`) enregistrant le lot, la catégorie et le nombre d'éléments traités, sans ré-exposer les données personnelles supprimées.

#### DEC-017-07 : Traçabilité des Refus d'Accès Champ
- Toute tentative d'altération ou de forçage de champ restreint est consignée dans le journal d'audit de sécurité avec l'identifiant de l'acteur et le champ ciblé.

#### DEC-017-08 : Préservation de l'Intégrité des Brouillons Actifs (`ERR-01`) & GED (`F015`)
- Les brouillons actifs récents ($< 30$ jours) ne sont jamais altérés par le moteur de rétention.
- Les documents GED stockés hermétiquement bénéficient de la même politique de rétention avec suppression physique sécurisée lors de la purge.

---

### 3. USER STORIES MÉTIER

- **US1 (SEC-02 - Traitement Parallèle IES)** : En tant que Pilote Manifeste, je peux éditer les informations du manifeste sans pouvoir consulter ni altérer les données financières masquées.
- **US2 (SEC-02 - Intégrité API & Exports)** : En tant qu'utilisateur interrogeant l'API REST ou téléchargeant un export, les champs masqués ne sont jamais divulgués.
- **US3 (SEC-06 - Rétention Réglementaire & Purge)** : En tant que DSI/Conformité, le système identifie automatiquement les données expirées et exécute leur purge/anonymisation.
- **US4 (SEC-06 - Scellement Comptable Décennal)** : En tant que Comptable/Auditeur, les factures et pièces financières sont conservées scellées pendant 10 ans.

---

### 4. CONTRATS FONCTIONNELS & TEST-SPEC

- **Contrats C17** :
  - `C17-01` : Évaluation d'autorisation au niveau champ (`AutorisationChampService`).
  - `C17-02` : Projection et masquage contextuel des DTOs et exports.
  - `C17-03` : Évaluation des règles d'éligibilité à la rétention/purge.
  - `C17-04` : Exécution de purge / anonymisation idempotente.
  - `C17-05` : Journalisation d'audit de lot et gel légal (*Legal Hold*).
- **TEST-SPEC** :
  - `SEC-CERT-02` : Validation exhaustive de la matrice RBAC par champ.
  - `SEC-CERT-06` : Validation du cycle de vie ARTCI (30j, 3 ans, 5 ans, 10 ans, gel légal et idempotence).
