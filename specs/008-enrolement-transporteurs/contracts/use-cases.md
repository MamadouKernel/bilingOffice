# Contrats Applicatifs — Feature 008 (Enrôlement Transporteurs)

> **Document** : `specs/008-enrolement-transporteurs/contracts/use-cases.md`
> **Date** : 31 août 2026
> **Convention** : chaque contrat précise s'il s'agit d'une commande **RÉUTILISÉE telle quelle** (aucun code F008), **ÉTENDUE** (branche modulaire ajoutée à une commande générique existante), ou **NOUVELLE** (commande dédiée F008, à l'image de `CloturerReclamationCommand`).

---

## C8-01 — Soumettre un enrôlement (NOUVELLE)

| Champ | Valeur |
|:---|:---|
| **Commande** | `SoumettreEnrolementCommand` (nouvelle, `Application/Enrolement/`) |
| **Acteur** | Transporteur/Chauffeur (`ApplicationUser`, `Nature=Client`) |
| **Permission** | `Permission.EnrolementSoumettre` (globale, non bornée à une file) |
| **Préconditions** | Utilisateur authentifié et actif ; liste de pièces/informations configurée (précondition REC-SCE-08 texte exact) |
| **Entrée** | `SoumettreEnrolementRequete(string Objet, string Description)` + dépôt de pièces séparé (voir C8-06) |
| **Sortie** | `SoumettreEnrolementResultat(Guid TicketId, string NumeroTicket)` |
| **Changements d'état** | Création `Dossier` (`ClientId=appelantId`, `AuteurId=null`) ; création `Ticket` (`Module="Enrolement"`, `TypeDemande="Enrolement"`, `Statut=EnAttenteDePriseEnCharge`, `FileCouranteId=`file "Agent Gate") ; `Ticket.DefinirInformationsEnrolement(Objet, Description)` |
| **Audit** | Historique `"TicketCree"` (générique) + `"EnrolementSoumis"` (détail) ; Audit `TicketCree` |
| **Erreurs** | `AccesRefuseException` si permission absente ; `ArgumentException` si `Objet`/`Description` vides |
| **Cloisonnement** | Aucun — création, propriétaire = appelant par construction |
| **Exigence ENR** | ENR-01 (Must) |
| **Étape REC-SCE-08** | 1. "Dossier à distance" |
| **Test prévu** | Unitaire (permission refusée, champs vides) + intégration EF réelle (ticket créé, visible en file Agent Gate) + bUnit (formulaire de soumission, dépôt de pièce inclus) |

---

## C8-02 — Prendre en charge un dossier d'enrôlement (ÉTENDUE)

