# Revue de convergence avant audit final

*Mode AUDIT / READ ONLY. Aucun code, test, allowlist ou migration n'a été modifié.*
*Aucun arbitrage métier n'est pris ici.*

---

## 1. Résumé exécutif

**La fonctionnalité de clôture de compte ajoutée en parallèle est saine.** Sa revue complète —
page, commande, appelant, effets de bord, quatorze scénarios — ne révèle **aucune faille**. Le
`[AllowAnonymous]` est **justifié** : la page est purement statique et ne s'affiche qu'après que le
compte a été désactivé et sa session révoquée. Un `[Authorize]` y renverrait l'utilisateur vers la
connexion, et il ne verrait jamais la confirmation.

L'échec de `SurfaceRoutableTests` n'est donc **pas un défaut de sécurité** mais une décision
d'inscription qui n'a pas été prise. Elle revient à l'auteur de la fonctionnalité, pas à moi.

**Deux constats méritent l'attention, aucun n'est bloquant :**

- La clôture repose sur la rotation du tampon de sécurité **et** sur
  `ValidationInterval = TimeSpan.Zero`. Or ce réglage est précisément celui dont le rapport P3
  analyse l'optimisation possible. **Le relever ferait de la phrase « vos sessions ont été
  déconnectées » une promesse fausse** pendant l'intervalle retenu. Ce couplage n'est écrit nulle
  part.
- La commande valide l'audit et la désactivation du rattachement **avant** la désactivation
  Identity, dans deux transactions distinctes. Un échec entre les deux laisse une trace de clôture
  sans clôture.

**RG-CPT-08 n'est pas une correction locale.** Ajouter `NatureUtilisateur.Transporteur` sans lot
dédié produirait **au moins deux régressions de sécurité** : les notes internes servies aux
transporteurs sur les fiches ticket et enrôlement, et l'accès transporteur à cinq pages de file
agent.

---

## 2. État du worktree

Le dépôt était **déjà largement modifié avant cette séquence** : 283 fichiers modifiés, 106 non
suivis, 7 supprimés. La majorité (modules Assistant, Exports, référentiels 3FN, migrations du
2026-09-04) précède mon intervention.

Le tri repose sur l'horodatage des fichiers, la fenêtre de mes remédiations P2/P3 étant clairement
identifiable.

### A. Remédiations de cette séquence

| Horaire | Périmètre |
|---|---|
| ~17:00–17:55 | P2-02 (tarification), P2-03 (journal de connexion), P2-04 (état engageant), P2-05 (RG-SIM-12), P2-06 (surfaces personnelles) |
| 17:52–18:00 | R-15 — script de reprise, jeu de répétition, répétition sur LocalDB |
| 18:17 | Partage de `ComptesDeTest` — 112 fichiers de test réécrits |
| 18:29–18:38 | P3 — `RegleAccesConnexion`, portée de file Avoir/Exo, fail-closed des notes internes, deux `CS9113`, corpus de relais de `ServicesAtteignablesTests` |
| 18:41–18:44 | `docs/architecture-autorisation.md`, `docs/remediation-P3.md` |

### B. Modifications parallèles, non attribuables à cette séquence

| Horaire | Fichier | Nature |
|---|---|---|
| 18:13 | `Application/Comptes/CloturerMonCompteCommand.cs` | **créé** |
| 18:14 | `Application.Tests/Comptes/CloturerMonCompteCommandTests.cs` | **créé** — 9 tests |
| 18:17 | `Application.Tests/Tickets/ClotureArchivageTests.cs` | créé ou modifié |
| 18:18 | `Web/Components/Pages/Comptes/CompteCloture.razor` | **créée** |
| 18:21 | `Web/Components/Pages/Comptes/MonProfil.razor` | modifiée — modale de clôture |

Ces fichiers encadrent mes propres modifications dans le temps ; je n'y ai pas touché, et la revue
qui suit les traite comme du code tiers.

### C. Origine incertaine

`tests/CIT.Plateforme.Application.Tests/Tickets/ClotureArchivageTests.cs` porte l'horodatage 18:17,
soit la minute exacte de ma passe de réécriture des 112 fichiers de test. Son **contenu** relève de
la clôture (groupe B) ; son horodatage peut ne refléter qu'une réécriture de namespace de ma part.
**Impossible de trancher sans l'historique Git**, et le fichier n'est pas suivi.

