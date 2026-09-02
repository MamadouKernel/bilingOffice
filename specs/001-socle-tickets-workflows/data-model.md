# Data Model — Modèle conceptuel fonctionnel

**Feature** : `001-socle-tickets-workflows` | **Date** : 2026-08-28 | **Phase 1 de `/speckit.plan`**

> **Cadre de ce document** : conformément à la constitution (Portée — pas de code, pas de schéma SQL
> détaillé, pas de classes de domaine) et à la session `/speckit.clarify`, ce document décrit le modèle
> **au niveau fonctionnel uniquement** : entités, attributs métier significatifs, relations, invariants
> et traçabilité vers les exigences. **Aucun DDL, aucun type SQL, aucune classe C#, aucun pattern
> technique.** Le mapping physique (tables, index, colonnes) sera défini en `/speckit.tasks` et
> `/speckit.implement` dans le respect des décisions D3 (SQL Server + EF Core), D6 (séquences) et
> D8 (deux journaux).

---

## 1. Entités du socle

### 1.1 Référentiel & organisation

| Entité | Description | Attributs métier significatifs | Exigences |
|---|---|---|---|
| **Utilisateur** | Compte client CIT ou agent CIT | Identité, contact, actif/inactif, nature (client/agent) | SEC-01, SEC-02, SEC-11 |
| **Rôle & Habilitation** | Permissions attribuables, rattachements paramétrables | Libellé, permissions (transfert, clôture, réouverture, supervision…), rattachements | SEC-11, PC-03, PC-20 |
| **File** | File de traitement d'un module | Module, types de tickets admis, stratégie d'affectation active (AFF-07) | SOC-04, WKF-01, AFF-07 |
| **Rattachement File↔Habilitation** | Qui peut voir/travailler une file | File, habilitation requise | SEC-11, WKF-01 |
| **Périmètre de supervision** | Rattachement superviseur → files/services, borné et configurable | Superviseur, files/services couverts (CLAR-05) | SUP-01..05, PC-04 |
| **Disponibilité** | Statut de disponibilité d'un agent | Statut (disponible/indisponible/absent), dernière activité | DIS-01..03 |

### 1.2 Tickets & dossiers

| Entité | Description | Attributs métier significatifs | Exigences |
|---|---|---|---|
| **Dossier** | Regroupement de tickets d'un client pour un même sujet | Numéro unique jamais réutilisé, client, objet ; **cycle de vie dérivé des tickets, sans état ni action propre (CLAR-01)** | SOC-09, SOC-11 |
| **Ticket** | Unité de travail complète | Numéro unique, module, type, objet, description, **statut** (reçu, en attente de prise en charge, en cours, en attente client, clôturé, annulé), priorité résultante | SOC-01..09, CYC-01, PRI-01..03 |
| **Historique du ticket** | Chronologie fonctionnelle, visible client/agents habilités | Événements horodatés (création, réponse, changement de statut, transfert, affectation, clôture…), append-only | SOC-09, TRF-02 |
| **Transition de statut** | Changement de statut tracé | Statut initial, statut final, auteur, motif | CYC-01..07 |
| **Demande de réouverture** | Demande client sur ticket clôturé | Motif, décision, traitant | CYC-07 |
| **Priorité** | Résultat des règles paramétrables | Niveau, facteurs (module, type, impact client), origine (règle/dérogation manuelle) | PRI-01..04 |

### 1.3 Messagerie & documents

| Entité | Description | Attributs métier significatifs | Exigences |
|---|---|---|---|
| **Message** | Échange dans le fil du ticket | Auteur, contenu, horodatage, destinataires/vision, pièce jointe | MSG-01..05 |
| **Document** | Pièce attachée à un ticket/message | Métadonnées + contenu stocké **derrière le port de stockage** (D9), statut d'analyse antivirus (SEC-07), habilitations d'accès (SEC-12), conservation = paramètre (PC-14) | SOC-12, SEC-07, SEC-12 |

