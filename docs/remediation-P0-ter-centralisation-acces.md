# Remédiation P0-ter — Contrôle d'accès centralisé

*Périmètre autorisé : P0-ter et P2-01 (centralisation des décisions d'accès). Aucun autre P2/P3.*
*Lot 3FN, migrations tarifaires, R-15 et données réelles : non touchés.*

---

## 1. Vulnérabilité

La désactivation d'un compte interne — agent ou administrateur CIT — n'avait **aucun effet sur ses
permissions**. `EstActif` était une donnée applicative que la couche d'autorisation ne consultait
jamais.

Un administrateur désactivé conservant l'habilitation `administration.comptes` pouvait ouvrir une
session neuve, atteindre les écrans d'administration et **se réactiver lui-même**.

Règles atteintes : **RG-SEC-01** (moindre privilège), **RG-SEC-02** (cloisonnement), **SEC-11**
(« désactivation immédiate »).

---

## 2. Chaîne d'exploitation

Quatre maillons, chacun lu et vérifié avant correction :

| # | Maillon | Preuve |
|---|---|---|
| 1 | **La connexion ne vérifie pas l'activité d'un compte interne** | `Connexion.razor:175` — `utilisateur.Nature != NatureUtilisateur.Client \|\| await VerifierAcces.AccesAutoriseAsync(...)`. Pour un agent, la première branche est vraie et court-circuite tout. `EstActif` étant une propriété applicative, aucune primitive Identity ne la connaît, et aucun `SecurityStampValidator` n'était configuré. |
| 2 | **L'autorisation ignore l'état du compte** | `AutorisationService.cs:22-33` — la décision lisait `db.Habilitations` et **rien d'autre**. |
| 3 | **19 pages routables sur 69 n'exigent pas `EstAuthentifie`** | dont les **dix** pages d'administration, toutes porteuses de `[Authorize]`, que le cookie satisfait. |
| 4 | **La commande d'administration n'exige qu'une permission** | `AdministrationComptesAgentsCommands.cs:72` — `ExigerAsync(appelantId, Permission.AdministrationComptes)`, sans aucun autre contrôle. |

**Atténuation qui existait, et qu'il faut nommer honnêtement :** `ObtenirContexteUtilisateurQuery:22`
refusait bien un compte inactif et renvoyait un contexte mort ; 50 pages sur 69 le respectaient.
**La frontière de sécurité vivait donc dans l'interface** — et là où l'interface ne la portait pas,
il n'y avait rien.

---

## 3. Cause racine

**La même que P0 et P0-bis : la décision d'autorisation est réécrite là où elle est utilisée.**

Les endpoints `/api/v1` avaient leur copie (P0). Les fiches ticket et dossier avaient la leur
(P0-bis). Ici, ce sont la page de connexion, le service d'autorisation et dix-neuf pages qui
portaient chacun un fragment de la décision — et les fragments ne se recouvraient pas.

Deux causes secondaires, indissociables :

- **Une exclusion trop large.** Le correctif du 2026-08-30 avait raison de soustraire les comptes
  internes à la règle *société* (CPT-03), qui les bloquait tous. Mais les deux règles — activité et
  rattachement — étaient portées par **une seule expression** : `Nature != Client || AccesAutorise(...)`
  rendait vraie la condition entière dès que l'appelant n'était pas un client. Soustraire l'un
  soustrayait l'autre.

- **Un test tautologique.** `SecCert05DesactivationTests` certifiait SEC-11 avec
  `agent.EstActif = false;` suivi de `Assert.False(agent.EstActif);`. Un tel test passe quoi qu'il
  arrive. Il a donné à la règle une apparence de couverture pendant trois audits.

---

## 4. Reproduction avant correction

`tests/CIT.Plateforme.Application.Tests/Securite/EtatCompteEtAutorisationTests.cs` — les douze cas
exigés plus un témoin, écrits contre le code d'alors et **échouant sur assertion**, non sur
compilation :

```
échec : 6, réussite : 7, total : 13
```

| Cas | Attendu | Avant correction |
|---|---|---|
| 1 — agent actif + permission | Autorisé | ✅ passait (témoin) |
| **2 — agent désactivé + permission** | Refusé | ❌ **autorisé** |
| 3 — administrateur actif | Autorisé | ✅ passait |
| **4 — administrateur désactivé** | Refusé | ❌ **administrait** |
| **5 — administrateur désactivé se réactive lui-même** | Refusé | ❌ **réactivation réussie** |
| **6 — session préexistante après désactivation** | Refusé | ❌ **autorisé** |
| 7 — agent désactivé, lecture protégée | Refusé | ✅ (fermé en P0-bis) |
| **8 — agent désactivé, commande métier** | Refusé | ❌ **autorisé** |
| 9 — agent sans habilitation | Refusé | ✅ passait |
| **10 — compte inconnu, habilitation résiduelle** | Refusé | ❌ **autorisé** |
| 11 — client désactivé | Refusé | ✅ (fermé en P0-bis) |
| 12 — transporteur désactivé | Refusé | ✅ (fermé en P0-bis) |
| 12bis — transporteur actif (témoin) | Autorisé | ✅ passait |

Les cas 7, 11 et 12 passaient déjà : `ControleAccesRessourceService` portait le contrôle depuis
P0-bis. **Les six échecs sont tous du côté `AutorisationService`** — c'est-à-dire du côté des
actions, jamais éprouvé jusqu'ici.

Le cas 5 est le cas décisif : il rendait la désactivation réversible par la personne même qu'elle
visait.

---

## 5. Architecture avant

```
                    ┌──────────────────────────────┐
   cookie valide ──►│  Connexion.razor             │  Nature != Client ⇒ aucun contrôle
                    └──────────────────────────────┘
                                  │
      ┌───────────────────────────┼────────────────────────────┐
      ▼                           ▼                            ▼
┌──────────────┐   ┌──────────────────────────┐   ┌────────────────────────┐
│ 19 pages     │   │ ObtenirContexteUtilisateur│   │ AutorisationService    │
│ sans garde   │   │ (refuse l'inactif)        │   │ Habilitations SEULES   │
└──────┬───────┘   └──────────────────────────┘   └───────────┬────────────┘
       │                       ▲                               │
       │            50 pages ──┘  ← la frontière               ▼
       └─────────────────────────────────────────────►  commandes métier
                                                          (92 cas d'usage)

  5 implémentations distinctes de la décision d'accès aux ressources :
  ControleAccesRessourceService · FicheTicket · FicheDossier · FicheEnrolement · FicheAvoirExo
```

---

## 6. Architecture après

```
                        ┌───────────────────────────┐
                        │  GardeIdentiteService     │   « identité valide ET compte actif »
                        │  (règle énoncée UNE fois) │   deny-by-default
                        └────────────┬──────────────┘
                     ┌───────────────┴───────────────┐
                     ▼                               ▼
        ┌────────────────────────┐      ┌────────────────────────────┐
        │ AutorisationService    │      │ ControleAccesRessourceService│
        │ identité PUIS permission│      │ identité PUIS propriété/file │
        └───────────┬────────────┘      └──────────────┬─────────────┘
                    ▼                                  ▼
          92 cas d'usage applicatifs        4 fiches, toutes déléguantes
          (dont les 10 pages admin)         Ticket · Dossier · Enrôlement · Avoir/Exo

   Défense en profondeur (n'est PAS la frontière) :
   rotation du tampon de sécurité à la désactivation + validation à chaque requête
```

Le contrôle générique n'existe plus qu'**une fois**. Les fiches conservent uniquement leurs règles
propres au module.

---

## 7. Gestion `EstActif`

`EstActif` est désormais une **règle de sécurité**, portée par un seul objet :

`src/CIT.Plateforme.Application/Securite/GardeIdentiteService.cs`

```csharp
public async Task<UtilisateurInfo?> ObtenirCompteUtilisableAsync(Guid utilisateurId, CancellationToken ct = default)
{
    if (utilisateurId == Guid.Empty) { return null; }

    var compte = await utilisateurs.ObtenirInfoAsync(utilisateurId, ct).ConfigureAwait(false);
    if (compte is null) { return null; }   // inconnu ou supprimé, session encore présentée
    if (!compte.EstActif) { return null; } // SEC-11 — désactivation immédiate

    return compte;
}
```

Il renvoie le **compte** plutôt qu'un booléen : l'appelant y lit la nature sans seconde lecture, et
une relecture serait une occasion de plus de diverger.

Aucune page n'a besoin de penser à vérifier quoi que ce soit : la règle est imposée en amont, dans
les deux services qui décident.

---

## 8. Authentification

`Connexion.razor` — les deux règles sont désormais **énoncées séparément**, dans cet ordre :

```csharp
private async Task<bool> EstAccesAutoriseAsync(ApplicationUser utilisateur)
{
    if (!utilisateur.EstActif)
    {
        return false;
    }

    return utilisateur.Nature != NatureUtilisateur.Client
        || await VerifierAcces.AccesAutoriseAsync(utilisateur.Id);
}
```

1. **Activation** — applicable à **tous** les comptes, sans exception de nature.
2. **Rattachement société** (CPT-03) — propre aux comptes Client, un compte interne n'étant jamais
   rattaché à une société par conception.

La condition `Nature != Client || ...` a disparu en tant que garde unique : elle subsiste seulement
comme aiguillage vers la règle société, après que l'activité a été vérifiée.

---

## 9. Autorisation

`AutorisationService.DeciderAsync` — l'identité précède la permission :

```csharp
var garde = new GardeIdentiteService(new UtilisateurInfoRepository(db));
if (!await garde.CompteEstUtilisableAsync(utilisateurId, ct).ConfigureAwait(false))
{
    return false;   // compte vide, inconnu, supprimé ou désactivé
}

return await db.Habilitations
    .Where(h => h.UtilisateurId == utilisateurId && h.PermissionCode == permissionCode)
    .AnyAsync(h => h.FileId == null || h.FileId == fileId, ct)
    .ConfigureAwait(false);
```

**L'ordre est délibéré et testé.** Interroger d'abord les habilitations traiterait la permission
comme la question principale et l'état du compte comme un correctif — c'est la hiérarchie exacte
qui a produit le défaut. Un test de câblage compare les positions dans le fichier.

Ce point est le passage obligé des **92 cas d'usage applicatifs** qui consomment
`IAutorisationService` : le durcir ferme la surface des commandes d'un seul geste. La possession
historique d'une habilitation ne contourne plus l'état du compte.

---

## 10. Session déjà ouverte

**Scénario traité** : compte actif → connexion → cookie émis → désactivation → requête suivante.

**Stratégie retenue, en deux couches — et une seule est la frontière de sécurité :**

**a. La frontière : la décision serveur.** `AutorisationService` et `ControleAccesRessourceService`
consultent la garde à **chaque** décision. Un compte désactivé n'obtient plus rien, quel que soit
l'état de son cookie. C'est ce que démontre le cas 6, qui n'implique aucun mécanisme de session :
même identité, même permission, seule l'activité a changé.

**b. La défense en profondeur : le tampon de sécurité.** `AdministrationComptesRepository`
fait tourner le tampon (`UpdateSecurityStampAsync`) dans les deux sens — désactivation **et**
réactivation, car on ne sait pas ce que portent les sessions ouvertes avant. `Program.cs` fixe
`SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`.

**Compromis assumé, et pourquoi ce choix plutôt qu'un autre.** L'intervalle nul impose une lecture
du compte par requête, là où le défaut d'Identity est de 30 minutes. Cette demi-heure est
incompatible avec le mot « immédiatement » de SEC-11. Les autres options ont été écartées :

- une **liste de révocation** en mémoire ne survit pas à un redémarrage et ne se partage pas entre
  instances ;
- un **cookie de durée très courte** dégraderait l'expérience de tous pour le cas de quelques-uns ;
- **ne rien faire côté session** aurait laissé l'intéressé avec une interface vide plutôt qu'un
  refus lisible, et une trace d'activité trompeuse dans les journaux.

Ce n'est pas cette validation qui protège les données — c'est (a). (b) rend le refus lisible.

---

## 11. Centralisation P2-01

| # | Implémentation | Règle qu'elle portait | Devenue |
|---|---|---|---|
| 1 | `ControleAccesRessourceService` | Identité, activité, propriété, société, portée file | **Le mécanisme central.** Son contrôle d'activité est délégué à `GardeIdentiteService` |
| 2 | `ObtenirFicheTicketQuery` | (déjà déléguante depuis P0-bis) | Inchangée — délègue |
| 3 | `ObtenirFicheDossierQuery` | (déjà déléguante depuis P0-bis) | Inchangée — délègue |
| 4 | `ObtenirFicheEnrolementQuery` | Propriété du transporteur + habilitation `enrolement.instruire` bornée à la file | **Délègue** le générique ; conserve l'exigence `enrolement.instruire`, propre à F008 |
| 5 | `ObtenirFicheAvoirExonerationQuery` | Propriété via société + permission `avoirexoneration.instruire` | **Délègue** le générique ; `avoirexoneration.instruire` ne décide plus de l'accès mais du **contenu** (notes internes) |

**Implémentations génériques restantes : 1.**

Le contrôle d'activité, lui, n'existe plus qu'une fois — dans `GardeIdentiteService`, que le
mécanisme central et le service d'autorisation consultent tous deux.

---

## 12. Fiches migrées

**`ObtenirFicheEnrolementQuery`** — quatrième copie. Elle traitait correctement la propriété du
transporteur et l'habilitation de l'agent, mais **ignorait l'état du compte**, et sa seule
protection contre un compte désactivé était la garde d'interface de `FicheEnrolement.razor`. Une
frontière de sécurité ne vit pas dans une page.

**`ObtenirFicheAvoirExonerationQuery`** — cinquième copie. Correcte sur le fond : `estAgent` y était
une véritable vérification de permission, non la nature de l'appelant — l'audit l'avait d'ailleurs
signalée comme faux négatif. Elle réécrivait néanmoins la propriété du dossier et ignorait l'état du
compte.

**Un effet de bord notable, et c'est une correction.** La fiche Avoir/Exonération ne vérifiait que
`dossier.ClientId == societeId`, sans appliquer la restriction d'auteur d'**ENT-03** (RG-CPT-07 —
« ses seules demandes » ou « l'ensemble des demandes de sa société »). Le contrôle central
l'applique. Un collaborateur restreint à ses propres demandes ne lit donc plus celles d'un collègue
sur ce module, ce qui n'était pas le cas avant.

Le patron appliqué aux deux, conforme au § 10 de la mission :

```csharp
// ── 1. CONTRÔLE GÉNÉRIQUE — délégué
await controleAcces.ExigerConsultationTicketAsync(appelantId, ticket, ct);

// ── 2. CONTRÔLE MÉTIER SPÉCIFIQUE — conservé
if (!estClient && !aDroitInstruction) { throw new AccesRefuseException(...); }
```

---

## 13. Administration

Les **dix** pages d'administration ont été vérifiées **au niveau des commandes serveur**, non des
écrans :

| Page | Commandes | Gardées |
|---|---|---|
| CompetencesOperateur | 1 | 1 |
| **ComptesAgents** | **3** | **3** |
| EtapesBloquantesAnnulation | 1 | 1 |
| MatriceTransitions | 1 | 1 |
| MotifsTransfert | 1 | 1 |
| ParametresSla | 1 | 1 |
| PerimetresSupervision | 1 | 1 |
| PiecesExigeesCompte | 1 | 1 |
| PiecesExigeesEtape | 1 | 1 |
| ReglesEscalade | 2 | 2 |

Toutes passent par `IAutorisationService.ExigerAsync`, donc par la garde d'identité. Un compte
désactivé ne peut plus consulter les comptes, en créer, en modifier, attribuer un rôle, modifier une
habilitation, désactiver ou réactiver un utilisateur, **ni se réactiver lui-même** — les cas 3, 4 et
5 l'établissent sur la commande réelle, pas sur la page.

---

## 14. Tests ajoutés

| Suite | Cas | Objet |
|---|---|---|
| `EtatCompteEtAutorisationTests` *(créée)* | **13** | Les 12 cas exigés + un témoin transporteur actif |
| `CablageControleAccesTests` *(étendue)* | **17** (9 → 17) | 5 méthodes ajoutées, dont une `[Theory]` sur les 4 fiches |
| `SecCert05DesactivationTests` *(corrigée)* | 4 | Le volet tautologique remplacé (§ 15) |
| `CloisonnementAgentFichesTests` | 17 | Inchangée — non-régression P0-bis |
| `ApiCloisonnementTests` | 12 | Inchangée — non-régression P0 |

**Test tautologique corrigé (§ 15 de la mission).** `SecCert05DesactivationTests` ne se contente
plus de `Assert.False(EstActif)`. Le volet 1 attribue une permission au compte **actif**, vérifie
qu'il est autorisé (témoin), désactive, puis vérifie le refus — à permission constante, seule
l'activité ayant changé. **L'objectif documentaire n'a pas été supprimé** : le volet 2 (conservation
intégrale de l'historique) et la garde d'abstention T025 sont conservés tels quels. La note de
périmètre qui excluait « l'invalidation d'une session déjà ouverte » a été révisée plutôt
qu'effacée : elle reposait sur une lecture trop littérale du CDC, une désactivation qui laisse agir
jusqu'à l'expiration du cookie n'étant pas « immédiate ».

Une découverte au passage : `CreerAgent` **construisait** le compte sans jamais l'inscrire en base.
La suite ne pouvait donc éprouver aucune décision serveur — le compte n'existait pas.

---

## 15. Tests de mutation

Trois mutations appliquées au code corrigé, puis restaurées. Chacune reproduit un défaut précis.

| Mutation | Régression simulée | Tests tombés |
|---|---|---|
| **A** — `AutorisationService` ignore la garde | Le P0-ter lui-même | **8** (7 Application + 1 câblage) |
| **B** — `GardeIdentiteService` ignore `EstActif` | La désactivation redevient décorative | **11** (10 Application + 1 câblage) |
| **C** — `ObtenirFicheEnrolementQuery` contourne le contrôle central | Une fiche réimplémente le générique | **3** (1 Application + 2 Web, dont le câblage) |

Chaque mutation a été détectée à la fois par un test **fonctionnel** et par un test de **câblage** —
c'est la double détection qui compte : le premier prouve l'effet, le second empêche la suppression
silencieuse.

**Restauration vérifiée** par recomptage des trois marqueurs (`ExigerConsultationTicketAsync`,
`CompteEstUtilisableAsync`, `!compte.EstActif`) : 1 occurrence chacun, et suite complète verte.

---

## 16. Régressions P0 / P0-bis

| Suite | Résultat |
|---|---|
| `ApiCloisonnementTests` (P0 — IDOR/BOLA API) | **12 / 12** |
| `CloisonnementAgentFichesTests` (P0-bis — agent/file/notes internes) | **17 / 17** |
| `CablageControleAccesTests` | **17 / 17** |
| Cloisonnement client, société, transporteur, ticket, dossier | verts |

Aucune assertion affaiblie, aucun test supprimé, aucune couverture réduite.

**107 fixtures de test complétées, et pourquoi ce n'est pas un affaiblissement.** La correction a
fait échouer 171 tests d'un coup. Cause unique : ces fixtures attribuaient une permission à un
`Guid.NewGuid()` **sans jamais créer le compte correspondant**. Cela fonctionnait parce que
`AutorisationService` ne lisait que la table des habilitations — une permission suffisait, l'identité
ne comptait pas. C'est exactement le défaut corrigé.

En production, une habilitation ne peut pas exister sans compte : `CreerCompteAgentCommand` est le
seul chemin qui en crée. Les fixtures étaient donc **incomplètes, non fausses**. Le helper
`ComptesDeTest.CompteActif` (idempotent, présent dans les trois projets de test) rétablit cette
vérité en créant un compte **actif** — les scénarios continuent d'éprouver ce qu'ils éprouvaient.
Même raisonnement pour les rattachements société, qui impliquent un compte client réel.

Ce chiffre de 171 est lui-même un résultat d'audit : la suite accordait des permissions à des
identités qui n'existaient pas.

---

## 17. Balayage des surfaces

**80 pages routables** inventoriées ; **83 commandes et cas d'usage** atteignables depuis une page.

Le balayage a servi de **validation**, non de prétexte à ajouter des `if` : 18 commandes ne
contiennent aucun appel d'autorisation. Chacune a été inspectée.

| Catégorie | Nombre | Verdict |
|---|---|---|
| **Façades déléguantes** — `ActionsSuperviseurCommand`, `DeposerPreuvePaiementCommand` | 2 | Couvertes : leurs délégués (`AffecterManuellement`, `Reaffecter`, `Transferer`, `DeposerDocument`) appellent tous `ExigerAsync` |
| **Commandes dont l'appelant EST la portée** — profil, préférences, assistant, soumissions client | 10 | Couvertes : l'identifiant de l'appelant borne l'écriture ; il n'y a pas d'autre ressource à autoriser |
| **Commandes d'administration de société** — rattachement, rôles, droits, invitations | 6 | Couvertes par un mécanisme distinct et légitime : `EstAdministrateur` sur le rattachement (CPT-06) |

**Surfaces sensibles contournant le mécanisme central : 0.**

---

## 18. Fichiers modifiés

**Production — 9 fichiers**

| Fichier | Nature |
|---|---|
| `Application/Securite/GardeIdentiteService.cs` | **créé** — la règle, énoncée une fois |
| `Application/Securite/ControleAccesRessourceService.cs` | délègue son contrôle d'activité à la garde |
| `Infrastructure/Identity/AutorisationService.cs` | identité avant permission |
| `Infrastructure/Identity/AdministrationComptesRepository.cs` | rotation du tampon de sécurité |
| `Infrastructure/DependencyInjection.cs` | enregistrement de la garde |
| `Web/Program.cs` | `ValidationInterval = TimeSpan.Zero` |
| `Web/Components/Pages/Comptes/Connexion.razor` | règle d'activation explicite |
| `Application/Enrolement/ObtenirFicheEnrolementQuery.cs` | délégation (P2-01) |
| `Application/AvoirExoneration/DemandeAvoirExoQueries.cs` | délégation (P2-01) |

**Tests — 5 fichiers créés, ~110 modifiés**

`EtatCompteEtAutorisationTests.cs` · `ComptesDeTest.cs` (×3 projets) · `FabriqueSecurite.cs` ;
`CablageControleAccesTests.cs`, `SecCert05DesactivationTests.cs`, `FabriqueRequetes.cs`,
`ConsulterAvoirExoTests.cs` et 107 fixtures complétées.

---

## 19. Risques résiduels

1. **`SocieteId` fourni par l'appelant côté tarification** (P2-02, **hors périmètre imposé**).
   `ConsulterSimulationProformaQuery` et `GenererDocumentProformaPdfQuery` comparent un `SocieteId`
   reçu de l'appelant au lieu de le dériver de l'identité. Non exploitable depuis l'IHM — les pages
   le tirent du contexte serveur — mais c'est le motif même qui a produit le P0 d'origine. Ces deux
   requêtes bénéficient désormais de la garde par `AutorisationService`, sans que leur décision de
   propriété soit centralisée.

2. **19 pages sans garde `EstAuthentifie`** (P2-06, hors périmètre). Elles ne sont plus une faille —
   la décision serveur ne dépend plus d'elles — mais elles serviront une interface dégradée plutôt
   qu'un refus explicite à un compte désactivé.

3. **Coût de la validation par requête.** `ValidationInterval = TimeSpan.Zero` ajoute une lecture du
   compte à chaque requête. Assumé (§ 10), à surveiller en charge.

4. **Le helper `ComptesDeTest` est dupliqué dans les trois projets de test.** Duplication
   d'échafaudage, sans effet sur la production ; un projet de test partagé la supprimerait.

5. **Journalisation des connexions toujours absente** (P2-03, hors périmètre). Une tentative d'accès
   par compte désactivé est refusée mais **non tracée** — c'est ce qui a rendu le P0-ter invisible
   en exploitation.

6. **R-15 reste ouvert** — non touché, conformément aux consignes.

7. **Aucune vérification en base réelle.** Tests sur `InMemory` et LocalDB ; aucune migration
   exécutée sur une base peuplée.

---

## 20. Conclusion

Le P0-ter est fermé, et fermé **à l'endroit où la décision se prend** : un compte inconnu ou
désactivé n'obtient plus aucune autorisation métier, quels que soient son cookie, ses habilitations
et l'écran par lequel il passe. Les six chemins qui laissaient passer avant correction sont refusés,
et trois mutations démontrent que les tests s'en aperçoivent.

Le contrôle générique d'accès aux ressources ne compte plus qu'**une implémentation** au lieu de
cinq, et la règle « compte valide et actif » n'est écrite **qu'une fois**, dans un objet que les deux
services décisionnels consultent. C'est la première fois, sur cette suite de remédiations, que la
cause racine est traitée en même temps que le symptôme — P0 et P0-bis avaient chacun fermé leur
surface en laissant le motif intact, et c'est précisément ce qui a produit le défaut suivant.

Trois choses méritent d'être retenues :

- **Un test tautologique est pire que pas de test.** `Assert.False(EstActif)` après avoir écrit
  `false` a certifié SEC-11 pendant trois audits, sur un contrôle qui ne fonctionnait pas.
- **Les 171 tests tombés étaient un diagnostic, pas un obstacle.** La suite accordait des permissions
  à des identités inexistantes ; c'était possible parce que le code de production l'était aussi.
- **L'ordre des vérifications est une décision d'architecture.** Placer la permission avant
  l'identité fait de l'état du compte un correctif. Un test de câblage compare désormais les
  positions dans le fichier — parce que cet ordre est la règle, pas un détail de rédaction.

Restent hors périmètre, tels quels : P2-02 (tarification), P2-03 (journalisation des connexions),
P2-06 (gardes d'interface), R-15. Le score global n'est pas recalculé.
