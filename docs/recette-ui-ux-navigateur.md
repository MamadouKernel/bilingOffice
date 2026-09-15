# Recette UI/UX navigateur

> **L'application a été réellement exécutée et observée dans un navigateur.**
> Ce rapport n'est plus une analyse statique — mais sa couverture est **partielle**, et la raison
> est donnée au § Comptes UAT.

---

## Environnement

| | |
|---|---|
| **Environnement** | **UAT / RECETTE** — isolé |
| Base de données | `CIT_UAT_RECETTE` sur `(localdb)\MSSQLLocalDB` — **créée vide pour cette recette** |
| Chaîne de connexion | posée en `dotnet user-secrets` sur le projet Web — **hors dépôt**, aucun fichier versionné modifié |
| URL | `https://localhost:7097` |
| Commit | `1c3cd38260c3b196de8641b8ec1d5a89f27a9762` + remédiation UI/UX P0/P1 non committée |
| Date | 2026-09-06 |
| Migrations | **37 migrations appliquées sur `CIT_UAT_RECETTE` uniquement**, par `MigrateAsync` au démarrage |

### Isolation — vérifiée avant démarrage

| Garantie | État | Preuve |
|---|---|---|
| Base strictement de test | ✅ | Base neuve, nom dédié, créée par la recette |
| Aucune donnée de production | ✅ | Base vide au départ ; aucune restauration, aucun import |
| Aucune donnée personnelle réelle | ✅ | Aucune donnée n'a été créée du tout — voir § Comptes UAT |
| Migrations ciblant cette base seule | ✅ | Journal de démarrage : chaque `INSERT INTO [__EFMigrationsHistory]` sur `CIT_UAT_RECETTE` |
| **Aucun SMS / WhatsApp / e-mail réel** | ✅ **par construction** | Les trois canaux externes sont `CanalJournalise` — aucun fournisseur n'est configuré (PC-16 non arbitré). L'envoi réel est **impossible**, pas seulement désactivé |

**L'isolation est garantie.** La condition d'arrêt du § 1 n'est pas déclenchée.

---

## Comptes UAT

> ### ⚠️ AUCUN COMPTE N'A PU ÊTRE CRÉÉ — LA RECETTE AUTHENTIFIÉE N'A PAS EU LIEU

Au démarrage, le journal de l'application indique :

```
DevelopmentSeeder : Amorçage de démonstration :
  Variable « CIT_TEST_USERS_PASSWORD » absente : amorçage ignoré.
  Aucun mot de passe n'est déduit ni codé en dur.
```

### Pourquoi, exactement

`DevelopmentSeeder` lit son secret par `Environment.GetEnvironmentVariable("CIT_TEST_USERS_PASSWORD")`.
Or `src/CIT.Plateforme.Web/Properties/launchSettings.json` — **fichier versionné** — force cette
variable à la chaîne vide dans les deux profils :

```json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development",
  "CIT_TEST_USERS_PASSWORD": ""
}
```

Une variable d'environnement définie ailleurs est donc écrasée au lancement. Et parce que le seeder
lit l'environnement et non la configuration, `user-secrets` ne peut pas l'alimenter.

**Trois voies existaient, aucune n'était acceptable dans cette phase :**

1. Modifier `launchSettings.json` — c'est un **fichier versionné**, et y inscrire un mot de passe
   contredirait l'intention explicite de son auteur (« Aucune valeur par défaut, jamais »).
2. Écrire `SeedRecetteUat` — la consigne le demande (§ 2), mais il se heurterait **au même blocage** :
   il lui faudrait aussi une variable que `launchSettings.json` neutralise.
3. Réutiliser `DevelopmentSeeder` tel quel — **impossible** : il crée un compte au nom et à l'adresse
   d'une personne réelle (`m.konate@cotedivoireterminal.com`), ce que la consigne interdit
   formellement.

