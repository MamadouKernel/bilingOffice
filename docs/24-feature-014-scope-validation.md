# FEATURE 014 — SCOPE VALIDATION GATE

**Auteur** : Scrum Master & Lead QA Platform CIT  
**Date** : 2026-09-02  
**Baseline CDC d'entrée** : **168 / 188 exigences conformes (89,36 %)**  
**Commit de référence** : `7fda32734bc028f5666d622c1bd8b935e4a0eac1` (F013 CLOSED)  

---

## 1. AUDIT FONCTIONNEL & TEXTE CDC DE NFR-07

### 1.1 Texte exact du CDC
- **Référence** : `NFR-07` (§19 du CDC, ligne 906).
- **Priorité** : **Should**.
- **Texte exact** : *« Interface disponible en anglais (clients internationaux). »*
- **Acteurs concernés** : Clients internationaux (armateurs, transitaires internationaux, agents maritimes anglophones : Maersk, MSC, CMA CGM, Hapag-Lloyd, transitaires régionaux).

### 1.2 État de l'art dans le code existant
- **Localisation .NET existante** : Aucune infrastructure `IStringLocalizer` active.
- **Fichiers .resx / JSON de ressources** : Aucun dictionnaire de traduction anglais n'est présent.
- **Middleware de culture** : Non configuré dans `Program.cs` (`RequestLocalizationOptions`).
- **`App.razor`** : Hardcodé en `lang="fr"`.
- **Labels et formulaires** : Hardcodés en français dans l'ensemble des composants Blazor.

### 1.3 Portée minimale requise pour NFR-07
1. **Socle technique d'internationalisation** :
   - Middleware de localisation ASP.NET Core (`Microsoft.AspNetCore.Localization`).
   - Fournisseurs de culture : cookie de préférence de langue (`.AspNetCore.Culture`), en-tête HTTP `Accept-Language`, fallback par défaut en français (`fr-FR`).
   - Sélecteur de langue dynamique dans le header / layout (composant `LanguageSwitcher.razor` avec bascule FR 🇫🇷 / EN 🇬🇧).
2. **Localisation des parcours clients clés** :
   - Page d'accueil et portail d'authentification (`Home.razor`, `Connexion.razor`).
   - Navigation principale, bandeaux et pied de page.
   - Formulaires de dépôt et suivi client : Demande Import (`SoumettreDemande.razor`), Demande Export (`SoumettreDemandeExport.razor`), Réclamations (`NouvelleReclamation.razor`, `MesReclamations.razor`), Enrôlement (`SoumettreDemande.razor`).
   - Outil de simulation proforma (`SimulationProformaPage.razor`).
   - Consultation et suivi des tickets (`DetailTicket.razor`, `DetailDossier.razor`).
3. **Portée hors F014** :
   - Traduction des données métier dynamiques saisies par les utilisateurs (noms de sociétés, commentaires libres, descriptions de litiges).
   - Traduction multilingue des notifications WhatsApp/SMS (bloqué par PC-16).

---

## 2. AUDIT FORMEL DE NFR-03 (DISPONIBILITÉ EN HEURES OUVRÉES)

### 2.1 Texte exact du CDC
- **Référence** : `NFR-03` (§19 du CDC, ligne 894).
- **Priorité** : **Must**.
- **Texte exact** : *« Disponibilité cible en heures ouvrées du Billing Office ; objectif ≥ 99 % ; maintenance planifiée hors heures de pointe. »*

### 2.2 Analyse et démystification
- **Nature de l'exigence** : NFR-03 est un **SLA d'infrastructure et d'exploitation** (objectif de disponibilité ≥ 99% en production), à l'instar de NFR-10 (disponibilité 24/7) et NFR-06 (PRA/Sauvegardes).
- **Confusion à proscrire** : NFR-03 ne concerne pas une "architecture modulaire" (qui relevait de NFR-08, déjà certifiée en F013).
- **Statut réel** : Relever NFR-03 relève de la **Production Readiness & Exploitation (F018)** lors du déploiement sur l'infrastructure cible d'hébergement CIT/AGL avec sonde de monitoring et politique de fenêtres de maintenance. Tenter de la certifier par du code unitaire en F014 serait un faux gain non soutenu par le CDC.

---

## 3. ÉVALUATION COMPARATIVE DES OPTIONS DE SCOPE F014

