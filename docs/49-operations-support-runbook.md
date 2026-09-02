# Runbook d'exploitation et de support

> **Document** : `docs/49-operations-support-runbook.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Portée** : ce que l'application expose et comment l'exploiter. L'organisation du support et
> l'outillage de supervision relèvent de la DSI — `TBD_DSI`.

---

## 1. Points d'observation exposés par l'application

| Point | Ce qu'il dit | Interprétation d'un échec |
|:---|:---|:---|
| `GET /healthz` | Le processus répond | Application arrêtée, plantée, ou saturée |
| `GET /ready` | L'application **et ses dépendances** sont prêtes | Base inatteignable, ou dépendance en défaut |
| En-tête `Server-Timing` | Temps applicatif de la requête | Dégradation de performance |
| Journal d'audit | Traces des actions sensibles | Une chute du volume signale un dysfonctionnement silencieux |
| Journaux applicatifs | Erreurs et avertissements | — |

**Distinction essentielle.** `/healthz` peut répondre `200` alors que `/ready` échoue : le
processus vit, mais ne peut pas servir. C'est exactement le cas d'une base indisponible. Superviser
`/healthz` seul masquerait cette situation.

---

## 2. Que surveiller

| Objet | Seuil | Sévérité | Valeur |
|:---|:---|:---|:---|
| `/healthz` non `200` | Immédiat | **P1** | — |
| `/ready` non `200` | Immédiat | **P1** | — |
| Taux d'erreur HTTP 5xx | `TBD_DSI` | P1 | — |
| Temps de réponse p95 | > 3000 ms — NFR-04 | P2 | Contractuel |
| Espace disque — stockage documentaire | `TBD_INFRA` | P2 | — |
| Espace disque — base et journal | `TBD_INFRA` | P1 | — |
| Erreurs d'analyse antivirus | Toute occurrence répétée | P2 | — |
| Échecs de connexion | Pic anormal | P2 | Possible attaque |
| Volume d'écriture au journal d'audit | Chute brutale | P2 | Dysfonctionnement silencieux |
| Traitements de fond | Absence d'exécution | P2 | Notifications non distribuées |

---

## 3. Mode maintenance

**Quand l'utiliser** : migration de schéma, déploiement, intervention sur la base ou le stockage.

**Comportement** : page d'indisponibilité, en-tête `Retry-After`, aucune écriture acceptée.

**Ce qu'il ne fait pas** : il n'interrompt pas les traitements de fond en cours. Une intervention
sur la base suppose de vérifier qu'aucun traitement n'écrit — sans quoi la maintenance ne protège
de rien.

---

## 4. Diagnostics

### 4.1 L'application ne répond plus

1. `/healthz` — si KO, le processus est en cause : journaux, mémoire, redémarrage.
2. Si `/healthz` est OK et `/ready` KO : la dépendance est en cause, presque toujours la base.
3. Vérifier la connectivité et l'espace disque de la base.
4. Vérifier l'espace disque du serveur applicatif.

### 4.2 Lenteurs

1. Comparer le `Server-Timing` au temps total observé côté client — l'écart isole le réseau.
2. Si le temps applicatif est élevé : examiner les requêtes lentes côté base.
3. Vérifier l'occupation CPU et mémoire.
4. Vérifier si un dépôt de document volumineux est en cours — le chemin le plus coûteux.

### 4.3 Un utilisateur ne peut pas se connecter

Dans cet ordre, du plus fréquent au plus rare :

1. Compte verrouillé — cinq tentatives, quinze minutes.
2. Compte **désactivé** — `EstActif` à faux.
3. Compte client dont la **société n'est pas validée**, ou rattachement désactivé.
4. Second facteur activé et code invalide.

**Le message affiché est volontairement générique** et ne distingue pas ces cas : c'est un choix
de sécurité. Le diagnostic se fait donc côté exploitation, jamais depuis l'écran.

### 4.4 Un document n'est pas accessible

1. Vérifier le statut antivirus — un document non déclaré sain n'est pas accessible.
2. Vérifier la présence physique du fichier dans le stockage.
3. Vérifier les permissions du compte de service sur le stockage.

### 4.5 Notifications non reçues

1. Vérifier que l'événement a bien été publié.
2. Vérifier que le traitement de fond de distribution s'exécute.
3. Vérifier qu'une règle de diffusion active existe pour ce type d'événement et ce canal.
4. Vérifier la préférence de canal du destinataire.

---

## 5. Niveaux de support

L'organisation relève de la DSI — `TBD_DSI`. Répartition proposée, à valider :

| Niveau | Périmètre | Compétence |
|:---|:---|:---|
| **N1** | Accueil, qualification, cas connus : verrouillage, mot de passe, droits | Fonctionnelle |
| **N2** | Diagnostic technique, journaux, base, stockage, supervision | Exploitation |
| **N3** | Anomalie applicative, correction de code | Développement |

**Critère d'escalade N1 → N2** : le cas n'est pas dans les diagnostics du § 4, ou le contournement
n'a pas fonctionné.

**Critère d'escalade N2 → N3** : le comportement observé contredit la spécification, ou une
exception applicative non gérée apparaît.

---

## 6. Classification des incidents

| Niveau | Définition | Exemple |
|:---|:---|:---|
| **P1** | Service indisponible, ou fuite de données | `/ready` KO ; un client voit les dossiers d'un autre |
| **P2** | Fonction majeure dégradée | Dépôt de documents en échec ; notifications non distribuées |
| **P3** | Gêne contournable | Un export échoue sur un cas précis |
| **P4** | Cosmétique | Libellé erroné |

**Toute suspicion de fuite de données est P1**, même si elle paraît limitée : le cloisonnement est
un invariant de sécurité, pas une fonctionnalité.

---

## 7. Collecte de preuves lors d'un incident

À rassembler **avant** toute tentative de correction :

- horodatage précis et fuseau ;
- **commit déployé** ;
- journaux applicatifs sur la fenêtre ;
- réponses de `/healthz` et `/ready` ;
- compte et profil concernés — **jamais le mot de passe** ;
- capture de l'écran ou réponse d'API ;
- entrées du journal d'audit correspondantes.

Redémarrer avant de collecter détruit souvent la seule trace exploitable.

---

## 8. Retour au nominal

Un incident n'est clos qu'après : rétablissement vérifié par les sondes, contrôle qu'aucune donnée
n'a été perdue, consignation de la cause, et — si la cause est applicative — **création d'une
anomalie**. Un incident résolu par redémarrage sans cause identifiée n'est pas clos : il est
suspendu.
