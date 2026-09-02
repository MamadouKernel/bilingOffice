# GUIDE DE DÉMARRAGE RAPIDE — FEATURE 016

## Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

---

### 1. VÉRIFICATION DES HEALTH CHECKS

Pour tester localement ou en recette les sondes de santé de l'application :

```bash
# Sonde de vivacité (Liveness)
curl -i http://localhost:5000/healthz

# Sonde de disponibilité des dépendances (Readiness)
curl -i http://localhost:5000/ready
```

---

### 2. EXÉCUTION DES BENCHMARKS DE PERFORMANCE (< 3s)

Pour rejouer la suite de benchmarks automatisés de temps de réponse :

```bash
dotnet test --filter FullyQualifiedName~NfrCert04PerformanceTests
```

---

### 3. EXÉCUTION DES TESTS DE SUPERVISION ET DISPONIBILITÉ

```bash
dotnet test --filter FullyQualifiedName~Supervision
```
