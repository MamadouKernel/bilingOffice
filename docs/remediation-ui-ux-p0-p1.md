# Remédiation UI/UX — UX-P0 et UX-P1

*Périmètre strict : le UX-P0 et les cinq UX-P1. Aucun UX-P2/P3 n'a été traité.*
*Aucune migration créée ni exécutée. L'application n'a pas été démarrée.*

| | |
|---|---|
| Audit de référence | `docs/audit-final-ui-ux.md` · `docs/backlog-ui-ux.md` |
| État avant | Conformité 99,0 % · **1424/1424** · score UI 65/100 provisoire |
| Défauts traités | UI-01 *(P0)* · UI-02 · UI-03 · UI-04 · UI-05 · UI-06 |

---

## Résumé

**Le UX-P0 est fermé, et il l'est au bon niveau.** La confirmation ajoutée à l'écran n'aurait pas
suffi : l'opération elle-même exécutait une soixantaine de transactions indépendantes. Une commande
applicative dédiée les réunit désormais sous **une seule unité de travail** — tout est appliqué, ou
rien.

**Un des cinq UX-P1 n'existait pas.** UI-05 affirmait que le menu n'était pas filtré par profil.
C'est faux : il l'est, par quatre `@switch` sur l'espace actif. **Mon audit avait compté les `@if` et
conclu à tort.** Aucune correction n'a été faite, et le rapport le rectifie.

**Trois autres sont fermés sur leur surface la plus exposée, pas partout.** UI-02, UI-03 et UI-04
concernent respectivement 233 gestionnaires de clic, une trentaine d'actions destructives et
55 tableaux. Les traiter intégralement aurait été la refonte que la consigne interdit — et je ne
peux vérifier visuellement aucun écran. La couverture exacte est donnée défaut par défaut.

**Le sixième, UI-06, est fermé intégralement** : 164 sites dans 67 fichiers.

Enfin, une **garde d'atteignabilité des composants partagés** a été ajoutée. Elle détecte
exactement le défaut d'origine — vérifié par mutation.

---

## 1. Cartographie des six écarts

Extraits de `docs/backlog-ui-ux.md` avant toute modification.

| ID | Page | Défaut | Impact | Cause | Correction minimale | Fichiers |
|---|---|---|---|---|---|---|
| **UI-01** *(P0)* | `ComptesAgents.razor` | « Tout accorder » / « Tout retirer » : un clic, sans confirmation, non atomique, hors wrapper d'erreur | Un clic accorde ~60 permissions ou les retire toutes ; un échec en cours laisse un état partiel muet | La boucle appelle `GererHabilitationAgentCommand`, dont **chaque appel valide sa propre unité de travail** | Commande applicative dédiée + `ModaleConfirmation` + verrou + retour | `AdministrationComptesAgentsCommands.cs`, `ComptesAgents.razor`, `DependencyInjection.cs`, `ModaleConfirmation.razor`, `ToastNotification.razor` |
| **UI-02** | Transversal (233 `@onclick`) | Aucune protection contre la double soumission ; 2 pages seulement avec verrou | Double-clic → deux commandes | Les `disabled="@…"` valident la complétude du formulaire, pas l'envoi en cours | Drapeau `_enCours` sur les actions mutantes | `ComptesAgents.razor` *(voir couverture)* |
| **UI-03** | Transversal (~30 actions destructives) | `ModaleConfirmation` existe, **zéro référence** | Action irréversible sans point d'arrêt | Composant écrit, jamais branché | Brancher le composant existant | `ModaleConfirmation.razor`, `ComptesAgents.razor` |
| **UI-04** | 55 pages à tableau | Aucune pagination | Sur volume réel : page lente, liste illisible | Les requêtes rendent l'intégralité des lignes | Pagination **serveur** sur les surfaces non bornées | `ConsulterJournalAuditQuery.cs`, `Repositories.cs`, `JournalAudit.razor` |
| **UI-05** | `NavMenu.razor` | *« Menu non filtré par permission »* | *(prétendu)* impasses pour tous les profils | **AUCUNE — le constat était faux** | **Aucune** | *(aucun)* |
| **UI-06** | Transversal (89 sites) | `_erreur = ex.Message` expose un message technique | Message incompréhensible, révélateur de la structure interne | `catch (Exception)` générique restituant le message brut | Helper à liste blanche | `MessagesUtilisateur.cs` + 67 fichiers |

