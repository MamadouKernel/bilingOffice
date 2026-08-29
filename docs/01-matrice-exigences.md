# Matrice des Exigences
## Plateforme Digitale CIT — v1.0

> **Document** : `docs/01-matrice-exigences.md`  
> **Référence CDC** : CIT-CIV-DSI-RFP-XXXX v1.0 — 12 août 2026  
> **Révisé le** : 28 août 2026 (révision post-audit — corrections appliquées)  
> **Statut** : Pré-analyse — en attente de validation métier

**Colonnes :**
- **Référence** : identifiant du CDC
- **Priorité** : Must / Should / Could
- **Module** : module fonctionnel concerné
- **Exigence** : description synthétique fidèle au texte du CDC
- **Acteur(s)** : acteurs concernés (DEFINED / FUNCTIONAL_RESPONSIBILITY / ROLE_TO_CONFIRM)
- **Dépendances** : références pré-requises — indication **[E]** = explicite dans le CDC · **[D]** = déduite logiquement
- **Couche** : Domaine / Application / Infrastructure / Sécurité / UX / Exploitation
- **Testable** : Oui / Partiel (critères à préciser) / Non (décision métier)
- **Statut** : Confirmé / À valider / Ambiguïté

---

## A — Socle de gestion des tickets (SOC / CYC / MSG)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| SOC-01 | Must | Socle | Toute demande génère un ticket avec numéro unique communiqué au client (portail + e-mail) | Tous | — | Domaine · Application | Oui | Confirmé |
| SOC-02 | Must | Socle | Le ticket centralise : documents, statut, historique horodaté, échanges client/agent, décisions, motifs de rejet, documents finaux | Tous | SOC-01 [D] | Domaine · Application | Oui | Confirmé |
| SOC-03 | Must | Socle | Chaque ticket suit un workflow propre à son module et, pour le Billing Office, au type de client | Tous | CPT-02 [D] · WKF-01 [D] | Domaine | Oui | Confirmé |
| SOC-04 | Must | Socle | Files de traitement par rôle : chaque agent voit les tickets en attente de son rôle, avec tri et filtres (date, type client, statut, priorité) | Agents (DEFINED_ROLE) | SOC-01 [D] | Application · UX | Oui | Confirmé |
| SOC-05 | Must | Socle | Fil de commentaires (chat) par ticket entre le client et les agents, horodaté, conservé dans l'historique. *(Note : COM-04 réaffirme cette même exigence dans le contexte des communications multicanales — un seul journal)* | Client · Agents | SOC-01 [D] | Application | Oui | Confirmé |
| SOC-06 | Must | Socle | Actions de workflow standard : valider, rejeter avec motif obligatoire, demander un complément au client | Agents | SOC-01 [D] · WKF-01 [D] | Domaine · Application | Oui | Confirmé |
| SOC-07 | Must | Socle | Dépôt de documents par le client et par les agents ; types de pièces paramétrables par étape ; formats acceptés et taille maximale configurables | Client · Agents | SOC-01 [D] | Application · Sécurité | Oui | Confirmé |
| SOC-08 | Must | Socle | Statuts visibles par le client en temps réel sur son profil, sur chaque ticket | Client | SOC-01 [D] | Application · UX | Oui | Confirmé |
| SOC-09 | Must | Socle | Clôture d'un ticket suivie d'un archivage automatique ; les archives restent consultables en permanence par le client et par les agents habilités | Client · Agents | SOC-01 [D] | Application · Exploitation | Oui | Confirmé |
| SOC-10 | Must | Socle | Liaison entre tickets (y compris entre modules) pour conserver la continuité d'un même dossier métier | Agents (création liaison) · Client (visibilité) | SOC-01 [D] | Domaine · Application | Oui | Confirmé |
| SOC-11 | Must | Socle | Dossier : les tickets d'une même demande multi-BL/booking regroupés sous un numéro de dossier unique, permettant un suivi global | Client · Agents | SOC-01 [D] | Domaine · Application | Oui | Confirmé |
| SOC-12 | Must | Socle | Le système distingue l'arrivée d'un ticket en file de sa prise en charge effective ; il enregistre heure d'arrivée, heure de PEC, opérateur, début traitement, dernière activité. *(Note fondatrice : PEC-01-05 opérationnalisent SOC-12)* | Agents · Superviseur | — | Domaine · Application | Oui | Confirmé |
| SOC-13 | Must | Socle | Journal d'audit inviolable : toute action tracée avec auteur, date/heure et détail. *(Note : SEC-05 qualifie ce même journal unique comme exigence de sécurité — non modifiable)* | Tous | — | Sécurité · Exploitation | Oui | Confirmé |
| SOC-14 | Could | Socle | Export des indicateurs et listes de tickets aux formats usuels (Excel, PDF) | Superviseur · Admin | — | Application | Oui | Confirmé |
| CYC-01 | Must | Socle | Le client peut annuler lui-même un ticket tant qu'aucun traitement engageant ni document officiel n'a été produit ; les étapes bloquantes sont paramétrables par workflow (non codées en dur) | Client | SOC-01 [D] | Domaine | Oui | Confirmé |
| CYC-02 | Must | Socle | Au-delà de l'étape bloquante, le client soumet une demande d'annulation transmise à la file ou au responsable habilité pour décision | Client · Agent habilité | CYC-01 [E] | Domaine · Application | Oui | Confirmé |
| CYC-03 | Must | Socle | Toute annulation ou demande d'annulation est motivée, horodatée et conservée dans l'historique | Client · Agent | CYC-01 [E] · CYC-02 [E] | Domaine · Sécurité | Oui | Confirmé |
| CYC-04 | Must | Socle | Relances automatiques paramétrables (nombre et cadence) ; au terme des relances, la plateforme génère une suggestion de clôture — la clôture reste une décision manuelle d'un agent habilité, jamais automatique. *Relation avec COM-06 : CYC-04 définit le mécanisme (Must) ; COM-06 (Could) précise les canaux de notification des relances.* | Agent habilité | CYC-05 [E] | Domaine · Application | Oui | Confirmé |
| CYC-05 | Must | Socle | La suggestion de clôture pour absence de réponse n'est jamais proposée lorsque le ticket a atteint un état engageant (paiement confirmé ou document officiel produit) | — | CYC-04 [E] | Domaine | Oui | Confirmé |
| CYC-06 | Should | Socle | Un ticket clôturé peut être rouvert par un utilisateur CIT habilité ; la réouverture est motivée et tracée ; le ticket conserve son numéro, historique, documents, échanges | Agent habilité | SOC-09 [D] | Domaine · Application | Oui | Confirmé |
| CYC-07 | Should | Socle | Le client ne peut pas rouvrir directement un ticket clôturé ; il peut demander une réouverture ou créer une nouvelle demande | Client | CYC-06 [D] | Domaine | Oui | Confirmé |
| MSG-01 | Must | Socle | Message client/agent : visible par le client et les agents habilités | Client · Agents | SOC-05 [E] | Application | Oui | Confirmé |
| MSG-02 | Must | Socle | Note interne CIT : visible uniquement par les collaborateurs CIT habilités ; jamais visible du client | Agents (DEFINED_ROLE) | SOC-05 [E] | Application · Sécurité | Oui | Confirmé |
| MSG-03 | Must | Socle | Le caractère interne ou externe d'un message est visuellement très distinct dans l'interface ; les deux types sont horodatés et historisés | Agents | MSG-01 [E] · MSG-02 [E] | UX · Sécurité | Oui | Confirmé |
| MSG-04 | Should | Socle | Un collaborateur peut être mentionné (@Nom) dans une note interne ; la personne mentionnée reçoit une notification portail et, selon paramétrage, un e-mail | Agents | MSG-02 [D] | Application | Oui | Confirmé |
| MSG-05 | Must | Socle | Une mention n'entraîne pas de réaffectation : le ticket reste sous la responsabilité de son opérateur courant | — | MSG-04 [E] | Domaine | Oui | Confirmé |

