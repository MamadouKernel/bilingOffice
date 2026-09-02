# Checklist Qualité de la Spécification — Feature 011 (Certification Sécurité & Socle NFR)

> **Document** : `specs/011-certification-securite-nfr/checklists/requirements.md`
> **Date** : 1er septembre 2026
> **Portée** : vérifie la qualité et la fidélité de `spec.md` au CDC et au Final Scope Gate. Ne vérifie **pas** l'implémentation (aucun code F011 n'existe).

## 1. Périmètre — 11 exigences exactement

- [x] SEC-01 présente, avec son texte CDC intégral (spec.md §2)
- [x] SEC-03 présente, avec son texte CDC intégral
- [x] SEC-05 présente, avec son texte CDC intégral
- [x] SEC-10 présente, avec son texte CDC intégral
- [x] SEC-11 présente, avec son texte CDC intégral
- [x] SEC-12 présente, avec son texte CDC intégral
- [x] ERR-02 présente, avec son texte CDC intégral
- [x] ERR-03 présente, avec son texte CDC intégral
- [x] NFR-01 présente, avec son texte CDC intégral
- [x] NFR-02 présente, avec son texte CDC intégral
- [x] NFR-11 présente, avec son texte CDC intégral
- [x] **11/11 exigences présentes**, aucune omise, aucune ajoutée
- [x] **11 Must, 0 Should, 0 Could** — priorités vérifiées contre le CDC
- [x] Textes repris depuis `docs/cdc_text.txt` (source contractuelle), non depuis la matrice secondaire

## 2. Exclusions du Final Scope Gate

- [x] **SEC-02 absente du périmètre** — n'apparaît dans aucune exigence, User Story, critère d'acceptation ni suite TEST-SPEC
- [x] SEC-02 figure uniquement en §3 « Hors périmètre », avec son motif (PC-05, texte CDC « traitements parallèles »)
- [x] **Aucun champ réservé IES n'est inventé**
- [x] **Aucun moteur générique de Field-Level Security n'est spécifié**
- [x] **SEC-07 absente du périmètre** — n'apparaît dans aucune exigence, User Story, critère ni suite
- [x] SEC-07 figure uniquement en §3, avec son motif (aucun moteur antivirus réel)
- [x] **Aucun antivirus n'est spécifié**, **aucun fournisseur n'est nommé**
- [x] **Aucune validation de type ou de taille n'est requalifiée en analyse antivirale** — frontière explicite portée en §8
- [x] **SEC-08 absente du périmètre** — n'apparaît dans aucune exigence, User Story, critère ni suite
- [x] SEC-08 figure uniquement en §3, avec son motif (contamination inter-tests, cause indéterminée)
- [x] **L'authentification et le 2FA ne sont pas modifiés** par la spécification
- [x] Les trois exclusions ne comptent dans **aucun gain de couverture** (spec.md §14)

## 3. Nature de certification — non-duplication

- [x] La spécification énonce explicitement que F011 **ne réimplémente aucun mécanisme existant** (spec.md §1, §4)
- [x] Pour chaque exigence : texte CDC, mécanisme existant, preuve actuelle, preuve manquante (spec.md §5)
- [x] Aucune exigence n'est traitée par un simple « déjà implémenté » — chacune dispose d'une fiche ou d'une ligne de traçabilité
- [x] Interdiction de duplication de sécurité ou de logique métier formalisée (§4)
- [x] Interdiction d'absorber le périmètre AVE / CON / Notifications / Finance (§4)

## 4. Contraintes architecturales

- [x] **Aucune migration** spécifiée — règle d'arrêt formalisée (§4)
- [x] **Aucune nouvelle table** spécifiée
- [x] **Aucune nouvelle entité métier** spécifiée
- [x] **Aucune nouvelle fonctionnalité UI** spécifiée
- [x] Règle d'arrêt explicite si la conception conclut à la nécessité d'une migration (§4, CA-04)

## 5. SEC — fiches de certification

