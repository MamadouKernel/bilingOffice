# Preflight — Feature 011 : Certification Sécurité & Socle Non-Fonctionnel

> **Document** : `docs/18-preflight-certification-securite-nfr.md`
> **Date** : 1er septembre 2026
> **Nature** : preflight de certification. **Aucun code, aucun test, aucune migration, aucun workflow Spec Kit, aucun commit, aucun push.**
> **Identifiant Feature déterminé** : `specs/` contient `001` → `010` (plus `003-5`). **Prochain disponible : `011`**, répertoire `specs/011-certification-securite-nfr/`.

---

## 1. Contexte R0

`docs/17-baseline-nominative-188-exigences.md` a établi la baseline nominative officielle par comptage de lignes :

**146 / 188 CONFORMES = 77,66 %**

L'ancienne baseline `152/188` est **historique** : elle surévaluait la couverture de 6 exigences (`IMP-15` double comptée, `SOC-14`, `PRI-02`, `IMP-03`, `REC-02`, `REC-03` agrégées comme conformes malgré leurs réserves documentées).

Cette Feature est **principalement une Feature de certification** : elle vise des exigences **déjà servies par le socle mais jamais comptabilisées**, parce qu'elles ont toujours été traitées comme « contraintes transverses appliquées partout » plutôt que comme livrables d'une Feature.

---

## 2. Baseline technique (exécutée)

| Contrôle | Attendu | Mesuré | Statut |
|:---|:---|:---|:---:|
| `dotnet build` | 0 warning / 0 erreur | **0 / 0** | ✅ |
| Domain | — | **154 / 154** | ✅ |
| Application | — | **425 / 425** | ✅ |
| Infrastructure | — | **83 / 83** | ✅ |
| Web | — | **156 / 156** | ✅ |
| **TOTAL** | ≥ 818 | **818 / 818** | ✅ |
| `has-pending-model-changes` | 0 | **0** | ✅ |

Exécution **sérialisée** (`xUnit.ParallelizeTestCollections=false`, `xUnit.MaxParallelThreads=1`). **Gate franchi.**

---

## 3. Contrôle du worktree

| Élément | Valeur |
|:---|:---:|
| Entrées totales | 100 |
| Fichiers `src/` + `tests/` modifiés | **83** |
| Fichiers stagés | **0** |

Ces 83 fichiers correspondent à la **refonte UI / login / 2FA préexistante**, non commitée et **antérieure à toute Feature de certification**.

> **Règle opposable à F011** : cette Feature **ne doit absorber aucun de ces fichiers**. Tout commit devra être sélectif. Le piège `app.css` identifié lors de l'audit F009 (styles de la cloche entangés avec la refonte UI) reste ouvert.

---

## 4. Analyse individuelle des 14 exigences candidates

