# RAPPORT DE BENCHMARK DE PERFORMANCE K6 (NFR-04 - L3)

**Date** : 2026-09-02  
**Statut** : **NOT EXECUTED** (Script k6 `evidence/scripts/benchmark-k6.js` prêt, en attente d'URL de recette déployée)  
**Exigence** : `NFR-04` (Must) — Temps de réponse $< 3\text{ s}$ sur écrans courants  

---

### 1. PARAMÈTRES DU BENCHMARK

- **Outil** : k6 (Script JavaScript : `specs/016-disponibilite-performance-resilience/evidence/scripts/benchmark-k6.js`)
- **Seuil contractuel CDC** : $< 3\text{ s}$ (3 000 ms)
- **Surfaces ciblées** :
  1. `/comptes/connexion`
  2. `/healthz`
  3. `/ready`
  4. `/client/dashboard`
  5. `/client/demandes`
  6. `/api/v1/tickets`

---

### 2. RÉSULTAT DU BENCHMARK L3

- **Statut d'exécution** : **NOT EXECUTED**
- **Temps de réponse p95** : En attente d'exécution contre URL déployée
- **Verdict CDC NFR-04** : **NOT PROVEN** (Gain CDC conditionnel = +0)
