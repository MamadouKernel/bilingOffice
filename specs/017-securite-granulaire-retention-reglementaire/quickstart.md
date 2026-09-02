# GUIDE DE DÉMARRAGE RAPIDE — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### 1. EXÉCUTION DES TESTS UNITAIRES ET D'INTÉGRATION

```powershell
# Exécuter les tests du moteur RBAC par champ (SEC-02)
dotnet test --filter "FullyQualifiedName~AutorisationChamp"

# Exécuter les tests du moteur de rétention et purge (SEC-06)
dotnet test --filter "FullyQualifiedName~MoteurRetention"

# Exécuter l'ensemble de la suite
dotnet test -- NUnit.NumberOfTestWorkers=1 xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=1
```

---

### 2. VÉRIFICATION DU MODÈLE ET DES MIGRATIONS

```powershell
# Vérifier l'absence de modifications pendantes
dotnet ef migrations has-pending-model-changes --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```