| ID | Prio | CDC (texte source) | Code existant | Tests | Manque | Code requis ? | Certifiable ? |
|:---|:---:|:---|:---|:---|:---|:---:|:---:|
| **SEC-01** | Must | *Authentification obligatoire ; politique de mots de passe robuste ; verrouillage après échecs répétés* | `DependencyInjection.cs:27-29` — `RequiredLength=10`, `MaxFailedAccessAttempts=5`, `DefaultLockoutTimeSpan=15 min` | 4 tests nommant SEC-01 | Test dédié **verrouillage après 5 échecs** | **NON** | ✅ **B** |
| **SEC-02** | Must | *RBAC appliquant le moindre privilège, y compris au niveau des champs* | `IAutorisationService`, `Permission` (≈50 codes), `ExigerAsync` | 1 test nommant SEC-02 | Test nominatif « moindre privilège » ; **le volet « au niveau des champs » n'est pas démontré** | **NON** | ⚠️ **B avec réserve** |
| **SEC-03** | Must | *Cloisonnement strict : un client n'accède qu'à ses tickets ; un agent qu'aux files de son rôle* | `CloisonnementService`, filtres serveur | massivement testé **sous l'angle CPT-04**, 0 test nommant SEC-03 | Rattachement nominatif + test « agent hors file » | **NON** | ✅ **B** |
| **SEC-05** | Must | *Journal d'audit complet et non modifiable* | `JournalAuditAppendOnlyGuard.cs` (30 lignes) | 2 tests nommant SEC-05 | Test « tentative de modification rejetée » | **NON** | ✅ **B** |
| **SEC-07** | Must | *Contrôle des fichiers : types autorisés, taille max, **analyse antivirale*** | `ReglesDepotDocument(Extensions, TailleMax)`, `IAnalyseAntivirus` injecté dans `DeposerDocumentCommand` | indirect (F003/F006/F008) | Test dédié 3 volets. **Implémentation antivirale réelle = `AucuneAnalyseAntivirusConfiguree`** | **NON** (abstraction en place) | ⚠️ **B avec réserve** |
| **SEC-08** | Should | *Second facteur pour les profils agents et administrateurs* | `ActiverDoubleAuthentification.razor`, Identity TOTP | `ParcoursConnexion2FaTests` — **INSTABLE** | **Diagnostic de l'instabilité** (§6) | **harnais uniquement** | ⚠️ **C — sous condition** |
| **SEC-10** | Must | *Sessions expirent après une période d'inactivité **paramétrable*** | `Program.cs:30-31` — `ExpireTimeSpan` piloté par `Securite:ExpirationSessionMinutes`, `SlidingExpiration=true` | **0 test** | Test « paramètre lu et appliqué » | **NON** | ✅ **B** |
| **SEC-11** | Must | *Comptes agents désactivables immédiatement, **sans suppression de l'historique*** | `ApplicationUser.EstActif` | 1 test nommant SEC-11 | Test « désactivation → accès refusé **ET** historique intact » | **NON** | ✅ **B** |
| **SEC-12** | Must | *Aucun lien de notification ne donne accès direct à un document protégé* | Lien relatif `/tickets/{id}`, aucun jeton | 2 tests (`AccesDocumentAutoriseTests`, `RaccordementMentionTests`) | Certification nominative | **NON** | ✅ **A** |
| **ERR-02** | Must | *Nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres* | Moteur de reprise F009 complet | 6 tests (TEST-ERR-02) | **Fail-fast de configuration** (§5) | **OUI — 1 correction** | ⚠️ **C** |
| **ERR-03** | Must | *Documents vérifiés à l'upload (intégrité, type, taille) avec **message d'erreur explicite*** | `DeposerDocumentCommand` (contrôles) | indirect | Test dédié **message d'erreur explicite** | **NON** | ✅ **B** |
| **NFR-01** | Must | *Interface en français ; ergonomie simple* — **aucun seuil chiffré au CDC** | UI intégralement française | indirect | Test automatisé « absence de libellé non francophone » | **NON** | ✅ **B** |
| **NFR-02** | Must | *Portail responsive, utilisable sur mobile* — **aucun seuil chiffré au CDC** | `viewport` déclaré · 4 `@media` · **138 usages** de classes responsive | indirect | Test bUnit de rendu mobile | **NON** | ✅ **B** |
| **NFR-11** | Must | *Horaires de fonctionnement pour le calcul des SLA ; dépôt hors heures ouvrées possible* | `Domain/SuiviTemps/CalendrierOuvre.cs` (`EstJourOuvre`, calcul ouvré) | indirect | Test limite « dépôt hors heures ouvrées → SLA démarre selon la file » | **NON** | ✅ **B** |

---

## 5. ERR-02 — analyse détaillée