**Conséquence assumée : les 17 profils UAT n'existent pas, et aucun parcours authentifié n'a été
exécuté.** Je ne les présente pas comme testés.

### Ce qui a donc été réellement recetté

Les **surfaces publiques**, qui ne demandent aucun compte — et qui se trouvent être les premières
qu'un utilisateur rencontre :

| Surface | Route |
|---|---|
| Accueil | `/` |
| Connexion | `/comptes/connexion` |
| Inscription transporteur | `/comptes/inscription-transporteur` |
| Simulation proforma | `/simulation/proforma` |

---

## Données UAT

**Aucune donnée métier n'a été créée.** Sans compte, aucun BL, booking, ticket, document, simulation
enregistrée, avoir, réclamation ni enrôlement ne pouvait être produit par l'interface.

La base `CIT_UAT_RECETTE` ne contient que le schéma et les référentiels d'amorçage additifs
(libellés de statut) — **aucune donnée personnelle, fictive ou non**.

---

## Matrice des parcours

| Parcours | Profil | Résultat | UI | Responsive | Sécurité | Statut |
|---|---|---|---|---|---|---|
| Accueil et orientation | Anonyme | **Défaut confirmé** | 🔴 contraste | ✅ | ✅ | **EXÉCUTÉ** |
| Connexion (affichage) | Anonyme | Conforme | ✅ | ✅ | ✅ | **EXÉCUTÉ** |
| Inscription transporteur (affichage) | Anonyme | Conforme | ✅ | ✅ **excellent** | ✅ | **EXÉCUTÉ** |
| Simulation proforma | Anonyme | **Impasse confirmée** | 🔴 | ✅ | ✅ refus correct | **EXÉCUTÉ** |
| Client normal — Import | — | — | — | — | — | **NON EXÉCUTÉ** |
| Client en compte | — | — | — | — | — | **NON EXÉCUTÉ** |
| Client deposit | — | — | — | — | — | **NON EXÉCUTÉ** |
| Client IES | — | — | — | — | — | **NON EXÉCUTÉ** |
| Export / Draft IN | — | — | — | — | — | **NON EXÉCUTÉ** |
| Customer Complaint | — | — | — | — | — | **NON EXÉCUTÉ** |
| Avoir / Exonération | — | — | — | — | — | **NON EXÉCUTÉ** |
| Enrôlement (parcours) | — | — | — | — | — | **NON EXÉCUTÉ** |
| Agent / files | — | — | — | — | — | **NON EXÉCUTÉ** |
| Administrateur / actions de masse | — | — | — | — | — | **NON EXÉCUTÉ** |
| Parcours négatifs authentifiés | — | — | — | — | — | **NON EXÉCUTÉ** |

**4 parcours exécutés sur 15.**

---

## Desktop

Testé en **1366×768**. *(1920×1080 non testé : le volet navigateur mesure 662×602 et redimensionne
à l'échelle ; au-delà de 1366 la lecture n'apportait rien de plus.)*

| Surface | Thème clair | Thème sombre |
|---|---|---|
| Accueil | ⚠️ voir défauts | 🔴 **4 textes illisibles** |
| Connexion | ✅ | ✅ **0 défaut de contraste** |
| Simulation | ✅ | ✅ **0 défaut de contraste** |
| Inscription transporteur | ✅ | ✅ |

---

## Tablette

**NON TESTÉE.** Les tailles 1024×768 et 768×1024 n'ont pas été éprouvées — le budget d'inspection a
été porté sur le mobile, désigné prioritaire par la consigne (§ 13), et sur les défauts découverts en
desktop.

---

## Mobile — 390×844

**C'est le meilleur résultat de cette recette, et il contredit mon audit statique.**

| Surface | Débordement horizontal | Rendu | Verdict |
|---|---|---|---|
| Inscription transporteur | **aucun** (doc 390 px = viewport) | Formulaire pleine largeur, labels lisibles, CTA pleine largeur | ✅ **OK** |
| Connexion (sombre) | **aucun** | Carte sombre, champs confortables, bouton net | ✅ **OK** |
| Simulation (refus) | **aucun** | Page de refus centrée, lisible, deux sorties | ✅ **OK** |