Aucune migration EF n'a été créée ni exécutée durant cette séquence. Les seuls scripts ajoutés sont
`scripts/reprise-r15/`.

---

## 3. Modifications concurrentes

La fonctionnalité de clôture de compte forme un ensemble cohérent et **terminé** : commande,
tests (9), page de confirmation, interface d'appel. Elle n'est pas laissée à mi-chemin.

Un seul point est resté ouvert par son auteur : l'inscription de `CompteCloture` dans la liste des
pages publiques assumées de `SurfaceRoutableTests`.

---

## 4. Revue CompteCloture

### La page — `CompteCloture.razor`

**Elle ne fait rien.** C'est le fait central de cette revue, et il ne se lit pas dans l'attribut :

- aucun bloc `@code` ;
- aucun paramètre de route, aucune chaîne de requête, aucun jeton ;
- aucune injection de service, aucune lecture de données ;
- du texte statique et un lien vers `/comptes/connexion`.

Le contenu affiché est **générique** : « Votre compte a été clôturé », une mention de conservation
légale de l'historique, un bouton de retour. Il ne cite ni nom, ni identifiant, ni société, ni date.
Un anonyme qui ouvre l'URL n'apprend rien qu'il ne sache déjà.

### La commande — `CloturerMonCompteCommand`

Le contrat de requête est **anti-IDOR par construction** :

```csharp
public sealed record CloturerMonCompteRequete(string MotDePasseConfirmation, string? Motif);
```

**Aucun identifiant.** L'utilisateur cible est le paramètre `appelantId`, que l'appelant résout
depuis le contexte serveur. Il n'existe pas de champ à forger.

Contrôles, dans l'ordre du code :

| # | Contrôle | Effet |
|---|---|---|
| 1 | Mot de passe de confirmation non vide | `ArgumentException` |
| 2 | Compte existant | `AccesRefuseException` |
| 3 | `info.EstActif` | `InvalidOperationException` — déjà clôturé |
| 4 | `info.Nature == Client` | `InvalidOperationException` — les comptes internes CIT relèvent de l'administration |
| 5 | Mot de passe valide (`UserManager.CheckPasswordAsync`) | `AccesRefuseException` |
| 6 | CLAR-03 — `DecrementerAdministrateursActifs()` | `DernierAdministrateurActifException` si dernier admin actif |
| 7 | Rattachement désactivé, audit consigné, `DefinirActivationAsync(false)` | rotation du tampon → sessions coupées |

### L'appelant — `MonProfil.razor`

`@page "/comptes/mon-profil"` · `@attribute [Authorize]` · garde `!_contexte.EstAuthentifie` ·
la zone de clôture n'est rendue que si `_contexte?.Nature == NatureUtilisateur.Client`.

L'identité transmise est `_contexte.UtilisateurId`, issue de `ObtenirContexteAsync()` — **résolue
côté serveur**, conformément à la règle 1 de `docs/architecture-autorisation.md`.

---

## 5. Analyse sécurité clôture compte

### Les quatorze scénarios

