# RAPPORT DE VALIDATION FORENSIQUE DU PÉRIMÈTRE — FEATURE 016

**Date** : 2026-09-02  
**Baseline CDC officielle** : **172 / 188 (91,49 %)**  
**Statut précédent** : Feature 015 CLOSED (Commit `12aad9c`)  

---

## 1. RECONSTRUCTION FORENSIQUE DES EXIGENCES NFR DU CDC

L'analyse croisée du CDC source, de `docs/00-analyse-cdc.md`, `docs/01-matrice-exigences.md` et `docs/02-points-a-clarifier.md` permet de rétablir le texte et les seuils contractuels exacts :

| ID Exigence | Priorité CDC | Texte Contractuel Exact | Seuil CDC Réel | Type de Preuve & Nature | Statut Sélection F016 |
|:---|:---:|:---|:---:|:---|:---:|
| **`NFR-03`** | **Must** | *Disponibilité cible en heures ouvrées ≥ 99 % ; maintenance planifiée hors heures de pointe* | **≥ 99 %** *(et non 99.9%)* | Sondes Health Checks (`/healthz`, `/ready`), politique de maintenance planifiée | **ÉLIGIBLE F016** |
| **`NFR-04`** | **Must** | *Temps de réponse < 3 secondes pour les écrans courants dans des conditions normales de charge* | **< 3 s** *(et non < 2s)* | Benchmarks automatisés, middleware de métrologie de latence, budget de performance | **ÉLIGIBLE F016** |
| **`NFR-10`** | **Must** | *La plateforme est accessible 24h/24 et 7j/7, hors périodes de maintenance planifiées* | **24/7 hors maintenance** | Résilience de service, gestion des fenêtres de maintenance et persistance continue | **ÉLIGIBLE F016** |
| **`NFR-05`** | **Must** | *Volumétrie dimensionnante à confirmer en phase de cadrage : tickets/jour, taille des pièces jointes, utilisateurs simultanés* | Cadrage volumétrie | Bloqué par atelier de cadrage `PC-01` | DIFFÉRÉ (F018) |
| **`NFR-06`** | **Must** | *Sauvegardes régulières (données et documents) et procédure de restauration testée* | Sauvegardes & Restauration | Dépendance infrastructure et scripts d'exploitation | DIFFÉRÉ (F018) |
| **`NFR-09`** | **Should** | *Hébergement conforme aux standards du groupe ; le choix (infrastructure CIT/AGL ou cloud) sera arrêté selon le scénario de réalisation retenu* | Standard Groupe AGL | Bloqué par décision DSI `PC-13` *(et non capacité 500 users)* | DIFFÉRÉ (F018) |

---

## 2. RECTIFICATION DU RAPPORT DE SÉLECTION (docs/27)

1. **Correction sur `NFR-09`** : `NFR-09` concerne formellement le *choix d'hébergement du Groupe AGL (PC-13)* et non la capacité de 500 utilisateurs (qui relève de `NFR-05` / `PC-01`).
2. **Correction sur le seuil `NFR-03`** : La cible de disponibilité en heures ouvrées est **≥ 99 %** (et non 99.9%).
3. **Correction sur le seuil `NFR-04`** : Le seuil de temps de réponse contractuel est **< 3 secondes** (et non < 2 secondes).
4. **Intégration de `NFR-10`** : `NFR-10` (*Disponibilité portail client 24h/24 et 7j/7 hors maintenance*) est parfaitement alignée avec `NFR-03` et `NFR-04` pour former un bloc d'architecture de disponibilité et performance applicative autonome.

---

## 3. ÉVALUATION ET SCORING DU CANDIDAT RETENU

### Feature 016 — Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

- **Exigences ciblées** :
  - **`NFR-03` (Must)** : Disponibilité ≥ 99 % en heures ouvrées, sondes Health Checks (`/healthz`, `/ready`), indicateurs de disponibilité.
  - **`NFR-04` (Must)** : Temps de réponse < 3s sous charge normale, métrologie de latence par middleware, monitoring des temps d'exécution.
  - **`NFR-10` (Must)** : Accessibilité 24/7 du portail client avec gestion des plages de maintenance planifiées.
- **Score d'éligibilité** : **96 / 100**
- **Gain maximal certifiable** : **+3 exigences Must**
- **Baseline CDC projetée post-F016** : **172 + 3 = 175 / 188 (93,09 %)**
- **Dépendances externes** : 0 (100 % autonome dans le code .NET/Blazor).
- **Migration EF Core** : 0 migration requise.

---

## 4. INVENTAIRE DES 13 EXIGENCES RESTANTES APRÈS F016

| ID | Priorité | Description CDC | Blocage / Dépendance |
|:---|:---:|:---|:---|
| **`IA-01` à `IA-07`** (7 ex.) | 3 Should, 4 Could | Services d'IA, Assistant conversationnel, OCR et Vision | `PC-18` (Atelier DSI sur modèle LLM) |
| **`SEC-02`** | Must | Contrôle d'accès au niveau des champs pour la branche IES | `PC-05` (Matrice des champs IES) |
| **`SEC-06`** | Must | Rétention et archivage légal selon réglementation ARTCI | `PC-14` (Durées légales ARTCI) |
| **`ENR-04`** | Should | Notification SMS / WhatsApp | `PC-16` (Choix opérateur télécom) |
| **`NFR-05`** | Must | Cadrage de la volumétrie dimensionnante | `PC-01` (Atelier dimensionnement) |
| **`NFR-06`** | Must | Sauvegardes régulières et procédure de restauration | Procédure d'exploitation des backups |
| **`NFR-09`** | Should | Hébergement conforme aux standards Groupe AGL | `PC-13` (Décision d'hébergement DSI) |

---

## 5. DÉCISION FINALE DU SCOPE GATE

**VERDICT : READY FOR /speckit.specify**