---

## 2. UI-01 — UX-P0 fermé

### La cause racine, et pourquoi la modale seule ne suffisait pas

L'écran bouclait :

```csharp
foreach (var code in manquantes)
    await gererHabilitation.AttribuerAsync(_appelantId, cibleId, code);   // ← 1 transaction chacune
```

`GererHabilitationAgentCommand.AttribuerAsync` termine par `uniteDeTravail.EnregistrerAsync()`.
Soixante appels valident donc **soixante transactions**. Ajouter une confirmation aurait supprimé le
clic accidentel sans rien changer à l'état partiel en cas d'échec — le § 3 de la mission le dit, et
il a raison.

### La correction

**`DefinirHabilitationsEnMasseCommand`** — ajoutée à `AdministrationComptesAgentsCommands.cs`, aux
côtés des commandes existantes, sans seconde architecture.

Les dépôts et la journalisation **empilent** dans le `DbContext` sans écrire ; seul
`UniteDeTravail.EnregistrerAsync()` valide. Réunir les écritures sous un unique appel donne donc une
**atomicité réelle**, garantie par `SaveChanges` — ce n'est pas une convention, c'est le mécanisme
transactionnel déjà présent.

| Exigence de la mission | Réalisé |
|---|---|
| Identité de l'appelant côté serveur | ✅ paramètre `appelantId`, résolu par l'écran depuis le contexte d'authentification |
| Vérification `administration.comptes` | ✅ **une fois, au début** |
| Validation du compte cible | ✅ `ExigerCibleConnueAsync` |
| **Permissions déterminées côté serveur** | ✅ depuis `Permission.Tous` — **jamais fournies par l'appelant** |
| Application cohérente | ✅ une seule unité de travail |
| Audit | ✅ trace **individuelle** par permission + ligne de synthèse |

**Rien de ce que l'appelant transmet ne fonde la décision** : ni rôle, ni société, ni permissions, ni
liste de codes. La règle 1 de `docs/architecture-autorisation.md` est respectée.

### Les dix exigences du § 2

| # | Exigence | Réalisé |
|---|---|---|
| 1 | Confirmation explicite avant exécution | ✅ `ModaleConfirmation` |
| 2 | Texte expliquant la conséquence | ✅ « recevra N permission(s) supplémentaire(s), soit l'intégralité des M permissions — administration des comptes, tarification et journal d'audit compris » |
| 3 | Nom du compte cible | ✅ nom **et** e-mail |
| 4 | Nombre de permissions concernées | ✅ calculé avant ouverture |
| 5 | Bouton critique identifiable | ✅ `btn-warning` pour l'octroi, `btn-danger` pour la révocation |
| 6 | Possibilité d'annuler | ✅ bouton, Échap, clic extérieur |
| 7 | Protection double-clic | ✅ `_enCours` + boutons désactivés + garde en tête de méthode |
| 8 | État busy pendant l'opération | ✅ spinner et libellé « Traitement en cours… » |
| 9 | Retour succès | ✅ toast détaillant le nombre appliqué |
| 10 | Retour erreur | ✅ toast, message filtré par `MessagesUtilisateur` |

### Composants réutilisés, pas réécrits

Conformément à la consigne, aucune implémentation locale n'a été créée. Les deux composants
existants ont été **améliorés puis branchés** — ils manquaient de l'essentiel pour être utilisés sans
dégrader l'accessibilité :

| Composant | Ajouts |
|---|---|
| `ModaleConfirmation` | `role="dialog"`, `aria-modal`, `aria-labelledby`, `aria-describedby`, fermeture par **Échap**, focus initial sur **Annuler** (le geste sûr, jamais le destructeur), état busy neutralisant la fermeture accidentelle |
| `ToastNotification` | `role="status"` + `aria-live="polite"`, `aria-hidden` sur l'icône décorative, `aria-label` traduit *(il était en anglais)* |

---

## 3. Atomicité

