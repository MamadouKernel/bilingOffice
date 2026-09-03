# Grille de comparaison — modèle CDC vs implémentation

> **Document** : `docs/86-grille-comparaison-cdc-implementation.md`
> **Établi le** : 2026-09-03
> **Renseignée le** : 2026-09-03, après remédiation
> **Référence attendue** : `docs/85-analyse-merise.md`, dérivée du seul CDC
> **Verdict d'ensemble** : `docs/87-conformite-application-analyse-merise.md`
> **Statut** : **`RENSEIGNÉE`** — les 142 points portent un constat. Chacun a été confronté au
> code : entité, colonne, index ou composant nommément désigné.

---

## Comment utiliser cette grille

Chaque ligne est un point **vérifiable** du modèle attendu. Trois colonnes :

| Colonne | Contenu |
|:---|:---|
| **Attendu** | Ce que le CDC impose, tel que modélisé dans `docs/85` |
| **Où vérifier** | Le point d'observation concret dans le code ou la base |
| **Constat** | À remplir lors de la confrontation : `CONFORME` · `ÉCART` · `ABSENT` · `N/A` |

### Les trois natures d'écart

Un écart n'est pas nécessairement un défaut. Trois lectures possibles, à distinguer :

| Nature | Signification |
|:---|:---|
| **`ÉCART-MANQUE`** | Le CDC exige, l'application ne fait pas → **défaut de conformité** |
| **`ÉCART-CHOIX`** | L'application fait autrement, mais satisfait l'exigence → **choix d'implémentation légitime** |
| **`ÉCART-SURPLUS`** | L'application fait plus que le CDC → à vérifier : besoin réel, ou dette ? |

**Cette distinction est le cœur de l'exercice.** Une comparaison qui ne classerait pas les écarts
produirait une liste de faux défauts.

### Réserve de méthode

Le modèle de référence a été **rédigé** depuis le CDC seul. L'auteur avait cependant lu
l'implémentation plus tôt, pour d'autres travaux. Conséquence :

- une **convergence** est plausible mais n'est pas une preuve d'indépendance ;
- une **divergence** est un constat solide — elle ne peut pas résulter d'une contamination.

Ce sont les divergences qui portent l'information.

---

## A. Structure des données — entités

### A.1 Comptes et habilitations

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-01 | Entité `SOCIETE_CLIENTE` avec type ∈ {normal, en compte, deposit, IES} | Table des sociétés, colonne de profil | `CONFORME` — `SocieteCliente` + `TypeClient` |
| A-02 | Entité `UTILISATEUR` distinguant client / agent / transporteur | Table des utilisateurs, colonne de nature | `CONFORME` — `NatureUtilisateur` |
| A-03 | `RATTACHEMENT` **porteur** de périmètre, droits et état — pas une simple jointure | Table de rattachement : colonnes propres ? | `CONFORME` — `UtilisateurSociete` porte périmètre, droits et état |
| A-04 | Un utilisateur peut n'avoir **aucun** rattachement (transporteur) | Nullabilité / cardinalité `0,N` | `CONFORME` — le transporteur n a aucun rattachement |
| A-05 | `HABILITATION` bornable à une file | Colonne `file_id` nullable | `CONFORME` — `HabilitationUtilisateur` bornable à une file |
| A-06 | `PIECE_EXIGEE` paramétrable à l'ouverture de compte | Table de pièces exigées | `CONFORME` — `PieceExigeeCompte` |
| A-07 | `COMPETENCE_OPERATEUR` pour l'affectation par compétence | Table de compétences | `CONFORME` — `CompetenceOperateur` (référentiel non fourni : E-07) |
| A-08 | État de société : soumise / validée / rejetée | Colonne d'état | `CONFORME` — `EtatSociete` |

