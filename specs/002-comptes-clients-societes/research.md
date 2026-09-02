# Research — Comptes clients, sociétés et utilisateurs rattachés

**Feature** : `002-comptes-clients-societes` | **Date** : 2026-08-29 | **Phase 0 de `/speckit.plan`**

> **Cadre** : Feature 002 s'inscrit dans la même stack 100 % C#/.NET/SQL Server que Feature 001
> (directive maître d'ouvrage du 28/08/2026, Principe I de la constitution) — **aucune technologie
> supplémentaire**, aucun microservice. Chaque décision ci-dessous privilégie la **réutilisation**
> des mécanismes déjà livrés et clos de Feature 001 (`closure-review.md`) plutôt que leur
> duplication, conformément à l'instruction explicite de cette planification. Les clarifications
> validées (CLAR-01, CLAR-02, CLAR-03 — `spec.md` § Clarifications) sont des contraintes fermes de
> conception, pas des options.

---

## D1 — Relation entre l'identité technique (Identity) et l'utilisateur métier

- **Décision** : `ApplicationUser : IdentityUser<Guid>` (Feature 001) est réutilisé **sans
  modification**. Un `ApplicationUser` de nature `NatureUtilisateur.Client` devient un « utilisateur
  rattaché » au sens de Feature 002 dès qu'une ligne `UtilisateurSociete` (nouvelle entité, FK
  `UtilisateurId → ApplicationUser.Id`) existe pour lui. Aucun champ société n'est ajouté sur
  `ApplicationUser` lui-même.
