# Audit de sûreté des migrations

> **Document** : `docs/75-migration-safety-audit.md`
> **Établi le** : 2026-09-03
> **Aucune migration n'a été appliquée à `DbBillingOffice` pendant cet audit.**

---

## 1. Périmètre

**25 migrations**, de `SocleInitial` à `AjoutRetentionEtGelLegal`. Analyse des méthodes `Up()`
uniquement : c'est le sens dans lequel une migration s'applique et peut détruire des données. Les
`Down()` contiennent naturellement des suppressions et ne sont pas un risque de mise à niveau.

---

## 2. Résultat

| Opération dans un `Up()` | Occurrences |
|:---|---:|
| `DropTable` | **0** |
| `RenameTable` | **0** |
| `RenameColumn` | **0** |
| `AlterColumn` | **0** |
| `DropColumn` | **1** |

**Une seule opération potentiellement destructive sur 25 migrations.**

---

## 3. La seule opération à risque

`20260829164513_RemediationAuditSlaCalendrierCompetenceValueComparer` supprime
`ParametresSla.DureeCible` puis la recrée en `bigint`.

**Pourquoi une suppression plutôt qu'une conversion** : le changement de type est `time` → `bigint`,
entre lesquels aucune conversion automatique n'a de sens — une heure du jour et un nombre de ticks
ne se convertissent pas l'une en l'autre. La migration le documente explicitement.

**Pourquoi c'est sans danger sur une base neuve** : la colonne est créée par une migration
antérieure et supprimée par celle-ci, à l'intérieur de la même chaîne. Une base construite par
`dotnet ef database update` depuis zéro ne perd rien — la donnée n'a jamais existé.

**Où ce serait dangereux** : sur une base portant déjà des valeurs SLA réelles. Le commentaire de la
migration s'appuie sur le fait qu'aucune n'existait alors (PC-12 ouvert). Cette hypothèse doit être
**revérifiée** avant d'appliquer la chaîne à une base contenant des données.

---

## 4. Ordonnancement et cohérence

| Contrôle | Résultat |
|:---|:---|
| Ordre chronologique des horodatages | Cohérent |
| Instantané de modèle à jour | **Oui** — `has-pending-model-changes` : 0 |
| Migration sans `Down()` | Aucune |
| Colonne rendue non-nullable sans valeur par défaut | Aucune détectée |

---

## 5. Contrainte connue sur `DbBillingOffice`

Cette base **n'a pas d'historique de migrations** : `__EFMigrationsHistory` est absente, son schéma
provient d'un `EnsureCreated()`. Y appliquer la chaîne échouerait en SQL 2714 — objets déjà
existants.

Elle **ne doit pas être considérée comme validée par ces 25 migrations**. Le plan de remise en état
figure dans `docs/76`.

---

## 6. Verdict

**`MIGRATION_CHAIN_SAFE_FOR_FRESH_DEPLOYMENT`**

Une base Staging construite depuis zéro par la chaîne complète est sûre. Toute application à une
base existante et peuplée exige au préalable la vérification du § 3.