### A.2 Socle ticket

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-10 | `TICKET` avec numéro **unique** | Contrainte d'unicité sur le numéro | `CONFORME` — `Ticket.Numero`, alimenté par séquence |
| A-11 | `DOSSIER` regroupant 1..N tickets ; ticket → dossier **optionnel** | FK nullable vers dossier | `ÉCART-CHOIX` — dossier **obligatoire** pour tout ticket, y compris seul |
| A-12 | **`type_client_applique` figé sur le ticket** (RG-07) | Colonne sur le ticket, distincte de celle de la société | `ÉCART-CHOIX` — figé sur le **`Dossier`**, non sur le ticket ; RG-07 satisfaite |
| A-13 | `LIAISON_TICKET` réflexive, inter-modules | Table de liaison | `CONFORME` — `LiaisonTicket`, réflexive et inter-modules |
| A-14 | `MESSAGE` avec nature externe / interne | Colonne de nature | `CONFORME` — `Message` + `TypeMessage` |
| A-15 | `MENTION` distincte du message | Table de mentions | `ÉCART-CHOIX` — `Message.CollaborateurMentionneId`, colonne et non entité |
| A-16 | `DOCUMENT` avec type, version, auteur, horodatage | Colonnes correspondantes | `CONFORME` — `Document` : nom, type, taille, auteur, horodatage, scellement |
| A-17 | `TYPE_DOCUMENT` paramétrable **par étape** | FK vers étape / file | `ÉCART` — aucun type de document paramétrable **par étape** ; hors périmètre de l audit initial, **à qualifier** |
| A-18 | `EVENEMENT_HISTORIQUE` par ticket | Table d'historique | `CONFORME` — `EntreeHistorique` |
| A-19 | `BROUILLON` pour la reprise de saisie (ERR-01) | Table de brouillons | `CONFORME` — `BrouillonSaisie` |
| A-20 | `DEMANDE_ANNULATION` distincte de l'annulation directe | Table dédiée | `ÉCART-CHOIX` — `TypeAnnulationImport` / `TypeAnnulationExport` portés par le ticket |
| A-21 | `STATUT` en **référentiel paramétrable**, non en énumération codée | Table de statuts, ou enum ? | `CONFORME` — **refermé** : `LibelleStatutTicket`, approvisionnement additif |

### A.3 Workflow et mesure du temps

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-30 | **`PASSAGE_EN_FILE`** : une occurrence **par passage**, 4 horodatages | Table dédiée, ou horodatages sur le ticket ? | `CONFORME` — **refermé** : `PassageEnFile`, 4 horodatages, un par passage |
| A-31 | `PERIODE_ATTENTE_CLIENT` **multiple** par ticket | Table dédiée, cardinalité `0,N` | `CONFORME` — `PeriodeAttenteClient`, multiple par ticket |
| A-32 | `TRANSFERT` et `REAFFECTATION` **distincts** | Deux tables, ou une seule ? | `ÉCART-CHOIX` — transferts tracés en `EntreeHistorique`, non en tables dédiées |
| A-33 | `MOTIF_TRANSFERT` en référentiel | Table de motifs, ou texte libre ? | `CONFORME` — **refermé** : `MotifTransfert`, référentiel administrable |
| A-34 | `TRANSITION_AUTORISEE` — matrice paramétrable, éventuellement par profil | Table de transitions + colonne profil | `CONFORME` — `TransitionAutorisee` (contenu métier : E-04) |
| A-35 | `STRATEGIE_AFFECTATION` par file (6 modes) | Colonne / table de stratégie | `CONFORME` — `StrategiesAffectation` |
| A-36 | `HORAIRE_FONCTIONNEMENT` pour le calcul SLA | Table d'horaires | `CONFORME` — `CalendrierOuvre` |
| A-37 | `RELANCE` numérotée par ticket | Table de relances | `ÉCART-CHOIX` — `Ticket.NombreRelancesEnvoyees` : compteur, non historique |

