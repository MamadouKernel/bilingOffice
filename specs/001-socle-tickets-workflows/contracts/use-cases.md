# Contracts — Contrats fonctionnels des cas d'usage du socle

**Feature** : `001-socle-tickets-workflows` | **Date** : 2026-08-28 | **Phase 1 de `/speckit.plan`**

> **Cadre** : descriptions **fonctionnelles** des cas d'usage du socle (acteurs, préconditions, effet,
> données métier, règles appliquées, exigences couvertes, traces). **Aucun code, aucune signature C#,
> aucun contrat d'API technique** — le support d'exécution (pages Blazor, hubs SignalR, commandes
> applicatives, permissions) sera décliné en `/speckit.tasks` et `/speckit.implement` dans le respect
> de D2/D4/D5. Chaque contrôle d'autorisation mentionné est **vérifié côté serveur** (Principe VII).

---

## A. Administration & référentiel (agents habilités)

### C-01 — Gérer les files
- **Effet** : créer/modifier une file ; types de tickets admis ; **stratégie d'affectation active** (AFF-07).
- **Règles** : une file doit avoir une stratégie active ; rattachement des habilitations requis (SEC-11).
- **Traçabilité** : SOC-04, WKF-01, AFF-07. **Traces** : audit (D8).

### C-02 — Gérer la matrice des transitions de files
- **Effet** : par transition file source → file cible : destination autorisée, **rôles habilités à initier**, **motifs autorisés** (structure CLAR-04).
- **Règles** : aucune transition possible hors matrice (TRF-06) ; contenu métier validé en atelier (PC-07 — paramètre, non inventé).
- **Traçabilité** : TRF-06, WKF-01. **Traces** : audit.

### C-03 — Gérer rôles, habilitations et rattachements
- **Effet** : attribuer permissions (transférer, clôturer, réouvrir, superviser…) ; rattacher agents aux files.
- **Règles** : aucun rôle métier nommé dans le socle (PC-03) ; droits de clôture pour absence de réponse = habilitation paramétrable (PC-20) ; moindre privilège (SEC-11).
- **Traçabilité** : SEC-02, SEC-11, PC-03, PC-20. **Traces** : audit.

### C-04 — Gérer les périmètres de supervision
- **Effet** : rattacher un superviseur à des files/services (CLAR-05) sans développement.
- **Règles** : périmètre **borné et configurable** ; tout accès supervision est filtré par ce périmètre.
- **Traçabilité** : SUP-01..05, PC-04. **Traces** : audit.

### C-05 — Gérer les paramètres SLA et le calendrier ouvré
- **Effet** : administrer cibles SLA, seuils (info/alerte/dépassement), horaires ouvrés, jours fériés, fuseau de référence.
- **Règles** : **aucune valeur par défaut métier** dans le socle (PC-12, Principe XIII) ; modification sans développement (SLA-05).
- **Traçabilité** : SLA-05, TPS-05, PC-12. **Traces** : audit.

### C-06 — Gérer les règles de priorité
- **Effet** : définir les facteurs de priorisation (module, type, impact client) appliqués à la création/évolution des tickets.
- **Règles** : règles paramétrables (PRI-04) ; une dérogation manuelle reste possible et tracée (PRI-03).
- **Traçabilité** : PRI-01..04. **Traces** : audit + historique.

### C-07 — Gérer la disponibilité des agents
- **Effet** : déclarer les statuts Disponible / Indisponible / Absent ; dernière activité mise à jour automatiquement (DIS-02/03).
- **Règles** : un agent Indisponible ou Absent est **exclu de toute affectation automatique** (DIS-01).
- **Traçabilité** : DIS-01..03. **Traces** : audit, vue supervision.

## B. Espace client (portail client — futur consommateur du socle)

### C-08 — Créer un ticket
- **Acteurs** : client CIT authentifié.
- **Préconditions** : compte client actif (SEC-02).
- **Effet** : créer un dossier si premier ticket du sujet (SOC-09/11), puis un ticket : module, type, objet, description, documents ; **priorité calculée par les règles** (PRI-01..03) ; statut initial « reçu » (CYC-01) ; entrée dans la file cible ; numérotation unique transactionnelle (D6).
- **Règles** : cloisonnement — le client n'accède qu'à ses propres données (SEC-01) ; documents soumis à l'analyse antivirus avant accessibilité (SEC-07).
- **Traçabilité** : SOC-01..03, SOC-09, SOC-11, CYC-01, PRI-01..03, SEC-01/02/07. **Traces** : historique, audit, événement « TicketCréé » (D10).

