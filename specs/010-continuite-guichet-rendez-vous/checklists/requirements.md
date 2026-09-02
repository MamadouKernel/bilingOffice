# Checklist d'Exigences Fonctionnelles Clarifiées — Feature 010
## Continuité Guichet & Rendez-vous

> **Document** : `specs/010-continuite-guichet-rendez-vous/checklists/requirements.md`  
> **Statut** : **CLARIFIÉ & PRÊT POUR /speckit.plan**  
> **Date** : 1er septembre 2026

---

## 1. Exigences Cibles (CON / IMP / ERR)

| ID Exigence | Priorité | Description résumée | Critères d'acceptation définis | Statut spécification |
|:---|:---:|:---|:---:|:---:|
| **`CON-01`** | **Must** | Reprise d'un dossier commencé en ligne au guichet (ou inversement) sans ressaisie | ✅ Oui (US2, Scénarios 2.1 & 2.2) | **CLARIFIÉE & VALIDÉE** |
| **`CON-02`** | **Must** | Recherche rapide au guichet par N° ticket, N° dossier, N° BL/booking ou client | ✅ Oui (US1, Scénarios 1.1 à 1.3) | **CLARIFIÉE & VALIDÉE** |
| **`CON-03`** | **Must** | Prise de rendez-vous client rattachée au ticket/dossier pour passage au guichet | ✅ Oui (US3 & US4, Scénarios 3.1 à 4.2) | **CLARIFIÉE & VALIDÉE** |
| **`CON-04`** | **Should** | Indépendance du système de borne d'accueil physique externe (redirection/consigne) | ✅ Oui (Section 4.4, US4) | **CLARIFIÉE & VALIDÉE** |
| **`IMP-15`** | **Should** | Alternative présentielle à l'issue du contrôle BL Import | ✅ Oui (Section 4.5, US2) | **CLARIFIÉE & VALIDÉE** |
| **`ERR-04`** | **Should** | Mode dégradé organisationnel documenté en cas d'indisponibilité du portail | ✅ Oui (Section 4.6, US5) | **CLARIFIÉE & VALIDÉE** |

---

## 2. Scénario Contractuel Transverse (REC-SCE-14)

| Volet REC-SCE-14 | Responsabilité | Statut |
|:---|:---:|:---:|
| Notifications sur chaque canal avec lien vers ticket | Feature 009 | ✅ Clôturé |
| Contrôle des habilitations et étanchéité des files | Feature 001 / 002 | ✅ Clôturé |
| Journal d'audit des opérations | Feature 001 | ✅ Clôturé |
| **Recherche rapide de ticket au guichet** | **Feature 010 (`CON-02`)** | ✅ **Clarifié (US1)** |
| **Prise de rendez-vous donnant accès au bureau** | **Feature 010 (`CON-03`)** | ✅ **Clarifié (US3)** |
| **Continuité et reprise sans ressaisie** | **Feature 010 (`CON-01`)** | ✅ **Clarifié (US2)** |

---

## 3. Garde-fous et Invariants Architecturaux Validés

- [x] **Aucun mode offline complexe / PWA inventé** : architecture hébergée respectée.
- [x] **Aucun moteur de calendrier externe (Google/Exchange) imposé** : créneaux relationnels SQL.
- [x] **Aucun doublon de ticket/dossier** : réutilisation directe de l'entité `Ticket` / `Dossier`.
- [x] **Aucun canal de notification direct dans F010** : découplage via `EvenementSocle` vers F009.
- [x] **Sécurité stricte (Anti-IDOR)** : vérification serveur de l'identité pour la prise de RDV et la recherche guichet.
- [x] **Gestion des conflits de créneau** : rejet propre et message explicite si capacité maximale atteinte.
- [x] **Traçabilité totale** : toute opération de guichet consignée dans `EntreeHistorique` et `JournalAudit`.
- [x] **Aucune contamination externe** : 0 exigence AVE, 0 composant IA.