| Critère | Option A : NFR-07 seul (Recommandé) | Option B : NFR-03 seul | Option C : NFR-07 + NFR-03 (Bundle artificiel) |
|:---|:---:|:---:|:---:|
| **Cohérence fonctionnelle** | **Très forte** (100% centré sur l'expérience bilingue) | Faible (exigence d'exploitation isolée) | **Nulle** (mélange UX et SLA hébergeur) |
| **Autonomie & Dépendances**| **100% autonome** (0 tiers, 0 PC bloquante) | Dépendance infrastructure hébergement | Dépendance externe partielle |
| **Démontrabilité par tests** | **100% démontrable** (tests unitaires & UI) | Impossible sans infra de production | Démonstration affaiblie sur NFR-03 |
| **Gain CDC certifiable** | **+1** (`NFR-07`, Should) | 0 (non certifiable en code seul) | +1 réel (+1 artificiel contestable) |
| **Effort / Risque** | Effort modéré / Risque très faible | Effort faible / Risque de rejet audit élevé | Risque de rejet en Closure Review |
| **Score global (/100)** | **92 / 100** | 35 / 100 | 50 / 100 |

---

## 4. DÉCISION DE CADRAGE OFFICIELLE

### Option retenue : OPTION A — FEATURE 014 DÉDIÉE À NFR-07

```text
FEATURE :
014 — Internationalisation & Ergonomie Bilingue (Français / Anglais)

OBJECTIF :
Doter la plateforme CIT d'un système complet de localisation bilingue (Français / Anglais)
pour l'ensemble des parcours clients, de la navigation et des formulaires clés.

EXIGENCE COUVERTE :
- NFR-07 (Should) — Interface disponible en anglais (clients internationaux)

GAIN MAXIMAL :
+1 exigence

BASELINE PROJETÉE :
168 + 1 = 169 / 188 (89,89 %) — Non acquise avant Closure Review

MIGRATION EF CORE :
0 migration (couche purement applicative, ressources .resx / JSON et middleware)

DÉPENDANCES BLOQUANTES :
0
```

---

## 5. IMPACT TECHNIQUE & FICHIERS CONCERNÉS

- **Configuration & Middleware** :
  - `src/CIT.Plateforme.Web/Program.cs` (Enregistrement de `AddLocalization`, `RequestLocalizationOptions`, cultures `fr-FR` et `en-US`).
  - `src/CIT.Plateforme.Web/Components/App.razor` (Attribut `lang` dynamique selon la culture courante).
- **Ressources de localisation** :
  - Dictionnaires de ressources partagés et par page (`Resources/SharedResources.fr.resx`, `Resources/SharedResources.en.resx` ou équivalent JSON/PO).
- **Composants d'interface & Navigation** :
  - Nouveau composant de sélection de langue `LanguageSwitcher.razor` intégré au layout principal (`MainLayout.razor` / `PersonaSwitcher.razor`).
- **Formulaires et vues clients** :
  - Injection de `IStringLocalizer<SharedResources>` dans les composants majeurs (`Home.razor`, `SoumettreDemande.razor`, `SoumettreDemandeExport.razor`, `NouvelleReclamation.razor`, `SimulationProformaPage.razor`, `DetailTicket.razor`).

---

## 6. TEST-SPEC DÉFINI POUR NFR-07

### Suite `NFR-CERT-07` : Certification de l'Internationalisation Bilingue
- **T-NFR07-01** : Commutation de culture par sélecteur (mise à jour du cookie `.AspNetCore.Culture` et rafraîchissement réactif de l'UI).
- **T-NFR07-02** : Détection automatique via l'en-tête `Accept-Language: en-US` pour un client anglophone non authentifié.
- **T-NFR07-03** : Traduction intégrale des menus, boutons, titres et messages d'aide sur les 4 formulaires majeurs.
- **T-NFR07-04** : Absence de régression sur la culture par défaut (`fr-FR`) pour les utilisateurs francophones.
- **T-NFR07-05** : Persistance de la préférence linguistique au cours de la session de navigation.

---

## 7. CONTRÔLE ANTI-SCOPE

Sont **strictement exclus** de la Feature 014 :
- Toute tentative de certification fictive de NFR-03, NFR-04, NFR-05, NFR-06, NFR-10 (réservés à F018).
- Tout composant IA (`IA-01` à `IA-07`, réservés à F015 après levée de PC-18).
- Toute modification du modèle de données EF Core (0 migration).
- Tout service SMS / WhatsApp externe (réservé à F017 après levée de PC-16).
- Toute altération non sollicitée des composants d'administration ou des fonctionnalités déjà closes.
