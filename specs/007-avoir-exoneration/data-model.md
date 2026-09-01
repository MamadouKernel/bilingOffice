# Modèle de Données & Persistance — Feature 007
## Module Avoir & Exonération

> **Document** : `specs/007-avoir-exoneration/data-model.md`  
> **Date** : 1er septembre 2026  
> **Source de référence** : `research.md`, `.specify/memory/constitution.md`

---

## 1. Vue d'Ensemble & Stratégie Additive

Pour respecter le principe de frugalité (Principe IV - YAGNI) et de réutilisation maximale du socle (Principe V), **aucune nouvelle table SQL n'est introduite**. Le module Avoir & Exonération s'appuie sur la table centrale `Tickets` enrichie de colonnes optionnelles dédiées au traitement décisionnel des avoirs et exonérations.

```
┌────────────────────────────────────────────────────────────────────────┐
│                               Dossier                                  │
│ ────────────────────────────────────────────────────────────────────── │
│  Id (PK) : Guid                                                        │
│  Numero : string (ex: "DOS-2026-00042")                                │
│  ClientId : Guid (SocieteId propriétaire)                              │
│  AuteurId : Guid? (Utilisateur créateur)                               │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ 1
                                    │
                                    │ *
┌───────────────────────────────────▼────────────────────────────────────┐
│                                Ticket                                  │
│ ────────────────────────────────────────────────────────────────────── │
│  Id (PK) : Guid                                                        │
│  Numero : string (ex: "AVO-2026-00001", "EXO-2026-00001")             │
│  Module : string = "AvoirExoneration"                                  │
│  TypeDemande : string = "Avoir" | "Exoneration"                        │
│  DossierId (FK) : Guid                                                 │
│  FileCouranteId (FK) : Guid                                            │
│  Statut : StatutTicket (Cree, EnCours, EnAttenteClient, Valide, Rejete)│
│  Objet : string? (max 200)                                             │
│  Description : string? (max 2000)                                      │
│                                                                        │
│  [EXTENSIONS ADDITIVES F007]                                           │
│  DecisionAvoirExo : DecisionAvoirExo? (Accorde, Rejete)                │
│  MotifRejetAvoirExo : string? (max 1000, obligatoire si Rejet)         │
│  MontantConteste : decimal? (18,2)                                     │
│  MontantAccorde : decimal? (18,2)                                      │
│  DateDecisionAvoirExoUtc : DateTime?                                   │
│  DecideurAvoirExoId : Guid?                                            │
│                                                                        │
│  VersionConcurrence : byte[] (RowVersion)                              │
└──────┬────────────────────────────┬─────────────────────────────┬──────┘
       │ 1                          │ 1                           │ 1
       │                            │                             │
       │ *                          │ *                           │ *
┌──────▼──────┐              ┌──────▼──────┐               ┌──────▼──────┐
│  Documents  │              │   Messages  │               │LiaisonTicket│
└─────────────┘              └─────────────┘               └─────────────┘
```

---

## 2. Énumérations & Types Métier

### 2.1 Enum `DecisionAvoirExo`
Représente le résultat formel de l'instruction d'une demande d'avoir ou d'exonération :
```csharp
namespace CIT.Plateforme.Domain.AvoirExoneration;

public enum DecisionAvoirExo
{
    Accorde = 1,
    Rejete = 2
}
```

---

## 3. Extension de l'Entité `Ticket`

### 3.1 Propriétés Additives
```csharp
// Extensions Feature 007 — Module Avoir & Exonération
public DecisionAvoirExo? DecisionAvoirExo { get; private set; }
public string? MotifRejetAvoirExo { get; private set; }
public decimal? MontantConteste { get; private set; }
public decimal? MontantAccorde { get; private set; }
public DateTime? DateDecisionAvoirExoUtc { get; private set; }
public Guid? DecideurAvoirExoId { get; private set; }
```

### 3.2 Méthodes Métier de Domaine

