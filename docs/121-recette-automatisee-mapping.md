# Recette §22 automatisée — correspondance scénarios ↔ tests & mode d'exécution — 10/10/2026

> **Constat clé : la recette §22 est déjà entièrement automatisée.** Les « agents de test » demandés
> existent sous forme de tests d'intégration / end-to-end (familles `RecSce01→14`, `GoldenMasterProforma`,
> `Stationnement`, `SimCert`). Ce document les recense, les mappe au CDC, et explique comment les lancer.
>
> État au 10/10 : **55 tests recette verts** (17 Domain + 6 Application + 32 Web), 0 échec.

## 1. Billing Office — parcours Import (4 types de client) & Export

| Scénario §22 | Test automatisé | Fichier |
|---|---|---|
| Import **Normal** (multi-BL instruits indépendamment) | `Trois_bl_dans_un_dossier_multi_bl_sont_instruits_independamment…` | `RecSce01ImportNormalTests` |
| Import **En Compte** (facture → contrôle doc, sans paiement) | `Facture_editee_passe_directement_au_controle_documentaire…` | `RecSce02ImportEnCompteTests` |
| Import **Deposit** (facture → lettrage → contrôle doc → clôture) | `Facture_puis_lettrage_confirme_puis_controle_documentaire…` | `RecSce03ImportDepositTests` |
| Import **IES** (traitement **parallèle**, champs réservés par rôle, jointure) | `Traitement_parallele_avec_champs_reserves_par_role_jointure…` | `RecSce04ImportIesTests` |
| **Export** E2E pour les 4 types + multi-bookings indépendants | 5 tests `REC_SCE_05_Circuit_Client_*` + `Multi_Bookings_Independance` | `RecSce05ExportE2ETests` |

## 2. Moteur de Simulation de Proforma — cas de référence au franc près

| Cas de référence | Test automatisé (golden master) | Fichier |
|---|---|---|
| test01 Import Transit 20' Dry 100t OOG Dangereux C4 | `GOLDEN_SIM_01_test01_…` | `GoldenMasterProformaTests` |
| test02 Import Local 40' Reefer 200t C1 | `GOLDEN_SIM_02_test02_…` | id. |
| test03 Export Local 20' Dry 300t OOG C2 | `GOLDEN_SIM_03_test03_…` | id. |
| test04 Export Transit 40' Reefer 100t Dangereux C5 | `GOLDEN_SIM_04_test04_…` | id. |
| test06 Import Local 40' Reefer 300t C3 | `GOLDEN_SIM_05_test06_…` | id. |
| Stationnement par tranches + franchise + 4ᵉ tranche sans dev | `Une_quatrieme_tranche_de_stationnement_sajoute_sans_developpement` + `StationnementEtReeferTests` | id. / `StationnementEtReeferTests` |
| Calcul multi-conteneurs contrôlé **au franc près** vs Annexe A8 | `Calcul_multi_conteneurs_controle_au_franc_pres_contre_Annexe_A8` | `RecSce06SimulationE2ETests` |
| Changement de paramètre tarifaire répercuté sans dev | `Modification_de_parametre_tarifaire…se_repercute_immediatement` | id. |

> Dry **et** Reefer (franchise 2 j), majorations lourd/dangereux/hors-gabarit (OOG) et stationnement par
> tranches sont tous exercés. Les montants attendus sont figés dans le golden master (comparaison exacte).

## 3. Socle transverse & workflows

