# Revue de sécurité — cit-digital-platform

**Date :** 2026-10-22 (révisé 2026-10-25) · **Type :** revue **statique** du code (lecture + build +
scan de dépendances), **pas** un pentest dynamique en exécution · **Branche :** main

> Aucune attaque n'a été exécutée (pas de DDoS, pas d'injection à l'aveugle, pas de flood). Les
> conclusions viennent de l'analyse du code, de la configuration et des dépendances.

## Tableau des résultats

| # | Vecteur (OWASP) | Sévérité | Statut | Constat | Preuve |
|---|---|---|---|---|---|
| 1 | Injection SQL (A03) | Critique | ✅ Sûr | Tout en EF Core paramétré ; seul SQL brut = `ServiceNumerotation`, jamais d'entrée utilisateur | `ServiceNumerotation.cs:60` |
| 2 | XSS (A03) | Élevé | ✅ Sûr | Tous les `MarkupString` rendent des libellés i18n (`Loc[...]`), pas de données utilisateur | ~23 occurrences `.razor` |
| 3 | Secrets en dur (A05/A07) | Critique | ✅ Sûr | Aucun secret versionné ; `appsettings.Production.json` hors Git depuis `3e1b0a9` | `SecCert09_02_AucunSecretProductionEnClairDansGit` |
| 4 | Auth / session (A07) | Élevé | ✅ Sûr | Cookie `HttpOnly` + `Secure` en prod, HSTS, révocation par security stamp | `Program.cs:208` |
| 5 | CSRF (A01) | Élevé | ✅ Sûr | `UseAntiforgery` + validation explicite sur logout et impersonation | `Program.cs:507` |
| 6 | Contrôle d'accès (A01) | Élevé | ✅ Sûr | Pages sans `[Authorize]` toutes légitimes ; gardes en code sinon ; simulation publique **documentée** (SIM-01) | `PermissionDenied`, `SimulationProformaPage` |
| 7 | Divulgation d'info (A04/A05) | Moyen | ✅ Sûr | OpenAPI hors dev uniquement, `/ready` sans détail infra, journal sans secret | `SurfaceExposeeProduction`, `CablageJournalConnexion` |
| 8 | Reset mot de passe (A07) | Moyen | ✅ Sûr | Non-divulgation de jeton testée | `SecResetDivulgationJeton` |
| 9 | Brute-force / flood (A07) | Moyen | ✅ Corrigé | Pas de plafond resserré sur l'auth → **ajouté** : 10 tentatives/5 min par IP sur login, reset, 2FA + test | `a0a5daf`, `f968681`, `Program.cs:93` |
| 10 | **Composants vulnérables (A06)** | Élevé | ✅ Corrigé | `.Pkcs` épinglé **10.0.13** (Infrastructure + Web) et `.Xml` épinglé **10.0.13** (Web.Tests) → **scan vide sur les 9 projets**. | `dotnet list package --vulnerable` (vide) |

## Détail A06 (point 10) — à traiter

| Package | Version résolue | Projets prod touchés | Avis |
|---|---|---|---|
| `System.Security.Cryptography.Pkcs` | 9.0.11 | Infrastructure, Web | High `GHSA-846f-f764-76mx` + Moderate `GHSA-2fmv-rv2c-59x7` |
| `System.Security.Cryptography.Xml` | 10.0.12 | (tests uniquement) | High `GHSA-r955-8q5j-3h83`, `GHSA-7hqp-7c5m-4vq6`, `GHSA-c399-9853-6jmj` |

**Nature :** dépendances **transitives** (tirées par le runtime/Identity), pas du code maison.
**Remédiation :** épingler une `PackageReference` directe vers la version corrigée de
`System.Security.Cryptography.Pkcs` dans les projets Infrastructure et Web (l'épingle directe
prime sur la version transitive), après vérification que la build et les tests passent. À appliquer
sur un checkout propre d'`origin/main`.

## Bilan

- **Vulnérabilités dans le code maison : 0 critique/élevée.** Posture solide, déjà couverte par
  une suite de tests sécurité dédiée (`tests/.../Securite/`).
- **2 durcissements livrés** : rate-limiter par IP sur l'authentification (point 9) et mise à jour
  des dépendances crypto vulnérables vers 10.0.13 (point 10, `.Pkcs` + `.Xml`).
- **Scan de dépendances : 100 % vert** sur les 9 projets de la solution.

## Portée & limites

- Revue **statique** : pas de scan dynamique, fuzzing, ni exploitation en exécution.
- Config **infra** (TLS, pare-feu L7/WAF, durcissement serveur/IIS) hors périmètre code.
