# Data Model & Invariants de Persistance — Feature 012

> **Document** : `specs/012-remediation-dettes-recette-transverse/data-model.md`  
> **Feature** : F012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves  
> **Date** : 1er septembre 2026  
> **Statut** : DÉMONSTRATION COMPLÈTE : 0 ENTITÉ, 0 TABLE, 0 MIGRATION (Phase 1)

---

## 1. Principe Directeur : Zéro Migration

La Feature 012 s'appuie intégralement sur les structures de données créées lors des Features 001 à 011.

**Aucune commande `dotnet ef migrations add` ne sera exécutée.**

---

## 2. Démonstration de Suffisance par Exigence

### PRI-02 — Priorité automatique et ordonnancement
- **Entités requises** : `NiveauPriorite`, `ModificationPriorite`, `Ticket`.
- **Tables existantes** : `NiveauxPriorite`, `ModificationsPriorite`, `Tickets`.
- **Colonnes utilisées** : `Tickets.PrioriteId` (clé étrangère nullable vers `NiveauxPriorite.Id`), `NiveauxPriorite.Rang` (entier pour l'ordonnancement).
- **Suffisance constatée** : Les colonnes et relations existent déjà dans `PlateformeDbContext`. Aucune altération de schéma n'est requise.

### IMP-03 — Numéro de ticket communiqué par e-mail
- **Entités requises** : `EvenementSocle`, `Notification`, `EnvoiNotification`, `RegleDiffusion`.
- **Tables existantes** : `JournalEvenements`, `Notifications`, `EnvoisNotification`, `ReglesDiffusion`.
- **Colonnes utilisées** : `JournalEvenements.TypeEvenement` ("TicketCree"), `EnvoisNotification.Canal` (Email = 1).
- **Suffisance constatée** : Schéma F009 déjà complet.

### REC-02 & REC-03 — Catégories de réclamation et orientation
- **Entités requises** : `CategorieReclamation`, `Ticket`, `FileDeTraitement`.
- **Tables existantes** : `CategoriesReclamation`, `Tickets`, `FilesDeTraitement`.
- **Colonnes utilisées** : `CategoriesReclamation.Code`, `CategoriesReclamation.Libelle`, `CategoriesReclamation.FileCibleDefautId`, `CategoriesReclamation.EstActif`, `Tickets.CategorieReclamationId`.
- **Suffisance constatée** : Déjà migré lors de la remédiation post-audit F006 (`20260830180721_AjoutSupportCustomerComplaint`).

### SEC-08 — Double authentification 2FA
- **Entités requises** : `ApplicationUser`, tables ASP.NET Core Identity (`AspNetUserTokens`, etc.).
- **Suffisance constatée** : Schema Identity natif déjà configuré.

### SOC-14 — Export tabulaire CSV
- **Nature** : Traitement de lecture seule (projection en mémoire vers un flux textuel CSV).
- **Persistance** : Aucune persistance d'état requise (pas de table d'export ou d'historique de téléchargement imposée par le CDC).
- **Suffisance constatée** : 0 colonne, 0 table.

### COM-06 — Relances automatiques multicanales
- **Entités requises** : `RegleDiffusion`, `PreferenceCanal`, `Notification`, `EnvoiNotification`.
- **Tables existantes** : `ReglesDiffusion`, `PreferencesCanal`, `Notifications`, `EnvoisNotification`.
- **Suffisance constatée** : Configuration portée par les données de règles existantes.

---

## 3. Invariants de Données Préservés

1. **Invariant #1 (Cloisonnement CPT-04)** : Tout jeu de données exporté (SOC-14) doit filtrer strictement par `ClientId == SocieteId` de l'utilisateur appelant.
2. **Invariant #2 (Immutabilité de l'Audit SEC-05)** : Les événements de création, modification de priorité et téléchargement s'inscrivent dans le journal append-only `JournalAudit` sans possibilité de mise à jour ou suppression.
3. **Invariant #3 (Distribution Effectively-Once F009)** : L'index unique `(EvenementSourceId, DestinataireId)` garantit qu'aucune notification en double n'est générée pour `TicketCree` ou `RelanceAutomatique`.

---

## 4. Synthèse

| Composant | Nouveau Modèle | Nouvelle Table | Nouvelle Migration |
|:---|:---:|:---:|:---:|
| **PRI-02** | 0 | 0 | 0 |
| **IMP-03** | 0 | 0 | 0 |
| **REC-02** | 0 | 0 | 0 |
| **REC-03** | 0 | 0 | 0 |
| **SEC-08** | 0 | 0 | 0 |
| **SOC-14** | 0 | 0 | 0 |
| **COM-06** | 0 | 0 | 0 |
| **REC-SCE-14** | 0 | 0 | 0 |
| **TOTAL** | **0** | **0** | **0** |
