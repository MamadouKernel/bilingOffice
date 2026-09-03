# Grille de comparaison — modèle CDC vs implémentation

> **Document** : `docs/86-grille-comparaison-cdc-implementation.md`
> **Établi le** : 2026-09-03
> **Référence attendue** : `docs/85-analyse-merise.md`, dérivée du seul CDC
> **Statut** : **`NON RENSEIGNÉE`** — aucune colonne « Constat » n'est remplie. La confrontation
> avec le code n'a pas été effectuée.

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
| A-01 | Entité `SOCIETE_CLIENTE` avec type ∈ {normal, en compte, deposit, IES} | Table des sociétés, colonne de profil | |
| A-02 | Entité `UTILISATEUR` distinguant client / agent / transporteur | Table des utilisateurs, colonne de nature | |
| A-03 | `RATTACHEMENT` **porteur** de périmètre, droits et état — pas une simple jointure | Table de rattachement : colonnes propres ? | |
| A-04 | Un utilisateur peut n'avoir **aucun** rattachement (transporteur) | Nullabilité / cardinalité `0,N` | |
| A-05 | `HABILITATION` bornable à une file | Colonne `file_id` nullable | |
| A-06 | `PIECE_EXIGEE` paramétrable à l'ouverture de compte | Table de pièces exigées | |
| A-07 | `COMPETENCE_OPERATEUR` pour l'affectation par compétence | Table de compétences | |
| A-08 | État de société : soumise / validée / rejetée | Colonne d'état | |

### A.2 Socle ticket

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-10 | `TICKET` avec numéro **unique** | Contrainte d'unicité sur le numéro | |
| A-11 | `DOSSIER` regroupant 1..N tickets ; ticket → dossier **optionnel** | FK nullable vers dossier | |
| A-12 | **`type_client_applique` figé sur le ticket** (RG-07) | Colonne sur le ticket, distincte de celle de la société | |
| A-13 | `LIAISON_TICKET` réflexive, inter-modules | Table de liaison | |
| A-14 | `MESSAGE` avec nature externe / interne | Colonne de nature | |
| A-15 | `MENTION` distincte du message | Table de mentions | |
| A-16 | `DOCUMENT` avec type, version, auteur, horodatage | Colonnes correspondantes | |
| A-17 | `TYPE_DOCUMENT` paramétrable **par étape** | FK vers étape / file | |
| A-18 | `EVENEMENT_HISTORIQUE` par ticket | Table d'historique | |
| A-19 | `BROUILLON` pour la reprise de saisie (ERR-01) | Table de brouillons | |
| A-20 | `DEMANDE_ANNULATION` distincte de l'annulation directe | Table dédiée | |
| A-21 | `STATUT` en **référentiel paramétrable**, non en énumération codée | Table de statuts, ou enum ? | |

### A.3 Workflow et mesure du temps

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-30 | **`PASSAGE_EN_FILE`** : une occurrence **par passage**, 4 horodatages | Table dédiée, ou horodatages sur le ticket ? | |
| A-31 | `PERIODE_ATTENTE_CLIENT` **multiple** par ticket | Table dédiée, cardinalité `0,N` | |
| A-32 | `TRANSFERT` et `REAFFECTATION` **distincts** | Deux tables, ou une seule ? | |
| A-33 | `MOTIF_TRANSFERT` en référentiel | Table de motifs, ou texte libre ? | |
| A-34 | `TRANSITION_AUTORISEE` — matrice paramétrable, éventuellement par profil | Table de transitions + colonne profil | |
| A-35 | `STRATEGIE_AFFECTATION` par file (6 modes) | Colonne / table de stratégie | |
| A-36 | `HORAIRE_FONCTIONNEMENT` pour le calcul SLA | Table d'horaires | |
| A-37 | `RELANCE` numérotée par ticket | Table de relances | |

