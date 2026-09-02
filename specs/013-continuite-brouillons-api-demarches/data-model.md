# DATA MODEL — FEATURE 013

**Feature** : Feature 013 — Continuité Opérationnelle, Brouillons de Saisie, Exposition d'API & Clôture Démarches  
**Date** : 2026-09-01  

---

## 1. NOUVELLES ENTITÉS

### Entité `BrouillonSaisie` (Domaine Socle / Formulaires)

```csharp
namespace CIT.Plateforme.Domain.Formulaires;

public sealed class BrouillonSaisie
{
    public Guid Id { get; private set; }
    public Guid UtilisateurId { get; private set; }
    public Guid SocieteId { get; private set; }
    public string TypeFormulaire { get; private set; } = string.Empty;
    public string DonneesJson { get; private set; } = string.Empty;
    public DateTime DateCreationUtc { get; private set; }
    public DateTime DateDerniereModificationUtc { get; private set; }

    private BrouillonSaisie() { }

    public static BrouillonSaisie Creer(
        Guid id,
        Guid utilisateurId,
        Guid societeId,
        string typeFormulaire,
        string donneesJson,
        DateTime dateCreationUtc)
    {
        return new BrouillonSaisie
        {
            Id = id,
            UtilisateurId = utilisateurId,
            SocieteId = societeId,
            TypeFormulaire = typeFormulaire,
            DonneesJson = donneesJson,
            DateCreationUtc = dateCreationUtc,
            DateDerniereModificationUtc = dateCreationUtc
        };
    }

    public void MettreAJour(string donneesJson, DateTime dateModificationUtc)
    {
        DonneesJson = donneesJson;
        DateDerniereModificationUtc = dateModificationUtc;
    }
}
```

---

## 2. MODÈLE RELATIONNEL & SCHÉMA SQL

### Table `BrouillonsSaisie`

| Colonne | Type SQL | Nullable | Contraintes / Index |
|:---|:---|:---:|:---|
| `Id` | `uniqueidentifier` | NON | PK |
| `UtilisateurId` | `uniqueidentifier` | NON | Index `IX_Brouillons_Utilisateur_Type` (avec `TypeFormulaire`) |
| `SocieteId` | `uniqueidentifier` | NON | Index `IX_Brouillons_Societe` (CPT-04) |
| `TypeFormulaire` | `nvarchar(100)` | NON | Index |
| `DonneesJson` | `nvarchar(max)` | NON | Stockage du payload sérialisé |
| `DateCreationUtc` | `datetime2` | NON | Horodatage création |
| `DateDerniereModificationUtc` | `datetime2` | NON | Horodatage mise à jour |

---

## 3. MIGRATION EF CORE PROJETÉE

- **Nom de la migration** : `AjouterBrouillonsSaisie`
- **Caractère** : **Strictement additive** (aucune modification de table existante, 0 impact sur les données F001→F012).
- **Projet EF** : `src/CIT.Plateforme.Infrastructure` (DbContext `PlateformeDbContext`).
