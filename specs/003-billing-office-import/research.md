# Research — Billing Office IMPORT & Unicité BL

**Feature** : `003-billing-office-import` | **Date** : 2026-08-30 | **Phase 0 de `/speckit.plan`**

> **Cadre** : Feature 003 s'inscrit dans la même stack 100 % C#/.NET/SQL Server que Feature 001 et
> Feature 002 (Principe I de la constitution) — **aucune technologie supplémentaire**, aucun
> microservice, aucun broker, aucun scheduler externe (Hangfire/Quartz), aucune nouvelle base.
> Chaque décision ci-dessous a été vérifiée contre le **code source réel** de Feature 001/Feature 002
> (pas seulement leur documentation), conformément à la consigne explicite de cette planification.
> Les 4 clarifications validées lors de `/speckit.clarify` (2026-08-30) sont des contraintes fermes
> de conception, référencées comme **CLARIFICATION**, jamais comme texte original du CDC :
> - **CLAR-BL-01** : le contrôle d'unicité du numéro de BL porte sur une portée **globale**
>   (plateforme entière, tous clients/sociétés confondus).
> - **CLAR-BL-02** : un ticket **Rejeté** libère son BL pour l'unicité, au même titre qu'un ticket
>   **Annulé**.
> - **CLAR-PAIEMENT-01** : une preuve de paiement jugée invalide renvoie le ticket à un état
>   « en attente de nouvelle preuve », motif tracé, par analogie avec IMP-05.
> - **CLAR-DEPOSIT-01** : un lettrage impossible ou à reprendre laisse le ticket en file
>   Comptabilité Client, sans clôture automatique ni retour vers la Facturation.

---

## D1 — Matrice de réutilisation des capacités Feature 001

> Vérifiée contre le code réel (`src/CIT.Plateforme.Domain/{Tickets,Files,Affectation,Audit}`,
> `src/CIT.Plateforme.Application/{Tickets,Abstractions,Securite}`), pas seulement
> `specs/001-socle-tickets-workflows/data-model.md`.

| Besoin Feature 003 | Capacité F001 existante (code réel) | Réutilisable telle quelle ? | Extension nécessaire | Justification |
|---|---|---|---|---|
| Unité de travail par BL | `Ticket` (`Domain/Tickets/Ticket.cs`) — numéro unique, statut, file courante, opérateur assigné, jeton de concurrence | ✅ Oui, structurellement | ➕ Un champ additif `NumeroBl` (nullable, non renseigné pour les tickets des autres modules) — voir D2 | `Ticket.Creer(...)` n'a pas de paramètre BL ; aucun champ existant ne convient (`Numero` est l'identifiant technique du ticket, pas le BL métier) |
| Regroupement multi-BL sous un même dossier | `Dossier` (`Domain/Tickets/Dossier.cs`) — `Ticket.DossierId` **non nullable**, « toujours renseigné » (SOC-11, contrat C-08) ; plusieurs tickets peuvent déjà partager un même `DossierId` | ✅ **Oui, sans aucune extension** | Aucune | C'est exactement le mécanisme qui porte SOC-11 dans le socle : créer N tickets avec le même `DossierId` reproduit une soumission multi-BL sans nouvelle structure |
| Contexte société propriétaire + auteur individuel | `Dossier.ClientId` (société, non nullable) + `Dossier.AuteurId` (Feature 002, `Guid?`) | ✅ Oui, sans extension | Aucune | Exactement le modèle CLAR-01 de Feature 002, déjà démontré et clos |
| Type de client figé (non-rétroactif) | `Dossier.TypeClient` (`TypeClient?`, Feature 002, remédiation post-audit) | ✅ Oui, sans extension | Aucune | Instantané déjà posé à la création du dossier — voir D5 |
| File « Pilote Manifeste », « Facturation », « Contrôle Documentaire », « Comptabilité Client » | `FileDeTraitement` (`Domain/Files/FileDeTraitement.cs`) — module, nom, stratégie d'affectation paramétrable | ✅ Oui, sans extension | Aucune (4 nouvelles **instances** de données, pas de nouveau code) | `FileDeTraitement.Creer(module, nom)` suffit ; le module Billing IMPORT crée 4 files paramétrées, exactement comme Feature 001 le permet déjà pour tout module |
| Transitions autorisées entre files | `TransitionAutorisee` (`Domain/Files/TransitionAutorisee.cs`) — file source, file cible, motifs autorisés | ✅ Oui, sans extension | Aucune (nouvelles **lignes** de données, pas de nouveau code) | Modélise directement Manifeste→Facturation→Validation Paiement→Contrôle Documentaire, Facturation→Comptabilité Client→Contrôle Documentaire, etc. |
| Prise en charge par un agent d'une file | `Ticket.PrendreEnCharge/AffecterManuellement` | ✅ Oui, sans extension | Aucune | Un Pilote Manifeste/Facturation/CD/Comptable Client est un opérateur habilité d'une file comme tout autre |
| Transfert entre étapes (Manifeste→Facturation, etc.) | `Ticket.Transferer(fileCibleId, destinataireId, maintenantUtc)` | ✅ Oui, sans extension | Aucune | Chaque changement d'étape du circuit IMPORT est un transfert de file au sens strict de TRF-01..06 |
| Attente d'action du client (complément Manifeste, nouvelle preuve de paiement) | `Ticket.MettreEnAttenteClient()` / `RependreApresReponseClient()` (TPS-02/04 — suspend le SLA) | ✅ **Oui, sans aucune extension** | Aucune | IMP-05 (complément) et CLAR-PAIEMENT-01 (preuve invalide) sont tous deux, fonctionnellement, un aller-retour « en attente client » — mécanisme déjà démontré, SLA suspendu comme il se doit |
| Rejet définitif d'un ticket (IMP-04) | `Ticket.Annuler(maintenantUtc)` | ✅ Oui, sans extension du Domain | Le **motif obligatoire** est tracé via `EntreeHistorique.Detail` (déjà un champ libre) à la couche Application, pas sur `Ticket` | Voir D13 — un ticket Rejeté ne doit **jamais** être rouvert ; `Ticket.Rouvrir()` interdit déjà la réouverture d'un ticket `Annule` (seul `Cloture` est réouvrable, CLAR-02) — mapper Rejeté→Cloture aurait permis une réouverture non voulue |
| Dépôt de documents (BL, facture, preuve de paiement, BS, BADT) | `Document` + `IDocumentRepository` (SOC-07, SEC-07, SEC-12) | ✅ Oui, sans extension | Aucune | Voir D11 |
| Journal d'audit + historique visible client | `EntreeJournalAudit` / `EntreeHistorique` (`IJournalisationService`) | ✅ Oui, sans extension | Aucune | Champs déjà libres (`Detail`, `DetailAvant/Après`) — suffisent à tracer décision Manifeste, motif de rejet, validation/refus de paiement, confirmation de lettrage |
| Autorisation serveur par permission | `IAutorisationService.EstAutoriseAsync/ExigerAsync` + `Permission` (codes `"module.action"`) | ✅ Oui, sans extension du mécanisme | ➕ Nouveaux **codes** de permission (données, pas de nouveau code) — voir D15 | Même mécanisme que Feature 001/002, aucune permission codée en dur |
| Cloisonnement société | `CloisonnementService.ExigerProprieteClientAsync(clientId, ticketId)` — compare `Dossier.ClientId` | ✅ **Oui, sans aucune extension** | Aucune | Appeler ce service avec l'identifiant de société suffit ; `Dossier.ClientId` porte déjà la société depuis Feature 002 |
| Numérotation du ticket | `IServiceNumerotation`/`ServiceNumerotation` — séquence SQL Server transactionnelle | ✅ Oui, sans extension | Aucune | Le numéro de ticket IMPORT est un numéro de ticket ordinaire (`module = "BillingImport"`) |
| Traitements de fond (SLA, affectation, alertes) | `SurveillanceSlaService`, `DistributionAutomatiqueService`, `RelancesAbsenceReponseService` (`Infrastructure/TraitementsDeFond`) | ✅ Oui, sans extension | Aucune | S'appliquent automatiquement à toute file/ticket paramétré, y compris IMPORT — **aucun nouveau `BackgroundService`** requis par cette feature |

