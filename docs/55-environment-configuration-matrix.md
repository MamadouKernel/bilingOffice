# Matrice de configuration par environnement

> **Document** : `docs/55-environment-configuration-matrix.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Règle** : aucune valeur Staging ou Production n'est inventée. Ce qui n'est pas connu porte un
> marqueur explicite.

---

## Légende

| Marqueur | Signification |
|:---|:---|
| `TBD_DSI` | Décision métier ou de politique — DSI |
| `TBD_INFRA` | Valeur d'infrastructure — Exploitation |
| `TBD_SECRET` | Secret — jamais dans le dépôt, jamais dans ce document |

---

## 1. Réglages critiques — à renseigner impérativement

Ces lignes conditionnent le fonctionnement. Aucune ne peut rester vide.

| Réglage | Description | DEV | STAGING | PROD | Source | Secret | Détenteur | Statut |
|:---|:---|:---|:---|:---|:---|:---|:---|:---|
| `ConnectionStrings:PlateformeDb` | Accès SQL Server | user-secrets | `TBD_SECRET` | `TBD_SECRET` | Coffre / variable | **Oui** | Infra | **BLOQUANT** |
| `Stockage:RepertoireDocuments` | Répertoire des pièces déposées | *(non défini → `%TEMP%`)* | `TBD_INFRA` | `TBD_INFRA` | Configuration | Non | Infra | **BLOQUANT** |
| `ASPNETCORE_ENVIRONMENT` | Environnement d'exécution | `Development` | **`Staging`** | **`Production`** | Variable | Non | Infra | **BLOQUANT** |
| `Administration:AdministrateurInitial:MotDePasse` | Amorçage du premier administrateur | user-secrets | `TBD_SECRET` | `TBD_SECRET` | Coffre / variable | **Oui** | DSI | **BLOQUANT** |
| `Notifications:Reessai:NombreTentativesMax` | Nombre de tentatives de renvoi | `3` *(Development uniquement)* | `TBD_DSI` | `TBD_DSI` | Configuration | Non | DSI | **BLOQUANT — empêche le démarrage** |
| `Notifications:Reessai:DelaiEntreTentatives` | Délai entre deux tentatives | `00:05:00` *(Development uniquement)* | `TBD_DSI` | `TBD_DSI` | Configuration | Non | DSI | **BLOQUANT — empêche le démarrage** |

### Pourquoi ces réglages-là

**`Stockage:RepertoireDocuments`** n'est déclaré dans aucun `appsettings.json`. Non renseigné, les
documents partent dans le répertoire temporaire du système, qui est purgé. La panne est
silencieuse et différée — c'est ce qui la rend dangereuse.

**`ASPNETCORE_ENVIRONMENT`** en `Development` déclenche le jeu de données de test et désactive
HSTS. Un Staging ainsi configuré serait à la fois peuplé de comptes fictifs et dégradé en
sécurité.

**Sans le mot de passe d'amorçage**, aucun compte ne peut administrer les comptes agents : la
plateforme démarre verrouillée de l'extérieur.

**`Notifications:Reessai` n'est déclaré que dans `appsettings.Development.json`.** Ni le fichier de
base, ni `appsettings.Staging.json`, ni `appsettings.Production.json` ne le portent — c'est
délibéré : le CDC (ERR-02) exige une nouvelle tentative sans en fixer ni le nombre ni la cadence, et
le code refuse d'inventer ces valeurs.

**Conséquence à connaître avant de déployer** : ces paramètres sont consommés par un service de
fond, construit au démarrage de l'hôte. En leur absence, `ParametresReessaiNotification.Valider()`
lève une `InvalidOperationException` et **l'application ne démarre pas**. Le message d'erreur nomme
la clé manquante.

C'est le comportement voulu — un échec explicite vaut mieux qu'une cadence devinée — mais un
déploiement Staging qui n'aurait pas fourni ces deux valeurs échouerait au lancement, avant même la
première requête. À fournir par variable d'environnement
(`Notifications__Reessai__NombreTentativesMax`, `Notifications__Reessai__DelaiEntreTentatives`) ou
par un fichier de configuration propre à l'environnement.

---

## 2. Base de données

| Réglage | Description | DEV | STAGING | PROD | Secret | Détenteur |
|:---|:---|:---|:---|:---|:---|:---|
| Serveur | Instance SQL | `localhost` | `TBD_INFRA` | `TBD_INFRA` | — | Infra |
| Base | Nom de la base | `DbBillingOffice` | `TBD_INFRA` | `TBD_INFRA` | — | Infra |
| Authentification | Mode de connexion | Intégrée Windows | `TBD_INFRA` | `TBD_INFRA` | — | Infra |
| Compte applicatif | Compte dédié | — | `TBD_INFRA` | `TBD_INFRA` | **Oui** | Infra |
| Chiffrement en transit | `Encrypt` | `False` | `TBD_INFRA` | **recommandé `True`** | — | Infra |

---

## 3. Sécurité

| Réglage | Description | DEV | STAGING | PROD | Source | Détenteur |
|:---|:---|:---|:---|:---|:---|:---|
| `Securite:ExpirationSessionMinutes` | Durée de session glissante | 30 *(repli technique)* | `TBD_DSI` | `TBD_DSI` | Configuration | DSI |
| `Securite:Fichiers:ExtensionsAutorisees` | Types acceptés | `.pdf .jpg .jpeg .png .xlsx` | `TBD_DSI` | `TBD_DSI` | `appsettings` | Métier |
| `Securite:Fichiers:TailleMaxOctets` | Taille maximale | 10 485 760 (10 Mo) | `TBD_DSI` | `TBD_DSI` | `appsettings` | Métier |
| Certificat TLS | Certificat serveur | *(dev)* | `TBD_INFRA` | `TBD_INFRA` | Infrastructure | Infra |
| HSTS | Actif hors développement | Inactif | **Actif** | **Actif** | Code | — |

**Sur l'expiration de session** : 30 minutes est un **repli technique**, pas une décision métier.
Le CDC ne fixe aucune durée. Elle doit être arbitrée.

**Sur les extensions et la taille** : les valeurs livrées sont des défauts raisonnables, non des
exigences du CDC. Elles doivent être validées par le métier — un dépôt de 10 Mo peut être trop
petit pour certaines liasses documentaires.

---

## 4. Antivirus

| Réglage | Description | DEV | STAGING | PROD | Détenteur |
|:---|:---|:---|:---|:---|:---|
| `Securite:Antivirus:Mode` | Moteur | `Defender` | `TBD_INFRA` | `TBD_INFRA` | Infra |
| `Securite:Antivirus:CheminExecutable` | Chemin de `MpCmdRun.exe` | *(détection auto)* | `TBD_INFRA` | `TBD_INFRA` | Infra |
| `Securite:Antivirus:FailClosed` | Comportement en échec | `true` | **`true`** | **`true`** | Sécurité |
| `Securite:Antivirus:TimeoutSecondes` | Délai maximal | 15 | `TBD_INFRA` | `TBD_INFRA` | Infra |

**`FailClosed` doit rester `true`.** Le passer à `false` laisserait passer des fichiers non
analysés en cas d'indisponibilité de l'antivirus — exactement ce que SEC-07 vise à empêcher.

---

## 5. Comptes et administration

| Réglage | Description | DEV | STAGING | PROD | Secret | Détenteur |
|:---|:---|:---|:---|:---|:---|:---|
| `Administration:AdministrateurInitial:Email` | Adresse du premier administrateur | *(vide)* | `TBD_DSI` | `TBD_DSI` | Non | DSI |
| `Administration:AdministrateurInitial:NomAffiche` | Nom affiché | *(vide)* | `TBD_DSI` | `TBD_DSI` | Non | DSI |
| `Comptes:ValiditeInvitationJours` | Validité d'une invitation | 7 *(repli technique)* | `TBD_DSI` | `TBD_DSI` | Non | Métier |

**7 jours est un repli technique.** Le CDC ne fixe aucune durée pour une invitation ; la valeur
existe parce qu'un jeton porteur sans échéance serait un défaut de sécurité.

---

## 6. Notifications

| Réglage | Description | DEV | STAGING | PROD | Détenteur |
|:---|:---|:---|:---|:---|:---|
| `Notifications:Reessai` | Politique de réessai des envois | `3` / `00:05:00` — **Development uniquement** | `TBD_DSI` | `TBD_DSI` | DSI |
| Canal e-mail | Fournisseur | **aucun** | `TBD_PROVIDER` | `TBD_PROVIDER` | DSI — PC-16 |
| Canal SMS / WhatsApp | Fournisseur | **aucun** | `TBD_PROVIDER` | `TBD_PROVIDER` | DSI — PC-16 |

Seul le canal In-App est opérationnel. Les autres attendent un contrat fournisseur — `docs/44`.

---

## 7. Journalisation

| Réglage | Description | DEV | STAGING | PROD | Détenteur |
|:---|:---|:---|:---|:---|:---|
| `Logging:LogLevel:Default` | Niveau général | `Information` | `TBD_INFRA` | **`Warning` recommandé** | Infra |
| `Logging:LogLevel:Microsoft.AspNetCore` | Niveau framework | `Warning` | `Warning` | `Warning` | Infra |
| Destination | Où vont les journaux | Console | `TBD_INFRA` | `TBD_INFRA` | Infra |
| Rétention | Durée de conservation | — | `TBD_DSI` | `TBD_DSI` | DSI |

En production, `Information` produit un volume important. `Warning` est le réglage usuel — à
arbitrer selon les besoins de diagnostic.

---

## 8. Réglages sans intervention

| Réglage | Valeur | Motif |
|:---|:---|:---|
| `AllowedHosts` | `*` | À restreindre au nom de domaine réel — `TBD_INFRA` |
| Culture | Français / anglais | Géré par la plateforme, bilinguisme livré |
| Mode maintenance | Piloté à l'exécution | Aucune configuration statique |
| Health checks | `/healthz`, `/ready` | Chemins fixes |

---

## 9. Récapitulatif

| Catégorie | Réglages | Dont bloquants | Dont secrets |
|:---|---:|---:|---:|
| Critiques | 6 | **6** | 2 |
| Base de données | 5 | 5 | 1 |
| Sécurité | 5 | 1 | — |
| Antivirus | 4 | 1 | — |
| Comptes | 3 | 1 | 1 |
| Notifications | 3 | *(voir § 1)* | — |
| Journalisation | 4 | 0 | — |

**Six réglages bloquent le démarrage ou l'usage.** Les autres ont des défauts fonctionnels, qu'il
reste préférable d'arbitrer plutôt que de subir.

**Deux d'entre eux empêchent littéralement l'application de démarrer** : `Notifications:Reessai:*`.
Les autres produisent une panne différée ou une dégradation, ce qui est plus insidieux mais moins
immédiat.
