# Checklist d'exécution de la certification F016

> **Document** : `docs/60-f016-certification-execution-checklist.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Résultat de certification** : **`NOT EXECUTED`**
> **Complément opérationnel de** `docs/38`, qui porte le runbook complet.

---

## 1. Artefacts disponibles — vérifiés dans le dépôt

| Artefact | Emplacement | État |
|:---|:---|:---|
| Sonde de vivacité | `/healthz` | Livrée |
| Sonde de disponibilité | `/ready` | Livrée |
| Métrologie de performance | `Middlewares/MetrologiePerformanceMiddleware.cs` | Livrée |
| En-tête `Server-Timing` | Émis par le middleware | Livré |
| Mode maintenance | `Middlewares/MaintenanceMiddleware.cs` | Livré |
| Calcul de disponibilité | `Application/Supervision/CalculateurDisponibilite.cs` | Livré |
| Script de charge | `specs/016-.../evidence/scripts/benchmark-k6.js` | Livré |

**Aucun développement ne manque.** Ce qui manque est un environnement à mesurer.

---

## 2. Les trois rapports doivent rester en l'état

| Rapport | État actuel |
|:---|:---|
| `evidence/availability-report.md` | **`NOT EXECUTED`** |
| `evidence/performance-report.md` | **`NOT EXECUTED`** |
| `evidence/continuous-access-report.md` | **`NOT EXECUTED`** |

Ils ne doivent être renseignés qu'après une campagne réelle. Une mesure prise depuis un poste de
développement mesurerait ce poste, pas la cible — et un rapport rempli sans campagne serait une
preuve fabriquée.

---

## A. NFR-03 — Disponibilité ≥ 99 %

### Ce que le CDC demande

> Disponibilité cible **en heures ouvrées** du Billing Office ; objectif **≥ 99 %**.

L'exigence porte sur les **heures ouvrées**, pas sur une moyenne 24 heures. Une fenêtre mal choisie
invaliderait la mesure.

### Paramètres

| Paramètre | Valeur |
|:---|:---|
| Fenêtre d'observation | `TBD_DSI` — doit couvrir des heures ouvrées réelles |
| Définition des heures ouvrées | `TBD_DSI` |
| Fréquence de sondage | `TBD_INFRA` — recommandé : 1 minute |
| Sonde utilisée | **`/ready`**, non `/healthz` |
| Seuil | **≥ 99 %** — contractuel |

**Pourquoi `/ready`.** `/healthz` peut répondre `200` alors que la base est inatteignable :
l'application vit mais ne sert pas. Mesurer la disponibilité sur `/healthz` surestimerait le
résultat.

### Commande

```bash
curl -sS -o /dev/null -w "%{http_code} %{time_total}\n" "$BASE_URL/ready"
```

À exécuter périodiquement sur la fenêtre, en horodatant chaque relevé.

### Formule

```
Disponibilité = sondages en 200 / sondages totaux, sur les heures ouvrées
```

**Les indisponibilités planifiées et annoncées** — mode maintenance — sont à consigner
séparément : leur traitement dans le calcul relève d'une décision, `TBD_DSI`.

### Critère

| Résultat | Verdict |
|:---|:---|
| ≥ 99 % | **PASS** |
| < 99 % | **FAIL** — consigner la mesure, ne pas relancer jusqu'à obtenir un bon chiffre |

---

## B. NFR-04 — Temps de réponse < 3 s

### Ce que le CDC demande

> Temps de réponse **< 3 secondes** pour les écrans courants dans des conditions [nominales].

### Prérequis d'exécution

| Élément | Valeur |
|:---|:---|
| k6 installé | `TBD_INFRA` |
| `BASE_URL` | `TBD_INFRA` |
| Comptes de test avec habilitations | `TBD_UAT` |
| Jeu de données représentatif | `TBD_UAT` |
| Profil de charge | **`TBD_DSI` — PC-01** |
| Autorisation de générer de la charge | `TBD_DSI` |

**Le profil de charge est bloquant.** Sans volumétrie cible, un tir mesure une charge arbitraire :
le chiffre obtenu serait exact et sans valeur probante.

### Surfaces à couvrir

Les « écrans courants » au sens du CDC :

| Surface | Justification |
|:---|:---|
| Connexion | Point de passage obligé, coûteux (hachage) |
| Tableau de bord client | Écran le plus consulté |
| Liste des demandes | Sensible au volume |
| Fiche de ticket | Composite : messages, documents, historique |
| File de traitement agent | Sensible aux index |
| **Dépôt de document** | **Chemin le plus coûteux** : réseau, disque, antivirus, base |

### Commande

```bash
k6 run --env BASE_URL="$BASE_URL" specs/016-disponibilite-performance-resilience/evidence/scripts/benchmark-k6.js
```

### Métriques

Côté client : `http_req_duration` (p50, p90, **p95**, p99), `http_req_failed`, `iterations`.
Côté serveur : `Server-Timing`, CPU, mémoire, attentes SQL.

**Mesurer côté client seul ne suffit pas** : un temps dégradé ne dirait pas si la cause est la
base, le disque ou l'antivirus, et un échec ne serait pas diagnosticable.

### Critère

| Critère | Seuil | Source |
|:---|:---|:---|
| `p95(http_req_duration)` | **< 3000 ms** | CDC, explicite |
| Taux d'erreur | `TBD_DSI` | **Non fixé par le CDC** |

---

## C. NFR-10 — Accessibilité 24/7 et résilience

### Ce que le CDC demande

> Disponibilité du portail client : la plateforme est accessible **24h/24**.

### Contrôles

| # | Contrôle | Attendu |
|---:|:---|:---|
| 1 | Sondage **hors heures ouvrées** | `/ready` répond `200` |
| 2 | Tir d'endurance prolongé | Aucune dégradation progressive, aucune fuite mémoire |
| 3 | Activation du mode maintenance | Page d'indisponibilité, en-tête **`Retry-After`** |
| 4 | Sortie du mode maintenance | **Retour au nominal complet** |
| 5 | Comportement après redémarrage | Reprise sans intervention |

**Le point 4 compte autant que le 3.** Une bascule en maintenance qui ne revient pas proprement au
nominal transforme une opération planifiée en incident.

### Paramètres

| Paramètre | Valeur |
|:---|:---|
| Durée du tir d'endurance | `TBD_DSI` |
| Créneau hors heures ouvrées | `TBD_DSI` |
| Seuil de dérive acceptable | `TBD_DSI` |

---

## 3. Consignation

Renseigner les trois rapports en remplaçant `NOT EXECUTED` par les mesures, en joignant :

- horodatages de début et de fin ;
- **commit déployé** ;
- valeurs de tous les paramètres `TBD` effectivement retenus ;
- sortie brute de k6 ;
- verdict par exigence, avec la mesure qui le fonde.

---

## 4. Verdict

**`ENVIRONMENT CERTIFICATION RESULT = NOT EXECUTED`**

Trois exigences **Must** attendent une mesure, pas un développement. C'est le levier au meilleur
rapport valeur / effort du projet : **+3 exigences sans une ligne de code**.