```
CDC :
  « Échec d'envoi d'une notification : nouvelle tentative automatique et traçage ;
    l'échec d'un canal n'empêche pas les autres. »

EXISTANT :
  - statut d'envoi par canal (EnvoiNotification.Statut) ...................... ✅
  - nouvelle tentative automatique (ReessaiEnvoisNotificationService) ........ ✅
  - traçage (ConsignerAuditAsync, sans contenu de message) ................... ✅
  - isolation inter-canaux (testée : 1 canal en échec, 2 aboutissent) ........ ✅
  - plafond de tentatives (NombreTentatives < max) ........................... ✅
  - aucune valeur métier inventée (0 littéral, 0 backoff, 0 fallback) ........ ✅
  - 6 tests TEST-ERR-02 verts ................................................ ✅

ÉCART (vérifié ce jour) :
  Le rapport F009 affirme « un déploiement de production échouera au démarrage ».
  C'est FAUX. Chaîne réelle :
    1. ParametresReessaiNotification est enregistré via AddSingleton(sp => {... Valider(); ...})
       → FABRIQUE PARESSEUSE.
    2. Unique consommateur : ReessayerEnvoisNotificationUseCase.
    3. Résolu uniquement dans ReessaiEnvoisNotificationService.TraiterUneFoisAsync().
    4. Cette méthode ne s'exécute qu'après le premier tic d'un PeriodicTimer d'1 minute.
    5. L'exception y est CAPTURÉE et journalisée ; la boucle continue.

  Mesure indépendante : « ValidateOnStart » → 0 occurrence dans tout src/.
  Vérifié : appsettings.json ne contient PAS la section Notifications ;
            appsettings.Development.json la contient (valeurs DEV non contractuelles).

  CONSÉQUENCE EN PRODUCTION SANS CONFIGURATION :
    - l'application DÉMARRE normalement ;
    - une erreur est journalisée chaque minute ;
    - ERR-02 (Must) ne fonctionne JAMAIS — aucun envoi externe n'est jamais retenté ;
    - les notifications In-App continuent d'être créées (le service de distribution
      n'a pas besoin de cette configuration) → le système PARAÎT fonctionner alors
      qu'une exigence Must est silencieusement morte.

CORRECTION NÉCESSAIRE :
  Résolution eager au démarrage (AddOptions<T>().Validate(...).ValidateOnStart(),
  ou résolution explicite du singleton dans le pipeline de démarrage).
  Périmètre : DependencyInjection.cs uniquement. AUCUNE règle métier touchée.
  AUCUNE valeur de retry n'est introduite — CLAR-NOT-02 reste ouverte.

TEST MANQUANT :
  « Démarrage sans configuration Notifications:Reessai → échec explicite au démarrage ».

IMPACT F009 :
  F009 reste CLOSED. Sa dette D9-6 est simplement requalifiée : ce n'est pas une
  dette de configuration, c'est un ÉCART entre le comportement annoncé et le
  comportement réel. Aucune requalification rétroactive de la Feature.
```

---

## 6. ERR-03 — analyse détaillée

```
CDC :
  « Les documents déposés sont vérifiés à l'upload (intégrité, type, taille)
    avec message d'erreur explicite. »

EXISTANT :
  - ReglesDepotDocument(ExtensionsAutorisees, TailleMaxOctets) ............... ✅
  - contrôles appliqués dans DeposerDocumentCommand .......................... ✅
  - IAnalyseAntivirus (volet intégrité) ...................................... ✅
  - consommé par F003, F004, F006, F008 ...................................... ✅

TEST :
  Indirect uniquement (les Features consommatrices testent leurs propres parcours).
  AUCUN test ne nomme ERR-03.

ÉCART :
  Le volet « message d'erreur explicite » n'est démontré par aucun test dédié.

CERTIFIABLE SANS CODE : OUI
CORRECTION ÉVENTUELLE : aucune. Ajout de tests uniquement.
```

---

## 7. NFR — analyse détaillée

### NFR-01 et NFR-02 — seuils

**Vérification faite dans le CDC source** : ni NFR-01 ni NFR-02 ne portent de **seuil chiffré**. NFR-01 exige « interface en français ; ergonomie simple » ; NFR-02 exige « responsive, utilisable sur mobile ». Ce sont des exigences **qualitatives**.

**Conséquence méthodologique** : la règle *« une exigence NFR nécessitant une mesure réelle ne peut être certifiée par lecture du code »* **ne s'applique pas ici** — il n'y a aucune mesure à produire. Elle s'applique en revanche pleinement à **NFR-03 (99 %)**, **NFR-04 (< 3 s)** et **NFR-05 (volumétrie)**, qui sont **exclues du périmètre** (§9).

