# Backlog UI/UX ultime

**Date** : 7 septembre 2026. Tests **1606/1606**. Score **93/100**. Aucun commit.

## A. Décisions métier (AM) — NON tranchées

### AM-01 — Durée de séjour facturée : borne incluse ou exclue ?
- **Écran** : simulateur proforma / historique / branchement frigo.
- **Comportement actuel** : affichages divergents (14 j / 15 j / 13 j) pour le même intervalle.
- **Impact utilisateur** : montant perçu incohérent selon l'écran.
- **Options** : (a) borne exclue ; (b) borne incluse.
- **Impact métier** : règle de facturation réelle. **Impact sécurité** : nul.
- **Recommandation technique** : uniformiser sur la règle du CDC une fois tranchée.
- **Décision attendue** : métier/facturation.

### AM-02 — Périmètre du « collaborateur limité »
- **Écran** : espace client (`collab.limite`). Atterrit sur le **même** dashboard que
  `client.normal` (constaté en recette live ce tour).
- **Comportement actuel** : `collab.limite` porte une permission réduite (`BillingImportSoumettre`
  seule) vs `client.normal` (import + export + réclamation) — mais l'écart d'expérience n'est pas
  formalisé.
- **Impact utilisateur** : périmètre réduit peu lisible.
- **Options** : (a) restreindre visiblement ; (b) statu quo.
- **Impact sécurité** : le cloisonnement serveur existe déjà (permissions) ; c'est l'UX qui est en
  jeu.
- **Recommandation** : définir le périmètre réduit attendu, puis test de cloisonnement dédié.
- **Décision attendue** : métier.

### AM-03 — Confirmation des transitions de workflow
- **Écran** : « Valider Booking » (et transitions analogues).
- **Comportement actuel** : avance le workflow **sans** confirmation, alors qu'une attribution
  d'habilitation en exige une.
- **Options** : (a) confirmer toute transition ; (b) ne confirmer que le destructif.
- **Recommandation** : harmoniser après décision (la modale de confirmation existe et est câblée).
- **Décision attendue** : métier.

## B. Décision d'infrastructure

### D-04 — Session / antiforgery / DataProtection
| Environnement | Comportement |
|---|---|
| Development | Page antiforgery brute possible sur jeton périmé. |
| UAT | Idem Development (écran de marque via `/Error` en non-Development). |
| Production | Écran de marque `/Error` (exception handler). |
| Mono-instance | Clés DataProtection en mémoire/fichier local — OK au redémarrage près. |
| Multi-instance | **Risque** : sans magasin de clés partagé (`PersistKeysTo` + `SetApplicationName`), un cookie émis par une instance est rejeté par une autre → déconnexions. |
| Recommandation | Message « session expirée » spécifique + persistance des clés DataProtection partagée. **Modification du pipeline de sécurité** : à faire **avec** la décision d'infrastructure, pas pour un point UI. |

## C. Technique bloqué (harnais)

| Point | Blocage |
|---|---|
| Recette **visuelle** des 68 pages authentifiées (responsive, sombre, zoom, clavier) | CSS non servi au navigateur de prévisualisation en environnement non-Development (variante compressée vide). Le serveur sert pourtant la feuille complète (`curl`). Déblocage : `dotnet publish` ou navigateur gérant la compression servie. |

## D. Risques acceptés / faux positifs

| ID | Nature |
|---|---|
| Boutons de marque blanc-sur-azur 4,16:1 | Décision de token `--action` (impact identité). |
| `!important` Bootstrap | Requis pour teinter le thème sombre ; retrait = régression (démontré). |
| ~200 `style=` inline résiduels | Dette cosmétique sans impact ([`sweep-design-final.md`](sweep-design-final.md), classe F). |
| Emojis décoratifs sans `aria-hidden` | Faible valeur (contrôles déjà nommés par leur texte). |
| Cumul CSV syntaxe+référentiel (D-22) | Faux positif : une ligne mal formée ne peut être vérifiée sémantiquement. |

## E. Acquis ce tour (ne plus rouvrir)

- Mécanisme d'authentification de recette UAT (sans secret, 4 barrières, impossible en Production) —
  testé + mutation + prouvé live.
- Sidebar filtrée par permissions : **0 lien interdit** sur 6 profils, vérifié **en navigateur**.
- Enforcement serveur sur accès direct (agent sans droits refusé) — vérifié live.
- Compte désactivé : sign-in refusé sans cookie — vérifié live.
