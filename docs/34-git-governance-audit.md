# Audit de gouvernance Git — traçabilité des specs et de la documentation

> **Document** : `docs/34-git-governance-audit.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Recommandation** : **`RESTORE_TRACKING`** — non appliquée, elle relève d'un arbitrage.

---

## 1. Constat

`docs/` et `specs/` sont **effectivement ignorés par Git**. Vérifié par `git check-ignore` :

```
.gitignore:45:docs/     docs/33-post-f017-global-cdc-matrix.md
.gitignore:47:specs/    specs/017-securite-granulaire-retention-reglementaire/spec.md
```

| Question | Réponse |
|:---|:---|
| `docs/` est-il ignoré ? | **Oui** — `.gitignore` ligne 45 |
| `specs/` est-il ignoré ? | **Oui** — `.gitignore` ligne 47 |
| Depuis quand ? | Commit **`bf8c327`** — « chore: preparer la publication du depot » |
| Mécanisme global ou local ? | **Local** — `.git/info/exclude` est vide, aucun `core.excludesFile` en cause |
| Volume non historisé | **266 fichiers** : 47 dans `docs/`, 219 dans `specs/` |

---

## 2. Pourquoi ce retrait a été fait

Le dépôt a été publié sur GitHub. La consigne était que l'outillage de développement, les
artefacts de spécification et la documentation interne **n'apparaissent pas** dans l'arborescence
du dépôt distant — le CDC du client figurant dans `docs/`.

L'objectif a été atteint : l'arborescence publiée ne contient que `src`, `tests` et la
configuration racine.

---

## 3. La conséquence non anticipée

Retirer un dossier du suivi ne le sort pas seulement de la **publication** : cela le sort du
**versionnement**. Les deux effets ont été confondus.

### 3.1 Deux features closes n'ont aucune spécification versionnée

| Feature | Sur disque | Dans l'historique Git |
|:---|:---:|:---:|
| `specs/001` à `specs/015` | Oui | **Oui**, jusqu'à `bf8c327^` |
| **`specs/016`** — Disponibilité, performance, résilience | Oui | **NON — jamais** |
| **`specs/017`** — Sécurité granulaire, rétention réglementaire | Oui | **NON — jamais** |

`specs/016` et `specs/017` ont été créées **après** le commit qui a posé l'exclusion. Elles n'ont
donc jamais été enregistrées, pas même une fois.

Cela concerne :

- la spécification et le plan de F016 et F017 ;
- leurs rapports de clôture — pièces contractuelles attestant SEC-02 et SEC-06 ;
- les artefacts de preuve F016 : `availability-report.md`, `performance-report.md`,
  `continuous-access-report.md`, `benchmark-k6.js`.

### 3.2 Ce que cela signifie concrètement

| Risque | Portée |
|:---|:---|
| Perte du répertoire de travail | **Perte définitive** des specs 016 et 017 et de leurs preuves |
| Historique des specs 001 à 015 | Récupérable jusqu'à `bf8c327^`, **figé** depuis |
| Modification d'une spec depuis `bf8c327` | **Non tracée** — aucun diff, aucun auteur, aucune date |
| Reprise par un tiers depuis le dépôt distant | Il reçoit le code **sans aucune spécification** |
| Traçabilité Spec Kit | **Rompue** — la chaîne exigence → spec → tâches → preuve n'est plus opposable |

### 3.3 Effet sur la traçabilité contractuelle

C'est le point le plus sérieux. La démarche Spec Kit tire sa valeur du fait que chaque exigence
CDC est reliée à une spécification datée, à des tâches et à un rapport de clôture. Cette chaîne
est aujourd'hui **conservée sur un seul poste de travail**, sans sauvegarde ni historique.

Un rapport de clôture qui n'est ni versionné ni horodaté par un système tiers est difficilement
opposable : rien ne prouve qu'il n'a pas été écrit après coup.

---

## 4. Ce qui a été confondu

Deux besoins distincts ont été traités par un seul mécanisme :

| Besoin | Mécanisme approprié |
|:---|:---|
| **Ne pas publier** ces dossiers sur un dépôt distant consultable | Visibilité du dépôt, ou dépôt distinct |
| **Ne pas perdre** l'historique de ces dossiers | Versionnement — `git` |

`.gitignore` ne sait faire que le second, et il le fait en supprimant le premier.

---

## 5. Options

| Option | Effet | Coût | Risque résiduel |
|:---|:---|:---|:---|
| **A — `KEEP_IGNORED`** (statu quo) | Rien n'est publié | Nul | **Perte possible de 266 fichiers**, dont deux specs jamais versionnées |
| **B — `RESTORE_TRACKING`** | Traçabilité rétablie | Un commit | Le CDC et les specs deviennent visibles sur le dépôt distant |
| **C — `RESTORE_TRACKING` + dépôt privé** | Traçabilité rétablie, rien de public | Changement de visibilité GitHub | Aucun, si la visibilité est effectivement privée |
| **D — Second dépôt privé** pour `docs/` et `specs/` | Séparation nette des deux besoins | Deux dépôts à tenir | Risque de désynchronisation entre code et specs |

---

## 6. Recommandation

**`RESTORE_TRACKING`**, sous la forme de l'option **C** ou **D**.

Le raisonnement est simple : l'option A accepte que les pièces contractuelles de deux features
closes reposent sur un seul disque, sans sauvegarde. Ce n'est pas un compromis, c'est un risque
non couvert — et il est déjà réalisé, puisque `specs/016` et `specs/017` n'ont jamais été
enregistrées une seule fois.

Entre C et D, **C est plus simple** : un dépôt unique garde le code et ses specs synchronisés,
ce que la démarche Spec Kit suppose. D convient si le code doit rester public.

### Ce qui n'a pas été fait, et pourquoi

`.gitignore` **n'a pas été modifié**. Le retrait du suivi résultait d'une consigne explicite sur
la publication ; le rétablir change la gouvernance du dépôt et, dans l'option B, rend public le
CDC d'un client. Ce n'est pas une correction technique, c'est une décision.

### Si l'option C ou D est retenue

L'opération est simple et sans risque de perte — les fichiers sont sur disque :

1. retirer `docs/` et `specs/` du `.gitignore` ;
2. `git add docs specs` ;
3. commit dédié, sans mélange avec du code ;
4. vérifier la visibilité du dépôt distant **avant** de pousser.

L'ordre du point 4 n'est pas indifférent : une fois poussé, un dépôt public l'a été.