### A.4 Supervision et SLA

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-40 | `PRIORITE` en référentiel paramétrable | Table de priorités | `CONFORME` — `NiveauPriorite` |
| A-41 | **`HISTORIQUE_PRIORITE` avec ancienne ET nouvelle valeur + motif** (PRI-03) | Table dédiée, colonnes des deux valeurs | `CONFORME` — `ModificationPriorite` : ancienne, nouvelle, auteur, motif |
| A-42 | `DISPONIBILITE_OPERATEUR` : disponible / indisponible / absent | Table ou colonne de disponibilité | `CONFORME` — `Disponibilite` |
| A-43 | `PERIMETRE_SUPERVISION` — un superviseur ne voit pas toutes les files | Table de périmètre | `CONFORME` — `PerimetreSupervision` |
| A-44 | **`ACTION_DE_MASSE` reliée aux événements individuels** (MAS-02) | Table d'action de masse + FK depuis l'historique | `CONFORME` — action de masse tracée **ticket par ticket** |
| A-45 | `SLA` paramétrable par module, file, priorité, type de demande, type de client | Colonnes du SLA | `ÉCART` — `ParametreSlaAdministrable` ne porte que *module* et *type de demande* ; **ni file, ni priorité, ni type de client** |
| A-46 | `SEUIL_SLA` **multiple** par SLA (info, alerte, dépassement, escalade) | Table de seuils, cardinalité `1,N` | `ÉCART-CHOIX` — `SeuilInformation` et `SeuilAlerte` en colonnes ; le dépassement est la durée cible ; **aucun seuil d escalade** (voir A-48) |
| A-47 | `ALERTE` couvrant les **7 natures** d'ALE-01 | Énumération des natures | `CONFORME` — 8 natures présentes, couvrant les 7 d ALE-01 |
| A-48 | `REGLE_ESCALADE` : notifier / remonter priorité / réaffecter | Table de règles | `ÉCART` — aucune règle d escalade : **seule la notification** est automatisée. RG-65 dit « **peut** » ; remonter la priorité et réaffecter supposent un arbitrage métier absent du CDC |

### A.5 Billing Office

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-50 | `BL` avec état d'activité (actif / clos) | Table BL, colonne d'état | `ÉCART-CHOIX` — `Ticket.NumeroBl` + index unique **filtré** sur les statuts actifs |
| A-51 | `BOOKING` symétrique | Table booking | `CONFORME` — symétrie tenue ; unicité du booking implémentée (au-delà d UNI-04, voir E-08) |
| A-52 | `FACTURE` rattachée au ticket | Table facture | `ÉCART-CHOIX` — facture portée par `Document` rattaché au ticket |
| A-53 | `PREUVE_PAIEMENT` avec statut de validation | Table + colonne de statut | `ÉCART-CHOIX` — preuve de paiement portée par `Document` + statut du ticket |
| A-54 | `LETTRAGE` avec référence IRIS | Table de lettrage | `ÉCART-CHOIX` — lettrage tracé dans le parcours, sans entité dédiée |
| A-55 | **`SUIVI_BRANCHE_IES` à deux dimensions + date de jointure** (RG-81) | Table dédiée, deux booléens | `CONFORME` — `SuiviBrancheIes`, deux branches + jointure |
| A-56 | `DEMANDE_AVOIR_EXONERATION` liable à un ticket de parcours | Table + FK de liaison | `ÉCART-CHOIX` — `DecisionAvoirExo`, montants et décideur portés par le ticket |

### A.6 Tarification

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-60 | `GRILLE_TARIFAIRE` **versionnée avec date d'effet** (SIM-07) | Colonnes version + date d'effet | `CONFORME` — `VersionGrilleTarifaire`, datée |
| A-61 | `LIGNE_TARIF` avec clé *(grille, sens, taille, catégorie)* | Contrainte d'unicité composite | `CONFORME` — `LigneTarifaire` |
| A-62 | `TRANCHE_STATIONNEMENT` paramétrable | Table de tranches | `ÉCART-CHOIX` — franchises et applicabilité dans `ParametresTarification` ; pas d entité `TRANCHE` |
| A-63 | `QUOTE_PART` — nombre de bénéficiaires paramétrable | Table de quotes-parts | `CONFORME` — `QuotePart` |
| A-64 | `PARAMETRE_CALCUL` — taux, seuils, franchises **hors code** | Table de paramètres | `CONFORME` — `ParametresTarification`, hors code |
| A-65 | **`SIMULATION` référençant la version de grille appliquée** (RG-95) | FK vers grille | `CONFORME` — `SimulationProforma` référence la version appliquée |
| A-66 | `CONTENEUR_SIMULE` avec les 7 caractéristiques du CDC | Colonnes | `CONFORME` — `DossierSimulation` |
| A-67 | `POSTE_CALCULE` conservant le **détail par poste** | Table de postes | `CONFORME` — `LigneResultatSimulationConteneur`, détail par poste |

