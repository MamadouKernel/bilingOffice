# Sauvegarde, restauration et plan de reprise — NFR-06

> **Document** : `docs/41-backup-restore-pra-readiness.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Verdict** : **`NOT_PROVEN`** — aucun test de restauration réel n'a été exécuté.

---

## 1. Ce que le CDC exige

> **NFR-06 (Must)** — « Sauvegardes régulières (données et documents) et procédure de restauration. »

Trois éléments sont contractuels, et un seul est souvent retenu :

1. **Régulières** — une fréquence doit être définie, pas seulement une capacité à sauvegarder.
2. **Données *et* documents** — le CDC nomme les deux. Sauvegarder la base seule ne satisfait pas.
3. **Procédure de restauration** — l'exigence porte sur la restauration, pas sur la sauvegarde.

**Conséquence directe.** Une sauvegarde qui n'a jamais été restaurée ne démontre pas NFR-06. Seul
un **test de restauration réel** peut le faire.

---

## 2. Périmètre à sauvegarder

| Élément | Contenu | Criticité | Cohérence requise |
|:---|:---|:---|:---|
| **Base SQL Server** | Tickets, dossiers, comptes, habilitations, journal d'audit, rétention | **Vitale** | Avec le stockage documentaire |
| **Stockage documentaire** | Pièces déposées, factures, documents finaux | **Vitale** | Avec la base |
| **Configuration** | `appsettings.*`, variables d'environnement | Élevée | — |
| **Secrets** | Chaîne de connexion, mot de passe d'amorçage | **Vitale** | Selon le mécanisme retenu |
| **Artefact applicatif** | Binaires publiés | Moyenne | Reconstructible depuis Git |
| **Dépôt Git** | Code, migrations | Élevée | Déjà distant |
| **`docs/` et `specs/`** | Spécifications, preuves | **Élevée** | **Non versionnés — voir `docs/34`** |

### Le point de cohérence, souvent manqué

La base contient des **références** vers des fichiers du stockage documentaire. Sauvegarder l'une
sans l'autre, ou à des instants différents, produit à la restauration un système **incohérent** :
des dossiers pointant vers des pièces absentes, ou des pièces orphelines.

Les deux sauvegardes doivent être **synchronisées** — même fenêtre, ou gel applicatif pendant la
prise.

### Le cas des secrets

Les secrets ne doivent **jamais** être sauvegardés en clair aux côtés des données. Leur
sauvegarde dépend du mécanisme retenu, qui n'est pas arbitré — `TBD_DSI`.

### Le cas de `docs/` et `specs/`

266 fichiers, dont les specs de deux features closes, **ne sont pas versionnés**. Ils ne sont donc
couverts par aucune sauvegarde du dépôt distant. Détail et recommandation : `docs/34`.

---

## 3. Paramètres à arbitrer

Aucun ne peut être choisi par l'équipe technique : ce sont des engagements de service.

| Paramètre | Question | Valeur |
|:---|:---|:---|
| **RPO** | Quelle perte de données est acceptable ? | `TBD_DSI` |
| **RTO** | En combien de temps le service doit-il être rétabli ? | `TBD_DSI` |
| Fréquence — base | Complète, différentielle, journal de transactions ? | `TBD_INFRA` |
| Fréquence — documents | Quotidienne ? Continue ? | `TBD_INFRA` |
| Rétention | Combien de temps conserver ? | `TBD_DSI` |
| Chiffrement | Les sauvegardes sont-elles chiffrées ? Avec quelle clé ? | `TBD_DSI` |
| Localisation | Sur site, hors site, les deux ? | `TBD_DSI` — lié à PC-13 |
| Support secondaire | Copie hors du site principal ? | `TBD_INFRA` |
| Responsable d'exécution | Qui lance et qui contrôle ? | `TBD_DSI` |
| Périodicité du test de restauration | Trimestriel ? Annuel ? | `TBD_DSI` |
| PRA | Existe-t-il un site de repli ? | `TBD_DSI` |

**RPO et RTO commandent tout le reste.** Un RPO d'une heure impose une sauvegarde du journal de
transactions ; un RPO de 24 heures se contente d'une sauvegarde complète quotidienne. Tant qu'ils
ne sont pas fixés, aucune procédure ne peut être dimensionnée.

---

## 4. Procédure de restauration — à valider par un test réel

### 4.1 Restauration de la base

```sql
RESTORE DATABASE [<BASE>] FROM DISK = N'<CHEMIN_SAUVEGARDE>' WITH REPLACE, RECOVERY;
```

### 4.2 Restauration du stockage documentaire

À la **même date de référence** que la sauvegarde de base restaurée. Un écart entre les deux
produit les incohérences décrites au § 2.

### 4.3 Contrôles de cohérence après restauration

| Contrôle | Méthode |
|:---|:---|
| L'application démarre | `/healthz` répond `200` |
| La base est atteignable | `/ready` répond `200` |
| Aucune migration en attente | `dotnet ef migrations list` |
| Les documents référencés existent | Échantillon de dossiers → vérification des fichiers |
| Le journal d'audit est intact | Comptage et vérification du dernier horodatage |
| Connexion d'un compte réel | Parcours de connexion complet |

### 4.4 Test de restauration — le seul qui certifie

Un test de restauration **réel** consiste à restaurer sur un environnement distinct, puis à
exécuter les six contrôles ci-dessus **et un parcours métier complet**.

Tant qu'il n'a pas eu lieu, NFR-06 reste **`NOT_PROVEN`**. Une procédure écrite n'est pas une
procédure vérifiée.

---

## 5. Questions au décideur

1. Quel **RPO** et quel **RTO** ?
2. Qui exécute les sauvegardes, et qui **contrôle** qu'elles ont réussi ?
3. Les sauvegardes sont-elles **chiffrées** ? Où est la clé ?
4. Une copie **hors site** existe-t-elle ?
5. Quelle **périodicité** pour le test de restauration ?
6. Existe-t-il un **PRA** ? Un site de repli ? Un PCA ?
7. Comment les **secrets** sont-ils sauvegardés et restaurés ?
8. `docs/` et `specs/` doivent-ils être couverts ? (voir `docs/34`)

---

## 6. Verdict

**NFR-06 : `BLOCKED_ENVIRONMENT` — `NOT_PROVEN`.**

Rien ne manque côté applicatif : la plateforme n'a aucune exigence particulière en matière de
sauvegarde au-delà de la cohérence base / documents, qui est signalée ici.

Ce qui manque relève entièrement de l'infrastructure et de l'exploitation : une politique, des
paramètres arbitrés, et **un test de restauration réellement exécuté**.