| Champ | Valeur |
|:---|:---|
| **Commande** | `PrendreEnChargeCommand` (F001, générique — branche `Module=="Enrolement"` ajoutée) |
| **Acteur** | Agent Gate |
| **Permission** | `Permission.EnrolementInstruire`, bornée à la file "Agent Gate" |
| **Préconditions** | Ticket `EnAttenteDePriseEnCharge` |
| **Entrée** | `(Guid operateurId, Guid ticketId)` (signature inchangée) |
| **Sortie** | — (void), `Ticket.Statut → EnCours` |
| **Changements d'état** | `Ticket.PrendreEnCharge` (générique, jeton de concurrence optimiste EF Core — garantit qu'une seule prise en charge concurrente réussit, réutilisation directe du mécanisme déjà testé F001) |
| **Audit** | Historique `"TicketPrisEnCharge"` (générique, inchangé) |
| **Erreurs** | `AccesRefuseException` (permission) ; `InvalidOperationException` (statut incorrect ou conflit de concurrence) |
| **Cloisonnement** | Habilitation Agent Gate vérifiée côté serveur sur `ticket.FileCouranteId` |
| **Exigence ENR** | ENR-02 (Must, précondition à l'instruction) |
| **Étape REC-SCE-08** | 2 (implicite — préalable à "Complément"/"Validation") |
| **Test prévu** | Réutilise directement la suite de tests existante de `PrendreEnChargeCommand` (F001) + 1 cas négatif dédié : agent sans `EnrolementInstruire` refusé sur un ticket Enrolement |

---

## C8-03 — Vérifier / échanger sur un dossier (ÉTENDUE)

| Champ | Valeur |
|:---|:---|
| **Commande** | `PublierMessageCommand.PublierMessageAgentAsync` / `PublierNoteInterneAsync` (F001, branche `Module=="Enrolement"` ajoutée) ; `PublierMessageClientAsync` **inchangée** (générique via `CloisonnementService`) |
| **Acteur** | Agent Gate (message externe ou note interne) ; Transporteur/Chauffeur (message externe, réponse) |
| **Permission** | Agent : `Permission.EnrolementInstruire` (message externe) — note interne : idem (bundlée, cf. research.md D3) ; Client : `CloisonnementService.ExigerProprieteClientAsync` |
| **Préconditions** | Ticket existant, appartenant au module Enrolement pour le branchement agent |
| **Entrée** | `(Guid auteurId, Guid ticketId, string contenu)` |
| **Sortie** | `Message` créé |
| **Changements d'état** | Aucun changement de `StatutTicket` |
| **Audit** | Historique `"MessagePublie"` (générique) |
| **Erreurs** | `AccesRefuseException` (permission ou cloisonnement) |
| **Cloisonnement** | Client : dossier propriétaire uniquement. Agent : file habilitée uniquement |
| **Exigence ENR** | ENR-02 (support de la vérification) |
| **Étape REC-SCE-08** | 2 |
| **Test prévu** | Réutilise la suite `PublierMessageCommand` existante + 2 cas négatifs (client hors propriété, agent sans habilitation) sur module Enrolement |

---

## C8-04 — Demander un complément / répondre (ÉTENDUE — CLAR-ENR-03)

| Champ | Valeur |
|:---|:---|
| **Commande** | `AttenteClientCommand.MettreEnAttenteAsync` (demande, branche `Module=="Enrolement"` ajoutée) / `RependreApresReponseAsync` (réponse, générique, inchangée) |
| **Acteur** | Demande : Agent Gate. Réponse : déclenchée après dépôt de message/document du Transporteur/Chauffeur (voir C8-03/C8-06), pas d'action agent distincte |
| **Permission** | `Permission.EnrolementInstruire` pour `MettreEnAttenteAsync` |
| **Préconditions** | Ticket `EnCours` (demande) ; `EnAttenteClient` (réponse) |
| **Entrée** | `(Guid agentId, Guid ticketId, string motif)` |
| **Sortie** | — |
| **Changements d'état** | `Ticket.Statut: EnCours → EnAttenteClient` (demande) ; `→ EnCours` (réponse) ; `PeriodeAttenteClient` ouverte/fermée (SLA suspendu, TPS-04) |
| **Audit** | Historique `"TicketEnAttenteClient"` / `"ReponseClientRecue"` (génériques) |
| **Erreurs** | `AccesRefuseException` ; `InvalidOperationException` (statut incorrect) |
| **Cloisonnement** | Agent Gate habilité sur la file du ticket |
| **Exigence ENR** | ENR-02 (Must) |
| **Étape REC-SCE-08** | 2. "Complément" |
| **Test prévu** | Réutilise la suite `AttenteClientCommand` existante (déjà validée F003/F006) + cas Enrolement dédié (permission, cycle complet demande→réponse→reprise) |

---

## C8-05 — Valider un dossier d'enrôlement (NOUVELLE)

| Champ | Valeur |
|:---|:---|
| **Commande** | `ValiderEnrolementCommand` (nouvelle, structure identique à `CloturerReclamationCommand`) |
| **Acteur** | Agent Gate |
| **Permission** | `Permission.EnrolementInstruire` |
| **Préconditions** | `ticket.Module == "Enrolement"` (sinon `InvalidOperationException`) ; `Ticket.Statut == EnCours` |
| **Entrée** | `(Guid agentId, Guid ticketId)` |
| **Sortie** | — |
| **Changements d'état** | Aucun changement de `StatutTicket` (reste `EnCours` — validation est un jalon d'historique, cf. research.md D6, cohérent avec `ActionsWorkflowStandardCommand.ValiderAsync` qui ne change pas non plus le statut) |
| **Audit** | Historique `"EnrolementValide"` ; Audit `EnrolementValide` |
| **Erreurs** | `AccesRefuseException` ; `InvalidOperationException` (module incorrect ou statut incorrect) |
| **Cloisonnement** | Agent Gate habilité sur la file du ticket |
| **Exigence ENR** | ENR-02 (Must) |
| **Étape REC-SCE-08** | 3. "Validation" |
| **Test prévu** | Unitaire (nominal, permission refusée, module incorrect) + intégration EF |

---

## C8-06 — Déposer une pièce (soumission ou complément) (RÉUTILISÉE telle quelle)