| Exigence | Comportement attendu | Preuves techniques réelles | Reproductibilité |
|:---|:---|:---|:---|
| **NFR-01** | Interface en français, ergonomie simple | UI intégralement francophone ; `docs/06-audit-ui-ux` | Test automatisé de scan de libellés — reproductible |
| **NFR-02** | Utilisable sur mobile | `viewport` déclaré (`App.razor`) · **4 `@media`** (`991.98px`, `575.98px`, `prefers-reduced-motion`, `print`) · **138 usages** de classes responsive | Test bUnit avec émulation mobile — reproductible |

### NFR-11

```
CalendrierOuvre : Domain/SuiviTemps/CalendrierOuvre.cs
  - EstJourOuvre(DateOnly)
  - calcul de durée ouvrée (boucle jour par jour)
Administrable : GererParametresSlaCommand
Cohérence avec les traitements de fond : SurveillanceSlaService consomme les
  paramètres SLA, eux-mêmes porteurs du calendrier.

TESTS LIMITES À PRODUIRE :
  - dépôt hors heures ouvrées → le SLA démarre selon les règles de la file (texte CDC)
  - dépôt un jour non ouvré → report au prochain jour ouvré
  - franchissement de seuil calculé en heures ouvrées, non en heures calendaires
```

---

## 8. Analyse 2FA — **facteur de risque principal**

Mesures réelles effectuées ce jour :

| Mode d'exécution | Résultat |
|:---|:---|
| Suite Web complète, parallèle | 156/156 ✅ |
| **Filtre `ParcoursConnexion2Fa`, sérialisé, ×3** | **1 échec / 3** ❌ |
| (mesure antérieure, audit F009 : filtre parallèle ×4) | 3 échecs / 4 ❌ |

```
2FA :

PRODUIT :
  Aucune anomalie démontrée. Activation, vérification et parcours de connexion
  sont implémentés ; la majorité des exécutions passent.

TEST :
  ParcoursConnexion2FaTests — instable, y compris EN EXÉCUTION SÉRIALISÉE.
  L'assertion qui casse est TOUJOURS EstConnecteAsync(client), c'est-à-dire
  GET /comptes/double-authentification ne retournant pas 200.

CAUSE :
  NON ÉTABLIE AVEC CERTITUDE. Deux facteurs candidats mesurés :

  (a) Harnais PlateformeWebApplicationFactory : EnsureDeleted()/EnsureCreated()
      gardé par un booléen STATIQUE sur une base LocalDB partagée.
  (b) GenererCodeTotp utilise DateTimeOffset.UtcNow sans instant injectable →
      sensible au franchissement de la fenêtre de 30 s.

  DISCRIMINANT DÉCISIF : le test A_Utilisateur_sans_2FA_... échoue aussi, et il
  N'UTILISE AUCUN TOTP. Le facteur (b) ne peut donc pas expliquer à lui seul
  l'instabilité. Le facteur (a) reste le seul compatible avec les deux échecs.

  L'hypothèse « artefact de parallélisme » avancée lors de l'audit F009 est
  RÉFUTÉE par la mesure sérialisée de ce jour.

CORRECTION REQUISE :
  Diagnostic du HARNAIS, jamais du produit. Aucune modification du code de
  production ne doit être envisagée pour stabiliser ce test.

BLOQUE CERTIFICATION : OUI pour SEC-08 uniquement.
  On ne certifie pas une exigence sur un test dont l'échec n'est pas expliqué.
  SEC-08 entre au périmètre SOUS CONDITION : si le diagnostic révèle un défaut
  PRODUIT, SEC-08 sort du périmètre et le gain retombe à +13.
```

---

## 9. Périmètre exclu — Production Readiness ≠ Certification CDC

**Aucune** des exigences retenues ne dépend d'un sujet de mise en production. Vérifié individuellement :

