# RAPPORT DE SÉLECTION DE FEATURE POST-F017 — NO IMPLEMENTATION

> ⚠️ **CORRECTED AFTER FORENSIC CDC RECONCILIATION — 2026-09-02**
>
> Ce document contenait trois erreurs factuelles, corrigées et tracées en § 2 bis. Les
> descriptions d'`IA-01` à `IA-07` ne correspondaient à aucun texte du CDC, et deux priorités NFR
> étaient inexactes. Le compteur 174 / 188 n'est pas affecté ; la **caractérisation du reste à
> faire** l'était. Référence : `docs/33-post-f017-global-cdc-matrix.md`.

## Analyse d'Éligibilité & Cadrage de la Prochaine Étape

---

### 1. BASELINE TECHNIQUE VALIDÉE

- **Commit de référence** : `d80a126` (`feat: enforce granular access and regulatory retention`)
- **Compilation** : `dotnet build /p:UseSharedCompilation=false` $\implies$ **SUCCESS (0 warning, 0 error)**
- **EF Core Migrations** : `dotnet ef migrations has-pending-model-changes` $\implies$ **0 pending model changes**
- **Suite de tests** : `dotnet test` $\implies$ **1 041 / 1 041 tests verts (100 %)**
  - Domain : 158 / 158
  - Application : 560 / 560
  - Infrastructure : 94 / 94
  - Web : 229 / 229
- **Worktree** : **100 % propre (Clean)**
- **Baseline CDC certifiée** : **174 / 188 (92,55 %)**

---

### 2. RECONSTRUCTION FORENSIQUE DES 188 EXIGENCES CDC

- **Exigences certifiées conformes** : **174 / 188 (92,55 %)**
- **Exigences restantes non certifiées** : **14 / 188 (7,45 %)**

