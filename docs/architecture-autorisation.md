# Architecture d'autorisation — comment décider un accès

*Document de référence pour toute contribution touchant à la sécurité.*
*À lire avant d'écrire une garde, un contrôle d'accès, ou une requête qui lit une ressource.*

---

## En une phrase

**Une identité invalide ou désactivée n'obtient aucune autorisation métier, et cette décision se
prend à un seul endroit.**

---

## Pourquoi ce document existe

Le projet a produit **quatre** défauts d'accès successifs — P0, P0-bis, P0-ter, puis la
consolidation — et les quatre ont eu la même cause : **la décision d'autorisation était réécrite là
où elle était utilisée.**

| Défaut | Où la décision avait été recopiée | Comment elle avait divergé |
|---|---|---|
| **P0** | Endpoints `/api/v1` | fail-open : le contrôle ne s'appliquait qu'aux comptes ayant une société |
| **P0-bis** | Fiches ticket et dossier | la garde était enveloppée dans `if (estClient)` : aucune branche agent |
| **P0-ter** | Page de connexion, service d'autorisation, 19 pages | l'état du compte n'entrait dans aucune décision |
| **Consolidation** | `CloisonnementService`, 12 commandes | la règle refusait les clients légitimes, et l'IHM la contournait |

Aucun de ces défauts n'était une faute d'inattention isolée. Chacun est né d'une copie qui
paraissait raisonnable au moment où elle a été écrite. **C'est le motif qu'il faut refuser, pas les
instances.**

---

## La chaîne de décision

```
                       ┌────────────────────────────┐
                       │   GardeIdentiteService     │   « ce compte peut-il obtenir
                       │   identité valide + actif  │     quoi que ce soit ? »
                       └─────────────┬──────────────┘
                     ┌───────────────┴───────────────┐
                     ▼                               ▼
       ┌──────────────────────────┐   ┌──────────────────────────────────┐
       │   AutorisationService    │   │  ControleAccesRessourceService   │
       │   « détient-il la        │   │  « cette ressource lui est-elle  │
       │     permission ? »       │   │    ouverte ? »                   │
       └────────────┬─────────────┘   └────────────────┬─────────────────┘
                    ▼                                   ▼
        92 cas d'usage applicatifs        fiches, façade de cloisonnement,
        (commandes, administration)        endpoints API, tableau de bord
```

### Trois notions, à ne jamais confondre

| Notion | Question | Qui répond |
|---|---|---|
| **Authentifié** | l'identité est-elle techniquement reconnue ? | le cookie ASP.NET Identity |
| **Actif** | le compte a-t-il encore le droit d'utiliser la plateforme ? | `GardeIdentiteService` |
| **Autorisé** | détient-il ce qu'il faut pour cette action-ci ? | `AutorisationService` / `ControleAccesRessourceService` |

**Un cookie valide ne suffit jamais.** C'est exactement la situation d'un compte désactivé pendant
que sa session vit — et c'était le P0-ter.

---

## Les cinq règles

### 1. L'identité vient du serveur, jamais de l'appelant

L'identifiant de l'utilisateur se résout depuis le contexte d'authentification. **Rien de ce que
l'interface transmet ne peut servir de preuve.**

Sont interdits comme fondement d'une décision : `clientId`, `SocieteId`, un rôle, une file, un
contexte reconstruit depuis un formulaire.

> **Ce qui s'est passé.** `FicheTicket.razor` passait `_fiche.SocieteId` comme identité cliente, pour
> contourner un contrôle qui refusait les clients légitimes. L'appelant fournissait donc la valeur à
> laquelle le contrôle se comparait — et l'auteur des messages est devenu la société, non la personne.

Une société n'est pas un compte : elle ne s'authentifie pas et n'agit pas. Si un contrôle a besoin
de la société, il la **résout** : `ControleAccesRessourceService.ResoudrePorteeSocieteAsync`.

### 2. Une seule décision générique

Le **contrôle générique** — compte valide, actif, propriété, rattachement société, ENT-03, portée de
file — appartient à `ControleAccesRessourceService`. Il n'en existe qu'une implémentation.

Une requête métier garde ses **règles propres**, et seulement elles :

```csharp
// ── 1. CONTRÔLE GÉNÉRIQUE — délégué
await controleAcces.ExigerConsultationTicketAsync(appelantId, ticket, ct);

// ── 2. CONTRÔLE MÉTIER SPÉCIFIQUE — conservé
if (!estClient && !aDroitInstruction) { throw new AccesRefuseException(...); }
```