### A.4 Supervision et SLA

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-40 | `PRIORITE` en référentiel paramétrable | Table de priorités | |
| A-41 | **`HISTORIQUE_PRIORITE` avec ancienne ET nouvelle valeur + motif** (PRI-03) | Table dédiée, colonnes des deux valeurs | |
| A-42 | `DISPONIBILITE_OPERATEUR` : disponible / indisponible / absent | Table ou colonne de disponibilité | |
| A-43 | `PERIMETRE_SUPERVISION` — un superviseur ne voit pas toutes les files | Table de périmètre | |
| A-44 | **`ACTION_DE_MASSE` reliée aux événements individuels** (MAS-02) | Table d'action de masse + FK depuis l'historique | |
| A-45 | `SLA` paramétrable par module, file, priorité, type de demande, type de client | Colonnes du SLA | |
| A-46 | `SEUIL_SLA` **multiple** par SLA (info, alerte, dépassement, escalade) | Table de seuils, cardinalité `1,N` | |
| A-47 | `ALERTE` couvrant les **7 natures** d'ALE-01 | Énumération des natures | |
| A-48 | `REGLE_ESCALADE` : notifier / remonter priorité / réaffecter | Table de règles | |

### A.5 Billing Office

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-50 | `BL` avec état d'activité (actif / clos) | Table BL, colonne d'état | |
| A-51 | `BOOKING` symétrique | Table booking | |
| A-52 | `FACTURE` rattachée au ticket | Table facture | |
| A-53 | `PREUVE_PAIEMENT` avec statut de validation | Table + colonne de statut | |
| A-54 | `LETTRAGE` avec référence IRIS | Table de lettrage | |
| A-55 | **`SUIVI_BRANCHE_IES` à deux dimensions + date de jointure** (RG-81) | Table dédiée, deux booléens | |
| A-56 | `DEMANDE_AVOIR_EXONERATION` liable à un ticket de parcours | Table + FK de liaison | |

### A.6 Tarification

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-60 | `GRILLE_TARIFAIRE` **versionnée avec date d'effet** (SIM-07) | Colonnes version + date d'effet | |
| A-61 | `LIGNE_TARIF` avec clé *(grille, sens, taille, catégorie)* | Contrainte d'unicité composite | |
| A-62 | `TRANCHE_STATIONNEMENT` paramétrable | Table de tranches | |
| A-63 | `QUOTE_PART` — nombre de bénéficiaires paramétrable | Table de quotes-parts | |
| A-64 | `PARAMETRE_CALCUL` — taux, seuils, franchises **hors code** | Table de paramètres | |
| A-65 | **`SIMULATION` référençant la version de grille appliquée** (RG-95) | FK vers grille | |
| A-66 | `CONTENEUR_SIMULE` avec les 7 caractéristiques du CDC | Colonnes | |
| A-67 | `POSTE_CALCULE` conservant le **détail par poste** | Table de postes | |

### A.7 Notifications et périphérie

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-70 | **`ENVOI` séparé de `NOTIFICATION`** (RG-111) | Deux tables, `1,N` | |
| A-71 | `ENVOI` avec statut **et compteur de tentatives** par canal | Colonnes | |
| A-72 | `MODELE_MESSAGE` paramétrable | Table de modèles | |
| A-73 | `REGLE_DIFFUSION` par événement **et** par canal | Table de règles | |
| A-74 | `PREFERENCE_CANAL` par utilisateur | Table de préférences | |
| A-75 | `CATEGORIE_RECLAMATION` avec file cible | Table + FK | |
| A-76 | `DOSSIER_ENROLEMENT`, `CONVOCATION`, `BADGE` | Trois tables | |
| A-77 | `BADGE` avec **date d'expiration** (ENR-05) | Colonne | |
| A-78 | `RENDEZ_VOUS` rattachable au ticket **ou** au dossier | Deux FK nullables | |
| A-79 | `CONVERSATION_IA` rattachée au ticket (IA-03) | Table, ou module absent ? | |
| A-80 | `ENQUETE_SATISFACTION` (REC-06) | Table | |

### A.8 Sécurité

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| A-90 | `ENTREE_AUDIT` **append-only** | Contrainte / déclencheur / garde applicative | |
| A-91 | Chaînage par empreinte rendant l'altération détectable | Colonne d'empreinte | |
| A-92 | `SESSION` avec expiration paramétrable | Table ou mécanisme équivalent | |
| A-93 | `TENTATIVE_CONNEXION` pour le verrouillage | Table ou mécanisme équivalent | |

---

