# Use Cases & Application Query Contracts: Consolidation UI/UX F001-F003

**Feature Branch**: `003-5-ui-ux-consolidation`  
**Date**: 2026-08-30  
**Phase**: `/speckit.plan`

---

## 1. Vue d'Ensemble des Contrats de Lecture

Les contrats ci-dessous définissent les cas d'usage de lecture (Queries) et d'agrégation nécessaires pour alimenter les composants d'interface sans jamais dupliquer la logique métier dans les fichiers Razor.

---

### Contrat C4-01 : `ObtenirContexteUtilisateurQuery`
- **Acteur** : Tout utilisateur connecté (Client, Collaborateur, Agent, Superviseur, Admin).
- **Entrée** : `Guid utilisateurId` (extrait du cookie de session Identity).
- **Autorisation** : Authentification valide requise.
- **Traitement** :
  1. Charge l'utilisateur Identity `ApplicationUser`.
  2. Recherche le rattachement société actif dans `IUtilisateurSocieteRepository`.
  3. Si rattaché à une société, charge le profil de la société (`ISocieteClienteRepository`) et vérifie que la société et le rattachement sont actifs.
  4. Récupère les permissions CIT et les files attribuées via `IAutorisationService`.
  5. Vérifie si l'utilisateur est superviseur (possède un périmètre de supervision).
  6. Compose la liste des espaces fonctionnels accessibles (`Client`, `AdministrateurSociete`, `Agent`, `Supervision`, `AdministrationCit`).
  7. Retourne `ContexteUtilisateurVue`.
- **Sortie** : `ContexteUtilisateurVue`.
- **Erreurs** : `UtilisateurInconnuException`, `AccesRefuseException` (si compte désactivé).
- **Source CDC** : `CPT-03`, `CPT-04`, `ENT-01`, `ENT-02`, `SEC-02`.

---

