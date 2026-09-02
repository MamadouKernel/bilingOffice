# Checklist de Conformité aux Exigences — Feature 006

> **Document** : `specs/006-customer-complaint/checklists/requirements.md`  
> **Date** : 30 Août 2026

## 1. Périmètre strict
- [x] L'ensemble des exigences **REC-01 à REC-06** sont spécifiées.
- [x] **Aucune** exigence externe (AVE, ENR, COM, NOT, CON, IA) n'a été absorbée ou implémentée.
- [x] Le scénario **REC-SCE-07** est intégralement couvert par les User Stories.

## 2. Respect du Modèle F001-F005
- **Aucun nouveau modèle de Ticket** n'est inventé. La réclamation est un Ticket (SOC-01) avec ajout limité (Catégorie, Réponse).
- **Aucun nouveau rôle ASP.NET Identity** n'est ajouté. Seules des permissions sont spécifiées (F002).
- **Aucun nouveau SLA "en dur"** n'est créé. Le moteur paramétrable SLA-01 de F001 est réutilisé.
- **Aucun type documentaire en dur** n'est inventé. La gestion des documents repose sur SOC-07. La "réponse formalisée" (REC-04) est un champ texte.
- **Aucune notification E-mail/SMS spécifique** n'est développée. COM-01 gèrera la notification.

## 3. Testabilité
- [x] 100% des exigences Must (REC-01, 02, 03, 04) disposent de critères d'acceptation (User Stories).
- [x] Le scénario de recette officiel (REC-SCE-07) est découpé en étapes claires et vérifiables.

## 4. Frontières et Cohérence
- [x] La limite financière avec le module AVE (Avoirs) est explicitement documentée. La F006 se termine à la réponse formelle de clôture, sans remboursement intégré.
- [x] Les priorités CDC (Must, Should, Could) sont exactement reportées.
- [x] Aucun point "BLOQUANT POUR PLAN" (PC) n'est masqué (PC-15 n'impacte pas F006).

## 5. Résultat Mécanique
- Couverture CDC de la Feature : 6 exigences (REC-01..06)
- Conformité : 6 / 6 (100%)