| Sujet exclu | Impact sur le périmètre |
|:---|:---|
| Fournisseur SMS / WhatsApp (PC-16) | **Aucun** — ERR-02 se certifie sur `CanalJournalise`, sans fournisseur |
| Fournisseur e-mail | **Aucun** — idem |
| Hébergement (PC-13) | **Aucun** — `SEC-04` (TLS) et `NFR-09` sont **exclues** |
| Sauvegardes | **Aucun** — `NFR-06` **exclue** |
| Secrets de production | **Aucun** — le fail-fast ERR-02 ne lit aucun secret |
| Conformité ARTCI (PC-14) | **Aucun** — `SEC-06` **exclue** |
| Assistant IA (PC-18) | **Aucun** — `IA-01→07` hors périmètre |
| Nouvelles fonctionnalités métier | **Aucune** — Feature de certification |

**Exigences explicitement laissées hors périmètre** : `SEC-04`, `SEC-06`, `SEC-09`, `NFR-03`→`NFR-10` (hors 11), `IA-*`, `ENR-04/05`, `COM-06`, `SIM-11`, `SOC-14`, `PRI-02`, `IMP-03`, `REC-02`, `REC-03`.

> `SOC-14`, `PRI-02`, `IMP-03`, `REC-02`, `REC-03` sont **certifiables** mais relèvent d'une **cohérence fonctionnelle différente** (dettes ponctuelles hétérogènes réparties sur 3 modules). Les regrouper ici diluerait le thème « sécurité & socle non fonctionnel ». Elles constituent la **Feature de remédiation suivante (+6 avec COM-06)**.

---

## 10. Migration

**MIGRATION ATTENDUE : AUCUNE.**

Aucune des 14 exigences ne requiert de modification du modèle. Toutes les capacités
sont déjà persistées (`ApplicationUser.EstActif`, `EntreeJournalAudit`, `CalendrierOuvre`,
`EnvoiNotification.Statut`). La seule correction de code identifiée (fail-fast ERR-02)
touche `DependencyInjection.cs`, pas le modèle.

> **Si l'implémentation conclut qu'une migration est nécessaire → STOP.** Une Feature
> censée certifier le socle existant ne doit pas modifier le modèle ; ce serait le signe
> que le périmètre a dérivé.

---

## 11. Classification finale

### A — CERTIFIABLE SANS CODE (1)
`SEC-12` — code et 2 tests existent, certification nominative suffisante.

### B — CERTIFIABLE AVEC TESTS (11)
`SEC-01` · `SEC-02` · `SEC-03` · `SEC-05` · `SEC-07` · `SEC-10` · `SEC-11` · `ERR-03` · `NFR-01` · `NFR-02` · `NFR-11`

> Deux réserves à porter dans la spec :
> - **SEC-02** : le volet CDC *« y compris au niveau des champs »* n'est démontré par aucun mécanisme identifié. À traiter en clarification lors de `/speckit.clarify` — il pourrait faire basculer SEC-02 en C.
> - **SEC-07** : l'implémentation antivirale réelle est `AucuneAnalyseAntivirusConfiguree`. L'**abstraction** satisfait le CDC au niveau de la conception ; l'**analyse effective** relève de la production. À trancher en clarification.

### C — CORRECTION MINIMALE REQUISE (2)
- `ERR-02` — fail-fast de configuration (`DependencyInjection.cs`), **aucune règle métier touchée**
- `SEC-08` — **sous condition** : diagnostic du harnais 2FA d'abord ; sortie du périmètre si le défaut est côté produit

### D — BLOQUÉE / EXCLUE (0 du périmètre candidat)
Aucune des 14 candidates n'est bloquée. Les exclusions du §9 n'étaient pas candidates.

---

## 12. Risques de régression

