# Contracts — Contrats fonctionnels des cas d'usage de Feature 002

**Feature** : `002-comptes-clients-societes` | **Date** : 2026-08-29 | **Phase 1 de `/speckit.plan`**

> **Cadre** : descriptions **fonctionnelles** des cas d'usage (acteurs, préconditions, effet, règles,
> exigences couvertes, audit, erreurs/refus). **Aucun code, aucune signature C#, aucun contrat d'API
> technique** — la déclinaison technique (commandes applicatives, pages Blazor) relève de
> `/speckit.tasks` et `/speckit.implement`. Chaque contrôle d'autorisation est **vérifié côté
> serveur** (Principe VII). Numérotation locale à cette feature (`C2-xx`), distincte de `C-01..C-25`
> de Feature 001.

---

## A. Cycle de vie du compte société (CIT + demandeur)

### C2-01 — Soumettre une demande de compte société
- **Acteur** : visiteur non encore titulaire d'un compte (représentant de la future société).
- **Préconditions** : aucune (accès public au formulaire de demande).
- **Effet** : création d'une société cliente à l'état « en attente de validation », avec le profil
  déclaré (normal / en compte / deposit / IES) et les informations/justificatifs fournis (contenu
  paramétrable — PC-08, non fixé ici) ; non utilisable pour s'authentifier.
- **Règles** : le profil déclaré n'est pas encore confirmé (le sera à la validation, C2-02) ; aucune
  information personnelle non exigée par le CDC n'est demandée (principe de minimisation, `spec.md`).
- **Exigences** : CPT-01, CPT-02 (FR-001, FR-002 — US1, scénario 1).
- **Audit** : entrée journal (auteur = demandeur, action = « demande de compte soumise »).
- **Erreurs/refus** : profil non reconnu parmi les 4 valeurs fermées → refus avec message explicite.

### C2-02 — Valider ou rejeter une demande de compte société
- **Acteur** : administrateur fonctionnel CIT (permission `compte.valider`, réutilise
  `IAutorisationService` de Feature 001).
- **Préconditions** : société cliente à l'état « en attente de validation ».
- **Effet** : *validation* → état « active », profil confirmé, le demandeur devient automatiquement
  le premier administrateur de société (`spec.md` § Assumptions), `NombreAdministrateursActifs = 1`.
  *Rejet* (motif obligatoire) → état « refusée », le compte reste inutilisable.
- **Règles** : deux administrateurs fonctionnels ne peuvent pas valider/rejeter la même demande deux
  fois (concurrence — un seul réussit, par analogie avec la prise en charge concurrente de
  Feature 001, `research.md` D7) ; aucune valeur de profil n'est modifiée à la validation au-delà de
  sa confirmation.
- **Exigences** : CPT-01, CPT-02 (FR-001, FR-002 — US1, scénarios 2-4).
- **Audit** : entrée journal (auteur = administrateur fonctionnel, action = « compte validé » ou
  « compte rejeté », détail = motif si rejet).
- **Erreurs/refus** : appelant non habilité → `AccesRefuseException` (Feature 001, réutilisée) ;
  société déjà décidée (état ≠ en attente) → refus, aucune double décision possible.

## B. Authentification (titulaire de compte)

### C2-03 — S'authentifier
- **Acteur** : utilisateur rattaché à une société.
- **Préconditions** : société à l'état « active » ; rattachement utilisateur actif (`UtilisateurSociete.EstActif`).
- **Effet** : accès accordé (Identity, Feature 001, réutilisé tel quel — D1).
- **Règles** : refus si société non validée, société refusée, ou rattachement désactivé — message ne
  révélant pas l'état exact du compte (scénario négatif, `spec.md` US2 scénario 2).
- **Exigences** : CPT-03 (FR-003 — US2, scénarios 1-2).
- **Audit** : hors périmètre de ce contrat (authentification technique, Feature 001/`/speckit.plan`).
- **Erreurs/refus** : identifiants incorrects, compte non actif, rattachement désactivé → accès
  refusé, message générique.

