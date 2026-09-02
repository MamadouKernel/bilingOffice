# Audit d'Avancement Global — CIT Digital Platform

> **Document** : `docs/09-etat-avancement-global.md`
> **Date** : 30 Août 2026
> **Objectif** : État des lieux exhaustif de la couverture du CDC, avancement réel, baseline de tests et feuille de route.

---

## 1. Résumé Exécutif

L'audit complet de la plateforme confirme que les fondations fonctionnelles majeures (Socle, Comptes, Workflows Import/Export, Proforma) sont intégralement livrées et opérationnelles. 
Le moteur de workflow (F001) prouve son efficacité en supportant de manière fluide l'ajout des modules métiers complexes (Import, Export, Proforma). La base de code est très saine, soutenue par une excellente couverture de tests et une UI premium (F003.5). 

La Feature 005 (Simulation Proforma) vient d'être officiellement clôturée. **Aucun développement n'est actuellement en cours.** La prochaine étape logique est le démarrage de la Feature 006.

---

## 2. État des Modules du Cahier des Charges

La matrice du CDC a été consolidée en 11 grands ensembles fonctionnels. 

| # | Module fonctionnel | Exigences | Couvertes | Avancement | Feature(s) | Statut |
|---|--------------------|-----------|-----------|------------|------------|--------|
| 1 | A&B - Socle & Workflow | 74 | 74 | 100% | F001, F003.5 | TERMINÉ |
| 2 | C - Comptes & RBAC | 11 | 11 | 100% | F002 | TERMINÉ |
| 3 | D - Billing Import | 20 | 20 | 100% | F003 | TERMINÉ |
| 4 | E - Billing Export | 5 | 5 | 100% | F004 | TERMINÉ |
| 5 | F - Simulation Proforma | 11 | 10 | 90% | F005 | TERMINÉ AVEC DETTE |
| 6 | G - Avoir & Exonération | 4 | 0 | 0% | N/A | NON COMMENCÉ |
| 7 | H - Customer Complaint | 6 | 0 | 0% | N/A | NON COMMENCÉ |
| 8 | I - Enrôlement Transporteurs | 5 | 0 | 0% | N/A | NON COMMENCÉ |
| 9 | J&K - Notifications & Continuité | 13 | 4 | 30% | F001 (partiel) | PARTIEL |
| 10| L - Assistant IA | 7 | 0 | 0% | N/A | NON COMMENCÉ |
| 11| M&N - Sécurité & NFR | 32 | 1 | 3% | F001 (partiel) | PARTIEL |

### Représentation Visuelle

```text
MODULE 1 (Socle)     ██████████ 100%
MODULE 2 (Comptes)   ██████████ 100%
MODULE 3 (Import)    ██████████ 100%
MODULE 4 (Export)    ██████████ 100%
MODULE 5 (Proforma)  █████████░  90%
MODULE 6 (Avoirs)    ░░░░░░░░░░   0%
MODULE 7 (Complaint) ░░░░░░░░░░   0%
MODULE 8 (Enrôlement)░░░░░░░░░░   0%
MODULE 9 (Notifs)    ███░░░░░░░  30%
MODULE 10 (IA)       ░░░░░░░░░░   0%
MODULE 11 (Sécurité) ░░░░░░░░░░   3%

AVANCEMENT GLOBAL CDC
█████████████░░░░░░░ 66.5 %
```

---

## 3. Historique des Features Réalisées

| Feature | Module CDC | Périmètre | Statut | Commit de clôture | Tests |
|:---|:---|:---|:---|:---|:---|
| **F001** | Socle, Workflow, SUP | Création tickets, Files, Moteur WKF, Supervision | TERMINÉ | `a9657a4` | Validé |
| **F002** | Comptes, RBAC | Identité, Profils clients, Habilitations strictes | TERMINÉ | `46b2605` | Validé |
| **F003** | Billing Import | Saisie BL, Pilotage Manifeste/CD, 4 Workflows | TERMINÉ | `cc9a81d` | Validé |
| **F003.5** | UI/UX (Transverse) | Refonte esthétique, Soft UI, Dark Mode | TERMINÉ | `0903fb5` | Validé |
| **F004** | Billing Export | Booking, Back Office, BEE, Export Workflows | TERMINÉ | `29c88e2` | Validé |
| **F005** | Simulation Proforma | Moteur tarifaire, Grilles, Calcul de stationnement | TERMINÉ | `98b7b46` | **432** |

