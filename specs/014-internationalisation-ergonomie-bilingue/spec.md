# SPÉCIFICATION FONCTIONNELLE DÉTAILLÉE & CLARIFIÉE — FEATURE 014

**Feature** : Feature 014 — Internationalisation & Ergonomie Bilingue (Français / Anglais)  
**Date** : 2026-09-02  
**Baseline CDC d'entrée** : **168 / 188 exigences conformes (89,36 %)**  
**Périmètre fonctionnel** : **NFR-07** (Should)  
**Gain maximal** : **+1 exigence**  
**Projection cible** : **169 / 188 (89,89 %)** *(non acquise avant Closure Review)*  

---

## 1. CONTEXTE MÉTIER & EXIGENCE CDC

### 1.1 Texte exact du CDC
> **NFR-07 (Should)** : *« Interface disponible en anglais (clients internationaux). »* (§19 du CDC, ligne 906).

### 1.2 Objectif de la Feature 014
Permettre aux armateurs mondiaux, transitaires multinationaux et agents maritimes anglophones d'interagir avec la plateforme CIT Digital Platform en anglais (`en-US`), tout en conservant le français (`fr-FR`) comme langue native par défaut, sans modifier les données métier stockées ni impacter l'architecture de sécurité et d'API.

---

## 2. DÉCISIONS DE CLARIFICATION FONCTIONNELLE (CLAR-NFR-07)

