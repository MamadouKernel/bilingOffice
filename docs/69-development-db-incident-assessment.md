# Constat d'incident — base de développement `DbBillingOffice`

> **Document** : `docs/69-development-db-incident-assessment.md`
> **Établi le** : 2026-09-03, sur le commit `dbbccb3`
> **Nature** : constat **en lecture seule**. Aucune écriture, aucune purge, aucune réparation.
> **Suite** : plan de remise en état dans `docs/72`.

---

## 1. Ce qui s'est produit

Le harnais de test Web ne fixe pas sa base de façon étanche. En environnement `Development`,
`WebApplication.CreateBuilder` charge les **user-secrets**, dont la valeur de
`ConnectionStrings:PlateformeDb` **prime sur** celle que la fabrique de test injecte par
`ConfigureAppConfiguration`.

Conséquence : les tests Web se sont exécutés contre `DbBillingOffice`, la base de développement
réelle, et non contre `CitPlateformeTestsWeb`.

Deux mécanismes destructifs s'y sont appliqués :

| # | Mécanisme | Origine | Effet |
|---:|:---|:---|:---|
| 1 | `EnsureDeleted()` puis `EnsureCreated()` | `PlateformeWebApplicationFactory` | **Suppression et recréation de la base** |
| 2 | `PurgerDonneesTestAsync()` | `DevelopmentSeeder.SeedAsync()` | Purge des comptes et données à chaque démarrage d'hôte |

Le premier est de loin le plus lourd : il ne vide pas des tables, il **détruit la base**.

---

## 2. Preuve

| Observation | Valeur | Ce qu'elle établit |
|:---|:---|:---|
| `sys.databases.create_date` | **2026-09-03 03:19:10** | La base a été **recréée** ce jour |
| Table `__EFMigrationsHistory` | **absente** | Le schéma vient de `EnsureCreated()`, **pas des 25 migrations** |
| Nombre de tables | 45 | Schéma complet, mais hors gestion de migrations |

**La base n'est plus pilotée par les migrations.** C'est le cas « C » de `docs/57 § cas C » : un
`dotnet ef database update` échouerait en SQL 2714 (objets déjà existants).

---

## 3. Volumétrie constatée

| Table | Lignes |
|:---|---:|
| `AspNetUsers` | **1** |
| `Societes` | 4 |
| `UtilisateursSociete` | 4 |
| `Tickets` | 1 |
| `Dossiers` | 2 |
| `Habilitations` | 63 |
| `Files` | 10 |
| `JournalAudit` | 19 |
| `Notifications` | 0 |
| `InvitationsCollaborateur` | 0 |

Ces valeurs **ont diminué en cours de session** : un relevé antérieur donnait `Societes` 9,
`Tickets` 9, `Dossiers` 9, `Habilitations` 81. Chaque démarrage d'hôte de test relance la purge.

**La dégradation est donc en cours, et se poursuivra à chaque exécution de la suite Web** tant que
l'isolation n'est pas rétablie.

---

## 4. Intégrité référentielle

| Contrôle | Résultat |
|:---|---:|
| `UtilisateursSociete` sans `Societe` | 0 |
| **`UtilisateursSociete` sans utilisateur** | **4** |
| **`Habilitations` sans utilisateur** | **9** |

### Pourquoi la purge échoue désormais à chaque fois

`PurgerDonneesTestAsync()` supprime les rattachements **uniquement pour les utilisateurs qu'elle
purge** (`userIdsToPurge`). Les 4 rattachements ci-dessus appartiennent à des utilisateurs **déjà
supprimés** : ils ne sont donc jamais nettoyés. La purge tente ensuite
`context.Societes.RemoveRange(...)` et se heurte à
`FK_UtilisateursSociete_Societes_SocieteId`.

C'est la cause unique des **65 classes de tests Web en échec sur 67** : l'exception survient au
démarrage de l'hôte, avant que le code testé ne soit atteint.

---

## 5. Ce qui subsiste

| Élément | État |
|:---|:---|
| Compte `m.konate@cotedivoireterminal.com` | **Présent** |
| Ses habilitations | **54** — complètes |
| Files de traitement | 10 |
| Journal d'audit | 19 entrées |
| Comptes synthétiques (`@test.local`, `@exemple.test`) | **Supprimés** — une cinquantaine |

**Aucune donnée de production n'est concernée** : cette base est un environnement de développement
local, et les comptes détruits étaient synthétiques.

---

## 6. Ce qui reste inconnu

| Question | Statut |
|:---|:---|
| Existe-t-il une sauvegarde antérieure au 2026-09-03 03:19 ? | `TBD_HUMAIN` |
| Des données métier saisies à la main ont-elles été perdues ? | `TBD_HUMAIN` — seul l'utilisateur peut le dire |
| Le mot de passe de `m.konate` a-t-il été réécrit par le seeder ? | **Oui, probablement** — `SeedAdminCitAsync` réapplique une constante |

Le dernier point est traité dans `docs/70` : le secret est compromis, et sa rotation est un acte
humain.

---

## 7. Verdict

**`MANUAL_REVIEW_REQUIRED`**

Aucune réparation n'est engagée par ce document. Deux corrections doivent précéder toute remise en
état, faute de quoi elle sera détruite à la prochaine exécution :

1. **Isolation du harnais de test** — plus aucun test ne doit pouvoir atteindre `DbBillingOffice` ;
2. **Verrouillage du seeder destructif** — une absence de configuration doit empêcher la purge, non
   l'autoriser.

Le plan de remise en état figure dans `docs/72`.
