# MODÈLE DE DONNÉES — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### 1. EXTENSIONS DU MODÈLE EXISTANT

#### 1.1 Entité `Ticket` (`CIT.Plateforme.Domain.Tickets`)
- **Nouvelles propriétés** :
  - `public bool EstSousGelLegal { get; set; } = false;`
  - `public DateTime? DateArchivageUtc { get; set; }`
  - `public bool EstAnonymise { get; set; } = false;`

#### 1.2 Entité `Document` (`CIT.Plateforme.Domain.Tickets`)
- **Nouvelles propriétés** :
  - `public bool EstScelle { get; set; } = false;`
  - `public DateTime? DateScellementUtc { get; set; }`
  - `public string? EmpreinteScellementSha256 { get; set; }`

---

### 2. NOUVELLE ENTITÉ DE TRAÇABILITÉ DE LOT (`LotPurgeRetention`)

```csharp
namespace CIT.Plateforme.Domain.Supervision;

public class LotPurgeRetention
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime DateExecutionUtc { get; set; }
    public string Categorie { get; set; } = string.Empty; // "Brouillons", "Dossiers", "Comptable", "Audit"
    public int NombreElementsTraites { get; set; }
    public bool EstSucces { get; set; } = true;
    public string? MessageErreur { get; set; }
}
```

---

### 3. IMPACT SUR LA MIGRATION EF CORE

- **Migration EF Core** : **1 migration additive** (`AjoutRetentionEtGelLegal`).
- **Tables modifiées** : `Tickets`, `Documents`.
- **Nouvelle table** : `LotsPurgeRetention`.
- **Opérations destructives** : **0 DROP TABLE, 0 DROP COLUMN, 0 RENOMMAGE**.
