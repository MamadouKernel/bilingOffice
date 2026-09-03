# Pack de fumée et de non-régression — exécutable après déploiement

> **Document** : `docs/80-uat-smoke-regression-pack.md`
> **Établi le** : 2026-09-03
> **Durée visée** : **moins de 20 minutes**. Au-delà, ce n'est plus un pack de fumée.
> **Complète** `docs/59`, qui porte le plan détaillé en 24 tests.

---

## 1. Usage

À jouer **immédiatement après chaque déploiement**, avant réouverture du service. Il répond à
« le déploiement a-t-il abouti », pas à « le produit est-il conforme ».

**Un échec bloquant arrête la séquence** : les tests suivants n'auraient plus de sens.

---

## 2. Bloc A — Vie de l'application (2 min)

| # | Contrôle | Attendu | Bloquant |
|---:|:---|:---|:---|
| A1 | `GET /healthz` | `200` | **Oui** |
| A2 | `GET /ready` | `200`, quatre contrôles `Healthy` | **Oui** |
| A3 | Journal de démarrage | Aucune erreur ; l'amorçage annonce sa décision | **Oui** |
| A4 | `GET /swagger/v1/swagger.json` | **`404`** hors développement | **Oui** |

```bash
curl -sS -o /dev/null -w "healthz %{http_code}\n" "$BASE_URL/healthz"; curl -sS "$BASE_URL/ready" | head -c 400; echo; curl -sS -o /dev/null -w "openapi %{http_code}\n" "$BASE_URL/swagger/v1/swagger.json"
```

**A4 est un contrôle de sécurité** : un `200` signifierait que la surface d'API est publiée.

---

## 3. Bloc B — Transport et en-têtes (2 min)

| # | Contrôle | Attendu | Bloquant |
|---:|:---|:---|:---|
| B1 | Redirection HTTP → HTTPS | `301` / `308` | **Oui** |
| B2 | `Strict-Transport-Security` | Présent | **Oui** |
| B3 | `X-Content-Type-Options` | `nosniff` | **Oui** |
| B4 | `X-Frame-Options` | `SAMEORIGIN` | **Oui** |
| B5 | `Content-Security-Policy` | **Une seule**, sans `unsafe-eval` | **Oui** |
| B6 | `Permissions-Policy` | Présent | Non |
| B7 | `Server-Timing` | Présent | Non |

```bash
curl -sSI "$BASE_URL/comptes/connexion" | grep -iE "strict-transport|content-security|x-frame|x-content-type|permissions-policy|server-timing"
```

**B5 : vérifier qu'il n'y a qu'une ligne `content-security-policy`.** Deux en-têtes signaleraient
une régression du middleware.

---

## 4. Bloc C — Authentification (5 min)

| # | Contrôle | Attendu | Bloquant |
|---:|:---|:---|:---|
| C1 | Page de connexion accessible | `200` | **Oui** |
| C2 | Connexion d'un compte client | Session ouverte | **Oui** |
| C3 | Connexion avec un mauvais secret | Refus, message générique | **Oui** |
| C4 | Bascule afficher / masquer du mot de passe | Fonctionne, re-masque à la soumission | Non |
| C5 | Formulaire d'oubli, adresse **existante** | Message générique, **aucun lien affiché** | **Oui** |
| C6 | Formulaire d'oubli, adresse **inexistante** | **Réponse identique à C5** | **Oui** |
| C7 | Déconnexion | Session invalidée | **Oui** |

**C5 et C6 ensemble** constituent le contrôle : c'est leur *identité* qui prouve l'absence d'oracle
d'énumération. Les jouer séparément ne prouve rien.

---

## 5. Bloc D — Métier et cloisonnement (7 min)

| # | Contrôle | Attendu | Bloquant |
|---:|:---|:---|:---|
| D1 | Tableau de bord client | Données affichées | **Oui** |
| D2 | Consultation d'un dossier | Fiche affichée | **Oui** |
| D3 | Consultation d'un ticket | Historique et messages | **Oui** |
| D4 | Dépôt d'un document **sain** | Accepté, puis **téléchargeable** | **Oui** |
| D5 | Dépôt d'une extension interdite | Refus | **Oui** |
| D6 | **Cloisonnement : ressource d'une autre société** | **Refus** | **Oui** |
| D7 | Connexion d'un compte agent, file visible | Session et file | **Oui** |
| D8 | Compte sans habilitation | **Refus** | **Oui** |

**D6 et D8 ne se contournent jamais.** Leur échec signifie qu'une société voit les données d'une
autre, ou qu'un compte non habilité agit.

---

## 6. Bloc E — API (2 min)

| # | Contrôle | Attendu | Bloquant |
|---:|:---|:---|:---|
| E1 | `GET /api/v1/tickets/{numero}` **non authentifié** | **`401`** — jamais `302`, jamais du HTML | **Oui** |
| E2 | Même appel authentifié | `200` | Oui |

```bash
curl -sS -o /dev/null -w "api anonyme %{http_code}\n" "$BASE_URL/api/v1/tickets/TEST"
```

**Un `302` en E1 serait une régression** : l'API renverrait une page de connexion à un client
programmatique.

---

## 7. Bloc F — Audit et persistance (2 min)

| # | Contrôle | Attendu | Bloquant |
|---:|:---|:---|:---|
| F1 | Journal d'audit après les écritures des blocs C à E | Entrées présentes et nominatives | **Oui** |
| F2 | Document déposé en D4, **relu après redémarrage** | Toujours présent | **Oui** |

**F2 est le contrôle qui détecte un stockage mal configuré.** Un document qui disparaît après
redémarrage signale que la racine documentaire n'est pas persistante — la panne la plus insidieuse
de ce déploiement.

---

## 8. Critères

| Résultat | Conduite |
|:---|:---|
| Tous les bloquants passent | **Réouverture autorisée** |
| Un bloquant échoue | **Retour arrière** — `docs/48 § 10` |
| Un non-bloquant échoue | Réouverture possible, anomalie créée |

---

## 9. Consignation

Horodatage, **commit déployé**, résultat par contrôle, captures des refus attendus (C6, D6, D8, E1).

**Sans le commit déployé, l'exécution n'est rattachable à aucune version.**

**Statut : `NOT EXECUTED`.**