| ID | Sujet | Question / Ambiguïté | Décision fonctionnelle validée | Type de décision |
|:---|:---|:---|:---|:---:|
| **DEC-014-01** | Langues & Cultures | Quelles cultures précises supporter ? | `fr-FR` (Français - défaut) et `en-US` (Anglais - secondaire). | Décision Produit |
| **DEC-014-02** | Priorité de résolution | Quel ordre de priorité entre les sources de langue ? | 1. Choix manuel > 2. Préférence mémorisée > 3. `Accept-Language` > 4. `fr-FR` par défaut. | Décision Produit |
| **DEC-014-03** | Mémorisation | Quelle portée pour la mémorisation ? | Mémorisation persistée dans le navigateur (reconnexions et sessions successives sans DB). | Décision Produit |
| **DEC-014-04** | Détection initiale | Comment traiter la première visite ? | Si `Accept-Language` contient `en` (ex. `en-US`, `en-GB`), orienter vers `en-US` ; sinon `fr-FR`. | Décision Produit |
| **DEC-014-05** | Changement de langue | Quel impact sur la saisie en cours ? | Commutation sans déconnexion, sans perte de données saisies et sans impact sur les brouillons ERR-01. | Décision Produit |
| **DEC-014-06** | Surfaces certifiées | Quelles pages et formulaires inclure ? | Accueil, Connexion/2FA, Layout, 4 formulaires majeurs (Import, Export, Réclamation, Enrôlement), Simulation proforma, Fiches tickets/dossiers. | CDC / Produit |
| **DEC-014-07** | Données dynamiques | Les données utilisateur sont-elles traduites ? | **NON**. Noms de sociétés, numéros de BL/booking/tickets, commentaires libres restent intangibles. | CDC / Invariant |
| **DEC-014-08** | Statuts & Enums | Les valeurs stockées changent-elles ? | **NON**. Seul le libellé d'affichage est localisé (ex. `StatutTicket.EnCours` → "En cours" / "In progress"). | Architecture |
| **DEC-014-09** | Formatage culturel | Les dates et nombres sont-ils adaptés ? | Dates et nombres formatés selon la culture courante ; devise reste strictement `XOF` (0 conversion). | Décision Produit |
| **DEC-014-10** | Emails & Notifications | Les e-mails F009/F010 sont-ils traduits ? | **NON (Hors scope F014)**. Modèles de communication F009 inchangés (évite l'effet de bord). | Invariant Scope |
| **DEC-014-11** | Contrats d'API (NFR-08) | Les propriétés JSON d'API changent-elles ? | **NON**. Les DTOs et contrats OpenAPI restent strictement inchangés. | Invariant NFR-08 |
| **DEC-014-12** | Fallback de traduction | Que faire si une clé anglaise manque ? | Fallback vers le libellé français avec test de complétude bloquant à l'audit. | Décision Qualité |

---

## 3. PÉRIMÈTRE DÉTAILLÉ DES SURFACES LOCALISÉES (FR / EN)

### 3.1 Surfaces incluses (Matrice d'interface F014)
1. **Socle public & Navigation** :
   - Page d'accueil (`Home.razor`) : Accroches, descriptions des services CIT, boutons d'action.
   - Header & Navigation : Liens de menu, sélecteur de langue (`LanguageSwitcher.razor`), profil et déconnexion.
   - Pied de page : Mentions légales, informations CIT.
2. **Authentification & Sécurité** :
   - Page de connexion (`Connexion.razor`) : Champs identifiants, labels, boutons, messages d'erreur d'authentification.
   - Double authentification (`ActiverDoubleAuthentification.razor`, `VerifierCode.razor`) : Instructions QR code, saisie OTP, alertes.
3. **Formulaires majeurs de démarches (4/4)** :
   - **Demande Import** (`SoumettreDemande.razor`) : Étapes, champs BL, conteneurs, type de livraison, pièces jointes.
   - **Demande Export** (`SoumettreDemandeExport.razor`) : Booking, escales, empotage, formalités.
   - **Réclamations Client** (`NouvelleReclamation.razor`, `MesReclamations.razor`) : Catégories, objet, description, suivi.
   - **Enrôlement Transporteur** (`SoumettreDemande.razor`) : Identité, véhicules, chauffeurs, documents requis.
4. **Outil de Simulation Proforma** :
   - `SimulationProformaPage.razor` : Paramètres tarifaires, options de séjour, conteneurs, alertes et aides de franchise.
5. **Consultation & Suivi** :
   - `DetailTicket.razor`, `DetailDossier.razor` : Badges d'état, jalons chronologiques, sections récapitulatives.

### 3.2 Surfaces exclues
- Données textuelles libres et dynamiques (commentaires saisis, réclamations, adresses réelles).
- Communications sortantes SMS / WhatsApp (bloqué par PC-16).
- Logs techniques du serveur et traces d'exception.
- Schémas et noms de champs JSON des API REST (NFR-08).

---

## 4. RÈGLES DE COMPORTEMENT FONCTIONNEL

1. **Règle de persistance** : La sélection d'une langue est conservée durant toute la session et pour les connexions futures sur le même navigateur.
2. **Règle de non-régression de session** : Changer de langue ne déconnecte jamais l'utilisateur, ne réinitialise pas le formulaire en cours et ne détruit aucun brouillon ERR-01.
3. **Règle d'immuabilité métier** : Toute donnée transmise en base de données ou vers le domaine conserve ses identifiants, codes énumérés et montants standards.

---

## 5. USER STORIES

### US1 — Sélection manuelle de l'anglais par un utilisateur international
- **En tant que** transitaire maritime anglophone,
- **Je veux** sélectionner l'anglais depuis l'en-tête du portail,
- **Afin de** consulter l'ensemble de l'interface dans ma langue de travail sans être déconnecté.

### US2 — Soumission de démarches et validation en anglais
- **En tant que** client international,
- **Je veux** saisir une demande de facturation (Import/Export) ou une réclamation avec des libellés et messages d'aide en anglais,
- **Afin d'** éviter toute erreur d'interprétation lors de mes démarches portuaires.

### US3 — Prise en compte de la langue du navigateur et retour au français
- **En tant qu'** utilisateur accédant pour la première fois avec un navigateur en anglais,
- **Je veux** être accueilli directement en anglais, avec la possibilité de rebasculer en français à tout moment.

---

## 6. TEST-SPEC `NFR-CERT-07`

| Identifiant | Cas de test | Comportement attendu | Type de test |
|:---|:---|:---|:---:|
| **`NFR-CERT-07-01`** | Affichage par défaut | La plateforme démarre en `fr-FR` en l'absence de préférence explicite | Unitaire / Intégration |
| **`NFR-CERT-07-02`** | Détection `Accept-Language` | Un en-tête `Accept-Language: en-US` initialise l'UI en anglais à la première visite | Intégration Web |
| **`NFR-CERT-07-03`** | Bascule manuelle & Persistance | Le sélecteur de langue bascule l'UI en `en-US` et persiste la préférence | Intégration Web |
| **`NFR-CERT-07-04`** | Continuité de saisie lors du changement | Changer de langue sur un formulaire ne vide pas les champs saisis | Intégration UI |
| **`NFR-CERT-07-05`** | Complétude des 4 formulaires majeurs | 100% des labels, aides et boutons des 4 formulaires sont traduits | Intégration UI |
| **`NFR-CERT-07-06`** | Préservation de la sécurité & Données | Aucun claim, rôle, permission, DTO d'API ou valeur stockée n'est altéré | Régression / Sécurité |

---

## 7. TRAÇABILITÉ & SYNTHÈSE CDC

```text
Exigence CDC : NFR-07 (Should)
Priorité     : Should
Baseline     : 168 / 188 (89,36 %)
Gain cible   : +1 exigence
Projection   : 169 / 188 (89,89 %)
Migration    : 0 migration (0 nouvelle table, 0 nouvelle entité)
Fournisseurs : 0 dépendance externe
```
