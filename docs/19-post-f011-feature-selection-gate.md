# POST-F011 FEATURE SELECTION GATE
## Plateforme Digitale CIT — Arbitrage Scrum Master & Quality Gate

> **Document** : `docs/19-post-f011-feature-selection-gate.md`  
> **Date** : 1er septembre 2026  
> **Auteur** : Scrum Master senior, Business Analyst & Quality Gate  
> **Contexte** : Clôture validée de la Feature 011 (Commit `a2dcc76351d5218f29cc6c2325978107f3359941`)  
> **Statut** : **ARBITRAGE DE SÉLECTION OFFICIEL** — Aucune nouvelle Feature commencée.

---

## 1. Baseline Technique Rejouée

| Indicateur | Valeur mesurée | Attendu | Statut |
|:---|:---:|:---:|:---:|
| **Build dotnet** | 0 erreur, 0 avertissement | 0 erreur, 0 warning | ✅ CONFORME |
| **Domain.Tests** | 158 / 158 verts | 158 / 158 | ✅ CONFORME |
| **Application.Tests** | 439 / 439 verts | 439 / 439 | ✅ CONFORME |
| **Infrastructure.Tests** | 83 / 83 verts | 83 / 83 | ✅ CONFORME |
| **Web.Tests** (sérialisé `--parallel none`) | 178 / 178 verts | 178 / 178 | ✅ CONFORME |
| **Total Tests** | **858 / 858 verts** | 858 / 858 | ✅ CONFORME |
| **EF Migrations HasPendingModelChanges** | 0 pending change | 0 pending change | ✅ CONFORME |

---

## 2. Baseline CDC Nominative Post-F011

La baseline est établie par **dénombrement nominatif ligne à ligne** à partir de `docs/17-baseline-nominative-188-exigences.md` et de la clôture de F011 (+11 exigences certifiées).

| Statut | Avant F011 | Clôture F011 | Après F011 | Pourcentage |
|:---|:---:|:---:|:---:|:---:|
| **CONFORME** | 146 | +11 | **157** | **83,51 %** |
| **PARTIELLE** | 19 | -11 | **8** | 4,26 % |
| **DIFFÉRÉE** | 4 | 0 | **4** | 2,13 % |
| **ABSENTE** | 11 | 0 | **11** | 5,85 % |
| **À CLARIFIER** | 8 | 0 | **8** | 4,26 % |
| **TOTAL** | **188** | **0** | **188** | **100,00 %** |

**Contrôle d'intégrité** : `157 + 8 + 4 + 11 + 8 = 188`. Écart : **0**.

---

## 3. Les 31 Exigences Restantes (Inventaire Nominatif)

### Groupe A — PARTIELLES (8 exigences : 6 Must, 1 Should, 1 Could)

| ID | Prio | Texte CDC / Objet | Statut | Cause / Travail manquant | Feature d'origine |
|:---|:---:|:---|:---:|:---|:---:|
| **PRI-02** | **Must** | Priorité déductible de critères métier paramétrables | PARTIELLE | Calcul de priorité automatique non branché lors de la soumission de ticket | F001 (D-F2) |
| **IMP-03** | **Must** | Numéros de ticket communiqués sur le profil et par e-mail | PARTIELLE | Canal e-mail opérationnel depuis F009 mais non certifié sur la soumission import F003 | F003 / F009 |
| **REC-02** | **Must** | Catégories de réclamations paramétrables sans développement | PARTIELLE | Gestion des catégories en base mais IHM d'administration complète et règles associées incomplètes | F006 (D-F3) |
| **REC-03** | **Must** | Orientation automatique Support Opérationnel et instruction | PARTIELLE | Routage et filtres dédiés au Support Opérationnel non finalisés | F006 (D-F3) |
| **SEC-02** | **Must** | RBAC y compris au niveau des champs pour les traitements parallèles | PARTIELLE | Bloqué par PC-05 (matrice des champs réservés IES Manifeste vs CD non spécifiée) | F001 / F003 |
| **SEC-07** | **Must** | Contrôle des documents déposés (type, taille, analyse antivirale) | PARTIELLE | Type et taille certifiés (ERR-03) ; volet scan antivirus réel absent (0 fournisseur DSI) | F001 |
| **SEC-08** | **Should** | Double authentification (2FA) pour agents et administrateurs | PARTIELLE | Code produit présent (`ActiverDoubleAuthentification.razor`) ; instabilité harnais de test xUnit en parallèle (D-T1) | Transverse |
| **SOC-14** | **Could** | Export des données | PARTIELLE | Exports partiels existants, composant d'export tabulaire transverse non certifié | F001 (D-F1) |

