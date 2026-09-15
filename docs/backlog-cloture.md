# Backlog de clôture

*Directement exploitable pour créer des tickets. Aucun arbitrage n'est pris, aucun code n'est modifié.*

Référence : `docs/plan-cloture-et-preparation-production.md`
Base : commit `1c3cd38260c3b196de8641b8ec1d5a89f27a9762` · conformité 99,0 % · suite 1424/1424

**Légende — Type :** `ARBITRAGE` · `BLOQUANT` · `DETTE` · `AMELIORATION` · `RISQUE ACCEPTE` · `POST-PROD`
**Effort :** `XS` < 0,5 j · `S` 0,5–2 j · `M` 2–5 j · `L` > 5 j — *indicatif, à confirmer par l'équipe*

---

## Table unique

| ID | Type | Priorité | Sujet | Responsable | Dépendance | Effort | Bloque prod | Critère de fermeture |
|---|---|---|---|---|---|---|---|---|
| **C-02** | BLOQUANT | **P0** | Committer l'état post-remédiation, worktree propre | Tech Lead | — | S | **OUI** | Hash créé · `git status` vide · 1424/1424 rejoué sur ce hash |
| **C-01** | BLOQUANT | **P0** | Répéter R-15 sur copie de production | DBA (A) / Tech Lead (R) | C-02 | M | **OUI** | Rapport de répétition consigné · les 8 critères GO satisfaits |
| **A-04** | ARBITRAGE | **P0** | Valider les durées légales de conservation | Juridique (A/R) | — | M | NON | Durées écrites, datées, validées juridiquement |
| **A-02** | ARBITRAGE | **P1** | Décider des canaux de notification activés | DSI (A) | — | M | NON | Décision écrite · fournisseur(s) contractualisé(s) ou report acté |
| **A-01** | ARBITRAGE | **P1** | Trancher RG-CPT-08 — nature Transporteur distincte ? | Métier / PO (A) | — | M | NON | Option A ou B tranchée par écrit · statut de la règle mis à jour |
| **A-03** | ARBITRAGE | **P1** | Choisir le moteur et le périmètre de l'assistant | DSI (A) / Métier (C) | — | M | NON | Option A/B/C tranchée · périmètre « aide à la complétion » arbitré |
| **D-01** | DETTE | **P1** | Câbler le déclencheur de la rétention SEC-06 | Tech Lead (A) / Dév (R) | **A-04** | M | NON | `BackgroundService` enregistré · durées en configuration · dry-run disponible · test de double exécution |
| **D-02** | DETTE | **P1** | Rendre atomiques les actions en masse `ComptesAgents` | Tech Lead (A) / Dév (R) | — | S | NON | Commande applicative dédiée · unité de travail unique · résultat détaillé par permission |
| **D-03** | DETTE | **P1** | Router les actions en masse par le wrapper d'erreur | Dév (R) | D-02 | XS | NON | Les 4 méthodes passent par `ExecuterAsync` · un refus affiche le panneau de refus |
| **D-04** | DETTE | **P2** | Documenter le couplage `ValidationInterval` ↔ clôture de compte | Tech Lead (A/R) | — | XS | NON | Commentaire à l'endroit du réglage · mention dans `architecture-autorisation.md` |
| **D-08** | DETTE | **P2** | Éprouver le 2FA de bout en bout | Tech Lead (A) / QA (R) | — | M | NON | Test d'intégration sur le parcours complet, **ou** acceptation formelle écrite |
| **D-09** | DETTE | **P2** | Unifier la transaction de clôture de compte | Dév (R) | — | S | NON | Audit, rattachement et désactivation Identity dans une seule unité de travail |
| **D-11** | DETTE | **P2** | Couvrir `ComptesAgents.razor` par un test de rendu | QA (R) / Dév (C) | D-02 | M | NON | Test bUnit sur les trois actions en masse et le refus serveur |
| **D-05** | DETTE | **P3** | `workflow.deroger` non attribuable | Tech Lead (A) | D-06 | XS | NON | Décision jointe à D-06 : code supprimé, ou permission déclarée dans `Permission` |
| **D-06** | DETTE | **P3** | `DerogationWorkflowService` inatteignable | Tech Lead (A) / Dév (R) | — | S | NON | Service supprimé **ou** câblé à un écran, avec sa permission déclarée |
| **D-07** | DETTE | **P3** | `CreerVersionGrilleTarifaireCommandHandler` doublonné | Tech Lead (A) / Dév (R) | — | S | NON | Handler supprimé **ou** exposé à l'écran ; dans les deux cas, sorti de la liste des orphelins |
| **D-10** | DETTE | **P3** | Limiter le parallélisme de la suite | Dév (R) / QA (C) | — | XS | NON | `xunit.runner.json` en place · suite complète stable en exécution simultanée |
| **I-01** | AMELIORATION | **P2** | Versionner le runbook de MEP (R-15 → migrations → smoke) | Tech Lead (A) / Infra (C) | C-01 | S | NON | Runbook dans `docs/`, rejoué au moins une fois en recette |
| **I-02** | AMELIORATION | **P2** | Étendre la garde d'atteignabilité aux services hébergés | Tech Lead (A) / Dév (R) | — | S | NON | Un service métier périodique sans `AddHostedService` fait tomber un test |
| **I-03** | AMELIORATION | **P3** | Confirmation explicite sur « tout accorder » / « tout retirer » | PO (C) / Dév (R) | D-02 | XS | NON | Boîte de confirmation nommant le compte cible et le nombre de permissions |
| **R-ACC-01** | RISQUE ACCEPTE | — | Asymétrie nature/permission du filtre des notes internes | Tech Lead (A) | — | — | NON | Décision d'acceptation consignée · revue si le CDC évolue |
| **R-ACC-02** | RISQUE ACCEPTE | — | `fileId: null` sur `ConsulterFileAvoirExonerationQuery` | Tech Lead (A) | — | — | NON | Acceptation consignée · à revoir si le module devient multi-file |
| **PP-01** | POST-PROD | **P1** | Première exécution réelle de la rétention | Tech Lead (A) / DBA (R) | D-01 + A-04 | S | NON | Dry-run lu et validé · exécution réelle · journal de lot vérifié |
| **PP-02** | POST-PROD | **P2** | Activer les canaux mobiles (vague 2) | DSI (A) / Dév (R) | A-02 | M | NON | Adaptateur réel substitué à `CanalJournalise` · notification reçue de bout en bout |
| **PP-03** | POST-PROD | **P2** | Lot RG-CPT-08, si l'option B est retenue | Tech Lead (A) / Dév (R) | A-01 | **L** | NON | Étape 1 (`EstUsagerExterne`, comportement constant) puis étape 2 · tests de cloisonnement validés par mutation |
| **PP-04** | POST-PROD | **P2** | Intégrer le moteur d'assistant retenu | Tech Lead (A) / Dév (R) | A-03 | M–L | NON | Adaptateur `IAssistantConversationnel` réel · garde-fous et limites de coût en place |