| # | Scénario | Verdict | Preuve |
|---|---|---|---|
| 1 | Utilisateur authentifié clôture son compte | **AUTORISÉ** | `MonProfil` `[Authorize]` + `EstAuthentifie` + `Nature == Client` ; commande contrôles 1→7 |
| 2 | Anonyme ouvre directement l'URL | **AUTORISÉ (sans effet)** | page statique, aucun `@code`, aucune donnée — rien ne se produit |
| 3 | A tente de clôturer B | **REFUSÉ** | `CloturerMonCompteRequete` ne porte aucun identifiant ; la cible est `appelantId` |
| 4 | Identifiant forgé | **NON EXPRIMABLE** | aucun champ d'identité dans le contrat |
| 5 | Jeton absent | **SANS OBJET** | le flux n'utilise aucun jeton |
| 6 | Jeton invalide | **SANS OBJET** | idem |
| 7 | Jeton expiré | **SANS OBJET** | idem |
| 8 | Jeton réutilisé | **SANS OBJET** | idem |
| 9 | Compte déjà clôturé | **REFUSÉ** | `if (!info.EstActif) throw` — couvert par `CompteDejaInactif_EstRefuse` |
| 10 | Compte désactivé | **REFUSÉ** | même contrôle ; et la session serait déjà rejetée en amont |
| 11 | Compte administrateur | **REFUSÉ (interne CIT)** · **REFUSÉ si dernier admin société** · autorisé sinon | `Nature != Client` ; `DecrementerAdministrateursActifs()` — couverts par `AgentCit_EstRefuseEnLibreService` et `DernierAdministrateurActif_EstRefuse…` |
| 12 | CSRF | **SANS OBJET** | Blazor Server : `@onclick` sur un circuit SignalR authentifié, non un `POST` de formulaire forgeable depuis un autre site |
| 13 | Navigation directe vers la page | **AUTORISÉ (sans effet)** | cf. scénario 2 |
| 14 | Session existante après clôture | **RÉVOQUÉE** | `DefinirActivationAsync` → `UpdateSecurityStampAsync` ; `ValidationInterval = TimeSpan.Zero` rejette le cookie à la requête suivante ; `NavigateTo(..., forceLoad: true)` **est** cette requête |

Le mot de passe est exigé et vérifié : la clôture ne peut pas être déclenchée par un tiers ayant
momentanément accès à une session ouverte.

### Deux observations, non bloquantes

**a. Un couplage non documenté avec un réglage de performance.**
La révocation de session repose sur la conjonction de deux mécanismes : la rotation du tampon
(dans la commande) et `ValidationInterval = TimeSpan.Zero` (dans `Program.cs`).

Le rapport P3 analyse précisément l'optimisation de ce second réglage. **Le relever rendrait la
phrase affichée — « vos sessions ont été déconnectées » — fausse pendant l'intervalle retenu.**
Rien, dans le code ni dans les commentaires, ne signale cette dépendance à qui envisagerait
l'optimisation.

**b. Deux transactions au lieu d'une.**
`uniteDeTravail.EnregistrerAsync()` valide l'audit et la désactivation du rattachement ; puis
`comptes.DefinirActivationAsync(...)` effectue une écriture Identity **séparée**. Un échec entre les
deux laisse : un audit disant « clôturé », un rattachement désactivé, et un compte toujours actif.

Ni l'un ni l'autre n'ouvre un accès non autorisé. Ce sont des points de conception à signaler à
l'auteur.

**c. Hygiène mineure.** `_motDePasseCloture` n'est pas vidé après usage.

---

## 6. Décision AllowAnonymous

> ### A. `[AllowAnonymous]` **JUSTIFIÉ**

**Pourquoi, et non à partir de l'attribut.**

La séquence est : l'utilisateur confirme → le compte est désactivé → le tampon tourne → la
navigation `forceLoad: true` déclenche une requête HTTP → le cookie est rejeté → **l'utilisateur
arrive sur la page en état anonyme**.

Une page `[Authorize]` le renverrait alors vers `/comptes/connexion`. Il n'aurait **aucune
confirmation** que sa demande a abouti, et se retrouverait devant un formulaire dont ses
identifiants ne fonctionnent plus. C'est un chemin sans issue lisible.

Le caractère public est donc **une conséquence de la fonctionnalité**, non une facilité.

Et il est sans risque, parce que la page ne fait rien : pas de paramètre, pas de donnée, pas
d'action, un contenu générique. Ouvrir l'URL sans être concerné n'apprend rien et ne déclenche rien.

### Pourquoi elle pourrait légitimement être inscrite

`SurfaceRoutableTests` n'accepte pas `[AllowAnonymous]` comme preuve — délibérément : *« l'ouverture
au public devient une décision explicite, jamais un oubli »*. La liste porte des entrées nommées,
chacune avec sa raison.

Un précédent y figure déjà, et il est exactement analogue :

```
"VerifierCode",  // second facteur, avant session complète
```

Une page atteinte alors que la session n'est pas (ou n'est plus) complète. `CompteCloture` relève de
la même catégorie, de l'autre côté du cycle de vie : **après** que la session a cessé.

La raison qui pourrait être inscrite : *« confirmation de clôture, affichée après révocation de la
session »*.

