# Analyse d'écarts — préparation Staging / UAT

> **Document** : `docs/67-staging-readiness-gap-analysis.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Objet** : consigner ce que cette campagne de préparation a trouvé, sans rien corriger en
> silence et sans rien minimiser.

---

## Légende

| Classe | Signification |
|:---|:---|
| `GAP_PRODUIT` | Écart dans le code ou la configuration livrée |
| `GAP_DOC` | Écart de documentation |
| `GAP_ENV` | Écart d'environnement — externe au projet |
| `GAP_DECISION` | Décision attendue, non prise |

---

## 1. Écarts découverts pendant cette campagne

Ces quatre points n'étaient consignés nulle part avant ce jour. Tous ont été vérifiés dans le code.

### G-01 — `Notifications:Reessai` absent des configurations Staging et Production

| | |
|:---|:---|
| **Classe** | `GAP_PRODUIT` |
| **Statut** | **`OPEN` — `CONFIGURATION_BLOCKER`** |
| **Gravité** | **Bloquante pour le déploiement** |
| **Constat** | `appsettings.Development.json` déclare `Notifications:Reessai` ; ni `appsettings.json`, ni `appsettings.Staging.json`, ni `appsettings.Production.json` ne le portent |
| **Effet** | `ParametresReessaiNotification.Valider()` lève une `InvalidOperationException` à la construction d'un service de fond, au démarrage de l'hôte. **L'application ne démarre pas.** |
| **Vérification** | `DependencyInjection.cs:343-354` ; `ReessayerEnvoisNotificationUseCase.cs:22-38` |

**Ce n'est pas un défaut de conception.** L'absence est délibérée et documentée dans le fichier de
développement : le CDC (ERR-02) exige une nouvelle tentative sans en fixer ni le nombre ni la
cadence, et le code refuse d'inventer une valeur métier. L'échec est explicite et nomme la clé.

**L'écart est ailleurs** : cette contrainte de démarrage n'apparaissait dans aucun document de
déploiement. Un exploitant aurait déployé, constaté un service qui ne démarre pas, et cherché la
cause dans l'infrastructure.

**Traitement appliqué** : consigné dans `docs/55 § 1`, `docs/59 § 2`, `docs/65 § 5.1`, `docs/66 §
3.3b`, et contrôlé par `precheck.ps1`.
**Non corrigé dans le code** : renseigner ces valeurs serait décider à la place du métier.

---

### G-02 — La purge de rétention n'est déclenchée par aucun code applicatif

| | |
|:---|:---|
| **Classe** | `GAP_PRODUIT` |
| **Statut** | **`OPEN` — `OPEN_PRODUCT_RISK`** |
| **Gravité** | Majeure |
| **Constat** | `GestionnaireRetentionService` est enregistré en `AddScoped` (`DependencyInjection.cs:429`). Aucun code de production ne le résout — **seuls les tests l'invoquent** |
| **Effet** | La purge des données échues ne s'exécute jamais d'elle-même |
| **Vérification** | Recherche exhaustive des références dans `src/` et `tests/` |

Le service **fonctionne** : neuf tests couvrent son comportement, gel légal compris. Ce qui manque
est son déclenchement — ni service de fond, ni tâche planifiée, ni commande d'administration.

**Effet en recette** : le scénario de rétention de `docs/61 § 4` ne s'observera pas spontanément.

**Non corrigé.** Il faudrait choisir une cadence d'exécution, et le CDC n'en fixe aucune. Inventer
une périodicité de purge serait un choix métier déguisé en correctif technique — sur des données
dont la suppression est irréversible.

**Décision attendue de la DSI** : périodicité de la purge, et mode de déclenchement (service de fond
ou tâche planifiée externe).

---

### G-03 — La durée des requêtes n'est journalisée nulle part

| | |
|:---|:---|
| **Classe** | `GAP_PRODUIT` |
| **Statut** | **`OPEN` — `OBSERVABILITY_GAP`** |
| **Gravité** | Mineure |
| **Constat** | `MetrologiePerformanceMiddleware` injecte un `ILogger` et **ne s'en sert jamais** ; il n'écrit que l'en-tête `Server-Timing` |
| **Effet** | Le temps de réponse n'est observable que par un client lisant l'en-tête au moment de l'appel. Aucune trace ne subsiste côté serveur |
| **Vérification** | `Middlewares/MetrologiePerformanceMiddleware.cs` |

**Effet en recette** : « c'était lent hier vers 15 h » ne laissera aucune trace exploitable.

**Palliatif sans modification du produit** : activer la journalisation des requêtes du serveur
d'hébergement, qui conserve URL, code et durée. Consigné dans `docs/63 § 4`, action 2.

**Non corrigé** : ajouter une journalisation exigerait de choisir un niveau et un seuil, et
modifierait le comportement d'exécution hors du périmètre de cette campagne.

---

### G-04 — Collision de numérotation documentaire

| | |
|:---|:---|
| **Classe** | `GAP_DOC` |
| **Gravité** | Cosmétique |
| **Constat** | Deux documents portent le préfixe `33-` : `33-post-f017-dsi-decision-gate.md` et `33-post-f017-global-cdc-matrix.md` |
| **Effet** | Une référence à « docs/33 » est ambiguë |

**Non corrigé** : renommer un document déjà référencé par d'autres casserait les renvois existants.
À traiter par une passe de cohérence documentaire, pas au détour d'une autre campagne.

---

## 2. Écarts déjà connus, confirmés

| Réf. | Écart | Classe | Statut |
|:---|:---|:---|:---|
| C-01 | `Stockage:RepertoireDocuments` déclaré dans aucun `appsettings` → repli sur `%TEMP%` | `GAP_PRODUIT` | Consigné `docs/54`, `docs/55` — **panne différée et silencieuse** |
| C-02 | Antivirus lié à `MpCmdRun.exe`, donc à Windows | `GAP_PRODUIT` | Consigné `docs/58 § 4` — discrimine les options de déploiement |
| C-03 | Aucun `Dockerfile` dans le dépôt | `GAP_PRODUIT` | Consigné `docs/58 § 3` |
| C-04 | Comportement multi-instance des cinq services de fond jamais vérifié | `GAP_PRODUIT` | Consigné `docs/58 § 6` |
| C-05 | Trois rapports de certification F016 en `NOT EXECUTED` | `GAP_ENV` | Consigné `docs/60` |
| C-06 | Module Tarification n'utilise pas `IHorloge` — 5 emplacements | `GAP_PRODUIT` | Consigné `docs/35` |
| C-07 | `MaFile.razor` : page routable, morte et défectueuse | `GAP_PRODUIT` | Consigné `docs/35` — non supprimée |

**C-01 reste le risque technique le plus élevé de ce déploiement** : le dépôt réussit, la base garde
la référence, et le fichier disparaît plus tard. Rien n'alerte au moment de l'erreur.

---

## 3. Écarts d'environnement — hors projet

| Réf. | Écart | Détenteur |
|:---|:---|:---|
| E-01 | Aucun environnement Staging | Exploitation |
| E-02 | Aucune instance SQL de recette | Exploitation |
| E-03 | Aucun répertoire de stockage persistant | Exploitation |
| E-04 | Aucun certificat ni nom de domaine | Exploitation |
| E-05 | Aucun flux réseau ouvert | Exploitation |
| E-06 | Aucune journalisation d'hébergement | Exploitation |
| E-07 | Aucune sauvegarde configurée | Exploitation |

---

## 4. Décisions attendues

| Réf. | Décision | Bloque | Référence |
|:---|:---|:---|:---|
| D-01 | Option de déploiement | Tout le déploiement | `PC-13`, `docs/58` |
| D-02 | **Profil de charge cible** | NFR-04 | `PC-01`, `docs/60` |
| D-03 | Heures ouvrées et fenêtre d'observation | NFR-03 | `docs/60` |
| D-04 | Durée du tir d'endurance | NFR-10 | `docs/60` |
| D-05 | **Paramètres de réessai des notifications** | **Le démarrage** | G-01 |
| D-06 | **Périodicité de la purge de rétention** | G-02 | G-02 |
| D-07 | Durée d'expiration de session | Rien — repli en place | `docs/55 § 3` |
| D-08 | Validité d'invitation | Rien — repli en place | `docs/55 § 5` |
| D-09 | Extensions et taille de fichier | Rien — défauts en place | `docs/55 § 3` |
| D-10 | Fournisseur e-mail | COM-01 | `PC-16`, `docs/44` |
| D-11 | Fournisseur SMS / WhatsApp | ENR-04 | `PC-16`, `docs/44` |
| D-12 | Fournisseur de modèle de langage | IA-01 à IA-07 | `PC-18`, `docs/45` |
| D-13 | Hébergement souverain | NFR-09 | `docs/43` |
| D-14 | Rétention des journaux | Rien | `docs/63` |

**Les deux seules décisions qui bloquent le démarrage sont D-05 et, indirectement, C-01.** Toutes
les autres bloquent une exigence, une campagne ou une mise à l'échelle — pas le lancement.

---

## 5. Ce que cette campagne n'a **pas** fait

Pour que le périmètre soit sans ambiguïté :

| Action | Statut |
|:---|:---|
| Modification du code applicatif | **Aucune** |
| Création ou modification de migration | **Aucune** |
| Modification d'un test | **Aucune** |
| Modification du score de conformité CDC | **Aucune** |
| Renseignement d'un rapport de certification F016 | **Aucun** — les trois restent `NOT EXECUTED` |
| Invention d'une valeur métier absente du CDC | **Aucune** |
| Commit ou push | **Aucun** |
| Ouverture d'une Feature 018 | **Aucune** |

**Aucun fichier suivi par Git n'a été modifié** : `git diff --stat` est vide. Tous les fichiers
touchés — `docs/54` à `docs/67` et `scripts/staging-precheck/` — ont été créés par cette campagne.
Les retouches apportées en cours de route à `docs/55`, `docs/59` et au paquet de précontrôles
servaient à y consigner l'écart G-01, découvert après leur première rédaction.

---

## 6. Registre des risques ouverts

**Les trois écarts produits découverts restent `OPEN`.** Aucun n'a été corrigé, et aucun ne doit
être présenté comme traité tant que son détenteur n'a pas tranché.

| Réf. | Type | Détenteur | Action attendue | Statut |
|:---|:---|:---|:---|:---|
| **G-01** | `CONFIGURATION_BLOCKER` | DSI / Métier / Exploitation | Fournir `Notifications:Reessai:NombreTentativesMax` et `DelaiEntreTentatives` pour Staging et Production | **`OPEN`** |
| **G-02** | `OPEN_PRODUCT_RISK` | Métier + DSI | Décider la cadence et le mode de déclenchement de la purge de rétention | **`OPEN`** |
| **G-03** | `OBSERVABILITY_GAP` | IT Solution / Exploitation | Décider si une journalisation serveur de la latence, ou un APM, est requise | **`OPEN`** |

### Pourquoi aucun n'est corrigé ici

| Réf. | Correction évidente | Pourquoi elle serait fautive |
|:---|:---|:---|
| G-01 | Écrire une valeur dans `appsettings.Staging.json` | Fixerait une cadence de renvoi que le CDC laisse au métier |
| G-02 | Ajouter un service de fond de purge | Fixerait une périodicité de suppression **irréversible** que personne n'a choisie |
| G-03 | Ajouter un `LogInformation` dans le middleware | Fixerait un niveau et un seuil de journalisation non arbitrés, et modifierait le comportement d'exécution |

Dans les trois cas, la correction technique est triviale et la décision qu'elle emporte ne l'est
pas. **C'est la décision qui manque, pas le code.**

---

## 7. Synthèse

| Classe | Nombre |
|:---|---:|
| `GAP_PRODUIT` découverts | **3** |
| `GAP_DOC` découverts | 1 |
| `GAP_PRODUIT` déjà connus, confirmés | 7 |
| `GAP_ENV` | 7 |
| `GAP_DECISION` | 14 |

**Trois risques produits restent ouverts** — G-01, G-02, G-03. Ce document ne conclut pas à
l'absence de risque produit, et aucune ligne de ce lot ne doit être lue ainsi.

Cela dit, **aucun de ces trois écarts n'est un défaut de conception.** G-01 et G-02 sont deux
manifestations de la même règle, appliquée avec constance dans tout le projet : **ne jamais inventer
une valeur métier que le CDC ne fixe pas**. Le code préfère échouer explicitement ou ne rien faire,
plutôt que de deviner.

Cette rigueur a un prix — deux paramètres à fournir avant le premier démarrage — et une contrepartie :
aucune donnée n'a été purgée selon une périodicité que personne n'a choisie, et aucun envoi n'a été
relancé selon une cadence que personne n'a validée.

**C'est le bon arbitrage.** Il fallait seulement qu'il soit écrit quelque part.
