# Gestion des anomalies de recette

> **Document** : `docs/64-uat-defect-management.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Objet** : comment une anomalie constatée en recette est qualifiée, arbitrée et refermée.

---

## 1. Principe

Une anomalie de recette n'est pas une demande d'évolution. La distinction est la seule qui compte,
et c'est celle qu'on escamote le plus souvent sous la pression du calendrier.

| Nature | Définition | Traitement |
|:---|:---|:---|
| **Anomalie** | L'application ne fait pas ce que le **CDC** ou la spécification décrit | Correction, dans le périmètre |
| **Évolution** | L'application fait ce qui est décrit, mais autre chose est souhaité | **Hors périmètre** — arbitrage |
| **Écart de données** | Le comportement est correct, le jeu de recette est faux | Correction du jeu de données |
| **Écart d'environnement** | Le comportement est correct, l'environnement est mal configuré | Correction de configuration |

**Toute anomalie doit citer sa référence CDC ou sa spécification.** Sans référence, ce n'est pas une
anomalie : c'est une attente, et une attente s'arbitre au lieu de se corriger en silence.

---

## 2. Gravité

| Niveau | Critère | Effet sur le go/no-go |
|:---|:---|:---|
| **Bloquante** | Un parcours **Must** du CDC est impraticable, ou une propriété de sécurité est en défaut | **Interdit le passage** |
| **Majeure** | Un parcours Must est praticable mais dégradé, ou un parcours Should est impraticable | Passage sous réserve **écrite et acceptée** |
| **Mineure** | Gêne d'usage, libellé, ergonomie | N'empêche pas le passage |
| **Cosmétique** | Présentation sans effet fonctionnel | N'empêche pas le passage |

### Sont bloquantes par nature, sans discussion

Ces cas ne se négocient pas, quelle que soit la pression :

| Cas | Raison |
|:---|:---|
| Une société voit les données d'une autre | SEC-03 — cloisonnement |
| Un fichier infecté est accepté ou reste téléchargeable | SEC-07 |
| Une API répond sans authentification | Contrôle d'accès |
| Un compte désactivé conserve l'accès | SEC-11 |
| Une action sensible n'apparaît pas au journal d'audit | SEC-05 |
| Une entrée du journal d'audit peut être modifiée ou supprimée | Journal append-only |
| Un document déposé disparaît | Intégrité documentaire |
| Un calcul de facturation est faux | Cœur métier |

**Une anomalie de cette liste ne peut pas être « acceptée avec réserve ».** Accepter une réserve
sur le cloisonnement, c'est accepter une fuite de données ; accepter une réserve sur l'audit, c'est
renoncer à savoir ce qui s'est passé.

---

## 3. Contenu d'une fiche d'anomalie

Sans ces éléments, une anomalie n'est pas exploitable et coûtera plus cher en allers-retours qu'en
correction :

| Champ | Obligatoire | Remarque |
|:---|:---:|:---|
| Identifiant | Oui | |
| Titre | Oui | Le symptôme, pas la cause supposée |
| **Commit déployé** | **Oui** | Sans lui, l'anomalie n'est rattachable à aucune version |
| Environnement | Oui | Staging, recette… |
| **Compte utilisé et ses permissions** | **Oui** | Un refus attendu est souvent pris pour un défaut |
| Étapes de reproduction | Oui | Numérotées, rejouables par un tiers |
| Résultat observé | Oui | |
| Résultat attendu | Oui | |
| **Référence CDC ou spécification** | **Oui** | Fonde la qualification en anomalie |
| Gravité proposée | Oui | Arbitrée ensuite |
| Horodatage | Oui | Permet le rapprochement avec les journaux |
| Copie d'écran | Si pertinent | |
| Extrait de journal | Si disponible | |

**Le champ « compte utilisé et ses permissions » évite la moitié des faux positifs.** Sur une
plateforme où tout est permission par permission, « je ne vois pas le bouton » est le plus souvent
une habilitation absente — c'est-à-dire le fonctionnement attendu.

---

## 4. Circuit

```
Constat  ->  Fiche  ->  Qualification  ->  Correction  ->  Vérification  ->  Clôture
                            |
                            +-->  Écart de données ou d'environnement  ->  correction sur place
                            |
                            +-->  Évolution  ->  arbitrage, hors périmètre
```

| Étape | Acteur | Sortie |
|:---|:---|:---|
| Constat | Testeur | Fiche complète |
| Qualification | Responsable de recette | Nature + gravité arbitrées |
| Correction | Équipe de développement | Correctif + test automatisé |
| Vérification | **Le testeur d'origine** | Rejeu du scénario |
| Clôture | Responsable de recette | Fiche fermée |

**La vérification revient au testeur d'origine**, jamais à celui qui a corrigé : il est le seul à
savoir ce qu'il a réellement observé, et un correctif qui traite un symptôme mal compris passerait
autrement inaperçu.

---

## 5. Règle de correction

Toute correction d'anomalie **bloquante ou majeure** s'accompagne d'un **test automatisé qui échoue
avant le correctif et passe après**.

Sans lui, rien ne garantit que le défaut ne reviendra pas — et l'expérience du projet le confirme :
la suite de 1041 tests est ce qui a permis de corriger sans régresser.

**Une correction ne se livre pas en recette sans exécution complète de la suite de tests.**

---

## 6. Ce qui ne doit pas arriver

| Pratique | Pourquoi elle est proscrite |
|:---|:---|
| Modifier un test pour qu'il passe | Le test devient une décoration ; le défaut reste |
| Requalifier une bloquante en majeure pour tenir une date | Le risque n'est pas réduit, il est masqué |
| Corriger directement en base de données | Le journal d'audit ne le voit pas ; le défaut se reproduira |
| Clore une fiche sans rejeu | La correction n'est pas démontrée |
| Corriger sans référence CDC | Le correctif introduit un comportement non spécifié |

---

## 7. Indicateurs de campagne

| Indicateur | Usage |
|:---|:---|
| Anomalies ouvertes par gravité | Suit le reste à traiter |
| **Bloquantes ouvertes** | **Doit valoir zéro pour le passage** |
| Majeures ouvertes avec réserve acceptée | Doit être nommément listé au go/no-go |
| Taux de scénarios exécutés | Mesure l'avancement réel |
| Taux de scénarios en succès | Mesure la qualité |
| Anomalies réouvertes | Signale des corrections superficielles |

Le dernier indicateur est le plus révélateur : une anomalie qui rouvre signale qu'on a traité le
symptôme sans comprendre la cause.

---

## 8. Outillage

`TBD_DSI` — aucun outil de suivi n'est imposé par le CDC, et ce document n'en désigne aucun. Le
minimum exigible : les champs du § 3, un historique, et une traçabilité vers le commit correctif.

---

## 9. Statut

**`NOT EXECUTED`** — aucune campagne n'est ouverte ; aucune anomalie de recette n'existe à ce jour.
