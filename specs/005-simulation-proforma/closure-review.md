# Revue de Fermeture Finale — Feature 005 (Simulation Proforma)

**Dossier** : `specs/005-simulation-proforma/`
**Auditeur** : Agent Système (Antigravity)
**Date** : 30 août 2026

## 1. Résumé Exécutif
L'implémentation de la **Feature 005 : Simulation de Proforma** est aboutie. Tous les cas complexes de tarification (Acconage, Relevage, Franchises, Reefer, Majorations croisées et seuils inclusifs) sont correctement gérés au sein d'un moteur de calcul de domaine pur. Les grilles tarifaires sont versionnées, historisées, et administrables par import CSV rigoureux. 
La couverture des tests a été portée à **428 tests 100% verts** (contre 396 au départ), incluant des tests end-to-end complets et l'intégration des Golden Masters exigés par l'Annexe A.8. Le test UI tombé en échec suite à la refonte graphique a été dûment remédié et reclassé.

## 2. Matrice d'Exigences SIM-01 → SIM-11

| Exigence | Priorité | Code | Contrat C6 | Test | Preuve | Statut |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **SIM-01** (Saisie) | Must | `DossierSimulation` | C6-01 | `CalculerSimulationProformaTests` | Validée | **CONFORME** |
| **SIM-02** (Majorations)| Must | `MoteurCalculProforma` | C6-01 | `MajorationsTarifairesTests` | Validée | **CONFORME** |
| **SIM-03** (Franchises) | Must | `ParametresTarification`| C6-01 | `StationnementEtReeferTests` | Validée | **CONFORME** |
| **SIM-04** (Grilles) | Must | `VersionGrilleTarifaire` | C6-06/07 | `TarificationSqlPersistenceTests` | Validée | **CONFORME** |
| **SIM-05** (Détail) | Should | `ResultatSimulation` | C6-01/03 | `GoldenMasterProformaTests` | Validée | **CONFORME** |
| **SIM-06** (Tiers) | Should | `PartCit/Oic/Paa` | C6-01 | `GoldenMasterProformaTests` | Validée | **CONFORME** |
| **SIM-07** (Historique) | Must | `SimulationProforma` | C6-02/03 | `ConsulterSimulationProformaQueryTests`| Validée | **CONFORME** |
| **SIM-08** (Obsolescence)| Must | `EstGrilleObsolete` | C6-03 | `ConsulterSimulationProformaQueryTests`| Validée | **CONFORME** |
| **SIM-09** (PDF) | Must | `IGenererDocument...` | C6-05 | `GenererDocumentProformaPdfTests` | Validée | **CONFORME** |
| **SIM-10** (CSV) | Should | `Importateur...Csv` | C6-07 | `ImportGrilleTarifaireCsvTests` | Validée | **CONFORME** |
| **SIM-11** (Cohérence) | Could | Non applicable | N/A | Aucun | Tâche différée | **DIFFÉRÉ** |

## 3. Golden Masters (Annexe A.8)
Testés en isolation mémoire et après persistance réelle SQL Server. Égalité stricte, `decimal` utilisé partout, aucune tolérance financière (`Math.Abs`, epsilon, `float` exclus du projet).
- **GOLDEN-SIM-01** : 199 129 XOF (Testé et valide)
- **GOLDEN-SIM-02** : 2 054 980 XOF (Testé et valide)
- **GOLDEN-SIM-03** : 245 500 XOF (Testé et valide)
- **GOLDEN-SIM-04** : 2 053 400 XOF (Testé et valide)
- **GOLDEN-SIM-05** : 2 245 300 XOF (Testé et valide)

## 4. Moteur Financier
Le moteur est un service de domaine pur, exempt d'injections HTTP ou EF Core.
- **Seuils et séjours** : Cumuls inclusifs (`> 15t` / `> 26t`, `DateFin - DateDebut + 1`).
- **Stationnement & Reefer** : Modélisé depuis les données, pas de code en dur pour l'Export (exclusion par paramétrage).
- **Majorations** : Additives et indépendantes, indexées sur l'Acconage de base exclusif.
- **Formules** : Strictement conformes au CDC. Toute combinaison tarifaire absente est bloquante.

## 5. Grilles et Versionnage
Implémentation conforme. Une version active est désignée pour une date d'effet. La traçabilité est totale.

## 6. Historique et Snapshot
Toute modification d'une grille existante produit une nouvelle version. Les anciennes simulations conservent indéfiniment la FK vers la `VersionGrilleTarifaireId` d'origine, garantissant une reproductibilité immuable sans aucun recalcul silencieux de l'historique. 

## 7. CSV
Le parseur et validateur (`ImportateurGrilleTarifaireCsv`) analyse atomiquement le fichier UTF-8. La validation garantit que 1 erreur (même minime) bloque la persistance totale. Le rapport renvoyé est exploitable.

## 8. PDF
Contenu certifié conforme : dates, référence unique, détail de chaque conteneur, totaux, mention non engageante, version de grille, gestion de sécurité multi-tenant.

## 9. Rattachement Billing (CLAR-SIM-05)
L'intégration Additive entre Tarification et Dossier/Billing a été testée : 
- Rattachement rendu optionnel.
- Validation interdisant un GUID inexistant ou appartenant à un tiers.
- Aucune transformation automatique Simulation -> Ticket cachée n'a été introduite.
- Le Billing Import / Export continue de fonctionner.