**Je ne l'inscris pas.** La consigne l'interdit, et surtout : qualifier de « publique assumée »
l'écran d'une fonctionnalité qui n'est pas la mienne reviendrait à prendre une décision de sécurité
à la place de son auteur. Mon rôle s'arrête à établir que la décision serait fondée.

**Risque sécurité de la page : AUCUN.**

---

## 7. Impact RG

Aucune règle nouvelle n'est créée. Quatre règles existantes sont touchées, aucune ne change de
statut.

| RG | Impact | Ancien statut | Statut probable | Preuve |
|---|---|---|---|---|
| **RG-SEC-03** | La clôture est consignée (`CompteClotureParUsager`) dans le journal append-only | 🟡 (connexion tracée depuis P2-03) | 🟡 **inchangé** | `ConsignerAuditAsync` + `JournalAuditAppendOnlyGuard` |
| **RG-CPT-06** | Un administrateur de société ne peut pas laisser l'entreprise orpheline | ✅ | ✅ **inchangé, renforcé** | `DecrementerAdministrateursActifs()` (CLAR-03) |
| **RG-CPT-08** | La clôture **confirme** que le transporteur est un `Nature.Client` : elle lui est ouverte au même titre qu'au client | 🟡 | 🟡 **inchangé** | `if (info.Nature != NatureUtilisateur.Client) throw` ; test `Nominal_TransporteurSansSociete_…` |
| **RG-TCK-06** | La page **promet** à l'usager que l'historique reste archivé | 🟡 | 🟡 **inchangé** | La promesse est tenue en pratique — les tickets clôturés restent consultables — mais l'archivage n'est toujours pas matérialisé comme état. **L'écart entre la promesse affichée et le modèle mérite d'être connu.** |

**RG-SEC-01 / RG-SEC-02** : non affectées. La clôture ne crée aucun chemin d'accès ; elle en
supprime un.

---

## 8. RG-CPT-08

> **RG-CPT-08** — « Un compte transporteur/chauffeur est un type de compte distinct du compte
> client, dédié au module Enrôlement Transporteurs. »

### Le modèle actuel

`NatureUtilisateur` ne comporte que `Client` et `Agent`. Un transporteur est un `Client` **sans
rattachement société**. Sa distinction repose aujourd'hui sur :

- un parcours d'inscription dédié — `InscrireTransporteurCommand` ;
- l'absence de rattachement, que `VerifierAccesCompteQuery` reconnaît explicitement (« une personne
  physique qui n'a jamais déposé de demande de société ») ;