---

## B — Moteur de workflow, files et supervision (WKF / PEC / TRF / TPS / SLA / ALE / SUP / DIS / AFF / PRI / MAS)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| WKF-01 | Must | Socle | La plateforme applique automatiquement le workflow correspondant au module, au type de demande et au profil du client | Système | CPT-02 [D] | Domaine · Application | Oui | Confirmé |
| WKF-02 | Must | Socle | Circuit non rigide : les utilisateurs habilités peuvent transférer ou réaffecter un ticket lorsque la situation opérationnelle le nécessite, dans le respect des règles d'habilitation paramétrées | Agent habilité · Superviseur | WKF-01 [E] | Domaine | Oui | Confirmé |
| WKF-03 | Must | Socle | Toute dérogation au circuit nominal est tracée dans l'historique du ticket et le journal d'audit | — | WKF-02 [E] · SOC-13 [D] | Sécurité · Exploitation | Oui | Confirmé |
| PEC-01 | Must | Socle | Un ticket entrant dans une file est à l'état « en attente de prise en charge » tant qu'aucun opérateur ne l'a accepté *(opérationnalisation de SOC-12)* | — | SOC-12 [E] | Domaine | Oui | Confirmé |
| PEC-02 | Must | Socle | Un opérateur habilité peut prendre en charge (accepter) un ticket de sa file ; le ticket lui est affecté et passe en traitement | Agent (DEFINED_ROLE) | PEC-01 [E] | Application | Oui | Confirmé |
| PEC-03 | Must | Socle | Enregistrement pour chaque ticket : heure d'arrivée en file, heure de PEC, opérateur ayant accepté, début de traitement, dernière activité *(opérationnalisation de SOC-12)* | — | SOC-12 [E] | Domaine · Application | Oui | Confirmé |
| PEC-04 | Must | Socle | Le délai de PEC (arrivée en file → acceptation) est calculé et exploitable dans les indicateurs | Superviseur | PEC-03 [E] | Application · Exploitation | Oui | Confirmé |
| PEC-05 | Should | Socle | Affectation directe : un superviseur ou une règle d'affectation paramétrée peut affecter un ticket non pris en charge à un opérateur précis | Superviseur | PEC-01 [D] | Application | Oui | Confirmé |
| TRF-01 | Must | Socle | Un opérateur habilité peut transférer un ticket vers une autre file de traitement autorisée, sans que le client recommence sa demande | Agent habilité | SOC-01 [D] · WKF-02 [E] | Domaine | Oui | Confirmé |
| TRF-02 | Must | Socle | Lors d'un transfert, le ticket conserve son numéro, son dossier, son historique, ses documents, ses commentaires et les informations déjà collectées | — | TRF-01 [E] | Domaine · Application | Oui | Confirmé |
| TRF-03 | Must | Socle | Un transfert trace au minimum : opérateur à l'origine, date/heure, file source, file destination, motif (obligatoire), destinataire éventuel | — | TRF-01 [E] · SOC-13 [D] | Sécurité | Oui | Confirmé |
| TRF-04 | Must | Socle | Un opérateur habilité peut réaffecter un ticket à un autre opérateur habilité, **sous réserve des droits configurés** ; ce droit n'est pas réservé au superviseur | Agent habilité · Superviseur | WKF-02 [E] | Application | Oui | Confirmé |
| TRF-05 | Must | Socle | Le client est automatiquement notifié d'une réorientation ayant un impact sur son parcours | Client | TRF-01 [D] · COM-01 [D] | Application | Oui | Confirmé |
| TRF-06 | Must | Socle | Les transitions autorisées entre files sont définies par une matrice paramétrable ; aucun circuit de transfert n'est codé en dur | Admin fonctionnel | — | Domaine · Application | Oui | Confirmé |
| TPS-01 | Must | Socle | Pour chaque étape/file traversée : mesure du temps d'attente (arrivée → PEC) et du temps de traitement (PEC → sortie) | — | PEC-03 [E] | Domaine · Application | Oui | Confirmé |
| TPS-02 | Must | Socle | Mesure du temps d'attente client (durée en état « En attente client ») | — | SOC-12 [D] | Domaine | Oui | Confirmé |
| TPS-03 | Must | Socle | La plateforme distingue trois durées : temps interne CIT, temps d'attente client, temps total du parcours (création → clôture) | — | TPS-01 [E] · TPS-02 [E] | Domaine · Application | Oui | Confirmé |
| TPS-04 | Must | Socle | Le temps d'attente client n'est pas imputé à la performance opérateur/file ; les SLA internes peuvent être suspendus pendant cet état | — | TPS-02 [D] · SLA-03 [D] | Domaine | Oui | Confirmé |
| TPS-05 | Should | Socle | L'historique du ticket restitue la chronologie détaillée par étape (attente + traitement pour chaque file traversée) | Client · Agents | TPS-01 [D] | Application · UX | Oui | Confirmé |
| SLA-01 | Must | Socle | SLA paramétrables selon, au minimum : module, type de demande, étape, file, priorité, et éventuellement catégorie de client | Admin fonctionnel | — | Domaine · Application | Oui | Confirmé |
| SLA-02 | Must | Socle | Plusieurs seuils par SLA : seuil d'information (ex. 50%), seuil d'alerte (ex. 80%), dépassement (100%) et escalade au-delà | Admin fonctionnel | SLA-01 [E] | Domaine · Application | Oui | Confirmé |
| SLA-03 | Must | Socle | Les SLA peuvent être suspendus ou adaptés pendant certains états, notamment « En attente client » | — | TPS-04 [D] · SLA-01 [D] | Domaine | Oui | Confirmé |
| SLA-04 | Should | Socle | Les horaires de fonctionnement (heures ouvrées du service) peuvent être pris en compte dans le calcul des SLA | Admin fonctionnel | SLA-01 [D] | Domaine | Partiel (définition des heures ouvrées à préciser — PC-12) | Confirmé |
| SLA-05 | Must | Socle | Les valeurs de seuils et durées cibles des SLA sont administrables sans développement (les valeurs exactes seront déterminées avec le métier) | Admin fonctionnel | SLA-01 [E] | Application | Oui | Confirmé |
| ALE-01 | Must | Socle | Alertes générées pour : ticket non PEC depuis X min, ticket sans activité depuis X min/h, SLA bientôt dépassé, SLA dépassé, backlog file > seuil, volume anormal, opérateur indisponible avec tickets affectés | Superviseur | SLA-01 [D] | Application · Exploitation | Oui | Confirmé |
| ALE-02 | Must | Socle | Les règles d'alerte, seuils, destinataires et canaux sont paramétrables sans développement | Admin fonctionnel | ALE-01 [E] | Application | Oui | Confirmé |
| ALE-03 | Must | Socle | Le dépassement d'un SLA peut déclencher une escalade (notification superviseur, remontée priorité, réaffectation) selon des règles paramétrables | Système · Superviseur | SLA-02 [E] · ALE-01 [D] | Domaine · Application | Oui | Confirmé |
| SUP-01 | Must | Socle / Supervision | Vue temps réel des tickets par état : en attente, non affectés, en cours, attente client, proches SLA, en dépassement SLA, sans activité depuis durée définie | Superviseur | PEC-01 [D] · SLA-01 [D] | Application · Exploitation | Oui | Confirmé |
| SUP-02 | Must | Socle / Supervision | Vue temps réel par file : nombre tickets, backlog, ancienneté ticket le plus ancien, délai moyen, respect SLA | Superviseur | TPS-01 [D] · SLA-01 [D] | Application · Exploitation | Oui | Confirmé |
| SUP-03 | Must | Socle / Supervision | Vue de la charge par opérateur et de la disponibilité/connexion des opérateurs du périmètre | Superviseur | DIS-01 [D] | Application | Oui | Confirmé |
| SUP-04 | Must | Socle / Supervision | Le superviseur peut : affecter un ticket non PEC, réaffecter, transférer, équilibrer la charge, intervenir sur un ticket bloqué, rechercher et consulter l'historique complet | Superviseur | TRF-01 [D] · TRF-04 [D] | Application | Oui | Confirmé |
| SUP-05 | Must | Socle / Supervision | Suivi activité opérationnelle des agents du périmètre : statut disponibilité, dernière activité, tickets affectés/en cours, volumes, indicateurs de délai | Superviseur | DIS-01 [D] · PEC-03 [D] | Application · Exploitation | Oui | Confirmé |
| DIS-01 | Must | Socle | La plateforme permet de connaître la disponibilité opérationnelle d'un agent (Disponible / Indisponible / Absent) ; un agent déclaré indisponible ou absent ne reçoit pas de nouvelle affectation automatique | Agents · Superviseur | — | Domaine · Application | Oui | Confirmé |
| DIS-02 | Must | Socle | Le superviseur peut : modifier la disponibilité d'un opérateur, identifier ses tickets affectés, réaffecter en masse, consulter la charge restante | Superviseur | DIS-01 [E] | Application | Oui | Confirmé |
| DIS-03 | Must | Socle | Les changements de disponibilité sont tracés | — | DIS-01 [D] · SOC-13 [D] | Sécurité | Oui | Confirmé |
| AFF-01 | Must | Socle | Prise libre : un opérateur habilité prend un ticket dans sa file | Agent (DEFINED_ROLE) | PEC-02 [D] | Application | Oui | Confirmé |
| AFF-02 | Must | Socle | Affectation manuelle : un superviseur affecte un ticket à un opérateur *(Must autonome — indépendant de PEC-05 Should)* | Superviseur | — | Application | Oui | Confirmé |
| AFF-03 | Should | Socle | Distribution automatique Round-Robin | Système | DIS-01 [D] *(déduit : un opérateur indisponible ne doit pas recevoir de ticket en Round-Robin)* | Application | Oui | Confirmé |
| AFF-04 | Should | Socle | Distribution selon la charge actuelle des agents | Système | DIS-01 [D] | Application | Oui | Confirmé |
| AFF-05 | Should | Socle | Distribution selon la disponibilité des agents | Système | DIS-01 [E] | Application | Oui | Confirmé |
| AFF-06 | Should | Socle | Distribution selon la compétence / l'habilitation | Système | — | Application | Oui | Confirmé |
| AFF-07 | Must | Socle | Le changement de stratégie d'affectation d'une file est réalisable sans développement | Admin fonctionnel | AFF-01 [D] | Application | Oui | Confirmé |
| PRI-01 | Must | Socle | Chaque ticket dispose d'un niveau de priorité issu d'un référentiel paramétrable ; la priorité peut être attribuée automatiquement ou modifiée par un utilisateur habilité | Système · Agent habilité | — | Domaine · Application | Oui | Confirmé |
| PRI-02 | Must | Socle | La priorité peut influencer l'ordre du ticket dans la file, le SLA applicable, les alertes et les règles d'escalade | — | PRI-01 [E] · SLA-01 [D] | Domaine | Oui | Confirmé |
| PRI-03 | Must | Socle | Toute modification manuelle de priorité enregistre : ancienne priorité, nouvelle priorité, auteur, date/heure, motif | — | PRI-01 [E] · SOC-13 [D] | Sécurité | Oui | Confirmé |
| PRI-04 | Must | Socle | Le client ne peut pas déclarer librement sa demande urgente ou critique, sauf si un processus métier particulier le prévoit | Client | PRI-01 [E] | Domaine · Sécurité | Oui | Confirmé |
| MAS-01 | Should | Socle / Supervision | Le superviseur peut, en masse : affecter, réaffecter, transférer de file, modifier la priorité, et réaliser d'autres actions administratives autorisées | Superviseur | SUP-04 [D] | Application | Oui | Confirmé |
| MAS-02 | Must | Socle / Supervision | Toute action de masse est tracée ticket par ticket dans l'historique et le journal d'audit. *Tension logique : MAS-02 (Must) est testable uniquement si MAS-01 (Should) est implémenté. Si MAS-01 est différé, MAS-02 ne peut pas être testé et doit être marqué « non applicable » dans la recette.* | — | MAS-01 [E] · SOC-13 [D] | Sécurité | Partiel (conditionné à MAS-01) | Confirmé |