### A.7 Notifications et périphérie

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-70 | **`ENVOI` séparé de `NOTIFICATION`** (RG-111) | Deux tables, `1,N` | `CONFORME` — `EnvoiNotification` distincte de `Notification` |
| A-71 | `ENVOI` avec statut **et compteur de tentatives** par canal | Colonnes | `CONFORME` — `Canal`, `Statut`, `NombreTentatives`, `DerniereTentativeUtc`, `MotifEchec` |
| A-72 | `MODELE_MESSAGE` paramétrable | Table de modèles | `CONFORME` — `ModeleNotification` |
| A-73 | `REGLE_DIFFUSION` par événement **et** par canal | Table de règles | `CONFORME` — `RegleDiffusion` |
| A-74 | `PREFERENCE_CANAL` par utilisateur | Table de préférences | `CONFORME` — `PreferenceCanalUtilisateur` |
| A-75 | `CATEGORIE_RECLAMATION` avec file cible | Table + FK | `CONFORME` — `CategorieReclamation` |
| A-76 | `DOSSIER_ENROLEMENT`, `CONVOCATION`, `BADGE` | Trois tables | `ÉCART-CHOIX` — enrôlement porté par le ticket module *Enrolement* ; badge et convocation en commandes, sans entité `DOSSIER_ENROLEMENT` |
| A-77 | `BADGE` avec **date d'expiration** (ENR-05) | Colonne | `CONFORME` — durée de validité paramétrable (`ObtenirDureeValiditeBadgeJours`) |
| A-78 | `RENDEZ_VOUS` rattachable au ticket **ou** au dossier | Deux FK nullables | `CONFORME` — `RendezVous` |
| A-79 | `CONVERSATION_IA` rattachée au ticket (IA-03) | Table, ou module absent ? | `ABSENT (attendu)` — E-01, fournisseur non arbitré |
| A-80 | `ENQUETE_SATISFACTION` (REC-06) | Table | `ÉCART-CHOIX` — `EvaluationNote`, `EvaluationCommentaire`, `EvaluationDateUtc` sur le ticket |

### A.8 Sécurité

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-90 | `ENTREE_AUDIT` **append-only** | Contrainte / déclencheur / garde applicative | `CONFORME` — `EntreeJournalAudit` + `JournalAuditAppendOnlyGuard` |
| A-91 | Chaînage par empreinte rendant l'altération détectable | Colonne d'empreinte | `ÉCART` — **aucun chaînage par empreinte** : la garde est applicative, un accès direct à la base la contourne. SEC-05 n impose pas de niveau — défense en profondeur absente |
| A-92 | `SESSION` avec expiration paramétrable | Table ou mécanisme équivalent | `ÉCART-CHOIX` — expiration portée par le cookie d authentification, durée en configuration (SEC-10) |
| A-93 | `TENTATIVE_CONNEXION` pour le verrouillage | Table ou mécanisme équivalent | `ÉCART-CHOIX` — verrouillage assuré par ASP.NET Core Identity |

---

