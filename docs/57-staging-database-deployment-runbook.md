# Runbook de déploiement de la base — Staging

> **Document** : `docs/57-staging-database-deployment-runbook.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Aucune migration n'a été appliquée à une base externe.** Ce document prépare les commandes.

---

## 1. État du jeu de migrations

| Élément | Valeur |
|:---|:---|
| Nombre de migrations | **25** |
| Première | `20260829061622_SocleInitial` |
| Dernière | `20260902151046_AjoutRetentionEtGelLegal` (F017) |
| Écart modèle / migrations | **0** — « No changes have been made to the model » |

Le modèle EF Core et le jeu de migrations sont synchronisés. **Aucune migration n'est à générer.**

---

## 2. Cas de figure

La procédure diffère selon l'état de la base cible. À déterminer **avant** toute action.

| Cas | Situation | Procédure |
|:---|:---|:---|
| **A** | Base vierge, aucune table | § 5, option 1 ou 2 — les 25 migrations s'appliquent |
| **B** | Base existante, historique de migrations présent | § 5 — seules les migrations manquantes s'appliquent |
| **C** | Base existante, **historique absent alors que les tables existent** | § 6 — **baseline requis**, ne pas appliquer |

**Le cas C est piégeux.** Il se reconnaît à ce que `migrations list` affiche **l'intégralité** du
jeu en `(Pending)` sur une base manifestement peuplée. Appliquer les migrations produit alors une
erreur SQL 2714 (« objet déjà existant »). Ce cas s'est déjà présenté sur ce projet.

---

## 3. PRE-CHECK

À exécuter et consigner.

```bash
dotnet ef migrations list --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

| Contrôle | Attendu |
|:---|:---|
| Migrations en `(Pending)` | Uniquement celles à appliquer |
| Base peuplée mais **tout** en `(Pending)` | → **cas C**, voir § 6 |
| Connectivité SQL | Établie depuis le serveur applicatif |
| Espace disque | Base **et** journal de transactions |
| Utilisateurs connectés | Aucun, ou fenêtre de maintenance annoncée |
| Droits du compte | Modification de schéma, temporairement |

---

## 4. BACKUP — obligatoire

```sql
BACKUP DATABASE [<BASE>] TO DISK = N'<CHEMIN>\<BASE>_avant_migration.bak' WITH INIT, CHECKSUM, STATS = 10;
```

```sql
RESTORE VERIFYONLY FROM DISK = N'<CHEMIN>\<BASE>_avant_migration.bak' WITH CHECKSUM;
```

**Une sauvegarde non vérifiée n'est pas une sauvegarde.** Consigner chemin, horodatage, taille.

`<BASE>` et `<CHEMIN>` : `TBD_INFRA`.

**Sauvegarder aussi le stockage documentaire**, dans la même fenêtre. Base et fichiers forment un
tout ; les restaurer à des instants différents produit des dossiers pointant vers des pièces
absentes.

---

## 5. MIGRATION

### Option 1 — script SQL idempotent (recommandée)

```bash
dotnet ef migrations script --idempotent --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web --output staging-migration.sql
```

Le script produit est **rejouable sans effet de bord** : chaque migration est encadrée d'un test
de présence dans l'historique.

**Avant exécution, le relire** — rechercher explicitement :

```bash
grep -inE "DROP TABLE|DROP COLUMN|ALTER COLUMN|DROP CONSTRAINT" staging-migration.sql
```

Toute opération destructive doit être justifiée et validée. Sur un jeu additif, ce contrôle ne
doit rien remonter.

Trois avantages sur l'option 2 : le script est **relisible avant exécution**, il est
**idempotent**, et il n'exige pas le SDK .NET sur le serveur de base.

### Option 2 — application directe (environnements non productifs)

```bash
dotnet ef database update --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

---

## 6. Cas C — baseline d'un schéma créé hors migrations

**Ne jamais appliquer les migrations dans ce cas** : elles échoueraient sur la première.

La remédiation consiste à **déclarer appliquées** les migrations que le schéma reflète déjà.

**Condition préalable, non négociable** : vérifier que le schéma correspond réellement au modèle.
Baseliner un schéma incomplet produirait une base durablement incohérente.

```bash
dotnet ef migrations script --idempotent --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web --output verification.sql
```

Puis comparer les tables attendues aux tables présentes. Si l'écart est nul, insérer les 25
identifiants dans `__EFMigrationsHistory` — un script de ce type figure déjà dans
`docs/sql/`.

---

## 7. VALIDATION

```bash
dotnet ef migrations list --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

```bash
dotnet ef migrations has-pending-model-changes --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web --no-build
```

| Contrôle | Attendu |
|:---|:---|
| Migrations en attente | **Aucune** |
| Écart modèle / migrations | « No changes have been made » |
| Tables présentes | Conformes au modèle |
| Index et contraintes | Créés |

---

## 8. SMOKE TEST base

Avant réouverture :

1. `/ready` répond `200` — c'est cette sonde qui atteste l'accès à la base ;
2. connexion d'un compte de test ;
3. lecture d'une entité par module ;
4. une écriture, puis vérification de la trace au journal d'audit.

**Le point 4 est le plus révélateur** : il exerce la base en écriture, l'unité de travail et le
garde append-only du journal d'audit en une seule opération.

---

## 9. ROLLBACK

| Situation | Conduite |
|:---|:---|
| Migration **additive** uniquement | `dotnet ef database update <MIGRATION_PRECEDENTE>` |
| Migration **destructive**, ou doute | **Restaurer la sauvegarde** — la base *et* les documents |
| Échec en cours de script idempotent | Restaurer ; ne pas relancer sur un état partiel |

**Le retour arrière par `database update` ne recrée pas des données supprimées** : il recrée une
structure vide. Dès qu'une migration touche à des données, seule la restauration convient.

Redéployer ensuite la version applicative **antérieure** : une application récente sur un schéma
ancien échoue au démarrage, ou pire, silencieusement à l'exécution.

---

## 10. Durée

La durée dépend du volume, inconnu — `PC-01` n'est pas arbitré.

Sur une base vierge, les 25 migrations s'appliquent en quelques secondes. Sur une base peuplée, la
seule méthode honnête est la **mesure sur une copie** : extrapoler depuis un jeu de développement
donnerait un chiffre sans valeur.

`Fenêtre de maintenance : TBD_DSI`
