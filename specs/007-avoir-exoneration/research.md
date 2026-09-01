# Recherche Technique & Décisions d'Architecture — Feature 007
## Module Avoir & Exonération

> **Document** : `specs/007-avoir-exoneration/research.md`  
> **Date** : 1er septembre 2026  
> **Source de référence** : Cahier des Charges Fonctionnel CIT v1.0 (§11.6, §4, §25), `docs/15-preflight-revise-feature-007.md`, code réel F001-F010.

---

## 1. Analyse des Décisions d'Architecture (D1 à D10)

### D1 — Frontière de Responsabilité : Dissociation Portail vs Finance Interne
- **Contexte** : Le CDC (§1, §4, §25 / PC-15) stipule que la plateforme est autonome et ne remplace pas les logiciels métiers comptables (IRIS) ni les circuits internes d'approbation financière.
- **Décision** : Le module F007 gère la relation client/agent sur le portail (soumission, collecte des pièces, instruction, suivi, échanges, enregistrement de la décision finale, notification et archivage). Les calculs comptables fins et les visas hiérarchiques physiques se déroulent **HORS APPLICATION**.
- **Conséquence** : Aucun moteur ERP comptable ni chaîne hiérarchique complexe (Finance N1/N2/DAF) n'est codé en dur dans le domaine CIT.

### D2 — Typage Strict des Demandes (`TypeDemande`)
- **Contexte** : `AVE-01` cite expressément *« une demande d'avoir ou une demande d'exonération »*.
- **Décision** : Deux types de demande stricts sont supportés : `"Avoir"` et `"Exoneration"`. La propriété `TypeDemande` de l'entité `Ticket` (F001) porte cette discrimination, le `Module` étant positionné à `"AvoirExoneration"`.
- **Conséquence** : 0 entité superflue pour le typage, réutilisation directe du socle `Ticket`.

### D3 — Modèle de Persistance & Extensions Additives de `Ticket`
- **Contexte** : Les demandes d'avoirs et d'exonérations partagent l'essentiel de leur cycle de vie avec les tickets génériques du socle (F001).
- **Décision** : F007 n'introduit **aucune nouvelle table SQL**. Elle enrichit l'entité `Ticket` existante de manière purement additive avec quelques champs optionnels :
  - `DecisionAvoirExo` (enum/string nullable : `Accorde`, `Rejete`)
  - `MotifRejetAvoirExo` (string nullable, obligatoire si Rejet)
  - `MontantConteste` (decimal nullable, montant en XOF)
  - `MontantAccorde` (decimal nullable, montant final validé)
  - `DateDecisionAvoirExoUtc` (DateTime nullable)
  - `DecideurAvoirExoId` (Guid nullable)
- **Conséquence** : Modèle relationnel ultra-léger, migration 100% additive, intégrité référentielle garantie.

### D4 — Cycle de Vie & Statuts du Socle F001
- **Contexte** : `AVE-02` impose des workflows configurables pour chaque type de demande.
- **Décision** : F007 réutilise les statuts existants de `StatutTicket` :
  - `Cree` (Demande soumise en attente de prise en charge)
  - `EnCours` (Dossier pris en charge et en cours d'instruction)
  - `EnAttenteClient` (Demande de pièces ou précisions complémentaires)
  - `Valide` (Décision d'Accord prononcée)
  - `Rejete` (Décision de Rejet prononcée)
  - `Cloture` (Ticket clôturé formellement)
- **Conséquence** : 0 état domaine artificiel inventé. L'affichage utilisateur (ex: badge *« Accordé »*) est assuré par la couche de présentation.

### D5 — Enregistrement de la Décision (Accord / Rejet Motivé — AVE-03)
- **Contexte** : `AVE-03` exige que la décision (accord ou rejet motivé) soit consignée avec ses documents et notifiée.
- **Décision** :
  - En cas d'**Accord** : Passage à `StatutTicket.Valide`, enregistrement de `MontantAccorde`, dépôt optionnel du document de décision (ex: note de crédit transmise par la Finance), clôture et émission de l'événement `AvoirAccorde` ou `ExonerationAccordee`.
  - En cas de **Rejet** : Le champ `MotifRejetAvoirExo` est **strictement obligatoire** (rejet avec exception métier si chaîne vide). Passage à `StatutTicket.Rejete`, dépôt optionnel d'une pièce explicative, clôture et émission de l'événement `AvoirRejete` ou `ExonerationRejetee`.

### D6 — Liaison Optionnelle au Ticket Parent (AVE-04)
- **Contexte** : `AVE-04` (Should) permet de lier une demande Avoir/Exo au ticket de parcours client concerné (ex: ticket de facturation Import/Export ou réclamation).
- **Décision** : Réutilisation directe de `LiaisonTicket` (F001 `SOC-10`) via `ILiaisonTicketRepository`. La commande de soumission vérifie que le ticket lié existe, appartient à la même société (`ClientId`), et crée la liaison sans altérer le ticket parent.

### D7 — Dépôt Manuel des Documents & Sécurité
- **Contexte** : `AVE-01` et `AVE-03` prévoient le dépôt de pièces justificatives à la soumission et de documents de décision à l'accord/rejet.
- **Décision** : Réutilisation directe du service documentaire F001 (`Document.Deposer`, `IDocumentRepository`). Contrôle MIME, taille unitaire max 10 Mo, immuabilité et hachage.
- **Règle absolue** : La plateforme ne génère **aucun document comptable automatique**. Les pièces sont produites par les services émetteurs (client ou Finance) et déposées manuellement dans le ticket.

### D8 — Raccordement Événementiel F009
- **Contexte** : `AVE-03` impose la notification du client.
- **Décision** : F007 publie exclusivement des instances de `EvenementSocle` (`DemandeAvoirSoumise`, `DemandeExonerationSoumise`, `AvoirAccorde`, `AvoirRejete`, `ExonerationAccordee`, `ExonerationRejetee`, `AvoirComplementDemande`) dans `JournalEvenements`.
- **Règle absolue** : Aucun appel direct à SMTP, SMS, WhatsApp ou Twilio dans F007. La consommation et la diffusion sont assurées par le moteur d'arrière-plan F009.

### D9 — Permissions RBAC Dédiées (PC-03 Résolu)
- **Contexte** : Résolution de PC-03 par attribution de permissions modulaires sans figer d'acteurs organisationnels.
- **Décision** : Définition de 2 constantes de permission dans `NatureUtilisateur` :
  - `Permission.AvoirExonerationInstruire = "avoirexoneration.instruire"`
  - `Permission.AvoirExonerationDecider = "avoirexoneration.decider"`

### D10 — Sécurité Multi-Société & Dérivation d'Identité
- **Contexte** : Isolation étanche multi-tenant (SEC-01).
- **Décision** : Le `DemandeurId` et `SocieteId` sont dérivés côté serveur via `IContexteUtilisateurWeb`. L'accès aux requêtes et commandes est strictement restreint aux membres de la même société ou aux agents porteurs des permissions adéquates.