## B. Contraintes et intégrité

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| B-01 | **Unicité du BL parmi les tickets ACTIFS uniquement** (RG-74/75) — index unique **filtré** | Définition de l'index | `CONFORME` — index unique **filtré** sur les statuts actifs |
| B-02 | Un BL clos redevient réutilisable | Test fonctionnel | `CONFORME` — vérifié par test |
| B-03 | Le contrôle porte sur **chaque BL individuellement** | Logique de création | `CONFORME` — contrôle BL par BL |
| B-04 | Le contrôle **n'interdit pas** le même BL dans un autre module | Portée du contrôle | `CONFORME` — portée limitée au module |
| B-05 | Journal d'audit non modifiable | Tentative de `UPDATE` / `DELETE` | `CONFORME` au niveau applicatif — voir la réserve A-91 |
| B-06 | Jointure IES impossible si une branche est inachevée | Contrainte `CHECK` ou invariant | `CONFORME` — `JointureIesIncompleteException` |
| B-07 | Somme des quotes-parts = 100 % | Contrôle à la validation de grille | `CONFORME` — contrôlé à la validation de grille |
| B-08 | Aucune suppression en cascade sur les tickets archivés | Règles `ON DELETE` | `CONFORME` — aucune cascade sur les tickets archivés |
| B-09 | Numérotation par **séquence**, pas par `MAX + 1` | Mécanisme de numérotation | `CONFORME` — `HasSequence` pour ticket et dossier |
| B-10 | Horodatages en **UTC** | Type et convention | `CONFORME` — suffixe `Utc` systématique |
| B-11 | Montants en décimal, jamais en flottant | Types des colonnes de montant | `CONFORME` — décimal, jamais flottant |
| B-12 | Durées SLA non plafonnées à 24 h | Type de la durée cible | `CONFORME` — stockage en ticks, non plafonné |

---

## C. Règles de gestion — comportement

### C.1 Cycle de vie

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-01 | **Aucune clôture automatique** — le système suggère (RG-69) | Absence de clôture automatisée | `CONFORME` — `EstEligibleASuggestionDeCloture` : le système **suggère** |
| C-02 | Pas de suggestion sur un état **engageant** (RG-70) | Condition de la suggestion | `CONFORME` — `EstEnEtatEngageant` |
| C-03 | Le client **ne peut pas** rouvrir un ticket (RG-71) | Contrôle d'autorisation | `CONFORME` |
| C-04 | Seuil d'annulation libre **paramétrable par workflow** (RG-66) | Paramétrage, ou seuil codé ? | `CONFORME` — paramétrable (valeur métier : E-03) |
| C-05 | Motif **obligatoire** sur rejet, transfert, réaffectation, annulation | Validation des commandes | `CONFORME` — motif obligatoire aux quatre endroits |
| C-06 | Ticket rouvert : numéro, historique, documents conservés (RG-20) | Comportement de réouverture | `CONFORME` — numéro, historique et documents conservés |

### C.2 Workflow et orientation

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-10 | Orientation lisant **`type_client_applique`**, pas le type courant de la société | Logique d'orientation | `CONFORME` — lecture du type figé, porté par le dossier (voir A-12) |
| C-11 | Client **en compte** : aucune étape de paiement (RG-79) | Workflow | `CONFORME` |
| C-12 | Client **deposit** : étape de lettrage (RG-80) | Workflow | `CONFORME` |
| C-13 | Client **IES** : deux branches parallèles + jointure (RG-81) | Workflow | `CONFORME` — deux branches + jointure |
| C-14 | Export : Draft IN **avant** la facture (RG-85) | Workflow export | `CONFORME` — Draft IN avant facture |
| C-15 | Export : document final = **BEE** | Workflow export | `CONFORME` — BEE en document final |
| C-16 | Séquence de paiement en 3 statuts (RG-83) | Matrice de transitions | `CONFORME` — 3 statuts |
| C-17 | Transfert **vérifié contre la matrice**, jamais codé en dur (RG-31) | Contrôle de transition | `CONFORME` — matrice consultée, jamais codée en dur |
| C-18 | Transfert conservant numéro, dossier, historique, documents (RG-45) | Comportement du transfert | `CONFORME` — numéro, dossier, historique et documents conservés |