### Contrat C4-02 : `ObtenirDashboardClientQuery`
- **Acteur** : Utilisateur rattaché à un compte société actif.
- **Entrée** : `Guid utilisateurId`, `Guid societeId`.
- **Autorisation** : `CloisonnementService.ExigerProprieteClientAsync` (l'utilisateur doit être membre actif de la société).
- **Traitement** :
  1. Charge les métadonnées de la société (Nom, profil `Normal`/`EnCompte`/`Deposit`/`IES`, état de validation).
  2. Recherche les dossiers récents de la société (`IDossierRepository`). Si l'utilisateur a un droit restreint (`PropresDemandesUniquement`), filtre sur `AuteurId == utilisateurId` (`ENT-03`).
  3. Projette les tickets associés pour calculer les actions requises (`ACR-01`) :
     - Statut `EnAttenteClient` (motif complément) ➜ Action `ComplementManifeste`.
     - Statut `EnAttentePaiement` (client Normal) ➜ Action `PaiementAttendu`.
     - Statut `EnAttenteClient` (motif refus paiement) ➜ Action `NouvellePreuveRequise`.
  4. Calcule les indicateurs synthétiques (Total actifs, Total en cours CIT, Total actions requises).
  5. Retourne `DashboardClientVue`.
- **Sortie** : `DashboardClientVue`.
- **Erreurs** : `AccesRefuseException` (si tentative de consulter une autre société).
- **Source CDC** : `ACR-01` (Must, CDC §17.1), `SOC-08`, `ENT-03`, `CPT-04`.

---

### Contrat C4-03 : `ObtenirDashboardAgentQuery`
- **Acteur** : Agent CIT habilité sur au moins une file.
- **Entrée** : `Guid agentId`.
- **Autorisation** : L'utilisateur doit posséder au moins une habilitation interne CIT (`IAutorisationService`).
- **Traitement** :
  1. Récupère la liste des files pour lesquelles l'agent détient une permission active.
  2. Pour chaque file, calcule le nombre de tickets à l'état `EnAttenteDePriseEnCharge` et `EnCours`.
  3. Recherche les tickets actuellement pris en charge par l'agent (`OperateurId == agentId`).
  4. Récupère le statut de disponibilité opérationnelle de l'agent (`IGestionnaireDisponibilite`).
  5. Retourne `DashboardAgentVue`.
- **Sortie** : `DashboardAgentVue`.
- **Erreurs** : `AccesRefuseException` (si non agent).
- **Source CDC** : `SOC-04`, `PEC-01`, `PEC-02`, `DIS-01`, `SUP-05`.

---

### Contrat C4-04 : `ObtenirDashboardSocieteQuery`
- **Acteur** : Administrateur d'une société cliente (`EstAdministrateur == true`).
- **Entrée** : `Guid adminId`, `Guid societeId`.
- **Autorisation** : L'utilisateur doit être administrateur actif de la société (`CLAR-02`).
- **Traitement** :
  1. Charge le profil complet de l'entreprise.
  2. Récupère la liste des collaborateurs rattachés avec compteurs (actifs, inactifs, administrateurs).
  3. Récupère les 5 derniers dossiers soumis par l'ensemble des collaborateurs de l'entreprise (`CLAR-02`).
  4. Retourne `DashboardSocieteVue`.
- **Sortie** : `DashboardSocieteVue`.
- **Erreurs** : `AccesRefuseException` (si non administrateur de cette société).
- **Source CDC** : `ENT-01`, `ENT-02`, `ENT-03`, `CPT-05`, `CLAR-02`.

---

### Contrat C4-05 : `ObtenirDetailTicketQuery`
- **Acteur** : Client propriétaire du ticket OU Agent habilité sur la file du ticket.
- **Entrée** : `Guid appelantId`, `Guid ticketId`.
- **Autorisation** : Vérification stricte de cloisonnement (Appelant membre de la société propriétaire OU Agent possédant la permission sur la file courante).
- **Traitement** :
  1. Charge le `Ticket`, son `Dossier` parent et le snapshot `TypeClient`.
  2. Charge les pièces jointes (`IDocumentRepository`).
  3. Charge les messages (`IMessageRepository`) :
     - Si l'appelant est Client ➜ retourne uniquement les messages de type `Public` (masque les notes internes `MSG-02`).
     - Si l'appelant est Agent ➜ retourne les messages publics ET les notes internes.
  4. Calcule les indicateurs de temps (`MesureTemps`).
  5. Évalue les actions permises (`ActionsPermisesTicketVue`) en combinant le statut du ticket et les permissions de l'appelant.
  6. Retourne `TicketDetailVue`.
- **Sortie** : `TicketDetailVue`.
- **Erreurs** : `TicketInconnuException`, `AccesRefuseException` (403 si cloisonnement violé).
- **Source CDC** : `SOC-01`, `SOC-02`, `SOC-05`, `SOC-07`, `SOC-08`, `MSG-02`, `CPT-04`, `IMP-04..14`.

---

### Contrat C4-06 : `ObtenirDetailDossierQuery`
- **Acteur** : Client propriétaire du dossier OU Agent habilité.
- **Entrée** : `Guid appelantId`, `Guid dossierId`.
- **Autorisation** : Cloisonnement société ou habilitation agent.
- **Traitement** :
  1. Charge le `Dossier` et la liste de tous ses tickets rattachés (`IDossierRepository`).
  2. Calcule l'état global dérivé du dossier (`EtatDossierDerive`).
  3. Projette la liste des tickets avec leur numéro de BL individuel et leur statut propre (démontrant l'indépendance multi-BL `IMP-02`).
  4. Retourne `DossierDetailVue`.
- **Sortie** : `DossierDetailVue`.
- **Erreurs** : `DossierInconnuException`, `AccesRefuseException`.
- **Source CDC** : `SOC-11`, `IMP-01`, `IMP-02`, `CPT-04`.

---
*(Fin des contrats d'usage).*
