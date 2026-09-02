# RECHERCHE TECHNIQUE & ARCHITECTURALE — FEATURE 014

**Feature** : Feature 014 — Internationalisation & Ergonomie Bilingue (Français / Anglais)  
**Date** : 2026-09-02  
**Baseline CDC d'entrée** : **168 / 188 (89,36 %)**  
**Périmètre** : **NFR-07** (Should)  

---

## 1. ÉTAT DE L'ART DE LA PLATEFORME

### 1.1 Localisation existante
- **ASP.NET Core Localization** : Aucun service `AddLocalization()` ou `UseRequestLocalization()` n'est configuré dans `Program.cs`.
- **Composants Blazor** : Les pages Blazor (`Components/Pages/`) utilisent actuellement des libellés hardcodés en français dans le markup Razor.
- **Ressources linguistiques** : Aucun fichier `.resx` ou dictionnaire JSON de traduction n'existe dans le projet.
- **Attribut `lang`** : Le fichier `App.razor` possède un attribut fixe `lang="fr"`.

### 1.2 Modèle d'affichage et composants UI
- La plateforme utilise Blazor Server / SSR interactif (`InteractiveServer`).
- Les composants partagent un `MainLayout.razor` et un bandeau de personas (`PersonaSwitcher.razor`).
- Les enums de domaine (`StatutTicket`, `TypeDemande`, `SensTrafic`, etc.) sont affichés soit par des switch expressions locales, soit via leur nom technique.

---

## 2. DÉCISIONS D'ARCHITECTURE

### D1 : Mécanisme de localisation natif ASP.NET Core & Blazor
- **Choix** : Utilisation du framework natif `Microsoft.AspNetCore.Localization` combiné à `IStringLocalizer<SharedResources>`.
- **Justification** : Intégration transparente avec le pipeline de requêtes HTTP, support standard des cultures .NET (`CultureInfo.CurrentUICulture`), testabilité aisée et aucun surcoût d'architecture.

### D2 : Stratégie de persistance du choix de langue sans base de données
- **Choix** : Mémorisation de la culture sélectionnée via le cookie standard ASP.NET Core (`CookieRequestCultureProvider.DefaultCookieName` = `.AspNetCore.Culture`).
- **Justification** :
  - **Zéro migration de base de données** (0 impact sur `PlateformeDbContext`, 0 nouvelle table).
  - Fonctionne de manière homogène pour les utilisateurs anonymes et authentifiés.
  - Persiste entre les sessions et les réouvertures de navigateur.

### D3 : Priorité de résolution de la culture
- **Ordre configuré dans `RequestLocalizationOptions`** :
  1. `CookieRequestCultureProvider` (Choix explicite ou mémorisé).
  2. `AcceptLanguageHeaderRequestCultureProvider` (Détection navigateur à la première visite).
  3. `DefaultRequestCulture` fixé à `fr-FR`.
- **Règle anti-écrasement** : Dès qu'un choix utilisateur est émis, le cookie prend le pas définitif sur `Accept-Language`.

### D4 : Commutation de langue sans perte de données
- **Endpoint dédié** : Endpoint Minimal API `/api/v1/culture/set` définissant le cookie et redirigeant vers l'URL d'origine (`redirectUri`).
- **Préservation de saisie** : Les formulaires utilisant les brouillons ERR-01 sauvegardent automatiquement l'état courant avant basculement, garantissant 0 perte de données.

### D5 : Immuabilité absolue des données métier
- Les valeurs persistées (codes énumérés, identifiants `TCK-...`, `DOS-...`, montants en `XOF`, noms de sociétés et commentaires libres) ne sont jamais altérées.
- Seuls les libellés de présentation et textes d'interface statiques sont traduits.