**26 entrées — 2 bloquantes.**

---

## Détail des 11 dettes techniques

### D-01 — Chaîne de rétention SEC-06 sans déclencheur

| | |
|---|---|
| **Titre** | La rétention réglementaire n'est jamais exécutée |
| **Description** | `GestionnaireRetentionService` (orchestration) et `MoteurRetentionService` (décision) sont livrés, enregistrés au conteneur et couverts par des tests. **Aucun chemin de production ne les appelle** : ni service hébergé, ni commande d'administration. Aucune purge, anonymisation ou mise sous gel légal n'a jamais lieu |
| **Origine** | Extension de la garde d'atteignabilité, remédiation post-audit final |
| **Composants** | `Infrastructure/Securite/GestionnaireRetentionService.cs` · `Application/Securite/MoteurRetentionService.cs` · `DependencyInjection.cs` · `Domain/Securite/CategorieRetention.cs` |
| **Impact** | Obligation CDC **SEC-06 (Must)** non rendue. Conservation au-delà des délais réglementaires ARTCI/OHADA. **Aucune donnée n'est perdue** — c'est l'effacement qui n'a pas lieu |
| **Risque** | RQ-02 — si l'on câble avant A-04, anonymisation **irréversible** sur des délais non validés |
| **Sévérité** | **HIGH** |
| **Dépendances** | **A-04 (bloquante).** Les durées codées (30 j / 5 ans / 10 ans / 3 ans) ne sont sourcées nulle part ; le CDC les classe « à valider (durées légales) » |
| **Effort** | **M** — l'enveloppe d'hébergement suit un motif établi six fois ; le dry-run et l'externalisation des durées sont l'essentiel du travail |
| **Priorité** | **P1** |
| **Moment** | **AVANT PILOTE** *(planification et externalisation)* · **APRÈS PROD** *(première exécution réelle — PP-01)* |
| **Critère de fermeture** | `BackgroundService` écrit et enregistré · durées lues en configuration, aucune valeur en dur · mode dry-run disponible · test de double exécution prouvant l'idempotence · gel légal respecté comme condition d'exclusion |