| Champ | Valeur |
|:---|:---|
| **Commande** | `DeposerDocumentCommand.DeposerPourClientAsync` (F001, générique, **aucune modification**) |
| **Acteur** | Transporteur/Chauffeur |
| **Permission** | `CloisonnementService.ExigerProprieteClientAsync` (générique) |
| **Préconditions** | Ticket appartenant au demandeur ; règles de dépôt (`ReglesDepotDocument`) fournies par la couche Application F008 au moment de l'appel (type/taille — configuration, pas figée en dur, PC-08) |
| **Entrée** | `(Guid clientId, Guid ticketId, string nomFichier, string typeContenu, Stream contenu, long tailleOctets, ReglesDepotDocument reglesEtape)` |
| **Sortie** | `Document` créé |
| **Changements d'état** | Aucun changement de `StatutTicket` |
| **Audit** | Historique `"DocumentDepose"` (générique) |
| **Erreurs** | `AccesRefuseException` (cloisonnement) ; `DepotDocumentRefuseException` (règles non respectées) |
| **Cloisonnement** | Dossier propriétaire uniquement |
| **Exigence ENR** | ENR-01 (soumission), ENR-02 (complément) |
| **Étape REC-SCE-08** | 1, 2 |
| **Test prévu** | Réutilise la suite `DeposerDocumentCommand` existante — aucun nouveau test de commande, seulement un test d'intégration bout-en-bout F008 (formulaire → pièce visible côté Agent Gate) |

---

## C8-07 — Déclencher la convocation (NOUVELLE — CLAR-ENR-03/06)

| Champ | Valeur |
|:---|:---|
| **Commande** | `DeclencherConvocationCommand` (nouvelle) |
| **Acteur** | Agent Gate |
| **Permission** | `Permission.EnrolementInstruire` |
| **Préconditions** | `ticket.Module == "Enrolement"` ; `Ticket.Statut == EnCours` (dossier validé — le CDC ne décrit pas de contrôle machine strict de séquencement au-delà du statut, cohérent avec le socle qui ne bloque pas non plus la séquence Vérification→Validation par un état dédié, research.md D6) |
| **Entrée** | `(Guid agentId, Guid ticketId)` |
| **Sortie** | — |
| **Changements d'état** | Aucun changement de `StatutTicket` — événement d'historique uniquement (research.md D6) |
| **Audit** | Historique `"ConvocationDeclenchee"` ; Audit `ConvocationDeclenchee` ; événement publié `"ConvocationDeclenchee"` (consommable par un futur module Notifications, CLAR-ENR-06 — **aucun canal implémenté ici**) |
| **Erreurs** | `AccesRefuseException` ; `InvalidOperationException` (module incorrect) |
| **Cloisonnement** | Agent Gate habilité sur la file du ticket |
| **Exigence ENR** | ENR-03 (Must) |
| **Étape REC-SCE-08** | 4. "Convocation photo/badge" |
| **Test prévu** | Unitaire (nominal, permission refusée) + intégration EF + vérification que l'événement publié ne contient aucune donnée de canal (SMS/WhatsApp absent, conforme à la frontière) |

---

## C8-08 — Constater la remise du badge et clôturer (NOUVELLE — CLAR-ENR-05)

