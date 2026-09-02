# Research & Décisions d'Architecture — Feature 006

> **Document** : `specs/006-customer-complaint/research.md`  
> **Date** : 30 Août 2026

Ce document consigne les décisions d'architecture (ADR) prises pour implémenter la Feature 006 (Customer Complaint). L'objectif absolu est de maximiser la réutilisation du socle F001 existant et de minimiser le code métier spécifique.

---

## D1 : Réutilisation du moteur Ticket F001 vs nouvel agrégat CustomerComplaint

**Contexte** : La spécification clarifiée (CLAR-REC) stipule que la réclamation EST un ticket et obéit aux mêmes règles de cycle de vie (SLA, messages, documents, transfert, prise en charge). La question est de savoir s'il faut créer une nouvelle entité `CustomerComplaint` qui pointe vers `Ticket` (composition), hérite de `Ticket` (héritage TPT/TPH), ou simplement ajouter des propriétés à `Ticket`.

**Décision** : **Extension additive de l'entité `Ticket` existante.** 
Nous n'allons créer aucune table spécifique `CustomerComplaints`. Une réclamation sera un `Ticket` classique identifié par `Module = "CustomerComplaint"`. 
Nous ajouterons simplement les propriétés optionnelles suivantes à `Ticket` :
- `CategorieReclamationId` (Guid?)
- `ReponseFormalisee` (string?)
- `EvaluationSatisfaction` (Value Object / Owned Entity avec Note et Commentaire)

**Alternatives rejetées** :
- *Table/Agrégat `CustomerComplaint` séparé avec relation 1:1 vers `Ticket`* : Introduit une complexité de jointure et de synchronisation des états inutile pour 3 champs.
- *Héritage TPH/TPT* : L'architecture actuelle du projet privilégie des entités riches avec des propriétés optionnelles (comme `NumeroBl` pour Import) pour éviter la fragmentation du modèle.

**Impact** : Le modèle de données reste simple. Le workflow, les permissions, l'ACR-01 et les messages (F001) fonctionnent nativement sans aucune modification.

---

## D2 : Modélisation des Catégories de Réclamation

**Contexte** : REC-02 exige que les catégories soient paramétrables et dynamiques. REC-05 indique qu'une catégorie peut déterminer l'orientation (la file cible).

**Décision** : Création d'une nouvelle entité `CategorieReclamation` dans le module `CIT.Plateforme.Domain.Reclamations`.
- Propriétés : `Id`, `Code`, `Libelle`, `FileCibleDefautId` (Guid?), `EstActif` (bool).
- Relation : `Ticket` possède une clé étrangère optionnelle vers `CategorieReclamation`.

**Impact** : Respect strict de REC-02. Permet au client de choisir une catégorie dans un menu déroulant, et au serveur de router le ticket vers la file cible associée (REC-05).

---

## D3 : Fiche 360° et UI Blazor

**Contexte** : Le système possède déjà un composant de rendu riche pour les tickets (`FicheTicket.razor` / `FicheDossier.razor` dans F003.5).

**Décision** : Nous réutiliserons le composant `FicheTicket` existant, en y ajoutant des **blocs conditionnels** qui ne s'affichent que si `Ticket.Module == "CustomerComplaint"`.
- Bloc d'entête : Affiche la catégorie de la réclamation.
- Bloc de réponse formalisée : Affiché en bas du ticket s'il est clôturé.
- Composant de vote (Étoiles) : Affiché pour le client si le ticket est clôturé et non encore évalué.

**Justification** : Évite de dupliquer toute la logique complexe de rendu des messages, de la timeline d'historique et des documents.

---

## D4 : Boundary Customer Complaint → Avoir/Exonération (AVE)

**Contexte** : La F006 ne doit pas implémenter le module Avoir/Exonération, qui reste bloqué par le métier (PC-15). Cependant, la clôture d'une réclamation commerciale pourrait éventuellement aboutir plus tard à un geste commercial.

**Décision** : La frontière stricte s'arrête à la méthode `Ticket.CloturerAvecReponseFormalisee(...)`. 
- Si une compensation est accordée, elle est textuellement annoncée dans le champ `ReponseFormalisee`. 
- Aucune entité `Avoir`, aucune file "Finance" n'est implémentée.
- *Extension future prévue* : Lorsque la Feature AVE sera prête, un contrat `GenererDemandeAvoirCommand` pourra être appelé manuellement par l'agent depuis un ticket de réclamation clôturé.

**Statut** : Boundary respectée, aucun couplage avec AVE.