**Acceptable** : « cet agent a-t-il `enrolement.instruire` sur cette file ? », « ce ticket est-il
dans un état qui autorise l'annulation ? »

**Interdit** : recréer localement propriété, rôle, société ou activité du compte.

### 3. Le refus par défaut ne se raisonne pas par exclusion

« Ce n'est pas un client, donc on laisse passer » est la formulation exacte du P0-bis. Une
information d'autorisation **absente** — pas de société, pas d'habilitation, pas de rattachement —
ne vaut jamais autorisation.

Chaque chemin part de « refusé » et ne concède l'accès que sur preuve positive.

### 4. L'ordre est une règle, pas un détail de rédaction

L'autorisation précède **toute autre lecture**. Un refus ne doit rien apprendre : avant la
remédiation P0-bis, un appelant non autorisé déclenchait une requête et recevait un message citant
un identifiant interne.

De même, `AutorisationService` consulte la garde **avant** les habilitations. Interroger d'abord les
habilitations ferait de l'état du compte un correctif au lieu d'une condition. Un test de câblage
compare les positions dans le fichier.

### 5. L'interface n'est jamais la frontière

Une garde Razor (`if (!contexte.EstAuthentifie)`) est une **commodité d'affichage**. Elle doit rester
cohérente avec le serveur — ne pas montrer un bouton pour une action interdite — mais aucune
décision de sécurité ne peut en dépendre.

> **Ce qui s'est passé.** Au P0-ter, la protection réelle vivait dans les 50 pages qui consultaient
> `EstAuthentifie`. Les 19 autres ne le faisaient pas, dont les dix pages d'administration.

---

## Comment écrire une nouvelle surface

**Une commande** → appeler `IAutorisationService.ExigerAsync(appelantId, permission, fileId)`.
Passer la file quand la ressource en a une : `null` ne signifie pas « toute file », il restreint aux
habilitations non bornées.

**Une lecture de ressource de dossier** → appeler
`ControleAccesRessourceService.ExigerConsultationTicketAsync` ou `…DossierAsync`.

**Une lecture de ressource de société** (une simulation, par exemple) → appeler
`ExigerConsultationRessourceDeSocieteAsync(utilisateurId, societeProprietaire, auteurRessource)`.

**Une surface « mes données »** (mes notifications, mes préférences) → la portée ne remplace pas la
garde : consulter `GardeIdentiteService.CompteEstUtilisableAsync`. La question « à quelles données ? »
n'est pas la question « ce compte a-t-il encore le droit d'être là ? ».

**Une voie d'ouverture de session** → appeler `RegleAccesConnexion.AccesAutoriseAsync`. Ne pas
réécrire la règle : elle a vécu en deux exemplaires, et l'un des deux n'avait pas le contrôle
d'activité.

---

## Ce qui protège ces règles

| Suite | Ce qu'elle empêche |
|---|---|
| `CablageControleAccesTests` | qu'une garde soit retirée d'une fiche, qu'une règle soit réécrite localement, que l'ordre change |
| `EtatCompteEtAutorisationTests` | qu'un compte désactivé ou inconnu obtienne une permission |
| `IdentiteAppelanteEtCloisonnementTests` | qu'une identité vienne de l'appelant |
| `CloisonnementAgentFichesTests` · `ApiCloisonnementTests` | les régressions de cloisonnement agent et client |
| `GardeTableauDeBordAgentTests` | qu'une requête décide seule d'un périmètre |
| `ServicesAtteignablesTests` | qu'une fonction soit livrée sans être atteignable |

**Ces tests lisent parfois les sources.** C'est délibéré : le projet a produit deux fois une règle
écrite, testée, déclarée conforme — et sans effet. `MarquerEtatEngageant()` n'avait aucun appelant ;
la garde des fiches n'avait pas de branche agent. Dans les deux cas aucun test ne tombait, parce
qu'ils éprouvaient le comportement là où la condition était posée à la main.

**Toute modification d'une garde doit être validée par mutation** : retirer l'appel, vérifier qu'au
moins un test fonctionnel **et** un test de câblage tombent, restaurer.

---

## Points ouverts

| Sujet | État |
|---|---|
| `ValidationInterval = TimeSpan.Zero` | une lecture du compte par requête — voir `docs/remediation-P3.md` § Performance |
| Filtre des notes internes | sur la **nature** pour ticket et enrôlement, sur la **permission** pour avoir/exonération |
| `NatureUtilisateur` | ne distingue pas le transporteur du client (RG-CPT-08) |
| Journalisation | connexions tracées ; aucune alerte sur répétition d'échecs |
