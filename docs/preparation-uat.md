# Préparation de l'environnement UAT authentifié

*Aucun secret ne figure dans ce document, ni dans aucun fichier versionné.*

| | |
|---|---|
| Base | **`CIT_UAT_RECETTE`** sur `(localdb)\MSSQLLocalDB` |
| Comptes de recette | **18**, tous fictifs |
| Comptes hors recette dans la base | **0** — vérifié en SQL |
| Canaux externes réels | **0** — `CanalJournalise` par construction |
| Secret dans le dépôt | **aucun** |

---

## 1. Le blocage précédent, et sa cause exacte

La recette navigateur précédente n'a pas pu s'authentifier. `DevelopmentSeeder` lit son secret par
`Environment.GetEnvironmentVariable("CIT_TEST_USERS_PASSWORD")`, et
`src/CIT.Plateforme.Web/Properties/launchSettings.json` — **fichier versionné** — force cette
variable à la chaîne vide dans les deux profils de lancement. Toute variable définie ailleurs était
donc écrasée, et `user-secrets` ne pouvait pas l'alimenter puisque le seeder lit l'environnement et
non la configuration.

**La correction tient en un choix : `SeedRecetteUat` lit `IConfiguration`.**

```csharp
var motDePasse = configuration[PolitiqueRecetteUat.CleMotDePasse]      // "Uat:MotDePasse"
              ?? configuration[PolitiqueRecetteUat.CleMotDePasseEnv];  // "CIT_UAT_USERS_PASSWORD"
```

`dotnet user-secrets` alimente `IConfiguration`. Le secret vit donc dans `%APPDATA%`, **hors du
dépôt**, et `launchSettings.json` n'a pas été touché.

---

## 2. Barrières d'activation

`PolitiqueRecetteUat` — quatre conditions **cumulatives**, sur le modèle de `PolitiqueAmorcageDemo` :

| # | Condition | Comportement si absente |
|---|---|---|
| 1 | L'environnement n'est **pas** la production | Refus — *« Environnement de production : amorçage de recette refusé. »* |
| 2 | `Uat:ActiverSeed` vaut exactement `true` | Refus — l'amorçage ne se déclenche jamais tout seul |
| 3 | Un secret est fourni | Refus — aucune valeur n'est déduite ni codée en dur |
| 4 | `Uat:NomBase` désigne **la base courante** | Refus — le code ne devine jamais sur quelle base il a le droit d'écrire |

Le motif est journalisé ; **il ne contient jamais le secret**, et un test le verrouille.

### La quatrième barrière vient d'un incident, survenu pendant cette préparation

Les trois premières ne suffisaient pas, et la suite de tests l'a démontré immédiatement :

```
DevelopmentSeederTests.SeedAsync_DoitAttribuerPermissionTarificationAdministrer…
  Expected: 2
  Actual:   3
```

Le projet Web porte un `UserSecretsId`, et `WebApplicationFactory` charge **les mêmes secrets que
l'application**. Poser `Uat:ActiverSeed` sur le poste faisait donc s'exécuter l'amorçage de recette
**à l'intérieur de la suite de tests** : le compte `uat.admin.fonctionnel` s'ajoutait aux porteurs de
`tarification.administrer`, et le comptage attendu par le test tombait.

C'était une fragilité réelle, introduite par cette préparation — pas un défaut de configuration
locale. La corriger en retirant le secret de mon poste aurait laissé le piège en place pour
quiconque l'aurait reposé ensuite.

**Exiger que la base soit nommément désignée ferme la porte** : l'hôte de test travaille sur une base
éphémère au nom différent, et refuse donc l'amorçage, secrets présents ou non. Le motif de refus le
dit explicitement. Quatre tests couvrent ce cas, dont trois noms de base de substitution.

---

## 3. Les 23 profils de recette

Toutes les adresses sont sous **`@local.test`**, domaine réservé aux tests par la **RFC 6761** :
aucune ne peut correspondre à une boîte existante. Tous les noms portent le préfixe **`UAT_`**.

