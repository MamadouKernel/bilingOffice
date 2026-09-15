# Progression i18n FR/EN — par lots

Baseline tests avant travaux : **1644/1644** (Domain 231, Application 839, Infrastructure 175, Web 399).
Architecture i18n inchangée : `SharedResourcesLocalizer` (dictionnaires FR/EN en mémoire, clés
namespacées), culture par cookie `.AspNetCore.Culture`, `<html lang>` dynamique, `LanguageSwitcher`.

Règle de gate : on ne démarre pas le LOT N+1 tant que le LOT N n'est pas
**traduit + compilé + testé + parité + mutation + recette FR + recette EN + 0 texte résiduel injustifié**.

---

## LOT 1 — SHARED / NAVIGATION

**Périmètre :** coquille partagée rendue sur toutes les pages —
`Layout/` (NavMenu, HeaderBar, PersonaSwitcher, ReconnectModal) et les composants partagés de
coquille (`ModaleConfirmation`, `ToastNotification`, `EtatVide`, `IndicateurChargement`,
`PermissionDenied`, `FilAriane`). `ChampMotDePasse` et `StatutBadge` étaient déjà localisés.
`LanguageSwitcher` : les endonymes « Français (FR) » / « English (EN) » restent volontairement
non traduits (bonne pratique i18n).

Hors périmètre LOT 1 (traités dans leurs lots) : `BandeauDisponibiliteAgent` (LOT 4 Agent),
`ZoneActionsRequises` et `ChronologieTicket` (LOT 3 Client / tickets).

### Menu latéral
- Approche : les libellés (`groupe.Titre`, `lien.Libelle`) sont résolus par leur **texte français
  canonique** via `@Loc[...]` dans `NavMenu.razor`. `CatalogueNavigation.cs` (couche Application) et
  les tests de sécurité de navigation restent **inchangés** — risque minimal.
