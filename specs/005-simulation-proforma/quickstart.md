# Quickstart & Validation Manuelle — Feature 005 : Simulation de Proforma

> **Dossier** : `specs/005-simulation-proforma/`  
> **Date** : 30 Août 2026  
> **Statut** : **VALIDÉ**  
> **Auteur** : Antigravity (Pair Programming AI)

---

## 1. Parcours Client : Calcul, Sauvegarde & Export Proforma

### Étape 1 : Accès au Simulateur
1. Se connecter avec le persona **Client Normal** (ou tout autre profil client rattaché à une entreprise active).
2. Depuis le tableau de bord client (`/client/dashboard`) ou le menu latéral, cliquer sur **"Simulation de Proforma"** (`/simulation/proforma`).
3. Vérifier que la page s'ouvre sans demander de GUID ni bloquer sur un prérequis de ticket.

### Étape 2 : Saisie des Paramètres du Dossier
1. Sélectionner le Sens : `Import Local`.
2. Saisir la Date d'arrivée du navire : `10/02/2026`.
3. Saisir la Date d'enlèvement prévisionnel : `10/03/2026` (durée calculée automatiquement : `29 jours`).
4. (Facultatif) Saisir le Nom du Navire (`"MSC ABIDJAN"`), Voyage (`"2601A"`), N° BL (`"BL-SIM-001"`).

### Étape 3 : Ajout d'un Conteneur (Cas `test06`)
1. Cliquer sur **"+ Ajouter un conteneur"**.
2. Renseigner :
   - N° Conteneur : `MSCU9876543`
   - Taille : `40"`
   - Type : `Reefer`
   - Poids brut : `300` tonnes
   - Dangereux : `Non`
   - Hors gabarit (OOG) : `Non`
   - Catégorie : `C3 : Marchandises diverses`
3. Cliquer sur **"Calculer la proforma"**.

### Étape 4 : Vérification du Résultat Calculé
1. Vérifier que le total affiché est exactement **`2 245 300 XOF`**.
2. Déplier les détails du conteneur et vérifier la décomposition :
   - Acconage : `280 000 XOF`
   - Relevage : `50 000 XOF`
   - Branchement frigorifique (27 jours facturables à 67 000) : `1 809 000 XOF`
   - Majoration lourd (30% d'acconage) : `84 000 XOF`
   - Stationnement (18 jours facturables : 5j à 850 + 10j à 1 250 + 3j à 1 850) : `22 300 XOF`
   - Répartition stationnement : CIT (`6 690 XOF`), OIC (`4 460 XOF`), PAA (`11 150 XOF`).
3. Vérifier la présence de la **Mention Légale** d'avertissement non engageante (`SIM-05`).

### Étape 5 : Enregistrement & Historique
1. Cliquer sur **"Enregistrer la simulation"**.
2. Noter la référence générée (ex. `SIM-20260830-0001`).
3. Naviguer vers **"Mes simulations"** (`/simulation/historique`) et constater la présence de la simulation enregistrée.
4. Cliquer sur la ligne pour ouvrir la fiche de détail (`/simulation/{id}`).
5. Cliquer sur **"Télécharger PDF"** et vérifier le document généré.

---

## 2. Parcours Administrateur : Gestion des Grilles & Import CSV

### Étape 1 : Accès à l'Administration Tarifaire
1. Se connecter avec le persona **Administrateur CIT** (détenteur de `Permission.TarificationAdministrer`).
2. Ouvrir la page **"Grilles Tarifaires"** (`/admin/tarification`).
3. Constater l'affichage de la grille initiale active `v1.0` avec ses 16 lignes tarifaires (Annexe A.1).

### Étape 2 : Import d'un Fichier CSV Invalide (Test de Rejet Atomique)
1. Préparer un fichier `grille_erronee.csv` contenant une ligne avec un montant d'acconage négatif ou un texte invalide.
2. Téléverser le fichier dans la zone d'import CSV.
3. Constater le rejet immédiat, le message d'erreur indiquant le numéro de ligne en cause, et vérifier qu'aucune grille corrompue n'a été créée.

### Étape 3 : Import d'un Fichier CSV Valide & Prise d'Effet
1. Préparer un fichier `grille_v2.csv` valide avec une date d'effet future.
2. Importer le fichier avec succès.
3. Vérifier que la nouvelle version `v2.0` apparaît dans l'historique des versions.

---

## 3. Parcours de Rattachement Billing (Optionnel)

1. Revenir sur le persona **Client Normal**.
2. Ouvrir le formulaire de soumission **Billing Import** (`/billing-import/soumettre`) ou **Billing Export** (`/billing-export/soumettre`).
3. Renseigner le champ optionnel **"Référence Simulation Proforma"** avec `SIM-20260830-0001`.
4. Soumettre la demande et constater sur la fiche du ticket/dossier créé que la référence de simulation est bien archivée et consultable.
