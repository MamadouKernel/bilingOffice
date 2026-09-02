# MODÈLE DE DONNÉES & IMPACTS DE SCHÉMA — FEATURE 015

## Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement

---

### 1. SYNTHÈSE DES IMPACTS DE MODÉLISATION

- **Nouvelles entités Domaine** : **0**
- **Nouvelles tables SQL** : **0**
- **Nouveaux champs modélisés en base** : **0**
- **Migrations Entity Framework Core générées** : **0**
- **Pending Model Changes** : **0**

---

### 2. AUDIT DU MODÈLE EXISTANT ET RÉUTILISATION

L'entité `CIT.Plateforme.Domain.Documents.Document` porte déjà nativement les métadonnées nécessaires :

```csharp
public sealed class Document
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid DeposeParId { get; private set; }
    public string NomFichier { get; private set; }
    public string TypeContenu { get; private set; }
    public long TailleOctets { get; private set; }
    public string CleStockage { get; private set; }
    public StatutAnalyseAntivirus StatutAntivirus { get; private set; }
    public DateTime DateDepotUtc { get; private set; }

    public bool EstAccessible => StatutAntivirus == StatutAnalyseAntivirus.Sain;
}
```

Les invariants de sécurité (`SEC-04`, `SEC-07`, `SEC-09`) sont assurés par les adaptateurs et middlewares sans modifier le schéma relationnel de production.