#### Création d'une Demande Avoir / Exonération
```csharp
public static Ticket CreerDemandeAvoirExo(
    string numero,
    string typeDemande, // "Avoir" ou "Exoneration"
    Guid fileCibleId,
    Guid dossierId,
    string objet,
    string description,
    decimal? montantConteste,
    DateTime maintenantUtc)
{
    if (string.IsNullOrWhiteSpace(numero))
        throw new ArgumentException("Le numéro de ticket est obligatoire.", nameof(numero));
    if (typeDemande is not ("Avoir" or "Exoneration"))
        throw new ArgumentException("Le type de demande doit être 'Avoir' ou 'Exoneration'.", nameof(typeDemande));
    if (string.IsNullOrWhiteSpace(objet))
        throw new ArgumentException("L'objet de la demande est obligatoire.", nameof(objet));
    if (string.IsNullOrWhiteSpace(description))
        throw new ArgumentException("La description détaillée est obligatoire.", nameof(description));
    if (montantConteste.HasValue && montantConteste.Value < 0)
        throw new ArgumentException("Le montant contesté ne peut pas être négatif.", nameof(montantConteste));

    return new Ticket
    {
        Id = Guid.NewGuid(),
        Numero = numero,
        Module = "AvoirExoneration",
        TypeDemande = typeDemande,
        DossierId = dossierId,
        FileCouranteId = fileCibleId,
        Statut = StatutTicket.Cree,
        Objet = objet,
        Description = description,
        MontantConteste = montantConteste,
        DateCreationUtc = maintenantUtc,
        DateArriveeEnFileUtc = maintenantUtc,
        DateDerniereActiviteUtc = maintenantUtc
    };
}
```

#### Enregistrement de la Décision Finale
```csharp
public void EnregistrerDecisionAvoirExo(
    DecisionAvoirExo decision,
    string? motifRejet,
    decimal? montantAccorde,
    Guid decideurId,
    DateTime maintenantUtc)
{
    if (Statut is StatutTicket.Cloture or StatutTicket.Annule)
        throw new InvalidOperationException($"Impossible d'enregistrer une décision sur un ticket au statut {Statut}.");

    if (decision == DecisionAvoirExo.Rejete)
    {
        if (string.IsNullOrWhiteSpace(motifRejet))
            throw new ArgumentException("Le motif de rejet est obligatoire en cas de refus (AVE-03).", nameof(motifRejet));

        DecisionAvoirExo = DecisionAvoirExo.Rejete;
        MotifRejetAvoirExo = motifRejet.Trim();
        MontantAccorde = 0;
        Statut = StatutTicket.Rejete;
    }
    else if (decision == DecisionAvoirExo.Accorde)
    {
        DecisionAvoirExo = DecisionAvoirExo.Accorde;
        MotifRejetAvoirExo = null;
        MontantAccorde = montantAccorde;
        Statut = StatutTicket.Valide;
    }

    DecideurAvoirExoId = decideurId;
    DateDecisionAvoirExoUtc = maintenantUtc;
    DateDerniereActiviteUtc = maintenantUtc;
    DateClotureUtc = maintenantUtc;
}
```

---

## 4. Configuration Entity Framework Core (`PlateformeDbContext`)

Dans `PlateformeDbContext.OnModelCreating` :
```csharp
builder.Entity<Ticket>(b =>
{
    // Propriétés additives Feature 007
    b.Property(t => t.DecisionAvoirExo)
        .HasConversion<string>()
        .HasMaxLength(32);

    b.Property(t => t.MotifRejetAvoirExo)
        .HasMaxLength(1000);

    b.Property(t => t.MontantConteste)
        .HasPrecision(18, 2);

    b.Property(t => t.MontantAccorde)
        .HasPrecision(18, 2);

    b.Property(t => t.DateDecisionAvoirExoUtc);

    b.Property(t => t.DecideurAvoirExoId);

    // Index optimisé pour la recherche et le filtrage des demandes Avoir & Exonération
    b.HasIndex(t => new { t.Module, t.TypeDemande, t.Statut })
        .HasDatabaseName("IX_Tickets_Module_TypeDemande_Statut");
});
```

---

## 5. Migration EF Core Prévue

Nom de la migration : `AjoutModuleAvoirExoneration`

Opérations SQL générées dans `Up()` :
- `ALTER TABLE [Tickets] ADD [DecisionAvoirExo] nvarchar(32) NULL;`
- `ALTER TABLE [Tickets] ADD [MotifRejetAvoirExo] nvarchar(1000) NULL;`
- `ALTER TABLE [Tickets] ADD [MontantConteste] decimal(18,2) NULL;`
- `ALTER TABLE [Tickets] ADD [MontantAccorde] decimal(18,2) NULL;`
- `ALTER TABLE [Tickets] ADD [DateDecisionAvoirExoUtc] datetime2 NULL;`
- `ALTER TABLE [Tickets] ADD [DecideurAvoirExoId] uniqueidentifier NULL;`
- `CREATE INDEX [IX_Tickets_Module_TypeDemande_Statut] ON [Tickets] ([Module], [TypeDemande], [Statut]);`

**Impact : 100% additif, 0 suppression de colonne, 0 rupture de compatibilité.**
