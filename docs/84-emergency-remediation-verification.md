# Vérification des remédiations d'urgence

> **Document** : `docs/84-emergency-remediation-verification.md`
> **Établi le** : 2026-09-03, sur le commit `dbbccb3` + arbre de travail
> **Méthode** : preuve produite **à cette date**, par exécution. Aucun rapport antérieur n'est repris
> sur parole.

---

## INC-01 — Secret embarqué

| | |
|:---|:---|
| **Cause racine** | `DevelopmentSeeder` repliait sur un mot de passe écrit en dur quand `CIT_TEST_USERS_PASSWORD` était absente. Trois occurrences, poussées sur `origin/main`. |
| **Correction** | Repli supprimé. Le secret vient exclusivement de la variable d'environnement. La réécriture du mot de passe d'un compte existant est également supprimée. |
| **Test** | `SEED-SEC-01` balaie `src/` et échoue si un littéral est affecté à une variable évoquant un secret. `SEED-SEC-04` vérifie qu'un secret absent ne se remplace jamais. |
| **Preuve** | Recherche de l'ancien secret sur `src`, `tests`, `docs` et `scripts` → **0 occurrence**. Suite Infrastructure : 127/127. |
| **Statut** | **`FIXED_IN_TREE`** |
| **Risque résiduel** | **Élevé et hors code** : le secret reste dans l'historique Git publié. Seule la rotation humaine le referme — `docs/70`, `docs/71`. |

---

## INC-02 — Amorçage destructif non gardé

| | |
|:---|:---|
| **Cause racine** | La purge s'exécutait dès le démarrage en `Development`, sans condition. Elle a supprimé puis recréé la base de développement réelle. |
| **Correction** | `PolitiqueAmorcageDemo` : quatre barrières cumulatives — environnement `Development`, secret fourni, `CIT_ALLOW_DESTRUCTIVE_DEMO_SEED=true`, et `CIT_DEMO_DATABASE_NAME` correspondant à la base **réellement résolue**. |
| **Test** | `SEED-SEC-02/03/04`, `SEED-SAFE-01` à `05` — 20 tests, sans base ni hôte. |
| **Preuve** | Démarrage réel de l'application : `Amorçage de démonstration : Variable « CIT_TEST_USERS_PASSWORD » absente : amorçage ignoré.` puis `Application started.` Base inchangée. |
| **Statut** | **`FIXED_IN_TREE`** · `DEFAULT = DENY` |
| **Risque résiduel** | Aucun sur le comportement. La base déjà endommagée reste à traiter — `docs/72`, `docs/76`. |

---

## INC-03 — Dépôt non reproductible

| | |
|:---|:---|
| **Cause racine** | Un commit a publié six pages Razor référençant `<ChampMotDePasse>` et `App.razor` référençant `js/afficher-mot-de-passe.js`, sans versionner ni le composant ni le script. Un clone propre ne compilait pas. |
| **Correction** | Les deux ressources sont intégrées au lot proposé. |
| **Test** | `RessourcesReferenceesVersionneesTests` : tout script/style référencé doit exister sous `wwwroot`, tout composant employé doit avoir son fichier. |
| **Preuve** | Voir `docs/83 § clone propre`. |
| **Statut** | **`FIXED_IN_TREE`** — devient `RESOLVED` au commit |
| **Risque résiduel** | Le défaut se reproduira si un agent tiers commite à nouveau un arbre partiel. Le test le détecte désormais. |

---

## INC-04 — Divulgation du jeton de réinitialisation (G-04)

| | |
|:---|:---|
| **Cause racine** | `/comptes/mot-de-passe-oublie`, page publique, rendait le lien de réinitialisation — jeton compris — dans la réponse HTTP, sans garde d'environnement. |
| **Correction** | Affichage conditionné à `IsDevelopment()`. Réponse générique identique que le compte existe ou non. Hors développement, le lien n'est ni affiché ni journalisé. |
| **Test** | `SEC-RESET-01` à `08`. |
| **Preuve** | 6 des 8 échouaient avant correction, en Production **et** en Staging. **8/8 après.** |
| **Statut** | **`FIXED`** — le correctif est déjà dans `dbbccb3` ; les tests et la documentation sont dans le lot |
| **Risque résiduel** | Le parcours de réinitialisation autonome reste **indisponible** hors développement tant que PC-16 n'est pas tranché. C'est le choix fail-safe assumé : reset non distribuable plutôt que public. |

---

## INC-05 — Isolation de la base de test

| | |
|:---|:---|
| **Cause racine** | Les fabriques fixaient la base par `ConfigureAppConfiguration`. En `Development`, les user-secrets priment : les tests atteignaient `DbBillingOffice` et l'ont supprimée puis recréée. |
| **Correction** | Réenregistrement explicite du `DbContext` dans `ConfigureServices` + `GardeBaseDeTest` contrôlant la base **réellement résolue** contre une liste blanche de préfixe. |
| **Test** | `TEST-DB-01` à `05`. |
| **Preuve** | Empreinte de `DbBillingOffice` **avant** et **après** une suite Web complète : `create_date` 2026-09-03 03:19:10, Users 1, Societes 4, Tickets 1, Dossiers 2, Habilitations 63 — **identique**. Échecs Web passés de 87 à 0. |
| **Statut** | **`FIXED_IN_TREE`** · `DEVELOPMENT_DB_MUTATED_BY_TESTS = NO` |
| **Risque résiduel** | Aucun sur l'isolation. |

---

## INC-06 — `<ResourcePreloader />`

| | |
|:---|:---|
| **Cause racine** | Référence introduite dans `a9657a4`, le tout premier `App.razor`, vers un composant qui **n'a jamais existé** : `git log --diff-filter=A` ne trouve aucun ajout de fichier de ce nom, et aucune assembly référencée ne le définit. |
| **Classification** | **B — `STALE_REFERENCE`** |
| **Correction** | Référence retirée. |
| **Preuve** | Réponse HTTP servie **avant** retrait : 18 530 octets, 0 occurrence de `resourcepreloader`. **Après** : 18 524 octets, 0 occurrence. Écart de **6 octets** — l'indentation qu'occupait la balise. Elle ne produisait rien. |
| **Statut** | **`RESOLVED`** |
| **Risque résiduel** | Aucun. Le test de reproductibilité fait désormais échouer toute nouvelle référence pendante. |

---

## Synthèse

| Incident | Statut | Risque résiduel |
|:---|:---|:---|
| INC-01 | `FIXED_IN_TREE` | **Rotation humaine requise** |
| INC-02 | `FIXED_IN_TREE` | Base à reconstruire |
| INC-03 | `FIXED_IN_TREE` | — |
| INC-04 | `FIXED` | Parcours indisponible — PC-16 |
| INC-05 | `FIXED_IN_TREE` | — |
| INC-06 | `RESOLVED` | — |

**Aucun de ces incidents n'est vérifié sur parole** : chaque ligne « Preuve » correspond à une commande exécutée ce jour.
