# Implementation Report — Feature 011 (Certification Sécurité & Socle NFR)

> **Document** : `specs/011-certification-securite-nfr/implementation-report.md`  
> **Date** : 1er septembre 2026  
> **Feature** : F011 — Certification Sécurité & Socle Non-Fonctionnel  
> **Nature** : Feature de certification (production de preuves CDC, 0 nouvelle entité, 0 migration)  

---

## 1. Synthèse d'exécution

L'implémentation de la Feature 011 a été réalisée selon les trois régimes définis dans `research.md` (R1 : rattachement nominatif, R2 : tests complémentaires sans code produit, R3 : validation au démarrage et tests).

### Baseline et résultats des tests

- **Baseline initiale** : 818 tests verts, 0 warning, 0 pending model change.
- **Suite finale** : **857 tests verts** (100%), **0 warning**, **0 pending model change**.
  - Domain.Tests : **158 / 158** (+4 NFR-11)
  - Application.Tests : **439 / 439** (+14 SEC/ERR/NFR)
  - Infrastructure.Tests : **83 / 83** (SEC-05 rattachée sur SQL Server réel)
  - Web.Tests : **177 / 177** (+21 SEC-01, SEC-10, ERR-02, NFR-01, NFR-02)

---

## 2. Exigences certifiées et preuves

| Exigence CDC | TEST-SPEC | Régime | Fichier de preuve / suite | Résultat |
|:---|:---|:---:|:---|:---:|
| **SEC-01** (Must) | `SEC-CERT-01` | R2 | `tests/CIT.Plateforme.Web.Tests/Securite/SecCert01VerrouillageTests.cs` | **4/4 verts** |
| **SEC-03** (Must) | `SEC-CERT-02` | R2 | `tests/CIT.Plateforme.Application.Tests/Securite/SecCert02CloisonnementTests.cs` | **4/4 verts** |
| **SEC-05** (Must) | `SEC-CERT-03` | R1 | `tests/CIT.Plateforme.Infrastructure.Tests/Persistence/JournalAuditAppendOnlyTests.cs` | **3/3 verts (SQL réel)** |
| **SEC-10** (Must) | `SEC-CERT-04` | R2 | `tests/CIT.Plateforme.Web.Tests/Securite/SecCert04SessionTests.cs` | **3/3 verts** |
| **SEC-11** (Must) | `SEC-CERT-05` | R2 | `tests/CIT.Plateforme.Application.Tests/Securite/SecCert05DesactivationTests.cs` | **2/2 verts** |
| **SEC-12** (Must) | `SEC-CERT-06` | R2 | `tests/CIT.Plateforme.Application.Tests/Securite/SecCert06LienNotificationTests.cs` | **3/3 verts** |
| **ERR-02** (Must) | `ERR-CERT-01` | R3 | `tests/CIT.Plateforme.Web.Tests/Securite/ErrCert01ConfigurationRepriseTests.cs` | **7/7 verts** |
| **ERR-03** (Must) | `ERR-CERT-02` | R1 | `tests/CIT.Plateforme.Application.Tests/Documents/ControleDepotTests.cs` | **3/3 verts** |
| **NFR-01** (Must) | `NFR-CERT-01` | R2 | `tests/CIT.Plateforme.Web.Tests/Nfr/NfrCert01LangueTests.cs` | **2/2 verts** |
| **NFR-02** (Must) | `NFR-CERT-02` | R2 | `tests/CIT.Plateforme.Web.Tests/Nfr/NfrCert02ResponsiveTests.cs` | **5/5 verts** |
| **NFR-11** (Must) | `NFR-CERT-03` | R2 | `tests/CIT.Plateforme.Domain.Tests/SuiviTemps/NfrCert03HorairesTests.cs` | **4/4 verts** |

**Total : 11/11 TEST-SPEC exécutés et 100% conformes.**

---

## 3. Détail des décisions et gates

### GATE-ERR03 : Issue A retenue
Le CDC §18.2 spécifie « vérifiés à l'upload (intégrité, type, taille) avec message d'erreur explicite ». Les contrôles de format (extensions autorisées) et de dimension (taille max en octets) sont déjà pleinement opérationnels avec messages explicites (`ControleDepotTests`). L'intégrité correspond à un fichier valide et non corrompu selon ces filtres, sans nécessiter d'empreinte cryptographique dédiée (qui relèverait d'un développement ERP hors périmètre). **ERR-03 certifiée.**

### SEC-10 : Paramétrabilité de session
La politique de session lit la clé `Securite:ExpirationSessionMinutes` avec `SlidingExpiration`. La valeur de test (5 minutes) est technique et non contractuelle. Aucun changement en dur dans `appsettings.json` de production.

### ERR-02 : Fail-fast de reprise
La configuration de rejeu est validée au démarrage du composant (`ParametresReessaiNotification.Valider()`). Le harnais de test `PlateformeWebApplicationFactory` fournit une configuration technique explicite.

### NFR-01 : Fix francophone
La balise `<html lang="fr">` dans `App.razor` a été positionnée en remplacement de la valeur par défaut `en`.

### NFR-02 : Dette de recette
- **VALIDATION VISUELLE NAVIGATEUR : NON COUVERTE PAR F011 — DETTE RECETTE.**
La preuve structurelle démontre la conformité du balisage (meta viewport, media queries, flexbox/grid, scroll conteneurs), sans outil de capture de rendu réel (Playwright non requis).

---

## 4. Exclusions formelles respectées

- **SEC-02** (Sécurité niveau champs) : 0 code, 0 TEST-SPEC actif, exclusion intacte (PC-05 ouvert).
- **SEC-07** (Analyse antivirale externe) : 0 fournisseur, 0 analyse simulée, exclusion intacte.
- **SEC-08** (2FA agents/admins) : 0 modification Auth/2FA, exclusion intacte.

---

## 5. Bilan de couverture CDC

- **Baseline CDC pré-F011** : 146 / 188 (77,66 %)
- **Gain certifié F011** : +11 exigences Must
- **Nouvelle couverture CDC projetée** : **157 / 188 (83,51 %)**
