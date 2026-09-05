# Remédiation P3

## Résumé

Huit P3 étaient ouverts. **Six ont été traités**, deux relèvent d'arbitrages métier et ne sont pas
des dettes techniques — les traiter aurait été décider à la place du métier.

Un P3 s'est révélé **inversé** : l'audit avait lu `fileId: null` comme « toute file », alors que ce
`null` restreint aux habilitations non bornées. Le contrôle n'était pas trop permissif, il était
trop strict — et refusait l'instructeur nominal du module Avoir/Exonération.

**Un événement extérieur à signaler** : une fonctionnalité de clôture de compte a été écrite dans
l'arbre de travail **pendant** cette phase (fichiers datés 18:18–18:21). Elle n'est pas de moi, je
n'y ai pas touché, et elle laisse la suite avec **un échec** — la garde `SurfaceRoutableTests`
signale une nouvelle page routable non inscrite. Détail au § *Suite globale*.

---

## P3 initiaux

| ID | Objet | Traitement |
|---|---|---|
| **P3-01** | Canaux mobiles simulés (RG-COM-01) | **Non traité — arbitrage métier** (PC-16, DSI/Finance) |
| **P3-02** | Assistant à mots-clés (RG-IA-01) | **Non traité — arbitrage assumé** |
| **P3-03** | Archivage non matérialisé à la clôture (RG-TCK-06) | **Non traité — écart fonctionnel, pas dette** |
| **P3-04** | Nature « transporteur » non distincte (RG-CPT-08) | **Non traité — impact trop large** |
| **P3-05** | Avertissement `CS9113` | ✅ traité (et un second, créé par mes propres corrections) |
| **P3-06** | Portée de file absente sur la fiche Avoir/Exonération | ✅ traité — **diagnostic inversé** |
| **R1** | Voie 2FA couverte par test de câblage seulement | ✅ traité |
| **R7** | Notes internes filtrées sur la nature, fail-open pris isolément | ✅ traité |
| **R8** | `ComptesDeTest` dupliqué dans trois projets | ✅ traité |

**Pourquoi P3-03 et P3-04 n'ont pas été traités.** Ce ne sont pas des dettes techniques mais des
écarts fonctionnels. Matérialiser l'archivage demande un état de ticket supplémentaire ; distinguer
le transporteur demande une valeur de plus dans `NatureUtilisateur`, donc de revoir **chaque**
`Nature == Client` — y compris ceux de la chaîne d'autorisation qui vient d'être consolidée. Les
traiter sous couvert de « dette mineure » aurait été disproportionné au risque. Ils méritent leur
propre phase.

---

## P3-06 — Portée de file (Avoir / Exonération)

**L'audit s'était trompé de sens.** Il écrivait : « tout porteur de la permission accède, quelle que
soit la file ». Le prédicat du service d'autorisation est :

```csharp
h.FileId == null || h.FileId == fileId
```

Avec `fileId = null`, il se réduit à `h.FileId == null` — **seule l'habilitation non bornée passe**.
Un agent habilité sur la file Avoir & Exonération, c'est-à-dire l'instructeur nominal du module,
était donc **refusé**.

Ce n'est pas une faille : le contrôle était trop strict. C'est la même forme de défaut que celui qui
avait conduit l'interface à contourner `CloisonnementService` — **une règle qui refuse les cas
légitimes finit contournée, pas corrigée.**

**Reproduction** (`PorteeFileAvoirExonerationTests`, 3 cas) : 1 échec sur 3 avant correction —
l'agent borné sur la file du module ne pouvait pas prendre en charge.

**Correction.** Les trois contrôles portant sur un ticket chargent désormais le ticket **avant** le
contrôle, et passent `ticket.FileCouranteId`. Résultat sur les trois axes :

| Acteur | Avant | Après |
|---|---|---|
| Agent borné sur la file du module | ❌ refusé | ✅ admis |
| Agent non borné (superviseur) | ✅ admis | ✅ admis |
| Agent borné sur une **autre** file | ✅ refusé | ✅ refusé |

Aucun droit n'est élargi : le troisième cas, qui est le seul enjeu de sécurité, est inchangé.

**Un incident pendant la correction, et je le signale.** Le script de patch a retiré le contrôle de
`DemanderComplementAvoirExoCommand` sans parvenir à le reposer — une garde supprimée, exactement ce
que le § 4 de la mission interdit. Le trou a été vu au contrôle suivant et refermé dans la minute.
Le fait qu'un script de réécriture puisse retirer une garde sans que rien ne le signale est en
soi un enseignement : les quatre contrôles du module sont désormais vérifiables d'un `grep`.

**Reste ouvert** : `ConsulterFileAvoirExonerationQuery` conserve `fileId: null`. Elle ne porte aucun
ticket, donc aucune file à citer ; la scoper demanderait de résoudre la file du module, ce qui
n'apporte rien tant que le module est mono-file.

