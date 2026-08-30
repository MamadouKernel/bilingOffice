# Recherche & Décisions Techniques — Feature 005 : Simulation de Proforma

> **Dossier** : `specs/005-simulation-proforma/`  
> **Date** : 30 Août 2026  
> **Statut** : **VALIDÉ**  
> **Auteur** : Antigravity (Pair Programming AI)  
> **Sources de vérité** : `spec.md`, `checklists/requirements.md`, `docs/00-analyse-cdc.md` (§11.5, §22, §25, Annexe A), `docs/04-matrice-recette.md` (`REC-SCE-06`), `.specify/memory/constitution.md`

---

## 1. Inventaire des Décisions Architecturales (D1 à D16)

### D1 — Architecture du Moteur de Calcul Pur (`Domain`)
- **Problème** : Le calcul d'une proforma doit être déterministe, instantané, sans effet de bord, et testable unitairement sans dépendre d'une base de données, du framework web Blazor ou du réseau.
- **Décision** : Le moteur de calcul est implémenté sous forme d'un service de domaine pur (`MoteurCalculProforma`), sans état interne, recevant en entrée des objets de valeur (`DossierSimulation`, `IReadOnlyList<ConteneurSimulation>`, `VersionGrilleTarifaire`) et retournant un résultat immuable (`ResultatSimulationProforma`).
- **Principe constitutionnel** : Principe I (Domaine pur, aucune dépendance d'infrastructure dans les règles métier).

---

### D2 — Représentation Monétaire (Devise XOF)
- **Problème** : Les calculs financiers en Franc CFA (XOF) doivent être exacts au franc près, sans erreurs d'arrondi flottant (`float`/`double`).
- **Décision** : Utilisation du type standard `decimal` pour tous les calculs intermédiaires et montants stockés. Pour la devise XOF, qui n'a pas de sous-unité divisionnaire (centimes), chaque ligne tarifaire et poste est arrondi à l'entier le plus proche via `Math.Round(valeur, MidpointRounding.AwayFromZero)` ou calculé exactement sur des taux entiers/pourcentages.
- **Preuve** : Permet de reproduire au franc près les 5 cas de recette `test01` à `test06` de l'Annexe A.8.

---

### D3 — Représentation des Dates & Calcul de la Durée de Séjour
- **Problème** : Le CDC définit la durée de séjour comme un nombre entier de jours calendaires (bornes incluses) entre la date d'arrivée et la date d'enlèvement prévisionnel, indépendamment des fuseaux horaires ou des heures de la journée.
- **Décision** : Les dates d'arrivée et d'enlèvement sont représentées par `DateOnly`. La durée de séjour est calculée par la formule :
  $$\text{DureeSejourJours} = (\text{DateEnlevement}.DayNumber - \text{DateArrivee}.DayNumber) + 1$$
- **Invariant** : $\text{DateEnlevement} \ge \text{DateArrivee}$. Si $\text{DateEnlevement} < \text{DateArrivee}$, une exception de validation métier (`ValidationException`) est levée.

---

### D4 — Structure des Grilles Tarifaires & Paramètres Versionnés
- **Problème** : `SIM-06` et `SIM-07` exigent que tous les tarifs, taux, seuils, franchises et clés de répartition soient administrables sans développement et versionnés avec une date d'effet.
- **Décision** : Découplage clair entre :
  1. `VersionGrilleTarifaire` : Porte le numéro de version (ex. `v1.0`), la `DateEffetUtc`, l'auteur de création, le statut d'activation et la collection de `LigneTarifaire` (clé composite : Sens, Taille, Catégorie ➔ Acconage, Relevage, TarifJourBranchement) ;
  2. `ParametresTarificationProforma` : Objet de valeur rattaché à la version, définissant les taux de majoration (Lourd 30%, Dangereux 50%, OOG 50%), les seuils de poids (15t en 20", 26t en 40"), la franchise Reefer (2j), les franchises de stationnement par sens (11j Import Local, 21j Import Transit, 0j Export), les tranches de stationnement (1-5j à 850 XOF, 6-15j à 1 250 XOF, 16j+ à 1 850 XOF) et les quotes-parts de répartition (CIT 30%, OIC 20%, PAA 50%).

---

### D5 — Règle de Sélection de la Version de Grille Applicable (`CLAR-SIM-02`)
- **Problème** : Quelle version de grille doit s'appliquer lors de la soumission d'une simulation ?
- **Décision** : Le système sélectionne la version de grille active dont la `DateEffetUtc` est immédiatement antérieure ou égale à la date de création de la simulation (`DateCreationUtc`).
- **Traçabilité** : L'identifiant `VersionGrilleTarifaireId` et le libellé de version sont persistés sur l'entité `SimulationProforma`.

---

### D6 — Stratégie de Persistance & Snapshot Immuable (`SIM-08`)
- **Problème** : Lorsqu'un client enregistre une simulation, il doit pouvoir la consulter ultérieurement avec les montants exacts calculés au moment de la soumission, même si la grille tarifaire évolue par la suite.
- **Décision** : Double niveau de persistance :
  1. Données d'entrée et métadonnées métier persistées relationnellement (`SimulationProforma`, `ConteneurSimulation`) ;
  2. Résultat détaillé et ventilation des postes persistés sous forme de snapshot immuable (lignes de résultat relationnelles `LigneResultatSimulationConteneur`).
- **Bénéfice** : Consultation instantanée sans dépendance à l'état des grilles futures, historique financier inaltérable.

---

### D7 — Règle de Cumul Additif des Majorations (`SIM-02`)
- **Problème** : Comment s'appliquent les majorations (Lourd, Dangereux, OOG) lorsqu'un conteneur cumule plusieurs attributs ?
- **Décision (Prouvée par l'Annexe A.8 `test01`)** : Chaque majoration est calculée indépendamment sur le montant d'**Acconage de base** de la ligne tarifaire, puis les montants de majoration sont additionnés :
  $$\text{TotalMajorations} = (\text{TauxLourd} \times \text{Acconage}) + (\text{TauxDangereux} \times \text{Acconage}) + (\text{TauxOOG} \times \text{Acconage})$$
- **Règle absolue** : Jamais de calcul en cascade (composition multiplicative interdite).

---

### D8 — Calcul du Stationnement par Tranches Journalières Cumulatives
- **Problème** : Modélisation des franchises et tranches de stationnement.
- **Décision** : Le stationnement n'est calculé que sur les jours excédant la franchise de stationnement du sens concerné. Les jours facturables sont ventilés sur les tranches successives :
  - Tranche 1 (1 à 5 jours facturables) : tarif tranche 1 ;
  - Tranche 2 (6 à 15 jours facturables) : tarif tranche 2 ;
  - Tranche 3 (16 jours et plus) : tarif tranche 3.
- Si le sens a une franchise nulle ou est configuré comme non soumis au stationnement (ex. Export dans le paramétrage initial), le montant de stationnement est de $0\text{ XOF}$.

---

### D9 — Calcul du Branchement Frigorifique (Reefer)
- **Problème** : Modélisation du poste Reefer.
- **Décision** : Uniquement pour les conteneurs dont $\text{Type} == \text{Reefer}$. La franchise de 2 jours est déduite de la durée de séjour :
  $$\text{JoursFacturablesReefer} = \max(0, \text{DureeSejour} - 2)$$
  $$\text{MontantBranchement} = \text{JoursFacturablesReefer} \times \text{TarifJourBranchement}$$
- Pour un conteneur $\text{Dry}$, ce montant est strictement égal à $0\text{ XOF}$.

---

### D10 — Traitement des Combinaisons Non Tarifées (`CLAR-SIM-03`)
- **Problème** : Que faire si un conteneur a une combinaison $(\text{Sens}, \text{Taille}, \text{Catégorie})$ absente de la grille ?
- **Décision** : Le moteur lève une exception explicite de domaine `TarifNonTrouveException` indiquant précisément le conteneur en cause et le triplet manquant. Le calcul est bloqué. Aucun tarif par défaut à 0 n'est appliqué silencieusement.

---

### D11 — Détection de Révision de Grille sur Simulation Enregistrée (`CLAR-SIM-04`)
- **Problème** : Comment avertir le client qu'une nouvelle grille est en vigueur lors de la consultation d'une ancienne simulation ?
- **Décision** : La vue de détail compare le `VersionGrilleTarifaireId` de la simulation avec l'identifiant de la version actuellement active. Si une version plus récente existe, un indicateur booléen `EstGrilleObsolete = true` est renvoyé, déclenchant l'affichage du badge d'avertissement et du bouton "Recalculer sur la grille active".

---

### D12 — Extension Additive pour le Rattachement Billing (`CLAR-SIM-05`)
- **Problème** : Comment permettre le rattachement ultérieur d'une simulation à un ticket/dossier sans altérer les règles des Features 001 à 004 ?
- **Décision** : Ajout d'une propriété optionnelle et additive `ReferenceSimulationProforma` (`string?`, max 64) sur l'agrégat `Dossier`. Le client peut saisir cette référence lors de la soumission d'une demande Import ou Export. Aucune contrainte de clé étrangère bloquante n'est imposée pour préserver l'autonomie des modules.

---

### D13 — Abstraction & Génération du Document PDF (`SIM-09`)
- **Problème** : Générer un document PDF professionnel de simulation respectant les règles d'architecture propre (pas de couplage rigide du Domain avec une librairie PDF externe).
- **Décision** : Définition de l'interface applicative `IGenerateurDocumentProformaPdf`. L'implémentation dans la couche Infrastructure produit le document binaire contenant l'en-tête CIT, la référence, la date, la version de grille, le tableau des conteneurs avec décomposition des 7 postes, le total général et la mention légale obligatoire (`SIM-05`).

---

### D14 — Import de Grille Tarifaire par Fichier CSV Atomique (`SIM-10`, `CLAR-SIM-01`)
- **Problème** : Permettre à l'administrateur de charger une nouvelle grille par CSV sans risque de corruption partielle de la base.
- **Décision** : Service d'importation `ImportateurGrilleTarifaireCsv` :
  1. Analyse syntaxique du flux CSV (séparateur `;`, encodage UTF-8) ;
  2. Validation de l'en-tête obligatoire (`Sens;Taille;Categorie;Acconage;Relevage;BranchementJour`) ;
  3. Validation des types, montants positifs et valeurs énumérées sur l'ensemble des lignes ;
  4. Si une seule erreur est détectée ➔ Rejet global atomique avec liste exhaustive des erreurs (numéro de ligne + anomalie) ;
  5. Si 100% valide ➔ Création de la nouvelle `VersionGrilleTarifaire` avec ses lignes et enregistrement transactionnel.

---

### D15 — Habilitations, Sécurité & Multi-Tenant
- **Problème** : Garantir la sécurité et l'absence totale de failles IDOR.
- **Décision** :
  - **Espace Client** : Accessible à tout utilisateur client rattaché à une société active. Les simulations créées sont rattachées au `SocieteId`. Les requêtes de consultation vérifient la propriété via `CloisonnementService` ;
  - **Espace Administration Tarifaire** : Protégé par la nouvelle constante de permission `Permission.TarificationAdministrer` (`BillingTarificationAdministrer`). Seuls les agents CIT détenteurs de cette habilitation peuvent créer des versions de grille ou importer des fichiers CSV.

---

### D16 — Golden Master Contractuels (Annexe A.8)
- **Décision** : Création d'une suite de tests dédiée `GoldenMasterProformaTests` couvrant les identifiants :
  - `GOLDEN-SIM-01` ➔ `test01` (Import Transit, 20", Dry, 100t, OOG Oui, Dang Oui, C4) = **199 129 XOF**
  - `GOLDEN-SIM-02` ➔ `test02` (Import Local, 40", Reefer, 200t, OOG Non, Dang Non, C1) = **2 054 980 XOF**
  - `GOLDEN-SIM-03` ➔ `test03` (Export Local, 20", Dry, 300t, OOG Oui, Dang Non, C2) = **245 500 XOF**
  - `GOLDEN-SIM-04` ➔ `test04` (Export Transit, 40", Reefer, 100t, OOG Non, Dang Oui, C5) = **2 053 400 XOF**
  - `GOLDEN-SIM-05` ➔ `test06` (Import Local, 40", Reefer, 300t, OOG Non, Dang Non, C3) = **2 245 300 XOF**
- **Assertion** : Égalité stricte au franc près (`Assert.Equal(attendu, calcule)`).
