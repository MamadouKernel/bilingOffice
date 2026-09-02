# REQUIREMENTS CHECKLIST — FEATURE 013 (POST-CLARIFY)

**Feature** : Feature 013 — Continuité Opérationnelle, Brouillons de Saisie, Exposition d'API & Clôture Démarches  
**Date** : 2026-09-01  

---

## 1. CONTRÔLE DE CONFORMITÉ POST-CLARIFY

- [x] **Périmètre ERR-01 clarifié** : Formulaires majeurs identifiés (Import, Export, Réclamation, Enrôlement) avec sauvegarde automatique/manuelle, reprise après incident, purge sur soumission et cloisonnement CPT-04.
- [x] **Architecture ERR-01 découplée** : Le choix d'implémentation de stockage (serveur vs navigateur vs hybride) est délégué au plan technique.
- [x] **NFR-08 borné au contrat de service** : Façade API sécurisée et documentée (statut ticket, dossier, simulation proforma) sans choix de framework imposé prématurément.
- [x] **SIM-11 strictement aligné sur le CDC** : 4 règles de cohérence validées (taille/type, sens/catégorie, dates, Reefer) sans règle fiscale ou tarifaire inventée.
- [x] **ENR-05 clarifié** : Durée de validité et préavis de rappel paramétrables, calcul d'échéance dynamique, distribution via F009 sans SMS réel (PC-16 respecté).
- [x] **0 migration créée** : Le modèle EF Core reste à 0 pending model changes.
- [x] **Anti-Scope préservé** : Zéro dépendance externe non contractée, zéro composant IA, zéro régression sur les 867 tests existants.

---

## 2. STATUT DES 4 EXIGENCES

| ID | Priorité | Statut Clarification | Traitement `/speckit.plan` |
|:---|:---:|:---:|:---|
| **ERR-01** | Must | **CLARIFIÉ** | Arbitrer l'architecture de persistance (Option A / B / C) et les composants UI |
| **NFR-08** | Should | **CLARIFIÉ** | Définir les endpoints HTTP, l'authentification et l'outil de documentation |
| **SIM-11** | Could | **CLARIFIÉ** | Implémenter les validateurs et messages d'aide dans le composant simulation |
| **ENR-05** | Could | **CLARIFIÉ** | Implémenter la commande de renouvellement et le job/service de rappel via F009 |

---

## 3. VERDICT

- **Clarifications terminées** : **OUI (7/7 décisions consignées)**
- **Prêt pour /speckit.plan** : **OUI**
