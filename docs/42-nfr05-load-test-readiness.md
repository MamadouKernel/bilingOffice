# Préparation des tests de charge — NFR-05

> **Document** : `docs/42-nfr05-load-test-readiness.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Priorité corrigée** : **NFR-05 = Must** (les documents antérieurs indiquaient « Should »).
> **Statut** : `BLOCKED_DSI` — `PC-01` OPEN.

---

## 1. Ce que le CDC exige

> **NFR-05 (Must)** — « Volumétrie dimensionnante **à confirmer en phase de cadrage** : nombre de
> [...] »

L'exigence contient sa propre condition : la volumétrie **doit être confirmée**. Elle n'est pas
définie par le CDC, qui renvoie explicitement à un cadrage.

**C'est ce qui rend NFR-05 non spécifiable aujourd'hui** — et non un manque de développement.

---

## 2. Pourquoi rien ne peut être décidé ici

Un tir de charge produit toujours un chiffre. La question est de savoir **contre quoi** ce chiffre
se compare.

Sans volumétrie cible, un tir à 50 utilisateurs simultanés donnera un temps de réponse — mais
personne ne pourra dire s'il démontre l'exigence ou s'il mesure une charge sans rapport avec la
réalité d'exploitation. **Le résultat serait techniquement exact et contractuellement inutile.**

Inventer une volumétrie reviendrait à se fixer soi-même l'objectif à atteindre.

---

## 3. Modèle de charge paramétrable

Structure prête ; **valeurs à fournir**.

| Paramètre | Rôle | Valeur |
|:---|:---|:---|
| Utilisateurs nommés | Population totale | `TBD_DSI` — PC-01 |
| Utilisateurs simultanés en pointe | Dimensionne le tir | `TBD_DSI` — PC-01 |
| Tickets créés par jour | Charge d'écriture | `TBD_DSI` — PC-01 |
| Documents déposés par jour | Charge de stockage et d'antivirus | `TBD_DSI` — PC-01 |
| Taille moyenne d'un document | Bande passante et stockage | `TBD_DSI` — PC-01 |
| Taille maximale d'un document | Dimensionne les limites de requête | `TBD_DSI` — PC-01 |
| Requêtes par seconde en pointe | Dimensionne le tir | `TBD_DSI` — PC-01 |
| Facteur de pointe | Rapport pointe / moyenne | `TBD_DSI` — PC-01 |
| Horizon de croissance | Marge de dimensionnement | `TBD_DSI` — PC-01 |
| Durée du palier | Protocole | `TBD_DSI` |
| Montée en charge | Protocole | `TBD_DSI` |

---

## 4. Surfaces à solliciter

Déduites des parcours réels, indépendamment de la volumétrie.

| Surface | Type | Pourquoi elle compte |
|:---|:---|:---|
| Connexion | Écriture | Point de passage obligé, coûteux (hachage) |
| Tableau de bord client | Lecture | Écran le plus consulté |
| Liste des demandes | Lecture paginée | Sensible au volume |
| Fiche de ticket | Lecture composite | Agrège messages, documents, historique |
| Dépôt de document | Écriture + stockage + antivirus | **Chemin le plus coûteux** |
| File de traitement agent | Lecture filtrée | Sensible aux index |
| Soumission de demande | Écriture transactionnelle | Génère numérotation et événement |
| Recherche guichet | Lecture indexée | Attendue en temps réel |
| API tickets et dossiers | Lecture | Peut être appelée par des tiers |

**Le dépôt de document est le chemin critique** : il cumule transfert réseau, écriture disque,
analyse antivirus et écriture en base. C'est là que la charge se révèle.

---

## 5. Métriques à collecter

| Côté | Métrique | Source |
|:---|:---|:---|
| Client | `http_req_duration` — p50, p90, **p95**, p99 | k6 |
| Client | `http_req_failed` | k6 |
| Client | Débit effectif | k6 |
| Serveur | Temps applicatif | En-tête `Server-Timing` |
| Serveur | CPU, mémoire | Supervision — `TBD_INFRA` |
| Base | Attentes, verrous, requêtes lentes | SQL Server — `TBD_INFRA` |
| Stockage | Débit disque, latence | Supervision — `TBD_INFRA` |
| Antivirus | Durée d'analyse | Journaux applicatifs |

**Mesurer côté client seul ne suffit pas** : un temps de réponse dégradé ne dit pas si la cause
est la base, le disque ou l'antivirus. Sans les métriques serveur, un échec ne serait pas
diagnosticable.

---

## 6. Critères PASS / FAIL

| Critère | Seuil | Source |
|:---|:---|:---|
| `p95` du temps de réponse | **< 3000 ms** | CDC — NFR-04, explicite |
| Taux d'erreur | `TBD_DSI` | **Non fixé par le CDC** |
| Charge tenue | `TBD_DSI` — PC-01 | **Non fixée par le CDC** |
| Saturation ressources | `TBD_INFRA` | Dépend du dimensionnement |

Un seul seuil est contractuel. Les autres **doivent venir de l'arbitrage** — les poser ici
reviendrait à définir soi-même le niveau de service.

---

## 7. Ce qui existe déjà

| Élément | État |
|:---|:---|
| Script k6 | Livré — `specs/016-.../evidence/scripts/benchmark-k6.js` |
| Instrumentation serveur | `MetrologiePerformanceMiddleware`, en-tête `Server-Timing` |
| Sondes | `/healthz`, `/ready` |
| Runbook de campagne | `docs/38` |

**Aucun développement n'est nécessaire.** Le script devra être étendu aux surfaces du § 4 une fois
le profil connu — travail de configuration, non de développement.

---

## 8. Verdict

**NFR-05 : `BLOCKED_DSI` — `PC-01` OPEN.**

L'exigence est **Must**, et sa correction de priorité importe : elle n'est pas optionnelle.

Le blocage n'est pas technique. Il tient à une information métier que seule la DSI, avec les
équipes du Billing Office, peut fournir. Une piste : les **volumes du guichet physique actuel**
sont observables et constituent la meilleure approximation disponible.