Mon audit statique classait le mobile « incertain », en s'appuyant sur un seul point de rupture
(575.98px) et 12 `d-none`. **L'observation le dément** : la grille Bootstrap et les cartes se
comportent correctement à 390 px, sans débordement.

**Réserve** : les pages testées sont des formulaires simples. **Aucun tableau n'a pu être observé en
mobile** — les 55 tableaux de l'application sont tous derrière une authentification. Le risque
identifié dans l'audit statique (tableaux à 5-6 colonnes) **reste entier et non vérifié**.

### Zones tactiles

| Page | Boutons < 44 px de haut |
|---|---|
| Simulation | **2 sur 4** |
| Connexion | **1 sur 6** |

En deçà de la recommandation usuelle de 44 px.

---

## Client · Agent · Admin · Transporteur

**NON EXÉCUTÉS** — aucun compte. Voir § Comptes UAT.

Ce que la recette n'a donc **pas** pu vérifier, et qui reste ouvert :

- les actions de masse de `ComptesAgents` (modale, toast, double-clic, auto-retrait) — **la
  remédiation UX-P0 n'a jamais été vue à l'écran** ;
- la pagination du journal d'audit ;
- le Draft IN à l'écran ;
- le cloisonnement IES au niveau champ ;
- les tableaux en mobile ;
- les états vides, de chargement et d'erreur des surfaces authentifiées.

---

## Import · Export · Simulation

**Import et Export : NON EXÉCUTÉS.**

**Simulation : exécutée, et c'est là qu'est le second défaut confirmé.**

---

## Accessibilité

Mesures réelles, thème sombre, 1366×768 — scanner de contraste appliqué à tout texte visible.

| Page | Textes sous 3:1 |
|---|---|
| Connexion | **0** |
| Simulation | **0** |
| **Accueil** | **14 signalés — 4 confirmés** |

### Limite de la mesure, à connaître

Mon scanner remonte l'arbre jusqu'au premier `background-color` non transparent. **Il ignore les
dégradés et les images de fond.** Sur le héros de l'accueil — texte blanc sur dégradé bleu foncé,
visuellement lisible et vérifié par capture — il produit donc des **faux positifs** (ratios de 1,00
à 1,13 sur des textes parfaitement lisibles).

**N'ont été retenus que les cas où le fond résolu est une couleur pleine réelle, ET où la capture
d'écran confirme l'illisibilité.** Sur 14 signalements, **4 sont confirmés**.

### Autres observations

| Point | Constat |
|---|---|
| Navigation Tab, focus visible, Échap | **NON TESTÉS** — non atteints dans le budget d'inspection |
| Labels | Connexion : 2 labels pour 4 champs *(dont un champ masqué et une bascule de visibilité)* — à confirmer |
| Zones tactiles | 2/4 et 1/6 boutons sous 44 px |

**Aucune conclusion de conformité WCAG n'est possible sur cette base.**

---

## Responsive

| Classe | Verdict | Fondement |
|---|---|---|
| **Desktop 1366×768** | ✅ **OK** | Observé sur 4 surfaces |
| Desktop 1920×1080 | ⚪ non testé | — |
| **Tablette** | ⚪ **NON TESTÉE** | — |
| **Mobile 390×844** | ✅ **OK sur les surfaces publiques** | Observé, aucun débordement horizontal |
| Mobile — tableaux | ⚠️ **NON VÉRIFIÉ** | Tous derrière authentification |

---

## Erreurs

Un seul cas d'erreur a pu être observé, et il est **bien traité** : la page de refus d'accès de la
simulation présente un titre clair (« Accès Refusé »), un code (`CIT-SEC-403`), une explication en
français métier, un conseil d'orientation, et **deux sorties** (retour à l'accueil, tableau de bord).