| Scénario §22 | Test automatisé | Fichier |
|---|---|---|
| Tickets regroupés en dossier + suivi global | `Plusieurs_tickets_du_meme_client_sont_regroupes_dans_un_dossier…` | `RecSce14TransverseTests` |
| Journal d'audit inviolable des actions sensibles | `Les_actions_sensibles_du_socle_produisent_une_entree_d_audit_verifiable` | id. |
| Prise en charge (file → acceptation, horodatages) | `Un_ticket_arrive_en_attente_de_pec_puis_est_accepte…` | `RecSce09PriseEnChargeTests` |
| Transfert conforme à la **matrice** ; hors matrice refusé | `Transfert_autorise_conserve_le_ticket_et_transfert_hors_matrice_est_refuse` | `RecSce10TransfertTests` |
| Réaffectation A→B tracée ; refus sans habilitation | 3 tests `Reaffectation_*` | `RecSce11ReaffectationTests` |
| **SLA** : dépassement déclenche alerte ; **attente client** non imputée | `Un_ticket_qui_depasse_son_sla…` + `L_attente_client_n_est_jamais_imputee…` | `RecSce12TempsSlaTests` |
| Supervision temps réel + affectation d'un ticket non pris en charge | `Un_superviseur_consulte_sa_vue_temps_reel_et_affecte…` | `RecSce13SupervisionTests` |
| Cloisonnement inter-sociétés | 2 tests `RecSce14Cloisonnement*` | `RecSce14CloisonnementCompteTests` |

## 4. Modules complémentaires

| Scénario §22 | Test automatisé | Fichier |
|---|---|---|
| **Customer Complaint** (soumission → instruction → complément → clôture → évaluation) | `Scenario_Complet_Soumission_Instruction_Complement_Cloture_Evaluation` + `RecSce07_ParcoursContractuelComplet_Reussite` | `RecSce07CustomerComplaintE2ETests` |
| **Enrôlement transporteur** (soumission → complément → validation Gate → convocation → badge → clôture) | `Scenario_Complet_Soumission_Complement_Validation_Convocation_Badge_Cloture` | `RecSce08EnrolementTransporteurE2ETests` |
| Continuité guichet / rendez-vous de bout en bout | `Scenario_Complet_REC_SCE_14_DeBoutEnBout` | `RecSce14ContinuiteGuichetRendezVousE2ETests` |
| Notifications multicanales (7 volets) | `REC_SCE_14_E2E_demonstration_complete_des_7_volets_contractuels` | `RecSce14NotificationsE2ETests` |

## 5. Comment les exécuter

### Toute la recette en une commande (depuis la racine du dépôt)

```bash
dotnet test --filter "FullyQualifiedName~RecSce|FullyQualifiedName~GoldenMasterProforma|FullyQualifiedName~Stationnement|FullyQualifiedName~SimCert"
```

Résultat attendu : **55 réussis, 0 échec** (Domain + Application + Web).

### Un domaine précis

```bash
dotnet test --filter "FullyQualifiedName~RecSce05"          # Export
dotnet test --filter "FullyQualifiedName~GoldenMasterProforma"  # Proforma au franc près
dotnet test --filter "FullyQualifiedName~RecSce12"          # SLA / attente client
```

### Toute la suite (régression complète, ~2160 tests)

```bash
dotnet test
```

### En continu (déjà en place)

Le pipeline **GitHub Actions** (`.github/workflows/ci.yml`) exécute `dotnet test` à chaque push / PR :
la recette tourne donc automatiquement à chaque livraison, sans testeur humain.

## 6. Nature de ces tests (à expliquer au métier)

- Ce sont des **tests d'intégration / E2E au niveau applicatif** : ils rejouent les parcours complets
  à travers les commandes, le domaine et la persistance (base en mémoire), en vérifiant états, transitions,
  montants et traces d'audit. Ils prouvent que **la logique métier** est conforme.
- Ils **ne remplacent pas** la recette **signée** par le métier (J7→J9 du plan `docs/116`) : la signature
  reste requise pour prononcer la conformité. Mais ils évitent de mobiliser des testeurs sur les
  vérifications répétitives, et garantissent la non-régression à chaque modification.
- Ce qui n'est **pas** couvert par ces tests (hors périmètre code) : l'IHM pixel par pixel, les canaux
  SMS/WhatsApp (exclus), et les preuves en environnement réel (perf, dispo, restauration — scripts `deploy/`).
