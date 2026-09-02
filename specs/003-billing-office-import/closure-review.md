# Revue de fermeture — Feature 003 : Billing Office IMPORT & Unicité BL

**Date** : 2026-08-30
**Sources** : `.specify/memory/constitution.md`, `spec.md`, `plan.md`, `research.md`, `data-model.md`,
`contracts/use-cases.md`, `quickstart.md`, `tasks.md`, `post-implementation-audit.md`,
`docs/01-matrice-exigences.md`, `docs/04-matrice-recette.md`,
`specs/001-socle-tickets-workflows/closure-review.md`, `specs/002-comptes-clients-societes/closure-review.md`,
inspection directe du code source, des migrations, d'une base SQL Server réelle
(`CitPlateformeAuditFresh`, recréée depuis zéro via `dotnet ef database update` — jamais
`EnsureCreated` pour cette vérification), et de l'état Git.

> Cette revue reconstitue chaque score et chaque affirmation **indépendamment** de
> `post-implementation-audit.md` — ce dernier est utilisé comme référence à vérifier, jamais comme
> preuve suffisante en lui-même, à l'image de la méthode déjà appliquée par les revues de fermeture
> de Feature 001 et Feature 002. Aucune tâche cochée dans `tasks.md`, aucun commentaire de code,
> aucun score annoncé n'est accepté sans re-vérification directe (build, tests réexécutés, requêtes
> SQL directes, lecture de code). Ce n'est pas une nouvelle implémentation : aucune correction de
> code n'a été appliquée, seules des incohérences documentaires strictement inoffensives auraient été
> corrigées si trouvées (aucune ne l'a été).

---

## 1. Résumé exécutif

Feature 003 a traversé : `/speckit.clarify` → `/speckit.plan` (D1-D16) → `/speckit.tasks` (112
tâches) → `/speckit.implement` → un audit post-implémentation indépendant ayant démontré 2 écarts de
sécurité réels (finalisation IES sans autorisation ; lecture non autorisée de 6 files) et 1 écart
documentaire (PC-19) → une remédiation ciblée ayant fermé les 3 écarts (dont un 4ᵉ point trouvé
pendant l'audit exhaustif des lectures exigé par la remédiation elle-même) → cette revue de fermeture
finale.

**Reconstitution indépendante confirmée** :
- **24/24 exigences retenues tracées**, dont **23/23 Must CONFORME** (0 PARTIEL, 0 ÉCART, 0 NON
  DÉMONTRÉ) et 1 Should (IMP-15) CONFORME ; 1 exigence PARTIELLE assumée (IMP-03, portion e-mail hors
  périmètre, conforme à l'auto-classification de `spec.md`) ; UNI-04 correctement absente.
- **19/19 contrats C3-01→C3-19** mappés à une classe implémentée et testée.
- **12/12 CAS d'unicité/concurrence/normalisation** verts sur SQL Server réel (LocalDB), réexécutés
  dans cette session.
- **4/4 REC-SCE (portion Feature 003)** rejoués, assertions vérifiées littéralement contre le texte
  contractuel.
- **260/260 tests verts** (37 Domain + 159 Application + 36 Infrastructure/SQL Server réel + 28 Web),
  réexécutés à neuf dans cette session, 0 échec, 0 ignoré.
- **15 migrations** appliquées depuis zéro sur une base recréée dans cette session, rejouables,
  vérifiées par requête SQL directe (jamais `EnsureCreated`) ; **0 pending model change**.
- **2 écarts de sécurité et 1 écart documentaire, tous fermés et re-testés** (voir §13) ; **1 point
  supplémentaire trouvé et fermé pendant la remédiation** (accès repository direct dans
  `FileIes.razor`).
- **3 dettes techniques restantes, toutes classées ACCEPTÉE** (§18) — aucune ne remplit un critère de
  blocage de fermeture.
- **PC-03, PC-05, PC-08, PC-19 tous encore ouverts**, aucune valeur métier inventée pour les
  résoudre.
- **État Git propre** : 9 fichiers Feature 001/002 modifiés, tous additifs sauf une seule
  modification rétrocompatible (`Ticket.Creer`, paramètre optionnel) ; aucun secret ; aucun artefact
  `bin`/`obj` suivi ; aucun fichier Feature 004.

**Décision finale : `CLOSE`** (détail section 22).

---

## 2. Score final — reconstitution indépendante des 24 exigences

Reconstitué ligne par ligne à partir de `docs/01-matrice-exigences.md`, du code source
(`src/CIT.Plateforme.{Domain,Application,Infrastructure,Web}/**/BillingImport*`, extensions de
`Ticket.cs`), et d'une réexécution fraîche de la suite de tests dans cette session
(`dotnet test` → 260/260, 0 échec). Chaque cellule ci-dessous a été revérifiée indépendamment de
`post-implementation-audit.md` (lecture de code + test réellement exécuté), pas recopiée depuis lui.

| Exigence | Priorité (`docs/01-matrice-exigences.md`) | Mécanisme implémenté (vérifié) | Test(s) réel(s) exécuté(s) | Statut final |
|---|---|---|---|---|
| IMP-01 | Must | `SoumettreDemandeImportCommand` — formulaire multi-BL, un ticket par BL | `SoumettreDemandeImportTests` | **CONFORME** |
| IMP-02 | Must | Chaque BL traité indépendamment, dossier commun ; aucune transaction englobante (vérifié §5) | `Cinq_BL_dont_un_doublon...`, `AtomiciteMultiBlSqlTests` | **CONFORME** |
| IMP-03 | Must | Portion IMP (numéros retournés au profil) implémentée ; portion e-mail hors périmètre (Feature Notifications, non construite) | `SoumettreDemandeImportTests` | **PARTIEL — assumé**, conforme à l'auto-classification de `spec.md` L794 |
| IMP-04 | Must | `ValiderTicketImportCommand`/`RejeterTicketImportCommand`/`GestionComplementManifesteCommand` | `InstructionManifesteTests`, `RecSce01ImportNormalTests` | **CONFORME** |
| IMP-05 | Must | Complément demandé/déposé, retour file Pilote Manifeste | `Demander_complement_met_le_ticket_en_attente_client`, `Deposer_complement_reprend_le_traitement` | **CONFORME** |
| IMP-06 | Must | Aiguillage exclusif sur `Dossier.TypeClient` (snapshot) — voir §8 | `AiguillageTypeClientTests.Changement_de_type_apres_creation_du_dossier_avant_aiguillage_n_affecte_pas_le_circuit` | **CONFORME** |
| IMP-10 | Must | `DemanderFactureCommand` | `CircuitClientNormalTests.Demander_facture_trace_le_signal_client_sans_changer_de_file` | **CONFORME** |
| IMP-11 | Must | `EditerFactureCommand` | `CircuitClientNormalTests.Editer_facture_depose_le_document_et_transfere_vers_validation_paiement` | **CONFORME** |
| IMP-12 | Must | `DeposerPreuvePaiementCommand` | `CircuitClientNormalTests.Deposer_preuve_de_paiement_puis_valider...` | **CONFORME** |
| IMP-13 | Must | `ValiderPreuvePaiementCommand` (validation + refus non définitif, CLAR-PAIEMENT-01) | `Preuve_de_paiement_refusee_ramene_le_client_vers_un_nouveau_depot_sans_rejet_definitif` | **CONFORME** |
| IMP-14 | Must | `DeposerDocumentsControleEtCloturerCommand` | `CircuitClientNormalTests.Depot_bs_badt_et_cloture_termine_le_ticket` | **CONFORME** |
| IMP-15 | **Should** | `DeclarerFinalisationPresentielleCommand` — déclaration/trace uniquement (voir §14) | `DeclarationPresentielleTests` (3 tests) | **CONFORME** |
| IMP-20 | Must | `EditerFactureCommand` (branche En Compte) | `CircuitClientEnCompteTests.Editer_facture_pour_un_dossier_en_compte...` | **CONFORME** |
| IMP-21 | Must | Transfert direct Contrôle Documentaire, jamais Validation Paiement | `Assert.NotEqual(NomsFiles.ValidationPaiement, ...)` + `RecSce02ImportEnCompteTests` | **CONFORME** |
| IMP-22 | Must | `ConsulterDocumentsTicketClotureQuery` (cloisonnement client) | `Ticket_en_compte_cloture_permet_au_client_de_recuperer_facture_et_pieces_de_controle`, `Un_tiers_non_proprietaire_ne_peut_pas_consulter_les_documents` | **CONFORME** |
| IMP-30 | Must | `EditerFactureCommand` (branche Deposit) | `CircuitClientDepositTests.Editer_facture_pour_un_dossier_deposit...` | **CONFORME** |
| IMP-31 | Must | `ConfirmerLettrageCommand` — confirmation uniquement, aucune intégration IRIS (voir §10) | `Lettrage_confirme_transfere_vers_le_controle_documentaire`, `Lettrage_impossible_ou_a_reprendre_ne_declenche_aucune_action...` | **CONFORME** |
| IMP-32 | Must | `DeposerDocumentsControleEtCloturerCommand` | `Apres_lettrage_confirme_pilote_cd_depose_les_documents_et_cloture` | **CONFORME** |
| IMP-40 | Must | `InstruireBrancheIesCommand` — 2 branches, permissions distinctes | `CircuitClientIesTests` (création branches, confidentialité croisée) | **CONFORME** |
| IMP-41 | Must | Jointure dérivée, jamais avant achèvement des deux branches | `Manifeste_achevee_seule_ne_restitue_pas_le_ticket` + symétrique | **CONFORME** |
| IMP-42 | Must | `ConstaterJointureIesEtFinaliserCommand` — finalisation autorisée (branche IES) + concurrence sûre | `ConstaterJointureIesAutorisationTests` (6, A→F), `ConcurrenceIesFinalisationSqlTests` | **CONFORME** |
| UNI-01 | Must | Index unique filtré SQL Server, portée globale | CAS1-3, `ControleUniciteBlTests` | **CONFORME** |
| UNI-02 | Must | Filtre par inclusion des 3 statuts bloquants — Cloturé/Annulé/Rejeté libèrent | CAS4-6 | **CONFORME** |
| UNI-03 | Must | Filtre applicatif `Module == "BillingImport"` | `Bl_deja_reference_par_un_ticket_d_un_autre_module_n_entre_pas_dans_le_controle` | **CONFORME** |
| UNI-04 | Should | Hors périmètre (Feature EXPORT) — non implémentée | Recherche exhaustive : 0 occurrence | **Correctement absente** |

**Total reconstitué : 24/24 exigences retenues tracées — 23/23 Must CONFORME, 1 Should (IMP-15)
CONFORME, 1 PARTIEL assumé (IMP-03), UNI-04 correctement hors périmètre.** Identique au score
annoncé par `post-implementation-audit.md` §2, mais chaque ligne a été revérifiée indépendamment ici
(recherche de code fraîche + réexécution des tests cités).

---

## 3. Must — reconstitution indépendante

**23 Must retenus** (lu directement dans `docs/01-matrice-exigences.md` et `spec.md` §0.3) :
IMP-01→06, IMP-10→14, IMP-20→22, IMP-30→32, IMP-40→42, UNI-01→03. **1 Should** : IMP-15. **1 Should
hors périmètre** : UNI-04.

| Critère | Résultat |
|---|---|
| 23/23 Must implémentés | ✅ (§2) |
| 23/23 Must testés par un test comportemental réellement exécuté | ✅ (§2, `dotnet test` → 260/260) |
| 0 Must PARTIEL | ✅ |
| 0 Must ÉCART | ✅ |
| 0 Must NON DÉMONTRÉ | ✅ |

Vérification volontairement **au-delà de la seule présence de code** : chaque ligne du tableau §2 cite
un test réellement exécuté dans cette session, pas une référence de code lue sans exécution.

**Statut : CONFORME.**

---

## 4. Unicité BL — confirmation définitive

Vérifié par requête SQL directe sur `CitPlateformeAuditFresh` (base recréée depuis zéro dans cette
session, migrations réelles appliquées) :

| Propriété exigée | Vérification | Résultat |
|---|---|---|
| Portée globale | Index unique sur `Tickets.NumeroBl` seul (pas de segmentation par société) | ✅ confirmé (`sys.indexes`) |
| Filtre par **inclusion** (jamais `NOT IN`) | `filter_definition` = `([NumeroBl] IS NOT NULL AND ([Statut] IN ((0), (1), (2))))` | ✅ confirmé, inclusion explicite |
| Collation déterministe | `NumeroBl` : `Latin1_General_CI_AS`, colonne explicite (pas héritée du défaut d'instance) | ✅ confirmé (`sys.columns`), re-démontré indépendant du défaut d'instance par CAS12 |
| Trim externe uniquement | `SoumettreDemandeImportCommand` : `numeroBlBrut.Trim()`, jamais de suppression interne | ✅ confirmé par lecture + CAS10/CAS11 |
| Casse insensible | Collation `CI` | ✅ confirmé par CAS9/CAS12 |
| Tirets/espaces internes conservés | Jamais supprimés (aucune normalisation au-delà du trim) | ✅ confirmé par CAS11 (3 valeurs distinctes acceptées) |
| Rejeté libère le BL | `RejeterImport()` positionne `Statut = Annule` (statut 4, hors filtre) | ✅ confirmé, CAS6 |
| Annulé libère le BL | `Annuler()` (F001) positionne également `Statut = Annule` | ✅ confirmé, CAS5 |
| Clôturé libère le BL | `Cloturer()` positionne `Statut = Cloture` (statut 3, hors filtre) | ✅ confirmé, CAS4 |

**CAS1→CAS12 réexécutés dans cette session** (`dotnet test tests/CIT.Plateforme.Infrastructure.Tests --filter "FullyQualifiedName~ConcurrenceUniciteBlSqlTests"`) → **12/12 verts**, sur SQL Server réel
(LocalDB), jamais InMemory.

**Statut : CONFORME, définitivement confirmé.**

---

## 5. Multi-BL — indépendance transactionnelle

`SoumettreDemandeImportCommand` : chaque BL de la boucle appelle `IUniteDeTravail.EnregistrerAsync()`
**individuellement** — aucune transaction englobant la boucle entière (confirmé par lecture directe,
aucun `BeginTransactionAsync` autour du `foreach`).

**Scénario A accepté / B doublon / C accepté** : démontré par `SoumettreDemandeImportTests.Cinq_BL_dont_un_doublon_les_quatre_valides_sont_crees_independamment_du_doublon` (InMemory) **et** par
`ConcurrenceUniciteBlSqlTests.CAS8` (SQL Server réel) — A et C committés indépendamment, B refusé, ni
rollback ni interférence.

**Cas supplémentaire vérifié en remédiation** (`AtomiciteMultiBlSqlTests`) : même garantie sous une
**exception inattendue** (pas seulement `BlDejaActifException`) survenant en milieu de boucle — le
1er BL déjà committé reste committé, vérifié depuis un `DbContext` totalement indépendant.

**Cohérence avec IMP-01/IMP-02/UNI-03/REC-SCE-01** : `RecSce01ImportNormalTests` rejoue exactement ce
principe à l'échelle du contrat (3 BL, décisions indépendantes, aucune décision sur un BL n'affecte
les deux autres) — réexécuté, **vert**.

**Statut : CONFORME.**

---

## 6. Rejet Manifeste vs Annulation client

Vérifié par lecture fraîche de `Ticket.cs` :

```csharp
public void Annuler(DateTime maintenantUtc)          // F001, inchangée
{
    ...
    Statut = StatutTicket.Annule;                     // TypeAnnulationImport JAMAIS touché → reste null
}

public void RejeterImport(DateTime maintenantUtc)     // Feature 003
{
    ...
    Statut = StatutTicket.Annule;
    TypeAnnulationImport = Tickets.TypeAnnulationImport.RejetManifeste;
}
```

**Confirmé** :
- Annulation client : `Statut = Annule`, `TypeAnnulationImport = null`.
- Rejet Manifeste : `Statut = Annule`, `TypeAnnulationImport = RejetManifeste`.

**Distinction démontrée via les commandes réelles** (`AuditDistinctionAnnulationRejetTests`, ajouté
pendant l'audit, réexécuté ici) : `AnnulerTicketCommand.AnnulerLibrementAsync` (F001) vs
`RejeterTicketImportCommand` (Feature 003) sur deux tickets Billing IMPORT distincts → statuts
identiques, `TypeAnnulationImport` distinct, **et les deux BL également libérés** (la libération ne
dépend que de `Statut`, jamais de `TypeAnnulationImport`, cohérent avec le filtre SQL §4). **Vert.**

**Audit/historique** : chaque transition consigne une entrée distincte (`"TicketRejete"` avec motif
vs `"TicketAnnule"`, vérifié dans `RejeterTicketImportCommand`/`AnnulerTicketCommand`) —
distinguables en consultation.

**Statut : CONFORME.**

---

## 7. Type Client — snapshot exclusif

`ValiderTicketImportCommand` lit exclusivement `dossier.TypeClient` (jamais
`SocieteCliente.Profil`/`TypeClient` courant) — confirmé par lecture directe.

**Non-rétroactivité démontrée** : `AiguillageTypeClientTests.Changement_de_type_apres_creation_du_dossier_avant_aiguillage_n_affecte_pas_le_circuit` — société créée `Normal`, dossier créé (snapshot
`Normal`), société modifiée en `IES` **avant** validation Manifeste, ticket validé → **toujours**
aiguillé vers `Facturation`, jamais `File IES`. Réexécuté : **vert**.

**Statut : CONFORME.**

---

## 8. Client Normal — circuit complet

Rejoué via `CircuitClientNormalTests` (6 tests) + `RecSce01ImportNormalTests` — réexécutés dans cette
session, tous verts :

facturation (`DemanderFactureCommand`) → édition facture (`EditerFactureCommand`) → dépôt preuve
(`DeposerPreuvePaiementCommand`) → validation/refus (`ValiderPreuvePaiementCommand`) → **si refus** :
`Statut = EnAttenteClient` (jamais `Annule`), retour client, nouveau dépôt possible (démontré
explicitement, 2 documents successifs sur le même ticket) → validation → contrôle documentaire →
BS/BADT (`DeposerDocumentsControleEtCloturerCommand`) → clôture.

**PC-03** : grep `Caisse|Finance` dans `BillingImport/` → 0 résultat hors commentaires niant
explicitement leur usage. Seule la permission neutre `billing_import.paiement.valider` existe.
**Non résolu arbitrairement — confirmé.**

**Statut : CONFORME.**

---

## 9. Client En Compte

`EditerFactureCommand`, branche `TypeClient.EnCompte` : transfert **direct** vers Contrôle
Documentaire — assertion explicite `Assert.NotEqual(NomsFiles.ValidationPaiement, file.Nom)`
immédiatement après édition de la facture (aucune étape intermédiaire tentée, pas seulement absente
de test). `RecSce02ImportEnCompteTests` confirme la même transition à l'échelle du scénario CDC.

**Statut : CONFORME.**

---

## 10. Client Deposit

`EditerFactureCommand` (branche Deposit) → `ConfirmerLettrageCommand` → `DeposerDocumentsControleEtCloturerCommand`. `RecSce03ImportDepositTests` rejoue la chaîne complète, réexécuté : **vert**.

- **Aucune intégration IRIS** : `ConfirmerLettrageCommand` ne fait qu'enregistrer une confirmation
  déjà survenue hors portail — recherche `IRIS` dans `BillingImport/` : 0 occurrence hors commentaires
  niant explicitement toute intégration.
- **Aucun appel HTTP/API externe** : recherche `HttpClient|RestClient|SoapClient` dans
  `BillingImport/` : 0 résultat.
- **Lettrage impossible = ticket reste en file** : `ConfirmerLettrageCommand` n'expose **qu'une**
  méthode (`ConfirmerAsync`) — aucun second point d'entrée « lettrage impossible » n'existe, donc
  aucune transition automatique ne peut se produire dans ce cas. Démontré par
  `Lettrage_impossible_ou_a_reprendre_ne_declenche_aucune_action_le_ticket_reste_en_comptabilite_client`
  (capture l'état avant, n'exécute aucune commande, relit après → identique). Réexécuté : **vert**.

**Statut : CONFORME.**

---

## 11. IES

| Propriété exigée | Vérification | Résultat |
|---|---|---|
| Deux branches | `SuiviBrancheIes` × 2 par ticket IES à l'aiguillage | ✅ `Ticket_ies_aiguille_cree_deux_suivis_de_branche_non_acheves` |
| Progression indépendante | `InstruireBrancheIesCommand` ne touche que sa propre branche | ✅ confirmé par lecture + tests des 2 ordres |
| Permissions par branche | `BillingImportIesManifesteInstruire` / `...ControleDocumentaireInstruire`, refus croisé | ✅ `Agent_ne_detenant_que_la_permission_manifeste_ne_peut_pas_instruire_la_branche_controle_documentaire` + symétrique |
| Confidentialité | Refus serveur structurel dans les deux sens (mécanisme générique, PC-05 non résolu) | ✅ |
| Finalisation impossible à une seule branche | `JointureIesIncompleteException` systématique | ✅ `Manifeste_achevee_seule_ne_restitue_pas_le_ticket` + symétrique + cas E de `ConstaterJointureIesAutorisationTests` |
| **Finalisation autorisée uniquement pour utilisateur habilité** | `ConstaterJointureIesEtFinaliserCommand.ExecuterAsync` exige `EstAutoriseAsync` sur l'une des 2 permissions IES avant toute mutation | ✅ cas A/B/C/F de `ConstaterJointureIesAutorisationTests` (6/6 verts) |
| **Aucune finalisation avec `auteurId` null** | Signature `Guid auteurId` (non-nullable) — vérifié par lecture directe fraîche de `ConstaterJointureIesEtFinaliserCommand.cs` | ✅ confirmé — plus aucun paramètre nullable ; cas D (`Guid.Empty`) refusé |
| Concurrence sans double clôture | Jeton `VersionConcurrence` (F001, inchangé) → `ConflitDeConcurrenceException` sur le second appel concurrent | ✅ `ConcurrenceIesFinalisationSqlTests` (SQL Server réel, 2 `DbContext` indépendants) |

**PC-05** : `InstruireBrancheIesCommand`/`SuiviBrancheIes` ne connaissent **aucun champ métier** —
seulement `Branche`/`EstTerminee`. Aucune liste de champs réservés inventée. **Reste ouvert,
confirmé.**

**Statut : CONFORME.**

---

## 12. Autorisation / IDOR — fermeture des écarts

Vérifié par lecture fraîche de chaque fichier concerné (pas par confiance dans le rapport de
remédiation) :

| Fichier | Vérification directe (cette session) | Résultat |
|---|---|---|
| `ConstaterJointureIesEtFinaliserCommand.cs` | `EstAutoriseAsync` présent (2 appels), `AccesRefuseException` levée si aucune permission ; `auteurId` non-nullable | ✅ écart fermé |
| `FilePiloteManifeste.razor` | `@inject ConsulterFileBillingImportQuery` (grep `PlateformeDbContext` : 0 résultat) | ✅ |
| `FileFacturation.razor` | Idem | ✅ |
| `FileControleDocumentaire.razor` | Idem | ✅ |
| `FileComptabiliteClient.razor` | Idem | ✅ |
| `FileIes.razor` | Idem, **et** `ISuiviBrancheIesRepository` remplacé par `ConsulterSuivisBranchesIesQuery` (point supplémentaire trouvé en remédiation) | ✅ |
| `ValidationPaiement.razor` | Idem (page restructurée en section client/agent) | ✅ |

**Recherche fraîche, cette session** : `grep -rl "PlateformeDbContext" src/CIT.Plateforme.Web/Components/Pages/BillingImport/*.razor` → **0 résultat**. `grep "@inject I[A-Z][a-zA-Z]*Repository"` sur les
8 pages → **0 résultat**.

**Pattern cible confirmé cohérent avec F001** : les 3 requêtes autorisées ajoutées
(`ConsulterFileBillingImportQuery`, `ConsulterSuivisBranchesIesQuery`) suivent exactement le schéma
`UI → Query/Application → IAutorisationService.EstAutoriseAsync/ExigerAsync → repository`, identique
à `Tickets/ConsulterFileQuery.cs` (F001).

**Statut : CONFORME — 0 écart de sécurité résiduel.**

---

## 13. Sécurité — historique des écarts (référence)

Les 3 écarts découverts par `post-implementation-audit.md` et le point supplémentaire trouvé en
remédiation sont **tous fermés** (détail §12) :
1. Finalisation IES sans autorisation → corrigé.
2. Lecture non autorisée de 6 files → corrigé.
3. `FileIes.razor` — accès repository direct (trouvé en remédiation) → corrigé.
4. PC-19 — divergence documentaire (traité §16 de cette revue, pas un écart de sécurité).

**0 ÉCART sécurité résiduel, confirmé indépendamment dans cette revue.**

---

## 14. IMP-15 — périmètre du Should

`DeclarerFinalisationPresentielleCommand` (lu à nouveau dans cette session) : positionne uniquement
`Ticket.DeclarationPresentielle = true`, précondition « validé par le Pilote Manifeste »,
cloisonnement client. Recherche fraîche `rendez-vous|rdv|guichet|creneau|calendrier` dans
`BillingImport/` : **0 résultat** hors un commentaire qui **nie explicitement** leur implémentation.
Aucun workflow CON-xx complet — confirmé absent.

**Statut : CONFORME au périmètre Should défini par `spec.md` §0.1.**

---

## 15. Points PC — statut final

| Point | Statut | Vérification |
|---|---|---|
| PC-03 (rôle validation paiement) | Ouvert | Permission neutre `billing_import.paiement.valider`, aucun rôle nommé — confirmé §8 |
| PC-05 (champs réservés IES) | Ouvert | Aucun champ métier dans le code IES — confirmé §11 |
| PC-08 (pièces exactes) | Ouvert | `ReglesDepotDocument` paramétrable partout — confirmé par grep, aucune liste figée |
| PC-19 (limite de BL) | Ouvert, **« À CONFIRMER AVANT PRODUCTION »** | Voir §16 |

**Aucun des 4 points n'a été résolu arbitrairement — confirmé par relecture directe du code dans
cette session, pas par confiance dans une déclaration antérieure.**

---

## 16. PC-19 — vérification ciblée

- **Code** : `SoumettreDemandeImportCommand` — recherche fraîche de toute borne sur `NumerosBl.Count`
  dans cette session : **0 résultat**. Aucune limite fixe n'est imposée.
- **`quickstart.md` §3** : relu — indique désormais « aucune contrainte serveur en l'état », renvoie
  vers `post-implementation-audit.md`. Ne présente plus 5 comme une règle appliquée.
- **`SoumettreDemande.razor`** : relu — le texte d'en-tête et le label du champ ne mentionnent plus
  « jusqu'à 5 BL » comme une règle ; indique explicitement que PC-19 est un point ouvert.
- **`spec.md` CL-08** : inchangé, toujours « **À CONFIRMER AVANT PRODUCTION** » — confirmé non
  requalifié en écart, conformément à la consigne de cette revue.

**Statut : PC-19 reste ouvert, correctement documenté comme tel — pas un écart.**

---

## 17. REC-SCE — rejeu et confirmation

Réexécutés dans cette session (`dotnet test tests/CIT.Plateforme.Web.Tests --filter "FullyQualifiedName~BillingImport.RecSce0"`) → **4/4 verts** :

| REC-SCE | Intitulé (`docs/04-matrice-recette.md`, reproduit mot pour mot) | Statut |
|---|---|---|
| REC-SCE-01 | IMPORT client normal — Facture éditée → passage direct... (portion IMP/UNI) | ✅ vert |
| REC-SCE-02 | IMPORT client en compte — Facture éditée → passage direct au Contrôle Documentaire... | ✅ vert |
| REC-SCE-03 | IMPORT client deposit — Facture → lettrage Comptabilité → Contrôle Documentaire → clôture | ✅ vert |
| REC-SCE-04 | IMPORT client IES — Traitement parallèle Manifeste/CD... | ✅ vert |

**Aucune mention de REC-SCE-05 ou suivant** dans le code ou les tests Feature 003 — confirmé par
recherche fraîche (`grep -r "REC-SCE-0[5-9]"` dans `src/`, `tests/` : 0 résultat).

**Statut : CONFORME.**

---

## 18. Migrations

Vérifié dans cette session sur `CitPlateformeAuditFresh`, recréée depuis zéro via
`dotnet ef database update` (jamais `EnsureCreated`) :

- **15 migrations totales** (8 F001 + 4 F002 + **3 F003**), listées dans l'ordre chronologique exact
  par requête directe sur `__EFMigrationsHistory`.
- **3 migrations Feature 003** : `ExtensionTicketBillingImport`, `IndexUniqueFiltreNumeroBl`,
  `AjoutSuiviBrancheIes` — confirmées additives (aucun `DropColumn`/`DropTable`/`RenameColumn`).
- **Base recréable depuis zéro** : démontré dans cette session même (recréation complète).
- **Index unique filtré présent** : `IX_Tickets_NumeroBl`, `is_unique=1`,
  `filter_definition = ([NumeroBl] IS NOT NULL AND ([Statut] IN ((0), (1), (2))))` — confirmé §4.
- **Collation `NumeroBl` correcte** : `Latin1_General_CI_AS`, colonne explicite — confirmé §4.
- **Table IES correcte** : `SuivisBranchesIes` — 6 colonnes (`Id`, `TicketId`, `Branche`,
  `EstTerminee`, `AuteurId`, `DateAchevementUtc`), 0 FK (entité satellite), confirmé par requête
  directe dans cette session.
- **0 pending model change** : `dotnet ef migrations has-pending-model-changes` → « No changes have
  been made to the model since the last migration » — réexécuté dans cette session.

**Statut : CONFORME.**

---

## 19. Tests — totaux reconstitués

Suite complète réexécutée fraîchement dans cette session :

| Projet | Total | Résultat |
|---|---|---|
| `CIT.Plateforme.Domain.Tests` | 37 | 37/37 verts |
| `CIT.Plateforme.Application.Tests` | 159 | 159/159 verts |
| `CIT.Plateforme.Infrastructure.Tests` | 36 | 36/36 verts (SQL Server réel, LocalDB) |
| `CIT.Plateforme.Web.Tests` | 28 | 28/28 verts |
| **Total** | **260** | **260/260, 0 échec, 0 ignoré** |

Confirme exactement le total annoncé par `post-implementation-audit.md` §24.9 — **vérifié par
réexécution réelle dans cette session**, pas par confiance dans le chiffre du rapport.

Sous-ensembles spécifiquement revérifiés dans cette session :
- **CAS1→CAS12** : 12/12 verts (`--filter "FullyQualifiedName~ConcurrenceUniciteBlSqlTests"`).
- **REC-SCE-01→04** : 4/4 verts (`--filter "FullyQualifiedName~BillingImport.RecSce0"`).
- **Autorisation IES** (A→F) : incluse dans les 159 Application, réexécutée avec la suite complète.
- **Lectures de files (tests négatifs)** : incluse dans les 159 Application, réexécutée avec la suite
  complète.

**Statut : CONFORME.**

---

## 20. Dette technique — classification finale

Les 3 dettes non touchées par la remédiation (`post-implementation-audit.md` §21) sont reclassées
ici individuellement, comme demandé — aucune n'est transformée en écart CDC sans preuve
comportementale, car aucune des trois n'a de conséquence comportementale démontrée par un test.

### A. CAS1-12 exécutés via `EnsureCreated()`, pas les migrations réelles dans `Infrastructure.Tests`

- **Risque** : théorique — un fichier de migration pourrait diverger du modèle EF sans que
  `ConcurrenceUniciteBlSqlTests` ne le détecte (il construit son schéma directement depuis le modèle).
- **Justification de l'acceptation** : ce risque a été **indépendamment fermé** dans cette revue et
  dans l'audit initial par une vérification SQL directe sur une base migrée réellement depuis zéro
  (§4, §18) — les deux chemins (modèle EF via `EnsureCreated`, migrations réelles via
  `dotnet ef database update`) produisent un schéma identique, vérifié à deux reprises maintenant
  (audit initial + cette revue). Aucune divergence trouvée.
- **Classification : ACCEPTÉE.**
- **Échéance** : aucune fixe — à reconsidérer seulement si une future migration Feature 003+ modifie
  ce même schéma sans revérification SQL directe équivalente.

### B. Absence de contrainte unique SQL sur `FileDeTraitement(Module, Nom)`

- **Risque** : un code applicatif (production ou test) pourrait insérer une file en double sans
  qu'aucune contrainte de base ne l'empêche — la protection actuelle est uniquement conventionnelle
  (paramétrage effectué une fois, cf. `quickstart.md` §3).
- **Justification de l'acceptation** : hérité de Feature 001 (`FileDeTraitement` n'a jamais porté
  cette contrainte, y compris pour les files F001/F002), jamais rouverte par Feature 003 — corriger
  le schéma F001 est hors périmètre d'une feature qui doit rester une extension additive du socle
  (Principe d'extension additive, `research.md`). La dette symétrique côté tests
  (`Infrastructure.Tests`, deux classes séparées insérant chacune la même référence) a déjà été
  fermée à la racine par une donnée de test idempotente (post-implementation-audit.md §10.4/§21) —
  cette dette-ci ne concerne plus que le schéma de production, où le paramétrage est un acte humain
  unique documenté (`quickstart.md`), pas une insertion répétée par du code.
- **Classification : ACCEPTÉE.**
- **Échéance** : aucune fixe — à reconsidérer si une automatisation future du paramétrage des files
  (au-delà de l'acte manuel documenté) est introduite.

### C. Portée applicative de UNI-03 non répliquée dans l'index SQL Server lui-même

- **Risque** : l'unicité BL au niveau **base** (`IX_Tickets_NumeroBl`) est globale à `Tickets`, pas
  restreinte au module `BillingImport` — seul le filtre applicatif (`Module == "BillingImport"` dans
  `ObtenirTicketActifParNumeroBlAsync`) empêche un ticket d'un autre module de bloquer un BL IMPORT.
  Aujourd'hui, aucun autre module de ce dépôt n'écrit dans `Ticket.NumeroBl` (confirmé par recherche
  fraîche : seul `SoumettreDemandeImportCommand`/`Ticket.Creer(..., numeroBl:)` le renseigne) — la
  protection fonctionne **par absence de collision**, pas par une garantie structurelle en base.
- **Justification de l'acceptation** : répliquer ce filtre au niveau SQL exigerait d'ajouter `Module`
  au prédicat de l'index déjà validé et testé (CAS1-12) — un changement de schéma non justifié par un
  écart comportemental actuellement démontré (UNI-03 est **CONFORME**, §2/§7). Le modifier
  maintenant, sans besoin fonctionnel prouvé, contredirait le principe de ne pas inventer de
  changement non demandé.
- **Classification : ACCEPTÉE.**
- **Échéance** : à réévaluer **avant** qu'un futur module (ex. Feature EXPORT si elle venait à
  réutiliser `NumeroBl`) ne commence à écrire dans ce même champ — recommandation à consigner pour
  cette future feature, pas une action requise pour la fermeture de Feature 003.

**Aucune des trois dettes ne remplit un critère de blocage de fermeture** — aucune n'empêche la
satisfaction d'un Must, ne compromet une garantie de sécurité ou d'intégrité déjà démontrée,
n'invalide un critère d'acceptation, ni ne bloque un REC-SCE applicable. **3/3 ACCEPTÉE.**

---

## 21. Documentation — cohérence avec le code final

Vérifié par relecture ciblée (corrections purement documentaires autorisées, aucune règle métier
changée) :

- **`spec.md`** : inchangé depuis l'audit — CL-08/PC-19 toujours correctement qualifié « À CONFIRMER
  AVANT PRODUCTION », cohérent avec le code (§16). Aucune incohérence trouvée.
- **`plan.md`** : Constitution Check et décisions D1-D16 restent cohérents avec l'implémentation
  finale (vérifié par échantillonnage : D3 unicité BL, D9 jointure IES, D10 confidentialité IES,
  D15 permissions — tous conformes au code relu dans cette session).
- **`research.md`** : décisions D1-D16 cohérentes avec le code ; D10 (permissions par branche IES)
  se vérifie désormais aussi dans `ConstaterJointureIesEtFinaliserCommand` (pas seulement
  `InstruireBrancheIesCommand`, comme au moment de la rédaction initiale de D10) — **cohérence
  substantielle confirmée**, aucune correction requise (D10 décrit un mécanisme générique de
  permission par branche, qui s'applique bien aux deux commandes).
- **`data-model.md`** : `SuiviBrancheIes` et les extensions de `Ticket` correspondent exactement au
  schéma SQL vérifié en §18.
- **`quickstart.md`** : corrigé pendant la remédiation (PC-19, §16) — cohérent avec le code à ce jour,
  relu dans cette session, aucune autre divergence trouvée.

**Aucune incohérence documentaire supplémentaire trouvée dans cette revue** au-delà de celle déjà
corrigée en remédiation (PC-19). Aucune correction n'a donc été nécessaire pendant cette revue de
fermeture.

**Statut : CONFORME.**

---

## 22. État Git

Vérifié par `git status --short` et `git diff --stat` (cette session) :

**9 fichiers Feature 001/002 modifiés**, tous conformes à l'extension additive :
- `src/CIT.Plateforme.Application/Abstractions/Repositories.cs` (+15/-0)
- `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs` (+11/-0)
- `src/CIT.Plateforme.Domain/Tickets/Ticket.cs` (+56/**-1**) — la seule ligne modifiée est la
  signature de `Creer(...)`, un paramètre optionnel additif (`numeroBl = null`), rétrocompatible avec
  tous les appels F001/F002 existants (confirmé par le build complet et les 37+159 tests
  Domain/Application, qui incluent la suite héritée de F001/F002 sans régression).
- `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` (+35/-0)
- `src/CIT.Plateforme.Infrastructure/Persistence/Migrations/PlateformeDbContextModelSnapshot.cs` (+46/-0, généré automatiquement)
- `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs` (+27/-0)
- `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs` (+45/-0)
- `src/CIT.Plateforme.Web/Components/Pages/Tickets/DetailTicket.razor` (+14/-0) — section additive
  (bascule présentielle IMP-15), clairement étiquetée comme spécifique à Billing IMPORT.
- `tests/CIT.Plateforme.Infrastructure.Tests/Support/BaseDeTestSql.cs` (+16/-0) — seed de test
  idempotent (fermeture de dette, §20/post-implementation-audit.md §10.4).

**Total : 264 insertions, 1 suppression sur l'ensemble du socle F001/F002** — extension additive
quasi pure.

**Nouveaux répertoires/fichiers Feature 003** (non suivis, prêts à être ajoutés) :
`specs/003-billing-office-import/` (spec, plan, research, data-model, contracts, quickstart, tasks,
post-implementation-audit, ce fichier de clôture), `src/CIT.Plateforme.Domain/Tickets/{BlDejaActifException,BrancheIes,JointureIesIncompleteException,SuiviBrancheIes,TypeAnnulationImport}.cs`,
`src/CIT.Plateforme.Application/BillingImport/`, `src/CIT.Plateforme.Web/Components/Pages/BillingImport/`,
3 migrations F003 (`.cs`+`.Designer.cs`), `tests/CIT.Plateforme.{Domain,Application,Infrastructure,Web}.Tests/BillingImport/`, `tests/CIT.Plateforme.Infrastructure.Tests/Support/SqlConnectionAdministration.cs`,
`tests/CIT.Plateforme.Web.Tests/AssemblyInfo.cs`.

**Vérifications de propreté** (cette session) :
- Aucun secret détecté (aucun `.env`, clé, mot de passe en clair dans les fichiers modifiés/nouveaux).
- Aucun artefact `bin`/`obj` suivi par Git.
- **Aucun fichier Feature 004** détecté dans l'arbre de travail — confirmé, aucune commande
  `/speckit.*` de Feature 004 n'a été exécutée.
- `.claude/launch.json` : outil de session (prévisualisation navigateur), sans rapport fonctionnel
  avec Feature 003 — même statut que dans les revues de fermeture F001/F002, à la discrétion de
  l'utilisateur.
- `docs/05-roadmap-features.md` : non suivi — fichier de documentation de roadmap, sans rapport avec
  le code Feature 003, à la discrétion de l'utilisateur pour l'ajout au dépôt.

**Aucun commit, aucun push effectué** pendant cette revue, conformément à la consigne.

---

## 23. Critères de fermeture — vérification finale

| Critère (section 21 de la demande) | Statut |
|---|---|
| 24/24 exigences tracées | ✅ |
| 23/23 Must conformes | ✅ |
| 0 ÉCART fonctionnel | ✅ |
| 0 ÉCART sécurité | ✅ |
| 0 NON DÉMONTRÉ bloquant | ✅ |
| CAS1→CAS12 verts | ✅ |
| REC-SCE-01→04 verts | ✅ |
| Migrations rejouables | ✅ |
| Feature 001/002 sans régression | ✅ |
| PC-03/05/08/19 non résolus arbitrairement | ✅ |
| Dette restante acceptée/documentée | ✅ (3/3 ACCEPTÉE, §20) |

**11/11 critères satisfaits.**

## 24. Décision finale

## **CLOSE**

**Feature 003 (Billing Office IMPORT & Unicité BL) est déclarée CLÔTURÉE.**

**Justification** :
- Le score de 24/24 exigences tracées (23/23 Must CONFORME) n'est pas un report du rapport d'audit :
  chaque ligne a été reconstituée indépendamment dans cette revue, par lecture de code fraîche et
  réexécution réelle des tests cités.
- Les 3 écarts découverts par l'audit post-implémentation (2 de sécurité, 1 documentaire) sont fermés
  et re-vérifiés indépendamment ici (§12, §16) — y compris le 4ᵉ point trouvé pendant l'audit
  exhaustif des lectures exigé par la remédiation elle-même.
- L'unicité BL (portée globale, filtre par inclusion, collation déterministe, normalisation, CAS1-12)
  est confirmée définitivement par requête SQL directe sur une base recréée depuis zéro dans cette
  session — pas seulement par des tests dont la configuration pourrait diverger des migrations
  réelles (ce risque méthodologique lui-même a été fermé, §20.A).
- Les 3 dettes techniques restantes sont classées ACCEPTÉE avec risque, justification et échéance
  documentés — aucune ne remplit un critère de blocage, et aucune n'a été artificiellement
  transformée en écart CDC sans preuve comportementale (aucune des trois n'a de test qui la
  contredirait).
- PC-03, PC-05, PC-08, PC-19 restent tous ouverts, aucune valeur métier n'a été inventée pour les
  clore prématurément — confirmé par relecture directe du code, pas par confiance déclarative.
- L'état Git est propre : les seules modifications du socle F001/F002 sont additives (264
  insertions, 1 suppression rétrocompatible), aucun secret, aucun artefact indésirable, aucun fichier
  Feature 004.
- **260/260 tests verts**, réexécutés à neuf dans cette session — 0 échec, 0 ignoré.

### Éléments à inclure dans un futur commit (présentés pour validation humaine — aucun `git add`/`git commit` exécuté)

**Fichiers Feature 001/002 modifiés (extension additive)** :
- `src/CIT.Plateforme.Application/Abstractions/Repositories.cs`
- `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs`
- `src/CIT.Plateforme.Domain/Tickets/Ticket.cs`
- `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs`
- `src/CIT.Plateforme.Infrastructure/Persistence/PlateformeDbContext.cs`
- `src/CIT.Plateforme.Infrastructure/Persistence/Repositories.cs`
- `src/CIT.Plateforme.Infrastructure/Persistence/Migrations/PlateformeDbContextModelSnapshot.cs`
- `src/CIT.Plateforme.Web/Components/Pages/Tickets/DetailTicket.razor`
- `tests/CIT.Plateforme.Infrastructure.Tests/Support/BaseDeTestSql.cs`

**Nouveaux répertoires/fichiers Feature 003** :
- `specs/003-billing-office-import/` (intégralement)
- `src/CIT.Plateforme.Domain/Tickets/{BlDejaActifException,BrancheIes,JointureIesIncompleteException,SuiviBrancheIes,TypeAnnulationImport}.cs`
- `src/CIT.Plateforme.Application/BillingImport/`
- `src/CIT.Plateforme.Web/Components/Pages/BillingImport/`
- 3 migrations : `20260830055350_ExtensionTicketBillingImport*`,
  `20260830060458_IndexUniqueFiltreNumeroBl*`, `20260830060702_AjoutSuiviBrancheIes*`
- `tests/CIT.Plateforme.Domain.Tests/Tickets/TicketExtensionsBillingImportTests.cs`
- `tests/CIT.Plateforme.Application.Tests/BillingImport/`
- `tests/CIT.Plateforme.Infrastructure.Tests/BillingImport/`,
  `tests/CIT.Plateforme.Infrastructure.Tests/Support/SqlConnectionAdministration.cs`
- `tests/CIT.Plateforme.Web.Tests/BillingImport/`, `tests/CIT.Plateforme.Web.Tests/AssemblyInfo.cs`

**À exclure ou traiter séparément (à la discrétion de l'utilisateur, hors périmètre de cette revue)** :
- `.claude/launch.json` — outil de session, sans rapport fonctionnel avec Feature 003.
- `docs/05-roadmap-features.md` — documentation de roadmap, à évaluer séparément.

**Feature 004 n'a pas été commencée par cette revue et ne le sera pas tant qu'une validation humaine
explicite n'aura pas été donnée sur ce résultat.**
