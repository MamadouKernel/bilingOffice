# Modèle de Données & Schéma — Feature 004 : Billing Office EXPORT

> **Dossier** : `specs/004-billing-office-export/`  
> **Date** : 30 Août 2026  
> **Statut** : Validé pour Planification (`/speckit.plan`)  
> **Sources de vérité** : CIT-CIV-DSI-RFP-XXXX v1.0, `.specify/memory/constitution.md`, `specs/004-billing-office-export/research.md`

---

## 1. Extensions du Modèle de Domaine

### 1.1. Extension de l'Agrégat `Ticket` (`CIT.Plateforme.Domain.Tickets`)
Ajout de la propriété additive `NumeroBooking` pour porter le numéro de booking métier (spécifique au module `BillingExport`).

```csharp
public sealed class Ticket
{
    // ... Propriétés existantes inchangées (Id, Numero, Module, TypeDemande, DossierId, FileCouranteId, Statut...) ...

    /// <summary>
    /// EXTENSION FEATURE 004 (research.md D1) : numéro de booking métier fourni par le client.
    /// Nullable, uniquement renseigné pour les tickets du module Billing EXPORT.
    /// Portée d'unicité globale garantie par un index SQL Server filtré dédié (D2).
    /// </summary>
    public string? NumeroBooking { get; private set; }

    /// <summary>
    /// Usine de création adaptée de manière rétrocompatible (paramètre optionnel numeroBooking).
    /// </summary>
    public static Ticket Creer(
        string numero,
        string module,
        string typeDemande,
        Guid fileCibleId,
        Guid dossierId,
        DateTime maintenantUtc,
        string? numeroBl = null,
        string? numeroBooking = null)
    {
        // ...
        return new Ticket
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            Module = module,
            TypeDemande = typeDemande,
            FileCouranteId = fileCibleId,
            DossierId = dossierId,
            Statut = StatutTicket.EnAttenteDePriseEnCharge,
            DateCreationUtc = maintenantUtc,
            DateArriveeEnFileUtc = maintenantUtc,
            DateDerniereActiviteUtc = maintenantUtc,
            NumeroBl = numeroBl,
            NumeroBooking = numeroBooking
        };
    }
}
```

### 1.2. Extension de l'Énumération `BrancheIes` (`CIT.Plateforme.Domain.Tickets`)
Ajout de la valeur `BackOffice` pour le traitement parallèle IES Export (`EXP-04` / `D9`).

```csharp
public enum BrancheIes
{
    Manifeste = 0,              // Branche Import
    ControleDocumentaire = 1,   // Branche Import & Export
    BackOffice = 2              // Branche Export (FEATURE 004)
}
```

---

## 2. Configuration Entity Framework Core (`PlateformeDbContext`)

Dans `CIT.Plateforme.Infrastructure.Persistence.PlateformeDbContext.cs` :

```csharp
builder.Entity<Ticket>(b =>
{
    // Configuration existante conservée...
    
    // FEATURE 004 (research.md D1/D2) : Collation fixe et index unique filtré pour les bookings actifs
    b.Property(t => t.NumeroBooking)
        .HasMaxLength(64)
        .UseCollation("Latin1_General_CI_AS");

    b.HasIndex(t => t.NumeroBooking)
        .IsUnique()
        .HasDatabaseName("IX_Tickets_NumeroBooking_Actif")
        .HasFilter("[NumeroBooking] IS NOT NULL AND [Statut] IN (0, 1, 2)");
});
```

### Explication de l'Index Filtré :
- `[NumeroBooking] IS NOT NULL` : Seuls les tickets Export portant un booking sont indexés.
- `[Statut] IN (0, 1, 2)` : Seuls les statuts actifs bloquent l'unicité (`EnAttenteDePriseEnCharge=0`, `EnCours=1`, `EnAttenteClient=2`).
- Les statuts terminaux `Cloture=3` et `Annule=4` (comprenant les rejets) sont exclus par liste d'inclusion positive, garantissant la libération instantanée du numéro de booking.

---

## 3. Données de Référence & Paramétrage Initial (Seed)

### 3.1. Files de Traitement
Ajout de la file Back Office Export dans le catalogue des files :
- **Id** : GUID déterministe / généré
- **Module** : `BillingExport`
- **Nom** : `Back Office Export`
- **StrategieAffectation** : `PriseLibre` (par défaut)

### 3.2. Habilitations & Permissions
- `BillingExportSoumettre` : Droit de soumettre une demande Export (accordé aux collaborateurs clients).
- `BillingExportBackOfficeInstruire` : Droit de prendre en charge et valider la codification IPAKI en file Back Office Export.
- `BillingFacturationInstruire` : Réutilisation du droit existant pour le dépôt du Draft IN et de la Facture Export.
- `BillingPaiementValider` : Réutilisation du droit existant (`PC-03` / `D7`) pour la validation quittance Caisse/Finance.
- `BillingControleDocumentaireInstruire` : Réutilisation du droit existant pour le contrôle final et le dépôt du BEE.

---

## 4. Invariants & Règles d'Intégrité Métier

1. **Unicité Absolue du Booking Actif (`UNI-04`)** : Impossible d'insérer ou de faire passer à l'état actif un ticket dont le `NumeroBooking` correspond à un autre ticket actif, quelle que soit la société ou le dossier.
2. **Immuabilité du Type Client (`CPT-06` / `EXP-03`)** : Le parcours Export suit exclusivement la valeur snapshotée `Dossier.TypeClient` fixée à la création de la demande.
3. **Indépendance des Tickets Multi-Bookings (`EXP-01`)** : Chaque ticket possède son propre cycle de vie au sein du dossier.
4. **Cloisonnement des Documents (`SEC-02` / `SEC-03`)** : Seuls les documents validés antivirus (`Sain`) sont téléchargeables. Le BEE est réservé au client et aux agents habilités dès sa publication.