- un module dédié — Enrôlement — et des dossiers **personnels** (`Dossier.ClientId` porte
  l'utilisateur, non une société).

La distinction existe donc **fonctionnellement**. Ce qui manque, au regard du texte, c'est qu'elle
ne soit pas **nommée** : elle se déduit d'une absence.

### Analyse d'impact si `NatureUtilisateur.Transporteur` était ajouté

23 usages en production. Le clivage est net.

**Groupe 1 — sans impact** (la question posée est « est-ce un agent interne ? ») :

| Fichier | Condition | Impact |
|---|---|---|
| `Agent/ObtenirDashboardAgentQuery` | `!= Agent` → vue vide | aucun |
| `Securite/ControleAccesRessourceService` (×2) | `!= Agent` → branche client ; `== Agent` → branche agent | aucun — le transporteur irait dans la branche client, ce qui est correct |
| `Notifications/Resolveurs/ResolveurCollaborateurMentionne` | `!= Agent` | aucun |
| `Web/ObtenirContexteUtilisateurQuery` (espaces, superviseur) | `== Agent` | aucun |
| `Identity/AdministrationComptesRepository` | `== Agent` (liste des agents) | aucun |
| `Pages/Agent/DashboardAgent.razor` | `!= Agent` | aucun |
| `Pages/AvoirExoneration/FicheAvoirExoneration.razor` | `== Agent \|\|` | aucun |

**Groupe 2 — RÉGRESSION DE SÉCURITÉ** (la question posée est « est-ce un client externe ? ») :

| Fichier | Condition actuelle | Comportement actuel | Impact si `Transporteur` ajouté | Modification nécessaire |
|---|---|---|---|---|
| **`Tickets/ObtenirFicheTicketQuery`** | `estClient = Nature == Client` | filtre les notes internes | **le transporteur reçoit la vue AGENT, notes internes comprises** | traiter Transporteur comme externe |
| **`Enrolement/ObtenirFicheEnrolementQuery`** | idem | idem | **idem — sur le module dont il est l'usager principal** | idem |
| **5× `Pages/BillingExport/File*.razor`** | `Nature == Client` → refus | ferme les files agent aux clients | **le transporteur accède à cinq pages de file agent** | idem |

**Groupe 3 — régression fonctionnelle** :

| Fichier | Impact si `Transporteur` ajouté |
|---|---|
| `Comptes/CloturerMonCompteCommand` (`!= Client` → refus) | le transporteur ne pourrait plus clôturer son compte |
| `Messagerie/MentionnerCollaborateurCommand` (`== Client` → refus) | un transporteur deviendrait mentionnable dans une note interne |
| `Web/ObtenirContexteUtilisateurQuery` (`userSoc is not null \|\| == Client`) | l'espace Client ne lui serait plus proposé |
| `Pages/Comptes/MonProfil.razor` (`== Client`) | la zone de clôture lui serait masquée |
| `Pages/{Tickets,Dossiers,Enrolement}/Fiche*.razor` (`_estClient`) | traité comme agent dans l'interface |

**Rayon d'explosion : 13 sites, dont 7 à conséquence de sécurité.**

### La cause du risque

Le code n'exprime nulle part la notion d'**usager externe**. Il l'écrit `Nature == Client`, ce qui
confond « n'est pas un agent » et « est un client d'entreprise ». Ajouter une troisième nature ne
crée pas le problème : **il le révèle**.

Une correction sûre commencerait donc par nommer cette notion — un prédicat unique, du type
`EstUsagerExterne`, substitué aux 13 sites — **avant** d'introduire la valeur d'énumération. C'est
exactement la démarche des remédiations P0 → consolidation : nommer la règle une fois, puis la faire
varier.

---

## 9. Décision RG-CPT-08

> ### C. **Changement de modèle nécessitant un lot dédié**

Ce n'est ni une correction locale (13 sites, dont 7 sensibles), ni une évolution transversale
maîtrisable en l'état (elle traverse la chaîne d'autorisation tout juste consolidée).

**Un préalable existe, et il est de nature métier** : le texte exige un « type de compte distinct ».
La distinction fonctionnelle existe déjà — parcours d'inscription, absence de société, module dédié.
Reste à savoir si le CDC exige qu'elle soit **nommée dans le modèle**, ou si la distinction
opérationnelle suffit. **Cette question ne m'appartient pas.**

Si la réponse est « nommée dans le modèle », le lot devrait, dans cet ordre :

1. introduire `EstUsagerExterne` et l'appliquer aux 13 sites, **à comportement constant** — vérifiable
   par la suite existante ;
2. seulement ensuite ajouter `NatureUtilisateur.Transporteur` ;
3. couvrir les 7 sites sensibles par des tests de cloisonnement dédiés, validés par mutation.

**Non implémenté.**

---

## 10. RG-COM-01 / PC-16

> **RG-COM-01** — « le client est notifié via e-mail, notification portail, et au moins un canal
> mobile (WhatsApp et/ou SMS) ; chaque notification comporte un lien direct vers le ticket. »

| Élément | État | Nature |
|---|---|---|
| Port `ICanalNotification` | ✅ | **code prêt** |
| Trois canaux enregistrés en DI (Email, SMS, WhatsApp) | ✅ | **code prêt** |
| Résolution des destinataires, gabarits, interpolation, file de reprise | ✅ | **techniquement implémenté** |
| Notification portail (In-App) | ✅ | **canal activé** — l'existence de la notification est sa diffusion |
| Lien direct vers le ticket (`ILienPortail`) | ✅ | **techniquement implémenté** |
| Email / SMS / WhatsApp | `CanalJournalise` — écrit dans le journal, n'envoie rien | **canal NON activé** |
| Choix du fournisseur, contrat, coûts | ✗ | **décision métier manquante (PC-16)** |

**Rien ne relève du code.** Le commentaire de `DependencyInjection.cs` l'assume : *« implémentation
JOURNALISÉE, AUCUN FOURNISSEUR (research.md D10, PC-16) »*.