### C2-04 — Récupérer un mot de passe oublié
- **Acteur** : titulaire d'un compte validé.
- **Effet** : redéfinition autonome du mot de passe, sans intervention CIT (mécanisme Identity natif,
  Feature 001).
- **Règles** : politique de robustesse du mot de passe déjà en place (Feature 001, `AddIdentityCore`)
  appliquée sans changement.
- **Exigences** : CPT-03 (FR-003 — US2, scénarios 3-4).
- **Audit** : hors périmètre (mécanisme technique Identity).
- **Erreurs/refus** : nouveau mot de passe non conforme à la politique → refus avec message explicite.

### C2-05 — Activer une authentification renforcée (2FA)
- **Acteur** : titulaire d'un compte actif.
- **Préconditions** : compte actif et authentifié.
- **Effet** : activation du second facteur (Identity natif, D9) ; les connexions suivantes l'exigent.
- **Règles** : jamais imposé par défaut (Could — CPT-07).
- **Exigences** : CPT-07 (FR-007 — US6, scénario 1).
- **Audit** : hors périmètre (mécanisme technique Identity).
- **Erreurs/refus** : aucun refus fonctionnel — capacité optionnelle.

## C. Gestion des utilisateurs rattachés (administrateur de société)

### C2-06 — Rattacher un utilisateur à une société
- **Acteur** : administrateur de société (permission `compte.gerer_collaborateurs`, portée à SA
  société uniquement — jamais une autre, CPT-04).
- **Préconditions** : société active ; appelant administrateur actif de cette société.
- **Effet** : nouveau rattachement créé, état actif, droit de consultation initial « ses demandes
  uniquement », droit de soumission initial (valeur par défaut — paramètre non fixé ici, PC-08 par
  analogie).
- **Règles** : cloisonnement — un administrateur ne peut rattacher un utilisateur qu'à SA propre
  société.
- **Exigences** : ENT-01, ENT-02 (FR-008, FR-009 — US3, scénario 1).
- **Audit** : entrée journal (ENT-04).
- **Erreurs/refus** : appelant non administrateur de cette société → refus côté serveur.

### C2-07 — Activer ou désactiver un utilisateur rattaché
- **Acteur** : administrateur de société.
- **Préconditions** : cible rattachée à la même société que l'appelant.
- **Effet** : `UtilisateurSociete.EstActif` bascule ; un utilisateur désactivé ne peut plus
  s'authentifier dans le contexte de sa société ; son historique n'est jamais supprimé (D10).
- **Règles** : **si la cible est le dernier administrateur actif de la société, la désactivation est
  refusée** (invariant CLAR-03, `research.md` D6) — y compris si l'appelant tente de se désactiver
  lui-même.
- **Exigences** : ENT-02 (FR-009 — US3, scénarios 3, 6).
- **Audit** : entrée journal (ENT-04).
- **Erreurs/refus** : cible = dernier administrateur actif → `DernierAdministrateurActifException` ;
  appelant non administrateur de cette société → refus ; conflit de concurrence (D7) → nouvelle
  tentative demandée à l'appelant.

### C2-08 — Modifier les droits de consultation et de soumission d'un utilisateur
- **Acteur** : administrateur de société.
- **Préconditions** : cible rattachée à la même société que l'appelant.
- **Effet** : `DroitConsultation` et/ou `DroitSoumission` mis à jour, effet immédiat.
- **Règles** : aucune valeur par défaut permissive n'est réintroduite silencieusement ; cloisonnement
  société identique à C2-06/C2-07.
- **Exigences** : ENT-02 (FR-009 — US3, scénario 2).
- **Audit** : entrée journal (ENT-04, détail avant/après).
- **Erreurs/refus** : appelant non administrateur de cette société → refus ; conflit de concurrence
  (D7) → nouvelle tentative demandée.

### C2-09 — Attribuer ou retirer le rôle Administrateur de société
- **Acteur** : administrateur de société (existant).
- **Préconditions** : cible rattachée à la même société.
- **Effet** : *attribution* → la cible devient administratrice (aucune limite au nombre d'administrateurs
  simultanés — CLAR-03) ; *retrait* → la cible redevient simple utilisateur de société.
