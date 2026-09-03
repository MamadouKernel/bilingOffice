# Matrice de tests de sécurité internes

> **Document** : `docs/78-internal-security-test-matrix.md`
> **Établi le** : 2026-09-03
> **Portée** : ce qui est testable **localement**, sans environnement déployé.
> Chaque ligne `PASS` correspond à un test automatisé exécuté, pas à une lecture de code.

---

## Légende

`PASS` vérifié par exécution · `PASS (code)` établi par lecture, non exerçable localement ·
`DEFERRED` exige un environnement · `OPEN` défaut constaté non corrigé

---

## AUTH — authentification

| ID | Attaque | Attendu | Réel | État | Preuve |
|:---|:---|:---|:---|:---|:---|
| AUTH-01 | Divulgation du jeton de réinitialisation à un anonyme | Aucun jeton dans la réponse | Aucun | **PASS** | `SEC-RESET-01` |
| AUTH-02 | Divulgation de l'URL de réinitialisation | Aucune URL | Aucune | **PASS** | `SEC-RESET-02` |
| AUTH-03 | Énumération de comptes par le formulaire d'oubli | Réponses indiscernables | Indiscernables | **PASS** | `SEC-RESET-03` |
| AUTH-04 | Divulgation en Staging | Aucune | Aucune | **PASS** | `SEC-RESET-05` |
| AUTH-05 | Jeton présent dans les journaux | Absent | Absent | **PASS** | `SEC-RESET-07` |
| AUTH-06 | Parcours Identity officiel encore fonctionnel | Fonctionne | Fonctionne | **PASS** | `SEC-RESET-08` |
| AUTH-07 | Verrouillage après échecs répétés | Verrouillage | Verrouillage | **PASS** | `SecCert01VerrouillageTests` |
| AUTH-08 | Expiration de session | Session expirée rejetée | Rejetée | **PASS** | `SecCert04SessionTests` |
| AUTH-09 | Second facteur exigé après activation | Exigé | Exigé | **PASS** | `DoubleAuthentificationTests` |
| AUTH-10 | Second facteur non exigé sans activation | Non exigé | Non exigé | **PASS** | `DoubleAuthentificationTests` |
| AUTH-11 | Secret TOTP transmis à un tiers | Jamais | Généré localement | **PASS** | `AucuneRessourceExterneTests` |
| AUTH-12 | Rejeu d'un jeton de réinitialisation consommé | Refusé | — | **PASS (code)** | Identity invalide par `SecurityStamp` |

---

## AUTHZ / IDOR / TENANT — habilitations et cloisonnement

| ID | Attaque | Attendu | Réel | État | Preuve |
|:---|:---|:---|:---|:---|:---|
| AUTHZ-01 | Client société A consulte une demande de société B | Refus | Refus | **PASS** | `RecSce14CloisonnementCompteTests` |
| AUTHZ-02 | Visibilité inter-sociétés par requête directe | Jamais | Jamais | **PASS** | `RecSce14CloisonnementCompteTests` |
| AUTHZ-03 | Accès sans habilitation | Refus | Refus | **PASS** | `CustomerComplaintSecurityTests` |
| AUTHZ-04 | Accès document hors périmètre | Refus | Refus | **PASS** | `AccesDocumentAutoriseTests` |
| AUTHZ-05 | Dépôt de document sans droit | Refus | Refus | **PASS** | `DepotDocumentAutorisationTests` |
| AUTHZ-06 | Appel d'API non authentifié | **401** | 401 | **PASS** | `SurfaceExposeeProductionTests` |
| AUTHZ-07 | Appel d'API authentifié sans droit | 403 | — | **PASS (code)** | `OnRedirectToAccessDenied` → 403 sur `/api` |

---

## RBAC — champ par champ (SEC-02)

| ID | Attaque | Attendu | Réel | État | Preuve |
|:---|:---|:---|:---|:---|:---|
| RBAC-01 | Lecture d'un champ restreint via l'API | `null` | `null` | **PASS** | `SecCert02RbacChampTests` |
| RBAC-02 | Champ restreint dans un export CSV | `[RESTREINT]` | `[RESTREINT]` | **PASS** | `SecCert02RbacChampTests` |
| RBAC-03 | Mutation d'un champ restreint | 403 | 403 | **PASS** | `SecCert02RbacChampTests` |
| RBAC-04 | Rôle inconnu | Fail-secure | Fail-secure | **PASS** | `SecCert02RbacChampTests` |

---

## UPLOAD / FILES — dépôt documentaire

| ID | Attaque | Attendu | Réel | État | Preuve |
|:---|:---|:---|:---|:---|:---|
| FILE-01 | Traversée de répertoire à la **lecture** | Refus | Refus | **PASS** | `StockageDocumentsSecuriteTests` |
| FILE-02 | Clé de stockage absolue | Refus | Refus | **PASS** | `StockageDocumentsSecuriteTests` |
| FILE-03 | Traversée dans le nom déposé | Neutralisée | Neutralisée | **PASS** | `StockageDocumentsSecuriteTests` |
| FILE-04 | Extension interdite | Refus | Refus | **PASS** | `ControleDepotTests` |
| FILE-05 | Fichier au-delà de la taille max | Refus | Refus | **PASS** | `ControleDepotTests` |
| FILE-06 | Antivirus indisponible | **Fail-closed** | `Infecte` | **PASS** | `ScannerAntivirusDefender` — tous chemins d'erreur |
| FILE-07 | Dépassement du délai antivirus | Fail-closed | `Infecte` | **PASS** | idem |
| FILE-08 | Implémentation antivirus fail-open enregistrée | Jamais | Jamais | **PASS** | `StockageDocumentsSecuriteTests` |
| FILE-09 | Stockage hors `wwwroot` | Hors racine web | Hors racine | **PASS** | `StockageSecuriseTests` |
| FILE-10 | **Fichier EICAR réel** | Refusé | — | **DEFERRED** | Exige un antivirus actif — `docs/59 § 4` |