| Profil | Adresse | Nature | Actif |
|---|---|---|---|
| Client normal | `uat.client.normal@local.test` | Client | ✅ |
| Client en compte | `uat.client.compte@local.test` | Client | ✅ |
| Client deposit | `uat.client.deposit@local.test` | Client | ✅ |
| Client IES | `uat.client.ies@local.test` | Client | ✅ |
| Administrateur société | `uat.admin.societe@local.test` | Client | ✅ |
| Collaborateur limité | `uat.collab.limite@local.test` | Client | ✅ |
| Collaborateur global | `uat.collab.global@local.test` | Client | ✅ |
| Pilote Manifeste | `uat.pilote.manifeste@local.test` | Agent | ✅ |
| Pilote Facturation Terre | `uat.facturation@local.test` | Agent | ✅ |
| Pilote Contrôle Documentaire | `uat.controle.doc@local.test` | Agent | ✅ |
| Back Office Export | `uat.backoffice.export@local.test` | Agent | ✅ |
| Support Opérationnel | `uat.support@local.test` | Agent | ✅ |
| Agent Gate | `uat.agent.gate@local.test` | Agent | ✅ |
| Superviseur | `uat.superviseur@local.test` | Agent | ✅ |
| Administrateur fonctionnel | `uat.admin.fonctionnel@local.test` | Agent | ✅ |
| Transporteur / Chauffeur | `uat.transporteur@local.test` | Client | ✅ |
| Caisse (validation des paiements) | `uat.caisse@local.test` | Agent | ✅ |
| Comptabilité client (lettrage) | `uat.comptabilite@local.test` | Agent | ✅ |
| Agent Guichet (recherche, rendez-vous) | `uat.guichet@local.test` | Agent | ✅ |
| Avoirs & Exonérations | `uat.avoir@local.test` | Agent | ✅ |
| Support IT (N1 + N2) | `uat.support.it@local.test` | Agent | ✅ |
| **Agent sans habilitation** | `uat.agent.sansdroits@local.test` | Agent | ✅ |
| **Compte désactivé** | `uat.compte.desactive@local.test` | Agent | **❌ inactif** |

### Les deux comptes négatifs, et pourquoi ils sont construits ainsi

**`uat.agent.sansdroits`** possède **zéro habilitation** — vérifié en base. Il éprouve le refus par
absence de droit.

**`uat.compte.desactive`** est inactif **et détient deux permissions** — vérifié en base. C'est
délibéré : un compte désactivé *sans* droits serait refusé pour la mauvaise raison. Il faut qu'il
soit refusé **malgré** ses droits, pour démontrer que la garde d'identité précède la permission
(remédiation P0-ter).

### Répartitions verrouillées par test

- l'administrateur fonctionnel est le **seul** à porter `administration.comptes` ;
- les deux branches IES sont portées par des **profils distincts** — sans quoi le cloisonnement au
  niveau champ (RG-SEC-01) ne serait pas éprouvable ;
- **aucun profil client** ne porte de permission d'agent ou d'administration ;
- le transporteur est un client borné à `enrolement.soumettre`, **sans rattachement société** — ce
  qui le distingue fonctionnellement d'un client (RG-CPT-08).

---

## 4. Données métier de recette

| Objet | Quantité | Détail |
|---|---|---|
| Files de traitement | **15** | Import (6), Export (6), Complaint, Avoir/Exonération, Enrôlement |
| Sociétés | **4** | une par profil tarifaire — Normal, En Compte, Deposit, IES — toutes validées |
| Rattachements | **7** | dont les **deux portées ENT-03** sur la même société, pour être comparables |
| Dossiers | **4** | Import, Export, Réclamation, Enrôlement |
| Tickets | **6** | 2 BL Import *(un dossier, deux tickets — SOC-11)*, 1 booking Export **en Facturation, prêt pour le Draft IN**, 1 réclamation, 1 enrôlement, 1 demande d'avoir |
| Documents | 1 | `UAT_connaissement.pdf` |
| Messages | 2 | un message client **et une note interne** — pour éprouver le filtrage MSG-01 |

Le dossier d'enrôlement est **personnel** (`ClientId` = le transporteur, aucune société), forme de
propriété que la chaîne d'autorisation reconnaît nommément.

---

## 5. Idempotence — vérifiée par exécution

Deux démarrages successifs de l'application, journal à l'appui :

```
1er :  Amorçage de recette UAT terminé : 18 compte(s) créé(s), 0 déjà présent(s), 6 ticket(s).
2e  :  Amorçage de recette UAT terminé :  0 compte(s) créé(s), 18 déjà présent(s), 0 ticket(s).
```

Rien n'est dupliqué. Les clés de rapprochement sont stables : adresse pour les comptes, raison
sociale pour les sociétés, (module, nom) pour les files, numéro pour dossiers et tickets.

**Le mot de passe d'un compte existant n'est jamais réécrit** — même règle que l'amorçage de
démonstration : une rotation faite par un opérateur ne doit pas être défaite au démarrage suivant.
Seul l'état d'activité est reposé, car c'est une donnée de scénario et non un secret.

---

## 6. Vérification en base

Requêtes SQL exécutées sur `CIT_UAT_RECETTE` après le second démarrage :

| Mesure | Valeur |
|---|---|
| Comptes `@local.test` | **18** |
| dont actifs | 17 |
| dont désactivés | **1** |
| Clients / Agents | 8 / 10 |
| Sociétés `UAT_` | 4 |
| Rattachements | 7 |
| Dossiers `UAT-` | 4 |
| Tickets `UAT-` | 6 |
| Habilitations | 74 |
| Documents / Messages | 1 / 2 |
| Files | 15 |
| **Comptes HORS `@local.test`** | **0** |
| Habilitations de `uat.agent.sansdroits` | **0** |
| Habilitations de `uat.compte.desactive` | **2** |