## B. Contraintes et intégrité

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| B-01 | **Unicité du BL parmi les tickets ACTIFS uniquement** (RG-74/75) — index unique **filtré** | Définition de l'index | |
| B-02 | Un BL clos redevient réutilisable | Test fonctionnel | |
| B-03 | Le contrôle porte sur **chaque BL individuellement** | Logique de création | |
| B-04 | Le contrôle **n'interdit pas** le même BL dans un autre module | Portée du contrôle | |
| B-05 | Journal d'audit non modifiable | Tentative de `UPDATE` / `DELETE` | |
| B-06 | Jointure IES impossible si une branche est inachevée | Contrainte `CHECK` ou invariant | |
| B-07 | Somme des quotes-parts = 100 % | Contrôle à la validation de grille | |
| B-08 | Aucune suppression en cascade sur les tickets archivés | Règles `ON DELETE` | |
| B-09 | Numérotation par **séquence**, pas par `MAX + 1` | Mécanisme de numérotation | |
| B-10 | Horodatages en **UTC** | Type et convention | |
| B-11 | Montants en décimal, jamais en flottant | Types des colonnes de montant | |
| B-12 | Durées SLA non plafonnées à 24 h | Type de la durée cible | |

---

## C. Règles de gestion — comportement

### C.1 Cycle de vie

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-01 | **Aucune clôture automatique** — le système suggère (RG-69) | Absence de clôture automatisée | |
| C-02 | Pas de suggestion sur un état **engageant** (RG-70) | Condition de la suggestion | |
| C-03 | Le client **ne peut pas** rouvrir un ticket (RG-71) | Contrôle d'autorisation | |
| C-04 | Seuil d'annulation libre **paramétrable par workflow** (RG-66) | Paramétrage, ou seuil codé ? | |
| C-05 | Motif **obligatoire** sur rejet, transfert, réaffectation, annulation | Validation des commandes | |
| C-06 | Ticket rouvert : numéro, historique, documents conservés (RG-20) | Comportement de réouverture | |

### C.2 Workflow et orientation

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-10 | Orientation lisant **`type_client_applique`**, pas le type courant de la société | Logique d'orientation | |
| C-11 | Client **en compte** : aucune étape de paiement (RG-79) | Workflow | |
| C-12 | Client **deposit** : étape de lettrage (RG-80) | Workflow | |
| C-13 | Client **IES** : deux branches parallèles + jointure (RG-81) | Workflow | |
| C-14 | Export : Draft IN **avant** la facture (RG-85) | Workflow export | |
| C-15 | Export : document final = **BEE** | Workflow export | |
| C-16 | Séquence de paiement en 3 statuts (RG-83) | Matrice de transitions | |
| C-17 | Transfert **vérifié contre la matrice**, jamais codé en dur (RG-31) | Contrôle de transition | |
| C-18 | Transfert conservant numéro, dossier, historique, documents (RG-45) | Comportement du transfert | |

### C.3 Temps et SLA

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-20 | Temps d'attente **et** de traitement mesurés **par étape** (RG-35) | Calcul des durées | |
| C-21 | Attente client **soustraite** de la performance opérateur (RG-38) | Formule de calcul | |
| C-22 | SLA **suspendable** en attente client (RG-42) | Comportement du SLA | |
| C-23 | Horaires de fonctionnement pris en compte (RG-43) | Calcul SLA | |
| C-24 | Chronologie détaillée restituée par étape (RG-39) | Écran d'historique | |
| C-25 | Priorité influençant ordre, SLA, alertes, escalade (RG-59) | Logique de tri et de SLA | |

### C.4 Affectation et supervision

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-30 | Un opérateur **indisponible ne reçoit pas** d'affectation automatique (RG-52) | Logique de distribution | |
| C-31 | Stratégie d'affectation **changeable sans développement** (RG-56) | Paramétrage par file | |
| C-32 | Action de masse tracée **ticket par ticket** (RG-62) | Journalisation | |
| C-33 | Modification de priorité : ancienne, nouvelle, auteur, motif (RG-60) | Enregistrement | |
| C-34 | Le client **ne peut pas** se déclarer urgent (RG-61) | Contrôle de saisie | |
| C-35 | Réaffectation **non réservée au superviseur** (RG-47) | Contrôle d'autorisation | |

