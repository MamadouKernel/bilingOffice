# Audit sincère de YABA — 05/10/2026

> But : dire honnêtement où en est la plateforme, sans gonfler les chiffres. Complète
> `docs/112` (couverture par exigence) par un regard critique sur la **différence entre
> « le code existe et les tests passent » et « c'est prouvé conforme au CDC »**.
> Hors périmètre retenu : SMS / WhatsApp (exclus par la DSI).

## 1. Ce qui est vrai et vérifié

La largeur fonctionnelle est réelle. Vérifié directement (lecture du code, exécution des tests) :

- Chaque module du CDC a du code substantiel **et** des tests : Billing Office import/export (4 types de clients, IES parallèle), proforma, avoir/exonération, Customer Complaint, enrôlement, socle tickets, workflow/files/SLA/escalade, supervision, notifications (e-mail + portail), administration.
- **Moteur proforma calé sur l'Annexe A** : les tests assertent les vraies valeurs du barème (acconage 66 800 / 140 000 / 133 600, relevage 25 000 / 50 000) et un cas complet au franc (total 2 245 300 XOF). 84 tests tarification verts.
- **Cloisonnement client** appliqué côté serveur (identité résolue par le serveur, jamais depuis la vue).
- **2162 tests** automatisés verts ; build Release 0 erreur.
- Prod déployée, `/ready` : 5 composants Healthy (DB, stockage, antivirus, chiffrement, maintenance).

Estimation de couverture **en code** : ~90 % des exigences du CDC. C'est une base honnête.

## 2. Ce que « 90 % » ne dit pas

« Code + tests verts » n'est pas « prouvé en production ».

| Dimension | Réalité au 05/10 |
|---|---|
| Couverture **en code** | ~90 % |
| Scénarios de recette §22 **automatisés** | **présents et verts** — `RecSce01`→`RecSce14` (32 tests E2E) couvrent import (4 types), export (4 types), proforma, réclamation, enrôlement, prise en charge, transfert (+ refus hors matrice), réaffectation, temps/SLA, supervision, transverse |
| Scénarios de recette §22 **joués et signés** (PV humain) | **0** — c'est le critère d'acceptation officiel du CDC, et c'est lui qui manque |
| Ce que `/ready` prouve | l'**infrastructure**, pas les parcours métier |
| Preuves non fonctionnelles réelles (perf < 3 s, dispo ≥ 99 %, restauration testée) | **outillées, non exécutées / non signées** |

> Correction honnête : contrairement à une première formulation, les scénarios de recette ne sont
> **pas** « à écrire » — ils existent déjà en automatisé (32 tests `RecSce*` verts). Ce qui manque
> est leur **exécution et signature en recette formelle** (procès-verbaux), qui relèvent d'une
> campagne métier, pas du développement.

## 3. Signaux de fragilité observés (faits, pas hypothèses)

Rencontrés pendant les manipulations de cette session :

- Un **secret de production** (chaîne SQL en clair) était sur le point d'être commité — rattrapé.
- **`main` ne compilait pas** à un instant : un WIP « date d'enlèvement » jamais commité traînait dans le worktree de publication et un appel sans méthode cassait la build.
- Des **fixtures de test en retard** sur le code (commandes ajoutées sans mettre à jour leurs doubles) — révélées seulement en exécutant toute la suite.
- Un **incident de connexion en prod le 02/10** (paramétrage métier non provisionné au démarrage).

Lecture : la base est **large mais la discipline de livraison est irrégulière**. Le « vert » est vrai à l'instant T, pas robuste dans le temps sans garde-fous. La CI a été ajoutée mais reste **non bloquante** (branch protection GitHub non activée).

## 4. Écarts réels (hors SMS/WhatsApp)

| # | Écart | Nature |
|---|---|---|
| 1 | **Recette §22 signée** | scénarios automatisés présents (32 tests `RecSce*` verts) mais **0 joué/signé en recette formelle (PV)** — le vrai bloqueur de conformité |
| 2 | Preuves perf / dispo / restauration en environnement réel | scripts prêts, non exécutés |
| 3 | Sécurité d'exploitation | certificat reconnu, compte `sa` à remplacer, antivirus à confirmer sur `/ready` prod |
| 4 | Cohérence ticket ↔ IPAKI / IRIS / SYDAM | manuelle par conception → dépend de la discipline agent, non testable automatiquement |
| 5 | IA générative | choix assumé : base de connaissances + orientation, pas de génération libre |
| 6 | Garde-fou CI | branch protection GitHub non active (la CI ne bloque donc pas) |

## 5. Verdict sincère

La plateforme est **fonctionnellement riche et en production**, mais **pas encore « prouvée conforme »** au sens strict du CDC : il manque l'étape qui transforme « ça compile et les tests passent » en « ça marche pour de vrais clients, signé » — la **recette §22**.

État réel : **pilote déployé**, pas **généralisation validée**. Le code n'est pas le risque principal ; le risque est l'**absence de preuve métier signée** et une **discipline de livraison à durcir** (CI bloquante, revue avant merge, zéro WIP non commité).

## 6. Chemin vers la conformité

1. **Recette §22 : exécuter et signer** la campagne formelle en environnement de recette. Les
   scénarios sont déjà automatisés (`RecSce01`→`RecSce14`, 32 tests verts) et servent de base ;
   il reste à les **rejouer avec le métier et produire les PV signés**.
2. Activer la **branch protection** `main` (CI bloquante).
3. Exécuter et **signer** les preuves non fonctionnelles (perf, dispo, restauration).
4. Finaliser la **sécurité d'exploitation** (certificat, `sa`, antivirus).
5. PV de recette signés → bascule « pilote » → « conforme ».