**Aucune identité réelle n'existe dans cette base** — pas même celle du jeu de démonstration, qui
n'a pas été amorcé faute de son propre secret.

---

## 7. Sécurité

| Vérification | État |
|---|---|
| Le secret vient de la configuration locale seule | ✅ `user-secrets`, dans `%APPDATA%` |
| Aucun secret dans un fichier versionné | ✅ `launchSettings.json` **non modifié** |
| Aucun secret journalisé | ✅ le motif ne cite que la décision ; un test l'assure |
| Aucun secret dans ce document | ✅ |
| Canaux externes | ✅ `CanalJournalise` — **aucun envoi possible**, PC-16 non arbitré |
| Données de production | ✅ aucune — base créée vide |
| Production protégée | ✅ barrière n° 1, verrouillée par trois tests |
| Suite de tests protégée de l'amorçage | ✅ barrière n° 4, verrouillée par sept tests |

Le secret a été **généré aléatoirement et n'a pas été conservé** par l'assistant. Pour le relire sur
la machine de recette :

```bash
dotnet user-secrets list --project src/CIT.Plateforme.Web
```

---

## 8. Tests

**36 tests**, dans `tests/CIT.Plateforme.Infrastructure.Tests/Securite/SeedRecetteUatTests.cs`.

| Suite | Cas | Couvre |
|---|---|---|
| `PolitiqueRecetteUatTests` | 22 | Refus en production (3 casses), sans activation (5 valeurs), sans secret (3 valeurs), **sans base désignée (3 valeurs)**, **sur une autre base (3 noms)**, comparaison insensible à la casse, autorisation sur 3 environnements, absence du secret dans le motif |
| `CatalogueProfilsUatTests` | 14 | 23 profils, domaine réservé, préfixe, unicité, permissions existantes, comptes négatifs, répartition IES, cloisonnement client/agent, transporteur, 4 profils tarifaires |

L'amorçage lui-même a été éprouvé **par exécution réelle**, deux fois, avec vérification SQL.

---

## 9. Reproduire l'environnement

```bash
# 1 — pointer vers la base de recette (hors dépôt)
dotnet user-secrets set "ConnectionStrings:PlateformeDb" \
  "Server=(localdb)\MSSQLLocalDB;Database=CIT_UAT_RECETTE;Trusted_Connection=True;TrustServerCertificate=True" \
  --project src/CIT.Plateforme.Web

# 2 — poser le secret de recette (choisir une valeur forte, jamais versionnée)
dotnet user-secrets set "Uat:MotDePasse" "<valeur-forte>" --project src/CIT.Plateforme.Web

# 3 — activer explicitement l'amorçage
dotnet user-secrets set "Uat:ActiverSeed" "true" --project src/CIT.Plateforme.Web

# 4 — désigner NOMMÉMENT la base autorisée à recevoir le jeu de recette
dotnet user-secrets set "Uat:NomBase" "CIT_UAT_RECETTE" --project src/CIT.Plateforme.Web

# 4 — démarrer : migrations puis amorçage
dotnet run --project src/CIT.Plateforme.Web --launch-profile https
```

**Contrainte de mot de passe** : Identity exige 10 caractères minimum, avec majuscule, minuscule,
chiffre et caractère non alphanumérique.

**Pour désactiver l'amorçage** : `dotnet user-secrets remove "Uat:ActiverSeed" --project src/CIT.Plateforme.Web`.

---

## 10. Fichiers

### Créés

| Fichier | Objet |
|---|---|
| `Infrastructure/Persistence/PolitiqueRecetteUat.cs` | Les quatre barrières d'activation |
| `Infrastructure/Persistence/SeedRecetteUat.cs` | Le jeu de recette — 23 profils, données métier |
| `Infrastructure.Tests/Securite/SeedRecetteUatTests.cs` | **36 tests** |

### Modifiés

| Fichier | Modification |
|---|---|
| `Infrastructure/DependencyInjection.cs` | Enregistrement de `SeedRecetteUat` |
| `Web/Program.cs` | Appel de l'amorçage — **sans effet** hors configuration explicite |

**`launchSettings.json`, `appsettings*.json` : inchangés.** Aucun secret versionné.

---

## 11. Ce qui reste à faire

L'environnement est prêt ; **la recette authentifiée n'a pas été lancée**, conformément à la
consigne.

Onze parcours restent à exécuter — Client (4 profils), Export et Draft IN, Réclamation,
Avoir/Exonération, Enrôlement mobile, Agent, Administrateur, et les parcours négatifs. Trois points
méritent d'être regardés en priorité, parce qu'ils n'ont **jamais** été vus fonctionner :

1. **Les actions de masse de `ComptesAgents`** — la remédiation UX-P0 livrée précédemment : modale de
   confirmation, verrou de double-clic, toast, auto-retrait.
2. **Les tableaux en mobile** — 55 tableaux, tous derrière authentification, jamais observés à 390 px.
3. **Le Draft IN à l'écran** — le ticket Export de recette est posé en Facturation, prêt.
