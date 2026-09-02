# Guide de Démarrage Rapide (Quickstart) — Feature 004 : Billing Office EXPORT

> **Dossier** : `specs/004-billing-office-export/`  
> **Date** : 30 Août 2026  
> **Statut** : Validé pour Planification (`/speckit.plan`)  

---

## 1. Prérequis & Environnement

- **SDK .NET** : .NET 10.0
- **Base de Données** : SQL Server (LocalDB ou instance Docker dédiée)
- **Baseline de départ** : 310 tests verts

---

## 2. Exécution des Tests

### Vérification de la Non-Régression Globale
```bash
dotnet build
dotnet test
```

### Exécution Spécifique des Tests Feature 004
```bash
# Tests de Domaine & Unicité Booking
dotnet test --filter "FullyQualifiedName~BillingExport"

# Scénario E2E REC-SCE-05 (4 types de clients)
dotnet test --filter "ParcoursExportE2ETests"
```

---

## 3. Scénario de Démonstration REC-SCE-05

1. **Connexion Client Normal** :
   - Accéder à `/billing-export/nouvelle-demande`.
   - Saisir les numéros de booking `BKG-EXP-001`, `BKG-EXP-002`.
   - Téléverser la confirmation de booking.
   - Soumettre : 1 Dossier `DOS-EXP-...` et 2 Tickets `TCK-EXP-...` sont générés.
2. **Instruction Back Office Export** :
   - Basculer sur le persona `Agent Back Office`.
   - Accéder à `/billing-export/back-office`.
   - Prendre en charge `TCK-EXP-001` et valider la codification IPAKI.
3. **Facturation Export & Draft IN** :
   - Basculer sur le persona `Pilote Facturation`.
   - Accéder à `/billing-import/facturation` (file transverse).
   - Joindre le Draft IN et la Facture Export.
4. **Paiement & Contrôle Documentaire** :
   - Déposer la preuve de paiement (côté client).
   - Valider le paiement (Caisse / Finance).
   - En file Contrôle Documentaire, déposer le **BEE** officiel.
   - Le ticket est clôturé et le BEE est téléchargeable par le client.