Le seul défaut technique serait de **présenter la règle comme satisfaite** : aujourd'hui, aucun
canal mobile n'atteint réellement le client. Le statut 🟡 le dit.

**PC-16 : OUVERT** — arbitrage DSI/Finance. Non pris ici.

---

## 11. RG-IA-01 / IA-02

> **RG-IA-01** — « L'assistant répond en français aux questions de premier niveau, oriente le client
> dans ses démarches et **l'aide à compléter ses dossiers**. »
>
> **RG-IA-02** — « L'assistant escalade vers un opérateur humain uniquement lorsque nécessaire, sans
> perte de contexte (le fil de conversation reste attaché au ticket). »

### Comportement réel

`AssistantSimule` — **8 sujets**, chacun déclenché par une liste de mots :

```csharp
if (mots.Any(m => normalisee.Contains(m, StringComparison.Ordinal)))
```

Hors de ces huit sujets, il ne devine pas : il renvoie une réponse d'escalade
(`EscaladeRecommandee: true`) plutôt qu'une approximation. C'est un comportement honnête, et il est
délibéré.

### Confrontation, ligne à ligne

| Exigence | État | Analyse |
|---|---|---|
| Répond en français aux questions de premier niveau | 🟡 | Répond sur **8 sujets pré-écrits**. Un appariement de sous-chaînes n'est pas une compréhension : une question formulée autrement, ou portant sur un neuvième sujet, n'obtient rien |
| **Oriente** le client dans ses démarches | 🟡 | Les 8 réponses orientent, dans leur périmètre |
| **L'aide à compléter ses dossiers** | ❌ | **Non rendu.** Aucun accès au dossier, aucune lecture de son état, aucune assistance à la saisie |
| **RG-IA-02** — escalade sans perte de contexte | ✅ | `EscaladerAssistantVersOperateurCommand`, appelée par `AssistantVirtuel.razor:202`, transmet le fil intégral en message client ; conversation persistée, rattachée au ticket |

**Architecture prête ≠ capacité de production.** Le port `IAssistantConversationnel` est propre, et
substituer un vrai moteur n'exige aucune refonte. Mais le service **rendu au client** aujourd'hui
est celui de huit réponses par mots-clés.

### Ce qu'il reste

| Besoin | Type |
|---|---|
| Choix du moteur / fournisseur (hébergement, données, coût, langue) | **arbitrage métier** |
| Adaptateur `IAssistantConversationnel` réel | **implémentation** — bornée, le port existe |
| Accès au dossier pour l'aide à la complétion (RG-IA-01) | **implémentation** — nouvelle capacité, non un adaptateur |
| Garde-fous, journalisation, limites de coût | **configuration** |

**Assistant IA production : NON PRÊT.** RG-IA-02 est satisfaite ; RG-IA-01 ne l'est pas.

---

## 12. Risques résiduels

Reprise des 8 risques du rapport P3.

| # | Risque | État | Classement |
|---|---|---|---|
| **1** | `CompteCloture` non inscrite dans la liste des pages publiques | **Ouvert** — la revue établit que l'inscription serait fondée ; la décision revient à l'auteur | **DECISION REQUIRED** — bloque la suite verte, **pas** la sécurité |
| **2** | Mécanisme 2FA d'Identity non éprouvé de bout en bout | Ouvert — la **règle** l'est désormais (`RegleAccesConnexionTests`) ; le mécanisme tiers ne l'est pas | **LOW** — dette de test sur du code d'infrastructure |
| **3** | `ConsulterFileAvoirExonerationQuery` conserve `fileId: null` | Ouvert — sans effet, module mono-file | **ACCEPTED** |
| **4** | Asymétrie nature / permission du filtre des notes internes | Ouvert, **délibérément** — unifier restreindrait des droits sans base au CDC | **ACCEPTED** |
| **5** | `ValidationInterval = TimeSpan.Zero` non mesuré | Ouvert — **et cette revue lui ajoute un enjeu** : la clôture de compte en dépend pour sa promesse de déconnexion | **MEDIUM** (était LOW) — toute optimisation devra tenir compte de ce couplage |
| **6** | RG-TCK-06 (archivage) et RG-CPT-08 (nature) | Ouverts — écarts fonctionnels. RG-TCK-06 gagne un enjeu : la page de clôture **promet** l'archivage à l'usager | **DECISION REQUIRED** (RG-CPT-08) · **MEDIUM** (RG-TCK-06) |
| **7** | RG-COM-01 (PC-16) et RG-IA-01 | Ouverts — arbitrages métier, rien de technique en attente pour PC-16 | **DECISION REQUIRED** |
| **8** | R-15 non confronté aux données de production | Ouvert | **BLOCKER** — pour le déploiement uniquement |