- 65 clés de menu ajoutées (4 titres de groupe + 61 libellés d'entrée) en FR **et** EN.
- **Les routes et permissions ne dépendent pas de la culture** : `routes FR == routes EN`, seuls les
  libellés changent. Vérifié par `NavigationI18nTests.Les_routes_du_menu_sont_identiques_quelle_que_soit_la_culture`.

### Clés ajoutées (familles)
`Common.*` (Confirm, Confirmation, ConfirmActionQuestion, Processing, Breadcrumb, Close,
LoadingData, EmptyTitle, EmptyMessage, CloseNotification), `Nav.*` (SignIn, CreateCompanyAccount,
MenuLoading, NoAccessibleScreen), `Header.*` (barre supérieure + libellés d'espace),
`AccessDenied.*` (403), `Reconnect.*` (superposition de reconnexion du framework), + 65 libellés de
menu. Codes métier laissés invariants (ex. `CIT-SEC-403`).

### Gate LOT 1
| Critère | État | Preuve |
|---|---|---|
| Traduit | ✅ | Coquille partagée FR/EN complète (voir périmètre) |
| Compilé | ✅ | `dotnet build` Web : 0 avertissement, 0 erreur |
| Testé | ✅ | Web **426/426** (399 baseline + 27 gardes LOT 1) |
| Parité FR/EN | ✅ | `TraductionParityTests` (clés FR == clés EN) reste vert |
| Textes en dur | ✅ | `TextesEnDurLot1Tests` (10 fichiers de coquille : câblage localizer + littéral FR interdit) |
| Mutation | ✅ | EN `Tableau de bord` → `MUTATION_Dashboard` ⇒ `NavigationI18nTests` tombe (1 échec), puis restauré |
| Recette FR | ✅ | Navigateur UAT (env=UAT, base seedée). Sidebar/HeaderBar/menu utilisateur FR corrects |
| Recette EN | ✅ | Bascule cookie `.AspNetCore.Culture=en-US` : libellés traduits, **routes identiques**, codes métier conservés |
| 0 texte résiduel | ✅ (menu) | `NavigationI18nTests.Aucun_libelle_de_menu_ne_reste_en_francais_en_anglais` |

### Recette navigateur — profils vérifiés (serveur UAT, `/uat/session/{email}`)
| Profil | FR | EN | Routes FR==EN |
|---|---|---|---|
| Admin fonctionnel | ✅ sidebar+header+menu utilisateur | ✅ (Dashboard, Account Requests, Audit Log…, Digital Platform, Sign Out…) | ✅ hrefs identiques |
| Superviseur | ✅ (Vue Supervision, Disponibilité agents) | ✅ (Supervision View, Agent Availability) | ✅ /supervision, /administration/disponibilite |
| Agent gate | — | ✅ (Operator Dashboard, Gate Agent Queue, Security (2FA)) | ✅ |
| Agent sans droits | — | ✅ (Operator Dashboard, Security (2FA) — liens sans permission) | ✅ |
| Client normal | — | ✅ (menu complet : My Requests, Proforma Simulation, New Complaint…) | ✅ |
| Transporteur | — | ✅ (New Enrolment, My Enrolments…) | ✅ |

Coquille additionnelle vérifiée en EN : page **403** (`PermissionDenied`) → "Access Denied", "Port Security",
"Guidance:", "Back to home", "My Dashboard", code `CIT-SEC-403` conservé, aucun texte FR résiduel.
`<html lang>` bascule bien `fr` ⇄ `en`. Endonymes du sélecteur de langue laissés inchangés.

**Statut LOT 1 : GATE CLÔTURÉ (traduit + compilé + testé + parité + mutation + recette FR + recette EN + 0 résiduel).**

---

## LOT 2 — PUBLIC / AUTH  ✅ GATE CLÔTURÉ

**Périmètre (12 surfaces) :** Home, Error (500), NotFound (404) ; Connexion, VerifierCode (2FA),
MotDePasseOublie, ReinitialiserMotDePasse, SoumettreDemande (compte société), InscriptionTransporteur,
AccepterInvitation, AccesRefuse, CompteCloture. **202 chaînes inventoriées** (voir `inventaire-lot2.md`).

Correction de coquille partagée découverte pendant la recette EN (fuite FR sur toutes les pages) :
`MainLayout` — lien d'évitement (`Layout.SkipToContent`) et barre d'erreur Blazor
(`Layout.UnexpectedError`, `Layout.Reload`) localisés.

**Frontière documentée (NON TRAITÉ, chantier transverse) :** messages d'exceptions métier
(`MessagesUtilisateur.Pour`, libellés Domain/Application) et messages de règles de mot de passe
d'ASP.NET Identity (`resultat.Errors`). À traiter dans une passe dédiée (touche tous les lots).

### Simulation Proforma publique — RECERTIFIÉE
Anonyme, build Release publié (circuit InteractiveServer requis). Mêmes entrées (Import local, 20',
Dry, C1, 12 t, 01→12 sept.) :
- EN → **76,850 XOF** (61,000 + 15,000 + 850)
- FR → **76 850 XOF** (61 000 + 15 000 + 850)
- **Parité métier confirmée** : montant identique (76850) ; seule la mise en forme du séparateur de
  milliers suit la culture (allowed). La culture ne modifie ni tarif, ni durée, ni arrondi.

### Gate LOT 2
| Critère | État | Preuve |
|---|---|---|
| Traduit | ✅ | 12 surfaces FR/EN (pages, PageTitle, labels, placeholders, aria, boutons, messages détenus par la page) |
| Compilé | ✅ | build Web : 0 avertissement / 0 erreur |
| Testé | ✅ | Web **455/455** (426 + 29 gardes LOT 2) |
| Parité FR/EN | ✅ | `TraductionParityTests` vert (clés FR == EN) |
| Textes en dur | ✅ | `Lot2PublicAuthI18nTests` (12 pages : marqueur localizer + littéral FR interdit) |
| PageTitle | ✅ | garde `Le_PageTitle_est_localise` (12/12) |
| Placeholders | ✅ | garde `Les_placeholders_de_texte_sont_localises` (5/5) + recette EN (jane.doe@company.com) |
| Mutation M1 (clé EN) | ✅ | renommer clé EN ⇒ parité tombe, restauré |
| Mutation M2 (PageTitle FR) | ✅ | PageTitle FR en dur ⇒ garde PageTitle tombe, restauré |
| Mutation M3 (placeholder FR) | ✅ | placeholder FR en dur ⇒ garde placeholder tombe, restauré |
| Mutation M4 (parité métier) | ✅ | calcul simulation FR==EN (76850) — divergence culturelle limitée au format |
| Recette FR | ✅ | Home + pages : texte FR correct, 0 fuite EN |
| Recette EN | ✅ | Home/Connexion/Soumettre/Inscription/MdpOublié/404 : 0 fuite FR (après fix coquille) |
| Responsive EN | ✅ | Home + Connexion : 0 overflow horizontal à 320 / 768 / 1024 / 1366 |
| Dark mode EN | ✅ | Connexion EN dark : rendu propre, contraste OK, aucune casse |
| 0 texte résiduel | ✅ | balayage DOM EN par mots FR : 0 fuite sur pages LOT 2 |
| Aucun commit | ✅ | — |

**Arithmétique des tests :** baseline d'origine 1644 (Web 399) → LOT 1 **+27** = 1671 (Web 426) →
LOT 2 **+29** = **1700** (Web 455). Global mesuré : Domain 231 + Application 839 + Infrastructure 175
+ Web 455 = **1700/1700**, 0 échec, 0 ignoré. (Le « 1644 avant LOT 2 » du brief désigne le baseline
pré-LOT 1 ; le vrai pré-LOT 2 mesuré est 1671.)

**Statut LOT 2 : OK.**

---

## LOT 3 — CLIENT — EN COURS

**Périmètre (~25 surfaces) :** Dashboards Client & Société, Mes demandes, Avoir/Exonération
(demandeur), Réclamations (demandeur), Enrôlement (demandeur), Tickets (côté client), Rendez-vous,
Mon profil / Profil société / 2FA, soumissions Import/Export, et les 3 écrans Simulation.
Hors périmètre (→ LOT 4 Agent) : FileReclamations, FileAgentGate, FileAvoirExoneration,
RechercheGuichet, AgendaGuichet, Dossiers, GestionGrillesTarifaires.

### Vague 1 — Simulateur de proforma ✅ (traduit, non encore « gate »)
- 45 clés `Sim.*` (FR/EN, parité par construction) câblées dans `SimulationProformaPage.razor`
  (en-tête, paramètres, cartes conteneurs, bascules Danger/OOG, barre d'action, alertes, bandeau
  total, en-têtes du tableau de décomposition, boutons, messages `@code`).
- Données référentielles (Import local, 20 pieds, Dry/Reefer, « Cx — Catégorie… ») laissées
  invariantes (données base, pas UI).
- ✅ compile 0 erreur ; ✅ 45/45 clés utilisées résolvent ; ✅ recette navigateur FR + EN (0 fuite
  UI, 0 clé littérale). Design refait précédemment conservé.

### Vague 2 — Cœur Client (en cours)
- ✅ `DashboardClient.razor` traduit (33 clés `Dash.*`, FR/EN, parité par construction) : titre, KPI,
  tableau des demandes récentes, état vide, libellés d'état de dossier, message d'erreur. Compile 0 erreur.
  (Recette navigateur FR/EN à faire au gate.)

- ✅ `MesDemandes.razor` traduit (16 clés `Demandes.*` + réutilisation de `Dash.*` pour statuts/colonnes) :
  titre, recherche (aria + placeholder), export CSV, table, état vide, message de session. Compile 0 erreur.

### Vague 3 — Société + Compte + Avoir/Exonération (en cours, session post-compact)
Toutes traduites (clés FR/EN, parité par construction ; valeurs métier/enums laissées invariantes ;
messages `@code` via `Loc[].Value` ou `SharedResourcesLocalizer.Get` pour les méthodes statiques) :
- ✅ `Societe/DashboardSociete.razor` (clés `Societe.*` + réutilisation `Dash.Company/Profile`).
- ✅ `Societe/Collaborateurs.razor` (clés `Collab.*` : rattachement, invitations, actions admin,
  modales de confirmation désactivation/révocation, messages `@code`).
- ✅ `Comptes/MonProfil.razor` (clés `Profil.*` : identité, rattachement, sécurité, zone clôture +
  modale ; `LibelleEspace` statique via `Get`).
- ✅ `Comptes/ProfilSociete.razor` (clés `ProfilSoc.*`).
- ✅ `Comptes/ActiverDoubleAuthentification.razor` (clés `TwoFa.*` ; 2 clés HTML via MarkupString ;
  `issuer` otpauth invariant).
- ✅ `AvoirExoneration/NouvelleDemandeAvoirExo.razor` (clés `AvoirExo.*` ; `<option value>` métier
  `Avoir`/`Exoneration` invariants ; `Common.KoUnit` réutilisable ajouté).
- ✅ `AvoirExoneration/MesDemandesAvoirExo.razor` (clés `AvoirExoList.*` ; `TypeDemande`/`Statut`/
  `Decision`/XOF invariants).
- ✅ `AvoirExoneration/FicheAvoirExoneration.razor` (clés `FicheAvoirExo.*` ; page mixte client+agent,
  `ObtenirBadgeStatut` et comparaisons de statut invariants).

⚠️ **Vérification compile** : impossible d'obtenir un build vert « propre » cette session — l'instance
:5018 (Dev/Debug) **et** un process build/serveur Razor verrouillent `obj/{Debug,Release}/net10.0`
(`rpswa.dswa.cache.json`, `GeneratedMSBuildEditorConfig.editorconfig`). Aucune erreur `error CS`/`RZ`/
`.razor(...)` n'apparaît dans les builds tentés (génération Razor OK) mais **CoreCompile complet non
prouvé**. Le build vert + tests + recette FR/EN = **gate LOT 3**, à faire instance arrêtée.

### Vague 3 (suite) — Réclamations, Enrôlement, Guichet ✅ (post-compact, session 14 sept.)
- ✅ `CustomerComplaint/NouvelleReclamation.razor` (clés `Complaint.*` complétées : littéraux résiduels).
- ✅ `CustomerComplaint/MesReclamations.razor` (clés `ReclamList.*` ; `Libelle` statique → `Get` ; `Module=="CustomerComplaint"` invariant).
- ✅ `Enrolement/NouvelEnrolement.razor` (clés `Enrol.*`).
- ✅ `Enrolement/MesEnrolements.razor` (clés `EnrolList.*` ; `StatutBadge`/`StatutTicket.Cloture` invariants).
- ✅ `Enrolement/FicheEnrolement.razor` (clés `FicheEnrol.*` ; page mixte client+agent).
- ✅ `Guichet/MesRendezVous.razor` (clés `Rdv.*` ; modales report/annulation ; `r.Statut` invariant).
- ✅ `Guichet/PrendreRendezVous.razor` (clés `PrdRdv.*` ; `<option value>` motifs + `Statut`/`Module` invariants).

### Vague 4 (suite) — Composants partagés, Tickets, Tarification, Billing, Dossiers ✅ (session 14 sept.)
- ✅ Composants partagés `Shared/ChronologieTicket.razor` (clés `Chrono.*`) et `Shared/ZoneActionsRequises.razor`
  (clés `Zar.*`) — corrige aussi les fuites dans les pages déjà traduites (DashboardClient, FicheEnrolement).
- ✅ `Tickets/FicheTicket.razor` (clés `Ticket.*` ~70 ; page mixte client+agent ; `Statut`/`Module`/enums invariants).
- ✅ `Tickets/Documents.razor` (clés `Docs.*` ; dépôt client ; enum antivirus brut invariant).
- ✅ `Tarification/HistoriqueSimulationsPage.razor` (clés `SimHist.*`).
- ✅ `Tarification/DetailSimulationPage.razor` (clés `SimDet.*` ; réutilise `Sim.Col*`).
- ✅ `BillingImport/SoumettreDemande.razor` (clés `BImp.*` ; brouillon, doublons, MarkupString lien).
- ✅ `BillingExport/SoumettreDemandeExport.razor` (clés `BExp.*` + réutilisation `BImp.*`).
- ✅ `Dossiers/DetailDossier.razor` (clés `DetDos.*`) et `Dossiers/FicheDossier.razor`
  (clés `DosF.*` ; `LibelleEtatDossier` statique → `Dash.Etat*` via `Get`).

**→ Périmètre CLIENT du LOT 3 : terminé (traduction + statut invariants).** Reste à vérifier au GATE :
build vert (instance :5018 arrêtée), tests (parité/textes-en-dur/PageTitle/placeholders), mutations, recette FR/EN.

### Hors périmètre LOT 3 (→ LOT 4 Agent/Supervision)
`Tickets/DetailTicket.razor` (`/administration`), `Tickets/IndicateursTemps.razor` (SLA), et tous les
`File*` (FileReclamations, FileAgentGate, FileAvoirExoneration, File*Import/Export), RechercheGuichet,
AgendaGuichet, GestionGrillesTarifaires, DashboardAgent, Supervision, Administration/*.

Puis : gardes LOT 3 (parité déjà globale, textes-en-dur ciblés, PageTitle, placeholders), mutations,
recette FR/EN complète → **gate LOT 3**.
## LOT 4 — AGENT / SUPERVISEUR — à venir
## LOT 5 — ADMINISTRATION — à venir
## LOT 6 — RESTE — à venir

> Ne jamais annoncer GLOBAL 100 % avant la clôture du LOT 6. Ne jamais forcer 100. AUCUN COMMIT.