---

## INPUT — injections

| ID | Attaque | Attendu | Réel | État | Preuve |
|:---|:---|:---|:---|:---|:---|
| INJ-01 | Injection SQL | Impossible | Aucun SQL construit depuis une saisie | **PASS (code)** | Balayage : 0 `FromSqlRaw` / `ExecuteSqlRaw` sur entrée |
| INJ-02 | XSS par HTML brut | Impossible | 0 `MarkupString`, 0 `Html.Raw` | **PASS (code)** | Balayage exhaustif |
| INJ-03 | Redirection ouverte | Impossible | Aucun paramètre de redirection | **PASS (code)** | 0 usage de `returnUrl` |
| INJ-04 | Injection de formule CSV | Neutralisée | — | **PASS (code)** | Exports contrôlés — `SecCert02RbacChampTests` |

---

## HEADERS / SESSION / API

| ID | Attaque | Attendu | Réel | État | Preuve |
|:---|:---|:---|:---|:---|:---|
| HDR-01 | Absence d'en-têtes de défense | Présents | Présents | **PASS** | `SecCert04TransportTests` |
| HDR-02 | CSP autorisant `unsafe-eval` | Refusé | Retiré | **PASS** | `EntetesSecuriteDurciesTests` |
| HDR-03 | CSP dupliquée / malformée | Unique | Unique | **PASS** | `EntetesSecuriteDurciesTests` |
| HDR-04 | Absence de `object-src`, `base-uri`, `form-action` | Présents | Présents | **PASS** | `EntetesSecuriteDurciesTests` |
| HDR-05 | Fonctionnalités navigateur non restreintes | Refusées | Refusées | **PASS** | `EntetesSecuriteDurciesTests` |
| HDR-06 | Page authentifiée mise en cache | `no-store` | `no-store` sur HTML | **PASS (code)** | `SecurityHeadersMiddleware` |
| HDR-07 | HSTS hors développement | Actif | Actif | **PASS** | `SecCert04TransportTests` |
| API-01 | Description OpenAPI exposée hors développement | 404 | 404 | **PASS** | `SurfaceExposeeProductionTests` |
| CSRF-01 | Soumission sans jeton antiforgery | Refus | — | **PASS (code)** | `UseAntiforgery` global |
| CORS-01 | Origine tierce | Refus | Aucun CORS configuré | **PASS (code)** | 0 `UseCors` |

---

## SECRETS / CONFIGURATION

| ID | Contrôle | Attendu | Réel | État | Preuve |
|:---|:---|:---|:---|:---|:---|
| SEC-01 | Secret en dur dans les sources | 0 | 0 | **PASS** | `SEED-SEC-01` + balayage |
| SEC-02 | Repli sur un secret quand la configuration manque | Jamais | Jamais | **PASS** | `SEED-SEC-04` |
| SEC-03 | Purge destructive par défaut | Refusée | Refusée | **PASS** | `SEED-SAFE-01..05` |
| SEC-04 | Stockage documentaire non configuré hors développement | Erreur explicite | Erreur explicite | **PASS** | `StockageDocumentsSecuriteTests` |
| SEC-05 | Réessai de notification non configuré | Erreur explicite | Erreur explicite | **PASS** | `ErrCert01ConfigurationRepriseTests` |
| SEC-06 | Journalisation de latence divulguant la requête | Jamais | Jamais | **PASS** | `JournalisationLatenceTests` |
| SEC-07 | Ressource externe chargée | Aucune | Aucune | **PASS** | `AucuneRessourceExterneTests` |
| SEC-08 | Base de développement atteinte par les tests | Impossible | Impossible | **PASS** | `TEST-DB-01..05` |

---

## RATE LIMITING

| ID | Contrôle | État |
|:---|:---|:---|
| RATE-01 | Verrouillage de compte après 5 échecs, 15 min | **PASS** — seule protection technique existante |
| RATE-02 | Limitation de débit sur `/comptes/mot-de-passe-oublie` | **OPEN** — aucune ; seuil à arbitrer |
| RATE-03 | Limitation de débit sur l'API | **OPEN** — aucune ; seuil à arbitrer |
| RATE-04 | Limitation sur les dépôts de fichiers | **OPEN** — aucune |

Aucune limite n'est inventée : ces seuils sont des décisions métier / DSI (`docs/82`).

---

## Synthèse

| Catégorie | PASS | PASS (code) | DEFERRED | OPEN |
|:---|---:|---:|---:|---:|
| AUTH | 11 | 1 | 0 | 0 |
| AUTHZ / TENANT | 6 | 1 | 0 | 0 |
| RBAC | 4 | 0 | 0 | 0 |
| FILES | 9 | 0 | 1 | 0 |
| INPUT | 0 | 4 | 0 | 0 |
| HEADERS / API | 7 | 3 | 0 | 0 |
| SECRETS / CONFIG | 8 | 0 | 0 | 0 |
| RATE LIMIT | 1 | 0 | 0 | 3 |
| **Total** | **46** | **9** | **1** | **3** |

**Aucun `OPEN` n'est un défaut technique non corrigé** : les trois concernent des seuils que le
projet refuse d'inventer. Le seul `DEFERRED` exige un antivirus actif sur un serveur.
