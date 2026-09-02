# Spécification Fonctionnelle — Feature 006 (Customer Complaint)

> **Document** : `specs/006-customer-complaint/spec.md`  
> **Feature** : F006 — Customer Complaint  
> **Date** : 30 Août 2026  
> **Statut** : CLARIFIED (Prêt pour /speckit.plan)  
> **Couverture projetée après clôture de F006** : 131/188  

---

## 1. Objectif

La Feature 006 implémente le module **Customer Complaint** (Réclamations Clients). Elle dote les clients d'un canal structuré de réclamation (catégorisé) et fournit au Support Opérationnel une file dédiée pour instruire, échanger et résoudre ces demandes, avec une réponse formelle consignée dans le ticket.

---

## 2. Périmètre Contractuel (Exigences CDC)

F006 couvre exclusivement la famille **REC**.

### REC-01 (Must)
- **Texte CDC** : Le client crée une demande en sélectionnant une catégorie de réclamation ; chaque demande génère un ticket tracé de bout en bout.
- **Acteur(s)** : Client
- **Déclencheur** : Soumission d'une réclamation
- **Workflow** : Création → Génération d'un ticket (SOC-01)
- **Données** : Catégorie (obligatoire)

### REC-02 (Must)
- **Texte CDC** : Les catégories sont paramétrables et dynamiques (ajout/modification sans développement).
- **Acteur(s)** : Admin fonctionnel
- **Déclencheur** : Administration
- **Données** : Référentiel des catégories

### REC-03 (Must)
- **Texte CDC** : Les tickets sont orientés vers le Support Opérationnel, qui peut instruire, demander des compléments, joindre des documents, échanger avec le client et orienter vers les services concernés.
- **Acteur(s)** : Support Opérationnel
- **Workflow** : Affectation file "Support Opérationnel". Actions possibles : Instruction, Demande de Complément, Transfert (orientation).

### REC-04 (Must)
- **Texte CDC** : La clôture d'une réclamation comporte une réponse formalisée au client, conservée dans le ticket.
- **Acteur(s)** : Support Opérationnel
- **Workflow** : Clôture du ticket
- **Données** : Réponse formalisée

### REC-05 (Should)
- **Texte CDC** : Règles d'orientation automatique par catégorie (file ou service destinataire paramétrable).
- **Acteur(s)** : Système
- **Workflow** : Affectation initiale de file conditionnée par la catégorie.

### REC-06 (Could)
- **Texte CDC** : Enquête de satisfaction à la clôture du ticket.
- **Acteur(s)** : Client
- **Déclencheur** : Consultation d'un ticket clôturé.

---

## 3. Clarifications Fonctionnelles (CLAR-REC)

Toutes les questions fonctionnelles ont été résolues en s'appuyant strictement sur le CDC et le socle existant F001, sans introduire de complexité non justifiée.

