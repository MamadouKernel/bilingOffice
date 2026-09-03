# Plan de remise en état — base de développement `DbBillingOffice`

> **Document** : `docs/72-development-db-recovery-plan.md`
> **Établi le** : 2026-09-03, sur le commit `dbbccb3`
> **Constat d'origine** : `docs/69`
> **Aucune suppression, aucune réparation automatique n'est engagée par ce document.**

---

## 1. La condition préalable est désormais remplie

Une remise en état engagée avant de refermer les deux causes aurait été détruite à l'exécution
suivante de la suite de tests. Les deux causes sont traitées :

| Cause | Correction | Vérification |
|:---|:---|:---|
| Le harnais de test atteignait la base de développement | Réenregistrement explicite du `DbContext` + garde sur la base réellement résolue | `TEST-DB-01` à `TEST-DB-05` |
| L'amorçage purgeait sans condition | Quatre barrières cumulatives ; une absence de configuration **empêche** la purge | `SEED-SEC-01` à `04`, `SEED-SAFE-01` à `05` |

**Preuve de non-régression** : après correction, une exécution complète de la suite Web laisse
`sys.databases.create_date` de `DbBillingOffice` **inchangée** (`2026-09-03 03:19:10`) et
`AspNetUsers` à 1. La base n'est plus touchée.

---

## 2. Ce qui manque, et ce qui reste

| Élément | État | Nature |
|:---|:---|:---|
| Comptes synthétiques (`@test.local`, `@exemple.test`) | **Supprimés** — une cinquantaine | Données de test |
| Compte `m.konate@cotedivoireterminal.com` | **Présent**, 54 habilitations | À préserver |
| Files de traitement | 10 | Paramétrage, à préserver |
| Journal d'audit | 19 entrées | **À préserver — append-only** |
| `Societes` | 4 | À examiner |
| `Tickets` / `Dossiers` | 1 / 2 | À examiner |
| `Habilitations` | 63, dont **9 orphelines** | À nettoyer |
| `UtilisateursSociete` | 4, **toutes orphelines** | À nettoyer |
| Table `__EFMigrationsHistory` | **Absente** | **Le point structurant** |

---

## 3. Le problème de fond n'est pas les données, c'est le schéma

La base a été recréée par `EnsureCreated()`, qui construit le schéma depuis le modèle **sans
inscrire la moindre migration**. `__EFMigrationsHistory` est absente.

Conséquences :

| Conséquence | Effet |
|:---|:---|
| `dotnet ef database update` | Tenterait d'appliquer les 25 migrations sur un schéma déjà présent → **SQL 2714** |
| Migrations futures | Inapplicables sans intervention |
| Représentativité | Cette base ne reproduit plus le chemin de déploiement réel |

C'est exactement le **cas C** décrit au runbook `docs/57`. Une base de développement qui diverge du
chemin de migration finit par masquer les défauts que ce chemin révélerait.

> **À retenir sans ambiguïté : la base `DbBillingOffice` actuelle NE DOIT PAS être considérée
> comme validée par l'historique des 25 migrations.** Son schéma provient d'un `EnsureCreated()`.
> Il n'a jamais été produit ni vérifié par le chemin de migration, et aucune trace ne permet de
> l'affirmer. Toute conclusion tirée de cette base sur la validité des migrations serait infondée.

**Opérations proscrites sur cette base** tant qu'une décision n'est pas prise :
`dotnet ef database update`, `EnsureDeleted`, `EnsureCreated`, `DROP`, `DELETE`, et toute migration
automatique.

---

## 4. Voies de remise en état

### Voie 1 — Recréer par les migrations *(recommandée)*

Supprimer la base et la reconstruire avec `dotnet ef database update`, puis réamorcer un jeu
synthétique.

| | |
|:---|:---|
| **Avantages** | `__EFMigrationsHistory` correctement renseignée ; la base redevient représentative du déploiement réel |
| **Coût** | Perte des 4 sociétés, 1 ticket, 2 dossiers et **19 entrées d'audit** restants |
| **Préalable** | Confirmer que rien de ces données n'a de valeur — **seul l'utilisateur peut le dire** |

### Voie 2 — Poser la ligne de base des migrations sur la base existante

Créer `__EFMigrationsHistory` et y inscrire les 25 migrations comme déjà appliquées, sans toucher
aux données.

| | |
|:---|:---|
| **Avantages** | Aucune perte de données ; les migrations futures redeviennent applicables |
| **Limites** | Suppose que le schéma issu de `EnsureCreated()` soit **identique** à celui produit par les migrations — vraisemblable, mais **non démontré** |
| **Contrôle indispensable** | Comparer le schéma réel au schéma attendu **avant** d'inscrire quoi que ce soit |

### Voie 3 — Restaurer une sauvegarde antérieure au 2026-09-03 03:19

| | |
|:---|:---|
| **Avantages** | Restitue les données perdues |
| **Limites** | **L'existence d'une telle sauvegarde n'est pas établie** — `TBD_HUMAIN` |

### Voie 4 — Ne rien faire pour l'instant

Recevable : la base est fonctionnelle pour un usage courant, et elle ne se dégrade plus. Le
problème de migrations ne se manifestera qu'à la prochaine migration.

---

## 5. Nettoyage des orphelins

13 lignes orphelines subsistent : 4 `UtilisateursSociete` et 9 `Habilitations` référençant des
utilisateurs supprimés.

**Elles ne sont pas urgentes** : elles ne bloquent plus rien depuis que la purge ne s'exécute plus.
Elles disparaissent d'elles-mêmes par la voie 1, et se traitent séparément dans la voie 2.

**Aucune suppression n'est proposée ici sous forme de commande** : sur une base dont l'état vient
d'être perturbé, une suppression supplémentaire non validée serait exactement le geste de trop.

---

## 6. Ordre recommandé

```
1. Confirmer qu'aucune donnée saisie manuellement n'a de valeur   (utilisateur)
2. Rechercher une sauvegarde antérieure au 2026-09-03 03:19       (utilisateur)
3. Choisir la voie : 1 (recréer) · 2 (ligne de base) · 3 (restaurer) · 4 (différer)
4. Exécuter la voie retenue
5. Vérifier : __EFMigrationsHistory renseignée, 0 orphelin, m.konate à 54 habilitations
6. Effectuer la rotation du mot de passe                          (docs/70)
```

L'étape 6 vient **après** : une recréation de base ne doit pas ramener un compte au secret
compromis. L'amorçage ne réécrivant plus le mot de passe d'un compte existant, l'ordre inverse
serait également sûr — mais celui-ci l'est sans condition.

---

## 7. Verdict

**`MANUAL_REVIEW_REQUIRED`**

Les causes sont refermées et prouvées telles ; la dégradation a cessé. Ce qui reste est un choix
qui engage des données : il appartient à l'utilisateur, pas à l'agent.

Aucune commande destructive ne figure dans ce document.