### Groupe B — DIFFÉRÉES (4 exigences : 0 Must, 1 Should, 3 Could)

| ID | Prio | Texte CDC / Objet | Statut | Cause / Dépendance |
|:---|:---:|:---|:---:|:---|
| **ENR-04** | **Should** | Notification chauffeur par SMS / WhatsApp lors de l'enrôlement | DIFFÉRÉE | Bloqué par **PC-16** (souscription fournisseur SMS/WhatsApp) |
| **SIM-11** | **Could** | Validations croisées avancées pour la simulation proforma | DIFFÉRÉE | Différée lors de F005 (règles tarifaires complexes optionnelles) |
| **ENR-05** | **Could** | Renouvellement de badge transporteur avec rappel automatique | DIFFÉRÉE | Différée lors de F008 |
| **COM-06** | **Could** | Canaux configurables pour les relances automatiques | DIFFÉRÉE | Différée lors de F009 (moteur de relance relève d'un paramétrage spécifique) |

### Groupe C — ABSENTES (11 exigences : 6 Must, 5 Should, 0 Could)

| ID | Prio | Texte CDC / Objet | Statut | Cause / Dépendance |
|:---|:---:|:---|:---:|:---|
| **IA-01** | **Must** | Réponses en français aux questions fréquentes par assistant IA | ABSENTE | Bloqué par **PC-18** (choix hébergement / modèle souverain / coût) |
| **IA-02** | **Must** | Orientation vers la bonne démarche / bon formulaire par l'IA | ABSENTE | Bloqué par **PC-18** |
| **IA-03** | **Must** | Escalade automatique vers un agent si nécessaire | ABSENTE | Bloqué par **PC-18** |
| **IA-06** | **Must** | Base de connaissances administrable sans développement | ABSENTE | Bloqué par **PC-18** |
| **IA-07** | **Must** | Traçabilité des échanges avec l'assistant IA | ABSENTE | Bloqué par **PC-18** |
| **IA-04** | **Should** | Synthèse automatique du dossier par IA pour l'agent | ABSENTE | Bloqué par **PC-18** |
| **IA-05** | **Should** | Suggestion de réponse pré-rédigée pour l'agent | ABSENTE | Bloqué par **PC-18** |
| **ERR-01** | **Must** | Aucune perte de saisie (système de brouillons et reprise) | ABSENTE | Capacité de sauvegarde de formulaires non finalisés absente du socle |
| **SEC-09** | **Should** | Cloisonnement strict des environnements DEV / QUAL / PROD | ABSENTE | Dépendance infrastructure d'hébergement / CI-CD |
| **NFR-07** | **Should** | Interface multilingue (version anglaise) | ABSENTE | Localisation i18n anglaise non implémentée |
| **NFR-08** | **Should** | Architecture ouverte exposant des API sécurisées | ABSENTE | Absence de contrôleurs / endpoints API publics ouverts |

### Groupe D — À CLARIFIER (8 exigences : 7 Must, 1 Should, 0 Could)

| ID | Prio | Texte CDC / Objet | Statut | Dépendance / Bloqueur |
|:---|:---:|:---|:---:|:---|
| **SEC-04** | **Must** | Chiffrement systématique des flux (HTTPS / TLS) | À CLARIFIER | **PC-13** (configuration d'infrastructure et certificats hébergeur) |
| **SEC-06** | **Must** | Protection des données personnelles et conformité ARTCI | À CLARIFIER | **PC-14** (politique de rétention légale et d'archivage) |
| **NFR-03** | **Must** | Disponibilité ≥ 99 % sur les plages ouvrées | À CLARIFIER | Exploitation / SLA hébergeur (non testable en code unitaire) |
| **NFR-04** | **Must** | Temps de réponse inférieur à 3 secondes | À CLARIFIER | **PC-01** (nécessite campagne de tir de charge sur infrastructure cible) |
| **NFR-05** | **Must** | Dimensionnement selon la volumétrie opérationnelle | À CLARIFIER | **PC-01** (données de volumétrie à figer par le cadrage métier) |
| **NFR-06** | **Must** | Sauvegardes et procédure de restauration testée | À CLARIFIER | **PC-13** (plan de sauvegarde / PRA hébergeur) |
| **NFR-09** | **Should** | Hébergement conforme aux exigences de sécurité Groupe | À CLARIFIER | **PC-13** (décision DSI / Groupe AGL sur l'hébergement) |
| **NFR-10** | **Must** | Accessibilité de la plateforme 24/7 | À CLARIFIER | Exploitation / infrastructure |

---

## 4. Audit des Dettes Ouvertes

| Identifiant dette | Feature d'origine | Exigence CDC | Impact métier | Impact production | Bloquante PROD | Bloquante Recette | Traitement recommandé |
|:---|:---:|:---:|:---|:---|:---:|:---:|:---|
| **D-T1** | Hors feature | SEC-08 | Aucun en exploitation | Build instable si tests xUnit exécutés en parallèle | NON | OUI | Isoler la fixture WebApplicationFactory ou configurer la collection xUnit sérialisée |
| **D-F3** | F006 | REC-02, REC-03 | Catégories figées en dur dans l'IHM | Nécessite intervention développeur pour ajouter une catégorie | NON | OUI | Rendre dynamique l'administration des catégories et le routage |
| **D-F2** | F001 | PRI-02 | Priorité calculée manuellement | Risque de mauvaise priorisation d'un dossier urgent | NON | NON | Implémenter la déduction automatique de priorité |
| **D-F1** | F001 | SOC-14 | Exports manuels limités | Nécessite requêtes SQL pour extraction de masse | NON | NON | Développer composant export tabulaire CSV/Excel |
| **D9-3** | F009/F010 | REC-SCE-14 | Aucun | Absence de preuve E2E transverse automatisée | NON | OUI | Créer `RecSce14NotificationsE2ETests.cs` |
| **DETTE-REC-NFR02** | F011 | NFR-02 | Aucun (CSS responsive conforme) | Validation visuelle sur périphériques physiques non automatisée | NON | NON | Campagne de recette manuelle multi-devices |
| **DETTE-CFG-SEC10** | F011 | SEC-10 | Aucun (paramétrable) | Définir la durée de session définitive de production | OUI (mise en service) | NON | Configuration `appsettings.Production.json` |
| **DETTE-F010-01** | F010 | ERR-04 | Mode dégradé documenté | Bouton UI explicite absent | NON | NON | Optionnel |

---

## 5. Réévaluation Approfondie des Exclusions F011

### SEC-02 (RBAC au niveau des champs)
- **Constat** : Le texte CDC stipule : *« RBAC […] y compris au niveau des champs pour les traitements parallèles »*. Cela vise spécifiquement le circuit IES Import/Export (Pilote Manifeste vs Pilote Contrôle Documentaire).
- **Blocage** : **PC-05 est ouvert**. Aucune spécification métier ne détaille la liste exhaustive des champs réservés par rôle. Créer un moteur générique de Field-Level Security relèverait du sur-développement (Scope Creep) interdit.
- **Verdict** : **Statut = À CLARIFIER / BLOQUÉ PAR PC-05**.

### SEC-07 (Contrôle documentaire & Antivirus)
- **Constat** : Le contrôle de type et de dimension maximale est certifié conforme (ERR-03). En revanche, le volet analyse antivirale réelle ne peut être certifié avec l'abstraction factice existante.
- **Blocage** : Aucun fournisseur / API antivirus (ClamAV, Defender API, VirusTotal) n'a été choisi ni contracté par la DSI CIT.
- **Verdict** : **Statut = BLOQUÉ PAR DÉCISION DSI**.

### SEC-08 (Second Facteur 2FA Agents / Admins)
- **Constat** : Le code produit est 100% fonctionnel (`ActiverDoubleAuthentification.razor`, TOTP). Chaque test unitaire passe avec succès en isolation (0 échec). L'échec intermittent ne survient qu'en exécution parallèle sous xUnit par collision de cookie/session du serveur de test in-memory.
- **Verdict** : **Défaut de harnais de test (D-T1)**. Parfaitement certifiable dès lors que la suite de test est configurée avec une collection isolée.

---

## 6. Audit de REC-SCE-14

- **Texte CDC** : Énonce 5 volets fondamentaux sans numérotation d'étapes (pas de 14/14 au CDC).
- **Couverture fonctionnelle** : 7/7 volets couverts dans l'application.
- **Couverture tests unitaires & intégration** : Couverte sur Domain, Application, Infrastructure et Web.
- **Couverture E2E** : Partielle (3/7 volets rejoués de bout en bout dans les suites F008/F010). Absence de `RecSce14NotificationsE2ETests.cs`.
- **Verdict** : Recommandation d'une suite E2E transverse pour clore définitivement la preuve REC-SCE-14.

---

## 7. Cartographie des Bloqueurs de Clarification (PC-*)

| Point à Clarifier | Exigences Impactées | Bloque SPEC ? | Bloque PLAN ? | Bloque IMPL ? | Bloque PROD ? |
|:---|:---:|:---:|:---:|:---:|:---:|
| **PC-01** (Volumétrie) | NFR-04, NFR-05 | NON | NON | NON | **OUI** |
| **PC-05** (Champs IES) | SEC-02 | **OUI** | **OUI** | **OUI** | **OUI** |
| **PC-13** (Hébergement) | SEC-04, NFR-06, NFR-09 | NON | NON | NON | **OUI** |
| **PC-14** (Conservation ARTCI) | SEC-06 | **OUI** | **OUI** | **OUI** | **OUI** |
| **PC-16** (SMS/WhatsApp) | ENR-04, COM-06 | NON | NON | **OUI** | **OUI** |
| **PC-18** (Assistant IA) | IA-01 → IA-07 | **OUI** | **OUI** | **OUI** | **OUI** |

---

## 8. Candidats de Feature Évalués

### CANDIDAT #1 : Feature 012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves
- **Exigences ciblées (7)** : `PRI-02` (Must), `IMP-03` (Must), `REC-02` (Must), `REC-03` (Must), `SEC-08` (Should), `SOC-14` (Could), `COM-06` (Could).
- **Livrables clés** :
  1. Résolution de la dette F006 (catégories dynamiques et routage support opérationnel REC-02/03).
  2. Raccordement e-mail sur soumission import et certification (IMP-03).
  3. Moteur de déduction de priorité paramétrable (PRI-02).
  4. Composant d'export générique tabulaire (SOC-14).
  5. Relances automatiques configurables (COM-06).
  6. Stabilisation du harnais 2FA et certification nominative (SEC-08).
  7. Écriture du test E2E transverse `RecSce14NotificationsE2ETests.cs`.
- **Dépendances externes** : **0**. Aucune décision DSI ou fournisseur externe requis.
- **Gain strict** : **+7 exigences** (4 Must, 1 Should, 2 Could).
- **Baseline projetée** : **164 / 188 (87,23 %)**.

### CANDIDAT #2 : Feature 013 — Gestion des Brouillons & Continuité de Saisie
- **Exigences ciblées (3)** : `ERR-01` (Must), `SIM-11` (Could), `ENR-05` (Could).
- **Livrables clés** : Sauvegarde automatique / manuelle des formulaires en brouillon (Import, Export, Réclamations, Enrôlement) et reprise de saisie sans perte.
- **Dépendances externes** : 0.
- **Gain strict** : **+3 exigences** (1 Must, 2 Could).
- **Baseline projetée** : **160 / 188 (85,11 %)**.

### CANDIDAT #3 : Module Assistant IA & Orientation Usager
- **Exigences ciblées (7)** : `IA-01` à `IA-07` (5 Must, 2 Should).
- **Statut** : **NOT READY** (Bloqué par **PC-18** : hébergement souverain, données portuaires, coût d'API, gouvernance).

### CANDIDAT #4 : Canaux Mobiles SMS & WhatsApp
- **Exigences ciblées (2)** : `ENR-04` (Should), `COM-06` (Could).
- **Statut** : **NOT READY** (Bloqué par **PC-16** : souscription opérateur / agrégateur SMS-WhatsApp en Côte d'Ivoire).

### CANDIDAT #5 : Production Readiness & Exploitation
- **Exigences ciblées (9)** : `SEC-04`, `SEC-06`, `SEC-09`, `NFR-03` à `NFR-06`, `NFR-09`, `NFR-10`.
- **Statut** : **NOT READY** pour développement immédiat (relève de la mise en service sur infrastructure cible et décisions DSI PC-01/13/14).

---

## 9. Matrice de Scoring Scrum Master (/100)

| Critère d'évaluation | Poids | Candidat #1 (Remédiation & Réserves) | Candidat #2 (Brouillons ERR-01) | Candidat #3 (Assistant IA) | Candidat #4 (Canaux Mobiles) | Candidat #5 (Prod Readiness) |
|:---|:---:|:---:|:---:|:---:|:---:|:---:|
| **Valeur Métier** | /25 | 23 | 18 | 22 | 14 | 16 |
| **Couverture Must** | /20 | 18 (4 Must) | 8 (1 Must) | 16 (5 Must) | 0 (0 Must) | 14 (7 Must) |
| **Réduction de Risque** | /15 | 15 | 8 | 4 | 4 | 5 |
| **Readiness / Clarté (Pénalités PC)** | /15 | 15 (0 PC) | 12 | 2 (Pénalité PC-18) | 3 (Pénalité PC-16) | 2 (Pénalités PC-01/13/14) |
| **Effort / ROI** | /10 | 9 | 6 | 4 | 4 | 5 |
| **Dépendances** | /5 | 5 (Autonome) | 4 | 1 (Dépendance LLM) | 1 (Agrégateur) | 1 (DSI/Infra) |
| **Testabilité** | /5 | 5 | 4 | 3 | 2 | 2 |
| **Réduction de Dette** | /5 | 5 | 3 | 1 | 1 | 3 |
| **TOTAL** | **/100** | **95 / 100** | **63 / 100** | **53 / 100** | **29 / 100** | **48 / 100** |

---

## 10. Recommandation Scrum Master & Quality Gate

### Feature Recommandée : **Feature 012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves**

- **Pourquoi maintenant ?**  
  C'est le seul lot à très fort ROI (7 exigences dont 4 Musts) qui ne souffre d'**aucun point de blocage ou d'ambiguïté (0 PC bloquant)**. Il assainit définitivement la dette des Features 001 à 011, sécurise le harnais de test 2FA, finalise la recette transverse REC-SCE-14, et porte la couverture du CDC à plus de **87 %**.
- **Pourquoi pas les autres ?**  
  - L'Assistant IA (Candidat 3) et les Canaux Mobiles (Candidat 4) sont bloqués par les points d'architecture et de souscription DSI (**PC-18**, **PC-16**).
  - Production Readiness (Candidat 5) relève de l'infrastructure de déploiement et des tirs de charge, pas d'un développement fonctionnel en local.
  - La gestion des brouillons (Candidat 2) a un ROI Must beaucoup plus faible (+1 Must) pour un effort d'infrastructure de persistance conséquent.
- **Gain strict** : **+7 exigences** (`PRI-02`, `IMP-03`, `REC-02`, `REC-03`, `SEC-08`, `SOC-14`, `COM-06`).
- **Baseline projetée** : **164 / 188 (87,23 %)**.

---

## 11. Roadmap vers 188/188

```
Baseline Actuelle Post-F011 : 157 / 188 (83,51 %)
    │
    ▼
[R1] Feature 012 — Remédiation Dettes & Recette Transverse (+7)
     PRI-02, IMP-03, REC-02, REC-03, SEC-08, SOC-14, COM-06 + REC-SCE-14 E2E
     ==> 164 / 188 (87,23 %)
    │
    ▼
[R2] Feature 013 — Continuité de Saisie & Brouillons (+3)
     ERR-01, SIM-11, ENR-05
     ==> 167 / 188 (88,83 %)
    │
    ▼
[R3] Feature 014 — Assistant IA & Orientation Usager (+7) [Après levée PC-18]
     IA-01 → IA-07
     ==> 174 / 188 (92,55 %)
    │
    ▼
[R4] Feature 015 — Canaux Mobiles WhatsApp/SMS & Traitements IES (+2) [Après levée PC-16 et PC-05]
     ENR-04, SEC-02
     ==> 176 / 188 (93,62 %)
    │
    ▼
[R5] Feature 016 — Production Readiness, Sécurité & Exploitation (+10) [Après levée PC-01/13/14]
     SEC-04, SEC-06, SEC-07, SEC-09, NFR-03, NFR-04, NFR-05, NFR-06, NFR-09, NFR-10
     ==> 186 / 188 (98,94 %)
    │
    ▼
[R6] Feature 017 — Clôture CDC & Internationalisation (+2)
     NFR-07 (Anglais), NFR-08 (API ouvertes)
     ==> 188 / 188 (100,00 %)
```

---

## 12. Risques & Points de Vigilance

1. **Stabilité du harnais 2FA (SEC-08)** : Nécessite d'isoler la fixture `WebApplicationFactory` pour éviter toute pollution d'état entre les tests en exécution parallèle.
2. **Intégrité des données d'administration des réclamations (REC-02/03)** : S'assurer que les catégories de réclamations existantes ne sont pas rompues par le passage en mode dynamique.

---

## 13. Verdict Quality Gate

**READY FOR FEATURE SCOPE VALIDATION**

*Le document est prêt pour validation du Product Owner / Direction de projet avant engagement de la Feature 012.*