### C-09 — Consulter ses dossiers et tickets
- **Effet** : liste et détail des dossiers/tickets du client, chronologie et temps constatés.
- **Règles** : périmètre strictement limité à ses tickets ; état du dossier **dérivé** de ses tickets (CLAR-01).
- **Traçabilité** : SOC-06..11, TPS-05. **Traces** : audit (consultations sensibles).

### C-10 — Échanger par la messagerie du ticket
- **Effet** : envoyer un message dans le fil du ticket ; horodatage ; pièces jointes.
- **Règles** : messages internes CIT invisibles du client (MSG-03) ; horodatage systématique (MSG-02) ; cloisonnement (SEC-01).
- **Traçabilité** : MSG-01..05, SOC-08. **Traces** : historique, événement « MessagePosté ».

### C-11 — Déposer un document sur un ticket
- **Effet** : joindre une pièce ; métadonnées enregistrées ; contenu stocké derrière le port (D9).
- **Règles** : antivirus configurable avant validation (SEC-07) ; accès ultérieur contrôlé à chaque téléchargement (SEC-12).
- **Traçabilité** : SOC-12, SEC-07, SEC-12. **Traces** : historique, audit.

### C-12 — Clôturer / annuler / demander l'annulation (client)
- **Effet** : le client peut clôturer son ticket résolu (CYC-03), demander l'annulation (CYC-02) ; l'annulation effective suit les règles du cycle (CYC-05) et reste traçable.
- **Règles** : un ticket annulé n'est **pas rouvert** (CLAR-02) ; toute action horodatée et tracée.
- **Traçabilité** : CYC-02/03/05, SOC-09. **Traces** : historique, audit, événements.

### C-13 — Demander la réouverture d'un ticket clôturé
- **Préconditions** : ticket **clôturé** (clôture normale ou pour absence de réponse) — jamais un ticket annulé (CLAR-02).
- **Effet** : demande de réouverture avec motif ; décision selon CYC-07 ; si acceptée, le ticket redevient actif et le dossier associé est réactivé automatiquement (CLAR-01).
- **Traçabilité** : CYC-06/07, SOC-11. **Traces** : historique, audit, événement « RéouvertureDemandée/Décidée ».

## C. Traitement interne CIT (agents des files)

### C-14 — Consulter sa file et prendre en charge un ticket
- **Acteurs** : agent habilité de la file (SEC-11).
- **Préconditions** : ticket dans une de ses files, non affecté (prise libre, AFF-01) ou affecté à lui.
- **Effet** : prise en charge — statut « en cours » (PEC-01), temps d'attente stoppé / temps de traitement démarré (TPS-01/02), dernière activité mise à jour (DIS-02).
- **Règles** : un ticket déjà pris ne peut pas être repris sans réaffectation (PEC-05) ; horodatage systématique (TPS-04).
- **Traçabilité** : PEC-01..05, TPS-01/02, AFF-01. **Traces** : historique, audit, événement « TicketPrisEnCharge ».

### C-15 — Transférer un ticket vers une autre file
- **Préconditions** : transition autorisée par la **matrice** (TRF-06) ; initiateur habilité par la matrice (CLAR-04) ; motif conforme à la matrice.
- **Effet** : changement de file ; **ticket arrivant non affecté** « en attente de prise en charge » sauf destinataire désigné (TRF-03, CLAR-03) ; **nouveau temps d'attente** et nouvelle échéance SLA de l'étape (TPS-01, SLA-01).
- **Traçabilité** : TRF-01..06, CLAR-03/04. **Traces** : historique (TRF-02), audit (TRF-05), événement « TicketTransféré ».

### C-16 — (Ré)affecter un ticket
- **Effet** : affectation manuelle à un agent de la file (AFF-02) ; réaffectation vers un autre agent.
- **Règles** : cible active et habilitée ; aucune affectation d'un Indisponible/Absent (DIS-01) ; selon la stratégie de file, l'affectation peut être automatique (AFF-03..06 — voir C-23).
- **Traçabilité** : AFF-02..06, PEC-04/05. **Traces** : historique, audit.

