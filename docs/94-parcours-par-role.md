# Guide complet des rôles et parcours utilisateur

> **Public** : équipes métier, administrateurs, support et nouveaux utilisateurs.
> **But** : expliquer qui utilise chaque espace, comment accéder au compte, le parcours normal de
> bout en bout et les limites d’accès.
> **Date de révision** : 2026-10-02.
> Cette version s’adresse d’abord aux utilisateurs métier. Les mots de menu sont repris tels qu’ils
> apparaissent dans l’application; les détails techniques sont regroupés à la fin pour l’équipe de
> maintenance.

## Pour commencer

1. **Choisissez votre profil** dans les sections ci-dessous. Si vous ne savez pas lequel choisir,
   demandez à votre responsable ou au contact CIT.
2. **Connectez-vous avec votre propre compte.** Ne partagez pas votre mot de passe ou votre code de
   sécurité avec un collègue.
3. **Suivez le menu et l’aide « Aide »** en haut de l’écran. L’aide explique à quoi sert la page et
   quoi faire ensuite. Les menus affichent seulement les actions prévues pour votre compte.

### Quelques mots utilisés dans ce guide

| Mot | Explication simple |
|---|---|
| Dossier | L’ensemble des informations, pièces et échanges liés à une demande. |
| Ticket | L’élément de travail suivi par l’équipe CIT à l’intérieur du dossier. |
| File | La liste de dossiers qu’une équipe doit traiter, comme une bannette de courrier. |
| Complément | Une information ou une pièce supplémentaire demandée pour continuer le traitement. |
| Habilitation | L’autorisation donnée à une personne pour effectuer certaines tâches. |
| Supervision | La vue qui aide un responsable à suivre la charge et les délais de son équipe. |

## 1. Comprendre les profils avant d’utiliser le guide

L’application ne donne pas les droits à partir d’un intitulé de poste libre. Un compte peut voir
plusieurs espaces. Ses accès effectifs dépendent de son rattachement à une société et des
habilitations accordées à son compte, éventuellement limitées à une file de traitement. Les espaces
« Agent », « Supervision », « Administration CIT » et « Support N1 » organisent le menu ; ils ne
remplacent jamais les contrôles serveur.

| Profil métier présenté ici | Création / rattachement | Principe d’accès |
|---|---|---|
| Visiteur | Aucun compte | Accès aux pages publiques, dont la simulation tarifaire indicative |
| Client d’entreprise | Demande de société approuvée par CIT | Accès aux opérations de sa société selon le droit de soumission |
| Administrateur de société | Premier compte de la société approuvée ou désigné par celle-ci | Opérations société et gestion de ses collaborateurs |
| Collaborateur de société | Invitation par l’administrateur de sa société | Opérations permises dans le périmètre de sa société |
| Transporteur / chauffeur | Inscription publique, sans société | Dépôt et suivi de ses dossiers d’enrôlement |
| Agent métier CIT | Compte interne créé par un administrateur | Uniquement les files et actions correspondant à ses habilitations |
| Superviseur | Compte interne avec droits de supervision et périmètres configurés | Observation des opérations et actions d’administration explicitement accordées |
| Administration CIT | Compte interne avec droits d’administration | Comptes, référentiels ou paramètres correspondant à ses permissions |
| Support informatique N1 / N2 | Compte interne avec droits support | Incidents techniques; périmètre d’intervention distinct des dossiers métier |

**Les profils peuvent se cumuler.** Par exemple, un administrateur CIT peut aussi avoir accès à la
supervision et au support. Un agent ne reçoit pas automatiquement tous les droits d’un autre agent.

## 2. Connexion, navigation et aide communes

1. Ouvrir `/comptes/connexion` et saisir l’adresse e-mail et le mot de passe du compte.
2. Terminer la vérification à deux facteurs si elle est activée ou demandée.
3. L’application calcule les espaces accessibles et ouvre la première page autorisée. Les liens
   absents du menu correspondent à des fonctions sans habilitation pour ce compte.
4. En cas de refus, vérifier que l’on utilise le bon compte, la bonne société et, pour le personnel
   CIT, la bonne habilitation et la bonne file. Une URL directe ne contourne pas le contrôle.

Tous les comptes disposent également, selon leur état de compte, de leurs préférences de
notifications, de la sécurité 2FA, du centre d’aide et des fonctions de signalement/suivi d’incident
technique. Une notification n’est visible que par son destinataire.

## 3. Visiteur sans compte

**Accès** : page d’accueil `/`.

**Parcours possibles** :

- Simuler une facture proforma sur `/simulation/proforma`, saisir dates, sens, conteneurs et
  informations de dossier, vérifier le calcul, puis télécharger la facture PDF. Le document est
  indicatif et non contractuel. L’historique et l’enregistrement nécessitent un compte admissible.