- [x] Les 6 fiches SEC portent : acteur, règle protégée, comportement attendu, comportement interdit, contrôle serveur, scénario positif, scénario négatif (§6)
- [x] Principe transverse formalisé : **une protection uniquement UI est insuffisante** (§6)
- [x] SEC-01 : scénario négatif de verrouillage, avec refus **postérieur au bon mot de passe**
- [x] SEC-03 : preuve de cloisonnement sur les **deux axes** (client ↔ client, agent ↔ file), accès direct par identifiant inclus
- [x] SEC-05 : scénario négatif couvrant **modification ET suppression**
- [x] SEC-10 : paramétrabilité **et** application effective exigées ; **aucune durée imposée** par la spec
- [x] SEC-11 : **deux volets dans le même scénario** — accès refusé ET historique conservé
- [x] SEC-12 : scénario non authentifié **et** scénario d'habilitation retirée, sans divulgation

## 6. ERR-02 — aucune valeur métier inventée

- [x] Texte CDC exact cité (§7)
- [x] Écart décrit précisément : validation paresseuse, dégradation silencieuse
- [x] Comportement cible exprimé **en termes de comportement**, pas d'implémentation (§7)
- [x] **Aucune valeur de reprise en dur** — interdiction explicite
- [x] **Aucune valeur par défaut de production** destinée à contourner le fail-fast
- [x] **CLAR-NOT-02 n'est pas résolue implicitement** — mention explicite
- [x] **Le moteur F009 n'est pas modifié** sauf nécessité démontrée
- [x] **Aucun fournisseur tiers** introduit
- [x] Précaution d'ordonnancement du harnais formalisée (configuration valide **avant** activation du fail-fast)
- [x] La spécification n'impose **aucun mécanisme technique** — le choix est renvoyé à `/speckit.plan`

## 7. ERR-03

- [x] Les trois volets du CDC sont traités : intégrité, type, taille (§8)
- [x] Le **message d'erreur explicite** est exigé et testé
- [x] Frontière avec SEC-07 formalisée — le volet antivirus est explicitement exclu

## 8. NFR — aucun seuil inventé

- [x] Constat formalisé : NFR-01 et NFR-02 **ne portent aucun seuil chiffré** au CDC (§9)
- [x] NFR-01 : **aucun score d'ergonomie**, aucun référentiel externe, aucune métrique d'utilisabilité inventés
- [x] NFR-02 : **aucune largeur de rupture contractuelle**, aucun score de performance mobile inventés
- [x] NFR-11 : le calendrier ouvré existant est **certifié, pas étendu** ; **aucune nouvelle règle calendaire**
- [x] NFR-11 : les **deux propriétés** du CDC sont distinguées (dépôt hors heures possible ; décompte selon les règles de la file)
- [x] NFR-03, NFR-04, NFR-05 et les autres NFR sont **explicitement hors périmètre** (§3)

## 9. TEST-SPEC — 11 suites nominatives

- [x] **SEC-CERT : 6 suites** (SEC-CERT-01 → 06)
- [x] **ERR-CERT : 2 suites** (ERR-CERT-01, ERR-CERT-02)
- [x] **NFR-CERT : 3 suites** (NFR-CERT-01 → 03)
- [x] **11 suites pour 11 exigences** — correspondance exacte, aucun double comptage
- [x] Chaque suite précise : exigence CDC, précondition, action, résultat attendu, **résultat interdit**, niveau de test, preuve pour la Closure Review
- [x] Règle formalisée : un test existant **ne vaut preuve que s'il démontre effectivement le texte CDC**
- [x] Chaque suite comporte au moins un **scénario négatif**

## 10. Traçabilité

- [x] Matrice complète produite : exigence → texte CDC → mécanisme actuel → preuve actuelle → preuve manquante → TEST-SPEC → statut attendu (§5)
- [x] **Total exactement 11 exigences**, aucun double comptage
- [x] Chaque exigence est rattachée à **exactement une** suite TEST-SPEC

## 11. Couverture

- [x] **Baseline 146/188** citée comme acquise, avec sa source (`docs/17`)
- [x] **Projection maximale 157/188 (83,51 %)** annoncée comme **projection**, jamais comme acquis
- [x] Mention explicite : **aucune couverture n'est acquise avant Closure Review** (§14, CA-09)
- [x] La baseline historique 152/188 n'est **pas** utilisée comme base de calcul

## 12. Qualité rédactionnelle

- [x] Aucun détail d'implémentation (langage, framework, API, nom de classe) dans les exigences et critères
- [x] Exigences testables et non ambiguës
- [x] Critères de succès vérifiables sans connaître l'implémentation
- [x] Sections obligatoires complétées
- [x] Aucun marqueur `[NEEDS CLARIFICATION]` résiduel
- [x] Hypothèses documentées (§13)