| # | Risque | Probabilité | Mitigation |
|:--|:---|:---:|:---|
| **R1** | Le fail-fast ERR-02 casse le démarrage des tests Web (`WebApplicationFactory` sans configuration) | **Élevée** | Fournir la configuration au harnais **avant** d'activer le fail-fast ; sinon 156 tests Web tombent d'un coup |
| **R2** | Le diagnostic 2FA révèle un défaut produit | Moyenne | SEC-08 sort du périmètre, gain +13 |
| **R3** | Les nouveaux tests de sécurité dupliquent des mécanismes centralisés | Moyenne | Rattacher aux services existants (`CloisonnementService`, `IAutorisationService`), **ne rien réimplémenter** |
| **R4** | Absorption involontaire des 83 fichiers préexistants | Moyenne | Commit sélectif fichier par fichier ; piège `app.css` documenté |
| **R5** | SEC-02 « au niveau des champs » élargit le scope en cours de route | Moyenne | Clarification **avant** `/speckit.plan` |

---

## 13. Projection de couverture

```
COUVERTURE ACQUISE AVANT FEATURE  : 146 / 188  (77,66 %)
EXIGENCES RETENUES                : 14  (A=1, B=11, C=2)
COUVERTURE PROJETÉE MAXIMALE      : 160 / 188  (85,11 %)
COUVERTURE PROJETÉE SI SEC-08 SORT: 159 / 188  (84,57 %)

COUVERTURE CERTIFIÉE APRÈS FEATURE : NON ENCORE ACQUISE
```

> **Aucune exigence ne sera comptée avant certification finale.** La projection n'est pas un acquis.

---

```
PREFLIGHT — CERTIFICATION SÉCURITÉ & SOCLE NFR

FEATURE : 011 — specs/011-certification-securite-nfr/
          (convention du dépôt : 001→010 occupés, 011 prochain disponible)

BASELINE :
BUILD : SUCCÈS
WARNINGS : 0
TESTS : 818/818 (Domain 154 · Application 425 · Infrastructure 83 · Web 156)
        exécution sérialisée xUnit.MaxParallelThreads=1
PENDING MODEL CHANGES : 0

BASELINE CDC OFFICIELLE :
146/188 (77,66 %)

EXIGENCES CANDIDATES : 14

SEC : SEC-01, SEC-02, SEC-03, SEC-05, SEC-07, SEC-08, SEC-10, SEC-11, SEC-12  (9)
ERR : ERR-02, ERR-03                                                          (2)
NFR : NFR-01, NFR-02, NFR-11                                                  (3)

CLASSIFICATION :
A — CERTIFIABLE SANS CODE : 1   (SEC-12)
B — CERTIFIABLE AVEC TESTS : 11 (SEC-01, SEC-02, SEC-03, SEC-05, SEC-07,
                                 SEC-10, SEC-11, ERR-03, NFR-01, NFR-02, NFR-11)
C — CORRECTION MINIMALE : 2     (ERR-02 fail-fast ; SEC-08 sous condition)
D — BLOQUÉE/EXCLUE : 0

PÉRIMÈTRE FINAL :
TOTAL : 14
MUST : 13
SHOULD : 1  (SEC-08)
COULD : 0

CODE FONCTIONNEL À MODIFIER :
  1 fichier — src/CIT.Plateforme.Infrastructure/DependencyInjection.cs
  (fail-fast de la configuration de reprise ERR-02 ; aucune règle métier,
   aucune valeur de retry introduite, CLAR-NOT-02 reste ouverte)

TESTS À AJOUTER :
  SEC-01 verrouillage · SEC-02 moindre privilège · SEC-03 cloisonnement nominatif
  SEC-05 journal non modifiable · SEC-07 type/taille/antivirus · SEC-10 expiration
  SEC-11 désactivation sans perte d'historique · SEC-12 certification nominative
  ERR-02 démarrage sans configuration · ERR-03 message d'erreur explicite
  NFR-01 libellés français · NFR-02 rendu mobile · NFR-11 heures ouvrées
  → suites TEST-SPEC : SEC-CERT, ERR-CERT, NFR-CERT

MIGRATION : AUCUNE (STOP si l'implémentation en conclut autrement)
UI : AUCUNE modification fonctionnelle ; seuls des tests de rendu sont ajoutés
2FA : instabilité RÉELLE, non expliquée, présente même en exécution sérialisée
      (1 échec/3). Hypothèse « parallélisme » RÉFUTÉE. Diagnostic du harnais
      obligatoire avant certification de SEC-08.
ERR-02 : écart confirmé — Valider() en fabrique paresseuse, 0 ValidateOnStart
         dans tout src/. Pas de fail-fast réel. Correction ciblée requise.

GAIN MAXIMAL : +14
COUVERTURE PROJETÉE : 160/188 (85,11 %)  —  159/188 si SEC-08 sort du périmètre

RISQUES :
  R1 le fail-fast peut casser les 156 tests Web si le harnais n'est pas configuré d'abord
  R2 le diagnostic 2FA peut révéler un défaut produit
  R3 duplication de mécanismes de sécurité centralisés
  R4 absorption des 83 fichiers préexistants (refonte UI/login/2FA)
  R5 SEC-02 « au niveau des champs » peut élargir le scope

DETTE :
  SEC-02 volet « au niveau des champs » non démontré  → clarification requise
  SEC-07 antivirus réel = AucuneAnalyseAntivirusConfiguree → clarification requise
  SEC-08 cause d'instabilité non établie
  Reste hors périmètre : SOC-14, PRI-02, IMP-03, REC-02, REC-03, COM-06,
  REC-SCE-14 E2E → Feature de remédiation suivante (+6)

DOCUMENT :
docs/18-preflight-certification-securite-nfr.md

GO / NO-GO : GO — sous réserve de traiter SEC-02 et SEC-07 en clarification,
             et de diagnostiquer le harnais 2FA avant de certifier SEC-08.

VERDICT :
READY FOR /speckit.specify
```