---

## C — Comptes clients et sociétés (CPT / ENT)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| CPT-01 | Must | Comptes | Création de compte client soumise à validation par CIT avant activation ; les informations et justificatifs exigés sont paramétrables | Admin fonctionnel | — | Application | Oui | Confirmé |
| CPT-02 | Must | Comptes | Le profil du client est déclaré par le client à la création et validé par CIT : normal, en compte, deposit ou IES. Ce profil détermine automatiquement le workflow Billing Office applicable | Client (déclaration) · Admin fonctionnel CIT (validation) | CPT-01 [E] · WKF-01 [D] | Domaine | Oui | Confirmé |
| CPT-03 | Must | Comptes | Authentification sécurisée (identifiant/mot de passe avec politique de robustesse) ; récupération de mot de passe en autonomie | Client | — | Sécurité | Oui | Confirmé |
| CPT-04 | Must | Comptes | Un client n'accède qu'à ses propres tickets, documents et échanges | Client | SEC-02 [E] | Sécurité | Oui | Confirmé |
| CPT-05 | Must | Comptes | La gestion multi-utilisateurs pour une même société cliente est requise (compte entreprise — voir ENT-01-03) | Admin de société | ENT-01 [E] | Application | Oui | Confirmé |
| CPT-06 | Should | Comptes | Modification du type de client par l'administrateur fonctionnel CIT : tracée, sans effet rétroactif sur les tickets en cours | Admin fonctionnel CIT | CPT-02 [E] | Domaine | Oui | Confirmé |
| CPT-07 | Could | Comptes | Authentification renforcée (second facteur) pour les comptes clients. *(Note : exigence indépendante de SEC-08 qui concerne les agents et admins)* | Client | — | Sécurité | Oui | Confirmé |
| ENT-01 | Must | Comptes | Une société cliente peut rattacher plusieurs utilisateurs à son compte | Admin de société | CPT-05 [E] | Application | Oui | Confirmé |
| ENT-02 | Must | Comptes | Un administrateur de la société gère ses collaborateurs : activation/désactivation, droits de consultation et de soumission paramétrables | Admin de société | ENT-01 [E] | Application | Oui | Confirmé |
| ENT-03 | Must | Comptes | La visibilité des dossiers est fonction des droits accordés : l'utilisateur voit uniquement ses propres demandes ou l'ensemble des demandes de sa société | Utilisateur de société | ENT-02 [E] | Application · Sécurité | Oui | Confirmé |
| ENT-04 | Should | Comptes | Journalisation des actions des administrateurs de société (ajout/retrait de collaborateurs, modification de droits) | — | ENT-02 [D] · SOC-13 [D] | Sécurité | Oui | Confirmé |