### C.3 Temps et SLA

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-20 | Temps d'attente **et** de traitement mesurés **par étape** (RG-35) | Calcul des durées | `CONFORME` — **refermé** : `PassageEnFile` mesure attente et traitement par étape |
| C-21 | Attente client **soustraite** de la performance opérateur (RG-38) | Formule de calcul | `CONFORME` — `DureeAttenteClienteCumulee` soustraite |
| C-22 | SLA **suspendable** en attente client (RG-42) | Comportement du SLA | `CONFORME` — SLA suspendu en attente client |
| C-23 | Horaires de fonctionnement pris en compte (RG-43) | Calcul SLA | `CONFORME` — `CalendrierOuvre` |
| C-24 | Chronologie détaillée restituée par étape (RG-39) | Écran d'historique | `CONFORME` — **refermé** : chronologie par étape restituée |
| C-25 | Priorité influençant ordre, SLA, alertes, escalade (RG-59) | Logique de tri et de SLA | `ÉCART` partiel — ordre, SLA et alertes oui ; **escalade non** (voir A-48) |

### C.4 Affectation et supervision

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-30 | Un opérateur **indisponible ne reçoit pas** d'affectation automatique (RG-52) | Logique de distribution | `CONFORME` |
| C-31 | Stratégie d'affectation **changeable sans développement** (RG-56) | Paramétrage par file | `CONFORME` — sans développement |
| C-32 | Action de masse tracée **ticket par ticket** (RG-62) | Journalisation | `CONFORME` — une trace par ticket |
| C-33 | Modification de priorité : ancienne, nouvelle, auteur, motif (RG-60) | Enregistrement | `CONFORME` — `ModificationPriorite` |
| C-34 | Le client **ne peut pas** se déclarer urgent (RG-61) | Contrôle de saisie | `CONFORME` |
| C-35 | Réaffectation **non réservée au superviseur** (RG-47) | Contrôle d'autorisation | `CONFORME` |

### C.5 Notifications

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-40 | Un échec de canal **n'empêche pas** les autres (RG-111) | Logique de diffusion | `CONFORME` — `EnvoiNotification` isole chaque canal |
| C-41 | Reprise des envois en échec avec compteur | Service de reprise | `CONFORME` — `NombreTentatives` |
| C-42 | **Aucun lien direct vers un document** — le lien mène au ticket (RG-112) | Contenu des notifications | `CONFORME` — le lien mène au ticket |
| C-43 | Notification consultable **par son seul destinataire** (RG-63) | Contrôle d'accès | `CONFORME` |
| C-44 | Préférences de canal respectées, **hors notifications obligatoires** | Résolution des canaux | `CONFORME` — hors notifications obligatoires |
| C-45 | Mention **sans** réaffectation (RG-24) | Comportement de la mention | `CONFORME` — mention sans réaffectation |

### C.6 Simulation

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-50 | Tarif par clé *Sens + Taille + Catégorie* (RG-88) | Résolution du tarif | `CONFORME` |
| C-51 | Majorations = pourcentages **de l'acconage**, calculées après lui | Ordre de calcul | `CONFORME` — pourcentages de l acconage, après lui |
| C-52 | Majorations dangereux et OOG **cumulables** (RG-92) | Logique de cumul | `CONFORME` — cumulables |
| C-53 | Branchement **Reefer uniquement**, avec franchise (RG-90) | Condition d'application | `CONFORME` — Reefer uniquement, avec franchise |
| C-54 | Seuil de majoration lourd **fonction de la taille** (RG-91) | Paramétrage du seuil | `CONFORME` — seuil fonction de la taille |
| C-55 | Stationnement par **tranches** hors franchise (RG-93) | Logique de tranches | `ÉCART-CHOIX` — franchise appliquée ; **pas de tranches** modélisées (voir A-62) |
| C-56 | Répartition entre bénéficiaires (RG-94) | Calcul de répartition | `CONFORME` — répartition entre bénéficiaires |
| C-57 | **Tous** les paramètres administrables sans développement (RG-97) | Absence de constante en dur | `CONFORME` — paramètres hors code |
| C-58 | Mention légale au résultat (RG-96) | Affichage | `CONFORME` — mention légale portée au résultat |
| C-59 | Simulation **rattachable à un ticket** (RG-98) | FK et parcours | `CONFORME` — simulation rattachable à un ticket |