---

## 4. Couverture Exacte du CDC

Recalcul strict mécanique basé sur les identifiants uniques d'exigences :

- **Exigences satisfaites** : 125 / 188
- **Exigences non implémentées** : 62 / 188
- **Exigences différées/dette acceptée** : 1 / 188 (SIM-11)

**Total réconcilié : 188 / 188**

**Avancement fonctionnel : 66.5 %** (calculé uniquement sur les 125 exigences satisfaites)

---

## 5. Baseline des Tests (État Actuel)

Résultat de l'exécution mécanique de `dotnet test` sur la dernière version du code :

- **Domain** : 63
- **Application** : 217
- **Infrastructure** : 63
- **Web** : 89
- **Total** : 432
- **Passed** : 432
- **Failed** : 0
- **Skipped** : 0

La base de code est d'une robustesse exceptionnelle (0 régression).

---

## 6. Dettes Acceptées et Points Bloquants

### Dette
- **SIM-11 (Could - Différée)** : Les contrôles croisés avancés de saisie sur la proforma ont été reportés à la Feature 011 (Hardening global) pour ne pas bloquer l'avancement.

### Points à clarifier (PC) ouverts et bloquants
- **PC-03 / PC-15 (BLOQUANT)** : Le workflow d'approbation financière des Avoirs & Exonérations (Module G) nécessite une définition claire des acteurs (Rôle "Caisse/Finance") et de leurs limites d'approbation. Ce point paralyse le développement du module G.

---

## 7. Comptes et Personas pour la Recette Manuelle

L'architecture supporte pleinement le test local grâce à un **Seed de Développement idempotent** (cf. `docs/09-comptes-test-development.md`).
Les comptes suivants sont créés automatiquement au démarrage si la variable d'environnement `CIT_TEST_USERS_PASSWORD` est définie.

| Persona | Email (Login) | Rôle / Capacité |
|:---|:---|:---|
| Admin Société (Normal) | `client.normal.admin@test.local` | Accès total Import/Export Normal |
| Admin Société (En Compte) | `client.encompte.admin@test.local` | Workflow direct sans paiement |
| Admin Société (Deposit) | `client.deposit.admin@test.local` | Workflow Lettrage Comptable |
| Admin Société (IES) | `client.ies.admin@test.local` | Workflow Double Guichet |
| Agent Import | `agent.import@test.local` | File Pilote Manifeste |
| Agent Export | `agent.export@test.local` | File Back Office Export |
| Agent Facturation | `agent.facturation@test.local` | File Facturation |
| Agent Paiement | `agent.paiement@test.local` | File Validation Paiement |
| Agent CD | `agent.cd@test.local` | File Contrôle Documentaire |
| Agent Compta | `agent.compta@test.local` | File Comptabilité Client |
| Superviseur | `superviseur@test.local` | Tableau de bord et affectations |
| Tarification | `tarification@test.local` | Upload des grilles F005 |

---

## 8. Roadmap Restante Recommandée

En raison des points bloquants sur le module Avoirs/Exonérations (PC-15), la roadmap recommandée pour finaliser le projet est la suivante :

1. **Feature 006** : Customer Complaint (REC) - *6 exigences, aucune dépendance externe, grande valeur.*
2. **Feature 007** : Avoir & Exonération (AVE) - *Sous réserve de levée du point PC-15.*
3. **Feature 008** : Enrôlement Transporteurs (ENR).
4. **Feature 009** : Notifications & Continuité.
5. **Feature 010** : Assistant IA.
6. **Feature 011** : Hardening, Sécurité finale et apurement des dettes (dont SIM-11).

---

## 9. Conclusion

MODULE ACTUEL : AUCUN (En transition)
FEATURE ACTUELLE : AUCUNE (Attente démarrage F006)
DERNIÈRE FEATURE CLÔTURÉE : Feature 005 (Simulation Proforma)
PROCHAINE FEATURE RECOMMANDÉE : Feature 006 (Customer Complaint)
EXIGENCES CDC COUVERTES : 125
EXIGENCES CDC RESTANTES : 62 (+ 1 dette)
AVANCEMENT GLOBAL : 66.5 %
BASELINE TESTS : 432 Tests (432 Passed, 0 Failed)
POINTS BLOQUANTS : PC-15 (Workflow Avoirs/Exonérations non défini)
VERDICT : PROJET TRÈS SAIN, PRÊT POUR F006.