---

# ADDENDUM — Final Scope Gate (1er septembre 2026)

> Ce gate **supersède la classification du §11**. Trois exigences sortent du périmètre.

## SEC-02 — EXCLUE

**Le texte de `docs/01-matrice-exigences.md` était tronqué dans mon analyse initiale.** Texte CDC intégral (`cdc_text.txt:848`) :

> *« Contrôle d'accès par rôles (RBAC) appliquant le moindre privilège, **y compris au niveau des champs pour les traitements parallèles**. »*

Le CDC désigne un cas **précis et borné**, corroboré par trois autres passages :
- `l.56` : *« champs correspondant à son rôle […] y compris lorsque plusieurs rôles traitent »*
- `l.583` : *« le ticket d'un client IES est traité **en parallèle** par le Pilote Manifeste et le Pilote Contrôle Documentaire, **chacun intervenant exclusivement** sur les ch[amps réservés] »*
- `l.832` : *« champs et actions propres à mon rôle (**y compris champs réservés en traitement parallèle IES**) »*

**Interprétation retenue : ni A ni B — interprétation C.** Il ne s'agit ni d'une simple protection serveur générique, ni d'un moteur générique champ-par-champ, mais du **cloisonnement au niveau des champs dans la branche parallèle IES**.

**État réel** : F003 a livré une séparation **par branche** (`InstruireBrancheIesCommand`, une permission distincte par branche, vérifiée avant toute mutation), testée (`CircuitClientIesTests`, `ConstaterJointureIesAutorisationTests`). Le code porte lui-même la réserve : *« la portée exacte des champs réservés reste PC-05, non résolue »*.

**Blocage** : **PC-05** — 🟡 IMPORTANT, **« Bloque : SPECIFICATION »**, source `IMP-40 · SEC-02`. Le CDC ne dit pas quels champs sont réservés à quel rôle, ni si la lecture croisée est permise. Une décision métier est requise (atelier Pilotes Manifeste / CD).

**Classification : D — EXCLUE.** Certifier « au niveau des champs » sans savoir quels champs serait une certification par présomption.

## SEC-07 — EXCLUE

Texte CDC (`cdc_text.txt:863`) : *« Contrôle des fichiers déposés : types autorisés, taille maximale, **analyse antivirale**. »* — trois volets.

| Volet | État |
|:---|:---|
| Types autorisés | ✅ `ReglesDepotDocument.ExtensionsAutorisees` |
| Taille maximale | ✅ `ReglesDepotDocument.TailleMaxOctets` |
| **Analyse antivirale** | ❌ **aucun moteur réel** |

