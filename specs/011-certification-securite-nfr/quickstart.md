# Quickstart — Feature 011 (Certification Sécurité & Socle NFR)

> **Phase 1.** Guide de **validation orientée certification**. Chaque parcours démontre une ou plusieurs exigences CDC.
> Ce document n'est pas un guide d'implémentation : il décrit **ce qu'il faut observer** pour tenir la Closure Review.

---

## Prérequis

| Élément | État attendu |
|:---|:---|
| Baseline | **818 / 818 verts**, 0 avertissement, 0 changement de modèle en attente |
| Exécution | **sérialisée** — la suite présente une instabilité connue en parallèle (`SEC-08`, hors périmètre) |
| Base de test | Instance SQL locale disponible (parcours C uniquement) |
| Migration | **Aucune** — sa présence invaliderait la nature de certification |

```bash
dotnet build
```

```bash
dotnet test -- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=1
```

```bash
dotnet ef migrations has-pending-model-changes --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

---

## Parcours A — Authentification et verrouillage (SEC-01)

**Suite** : `SEC-CERT-01` · **Niveau** : intégration sur hôte réel

| Étape | Action | Observation attendue |
|:--|:---|:---|
| A1 | Se présenter avec des identifiants valides | Accès accordé |
| A2 | Enchaîner des tentatives erronées jusqu'au plafond configuré | Le compte se verrouille |
| A3 | Présenter le **bon** mot de passe pendant le verrouillage | **Accès refusé** |
| A4 | Demander une ressource protégée sans session | Renvoi au contrôle d'identité |

> **Résultat interdit** : accès accordé après verrouillage, ou nombre d'essais non borné.
> **Preuve Closure** : A3 est le cas décisif — il démontre que le verrouillage prime sur la validité du mot de passe.

---

## Parcours B — Cloisonnement (SEC-03)

**Suite** : `SEC-CERT-02` · **Niveau** : application + intégration

| Étape | Action | Observation attendue |
|:--|:---|:---|
| B1 | Client A consulte son propre ticket | Accès accordé |
| B2 | Client A demande le ticket de client B **en fournissant son identifiant** | **Refus côté serveur**, sans divulguer l'existence ni l'appartenance |
| B3 | Agent habilité agit dans sa file | Action acceptée |
| B4 | Agent tente une action sur une file **hors de son périmètre** | **Refus côté serveur** |

> **Résultat interdit** : ressource retournée ; message révélant l'existence de la ressource ; refus prononcé uniquement par masquage d'interface.
> **Preuve Closure** : B2 et B4 démontrent les **deux axes** du texte CDC (client ↔ client, agent ↔ file).

---

## Parcours C — Journal d'audit append-only (SEC-05)

**Suite** : `SEC-CERT-03` · **Niveau** : infrastructure, **base réelle**

| Étape | Action | Observation attendue |
|:--|:---|:---|
| C1 | Produire une action tracée | Entrée créée avec auteur et horodatage |
| C2 | Tenter de **modifier** l'entrée | **Rejet explicite** |
| C3 | Tenter de **supprimer** l'entrée | **Rejet explicite** |
| C4 | Ajouter une nouvelle entrée | Acceptée |

> **Régime R1** : ce parcours est **déjà couvert** par la suite existante. Le travail de F011 consiste à la **rattacher nominativement**, pas à la réécrire.
> **Résultat interdit** : entrée modifiée ou supprimée ; création d'un second journal.

---

## Parcours D — Expiration de session paramétrable (SEC-10)

**Suite** : `SEC-CERT-04` · **Niveau** : intégration

| Étape | Action | Observation attendue |
|:--|:---|:---|
| D1 | Démarrer l'hôte avec une durée de session **fournie par la configuration de test** | La politique de session reflète **la valeur configurée** |
| D2 | Utiliser une session dans la période | Accès accordé |
| D3 | Présenter une session dont la validité est dépassée | **Accès refusé**, ré-authentification exigée |

> **Résultat interdit** : session perpétuelle ; durée non paramétrable ; **inscription d'une durée dans la configuration de production**.
> **Preuve Closure** : D1 démontre le mot *« paramétrable »* du CDC ; D3 démontre l'effet.
> **La durée employée est technique et non contractuelle.** Le défaut technique existant n'est ni modifié, ni promu en valeur métier.

---

## Parcours E — Compte désactivé (SEC-11)

**Suite** : `SEC-CERT-05` · **Niveau** : application + intégration

| Étape | Action | Observation attendue |
|:--|:---|:---|
| E1 | Un compte agent actif produit de l'historique | Traces créées |
| E2 | Désactiver le compte | Désactivation effective |
| E3 | Le compte désactivé tente d'accéder | **Accès refusé immédiatement** |
| E4 | Consulter l'historique produit par ce compte | **Intégralement conservé** |

> **Résultat interdit** : accès maintenu après désactivation ; trace supprimée, anonymisée ou détachée du compte.
> **Preuve Closure** : E3 **et** E4 doivent être démontrés **dans le même scénario** — c'est la conjonction qui satisfait le texte CDC.
> **Hors périmètre** : la suppression de compte et l'invalidation d'une session déjà ouverte ne sont pas évoquées par le CDC — ne pas extrapoler.

---

## Parcours F — Configuration de reprise (ERR-02)

**Suite** : `ERR-CERT-01` · **Niveau** : intégration, démarrage réel
**Contrat produit** : `C11-P01` — **seule modification de produit de la Feature**

> ⚠️ **Ordre contraignant.** Ce parcours ne devient exécutable qu'après l'étape 3 de l'ordre de mise en œuvre. L'inverser ferait tomber toute la suite Web.

| Étape | Action | Observation attendue |
|:--|:---|:---|
| F0 | *(préalable)* Harnais adapté, baseline verte | 818/818 avant toute modification de produit |
| F1 | Démarrer le composant **sans** configuration de reprise | **Échec explicite du composant**, message désignant le paramètre manquant |
| F2 | Démarrer avec une configuration **invalide** | Même échec explicite |
| F3 | Démarrer avec une configuration **valide** | Composant démarré, reprise opérationnelle |
| F4 | Vérifier le reste de la plateforme en l'absence de configuration | **Le reste de la plateforme n'est pas empêché de démarrer** |

> **Résultat interdit** : démarrage réussi suivi d'une reprise silencieusement inopérante ; valeur par défaut substituée ; **échec de l'application entière**.
> **Preuve Closure** : F1 démontre le fail-fast ; F4 démontre que la portée est bien **le composant**, pas la plateforme.
> **CLAR-NOT-02 reste ouverte** : aucune valeur de reprise n'est introduite.

---

## Parcours G — Contrôle des documents déposés (ERR-03)

**Suite** : `ERR-CERT-02` · **Niveau** : application
**⚠️ Sous GATE-ERR03**

| Étape | Action | Observation attendue |
|:--|:---|:---|
| G1 | Déposer un document conforme | Accepté et rattaché |
| G2 | Déposer un **type non autorisé** | Refusé **avec message désignant le type** |
| G3 | Déposer un fichier **au-delà de la taille maximale** | Refusé **avec message désignant la taille** |
| G4 | **Volet « intégrité »** | ⚠️ **Aucun mécanisme dédié — voir gate ci-dessous** |

> **Régime R1** : G1 à G3 sont **déjà couverts** par la suite existante, **avec assertion du message**. Le travail consiste à **rattacher**.
>
> **GATE-ERR03 — à trancher avant certification** :
> - si « intégrité » = *fichier exploitable selon les mécanismes existants* → **ERR-03 reste dans F011**, gain **+11**, projection **157/188** ;
> - si « intégrité » exige un mécanisme distinct (empreinte, somme de contrôle) → **ERR-03 SORT de F011**, gain **+10**, projection **156/188**.
>
> **Interdiction absolue** : ne pas inventer une somme de contrôle pour conserver +11.

---

## Parcours H — Interface française et responsive (NFR-01, NFR-02)

**Suites** : `NFR-CERT-01`, `NFR-CERT-02` · **Niveau** : interface

### H1 — Langue (NFR-01)

| Étape | Action | Observation attendue |
|:--|:---|:---|
| H1.1 | Parcourir les écrans exposés à l'utilisateur | Libellés, messages et intitulés **en français** |

> **Résultat interdit** : libellé non francophone exposé à l'utilisateur.
> **Non exigé** : aucun score d'ergonomie, aucun seuil d'accessibilité, aucun nombre de clics — le CDC n'en fixe aucun.

### H2 — Responsive (NFR-02) — **preuve structurelle**

| Étape | Vérification | Observation attendue |
|:--|:---|:---|
| H2.1 | Directive de largeur d'affichage pour terminaux mobiles | Déclarée |
| H2.2 | Points de rupture dans la feuille de styles | Définis, distinguant affichage réduit et large |
| H2.3 | Écrans du parcours client | Emploient des primitives adaptatives, pas des largeurs fixes |
| H2.4 | Contenus larges (tableaux) | Placés dans un conteneur défilant propre |
| H2.5 | Variante réduite | Navigation et actions principales atteignables |

> **Limite déclarée, à porter en Closure Review** : cette preuve démontre que **le contrat responsive est appliqué**, non que le rendu est visuellement exempt de défaut. Le dépôt **ne dispose d'aucun navigateur automatisé** ; le rendu de composants produit du balisage sans moteur de mise en page.
> **La validation visuelle sur navigateur réel est une dette de recette**, distincte de la certification CDC.
> **Interdiction** : aucune largeur employée dans un test ne doit être présentée comme un seuil CDC.

---

## Parcours I — Calendrier ouvré et SLA (NFR-11)

**Suite** : `NFR-CERT-03` · **Niveau** : domaine + application

| Étape | Action | Observation attendue |
|:--|:---|:---|
| I1 | Déposer **en** heures ouvrées | Décompte conforme aux règles de la file |
| I2 | Déposer **hors** heures ouvrées | **Dépôt accepté** ; décompte démarrant selon les règles de la file, **pas à l'instant du dépôt** |
| I3 | Franchir une frontière de journée | Seules les plages ouvrées sont comptées |
| I4 | Traverser un jour non ouvré | Non compté |
| I5 | Traverser un **jour férié configuré** | Non compté |
| I6 | Calendrier en mode « toujours ouvert » | Décompte calendaire, **aucun horaire métier inventé** |

> **Résultat interdit** : dépôt hors heures ouvrées refusé ; décompte en heures calendaires alors que la file est paramétrée en heures ouvrées.
> **Preuve Closure** : I2 est le cas décisif — c'est la phrase littérale du CDC.
> **Interdiction** : aucun calendrier national, aucun service externe de jours fériés. Le mécanisme existe déjà et est paramétrable — le certifier, pas l'étendre.

---

## Contrôles de sortie

| Contrôle | Attendu |
|:---|:---|
| Suite complète | **≥ 818 verts**, aucun test existant supprimé, ignoré ou affaibli |
| Avertissements de compilation | **0** |
| `has-pending-model-changes` | **0** |
| Fichiers de migration | **inchangés** |
| Diff sur les entités de domaine | **aucune ligne** |
| Diff sur le contexte de persistance | **aucune ligne** |
| Fichiers de produit modifiés | **1** (point de validation ERR-02) |
| Écrans modifiés | **0** — sauf anomalie démontrée |
| Occurrences de SEC-02, SEC-07, SEC-08 hors « Hors périmètre » | **0** |
| Occurrences de largeur présentée comme seuil CDC | **0** |
| Valeurs de reprise en dur | **0** |

---

## Ce que ce quickstart ne couvre pas

| Sujet | Motif |
|:---|:---|
| Second facteur d'authentification | **SEC-08 exclue** — ne pas diagnostiquer, ne pas modifier |
| Analyse antivirale | **SEC-07 exclue** — aucun moteur, aucun fournisseur |
| RBAC au niveau des champs | **SEC-02 exclue** — PC-05 bloque la spécification |
| Validation visuelle sur navigateur | **Dette de recette** (H2) |
| Volet « intégrité » d'ERR-03 | **GATE-ERR03** ouvert |
| Valeurs de reprise (nombre, cadence, arrêt) | **CLAR-NOT-02** ouverte |
| Durée de session d'exploitation | Décision métier/DSI, hors périmètre |
