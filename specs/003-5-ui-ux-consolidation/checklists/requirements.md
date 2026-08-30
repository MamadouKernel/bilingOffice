# Quality Checklist: Consolidation UI/UX F001-F003

> **Feature**: `specs/003-5-ui-ux-consolidation/`  
> **Type**: Spécification d'Expérience Utilisateur & Navigation  
> **Date**: 2026-08-30  
> **Statut**: Clarifié (`/speckit.clarify` complété)

---

## 1. Respect des Contraintes Non-Négociables (Constitution)

- [x] **Aucune règle métier inventée** : La spécification réutilise exclusivement les règles et états existants de F001, F002 et F003.
- [x] **Aucun rôle inventé** : Seuls les rôles existants (`Client`, `Agent`, `Superviseur`, `Administrateur`) et les permissions paramétrables sont utilisés.
- [x] **Sécurité côté serveur (Principe VII)** : Les contrôles d'accès et de cloisonnement restent appliqués dans la couche Application/Domaine ; aucune sécurité ne repose uniquement sur le masquage UI.
- [x] **Non-régression sur le Domaine** : Le Domaine reste pur et inchangé ; les éventuelles nouvelles requêtes sont de purs Read Models / Queries de présentation.
- [x] **Périmètre négatif respecté** : Aucune fonctionnalité d'Export, Simulation, Avoir, Complaint ou Enrôlement n'est anticipée.
- [x] **Baseline de non-régression** : 260 tests existants (37 Domain, 159 Application, 36 Infrastructure, 28 Web) préservés à 100 %.

---

## 2. Exhaustivité des Clarifications UX

- [x] **CLAR-UX-01 (Landing & Redirection)** : Redirection automatique selon le contexte utilisateur et persona fonctionnel + sélecteur d'espace si multi-habilité.
- [x] **CLAR-UX-02 (Persona ≠ Rôle)** : Adaptation dynamique du menu à la combinaison exacte des habilitations détenues.
- [x] **CLAR-UX-03 (Navigation & Sécurité)** : Masquage contextuel ergonomique et refus d'accès 403 systématique côté serveur.
- [x] **CLAR-UX-04 (Scope Demandes Client)** : Application stricte de `ENT-03` / `CLAR-02` (propres demandes vs. toutes les demandes entreprise).
- [x] **CLAR-UX-05 (Mes actions requises - `ACR-01`)** : Dérivation rigoureuse des états réels existants (complément, attente preuve paiement, refus paiement) et disparition automatique après traitement.
- [x] **CLAR-UX-06 (Dashboard Agent & Files)** : Vue des files habilitées, compteurs de tickets et sélecteur de disponibilité opérationnelle (`DIS-01`).
- [x] **CLAR-UX-07 (Dashboard Admin Société)** : Espace entreprise pour piloter les collaborateurs et visualiser l'activité globale de la société.
- [x] **CLAR-UX-08 (Fiche Ticket Contextuelle)** : Affichage conditionné des actions autorisées dans l'état courant.
- [x] **CLAR-UX-09 (Confidentialité Fiche Ticket)** : Cloisonnement strict des notes internes (`MSG-02`) et des branches IES (`PC-05`).
- [x] **CLAR-UX-10 (Historique vs. Audit)** : Historique fonctionnel visible client/agent ; journal d'audit (`SOC-13`) réservé admin/superviseur.
- [x] **CLAR-UX-11 (Éradication des GUIDs)** : 0 champ de saisie de GUID dans les parcours cibles.
- [x] **CLAR-UX-12 (Retour après action)** : Rafraîchissement sur place pour ticket, retour file pour agent, redirection dossier pour nouvelle demande.
- [x] **CLAR-UX-13 (Traçabilité `ACR-01`)** : Exigence contractuelle Must (Section 17.1 CDC) formellement tracée (111 exigences CDC couvertes au total).

---

## 3. Bilan de Conformité et État de Préparation

| Rubrique | Statut | Commentaire |
|---|:---:|---|
| **Alignement CDC & Constitution** | ✅ CONFORME | Respect strict de l'architecture et des principes de sécurité |
| **Prise en compte de l'audit UI/UX** | ✅ CONFORME | 100 % des ruptures identifiées dans `docs/06` sont clarifiées et traitées |
| **Zéro régression backend** | ✅ CONFORME | Baseline de 260 tests réaffirmé |
| **Prêt pour `/speckit.plan`** | ✅ **READY** | Toutes les questions structurantes sont résolues |