### D-02 — Actions en masse `ComptesAgents` non atomiques

| | |
|---|---|
| **Titre** | Les attributions et révocations en masse laissent un état partiel en cas d'échec |
| **Description** | `AttribuerToutesAsync`, `RetirerToutesAsync` et `BasculerGroupeAsync` bouclent N appels indépendants sans unité de travail commune. Un échec au n-ième laisse les n−1 précédents validés |
| **Origine** | Modification concurrente `ComptesAgents.razor` (2026-09-05 22:26), analysée en recertification |
| **Composants** | `Web/Components/Pages/Administration/ComptesAgents.razor` · `Application/Comptes/AdministrationComptesAgentsCommands.cs` |
| **Impact** | État d'habilitation incohérent après échec, sans message. Cas concret : « tout retirer » sur soi-même dépouille l'administrateur puis lève sur `administration.comptes` |
| **Risque** | RQ-04 |
| **Sévérité** | **MEDIUM** |
| **Dépendances** | — |
| **Effort** | **S** |
| **Priorité** | **P1** |
| **Moment** | **AVANT PILOTE** |
| **Critère de fermeture** | Commande applicative dédiée portant la boucle · une seule unité de travail · résultat détaillé par permission · audit consolidé conservant la trace individuelle (RG-SEC-03) · refus explicite de l'auto-retrait de la dernière permission d'administration |

### D-03 — `AccesRefuseException` non gérée sur les actions en masse

| | |
|---|---|
| **Titre** | Un refus serveur remonte en exception non traitée |
| **Description** | Les quatre nouvelles méthodes n'utilisent pas le wrapper `ExecuterAsync`, qui attrape `AccesRefuseException` et affiche le panneau de refus |
| **Origine** | Même modification concurrente |
| **Composants** | `Web/Components/Pages/Administration/ComptesAgents.razor` |
| **Impact** | Exception non traitée dans le circuit Blazor au lieu d'un message lisible. **Aucun accès n'est ouvert** : le serveur refuse correctement |
| **Risque** | RQ-04 (aggravant) |
| **Sévérité** | **MEDIUM** |
| **Dépendances** | D-02 — même geste, même fichier |
| **Effort** | **XS** |
| **Priorité** | **P1** |
| **Moment** | **AVANT PILOTE** |
| **Critère de fermeture** | Les quatre méthodes passent par le wrapper · un refus affiche le panneau, sans exception |

### D-04 — Couplage `ValidationInterval` ↔ clôture de compte

| | |
|---|---|
| **Titre** | Une optimisation de performance rendrait fausse une promesse affichée |
| **Description** | La révocation de session à la clôture de compte repose sur la rotation du tampon de sécurité **et** sur `ValidationInterval = TimeSpan.Zero`. Relever ce réglage rendrait fausse la phrase « vos sessions ont été déconnectées » pendant l'intervalle retenu |
| **Origine** | Revue de convergence avant audit final |
| **Composants** | `Web/Program.cs` · `Application/Comptes/CloturerMonCompteCommand.cs` · `Web/Components/Pages/Comptes/CompteCloture.razor` |
| **Impact** | Aucun aujourd'hui. Le risque naît le jour où quelqu'un optimise sans connaître la dépendance |
| **Risque** | RQ-05 |
| **Sévérité** | **MEDIUM** |
| **Dépendances** | — |
| **Effort** | **XS** |
| **Priorité** | **P2** |
| **Moment** | **AVANT PILOTE** *(documentation seule)* |
| **Critère de fermeture** | Commentaire explicite à l'endroit du réglage dans `Program.cs` · mention dans `docs/architecture-autorisation.md` |

### D-05 — `workflow.deroger` non attribuable

| | |
|---|---|
| **Titre** | Une permission exigée par le code n'existe pas dans le référentiel |
| **Description** | `DerogationWorkflowService` exige `"workflow.deroger"`, constante **locale au service**, absente de `Permission.Tous`. `GererHabilitationAgentCommand.ExigerPermissionConnue` refuse les codes inconnus : **personne ne peut détenir cette permission** |
| **Origine** | Recertification post-remédiation |
| **Composants** | `Application/Tickets/DerogationWorkflowService.cs` · `Domain/Securite/NatureUtilisateur.cs` *(classe `Permission`)* |
| **Impact** | Nul aujourd'hui — le service est déjà inatteignable. Le défaut se révélerait si quelqu'un le câblait |
| **Risque** | — |
| **Sévérité** | **LOW** |
| **Dépendances** | D-06 — même décision |
| **Effort** | **XS** |
| **Priorité** | **P3** |
| **Moment** | **BACKLOG** |
| **Critère de fermeture** | Code supprimé avec le service, **ou** permission déclarée dans `Permission` et attribuable |

