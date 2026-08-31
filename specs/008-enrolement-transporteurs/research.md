# Research — Feature 008 (Enrôlement Transporteurs)

> **Document** : `specs/008-enrolement-transporteurs/research.md`
> **Date** : 31 août 2026
> **Entrée** : `spec.md` (CLARIFIED), `checklists/requirements.md` (READY FOR /speckit.plan)
> **Méthode** : chaque décision est confrontée au code réel F001/F002/F006 avant d'être retenue — aucune abstraction n'est proposée sans vérification que le socle ne la fournit pas déjà.

---

## D1 — Représentation du Transporteur/Chauffeur (CLAR-ENR-01)

**Decision** : Le Transporteur/Chauffeur est représenté comme un `ApplicationUser` standard, `NatureUtilisateur.Client`, **sans société** (`SocieteCliente` non créée pour lui). `Dossier.ClientId` = l'`Id` de cet `ApplicationUser` directement ; `Dossier.AuteurId` reste `null` (aucune distinction auteur/propriétaire nécessaire puisqu'il n'y a qu'une seule personne).

**Rationale** :
- CLAR-ENR-01 (spec.md §3, RÉSOLU) établit que Transporteur = Chauffeur = une seule personne physique, sans société.
- `Dossier.ClientId` (`src/CIT.Plateforme.Domain/Tickets/Dossier.cs:19`) est un `Guid` **sans contrainte FK vers `SocieteCliente`** (vérifié dans `PlateformeDbContext.cs:171-181` : aucune relation EF configurée). `CloisonnementService.ClientEstProprietaireDuTicketAsync` (`src/CIT.Plateforme.Application/Securite/CloisonnementService.cs:14-24`) ne fait qu'un test d'égalité `dossier.ClientId == clientId` — il n'exige jamais que `clientId` référence une `SocieteCliente`.
- `ObtenirContexteUtilisateurQuery.cs:83` contient déjà la clause `if (userSoc is not null || userInfo.Nature == NatureUtilisateur.Client) espaces.Add(EspaceFonctionnel.Client)` — **le socle anticipe déjà un utilisateur `Client` sans société rattachée** et lui attribue correctement `EspaceFonctionnel.Client`. Aucune modification requise dans `ObtenirContexteUtilisateurQuery`, `ContexteUtilisateurVue`, ni `EspaceFonctionnel`.
- `NatureUtilisateur` (`Domain/Securite/NatureUtilisateur.cs`) n'a que deux valeurs (`Client`, `Agent`) — le Transporteur/Chauffeur est un `Client` au sens de ce socle (demandeur externe), l'Agent Gate un `Agent`. Aucune troisième valeur n'est nécessaire.

**Alternatives rejetées** :
- *Créer une `SocieteTransporteur` (analogue à `SocieteCliente`)* : rejeté — CLAR-ENR-01 exclut explicitement toute structure organisationnelle ; introduirait un cycle de validation CPT-01-like absent du texte ENR.
- *Ajouter une `NatureUtilisateur.Transporteur`* : rejeté — aucune divergence de comportement avec `Client` n'est requise par ENR-01→05 ; ajouterait une branche jamais exploitée (YAGNI).

**Impact** : Aucune nouvelle entité `Comptes`, aucune modification de `NatureUtilisateur`, `EspaceFonctionnel`, `ObtenirContexteUtilisateurQuery`. F008 crée un `ApplicationUser` (Identity, mécanisme F002 générique) au premier enrôlement ou réutilise un compte existant — décision d'inscription (auto-inscription vs création CIT) laissée à `/speckit.tasks` comme détail d'implémentation, non structurante pour le modèle.

---

## D2 — Le Dossier/Ticket comme unique objet enrôlé (CLAR-ENR-02)

**Decision** : Un seul `Ticket` (Module = `"Enrolement"`) par démarche d'enrôlement, rattaché à un `Dossier` créé pour l'occasion (pas de réutilisation de dossier existant — chaque enrôlement est un sujet nouveau, à la différence de Billing où plusieurs BL peuvent partager un dossier).

