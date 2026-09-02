# Audit post-implémentation indépendant — Feature 003 : Billing Office IMPORT & Unicité BL

**Date de l'audit initial** : 2026-08-30
**Date de la remédiation ciblée** : 2026-08-30 (même jour, session suivante)

**Sources** : CDC (`docs/00-analyse-cdc.md`, `docs/01-matrice-exigences.md`, `docs/04-matrice-recette.md`),
`.specify/memory/constitution.md`, `spec.md`, `plan.md`, `research.md`, `data-model.md`,
`contracts/use-cases.md`, `quickstart.md`, inspection directe du code source
(`src/CIT.Plateforme.{Domain,Application,Infrastructure,Web}/**`), des migrations, d'une base SQL
Server réellement recréée depuis zéro (`dotnet ef database update`, jamais `EnsureCreated` pour cette
vérification), de requêtes SQL directes (`sqlcmd`), et de tests réellement exécutés dans cette
session (jamais une confiance dans `tasks.md` coché, le rapport final d'implémentation, ou les
commentaires de code).

> **Discipline appliquée (audit initial)** : chaque affirmation était adossée soit à une commande
> exécutée dans la session d'audit (avec sa sortie observée), soit à une lecture directe et citée du
> code source. Aucun écart constaté n'avait été corrigé à ce stade — seuls 4 fichiers de test avaient
> été ajoutés, exclusivement pour démontrer ou réfuter un comportement déjà spécifié.

## 0. Encart de remédiation ciblée (2026-08-30, seconde session)

Une remédiation **ciblée** a ensuite corrigé les écarts démontrés en §8.2/§8.3/§10.4/§18 (et un
écart supplémentaire trouvé pendant l'audit exhaustif des lectures demandé par la remédiation elle-
même, §3 ci-dessous). **Le corps du rapport ci-dessous (sections 1 à 23) est conservé tel qu'écrit
pendant l'audit initial, à titre de preuve d'origine** — seules les sections directement concernées
par une correction (§2 lignes IMP-22/IMP-42, §8, §10.4, §18, §20, §21, §23) portent désormais un
bloc **« MISE À JOUR REMÉDIATION »** immédiatement après le texte d'origine, plutôt que de réécrire
l'historique. Aucune assertion métier existante n'a été modifiée pour faire passer un test — les
tests d'audit qui démontraient un écart désormais corrigé ont été remplacés par des suites de
régression permanentes (détail §8 et §10.4).

**Résumé exécutif de la remédiation** — voir §24 pour le détail complet :
- 2/2 écarts de sécurité corrigés et re-testés (finalisation IES, lecture des 6 files).
- 1 écart supplémentaire trouvé et corrigé pendant l'audit exhaustif des lectures (§3) : un
  7ᵉ point d'accès direct (`ISuiviBrancheIesRepository` injecté dans `FileIes.razor`).
- PC-19 : documentation/UI corrigées (option B — aucune limite imposée arbitrairement).
- Dette technique « parallélisation Infrastructure.Tests » fermée — corrigée à la racine
  (données de référence idempotentes), pas seulement contournée par une sérialisation.
