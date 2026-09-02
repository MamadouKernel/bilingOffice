# MODÈLE DE DONNÉES & SCHÉMA — FEATURE 016

## Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

---

### 1. ABSENCE D'IMPACT SUR LE MODÈLE RELATIONNEL

La Feature 016 ne nécessite aucune persistance en base de données relationnelle.

- **Nombre de migrations créées** : **0**
- **Nombre de nouvelles tables** : **0**
- **Nombre de nouvelles entités de domaine** : **0**
- **Nombre de modifications de colonnes** : **0**

---

### 2. MODÈLES EN MÉMOIRE & STRUCTURES DTO

#### 2.1 Modèles de Diagnostic & Santé (`CIT.Plateforme.Application.Supervision`)

```csharp
public record RapportSanteComposant(
    string Nom,
    string Statut,
    string? Description,
    TimeSpan Duree);

public record RapportSanteGlobal(
    string StatutGlobal,
    DateTime HorodatageUtc,
    IReadOnlyList<RapportSanteComposant> Composants);
```

#### 2.2 Modèle de Calcul de Disponibilité

```csharp
public record PeriodeObservation(
    DateTime Debut,
    DateTime Fin);

public record PlageMaintenance(
    DateTime Debut,
    DateTime Fin,
    string Motif);

public record ResultatCalculDisponibilite(
    TimeSpan TempsOuvreTotal,
    TimeSpan TempsMaintenancePlanifiee,
    TimeSpan TempsEligible,
    TimeSpan TempsIndisponible,
    TimeSpan TempsDisponible,
    double PourcentageDisponibilite,
    bool EstConformeSla);
```