**Rationale** : CLAR-ENR-02 confirme qu'il n'existe qu'un seul acteur = objet enrôlé. Aucune notion de "dossier multi-tickets" n'est décrite par ENR-01→05 (à la différence d'IMP-01/02 multi-BL). `CreerTicketCommand` (générique F001) accepte déjà `DossierExistantId: Guid?` optionnel — F008 n'utilise jamais ce paramètre, ce qui est un usage standard déjà supporté nativement (`SoumettreReclamationCommand` fait de même : il crée toujours un nouveau dossier).

**Alternatives rejetées** : *Regrouper plusieurs enrôlements du même chauffeur (ex. renouvellement badge, ENR-05) dans le même dossier* — non sourcé par le CDC ; différé sans décision arbitraire, cf. D8.

---

## D3 — Permissions RBAC dédiées F008

**Decision** : Deux nouvelles constantes dans `Domain.Securite.Permission` :
- `EnrolementSoumettre = "enrolement.soumettre"` — Transporteur/Chauffeur, soumission (ENR-01).
- `EnrolementInstruire = "enrolement.instruire"` — Agent Gate, **permission unique bundlée** couvrant prise en charge, vérification/messages, demande de complément, validation, convocation, constatation de remise de badge.

**Rationale** — deux précédents examinés dans le code réel, choix tranché entre eux :
1. **Billing Import/Export** : une permission **par étape distincte** (`BillingImportManifesteInstruire`, `...FacturationEditer`, `...PaiementValider`, `...ComptabiliteConfirmerLettrage`, `...ControleDocumentaireTraiter`) — justifié parce que ces étapes sont réellement instruites par des **rôles CIT différents** (Pilote Manifeste, Caisse, Comptabilité, Contrôle Documentaire — PC-03).
2. **Customer Complaint** : une **permission bundlée unique** `ComplaintInstruire`, réutilisée telle quelle par `PrendreEnChargeCommand`, `AttenteClientCommand`, `PublierMessageCommand` (branches `if (ticket.Module == "CustomerComplaint")`, vérifiées dans le code) et par la commande dédiée `CloturerReclamationCommand` — justifié parce que REC-02 ne décrit qu'un seul acteur "Support Opérationnel", jamais plusieurs rôles distincts.

ENR-02/03 décrit un **acteur unique "Agent Gate"** pour vérifier, demander un complément, valider, convoquer et constater la remise du badge — structurellement identique au cas Customer Complaint, pas à Billing. Le précédent Customer Complaint est donc appliqué, conformément à l'instruction de clarification qui pointait déjà IMP-04/REC-03 comme référence pour CLAR-ENR-03.

**Alternatives rejetées** : *Réutiliser directement `Permission.TicketPrendreEnCharge`/`TicketCloturer` sans rien créer* — rejeté après examen : cela mélangerait, sous une même permission générique, le "ramassage" de n'importe quel ticket socle et l'action physiquement sensible de remise de badge (accès au terminal portuaire) ; une permission dédiée nommée reste plus auditable pour un Admin fonctionnel qui doit accorder ce droit précisément, sans sur-accorder un droit générique valable sur tous les modules.

**Impact code (branches à ajouter, mécanisme déjà existant, jamais recréé)** :
- `PrendreEnChargeCommand.cs:25-32` : passer de 2 branches (`if CustomerComplaint / else`) à 3 (`if CustomerComplaint / else if Enrolement / else`).
- `AttenteClientCommand.MettreEnAttenteAsync` (`Tickets/AttenteClientCommand.cs:23-30`) : même extension à 3 branches (utilisée pour ENR-02 "demande de complément").
- `Messagerie/PublierMessageCommand.cs` : `PublierMessageAgentAsync` (ligne 30) et `PublierNoteInterneAsync` (ligne 48), même extension à 3 branches. `PublierMessageClientAsync` (ligne 20-24) **ne change pas** — générique via `CloisonnementService`, fonctionne déjà pour tout module dont `Dossier.ClientId` est correctement renseigné (cf. D1, et le correctif transversal déjà appliqué en F006 closure).
- `Tickets/ConsulterFileQuery.cs:15-16` : ajouter un 3ᵉ test `peutEnrolement = await autorisation.EstAutoriseAsync(agentId, Permission.EnrolementInstruire, fileId, ct)`, étendre la condition de refus à `!peutStandard && !peutComplaint && !peutEnrolement`.

**Ce qui NE change PAS** (réutilisation confirmée à 100%, zéro modification) :
- `ActionsWorkflowStandardCommand` n'est pas utilisé par F008 (voir D5 — F008 utilise ses propres commandes dédiées "Valider"/"Convoquer"/"Constater badge", à l'image de `CloturerReclamationCommand`, pas le générique non modulé). Aucune modification de ce fichier.
- `CreerTicketCommand`, `TransfererTicketCommand`, `ReaffecterTicketCommand`, `LierTicketsCommand`, `ModifierPrioriteCommand`, `ReouvrirTicketCommand`, `AnnulerTicketCommand` : génériques, aucune branche modulaire à ajouter, utilisables tels quels si besoin futur (aucun n'est requis par ENR-01→05 Must).

---

## D4 — Fichier/File "Agent Gate"

**Decision** : Une nouvelle `FileDeTraitement` : `FileDeTraitement.Creer("Enrolement", "Agent Gate")` (Module = `"Enrolement"`, Nom = `"Agent Gate"`), donnée de configuration pure — pas de nouveau type ni de nouvelle capacité.

**Rationale** : `FileDeTraitement.Creer` (F001, générique) est déjà appelée avec des couples `(Module, Nom)` distincts pour chaque file existante (ex. `FileDeTraitement.Creer("Billing Office", f)` dans `DevelopmentSeeder.cs:149`, `"CustomerComplaint"`/`"Support Opérationnel"` pour F006). F008 suit ce même mécanisme, sans rien y ajouter.

**Constante applicative** : `SoumettreEnrolementCommand.FileAgentGate = "Agent Gate"` (mirroring `SoumettreDemandeImportCommand.FilePiloteManifeste`, `SoumettreReclamationCommand` implicite via `CategorieReclamation.FileCibleDefautId`).

---

## D5 — Commandes dédiées vs générique non modulé

**Decision** : F008 introduit 4 commandes dédiées, toutes structurées à l'image de `CloturerReclamationCommand` (module-check explicite + permission `EnrolementInstruire` + historique/audit nommé) :

| Commande | Étape ENR/REC-SCE-08 | Rôle |
|:---|:---|:---|
| `SoumettreEnrolementCommand` | ENR-01, étape 1 | Créer Dossier + Ticket (Module=Enrolement) |
| `ValiderEnrolementCommand` | ENR-02, étape 3 (après complément éventuel via `ActionsWorkflowStandardCommand`... **non**, voir note) | Enregistrer la décision de validation |
| `DeclencherConvocationCommand` | ENR-03, étape 4 | Enregistrer l'événement de convocation |
| `ConstaterRemiseBadgeCommand` | ENR-03, étape 5 | Constater la remise physique du badge → clôturer le ticket |

**Note sur la demande de complément (étape 2)** : réutilise **directement** `AttenteClientCommand.MettreEnAttenteAsync` / `RependreApresReponseAsync` (D3, branche ajoutée) — **aucune commande dédiée créée pour cette étape**, conformément à CLAR-ENR-03 (RÉSOLU : réutilisation intégrale du socle générique F001, aucun second moteur).

**Rationale pour `ValiderEnrolementCommand` (nouvelle, pas `ActionsWorkflowStandardCommand`)** : `ActionsWorkflowStandardCommand.ValiderAsync` (`Tickets/ActionsWorkflowStandardCommand.cs:18-19`) est **non modulé** — il vérifie uniquement `Permission.TicketPrendreEnCharge`, jamais `ComplaintInstruire` ni (hypothétiquement) `EnrolementInstruire`. F006 ne l'utilise d'ailleurs pas non plus (sa validation est fondue dans `CloturerReclamationCommand`). Réutiliser ce générique pour F008 obligerait à accorder à l'Agent Gate la permission `TicketPrendreEnCharge` **en plus** de `EnrolementInstruire` (deux permissions pour un seul rôle, incohérent avec D3). Une commande dédiée de 15 lignes (même structure que `CloturerReclamationCommand`) résout cette incohérence sans dupliquer de règle métier — elle ne fait qu'enregistrer une décision et vérifier une permission, exactement le rôle du socle `ActionsWorkflowStandardCommand`, mais correctement modulée.

**Alternative rejetée** : *Modifier `ActionsWorkflowStandardCommand` pour le rendre modulé (3 branches comme D3)* — rejeté : ce type est utilisé par les modules Billing avec des permissions granulaires différentes par action (D3), le rendre "Enrolement-aware" spécifiquement casserait sa généricité pour un seul module alors qu'une petite commande dédiée suffit et suit un patron déjà validé (F006).

**`RejeterAsync` d'`ActionsWorkflowStandardCommand`** : non utilisé par F008 (CLAR-ENR-04, RÉSOLU — rejet hors périmètre). Reste disponible pour d'autres modules, inchangé.

---

## D6 — Convocation et Badge : événements historique, aucun nouvel état `StatutTicket`

**Decision** : Aucune modification de `Domain/Tickets/StatutTicket.cs`. Les étapes "Vérification", "Validation", "Convocation" restent toutes à l'intérieur de `StatutTicket.EnCours` (avec un passage transitoire par `EnAttenteClient` pendant un complément) ; seules deux transitions réelles de statut existent : `EnAttenteDePriseEnCharge → EnCours` (prise en charge, générique) et `EnCours → Cloture` (constatation de la remise du badge).

**Rationale** : Vérifié sur `Ticket.cs` — même les workflows Billing Import/Export à 6-9 étapes (facturation, paiement, lettrage, contrôle documentaire) ne créent **aucun** état `StatutTicket` intermédiaire : ils tracent leurs jalons exclusivement via `IJournalisationService.ConsignerHistoriqueAsync` (visible dans `PrendreEnChargeCommand`, `CloturerReclamationCommand`, etc. — aucun ne passe de date explicite, l'horodatage est géré par le journal lui-même). F008 suit strictement ce même patron : "ConvocationDeclenchee" et "BadgeRemisEtDossierCloture" sont des libellés d'événement historique, pas des colonnes ni des valeurs d'enum.

**Impact** : `Ticket.cs` (Domain) n'est **pas modifié** par F008 — aucune propriété additive requise (contrairement à F003/F004/F006 qui avaient chacune ajouté des colonnes nullable). C'est le résultat direct de CLAR-ENR-05/06 (badge et convocation = événements tracés, jamais des objets/états à part entière).

---

## D7 — Documents (ENR-01, PC-08)

**Decision** : Réutilisation intégrale et sans extension du mécanisme `DeposerDocumentCommand`/`Document`/`IDocumentRepository`/`ReglesDepotDocument` (F001 générique, déjà utilisé sans modification par F003/F004/F006). `DeposerPourClientAsync` (générique, vérifie `CloisonnementService.ExigerProprieteClientAsync`) est utilisé tel quel par le Transporteur/Chauffeur.

**Rationale** : `ReglesDepotDocument` est déjà un objet de configuration passé par l'appelant (pas figé dans le socle) — la "liste des pièces paramétrable" d'ENR-01 se résout donc **sans aucune extension**, en fournissant simplement les règles (types acceptés, taille max) au moment de l'appel depuis la couche Application F008, exactement comme le font déjà les autres modules. PC-08 (liste exacte) reste "à confirmer avant implémentation" — cette clarification ne bloque ni la conception ni le plan : la mécanique est déjà générique et paramétrable par construction.

**Alternative rejetée** : *Créer une entité `TypeDocumentEnrolement` ou une liste figée en dur* — rejeté explicitement par l'instruction de cette passe de plan (§6) et par PC-08 lui-même.

---

## D8 — Informations d'enrôlement (ENR-01, "informations paramétrables")

**Decision** : Réutilisation des colonnes génériques déjà présentes sur `Ticket` (`Objet`, `Description`, ajoutées en F006 mais nommées de façon générique, pas spécifiques à "Reclamation") via une nouvelle méthode de domaine `Ticket.DefinirInformationsEnrolement(string objet, string description)` — même signature et mêmes garde-fous que `DefinirContenuReclamation` (objet/description non vides), mais nommée pour son usage réel.

**Rationale** : Les propriétés `Ticket.Objet`/`Ticket.Description` (`Ticket.cs:111,117`) sont des `string?` génériques, sans lien de nommage ou de contrainte avec Customer Complaint dans leur définition EF — les réutiliser pour F008 est une **extension additive de méthode de domaine**, pas une extension de schéma (aucune nouvelle colonne SQL). Cela évite d'inventer une structure de champs paramétrables complexe (formulaire dynamique) alors que CLAR-ENR-02 confirme que la liste précise n'est pas fixée par le CDC — un couple objet/description libre suffit pour capturer "les informations nécessaires" sans préjuger de leur contenu exact, à charge pour `/speckit.tasks`/implémentation de préciser le RGPD/format si nécessaire.

**Alternative rejetée** : *Créer une entité satellite `InformationEnrolement` clé/valeur pour un vrai paramétrage dynamique* — jugé disproportionné tant que PC-08/CLAR-ENR-02 ne fixent aucune liste ; reporté comme option à réévaluer en `/speckit.tasks` uniquement si l'implémentation détaillée le justifie, non retenu ici (YAGNI, Principe IV).

---

## D9 — Lister "mes enrôlements" (nouvelle méthode de repository générique)

**Decision** : Ajout de `IDossierRepository.ListerParClientAsync(Guid clientId)` — méthode **générique** (pas spécifique à Enrolement), complémentaire de `ListerParSocieteAsync(Guid societeId)` déjà existante.

**Rationale** : `IDossierRepository` (`Abstractions/Repositories.cs:42-49`) n'expose aujourd'hui que `ListerParSocieteAsync`, adapté aux modules où le propriétaire est toujours une société. F008 a besoin de lister les dossiers d'un `ClientId` qui n'est PAS une société (D1). Plutôt que d'ajouter une méthode nommée "Enrolement", la méthode est nommée génériquement `ListerParClientAsync` — cohérent avec le fait que `Dossier.ClientId` est déjà un concept générique (D1), et réutilisable par un futur module individuel sans société. Combinée à `ITicketRepository.ListerParDossiersAsync(IEnumerable<Guid>)` (déjà existante, générique), elle fournit "Mes enrôlements" sans nouvelle capacité de requête ticket.

**Alternative rejetée** : *Ajouter `ITicketRepository.ListerParClientDirectAsync`* — rejeté : le socle distingue déjà Dossier (propriété) et Ticket (unité de travail) ; passer par les dossiers du client puis leurs tickets respecte cette séparation existante (identique au chemin `ListerParSocieteAsync` + `ListerParDossiersAsync` déjà utilisé pour les sociétés).

---

## D10 — Absence de rejet (CLAR-ENR-04)

**Decision** : Aucune commande de rejet n'est créée pour F008. `Ticket.RejeterImport`/`RejeterExport` (Domain, spécifiques à Billing) ne sont ni réutilisées ni étendues à Enrolement.

**Rationale** : CLAR-ENR-04 (RÉSOLU) — confirmé par recherche exhaustive et négative dans le CDC. Le cycle de vie de F008 ne comporte que 2 issues : progression vers clôture normale, ou blocage indéfini en attente de complément (le CDC ne prévoit pas d'abandon automatique pour Enrolement — à la différence de `ClorePourAbsenceReponseAsync`, générique et disponible si un besoin futur confirmé l'exige, mais non activé/exposé par F008 sans nouvelle clarification).

---

## D11 — Sécurité : anti-IDOR et ownership

**Decision** : Aucun mécanisme de sécurité nouveau — réutilisation stricte des 3 gardes-fous déjà éprouvés (F002/F006) :
1. **Identité serveur uniquement** : chaque commande F008 reçoit `appelantId`/`agentId` depuis le contexte authentifié (`ClaimsPrincipal` → couche Web), jamais depuis une valeur soumise par le client. Aucune UI F008 ne transmet de GUID libre (cohérent avec le principe déjà appliqué à `SoumettreReclamationCommand`, `PublierMessageClientAsync`).
2. **Cloisonnement demandeur** : `CloisonnementService.ExigerProprieteClientAsync(clientId, ticketId)` (générique, non modifié) protège la consultation/réponse/dépôt de document du Transporteur/Chauffeur.
3. **Habilitation Agent Gate côté serveur** : chaque commande Agent Gate vérifie `IAutorisationService.ExigerAsync(agentId, Permission.EnrolementInstruire, ticket.FileCouranteId)` — jamais une vérification uniquement côté UI (Constitution Principe VII).

**Tests négatifs prévus** (détaillés dans `contracts/use-cases.md` et `quickstart.md`) : Transporteur A ne peut pas consulter/répondre au dossier de Transporteur B (403) ; utilisateur sans `EnrolementInstruire` ne peut ni prendre en charge, ni valider, ni convoquer, ni constater un badge (403, vérifié serveur même si l'UI masquerait le bouton) ; utilisateur non authentifié redirigé à la connexion sur toute route Enrolement.

---

## D12 — CLAR-ENR-07 (expiration badge, ENR-05/Could)

**Decision** : Non traité par ce plan. ENR-05 (Could) reste hors du périmètre `/speckit.tasks` immédiat — aucune entité "expiration badge" ni tâche de rappel automatique n'est produite ici. Si retenue ultérieurement, elle nécessitera sa propre clarification (durée) avant conception, conformément à CLAR-ENR-07 ("À CONFIRMER AVANT IMPLEMENTATION").

---

## Synthèse des décisions

| # | Décision | Nouveau code Domain ? | Nouveau code Application ? | Nouvelle table/colonne SQL ? |
|:---|:---|:---:|:---:|:---:|
| D1 | Transporteur/Chauffeur = ApplicationUser, Client, sans société | Non | Non | Non |
| D2 | 1 Ticket = 1 Dossier par enrôlement | Non | Non | Non |
| D3 | 2 permissions dédiées + 4 branches modulaires | Non | Oui (branches) | Non |
| D4 | File "Agent Gate" (donnée) | Non | Non (seed) | Non (ligne de donnée) |
| D5 | 4 commandes Application dédiées | Non | Oui | Non |
| D6 | Aucun nouvel état StatutTicket | Non | Non | Non |
| D7 | Documents 100% réutilisés | Non | Non | Non |
| D8 | `Ticket.DefinirInformationsEnrolement` (réutilise Objet/Description) | Oui (1 méthode) | Non | Non |
| D9 | `IDossierRepository.ListerParClientAsync` | Non | Oui (interface + impl) | Non |
| D10 | Aucun rejet | Non | Non | Non |
| D11 | Sécurité 100% réutilisée | Non | Non | Non |
| D12 | ENR-05/CLAR-ENR-07 différés | — | — | — |

**Constat net** : F008 est la première feature depuis F001 dont l'implémentation ne nécessite **aucune migration EF Core** au sens "nouvelle colonne" — uniquement des lignes de données de configuration (File, Permissions) et une nouvelle méthode de Domain sans nouvelle propriété. Ceci confirme le degré de réutilisation visé par la Constitution (Principe V) et par CLAR-ENR-01→04.
