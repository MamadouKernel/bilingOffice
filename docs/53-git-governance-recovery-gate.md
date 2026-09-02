# Git Governance Recovery Gate — audit et plan de restauration

> **Document** : `docs/53-git-governance-recovery-gate.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Verdict** : **`BLOCKED_CONFIDENTIALITY_REVIEW`**
> **Aucun `git add` n'a été effectué.** Aucun commit, aucun push.

---

## 1. Constat prioritaire — exposition déjà réalisée

Cet audit devait évaluer un **risque** de confidentialité. Il a établi un **fait**.

| Vérification | Résultat |
|:---|:---|
| Visibilité du dépôt distant | **PUBLIC** — `private: false`, `visibility: public` |
| `docs/cdc_text.txt` présent dans le commit `12aad9c` | **Oui** |
| `12aad9c` est-il un ancêtre de `origin/main` ? | **Oui** |
| `docs/CDC_Plateforme_Digitale_CIT_v3.docx` | **Idem** |

**Le cahier des charges de Côte d'Ivoire Terminal est actuellement accessible publiquement** sur
`https://github.com/MamadouKernel/bilingOffice`.

Le retrait du suivi opéré au commit `bf8c327` a fait disparaître ces fichiers de l'**arborescence**
du dépôt, mais ils demeurent dans les **commits antérieurs**, qui ont été poussés. Ils se
récupèrent en deux commandes par n'importe qui.

### Nature du document exposé

```
CÔTE D'IVOIRE TERMINAL
Direction des Systèmes d'Information
CAHIER DES CHARGES FONCTIONNEL
Référence : CIT-CIV-DSI-RFP-XXXX
Version 1.0 — 12 août 2026
```

Un cahier des charges d'appel d'offres, nominatif, émanant de la DSI du client. Il ne porte pas de
tampon « confidentiel » — **cela ne le rend pas publiable**. Un document contractuel appartenant à
un client nommé est confidentiel par nature et par usage commercial, sauf accord explicite de ce
client.

Sont également exposés : 201 fichiers d'analyse, d'audit et de spécification décrivant les
processus internes, l'organisation et les exigences de CIT.

---

## 2. Audit des règles d'exclusion

| Source | Contenu |
|:---|:---|
| `.gitignore` lignes 42-47 | `.claude/`, `.specify/`, `.vs/`, `docs/`, `scratch/`, `specs/` |
| `.git/info/exclude` | **Vide** |
| `core.excludesFile` (global) | **Non défini** |
| Autre règle de configuration | **Aucune** |

Le mécanisme est unique et local : `.gitignore`. Aucune règle cachée.

---

## 3. Inventaire exact

| Répertoire | Sur disque | Jamais versionnés |
|:---|---:|---:|
| `docs/` | 61 | **33** |
| `specs/` | 219 | **32** |
| **Total docs + specs** | **280** | **65** |

Les 215 autres existent dans l'historique jusqu'à `bf8c327^`, mais sont **figés** depuis.

### Le cas de F016 et F017

| Feature | Fichiers | Jamais versionnés |
|:---|---:|---:|
| `specs/016-disponibilite-performance-resilience` | 16 | **16 / 16** |
| `specs/017-securite-granulaire-retention-reglementaire` | 12 | **12 / 12** |

**Aucun de ces 28 fichiers n'a jamais été enregistré.** Spécifications, plans, rapports de clôture
et preuves de deux features déclarées closes n'existent que sur ce poste de travail.

---

## 4. Classification

| Catégorie | Fichiers | Dont jamais versionnés |
|:---|---:|---:|
| `CONFIDENTIAL` | **201** | 42 |
| `SAFE_TO_VERSION` | 72 | 19 |
| `ENVIRONMENT_EVIDENCE` | 4 | **4** |
| `SECRET_OR_SENSITIVE` | 3 | 0 |
| `GENERATED` | 0 | 0 |

### Critère retenu pour `CONFIDENTIAL`

Tout fichier citant le cahier des charges, ses exigences ou le client nommé. Le critère est
volontairement **large** : dans un corpus de spécification, un document qui décrit une exigence
décrit un besoin métier du client. Le distinguo entre « le CDC » et « ce qui en dérive » n'a pas
d'effet protecteur — un audit détaillant les processus de facturation portuaire est aussi parlant
que le CDC lui-même.

### `SECRET_OR_SENSITIVE` — les 3 fichiers

| Fichier | Motif |
|:---|:---|
| `specs/003-5-ui-ux-consolidation/quickstart.md` | Adresses professionnelles réelles |
| `specs/010-continuite-guichet-rendez-vous/quickstart.md` | Adresses professionnelles réelles |
| `specs/006-customer-complaint/closure-review.md` | Chaîne de connexion — hôte, port, base ; **sans mot de passe** |

Les adresses relevées sont sur les domaines réels `cotedivoireterminal.com` et `cit.ci`, dont
`m.konate@cotedivoireterminal.com` — une **adresse professionnelle nominative**. Publiée, elle
devient une cible de hameçonnage et un point d'entrée d'ingénierie sociale.

