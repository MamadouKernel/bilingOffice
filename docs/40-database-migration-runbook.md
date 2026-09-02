# Runbook de migration de base de données

> **Document** : `docs/39-database-migration-runbook.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **État vérifié** : 25 migrations · **0 pending model change** · dernière migration
> `20260902151046_AjoutRetentionEtGelLegal` (F017)

---

## 1. État constaté

```
dotnet ef migrations has-pending-model-changes
  → No changes have been made to the model since the last migration.
```

Le modèle EF Core et le jeu de migrations sont **synchronisés**. Aucune migration n'est à générer.

| Contrôle | Résultat |
|:---|:---|
| Nombre de migrations | 25 |
| Écart modèle / migrations | **0** |
| Migration la plus récente | `AjoutRetentionEtGelLegal` (F017) |
| Snapshot cohérent | Oui — sans quoi la commande ci-dessus signalerait un écart |

---

## 2. Principe retenu, et pourquoi

**L'application n'applique pas les migrations à son démarrage.** Ce n'est pas un manque : c'est
une protection. Un simple redémarrage ne doit jamais pouvoir modifier le schéma d'une base de
production — un redémarrage automatique après incident deviendrait sinon une opération de schéma
non supervisée.

Les migrations relèvent donc du **déploiement**, exécutées sciemment, avec sauvegarde préalable.

---

## 3. PRE-CHECK

À exécuter avant toute intervention, et à consigner.

```bash
dotnet ef migrations list --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

| Vérification | Attendu |
|:---|:---|
| Migrations marquées `(Pending)` | Uniquement celles que l'on entend appliquer |
| Écart modèle / migrations | Aucun |
| Version applicative déployée | Correspond au commit dont proviennent les migrations |
| Utilisateurs connectés | Zéro, ou fenêtre de maintenance annoncée |
| Espace disque disponible | Suffisant pour la sauvegarde **et** la croissance du journal |

**Point d'attention historique.** Une base créée hors migrations présente un historique vide
alors que les tables existent : EF considère alors **toutes** les migrations comme non appliquées
et échoue en tentant de recréer des objets (`erreur SQL 2714`). Le symptôme est reconnaissable —
`migrations list` affiche l'intégralité du jeu en `(Pending)` sur une base manifestement peuplée.
Dans ce cas, la remédiation est un **baseline** de l'historique, pas une application de migrations.

---

## 4. BACKUP

**Obligatoire, sans exception.** Une migration EF Core n'est pas réversible par simple annulation
dès lors qu'elle supprime ou transforme des données.

```sql
BACKUP DATABASE [<BASE>] TO DISK = N'<CHEMIN>\<BASE>_avant_<MIGRATION>.bak' WITH INIT, CHECKSUM, STATS = 10;
```

```sql
RESTORE VERIFYONLY FROM DISK = N'<CHEMIN>\<BASE>_avant_<MIGRATION>.bak' WITH CHECKSUM;
```

La vérification n'est pas optionnelle : une sauvegarde non vérifiée n'est pas une sauvegarde.
Consigner le chemin, l'horodatage et la taille.

`<CHEMIN>` relève de l'exploitation — `NEEDS_INFRA_VALUE`.

---

## 5. MIGRATION

### Option A — application directe (environnements non productifs)

```bash
dotnet ef database update --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

### Option B — script SQL revu puis exécuté (recommandé en production)

```bash
dotnet ef migrations script <DERNIERE_APPLIQUEE> <CIBLE> --idempotent --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web --output migration.sql
```

L'option B est préférable en production pour trois raisons : le script est **relisible avant
exécution**, il est **idempotent**, et il n'exige pas que le SDK .NET soit installé sur le serveur
de base de données.

**Contrôle du script avant exécution** — rechercher `DROP TABLE`, `DROP COLUMN`, `ALTER COLUMN`.
Toute opération destructive doit être justifiée et validée explicitement.

---

## 6. VALIDATION

```bash
dotnet ef migrations list --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

| Contrôle | Attendu |
|:---|:---|
| Migrations en attente | Aucune |
| `has-pending-model-changes` | « No changes have been made » |
| Tables du modèle présentes en base | Comptage identique |
| Contraintes et index créés | Conformes au script |

---

## 7. SMOKE TEST

Après migration, avant réouverture du service :

1. `/healthz` répond `200` ;
2. `/ready` répond `200` — c'est cette sonde qui atteste l'accès à la base ;
3. connexion d'un compte de test ;
4. lecture d'une entité par module (ticket, dossier, société) ;
5. une écriture, puis vérification de la trace au journal d'audit.

**Critère de réouverture** : les cinq passent. Un seul échec justifie le retour arrière.

---

## 8. ROLLBACK

### Cas 1 — migration additive uniquement

```bash
dotnet ef database update <MIGRATION_PRECEDENTE> --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

Utilisable **si et seulement si** la migration ne supprimait ni ne transformait de données.

### Cas 2 — migration destructive, ou doute

**Restaurer la sauvegarde.** Le retour arrière par `database update` ne recrée pas des données
supprimées : il recrée une structure vide.

```sql
RESTORE DATABASE [<BASE>] FROM DISK = N'<CHEMIN>\<BASE>_avant_<MIGRATION>.bak' WITH REPLACE, RECOVERY;
```

Redéployer ensuite la version applicative **antérieure** : une application récente sur un schéma
ancien échouera au démarrage ou, pire, silencieusement à l'exécution.

---

## 9. Volumétrie et durée

La durée d'une migration dépend du volume, qui n'est pas connu : **PC-01** n'est pas arbitré.

Tant que la volumétrie cible n'est pas fournie, la fenêtre de maintenance ne peut pas être
dimensionnée autrement que par **mesure sur une copie de la base de production**. C'est la seule
méthode honnête : extrapoler depuis un jeu de développement donnerait un chiffre sans valeur.

`Durée de la fenêtre : <TBD_DSI_PC01>`