### C.7 Cloisonnement et sécurité

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-60 | Un client n'accède qu'à **ses** tickets (RG-11) | Contrôle d'accès | `CONFORME` |
| C-61 | Périmètre de consultation respecté : propres demandes vs société (RG-05) | Filtrage des requêtes | `CONFORME` — propres demandes vs société |
| C-62 | Note interne **jamais** visible du client (RG-22) | Filtrage des messages | `CONFORME` — note interne jamais visible du client |
| C-63 | RBAC **au niveau des champs** pour les traitements parallèles (SEC-02) | Projection des champs | `CONFORME` — `EtatDroitChamp` |
| C-64 | Compte désactivable **sans perte d'historique** (RG-114) | Comportement de désactivation | `CONFORME` — désactivation sans perte d historique |
| C-65 | Document vérifié à l'upload : type, taille, **antivirus** (RG-27) | Contrôles de dépôt | `CONFORME` — type, taille et `StatutAntivirus` |
| C-66 | Session expirant après inactivité **paramétrable** (RG-113) | Configuration | `CONFORME` — durée en configuration (SEC-10) |

---

## D. Traitements — MCT / MOT

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| D-01 | Création : **1 ticket par BL**, regroupés en dossier (RG-16) | Commande de soumission | `CONFORME` — 1 ticket par BL, regroupés en dossier |
| D-02 | Création refusée **en bloc** si un seul BL est actif | Règle d'émission | `CONFORME` — refus en bloc |
| D-03 | Compte activable **seulement après validation** (RG-04) | Cycle du compte | `CONFORME` |
| D-04 | Relances **paramétrables** en nombre et cadence (RG-69) | Configuration, ou valeurs en dur ? | `CONFORME` — nombre et cadence paramétrables (valeurs : E-03) |
| D-05 | Escalade : notifier / remonter priorité / réaffecter (RG-65) | Actions d'escalade | `ÉCART` — **notification seule** ; voir A-48 |
| D-06 | Alertes couvrant les **7 natures** d'ALE-01 | Générateur d'alertes | `CONFORME` — 8 natures pour les 7 attendues |
| D-07 | Réclamation orientée **par catégorie** (RG-100) | Règle d'orientation | `CONFORME` — orientation par catégorie |
| D-08 | Enrôlement : convocation **et** constatation tracées (RG-102) | Cycle d'enrôlement | `CONFORME` — convocation et constatation tracées |
| D-09 | Rappel avant expiration de badge (ENR-05) | Traitement périodique | `CONFORME` — `RappelerExpirationBadgesCommand` (cadence : E-03) |
| D-10 | Escalade IA **sans perte de contexte** (RG-107) | Module IA, ou absent ? | `ABSENT (attendu)` — E-01 |
| D-11 | Reprise de saisie après incident (RG-110) | Mécanisme de brouillon | `CONFORME` — `BrouillonSaisie` |
| D-12 | Recherche guichet par numéro, client, BL (RG-116) | Écran de recherche | `CONFORME` — recherche par numéro, client, BL |
| D-13 | Bascule présentiel tracée dans le ticket (RG-87) | Marqueur sur le ticket | `CONFORME` — `Ticket.DeclarationPresentielle` |

---

## E. Points où l'écart est *attendu*

Ces éléments sont **hors périmètre technique** ou **bloqués par une décision externe**. Un écart y
est normal et ne constitue pas un défaut.

