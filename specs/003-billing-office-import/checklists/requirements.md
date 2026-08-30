# Specification Quality Checklist: Billing Office IMPORT & Unicité BL

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-08-30
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs) — aucune technologie, aucune classe,
      aucune table SQL, aucun endpoint mentionné ; explicitement renvoyé à `/speckit.plan`
- [x] Focused on user value and business needs — chaque User Story porte une valeur métier propre
      (soumission, unicité, instruction, aiguillage, 4 circuits, bascule présentielle)
- [x] Written for non-technical stakeholders — vocabulaire CDC (Pilote Manifeste, Contrôle
      Documentaire, BL, BS/BADT), pas de jargon technique
- [x] All mandatory sections completed — Sources, Périmètre, User Scenarios & Testing, Requirements,
      Key Entities, Success Criteria, Assumptions, Clarifications présentes

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain inline — toutes les ambiguïtés sont consolidées dans la
      section dédiée « Clarifications nécessaires avant planification » (9 points classés), à
      l'identique de la méthode déjà validée en Feature 002
- [x] Requirements are testable and unambiguous — chaque FR-0xx est formulée en DOIT/DEVRAIT avec un
      résultat observable
- [x] Success criteria are measurable — SC-001 à SC-008 chiffrées ou vérifiables sans ambiguïté
- [x] Success criteria are technology-agnostic — aucun SC ne mentionne un framework, une base de
      données ou un protocole
- [x] All acceptance scenarios are defined — 9 User Stories, chacune avec scénarios Given/When/Then
- [x] Edge cases are identified — 12 cas limites documentés (table dédiée), dont les 10 explicitement
      demandés par la tâche
- [x] Scope is clearly bounded — §0 (garde de périmètre), « Périmètre de cette feature », « Hors
      périmètre Feature 003 »
- [x] Dependencies and assumptions identified — Assumptions (5), Frontière avec le socle (5 besoins
      d'extension fonctionnels), Matrice de couverture (colonne Dépendance)

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria — Matrice de couverture §20 relie
      chaque exigence à sa User Story et son critère
- [x] User scenarios cover primary flows — soumission, unicité, instruction, aiguillage, 4 circuits
      complets (Normal/En Compte/Deposit/IES), alternative présentielle
- [x] Feature meets measurable outcomes defined in Success Criteria — SC-001 à SC-008 dérivés
      directement des exigences retenues
- [x] No implementation details leak into specification — vérifié section par section

## Vérifications spécifiques à cette tâche (§21 de la demande)

- [x] **Périmètre réconcilié** — contradiction IMP-15 (§0.1) et contradiction UNI-04 (§0.2, découverte
      additionnelle) toutes deux résolues à partir des sources primaires, jamais de la roadmap seule ;
      décompte final 24 exigences (23 Must, 1 Should, 0 Could) reconstitué et non recopié
- [x] **Toutes les exigences couvertes** — 24/24 tracées dans la Matrice de couverture §20, UNI-04
      explicitement exclue et justifiée (pas une omission silencieuse)
- [x] **Priorités exactes** — reprises littéralement de `docs/01-matrice-exigences.md`, aucune
      reprioritisation
- [x] **Aucun rôle inventé** — Pilote Manifeste/Facturation/CD/Comptable Client utilisés tels que
      DEFINED_ROLE du CDC ; rôle de validation de paiement (IMP-13) laissé neutre, PC-03 référencé
      sans être résolu
- [x] **Aucune pièce inventée** — seuls BS, BADT, facture, preuve de paiement (nommés explicitement
      par le CDC) sont cités ; PC-08 laissé ouvert pour le reste
- [x] **Aucune intégration IRIS inventée** — IRIS et la plateforme IES explicitement traités comme
      hors portail/déclaratifs (Assumption A-005, Principe XI)
- [x] **REC-SCE littéraux** — REC-SCE-01 à 04 reproduits mot pour mot depuis
      `docs/04-matrice-recette.md`, aucun renommage ; portée partielle de REC-SCE-01 explicitement
      signalée (traverse SOC-*/COM-*)
- [x] **Frontières futures explicites** — section « Hors périmètre Feature 003 » couvre EXPORT,
      Simulation Proforma, Avoir/Exonération, Customer Complaint, Notifications, Continuité
      Digital/Présentiel, intégrations externes
- [x] **Aucune architecture technique prématurée** — aucune classe, table, endpoint ; Key Entities
      restent conceptuelles ; besoins d'extension du socle décrits fonctionnellement uniquement
- [x] **Feature 001/002 réutilisées** — section dédiée « Réutilisation Feature 001 et Feature 002 »
      (10 capacités mappées), aucune réimplémentation identifiée
- [x] **Points ouverts visibles** — 9 clarifications classées (2 BLOQUANTE POUR PLAN, 4 À CONFIRMER
      AVANT IMPLÉMENTATION, 2 À CONFIRMER AVANT PRODUCTION, 1 NON BLOQUANTE)

## Notes

- Les items marqués incomplets nécessiteraient une mise à jour de `spec.md` avant `/speckit.clarify`
  ou `/speckit.plan` — **aucun item n'est incomplet** à l'issue de cette rédaction.
- **Mise à jour post-`/speckit.clarify` (session 2026-08-30)** : les deux clarifications BLOQUANTE
  POUR PLAN (CL-01 portée de l'unicité BL, CL-02 stratégie de concurrence) sont **résolues** — portée
  globale retenue pour UNI-01/02/03. Deux ambiguïtés supplémentaires découvertes et résolues pendant
  la session (CL-05 : preuve de paiement invalide ; comportement du lettrage Deposit impossible/à
  reprendre — nouvelle, non numérotée CL-xx à l'origine). Plus aucune clarification BLOQUANTE POUR
  PLAN ne subsiste — `spec.md` est prêt pour `/speckit.plan`, sous réserve des points restants classés
  À CONFIRMER AVANT IMPLÉMENTATION/PRODUCTION (non bloquants), voir « Clarifications nécessaires avant
  planification » dans `spec.md`.
