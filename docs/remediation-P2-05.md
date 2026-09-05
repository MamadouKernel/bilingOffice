# Remédiation P2-05 — Reproductibilité RG-SIM-12

## Exigence

**RG-SIM-12** — « Les grilles tarifaires sont versionnées avec une date d'effet ; une simulation
référence la version appliquée et l'historique des grilles est conservé. »

L'objet de la règle est la **reproductibilité** : une proforma remise au client est un engagement
chiffré, et doit rester consultable telle qu'elle a été produite.

---

## Défaut

**Le comportement de production était correct.** `SimulationProforma` fige `MontantTotalXof` et
`LignesConteneurs` à la création, et `ConsulterSimulationProformaQuery` les **relit** sans jamais
recalculer.

**Mais aucun test n'assertait les montants.** Le test existant,
`ConsulterSimulation_detecte_si_la_grille_a_ete_revisee`, jouait bien le scénario V1 → S1 → V2 →
consultation de S1, et vérifiait :

- `EstGrilleObsolete == true`
- `VersionGrilleLibelle == "v1.0"`
- `VersionActiveActuelleLibelle == "v2.0"`

C'est-à-dire **la version référencée**, jamais le prix. Un recalcul silencieux avec la grille
courante aurait modifié le montant d'un dossier ancien sans qu'aucun test ne s'en aperçoive — et
c'est précisément le montant qui fait la valeur de la règle.

---

## Reproduction

Comme pour P2-04, la reproduction n'est pas un test qui échoue sur le code actuel, mais une
**mutation qui ne fait rien tomber**. Voir le § *Mutations* : la mutation appliquée laisse la suite
`ConsulterSimulationProformaQueryTests` **entièrement verte**, alors qu'elle altère le montant servi
pour toute simulation dont la grille a été révisée.

---

## Cause racine

Le test existant éprouvait la **traçabilité de la version** — ce qui est la moitié du texte de la
règle — et pas la **conservation des valeurs**, qui en est l'objet. Le libellé de la règle mentionne
explicitement le versionnement et la référence ; la reproductibilité des montants en est la
conséquence attendue, et c'est cette conséquence qui n'était pas vérifiée.

---

## Correction

Aucun code de production n'a été modifié.
`tests/CIT.Plateforme.Application.Tests/Tarification/ReproductibiliteHistoriqueSimulationTests.cs` —
**3 tests**, joués de bout en bout sur le chemin de production
(`SauvegarderSimulationProformaCommandHandler` puis `ConsulterSimulationProformaQueryHandler`).

**Le scénario complet**, en une seule méthode — l'ordre des événements *est* la démonstration :

```
grille V1  →  simulation S1  →  grille V2 activée  →  simulation S2  →  consultation de S1
```

V2 change tous les tarifs (acconage 99 000, relevage 44 000, branchement 55 000), et les deux
simulations portent **le même dossier** : sans cela, l'égalité des montants de S1 ne prouverait rien.

Les six assertions exigées :

| # | Assertion | Vérifie |
|---|---|---|
| 1 | `vueS1.VersionGrilleLibelle == "v1.0"` et `vueS2 == "v2.0"` | S1 référence V1, S2 référence V2 |
| 2 | `vueS1.EstGrilleObsolete` vrai, `vueS2` faux | l'obsolescence est signalée, non appliquée |
| 3 | `vueS1.MontantTotalXof == montantS1ALaCreation` | **S1 conserve ses montants historiques** |
| 4 | `vueS1.MontantTotalXof != vueS2.MontantTotalXof` | S1 n'est pas recalculée avec V2 |
| 5 | `ligneS1.TotalLigne != ligneS2.TotalLigne` | le détail conteneur reste celui de V1 |
| 6 | `vueS1.Lignes.Sum(TotalLigne) == vueS1.MontantTotalXof` | le total du dossier reste cohérent avec son propre détail |

Deux tests complémentaires, chacun fermant une échappatoire :

- **`Le_montant_historique_survit_a_un_rechargement_depuis_la_base`** — `ChangeTracker.Clear()` avant
  la consultation. Sans lui, un montant conservé seulement en mémoire passerait le scénario
  principal.
- **`L_historique_des_grilles_est_conserve`** — V1 reste lisible après l'activation de V2. Sans
  elle, la simulation référencerait une version disparue, et la règle serait vide de sens.

---

## Tests

| Suite | Résultat |
|---|---|
| `ReproductibiliteHistoriqueSimulationTests` *(créée)* | **3 / 3** |
| `ConsulterSimulationProformaQueryTests` (existante) | verte, inchangée |
| Golden masters tarifaires (`GoldenMasterProformaTests`, `AuditPreuveArrondiFrancTests`) | vertes |
| **Suite globale** | **1 366 / 1 366** |

---

## Mutations

| Mutation | Régression simulée | Tests tombés |
|---|---|---|
| `estObsolete ? simulation.MontantTotalXof + 1m : simulation.MontantTotalXof` dans la projection de consultation | Le montant servi n'est plus exactement le montant figé dès que la grille a changé | **2** |

**Le résultat le plus parlant de ce lot** : sous cette mutation,
`ConsulterSimulationProformaQueryTests` — la suite qui couvrait RG-SIM-12 jusqu'ici — **reste
entièrement verte**. Elle n'observe que la version, jamais le prix. C'est la démonstration directe
du trou que ce lot comble.

Une première mutation, plus fidèle à l'énoncé (recalcul complet avec la grille courante), a été
écartée : elle ne compilait pas sans reconstruire le dossier de simulation depuis ses lignes
persistées, et une mutation qui ne compile pas ne démontre rien. La divergence minimale retenue
appartient à la même classe — le montant servi cesse d'être le montant figé — et c'est cette
propriété que les assertions 3, 4 et 6 protègent.

Restauration vérifiée : aucune occurrence de `MUTATION` dans le fichier, suite globale verte.

---

## Fichiers modifiés

**Production : aucun.** Le comportement était conforme ; seule l'assertion manquait.

**Tests — 1 créé** :
`tests/CIT.Plateforme.Application.Tests/Tarification/ReproductibiliteHistoriqueSimulationTests.cs`.

---

## Risques résiduels

1. **La reproductibilité n'est vérifiée que sur un conteneur simple** — ni Reefer, ni lourd, ni
   dangereux, ni hors gabarit. Les golden masters couvrent ces postes au calcul ; leur
   **conservation historique** repose sur le même mécanisme de figement, mais n'est pas éprouvée
   poste par poste après révision de grille.
2. **Le stationnement et les quotes-parts bénéficiaires** ne font l'objet d'aucune assertion
   historique distincte : ils entrent dans `TotalLigne` et le total dossier, sans être isolés.
3. **Aucun test ne couvre une simulation référençant une version supprimée.** L'historique est
   conservé aujourd'hui ; rien n'empêche formellement une suppression de version en base.
4. **Tests sur `InMemory`.** Le figement repose sur la persistance des lignes de simulation, dont le
   comportement en SQL Server réel n'est pas éprouvé ici.
