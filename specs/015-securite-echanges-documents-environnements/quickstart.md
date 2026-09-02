# GUIDE DE DÉMARRAGE RAPIDE & VÉRIFICATION — FEATURE 015

## Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement

---

### 1. COMPILATION DU PROJET
```powershell
dotnet build /p:UseSharedCompilation=false
```

---

### 2. EXÉCUTION DE LA SUITE DE TESTS COMPLÈTE
```powershell
dotnet test -- NUnit.NumberOfTestWorkers=1 xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=1
```

---

### 3. VÉRIFICATION DE NON-MODIFICATION DU MODÈLE RELATIONNEL
```powershell
dotnet ef migrations has-pending-model-changes --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

---

### 4. VÉRIFICATION DES SCÉNARIOS SPÉCIFIQUES SEC-04, SEC-07, SEC-09
```powershell
dotnet test --filter "FullyQualifiedName~SecCert04TransportTests|FullyQualifiedName~ControleFichiersTests|FullyQualifiedName~ScannerAntivirusTests|FullyQualifiedName~SecCert09EnvironnementTests"
```
