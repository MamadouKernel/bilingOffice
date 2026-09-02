# Plan de tests de fumée — Staging

> **Document** : `docs/59-staging-smoke-test-plan.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Statut** : **non exécuté** — aucun environnement n'existe. Aucun test n'a été joué contre une
> cible inexistante.

---

## 1. Objet

Vérifier, **immédiatement après un déploiement et avant réouverture du service**, que la
plateforme est fonctionnelle sur ses chemins critiques.

Ce n'est pas une recette : c'est un contrôle de vie. Il répond à « le déploiement a-t-il abouti »,
pas à « le produit est-il conforme ». La recette, c'est `docs/36`.

**Durée visée : moins de 30 minutes.** Au-delà, ce n'est plus un test de fumée.

---

## 2. Prérequis

| Élément | Statut |
|:---|:---|
| Application déployée **et démarrée** | `TBD_INFRA` |
| `Notifications:Reessai` fourni | **Sans lui, l'application ne démarre pas** — `docs/55 § 1` |
| Migrations appliquées | `docs/57` |
| Comptes de test provisionnés | `TBD_UAT` — voir `docs/62` |
| Fichier de test sain (PDF) | À préparer |
| Fichier de test EICAR | **Voir § 4 — procédure encadrée** |

---

## 3. Les tests

Ordre volontaire : du plus fondamental au plus composite. **Un échec arrête la séquence** — les
tests suivants n'auraient pas de sens.

| # | Test | Attendu | Bloquant | Exigence |
|---:|:---|:---|:---|:---|
| 1 | `GET /healthz` | `200` | **Oui** | NFR-03 |
| 2 | `GET /ready` | `200` — atteste l'accès à la base | **Oui** | NFR-03 |
| 3 | En-tête `Server-Timing` présent | Présent | **Oui** | NFR-04 |
| 4 | En-têtes de sécurité | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, CSP | **Oui** | SEC |
| 5 | Redirection HTTP → HTTPS | `301` / `308` | **Oui** | SEC |
| 6 | Page de connexion accessible | `200` | **Oui** | CPT-03 |
| 7 | Connexion d'un compte client | Session ouverte | **Oui** | CPT-03 |
| 8 | Tableau de bord client | Données affichées | **Oui** | SOC |
| 9 | Consultation d'un dossier | Fiche affichée | **Oui** | SOC-02 |
| 10 | Consultation d'un ticket | Historique et messages affichés | **Oui** | SOC-02 |
| 11 | Dépôt d'un document **sain** | Accepté, puis **téléchargeable** | **Oui** | SOC-07 |
| 12 | Dépôt d'un fichier **EICAR** | **Refusé**, non téléchargeable | **Oui** | SEC-07 |
| 13 | Dépôt d'une extension interdite | Refusé | Oui | SOC-07 |
| 14 | Brouillon : saisie, abandon, reprise | Contenu restitué | Non | Brouillons |
| 15 | Connexion d'un compte agent | Session ouverte, file visible | **Oui** | SOC-04 |
| 16 | Cloisonnement : accès direct à une ressource d'une autre société | **Refusé** | **Oui** | SEC-03 |
| 17 | API `/api/v1/tickets/{numero}` authentifiée | `200` | Oui | API |
| 18 | Même appel **non authentifié** | `401` | **Oui** | SEC |
| 19 | Export CSV | Fichier cohérent, encodage correct | Non | EXP |
| 20 | Activation 2FA — QR code | Affiché, **sans appel à un service tiers** | Oui | CPT-07 |
| 21 | Connexion avec second facteur | Code accepté | Oui | CPT-07 |
| 22 | Mode maintenance : activation | Page d'indisponibilité, en-tête `Retry-After` | Oui | NFR-10 |
| 23 | Mode maintenance : sortie | Retour au nominal complet | **Oui** | NFR-10 |
| 24 | Journal d'audit après les écritures | Traces présentes et nominatives | **Oui** | SEC-05 |

---

## 4. Le test EICAR — procédure encadrée

Le test 12 utilise le fichier de test standard **EICAR**, conçu pour cet usage : il n'est pas un
virus, mais tout antivirus le reconnaît comme tel.

**Précautions, non facultatives :**

1. **Prévenir l'exploitation avant le test.** L'antivirus du serveur va lever une alerte ; sans
   préavis, elle sera traitée comme un incident de sécurité réel.
2. Ne pas conserver le fichier sur le serveur après le test.
3. Consigner l'horodatage, afin de rattacher l'alerte au test.

**Ce que le test démontre :** que `FailClosed` fonctionne, et qu'un fichier reconnu comme infecté
est refusé **et reste inaccessible**. C'est le seul moyen de vérifier SEC-07 sans attendre une
vraie infection.

Si l'exploitation refuse ce test, la conformité SEC-07 ne peut pas être démontrée en Staging — à
consigner comme réserve explicite.

---

## 5. Critères

| Résultat | Conduite |
|:---|:---|
| Tous les bloquants passent | **Réouverture du service autorisée** |
| Un bloquant échoue | **Retour arrière** — `docs/48 § 10` |
| Un non-bloquant échoue | Réouverture possible, anomalie créée |

**Aucune tolérance sur les tests 12, 16 et 18.** Ce sont des contrôles de sécurité : leur échec
signifie qu'un fichier infecté peut circuler, qu'une société voit les données d'une autre, ou
qu'une API est ouverte. Aucun ne se contourne.

---

## 6. Preuves à consigner

Pour chaque exécution : horodatage, **commit déployé**, résultat par test, captures des refus
attendus (12, 16, 18), et référence des entrées d'audit vérifiées au test 24.

**Sans le commit déployé, un test de fumée n'est pas rattachable à une version** — et un incident
ultérieur ne pourra pas être relié à ce qui a été vérifié.

---

## 7. Statut

**`NOT EXECUTED`** — aucun environnement Staging n'existe à ce jour.

Les 24 tests sont prêts et exécutables dès qu'une URL et des comptes seront disponibles.