- **Règles** : **le retrait du rôle du dernier administrateur actif est refusé** (CLAR-03, même
  invariant que C2-07) — y compris auto-retrait.
- **Exigences** : ENT-02 (FR-009 — US3, scénario 6).
- **Audit** : entrée journal (ENT-04).
- **Erreurs/refus** : cible = dernier administrateur actif → `DernierAdministrateurActifException` ;
  appelant non administrateur de cette société → refus.

## D. Consultation et visibilité

### C2-10 — Consulter le profil d'une société et ses utilisateurs rattachés
- **Acteur** : tout utilisateur rattaché à la société consultée, ou administrateur fonctionnel CIT
  habilité.
- **Préconditions** : appelant rattaché à la société consultée (ou CIT habilité).
- **Effet** : profil déclaré, type de client, liste des utilisateurs rattachés (identité, rôle, état,
  droits) — à titre informatif pour tout collaborateur (`spec.md` § Multi-utilisateurs, partage du
  compte lui-même, distinct du partage des demandes).
- **Règles** : cloisonnement inter-société absolu — jamais le profil d'une autre société (CPT-04).
- **Exigences** : CPT-04, ENT-01, ENT-03 (cas d'usage #7 déduit — `spec.md`).
- **Audit** : hors périmètre (consultation non sensible au sens de `spec.md` § Sécurité, sauf accès
  CIT — voir C2-10bis si un besoin de traçabilité des consultations CIT émergeait, non requis par le
  CDC ici).
- **Erreurs/refus** : appelant non rattaché à cette société et non CIT habilité → refus côté serveur.

### C2-11 — Déterminer la visibilité applicable à une demande (service consommé par les futures features métier)
- **Acteur** : appel interne, déclenché par un module consommateur (ex. future Billing Office) pour
  le compte d'un utilisateur rattaché.
- **Préconditions** : utilisateur rattaché à une société active.
- **Effet** : retourne le filtre de visibilité applicable (`research.md` D5) : demandes dont
  `ClientId == société de l'appelant` **ET** (`AuteurId == utilisateur appelant` **SI** droit
  « ses demandes uniquement » **ET NON** administrateur de société) ; sinon `ClientId == société de
  l'appelant` seul (droit total ou administrateur — CLAR-02) ; refus si aucun rattachement actif.
- **Règles** : un administrateur de société voit toujours l'ensemble des demandes de sa société,
  indépendamment de son propre `DroitConsultation` individuel (CLAR-02) ; jamais de filtre par
  défaut permissif (Principe VII).
- **Exigences** : ENT-03, CPT-04 (FR-010 — US4, scénarios 1-3, 5 ; portion CPT-04 de REC-SCE-14).
- **Audit** : hors périmètre (règle de filtrage, pas une action sensible en tant que telle).
- **Erreurs/refus** : utilisateur sans rattachement actif → refus, aucune demande visible.

### C2-12 — Modifier le type de client d'une société
- **Acteur** : administrateur fonctionnel CIT (permission `compte.type_modifier`).
- **Préconditions** : société existante (tout état).
- **Effet** : nouveau profil enregistré, avec ancien profil, auteur, date/heure tracés (journal
  d'audit, D8) ; **aucune modification rétroactive** des dossiers/tickets déjà créés — leur
  `ClientId`/société d'attribution reste inchangé (invariant §5.7 de `data-model.md`).
- **Règles** : la définition de « ticket en cours » et son traitement concret dans un workflow
  Billing Office restent hors périmètre (feature future, `spec.md`).
- **Exigences** : CPT-06 (FR-006 — US5, scénarios 1-3).
- **Audit** : entrée journal (CPT-06, détail avant/après).
- **Erreurs/refus** : appelant non administrateur fonctionnel → refus côté serveur ; conflit de
  concurrence (D7) → nouvelle tentative demandée.