---

## D — Billing Office IMPORT (IMP / UNI)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| IMP-01 | Must | Billing IMPORT | Formulaire dynamique de soumission permettant de saisir les informations nécessaires, d'ajouter jusqu'à 5 BL par demande et de déposer les documents associés à chaque BL | Client | SOC-01 [D] | UX · Application | Oui | Confirmé |
| IMP-02 | Must | Billing IMPORT | À la validation du formulaire, génération automatique d'un ticket par BL, totalement indépendant, avec correspondance claire ticket ↔ BL | Système | SOC-01 [E] · SOC-11 [D] | Domaine | Oui | Confirmé |
| IMP-03 | Must | Billing IMPORT | Le client reçoit ses numéros de ticket sur son profil et par e-mail | Client | SOC-01 [E] · COM-01 [D] *(déduit : la notification par e-mail suppose COM-01)* | Application | Oui | Confirmé |
| IMP-04 | Must | Billing IMPORT | Les tickets arrivent dans la file du Pilote Manifeste, qui prononce pour chaque ticket : Validé, Rejeté (motif obligatoire) ou En attente de complément | Pilote Manifeste (DEFINED_ROLE) | SOC-04 [D] · SOC-06 [D] | Application | Oui | Confirmé |
| IMP-05 | Must | Billing IMPORT | En cas de complément, le client est notifié, dépose les éléments dans le ticket, et le dossier revient dans la file du Pilote Manifeste | Client · Pilote Manifeste | SOC-07 [D] · COM-01 [D] | Application | Oui | Confirmé |
| IMP-06 | Must | Billing IMPORT | À l'issue du contrôle BL, le ticket validé s'oriente automatiquement selon le type de client | Système | CPT-02 [E] · WKF-01 [E] | Domaine | Oui | Confirmé |
| IMP-10 | Must | Billing IMPORT | Client normal : après validation, le client demande la facture depuis son ticket ; le ticket passe dans la file Facturation | Client normal | IMP-06 [E] | Application | Oui | Confirmé |
| IMP-11 | Must | Billing IMPORT | Pilote Facturation Terre effectue ses contrôles (hors portail), édite la facture et la joint au ticket ; le client est notifié | Pilote Facturation (DEFINED_ROLE) | IMP-10 [E] | Domaine | Oui | Confirmé |
| IMP-12 | Must | Billing IMPORT | Le client procède au paiement (hors portail) puis joint sa preuve de paiement au ticket | Client normal | IMP-11 [E] · SOC-07 [D] | Application | Oui | Confirmé |
| IMP-13 | Must | Billing IMPORT | La preuve de paiement fait l'objet d'une validation interne par le rôle habilité (Caisse/Finance — ROLE_TO_CONFIRM) avant le passage au Contrôle Documentaire (statuts : En attente paiement → En attente validation → Payé confirmé) | Caisse/Finance (ROLE_TO_CONFIRM — cf. PC-03) | IMP-12 [E] | Domaine | Oui | **Ambiguïté rôle (PC-03)** |
| IMP-14 | Must | Billing IMPORT | Le Pilote Contrôle Documentaire vérifie le dossier, dépose les documents de sortie (BS, BADT) dans le ticket, renvoie le ticket au client et le clôture | Pilote CD (DEFINED_ROLE) | IMP-13 [E] | Domaine | Oui | Confirmé |
| IMP-15 | Should | Billing IMPORT | Alternative présentielle : à l'issue du contrôle BL, le client peut choisir de prendre rendez-vous et finaliser physiquement au Billing Office ; le ticket porte la trace de cette bascule. *IMP-15 est une instanciation de CON-01 dans le module Billing Office — CON-01 (Must) rend IMP-15 possible.* | Client · Agent guichet (FUNCTIONAL_RESPONSIBILITY) | CON-01 [D] *(activée par)* | Application | Oui | Confirmé |
| IMP-20 | Must | Billing IMPORT | Client en compte : parcours identique au client normal jusqu'à l'édition de la facture | — | IMP-06 [E] | Domaine | Oui | Confirmé |
| IMP-21 | Must | Billing IMPORT | Client en compte : après édition de la facture, le ticket est transmis directement au Contrôle Documentaire (pas d'étape paiement) | Pilote Facturation (DEFINED_ROLE) | IMP-20 [E] | Domaine | Oui | Confirmé |
| IMP-22 | Must | Billing IMPORT | Client en compte : récupère son ticket clôturé avec l'ensemble des documents (facture, BS, BADT) | Client en compte | IMP-21 [E] | Application | Oui | Confirmé |
| IMP-30 | Must | Billing IMPORT | Client deposit : parcours identique au client normal jusqu'à l'édition de la facture | — | IMP-06 [E] | Domaine | Oui | Confirmé |
| IMP-31 | Must | Billing IMPORT | Client deposit : après édition de la facture, le ticket est transmis à la file Comptabilité Client pour lettrage dans IRIS (hors portail) ; le comptable confirme dans le ticket | Comptable Client (DEFINED_ROLE) | IMP-30 [E] | Domaine | Oui | Confirmé |
| IMP-32 | Must | Billing IMPORT | Client deposit : après lettrage, le ticket passe au Contrôle Documentaire puis est retourné au client avec ses documents | Pilote CD (DEFINED_ROLE) | IMP-31 [E] | Domaine | Oui | Confirmé |
| IMP-40 | Must | Billing IMPORT | Client IES : dès sa création, le ticket est traité en parallèle par le Pilote Manifeste et le Pilote CD, chacun intervenant exclusivement sur les champs et actions correspondant à son rôle | Pilote Manifeste · Pilote CD (DEFINED_ROLE) | SEC-02 [E] | Domaine · Sécurité | Oui | Confirmé |
| IMP-41 | Must | Billing IMPORT | Client IES : le ticket n'est retourné au client que lorsque les deux traitements parallèles sont terminés (jointure) | Système | IMP-40 [E] | Domaine | Oui | Confirmé |
| IMP-42 | Must | Billing IMPORT | Client IES : le client finalise son parcours sur la plateforme IES (facturation, paiement, documents) ; le ticket portail est clôturé à la remise des documents | Client IES | IMP-41 [E] | Domaine | Oui | Confirmé |
| UNI-01 | Must | Billing IMPORT | À la création d'un ticket IMPORT, la plateforme vérifie s'il existe un ticket actif pour le même BL ; si oui, la création est bloquée et un accès au ticket existant est proposé au client | Système · Client | SOC-01 [D] · SOC-09 [D] | Domaine | Oui | Confirmé |
| UNI-02 | Must | Billing IMPORT | Un BL associé uniquement à un ticket clôturé ou annulé peut faire l'objet d'une nouvelle demande | Système | UNI-01 [E] | Domaine | Oui | Confirmé |
| UNI-03 | Must | Billing IMPORT | Le contrôle d'unicité porte sur chaque BL individuellement et n'empêche pas l'utilisation du même numéro de BL dans d'autres modules (Customer Complaint par exemple) | — | UNI-01 [E] | Domaine | Oui | Confirmé |
| UNI-04 | Should | Billing EXPORT | Pour l'EXPORT, une règle équivalente sur le numéro de booking pourra être mise en œuvre après validation métier | — | UNI-01 [D] | Domaine | Partiel (à valider — PC-09) | **À valider (PC-09)** |

---

## E — Billing Office EXPORT (EXP)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| EXP-01 | Must | Billing EXPORT | Soumission client d'un ou plusieurs bookings avec documents (confirmation de booking, déclaration export, autres pièces) ; génération d'un ticket par booking | Client | SOC-01 [D] · SOC-11 [D] | Application | Oui | Confirmé |
| EXP-02 | Must | Billing EXPORT | Les tickets arrivent dans la file du Back Office, qui codifie le booking dans IPAKI (hors portail) et prononce : Codifié, Rejeté (motif) ou En attente de complément | Back Office (DEFINED_ROLE) | SOC-04 [D] · SOC-06 [D] | Domaine | Oui | Confirmé |
| EXP-03 | Must | Billing EXPORT | Après codification, orientation selon les mêmes variantes par type de client que l'import ; le Pilote Facturation crée le Draft IN avant l'édition de la facture ; le document final est le BEE | Pilote Facturation · Pilote CD (DEFINED_ROLE) | EXP-02 [E] · CPT-02 [E] | Domaine | Oui | Confirmé |
| EXP-04 | Must | Billing EXPORT | Client IES export : la facture et le BEE sont générés côté plateforme IES après traitement parallèle et remise des documents dans le ticket | Client IES | IMP-40 [D] | Domaine | Oui | Confirmé |

---

## F — Simulation de Proforma (SIM)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| SIM-01 | Must | Proforma | Le client peut lancer une simulation de proforma **avant, si nécessaire**, de démarrer son parcours de facturation, en saisissant les caractéristiques de son dossier et de ses conteneurs. *(La simulation est optionnelle et indépendante du parcours — elle n'est pas un pré-requis obligatoire)* | Client | — | Application · UX | Oui | Confirmé |
| SIM-02 | Must | Proforma | Le moteur calcule, par conteneur : acconage, relevage, branchement frigorifique (Reefer), majoration lourd, majoration dangereux, majoration hors gabarit, stationnement par tranches et répartition entre bénéficiaires | Système | SIM-03 [E] · SIM-04 [E] | Domaine | Oui | Confirmé |
| SIM-03 | Must | Proforma | Le tarif applicable est déterminé par la clé Sens + Taille + Catégorie de marchandise, à partir d'une grille tarifaire administrable | Admin fonctionnel | SIM-06 [D] | Domaine · Application | Oui | Confirmé |
| SIM-04 | Must | Proforma | La durée de séjour est déduite des dates d'arrivée et d'enlèvement prévisionnel ; elle alimente le calcul du branchement frigorifique et du stationnement | Système | — | Domaine | Oui | Confirmé |
| SIM-05 | Must | Proforma | Le résultat présente le détail des postes par conteneur, un total général et la mention légale indiquant que la proforma est fournie à titre indicatif et n'engage pas CIT | Client | SIM-02 [E] | Application · UX | Oui | Confirmé |
| SIM-06 | Must | Proforma | Tous les paramètres de calcul sont administrables sans développement : grilles, taux de majoration, seuils de poids, franchises, tranches de stationnement, quotes-parts de répartition, référentiels | Admin fonctionnel | — | Application | Oui | Confirmé |
| SIM-07 | Must | Proforma | Les grilles tarifaires sont versionnées avec une date d'effet ; une simulation référence la version de tarif appliquée ; l'historique des grilles est conservé | Admin fonctionnel | SIM-06 [E] | Domaine · Application | Oui | Confirmé |
| SIM-08 | Should | Proforma | Le client peut enregistrer une simulation, la retrouver sur son profil et la rattacher ultérieurement à un ticket de parcours | Client | SIM-01 [D] | Application | Oui | Confirmé |
| SIM-09 | Should | Proforma | Le client peut exporter/imprimer le résultat de simulation en PDF | Client | SIM-05 [D] | Application | Oui | Confirmé |
| SIM-10 | Should | Proforma | L'administrateur peut charger une nouvelle grille tarifaire par import (ex. tableur) en plus de la saisie manuelle | Admin fonctionnel | SIM-06 [D] | Application | Oui | Confirmé |
| SIM-11 | Could | Proforma | Contrôles de cohérence à la saisie (taille et type reconnus, catégorie compatible avec le sens) avec messages d'aide | Client | SIM-01 [D] | UX | Oui | Confirmé |

---

## G — Avoir et Exonération (AVE)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| AVE-01 | Must | Avoir/Exo | Le client peut soumettre une demande d'avoir ou d'exonération avec les justificatifs exigés ; chaque demande génère un ticket | Client | SOC-01 [D] | Application | Oui | Confirmé |
| AVE-02 | Must | Avoir/Exo | Chaque type de demande suit son propre workflow de validation, avec étapes et intervenants paramétrables | Admin fonctionnel | AVE-01 [E] · WKF-01 [D] | Domaine | Oui | **À valider (PC-15)** |
| AVE-03 | Must | Avoir/Exo | La décision (accord, rejet motivé) et les documents associés sont déposés dans le ticket et notifiés au client | Agent habilité (ROLE_TO_CONFIRM — cf. PC-03 et PC-15) | AVE-02 [E] · COM-01 [D] | Domaine | Oui | Confirmé |
| AVE-04 | Should | Avoir/Exo | Une demande d'avoir/exonération peut être liée au ticket de parcours client concerné | Client · Agent | SOC-10 [D] | Application | Oui | Confirmé |

---

## H — Customer Complaint (REC)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| REC-01 | Must | Customer Complaint | Le client crée une demande en sélectionnant une catégorie de réclamation ; chaque demande génère un ticket tracé de bout en bout | Client | SOC-01 [D] | Application | Oui | Confirmé |
| REC-02 | Must | Customer Complaint | Les catégories sont paramétrables et dynamiques (ajout/modification sans développement) | Admin fonctionnel | — | Application | Oui | Confirmé |
| REC-03 | Must | Customer Complaint | Les tickets sont orientés vers le Support Opérationnel, qui peut instruire, demander des compléments, joindre des documents, échanger avec le client et orienter vers les services concernés | Support Opérationnel (DEFINED_ROLE) | SOC-04 [D] · SOC-06 [D] | Application | Oui | Confirmé |
| REC-04 | Must | Customer Complaint | La clôture d'une réclamation comporte une réponse formalisée au client, conservée dans le ticket | Support Opérationnel (DEFINED_ROLE) | REC-03 [E] · SOC-09 [D] | Domaine | Oui | Confirmé |
| REC-05 | Should | Customer Complaint | Règles d'orientation automatique par catégorie (file ou service destinataire paramétrable) | Admin fonctionnel | REC-02 [E] · WKF-01 [D] | Application | Oui | Confirmé |
| REC-06 | Could | Customer Complaint | Enquête de satisfaction à la clôture du ticket | Client | REC-04 [D] | Application · UX | Oui | Confirmé |

---

## I — Enrôlement Transporteurs (ENR)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| ENR-01 | Must | Enrôlement | Le chauffeur ou le transporteur renseigne à distance les informations nécessaires et dépose les pièces demandées ; la liste des informations et pièces est paramétrable | Transporteur/Chauffeur | SOC-01 [D] | Application · UX | Oui | Confirmé |
| ENR-02 | Must | Enrôlement | La soumission génère un ticket transmis aux agents Gate, qui peuvent vérifier, demander des compléments ou valider | Agent Gate (DEFINED_ROLE) | SOC-04 [D] · SOC-06 [D] | Application | Oui | Confirmé |
| ENR-03 | Must | Enrôlement | Le déplacement physique du chauffeur n'est requis que pour les étapes qui l'exigent (photo, badge) ; le ticket trace la convocation et la réalisation | Agent Gate (DEFINED_ROLE) | COM-01 [D] | Domaine | Oui | Confirmé |
| ENR-04 | Should | Enrôlement | Notification au chauffeur (SMS/WhatsApp prioritaires) à chaque étape clé : dossier validé, convocation, badge disponible | Transporteur/Chauffeur | COM-01 [E] | Application | Oui | Confirmé |
| ENR-05 | Could | Enrôlement | Gestion du renouvellement des badges avec rappel automatique avant expiration | Transporteur/Chauffeur · Agent Gate | COM-01 [D] | Application | Oui | Confirmé |

---

## J — Communication et notifications (COM / NOT)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| COM-01 | Must | Notifications | La plateforme notifie le client via : e-mail, notifications portail, et au moins un canal de messagerie mobile (WhatsApp et/ou SMS) | Client | — | Application · Infrastructure | Oui | Confirmé |
| COM-02 | Must | Notifications | Chaque notification comporte un lien invitant le client à se connecter directement à son ticket (accès conditionnel à l'authentification — SEC-12) | Client | SEC-12 [E] | Application · Sécurité | Oui | Confirmé |
| COM-03 | Must | Notifications | Les événements déclencheurs et les modèles de messages sont paramétrables par l'administrateur fonctionnel (par module, par étape, par canal) | Admin fonctionnel | — | Application | Oui | Confirmé |
| COM-04 | Must | Notifications | Le chat/commentaires dans le ticket constitue le canal d'échange de référence entre le client et l'agent ; les messages sont horodatés et conservés. *(Réaffirmation de SOC-05 dans le contexte des communications — un seul journal de messages par ticket)* | Client · Agents | SOC-05 [E] | Application | Oui | Confirmé |
| COM-05 | Should | Notifications | Préférences de canaux par client (activation/désactivation par canal, dans le respect des notifications obligatoires) | Client | COM-01 [D] | Application · UX | Oui | Confirmé |
| COM-06 | Could | Notifications | Relances automatiques paramétrables lorsque le ticket est en attente d'une action client au-delà d'un délai défini. *(Précise la dimension canal de CYC-04 — le mécanisme de relances est Must via CYC-04 ; les canaux de notification des relances sont Could via COM-06)* | Admin fonctionnel | CYC-04 [D] | Application | Oui | Confirmé |
| NOT-01 | Must | Notifications | Chaque notification dispose d'un état Lu / Non lu et permet d'accéder directement au ticket concerné | Client · Agents | COM-01 [D] | Application · UX | Oui | Confirmé |
| NOT-02 | Must | Notifications | Les règles de diffusion sont paramétrables par événement et par canal | Admin fonctionnel | COM-03 [E] | Application | Oui | Confirmé |
| NOT-03 | Should | Notifications | Un indicateur du nombre de notifications non lues est visible en permanence dans le portail | Client · Agents | NOT-01 [D] | UX | Oui | Confirmé |

---

## K — Continuité digital/présentiel (CON)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| CON-01 | Must | Transverse | Un client peut commencer son parcours en ligne et le poursuivre physiquement (ou inversement) sans recommencer son dossier : le numéro de ticket permet à l'agent de retrouver l'historique et l'étape en cours | Client · Agent (FUNCTIONAL_RESPONSIBILITY) | SOC-01 [E] | Domaine · UX | Oui | Confirmé |
| CON-02 | Must | Transverse | Les agents de guichet (FUNCTIONAL_RESPONSIBILITY) disposent d'une recherche rapide de ticket (numéro, client, BL/booking) pour reprendre un dossier au guichet | Agent (FUNCTIONAL_RESPONSIBILITY) | SOC-01 [D] | Application · UX | Oui | Confirmé |
| CON-03 | Must | Transverse | La plateforme permet au client de prendre un rendez-vous lorsque la présence physique est nécessaire ; le rendez-vous est rattaché à son ticket/dossier et lui donne accès au bureau de facturation | Client | SOC-01 [D] | Application | Oui | Confirmé |
| CON-04 | Should | Transverse | La plateforme et le système de gestion de file d'attente existant restent deux systèmes distincts ; une redirection est possible ; une intégration API constitue une évolution future et n'est pas requise | — | — | Infrastructure | Partiel | Confirmé |

---

## L — Assistant IA (IA)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| IA-01 | Must | Assistant IA | L'assistant répond aux questions simples des clients (fonctionnement du portail, pièces à fournir, état d'avancement) en langage naturel, en français | Client | — | Application | Partiel (critères de qualité des réponses à définir) | Confirmé |
| IA-02 | Must | Assistant IA | L'assistant oriente le client dans ses démarches (choix du parcours, du module, de la catégorie de réclamation) et l'aide à compléter ses dossiers | Client | — | Application · UX | Partiel (critères d'orientation correcte à définir) | Confirmé |
| IA-03 | Must | Assistant IA | L'assistant escalade vers un opérateur humain lorsque cela est nécessaire ; le passage se fait sans perte de contexte (le fil est conservé dans le ticket) | Client · Agent | SOC-05 [E] | Application | Oui | Confirmé |
| IA-04 | Should | Assistant IA | L'assistant s'appuie sur une base de connaissances administrable (FAQ, procédures) ; il cite la source de ses réponses lorsque pertinent | Admin fonctionnel | — | Application | Partiel (qualité des citations à définir) | Confirmé |
| IA-05 | Should | Assistant IA | L'assistant est accessible depuis le portail et, si possible, depuis les canaux de messagerie configurés | Client | COM-01 [D] | Infrastructure | Partiel | Confirmé |
| IA-06 | Must | Assistant IA | L'assistant identifie les situations nécessitant une intervention humaine et permet le transfert vers un opérateur habilité sans perte de contexte | Système | IA-03 [E] | Application | Partiel (définition des situations à spécifier) | Confirmé |
| IA-07 | Must | Assistant IA | Le client peut demander une intervention humaine explicitement ; l'assistant réduit les sollicitations inutiles sans constituer une barrière | Client | IA-06 [E] | Application · UX | Oui | Confirmé |

---

## M — Sécurité (SEC)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| SEC-01 | Must | Sécurité | Authentification obligatoire pour tout accès ; politique de mots de passe robuste ; verrouillage après échecs répétés | Tous | — | Sécurité | Oui | Confirmé |
| SEC-02 | Must | Sécurité | Contrôle d'accès par rôles (RBAC) appliquant le moindre privilège, y compris au niveau des champs pour les traitements parallèles | Tous | — | Sécurité | Oui | Confirmé |
| SEC-03 | Must | Sécurité | Cloisonnement strict : un client n'accède qu'à ses tickets ; un agent n'accède qu'aux files et actions de son rôle | Tous | SEC-02 [E] | Sécurité | Oui | Confirmé |
| SEC-04 | Must | Sécurité | Chiffrement des échanges (HTTPS/TLS) et protection des documents stockés | — | — | Infrastructure · Sécurité | Oui | Confirmé |
| SEC-05 | Must | Sécurité | Journal d'audit complet et non modifiable des actions utilisateurs et administrateurs. *(Note : ce journal unique est aussi décrit par SOC-13 — deux exigences pour le même objet, portées complémentaires)* | — | SOC-13 [D] | Sécurité | Oui | Confirmé |
| SEC-06 | Must | Sécurité | Protection des données personnelles conformément à la réglementation ivoirienne (ARTCI) : minimisation, durées de conservation, droits des personnes | — | — | Sécurité · Juridique | Partiel (durées de conservation à valider — PC-14) | Confirmé |
| SEC-07 | Must | Sécurité | Contrôle des fichiers déposés : types autorisés, taille maximale, analyse antivirale | Système | SOC-07 [E] | Sécurité · Infrastructure | Oui | Confirmé |
| SEC-08 | Should | Sécurité | Second facteur d'authentification pour les profils agents et administrateurs. *(Note : exigence indépendante de CPT-07 qui concerne les clients)* | Agents · Admins | — | Sécurité | Oui | Confirmé |
| SEC-09 | Should | Sécurité | Cloisonnement des environnements (production, recette) et gestion des habilitations par environnement | Admin technique | — | Infrastructure | Oui | Confirmé |
| SEC-10 | Must | Sécurité | Les sessions utilisateurs expirent après une période d'inactivité paramétrable | Tous | — | Sécurité | Oui | Confirmé |
| SEC-11 | Must | Sécurité | Les comptes agents peuvent être désactivés immédiatement, sans suppression de leur historique | Admin fonctionnel | — | Sécurité | Oui | Confirmé |
| SEC-12 | Must | Sécurité | Aucun lien transmis par notification ne donne accès direct à un document protégé sans contrôle préalable d'identité et d'habilitations : le lien conduit au ticket après authentification | Système | COM-01 [E] · COM-02 [E] | Sécurité | Oui | Confirmé |

---

## N — Exigences non fonctionnelles et gestion des erreurs (NFR / ERR)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| NFR-01 | Must | Transverse | Interface en français ; ergonomie simple adaptée à des utilisateurs non techniciens | Tous | — | UX | Oui | Confirmé |
| NFR-02 | Must | Transverse | Portail client responsive, utilisable sur mobile (une part importante des clients et chauffeurs utilisera un smartphone) | Client · Chauffeur | — | UX · Infrastructure | Oui | Confirmé |
| NFR-03 | Must | Transverse | Disponibilité cible en heures ouvrées ≥ 99 % ; maintenance planifiée hors heures de pointe | — | — | Infrastructure | Oui | Confirmé |
| NFR-04 | Must | Transverse | Temps de réponse < 3 secondes pour les écrans courants dans des conditions normales de charge | — | — | Infrastructure | Oui (test de charge requis) | Confirmé |
| NFR-05 | Must | Transverse | Volumétrie dimensionnante à confirmer en phase de cadrage : tickets/jour, taille des pièces jointes, utilisateurs simultanés | — | — | Infrastructure | Partiel (à valider — PC-01) | **À valider (PC-01)** |
| NFR-06 | Must | Transverse | Sauvegardes régulières (données et documents) et procédure de restauration testée | Admin technique | — | Infrastructure | Oui | Confirmé |
| NFR-07 | Should | Transverse | Interface disponible en anglais (clients internationaux) | Client | NFR-01 [D] | UX | Oui | Confirmé |
| NFR-08 | Should | Transverse | Architecture ouverte exposant des API pour faciliter d'éventuelles intégrations futures | — | — | Infrastructure | Oui | Confirmé |
| NFR-09 | Should | Transverse | Hébergement conforme aux standards du groupe ; le choix (infrastructure CIT/AGL ou cloud) sera arrêté selon le scénario de réalisation retenu | Admin technique | — | Infrastructure | Partiel (à décider — PC-13) | **À décider (PC-13)** |
| NFR-10 | Must | Transverse | La plateforme est accessible 24h/24 et 7j/7, hors périodes de maintenance planifiées | Client | — | Infrastructure | Oui | Confirmé |
| NFR-11 | Must | Transverse | Les services CIT conservent leurs horaires de fonctionnement propres pour le calcul des SLA ; un client peut déposer un dossier hors heures ouvrées ; le SLA de traitement démarre selon les règles de la file | — | SLA-04 [D] | Domaine | Oui | Confirmé |
| ERR-01 | Must | Transverse | Aucune perte de données saisies en cas d'incident lors d'une soumission (brouillon ou reprise de saisie) | Client | — | Application | Oui | Confirmé |
| ERR-02 | Must | Transverse | Échec d'envoi d'une notification : nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres | Système | COM-01 [D] | Application · Infrastructure | Oui | Confirmé |
| ERR-03 | Must | Transverse | Les documents déposés sont vérifiés à l'upload (intégrité, type, taille) avec message d'erreur explicite | Client · Agents | SOC-07 [E] · SEC-07 [E] | Application | Oui | Confirmé |
| ERR-04 | Should | Transverse | Mode dégradé documenté : en cas d'indisponibilité du portail, le parcours physique reste possible ; la reprise s'effectue par le numéro de ticket | — | CON-01 [D] | Infrastructure | Partiel | Confirmé |

---

## O — Écrans et UX (ACR)

| Référence | Priorité | Module | Exigence | Acteur(s) | Dépendances | Couche | Testable | Statut |
|-----------|----------|--------|----------|-----------|-------------|--------|----------|--------|
| ACR-01 | Must | Espace client | Le tableau de bord client met clairement en évidence les actions attendues de sa part (« Mes actions requises »), distinctes de la simple liste des tickets ouverts (ex. : fournir un document, paiement attendu, rendez-vous à confirmer), chacune renvoyant au ticket concerné | Client | SOC-01 [D] · SOC-08 [D] | UX · Application | Oui | Confirmé |

**Source CDC** : Section 17.1 « Espace client ».

---

## Récapitulatif par priorité (recompté ligne par ligne sur la matrice, 28 août 2026)

> Recompte intégral effectué en dénombrant chaque référence de la matrice ci-dessus (aucun report des anciens totaux). Chaque référence n'apparaît qu'une seule fois dans la matrice : le total par priorité et le total par module désignent donc le **même ensemble** de 188 exigences — il n'y a pas de double-comptage à expliquer.

| Priorité | Nombre d'exigences |
|----------|-------------------|
| **Must** | **150** |
| **Should** | **32** |
| **Could** | **6** |
| **Total** | **188** |

## Récapitulatif par module (recompté ligne par ligne sur la matrice)

| Module | Must | Should | Could | Total |
|--------|------|--------|-------|-------|
| Socle tickets (SOC, CYC, MSG) | 22 | 3 | 1 | 26 |
| Moteur workflow / files / supervision (WKF, PEC, TRF, TPS, SLA, ALE, SUP, DIS, AFF, PRI, MAS) | 40 | 8 | 0 | 48 |
| Comptes clients / Sociétés (CPT, ENT) | 8 | 2 | 1 | 11 |
| Billing Office IMPORT (IMP, UNI) | 23 | 2 | 0 | 25 |
| Billing Office EXPORT (EXP) | 4 | 0 | 0 | 4 |
| Simulation Proforma (SIM) | 7 | 3 | 1 | 11 |
| Avoir / Exonération (AVE) | 3 | 1 | 0 | 4 |
| Customer Complaint (REC) | 4 | 1 | 1 | 6 |
| Enrôlement Transporteurs (ENR) | 3 | 1 | 1 | 5 |
| Notifications (COM, NOT) | 6 | 2 | 1 | 9 |
| Continuité digital/présentiel (CON) | 3 | 1 | 0 | 4 |
| Assistant IA (IA) | 5 | 2 | 0 | 7 |
| Sécurité (SEC) | 10 | 2 | 0 | 12 |
| Non fonctionnelles et erreurs (NFR, ERR) | 11 | 4 | 0 | 15 |
| Écrans et UX (ACR) | 1 | 0 | 0 | 1 |
| **TOTAL** | **150** | **32** | **6** | **188** |

> Les deux tableaux ci-dessus totalisent bien 188 dans les deux sens (par priorité et par module) : c'est le chiffre de référence unique pour la traçabilité Spec Kit. L'écart avec les décomptes précédents (135 puis 182, voir `03-revue-coherence-analyse.md`) venait d'un dénombrement manuel non recalculé sur le contenu réel de la matrice ; il est résorbé par ce recomptage intégral.

> **Réversibilité (§23.1)** : La réversibilité est un **critère d'évaluation et d'acquisition** (non une exigence numérotée du CDC). Elle n'est donc pas comptée dans la matrice. Elle est documentée dans `00-analyse-cdc.md` (section 12) et dans `02-points-a-clarifier.md`.

---

*Matrice générée lors de la pré-analyse du CDC. Aucune implémentation ne commence avant validation.*