**Constat vérifié** : l'unique implémentation enregistrée (`DependencyInjection.cs:88`) est `AucuneAnalyseAntivirusConfiguree`, dont le corps entier est :

```csharp
public Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default) =>
    Task.FromResult(StatutAnalyseAntivirus.Sain);
```

Elle **déclare tout fichier sain sans lire le flux**. L'abstraction est une bonne conception, mais elle **n'est pas une analyse antivirale**.

**Classification : D — EXCLUE.** Aucun antivirus n'est implémenté dans F011, aucun fournisseur n'est choisi, et aucune validation MIME n'est requalifiée en antivirus.

## SEC-08 — EXCLUE

### Reproduction

| Mode | Résultat |
|:---|:---|
| Suite Web complète, parallèle | 156/156 ✅ |
| Classe entière, **sérialisée**, ×3 | **1 échec / 3** ❌ |
| Classe entière, parallèle, ×4 (audit F009) | 3 échecs / 4 ❌ |
| **Chaque scénario SEUL, sérialisé, ×3** | **A 0/3 · B 0/3 · C 0/3 · D 0/3 · E 0/3 — tous verts** ✅ |

### Cause racine

**Le fait décisif** : les cinq scénarios passent **systématiquement** lorsqu'ils sont exécutés isolément. L'échec n'apparaît que lorsqu'ils s'exécutent **ensemble**, y compris en sérialisé. Il s'agit donc d'une **contamination d'état inter-tests**.

**Hypothèses écartées par la mesure** :
- ❌ *Parallélisme* — réfuté : échec en sérialisé.
- ❌ *Fenêtre TOTP de 30 s* — réfuté : le scénario `A_` n'utilise aucun TOTP et échoue aussi.
- ❌ *Collision d'utilisateurs* — réfuté : chaque test crée `{Guid.NewGuid()}@exemple.test`.
- ❌ *Fuite de contexte produit* — réfuté : `IContexteUtilisateurWeb` est **Scoped** (`Program.cs:38`) ; la mémoïsation `_contexteCache` est confinée à la portée.

**Hypothèses restantes, non discriminées** : état partagé du harnais `PlateformeWebApplicationFactory` (booléen **statique** `baseDejaCreee` + `EnsureDeleted()/EnsureCreated()` sur une base LocalDB **partagée** `CitPlateformeTestsWeb`), ou saturation du pool de connexions LocalDB par 5 hôtes successifs.

Discriminer exigerait de **modifier le harnais**, ce que le mandat de diagnostic interdit.

```
SEC-08 DIAGNOSTIC :
REPRODUCTION : classe entière 1/3 (sérialisé) ; chaque scénario seul 0/3
CAUSE RACINE : contamination d'état inter-tests — NON DÉTERMINÉE précisément
HARNAIS / PRODUIT / INDÉTERMINÉ : HARNAIS fortement indiqué, mécanisme INDÉTERMINÉ
CORRECTION : diagnostic du harnais (hors mandat) — AUCUNE correction produit
CODE PRODUIT NÉCESSAIRE : NON
CERTIFIABLE DANS F011 : NON
```

> **Aucun défaut produit n'a été mis en évidence** : le comportement 2FA passe chaque scénario en isolation. Ce n'est pas une faille de sécurité, c'est un **test non probant**. Mais la règle s'applique : une exigence de sécurité instable ne se certifie pas par présomption.

**Classification : D — EXCLUE.**

## Périmètre final recalculé

| | |
|:---|:---:|
| Périmètre initial | 14 |
| Exclusions | **3** (SEC-02, SEC-07, SEC-08) |
| **Périmètre final** | **11** |
| Must | **11** |
| Should | 0 |
| Could | 0 |

`SEC-01` · `SEC-03` · `SEC-05` · `SEC-10` · `SEC-11` · `SEC-12` · `ERR-02` · `ERR-03` · `NFR-01` · `NFR-02` · `NFR-11`

**Baseline 146/188 → projection 157/188 (83,51 %) — non acquise avant Closure Review.**