- **260/260 tests verts** (contre 246 à l'audit initial + 4 tests de preuve d'audit ⇒ 250 ; puis
  -1 test d'audit obsolète +12 tests de régression permanents ⇒ 260), suite complète réexécutée à
  plusieurs reprises dans cette session, y compris 5 exécutions consécutives d'`Infrastructure.Tests`
  pour prouver la fermeture de la course.
- Aucune migration créée (aucun changement de modèle nécessaire pour ces corrections) — confirmé par
  `dotnet ef migrations has-pending-model-changes`.
- **Recommandation : voir §25.**

---

## 1. Méthode et baseline réellement observé

### 1.1 Build et état Git

`dotnet build` (solution complète) → **succès, 0 avertissement, 0 erreur**, réexécuté dans cette
session.

`git status --short` : 8 fichiers du socle F001/F002 modifiés, ~20 nouveaux fichiers/répertoires non
suivis (Feature 003), état identique à celui annoncé par le rapport d'implémentation — vérifié, pas
supposé.

### 1.2 Migrations présentes

15 migrations au total (8 F001 + 4 F002 + **3 F003** : `ExtensionTicketBillingImport`,
`IndexUniqueFiltreNumeroBl`, `AjoutSuiviBrancheIes`). Détail de la vérification SQL directe en
section 9.

### 1.3 Suite de tests — baseline annoncé vs réellement observé

Baseline annoncé : 246 tests (37 Domain + 147 Application + 34 Infrastructure + 28 Web).

**Réexécution fraîche dans cette session (`dotnet test`, avant tout ajout d'audit)** :

| Projet | Annoncé | Observé | Écart |
|---|---|---|---|
| Domain.Tests | 37 | **37/37 verts** | Aucun |
| Application.Tests | 147 | **147/147 verts** | Aucun |
| Infrastructure.Tests | 34 | **34/34 verts** | Aucun |
| Web.Tests | 28 | **28/28 verts** | Aucun |
| **Total** | **246** | **246/246 verts** | **Le baseline annoncé est confirmé exact.** |

### 1.4 Tests de preuve ajoutés pendant cet audit (aucune correction, uniquement des preuves)

4 nouveaux fichiers, +5 tests, tous exécutés et rapportés section par section ci-dessous :

| Fichier | Section | But |
|---|---|---|
| `tests/CIT.Plateforme.Infrastructure.Tests/BillingImport/AtomiciteMultiBlSqlTests.cs` | §5 | Prouver D4/IMP-02 sous une exception **inattendue** en milieu de boucle |
| `tests/CIT.Plateforme.Infrastructure.Tests/BillingImport/ConcurrenceIesFinalisationSqlTests.cs` | §12 | Prouver l'absence de double clôture IES sous finalisation concurrente |
| `tests/CIT.Plateforme.Application.Tests/BillingImport/AuditAutorisationIesFinalisationTests.cs` | §8 | Démontrer l'écart d'autorisation sur la finalisation IES |
| `tests/CIT.Plateforme.Application.Tests/BillingImport/AuditDistinctionAnnulationRejetTests.cs` | §14 | Prouver, via les commandes réelles, la distinction Annulation/Rejet |

Avec ces ajouts, le total réel du dépôt est **250 tests** (246 + 4, un des 4 fichiers en portant 2).
Un état instable du test-runner en exécution parallèle complète a été découvert au passage — voir
§10.4 (dette technique, non corrigée).

---

## 2. Matrice des 24 exigences retenues

Chaque ligne cite un test réellement exécuté (pas un nom de tâche coché) et son résultat observé.

| Exigence | Test(s) démonstratif(s) réellement exécuté(s) | Résultat | Statut |
|---|---|---|---|
| IMP-01 | `SoumettreDemandeImportTests.Soumission_de_deux_BL_valides_cree_deux_tickets_independants_sous_un_meme_dossier` | Vert | **CONFORME** |
| IMP-02 | `Cinq_BL_dont_un_doublon_les_quatre_valides_sont_crees_independamment_du_doublon` ; `AtomiciteMultiBlSqlTests` (ajouté, §5) | Verts | **CONFORME** |
| IMP-03 | `SoumettreDemandeImportTests` (le résultat retourne les `Ticket.Numero` créés — portion « profil ») | Vert pour la portion IMP ; **aucun code d'envoi d'e-mail n'existe** dans Feature 003 (recherche `SmtpClient`/`IEmailSender` : 0 résultat dans `BillingImport/`) | **PARTIEL** — conforme à l'auto-classification de `spec.md` L794 (« Partielle, portion e-mail hors périmètre, dépend de Feature Notifications ») ; pas un écart caché |
| IMP-04 | `InstructionManifesteTests` (Valider/Rejeter/Complément), `RecSce01ImportNormalTests` | Verts | **CONFORME** |
| IMP-05 | `Demander_complement_met_le_ticket_en_attente_client`, `Deposer_complement_reprend_le_traitement` | Verts | **CONFORME** |
| IMP-06 | `AiguillageTypeClientTests.Changement_de_type_apres_creation_du_dossier_avant_aiguillage_n_affecte_pas_le_circuit` (voir détail §7) | Vert | **CONFORME** |
| IMP-10 | `CircuitClientNormalTests.Demander_facture_trace_le_signal_client_sans_changer_de_file` | Vert | **CONFORME** |
| IMP-11 | `CircuitClientNormalTests.Editer_facture_depose_le_document_et_transfere_vers_validation_paiement` | Vert | **CONFORME** |
| IMP-12 | `CircuitClientNormalTests.Deposer_preuve_de_paiement_puis_valider_transfere_vers_controle_documentaire` | Vert | **CONFORME** |
| IMP-13 | `CircuitClientNormalTests.Preuve_de_paiement_refusee_ramene_le_client_vers_un_nouveau_depot_sans_rejet_definitif` (voir §9) | Vert | **CONFORME** |
| IMP-14 | `CircuitClientNormalTests.Depot_bs_badt_et_cloture_termine_le_ticket` | Vert | **CONFORME** |
| IMP-15 | `DeclarationPresentielleTests` (3 tests, voir §13) | Verts | **CONFORME** |
| IMP-20 | `CircuitClientEnCompteTests.Editer_facture_pour_un_dossier_en_compte_transfere_directement_au_controle_documentaire` | Vert | **CONFORME** |
| IMP-21 | Idem — assertion explicite `Assert.NotEqual(NomsFiles.ValidationPaiement, ...)` + `RecSce02ImportEnCompteTests` (assertion sur la file immédiatement après édition de facture) | Verts | **CONFORME** |
| IMP-22 | `CircuitClientEnCompteTests.Ticket_en_compte_cloture_permet_au_client_de_recuperer_facture_et_pieces_de_controle` | Vert | **CONFORME** — *(la page `ConsulterTicketCloture.razor` n'était pas concernée par l'écart de lecture §8 — elle passait déjà par `ConsulterDocumentsTicketClotureQuery`, une requête autorisée. La confusion de la ligne d'origine ci-dessus provenait d'une généralisation excessive à toutes les pages ; corrigée ici après vérification page par page en remédiation, §8)* |
| IMP-30 | `CircuitClientDepositTests.Editer_facture_pour_un_dossier_deposit_transfere_vers_comptabilite_client` | Vert | **CONFORME** |
| IMP-31 | `CircuitClientDepositTests.Lettrage_confirme_transfere_vers_le_controle_documentaire` + `Lettrage_impossible_ou_a_reprendre_ne_declenche_aucune_action...` (voir §11) | Verts | **CONFORME** |
| IMP-32 | `CircuitClientDepositTests.Apres_lettrage_confirme_pilote_cd_depose_les_documents_et_cloture` | Vert | **CONFORME** |
| IMP-40 | `CircuitClientIesTests` (création des 2 branches, confidentialité bidirectionnelle) | Verts | **CONFORME** |
| IMP-41 | `CircuitClientIesTests.Manifeste_achevee_seule_ne_restitue_pas_le_ticket` + symétrique CD | Verts | **CONFORME** |
| IMP-42 | `CircuitClientIesTests` (jointure A→B et B→A) + `ConcurrenceIesFinalisationSqlTests` (mis à jour) + `ConstaterJointureIesAutorisationTests` (6 tests A→F, ajoutés en remédiation) | Verts | **CONFORME — écart d'autorisation §8.2 corrigé et re-testé (voir MISE À JOUR REMÉDIATION, §8)** |
| UNI-01 | `ControleUniciteBlTests` (2 tests), CAS1-3 (§4) | Verts | **CONFORME** |
| UNI-02 | `Bl_dont_l_unique_ticket_est_cloture_annule_ou_rejete_libere_une_nouvelle_soumission`, CAS4-6 (§4) | Verts | **CONFORME** |
| UNI-03 | `Bl_deja_reference_par_un_ticket_d_un_autre_module_n_entre_pas_dans_le_controle` (voir détail §6) | Vert | **CONFORME** |
| UNI-04 | Recherche exhaustive : **0 occurrence** de logique d'unicité cross-module ou EXPORT dans le code | — | **Correctement absente (hors périmètre respecté)** |

**23/23 Must couverts par au moins un test comportemental réellement exécuté. IMP-15 (Should) couvert. 1 exigence PARTIELLE (IMP-03), conforme à sa propre auto-classification dans spec.md — pas un écart non documenté.**

---

## 3. Matrice 23 Must → tests (vue consolidée)

| Must | Testé ? | Test comportemental (pas juste [X]) |
|---|---|---|
| IMP-01, 02, 04, 05, 06 | ✅ | Voir §2 |
| IMP-10 à 14 | ✅ | `CircuitClientNormalTests` (6 tests) |
| IMP-20, 21, 22 | ✅ | `CircuitClientEnCompteTests` (3 tests) |
| IMP-30, 31, 32 | ✅ | `CircuitClientDepositTests` (4 tests) |
| IMP-40, 41, 42 | ✅ | `CircuitClientIesTests` (7 tests) + 2 tests d'audit ajoutés |
| UNI-01, 02, 03 | ✅ | `ControleUniciteBlTests` (4 tests) + CAS1-12 (12 tests) |

**23/23 Must disposent réellement d'au moins un test comportemental exécuté dans cette session — confirmé, pas supposé.**

---

## 4. Unicité BL — audit critique

### 4.1 Mapping, collation, filtre (vérifié par SQL direct sur base recréée de zéro)

Base `CitPlateformeAuditFresh` créée et migrée intégralement via `dotnet ef database update` (jamais
`EnsureCreated`) depuis la première migration F001 jusqu'à la dernière F003. Requêtes `sqlcmd`
directes :

```
ColumnName|TypeName|max_length|is_nullable|collation_name
NumeroBl  |nvarchar|128       |1          |Latin1_General_CI_AS
Statut    |int     |4         |0          |NULL
```

```
IndexName          |is_unique|filter_definition
IX_Tickets_NumeroBl|1        |([NumeroBl] IS NOT NULL AND ([Statut] IN ((0), (1), (2))))
```

**Confirmé exactement** :
- `Statut` est stocké comme `int` (mapping par défaut EF Core, aucune conversion configurée) — les
  littéraux `(0, 1, 2)` du filtre portent donc bien sur les bonnes valeurs.
- `StatutTicket` : `EnAttenteDePriseEnCharge=0, EnCours=1, EnAttenteClient=2, Cloture=3, Annule=4`
  (ordre de déclaration de l'enum, vérifié dans `StatutTicket.cs`) — le filtre `IN (0,1,2)` couvre
  **exactement** les 3 statuts bloquants exigés, ni plus ni moins. `Cloture` et `Annule` **ne sont
  jamais mentionnés** dans le prédicat — l'exclusion est **structurelle par inclusion**, jamais une
  négation (`NOT IN`), conforme à la consigne explicite.
- Collation de `NumeroBl` : `Latin1_General_CI_AS`, explicite au niveau colonne, indépendante de la
  collation par défaut de l'instance/de la base (démontrée par CAS12, §4.3, sur une base recréée avec
  une collation d'instance délibérément `CS`).

### 4.2 Traduction `DbUpdateException → BlDejaActifException`

Lu directement dans `Persistence/Repositories.cs` (`UniteDeTravail.EnregistrerAsync`) : la traduction
est bornée par un `when` vérifiant **à la fois** le numéro d'erreur SQL Server (2601/2627) **et** la
présence littérale de `"IX_Tickets_NumeroBl"` dans le message — une violation de contrainte non liée
(ex. `IX_Tickets_Numero`) remonte **brute**, jamais mal classée. Prouvé empiriquement par
`AtomiciteMultiBlSqlTests` (§5) qui force précisément ce cas.

### 4.3 Rejet Manifeste et libération du BL

`Ticket.RejeterImport()` (lu dans `Ticket.cs`) positionne `Statut = Annule` **et**
`TypeAnnulationImport = RejetManifeste`. Le filtre SQL de l'index ne teste que `Statut` — un ticket
`Annule` (quelle que soit la cause) sort **immédiatement** du périmètre du filtre, donc libère le BL.
Démontré par CAS6 (`ConcurrenceUniciteBlSqlTests`, réel SQL Server, vert) et par
`ControleUniciteBlTests.Bl_dont_l_unique_ticket_est_cloture_annule_ou_rejete_libere_une_nouvelle_soumission`.

**Statut : CONFORME**, vérifié par lecture de code, par migration réellement appliquée, et par
requête SQL directe sur schéma réel — pas par confiance dans un commentaire.

---

## 5. CAS1→CAS12 — rejeu réel sur SQL Server/LocalDB

Tous exécutés dans `tests/CIT.Plateforme.Infrastructure.Tests/BillingImport/ConcurrenceUniciteBlSqlTests.cs`
contre `CitPlateformeTests` (LocalDB), **jamais InMemory** — base créée via `EnsureCreated()` à partir
du même modèle EF que les migrations (limite méthodologique documentée en §10.3, sans conséquence
fonctionnelle démontrée).

| CAS | Objectif | Résultat observé |
|---|---|---|
| CAS1 | 2 sociétés, même nouveau BL, simultané | 1 seul succès, l'autre `BlDejaActifException` — **vert** |
| CAS2 | Même société, 2 utilisateurs, même BL, simultané | 1 seul succès — **vert** |
| CAS3 | BL actif existant, nouvelle soumission | Refusée — **vert** |
| CAS4 | BL clôturé, nouvelle soumission | Autorisée (2 tickets actifs au sens historique, 1 seul actif au sens du filtre) — **vert** |
| CAS5 | BL annulé, nouvelle soumission | Autorisée — **vert** |
| CAS6 | BL rejeté Manifeste, nouvelle soumission | Autorisée, `TypeAnnulationImport` du 1er = `RejetManifeste` confirmé — **vert** |
| CAS7 | Échec transactionnel après insertion (rollback explicite) | Aucun verrou fantôme, nouvelle soumission possible — **vert** |
| **CAS8** | **Multi-BL : A accepté, B doublon, C accepté** | **Ajouté à `ConcurrenceUniciteBlSqlTests.cs` avant cet audit (Phase 12) — était absent au moment du premier speckit.implement**. Exécute `SoumettreDemandeImportCommand` réel : A et C committés indépendamment, B refusé, aucun rollback de A/C — **vert individuellement** (voir §10.4 pour la réserve sur l'exécution parallèle complète) |
| CAS9 | Casse différente (`abc123`/`ABC123`) | Traité comme doublon — **vert** |
| CAS10 | Espaces de bord trimés | Traité comme doublon après trim — **vert** |
| CAS11 | Tirets/espaces internes jamais supprimés | 3 valeurs distinctes acceptées — **vert** |
| CAS12 | Collation par défaut de l'instance délibérément `CS`, colonne reste `CI` | Même résultat que CAS9 (refusé) — **vert**, prouve l'indépendance vis-à-vis du défaut d'instance |

**12/12 CAS verts, individuellement vérifiés dans cette session, sur SQL Server réel.**

---

## 6. UNI-03 — portée du contrôle limitée à Billing IMPORT

Lu directement dans `Persistence/Repositories.cs` :

```csharp
public Task<Ticket?> ObtenirTicketActifParNumeroBlAsync(string numeroBl, CancellationToken ct) =>
    dbContext.Tickets.SingleOrDefaultAsync(
        t => t.NumeroBl == numeroBl && t.Module == "BillingImport" &&
             (t.Statut == StatutTicket.EnAttenteDePriseEnCharge || ... ), ct);
```

Le filtre `Module == "BillingImport"` est présent **au niveau applicatif** (la requête), en plus du
filtre SQL sur `Statut` (au niveau de l'index). Un ticket d'un autre module (ex. Customer Complaint)
portant le même `NumeroBl` ne bloque donc jamais une soumission IMPORT.

Testé par `ControleUniciteBlTests.Bl_deja_reference_par_un_ticket_d_un_autre_module_n_entre_pas_dans_le_controle` :
crée un ticket `"CustomerComplaint"` avec `NumeroBl = "GHI789"`, puis soumet le même BL en IMPORT →
**accepté**, aucun refus. **Vert.**

**Note** : ce filtre applicatif est **absent de l'index SQL Server lui-même** (l'index unique filtré
ne porte que sur `NumeroBl`/`Statut`, pas sur `Module`) — l'unicité SQL au niveau base est donc
**globale à `Tickets`**, pas Billing-IMPORT-only. En pratique cela ne cause aucun défaut observé
puisqu'aucun autre module de ce dépôt n'écrit jamais dans `Ticket.NumeroBl` (grep : seul
`SoumettreDemandeImportCommand`/`Ticket.Creer(..., numeroBl:)` le renseigne). C'est une dépendance
implicite entre modules qui **fonctionne aujourd'hui par absence de collision**, pas par une garantie
structurelle — à surveiller si un futur module venait à renseigner `NumeroBl` pour ses propres tickets.

**Statut : CONFORME au comportement exigé (démontré par test), avec une réserve d'architecture
documentée ci-dessus (dette technique, pas un écart fonctionnel actuel).**

---

## 7. Aiguillage Type Client (IMP-06) — snapshot exclusif

`ValiderTicketImportCommand` lit exclusivement `dossier.TypeClient` (jamais `SocieteCliente.Profil`
au moment de l'aiguillage) — confirmé par lecture directe du code.

Test `AiguillageTypeClientTests.Changement_de_type_apres_creation_du_dossier_avant_aiguillage_n_affecte_pas_le_circuit` :
société créée `TypeClient.Normal` → dossier créé (snapshot `Normal`) → société modifiée en `IES`
**avant** la validation Manifeste → ticket validé → **toujours aiguillé vers `Facturation`** (jamais
`File IES`). **Vert.** Démontre sans ambiguïté que le type courant de la société n'est jamais relu.

**Statut : CONFORME.**

---

## 8. Audit systématique des autorisations — TOUTES les commandes/requêtes + pages Blazor

### 8.1 Commandes/requêtes Application — présence d'un contrôle serveur

| Commande/requête | Contrôle observé |
|---|---|
| `SoumettreDemandeImportCommand` | `UtilisateurSociete.EstActif && DroitSoumission` (ENT-02) |
| `ValiderTicketImportCommand` | `ExigerAsync(..., BillingImportManifesteInstruire, ticket.FileCouranteId)` |
| `RejeterTicketImportCommand` | Idem |
| `GestionComplementManifesteCommand` | `ExigerAsync(BillingImportManifesteInstruire)` côté agent + `ExigerProprieteClientAsync` côté client |
| `DemanderFactureCommand` | `ExigerProprieteClientAsync` |
| `EditerFactureCommand` | `ExigerAsync(BillingImportFacturationEditer)` |
| `DeposerPreuvePaiementCommand` | Aucun appel direct, **mais délègue à `DeposerDocumentCommand.DeposerPourClientAsync`, qui appelle lui-même `ExigerProprieteClientAsync`** (vérifié par lecture) — **CONFORME par délégation** |
| `ValiderPreuvePaiementCommand` | `ExigerAsync(BillingImportPaiementValider)` |
| `DeposerDocumentsControleEtCloturerCommand` | `ExigerAsync(BillingImportControleDocumentaireTraiter)` |
| `ConfirmerLettrageCommand` | `ExigerAsync(BillingImportComptabiliteConfirmerLettrage)` |
| `InstruireBrancheIesCommand` | `ExigerAsync` avec la permission de branche correcte |
| `ConsulterDocumentsTicketClotureQuery` | `ExigerProprieteClientAsync` |
| `DeclarerFinalisationPresentielleCommand` | `ExigerProprieteClientAsync` + précondition de validation Manifeste |
| **`ConstaterJointureIesEtFinaliserCommand`** | **AUCUN — voir ÉCART CRITIQUE ci-dessous** |

### 8.2 ÉCART CRITIQUE — `ConstaterJointureIesEtFinaliserCommand` n'a aucun contrôle d'autorisation

Lecture directe du fichier : `ExecuterAsync(Guid? auteurId, Guid ticketId, ...)` — **aucun appel à
`IAutorisationService.ExigerAsync`**, aucune vérification que l'appelant est l'un des deux pilotes
ayant traité le ticket, `auteurId` est même **nullable** et accepté tel quel.

**Preuve empirique ajoutée** (`AuditAutorisationIesFinalisationTests`,
`Un_appelant_sans_aucune_habilitation_billing_import_peut_neanmoins_cloturer_le_ticket_ies`, **vert**) :
un utilisateur ne détenant **strictement aucune** habilitation `billing_import.*` — ni la permission
de branche, ni aucune autre — clôture avec succès le ticket dès que les deux branches sont achevées
par d'autres. Un second sous-cas avec `auteurId: null` (appel totalement anonyme) réussit également.

**Impact** : n'importe quel appelant authentifié (voire un appel système sans authentification,
puisque `auteurId` accepte `null`) connaissant un `ticketId` IES peut déclencher la clôture du ticket
dès que les deux branches sont marquées terminées par ailleurs — sans jamais prouver qu'il est
Pilote Manifeste, Pilote Contrôle Documentaire, ou même un utilisateur du tout.

**Classification : ÉCART (sécurité — contrôle d'accès manquant), pas corrigé conformément à la
consigne de cet audit.**

> ## MISE À JOUR REMÉDIATION (2026-08-30, seconde session)
>
> **Corrigé.** `ConstaterJointureIesEtFinaliserCommand.ExecuterAsync` prend désormais
> `Guid auteurId` (non-nullable — **plus aucun contournement `null`**) et exige, avant toute
> mutation, que l'appelant détienne **au moins une** des deux permissions de branche IES
> (`BillingImportIesManifesteInstruire` ou `BillingImportIesControleDocumentaireInstruire`), scopée
> à la file courante du ticket (`IAutorisationService.EstAutoriseAsync`, même service que toutes les
> autres commandes du module — aucun mécanisme nouveau introduit).
>
> **Décision documentée** : le contrat C3-18 cite « Acteur : système, déclenché à l'achèvement de la
> seconde branche », mais aucun mécanisme d'appel système distinct n'est spécifié ailleurs
> (`research.md`, `data-model.md`). Inventer une identité système dédiée aurait constitué une
> nouvelle règle métier non demandée par la remédiation (« ne pas inventer de nouvelles règles
> métier »). La correction reste donc strictement dans le mécanisme d'habilitation déjà établi par
> D10 : n'importe lequel des deux pilotes ayant légitimement pu achever la seconde branche peut
> constater la jointure et finaliser — ni plus, ni moins.
>
> **Tests** : `AuditAutorisationIesFinalisationTests.cs` (qui démontrait l'écart) a été **supprimé**
> et remplacé par `tests/CIT.Plateforme.Application.Tests/BillingImport/ConstaterJointureIesAutorisationTests.cs`,
> 6 tests couvrant exactement les cas A→F demandés par la remédiation :
>
> | Cas | Scénario | Résultat observé |
> |---|---|---|
> | A | Utilisateur habilité branche Manifeste, 2 branches achevées | Finalisation autorisée — **vert** |
> | B | Utilisateur sans aucune permission `billing_import.*` | `AccesRefuseException` — **vert** |
> | C | Utilisateur habilité IES mais sur une **autre file** | `AccesRefuseException` — **vert** |
> | D | `auteurId = Guid.Empty` (identité vide, aucun contournement anonyme) | `AccesRefuseException` — **vert** |
> | E | Une seule branche achevée, utilisateur pourtant autorisé | `JointureIesIncompleteException` (la précondition métier prime toujours sur l'autorisation) — **vert** |
> | F | 2 branches achevées, utilisateur habilité **uniquement** branche Contrôle Documentaire | Finalisation autorisée (symétrique de A) — **vert** |
>
> `ConcurrenceIesFinalisationSqlTests.cs` (test de concurrence existant, §10.5) a été **conservé** et
> mis à jour pour accorder aux deux agents simulés l'habilitation désormais requise — il continue de
> prouver l'absence de double clôture, indépendamment de l'autorisation (désormais également vérifiée
> ailleurs). Re-exécuté : **vert**.
>
> **Statut final : CONFORME.**

### 8.3 ÉCART — 6 pages Blazor Feature 003 lisent directement `PlateformeDbContext`, sans passer par un service d'autorisation, contrairement au précédent F001

Recherche `@inject` sur les 8 pages `.razor` de `Components/Pages/BillingImport/` : **6 pages**
injectent `PlateformeDbContext` directement et exécutent `DbContext.Tickets.Where(...)` pour lister
le contenu d'une file (`FilePiloteManifeste.razor`, `FileFacturation.razor`,
`FileControleDocumentaire.razor`, `FileComptabiliteClient.razor`, `FileIes.razor`,
`ValidationPaiement.razor`) — **aucune de ces 6 requêtes de listage n'appelle
`IAutorisationService.ExigerAsync`**. Seul le champ `agentId` est validé comme un GUID syntaxiquement
correct (`Guid.TryParse`), jamais vérifié comme habilité.

**Comparaison directe avec le précédent F001** : la page analogue `Tickets/MaFile.razor` injecte
`ConsulterFileQuery` (pas `PlateformeDbContext`), dont la lecture confirme
`await autorisation.ExigerAsync(agentId, Permission.TicketPrendreEnCharge, fileId, ct)` **avant**
tout retour de données. Feature 003 s'écarte de ce précédent déjà établi.

**Impact** : n'importe quel visiteur de ces 6 pages, en saisissant n'importe quel GUID syntaxiquement
valide dans le champ « agent », voit immédiatement la liste des tickets de la file correspondante
(numéros de ticket, numéros de BL, statuts) — sans détenir la moindre habilitation. Les **actions
mutantes** restent protégées (chaque commande sous-jacente vérifie sa propre permission, §8.1), seule
la **lecture/listage** est exposée.

**Classification : ÉCART (sécurité — contrôle d'accès manquant sur la lecture), pas corrigé.**

> ## MISE À JOUR REMÉDIATION (2026-08-30, seconde session)
>
> **Corrigé.** Création de
> [`ConsulterFileBillingImportQuery`](../../src/CIT.Plateforme.Application/BillingImport/ConsulterFileBillingImportQuery.cs)
> — une requête unique, réutilisable par les 6 pages, reproduisant exactement le schéma déjà établi
> par F001 (`ConsulterFileQuery`) : résout la file par nom, puis
> `IAutorisationService.ExigerAsync`/`EstAutoriseAsync` **avant** tout retour de tickets. Paramétrée
> par la ou les permissions acceptées (`IReadOnlyList<string>`) plutôt que codée en dur, pour couvrir
> aussi bien les files à permission unique que la File IES (double rôle, cf. §8.2). Les 6 pages
> injectent désormais cette requête au lieu de `PlateformeDbContext` :
>
> | Page | Permission(s) exigée(s) pour la lecture |
> |---|---|
> | `FilePiloteManifeste.razor` | `BillingImportManifesteInstruire` |
> | `FileFacturation.razor` | `BillingImportFacturationEditer` |
> | `FileControleDocumentaire.razor` | `BillingImportControleDocumentaireTraiter` |
> | `FileComptabiliteClient.razor` | `BillingImportComptabiliteConfirmerLettrage` |
> | `FileIes.razor` | `BillingImportIesManifesteInstruire` **ou** `BillingImportIesControleDocumentaireInstruire` |
> | `ValidationPaiement.razor` | `BillingImportPaiementValider` (voir ci-dessous pour le dépôt client) |
>
> **Décision documentée — `ValidationPaiement.razor`** : cette page mélangeait auparavant une action
> **client** (déposer une preuve) et une action **agent** (valider/refuser) dans la même table,
> alimentée par une seule liste. Gater cette liste par la permission agent
> (`BillingImportPaiementValider`) aurait bloqué le client, qui n'a — et ne doit pas avoir — cette
> permission. La page a donc été restructurée en deux sections indépendantes : une section « dépôt
> client » où le client saisit directement le `ticketId` qui lui a été communiqué (il ne parcourt
> jamais la file interne — cohérent avec « aucune page ne doit pouvoir charger une file simplement
> parce qu'elle connaît son identifiant », qui vaut aussi en sens inverse : le client n'a pas besoin
> de charger la file pour agir sur SON ticket), et une section « file agent » gatée par
> `ConsulterFileBillingImportQuery`. Aucune règle métier nouvelle : `DeposerPreuvePaiementCommand`
> vérifiait déjà (et vérifie toujours) la propriété du ticket via `CloisonnementService` — seul le
> chemin d'accès à l'action a changé, pas son autorisation.
>
> **Tests négatifs ajoutés** (niveau Application, comme demandé) —
> [`ConsulterFileBillingImportQueryTests.cs`](../../tests/CIT.Plateforme.Application.Tests/BillingImport/ConsulterFileBillingImportQueryTests.cs),
> 5 tests :
>
> | Test | Résultat observé |
> |---|---|
> | Agent habilité sur la file consulte son contenu | Succès (liste retournée) — **vert** |
> | Agent sans aucune habilitation | `AccesRefuseException` — **vert** |
> | Agent habilité sur une **autre** file Billing IMPORT | `AccesRefuseException` (pas de fuite inter-file) — **vert** |
> | Agent avec une permission Billing IMPORT réelle mais **incorrecte pour cette file** | `AccesRefuseException` — **vert** |
> | Liste correctement scopée (ticket d'une autre file jamais retourné) | Seul le ticket de la bonne file est présent — **vert** |
>
> **Statut final : CONFORME.**

### 8.4 Reste du périmètre — pas d'autre fuite IDOR trouvée

`SoumettreDemande.razor` et `ConsulterTicketCloture.razor` n'injectent pas `PlateformeDbContext` et
passent exclusivement par des commandes/requêtes contrôlées. Aucune autre commande de la liste §8.1
n'a d'appel manquant.

> ## MISE À JOUR REMÉDIATION (2026-08-30, seconde session) — §3 de la remédiation : audit exhaustif des lectures
>
> La remédiation demandait explicitement de rechercher, au-delà des deux écarts déjà connus : accès
> direct DbContext depuis Razor (traité §8.3 ci-dessus), **accès direct repository depuis Razor**,
> query sans autorisation, affichage d'un ticket par ID sans contrôle, téléchargement de document
> sans contrôle. Recherche menée sur les 8 pages `.razor` de `Components/Pages/BillingImport/` après
> correction de §8.2/§8.3 :
>
> - **1 point supplémentaire trouvé et corrigé** : `FileIes.razor` injectait
>   `ISuiviBrancheIesRepository` **directement** (un repository, pas une query autorisée) pour
>   afficher l'état des deux branches par ticket. Bien que les `ticketId` itérés provenaient déjà
>   d'une liste elle-même autorisée (§8.3), le motif lui-même (accès direct repository depuis Razor)
>   est exactement celui que la remédiation demandait de corriger. Créé
>   [`ConsulterSuivisBranchesIesQuery`](../../src/CIT.Plateforme.Application/BillingImport/ConsulterSuivisBranchesIesQuery.cs)
>   (même double-permission IES que §8.2/§8.3), et `FileIes.razor` l'injecte désormais à la place du
>   repository.
> - **Aucun autre accès direct repository trouvé** — grep `@inject I[A-Z][a-zA-Z]*Repository` sur les
>   8 pages : 0 résultat après correction.
> - **Aucune query sans autorisation trouvée** au-delà des deux écarts déjà traités.
> - **Aucun affichage de ticket par ID sans contrôle** : aucune page Feature 003 n'a de route
>   `{TicketId:guid}` (contrairement à `Tickets/DetailTicket.razor` de F001, hors périmètre de cette
>   remédiation) ; `ConsulterTicketCloture.razor` prend `ticketId` en **entrée de formulaire**, jamais
>   en route, et le fait vérifier par `ConsulterDocumentsTicketClotureQuery`
>   (`CloisonnementService.ExigerProprieteClientAsync`) avant tout retour.
> - **Aucun téléchargement de document sans contrôle** : aucune page Feature 003 n'expose de
>   téléchargement — les dépôts de documents Feature 003 réutilisent `DeposerDocumentCommand` de F001
>   (déjà autorisé, §4.1 rappelé) ; le téléchargement lui-même (`TelechargerDocumentQuery`) est une
>   page F001 (`Documents.razor`), hors périmètre de cette remédiation Feature 003.
>
> **Recherche documentée même sans autre trou découvert, conformément à la consigne §3 de la
> remédiation.**

---

## 9. Audit migrations SQL (base recréée depuis zéro)

Voir détail SQL en §4.1. Complément :

- **`__EFMigrationsHistory`** : 15 migrations listées dans l'ordre chronologique exact (F001 → F002 →
  F003), confirmé par requête `sqlcmd` directe sur `CitPlateformeAuditFresh`.
- **`dotnet ef migrations has-pending-model-changes`** → `No changes have been made to the model
  since the last migration.` — le modèle EF actuel est intégralement capturé par les migrations,
  aucune dérive.
- **Table `SuivisBranchesIes`** (requête directe) : `Id (uniqueidentifier, PK)`,
  `TicketId (uniqueidentifier, NOT NULL)`, `Branche (nvarchar(32), NOT NULL)`,
  `EstTerminee (bit, NOT NULL)`, `AuteurId (uniqueidentifier, NULL)`,
  `DateAchevementUtc (datetime2, NULL)` — conforme à research.md D8. Index non-unique sur
  `TicketId` confirmé (`IX_SuivisBranchesIes_TicketId`). **0 clé étrangère** — conforme à la
  conception « entité satellite sans relation EF » (même famille que `EntreeHistorique`).
- **Aucune migration destructive** : les 3 migrations F003 ne contiennent que `AddColumn`,
  `CreateIndex`, `CreateTable` — aucun `DropColumn`/`DropTable`/`RenameColumn` (lu directement dans
  les 3 fichiers de migration).
- **`TypeAnnulationImport`** : `nvarchar(32)`, collation `SQL_Latin1_General_CP1_CI_AS` (collation
  **par défaut de la base**, pas explicite) — sans conséquence puisque ce champ ne porte aucune
  contrainte d'unicité ni de comparaison métier sensible à la casse.

**Statut : CONFORME**, vérifié sans `EnsureCreated`, par migrations réelles + SQL direct.

---

## 10. Audit concurrence

### 10.1 CAS1/CAS2/CAS7 (unicité BL) — voir §5, tous verts sur SQL Server réel.

### 10.2 Atomicité multi-BL sous exception inattendue (D4/IMP-02) — test ajouté

Risque identifié par la consigne d'audit : une exception **non anticipée** (pas
`BlDejaActifException`) survenant au milieu de la boucle de `SoumettreDemandeImportCommand`
pourrait-elle annuler les BL déjà enregistrés avec succès ?

Lecture du code (`SoumettreDemandeImportCommand.cs`) : chaque BL appelle
`uniteDeTravail.EnregistrerAsync()` **individuellement dans la boucle** — aucune transaction
englobante, aucun `BeginTransactionAsync` autour de la boucle entière.

**Preuve empirique ajoutée** (`AtomiciteMultiBlSqlTests`, réel SQL Server) : numérotation truquée
provoquant une collision sur `IX_Tickets_Numero` (index F001 préexistant, **sans rapport** avec
`IX_Tickets_NumeroBl`) pour le 2ᵉ BL d'une soumission de 3 → `DbUpdateException` brute (pas
`BlDejaActifException`) propagée hors de `ExecuterAsync`, 3ᵉ BL jamais tenté. **Vérifié depuis un
`DbContext` totalement indépendant** (nouvelle connexion) : le 1er BL reste **committé et visible**,
le 3ᵉ est absent. **Vert.**

**Statut : CONFORME à D4/IMP-02** — aucune transaction globale « tout ou rien », confirmé
empiriquement sous un scénario de panne non anticipé, pas seulement le cas `BlDejaActifException`
déjà couvert par CAS8.

### 10.3 Limite méthodologique — CAS1-12 utilisent `EnsureCreated()`, pas les migrations réelles

`BaseDeTestSql.CreerContexte()` (support de tous les tests `Infrastructure.Tests`, y compris
CAS1-12) utilise `Database.EnsureDeleted()` + `Database.EnsureCreated()` — **jamais**
`dotnet ef database update`. `EnsureCreated()` construit le schéma directement depuis le modèle EF
courant (`OnModelCreating`), **en contournant les fichiers de migration**.

Cela signifie que CAS1-12, à eux seuls, **prouvent la configuration du modèle EF, pas que les
fichiers de migration la reproduisent fidèlement**. Ce risque a été fermé **indépendamment** dans
cet audit par la vérification §4.1/§9 (migrations réelles appliquées + inspection SQL directe,
résultat identique). **Aucune divergence trouvée entre les deux chemins** — mais la suite
`ConcurrenceUniciteBlSqlTests` elle-même ne le garantit pas structurellement.

**Classification : dette technique méthodologique (risque de test, pas un défaut fonctionnel
constaté) — non corrigée.**

### 10.4 Instabilité de suite découverte pendant cet audit — `Infrastructure.Tests` sans `DisableTestParallelization`

En ajoutant `AtomiciteMultiBlSqlTests.cs` (qui insère, comme `ConcurrenceUniciteBlSqlTests.CAS8`,
une ligne `FileDeTraitement` « Pilote Manifeste » sans vérifier son existence préalable), une
exécution complète de `dotnet test tests/CIT.Plateforme.Infrastructure.Tests` a produit :

```
CAS8_Soumission_multi_bl_...  [FAIL]
System.InvalidOperationException : Sequence contains more than one matching element
  at SoumettreDemandeImportCommand.ExecuterAsync(...) line 64
```

**Cause confirmée** : `BaseDeTestSql` pointe vers **une seule base LocalDB physique partagée**
(`CitPlateformeTests`) quelle que soit la classe de test. xUnit exécute les classes de test en
parallèle par défaut. Deux classes différentes (`ConcurrenceUniciteBlSqlTests` et
`AtomiciteMultiBlSqlTests`) insèrent chacune leur propre ligne « Pilote Manifeste » sans contrôle
d'existence préalable ni contrainte unique en base sur `FileDeTraitement(Module, Nom)` → doublon →
`SingleOrDefault` lève.

**Reproductibilité vérifiée** : échoue systématiquement en exécution complète parallèle ; **CAS8 et
le nouveau test passent chacun à 100 % en isolation** (`--filter`), confirmant qu'il ne s'agit pas
d'un défaut du code produit mais d'une course entre données de test partagées.

**Précédent exact déjà connu** : ce même défaut de classe avait été trouvé et corrigé dans
`Web.Tests` pendant l'implémentation (`tests/CIT.Plateforme.Web.Tests/AssemblyInfo.cs`,
`[assembly: CollectionBehavior(DisableTestParallelization = true)]`) — **cette correction n'a
jamais été étendue à `Infrastructure.Tests`**, qui reste donc exposé au même risque dès qu'une
2ᵉ classe de test SQL y insère la même donnée de référence.

**Classification : dette technique (fragilité de suite de tests) — non corrigée dans cet audit,
conformément à la consigne. Le baseline §1.3 (246/246, avant ajout des tests d'audit) n'était pas
affecté car une seule classe de ce type existait à ce moment.**

> ## MISE À JOUR REMÉDIATION (2026-08-30, seconde session)
>
> **Corrigé — à la racine, pas seulement contourné.** Une première tentative de correction par
> `[assembly: CollectionBehavior(DisableTestParallelization = true)]` (même remède que `Web.Tests`)
> a été **essayée puis rejetée** : 3 exécutions complètes consécutives d'`Infrastructure.Tests` avec
> la sérialisation activée échouaient **toujours** sur le même test. Analyse : contrairement à
> `Web.Tests` (où le seed est protégé par un `if (!Any(...))` — un vrai *check-then-act*, donc une
> vraie course sensible à l'ordonnancement), `ConcurrenceUniciteBlSqlTests.CAS8` et
> `AtomiciteMultiBlSqlTests` inséraient chacun leur ligne **sans aucune vérification d'existence** —
> deux insertions inconditionnelles produisent un doublon **qu'elles s'exécutent en parallèle ou en
> séquence**. Ce n'était donc pas fondamentalement une course, mais une donnée de référence non
> idempotente. `DisableTestParallelization` aurait masqué le symptôme sans le résoudre.
>
> **Cause racine réellement corrigée** : `BaseDeTestSql.CreerContexte()` sème désormais lui-même la
> file « Pilote Manifeste » **une seule fois**, à l'intérieur du même verrou double-checked qui
> protège déjà `EnsureDeleted()`/`EnsureCreated()` — donnée de référence idempotente et unique pour
> tout l'assembly (stratégie « données de référence idempotentes/uniques », troisième option listée
> par la remédiation). Les deux insertions redondantes dans `CAS8` et `AtomiciteMultiBlSqlTests` ont
> été retirées.
>
> **Preuve empirique** : `dotnet test tests/CIT.Plateforme.Infrastructure.Tests` exécuté **5 fois
> consécutives** après correction → **36/36 verts à chaque fois**, aucune exception. Avant
> correction (avec la tentative `DisableTestParallelization` déjà en place) : 3/3 exécutions
> échouaient systématiquement sur le même test — la comparaison directe confirme que seule la
> correction de la donnée de référence, pas la sérialisation, résout le problème.
>
> **Note explicite demandée par la remédiation — pourquoi cette correction ne concerne que les
> tests** : `BaseDeTestSql` est un type exclusivement situé sous `tests/CIT.Plateforme.Infrastructure.Tests/Support/`,
> jamais référencé par aucun projet `src/`. La ligne « Pilote Manifeste » qu'il sème désormais une
> fois est une donnée de **paramétrage de démonstration** (au même titre que l'item 3 de
> `quickstart.md` §3, « Rattacher les rôles... aux files »), pas un comportement du produit — en
> production, cette file est créée une fois par un administrateur (§3 de `quickstart.md`), jamais
> insérée à répétition par du code applicatif. Aucune ligne de `src/CIT.Plateforme.Application/BillingImport/SoumettreDemandeImportCommand.cs`
> n'a été modifiée : c'est d'ailleurs cette commande, inchangée, dont le `SingleOrDefault` a servi de
> révélateur fidèle à la duplication de données de test — un comportement produit correct (« il ne
> doit exister qu'une file Pilote Manifeste ») a détecté une donnée de test incorrecte, ce qui est le
> signe que le comportement produit n'avait pas besoin d'être touché.
>
> **Statut final : dette fermée.**

### 10.5 Finalisation IES concurrente (D9) — cas non couvert avant cet audit, test ajouté

Risque identifié par la consigne : deux branches IES achevées presque simultanément, deux appels
concurrents à `ConstaterJointureIesEtFinaliserCommand` pour le même ticket.

**Preuve empirique ajoutée** (`ConcurrenceIesFinalisationSqlTests`, réel SQL Server) : deux
`DbContext` indépendants chargent le même ticket (même `VersionConcurrence` de départ, jeton
optimiste F001 déjà présent sur `Ticket`, jamais modifié par Feature 003) ; le premier appel réussit
et clôture ; le second lève **`ConflitDeConcurrenceException`** (traduction déjà existante de
`DbUpdateConcurrencyException` dans `UniteDeTravail`, réutilisée sans modification). Vérifié depuis
un 3ᵉ `DbContext` indépendant : **une seule** entrée d'historique `TicketIesCloture`, statut final
`Cloture` une seule fois. **Vert.**

**Statut : CONFORME** — la double clôture est structurellement impossible, protégée par un mécanisme
F001 générique, **mais cette protection est un effet de bord du jeton de concurrence, pas une
garantie intentionnelle de la commande elle-même** (qui, rappel §8.2, n'a par ailleurs aucun contrôle
d'autorisation). Les deux constats sont indépendants : l'un porte sur l'intégrité des données
(CONFORME), l'autre sur le contrôle d'accès (ÉCART).

---

## 11. Circuits Client Normal / En Compte / Deposit — rejeu et vérification des assertions

Chaque test cité ci-dessous a été **relu intégralement** (pas seulement exécuté) pour confirmer que
ses assertions correspondent au texte contractuel, pas seulement à un nom de test plausible.

- **Client Normal** (`CircuitClientNormalTests`, 6 tests + `RecSce01ImportNormalTests`) : chaîne
  complète facturation → preuve → validation → BS/BADT → clôture. Preuve invalide testée
  explicitement : `RefuserAsync(agentId, ticketId, motif)` → `StatutTicket.EnAttenteClient` (jamais
  `Annule`), suivi d'un second dépôt réussi (`preuve2.pdf`) — confirmé par assertions directes sur le
  statut et sur `Documents.Count`, pas seulement l'absence d'exception.
- **En Compte** (`CircuitClientEnCompteTests`, 3 tests + `RecSce02ImportEnCompteTests`) : assertion
  explicite `Assert.NotEqual(NomsFiles.ValidationPaiement, file.Nom)` juste après édition de la
  facture — prouve l'absence de l'étape paiement par construction, pas par omission de test.
- **Deposit** (`CircuitClientDepositTests`, 4 tests + `RecSce03ImportDepositTests`) : **aucun appel
  HTTP/SDK externe** — recherche `HttpClient`/`RestClient`/`SoapClient` dans tout `BillingImport/` :
  0 résultat (hors commentaires documentant explicitement l'absence). Cas « lettrage impossible »
  testé par `Lettrage_impossible_ou_a_reprendre_ne_declenche_aucune_action_le_ticket_reste_en_comptabilite_client` :
  capture le statut et la file **avant**, n'exécute **aucune commande**, relit **après** → identiques.
  Ce test prouve littéralement qu'aucune commande n'existe pour ce cas (« aucune action » au sens le
  plus strict), conforme au contrat C3-15.

**Statut : CONFORME** pour les 3 circuits.

---

## 12. IES — audit approfondi (voir aussi §5, §8.2, §10.5)

- **Création exacte des deux branches** : `ValiderTicketImportCommand`, branche `TypeClient.IES`,
  crée exactement 2 `SuiviBrancheIes` (`Manifeste`, `ControleDocumentaire`), `EstTerminee=false` —
  confirmé par `CircuitClientIesTests.Ticket_ies_aiguille_cree_deux_suivis_de_branche_non_acheves`.
- **Indépendance** : `InstruireBrancheIesCommand` ne touche que la ligne de SA branche — confirmé par
  lecture (`suivis.SingleOrDefault(s => s.Branche == branche)`).
- **Permissions par branche + confidentialité** : `Permission.BillingImportIesManifesteInstruire` vs
  `...ControleDocumentaireInstruire`, refus croisé testé dans les 2 sens
  (`Agent_ne_detenant_que_la_permission_manifeste_ne_peut_pas_instruire_la_branche_controle_documentaire`
  et son symétrique).
- **Jointure dans les 2 ordres** : `Ordre_manifeste_puis_controle_documentaire_atteint_la_jointure` et
  `Ordre_controle_documentaire_puis_manifeste_atteint_la_jointure_meme_resultat` — verts, résultat
  final identique.
- **Impossibilité de finaliser à une seule branche** : `JointureIesIncompleteException` levée dans
  les 2 cas (Manifeste seule / CD seule) — confirmé.
- **Finalisation concurrente presque simultanée** : non couverte avant cet audit — **test ajouté et
  vert, voir §10.5**.
- **Double finalisation/clôture** : impossible par construction du jeton de concurrence optimiste —
  confirmé §10.5.
- **Autorisation de la commande de finalisation elle-même** : **ÉCART CRITIQUE, voir §8.2** —
  n'importe qui peut l'appeler.

---

## 13. IMP-15 — déclaration présentielle

`DeclarerFinalisationPresentielleCommand` :
1. **Uniquement après validation Manifeste** — vérifié via `ticket.FileCouranteId ==
   filePiloteManifeste.Id` (un ticket rejeté ou en attente de complément n'a jamais quitté cette
   file non plus, donc également bloqué — testé par
   `Declaration_tentee_avant_validation_manifeste_est_refusee`, `InvalidOperationException`).
2. **Limitée au propriétaire** — `CloisonnementService.ExigerProprieteClientAsync`, testé par
   `Un_tiers_non_proprietaire_ne_peut_pas_declarer` (`AccesRefuseException`).
3. **Simple déclaration/trace** — `Ticket.DeclarerFinalisationPresentielle()` ne fait que
   `DeclarationPresentielle = true` ; recherche `rendez-vous|rdv|guichet|creneau` dans
   `BillingImport/` : 0 résultat hors commentaires documentant explicitement l'absence.

**Statut : CONFORME.**

---

## 14. Rejet Manifeste vs Annulation client — distinction indépendamment vérifiée

**Test ajouté** (`AuditDistinctionAnnulationRejetTests`, InMemory — comportement logique, pas
concurrentiel) : exécute **les commandes réelles** (`AnnulerTicketCommand.AnnulerLibrementAsync`,
F001, réutilisée sans modification, vs `RejeterTicketImportCommand`, Feature 003) sur deux tickets
Billing IMPORT distincts du même dossier.

Résultat observé :
- Annulation client : `Statut = Annule`, **`TypeAnnulationImport = null`**.
- Rejet Manifeste : `Statut = Annule`, **`TypeAnnulationImport = RejetManifeste`**.
- Les deux BL sont **également** libérés pour une nouvelle soumission (`reprise.TicketsCrees.Count
  == 2`, `BlRefuses` vide) — la libération du BL ne dépend donc que de `Statut`, jamais de
  `TypeAnnulationImport`, cohérent avec le filtre SQL (§4.1) qui ne teste que `Statut`.

**Statut : CONFORME**, démontré par les commandes réelles (pas seulement par lecture de
`Ticket.cs`).

---

## 15. Audit migrations — voir §9 (fusionné pour éviter la duplication)

## 16. REC-SCE-01→04 — rejeu réel et vérification des assertions (pas seulement le nom du test)

Les 4 fichiers `tests/CIT.Plateforme.Web.Tests/BillingImport/RecSce0{1,2,3,4}Import*Tests.cs` ont été
relus intégralement (pas seulement réexécutés) :

| REC-SCE | Texte contractuel (`docs/04-matrice-recette.md`) | Assertions réellement présentes |
|---|---|---|
| 01 | 3 BL → 1 validé/1 rejeté/1 complément puis validation → circuit Normal complet | `finValide.Statut == Cloture` + `MotifCloture == Normale` ; `finRejete.Statut == Annule` + `TypeAnnulationImport == RejetManifeste` ; `finComplement.Statut == EnAttenteDePriseEnCharge` (ni Annule ni Cloture) ; les 3 partagent le même `DossierId` |
| 02 | Facture éditée → Contrôle Documentaire direct → documents sans paiement | `fileApresFacture.Nom == ControleDocumentaire` **immédiatement** après édition (aucune étape intermédiaire tentée) ; 2 documents récupérés (facture+BADT) |
| 03 | Facture → lettrage Comptabilité → Contrôle Documentaire → clôture | 3 assertions de file séquentielles (`ComptabiliteClient` puis `ControleDocumentaire`) + `Statut == Cloture` final |
| 04 | Parallèle Manifeste/CD, champs réservés, jointure, remise documents, clôture | 2 `AccesRefuseException` croisées + `JointureIesIncompleteException` à une seule branche + `Cloture` final après les deux |

**Tous rejoués dans cette session (`dotnet test`), tous verts. Les assertions correspondent
littéralement au texte contractuel — pas de simple vérification de non-exception.**

---

## 17. Régression F001/F002 — modifications du socle

`git diff --stat` sur les fichiers listés par la consigne :

| Fichier | Résultat |
|---|---|
| `StatutTicket.cs` | **0 ligne modifiée** |
| `CloisonnementService.cs` | **0 ligne modifiée** |
| `IAutorisationService.cs` | **0 ligne modifiée** |
| `IJournalisationService.cs` | **0 ligne modifiée** |
| `Ticket.cs` | 55 insertions, **1 seule modification** : signature de `Creer(...)` avec un paramètre `numeroBl` optionnel additif (défaut `null`) — tous les appels F001/F002 existants restent compilables et sémantiquement inchangés (confirmé par le build complet et les 37+149 tests Domain/Application, dont ceux de F001/F002 hérités) |
| `Repositories.cs`, `NatureUtilisateur.cs`, `DependencyInjection.cs`, `PlateformeDbContext.cs`, `Persistence/Repositories.cs`, `DetailTicket.razor` | **141 insertions, 0 suppression** cumulées sur ces 6 fichiers |

**Statut : CONFORME** — extension strictement additive du socle, démontrée par diff réel, pas par
déclaration.

---

## 18. Points PC toujours ouverts — aucune décision arbitraire masquée

- **PC-03** (rôle validant le paiement) : code neutre, `billing_import.paiement.valider` — grep
  `Caisse|Finance` dans `BillingImport/` : 0 résultat hors commentaires niant explicitement leur
  usage.
- **PC-05** (champs IES exacts) : `InstruireBrancheIesCommand` ne connaît **aucun champ métier**,
  seulement `BrancheIes`/`EstTerminee` — mécanisme générique confirmé, aucune liste de champs
  inventée.
- **PC-08** (pièces exactes) : `ReglesDepotDocument` reste paramétrable partout, aucune liste de
  pièces figée dans le code (recherche de constantes de type `.pdf`/`.jpg` : uniquement des exemples
  de « démonstration » dans les pages Blazor, jamais imposés côté Application).
- **PC-19** (limite de BL par soumission) : **divergence documentaire trouvée**. `quickstart.md` §3
  affirme « valeur par défaut retenue » (5) ; `SoumettreDemande.razor` affiche un label « jusqu'à 5
  BL » ; **mais `SoumettreDemandeImportCommand` n'impose strictement aucune limite** (recherche de
  toute borne sur `NumerosBl.Count` : 0 résultat). Une soumission de 100 BL serait acceptée sans
  erreur. **Classée dette technique/divergence documentaire — le code ne fait pas ce que
  `quickstart.md` affirme**, mais aucune exigence Must du CDC ne dépend de l'application réelle de
  cette limite dans le périmètre retenu par `spec.md`.

> ## MISE À JOUR REMÉDIATION (2026-08-30, seconde session)
>
> **Décision — option B retenue** (documentation/UI corrigées, aucune limite imposée). Relecture de
> `spec.md` (source normative) : FR-001 qualifie déjà « jusqu'à 5 par défaut » de **« valeur
> paramétrable, PC-19 ouvert sur son caractère figé ou non »**, et la ligne CL-08 de la table des
> points ouverts classe PC-19 **« À CONFIRMER AVANT PRODUCTION »**, avec la précision explicite :
> « la spécification retient déjà l'hypothèse *valeur par défaut paramétrable* (A-004) ; seule la
> valeur numérique définitive doit être confirmée ». `spec.md` n'a donc **jamais** validé « 1 à 5 BL »
> comme règle métier figée — l'option A (validation serveur d'une limite) aurait été une invention de
> règle métier non demandée, explicitement proscrite par la remédiation.
>
> **Corrections appliquées** (documentation/UI uniquement, `spec.md` non modifié — CDC non touché) :
> - `quickstart.md` §3, item 5 : « valeur par défaut retenue » (qui laissait croire qu'une limite
>   était appliquée) remplacé par « aucune contrainte serveur en l'état », avec renvoi explicite vers
>   ce rapport.
> - `SoumettreDemande.razor` : le texte d'en-tête et le label du champ ne présentent plus « jusqu'à 5
>   BL » comme un fait — le texte indique désormais explicitement que PC-19 est un point ouvert non
>   tranché et qu'aucune limite serveur n'est appliquée.
> - **Aucun changement de comportement** : `SoumettreDemandeImportCommand` continue de n'imposer
>   aucune limite — c'est la documentation qui a été alignée sur le code, jamais l'inverse, conforme
>   à « ne pas transformer une hypothèse en contrainte métier ».
>
> **Statut final : PC-19 reste ouvert (inchangé, c'est le point normatif) ; l'écart
> documentation/code est fermé.**

---

## 19. Périmètre négatif — confirmation par recherche de code

Recherche exhaustive (`grep -rniE`) dans `src/CIT.Plateforme.{Application,Domain,Web}/BillingImport*`
et fichiers IES dédiés : **0 occurrence** de `EXPORT`, `Proforma`, `Avoir`, `Exoneration`,
`CustomerComplaint`, `Enrôlement`/`Enrolement`, `Notification`, « IA »/Intelligence Artificielle,
`IRIS` (hors commentaires niant explicitement toute intégration), `HttpClient`/`RestClient`/
`SoapClient`. UNI-04 : aucune tâche, aucune ligne de code ne l'implémente (confirmé également au
niveau `tasks.md`, 3 occurrences textuelles, toutes des rappels d'exclusion).

**Statut : CONFORME — aucune absorption de périmètre hors mandat détectée.**

---

## 20. Écarts découverts — synthèse

| # | Écart | Sévérité | Section | **Statut post-remédiation** |
|---|---|---|---|---|
| 1 | `ConstaterJointureIesEtFinaliserCommand` ne vérifiait **aucune** autorisation — n'importe quel appelant (y compris anonyme) pouvait clôturer un ticket IES dont les deux branches étaient achevées | **Élevée** (contrôle d'accès manquant sur une mutation) | §8.2 | **CORRIGÉ** — habilitation de branche IES exigée, `auteurId` non-nullable, 6 tests A→F verts |
| 2 | 6 pages Blazor Feature 003 listaient le contenu d'une file en lisant `PlateformeDbContext` directement, sans passer par un service d'autorisation — contrairement au précédent F001 (`ConsulterFileQuery`) | **Moyenne** (divulgation d'information interne, pas de données client sensibles au-delà des numéros de BL/ticket) | §8.3 | **CORRIGÉ** — `ConsulterFileBillingImportQuery` autorisée, 5 tests négatifs verts |
| 3 | Divergence documentaire : `quickstart.md`/l'UI affirmaient une limite de 5 BL par soumission ; le code n'en applique aucune | **Faible** (documentation, pas de Must CDC dépendant) | §18 | **CORRIGÉ (documentation)** — option B, PC-19 reste ouvert, documentation/UI alignées sur le code |
| 4 *(nouveau, trouvé en remédiation §3)* | `FileIes.razor` injectait `ISuiviBrancheIesRepository` directement (accès repository depuis Razor) | **Faible** (les `ticketId` itérés provenaient déjà d'une liste autorisée) | §8.4 | **CORRIGÉ** — `ConsulterSuivisBranchesIesQuery` autorisée |

**4/4 écarts fermés.** Aucune assertion métier existante n'a été modifiée pour faire passer un test
(voir détail des tests remplacés/ajoutés dans les blocs « MISE À JOUR REMÉDIATION » de chaque
section).

---

## 21. Dette technique — synthèse

| # | Dette | Section | **Statut post-remédiation** |
|---|---|---|---|
| A | CAS1-12 (`ConcurrenceUniciteBlSqlTests`) utilisent `EnsureCreated()`, pas les migrations réelles — risque méthodologique fermé indépendamment dans cet audit (§4.1/§9), mais la suite elle-même ne le garantit pas structurellement | §10.3 | **Non touchée** — hors périmètre de la remédiation (aucun écart démontré n'en dépendait ; les migrations réelles ont déjà été vérifiées indépendamment en §4.1/§9) |
| B | `Infrastructure.Tests` — deux classes de test SQL inséraient chacune, sans contrôle d'existence, la même ligne de référence « Pilote Manifeste » | §10.4 | **FERMÉE** — `BaseDeTestSql` sème désormais cette donnée une seule fois (idempotent), les insertions redondantes retirées ; 5/5 exécutions complètes consécutives vertes |
| C | Absence de contrainte unique SQL sur `FileDeTraitement(Module, Nom)` — cause racine de la dette B, héritée de Feature 001 | §10.4 | **Non touchée** — la dette B est fermée en amont (donnée idempotente), ce qui rend cette absence de contrainte inoffensive pour la suite de tests ; correction du schéma F001 hors périmètre d'une remédiation Feature 003 |
| D | Portée applicative de UNI-03 (`Module == "BillingImport"`) non répliquée au niveau de l'index SQL Server lui-même — fonctionne aujourd'hui par absence de collision inter-module, pas par garantie structurelle | §6 | **Non touchée** — hors périmètre (aucun écart démontré, comportement déjà CONFORME) |

**1/4 dette technique fermée (B, celle explicitement visée par la remédiation §5). A, C, D
restent des observations d'architecture sans écart démontré, hors périmètre d'une remédiation
« corriger uniquement les écarts démontrés ».**

---

## 22. Points PC toujours ouverts — récapitulatif final

PC-03, PC-05, PC-08 : ouverts, mécanismes génériques opérationnels, aucune valeur métier inventée
(confirmé §18) — **inchangé par la remédiation, conformément à la consigne de ne pas les résoudre
arbitrairement**. PC-19 : **toujours ouvert** (c'est le point normatif, correctement documenté comme
tel) — l'écart entre la documentation/l'UI et le code, lui, est fermé (§18, mise à jour).

---

## 23. Score final (état à l'audit initial — conservé tel quel, voir §24 pour l'état courant)

- **24/24 exigences tracées**, dont **23/23 Must démontrés par un test comportemental réellement
  exécuté** dans cette session, 1 Should (IMP-15) démontré, 1 exigence PARTIELLE (IMP-03, conforme à
  sa propre auto-classification dans `spec.md`), UNI-04 correctement absente.
- **19/19 contrats C3-01→C3-19** mappés à une classe implémentée et à au moins un test.
- **12/12 CAS de concurrence/normalisation** verts sur SQL Server réel (avec la réserve
  méthodologique §10.3, indépendamment fermée).
- **4/4 REC-SCE (portion Feature 003)** rejoués, assertions vérifiées littéralement contre le texte
  contractuel.
- **Migrations** : intégralement vérifiées par SQL direct sur base recréée de zéro, aucune migration
  destructive, aucun changement de modèle non capturé.
- **Régression F001/F002** : 0 comportement modifié, extension strictement additive (git diff réel).
- **Périmètre négatif** : respecté, aucune absorption détectée.
- **3 écarts réels découverts** (2 de sécurité — contrôle d'accès manquant sur la finalisation IES et
  sur la lecture de 6 files ; 1 documentaire — PC-19), **non corrigés à ce stade**, documentés avec
  preuve reproductible.
- **4 dettes techniques** documentées (méthodologie de test, fragilité de parallélisation, absence de
  contrainte SQL, portée applicative non répliquée en base).

**Conclusion de l'audit initial** : la fonctionnalité métier de Feature 003 était **globalement
conforme** aux exigences CDC retenues, à `spec.md`, et aux contrats C3-01→C3-19, avec une preuve
empirique réelle pour chaque affirmation de cette section. **Deux écarts de contrôle d'accès réels et
exploitables avaient été trouvés et démontrés** (finalisation IES sans autorisation ; lecture des
files sans autorisation) — ils n'existaient dans aucun rapport antérieur et n'étaient visibles
qu'après lecture directe du code et test empirique, exactement le type de constat que cet audit
indépendant avait pour mandat de produire. **Aucune correction n'avait été appliquée à ce stade —
voir §24 pour la remédiation qui a suivi.**

---

## 24. Remédiation ciblée — validation finale (2026-08-30, seconde session)

### 24.1 Corrections appliquées

| # | Correction | Fichier(s) |
|---|---|---|
| 1 | `ConstaterJointureIesEtFinaliserCommand` exige désormais une habilitation de branche IES ; `auteurId` non-nullable | [`ConstaterJointureIesEtFinaliserCommand.cs`](../../src/CIT.Plateforme.Application/BillingImport/ConstaterJointureIesEtFinaliserCommand.cs) |
| 2 | Nouvelle requête autorisée pour la lecture du contenu d'une file (remplace la lecture directe de `PlateformeDbContext`) | [`ConsulterFileBillingImportQuery.cs`](../../src/CIT.Plateforme.Application/BillingImport/ConsulterFileBillingImportQuery.cs) |
| 3 | Nouvelle requête autorisée pour la lecture de l'état des branches IES (remplace l'accès direct au repository) | [`ConsulterSuivisBranchesIesQuery.cs`](../../src/CIT.Plateforme.Application/BillingImport/ConsulterSuivisBranchesIesQuery.cs) |
| 4 | Donnée de référence de test idempotente (ferme la dette de parallélisation à la racine) | [`BaseDeTestSql.cs`](../../tests/CIT.Plateforme.Infrastructure.Tests/Support/BaseDeTestSql.cs) |
| 5 | Documentation/UI PC-19 alignées sur le comportement réel du code (option B) | [`quickstart.md`](../quickstart.md), [`SoumettreDemande.razor`](../../src/CIT.Plateforme.Web/Components/Pages/BillingImport/SoumettreDemande.razor) |

**Aucune migration créée** — confirmé par `dotnet ef migrations has-pending-model-changes` (« No
changes have been made to the model since the last migration »), aucune de ces corrections
n'introduisant de nouveau champ ou table.

### 24.2 Pages Blazor modifiées

`FilePiloteManifeste.razor`, `FileFacturation.razor`, `FileControleDocumentaire.razor`,
`FileComptabiliteClient.razor`, `FileIes.razor` (injection directe de `PlateformeDbContext` et, pour
cette dernière, d'`ISuiviBrancheIesRepository`, remplacées par les requêtes autorisées) ;
`ValidationPaiement.razor` (restructurée en deux sections indépendantes — dépôt client par `ticketId`
direct, file agent gatée par permission — voir décision documentée §8.3) ; `SoumettreDemande.razor`
(texte PC-19 corrigé, aucun changement de comportement).

### 24.3 Queries/use cases ajoutés

`ConsulterFileBillingImportQuery` (contenu d'une file, une ou plusieurs permissions acceptées),
`ConsulterSuivisBranchesIesQuery` (état des deux branches IES pour un ticket, même double-permission
que la finalisation).

### 24.4 Tests ajoutés/modifiés

| Fichier | Nature | Compte |
|---|---|---|
| `ConstaterJointureIesAutorisationTests.cs` | Nouveau (remplace `AuditAutorisationIesFinalisationTests.cs`, supprimé) | 6 tests (A→F) |
| `ConsulterFileBillingImportQueryTests.cs` | Nouveau | 5 tests |
| `ConcurrenceIesFinalisationSqlTests.cs` | Modifié (habilitations ajoutées aux agents simulés, constructeur mis à jour) | 1 test (inchangé en nombre) |
| `CircuitClientIesTests.cs` | Modifié (constructeur `ConstaterJointureIesEtFinaliserCommand` mis à jour) | 7 tests (inchangé en nombre) |
| `ConcurrenceUniciteBlSqlTests.cs` (CAS8) | Modifié (seed redondant retiré) | 12 tests CAS1-12 (inchangé en nombre) |
| `AtomiciteMultiBlSqlTests.cs` | Modifié (seed redondant retiré) | 1 test (inchangé en nombre) |
| `AuditAutorisationIesFinalisationTests.cs` | **Supprimé** (démontrait un écart désormais fermé) | -1 test |

**Solde net** : 250 (baseline post-audit-initial) - 1 (fichier supprimé) + 6 + 5 = **260 tests**.

### 24.5 Résultat autorisation

`ConstaterJointureIesAutorisationTests` : **6/6 verts** (A→F, voir détail §8.2). `ConsulterFileBillingImportQueryTests` : **5/5 verts** (voir détail §8.3). Aucune régression sur les tests d'autorisation
préexistants (`DepotDocumentAutorisationTests`, tests d'autorisation F001/F002).

### 24.6 Résultat CAS1→CAS12

**12/12 verts**, réexécutés après correction (`dotnet test tests/CIT.Plateforme.Infrastructure.Tests --filter "FullyQualifiedName~ConcurrenceUniciteBlSqlTests"` → `réussite : 12, total : 12`).
**5 exécutions complètes consécutives** de tout `Infrastructure.Tests` (36 tests, pas seulement
CAS1-12) → **36/36 à chaque fois**, confirmant la fermeture de la dette §10.4/§21.

### 24.7 Résultat REC-SCE-01→04

**4/4 verts**, réexécutés après correction (`dotnet test tests/CIT.Plateforme.Web.Tests --filter "FullyQualifiedName~BillingImport.RecSce0"` → `réussite : 4, total : 4`). Aucune assertion modifiée
depuis §16 — ces tests n'étaient pas concernés par les écarts corrigés.

### 24.8 Résultat tests F001/F002

Aucun fichier de test F001/F002 modifié. Confirmé sans régression via la suite complète (§24.9) : le
nombre de tests F001/F002 hérité (37 Domain + une partie d'Application/Infrastructure/Web) reste
strictement inclus, tous verts.

### 24.9 Résultat total tests

| Projet | Avant remédiation (post-audit initial) | Après remédiation |
|---|---|---|
| Domain.Tests | 37 | **37** (inchangé) |
| Application.Tests | 149 | **159** (+6 A→F, +5 query, -1 fichier supprimé) |
| Infrastructure.Tests | 36 | **36** (inchangé en nombre — corrigé en fiabilité, voir §10.4) |
| Web.Tests | 28 | **28** (inchangé) |
| **Total** | **250** | **260** |

Suite complète (`dotnet test`, sans filtre) exécutée après toutes les corrections :
**260/260 verts, 0 échec.**

### 24.10 Statut PC-19

**Reste ouvert** (c'est le point normatif attendu — `spec.md` CL-08 : « À CONFIRMER AVANT
PRODUCTION »). L'écart documentaire/comportemental qui l'entourait (quickstart.md/UI affirmant une
limite non appliquée par le code) est **fermé** — voir §18, mise à jour.

### 24.11 Dette technique restante

A (méthodologie CAS1-12 via `EnsureCreated`), C (absence de contrainte unique SQL sur
`FileDeTraitement`), D (portée UNI-03 non répliquée dans l'index) — **non touchées**, hors périmètre
d'une remédiation « corriger uniquement les écarts démontrés » (aucune des trois ne correspondait à
un écart démontré par l'audit initial, seulement à des observations d'architecture). B (fragilité de
parallélisation `Infrastructure.Tests`) — **fermée** (§10.4, §21).

### 24.12 Nouveau score des 24 exigences

Inchangé en substance par rapport à §2/§3 (23/23 Must, 1 Should, 1 Partielle documentée, UNI-04
absente) — **les seules lignes dont le libellé a changé sont IMP-22 et IMP-42** (retrait des mentions
d'écart désormais corrigé, voir §2 mis à jour). **24/24 exigences tracées, 0 exigence dégradée par la
remédiation.**

---

## 25. Recommandation

**READY FOR CLOSURE REVIEW.**

Justification : les 2 écarts de sécurité démontrés par l'audit initial sont corrigés et re-testés
(6 + 5 tests dédiés, tous verts) ; l'audit exhaustif des lectures demandé par la remédiation elle-même
a trouvé et fermé un 3ᵉ point (accès repository direct) ; PC-19 est correctement documenté comme
ouvert plutôt que faussement présenté comme une règle appliquée ; la dette de parallélisation des
tests visée par la remédiation est fermée à la racine (donnée idempotente), pas seulement contournée ;
aucune régression sur CAS1-12, REC-SCE-01→04, ou les 250 tests préexistants ; aucune migration
inutile créée ; aucune décision arbitraire prise sur PC-03/PC-05/PC-08 ; le CDC n'a pas été modifié ;
Feature 004 n'a pas été commencée. Les 3 dettes techniques non touchées (A, C, D) sont des
observations d'architecture sans écart démontré — elles ne bloquent pas une revue de clôture, mais
devraient y être mentionnées comme points de vigilance pour une décision humaine (notamment C :
l'absence de contrainte unique SQL sur `FileDeTraitement(Module, Nom)`, un correctif simple à faible
risque si une revue de clôture souhaite le traiter).

**Cette remédiation ne referme pas Feature 003 elle-même** — conformément à la consigne, cette
décision reste à la validation humaine dans le cadre d'une revue de clôture dédiée (à l'image de
`specs/001-socle-tickets-workflows/closure-review.md` et
`specs/002-comptes-clients-societes/closure-review.md`).