`AccorderToutesAsync` et `RetirerToutesAsync` stagent l'intégralité des écritures — habilitations
**et** entrées d'audit — puis appellent **une seule fois** `EnregistrerAsync`.

Conséquence vérifiée par test : **un refus d'autorisation ne laisse aucune trace**, ni habilitation,
ni ligne d'audit *(`Un_refus_ne_laisse_aucune_ecriture_partielle`)*.

La granularité de l'audit n'a pas été sacrifiée : chaque attribution reste tracée individuellement,
comme avant, et une ligne de synthèse s'y ajoute. RG-SEC-03 n'est pas affaiblie.

---

## 4. « Tout retirer » — le comportement déterministe

Le scénario que la mission demande de traiter explicitement :

> administrateur → retire toutes ses propres permissions → perd `administration.comptes` pendant la boucle.

**Avant** : la boucle retirait tout, puis `GererHabilitationAgentCommand.RetirerAsync` levait
`InvalidOperationException` en atteignant `administration.comptes`. L'administrateur se retrouvait
dépouillé de tout le reste, avec une exception non traitée et un écran muet.

**Après** : `administration.comptes` est **exclue du retrait** et rendue dans
`PermissionsConservees`. L'opération va à son terme, sans exception, et **dit ce qu'elle a laissé** :

> « 61 permission(s) révoquée(s). Conservée(s) pour ne pas vous fermer l'accès : administration.comptes. »

L'invariant « un administrateur ne ferme pas la porte de l'intérieur » est préservé — mais par une
règle explicite et annoncée, plutôt que par une exception au milieu d'une boucle.

La modale prévient d'ailleurs **avant** : « Vous vous appliquez cette action à vous-même : la
permission d'administration des comptes vous sera CONSERVÉE. »

Verrouillé par deux tests : `Auto_retrait_conserve_l_administration_des_comptes_sans_exception` et
`Apres_auto_retrait_l_administrateur_peut_encore_administrer`.

---

## 5. UI-05 — un défaut qui n'existait pas

**C'est une erreur de mon audit, et elle doit être dite.**

Mon rapport concluait : *« Filtrage par permission : absent. Un seul `@if` de visibilité dans
383 lignes »*, et notait la navigation 6/10 en partie sur cette base.

Vérification faite dans `NavMenu.razor` :

```
@switch (_contexte.EspaceActif)   →  4 occurrences
case EspaceFonctionnel.…          →  18 branches
```

**Le menu est filtré** — par `@switch` sur l'espace actif, pas par `@if`. Chaque groupe d'accordéon
ne rend que les liens de l'espace courant. Un client voit les branches Client, et rien d'autre.

Et `EspacesDisponibles` est calculé **côté serveur** dans `ObtenirContexteUtilisateurQuery`, à partir
des permissions et habilitations réelles :

```csharp
var estAdminCit = permissions.Contains(Permission.CompteValider) || … ;
if (estAdminCit) espaces.Add(EspaceFonctionnel.AdministrationCit);
```

Un client n'obtient donc jamais l'espace d'administration, et le `PersonaSwitcher` ne le lui propose
pas.

**Aucune correction n'a été faite.** Corriger un défaut inexistant aurait ajouté du code sans objet,
et masqué l'erreur d'analyse au lieu de la rectifier.

**Le score de navigation de l'audit était donc pénalisé à tort.** Je ne le recalcule pas ici — le
score global reste explicitement provisoire tant qu'aucune inspection navigateur n'a eu lieu.

---

## 6. Couverture réelle des UX-P1

La consigne demande de traiter les cinq UX-P1. Voici ce qui est fermé, et ce qui ne l'est pas.