- Créer une demande de compte société sur `/comptes/soumettre`, puis attendre son instruction CIT.
- S’inscrire comme transporteur/chauffeur sur `/comptes/inscription-transporteur`.
- Se connecter à un compte existant.

## 4. Client d’entreprise

### Ouverture du compte

1. Le représentant soumet le dossier société sur `/comptes/soumettre` avec les données et pièces
   requises.
2. L’équipe CIT habilitée instruit la demande depuis `/comptes/valider` : elle vérifie les éléments,
   demande au besoin une correction, puis approuve ou rejette avec le motif prévu.
3. Après approbation et rattachement, le premier utilisateur dispose de l’espace société. L’accès
   aux opérations est lié au rattachement actif, au droit de soumission et au type de client.

### Parcours d’utilisation

1. Ouvrir le tableau de bord `/client/dashboard` et consulter l’état des dossiers.
2. Déposer la demande voulue : IMPORT (BL), EXPORT (Booking), Avoir/Exonération ou réclamation.
   Fournir les références et pièces justificatives demandées avant soumission.
3. Pour un besoin d’accès opérationnel chauffeur, passer par un compte transporteur séparé; ce
   compte n’est pas rattaché à la société cliente.
4. Suivre les demandes depuis `/client/demandes`, consulter les échanges et notifications, répondre
   aux demandes de complément et déposer les pièces réclamées.
5. Pour un rendez-vous, consulter `/rendez-vous/mes-rendez-vous` et suivre les instructions
   communiquées.
6. Pour une estimation tarifaire, ouvrir `/simulation/proforma`; pour une simulation enregistrée,
   consulter `/simulation/historique` et télécharger le PDF disponible.
7. Pour un problème de service, créer une réclamation puis en suivre le traitement dans
   `/reclamations/mes-reclamations`.

**Limites** : un client voit les dossiers de sa société selon ses droits et la portée de consultation.
Il ne peut pas administrer les paramètres CIT ni instruire les files internes.

## 5. Administrateur de société

Il s’agit d’un utilisateur client auquel le rattachement société confère la gestion de la société.
Il conserve les parcours client décrits ci-dessus et dispose en plus de :

- `/societe/dashboard` : vue d’ensemble des demandes de la société;
- `/societe/collaborateurs` ou `/comptes/collaborateurs` : inviter un collaborateur, suivre son
  invitation, gérer son accès et, si autorisé, la désignation d’un autre administrateur;
- `/comptes/profil` : consulter et gérer les informations modifiables du profil de l’entreprise;
- `/client/demandes?vue=societe` : consulter les demandes de la société plutôt que les seules
  demandes personnelles.

**Parcours collaborateur** : l’administrateur invite l’adresse e-mail professionnelle; la personne
accepte l’invitation et établit son accès, puis se connecte avec son propre compte. Ne jamais
partager un mot de passe entre collègues. L’administrateur ne peut gérer que les utilisateurs de sa
propre société.

## 6. Collaborateur d’entreprise

**Création** : invitation depuis l’espace de gestion des collaborateurs. Il ne s’inscrit pas comme
un nouveau dossier société.

**Parcours** : accepter l’invitation, se connecter, consulter son tableau de bord et agir sur les
dossiers autorisés de la société. Il peut soumettre et suivre les démarches client dans la mesure où
son droit de soumission et sa portée de consultation le permettent.

**Limites** : pas de gestion des collaborateurs, pas de désignation d’administrateur et pas de
gestion du profil société réservée à l’administrateur. Les données restent isolées des autres
sociétés.

## 7. Transporteur ou chauffeur

**Inscription** : `/comptes/inscription-transporteur`. Ce parcours crée un compte opérationnel sans
rattachement à une société. Après connexion, la page utile est `/enrolement/mes-enrolements`.

**Parcours d’enrôlement** :

1. Ouvrir `/enrolement/soumettre` et renseigner les informations demandées par le formulaire en
   vigueur.
2. Joindre les pièces requises (par exemple permis ou carte grise) puis envoyer le dossier.
3. Consulter le statut dans `/enrolement/mes-enrolements`.
4. Répondre à une demande de complément et fournir les pièces manquantes.
5. Suivre la validation et les consignes de remise de badge affichées/communiquées.

**Limites** : aucun accès aux demandes de facturation, aux réclamations client ou aux simulations
réservées à un compte société. L’enrôlement de flotte d’une société (`/enrolement/societe/...`)
appartient au parcours du représentant rattaché à cette société, pas au compte chauffeur.

## 8. Agent métier CIT

Le compte est créé dans `/administration/comptes-agents`. L’administrateur lui attribue des
permissions explicites. Certaines permissions sont bornées à une file : un accès à une file ne donne
pas accès à toutes les autres. L’espace `/agent/dashboard` résume l’activité accessible; la suite
dépend des habilitations.