- **Rationale** : `ApplicationUser` porte exclusivement l'authentification (decision D4 de
  Feature 001) ; y ajouter une référence société fusionnerait identité technique et rattachement
  métier, ce que le brief interdit explicitement (« ne pas fusionner : identité technique /
  utilisateur métier / société / client »). Une entité de rattachement séparée reproduit le style
  déjà retenu pour `HabilitationUtilisateur` (association externe plutôt que colonnes ajoutées sur
  l'utilisateur).
- **Alternatives considérées** :
  - Ajouter `SocieteId` directement sur `ApplicationUser` — rejeté (fusion identité/société interdite).
  - Second système d'identité dédié aux clients — rejeté explicitement par le brief.

## D2 — Modèle société / utilisateur rattaché

- **Décision** : deux entités Domain nouvelles, dans un module `CIT.Plateforme.Domain.Comptes` :
  - **`SocieteCliente`** (agrégat racine) : identifiant, informations déclarées, profil
    (`TypeClient`), état (`EtatSociete` : EnAttenteValidation / Active / Refusee), compteur
    `NombreAdministrateursActifs` (invariant CLAR-03, voir D6), jeton de concurrence.
  - **`UtilisateurSociete`** (rattachement, FK `UtilisateurId` et `SocieteId`) : `EstAdministrateur`,
    `DroitSoumission`, `DroitConsultation`, `EstActif`.
  - **Aucune entité séparée pour la « demande de création de compte »** : conformément au style déjà
    retenu par Feature 001 pour `Ticket` (un seul agrégat porte tout son cycle de vie, sans entité
    « TicketDemande » distincte — CLAR-01 de Feature 001), `SocieteCliente` existe dès la
    soumission, à l'état `EnAttenteValidation` ; elle n'est jamais un objet distinct du compte final.
    Ceci **précise** (sans le contredire) le vocabulaire de `spec.md` § Key Entities, qui distingue
    la demande du compte **conceptuellement** (transitoire vs actif), pas techniquement.
- **Rationale** : correspond au vocabulaire déjà validé dans `spec.md` (« Compte client (Société) »,
  « Utilisateur rattaché ») ; la séparation société/rattachement permet le modèle multi-utilisateurs
  d'ENT-01 et porte individuellement les droits ENT-02 sans toucher `HabilitationUtilisateur`.
- **Alternatives considérées** :
  - Une seule table dénormalisée société+utilisateur — rejeté (interdit un modèle 1..N propre,
    contraire à ENT-01).
  - Loger le rattachement société dans `HabilitationUtilisateur.FileId` détourné de son usage —
    rejeté explicitement (fusion interdite, `FileId` a une sémantique différente : file de
    traitement, pas société).
  - Entité séparée « Demande de création de compte » — rejeté (dupliquerait le cycle de vie de
    `SocieteCliente` sans besoin fonctionnel nouveau ; incohérent avec le style Feature 001).
- **Précision post-remédiation (2026-08-29, `post-implementation-audit.md` §10)** : à
  l'implémentation initiale, ni `SocieteId` ni `UtilisateurId` ne portaient de contrainte de clé
  étrangère SQL réelle (« FK » ci-dessus désignait uniquement la relation conceptuelle). L'audit a
  classé cette absence comme dette technique à trancher au cas par cas plutôt que comme un écart
  CDC. Décision retenue : `UtilisateurSociete.SocieteId` porte désormais une contrainte réelle
  (`FK_UtilisateursSociete_Societes_SocieteId`, `ON DELETE RESTRICT`) — empêche un rattachement
  orphelin sans risque de suppression en cascade de l'historique. `UtilisateurSociete.UtilisateurId`
  reste **volontairement** sans contrainte SQL vers `AspNetUsers` : cohérent avec le découplage
  Identity/Domaine déjà pratiqué par Feature 001 (aucune entité de ce socle ne référence `AspNetUsers`
  par FK), et sans risque puisqu'aucun `ApplicationUser` n'est jamais supprimé physiquement
  (désactivation par drapeau uniquement).

## D3 — Représentation du type de client (CPT-02)

> **Alignement post-remédiation (2026-08-29, `post-implementation-audit.md` §8/§9)** : l'audit a
> constaté que `Dossier` (Feature 001) ne conservait aucun instantané du type client applicable au
> moment de sa création, rendant la non-rétroactivité de CPT-06 non démontrable pour le type
> lui-même (seule la propriété `ClientId` était préservée). Remédiation : `Dossier` porte désormais
> un second champ nullable additif, `TypeClient? TypeClient` (même enum, même style que `AuteurId` —
> voir D11), renseigné par l'appelant à la création. **Ce champ appartient donc aux deux entités** :
> `SocieteCliente.Profil` (valeur courante, modifiable — CPT-06) et `Dossier.TypeClient` (instantané
> figé à la création, jamais réécrit). Aucun câblage automatique dans `CreerTicketCommand` (Feature
> 001) : cela reste, comme prévu, la responsabilité de la future feature Billing Office qui créera
> réellement des dossiers rattachés à une société.

- **Décision** : enum C# `TypeClient { Normal, EnCompte, Deposit, IES }` sur `SocieteCliente`,
  stocké via `HasConversion<string>()` (même style que `NatureUtilisateur` en Feature 001). Toute
  modification passe par une méthode Domain dédiée (`SocieteCliente.ModifierType(...)`) qui exige
  systématiquement auteur, date/heure et motif — jamais de branchement dispersé dans
  Application/Web. La logique de sélection d'un workflow Billing Office selon ce type reste hors
  périmètre (future feature — spec.md § Hors périmètre).
- **Rationale** : quatre valeurs fermées, strictement définies par le CDC (CPT-02) — un enum est
  directement approprié ; conservation en base sous forme lisible (string) pour les futurs workflows
  Billing Office qui consommeront cette valeur (déjà anticipé par spec.md comme « exploitable
  ultérieurement »).
- **Alternatives considérées** :
  - Table de référence `TypeClient` administrable en base — rejeté (YAGNI ; le CDC ferme la liste à
    4 valeurs, aucune évolutivité n'est demandée).
  - Stockage en `int` — rejeté (moins lisible, fragile en cas de réordonnancement accidentel de
    l'enum).

## D4 — Modèle des droits de consultation et de soumission (ENT-02)

- **Décision** : le **scope de visibilité** (« ses demandes uniquement » / « toutes celles de la
  société ») et le **droit de soumission** sont des attributs simples de `UtilisateurSociete`
  (`DroitConsultation`, `DroitSoumission : bool`), **pas** des `HabilitationUtilisateur`/`Permission`
  de Feature 001. Par défaut à la création d'un rattachement : `DroitConsultation =
  PropresDemandesUniquement` (moindre privilège, Principe VII). Les **actions CIT** (valider une
  demande, modifier un type client) réutilisent réellement le moteur `IAutorisationService`/
  `Permission` de Feature 001 : `compte.valider`, `compte.type_modifier` sont des codes de
  permission additifs, vérifiés avec `fileId: null` (portée globale).
  <br/>**Précision apportée en implémentation (2026-08-29)** : pour les actions de gestion des
  **collaborateurs par un administrateur de société** (rattacher, activer/désactiver, modifier les
  droits, attribuer/retirer le rôle admin), l'autorité n'est **pas** vérifiée via une
  `HabilitationUtilisateur`/`Permission.CompteGererCollaborateurs` générique, mais **directement** :
  l'appelant doit posséder un `UtilisateurSociete` actif avec `EstAdministrateur = true` **pour la
  société cible**. Rationale : le rôle Administrateur de société est attribué dynamiquement (premier
  admin à la validation, puis attribution/retrait via C2-09) — exiger en plus une habilitation
  générique provisionnée séparément par CIT créerait une étape de provisioning manuelle non prévue
  par le CDC et contraire à ENT-02 (« un administrateur de société gère ses collaborateurs », capacité
  intrinsèque au rôle). `Permission.CompteGererCollaborateurs` reste définie (réservée à un usage CIT
  de support/override si un besoin futur l'exigeait) mais n'est plus le mécanisme principal.
- **Rationale** : `HabilitationUtilisateur`/`AutorisationService` modélisent des permissions
  **d'action** bornées à une **file** (ex. « transférer dans la file X ») — un concept différent
  d'un **scope de données** (« quel périmètre de demandes un utilisateur voit-il dans sa société »).
  Loger un `SocieteId` dans `HabilitationUtilisateur.FileId` détournerait sa sémantique et casserait
  `AutorisationService.EstAutoriseAsync` (qui traite `FileId == null` comme « global » — un
  détournement introduirait une ambiguïté entre portée agent et portée société). Le brief interdit
  explicitement cette fusion.
- **Alternatives considérées** :
  - Étendre `HabilitationUtilisateur` avec un `SocieteId` optionnel en plus de `FileId` — rejeté
    (modifierait le comportement de `AutorisationService.EstAutoriseAsync`, avec risque de
    régression sur les tests Feature 001 existants, alors qu'une entité séparée est strictement
    additive et sans risque).

## D5 — Implémentation technique de la visibilité propre / société (ENT-03, CLAR-01, CLAR-02)

- **Décision** — trois volets :
  1. **Extension additive de `Dossier`** (Feature 001) : `Dossier.ClientId` (champ existant, déjà
     décrit par Feature 001 comme « identifiant client abstrait ») devient, dans le contexte
     Feature 002, l'identifiant de la **société** propriétaire (`SocieteCliente.Id`) — CLAR-01. Un
     nouveau champ **nullable** `AuteurId : Guid?` est ajouté à `Dossier`, avec un paramètre
     optionnel supplémentaire sur `Dossier.Creer(...)` par défaut à `null`. Tous les appels/tests
     existants de Feature 001 (signature à 3 arguments) restent valides sans modification — les
     tickets créés avant Feature 002 n'ont simplement pas cette distinction, ce qui est correct.
  2. **Requête applicative Feature 002** (`DeterminerVisibiliteDemandesQuery`, contrat C2-11) :
     résout, pour un utilisateur donné, sa société (`UtilisateurSociete`), `EstAdministrateur` et
     `DroitConsultation`, puis renvoie le filtre applicable à l'appelant (future Billing Office) :
     `ClientId == maSociete ET AuteurId == monId` (droit restreint), ou `ClientId == maSociete` seul
     (droit total **ou** administrateur — CLAR-02), ou refus si aucun rattachement société actif.
  3. **Cloisonnement inter-société dur** (CPT-04, « jamais un autre compte ») : réutilise
     `CloisonnementService.ExigerProprieteClientAsync(societeId, ticketId)` de Feature 001 **tel
     quel, sans aucune modification**, en lui passant l'identifiant société — le comportement déjà
     validé (« `ClientId` inconnu → toujours refusé ») continue de s'appliquer sans changement.
- **Rationale** : reproduit exactement CLAR-01/CLAR-02 déjà validées en `/speckit.clarify`. Le volet
  3 exploite le fait que `CloisonnementService` compare une simple égalité de GUID, indifférente à ce
  que ce GUID représente conceptuellement — extension sans aucune modification de code Feature 001.
- **Alternatives considérées** :
  - Dériver la société d'un utilisateur par jointure `UtilisateurSociete` à chaque lecture, sans
    stocker de société sur `Dossier` — rejeté : le brief exige explicitement que la demande « est
    rattachée à la société » (pas seulement dérivable) ; un stockage explicite au moment de la
    soumission évite aussi qu'une future résolution de NOUVEAU-03 (changement de société d'un
    utilisateur) ne modifie rétroactivement l'attribution de demandes déjà soumises — cohérent avec
    le principe de non-rétroactivité déjà retenu pour CPT-06.

## D6 — Invariant du dernier administrateur actif (CLAR-03)

- **Décision** : compteur dénormalisé `SocieteCliente.NombreAdministrateursActifs : int`, maintenu
  exclusivement par des méthodes Domain de `SocieteCliente` (`RetirerRoleAdministrateur(...)`,
  `DesactiverAdministrateur(...)`) qui lèvent `DernierAdministrateurActifException` si l'opération
  ferait tomber le compteur à 0, protégé par un jeton de concurrence optimiste EF Core (`RowVersion`)
  sur `SocieteCliente` — même mécanisme que `Ticket.VersionConcurrence` (D3/D6 de Feature 001).
- **Rationale** : l'invariant porte sur un **ensemble** de lignes (tous les `UtilisateurSociete`
  administrateurs actifs d'une société), pas sur une seule — un jeton de concurrence individuel par
  `UtilisateurSociete` ne suffirait pas à empêcher deux désactivations concurrentes de réussir
  toutes deux si elles portent sur deux administrateurs différents. En centralisant le compteur sur
  l'agrégat racine `SocieteCliente`, décrémenté dans la même transaction que toute opération
  affectant un administrateur, EF Core détecte la concurrence sur ce compteur
  (`DbUpdateConcurrencyException` → `ConflitDeConcurrenceException`, pattern déjà éprouvé pour la
  prise en charge concurrente de Feature 001).
- **Alternatives considérées** :
  - Trigger SQL Server (`AFTER UPDATE`, vérification `COUNT`) — rejeté (logique métier déplacée en
    base, contraire au Principe V).
  - Verrouillage pessimiste (`SELECT ... WITH (UPDLOCK, HOLDLOCK)`) comme mécanisme principal —
    rejeté (moins cohérent avec le style optimiste déjà retenu pour la même classe de problème en
    Feature 001 ; le compteur dénormalisé + RowVersion suffit et reste testable).
- **Précision post-remédiation (2026-08-29, `post-implementation-audit.md` §6)** : l'invariant
  formel est `NombreAdministrateursActifs == COUNT(UtilisateurSociete WHERE EstAdministrateur = true
  AND EstActif = true)`. L'audit a démontré une dérive possible : attribuer le rôle administrateur à
  un utilisateur déjà désactivé incrémentait le compteur sans que celui-ci soit réellement capable
  d'agir. Corrigé : `AttribuerRoleAsync`/`RetirerRoleAsync` ne modifient le compteur que si
  `cible.EstActif == true` (symétrique du traitement déjà correct d'`ActiverAsync`/`DesactiverAsync`,
  qui ne le modifient que si `cible.EstAdministrateur == true`). Prouvé sur les 8 transitions
  possibles (`InvariantCompteurAdministrateurTests.cs`) et sur SQL Server réel (CAS 1→5).

## D7 — Stratégie de gestion de la concurrence (opérations sensibles)

- **Décision** : `RowVersion` EF Core sur `SocieteCliente` (D6) couvre : désactivation/retrait de
  rôle administrateur, modification des droits de consultation/soumission (évite qu'une
  modification concurrente de droits soit silencieusement écrasée), changement de type client.
  Chaque opération sensible passe par `IUniteDeTravail.EnregistrerAsync` (Feature 001, réutilisé tel
  quel) et traduit `DbUpdateConcurrencyException` en `ConflitDeConcurrenceException` — aucune
  nouvelle exception de concurrence créée.
- **Rationale** : réutilise strictement le mécanisme déjà validé et testé de Feature 001, cohérent
  avec l'instruction de ne pas dupliquer ses services.
- **Alternatives considérées** : aucune — reprise directe du pattern existant ; le seul choix réel
  (« quoi verrouiller ») est tranché en D6.

## D8 — Audit (ENT-04, CPT-06 — réutilisation du journal Feature 001)

- **Décision** : `IJournalisationService.ConsignerAuditAsync(...)` (Feature 001, interface
  **inchangée**) est appelé pour : validation/rejet d'une demande de compte (CPT-01), modification
  du type client (CPT-06, `detailAvant`/`detailApres` = ancien/nouveau type), rattachement d'un
  utilisateur (ENT-01), activation/désactivation d'un utilisateur (ENT-02), modification des droits
  (ENT-02), attribution/retrait du rôle administrateur (ENT-02, CLAR-03). `entiteType` =
  `"SocieteCliente"` ou `"UtilisateurSociete"` selon le cas ; `auteurId` = l'agent CIT ou
  l'administrateur de société ayant agi.
- **Rationale** : couvre exactement ENT-04 (Should) et la traçabilité exigée par CPT-06, sans
  nouvelle table ni nouveau service — répond à l'instruction explicite « réutiliser, ne pas créer un
  second journal ».
- **Alternatives considérées** : aucune — un second journal violerait le Principe IX et l'instruction
  explicite du brief.

## D9 — Authentification renforcée (CPT-07, Could)

- **Décision** : capacités natives ASP.NET Core Identity pour le second facteur, activable par le
  titulaire du compte, **jamais imposé par défaut**. Aucune extension de schéma nécessaire : le
  magasin Identity actuel (`AddIdentityCore<ApplicationUser>()...AddEntityFrameworkStores<
  PlateformeDbContext>()`) porte déjà cette capacité, simplement non exploitée jusqu'ici.
- **Rationale** : exploite au maximum Identity déjà en place (consigne explicite du brief) ; CPT-07
  est Could — aucune obligation (Principe III, ne pas déguiser un Could en Must).
- **Alternatives considérées** : bibliothèque tierce de 2FA — rejetée (Identity couvre déjà ce
  besoin nativement ; dépendance supplémentaire injustifiée, Principe IV).
- **Alignement post-remédiation (2026-08-29, `post-implementation-audit.md` §11)** : le parcours
  décrit initialement ci-dessus ne couvrait que l'*activation* du second facteur
  (`ResetAuthenticatorKeyAsync` + `SetTwoFactorEnabledAsync`), pas son *exigibilité réelle* à la
  connexion. L'audit a démontré que `Connexion.razor` utilisait `CheckPasswordSignInAsync` (ne
  vérifie que le mot de passe) suivi d'un `SignInAsync` inconditionnel — la 2FA activée n'était
  jamais réellement exigée. Remédiation, toujours 100 % Identity natif, sans dépendance
  supplémentaire : `SignInManager.PasswordSignInAsync` (détecte `SignInResult.RequiresTwoFactor`) →
  redirection vers une page dédiée `/comptes/verifier-code` → `SignInManager.
  TwoFactorAuthenticatorSignInAsync` pour valider le code TOTP avant de finaliser la connexion.
  Prouvé par 5 tests HTTP réels via `WebApplicationFactory` (`ParcoursConnexion2FaTests`, cas A→E) —
  pas seulement les préconditions Identity.

## D10 — Désactivation d'un utilisateur rattaché

- **Décision** : la désactivation est portée par `UtilisateurSociete.EstActif = false` — jamais par
  une suppression, jamais par `ApplicationUser.LockoutEnd`/suppression Identity. L'authentification
  vérifie, en plus des contrôles Identity standards, le rattachement société actif : un
  `UtilisateurSociete.EstActif == false` bloque l'accès aux fonctionnalités du compte société sans
  désactiver le compte Identity lui-même. L'historique (audit, actions passées) n'est jamais
  supprimé.
- **Rationale** : cohérent avec `ApplicationUser.EstActif` déjà existant en Feature 001 (utilisé pour
  les agents) — même style de « drapeau actif », jamais de suppression physique (Principe VIII,
  SC-005 de `spec.md`).
- **Alternatives considérées** : `LockoutEnd = DateTimeOffset.MaxValue` (verrouillage Identity natif)
  comme mécanisme **principal** — rejeté : verrouillerait le compte Identity globalement, alors que
  la désactivation ENT-02 est un acte de l'administrateur de société, borné à SA société ;
  `UtilisateurSociete.EstActif` porte la bonne granularité.

## D11 — Évolution nécessaire de Feature 001 (limitée, additive, sans régression)

- **Décision (mise à jour post-remédiation, 2026-08-29)** : trois extensions strictement additives
  de `Dossier.Creer(...)` (paramètres optionnels, valeur par défaut `null`, tous les appels de test
  existants — signature à 3 arguments — restent valides sans modification) :
  1. `Guid? auteurId = null` → propriété `AuteurId` (D5, CLAR-01, dès l'implémentation initiale).
  2. `TypeClient? typeClient = null` → propriété `TypeClient` (D3, CPT-06, ajouté lors de la
     remédiation post-audit — `post-implementation-audit.md` §8).
  3. `.AddDefaultTokenProviders()` ajouté à la configuration Identity de Feature 001
     (`DependencyInjection.cs`) — extension additive de la capacité globale d'Identity (aucun
     fournisseur de jeton n'était enregistré, CPT-03/CPT-07 en avaient besoin ; ajouté lors de la
     remédiation post-audit, `post-implementation-audit.md` §9). Aucun comportement existant de
     Feature 001 n'en dépendait ni n'est modifié.

  **Aucune autre modification de Feature 001** : `CloisonnementService`, `IAutorisationService`,
  `HabilitationUtilisateur`, `AutorisationService` restent inchangés en comportement ; `Permission`
  est uniquement **complétée** de nouvelles constantes (déjà le style établi — voir `CompetenceGerer`
  ajouté lors de la remédiation post-implémentation de Feature 001).
- **Rationale** : chaque extension répond à un besoin déjà spécifié (CLAR-01, CPT-06, CPT-03/07),
  jamais une règle métier nouvelle ; toute autre tentation d'extension a été examinée et écartée
  (D4, D6) au profit d'entités Feature 002 séparées.
- **Alternatives considérées** : voir D2, D4, D6 pour le détail par cas rejeté.

## D12 — Stratégie de tests

- **Décision** : réutilisation intégrale de la structure à 4 projets de tests déjà en place
  (`CIT.Plateforme.Domain.Tests`, `Application.Tests` — EF Core InMemory, `Infrastructure.Tests` —
  LocalDB réel via `BaseDeTestSql`, `Web.Tests` — `PlateformeWebApplicationFactory` + bUnit), aucun
  nouveau projet. Tests Feature 002 regroupés sous un dossier `Comptes/` dans chaque projet
  existant. Tests de concurrence réels sur `SocieteCliente` (dernier administrateur, D6) exécutés
  contre LocalDB (`Infrastructure.Tests`), à l'image du test de prise en charge concurrente de
  Feature 001. La portion CPT-04 de REC-SCE-14 est rejouée en `Web.Tests` avec deux sociétés/
  utilisateurs de test distincts. Les 7 Must sans scénario CDC applicable (CPT-01, 02, 03, 05,
  ENT-01, 02, 03) reçoivent chacun un **TEST-SPEC** dérivé strictement de leurs critères
  d'acceptation dans `spec.md` — jamais un scénario REC-SCE inventé.
- **Rationale** : aucune nouvelle infrastructure de test nécessaire (Principe IV, YAGNI) ; cohérence
  complète avec la méthode déjà validée à la clôture de Feature 001.
- **Alternatives considérées** : nouveau projet `CIT.Plateforme.Comptes.Tests` dédié — rejeté
  (fragmenterait une base de tests déjà organisée par couche, pas par feature).