**Aucun mot de passe, clé d'API ni jeton n'a été trouvé.** La seule occurrence ressemblant à un
mot de passe (`docs/09`, ligne 23) est un **espace réservé** : `"VotreMotDePasseTestFort!"`.

### `ENVIRONMENT_EVIDENCE` — les 4 fichiers

Les trois rapports F016 (`NOT EXECUTED`) et le script `benchmark-k6.js`. Non sensibles, mais à
traiter à part : ils seront **remplis lors de la campagne** et deviendront alors des preuves
contractuelles.

---

## 5. Pourquoi ce gate s'arrête ici

La consigne était explicite : *« Si un risque de confidentialité existe : STOP avant `git add` ».*

Le risque n'existe pas — **il est réalisé**. Ajouter à l'index 201 fichiers classés
`CONFIDENTIAL` alors que le dépôt distant est public reviendrait à préparer une seconde
exposition, plus large que la première.

**Aucun `git add` n'a donc été exécuté.**

---

## 6. Remédiation — deux chantiers distincts

### Chantier A — Traiter l'exposition existante (urgent)

À traiter **avant** toute restauration de suivi.

| Option | Effet | Délai | Limite |
|:---|:---|:---|:---|
| **A1 — Passer le dépôt en privé** | Coupe l'accès public immédiatement | Minutes | Ne défait pas ce qui a déjà été récupéré |
| **A2 — Réécrire l'historique** (`git filter-repo`) puis pousser en forçant | Retire les fichiers des commits | Heures | GitHub conserve un temps les objets déréférencés ; les *forks* et caches subsistent |
| **A3 — Supprimer et recréer le dépôt** | Le plus radical | Heures | Perte de l'historique distant ; les clones existants subsistent |

**Recommandation : A1 immédiatement**, quelle que soit la suite. C'est la seule action qui arrête
l'exposition dans l'heure, elle est réversible, et elle ne détruit rien.

A2 ou A3 ne se justifient que si le dépôt doit **rester public**. Dans ce cas, aucune des deux ne
garantit un effacement complet : il faut considérer que ce qui a été public a pu être copié.

**Question à trancher :** le dépôt doit-il être public ? S'il s'agit d'un projet client sous
contrat, la réponse est probablement non — et A1 résout alors simultanément l'exposition et le
blocage de ce gate.

### Chantier B — Restaurer la traçabilité (après A)

Une fois le dépôt privé, la restauration devient **sans risque** et se réduit à :

```bash
git rm --cached -r --dry-run docs specs
```

*(vérification préalable — n'écrit rien)*

Puis, après retrait des lignes 45 et 47 de `.gitignore` :

```bash
git add docs specs
```

```bash
git status --short | wc -l
```

**Séquence recommandée**, en trois commits séparés pour rester lisibles :

| # | Contenu | Volume |
|---:|:---|---:|
| 1 | `.gitignore` — retrait de `docs/` et `specs/` | 1 fichier |
| 2 | `specs/016` et `specs/017` — **jamais versionnées** | 28 fichiers |
| 3 | Le reste de `docs/` et `specs/` | 252 fichiers |

Le commit 2 est le plus urgent : il porte les artefacts contractuels de deux features closes.

### Si le dépôt doit rester public

Alors `docs/` et `specs/` **ne doivent pas** y être versionnés. La traçabilité se rétablit par un
**second dépôt privé** dédié, ou par une sauvegarde versionnée hors GitHub. C'est l'option D de
`docs/34`, avec son inconvénient : le risque de désynchronisation entre le code et ses specs.

---

## 7. Avant toute remédiation — mesures conservatoires

1. **Sauvegarder immédiatement** `docs/` et `specs/` hors du poste de travail. 65 fichiers
   n'existent nulle part ailleurs ; une panne disque les perdrait définitivement, indépendamment
   de toute question de confidentialité.
2. Ne pas pousser tant que la visibilité du dépôt n'est pas tranchée.
3. Informer le responsable du contrat CIT de l'exposition, afin qu'il apprécie s'il y a lieu de
   notifier le client. Cette appréciation ne relève pas de la technique.

---

## 8. Ce qui reste à décider

| # | Question | Décideur |
|---:|:---|:---|
| 1 | Le dépôt GitHub doit-il être **public ou privé** ? | Responsable projet / DSI |
| 2 | L'exposition du CDC doit-elle être **notifiée** au client ? | Responsable contrat |
| 3 | Faut-il **réécrire l'historique**, ou le passage en privé suffit-il ? | Responsable projet |
| 4 | Si le dépôt reste public : quel support pour versionner specs et docs ? | Responsable projet |

---

## Verdict

# `BLOCKED_CONFIDENTIALITY_REVIEW`

Le blocage ne tient pas à un doute : il tient à un fait vérifié. Le dépôt est public, et le cahier
des charges du client y est accessible depuis le commit `12aad9c`.

Le plan de restauration est prêt et se ramène à trois commandes — **il ne doit pas être exécuté
avant que la question 1 ne soit tranchée.**