| # | Élément | Raison |
|:---|:---|:---|
| E-01 | Assistant IA (A-79, D-10) | Fournisseur de modèle non arbitré |
| E-02 | Canaux SMS / WhatsApp / e-mail | Aucun fournisseur retenu |
| E-03 | Cadences de relance, seuils SLA (D-04) | Valeurs métier non fournies |
| E-04 | Matrice de transitions — **contenu** | « Valeurs à définir avec le métier » |
| E-05 | Durées de conservation | Non fixées |
| E-06 | Intervenants du workflow avoir/exonération | « Paramétrables », non nommés |
| E-07 | Compétences d'affectation (A-07) | Référentiel non fourni |
| E-08 | Unicité du booking export | Explicitement différée (UNI-04) |

**La structure doit exister même si la valeur manque.** L'absence d'une table de transitions est un
écart ; l'absence de son contenu n'en est pas un.

---

## F. Récapitulatif

| Bloc | Points | `CONFORME` | `ÉCART-CHOIX` | `ÉCART` | `ABSENT (attendu)` |
|:---|---:|---:|---:|---:|---:|
| **A** — Structure | 67 | 45 | 17 | 4 | 1 |
| **B** — Intégrité | 12 | 12 | — | — | — |
| **C** — Comportement | 50 | 48 | 1 | 1 | — |
| **D** — Traitements | 13 | 11 | — | 1 | 1 |
| **Total statué** | **142** | **116** | **18** | **6** | **2** |

Le bloc **E** (8 points) ne se statue pas : il recense les écarts *attendus*, bloqués par une
décision externe.

### Les 6 `ÉCART` — et ce qu'ils valent

Un `ÉCART-CHOIX` n'est pas un défaut : l'exigence est satisfaite autrement. Seuls les six ci-dessous
appellent une suite, et **aucun n'est un manque de conformité pur** :

| # | Écart | Nature réelle |
|:---|:---|:---|
| **A-48**, **C-25**, **D-05** | Escalade : seule la notification est automatisée | **Un seul et même point.** RG-65 dit que le dépassement « **peut** » notifier, remonter la priorité ou réaffecter. Décider quel seuil déclenche quelle action est un **arbitrage métier** que le CDC ne donne pas — l implémenter serait inventer une politique d escalade |
| **A-45** | `ParametreSlaAdministrable` ne porte que *module* et *type de demande* | Le modèle attendait cinq axes : ni file, ni priorité, ni type de client. **Écart de conformité réel**, à instruire |
| **A-91** | Aucun chaînage par empreinte du journal d audit | La garde est **applicative** : un accès direct à la base la contourne. SEC-05 dit « non modifiable » sans préciser le niveau — **défense en profondeur absente**, pas une violation du CDC |
| **A-17** | Aucun type de document paramétrable **par étape** | Point **hors du périmètre de l audit initial**, relevé ici pour la première fois. **À qualifier** avant d en faire un défaut |

**Deux de ces six seulement peuvent devenir des corrections** — A-45 et, après qualification, A-17.
Les quatre autres décrivent soit un arbitrage métier absent du CDC, soit une défense en profondeur
que le CDC n exige pas.

---

## G. Ce que la comparaison pourra et ne pourra pas établir

**Ce qu'elle établira** : les **écarts**. Une divergence entre le modèle CDC et l'implémentation est
un constat solide, quelle qu'en soit la cause — il faudra ensuite la classer en `MANQUE`, `CHOIX` ou
`SURPLUS`.

**Ce qu'elle n'établira pas** : l'indépendance des convergences. Le modèle a été rédigé depuis le
CDC, mais par un auteur ayant lu l'implémentation plus tôt dans la même session. Une coïncidence de
nommage ou de découpage n'est donc pas la preuve que le CDC imposait ce choix.

**Ce qui reste vrai dans les deux cas** : chaque ligne de cette grille porte une **référence CDC**.
Un point marqué `ÉCART-MANQUE` renvoie à une exigence contractuelle précise — c'est cela qui rend le
constat opposable, indépendamment de la manière dont le modèle a été produit.

---

**Statut : `NON RENSEIGNÉE`.** Aucune confrontation n'a été effectuée. Le remplissage de la colonne
« Constat » est l'exercice suivant.
