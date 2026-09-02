# Preflight Check — Feature 006 Customer Complaint

> **Document** : `docs/10-preflight-feature-006.md`  
> **Date** : 30 Août 2026  
> **Contexte** : Audit final avant le lancement de `/speckit.specify` pour F006.

---

## 1. Réconciliation des 188 Exigences CDC

La vérification mécanique des exigences uniques issues du cahier des charges (`docs/01-matrice-exigences.md`) établit la couverture suivante :

- **Exigences satisfaites** : 125 / 188
- **Exigences non implémentées** : 62 / 188
- **Exigences différées/dette acceptée** : 1 / 188 (SIM-11)
- **Total réconcilié** : **188 / 188**

*Note : L'avancement fonctionnel strict est de 66.5 % (basé uniquement sur les exigences satisfaites).*

---

## 2. Baseline Technique (Point Zéro)

Exécution de `dotnet test` effectuée sur le code actuel. 

| Projet | Total Tests | Passed | Failed | Skipped |
|:---|:---:|:---:|:---:|:---:|
| `CIT.Plateforme.Domain.Tests` | 63 | 63 | 0 | 0 |
| `CIT.Plateforme.Application.Tests` | 217 | 217 | 0 | 0 |
| `CIT.Plateforme.Infrastructure.Tests` | 63 | 63 | 0 | 0 |
| `CIT.Plateforme.Web.Tests` | 89 | 89 | 0 | 0 |
| **TOTAL** | **432** | **432** | **0** | **0** |

**Conclusion** : Le socle est parfaitement stable. Aucun échec.

---

## 3. Matrice de Recette Manuelle — Comptes de Test

Les comptes suivants sont générés automatiquement par le mécanisme de test de l'application (en environnement de Développement). 
*Mot de passe temporaire : Celui défini localement par la variable `CIT_TEST_USERS_PASSWORD` (ex: `MotDePasseTest123!`).*

| Persona | Email / Login | Société | Type Client | Permissions principales | Parcours cible |
|:---|:---|:---|:---|:---|:---|
| **Client Normal** | `client.normal.user@test.local` | NORMAL TEST SA | Normal | Consultation, Soumission | Workflow Import/Export classique |
| **Admin Société** | `client.normal.admin@test.local` | NORMAL TEST SA | Normal | Gestion Collaborateurs, Workflow total | Gestion RBAC Client |
| **Client En Compte** | `client.encompte.admin@test.local` | EN COMPTE TEST SA | En Compte | Workflow total (sans paiement) | Import/Export sans Caisse |
| **Client Deposit** | `client.deposit.admin@test.local` | DEPOSIT TEST SA | Deposit | Workflow total (sans paiement direct) | Import/Export Lettrage |
| **Client IES** | `client.ies.admin@test.local` | IES TEST SA | IES | Soumission spécifique | Import/Export Double Guichet |
| **Admin CIT** | `superviseur@test.local` | CIT (Interne) | N/A | Supervision, Actions de masse | Tableaux de bord, Files |
| **Agent Facturation**| `agent.facturation@test.local` | CIT (Interne) | N/A | Éditer les factures | Facturation Import/Export |
| **Agent Caisse** | `agent.paiement@test.local` | CIT (Interne) | N/A | Valider preuve de paiement | Étape de validation Caisse |
| **Agent CD** | `agent.cd@test.local` | CIT (Interne) | N/A | Contrôle Documentaire (BADT/BEE) | Remise documentaire finale |
| **Agent Back Office**| `agent.export@test.local` | CIT (Interne) | N/A | Codifier Booking | Saisie EXPORT |

---

## 4. Checklist de Recette Rapide (F001 → F005)

### F001 & F002 — Socle, Workflows et Comptes
- **[ ] Connexion & Habilitations**
  - *Persona* : N'importe lequel
  - *Action* : Se connecter. Essayer d'accéder à l'URL `/admin/disponibilite` avec un compte Client.
  - *Attendu* : Redirection vers 403 (Accès refusé).
- **[ ] Gestion Société**
  - *Persona* : `client.normal.admin@test.local`
  - *Action* : Aller sur "Mon Équipe", ajouter un collaborateur.
  - *Attendu* : Le collaborateur est créé.