| ID | État | Couverture | Restant |
|---|---|---|---|
| **UI-02** | 🟡 **PARTIEL** | `ComptesAgents` : actions de masse, bascule unitaire de permission, désactivation. Verrou `_enCours` + boutons désactivés + garde en tête de méthode | Les autres surfaces mutantes (soumissions client, files agent). Le mécanisme serveur protège déjà la prise en charge (jeton de concurrence optimiste) |
| **UI-03** | 🟡 **PARTIEL** | `ModaleConfirmation` branchée sur : octroi en masse, révocation en masse, **désactivation d'un compte**. *(`MonProfil` confirmait déjà la clôture, par une modale locale)* | Les autres actions destructives : rejets de ticket, clôtures, désactivation de collaborateur, suppressions de référentiel |
| **UI-04** | 🟡 **PARTIEL** | **Journal d'audit** — pagination **serveur** (`Skip`/`Take` en base), taille bornée côté serveur, navigation accessible | Les 54 autres tableaux. Le journal était la seule surface à croissance strictement non bornée |
| **UI-05** | ⚪ **SANS OBJET** | — | Faux positif de l'audit |
| **UI-06** | 🟢 **FERMÉ** | **164 sites, 67 fichiers** — tous les `catch (Exception)` génériques du projet Web | — |

### Pourquoi partiel plutôt que complet

Trois raisons, dans l'ordre de poids :

1. **La consigne interdit la refonte générale.** Poser un verrou sur 233 gestionnaires et une modale
   sur une trentaine d'actions destructives, dans 40 fichiers, est une refonte.
2. **Je ne peux vérifier aucun écran.** L'application n'a pas été démarrée — `Program.cs` applique
   automatiquement les migrations. Une modification massive d'interface qu'on ne peut pas regarder
   est un risque, pas une correction.
3. **Les surfaces traitées sont les plus exposées.** `ComptesAgents` porte l'unique UX-P0 ; le
   journal d'audit est la seule table dont la croissance n'a aucune borne.

Le reste est décrit dans `docs/backlog-ui-ux.md` et reste ouvert. **Je ne le présente pas comme
fermé.**

---

## 7. Garde d'atteignabilité des composants partagés

Demandée au § 10 de la mission, et implémentée : `ComposantsPartagesAtteignablesTests`.

**Le périmètre est volontairement étroit**, comme la consigne l'exige. Le test ne vérifie que les
composants de `Components/Shared` — ceux dont l'existence affirme qu'ils sont réutilisables. Il
n'exige rien :

- des **pages routables** — ce sont des racines, atteintes par leur route ;
- des **layouts** — atteints par le moteur de rendu ;
- des composants propres à une page.