### 1.4 Files, transferts & affectation

| Entité | Description | Attributs métier significatifs | Exigences |
|---|---|---|---|
| **Transfert** | Passage d'une file à une autre | File source, file cible, initiateur, motif, destinataire éventuel (TRF-03) ; **ticket arrivant non affecté** (CLAR-03) | TRF-01..06 |
| **Matrice des transitions de files** | Paramétrage riche par transition | File source, file cible, **rôles habilités à initier**, **motifs autorisés** (CLAR-04) — contenu validé en atelier métier (PC-07) | TRF-06, WKF-01 |
| **Affectation / Prise en charge** | Lien opérateur–ticket | Opérateur, horodatage de prise en charge, mode (prise libre AFF-01, manuelle AFF-02, auto AFF-03..06) | PEC-01..05, AFF-01..07 |

### 1.5 Temps & SLA

| Entité | Description | Attributs métier significatifs | Exigences |
|---|---|---|---|
| **Mesure de temps** | Segments temporels du ticket | Attente, traitement, attente client (T0/T1), horodatages UTC (D7) | TPS-01..05 |
| **Échéance SLA** | Cible associée au ticket | Échéance calculée sur calendrier ouvré, statut (respectée/alerte/dépassée) | SLA-01..03 |
| **Paramètres SLA & calendrier** | Référentiel administrable | Cibles par module/type/priorité, seuils (info/alerte/dépassement), horaires ouvrés, jours fériés, fuseau CIT — **valeurs = PC-12, non inventées** | SLA-05, PC-12 |

### 1.6 Alertes, supervision, journaux

| Entité | Description | Attributs métier significatifs | Exigences |
|---|---|---|---|
| **Alerte** | Notification interne d'événement | Type (SLA, absence de réponse…), niveau, destinataire/rôle, statut de traitement | ALE-01..03 |
| **Vue de supervision** | Projections de lecture (files, charge, disponibilité, dernière activité, alertes) | Alimentées par événements et lectures ; rafraîchissement = paramètre (PC-02) | SUP-01..05 |
| **Journal d'audit** | Registre technique **append-only** (D8) | Auteur, horodatage, action, entité, avant/après — distinct de l'historique fonctionnel | SEC-05, PRI-03, TRF-05, DIS-03, MAS-02 |
| **Journal d'événements** | Événements du socle **persistés** (D10) | Type d'événement, payload fonctionnel, horodatage — consommé in-process ; future feature Notifications (PC-25) | MSG-04, ALE-01 |

## 2. Relations principales

```text
Utilisateur (client) 1..N — Dossier 1..N — Ticket 1 — File (à un instant donné)
                              │                        ├─ 1..N Message
                              │                        ├─ 1..N Document
                              │                        ├─ 1..N Mesure de temps / Échéance SLA
                              │                        ├─ 1..N Alerte
                              │                        ├─ 1..N Transfert (source → cible)
                              │                        └─ 1..N Affectation / Prise en charge
                              │
Utilisateur (agent) 1..N — Habilitation(s) — Rattachement(s) File — Périmètre de supervision
Utilisateur (agent) 1 — 1 Disponibilité
Ticket 1..N — Transition de statut — Historique du ticket / Journal d'audit
```

## 3. Invariants fonctionnels (extraits traçables)

1. Un **numéro de ticket/dossier est unique et jamais réutilisé**, y compris après annulation ou
   archivage (SOC-01, SOC-09) — attribution transactionnelle (D6).
2. Un **dossier** n'a **pas de cycle de vie propre** : son état perçu dérive de ses tickets ; il est
   consultable en permanence et réactivé automatiquement à la réouverture d'un de ses tickets
   (SOC-11, CLAR-01).
3. À un instant donné, un ticket appartient à **exactement une file** et est soit **non affecté**,
   soit **affecté à exactement un agent actif habilité** de cette file (PEC-01, AFF, SEC-11).