**Conclusion** : Feature 003 ajoute le **domaine métier Billing IMPORT** (nouvelles données de
paramétrage : files, transitions, permissions ; trois extensions additives ciblées de `Ticket`
(`NumeroBl`, `DeclarationPresentielle`, `TypeAnnulationImport` — D2, D12, D13bis) et une entité
satellite pour le traitement parallèle IES, D8) — elle ne recrée **aucune** capacité
transverse (ticket, dossier, file, transition, prise en charge, transfert, attente client, document,
audit, autorisation, concurrence, numérotation, traitements de fond).

## D2 — Représentation métier du BL et normalisation

- **Décision** : `Ticket` reçoit un champ additif **`NumeroBl : string?`**, non renseigné pour les
  tickets des autres modules (Customer Complaint, Enrôlement…), à l'image de l'extension
  `Dossier.AuteurId`/`TypeClient` de Feature 002. `Ticket.Creer(...)` gagne un paramètre optionnel
  supplémentaire par défaut `null` — compatibilité totale des appels existants (`CreerTicketCommand`
  actuel de Feature 001 continue de fonctionner sans modification).
- **Rationale** : `Ticket.Numero` est l'identifiant technique séquentiel du ticket (SOC-01) ; le BL
  est un identifiant métier fourni par le client, de nature et de cycle de vie différents (portée
  d'unicité propre, UNI-01..03). Les confondre romprait la garantie d'unicité/non-réutilisation déjà
  démontrée sur `Ticket.Numero`.
- **Normalisation — CLARIFICATION PROJET validée le 2026-08-30 (§8 de la demande initiale, tranchée
  lors de la revue de plan du 2026-08-30)** : le CDC (`docs/cdc_text.txt` §11.2,
  `docs/00-analyse-cdc.md` §6.9) ne précise **aucune règle de casse, d'espaces, de tirets ou de
  format** pour le numéro de BL — cette règle est donc explicitement identifiée comme une
  **CLARIFICATION PROJET**, jamais comme une exigence du CDC. Règle validée pour la comparaison
  d'unicité :

  | Transformation | Appliquée à la comparaison d'unicité ? | Exemple |
  |---|---|---|
  | Insensibilité à la casse | ✅ Oui | `ABC123` ≡ `abc123` |
  | Suppression des espaces en début/fin de saisie (trim externe) | ✅ Oui | `" ABC123 "` ≡ `ABC123` |
  | Suppression des tirets | ❌ Non | `ABC-123` ≠ `ABC123` |
  | Suppression des espaces internes | ❌ Non | `"ABC 123"` ≠ `ABC123` |
  | Toute autre normalisation implicite | ❌ Non | — |

  **Valeur stockée** : `NumeroBl` conserve la valeur **trimée** (espaces externes retirés avant
  persistance — opération non destructive d'information métier, appliquée une seule fois à
  l'écriture) mais garde la casse, les tirets et les espaces internes strictement tels que saisis —
  un seul champ, aucune double représentation brute/normalisée. La casse est traitée uniquement au
  niveau de la **comparaison** (collation, voir D3), jamais en réécrivant la valeur stockée — la
  valeur affichée au client/à l'audit reste donc celle qu'il a saisie (hors espaces de bord).
- **Alternatives rejetées** : stocker le BL comme une entité séparée (« BL » distincte du ticket) —
  rejeté, un BL et son ticket IMPORT ont un cycle de vie strictement 1:1 dans ce module (UNI-03 : un
  BL peut apparaître dans un AUTRE module, mais jamais deux fois comme ticket IMPORT actif) ; une
  entité séparée dupliquerait sans bénéfice le cycle de vie déjà porté par `Ticket`.

## D3 — Unicité globale du BL sous concurrence SQL Server réelle *(point critique — revu le 2026-08-30)*

- **Contrainte métier (CLAR-BL-01, CLAR-BL-02)** : à un instant donné, au plus **un** ticket
  « actif » (`BloqueUniciteBL = vrai` — voir D13/D13bis : Cloturé, Annulé **et** Rejeté sont tous
  trois non bloquants, sans que ces trois résultats métier soient confondus entre eux) peut exister
  pour un même `NumeroBl`, **sur toute la plateforme**.

### Vérification de faisabilité (avant de conserver la décision)

Vérifiée explicitement, pas admise par intuition :
- **SQL Server** supporte nativement les **index filtrés** (`CREATE UNIQUE INDEX ... WHERE ...`,
  fonctionnalité stable depuis SQL Server 2008) avec un prédicat combinant plusieurs colonnes par des
  opérateurs simples (`IS NOT NULL`, `=`, `<>`, `IN`, `AND`) — notre prédicat cible, exprimé au
  niveau conceptuel comme :

  ```text
  NumeroBl IS NOT NULL
  AND Statut IN (<statuts bloquants explicites — voir tableau ci-dessous>)
  ```

  est un prédicat **simple** au sens de la documentation SQL Server (pas de sous-requête, pas de
  fonction non déterministe) : il est **supportable sans réserve**. *(Correction du 2026-08-30 :
  cette planification exprime désormais le prédicat par une liste d'inclusion explicite des statuts
  **bloquants**, jamais par une exclusion des statuts non bloquants — voir justification et tableau
  ci-dessous. Aucun nombre magique n'apparaît dans ce document : les valeurs entières exactes de
  `StatutTicket` restent un détail de traduction propre à `/speckit.tasks`/`/speckit.implement`.)*
- **EF Core** expose ce mécanisme nativement via `IndexBuilder.HasFilter(string)` depuis EF Core 2.0
  — un fragment T-SQL brut fourni tel quel au `CREATE INDEX` généré par la migration ; aucune
  bibliothèque tierce, aucun SQL manuel hors du pipeline de migration standard.
- **Précision vérifiée dans le code réel** (`PlateformeDbContext.cs`, bloc `builder.Entity<Ticket>`) :
  `Ticket.Statut` n'a **aucune** configuration `.HasConversion(...)` — EF Core le persiste donc par
  défaut comme un **entier**, pas une chaîne ; la traduction du prédicat conceptuel ci-dessus vers ses
  valeurs entières concrètes est un détail de `/speckit.tasks`, sans impact sur la décision elle-même.
- **Précédent déjà en production dans ce projet** : `Ticket.Numero` porte déjà
  `HasIndex(t => t.Numero).IsUnique()` (non filtré, car toujours unique) — la seule différence pour
  `NumeroBl` est le caractère **filtré** (conditionnel au statut), pas un mécanisme nouveau.

**Verdict de faisabilité : CONFIRMÉE**, sans réserve technique.

### Statuts bloquants — liste explicite (§1 de la demande, vérifiée contre `StatutTicket` réel)

> `StatutTicket` (`src/CIT.Plateforme.Domain/Tickets/StatutTicket.cs`) compte **exactement 5
> valeurs** (vérifié dans le code, aucune autre n'existe) : `EnAttenteDePriseEnCharge`, `EnCours`,
> `EnAttenteClient`, `Cloture`, `Annule`. Le tableau ci-dessous couvre les 5, plus la distinction
> métier Rejeté/Annulé introduite par D13bis (qui partage `Statut = Annule`).

| État métier | `StatutTicket` (F001, inchangé) | `TypeAnnulationImport` (D13bis) | Bloque l'unicité BL ? |
|---|---|---|---|
| En attente de prise en charge | `EnAttenteDePriseEnCharge` | — | **Oui** |
| En cours de traitement | `EnCours` | — | **Oui** |
| En attente client (complément Manifeste, nouvelle preuve de paiement) | `EnAttenteClient` | — | **Oui** |
| Clôturé | `Cloture` | — | Non |
| Annulé (client, hors périmètre Billing IMPORT le cas échéant) | `Annule` | `null` | Non |
| Rejeté (Pilote Manifeste, IMP-04) | `Annule` | `RejetManifeste` | Non |

Le prédicat conceptuel du filtre est donc, en toutes lettres :
*« un ticket bloque l'unicité de son BL si et seulement s'il est en attente de prise en charge, en
cours de traitement, ou en attente client »* — les trois seuls états où un traitement reste
véritablement « en vol ».

### Comparaison des stratégies (§2 de la demande, critères : garantie de concurrence, simplicité,
atomicité, compatibilité EF Core, migrations, libération du BL, comportement crash/rollback,
maintenance, testabilité, couplage au workflow, risque de dérive)

| Stratégie | Garantie sous concurrence réelle | Simplicité / EF Core / migrations | Libération du BL & crash/rollback | Maintenance & risque de dérive | Retenue ? |
|---|---|---|---|---|---|
| A. Vérification applicative seule (SELECT puis INSERT) | ❌ Aucune — condition de course classique entre deux transactions simultanées | Triviale mais **incorrecte** | N/A (rejetée avant même cette question) | N/A | **Rejetée** — explicitement interdite par la demande |
| **B. Index unique filtré SQL Server** sur `NumeroBl` (statuts bloquants explicitement listés) | ✅ Totale — rejet atomique de la seconde insertion par le SGBD lui-même, quelle que soit la vitesse relative des transactions | Déclaratif, standard EF Core (`.HasFilter`), une seule migration additive | Libération automatique dès que `Statut` quitte l'ensemble bloquant (aucune purge manuelle) ; un rollback de la transaction d'insertion annule atomiquement l'entrée d'index avec elle (aucun verrou fantôme, CAS7) | Aucune donnée dupliquée à maintenir en synchronisation — la seule source de vérité est `Ticket.Statut`, déjà là | **Retenue** |
| C. Colonne technique dénormalisée (`BloqueUniciteBL bool`, mise à jour à chaque transition) + contrainte unique filtrée dessus | ✅ Totale si la colonne est **toujours** correctement synchronisée avec `Statut` | Un peu plus lisible (`WHERE BloqueUniciteBL = 1`, sans connaître les valeurs numériques du statut) | Risque de **dérive** : chaque méthode qui change `Statut` (`Annuler`, `Cloturer`, `MettreEnAttenteClient`, `Transferer`…) devrait aussi mettre à jour ce booléen — un oubli produit un BL bloqué à tort ou débloqué à tort, silencieusement | **Rejetée** — c'est exactement le type de compteur dénormalisé qui a produit la dérive corrigée lors de la remédiation ENT-02 de Feature 002 (`NombreAdministrateursActifs` désynchronisé de son `COUNT` réel) ; un filtre directement sur `Statut` n'a, par construction, **aucune** seconde source de vérité à désynchroniser |
| D. Verrouillage transactionnel explicite (`UPDLOCK`/`HOLDLOCK` sur lecture préalable) | ✅ Totale, mais verrous maintenus le temps de la transaction (risque d'interblocage sous forte volumétrie) | Nécessite du SQL brut comparable à `ServiceNumerotation`, plus complexe à exprimer et à tester qu'une contrainte déclarative | Comportement correct au rollback (verrous relâchés), mais mécanisme entièrement manuel à auditer | Code de verrouillage manuel à maintenir, plus grande surface de bug | Rejetée — la stratégie B atteint la même garantie avec un mécanisme déclaratif standard |
| E. Table dédiée des BL actifs (`BlActif(NumeroBl unique, TicketId)`, purgée à la clôture/annulation) | ✅ Totale, mécanisme équivalent à B | Entité supplémentaire, migration + code de purge à écrire et maintenir | Purge manuelle nécessaire à chaque transition libératrice — un oubli laisse un « verrou fantôme » | Duplique l'état déjà porté par `Ticket.Statut` (double source de vérité) | Rejetée — contraire au Principe IV (pas d'abstraction sans besoin identifié), aucun bénéfice sur la garantie elle-même |
| F. Autre mécanisme SQL Server (`SEQUENCE`, `MERGE` atomique) | Non applicable — résout un problème différent (numérotation, upsert), pas une contrainte d'unicité conditionnelle | — | — | — | Non retenue |

- **Décision (confirmée après revue du 2026-08-30)** : Stratégie **B — index unique filtré SQL
  Server** sur `(NumeroBl)`, restreint aux lignes où `NumeroBl IS NOT NULL` **et** `Statut` ∈
  {`EnAttenteDePriseEnCharge`, `EnCours`, `EnAttenteClient`} (liste d'**inclusion** des statuts
  bloquants, jamais d'exclusion des statuts non bloquants — voir tableau ci-dessus et justification
  du choix inclusion/exclusion ci-dessous). Aucune colonne dénormalisée supplémentaire (Stratégie C
  explicitement rejetée — voir la leçon ENT-02 ci-dessus) : le filtre référence directement
  `Ticket.Statut`, seule source de vérité.
- **Pourquoi une inclusion plutôt qu'une exclusion (choix délibéré de cette revue)** : une exclusion
  (`Statut ∉ {Cloture, Annule}`) aurait absorbé **automatiquement et silencieusement** tout nouveau
  statut ajouté un jour à `StatutTicket` comme « bloquant par défaut », sans jamais forcer de révision
  explicite de cette feature. Une inclusion force au contraire une décision **consciente** : tant que
  la liste n'est pas mise à jour, un nouveau statut ne bloque **pas** l'unicité — ce qui est
  acceptable ici uniquement parce que ce risque est explicitement documenté et surveillé (voir
  « Risque d'évolution » ci-dessous), alors que `StatutTicket` est un enum **transverse**, partagé par
  tous les modules du socle : toute évolution de cet enum doit de toute façon être examinée
  individuellement pour chaque invariant qui en dépend, jamais absorbée implicitement par un seul
  module.
- **Risque d'évolution (§2 de la demande, à surveiller explicitement)** : si un nouveau statut
  `StatutTicket` représentant un traitement encore « en vol » est ajouté à l'avenir (évolution du
  socle transverse Feature 001), le filtre de cet index **devra être révisé par une nouvelle
  migration** ajoutant ce statut à la liste d'inclusion — ce n'est **pas** automatique. Ce point est
  un risque de maintenance identifié et accepté, à vérifier explicitement à chaque évolution future de
  `StatutTicket` (checklist de revue de toute PR touchant cet enum, à instaurer en `/speckit.tasks`
  ou en pratique d'équipe).
- **Comportement attendu du perdant** : la seconde transaction concurrente échoue avec une violation
  de contrainte SQL Server, traduite en `DbUpdateException` par EF Core — traduite à son tour, côté
  Application, en un résultat métier « BL déjà actif » (même famille que la traduction déjà en place
  pour `ConflitDeConcurrenceException` en Feature 002, D6/D7) — **jamais** une exception technique
  brute exposée à l'appelant.
- **Atomicité et absence de verrou fantôme (CAS7)** : la vérification d'unicité et la création du
  ticket doivent être committées dans **une seule** transaction EF Core (`SaveChangesAsync` unique, ou
  transaction explicite si plusieurs `SaveChanges` sont nécessaires) — jamais deux transactions
  séparées pour un même BL. Sous cette garde, un échec ultérieur dans le même flux entraîne un
  rollback complet, qui annule atomiquement l'entrée d'index avec l'insertion du ticket : **aucun
  verrou métier fantôme ne peut subsister**, par construction des propriétés ACID standard de SQL
  Server — aucun code de nettoyage supplémentaire n'est nécessaire.
- **Articulation avec D4 (CAS8)** : c'est précisément parce que chaque BL d'une soumission multi-BL
  est créé dans sa **propre** opération/transaction indépendante (D4) qu'un échec d'unicité sur le
  BL B ne peut techniquement pas affecter les transactions déjà committées des BL A et C — les deux
  décisions (D3 et D4) se renforcent mutuellement, pas seulement par choix métier mais par nécessité
  transactionnelle.
- **Rationale** : une seule ligne de défense déclarative, au niveau où la garantie doit
  *structurellement* être appliquée (le SGBD, seul arbitre capable de trancher deux transactions
  strictement simultanées), cohérente avec le principe déjà appliqué à `Ticket.Numero` et à
  l'invariant du dernier administrateur de Feature 002 — et, cette fois, explicitement informée par
  la dérive de compteur déjà rencontrée et corrigée sur ce même projet (ENT-02).

### Cas de concurrence obligatoires à démontrer sur SQL Server réel (§3 de la demande)

| Cas | Scénario | Résultat attendu |
|---|---|---|
| CAS1 | Deux **sociétés différentes** soumettent simultanément le même nouveau BL | Exactement une création réussit — la portée globale (CLAR-BL-01) traite CAS1 et CAS2 de façon strictement identique au niveau de l'index : aucune société n'entre dans le prédicat |
| CAS2 | Même société, deux utilisateurs, même BL, simultanément | Exactement une création réussit (même mécanisme que CAS1) |
| CAS3 | BL déjà actif existant | Nouvelle soumission refusée (UNI-01) |
| CAS4 | BL dont l'unique ticket est Clôturé | Nouvelle soumission autorisée (UNI-02) |
| CAS5 | BL dont l'unique ticket est Annulé | Nouvelle soumission autorisée (UNI-02) |
| CAS6 | BL dont l'unique ticket est Rejeté | Nouvelle soumission autorisée (CLAR-BL-02, D13/D13bis — Rejeté est non bloquant même s'il reste distingué d'Annulé en reporting) |
| CAS7 | La transaction de création échoue **après** que l'insertion a satisfait l'index unique, pour une raison indépendante | Rollback complet, aucun verrou métier fantôme ne subsiste (garanti par l'atomicité transactionnelle ci-dessus, aucun code additionnel requis) |
| CAS8 | Soumission multi-BL : A accepté, B doublon, C accepté | A et C ne sont **jamais** annulés à cause de l'échec de B (D4 — opérations indépendantes) |
| CAS9 | Soumettre `"ABC123"` puis, immédiatement après, `"abc123"` (même BL, casse différente) | Le second est **refusé** (collation explicite insensible à la casse, ci-dessous) |
| CAS10 | Soumettre `" ABC123 "` (espaces de bord) puis `"ABC123"` | Le second est **refusé** — le trim externe rend les deux valeurs identiques avant même la comparaison (D2) |
| CAS11 | Soumettre `"ABC-123"` puis `"ABC123"` | Les **deux sont autorisés** — le tiret n'est jamais supprimé, ce sont deux valeurs distinctes (D2) ; idem pour un espace interne (`"ABC 123"` vs `"ABC123"`) |
| CAS12 | Rejouer CAS9 sur une base de test dont la collation **par défaut de l'instance** diffère volontairement (ex. une instance configurée par défaut en collation sensible à la casse) | **Même résultat qu'en CAS9** — le comportement est piloté par la collation **explicite** de la colonne `NumeroBl` (ci-dessous), jamais par le défaut de l'instance : le second BL reste refusé quelle que soit la configuration du serveur |

**Test de concurrence SQL Server réel (obligatoire, tous les cas ci-dessus, CAS1→CAS12)** : exécutés
contre une base SQL Server réelle (LocalDB ou équivalent) — **jamais EF InMemory** pour
CAS1/CAS2/CAS7/CAS8/CAS9/CAS10/CAS12, cohérent avec la méthode déjà exigée pour CLAR-03 de
Feature 002.

### Collation SQL Server — déterminisme indépendant de l'instance (§4 de la demande, corrige la revue précédente)

> La revue précédente (2026-08-30, tour antérieur) avait identifié le risque de collation mais
> l'avait laissé ouvert. Cette correction le referme : la normalisation métier est désormais décidée
> (D2, CLARIFICATION PROJET ci-dessus) ; il reste à garantir qu'elle soit appliquée de façon
> **déterministe**, indépendamment de la configuration de l'instance SQL Server cible.

- **Exigence** : la même application, déployée sur deux instances SQL Server différentes (dont les
  collations par défaut peuvent différer), doit produire **exactement le même** résultat d'unicité
  pour un même couple de valeurs de BL.
- **Décision** : la colonne `NumeroBl` (et donc l'index unique filtré construit dessus) reçoit une
  **collation explicitement fixée dans le mapping EF Core** — `PropertyBuilder.UseCollation(string)`
  (disponible depuis EF Core 5, traduite par le fournisseur SQL Server en clause `COLLATE` sur la
  colonne) — **jamais héritée** de la collation par défaut de la base ou de l'instance. La collation
  retenue doit être **insensible à la casse, sensible aux accents, sans autre repli implicite** (par
  exemple `Latin1_General_CI_AS` — le nom exact précis reste un détail de `/speckit.tasks`, la
  décision de plan porte sur le fait qu'elle soit **explicite et fixe**, jamais implicite).
- **Ce que la collation ne fait PAS** : une collation ne contrôle que les règles de **comparaison des
  caractères** (casse, accents) — elle ne supprime **jamais** un caractère. `"ABC-123"` et `"ABC123"`
  restent des chaînes distinctes sous **n'importe quelle** collation SQL Server (le tiret est un
  caractère à part entière, pas un séparateur ignoré) : la règle « ne pas supprimer les tirets » (D2)
  est donc automatiquement respectée, sans mécanisme dédié — de même pour les espaces internes.
- **Le trim (espaces externes) est une opération distincte de la collation** — une collation ne trime
  rien ; le trim est appliqué **avant persistance**, au niveau applicatif (D2), indépendamment du
  choix de collation.
- **Garantie de portabilité** : la collation étant fixée explicitement dans le modèle (donc reproduite
  identiquement par la migration EF Core, quelle que soit l'instance cible), le comportement
  d'unicité — y compris l'insensibilité à la casse — est garanti **identique** sur toute instance SQL
  Server, résolvant CAS12 par construction plutôt que par chance de configuration.

## D4 — Frontière transactionnelle d'une soumission multi-BL

- **Décision** (résout FR-006, US1/US3 scénario 4 de `spec.md`, déjà validé lors du `/speckit.clarify`
  de Feature 003) : chaque **paire** « vérification d'unicité + création du ticket » pour un BL
  donné s'exécute comme une opération **indépendante** des autres BL de la même soumission — **jamais**
  une transaction globale « tout ou rien » enveloppant les 5 BL possibles. Un échec d'unicité sur un
  BL ne doit produire **aucun effet secondaire** sur les autres BL de la même soumission (ni retour
  arrière, ni blocage).
- **Rationale** : c'est la lecture directe de IMP-02 (« totalement indépendant ») et du scénario
  contractuel REC-SCE-01 (3 BL soumis ensemble aboutissant à 3 décisions distinctes) — une
  transaction globale romprait mécaniquement ce comportement dès la création (avant même l'instruction
  du Pilote Manifeste).
- **Conséquence pour `/speckit.tasks`** : le cas d'usage de soumission multi-BL doit boucler sur
  chaque BL et committer (ou laisser échouer indépendamment) chaque création, jamais envelopper
  l'ensemble dans une unique portée transactionnelle EF Core.
- **Alternative rejetée** : une transaction unique avec `SavePoint` par BL — rejetée, complexité
  inutile (Principe IV) pour un besoin déjà satisfait par des opérations indépendantes séquentielles
  ou parallèles simples.

## D5 — Aiguillage automatique piloté par l'instantané TypeClient (IMP-06)

- **Décision** : l'aiguillage lit **exclusivement** `Dossier.TypeClient` (Feature 002, instantané figé
  à la création du dossier, remédiation post-audit CPT-06) — **jamais**
  `SocieteCliente.Profil` (valeur courante, mutable via `ModifierTypeClientCommand`).
- **Rationale** : c'est la clarification explicitement demandée en §6 de cette planification, déjà
  actée dans `spec.md` FR-012/US4 scénario 3, et directement permise **sans aucune extension** par le
  champ déjà existant. Router sur la valeur courante violerait CPT-06 (non-rétroactivité déjà
  démontrée et close par Feature 002) dès qu'un changement de type interviendrait entre la soumission
  et la décision du Pilote Manifeste.
- **Alternative rejetée** : relire `SocieteCliente.Profil` au moment de l'aiguillage — rejetée
  explicitement (interdiction formelle de la demande, rouvrirait CPT-06).

## D6 — Les 4 variantes (Normal / En Compte / Deposit / IES) comme paramétrage du moteur F001

- **Décision** : les 4 circuits ne sont **pas** 4 moteurs de workflow séparés, mais 4 **parcours de
  files** différents empruntés par un même ticket, exprimés uniquement par des **données**
  (`FileDeTraitement` + `TransitionAutorisee`) :
  - **Normal** : Manifeste → Facturation → Validation Paiement → Contrôle Documentaire → clôturé.
  - **En Compte** : Manifeste → Facturation → Contrôle Documentaire (transition directe, sans passer
    par la file Validation Paiement) → clôturé.
  - **Deposit** : Manifeste → Facturation → Comptabilité Client (lettrage) → Contrôle Documentaire →
    clôturé.
  - **IES** : Manifeste **et** Contrôle Documentaire en parallèle (voir D8/D9) → clôturé.
- **Rationale** : c'est exactement le rôle de la matrice de transitions paramétrable déjà livrée par
  Feature 001 (WKF-01, `TransitionAutorisee`) — l'aiguillage (D5) sélectionne simplement la **première
  transition** du parcours correspondant au type de client ; le reste du parcours est une succession
  de transferts standards, aucune nouvelle machine à états n'est nécessaire.
- **Alternative rejetée** : un `enum CircuitImport` avec une logique conditionnelle dispersée dans
  l'Application — rejetée, duplique en code ce que la matrice de transitions exprime déjà comme
  donnée paramétrable (Principe VI).

## D7 — Deposit / IRIS hors portail : confirmation de lettrage sans intégration

- **Décision** : le lettrage lui-même s'effectue **hors plateforme**, dans IRIS (Principe XI). La
  plateforme ne fait que tracer la **confirmation** du Comptable Client, représentée par un simple
  **transfert de file** (Comptabilité Client → Contrôle Documentaire), sans aucune donnée
  supplémentaire persistée au-delà de l'historique/l'audit déjà standard (auteur, horodatage).
  Aucune entité « Lettrage » n'est créée. Si le lettrage est **impossible ou à reprendre**
  (CLAR-DEPOSIT-01), le ticket reste simplement dans la file Comptabilité Client — **aucune action**,
  aucun nouveau statut, aucune transition.
- **Rationale** : le CDC ne décrit qu'une confirmation binaire (« le comptable confirme dans le
  ticket ») — inventer une structure de données IRIS (référence de lettrage, date de valeur, etc.)
  reviendrait à halluciner un contenu non fourni par le CDC. Le transfert de file est suffisant et
  strictement conforme à ce qui est écrit.
- **Alternative rejetée** : une entité conceptuelle « Lettrage » avec ses propres champs — rejetée,
  aucun champ n'est décrit par le CDC au-delà de la confirmation elle-même (déjà portée par l'audit).

## D8 — Traitement parallèle IES : deux branches indépendantes, une entité satellite légère

- **Contrainte** : `Ticket` ne peut se trouver, à un instant donné, que dans **une seule** file
  (invariant #3 de `data-model.md` de Feature 001, vérifié dans le code — `FileCouranteId` est une
  valeur scalaire unique). Le traitement IES exige pourtant que **deux rôles distincts** progressent
  **indépendamment** sur le même ticket.
- **Décision** : introduction d'une **unique** entité satellite nouvelle, `SuiviBrancheIes` (nom
  provisoire, à confirmer en `/speckit.tasks`) — un enregistrement par branche et par ticket IES
  (donc 2 lignes par ticket) : `TicketId`, `Branche` (Manifeste | ContrôleDocumentaire),
  `EstTerminee` (bool), `AuteurId`, `DateAchevementUtc`. Le `Ticket` lui-même reste dans **une seule**
  file dédiée (« File IES »), à laquelle les deux rôles (Pilote Manifeste et Pilote Contrôle
  Documentaire) sont rattachés simultanément — un rattachement multi-rôles vers une même file est
  déjà supporté nativement par Feature 001 (SEC-11, plusieurs habilitations peuvent être rattachées à
  une même file).
- **Rationale** : reproduit le style déjà établi par Feature 001 pour des données satellites
  autonomes liées à un ticket sans relation EF configurée (`EntreeHistorique`, `EntreeJournalAudit`,
  `PeriodeAttenteClient` — ce dernier explicitement commenté dans le code comme « Entité autonome
  (comme l'historique/l'audit), sans relation EF configurée vers `Ticket` »). Aucune modification du
  moteur de files/transitions n'est nécessaire ; le ticket ne « bouge » pas tant que la jointure n'est
  pas atteinte (voir D9).
- **Alternatives rejetées** :
  - Dupliquer le ticket en deux tickets liés (`LiaisonTicket`, SOC-10) — rejeté : `LiaisonTicket`
    modélise une continuité entre deux tickets distincts (souvent inter-module), pas deux instructions
    parallèles d'un même ticket ; utiliser ce mécanisme détournerait sa sémantique documentée.
  - Étendre `Ticket` avec deux jeux de champs « Manifeste » et « CD » directement dessus — rejeté :
    alourdirait `Ticket` pour un besoin propre à un seul module (IES), contraire au principe de
    non-duplication et à la simplicité du socle transverse.
  - Un second moteur de workflow parallèle générique — explicitement interdit par la demande.

## D9 — Synchronisation (jointure) IES

- **Décision** : la condition de jointure est **dérivée**, jamais un état persisté séparément :
  « jointure atteinte » ⟺ les deux lignes `SuiviBrancheIes` du ticket ont `EstTerminee = true`.
  Lorsque la seconde branche est marquée terminée, le cas d'usage applicatif déclenche alors le
  transfert normal du ticket (`Ticket.Transferer`, D1) vers l'étape de retour au client — jamais avant.
- **Rationale** : évite un double état (le composé et ses deux parties) qui pourrait diverger ; la
  dérivation est triviale (`COUNT(EstTerminee = false) = 0`) et testable indépendamment de l'ordre
  d'achèvement des deux branches (US8 scénario 3, testé dans les deux ordres A→B et B→A — D16).
- **Alternative rejetée** : un statut ticket dédié « Jointure IES en cours » — rejeté, ajouterait un
  état au socle transverse (`StatutTicket`) pour un besoin strictement local à un seul module.

## D10 — Confidentialité IES : permissions par branche, portée exacte différée (PC-05)

- **Décision** : deux nouveaux codes de permission distincts (D15) — un par branche
  (« instruire la branche Manifeste d'un ticket IES » / « instruire la branche Contrôle Documentaire
  d'un ticket IES ») — vérifiés côté serveur par `IAutorisationService.ExigerAsync` avant toute
  mutation de la branche correspondante. La **liste exacte** des champs/actions réservés à chaque
  branche (PC-05, classée « À CONFIRMER AVANT IMPLÉMENTATION » dans `spec.md`) reste un point ouvert
  — le mécanisme de séparation, lui, est entièrement spécifié : tant qu'un agent ne détient pas la
  permission de la branche opposée, aucune donnée ni action de cette branche ne lui est exposée.
- **Rationale** : répond à la consigne « supporter techniquement une politique de visibilité fine sans
  inventer aujourd'hui la matrice métier finale » — une permission par branche est un mécanisme
  générique, déjà éprouvé (même famille que toute autre permission du socle), qui n'exige aucune
  connaissance préalable de la liste exacte des champs.
- **Alternative rejetée** : un moteur RBAC champ par champ générique — explicitement écarté par la
  demande (« ne pas concevoir encore le RBAC champ par champ »).

## D11 — Documents paramétrables (réutilisation intégrale, PC-08 non bloquant)

- **Décision** : tous les documents du module (pièces à la soumission, facture, preuve de paiement,
  BS, BADT) réutilisent **sans extension** `Document`/`IDocumentRepository` de Feature 001 (SOC-07,
  SEC-07, SEC-12). Le type de document reste une donnée paramétrable, jamais une liste figée dans le
  code.
- **Rationale** : PC-08 reste ouvert (liste exacte des pièces) mais **ne bloque pas** cette décision —
  le mécanisme est déjà générique et ne présuppose aucune liste (même méthode que Feature 002,
  `quickstart.md` §3).

## D12 — IMP-15 : point d'extension pour la future Feature « Continuité Digital/Présentiel »

- **Décision** : un champ additif minimal sur `Ticket` — **`DeclarationPresentielle : bool`**
  (défaut `false`) plus la date de déclaration, portée par le même mécanisme d'historique/audit que
  toute autre action (aucun champ dédié à la date n'est strictement nécessaire : l'événement
  `EntreeHistorique` porte déjà un horodatage). Aucune autre donnée n'est modélisée : ni créneau, ni
  rendez-vous, ni agent de guichet.
- **Rationale** : conforme à `spec.md` FR-027/US9 — Feature 003 enregistre uniquement la déclaration
  et la rend disponible à la lecture pour la future Feature 009 (Continuité Digital/Présentiel), qui
  consommera ce champ sans que Feature 003 n'ait à connaître son mécanisme de RDV/guichet.
- **Alternative rejetée** : une entité « DemandeRdv » complète — explicitement hors périmètre
  (§0.1 de `spec.md`, confirmé §17 de cette demande).

## D13 — Rejet (IMP-04) et retours client (complément, preuve invalide) via les mécanismes `Ticket` existants

- **Décision (workflow, inchangée après revue)** :
  - **Rejeté (IMP-04)** → `Ticket.Annuler(maintenantUtc)` au niveau du **statut générique** du socle.
    **Jamais** `Ticket.Cloturer(...)` : `Ticket.Rouvrir()` interdit déjà explicitement la réouverture
    d'un ticket `Annule` (seul un ticket `Cloture` est réouvrable, CLAR-02 de Feature 001) — cohérent
    avec le fait qu'un rejet nécessite une **nouvelle** soumission, jamais une reprise de l'ancien
    ticket. Cette partie de la décision D13 originale est **confirmée** par la revue : au niveau du
    *workflow générique* (peut-on rouvrir ce ticket ? bloque-t-il l'unicité BL ?), Rejeté et Annulé
    se comportent strictement de la même façon, et il serait incorrect de les distinguer à ce niveau.
  - **Complément demandé (IMP-05)** → `Ticket.MettreEnAttenteClient()` puis, au dépôt des éléments,
    `Ticket.RependreApresReponseClient()` — mécanisme déjà démontré (TPS-02/04, SLA suspendu).
  - **Preuve de paiement invalide (CLAR-PAIEMENT-01)** → **même mécanisme**
    (`MettreEnAttenteClient()`/`RependreApresReponseClient()`).

## D13bis — Rejeté ≠ Annulé au niveau métier *(revu le 2026-08-30, corrige une confusion potentielle de D13)*

- **Constat de la revue** : la clarification validée `CLAR-BL-02` dit exactement *« un ticket Rejeté
  libère son BL, comme Annulé »* — elle porte **uniquement** sur l'effet du côté du contrôle
  d'unicité BL. Elle ne dit **jamais** que Rejeté et Annulé sont le **même événement métier**. Or la
  décision D13 originale, en écrivant « Rejeté = `Annule` » sans nuance, aurait pu laisser croire que
  toute distinction entre un rejet motivé par le Pilote Manifeste (décision d'un agent, avec motif
  obligatoire) et une annulation par le client (CYC-01, libre, sans motif) disparaît — ce qui
  **perdrait une information métier réelle** (utile à l'audit, au reporting « taux de rejet
  Manifeste », à l'affichage client — « Rejeté » et « Annulé » n'ont pas le même sens perçu — et à la
  compréhension future du parcours).
- **Séparation explicite retenue (§7 de la demande)** :

  | | État du workflow (`Ticket.Statut`, générique F001) | Bloque l'unicité BL ? | Résultat métier affiché/audité (Feature 003 uniquement) |
  |---|---|---|---|
  | Ticket rejeté par le Pilote Manifeste | `Annule` | Non | **Rejeté** (motif obligatoire) |
  | Ticket annulé par le client (CYC-01, hors périmètre Billing IMPORT) | `Annule` | Non (si c'était un ticket IMPORT) | **Annulé** |
  | Ticket clôturé | `Cloture` | Non | Clôturé |
  | Ticket en cours / en attente client | `EnCours` / `EnAttenteClient` | **Oui** | — |

  La colonne « Bloque l'unicité BL ? » est un **prédicat dérivé** de `Statut` (D3), identique pour
  Rejeté et Annulé — il n'a besoin d'**aucune** donnée supplémentaire. La colonne « Résultat métier »
  est une information **distincte**, propre à Feature 003.

- **Options comparées pour représenter Rejeté sans perdre l'information métier (§6 de la demande)** :

  | Option | Description | Impact Feature 001 | Retenue ? |
  |---|---|---|---|
  | A. Ajouter `Rejete` à `StatutTicket` | Nouvelle valeur d'enum dans le socle transverse | ❌ Modifierait un type partagé par **tous** les modules (Customer Complaint, Enrôlement…) pour un besoin propre à Billing IMPORT ; casserait potentiellement toute logique existante qui énumère `StatutTicket` de façon exhaustive (`switch` sans `default`, par exemple) | Rejetée — viole la consigne de ne pas modifier le comportement transverse de Feature 001 pour un besoin local à un seul module |
  | B. Uniquement du texte libre dans `EntreeHistorique.Detail` (décision D13 originale, non nuancée) | Le motif de rejet est tracé, mais **rien de structuré/typé** ne distingue Rejeté d'un Annulé générique pour des requêtes/rapports | Aucun | Rejetée seule — insuffisante pour du reporting fiable (« combien de BL rejetés ce mois-ci ? » nécessiterait une analyse de texte libre, fragile) |
  | **C. `Ticket.Statut = Annule` (inchangé) + un champ additif typé, propre à Feature 003** (`TypeAnnulationImport : RejetManifeste?`, nullable, non renseigné pour un `Annuler()` client ordinaire ou pour tout ticket d'un autre module) | Un seul champ nullable de plus sur `Ticket`, exactement le même style additif que `NumeroBl`/`DeclarationPresentielle` (D2, D12) | **Aucun** — `StatutTicket` reste à 5 valeurs, aucune logique existante de Feature 001 n'est affectée | **Retenue** |
  | D. Entité satellite séparée « Décision de rejet » (au lieu d'un champ sur `Ticket`) | Suivrait le style « `SuiviBrancheIes` » (D8) | Aucun, mais introduit une jointure supplémentaire pour une donnée simple (un type + son motif, déjà co-localisés avec le ticket) | Rejetée — sur-ingénierie pour une donnée aussi simple qu'un indicateur nullable ; le motif texte reste de toute façon dans l'historique |

- **Décision retenue : Option C.** `Ticket.Statut` reste `Annule` (aucune modification de
  `StatutTicket`, aucune régression possible sur Feature 001/Customer Complaint/Enrôlement) ; un
  **nouveau champ additif nullable** — `TypeAnnulationImport` (valeur unique possible à ce stade :
  `RejetManifeste`, extensible sans casser l'existant si un futur besoin similaire apparaît) — est
  positionné **uniquement** lors d'un rejet Pilote Manifeste sur un ticket Billing IMPORT ; il reste
  `null` pour toute annulation client ordinaire (CYC-01) et pour tout ticket des autres modules. Le
  **motif texte** (obligatoire, IMP-04) continue d'être tracé dans `EntreeHistorique.Detail` comme
  prévu par D13 originale — les deux mécanismes sont complémentaires, pas redondants (l'un est
  structuré/typé pour le reporting, l'autre est le texte libre lisible par un humain).
- **Conséquence directe pour D3/UNI-02/CLAR-BL-02 (confirmé, inchangé par la correction de grammaire
  du filtre)** : **aucun impact** sur le prédicat de l'index unique filtré —
  `TypeAnnulationImport` ne fait **pas** et ne fera **jamais** partie du filtre. Que le prédicat D3
  soit exprimé par inclusion des statuts bloquants (`Statut ∈ {EnAttenteDePriseEnCharge, EnCours,
  EnAttenteClient}`, forme retenue après correction) ou, hypothétiquement, par exclusion, Rejeté et
  Annulé sont couverts **identiquement** puisqu'ils partagent tous deux `Statut = Annule` — exactement
  ce que `CLAR-BL-02` demande. La distinction Rejeté/Annulé reste **purement informationnelle**
  (audit, reporting, affichage) et n'influence jamais la garantie d'unicité — confirmant que la
  séparation « état du workflow » / « bloque l'unicité BL ? » demandée en §7 est structurellement
  garantie par construction, quelle que soit la formulation exacte du prédicat.
- **Rationale d'ensemble** : préserve l'information métier réelle (audit, reporting, affichage client,
  compréhension du parcours) sans toucher au socle transverse de Feature 001, sans dupliquer l'état
  du workflow, et sans complexifier la garantie d'unicité — un seul champ nullable de plus, dans la
  continuité stricte du style déjà établi par D2/D12.

## D14 — Traçabilité de la validation de paiement (IMP-13)

- **Décision** : la preuve de paiement est un `Document` ordinaire (D11). Sa validation/refus par
  l'utilisateur habilité (PC-03) est un **événement**, tracé via `EntreeJournalAudit`/
  `EntreeHistorique` (auteur, décision, motif si refus) — **aucune nouvelle entité persistée**
  n'est nécessaire : l'état courant du paiement se déduit de la position du ticket (file Validation
  Paiement = en attente ; transfert vers Contrôle Documentaire = validé) et, en cas de refus, du
  passage par `MettreEnAttenteClient()` (D13). Si `/speckit.tasks` juge qu'une requête directe
  « statut de paiement actuel » nécessite une donnée dénormalisée pour des raisons de performance de
  lecture, ce sera une **projection** dérivée de ces événements, jamais une seconde source de vérité.
- **Rationale** : cohérent avec le style déjà retenu par Feature 001 pour « Transition de statut »
  (modélisée conceptuellement, réalisée via audit/historique, `data-model.md` F001 §1.2) — évite une
  entité dont le seul rôle serait de dupliquer une information déjà dérivable de la position du
  ticket et de son historique.

## D15 — Autorisations : nouveaux codes de permission

- **Décision** : extension **additive uniquement** de `Permission` (`Domain/Securite/NatureUtilisateur.cs`)
  — nouveaux codes au format déjà établi (`"module.action"`) :
  - `billing_import.soumettre`
  - `billing_import.manifeste.instruire`
  - `billing_import.facturation.editer`
  - `billing_import.paiement.valider` (PC-03 — habilitation générique, aucun rôle nommé en dur)
  - `billing_import.comptabilite.confirmer_lettrage`
  - `billing_import.controle_documentaire.traiter`
  - `billing_import.ies.manifeste.instruire` (D10)
  - `billing_import.ies.controle_documentaire.instruire` (D10)
- **Rationale** : même mécanisme exact que Feature 001 (`ticket.*`) et Feature 002 (`compte.*`) —
  aucune divergence de style, aucun rôle métier codé en dur (PC-03 respecté).
- **Aucune modification** de `IAutorisationService`, `AccesRefuseException`, ou du mécanisme de
  rattachement rôle/file/permission existant.

## D16 — Stratégie de tests

| Catégorie | Portée | Exemple |
|---|---|---|
| **TEST-CDC** | Portions REC-SCE-01 à 04 réellement exercées par Feature 003 (voir `contracts/use-cases.md` et la matrice de `spec.md` §10) | Rejeu intégral de REC-SCE-01 (portion IMP/UNI) : 3 BL → 1 validé/1 rejeté/1 complément → facturation → paiement → clôture |
| **TEST-SPEC** | Chaque Must sans couverture REC-SCE suffisante ; IMP-15 (Should, aucun REC-SCE applicable) | US9 (déclaration présentielle) ; comportement multi-BL indépendant (déjà démontré par REC-SCE-01 mais à re-tester unitairement) |
| **TEST-TECH — concurrence et normalisation BL SQL Server réel** | D3 : CAS1→CAS12 (deux sociétés, même société, BL actif/clôturé/annulé/rejeté, échec post-insertion, indépendance multi-BL, casse, trim, tirets/espaces internes, indépendance à la collation de l'instance) — jamais InMemory | Chaque cas produit exactement le résultat attendu de la table CAS1→CAS12 de D3 |
| **TEST-TECH — branches IES, ordre A→B et B→A** | D8/D9 : jointure indépendante de l'ordre d'achèvement | Deux scénarios distincts, mêmes assertions finales |
| **TEST-TECH — confidentialité IES** | D10 : refus serveur si permission de l'autre branche manquante | Tests d'accès négatifs, deux directions (Manifeste→CD et CD→Manifeste) |
| **TEST-TECH — snapshot TypeClient** | D5 : société changeant de type après création du dossier, avant aiguillage | Aiguillage utilise l'ancien type, jamais le nouveau |
| **TEST-TECH — indépendance multi-BL** | D4 : un échec d'unicité sur un BL ne bloque pas les autres | 5 BL, 1 doublon, 4 créés normalement |
| **TEST-TECH — autorisations serveur** | Chaque permission listée en D15, positif/négatif | Refus systématique sans la permission, quel que soit l'état de l'UI |
| **TEST-TECH — non-régression F001/F002** | Suite complète existante (180 tests + tests socle F001) réexécutée après toute extension additive | 0 échec attendu — aucune extension de `Ticket`/`Dossier` ne doit casser un appel existant |

**Utilisation de SQL Server réel** : obligatoire pour tous les invariants impossibles à démontrer
correctement en InMemory (concurrence BL — D3 ; toute assertion dépendant d'un index filtré réel).