| Champ | Valeur |
|:---|:---|
| **Commande** | `ConstaterRemiseBadgeCommand` (nouvelle) |
| **Acteur** | Agent Gate |
| **Permission** | `Permission.EnrolementInstruire` |
| **Préconditions** | `ticket.Module == "Enrolement"` ; `Ticket.Statut` ∉ `{Cloture, Annule}` (garde déjà portée par `Ticket.Cloturer`) |
| **Entrée** | `(Guid agentId, Guid ticketId)` |
| **Sortie** | — |
| **Changements d'état** | `Ticket.Cloturer(MotifClotureType.Normale, maintenantUtc)` — `Statut → Cloture`, `DateClotureUtc` renseignée (archivage automatique, SOC-09) |
| **Audit** | Historique `"BadgeRemisEtDossierCloture"` (nommage distinct de la clôture générique, pour traçabilité explicite de l'événement métier "badge remis" — mirroring `CloturerReclamationCommand`) ; Audit `BadgeRemisEtDossierCloture` |
| **Erreurs** | `AccesRefuseException` ; `InvalidOperationException` (module incorrect ou déjà clôturé/annulé) |
| **Cloisonnement** | Agent Gate habilité sur la file du ticket |
| **Exigence ENR** | ENR-03 (Must) |
| **Étape REC-SCE-08** | 5. "Clôture" |
| **Test prévu** | Unitaire (nominal, déjà clôturé, permission refusée, module incorrect) + intégration EF (vérifie `DateClotureUtc` renseignée, ticket non supprimé) |

---

## C8-09 — Consulter "Mes enrôlements" (ÉTENDUE — nouvelle méthode générique de repository)

| Champ | Valeur |
|:---|:---|
| **Commande** | Nouvelle query légère `ListerMesEnrolementsQuery` (Application), utilisant `IDossierRepository.ListerParClientAsync` (nouvelle, générique, research.md D9) + `ITicketRepository.ListerParDossiersAsync` (existante) |
| **Acteur** | Transporteur/Chauffeur |
| **Permission** | Implicite — l'appelant ne voit que ses propres dossiers par construction (`clientId = appelantId`, jamais un paramètre libre) |
| **Préconditions** | Utilisateur authentifié |
| **Entrée** | `(Guid appelantId)` |
| **Sortie** | `IReadOnlyList<Ticket>` filtrés `Module == "Enrolement"` |
| **Changements d'état** | Aucun (lecture seule) |
| **Audit** | Aucun (consultation) |
| **Erreurs** | Aucune (liste vide si aucun dossier) |
| **Cloisonnement** | Par construction : `clientId` dérivé du contexte authentifié, jamais transmis par le client (anti-IDOR, research.md D11) |
| **Exigence ENR** | Transverse (US2, support de ENR-02) |
| **Étape REC-SCE-08** | Support (non une étape du scénario elle-même) |
| **Test prévu** | Intégration EF (isolation entre deux Transporteurs distincts — cas cloisonnement explicite §21 spec.md) |

---

## C8-10 — Consulter la file "Agent Gate" (RÉUTILISÉE, extension mineure)

| Champ | Valeur |
|:---|:---|
| **Commande** | `ConsulterFileQuery` (F001, générique — ajout d'un 3ᵉ test de permission `EnrolementInstruire`) |
| **Acteur** | Agent Gate |
| **Permission** | `Permission.EnrolementInstruire` (nouveau test ajouté à côté de `TicketPrendreEnCharge`/`ComplaintInstruire` déjà présents) |
| **Préconditions** | — |
| **Entrée** | `(Guid agentId, Guid fileId, StatutTicket? filtreStatut)` |
| **Sortie** | `IReadOnlyList<Ticket>` |
| **Changements d'état** | Aucun |
| **Audit** | Aucun |
| **Erreurs** | `AccesRefuseException` si aucune des 3 permissions n'est accordée sur la file |
| **Cloisonnement** | File habilitée uniquement |
| **Exigence ENR** | Support de ENR-02/03 |
| **Étape REC-SCE-08** | Support |
| **Test prévu** | Réutilise la suite `ConsulterFileQuery` existante + 1 cas Enrolement (agent avec `EnrolementInstruire` seul, sans `TicketPrendreEnCharge`/`ComplaintInstruire`, doit accéder à la file Agent Gate) |

---

## Matrice de Traçabilité Complète — REC-SCE-08

| Étape REC-SCE-08 | Exigence ENR | Contrat applicatif | État Ticket | Acteur | Permission | Test prévu |
|:---|:---:|:---|:---|:---|:---|:---|
| 1. Dossier à distance | ENR-01 | C8-01 (+ C8-06 pour les pièces) | `EnAttenteDePriseEnCharge` | Transporteur/Chauffeur | `EnrolementSoumettre` | Intégration EF + bUnit soumission |
| 2. Complément | ENR-02 | C8-02, C8-03, C8-04 | `EnCours ↔ EnAttenteClient` | Agent Gate → Transporteur/Chauffeur | `EnrolementInstruire` (agent) / cloisonnement (client) | Suite réutilisée + cas Enrolement dédiés |
| 3. Validation | ENR-02 | C8-05 | `EnCours` (inchangé) | Agent Gate | `EnrolementInstruire` | Unitaire + intégration EF |
| 4. Convocation photo/badge | ENR-03 | C8-07 | `EnCours` (inchangé) | Agent Gate | `EnrolementInstruire` | Unitaire + intégration EF |
| 5. Clôture | ENR-03 | C8-08 | `EnCours → Cloture` | Agent Gate | `EnrolementInstruire` | Unitaire + intégration EF |

**Aucune étape du scénario contractuel ne reste sans implémentation prévue.**

---

## Récapitulatif des Contrats

| ID | Nom | Type |
|:---|:---|:---|
| C8-01 | Soumettre un enrôlement | NOUVELLE |
| C8-02 | Prendre en charge | ÉTENDUE |
| C8-03 | Vérifier / échanger | ÉTENDUE |
| C8-04 | Demander un complément / répondre | ÉTENDUE |
| C8-05 | Valider | NOUVELLE |
| C8-06 | Déposer une pièce | RÉUTILISÉE (zéro changement) |
| C8-07 | Déclencher la convocation | NOUVELLE |
| C8-08 | Constater la remise du badge / clôturer | NOUVELLE |
| C8-09 | Consulter "Mes enrôlements" | ÉTENDUE (nouvelle query légère + méthode repo générique) |
| C8-10 | Consulter la file Agent Gate | ÉTENDUE (1 test de permission ajouté) |

**4 commandes nouvelles, 5 commandes/queries étendues (branches ou tests de permission ajoutés à du code générique existant), 1 réutilisée à 100 % sans changement.** Aucune exigence ENR-01→03 (Must) ne reste sans contrat.