4. Un **transfert** déplace le ticket vers la file cible **sans affectation héritée** : le ticket
   arrive « en attente de prise en charge », sauf destinataire désigné (TRF-01/03, CLAR-03) ; un
   nouveau temps d'attente démarre (TPS-01, SLA-01).
5. La **réouverture** ne s'applique qu'aux tickets **clôturés** (clôture normale ou pour absence de
   réponse) ; un ticket **annulé** n'est jamais rouvert (CYC-06/07, CLAR-02).
6. Toute mutation d'état, d'affectation ou de file produit **au minimum** : une entrée d'historique
   fonctionnel et une entrée de journal d'audit (SOC-09, D8).
7. Les **mesures de temps** distinguent attente, traitement et attente client ; l'attente client
   suspend le décompte SLA (TPS-02/04, SLA-03).
8. Les calculs de temps et de SLA s'effectuent sur **horodatages UTC** et sur le **calendrier ouvré
   paramétrable** (D7).
9. Aucun élément historisé ne peut être **supprimé physiquement** (SOC-09) ; le journal d'audit est
   **append-only** (D8).
10. L'accès à un document exige, **à chaque accès**, identité et habilitation adéquates (SEC-12) ;
    un document non validé (antivirus, SEC-07) n'est pas accessible.
11. **Jamais** d'affectation automatique d'un agent Indisponible ou Absent (DIS-01).

## 4. Données paramétrables (administrées, jamais codées en dur)

Conformément aux Principes VI et XIII, les éléments suivants sont des **données de paramétrage** :

| Paramètre | Portée | Point ouvert |
|---|---|---|
| Matrice des transitions de files (destinations, rôles initiateurs, motifs) | Par transition | PC-07 (contenu — atelier métier) |
| Stratégie d'affectation par file | Par file | — (AFF-07) |
| Habilitations et rattachements (rôles ↔ files, droits de clôture…) | Global | PC-03, PC-20 |
| Périmètres de supervision | Par superviseur | PC-04 |
| Cibles SLA, seuils (info/alerte/dépassement), horaires ouvrés, jours fériés | Global / par module-type-priorité | PC-12 |
| Règles de priorité (facteurs et pondérations) | Global | PRI-04 |
| Relances d'absence de réponse (nombre, délai) et délai d'abandon | Global / par type | CYC-04 |
| Politique de conservation des documents | Global | PC-14 |
| Fréquence de rafraîchissement des vues de supervision | Global | PC-02 |
| Canaux de notification | Hors socle (feature Notifications) | PC-25 |

## 5. Traçabilité entités → familles d'exigences

| Famille | Entités principales |
|---|---|
| SOC (01–14) | Ticket, Dossier, File, Message, Document, Historique |
| CYC (01–07) | Ticket, Transition de statut, Demande de réouverture, Paramètres de relance |
| MSG (01–05) | Message, Journal d'événements |
| WKF (01–03) | File, Matrice des transitions, Journal d'audit |
| PEC (01–05) | Affectation/Prise en charge, Disponibilité |
| TRF (01–06) | Transfert, Matrice des transitions, Historique |
| TPS (01–05) | Mesure de temps |
| SLA (01–05) | Échéance SLA, Paramètres SLA & calendrier |
| ALE (01–03) | Alerte, Journal d'événements |
| SUP (01–05) | Vue de supervision, Périmètre de supervision |
| DIS (01–03) | Disponibilité, Journal d'audit |
| AFF (01–07) | Affectation, File (stratégie), Habilitation |
| PRI (01–04) | Priorité, Règles de priorité, Journal d'audit |
| SEC (01–12) | Utilisateur, Rôle & Habilitation, Document, Journal d'audit |

> **Limite assumée** : ce modèle est volontairement à un niveau d'abstraction fonctionnel. Les
> attributs techniques (clés, types, index, tables d'association, dénormalisations de lecture pour la
> supervision) relèvent de la conception détaillée (`/speckit.tasks`, `/speckit.implement`) sous le
> contrôle de D3/D6/D8 et du Principe VIII.