C'est un bon écran d'erreur. Le problème n'est pas sa qualité — c'est qu'il ne devrait pas apparaître
là. Voir UI-R01.

---

## Défauts

### UI-R01 — L'appel à l'action principal de la page publique mène à un refus

| | |
|---|---|
| **Priorité** | **UX-P1** |
| **Profil** | Anonyme |
| **Taille écran** | Toutes (observé en 1366×768 et 390×844) |
| **Étapes** | Ouvrir `/` → cliquer « Simuler une Proforma » *(un des trois CTA principaux du héros, répété plus bas en « Calculer une proforma »)* |
| **Attendu** | La simulation s'ouvre — `SurfaceRoutableTests` inscrit cette page parmi les pages publiques assumées avec la raison *« SIM-01 : simulation accessible avant tout parcours »* |
| **Observé** | **« Accès Refusé — Vous devez être connecté et rattaché à une entreprise pour simuler une proforma. »** |
| **Analyse** | La route est publique, mais la requête refuse l'appelant anonyme. **Le refus serveur est correct** au regard de RG-SIM-01 (« *le client* peut lancer une simulation ») — ce qui est incohérent, c'est de proposer l'action à un visiteur anonyme, et d'avoir justifié l'ouverture publique de la route par l'inverse |
| **Impact** | Le principal appel à l'action de la vitrine est une impasse. Un prospect qui vient estimer un coût avant de créer un compte se heurte à un mur |
| **Effort** | **S** — soit masquer le CTA aux anonymes, soit ouvrir réellement la simulation sans compte. **C'est un arbitrage métier, pas un correctif technique** |

### UI-R02 — Thème sombre : les cartes d'orientation de l'accueil sont illisibles

| | |
|---|---|
| **Priorité** | **UX-P1** |
| **Profil** | Anonyme |
| **Taille écran** | Toutes, **uniquement en thème sombre** |
| **Étapes** | Navigateur en `prefers-color-scheme: dark` → ouvrir `/` → faire défiler jusqu'à « Vous n'avez pas de compte ? Choisissez votre parcours » |
| **Attendu** | Les trois titres de carte lisibles |
| **Observé** | Texte `rgb(232,238,244)` sur fond `rgb(255,255,255)` — **contraste 1,17:1** (WCAG AA exige 4,5:1) |
| **Confirmé par** | Styles calculés **et** capture d'écran — les titres sont invisibles |
| **Éléments touchés** | « Nouvelle Entreprise » · « Collaborateur d'Entreprise » · « Transporteur / Chauffeur » (1,17:1) · « Comment obtenir votre compte entreprise en 3 étapes ? » (1,12:1) |
| **Cause** | Les cartes fixent leur fond en dur (`#ffffff`, `#f8fafc`) tandis que le texte suit le token de thème `--ink`, qui bascule au quasi-blanc en sombre. En clair, le même titre mesure **17,64:1** |
| **Impact** | Les trois cartes qui orientent un nouvel utilisateur — dont **« Transporteur / Chauffeur »** — sont sans titre lisible. C'est la porte d'entrée de la plateforme |
| **Effort** | **S** — remplacer les fonds codés en dur par les tokens de thème sur cette page |

### UI-R03 — Zones tactiles sous 44 px

| | |
|---|---|
| **Priorité** | **UX-P2** |
| **Profil** | Anonyme, mobile |
| **Observé** | Simulation : 2 boutons sur 4 · Connexion : 1 sur 6, de hauteur < 44 px |
| **Effort** | **XS** |

---

## Correction de l'audit statique

**Mon audit statique avait l'indice de UI-R02 et en a mal jugé la portée.**

Le défaut UI-16 relevait « ~40 couleurs hexadécimales dont plusieurs doublonnent des tokens » et le
classait **UX-P3**, avec ce commentaire : *« Aucun impact direct. Un changement de charte demanderait
de reprendre ces pages une par une. »*