#### A. Bloc F016 — Techniquement livré / Preuves environnementales différées (3 exigences)
1. **`NFR-03`** (Must) : Disponibilité opérationnelle $\ge 99\%$ $\implies$ **TECHNICALLY IMPLEMENTED / NOT PROVEN** (Sondes livrées en F016, mesure L3 différée en environnement).
2. **`NFR-04`** (Must) : Temps de réponse applicatif $< 3$ s $\implies$ **TECHNICALLY IMPLEMENTED / NOT PROVEN** (Instrumenté en F016, tir k6 L3 différé en environnement).
3. **`NFR-10`** (Must) : Accessibilité 24/7 & résilience continue $\implies$ **TECHNICALLY IMPLEMENTED / NOT PROVEN** (Mode dégradé/maintenance livré, tir d'endurance différé).

#### B. Bloc Hors F016 — Exigences restantes (11 exigences)
4. **`NFR-05`** (**Must** — corrigé, était « Should ») : Volumétrie & capacité de charge $\implies$ **BLOCKED_PC** (PC-01 : Profil de charge, volumétrie cible usagers/tickets non arrêtés par la DSI).
5. **`NFR-06`** (Must) : Sauvegardes & Plan de Reprise / RTO-RPO $\implies$ **BLOCKED_INFRA / EXPLOITATION** (Nécessite l'infrastructure de backup, scripts de dump/restore et protocole de bascule).
6. **`NFR-09`** (**Should** — corrigé, était « Must ») : Hébergement cible & conformité Groupe $\implies$ **BLOCKED_DSI / BLOCKED_INFRA** (PC-13 : Choix d'infrastructure cloud/on-premise et politique Groupe non formalisés).
7. **`ENR-04`** (Should) : Notification de convocation par SMS / WhatsApp $\implies$ **BLOCKED_PROVIDER / BLOCKED_PC** (PC-16 : Fournisseur externe SMS/WhatsApp non contractualisé ; canal journalisé livré en F009).
8. **`IA-01`** (**Must** — corrigé, était « Could ») : L'assistant répond aux questions simples des clients (portail, pièces à fournir, état d'avancement, prochaines étapes) en langage naturel, en français $\implies$ **BLOCKED_DSI** (PC-18).
9. **`IA-02`** (**Must** — corrigé, était « Could ») : L'assistant oriente le client dans ses démarches (choix du parcours, du module, de la catégorie) et l'aide à compléter ses dossiers $\implies$ **BLOCKED_DSI** (PC-18).
10. **`IA-03`** (**Must** — corrigé, était « Could ») : L'assistant n'escalade vers un opérateur humain que lorsque cela est réellement nécessaire ; le passage se fait sans perte de contexte $\implies$ **BLOCKED_DSI** (PC-18).
11. **`IA-04`** (**Should** — corrigé, était « Could ») : L'assistant s'appuie sur une base de connaissances administrable (FAQ, procédures) sans développement, et cite ses sources $\implies$ **BLOCKED_DSI** (PC-18).
12. **`IA-05`** (**Should** — corrigé, était « Could ») : L'assistant est accessible depuis le portail et, si possible, depuis les canaux de messagerie configurés $\implies$ **BLOCKED_DSI** (PC-18).
13. **`IA-06`** (**Must** — corrigé, était « Could ») : L'assistant identifie les situations nécessitant une intervention humaine et permet le transfert sans perte de contexte $\implies$ **BLOCKED_DSI** (PC-18).
14. **`IA-07`** (**Must** — corrigé, était « Could ») : Le client peut demander une intervention humaine ; l'assistant réduit les sollicitations inutiles sans faire barrière $\implies$ **BLOCKED_DSI** (PC-18).

---

### 2 bis. CORRECTIONS APPLIQUÉES APRÈS RÉCONCILIATION CDC

> Établies le 2026-09-02 en relisant `docs/cdc_text.txt` (texte contractuel) et
> `docs/01-matrice-exigences.md`. La version antérieure reste dans l'historique Git jusqu'au
> commit `d80a126`.

| Élément | Valeur erronée | Valeur contractuelle | Nature de l'erreur |
|:---|:---|:---|:---|
| `NFR-05` | Should | **Must** | Obligation minorée |
| `NFR-09` | Must | **Should** | Obligation majorée |
| `IA-01`..`IA-07` — priorité | « Could » (7) | **5 Must + 2 Should** | Obligation transformée en option |
| `IA-01`..`IA-07` — objet | 7 cas d'usage distincts (prédiction d'affluence, détection d'anomalies, routage intelligent, synthèse, aide à la décision, contentieux prédictif) | **Un assistant conversationnel unique** | Descriptions sans source contractuelle |

**Conséquence.** Les 14 exigences restantes comptent **10 Must et 4 Should**, et non une majorité
de Could. Le pourcentage de conformité (174 / 188) est inchangé.

**Origine.** `docs/02-points-a-clarifier.md` (PC-18) décrit correctement l'assistant : « réponses
en français, orientation, escalade, base de connaissances administrable ». L'écart a donc été
**introduit dans le présent document**, il n'est pas hérité de l'analyse initiale.

---

### 3. ANALYSE D'ÉLIGIBILITÉ DES CANDIDATS & SCORING

| Candidat | Type de Gate | Exigences | Gain Max | Dépendances Bloquantes | Score /100 | Statut |
|:---|:---|:---:|:---:|:---|:---:|:---:|
| **F016 Environment Gate** | **Certification Gate** | NFR-03, NFR-04, NFR-10 | +3 | Environnement Staging déployé + Tir k6 | **90 / 100** | **RECOMMENDED (Dès accès Staging)** |
| **Atelier DSI PC-01 / PC-13 / PC-18** | **DSI Decision Gate** | NFR-05, NFR-09, IA-01..07 | +9 | Arbitrage DSI / Décision Groupe | **80 / 100** | **PRIORITÉ IMMÉDIATE SANS DÉPLOIEMENT** |
| **NFR-06 Backup / PRA** | **Infra / Exploitation Gate** | NFR-06 | +1 | Infra de backup / Procédure de restauration | **65 / 100** | **BLOCKED INFRA** |
| **ENR-04 Fournisseur SMS** | **Provider Integration** | ENR-04 | +1 | Fournisseur / Clés API / Contrat | **45 / 100** | **BLOCKED PROVIDER** |
| **Feature 018 DEV** | **Feature DEV** | Aucune exigence candidate READY sans arbitrage | +0 | Arbitrages DSI indispensables | **0 / 100** | **NOT READY (Aucun code dev ready)** |

---

### 4. AUDIT DES DÉPENDANCES ET POINTS À CLARIFIER

1. **PC-01 (NFR-05 — Volumétrie / Charge)** : Non spécifiable en DEV sans volumétrie cible arrêtée par la DSI (nombre de sessions concurrentes, taille moyenne journalière).
2. **PC-13 (NFR-09 — Hébergement Groupe)** : Exigence purement organisationnelle et d'architecture d'hébergement.
3. **PC-16 (ENR-04 — Fournisseur SMS / WhatsApp)** : Le socle applicatif F009 dispose déjà de l'abstraction `ICanalNotification` ; le raccordement physique dépend exclusivement d'un contrat commercial fournisseur.
4. **PC-18 (IA-01 à IA-07 — Module Intelligence Artificielle)** : Les 7 exigences Could nécessitent un cadrage DSI sur la souveraineté, le budget LLM et les cas d'usage prioritaires.
5. **NFR-06 (Sauvegardes & Restauration)** : Dépend de l'infrastructure d'hébergement et des mécanismes d'exploitation système.

---

### 5. ROADMAP OFFICIELLE POUR ATTEINDRE 188 / 188 (100 %)

```text
Baseline post-F017 : 174 / 188 (92,55 %)

Étape 1 : F016 Environment Certification Gate (+3)
  - NFR-03 (Disponibilité >= 99%)
  - NFR-04 (Performance < 3s k6)
  - NFR-10 (Résilience 24/7)
  ==> Projection : 177 / 188 (94,15 %)

Étape 2 : Atelier DSI / Clarification Cible (+0 dev immédiat)
  - Résolution PC-01 (NFR-05), PC-13 (NFR-09), PC-16 (ENR-04), PC-18 (IA-01..07)

Étape 3 : Homologation Infrastructure & Exploitation (+2)
  - NFR-06 (Procédure & validation de restauration DB/GED)
  - NFR-09 (Certification d'hébergement Groupe)
  ==> Projection : 179 / 188 (95,21 %)

Étape 4 : Intégration Fournisseur Télécoms (+1)
  - ENR-04 (Raccordement provider SMS/WhatsApp réel)
  ==> Projection : 180 / 188 (95,74 %)

Étape 5 : Campagne de Performance & Montée en Charge (+1)
  - NFR-05 (Validation charge cible selon profil DSI)
  ==> Projection : 181 / 188 (96,28 %)

Étape 6 : Cadrage & Implémentation IA (+7)
  - IA-01 à IA-07 (Modules d'assistance et prédiction)
  ==> CIBLE CDC : 188 / 188 (100,00 %)
```

---

### 6. RECOMMANDATION FINALE

- **Aucune Feature DEV n'est prête à être développée immédiatement sans arbitrage DSI préalable**.
- **Actions prioritaires recommandées** :
  1. Si un environnement de staging/UAT instrumenté est accessible $\implies$ **Lancer le F016 Environment Certification Gate (+3 potentiels)**.
  2. Si aucun environnement déployé n'est disponible $\implies$ **Ouvrir l'Atelier DSI / Clarification Gate pour arbitrer PC-01, PC-13, PC-16 et PC-18**.