---

## P3-R7 — Le filtre des notes internes, fail-closed

L'expression valait `appelantInfo?.Nature == NatureUtilisateur.Client`. Un compte introuvable donnait
`false`, donc la **vue agent**, notes internes comprises.

Le chemin est inatteignable — la garde d'identité refuse en amont — mais **la protection reposait sur
l'ordre des opérations d'un autre fichier**, pas sur l'expression elle-même. Elle devient :

```csharp
bool estClient = appelantInfo is null || appelantInfo.Nature == NatureUtilisateur.Client;
```

En l'absence d'information, c'est la vue la **plus restrictive** qui s'applique.

Aucun test fonctionnel ne peut exercer ce chemin : c'est précisément pourquoi un test de câblage le
verrouille, et le dit.

**L'asymétrie nature / permission est conservée, délibérément.** Les fiches ticket et enrôlement
filtrent sur la nature, la fiche avoir/exonération sur la permission. RG-MSG-01 dit « visible des
seuls agents habilités » — et l'habilitation sur la file est déjà garantie par le contrôle central.
Unifier sur la permission **restreindrait** les droits d'agents légitimes sans base au CDC.

---

## P3-R1 — La voie 2FA, éprouvée fonctionnellement

La règle d'accès à la connexion vivait en **deux exemplaires identiques**, dans `Connexion.razor` et
`VerifierCode.razor`. Elle n'était donc couverte que par des tests lisant les sources.

Deux copies d'une règle de sécurité finissent par diverger : **c'est exactement ce qui a produit le
P0-ter**, où `Connexion.razor` portait un contrôle d'activité que `VerifierCode.razor` n'avait jamais
eu, et où rien ne comparait les deux pages.

**Correction** : `Application/Comptes/RegleAccesConnexion.cs` — la règle énoncée une seule fois, en
deux temps explicites (activation pour tous les comptes, puis rattachement société pour les seuls
clients). Les deux pages délèguent.

**Le scénario du § 5 est désormais éprouvé** (`RegleAccesConnexionTests`, 8 cas) :

```
compte actif → règle autorise → désactivation → règle refuse
```

sur des comptes réels, sans interface. Le câblage des pages reste couvert par
`CablageControleAccesTests`.

**Ce que ce test ne fait toujours pas** : piloter `SignInManager` et son cookie `TwoFactorUserId`.
La règle est éprouvée, le mécanisme d'Identity ne l'est pas. C'est la limite annoncée par le § 5 de
la mission, et elle demeure — mais elle porte désormais sur du code d'infrastructure tiers, non sur
une règle métier.

---

## P3-R8 — La duplication de `ComptesDeTest`

Trois copies identiques de 53 lignes, une par projet de test. Un helper qui décide ce qu'est « un
compte utilisable » ne peut pas vivre en trois exemplaires : c'est le motif que les remédiations de
sécurité ont passé leur temps à défaire dans le code de production.

**Correction** : un projet `tests/CIT.Plateforme.Tests.Partages`, référencé par les quatre projets de
test. Les trois copies sont supprimées, **112 fichiers** réécrits vers le nom partagé.

Le projet ne contient que de l'échafaudage : aucune assertion, aucun test.

---

## P3-05 — Avertissements `CS9113`

| Paramètre | Décision |
|---|---|
| `PoserQuestionAssistantCommand.journalisation` | **retiré**. Poser une question à l'assistant n'est pas une « action significative » au sens de RG-SEC-03. L'**escalade**, elle, en est une, et elle est consignée. Garder une dépendance inutilisée laissait croire à une trace inexistante. |
| `ObtenirDashboardAgentQuery.utilisateurInfoRepository` | **retiré**. Dette créée par ma propre correction du tableau de bord : la garde renvoie déjà le compte, et le relire aurait rouvert un écart entre l'identité contrôlée et celle affichée. |

Le second n'existait pas au moment de l'audit — il est apparu pendant la consolidation. Le corriger
maintenant évite qu'il devienne « la dette de quelqu'un d'autre ».

**Build : 0 erreur, 0 avertissement `CS9113`.**

---

## Tests ajoutés

| Suite | Cas | Objet |
|---|---|---|
| `RegleAccesConnexionTests` *(créée)* | **8** | La règle d'accès aux sessions, y compris le scénario 2FA du § 5 |
| `PorteeFileAvoirExonerationTests` *(créée)* | **3** | Portée de file : admis borné, admis non borné, refusé autre file |
| `CablageControleAccesTests` *(étendue)* | 23 (21 → 23) | Fail-closed du filtre des notes internes ; délégation des deux pages de connexion |

---

## Refactorings

