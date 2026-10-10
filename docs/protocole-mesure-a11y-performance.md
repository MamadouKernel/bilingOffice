# Protocole de mesure — Accessibilité (WCAG) & Performance

Ces deux piliers de l'audit ne se **décrètent** pas depuis le code : ils se **mesurent** sur l'application
en marche. Voici la procédure pour obtenir un score chiffré et reproductible, et viser 10/10.

Prérequis : l'app tournante (local `dotnet run` sur `src/CIT.Plateforme.Web`, ou l'instance IIS), et
un compte de test par rôle (voir le seed UAT).

## 1. Accessibilité (WCAG 2.2 AA)

### Outils
- **axe-core** (extension navigateur « axe DevTools ») ou **Lighthouse** (onglet Accessibility de Chrome).
- Clavier seul (Tab / Shift+Tab / Entrée / Échap) + un lecteur d'écran (NVDA gratuit).

### Écrans à auditer (5 parcours clés)
1. `/comptes/connexion` (public)
2. `/` tableau de bord (client ET agent)
3. Fiche ticket `/tickets/{id}`
4. Un formulaire lourd : `/billing-import/soumettre` ou `/enrolement`
5. Une page d'administration (tableau + modale)

### Check-list (bloquants pour un 10/10)
- [ ] **Contraste** >= 4.5:1 (texte normal), >= 3:1 (texte large / elements UI). Verifier la palette `u-c-*`.
- [ ] **Navigation clavier** complete : tout CTA atteignable et activable sans souris.
- [ ] **Focus visible** sur chaque element interactif (pas de `outline:none` nu).
- [ ] **Labels** de formulaire lies (`<label for>`), messages d'erreur annonces (aria-live).
- [ ] **Roles/landmarks** : `header`, `nav`, `main`, `footer` presents (deja le cas) ; modales `role="dialog"` + focus trap.
- [ ] **Images** : `alt` pertinent ; icones decoratives `aria-hidden`.
- [ ] **Langue** : `<html lang>` correct et coherent avec le selecteur FR/EN.
- [ ] Zero erreur axe « critical » / « serious » sur les 5 ecrans.

### Commande Lighthouse (CLI)
```bash
npx lighthouse http://localhost:5xxx/ --only-categories=accessibility --output=html --output-path=./a11y.html
```
Objectif : score >= 95 sur chaque ecran, 0 bloquant manuel (clavier/lecteur).

## 2. Performance

### Nature de l'app
Blazor **Server** (SSR + circuit SignalR). Les Core Web Vitals classiques comptent moins qu'en SPA ;
les vrais points chauds sont le **circuit SignalR** sous charge et les **requetes EF** (N+1).

### Lighthouse (rendu initial)
```bash
npx lighthouse http://localhost:5xxx/ --only-categories=performance --output=html --output-path=./perf.html
```
Cibles : LCP < 2,5 s · CLS < 0,1 · TBT < 200 ms (sur LAN interne, viser mieux).

### Charge circuit SignalR (le test qui compte vraiment)
- Simuler N utilisateurs connectes simultanes (ex. pic de formation) et mesurer la latence d'interaction.
- Outil : `bombardier`/`k6` pour le HTTP SSR ; pour le circuit, un script Playwright multi-contextes
  reproduisant une navigation type (login -> dashboard -> fiche ticket).
- Surveiller cote serveur : CPU, RAM, nombre de circuits, temps de reponse DB.

### Requetes EF (N+1)
- Activer le logging EF (`Microsoft.EntityFrameworkCore.Database.Command` = Information) en local.
- Charger les ecrans de **listes/dashboards** (supervision, annuaire agents, performance agents) et
  compter les requetes : une liste ne doit pas declencher 1 requete par ligne.
- Corriger par projection / `Include` cible / pagination (deja presente par endroits).

## 3. Restitution
Pour chaque pilier : score chiffre par ecran + liste des bloquants + correctif. Un **10/10** n'est
atteint que lorsque axe ne remonte aucun bloquant, le clavier/lecteur passent, et la charge cible
tient sans degradation. Tant que ces mesures ne sont pas faites, l'audit laisse ces deux piliers
« a mesurer » plutot que de leur attribuer une note non verifiee.
