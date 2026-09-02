# FEATURE 015 — RAPPORT DE VALIDATION DE SCOPE FORENSIQUE

---

## 1. BASELINE TECHNIQUE & ÉTAT DE L'ENVIRONNEMENT

- **Dernier commit officiel** : `490c8c83e20697ce79c6560da42aa376f9fe419c` (F014 Closed)
- **Compilation .NET 10** : **SUCCESS** (0 avertissement, 0 erreur)
- **Suite de tests sérialisée** : **904 / 904 tests verts (100 % PASS)**
- **Migrations EF Core** : **0 pending model changes**
- **Baseline CDC certifiée** : **169 / 188 (89,89 %)**

---

## 2. AUDIT DES SOURCES ORIGINALES & IDENTIFICATION DE L'ANOMALIE DE NOMENCLATURE

### 2.1 Confirmation formelle de l'anomalie dans `docs/25`
L'audit forensique direct de `docs/01-matrice-exigences.md`, `docs/02-points-a-clarifier.md` et `docs/00-analyse-cdc.md` **CONFIRME UNE ANOMALIE DE NOMENCLATURE DANS LE RAPPORT `docs/25`** :

1. **`SEC-02` n'est PAS le chiffrement au repos** :
   - **Texte CDC exact** : *« Contrôle d'accès par rôles (RBAC) appliquant le moindre privilège, y compris au niveau des champs pour les traitements parallèles »* (Must, §18 du CDC).
   - **Portée réelle** : Cloisonnement fin et RBAC au niveau des champs dans le traitement parallèle IES (Manifeste vs CD).
   - **PC associée** : **`PC-05`** (*Habilitation des agents en lecture croisée*), qui **BLOQUE LA SPÉCIFICATION** tant qu'un atelier métier n'a pas arrêté la matrice exacte des champs réservés.
   - **Conséquence** : **`SEC-02` est NOT READY pour F015**.

2. **`SEC-04` n'est PAS la politique de rétention RGPD** :
   - **Texte CDC exact** : *« Chiffrement des échanges (HTTPS/TLS) et protection des documents stockés »* (Must, §18 du CDC).
   - **Portée réelle** : Chiffrement des flux de transport (HTTPS/TLS, HSTS, CSP), stockage sécurisé et intégrité des documents GED.
   - **PC associée** : Aucune PC bloquante au niveau applicatif.
   - **Conséquence** : **`SEC-04` est 100 % READY**.