### C.5 Notifications

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-40 | Un échec de canal **n'empêche pas** les autres (RG-111) | Logique de diffusion | |
| C-41 | Reprise des envois en échec avec compteur | Service de reprise | |
| C-42 | **Aucun lien direct vers un document** — le lien mène au ticket (RG-112) | Contenu des notifications | |
| C-43 | Notification consultable **par son seul destinataire** (RG-63) | Contrôle d'accès | |
| C-44 | Préférences de canal respectées, **hors notifications obligatoires** | Résolution des canaux | |
| C-45 | Mention **sans** réaffectation (RG-24) | Comportement de la mention | |

### C.6 Simulation

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-50 | Tarif par clé *Sens + Taille + Catégorie* (RG-88) | Résolution du tarif | |
| C-51 | Majorations = pourcentages **de l'acconage**, calculées après lui | Ordre de calcul | |
| C-52 | Majorations dangereux et OOG **cumulables** (RG-92) | Logique de cumul | |
| C-53 | Branchement **Reefer uniquement**, avec franchise (RG-90) | Condition d'application | |
| C-54 | Seuil de majoration lourd **fonction de la taille** (RG-91) | Paramétrage du seuil | |
| C-55 | Stationnement par **tranches** hors franchise (RG-93) | Logique de tranches | |
| C-56 | Répartition entre bénéficiaires (RG-94) | Calcul de répartition | |
| C-57 | **Tous** les paramètres administrables sans développement (RG-97) | Absence de constante en dur | |
| C-58 | Mention légale au résultat (RG-96) | Affichage | |
| C-59 | Simulation **rattachable à un ticket** (RG-98) | FK et parcours | |

### C.7 Cloisonnement et sécurité

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| C-60 | Un client n'accède qu'à **ses** tickets (RG-11) | Contrôle d'accès |
| C-61 | Périmètre de consultation respecté : propres demandes vs société (RG-05) | Filtrage des requêtes | |
| C-62 | Note interne **jamais** visible du client (RG-22) | Filtrage des messages | |
| C-63 | RBAC **au niveau des champs** pour les traitements parallèles (SEC-02) | Projection des champs | |
| C-64 | Compte désactivable **sans perte d'historique** (RG-114) | Comportement de désactivation | |
| C-65 | Document vérifié à l'upload : type, taille, **antivirus** (RG-27) | Contrôles de dépôt | |
| C-66 | Session expirant après inactivité **paramétrable** (RG-113) | Configuration | |

---

## D. Traitements — MCT / MOT

| # | Attendu | Où vérifier | Constat |
|:---|:---|:---|:---|
| D-01 | Création : **1 ticket par BL**, regroupés en dossier (RG-16) | Commande de soumission | |
| D-02 | Création refusée **en bloc** si un seul BL est actif | Règle d'émission | |
| D-03 | Compte activable **seulement après validation** (RG-04) | Cycle du compte | |
| D-04 | Relances **paramétrables** en nombre et cadence (RG-69) | Configuration, ou valeurs en dur ? | |
| D-05 | Escalade : notifier / remonter priorité / réaffecter (RG-65) | Actions d'escalade | |
| D-06 | Alertes couvrant les **7 natures** d'ALE-01 | Générateur d'alertes | |
| D-07 | Réclamation orientée **par catégorie** (RG-100) | Règle d'orientation | |
| D-08 | Enrôlement : convocation **et** constatation tracées (RG-102) | Cycle d'enrôlement | |
| D-09 | Rappel avant expiration de badge (ENR-05) | Traitement périodique | |
| D-10 | Escalade IA **sans perte de contexte** (RG-107) | Module IA, ou absent ? | |
| D-11 | Reprise de saisie après incident (RG-110) | Mécanisme de brouillon | |
| D-12 | Recherche guichet par numéro, client, BL (RG-116) | Écran de recherche | |
| D-13 | Bascule présentiel tracée dans le ticket (RG-87) | Marqueur sur le ticket | |

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

| Bloc | Points | Objet |
|:---|---:|:---|
| **A** — Structure | 63 | Entités et attributs porteurs de règles |
| **B** — Intégrité | 12 | Contraintes et conventions physiques |
| **C** — Comportement | 45 | Règles de gestion applicatives |
| **D** — Traitements | 13 | MCT / MOT |
| **E** — Écarts attendus | 8 | Hors périmètre ou bloqués |
| **Total à confronter** | **133** | |

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