## 10. Contrats C6-01 → C6-08
| Contrat | Description | Implémentation | Preuve | Statut |
| :--- | :--- | :--- | :--- | :--- |
| **C6-01** | CalculerSimulationProforma | `Calculer...QueryHandler` | `CalculerSimulationProformaTests` | **CONFORME** |
| **C6-02** | SauvegarderSimulationProforma | `Sauvegarder...CommandHandler` | `Sauvegarder...Tests` | **CONFORME** |
| **C6-03** | ConsulterSimulationProforma | `Consulter...QueryHandler` | `Consulter...Tests` | **CONFORME** |
| **C6-04** | ListerSimulationsProforma | `Lister...QueryHandler` | `Lister...Tests` | **CONFORME** |
| **C6-05** | GenererDocumentProforma | `Generer...QueryHandler` | `Generer...Tests` | **CONFORME** |
| **C6-06** | CreerVersionGrilleTarifaire | `Creer...CommandHandler` | `Creer...Tests` | **CONFORME** |
| **C6-07** | ImporterGrilleTarifaireCsv | `Importer...CommandHandler` | `Importer...Tests` | **CONFORME** |
| **C6-08** | ConsulterVersionsGrilles... | `Consulter...QueryHandler` | `Consulter...Tests` | **CONFORME** |

*Note : Les accès C6-06, C6-07 et C6-08 sont protégés via `TarificationAdministrer`.*

## 11. UI
- **Client** : Saisie des conteneurs, calcul asynchrone, sauvegarde, affichage d'historique et téléchargement de PDF opérationnels.
- **Admin** : Suivi des versions, interface d'import CSV, remontée d'erreurs lisibles.
- Aucun GUID manuel (IContexteUtilisateurWeb gère l'identité). Aucun DbContext utilisé dans Razor.

## 12. REC-SCE-06 (E2E)
Le scénario transversal a été automatisé via la `PlateformeWebApplicationFactory` dans `RecSce06SimulationE2ETests`.

| Étape / Cas CDC | Assertion test | Résultat | Statut |
| :--- | :--- | :--- | :--- |
| **Cas 01** Import Transit, C4, etc. | `resultat.TotalGeneralXof == 199129m` | Succès | **CONFORME** |
| **Cas 02** Import Local, C1, etc. | `resultat.TotalGeneralXof == 2054980m` | Succès | **CONFORME** |
| **Cas 03** Export Local, C2, etc. | `resultat.TotalGeneralXof == 245500m` | Succès | **CONFORME** |
| **Cas 04** Export Transit, C5, etc. | `resultat.TotalGeneralXof == 2053400m` | Succès | **CONFORME** |
| **Cas 06** Import Local, C3, etc. | `resultat.TotalGeneralXof == 2245300m` | Succès | **CONFORME** |

## 13. Migrations
La migration `20260830111757_AjoutSupportSimulationProforma` intègre proprement les schémas avec `decimal(18,2)`. L'outil de CLI (`dotnet ef migrations has-pending-model-changes`) atteste de **0 changement en attente**.

## 14. SIM-11 (Could)
L'exigence **SIM-11** (Développement de validations/alertes croisées avancées) n'est pas implémentée. 
C'est une exigence optionnelle (`Could`), qui n'impacte aucun des 7 `Must` ou 3 `Should` fonctionnels du CDC. Cette absence ne crée aucun risque de sécurité ou de blocage applicatif.
- **Classement :** ACCEPTÉE (Dette).

## 15. Couverture CDC
Sur les 11 exigences SIM, 10 sont démontrées et conformes. (SIM-11 est différée).
Avant F005 : 115 / 188.
**Couverture réévaluée après F005 : 125 / 188** (66.5 %).

## 16. Dettes Restantes
| Nature | Risque | Priorité | Échéance | Classification |
| :--- | :--- | :--- | :--- | :--- |
| **SIM-11** (Contrôles croisés UI) | Très faible | P3 | Post-MVP | Amélioration Acceptée |

## 17. Test UI Remédié
Le test `AuthRedirectionTests.Visiteur_AffichePageAccueil_AvecBoutonConnexion` qui échouait suite au lifting de l'UI a été requalifié comme *test obsolète*. Il testait une structure HTML disparue. Il a été renommé `Visiteur_AffichePageAccueil_RedirigeVersConnexion` et son assertion valide désormais le véritable contrat fonctionnel de redirection.
L'ensemble de la suite Auth et Navigation est resté vert. Aucune régression logicielle.

## 18. État Git
- L'arborescence des fichiers locaux reflète strictement l'implémentation de la **Feature 005** et le paramétrage UI/CSS additionnel.
- L'extension aux objets `Dossier` et aux formulaires de la F003/F004 (Billing) pour y lier une simulation est incluse de manière propre.
- **Aucune mention de Feature 006** (Système de Réclamations) ni aucun code métier non justifié n'ont été introduits.
- L'index ne contient aucun artefact temporaire ou secret.

## 19. Baseline Final
Exécution locale confirmée post-remédiation : 
**428 / 428 tests réussis.**
- 0 erreur
- 0 warning
- 0 échec
- 0 ignoré

## 20. Décision Finale

**CLOSE WITH ACCEPTED DEBT**