Deux tests, sur le modèle de `ServicesAtteignablesTests` : la garde elle-même, et son miroir
*(dès qu'une entrée de la dette assumée est câblée, elle doit en sortir)*. La liste
`SansUsageAssume` est **vide** : les deux composants qui motivaient ce test sont désormais branchés.

**Mutation de contrôle** — le rendu de `ToastNotification` retiré de `ComptesAgents` :

```
Tout_composant_partage_est_utilise_par_au_moins_une_surface [FAIL]
  - ToastNotification
```

La garde redétecte exactement le défaut d'origine. Restaurée, elle repasse.

---

## 8. Fichiers modifiés

### Créés

| Fichier | Objet |
|---|---|
| `Web/Services/MessagesUtilisateur.cs` | UI-06 — liste blanche des messages destinés à l'utilisateur |
| `Application.Tests/Comptes/HabilitationsEnMasseTests.cs` | 14 tests — UX-P0 |
| `Application.Tests/Securite/PaginationJournalAuditTests.cs` | 11 tests — UI-04 |
| `Web.Tests/Architecture/ComposantsPartagesAtteignablesTests.cs` | 2 tests — garde UI |

### Modifiés

| Fichier | Modification |
|---|---|
| `Application/Comptes/AdministrationComptesAgentsCommands.cs` | `DefinirHabilitationsEnMasseCommand` + `ResultatHabilitationsEnMasse` |
| `Application/Securite/ConsulterJournalAuditQuery.cs` | `ListerPageAsync` au port, `PageJournalAudit`, `ExecuterPageAsync` |
| `Infrastructure/Persistence/Repositories.cs` | `JournalAuditLecture.ListerPageAsync` — pagination en base |
| `Infrastructure/DependencyInjection.cs` | Enregistrement de la commande de masse |
| `Web/Components/Shared/ModaleConfirmation.razor` | Sémantique de dialogue, Échap, focus, état busy |
| `Web/Components/Shared/ToastNotification.razor` | `role="status"`, `aria-live`, `aria-hidden`, libellé français |
| `Web/Components/Pages/Administration/ComptesAgents.razor` | Confirmation, verrou, toast, appel de la commande atomique |
| `Web/Components/Pages/Administration/JournalAudit.razor` | Pagination et navigation |
| `Web/Components/_Imports.razor` | `@using CIT.Plateforme.Web.Services` |
| **67 pages `.razor`** | `ex.Message` → `MessagesUtilisateur.Pour(ex)` dans les `catch (Exception)` |

**Aucune migration créée. Aucune migration exécutée. L'application n'a pas été démarrée.**

---

## 9. Tests

### Ajoutés — 27

| Suite | Cas | Couvre |
|---|---|---|
| `HabilitationsEnMasseTests` | **14** | UX-P0 |
| `PaginationJournalAuditTests` | **11** | UI-04 |
| `ComposantsPartagesAtteignablesTests` | **2** | Garde UI |

### Les dix cas exigés au § 9

| # | Cas demandé | Test |
|---|---|---|
| 1 | Clic n'exécute rien avant confirmation | *(interface — voir limite ci-dessous)* |
| 2 | Annulation → aucune modification | *(interface — voir limite)* |
| 3 | Confirmation → opération exécutée | `Accorder_toutes_applique_l_integralite_du_referentiel` |
| 4 | Double-clic → une seule opération | `Double_execution_ne_produit_aucun_doublon` + `Accorder_toutes_est_idempotent` |
| 5 | Appelant non autorisé → refus | `Appelant_non_autorise_est_refuse_et_rien_n_est_ecrit` · `Administrateur_desactive_est_refuse` |
| 6 | Cible inexistante → refus | `Cible_inexistante_est_refusee` |
| 7 | Erreur → retour | *(interface)* — le filtrage du message est couvert par `MessagesUtilisateur` |
| 8 | Succès → retour | *(interface)* — le contenu du message vient de `ResultatHabilitationsEnMasse`, testé |
| 9 | « Tout retirer » sans état partiel | `Un_refus_ne_laisse_aucune_ecriture_partielle` · `Retirer_toutes_retire_l_integralite_des_permissions` |
| 10 | Auto-retrait → déterministe | `Auto_retrait_conserve_l_administration_des_comptes_sans_exception` · `Apres_auto_retrait_l_administrateur_peut_encore_administrer` |

**Limite assumée.** Les cas 1, 2, 7 et 8 portent sur le rendu — ouverture de modale, annulation,
affichage du toast. Les couvrir demanderait des tests bUnit rendant `ComptesAgents`, une page de
830 lignes injectant `IServiceScopeFactory`. **Ils ne sont pas couverts, et je ne prétends pas
qu'ils le soient.** Les garanties de fond — autorisation, atomicité, idempotence, déterminisme —
sont testées là où elles vivent.

### Aucun test existant modifié

**Aucune assertion existante n'a été touchée.** Les 164 remplacements de `ex.Message` n'ont fait
tomber aucun test : les suites vérifient les exceptions applicatives à la source, pas les messages
rendus à l'écran.

---

## 10. Changements de comportement

| # | Avant | Après |
|---|---|---|
| 1 | « Tout accorder » / « Tout retirer » s'exécutaient au clic | Une confirmation nommant le compte et le volume s'interpose |
| 2 | ~60 transactions indépendantes | Une seule — tout ou rien |
| 3 | Auto-retrait : tout retiré **puis** exception | `administration.comptes` conservée, opération complète, résultat explicite |
| 4 | Désactivation d'un compte au clic | Confirmation, avec la conséquence expliquée |
| 5 | Retour d'action : alerte inline | Toast, annoncé aux lecteurs d'écran |
| 6 | `ex.Message` affiché tel quel | Message métier restitué, message technique remplacé |
| 7 | Journal d'audit : toutes les entrées | 50 par page, paginées **en base** |

**Le point 3 est le seul qui change une règle observable**, et il la rend plus sûre : l'invariant
était déjà là, mais il s'appliquait par une exception au milieu d'une boucle.

---

## 11. Risques résiduels

| # | Risque | Sévérité |
|---|---|---|
| 1 | **Aucune vérification visuelle.** Modale, toast et navigation de pagination n'ont jamais été rendus. Le balisage est correct et compile ; son apparence n'est pas observée | **MEDIUM** |
| 2 | UI-02, UI-03, UI-04 restent ouverts hors des surfaces traitées | MEDIUM |
| 3 | `MessagesUtilisateur` est une **liste blanche** : une exception métier future non inscrite verra son message remplacé par le générique. C'est le sens sûr — mais il faudra y penser | LOW |
| 4 | La pagination du journal ne conserve pas la page dans l'URL : un rafraîchissement ramène en page 1 | LOW |
| 5 | Les huit UX-P2 et six UX-P3 sont **intacts**, dont le responsive mobile non vérifié | *(hors périmètre)* |

---

## 12. UX-P2 / P3 volontairement non traités

Conformément à la consigne, **aucun n'a été touché** : UI-07 à UI-20. Ils restent dans
`docs/backlog-ui-ux.md`.

Deux méritent d'être rappelés parce qu'ils conditionnent la suite :

- **UI-14** — le rendu mobile n'a jamais été vérifié. C'est la première chose à regarder dès qu'un
  environnement de recette existe.
- **UI-08** — les icônes emoji sans `aria-hidden`. *(Les deux composants que j'ai touchés en sont
  désormais pourvus, ainsi que les boutons de masse de `ComptesAgents` — mais le reste de
  l'application ne l'est pas.)*

---

## 13. Recette navigateur

La consigne autorise la création de comptes et de données **strictement de test** dans un
environnement **isolé** de recette, avec préfixe `TEST_` / `RECETTE_` / `UAT_`, adaptateurs de
notification journalisés, et interdiction absolue de toucher la production.

**Cette recette n'a pas été conduite, et ne pouvait pas l'être dans cette phase.** Le § 12 de la
même mission interdit de démarrer l'application lorsque `Program.cs` déclenche `MigrateAsync` — ce
qui est le cas — et renvoie la recette à une étape séparée. Aucun environnement isolé n'existe
aujourd'hui.

### Ce qu'elle exigera

| Prérequis | État |
|---|---|
| Environnement isolé, base dédiée, aucune connectivité production | ❌ à créer |
| Chaîne de connexion de recette *(les `appsettings` sont vides par conception)* | ❌ à fournir |
| Variable d'amorçage des comptes de démonstration | ❌ à fournir |
| Adaptateurs de notification journalisés | ✅ **déjà en place** — les trois canaux sont `CanalJournalise`, aucun envoi réel n'est possible |
| 17 profils de test à créer | ❌ |
| Données métier fictives (BL, bookings, tickets, documents, simulations, avoirs, réclamations, enrôlements) | ❌ |

**Le point favorable** : aucun SMS, WhatsApp ou e-mail ne peut partir, quelle que soit la
manipulation — les trois canaux externes sont journalisés et PC-16 n'est pas arbitré. Le risque
d'envoi accidentel vers un tiers est **nul par construction**.

---

## 14. Conclusion

Le UX-P0 est fermé, et il l'est là où il fallait : la confirmation seule aurait traité le clic
accidentel en laissant intacte la soixantaine de transactions indépendantes. C'est le § 3 de la
mission qui a imposé de regarder plus bas que l'écran, et il avait raison.

Deux composants qui n'avaient jamais servi servent désormais — et une garde empêche qu'un troisième
connaisse le même sort. C'est la sixième occurrence de cette figure dans ce projet, et la première
qui reparte avec un filet.

**Une correction de ma part s'impose au passage.** UI-05 n'était pas un défaut : le menu est filtré,
par `@switch` là où j'avais cherché des `@if`. J'ai noté la navigation 6/10 en partie sur cette base.
L'erreur est mienne, elle est corrigée ici, et je n'ai pas ajouté de code pour la couvrir.

Enfin, trois des cinq UX-P1 sont fermés sur leur surface la plus exposée et **restent ouverts
ailleurs**. Les présenter comme réglés serait faux. Ils demandent soit un mandat de refonte —
que cette mission refuse explicitement — soit, plus raisonnablement, un environnement où l'on puisse
regarder ce qu'on modifie.
