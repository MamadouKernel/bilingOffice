# Plan de données de recette

> **Document** : `docs/61-uat-test-data-plan.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Règle directrice** : **aucune donnée personnelle réelle**. Aucune copie de production.

---

## 1. Règle de constitution

| Règle | Application |
|:---|:---|
| `SYNTHETIC` | **Par défaut.** Données inventées, cohérentes, sans lien avec des personnes ou des clients réels |
| `MASKED` | Pour un jeu dérivé du réel : identités et coordonnées remplacées, cohérence préservée |
| `PRODUCTION_COPY_FORBIDDEN` | **Interdiction** de copier la production telle quelle en recette |

### Pourquoi l'interdiction de la copie de production

Un environnement de recette n'a pas le niveau de protection de la production : les accès y sont
plus larges, les journaux moins surveillés, les sauvegardes moins encadrées. Y déverser des
données réelles de clients et de chauffeurs y transporterait des données personnelles sans le
cadre qui les protège.

S'il fallait néanmoins partir du réel, seul un jeu **`MASKED`** serait acceptable — et le
masquage devrait être validé, pas supposé.

### Conventions de nommage

Pour que rien ne soit confondu avec du réel :

| Type | Convention |
|:---|:---|
| Adresses e-mail | `@recette.test` — domaine non routable |
| Sociétés | Noms manifestement fictifs |
| Téléphones | Plages de test, jamais de numéro attribué |
| Références de dossier | Préfixe distinctif |

**Ne pas utiliser les domaines `cotedivoireterminal.com` ni `cit.ci`** pour des comptes de recette :
un envoi accidentel atteindrait de vraies boîtes.

---

## 2. Comptes par profil

Le CDC § 6 définit les profils. Chaque scénario doit être joué **avec le profil concerné**.

| Profil | Comptes | Règle | Usage en recette |
|:---|---:|:---|:---|
| Client normal | 2 | `SYNTHETIC` | Parcours import, cloisonnement entre clients |
| Client en compte | 1 | `SYNTHETIC` | Workflow spécifique |
| Client deposit | 1 | `SYNTHETIC` | Lettrage comptable |
| Client IES | 1 | `SYNTHETIC` | Branche IES |
| Collaborateur périmètre restreint | 1 | `SYNTHETIC` | ENT-03 — ne voit que ses demandes |
| Collaborateur périmètre société | 1 | `SYNTHETIC` | ENT-03 — voit tout |
| Transporteur / chauffeur | 2 | `SYNTHETIC` | Enrôlement, personne physique sans société |
| Pilote Manifeste | 1 | `SYNTHETIC` | File Contrôle BL |
| Back Office Export | 1 | `SYNTHETIC` | Codification export |
| Pilote Facturation Terre | 1 | `SYNTHETIC` | Édition de facture |
| Comptable Client | 1 | `SYNTHETIC` | Lettrage |
| Pilote Contrôle Documentaire | 1 | `SYNTHETIC` | Documents finaux, clôture |
| Support Opérationnel | 1 | `SYNTHETIC` | Réclamations |
| Agent Gate | 1 | `SYNTHETIC` | Enrôlements |
| Superviseur | 1 | `SYNTHETIC` | Supervision, réaffectation |
| Administrateur fonctionnel | 1 | `SYNTHETIC` | Comptes, files, SLA, catégories |
| Administrateur technique | 1 | `SYNTHETIC` | Journal d'audit |
| **Agent sans droit** | 1 | `SYNTHETIC` | **Vérifier les refus** |

**Le dernier compte est indispensable.** Sans un compte délibérément non habilité, on ne peut pas
démontrer que les autorisations refusent — seulement qu'elles autorisent.

---

## 3. Données métier

| Objet | Volume | Règle | Justification |
|:---|---:|:---|:---|
| Sociétés clientes | 4 | `SYNTHETIC` | Une par type de client, plus une pour le cloisonnement |
| Sociétés en attente de validation | 2 | `SYNTHETIC` | Parcours de validation et de rejet |
| Dossiers import | 5 | `SYNTHETIC` | Couvrir les états du workflow |
| Dossiers export | 5 | `SYNTHETIC` | Idem, branche export |
| Tickets à divers stades | 10 | `SYNTHETIC` | Files, SLA, historique |
| Réclamations | 3 | `SYNTHETIC` | Catégories différentes |
| Demandes d'avoir / exonération | 2 | `SYNTHETIC` | Instruction et décision |
| Dossiers d'enrôlement | 3 | `SYNTHETIC` | États : en cours, complément, validé |
| Documents déposés | 10 | `SYNTHETIC` | Types autorisés et interdits |
| Grille tarifaire | 1 version | **Réelle** | Le calcul doit être vérifiable — Annexe A du CDC |
| Files de traitement | 10 | Paramétrage | Une par rôle |
| Paramètres SLA | selon files | Paramétrage | Déclenchement des alertes |
| Catégories de réclamation | 5 | Paramétrage | Orientation |
| Pièces exigées à l'ouverture de compte | 2 à 3 | Paramétrage | Contrôle de complétude — CPT-01 |

**La grille tarifaire fait exception.** Elle doit être **réelle** : un calcul de proforma ne se
vérifie que contre les valeurs en vigueur, fournies en Annexe A du CDC. Ce ne sont pas des données
personnelles.

---

## 4. Cas particuliers à provisionner

Ces jeux sont souvent oubliés, et ce sont ceux qui révèlent les défauts.

| Cas | Données | Ce qu'il permet de vérifier |
|:---|:---|:---|
| **Gel légal** | 1 ticket sous gel | La purge de rétention ne l'emporte pas |
| **Rétention échue** | 1 dossier au-delà de sa durée | La purge s'applique |
| **Compte désactivé** | 1 agent désactivé avec historique | SEC-11 : accès perdu, historique conservé |
| **Société non validée** | 1 demandeur en attente | CPT-01 : compte créé mais inutilisable |
| **Société refusée** | 1 demande rejetée | L'accès reste fermé |
| **Collaborateur désactivé** | 1 rattachement inactif | ENT-02 |
| **2FA activé** | 1 compte avec second facteur | CPT-07 |
| **Fichier EICAR** | 1 fichier de test | SEC-07 — voir `docs/59 § 4` |
| **Document volumineux** | 1 fichier > 10 Mo | Limite de taille |
| **Extension interdite** | 1 fichier `.exe` | Filtrage des types |

---

## 5. Constitution du jeu

| Voie | Applicable | Remarque |
|:---|:---|:---|
| Écrans de l'application | **Oui** | Voie recommandée : exerce les parcours réels et produit un audit cohérent |
| Jeu de données de développement | **Non** | Ne s'exécute qu'en `Development` — inadapté à un Staging |
| Script d'insertion en base | Possible | Contourne les invariants métier ; à éviter |
| Copie de production | **Interdit** | `PRODUCTION_COPY_FORBIDDEN` |

**La constitution par les écrans est préférable**, même si elle est plus lente : elle garantit que
les données respectent les invariants, et elle produit un journal d'audit cohérent — que la
recette vérifiera ensuite.

Le compte administrateur initial (`docs/55`) permet de créer les comptes agents, puis les comptes
clients s'inscrivent eux-mêmes.

---

## 6. Cycle de vie

| Question | Réponse |
|:---|:---|
| Réinitialisation entre campagnes | `TBD_UAT` |
| Conservation après recette | `TBD_UAT` |
| Sauvegarde du jeu | Recommandée avant chaque campagne |
| Suppression en fin de projet | `TBD_DSI` |

---

## 7. Récapitulatif

| Catégorie | Éléments | Règle |
|:---|---:|:---|
| Comptes | **19** | `SYNTHETIC` |
| Sociétés | 6 | `SYNTHETIC` |
| Dossiers et tickets | 20 | `SYNTHETIC` |
| Documents | 10 + cas particuliers | `SYNTHETIC` |
| Paramétrage | 5 familles | Configuration |
| Grille tarifaire | 1 version | **Réelle** |
| Cas particuliers | 10 | `SYNTHETIC` |

**Aucune donnée personnelle réelle n'entre dans ce jeu.**