### D-06 — `DerogationWorkflowService` inatteignable

| | |
|---|---|
| **Titre** | Service de dérogation au circuit nominal sans consommateur |
| **Description** | Enregistré au conteneur, testé (`DerogationWorkflowTests`), **référencé par aucun écran ni cas d'usage** |
| **Origine** | Extension de la garde d'atteignabilité |
| **Composants** | `Application/Tickets/DerogationWorkflowService.cs` · `DependencyInjection.cs` |
| **Impact** | **Nul.** RG-WKF-02 et RG-WKF-03 sont ✅, servies en production par `TransfererTicketCommand` (matrice vérifiée) et `ReaffecterTicketCommand` |
| **Risque** | — |
| **Sévérité** | **LOW** |
| **Dépendances** | — |
| **Effort** | **S** |
| **Priorité** | **P3** |
| **Moment** | **BACKLOG** |
| **Classification** | **A — code mort** |
| **Recommandation** | **SUPPRIMER.** La dérogation est livrée par une voie qui vérifie la matrice ; celle-ci ne la vérifie pas, ce qui en fait une porte plus permissive sans usage démontré. La conserver, c'est garder une capacité non exposée et une permission fantôme |
| **Critère de fermeture** | Service, tests et enregistrement DI supprimés · sorti de `ServicesEnregistresSansConsommateurAssume` · suite verte |

### D-07 — `CreerVersionGrilleTarifaireCommandHandler` doublonné

| | |
|---|---|
| **Titre** | Handler de création de grille tarifaire non câblé, doublé par l'import CSV |
| **Description** | Enregistré au conteneur, exige `tarification.administrer`, **référencé par aucun écran**. La fonction est livrée par `ImporterGrilleTarifaireCsvCommandHandler`, câblé à `GestionGrillesTarifaires.razor` |
| **Origine** | Extension de la garde d'atteignabilité |
| **Composants** | `Application/Tarification/CreerVersionGrilleTarifaireCommand.cs` · `DependencyInjection.cs` |
| **Impact** | **Nul.** RG-SIM-11 et RG-SIM-12 sont ✅ par la voie CSV |
| **Duplication / responsabilité** | Les deux construisent une `VersionGrilleTarifaire` via `ConstructeurGrilleTarifaire`. La responsabilité est donc **partagée à la construction** et **divergente à l'entrée** : saisie structurée d'un côté, fichier CSV de l'autre |
| **Risque de divergence** | **Réel mais borné** : le constructeur commun limite l'écart possible. Le risque est qu'une évolution de la validation CSV ne soit pas répercutée sur le handler, qui n'a aucun test de bout en bout pour la signaler |
| **Sévérité** | **LOW** |
| **Dépendances** | — |
| **Effort** | **S** |
| **Priorité** | **P3** |
| **Moment** | **BACKLOG** |
| **Classification** | **A — code mort** |
| **Recommandation** | **SUPPRIMER**, sauf si la saisie structurée est au programme. Une fusion n'apporterait rien : le point commun — `ConstructeurGrilleTarifaire` — est **déjà** factorisé ; ne restent que deux formats d'entrée, dont un sans consommateur |
| **Critère de fermeture** | Handler et enregistrement supprimés · sorti de la liste des orphelins · suite verte — **ou** écran de saisie structurée livré |

### D-08 — 2FA non éprouvé de bout en bout

| | |
|---|---|
| **Titre** | Le mécanisme 2FA d'Identity n'est pas couvert par un test d'intégration complet |
| **Description** | La **règle d'accès** est éprouvée (`RegleAccesConnexionTests`, `ParcoursConnexion2FaTests`) ; la génération, la validation et l'expiration du code par ASP.NET Core Identity ne le sont pas de bout en bout |
| **Origine** | Remédiation P3 |
| **Composants** | `Web/Components/Pages/Comptes/VerifierCode.razor` · `Connexion.razor` · `Application/Comptes/RegleAccesConnexion.cs` |
| **Impact** | Un dysfonctionnement du mécanisme tiers ne serait pas détecté avant la production |
| **Risque** | RQ-07 |
| **Sévérité** | **LOW** |
| **Dépendances** | — |
| **Effort** | **M** — l'infrastructure de test peut être disproportionnée ; l'acceptation formelle est une issue légitime |
| **Priorité** | **P2** |
| **Moment** | **APRÈS PROD** |
| **Critère de fermeture** | Test d'intégration du parcours complet, **ou** acceptation formelle écrite + smoke test d'authentification en MEP |

