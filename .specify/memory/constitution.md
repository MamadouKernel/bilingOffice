<!--
Sync Impact Report
==================
Version change: [TEMPLATE] → 1.0.0 (ratification initiale)
Bump rationale: MAJOR — première ratification d'une constitution de projet réelle,
  remplaçant l'ensemble des 5 placeholders génériques du gabarit Spec Kit
  ([PRINCIPLE_1_NAME]…[PRINCIPLE_5_NAME]) par 17 principes spécifiques au projet.
  Un saut direct à 1.0.0 (et non 0.1.0) est retenu car ce document engage dès
  maintenant l'ensemble des décisions /speckit.plan, /speckit.specify et
  /speckit.implement à venir.

Modifié :
  - [PRINCIPLE_1_NAME] à [PRINCIPLE_5_NAME] (génériques) → 17 principes numérotés
    I à XVII, spécifiques à la Plateforme Digitale CIT (Stack technique,
    Source de vérité et traçabilité, Priorisation, Architecture, Domaine métier,
    Workflows/paramétrabilité, Sécurité, Données, Audit, Documents, Intégrations,
    Tests, Performance/exploitation, Observabilité, Points non résolus,
    Definition of Done, Gouvernance des écarts d'implémentation).

Ajouté :
  - Préambule (identité du projet, source de vérité)
  - Section « Portée de Cette Constitution » ([SECTION_2_NAME] du gabarit) :
    limites explicites (pas de code, pas de modèle SQL détaillé, pas de tables,
    pas de classes de domaine, pas d'architecture détaillée figée, pas de
    résolution arbitraire des points ouverts, pas de transformation des
    recommandations techniques en exigences métier).
  - Section « Documents de Référence et Chaîne de Traçabilité » ([SECTION_3_NAME]
    du gabarit) : liste des documents d'analyse, statistiques de référence
    (188 exigences : 150 Must / 32 Should / 6 Could), chaîne de traçabilité
    cible CDC → Exigence → User Story → Critère d'acceptation → Plan → Task →
    Implémentation → Test, et points bloquants nécessitant une attention
    particulière (PC-01, PC-03, PC-07, PC-08).
  - Section Governance complète (procédure d'amendement, politique de
    versionnement sémantique, revue de conformité).

Supprimé : aucun (le gabarit n'avait pas de contenu substantif à retirer).

TODO différés : aucun. Tous les placeholders du gabarit ont été résolus.
-->

# Plateforme Digitale de Gestion des Parcours et Demandes Clients — CIT Constitution

## Préambule

Ce projet numérise les parcours clients du Billing Office, du Customer Complaint
et de l'Enrôlement Transporteurs de Côte d'Ivoire Terminal (CIT), sur la base du
cahier des charges `docs/CDC_Plateforme_Digitale_CIT_v3.docx`, qui constitue la
**source de vérité métier unique** de ce projet.

L'analyse préalable, validée et tracée dans `docs/00-analyse-cdc.md`,
`docs/01-matrice-exigences.md`, `docs/02-points-a-clarifier.md`,
`docs/03-revue-coherence-analyse.md` et `docs/04-matrice-recette.md`, a
identifié **188 exigences numérotées** (150 Must, 32 Should, 6 Could), 14
scénarios de recette contractuels (CDC §22) et 26 points à clarifier encore
ouverts. Cette constitution s'appuie sur cette base documentaire et ne la
remplace pas : en cas de doute, les documents d'analyse et le CDC priment sur
toute reformulation présente ici.

## Portée de Cette Constitution

Cette constitution fixe les **principes non négociables** qui gouvernent toute
spécification, tout plan et toute implémentation ultérieurs de ce projet. Elle
ne constitue **ni une spécification, ni un plan d'architecture**.

Cette constitution, et toute activité menée directement en son nom, **NE DOIT
PAS** :

- produire du code ;
- concevoir le modèle de données SQL détaillé ni définir des tables ;
- inventer des classes ou des entités du domaine ;
- figer prématurément une architecture technique détaillée ;
- résoudre arbitrairement un point métier ouvert (`docs/02-points-a-clarifier.md`) ;
- transformer une recommandation ou une préférence technique en exigence
  métier opposable.

Les décisions détaillées d'architecture (structure des couches, modèle de
données, découpage des projets .NET, choix d'implémentation du RBAC, etc.)
**appartiennent à `/speckit.plan`**, dans le respect des principes énoncés
ci-dessous.

## Core Principles

### I. Stack Technique Imposée

Le projet DOIT être développé avec C#, .NET / ASP.NET Core, SQL Server,
Entity Framework Core, sous la forme d'une application Web responsive. Cette
contrainte est une décision de projet (maîtrise d'ouvrage technique CIT), et
non une exigence issue du CDC : elle DOIT être traitée comme telle et ne doit
jamais être présentée comme une exigence numérotée du CDC.

L'architecture DOIT rester simple, modulaire, maintenable et évolutive. Les
microservices NE DOIVENT PAS être introduits sans nécessité démontrée et
documentée — la complexité additionnelle doit se justifier par un besoin réel,
jamais par anticipation. L'architecture détaillée (couches, projets, modules)
est définie dans `/speckit.plan`, pas ici.

*Rationale* : imposer le stack au niveau constitutionnel évite que des choix
technologiques concurrents ressurgissent lors du plan ou de l'implémentation ;
limiter la complexité protège la capacité de l'équipe CIT/DSI à maintenir la
plateforme après livraison (cf. RSQ-02, RSQ-06 dans `02-points-a-clarifier.md`).

### II. Source de Vérité et Traçabilité (NON-NÉGOCIABLE)

Le CDC (`docs/CDC_Plateforme_Digitale_CIT_v3.docx`) reste la source de vérité
fonctionnelle unique. Toute fonctionnalité DOIT être traçable jusqu'à une ou
plusieurs exigences du CDC lorsque celles-ci existent, en conservant les
références originales (`SOC-xx`, `CYC-xx`, `MSG-xx`, `WKF-xx`, `PEC-xx`,
`TRF-xx`, `TPS-xx`, `SLA-xx`, `ALE-xx`, `SUP-xx`, `DIS-xx`, `AFF-xx`, `PRI-xx`,
`MAS-xx`, `CPT-xx`, `ENT-xx`, `IMP-xx`, `UNI-xx`, `EXP-xx`, `SIM-xx`, `AVE-xx`,
`REC-xx`, `ENR-xx`, `COM-xx`, `NOT-xx`, `CON-xx`, `IA-xx`, `SEC-xx`, `NFR-xx`,
`ERR-xx`, `ACR-xx`).

La chaîne de traçabilité cible est : CDC → Exigence → User Story → Critère
d'acceptation → Plan → Task → Implémentation → Test.

Aucune exigence Must NE PEUT être silencieusement ignorée, supprimée,
fusionnée ou dépriorisée. Toute exigence Must non encore implémentée DOIT
rester explicitement identifiable (dans les tasks, le suivi, ou la
documentation), plutôt que de disparaître sans trace.

*Rationale* : c'est la condition de fiabilité posée dans
`docs/03-revue-coherence-analyse.md` (score de préparation Spec Kit
91/100, vérifié par correspondance mécanique 188/188 entre le CDC et la
matrice d'exigences) — la perdre romprait la traçabilité déjà établie.

### III. Priorisation Must / Should / Could

Les priorités du CDC — Must, Should, Could — DOIVENT être respectées
strictement telles que documentées dans `docs/01-matrice-exigences.md`. Les
exigences Must sont obligatoires pour le périmètre auquel elles appartiennent.
Une contrainte ou préférence technique NE DOIT JAMAIS modifier silencieusement
la priorité métier d'une exigence du CDC ; toute proposition de
reprioritisation technique DOIT être signalée explicitement et validée par le
métier avant d'être appliquée (cf. Principe XVII).

### IV. Principes d'Architecture

L'architecture, définie en détail dans `/speckit.plan`, DOIT respecter :
séparation claire des responsabilités, forte cohésion, faible couplage,
dépendances explicites, logique métier testable indépendamment de
l'interface, infrastructure remplaçable lorsque cela est raisonnable,
simplicité avant sophistication, absence de duplication des règles métier.

Une abstraction NE DOIT PAS être introduite sans besoin identifié. Le système
NE DOIT PAS être sur-conçu pour des besoins hypothétiques (YAGNI).

### V. Domaine Métier : Le Ticket et les Capacités Transverses

Le **ticket** constitue le concept central de tout parcours dans ce système
(principe P1 du CDC). Les capacités transverses à tous les modules — tickets,
dossiers, workflows, files, affectations, transferts, documents, messages,
SLA, notifications, audit — DOIVENT être conçues comme des capacités
réutilisables par les différents modules métier (Billing Office, Customer
Complaint, Enrôlement Transporteurs), et non redéveloppées module par module.

Les règles métier critiques NE DOIVENT PAS être dispersées dans les
contrôleurs ou les vues : elles doivent résider dans une couche métier
identifiable et testable indépendamment de l'interface (cf. Principe IV).
Cette exigence de non-duplication ne préjuge d'aucune structure de package,
de classe ou de table précise — ce choix appartient à `/speckit.plan`.

### VI. Workflows Paramétrables par Conception

Conformément au principe directeur du CDC (« workflow par défaut, non
rigide »), les workflows DOIVENT être conçus paramétrables autant que
raisonnablement possible. Les éléments que le CDC déclare administrables —
workflows, étapes, transitions, files, rattachement rôles/files, règles
d'affectation, priorités, SLA, seuils, alertes, escalades, statuts, motifs,
types de documents, et les paramètres fonctionnels listés en section 11 du
CDC — NE DOIVENT PAS être codés en dur lorsque cela peut raisonnablement être
évité.

Le workflow nominal DOIT pouvoir supporter les dérogations autorisées par les
exigences métier (WKF-02, WKF-03). Toute dérogation DOIT rester traçable.

### VII. Sécurité (NON-NÉGOCIABLE)

La sécurité est une exigence transversale à tout le système. DOIVENT être
appliqués systématiquement : authentification, autorisation, RBAC (y compris
au niveau du champ dans le contexte du traitement parallèle IES — IMP-40,
SEC-02), moindre privilège, cloisonnement des données, contrôle d'accès côté
serveur, validation des entrées, HTTPS/TLS, protection des documents, gestion
sécurisée des sessions, protection contre les vulnérabilités Web courantes,
journalisation des événements sensibles.

Une restriction affichée uniquement dans l'interface NE constitue JAMAIS une
autorisation suffisante. Les contrôles d'autorisation DOIVENT être appliqués
côté serveur, sans exception.

### VIII. Données et Persistance

SQL Server est le système de gestion de base de données du projet. Entity
Framework Core est le mécanisme principal d'accès aux données, sauf
justification documentée et validée d'un besoin contraire. Les migrations de
schéma DOIVENT être versionnées. L'intégrité des données DOIT être protégée.

Les opérations destructrices DOIVENT respecter les exigences d'historisation,
d'audit et de conservation du CDC (SOC-09, SEC-05/SOC-13, SEC-06) : un
historique métier nécessaire à la traçabilité NE DOIT JAMAIS être supprimé.
Le modèle de données détaillé (tables, clés, contraintes) n'est pas défini
ici et appartient à `/speckit.plan`.

### IX. Audit et Traçabilité Opérationnelle

Toute action que le CDC désigne comme auditable DOIT être tracée. Le journal
d'audit DOIT permettre d'identifier, au minimum lorsque requis : l'auteur,
l'action, la date/heure, l'objet concerné, le changement réalisé et le
contexte pertinent (SOC-13, SEC-05).

L'historique fonctionnel du ticket et le journal d'audit de sécurité
répondent à des finalités différentes (l'un est consultable par les agents
habilités dans le cadre du dossier, l'autre est réservé à l'administrateur
technique pour la conformité et l'investigation) : ils NE DOIVENT PAS être
confondus lorsque le CDC les distingue. Les données d'audit NE DOIVENT PAS
être modifiables par les utilisateurs métier ordinaires — le journal est
inviolable par conception.

### X. Gestion des Documents

Les documents déposés (par les clients ou les agents) DOIVENT être traités
comme des données sensibles. DOIVENT être respectés : contrôle d'accès,
validation du type de fichier, validation de la taille, analyse de sécurité
(antivirale) lorsque prévue par le CDC (SEC-07), traçabilité, conservation
conforme à la politique définie (cf. PC-14), et téléchargement autorisé
uniquement après contrôle d'identité et d'habilitation (SEC-12).

Aucun lien envoyé par notification NE DOIT permettre de contourner
l'authentification ou l'autorisation, quelle que soit la commodité pour
l'utilisateur.

### XI. Intégrations et Périmètre Autonome

Le périmètre actuel du CDC DOIT être strictement respecté. Aucune intégration
automatique NE DOIT être implémentée avec IPAKI, IRIS, SydamWorld, IES, les
systèmes Douane, ou les plateformes de paiement, tant qu'une évolution
validée du périmètre ne l'autorise pas explicitement (principe P3 — «
fonctionnement autonome »).

L'architecture DEVRAIT néanmoins éviter tout choix qui rendrait des
intégrations futures impossibles (NFR-08 — architecture ouverte). Le
principe YAGNI s'applique : préparer l'extensibilité (points d'extension,
API bien délimitées) sans implémenter prématurément les connecteurs
eux-mêmes.

### XII. Tests et Stratégie de Validation

Les tests font partie intégrante de la fonctionnalité, pas d'une étape
séparée ou optionnelle. Chaque exigence Must DOIT être associée à une
stratégie de validation, choisie selon le besoin parmi : tests unitaires,
tests d'intégration, tests fonctionnels, tests d'autorisation, tests de
workflow, tests de sécurité, tests de performance, tests end-to-end.

Les 14 scénarios de recette extraits du CDC §22 (`docs/04-matrice-recette.md`,
REC-SCE-01 à REC-SCE-14) constituent les scénarios d'acceptation de
référence, à reproduire sans les modifier. Les exigences Must actuellement
sans scénario explicite dans le CDC (43 au 28 août 2026, cf.
`docs/04-matrice-recette.md` Partie 4) NE DOIVENT PAS être considérées comme
non testables ni comme facultatives : des scénarios complémentaires DOIVENT
être définis lors des spécifications détaillées (`/speckit.specify`).

### XIII. Performance et Exploitation

Les exigences non fonctionnelles du CDC DOIVENT être respectées. Les choix
techniques faits en `/speckit.plan` DOIVENT considérer : disponibilité
(NFR-03, NFR-10), performance (NFR-04), volumétrie (NFR-05), sauvegarde et
restauration (NFR-06), supervision, archivage (SOC-09), sécurité et
maintenabilité.

Les valeurs métier actuellement non définies par le CDC (seuils de SLA
précis, volumétrie cible, durées de conservation, etc. — cf.
`docs/02-points-a-clarifier.md`) NE DOIVENT PAS être inventées. Elles DOIVENT
rester configurables (cf. Principe VI) ou faire l'objet d'une demande de
clarification explicite avant d'être figées en dur.

### XIV. Observabilité

Toute fonctionnalité critique DOIT produire suffisamment d'informations pour
permettre le diagnostic, l'exploitation, l'investigation d'incident, le suivi
des erreurs et le suivi des traitements asynchrones.

Les mots de passe, secrets, jetons d'authentification et données sensibles
non nécessaires NE DOIVENT JAMAIS être journalisés en clair.

### XV. Gestion des Points Non Résolus (NON-NÉGOCIABLE)

Les points identifiés dans `docs/02-points-a-clarifier.md` NE DOIVENT JAMAIS
être résolus arbitrairement, à quelque étape que ce soit du projet. Lorsqu'une
spécification ou une décision technique dépend d'un point ouvert, l'équipe
DOIT : (1) signaler explicitement la dépendance, (2) documenter l'hypothèse
de travail éventuellement retenue, (3) demander une clarification si le point
est bloquant, (4) ne jamais transformer cette hypothèse en règle métier
définitive sans validation.

Une attention particulière DOIT être portée aux quatre points actuellement
classés BLOQUANT : **PC-01** (volumétrie opérationnelle cible), **PC-03**
(rôles non définis en section 6 du CDC — Caisse/Finance, Agent de guichet,
Intervenant Avoir/Exo), **PC-07** (matrice des transitions autorisées entre
files) et **PC-08** (liste exacte des pièces exigées par étape).

### XVI. Definition of Done

Une fonctionnalité n'est considérée comme terminée que si, cumulativement :

1. les exigences CDC associées sont identifiées et référencées ;
2. les critères d'acceptation sont satisfaits ;
3. les règles métier sont implémentées fidèlement au CDC ;
4. les autorisations (RBAC, cloisonnement) sont vérifiées côté serveur ;
5. les exigences d'audit applicables sont satisfaites ;
6. les tests nécessaires existent et passent ;
7. aucune exigence Must associée n'est silencieusement ignorée ;
8. les migrations de données nécessaires sont fournies et versionnées ;
9. la documentation utile est mise à jour ;
10. les points ouverts ou limitations connues sont explicitement documentés.

### XVII. Gouvernance des Écarts d'Implémentation

Toute dérogation à cette constitution, ou à une règle du CDC, DOIT être
explicite, justifiée, doit identifier les exigences concernées, documenter
les conséquences, et être validée avant implémentation. Une décision
d'implémentation NE PEUT JAMAIS modifier silencieusement une règle métier du
CDC — même pour une raison technique en apparence mineure.

## Documents de Référence et Chaîne de Traçabilité

| Document | Rôle |
|---|---|
| `docs/CDC_Plateforme_Digitale_CIT_v3.docx` | Source de vérité métier unique |
| `docs/00-analyse-cdc.md` | Analyse exhaustive du CDC par module fonctionnel |
| `docs/01-matrice-exigences.md` | Matrice des 188 exigences (référence, priorité, acteurs, dépendances, testabilité) |
| `docs/02-points-a-clarifier.md` | 26 points à clarifier et risques projet (§26 du CDC) |
| `docs/03-revue-coherence-analyse.md` | Audit de cohérence et score de préparation Spec Kit (91/100 au 28 août 2026) |
| `docs/04-matrice-recette.md` | 14 scénarios de recette CDC §22 et matrice de couverture des exigences Must |

**État de référence (28 août 2026)**, à considérer comme un instantané — le
décompte à jour fait toujours foi dans `docs/01-matrice-exigences.md` et
`docs/03-revue-coherence-analyse.md`, jamais dans cette constitution :
188 exigences (150 Must, 32 Should, 6 Could) ; 14 scénarios de recette ; 86
exigences Must couvertes directement par un scénario, 21 partiellement, 43
sans scénario CDC ; 26 points à clarifier (4 BLOQUANT, 9 CRITIQUE, 10
IMPORTANT, 3 SOUHAITABLE).

**Chaîne de traçabilité cible** :

```
CDC → Exigence → User Story → Critère d'acceptation → Plan → Task → Implémentation → Test
```

## Governance

Cette constitution prévaut sur toute autre pratique de développement pour ce
projet, y compris sur les préférences individuelles des contributeurs. Elle ne
prévaut jamais sur le CDC lui-même en matière de règle métier (Principe II).

**Amendements** : toute modification de cette constitution DOIT être
documentée (rationale explicite), accompagnée d'un rapport d'impact de
synchronisation (« Sync Impact Report » en tête de fichier), et validée avant
d'être appliquée aux artefacts Spec Kit en aval (`/speckit.plan`,
`/speckit.specify`, `/speckit.tasks`, `/speckit.implement`).

**Versionnement sémantique** :
- **MAJEUR** : suppression ou redéfinition rétro-incompatible d'un principe
  ou d'une règle de gouvernance existante.
- **MINEUR** : ajout d'un nouveau principe, ou extension matérielle d'un
  principe existant.
- **CORRECTIF** : clarification, reformulation, correction typographique ou
  affinement non sémantique.

**Revue de conformité** : toute sortie de `/speckit.plan` et tout incrément
issu de `/speckit.implement` DOIT être vérifié par rapport aux 17 principes
ci-dessus avant validation. Toute complexité ou tout écart doit être
justifié conformément au Principe XVII.

**Version** : 1.0.0 | **Ratifiée le** : 2026-08-28 | **Dernier amendement** : 2026-08-28
