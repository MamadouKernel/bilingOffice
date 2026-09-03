# Options de traitement du secret dans l'historique Git

> **Document** : `docs/71-git-secret-history-remediation-options.md`
> **Établi le** : 2026-09-03, sur le commit `dbbccb3`
> **Aucune réécriture n'est effectuée par ce document.** Il compare, il ne tranche pas.

---

## 1. Le fait à traiter

Le mot de passe de `m.konate@cotedivoireterminal.com` figure en clair dans `DevelopmentSeeder.cs`
aux commits **`57a4472`** et **`dbbccb3`**, tous deux **poussés sur `origin/main`**.

Le dépôt a été **public** avant son passage en privé. La fenêtre d'exposition est donc réelle, et
sa durée exacte n'est pas connue de ce document.

---

## 2. Le point de départ, non négociable

**La rotation du mot de passe (`docs/70`) doit précéder toute décision sur l'historique.**

Une réécriture d'historique ne « reprend » pas un secret déjà diffusé : elle empêche seulement de
le relire dans le dépôt à l'avenir. Si le secret reste valide, le nettoyer de l'historique donne un
sentiment de sécurité sans en fournir.

Autrement dit : **la rotation referme le risque ; le nettoyage d'historique referme la trace.**
Les faire dans l'ordre inverse, c'est faire le travail visible avant le travail utile.

---

## 3. Option A — Dépôt privé, secret rotaté, historique conservé

| | |
|:---|:---|
| **Geste** | Aucun sur Git. Rotation seule, plus le retrait déjà effectué du code courant. |
| **Effort** | Nul |
| **Risque d'opération** | Nul |

**Avantages.** Aucune réécriture, donc aucun risque de casser des références, des clones ou des
branches. L'historique reste une trace fidèle de ce qui s'est passé — y compris de l'incident,
ce qui a une valeur d'audit.

**Limites.** Le secret reste lisible pour quiconque a accès au dépôt et à son historique. Un clone
effectué pendant la période publique le contient toujours. Une revue de conformité ultérieure le
signalera.

**Impact collaborateurs.** Nul.

**Forks et clones.** Inchangés, secret compris.

**Auditabilité.** **La meilleure des trois** : l'incident et sa correction restent démontrables.

---

## 4. Option B — Réécriture d'historique (`git filter-repo`) et mise à jour forcée

| | |
|:---|:---|
| **Geste** | Réécriture de tous les commits contenant le secret, puis `push --force` coordonné. |
| **Effort** | Moyen à élevé |
| **Risque d'opération** | **Élevé** |

**Avantages.** Le secret disparaît des commits accessibles depuis les branches réécrites. C'est ce
qu'attend une politique de sécurité formelle.

**Limites.** **Tous les identifiants de commits changent après le point de réécriture.** Les
références de ce dépôt à des commits précis — et la documentation produite jusqu'ici en cite
beaucoup — deviennent caduques. GitHub conserve par ailleurs les objets déréférencés un certain
temps, et les vues de pull requests peuvent continuer d'exposer les anciens contenus tant qu'une
purge côté hébergeur n'a pas été demandée.

**Impact collaborateurs.** Chaque personne ayant un clone doit le réaligner ; un `pull` naïf après
réécriture réintroduit l'ancien historique. Cela suppose une coordination annoncée, non un geste
solitaire.

**Forks et clones.** **Non atteints.** Un fork ou un clone antérieur conserve le secret. C'est la
limite décisive : l'option B ne garantit pas ce qu'on attend d'elle.

**Auditabilité.** Dégradée : la trace de l'incident est effacée en même temps que le secret.

**Interdiction en vigueur.** Le cadre de travail actuel proscrit `rebase`, `reset --hard` et
`push --force` sur de l'historique publié. Cette option **ne peut pas être engagée** sans une
levée explicite de cette règle.

---

## 5. Option C — Nouveau dépôt privé, reparti d'un état propre

| | |
|:---|:---|
| **Geste** | Créer un dépôt neuf à partir de l'arbre courant, sans reprendre l'historique. |
| **Effort** | Élevé |
| **Risque d'opération** | Moyen |

**Avantages.** Aucun secret nulle part, aucune ambiguïté, aucune coordination de réécriture.

**Limites.** **Perte de tout l'historique** : 25 migrations, la récupération de traçabilité
documentée en `docs/53`, les gates successifs. Sur un projet dont la gouvernance documentaire est
un livrable, c'est une perte substantielle. L'ancien dépôt doit par ailleurs être archivé ou
supprimé, sans quoi le problème est dupliqué plutôt que résolu.

**Impact collaborateurs.** Maximal : nouvelle origine, nouveaux clones, références externes à
refaire.

**Forks et clones.** L'ancien dépôt continue d'exister tant qu'il n'est pas supprimé.

**Auditabilité.** **La plus faible** : la continuité du projet est rompue.

---

## 6. Comparatif

| Critère | A — conserver | B — réécrire | C — repartir |
|:---|:---|:---|:---|
| Secret retiré des commits | Non | Oui, sur ce dépôt | Oui |
| Secret retiré des clones existants | Non | **Non** | **Non** |
| Effort | Nul | Moyen-élevé | Élevé |
| Risque d'opération | Nul | **Élevé** | Moyen |
| Références de commits préservées | **Oui** | Non | Non |
| Historique préservé | **Oui** | Partiellement | **Non** |
| Coordination requise | Aucune | **Forte** | **Forte** |
| Autorisé par le cadre actuel | **Oui** | **Non** | À décider |

---

## 7. Ce que ce document retient

**Aucune des trois options ne récupère un secret déjà diffusé.** C'est pourquoi la rotation est le
seul geste qui referme réellement le risque, et pourquoi elle ne dépend d'aucun de ces choix.

Le choix entre A, B et C est une **décision de politique**, pas une décision technique : il dépend
du niveau d'exigence de conformité applicable au projet, et du prix qu'on accepte de payer en
traçabilité et en coordination.

Si aucune obligation formelle n'impose l'absence du secret dans l'historique d'un dépôt désormais
privé, **A est le meilleur rapport risque / bénéfice** — à condition que la rotation soit faite.

**Aucune réécriture n'est engagée. Ce document attend une décision.**
