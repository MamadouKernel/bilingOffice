# RAPPORT DE VALIDATION FORENSIQUE DU PÉRIMÈTRE — FEATURE 017

## Sécurité Granulaire Métier & Protection des Données Personnelles / Rétention Réglementaire

**Date** : 2026-09-02  
**Baseline technique** : 991 / 991 tests verts (100 %) | 0 warning | 0 pending model changes  
**Baseline CDC officielle** : **172 / 188 (91,49 %)**  
**Exigences cibles** : `SEC-02` (Must), `SEC-06` (Must)  
**Gain CDC potentiel** : **+2** $\implies$ **Projection : 174 / 188 (92,55 %)**  

---

## 1. ANALYSE CONTRACTUELLE DES EXIGENCES

### 1.1 SEC-02 (Must) — Contrôle d'Accès par Rôles au Niveau Champ (RBAC IES)
- **Texte CDC source** :
  > *« Contrôle d'accès par rôles (RBAC) appliquant le moindre privilège, y compris au niveau des champs pour les traitements parallèles. »*
- **Points Clés** :
  - Contexte contractuel : Traitements parallèles des dossiers IES (`IMP-40`, `IMP-41`, `REC-SCE-04`).
  - Acteurs concernés : Pilote Manifeste, Pilote Contrôle Documentaire (CD), Agent Facturation, Comptable, Superviseur, Administrateur.
  - Résolution PC-05 : Matrice de permissions par champ (Manifeste vs Douane vs Facturation) appliquée de manière stricte côté serveur (API, Commandes, Requêtes et UI).

### 1.2 SEC-06 (Must) — Protection des Données Personnelles & Rétention ARTCI
- **Texte CDC source** :
  > *« Protection des données personnelles (clients, chauffeurs) conformément à la réglementation ivoirienne en vigueur (ARTCI) : minimisation, durées de conservation, droits des personnes. »*
- **Points Clés** :
  - Cadre réglementaire : Réglementation ivoirienne de l'ARTCI (Loi n° 2013-450 relative à la protection des données à caractère personnel).
  - Résolution PC-14 :
    1. Minimisation des données collectées (chauffeurs, clients, collaborateurs).
    2. Politique de conservation : 5 ans pour dossiers opérationnels, 10 ans pour pièces comptables scellées.
    3. Droits des personnes : Droit d'accès, de rectification et d'anonymisation/purge réglementaire.
    4. Traçabilité des opérations de purge et registre des traitements.

---

## 2. MATRICE RBAC AU NIVEAU CHAMP (`SEC-02` / `PC-05`)

| Contexte / Bloc de Données | Champs Concernés | Pilote Manifeste | Pilote CD | Facturation / Caisse | Superviseur | Admin |
|:---|:---|:---:|:---:|:---:|:---:|:---:|
| **Données Manifeste** | N° BL, Navire, Armateur, Poids brut, Port | **Édition** | Lecture seule | Lecture seule | Édition | Lecture seule |
| **Données Douane & CD** | N° Déclaration, Statut conformité, Pièces CD | Lecture seule | **Édition** | Lecture seule | Édition | Lecture seule |
| **Données Financières** | Montant HT/TTC, Lignes tarifaires, Avoirs | **Masqué** | **Masqué** | **Édition** | Édition | Lecture seule |
| **Données Chauffeur** | Nom chauffeur, N° Permis, N° Téléphone | **Édition** | Lecture seule | Masqué | Édition | Lecture seule |

- **Règle absolue** : Les masquages et interdictions d'édition sont appliqués au niveau des DTOs d'application et de l'API REST (fail-secure), et audités en cas de tentative d'accès non autorisé (`SEC-03` / `SEC-05`).

---

## 3. POLITIQUE DE CONSERVATION & DROITS ARTCI (`SEC-06` / `PC-14`)

| Catégorie de Données | Durée Active | Durée Archivage | Fin de Cycle | Base Légale |
|:---|:---:|:---:|:---:|:---|
| **Dossiers d'import / export clôturés** | 5 ans | - | Anonymisation des données personnelles | Réglementation ARTCI & Douane |
| **Pièces comptables & Factures** | 5 ans | 5 ans (Total 10 ans) | Archivage légal avec scellement SHA-256 | Code de Commerce / OHADA |
| **Journaux d'audit (`SEC-03`/`SEC-05`)** | 3 ans | - | Conservation inaltérable | Norme ISO 27001 & ARTCI |
| **Brouillons abandonnés (`ERR-01`)** | 30 jours | - | Purge automatique | Minimisation ARTCI |

---

## 4. IMPACTS SUR LE MODÈLE DE DONNÉES & MIGRATIONS

- **Migration EF Core probable** : **1 migration requise** pour porter les métadonnées de conservation légale et de statut d'anonymisation (ex: `DateCloture`, `DateArchivage`, `EstAnonymise`, `EmpreinteScellementSha256`).
- **Entités impactées** : `Ticket`, `Document`, `DemandeCompte`.
- **Nouvelles entités Domain** : `RegistreTraitementDonnees`, `CertificatPurgeAudit`.

---

## 5. SCORING DU BUNDLE & CONCLUSION

- **Score de Cohérence & Maturité** : **95 / 100**
  - Valeur métier & sécurité : 20/20
  - Alignement Must CDC : 20/20
  - Gain CDC net : +2 points (15/15)
  - Autonomie d'exécution : 15/15 (0 dépendance externe bloquante)
  - Risques maîtrisés : 10/10
  - Clarté des décisions : 15/15

**VERDICT** : **READY FOR /speckit.specify** pour la Feature 017.