| Activité, si habilitée | Écran usuel | Travail attendu |
|---|---|---|
| Recherche / reprise au guichet | `/guichet/recherche` | Retrouver un ticket ou dossier, assister l’usager et reprendre le parcours permis |
| Rendez-vous | `/guichet/agenda` | Gérer créneaux et présence des usagers |
| IMPORT – manifeste | `/billing-import/manifeste` | Prendre en charge, contrôler la liasse BL, valider, rejeter ou demander un complément motivé |
| IMPORT – contrôle documentaire | `/billing-import/controle-documentaire` | Vérifier les pièces de l’étape et acheminer la décision |
| EXPORT – back-office | `/billing-export/back-office` | Instruire et coder la demande Booking selon la procédure |
| EXPORT – contrôle documentaire | `/billing-export/controle-documentaire` | Contrôler les pièces EXPORT et traiter les anomalies |
| IES | `/billing-import/ies`, `/billing-export/ies` | Traiter les sous-étapes attribuées; les deux volets peuvent être séparés |
| Facturation | `/billing-import/facturation`, `/billing-export/facturation` | Éditer la facture et déposer le document dans le dossier |
| Caisse | `/billing-import/validation-paiement`, `/billing-export/validation-paiement` | Vérifier la preuve et enregistrer la validation de paiement |
| Comptabilité | `/billing-import/comptabilite-client`, `/billing-export/comptabilite` | Confirmer le lettrage comptable et tracer l’opération |
| Avoir / Exonération | `/avoir-exoneration/file` | Instruire le type de demande pour lequel l’agent est habilité; la décision peut être séparée de l’instruction |
| Réclamations | `/reclamations/file` | Examiner la réclamation, échanger avec le client et répondre/clore selon habilitation |
| Agent Gate | `/enrolement/file-agent-gate` | Vérifier le dossier transporteur, demander complément, valider et suivre la remise du badge |

### Routine de traitement d’un dossier

1. Ouvrir le tableau de bord ou la file correspondant à son habilitation.
2. Prendre en charge un dossier attribué ou disponible dans la file, selon la procédure.
3. Vérifier les données, pièces, doublons et critères métier de l’étape.
4. Choisir une transition autorisée : validation/avancement, demande de complément, rejet motivé,
   transfert ou escalade. Rédiger un motif utile pour l’usager et les collègues.
5. Ajouter les pièces ou notes nécessaires; distinguer les notes internes des messages visibles au
   client.
6. Enregistrer et vérifier que le statut, l’affectation et la notification sont cohérents.

**Bonnes pratiques** : ne pas contourner une transition, ne pas partager de documents hors du
dossier, ne pas consulter une file non attribuée et ne pas utiliser une note interne pour communiquer
avec l’usager. Le SLA peut être suspendu pendant l’attente d’un complément; la réponse du client
relance le traitement.

## 9. Superviseur

Le superviseur dispose d’un périmètre explicite. La consultation de la supervision ne donne pas
automatiquement le droit d’agir sur les dossiers ni d’administrer tous les paramètres.

- `/supervision` : suivre les volumes, charges, retards et alertes dans son périmètre;
- `/administration/disponibilite` : consulter la disponibilité des agents;
- selon les habilitations : configurer stratégie des files, motifs de transfert, priorités, statuts,
  pièces attendues, transitions, règles de priorité automatique et blocages d’annulation;
- selon les habilitations : gérer paramètres SLA/escalade, périmètres de supervision et compétences
  des opérateurs;
- selon habilitation additionnelle : traiter/valider des demandes de compte ou consulter l’audit.

**Routine** : repérer les files sous tension et les dossiers proches/en dépassement de SLA, vérifier
la disponibilité et la compétence des agents, appliquer une action autorisée (réaffectation ou
escalade), puis suivre l’effet dans le tableau de supervision. Les actions en masse sont réservées à
une permission distincte et doivent respecter le périmètre configuré.

## 10. Administration CIT

L’administration CIT est un ensemble d’habilitations; ce n’est pas un accès universel par défaut.
Les écrans visibles dépendent des permissions attribuées.

| Domaine | Écrans indicatifs | Responsabilité |
|---|---|---|
| Demandes de compte | `/comptes/valider` | Instruire, demander correction, approuver ou rejeter le dossier société |
| Comptes et habilitations | `/administration/comptes-agents`, `/administration/comptes-utilisateurs` | Créer/désactiver les comptes internes et accorder le minimum d’habilitations nécessaire |
| Types de clients | `/comptes/modifier-type`, `/administration/types-client` | Gérer le typage et les paramètres correspondants |
| Référentiels | `/administration/referentiels`, `/admin/tarification/referentiels` | Administrer les listes métier et les références de tarification |
| Tarifs | `/admin/tarification` | Publier/administrer les versions de grilles selon dates d’effet |
| Paramètres opérationnels | `/administration/...` | Gérer seulement les domaines explicitement accordés : files, SLA, notifications, catégories, aide, transitions, etc. |
| Traçabilité | `/administration/audit`, `/administration/archives` | Consulter l’audit ou les archives si habilité |

