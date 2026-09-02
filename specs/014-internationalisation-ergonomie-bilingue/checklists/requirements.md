# REQUIREMENTS CHECKLIST — FEATURE 014 (CLARIFIÉE)

**Feature** : Feature 014 — Internationalisation & Ergonomie Bilingue (Français / Anglais)  
**Exigence cible** : **NFR-07** (Should)  

---

## 1. ALIGNEMENT CDC & DÉCISIONS DE CLARIFICATION

- [x] **Texte exact de NFR-07 sourcé** : *« Interface disponible en anglais (clients internationaux). »*
- [x] **Cultures clarifiées** : `fr-FR` (défaut) et `en-US` (secondaire).
- [x] **Ordre de résolution validé** : Choix manuel > Mémorisation > Accept-Language > `fr-FR`.
- [x] **Persistance clarifiée** : Persistée dans le navigateur sans migration de base de données.
- [x] **Détection Accept-Language clarifiée** : `en*` orienté vers `en-US`, sinon fallback `fr-FR`.
- [x] **Continuité de saisie garantie** : Changer de langue préserve les valeurs des formulaires et les brouillons ERR-01.
- [x] **Séparation stricte UI vs Données** : Aucune donnée métier stockée n'est traduite (BL, booking, tickets, sociétés, commentaires libres).
- [x] **Statuts et Enums clarifiés** : Valeurs énumérées internes stables, seuls les libellés affichés sont localisés.
- [x] **Formatage clarifié** : Dates et nombres adaptés à la culture ; monnaie reste `XOF` sans conversion.
- [x] **Surfaces incluses exhaustives** : Accueil, Connexion, 2FA, Layout/Header, 4 formulaires majeurs, Simulation proforma, Fiches de suivi.
- [x] **Surfaces exclues explicites** : E-mails, SMS, logs techniques, contrats d'API JSON.
- [x] **Fallback de ressource défini** : Fallback vers le français avec test de complétude bloquant.
- [x] **Sécurité & Permissions intangibles** : Aucun impact sur l'authentification, les claims, les rôles ou les autorisations.
- [x] **Contrats d'API NFR-08 intacts** : 0 modification des propriétés ou des schémas d'API REST.
- [x] **0 migration de base de données** : 0 table ajoutée, 0 colonne modifiée.
- [x] **0 dépendance externe** : 0 fournisseur tiers de traduction.
- [x] **Suite de test définie** : `NFR-CERT-07` avec scénarios `01` à `06`.
- [x] **Anti-scope vérifié** : Exclusion stricte de NFR-03/04/05/06/09/10, IA-01..07, SEC-02..09, ENR-04.