### F003 — Billing Office IMPORT
- **[ ] Soumission BL & Parcours Normal**
  - *Persona* : `client.normal.admin@test.local`
  - *Action* : Soumettre une demande d'Import, déposer un BL, valider le ticket avec l'Agent Import, uploader la preuve de paiement, valider avec la Caisse, clôturer avec l'Agent CD.
  - *Attendu* : Statut final "Clôturé", documents BADT/Facture disponibles au téléchargement.
- **[ ] Parcours Client IES**
  - *Persona* : `client.ies.admin@test.local`
  - *Action* : Soumettre une demande d'Import.
  - *Attendu* : Les Agents Import et CD interviennent en parallèle. La jointure s'effectue automatiquement.

### F003.5 — UI / UX Consolidation
- **[ ] Sécurité d'accès Ticket (Challenge 302/403)**
  - *Persona* : `client.normal.admin@test.local`
  - *Action* : Essayer d'accéder à un URL de ticket appartenant à un autre client (en changeant l'ID séquentiel).
  - *Attendu* : Redirection 403. Les GUID internes ne sont jamais exposés dans les URLs.
- **[ ] Dark Mode**
  - *Persona* : N'importe lequel
  - *Action* : Utiliser le bouton "🌓" dans le Header.
  - *Attendu* : L'interface bascule en Dark/Light mode, le choix est conservé après rafraîchissement.

### F004 — Billing Office EXPORT
- **[ ] Soumission Booking & Draft IN**
  - *Persona* : `client.normal.admin@test.local`
  - *Action* : Soumettre un Export, faire valider par le Back Office, facturer, générer le BEE par le CD.
  - *Attendu* : Parcours export complété, BEE téléchargeable.

### F005 — Simulation Proforma
- **[ ] Calcul Golden Masters**
  - *Persona* : `client.normal.admin@test.local`
  - *Action* : Lancer une simulation Import Transit, 20" Dry, 100t, OOG (Oui), Dangereux (Oui), Cat C4, Séjour 29j.
  - *Attendu* : Montant total calculé de **199 129 XOF** (cf. Annexe A.8).
- **[ ] Grilles Tarifaires**
  - *Persona* : `tarification@test.local`
  - *Action* : Naviguer vers "Tarification > Grilles", modifier le tarif d'acconage.
  - *Attendu* : Le changement impacte immédiatement les nouvelles simulations, mais n'altère pas l'historique des anciennes.

---

## 5. Préparation Feature 006 — Customer Complaint

### Analyse du Périmètre (REC-01 à REC-06)
- **REC-01** : Création d'un ticket de réclamation par catégorie.
- **REC-02** : Paramétrage dynamique des catégories.
- **REC-03** : Instruction par une nouvelle file "Support Opérationnel".
- **REC-04** : Clôture avec une *réponse formalisée* enregistrée dans le ticket.
- **REC-05** : Orientation automatique selon la catégorie.
- **REC-06** : Enquête de satisfaction post-clôture.

### Analyse des Dépendances
- **Socle F001** : La feature s'appuie à 100% sur le socle F001 (tickets, file d'attente, workflow, messages, documents). 
- **Avoirs/Exonérations (AVE)** : **Totalement isolée**. La Feature 006 n'inclut PAS le traitement financier des Avoirs (AVE-01..04), qui reste bloqué par PC-15. Les réclamations commerciales (Customer Complaint) peuvent être traitées et clôturées sans générer automatiquement un avoir sur la plateforme.

### Acteurs et Permissions
- Nouveaux Rôles : Agent Support Opérationnel (Interne).
- Permissions à créer : `ComplaintSoumettre`, `ComplaintInstruire`, `ComplaintAdministrer`.

### Points de Clarification Ouverts
- **Enquête de satisfaction (REC-06 Could)** : La modélisation de l'enquête n'est pas définie (Note sur 5 ? Texte libre ?). Peut nécessiter un cadrage rapide ou être différée à la fin.
- **Réponse formalisée (REC-04 Must)** : Faut-il un nouveau champ spécifique en base ou un type de document (PDF) particulier, ou un commentaire marqué comme "Réponse Officielle" suffit-il ?

---

## 6. Décision

Le périmètre est net, le socle (F001) est pleinement capable d'accueillir ce nouveau workflow avec très peu de friction. Le blocage financier (PC-15) propre au module AVE ne concerne pas le périmètre de la Feature 006 défini ici.

**VERDICT** :

GO FOR /speckit.specify