| ID | Question | Décision | Source | Impact | Statut |
|:---|:---|:---|:---|:---|:---|
| **CLAR-REC-01** | *PC-REC-A* : Modélisation exacte de la réponse formalisée (REC-04) ? | Un champ de texte riche saisi par l'agent lors de la clôture et stocké comme propriété du ticket. Aucun PDF inventé. | REC-04 ("conservée dans le ticket"), Principe IV | Ajout d'un champ `ReponseCloture` sur l'entité Ticket. | RÉSOLU |
| **CLAR-REC-02** | *PC-REC-B* : L'enquête nécessite-t-elle un cycle de vie (REC-06) ? | Non. Une simple interaction UI sur un ticket clôturé (ex: composant étoiles/texte), rattachée au ticket. | REC-06 (Could), Principe IV | Donnée simple liée au ticket. | RÉSOLU |
| **CLAR-REC-03** | Le résultat métier impose-t-il une nomenclature (Acceptée, Fondée) ? | Non spécifié au CDC. Utilisation des statuts F001 (Clôturé, Rejeté). La nuance est portée par la *réponse formalisée*. | Absence de mention dans REC. | Réutilisation 100% de `StatutTicket` F001. | RÉSOLU |
| **CLAR-REC-04** | Quels sont les champs de qualification de la demande ? | Catégorie = OBLIGATOIRE (REC-01). Motif, criticité, etc. = NON SPÉCIFIÉS. | REC-01. | Seul le référentiel `CategorieReclamation` est créé. | RÉSOLU |
| **CLAR-REC-05** | Le rattachement à un élément métier (BL, Facture) est-il obligatoire ? | OPTIONNEL. La réclamation peut être générique ou liée à une référence. | Absence d'obligation dans REC-01. | Référence facultative (SOC-10). | RÉSOLU |
| **CLAR-REC-06** | Y a-t-il des SLA métier en dur à coder ? | NON SPÉCIFIÉ. Le moteur générique SLA-01 de F001 s'applique. | Absence de délais explicites dans REC. | Configuration SLA par l'Admin. | RÉSOLU |
| **CLAR-REC-07** | Comment se comporte la demande de complément ? | Utilisation de SOC-06 (passage en "Attente Client"). Le client répond, le ticket repasse "En Traitement". Déclenche ACR-01 (action requise). | REC-03. | Comportement 100% hérité de F001. | RÉSOLU |
| **CLAR-REC-08** | Quels types documentaires ajouter ? | Les pièces jointes existent déjà (SOC-07). Aucun nouveau type en dur n'est inventé. | REC-03. | API Documents F001. | RÉSOLU |

---

## 4. Cycle de Vie Exact (Workflow)

Tableau de séquence de la réclamation selon REC-01..06 et REC-SCE-07 :

| Étape | Acteur | Action | État avant | État après | Visible client ? | Audit ? | Exigence |
|:---|:---|:---|:---|:---|:---:|:---:|:---|
| **1. Soumission** | Client | Soumet formulaire (Catégorie, texte) | *Nouveau* | **En File (Non assigné)** | Oui | Oui | REC-01, REC-05 |
| **2. Prise en charge** | Agent Support | Affectation du ticket | En File | **En Traitement** | Oui | Oui | REC-03 |
| **3. Instruction / Échange** | Agent Support | Ajout note ou message | En Traitement | En Traitement | Oui (Messages) | Oui | REC-03 |
| **4. Demande complément** | Agent Support | Demande info client | En Traitement | **En Attente Client** | Oui (ACR-01) | Oui | REC-03 |
| **5. Réponse** | Client | Envoi info/pièce | En Attente Client | **En Traitement** | Oui | Oui | REC-03 |
| **6. Clôture** | Agent Support | Saisie réponse formalisée | En Traitement | **Clôturé** | Oui | Oui | REC-04 |
| **7. Enquête** | Client | Note sa satisfaction | Clôturé | Clôturé (évalué) | Oui | Oui | REC-06 |

---

## 5. Scénario Contractuel Rejoué : REC-SCE-07

> **Intitulé** : Customer Complaint  

| Étape REC-SCE-07 | Précondition | Acteur | Action | Résultat | Exigence REC |
|:---|:---|:---|:---|:---|:---|
| 1. Création par catégorie | Catégories configurées | Client | Remplit le formulaire | Ticket créé, catérogisé, tracé | REC-01, REC-02 |
| 2. Orientation | Règle paramétrée | Système | Route le ticket | Arrivée dans la file Support Opérationnel | REC-05 |
| 3. Instruction & Complément | Ticket en file | Support | Prend en charge, demande une info | Statut = Attente Client. Notification. | REC-03 |
| 4. Complément (réponse) | Action requise | Client | Envoie le complément | Statut = En Traitement | REC-03 |
| 5. Orientation (transfert) | Habilitations | Support | Transfère à un autre service si besoin | Ticket change de file | REC-03 |
| 6. Clôture avec réponse | Ticket instruit | Support | Clôture et saisit le texte formel | Ticket clôturé, réponse visible | REC-04 |