3. **`SEC-06` est l'exigence portant sur la rétention et protection des données (ARTCI)** :
   - **Texte CDC exact** : *« Protection des données personnelles conformément à la réglementation ivoirienne (ARTCI) : minimisation, durées de conservation, droits des personnes »* (Must, §18 du CDC).
   - **PC associée** : **`PC-14`** (*Politique de conservation et d'archivage*), bloquant l'homologation légale tant que les durées de rétention ARTCI ne sont pas validées juridiquement.

4. **`SEC-07` est le contrôle des fichiers et l'analyse antivirale** :
   - **Texte CDC exact** : *« Contrôle des fichiers déposés : types autorisés, taille maximale, analyse antivirale »* (Must, §18 du CDC).
   - **Portée réelle** : Validation stricte des extensions et types MIME (déjà implémentée dans `ERR-03`), limites volumétriques, et pipeline d'assainissement / scanner antiviral.
   - **Conséquence** : **`SEC-07` est 100 % READY**.

---

## 3. MATRICE DE VÉRITÉ FORENSIQUE (EXIGENCES DE SÉCURITÉ AUDITÉES)

| ID | Texte CDC exact (§18) | Priorité | PC réelle | Code existant | Gap technique | Éligibilité F015 |
|:---|:---|:---:|:---:|:---|:---|:---:|
| **`SEC-02`** | *Contrôle d'accès par rôles (RBAC) appliquant le moindre privilège, y compris au niveau des champs pour les traitements parallèles* | **Must** | **PC-05** *(Bloque Spec)* | `IAutorisationService`, `Permission` | Matrice champs IES non validée métier | ❌ **NO-GO (Bloqué PC-05)** |
| **`SEC-04`** | *Chiffrement des échanges (HTTPS/TLS) et protection des documents stockés* | **Must** | Aucune | `UseHsts`, `UseHttpsRedirection` | Headers stricts (CSP, X-Content-Type), protection & intégrité stockage | ✅ **GO (100 % READY)** |
| **`SEC-06`** | *Protection des données personnelles conformément à la réglementation ivoirienne (ARTCI) : minimisation, durées de conservation, droits des personnes* | **Must** | **PC-14** *(Bloque Arch.)* | Néant | Durées légales de rétention ARTCI non arbitrées | ❌ **NO-GO (Bloqué PC-14)** |
| **`SEC-07`** | *Contrôle des fichiers déposés : types autorisés, taille maximale, analyse antivirale* | **Must** | **PC-05** *(Résolue)* | `ValiderAsync` (`ERR-03`, `SOC-07`) | Pipeline d'assainissement et scanner d'intégrité/antivirus | ✅ **GO (100 % READY)** |
| **`SEC-09`** | *Cloisonnement des environnements (production, recette) et gestion des habilitations par environnement* | **Should** | Aucune | `launchSettings.json`, configurations | Ségrégation stricte des environnements et secrets d'environnement | ✅ **GO (100 % READY)** |

---

## 4. AUDIT DES POINTS À CLARIFIER (PC) ASSOCIÉS

- **`PC-05` — Habilitation des agents en lecture croisée (traitement parallèle IES)** :
  - *Source CDC* : `IMP-40 · SEC-02`.
  - *Statut* : **Ouvert**.
  - *Impact* : **Bloque la spécification de SEC-02** car les rôles Pilote Manifeste et Pilote CD n'ont pas fait l'objet d'un atelier métier pour formaliser les colonnes autorisées en écriture/lecture respective.
- **`PC-13` — Choix d'hébergement** :
  - *Source CDC* : `§25 · NFR-09 · §23`.
  - *Statut* : **Ouvert**. Bloque l'architecture de production (F018).
- **`PC-14` — Politique de conservation et d'archivage** :
  - *Source CDC* : `§25 · SOC-09 · SEC-06`.
  - *Statut* : **Ouvert**. Bloque la spécification de SEC-06 (durées légales ARTCI).

---

## 5. ÉVALUATION COMPARATIVE DES OPTIONS POUR LA FEATURE 015

### Option A — Sécurité Transport, Intégrité & Assainissement Documentaire (`SEC-04`, `SEC-07`)
- **Exigences** : `SEC-04` (Must), `SEC-07` (Must).
- **Gain potentiel** : **+2 exigences Must**.
- **Autonomie** : 100 % autonome, zéro dépendance externe, zéro PC bloquante.
- **Score** : **96 / 100** — **EXCELLENT**.

### Option B — Sécurité Applicative Globale (`SEC-04`, `SEC-07`, `SEC-09`)
- **Exigences** : `SEC-04` (Must), `SEC-07` (Must), `SEC-09` (Should).
- **Gain potentiel** : **+3 exigences (2 Must + 1 Should)**.
- **Projection CDC** : 169 + 3 = **172 / 188 (91,49 %)**.
- **Autonomie** : 100 % autonome. Couvre le chiffrement des flux, les headers HTTP stricts, la protection documentaire GED, l'assainissement/scan des fichiers et le cloisonnement des environnements.
- **Score** : **98 / 100** — **RECOMMANDÉ (Top 1)**.

### Option C — Observabilité & Performance (`NFR-04`, `NFR-09`, `NFR-10`)
- **Exigences** : `NFR-04` (Must), `NFR-09` (Must), `NFR-10` (Must).
- **Gain potentiel** : +3 exigences Must.
- **Score** : **92 / 100** — Alternative valide (positionnée en F016).

---

## 6. PROPOSITION OFFICIELLE POUR LA FEATURE 015

```text
FEATURE :
015 — Sécurité des Échanges, Protection Documentaire & Cloisonnement d'Environnement

EXIGENCES COUVERTES :
- SEC-04 (Must)   : Chiffrement des échanges (HTTPS/TLS, HSTS, CSP) et protection des documents stockés
- SEC-07 (Must)   : Contrôle des fichiers déposés : types MIME stricts, taille maximale, analyse et assainissement
- SEC-09 (Should) : Cloisonnement des environnements (dev, recette, prod) et gestion des habilitations

GAIN MAXIMAL :
+3 exigences (2 Must, 1 Should)

BASELINE CDC PROJETÉE :
169 + 3 = 172 / 188 (91,49 %) — NON ACQUISE avant Closure Review

MIGRATION EF CORE :
0 migration (couche purement sécurité applicative, middleware, validateurs de flux et gestionnaires de stockage)

JUSTIFICATION :
1. Clôture des exigences de sécurité applicative 100 % autonomes sans dépendre des décisions de PC-05, PC-14 ou PC-16.
2. Renforce le socle de défense en profondeur (HSTS, CSP, X-Frame-Options, intégrité SHA-256 des fichiers déposés).
3. 100 % prêt pour /speckit.specify sans ambiguïté fonctionnelle.
```

---

## 7. CORRECTION APPORTÉE À LA ROADMAP DES 19 RESTANTES

1. **F015** : `SEC-04`, `SEC-07`, `SEC-09` (+3) → **172 / 188 (91,49 %)**
2. **F016** : `NFR-04`, `NFR-09`, `NFR-10` (+3) → **175 / 188 (93,09 %)**
3. **F017** : `IA-01`, `IA-02`, `IA-03`, `IA-04`, `IA-05`, `IA-06`, `IA-07` (+7) → **182 / 188 (96,81 %)**
4. **F018** : `SEC-02`, `SEC-06`, `ENR-04`, `NFR-03`, `NFR-05`, `NFR-06` (+6) → **188 / 188 (100 %)**