**Deux risques nouveaux, issus de cette revue :**

| # | Risque | Classement |
|---|---|---|
| **9** | La clôture valide l'audit et le rattachement **avant** la désactivation Identity, en deux transactions. Un échec intermédiaire laisse une trace de clôture sans clôture | **LOW** — à signaler à l'auteur |
| **10** | Fragilité de la suite en exécution parallèle : contention LocalDB entre projets (3 échecs transitoires observés). Les mêmes suites passent en série | **LOW** — un `xunit.runner.json` limitant le parallélisme suffirait |

**Total : 1 BLOCKER · 0 HIGH · 3 MEDIUM · 4 LOW · 2 ACCEPTED · 3 DECISION REQUIRED**
*(le total dépasse 10 : les risques 6 et 7 portent chacun deux règles de classements distincts)*

---

## 13. R-15

Aucune modification, aucune exécution durant cette revue.

| Point | État |
|---|---|
| Validation sur SQL Server (LocalDB), schéma pré-3FN monté par EF | ✅ réalisée |
| Scénario nominal — 0 ligne non résolue, comptages inchangés | ✅ validé |
| Idempotence — seconde exécution sans écriture | ✅ validé |
| Rollback sur données incohérentes — refus explicite, base intacte | ✅ validé |
| Chaîne EF — `database update` poursuit après reprise | ✅ validé |
| **Copie des données de production** | ❌ **non testée** |

> ### R-15 : **GO SOUS CONDITIONS**

La condition est celle qu'énonce le rapport R-15 lui-même : confronter les valeurs réellement
présentes en base aux correspondances d'ordinaux de la section E, sur une copie restaurée, **avant**
toute exécution réelle.

---

## 14. État des tests

| Projet | Résultat |
|---|---|
| `Domain.Tests` | 208 / 208 |
| `Application.Tests` | 732 / 732 |
| `Infrastructure.Tests` | 139 / 139 |
| `Web.Tests` | **313 / 314** |
| **Total** | **1 392 / 1 393** |

### Qualification de l'échec

| Type | Constat |
|---|---|
| Produit par les remédiations | **Aucun** |
| Préexistant | **Aucun** |
| **Dû à une modification parallèle** | **1** — `SurfaceRoutableTests.Toute_page_routable_est_protegee_ou_volontairement_publique`, sur `CompteCloture` |
| Environnemental / transitoire | 3 observés en exécution parallèle des quatre projets (contention LocalDB) ; **passent en série** |

**Ce résultat ne doit pas être présenté comme « globalement vert ».**

L'échec est **réel** : une garde d'architecture signale une page routable dont l'ouverture au public
n'a pas été décidée explicitement. Que la revue conclue à une ouverture légitime **ne rend pas le
test vert** — il attend une décision inscrite, pas une opinion.

Tant que cette décision n'est pas prise, la suite est **en échec**.

---

## 15. GO / NO-GO audit final

> ### 🟠 **GO AVEC RÉSERVES**

L'audit des 98 règles peut être conduit. Le périmètre est stable, les remédiations sont documentées,
et les statuts probables des règles touchées par la fonctionnalité concurrente sont établis au § 7 —
aucun ne change.

**Réserves à porter dans l'audit :**

1. La suite n'est pas verte. L'audit devra le mentionner, et ne pas traiter `SurfaceRoutableTests`
   comme un détail : c'est une garde d'architecture qui fait son travail.
2. Trois règles dépendent d'arbitrages non rendus — RG-CPT-08, RG-COM-01, RG-IA-01. Elles ne peuvent
   pas être notées autrement que 🟡 tant que les décisions ne sont pas prises, et l'audit ne doit pas
   les trancher techniquement.
