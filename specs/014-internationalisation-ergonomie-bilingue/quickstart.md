# GUIDE DE PRISE EN MAIN & VALIDATION RAPIDE (QUICKSTART) — FEATURE 014

**Feature** : Feature 014 — Internationalisation & Ergonomie Bilingue (Français / Anglais)  
**Périmètre** : **NFR-07** (Should)  

---

## 1. SCÉNARIOS DE VÉRIFICATION RAPIDE

### Scénario 1 : Première visite avec navigateur en français
1. Ouvrir le portail avec `Accept-Language: fr-FR,fr;q=0.9`.
2. **Vérification** : La page d'accueil et le menu s'affichent en français. L'attribut `<html lang="fr">` est présent.

### Scénario 2 : Première visite avec navigateur en anglais
1. Ouvrir le portail avec `Accept-Language: en-US,en;q=0.9`.
2. **Vérification** : La page d'accueil et les boutons d'accès s'affichent en anglais ("Sign In", "Submit Import", etc.).

### Scénario 3 : Changement de langue via le sélecteur
1. Cliquer sur le composant sélecteur de langue dans le header et choisir **English (EN)**.
2. **Vérification** : La page courante se recharge en anglais, l'URL et les paramètres de filtres sont conservés, et le cookie `.AspNetCore.Culture` est créé.

### Scénario 4 : Saisie d'une demande Import en anglais
1. Se connecter et accéder à `/billing-import/soumettre`.
2. **Vérification** : Les étapes du formulaire, les labels ("BL Number", "Containers", "Delivery Type") et les messages d'aide sont en anglais.
3. Soumettre le dossier : Le ticket généré conserve son numéro `TCK-...` et son statut interne `EnAttenteValidation`.

### Scénario 5 : Préservation de saisie lors d'une bascule de langue
1. Commencer à remplir un formulaire (ex. `/customer-complaint/nouvelle-reclamation`).
2. Saisir l'objet et la description.
3. Changer la langue via le sélecteur.
4. **Vérification** : Les textes déjà saisis restent intacts dans les champs et le brouillon ERR-01 est préservé.

### Scénario 6 : Test de complétude de la matrice bilingue
1. Exécuter le test automatisé `NfrCert07CompletudeRessourcesTests`.
2. **Vérification** : 100% des clés FR possèdent leur traduction EN non vide.