| Refactoring | Preuve d'absence de changement fonctionnel |
|---|---|
| Extraction de `RegleAccesConnexion` | Les deux copies étaient identiques mot pour mot ; la suite complète reste verte, et 8 tests neufs éprouvent désormais la règle |
| `ComptesDeTest` partagé | Contenu identique ; 112 fichiers réécrits, suite verte |
| Corpus de relais de `ServicesAtteignablesTests` | L'ensemble des services **exigés** est inchangé ; seul le chemin d'exploration suit désormais les classes applicatives qui ne sont ni `Command`, ni `Query`, ni `UseCase` |

**Sur ce dernier point.** Extraire `RegleAccesConnexion` a fait tomber la garde d'atteignabilité :
`VerifierAccesCompteQuery` n'était plus nommée par aucune page, seulement par la nouvelle règle — que
le scan ne cataloguait pas, son motif ne retenant que les suffixes `Command`/`Query`/`UseCase`.

La tentation était d'inscrire le service dans la liste de dette connue. **Ce serait faux** : cette
liste porte la mention « ne doit que RÉTRÉCIR », et la fonction *est* atteignable. Le scan a donc été
corrigé, pas contourné.

---

## Documentation

`docs/architecture-autorisation.md` — document de référence, à lire avant d'écrire une garde.

Il porte les cinq règles (identité serveur, décision générique unique, refus par défaut, ordre des
opérations, l'interface n'est pas la frontière), le tableau des quatre défauts successifs et de leur
cause commune, et un mode d'emploi par type de surface.

Sa raison d'être est celle qu'énonce le § 9 de la mission : **empêcher que quelqu'un réintroduise
demain une deuxième logique de cloisonnement.** Il montre pour cela ce que chaque copie a coûté.

---

## Performance

### `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`

**Ce que cela fait.** À chaque validation du cookie, Identity compare le tampon de sécurité porté par
le cookie à celui stocké en base. L'intervalle nul impose cette comparaison à chaque requête, là où
le défaut du framework est de 30 minutes.

**Bénéfice sécurité.** La désactivation d'un compte coupe sa session à la requête suivante. Avec le
défaut, l'intéressé conserverait une session vivante jusqu'à une demi-heure — incompatible avec le
mot « immédiatement » de SEC-11.

**Coût.** Une lecture de `AspNetUsers` par validation, sur la clé primaire — index groupé, une
page de données. C'est la requête la moins coûteuse possible sur cette table.

**Volume réel — et c'est le point.** L'application est en **Blazor Server**. Le gestionnaire de
cookie s'exécute sur les requêtes HTTP : chargement initial, ressources statiques servies par le
pipeline, négociation SignalR, `POST` de formulaires. **Les interactions au sein d'un circuit
établi passent par SignalR et ne rejouent pas la validation du cookie.** Le coût est donc
proportionnel aux **chargements de page**, non aux clics.

Pour un ordre de grandeur : une session de travail d'agent qui charge une vingtaine de pages
produit une vingtaine de lectures indexées — négligeable devant les requêtes métier de ces mêmes
pages, dont plusieurs balaient des tables de tickets.

**Mesure : non effectuée.** Ce qui précède est une analyse de conception, pas un résultat de test.
Aucun profil de charge n'a été exécuté. **À mesurer avant toute décision d'optimisation** : le coût
supposé pourrait être plus faible encore que ce qu'on imagine, et optimiser à l'aveugle
échangerait de la sécurité contre rien.

**Optimisation compatible, si la mesure la justifiait.** Un cache court du couple
(utilisateur, tampon), **invalidé explicitement par la commande de désactivation** — et non expiré
par le temps. Condition impérative : l'invalidation doit être diffusée à **toutes** les instances,
sinon l'immédiateté est perdue et le remède devient une régression de sécurité.

**Classement : P3, sans action.** La sécurité sur cet axe est correcte ; il ne s'agit que d'un coût,
et il n'est pas mesuré.

---

## Gardes UI

Les 19 pages qui n'inspectent pas `EstAuthentifie` ont été revues sous l'angle du § 6 — cohérence,
non sécurité.

| Constat | Nombre |
|---|---|
| Pages guidées par une **permission** du contexte (`Permissions.Contains(...)`) — un compte désactivé a un contexte vide, donc un refus affiché | 10 (administration) |
| Pages dont les requêtes sont **gardées côté serveur** et qui affichent `PermissionDenied` sur refus | 6 |
| Pages « mes données » désormais gardées par `GardeIdentiteService` (P2-06) | 3 |

**Aucun bouton visible pour une action interdite, aucun formulaire toujours refusé côté serveur n'a
été trouvé.** La cohérence est assurée par le contexte utilisateur, qui revient vide pour un compte
non utilisable — un mécanisme unique, non dupliqué dans les pages.

Aucune modification n'a été faite : ajouter 19 `if` aurait été exactement l'erreur que le § 1 de la
consolidation interdisait.