---

## 6. Permissions Métier

Les capacités fonctionnelles sont les suivantes, basées sur les règles de cloisonnement F002.

**CLIENT** (Standard ou Admin Société) :
- `Complaint.Soumettre` : Créer une réclamation.
- `Complaint.Consulter` : Voir les réclamations de sa société (règle Admin Société existante appliquée).
- `Complaint.Repondre` : Répondre à une demande de complément ou évaluer.

**AGENT SUPPORT OPÉRATIONNEL** :
- `Complaint.Instruire` : Voir la file, PEC, demander complément, transférer, clôturer avec réponse formalisée.

**ADMINISTRATEUR FONCTIONNEL** :
- `Complaint.Parametrer` : Gérer le référentiel des catégories (REC-02, REC-05).

*(Les noms exacts des Claims/Roles seront fixés dans le /plan).*

---

## 7. Testabilité des 4 Must

Chaque Must sera vérifié par un test fonctionnel/intégration dédié :

- **Test REC-01 (Nominal)** : Soumission par un client avec catégorie, vérification de la création en base et de la présence dans la file d'attente (validation IDOR inter-société).
- **Test REC-02 (Admin)** : Ajout d'une catégorie par l'Admin, vérification de son apparition dans le formulaire de soumission Client.
- **Test REC-03 (Workflow)** : Agent prend en charge le ticket généré, effectue une demande de complément, le ticket passe en Attente Client et apparaît dans les actions requises (ACR-01) du client.
- **Test REC-04 (Clôture)** : Agent tente de clôturer sans réponse formalisée (erreur attendue). Agent clôture avec réponse, client consulte le ticket et lit la réponse.

---

## 8. Frontière Strictement Définie avec AVE

La Feature 006 Customer Complaint est **totalement découplée** de la famille AVE (Avoirs / Exonérations). 
- Si une réclamation donne lieu à un geste commercial, l'agent Support indique ce fait dans sa **réponse formalisée** (ex: "Nous acceptons votre demande, un avoir va être généré").
- **Le workflow s'arrête là pour F006**. Aucune intégration, aucune validation de chaîne financière, aucun point PC-15 n'entre dans le périmètre F006.

---

## 9. Matrice de Réutilisation F001-F005

| Capacité existante | Réutilisée ? | Extension nécessaire ? | Justification |
|:---|:---:|:---:|:---|
| **Ticket / Dossier (F001)** | Oui | Limité | Ajout du lien `CategorieId` et du champ texte `ReponseFormalisee`. |
| **Files & Moteur (F001)** | Oui | Non | Création de la file "Support Opérationnel" dans le paramétrage. |
| **Documents / Messages** | Oui | Non | Appels natifs aux sous-systèmes existants. |
| **ACR-01 (Action Requise)** | Oui | Non | Détection automatique du statut "En Attente Client". |
| **Habilitations (F002)** | Oui | Limité | Création des permissions `Complaint.*`. |
| **Tableaux de bord (F003.5)**| Oui | Non | Affichage standard des listes de tickets. |

---

## 10. Matrice de Couverture

| Exigence | Priorité | Scénario REC-SCE | Clarification associée | Dépendance (Base) |
|:---|:---:|:---:|:---|:---|
| REC-01 | Must | REC-SCE-07 (Ét. 1) | CLAR-REC-04, 05 | SOC-01 |
| REC-02 | Must | REC-SCE-07 (Ét. 1) | CLAR-REC-04 | - |
| REC-03 | Must | REC-SCE-07 (Ét. 3-5)| CLAR-REC-07, 08 | SOC-04, SOC-06 |
| REC-04 | Must | REC-SCE-07 (Ét. 6) | CLAR-REC-01 | REC-03, SOC-09 |
| REC-05 | Should | REC-SCE-07 (Ét. 2) | - | REC-02, WKF-01 |
| REC-06 | Could | (Post-clôture) | CLAR-REC-02 | REC-04 |