C'était faux. Un fond codé en dur combiné à un texte tokenisé ne produit pas un défaut de charte :
il produit du **texte invisible** dès que le thème bascule. La sévérité aurait dû être P1, et seule
l'exécution pouvait le montrer.

À l'inverse, **UI-14 (« mobile incertain ») était trop pessimiste** : les surfaces publiques se
comportent bien à 390 px.

---

## Score final

> ### ⚠️ SCORE PARTIEL — 4 SURFACES PUBLIQUES OBSERVÉES SUR 81
>
> La consigne demandait un score « qui n'est plus statique ». Il ne l'est plus — mais il ne couvre
> que les surfaces publiques. **Je ne le présente pas comme le score de l'application.**

| Poste | Note | Fondement |
|---|---|---|
| Design visuel | **12 / 15** | Observé : identité soignée, hiérarchie claire, thème sombre réellement implémenté. Pénalisé par UI-R02 |
| Cohérence | **8 / 10** | Observé sur 4 surfaces : en-têtes, cartes et boutons homogènes |
| Navigation | **7 / 10** | Non observée en authentifié. Le menu latéral n'a jamais été rendu |
| Formulaires | **8 / 10** | Observés : labels visibles, champs confortables, mots de passe avec bascule de visibilité |
| Feedback | ⚪ **non noté** | Aucune action n'a pu être exécutée |
| Responsive | **8 / 10** | Observé : aucun débordement à 390 px. Tableaux non vérifiés |
| Accessibilité | **5 / 10** | Contraste mesuré ; clavier et focus non testés |
| Gestion erreurs | **8 / 10** | Un écran de refus observé, de bonne qualité |
| Parcours utilisateur | ⚪ **non noté** | 4 sur 15 |
| Qualité frontend | **3 / 5** | Inchangé — analyse statique |
| **TOTAL PARTIEL** | **59 / 80** *(postes notés)* | ≈ **74 / 100** rapporté à l'échelle, **sur périmètre public seul** |

---

## GO / NO-GO

> ### DESIGN : 🟢 **VALIDÉ AVEC RÉSERVES**
> Vu et jugé sur pièces : l'identité visuelle est soignée, le thème sombre existe réellement.
> Réserve : UI-R02.

> ### UX : 🟠 **À REVOIR**
> Deux défauts confirmés sur quatre surfaces observées, tous deux sur la page d'accueil publique —
> et l'un d'eux rend une impasse le principal appel à l'action.

> ### RESPONSIVE : 🟢 **VALIDÉ AVEC RÉSERVES**
> **Meilleur que prévu.** Aucun débordement à 390 px sur les trois surfaces observées. Réserve
> majeure : aucun tableau n'a pu être vu en mobile.

> ### ACCESSIBILITÉ : 🟠 **À REVOIR**
> Contraste mesuré, 4 défauts confirmés. Clavier, focus et modales **non testés**.

---

> ### UI PRÊTE POUR PILOTE : 🟠 **OUI SOUS CONDITIONS**
>
> Conditions : fermer **UI-R02** *(effort S, sur une seule page)* et trancher **UI-R01** *(arbitrage
> métier)*. Les deux sont sur la page d'accueil, et un pilote commence par elle.

> ### UI PRÊTE POUR PRODUCTION : 🔴 **NON**
>
> Non pas à cause de ce qui a été vu — mais de ce qui **n'a pas pu l'être**. Onze parcours sur
> quinze n'ont pas été exécutés, aucun tableau n'a été observé en mobile, et la remédiation UX-P0
> livrée à l'étape précédente **n'a jamais été vue fonctionner**.
>
> Le blocage n'est pas technique : il tient à une variable d'environnement neutralisée dans un
> fichier versionné. Le lever est l'affaire de quelques minutes — et il conditionne toute la valeur
> d'une recette.
