# CONTRATS D'USAGE (USE CASES) — FEATURE 014

**Feature** : Feature 014 — Internationalisation & Ergonomie Bilingue (Français / Anglais)  
**Périmètre** : **NFR-07** (Should)  

---

## 1. INVENTAIRE DES CONTRATS C14

| Contrat | Type | Nom du Contrat | Description métier | Exigence source |
|:---|:---:|:---|:---|:---:|
| **`C14-01`** | **Query** | `ObtenirCultureCouranteQuery` | Résout la culture active (`fr-FR` ou `en-US`) selon l'ordre de priorité | **NFR-07** |
| **`C14-02`** | **Command**| `ChangerCultureCommand` | Définit la préférence de culture via cookie sans perte de session | **NFR-07** |
| **`C14-03`** | **Query** | `LocaliserLibelleQuery` | Renvoie la traduction d'une clé avec fallback français garanti | **NFR-07** |
| **`C14-04`** | **Query** | `FormaterCulturellementQuery` | Formate les dates et nombres selon la culture d'affichage | **NFR-07** |
| **`C14-05`** | **Query** | `VerifierCompletudeRessourcesQuery` | Vérifie la parité stricte (100% FR == EN) de la matrice de ressources | **NFR-07** |

---

## 2. DÉFINITION TECHNIQUE DÉTAILLÉE DES CONTRATS

### 2.1 Contrat C14-01 : `ObtenirCultureCouranteQuery`
- **Objectif** : Identifier la culture à appliquer pour la requête ou le rendu Blazor.
- **Paramètres** : `HttpContext? httpContext`.
- **Résultat** : `string CultureName` (`fr-FR` ou `en-US`).
- **Règles** :
  - Si un cookie `.AspNetCore.Culture` valide est présent → retourne la culture du cookie.
  - Sinon, si l'en-tête `Accept-Language` contient `en` → retourne `en-US`.
  - Dans tous les autres cas → retourne `fr-FR`.

---

### 2.2 Contrat C14-02 : `ChangerCultureCommand`
- **Objectif** : Mémoriser le choix explicite de l'utilisateur.
- **Paramètres** : `string NouvelleCulture`, `string? RedirectUri`.
- **Comportement** :
  - Écrit le cookie `.AspNetCore.Culture` avec valeur formatée (`c=en-US|uic=en-US` ou `c=fr-FR|uic=fr-FR`) et expiration longue (1 an).
  - Redirige vers `RedirectUri` (ou `/`) en conservant les query parameters.

---

### 2.3 Contrat C14-03 : `LocaliserLibelleQuery`
- **Objectif** : Fournir la chaîne de caractères traduite pour un composant UI.
- **Paramètres** : `string Cle`, `string? Culture`.
- **Résultat** : `string TexteTraduit`.
- **Règles** :
  - Si la clé existe pour la culture demandée → retourne la traduction.
  - Si la traduction est manquante pour `en-US` → retourne la version `fr-FR` (fallback de sécurité).
  - Si la clé n'existe dans aucune langue → retourne la clé technique.

---

### 2.4 Contrat C14-04 : `FormaterCulturellementQuery`
- **Objectif** : Formater l'affichage des dates et nombres sans changer les calculs métier.
- **Paramètres** : `DateTime / DateOnly / decimal Valeur`, `TypeFormat Format`, `string? Culture`.
- **Règles** :
  - Les montants monétaires affichent le suffixe `XOF` (0 conversion de devise).
  - Les dates et nombres respectent les séparateurs culturels.

---

### 2.5 Contrat C14-05 : `VerifierCompletudeRessourcesQuery`
- **Objectif** : Exécuter un contrôle automatisé d'exhaustivité bilingue à destination de la suite de tests et de l'audit.
- **Résultat** : `CompletudeRapport { bool EstComplet, IReadOnlyList<string> ClesManquantesEn, IReadOnlyList<string> ClesManquantesFr }`.
- **Règle** : Échoue si une clé FR n'a pas son pendant EN non vide dans la matrice certifiée.