3. La fonctionnalité de clôture n'a pas d'exigence RG dédiée dans le CDC. L'audit ne doit **pas**
   créer de règle pour elle.

---

## 16. GO / NO-GO production

> ### 🟠 **GO SOUS CONDITIONS**

Aucun obstacle de sécurité ne subsiste : P0, P0-bis, P0-ter et la consolidation sont fermés, la
chaîne d'autorisation est unique, et la fonctionnalité ajoutée en parallèle ne l'entame pas.

Ce qui reste tient à trois choses : **une décision non prise, une validation non faite, et des
arbitrages métier en attente.** Aucune n'exige de développement.

---

## 17. Conditions restantes

```
[ ] 1. Arbitrage CompteCloture
       Inscrire la page dans la liste des pages publiques assumées de SurfaceRoutableTests,
       avec sa raison — ou décider qu'elle doit porter [Authorize] et revoir le flux.
       Décision de l'auteur de la fonctionnalité. La revue établit que l'inscription serait fondée.

[ ] 2. Suite globale 100 % verte
       Conséquence directe du point 1. Aucun autre échec n'est ouvert.

[ ] 3. R-15 confronté à une copie de production
       Restaurer une copie, confronter les valeurs réelles aux correspondances d'ordinaux
       (section E du script), exécuter, lire le journal, contrôler deux simulations historiques.
       BLOQUANT pour tout déploiement sur base peuplée.

[ ] 4. Arbitrage PC-16
       Fournisseur e-mail / SMS / WhatsApp. Sans lui, RG-COM-01 reste 🟡 :
       aucun canal mobile n'atteint réellement le client.

[ ] 5. Décision assistant IA
       Moteur et fournisseur (RG-IA-01). RG-IA-02 est satisfaite.
       Décider aussi si « aider à compléter ses dossiers » entre dans le périmètre livré.

[ ] 6. Décision RG-CPT-08
       Le CDC exige-t-il une nature NOMMÉE dans le modèle, ou la distinction opérationnelle
       actuelle suffit-elle ? Si nommée : lot dédié, 13 sites dont 7 sensibles.
```

**Points signalés à l'auteur de la clôture de compte — non bloquants :**

- le couplage entre la promesse « vos sessions ont été déconnectées » et
  `ValidationInterval = TimeSpan.Zero` ;
- la validation en deux transactions ;
- `_motDePasseCloture` non vidé après usage.

Rien d'autre n'est ajouté. En particulier, ni la mesure de charge de `ValidationInterval`, ni la
limitation du parallélisme des tests, ni RG-TCK-06 ne sont des conditions de mise en production :
ce sont des dettes, et elles sont documentées comme telles.

---

## 18. Conclusion

La fonctionnalité arrivée en cours de route ne dégrade rien. Sa revue complète — page, commande,
appelant, quatorze scénarios — ne trouve aucune faille, et son `[AllowAnonymous]` est justifié par
le flux, non par commodité.

L'unique échec de la suite n'est pas un défaut : c'est une garde qui exige qu'une ouverture au
public soit **décidée**, et la décision n'a pas été prise. Le corriger en inscrivant moi-même la
page reviendrait à répondre à sa place — et cette séquence entière a montré ce que coûte une
décision de sécurité prise par quelqu'un qui n'en portait pas la responsabilité.

Trois enseignements de cette revue :

- **Un attribut ne dit pas si une page est sûre.** `[AllowAnonymous]` était le point de départ de
  l'inquiétude ; c'est la lecture du flux — une page sans `@code`, atteinte après révocation — qui
  l'a levée. L'inverse est vrai aussi : un `[Authorize]` n'aurait rien prouvé.
- **Une nouvelle nature d'utilisateur ne crée pas le risque, elle le révèle.** Les 13 sites qui
  écrivent `Nature == Client` confondent depuis toujours « n'est pas un agent » et « est un client
  d'entreprise ». C'est cette confusion qu'il faut nommer avant d'ajouter quoi que ce soit.
- **Une optimisation peut casser une promesse écrite ailleurs.** Relever `ValidationInterval`
  rendrait fausse une phrase affichée à l'usager sur un écran qu'aucun développeur n'ouvrirait en y
  travaillant.

Le score final n'est pas recalculé.
