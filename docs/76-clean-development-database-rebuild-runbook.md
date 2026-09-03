# Runbook — reconstruction propre de la base de développement

> **Document** : `docs/76-clean-development-database-rebuild-runbook.md`
> **Établi le** : 2026-09-03
> **PROCÉDURE UNIQUEMENT. RIEN N'EST EXÉCUTÉ PAR CE DOCUMENT.**
> **Constat d'origine** : `docs/69` · **Décision** : `docs/72`

---

## 1. Pourquoi une reconstruction

`DbBillingOffice` a été supprimée et recréée par `EnsureCreated()` le 2026-09-03 à 03:19.
Conséquence : `__EFMigrationsHistory` est absente, et le schéma n'a jamais été produit par la chaîne
de 25 migrations.

Cette base **ne prouve donc rien** sur la validité du chemin de déploiement réel. Une base de
développement qui diverge du chemin de migration finit par masquer précisément les défauts que ce
chemin révélerait au moment du déploiement.

---

## 2. Préalables — les deux causes sont refermées

| Cause | État | Sans cela |
|:---|:---|:---|
| Le harnais de test atteignait cette base | **Refermé** — `GardeBaseDeTest` | La reconstruction serait détruite à la prochaine suite |
| L'amorçage purgeait sans condition | **Refermé** — `PolitiqueAmorcageDemo` | La reconstruction serait purgée au prochain démarrage |

**Ne pas engager la procédure avant que ces correctifs soient commités.**

---

## 3. Étape 0 — décisions humaines requises

| # | Question | Sans réponse |
|---:|:---|:---|
| 1 | Des données saisies à la main ont-elles de la valeur ? | **Ne pas continuer** |
| 2 | Une sauvegarde antérieure au 2026-09-03 03:19 existe-t-elle ? | Voie de restauration écartée |
| 3 | Le journal d'audit (19 entrées) doit-il être conservé ? | **Ne pas continuer** |

La question 3 mérite attention : le journal d'audit est append-only par conception. Le détruire est
une décision, pas une conséquence technique.

---

## 4. Sauvegarde — obligatoire

```powershell
sqlcmd -S localhost -E -Q "BACKUP DATABASE [DbBillingOffice] TO DISK='<chemin>\DbBillingOffice-avant-reconstruction.bak' WITH INIT, COMPRESSION"
```

Vérifier la sauvegarde **avant** de poursuivre :

```powershell
sqlcmd -S localhost -E -Q "RESTORE VERIFYONLY FROM DISK='<chemin>\DbBillingOffice-avant-reconstruction.bak'"
```

Une sauvegarde non vérifiée n'est pas une sauvegarde.

---

## 5. Reconstruction

### 5.1 Nouvelle base, nom distinct

Utiliser un **nom nouveau** plutôt que réutiliser l'ancien : l'ancienne base reste disponible en
repli tant que la nouvelle n'est pas validée.

### 5.2 Appliquer la chaîne de migrations

```bash
dotnet ef database update --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

C'est le point de la procédure : le schéma est produit **par les migrations**, et
`__EFMigrationsHistory` est renseignée.

### 5.3 Basculer la chaîne de connexion

Mettre à jour les user-secrets vers la nouvelle base.

### 5.4 Amorçage contrôlé

L'amorçage exige désormais quatre conditions. Les fournir explicitement, et **uniquement pour cette
opération** :

| Variable | Valeur |
|:---|:---|
| `ASPNETCORE_ENVIRONMENT` | `Development` |
| `CIT_TEST_USERS_PASSWORD` | Secret synthétique, **jamais** l'ancien secret compromis |
| `CIT_ALLOW_DESTRUCTIVE_DEMO_SEED` | `true` |
| `CIT_DEMO_DATABASE_NAME` | Le nom exact de la nouvelle base |

Retirer `CIT_ALLOW_DESTRUCTIVE_DEMO_SEED` **immédiatement après**.

---

## 6. Validation

| # | Contrôle | Attendu |
|---:|:---|:---|
| 1 | `__EFMigrationsHistory` | **25 lignes** |
| 2 | `has-pending-model-changes` | 0 |
| 3 | Démarrage de l'application | Sans erreur |
| 4 | Connexion du compte d'administration | Réussit |
| 5 | Habilitations de ce compte | 54 |
| 6 | Lignes orphelines | 0 |
| 7 | Suite de tests complète | 100 % |

Le contrôle 1 est celui qui distingue cette base de la précédente.

---

## 7. Retour arrière

L'ancienne base n'a pas été supprimée : il suffit de rebasculer la chaîne de connexion. Ne
supprimer l'ancienne qu'après plusieurs jours d'usage de la nouvelle.

---

## 8. Après la reconstruction

Procéder à la **rotation du mot de passe** (`docs/70`). L'ordre importe : une reconstruction ne doit
pas ramener un compte au secret compromis.

**Statut : `NOT_EXECUTED` — procédure prête, exécution humaine.**
