# CHECKLIST DES EXIGENCES & CLARIFICATIONS — FEATURE 015

## Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement

---

### 1. MATRICE DE TRAÇABILITÉ CDC

| Exigence CDC | Priorité | Texte exact du CDC (§18) | Couverture dans Spec & Décisions | Statut |
|:---|:---:|:---|:---|:---:|
| **`SEC-04`** | **Must** | *« Chiffrement des échanges (HTTPS/TLS) et protection des documents stockés »* | US1, US2, DEC-015-01..03, `SEC-CERT-04-01..03` | ✅ CLARIFIÉE |
| **`SEC-07`** | **Must** | *« Contrôle des fichiers déposés : types autorisés, taille maximale, analyse antivirale »* | US3, US4, DEC-015-04..11, `SEC-CERT-07-01..06` | ✅ CLARIFIÉE |
| **`SEC-09`** | **Should**| *« Cloisonnement des environnements (production, recette) et gestion des habilitations par environnement »* | US5, DEC-015-12..16, `SEC-CERT-09-01..02` | ✅ CLARIFIÉE |

---

### 2. AUDIT DES DÉCISIONS CLÉS (18/18 VALIDÉES)

- [x] **DEC-015-01 à 03 (SEC-04)** : Frontière TLS/HSTS claire, headers défensifs Blazor-compatibles, isolation GED hors `wwwroot` et intégrité SHA-256.
- [x] **DEC-015-04 à 06 (SEC-07 - Validations)** : Formats par défaut paramétrables, double validation Magic Bytes, plafond configurable (10 Mo par défaut).
- [x] **DEC-015-07 à 11 (SEC-07 - Antivirus)** : Analyse réelle observable (EICAR), politique de sécurité Fail-Closed, scan synchrone à l'upload, destruction des fichiers infectés, 0 SaaS cloud externe.
- [x] **DEC-015-12 à 16 (SEC-09 - Environnements)** : Profils Staging/Production étanches, 0 secret dans Git, `DevelopmentSeeder` désactivé en production, Swagger restreint en prod.
- [x] **DEC-015-17 & 18 (Invariants)** : 0 donnée sensible dans les logs, 0 migration EF Core.

---

### 3. EXCLUSIONS STRICTES (ANTI-SCOPE RESPECTÉ)

- [x] **`SEC-02` formellement exclue** : Différée à F018 car bloquée par l'atelier métier `PC-05`.
- [x] **`SEC-06` formellement exclue** : Différée à F018 car bloquée par `PC-14`.
- [x] **`ENR-04` formellement exclue** : Différée à F018 car bloquée par `PC-16`.
- [x] **`IA-01..07` formellement exclues** : Différées à F017 car bloquées par `PC-18`.
- [x] **Exigences d'exploitation / SLA exclues** : `NFR-03`, `NFR-04`, `NFR-05`, `NFR-06`, `NFR-09`, `NFR-10`.
- [x] **Aucun faux chiffrement AES/DPAPI inventé**.

---

### 4. RECAPITULATIF TEST-SPEC

- **`SEC-CERT-04`** : 3 scénarios (Redirection HTTPS/HSTS, Headers défensifs, Intégrité GED & rejet accès URL direct).
- **`SEC-CERT-07`** : 6 scénarios (Type valide, Rejet Magic Bytes, Rejet taille, Fichier sain inspecté, Détection EICAR/Infecté, Panne Fail-Closed).
- **`SEC-CERT-09`** : 2 scénarios (Garde-fous profil Production, Non-exposition de secrets versionnés).
- **Total scénarios** : **11 scénarios de validation stricts**.