**Routine de gestion des accès** : confirmer la demande et le responsable, créer ou sélectionner le
compte, attribuer seulement les habilitations métier nécessaires (et les files précises), faire
valider l’accès avec l’utilisateur, puis retirer les droits devenus inutiles lors d’un changement de
poste ou d’un départ. L’administration des comptes et l’approbation des sociétés sont des
permissions distinctes.

## 11. Support informatique N1 et N2

### Support N1

Le profil N1 est un paquet de permissions de diagnostic et d’intervention sur les incidents
techniques. Le cockpit `/support/incidents/file` sert au traitement des incidents IT; le guichet et
la supervision peuvent servir à rechercher ou observer, selon les droits. N1 peut recueillir le
symptôme, reproduire le problème, documenter le diagnostic et résoudre ou escalader l’incident.
L’accès de lecture ne confère pas le droit d’instruire les dossiers métier.

### Support N2

Les incidents escaladés dans la file « Support IT — N2 » exigent une permission distincte
(`support.incident.instruire.n2`). Le rôle consiste à approfondir le diagnostic, mettre en œuvre ou
coordonner la correction et consigner la résolution. N2 n’hérite pas automatiquement des droits N1
ni des droits d’administration CIT.

## 12. Parcours transversaux des dossiers métier

Les étapes exactes varient selon le module et les règles configurées. Le modèle commun est :

```text
Usager soumet le dossier
  → contrôles de saisie, pièces et unicité
  → ticket créé dans le dossier et placé dans une file
  → agent habilité prend en charge et instruit
  → [complément demandé → usager répond → dossier retourne en traitement]
  → validation, rejet motivé, transfert ou escalade selon la matrice
  → facturation / paiement / comptabilité lorsque le module le prévoit
  → notification et clôture
```

Les étapes IES peuvent être parallèles; IMPORT, EXPORT, réclamations, enrôlement, avoir et exonération
ont leurs propres files et règles. Les demandes de complément suspendent le SLA lorsque la règle
du module le prévoit. Les transitions non paramétrées sont refusées; une dérogation éventuelle exige
le droit correspondant, un motif et une trace d’audit.

Pour les séquences détaillées de tickets, responsabilités et notifications, consulter
[`108-architecture-workflow-swimlanes.md`](108-architecture-workflow-swimlanes.md). Pour les droits
de recette et les utilisateurs de test, consulter [`62-uat-access-matrix.md`](62-uat-access-matrix.md)
et [`10-comptes-test-cit.md`](10-comptes-test-cit.md). Ces comptes de test ne sont pas des comptes
de production.

## 13. Sécurité, responsabilités et résolution des blocages

- Les identifiants et pièces doivent rester dans le compte et le dossier concernés; chaque personne
  utilise son compte individuel.
- Les données client sont cloisonnées par société. Le personnel interne est cloisonné selon ses
  habilitations, files et périmètres.
- L’interface masque les fonctions non autorisées, mais le serveur contrôle aussi chaque action et
  ressource. Taper une URL directe ne donne aucun droit.
- Un compte désactivé ou invalide ne doit pas continuer à utiliser la plateforme, même si sa session
  était déjà ouverte.
- En cas d’accès manquant : relever le nom du compte, l’action attendue, l’écran et le message
  d’erreur; contacter l’administrateur CIT approprié. Pour un incident technique, utiliser
  « Signaler un problème technique ».
- Pour un complément métier, répondre depuis le dossier concerné plutôt que de créer un second
  dossier; cela conserve l’historique et évite les doublons.

## 14. Référence de maintenance

Cette fiche explique les parcours en langage métier. Pour confirmer ou modifier un droit, vérifier
dans cet ordre :

1. `src/CIT.Plateforme.Domain/Securite/NatureUtilisateur.cs` : codes de permission et profil
   support N1;
2. `src/CIT.Plateforme.Application/Web/CatalogueNavigation.cs` : entrées de menu et conditions
   d’affichage;
3. `src/CIT.Plateforme.Application/Web/ObtenirContexteUtilisateurQuery.cs` : espaces calculés;
4. les commandes et services d’autorisation du module : contrôle effectif côté serveur;
5. `docs/architecture-autorisation.md` : invariants de sécurité.

Le menu n’est pas la source d’autorisation. Toute modification de ce guide doit rester fidèle aux
habilitations, rattachements, contrôles des ressources et parcours réellement implémentés.
