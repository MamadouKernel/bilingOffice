# Checklist de Conformité des Exigences — Feature 004 : Billing Office EXPORT

> **Dossier** : `specs/004-billing-office-export/checklists/`  
> **Date** : 30 Août 2026  
> **Référence** : `specs/004-billing-office-export/spec.md`  

---

## 1. Inventaire des Exigences CDC (5/5)

- [x] **EXP-01** (Must) : Soumission multi-bookings & génération d'un ticket indépendant par booking.
- [x] **EXP-02** (Must) : File Back Office (codification IPAKI, validation, rejet motivé, complément).
- [x] **EXP-03** (Must) : Orientation par type de client, Draft IN en facturation, BEE au Contrôle Documentaire.
- [x] **EXP-04** (Must) : Circuit spécifique Client IES Export (facture & BEE sur plateforme IES).
- [x] **UNI-04** (Should) : Unicité sur le numéro de booking actif.

---

## 2. Scénarios de Recette & Traçabilité (1/1)

- [x] **REC-SCE-05** : EXPORT (4 types de clients : Normal, En Compte, Deposit, IES) formellement documenté et couvert par les User Stories US1 à US6.

---

## 3. Points à Clarifier (PC)

- [x] **PC-09** : Règle d'unicité sur le numéro de booking actif explicitement spécifiée et tracée.
- [x] **PC-03** : Rôle d'habilitation Caisse/Finance pour la validation de paiement réutilisé.

---

## 4. Principes d'Architecture & Non-Régression

- [x] Réutilisation de l'architecture Clean Architecture et des patterns établis dans F001, F002 et F003.
- [x] Réutilisation du layout et des composants ergonomiques de F003.5 (Zéro GUID manuel).
- [x] Aucune absorption non autorisée de features futures (Simulation, Complaint, Enrôlement, Notifications, IA).
- [x] Aucun code d'intégration API externe IPAKI automatisé inventé (conforme au CDC : saisie/traçage opérateur).
