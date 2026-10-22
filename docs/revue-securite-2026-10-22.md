# Revue de sécurité défensive — cit-digital-platform

**Date :** 2026-10-22 · **Périmètre :** code source (défensif, read-only) · **Branche :** main

## Verdict

**Aucune vulnérabilité critique ou élevée trouvée.** Code mature, posture sécurité solide,
déjà couvert par une suite de tests dédiée (`tests/.../Securite/`).

## Ce qui a été vérifié

| Vecteur | Résultat |
|---|---|
| **Injection SQL** | ✅ Sûr. Seul SQL brut en prod = `ServiceNumerotation.cs` (`SELECT NEXT VALUE FOR {sequence}`) ; `sequence` vient uniquement de constantes internes, jamais d'entrée utilisateur (un identifiant d'objet SQL n'est de toute façon pas paramétrable). Tout le reste passe par EF Core (requêtes paramétrées). |
| **XSS (Razor)** | ✅ Sûr. Tous les `@((MarkupString)…)` rendent des chaînes de ressources i18n (`Loc[...]`), contrôlées par le dev — jamais des données utilisateur. Les args de format sont des entiers (n° version, comptes). |
| **Secrets en dur** | ✅ Aucun. `appsettings.Production.json` non versionné (depuis `3e1b0a9`), secrets via user-secrets / env / secret manager. Mots de passe de test = synthétiques ou `CIT_TEST_USERS_PASSWORD`. Test de garde : `SecCert09_02_AucunSecretProductionEnClairDansGit`. |
| **Auth / cookies** | ✅ `HttpOnly`, `SecurePolicy.Always` en prod, HSTS, Data Protection partagé (Redis) pour scale-out. |
| **CSRF** | ✅ `UseAntiforgery` + validation explicite sur logout et impersonation (POST forgé cross-site bloqué). |
| **Autorisation des pages** | ✅ Pages sans `[Authorize]` toutes légitimes (connexion, mot de passe oublié, invitation, accueil…). `BillingImport/SoumettreDemande` et `SimulationProformaPage` gardent en code (`PermissionDenied`) ; la simulation est publique par décision métier **documentée** (SIM-01), l'enregistrement restant gaté (authentifié + société + permission admin). |
| **Révocation de session** | ✅ Security stamp vérifié contre la base ; désactivation d'un compte retire toute autorisation même si le cookie survit. |
| **Fuite d'info** | ✅ Journal de connexion n'écrit jamais de secret (test `CablageJournalConnexion`), non-divulgation de jeton de reset testée (`SecResetDivulgationJeton`). |

## Sur ta demande initiale (DDoS / flood)

La vraie défense contre « connexions/requêtes multiples » n'est pas d'attaquer, c'est le
**rate-limiting**. Je n'ai pas vu de middleware `AddRateLimiter` dans `Program.cs`.

**Recommandation (seul point d'action) :** ajouter un rate-limiter ASP.NET Core sur les
endpoints sensibles — surtout `/login`, reset mot de passe, 2FA — pour limiter le
brute-force et l'abus. C'est du natif .NET 8+, quelques lignes. Je peux le câbler si tu veux.

## Conclusion

Rien à corriger en urgence. Le code est écrit par quelqu'un qui connaît les pièges
(host-header injection, antiforgery sur logout, security stamp, cloisonnement des secrets).
Le seul durcissement proactif utile = rate-limiting.