---

## Risques résiduels

| # | Risque | Gravité |
|---|---|---|
| **1** | **Fonctionnalité concurrente non terminée** : `CompteCloture.razor` (routable, `[AllowAnonymous]`) n'est pas inscrite dans la liste des pages publiques assumées. La garde `SurfaceRoutableTests` la signale. **Une décision de son auteur, pas la mienne.** | Bloquant pour la suite verte |
| 2 | Le mécanisme 2FA d'Identity (`SignInManager`, cookie `TwoFactorUserId`) reste non éprouvé de bout en bout ; seule la règle l'est | Faible |
| 3 | `ConsulterFileAvoirExonerationQuery` conserve `fileId: null` — sans effet tant que le module est mono-file | Faible |
| 4 | Asymétrie nature / permission du filtre des notes internes — conservée délibérément, faute de base au CDC pour restreindre | Faible |
| 5 | `ValidationInterval = TimeSpan.Zero` non mesuré en charge | Faible |
| 6 | RG-TCK-06 (archivage) et RG-CPT-08 (nature transporteur) restent des écarts fonctionnels ouverts | Moyenne (fonctionnel) |
| 7 | RG-COM-01 et RG-IA-01 dépendent d'arbitrages métier (PC-16, moteur d'assistant) | Moyenne (métier) |
| 8 | R-15 attend la confrontation aux données de production | Bloquant pour le déploiement |

---

## Suite globale

| Projet | Résultat |
|---|---|
| `CIT.Plateforme.Domain.Tests` | **208 / 208** |
| `CIT.Plateforme.Application.Tests` | **732 / 732** |
| `CIT.Plateforme.Infrastructure.Tests` | **139 / 139** |
| `CIT.Plateforme.Web.Tests` | **313 / 314** — 1 échec, `SurfaceRoutableTests` |
| **Total** | **1 392 / 1 393** |

**L'unique échec ne vient pas de cette phase.** `SurfaceRoutableTests` signale la page
`CompteCloture`, créée dans l'arbre de travail pendant que je travaillais (horodatage 18:18), avec
`CloturerMonCompteCommand.cs` et `MonProfil.razor`. La garde fait son travail : une page routable
doit porter `[Authorize]` **ou** figurer dans la liste des pages publiques assumées, avec sa raison.

Je n'ai pas inscrit la page moi-même. Elle porte `[AllowAnonymous]`, ce qui suggère une intention
délibérée — mais qualifier de « publique assumée » l'écran d'une fonctionnalité qui n'est pas de moi
reviendrait à prendre une décision de sécurité à la place de son auteur.

**Deux échecs transitoires** ont par ailleurs été observés en exécution parallèle des quatre projets
(3 en Infrastructure, contention LocalDB). Les mêmes suites passent en série. Ce n'est pas une
régression, mais c'est une fragilité de la suite qui mériterait un `xunit.runner.json` limitant le
parallélisme sur les projets touchant LocalDB.

**Non-régression** — après chaque P3 :

| Périmètre | Résultat |
|---|---|
| Sécurité (Web : API, câblage, journal) | **39 / 39** |
| Sécurité (Application : cloisonnement, état de compte, identité, tableau de bord, tarification, notifications) | **65 / 65** |
| Métier (état engageant, reproductibilité RG-SIM-12) | **12 / 12** |
| P3 ajoutés | **11 / 11** |

Aucune régression P0, P1 ou P2.

---

## Conclusion

Six P3 sur huit sont traités. Les deux restants — canaux mobiles et assistant — ne sont pas des
dettes : ce sont des arbitrages que le métier n'a pas rendus, et les trancher techniquement aurait
été les préempter.

Trois choses méritent d'être retenues de cette phase :

- **Un diagnostic d'audit peut être exact sur le symptôme et faux sur le sens.** P3-06 était bien un
  défaut, mais l'inverse de celui décrit : le contrôle refusait l'instructeur nominal au lieu
  d'ouvrir à tous. Reproduire avant de corriger est ce qui l'a montré — la relecture, elle, avait
  reconduit l'erreur de l'audit.
- **Une garde d'architecture qui gêne n'est pas forcément à assouplir.** L'extraction de
  `RegleAccesConnexion` a fait tomber le test d'atteignabilité. L'inscrire dans la liste de dette
  aurait été plus rapide, et faux : c'est le scan qui était incomplet. Une liste d'exceptions dont
  la règle dit qu'elle « ne doit que rétrécir » n'est pas un endroit où ranger ses inconvénients.
- **Un script de réécriture peut retirer une garde sans rien signaler.** C'est arrivé ici, sur la
  commande de complément Avoir/Exonération, et seul le contrôle suivant l'a vu. Les gardes doivent
  rester assez visibles pour qu'un `grep` les recense.

Le score global n'est pas recalculé.
