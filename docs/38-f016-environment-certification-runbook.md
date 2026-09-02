# Runbook de certification environnementale F016 — NFR-03 · NFR-04 · NFR-10

> **Document** : `docs/37-f016-environment-certification-runbook.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Statut de la campagne** : **NON EXÉCUTÉE**
> **Blocage** : `BLOCKED_ENVIRONMENT` — aucun environnement Staging/UAT n'est disponible.

---

## 1. Ce qui est déjà livré

F016 a livré l'instrumentation. Vérifié dans le dépôt :

| Artefact | Emplacement | État |
|:---|:---|:---|
| Sonde de vivacité | `/healthz` (`Program.cs`) | Livrée |
| Sonde de disponibilité | `/ready` (`Program.cs`) | Livrée |
| Mesure de temps de réponse | `Middlewares/MetrologiePerformanceMiddleware.cs` | Livrée |
| Mode maintenance | `Middlewares/MaintenanceMiddleware.cs` | Livré |
| Calcul de disponibilité | `Application/Supervision/CalculateurDisponibilite.cs` | Livré |
| Script de charge | `specs/016-.../evidence/scripts/benchmark-k6.js` | Livré |

Les trois rapports de preuve existent et portent la mention **`NOT EXECUTED`** :

- `evidence/availability-report.md`
- `evidence/performance-report.md`
- `evidence/continuous-access-report.md`

**Ils doivent le rester** tant qu'aucune campagne réelle n'a eu lieu. Un rapport rempli depuis un
poste de développement ne certifie rien : il mesure une machine de développement, pas la cible.

---

## 2. Pourquoi la certification ne peut pas se faire ici

| Exigence | Ce qui est prouvé | Ce qui manque |
|:---|:---|:---|
| **NFR-03** — disponibilité ≥ 99 % en heures ouvrées | La sonde existe et calcule | Une **fenêtre d'observation réelle** sur l'environnement cible |
| **NFR-04** — temps de réponse < 3 s | La mesure est instrumentée et exposée | Un **tir de charge** sur une infrastructure représentative |
| **NFR-10** — accessibilité 24/7 et résilience | Le mode dégradé et la maintenance sont livrés | Un **tir d'endurance** et une observation continue |

Ces trois exigences ne se démontrent pas par du code : elles se démontrent par la **mesure d'un
système déployé**. C'est la raison — et la seule — pour laquelle elles ne sont pas certifiées.

---

## 3. Prérequis avant lancement

Aucun de ces éléments ne peut être fourni par l'équipe de développement.

| Prérequis | Classement | Détenteur |
|:---|:---|:---|
| URL de l'environnement Staging | `BLOCKED_ENVIRONMENT` | DSI / Infrastructure |
| Certificat TLS valide sur cette URL | `NEEDS_CERTIFICATE` | Infrastructure |
| Base de données de recette peuplée | `NEEDS_DATABASE` | DSI / Exploitation |
| Comptes de test avec habilitations représentatives | `NEEDS_DSI_VALUE` | Administration CIT |
| Fenêtre de tir autorisée (créneau, durée) | `BLOCKED_DSI` | DSI / Exploitation |
| Autorisation de générer de la charge | `BLOCKED_DSI` | DSI / Sécurité réseau |
| Profil de charge cible | `BLOCKED_DSI` — voir **PC-01** | DSI métier |
| Accès aux journaux serveur pendant la campagne | `NEEDS_INFRA_VALUE` | Exploitation |

**Le profil de charge (PC-01) conditionne NFR-04.** Sans volumétrie cible arrêtée, un tir k6
mesure une charge arbitraire : le résultat ne prouve rien contre l'exigence.

---

## 4. Procédure de campagne

### 4.1 Variables à renseigner

À définir **avant** le tir, jamais pendant :

```
BASE_URL              = <TBD_ENVIRONMENT>      # URL Staging, sans barre finale
VUS                   = <TBD_DSI_PC01>         # utilisateurs virtuels simultanés
DUREE                 = <TBD_DSI_PC01>         # durée du palier
RAMP_UP               = <TBD_DSI_PC01>         # montée en charge
SEUIL_P95_MS          = 3000                   # NFR-04, valeur contractuelle
SEUIL_TAUX_ERREUR     = <TBD_DSI>              # non fixé par le CDC
FENETRE_OBSERVATION   = <TBD_DSI>              # NFR-03, heures ouvrées
```

Seul `SEUIL_P95_MS` est dérivé du CDC (« temps de réponse < 3 secondes »). Les autres valeurs
**ne doivent pas être inventées** : elles proviennent de l'arbitrage PC-01.

### 4.2 Étape 1 — Vérification préalable des sondes

```bash
curl -sS -o /dev/null -w "healthz=%{http_code} en %{time_total}s\n" "$BASE_URL/healthz"
```

```bash
curl -sS -o /dev/null -w "ready=%{http_code} en %{time_total}s\n" "$BASE_URL/ready"
```

**Critère de poursuite** : les deux répondent `200`. Sinon, la campagne s'arrête — mesurer un
système qui ne se déclare pas prêt n'a pas de sens.

### 4.3 Étape 2 — Vérification de l'instrumentation

```bash
curl -sS -D - -o /dev/null "$BASE_URL/" | grep -i "server-timing"
```

**Critère** : l'en-tête `Server-Timing` est présent. C'est lui que la campagne exploite pour
NFR-04 ; son absence invaliderait la mesure côté serveur.

### 4.4 Étape 3 — Tir de charge (NFR-04)

```bash
k6 run --env BASE_URL="$BASE_URL" --vus "$VUS" --duration "$DUREE" specs/016-disponibilite-performance-resilience/evidence/scripts/benchmark-k6.js
```

Collecter : `http_req_duration` (p50, p90, **p95**, p99), `http_req_failed`, `iterations`,
`vus_max`, et la durée effective.

### 4.5 Étape 4 — Observation de disponibilité (NFR-03)

Sondage périodique de `/ready` sur la fenêtre convenue, puis calcul du ratio de disponibilité.
La fenêtre doit couvrir des **heures ouvrées réelles** : l'exigence porte sur elles, pas sur une
moyenne de 24 heures.

### 4.6 Étape 5 — Endurance et résilience (NFR-10)

Tir prolongé, plus bascule volontaire en **mode maintenance** puis retour au nominal, afin de
vérifier que l'indisponibilité annoncée est propre et que le retour est complet.

---

## 5. Critères PASS / FAIL

| Exigence | Critère | Source du seuil |
|:---|:---|:---|
| **NFR-04** | `p95(http_req_duration)` **< 3000 ms** sur les écrans courants | CDC, explicite |
| **NFR-04** | `http_req_failed` ≤ `<TBD_DSI>` | **Non fixé par le CDC** |
| **NFR-03** | Disponibilité mesurée **≥ 99 %** sur la fenêtre d'heures ouvrées | CDC, explicite |
| **NFR-10** | Aucune indisponibilité non annoncée ; retour au nominal complet | CDC, qualitatif |

Un critère marqué `<TBD_DSI>` **ne doit pas être choisi par l'équipe technique** : le fixer
reviendrait à définir soi-même le niveau de service à atteindre.

---

## 6. Consignation des résultats

Renseigner les trois rapports existants en remplaçant `NOT EXECUTED` par les mesures réelles, et
y joindre systématiquement :

- horodatage de début et de fin ;
- URL et version déployée (**commit**) ;
- valeurs des variables de la § 4.1 ;
- sortie brute de k6 ;
- verdict par exigence, avec la mesure qui le fonde.

**Sans le commit déployé, un rapport n'est pas opposable** : on ne saurait pas ce qui a été mesuré.

---

## 7. Reprise et retour arrière

| Situation | Conduite |
|:---|:---|
| Sondes KO avant tir | Arrêter. Ne rien consigner : il n'y a pas eu de campagne. |
| Tir interrompu | Rejouer intégralement. Une campagne partielle ne se recolle pas. |
| Seuil non atteint | Consigner **FAIL** avec la mesure. Ne pas relancer jusqu'à obtenir un bon chiffre. |
| Impact sur l'environnement | Bascule en mode maintenance, puis retour au nominal. |

Le retour arrière est trivial : cette campagne est **en lecture seule**. Elle ne modifie ni le
schéma, ni les données, ni la configuration. Aucun rollback applicatif n'est requis.

---

## 8. Verdict

**F016 Environment Certification Gate : NON FRANCHISSABLE en l'état.**

Ce n'est pas une insuffisance de développement. Les trois exigences sont techniquement livrées ;
il manque un environnement à mesurer et une volumétrie cible. La campagne est **prête à être
lancée le jour où l'URL Staging et PC-01 seront fournis**, sans développement supplémentaire.