### C-17 — Répondre au client / mettre en attente client
- **Effet** : réponse au client (le temps de traitement s'arrête, l'attente client démarre — TPS-04, SLA-03) ; mise en attente explicite ; la reprise du traitement arrête l'attente client.
- **Traçabilité** : TPS-01..05, MSG-01..02. **Traces** : historique, audit.

### C-18 — Clôturer un ticket
- **Préconditions** : habilitation de clôture ; réponse au client au besoin (CYC-03).
- **Effet** : statut « clôturé », motif, temps de traitement total constaté (TPS-05) ; dossier réévalué (CLAR-01 — archivé si tous ses tickets sont clos/annulés, consultable en permanence).
- **Traçabilité** : CYC-03, TPS-05, SOC-11. **Traces** : historique, audit, événement « TicketClôturé ».

### C-19 — Clôturer pour absence de réponse
- **Préconditions** : relances effectuées selon le paramétrage (CYC-04) ; habilitation **paramétrable** (PC-20).
- **Effet** : clôture pour absence de réponse après relance(s) et suggestion de clôture ; traçabilité spécifique du motif.
- **Traçabilité** : CYC-04, PC-20. **Traces** : historique, audit, événement « RelanceÉmise », « TicketClôturéPourAbsence ».

### C-20 — Annuler un ticket (interne) / traiter une demande d'annulation
- **Effet** : annulation selon les règles du cycle (CYC-05) ; traitement d'une demande client (CYC-02) avec décision tracée ; routage de la demande = paramètre (file ou responsable habilité).
- **Règles** : un ticket annulé n'est jamais rouvert (CLAR-02).
- **Traçabilité** : CYC-02, CYC-05. **Traces** : historique, audit, événement « TicketAnnulé ».

## D. Traitements de fond (automatiques — BackgroundService, D5)

### C-21 — Surveiller les échéances SLA et émettre les alertes
- **Effet** : détection des franchissements de seuils (info/alerte/dépassement) sur le calendrier ouvré paramétrable ; création d'alertes (ALE-01) ; visibilité supervision (SUP-05).
- **Règles** : valeurs issues exclusivement du paramétrage (PC-12) ; suspension pendant l'attente client (SLA-03).
- **Traçabilité** : SLA-01..05, ALE-01, TPS-02/04. **Traces** : alertes, journal d'événements.

### C-22 — Relancer le client et suggérer la clôture
- **Effet** : émission des relances d'absence de réponse selon le paramétrage (nombre/délai) ; suggestion de clôture aux agents (CYC-04).
- **Traçabilité** : CYC-04. **Traces** : historique, événements « RelanceÉmise ».

### C-23 — Distribuer automatiquement les tickets d'une file
- **Effet** : selon la **stratégie paramétrée de la file** : round-robin (AFF-03), charge (AFF-04), disponibilité (AFF-05), compétence/habilitation (AFF-06).
- **Règles** : jamais d'agent Indisponible/Absent (DIS-01) ; cohérence avec la prise libre (AFF-01) et l'affectation manuelle (AFF-02).
- **Traçabilité** : AFF-01..07, DIS-01. **Traces** : audit, événement « TicketAffecté ».

## E. Supervision & audit

### C-24 — Consulter la supervision temps réel
- **Acteurs** : superviseurs (périmètre CLAR-05) ; agents habilités.
- **Effet** : vue des files (encours, attentes, délais), charge par opérateur, disponibilité, dernière activité, alertes — **push SignalR** à une fréquence paramétrable (PC-02).
- **Règles** : strict filtrage par le périmètre de supervision configuré (SUP-01..05).
- **Traçabilité** : SUP-01..05, DIS-02/03, ALE. **Traces** : audit (accès).

### C-25 — Consulter le journal d'audit
- **Acteurs** : administrateur technique habilité (SEC-05).
- **Effet** : consultation du registre append-only (auteur, action, entité, avant/après, horodatage).
- **Règles** : distinct de l'historique fonctionnel ; aucune modification/suppression métier possible (D8).
- **Traçabilité** : SEC-05, PRI-03, TRF-05, DIS-03, MAS-02. **Traces** : audit de l'audit consulté.

---

> **Couverture** : ces 25 contrats couvrent les 74 exigences FR-001→FR-074 du socle ; la matrice
> complète contrat → exigence → User Story est tenue dans `../spec.md` (matrice de couverture) et sera
> déclinée tâche par tâche en `/speckit.tasks`.