## 13. Clarifications (session 2026-09-01)

- [x] Section `## 1 bis. Clarifications` créée avec sous-section datée `### Session 2026-09-01`
- [x] **8 clarifications** enregistrées, une ligne `Q: … → A: …` chacune, sans doublon
- [x] Chaque décision est **appliquée** dans la section concernée, pas seulement consignée
- [x] **SEC-10** : aucune durée contractuelle fixée ; constat vérifié que la clé est **absente des fichiers de configuration** et que le code retombe sur un **défaut technique de 30 min**, qualifié comme tel et **ni durci ni promu**
- [x] **SEC-10** : stratégie de preuve en deux volets (paramétrabilité + effet) **sans attendre la durée réelle** ; durée de test déclarée **non contractuelle** ; **aucune durée métier introduite dans le produit pour un test**
- [x] **ERR-02** : frontière tranchée — **option B, composant concerné**, pas l'application entière ; le durcissement « application entière » est explicitement écarté comme **non soutenu par le CDC**
- [x] **ERR-02** : **CLAR-NOT-02 explicitement non résolue** — nombre de tentatives, cadence et condition d'arrêt restent ouverts ; aucune valeur par défaut de production
- [x] **ERR-02** : ordre d'exécution obligatoire en 5 étapes formalisé (harnais → baseline → fail-fast → test invalide → test valide)
- [x] **ERR-02** : règle non négociable inscrite — **le produit n'est jamais modifié pour accommoder un harnais mal configuré**
- [x] **ERR-03** : réserve « intégrité » portée — volets *type* et *taille* implémentés avec messages explicites ; **aucun mécanisme d'intégrité dédié n'existe** et **aucun n'est inventé**
- [x] **ERR-03** : condition de sortie chiffrée (gain +10, projection 156/188) si le métier exige un contrôle d'intégrité distinct
- [x] **NFR-01** : aucun score UX, aucun seuil d'accessibilité, aucun nombre de clics, aucune métrique chiffrée
- [x] **NFR-02** : constat d'outillage vérifié — **aucun navigateur automatisé dans le dépôt** ; limite de bUnit reconnue explicitement
- [x] **NFR-02** : preuve **structurelle** en 5 points retenue ; **aucune infrastructure lourde introduite**
- [x] **NFR-02** : limite déclarée — la validation visuelle navigateur reste une **dette de recette**, distincte de la certification CDC
- [x] **NFR-02** : interdiction explicite de présenter 320 / 375 / 768 / 1024 px comme seuil CDC
- [x] **NFR-11** : état vérifié du calendrier (heures, jours ouvrés, **jours fériés déjà paramétrables**, mode « toujours ouvert » par défaut, 11 tests existants)
- [x] **NFR-11** : **aucun calendrier national ni service externe de jours fériés** introduit
- [x] Aucune clarification ne réintroduit SEC-02, SEC-07 ou SEC-08
- [x] Aucune clarification ne conclut à la nécessité d'une migration

## 14. Classification du code après clarification

- [x] Tableau « niveau de test / code existant / code à modifier » produit pour les 11 suites
- [x] Classification produite : **1 sans code** (SEC-12) · **9 tests uniquement** · **1 correction produit minimale** (ERR-02)
- [x] ERR-02 est la **seule** correction de produit, sur **un point unique**
- [x] Aucune exigence ne requiert de nouvelle entité, table ou migration

## Statut global

**PASS** — 14/14 catégories vérifiées, aucune case non cochée.

La spécification est **clarifiée** et prête pour `/speckit.plan` (non lancé par ce document).

**Réserves ouvertes, portées à `/speckit.plan`** :
1. **ERR-03 — volet « intégrité »** : aucun mécanisme dédié n'existe. Si le métier exige un contrôle distinct, ERR-03 **sort du périmètre** (gain +10, projection 156/188). C'est la seule réserve pouvant réduire le périmètre.
2. **NFR-02 — preuve structurelle** : la validation visuelle sur navigateur réel reste une **dette de recette**, à porter en Closure Review. Elle ne bloque pas la certification CDC, le CDC ne fixant aucun seuil.
3. **SEC-10 — défaut technique de 30 min** : constat d'état, non modifié par F011. La valeur d'exploitation reste une décision métier/DSI (hors périmètre).
