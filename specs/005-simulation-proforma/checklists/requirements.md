# Checklist des Exigences — Feature 005 : Simulation de Proforma

> **Dossier** : `specs/005-simulation-proforma/`  
> **Date** : 30 Août 2026  
> **Statut** : **VALIDÉ POST-CLARIFICATION — PRÊT POUR /SPECKIT.PLAN**

---

## 1. Inventaire Exhaustif des Exigences (SIM-01 à SIM-11)

- [x] **SIM-01** (Must) : Lancement optionnel d'une simulation avant, si nécessaire, de démarrer son parcours de facturation.
- [x] **SIM-02** (Must) : Moteur de calcul par conteneur (acconage, relevage, branchement frigorifique, majoration lourd, majoration dangereux, majoration hors gabarit, stationnement par tranches, répartition).
- [x] **SIM-03** (Must) : Tarif déterminé par la clé Sens + Taille + Catégorie de marchandise à partir d'une grille administrable.
- [x] **SIM-04** (Must) : Déduction de la durée de séjour pour le calcul du branchement frigorifique et du stationnement.
- [x] **SIM-05** (Must) : Détail des postes par conteneur, total général du dossier et mention légale obligatoire d'avertissement.
- [x] **SIM-06** (Must) : Paramétrage administrable sans développement de l'ensemble des éléments de calcul (Annexe A).
- [x] **SIM-07** (Must) : Versionnage des grilles tarifaires avec date d'effet et traçabilité de la version appliquée.
- [x] **SIM-08** (Should) : Enregistrement de la simulation, consultation sur le profil client et rattachement futur à un ticket (`CLAR-SIM-05`).
- [x] **SIM-09** (Should) : Export / impression du résultat de simulation en PDF.
- [x] **SIM-10** (Should) : Import d'une nouvelle grille tarifaire par fichier structuré CSV (`CLAR-SIM-01`).
- [x] **SIM-11** (Could) : Contrôles de cohérence dynamiques à la saisie avec messages d'aide contextuels.

---

## 2. Conformité du Scénario Contractuel REC-SCE-06

- [x] Scénario `REC-SCE-06` identifié et repris littéralement depuis le CDC §22.
- [x] Cas de référence `test01` à `test06` de l'Annexe A.8 intégrés comme critères de test au franc près.
- [x] Cas `test06` (Import Local, 40", Reefer, 300t, C3, séjour 29j ➔ Total $2\,245\,300\text{ XOF}$) prouvé mathématiquement.
- [x] Règle de cumul additif des majorations sur l'acconage de base prouvée par le cas `test01` (199 129 XOF).

---

## 3. Clarifications Métier Figées (CLAR-SIM-01 à CLAR-SIM-05)

- [x] `CLAR-SIM-01` : Format CSV standard (point-virgule, UTF-8) avec validation atomique.
- [x] `CLAR-SIM-02` : Grille active à la date de simulation (`DateCreationUtc`) figée avec traçabilité de version.
- [x] `CLAR-SIM-03` : Rejet bloquant en cas de combinaison non tarifée (aucun tarif à 0 par défaut).
- [x] `CLAR-SIM-04` : Conservation indéfinie et immuable des simulations avec badge d'avertissement si révision de grille.
- [x] `CLAR-SIM-05` : Référence unique et champ optionnel de rattachement lors des soumissions Import/Export.

---

## 4. Respect des Principes d'Architecture & Constitution

- [x] **Aucun ticket imposé pour simuler** : La simulation est totalement autonome et non engageante.
- [x] **Aucun tarif en dur** : Toutes les règles s'appuient sur des grilles et paramètres configurables.
- [x] **Zéro GUID manuel dans l'UX** : Sélection des référentiels via composants intuitifs.
- [x] **Sécurité Multi-Tenant** : Cloisonnement strict des simulations sauvegardées par société.
- [x] **Frontières nettes** : Pas d'absorption des Avoirs (`AVE`), Réclamations (`REC`) ou Enrôlement (`ENR`).