### D-09 — Clôture de compte en deux transactions

| | |
|---|---|
| **Titre** | L'audit et le rattachement sont validés avant la désactivation Identity |
| **Description** | `CloturerMonCompteCommand` valide l'audit et la désactivation du rattachement par `uniteDeTravail.EnregistrerAsync()`, **puis** appelle `DefinirActivationAsync` — écriture Identity séparée |
| **Origine** | Revue de convergence avant audit final |
| **Composants** | `Application/Comptes/CloturerMonCompteCommand.cs` |
| **Impact** | Un échec entre les deux enregistre une clôture qui n'a pas eu lieu : audit « clôturé », rattachement désactivé, compte toujours actif |
| **Risque** | — *(probabilité très faible, pas de risque nommé)* |
| **Sévérité** | **LOW** |
| **Dépendances** | — |
| **Effort** | **S** |
| **Priorité** | **P2** |
| **Moment** | **APRÈS PROD** |
| **Critère de fermeture** | Une seule unité de travail couvrant audit, rattachement et désactivation · test d'échec intermédiaire ne laissant aucune trace |

### D-10 — Parallélisme de la suite

| | |
|---|---|
| **Titre** | Contention LocalDB entre projets exécutés simultanément |
| **Description** | L'exécution concurrente de plusieurs projets de test produit des échecs transitoires. Vérifié en recertification : **jusqu'à 15 échecs Web en exécution concurrente, 0 en exécution isolée** |
| **Origine** | Revue de convergence, confirmé en recertification |
| **Composants** | `tests/**` · `Infrastructure.Tests/Support/BaseDeTestSql.cs` |
| **Impact** | Échecs trompeurs susceptibles de masquer une vraie régression en intégration continue |
| **Risque** | RQ-09 |
| **Sévérité** | **LOW** |
| **Dépendances** | — |
| **Effort** | **XS** |
| **Priorité** | **P3** |
| **Moment** | **BACKLOG** |
| **Critère de fermeture** | `xunit.runner.json` limitant le parallélisme · suite complète stable en exécution simultanée, sur trois lancements consécutifs |

### D-11 — `ComptesAgents.razor` sans test de rendu

| | |
|---|---|
| **Titre** | Un écran d'administration doublé de taille sans couverture d'écran |
| **Description** | La page est passée de 297 à 830 lignes et a gagné trois actions en masse. Les commandes sous-jacentes sont testées (`AdministrationComptesAgentsTests`) ; **l'écran ne l'est pas** |
| **Origine** | Recertification post-remédiation |
| **Composants** | `Web/Components/Pages/Administration/ComptesAgents.razor` |
| **Impact** | Une régression d'interface sur un écran d'administration des habilitations passerait inaperçue |
| **Risque** | — |
| **Sévérité** | **LOW** |
| **Dépendances** | D-02 — tester après refonte, pas avant |
| **Effort** | **M** |
| **Priorité** | **P2** |
| **Moment** | **APRÈS PROD** |
| **Critère de fermeture** | Test bUnit couvrant les trois actions en masse, le refus serveur et l'affichage du panneau de refus |

---

## Regroupement par lot

| Lot | Entrées | Bloque prod |
|---|---|---|
| **LOT A — Bloquants production** | C-02, C-01 | **OUI (2)** |
| **LOT B — Arbitrages avant pilote** | A-04, A-02, A-01, A-03 | non |
| **LOT C — Dettes court terme** | D-01, D-02, D-03, D-04 | non |
| **LOT D — Post-production** | D-05 → D-11, I-01 → I-03, R-ACC-01/02, PP-01 → PP-04 | non |

### Chemin critique

```
C-02  (committer)
  └→ C-01  (R-15 sur copie prod)  ─────────────→  MEP
A-04  (durées légales)
  └→ D-01  (câbler la rétention)  ─────────────→  PP-01 (première exécution)
A-01 / A-02 / A-03  (arbitrages)  ─────────────→  PP-02 / PP-03 / PP-04
```

**Deux dépendances sont non négociables :** C-02 avant C-01 — on ne répète pas une migration sur un
code non versionné ; **A-04 avant D-01** — on ne déclenche pas une purge sur des délais non validés.
