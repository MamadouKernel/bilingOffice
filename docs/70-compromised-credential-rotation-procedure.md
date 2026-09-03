# Procédure de rotation d'un identifiant compromis

> **Document** : `docs/70-compromised-credential-rotation-procedure.md`
> **Établi le** : 2026-09-03, sur le commit `dbbccb3`
> **Compte concerné** : `m.konate@cotedivoireterminal.com`
> **ANCIEN SECRET** : **`COMPROMISED`**
> **ROTATION STATUS** : **`HUMAN_ACTION_REQUIRED`**

---

## 1. Pourquoi ce secret est compromis

Le mot de passe de ce compte figurait **en clair dans `DevelopmentSeeder.cs`**, à trois endroits,
et a été **poussé sur `origin/main`**. Il est resté dans l'historique d'un dépôt qui a été public
avant son passage en privé.

Trois conséquences, indépendantes les unes des autres :

| Fait | Conséquence |
|:---|:---|
| Le secret a été publié | Il doit être considéré connu de tiers, **définitivement** |
| Il reste dans l'historique Git | Le retirer du code courant ne le retire pas des commits antérieurs |
| Il a circulé dans des conversations et des historiques de shell | Sa diffusion dépasse le dépôt |

**Le retrait du code, déjà effectué, ne suffit donc pas.** Seule une rotation referme l'exposition.

Le compte porte **les 54 permissions** de la plateforme : c'est l'identifiant le plus sensible du
système.

---

## 2. Ce qui a été corrigé, et ce qui ne l'est pas

| Élément | État |
|:---|:---|
| Secret retiré de `DevelopmentSeeder.cs` | **Fait** — plus aucune valeur en dur |
| Test empêchant sa réintroduction | **Fait** — `SEED-SEC-01` échoue si un secret est réécrit en dur |
| Écrasement du mot de passe par l'amorçage | **Supprimé** — voir § 3 |
| Secret présent dans l'historique Git | **Non traité** — voir `docs/71` |
| **Rotation effective du mot de passe** | **NON FAITE — acte humain** |

---

## 3. Un piège désormais désamorcé

Avant correction, l'amorçage **réinitialisait le mot de passe d'un compte existant** à chaque
démarrage en développement :

```csharp
var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
await userManager.ResetPasswordAsync(user, resetToken, password);
```

Toute rotation aurait donc été **défaite au lancement suivant**, en silence, et le compte serait
revenu à la valeur compromise. Ce comportement est supprimé : l'amorçage crée un compte absent, il
ne reprend jamais la main sur un compte existant.

**Sans cette correction préalable, la rotation décrite ci-dessous n'aurait pas tenu.**

---

## 4. Procédure — à exécuter par un administrateur humain

### Ce que l'agent ne fait pas

L'agent **ne choisit pas, ne reçoit pas, ne saisit pas et ne modifie pas** le nouveau mot de passe.
Il ne doit apparaître ni dans cette conversation, ni dans un fichier, ni dans un journal, ni dans
une commande.

### 4.1 Choisir le nouveau secret

| Règle | Exigence |
|:---|:---|
| Longueur | **10 caractères minimum** — imposé par la plateforme |
| Composition | Majuscule, minuscule, chiffre, caractère spécial |
| **Interdit** | **L'ancien secret compromis** — celui retiré de `DevelopmentSeeder.cs` — et toute variante |
| Interdit | Mot du dictionnaire, prénom, nom de l'entreprise, année en cours |
| Réutilisation | Jamais un secret déjà employé ailleurs |
| Conservation | Gestionnaire de mots de passe, jamais un fichier ni un message |

### 4.2 Effectuer la rotation

Par le mécanisme Identity officiel, décrit en détail au **§ 4 de `docs/68`** : utilitaire de
maintenance à **saisie masquée**, passant par `UserManager`, sans jamais écrire le secret en
argument, en variable d'environnement ou en fichier.

L'utilitaire enchaîne les cinq opérations attendues :

| # | Opération | Effet |
|---:|:---|:---|
| 1 | `GeneratePasswordResetTokenAsync` + `ResetPasswordAsync` | Applique le nouveau secret **via Identity**, sous la politique de robustesse |
| 2 | `UpdateSecurityStampAsync` | **Invalide les sessions et jetons antérieurs** |
| 3 | `SetLockoutEndDateAsync(null)` | Lève un verrouillage résiduel |
| 4 | `ResetAccessFailedCountAsync` | Remet le compteur d'échecs à zéro |
| 5 | — | Aucune écriture directe dans `PasswordHash` |

**L'écriture directe de `PasswordHash` en SQL est proscrite** : elle contourne la politique de mot
de passe et ne renouvelle pas le `SecurityStamp` — les sessions déjà ouvertes resteraient valides
avec l'ancien secret compromis.

### 4.3 Vérifier

| # | Contrôle | Attendu |
|---:|:---|:---|
| 1 | Connexion avec le **nouveau** secret | Réussit |
| 2 | Connexion avec **l'ancien secret compromis** | **Échoue** |
| 3 | Session ouverte avant la rotation | Rejetée |
| 4 | `SecurityStamp` en base | **Différent** de la valeur relevée avant |
| 5 | `AccessFailedCount` | `0` |
| 6 | `LockoutEnd` | Nul |
| 7 | Habilitations | **54**, inchangées |
| 8 | Redémarrage de l'application en développement | Le mot de passe **n'est pas réécrit** |

Le contrôle 2 est celui qui démontre la rotation. Le contrôle 8 démontre que le § 3 tient.

**Relever le `SecurityStamp` avant l'opération** : sans point de comparaison, le contrôle 4 ne
prouve rien.

### 4.4 Consigner

Date, opérateur, compte visé, motif — **jamais le secret**, ni ancien, ni nouveau.

---

## 5. Réserve — cette rotation n'est pas tracée par le journal métier

Une réinitialisation menée hors application n'apparaît pas dans le journal d'audit : `UserManager`
écrit dans les tables Identity, pas dans le journal applicatif.

C'est l'écart **G-05 `ADMIN_PASSWORD_RESET_NOT_AUDITED`**, documenté dans `docs/68 § 5.3` et **non
corrigé** : le contrat d'audit d'une réinitialisation administrative n'est pas défini, et
l'inventer reviendrait à décider seul de ce qui doit être tracé.

La consignation manuelle du § 4.4 y supplée en attendant.

---

## 6. Statut

**`HUMAN_ACTION_REQUIRED`** — aucune rotation n'a été effectuée par l'agent, et aucune ne le sera.

Aucun mot de passe, ancien ou nouveau, ne figure dans ce document.
