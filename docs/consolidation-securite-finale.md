# Consolidation sécurité finale

*Périmètre autorisé : les trois écarts de `docs/contre-audit-securite-post-P0-ter.md`.*
*Aucun autre P2/P3. Lot 3FN, R-15 et données réelles non touchés. Score global non recalculé.*

---

## 1. Risques résiduels initiaux

| # | Écart | Nature |
|---|---|---|
| **A** | `CloisonnementService` — seconde implémentation générique, 12 consommateurs de production, règle de propriété distincte, identité fournie par l'appelant | Architecture + identité |
| **B** | `VerifierCode.razor` — voie 2FA finalisant une session sans contrôle `EstActif` | Authentification |
| **C** | `ObtenirDashboardAgentQuery` — décision locale du périmètre de files, protégée par la seule garde de page | Architecture |

Un quatrième point, découvert pendant le balayage final et traité au titre de A : `ConsulterTicketCloture.razor`
passait lui aussi une société comme identité appelante.

---

## 2. CloisonnementService avant

Sa règle entière tenait en une comparaison :

```csharp
var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct);
return dossier is not null && dossier.ClientId == clientId;
```

**Trois divergences avec la règle centrale**, toutes vérifiées avant correction :

1. **Aucune garde d'identité.** Ni existence du compte, ni `EstActif`. Un compte désactivé
   conservait ses actions clientes — annuler, rouvrir, publier, déposer, télécharger.

2. **Aucun rattachement société, aucun ENT-03.** `Dossier.ClientId` porte la **société** pour
   Billing, Réclamation et Avoir (`SoumettreDemandeImportCommand` l'établit ainsi), et
   l'**utilisateur** pour l'Enrôlement. Comparer un identifiant d'utilisateur à une société est
   donc toujours faux sur les parcours société : **le contrôle refusait les clients légitimes**.

3. **Contournée par l'interface, et le code le documentait.** Pour lever ce refus,
   `FicheTicket.razor` passait `_fiche.SocieteId` à la place de l'utilisateur. L'appelant
   fournissait ainsi la valeur à laquelle le contrôle se compare — celui-ci ne pouvait plus que
   l'accepter. Effet de bord : `PublierMessageClientAsync` réutilise ce paramètre comme `AuteurId`,
   de sorte que **l'auteur des messages clients était la société, non la personne**.

### Analyse des 12 consommateurs

| Consommateur | Identité reçue avant | Identité attendue | Ressource | Contrôle avant | Après |
|---|---|---|---|---|---|
| `Tickets/AnnulerTicketCommand` | utilisateur | utilisateur | ticket | propriété nominale | garde centrale |
| `Tickets/ReouvrirTicketCommand` | utilisateur | utilisateur | ticket | idem | idem |
| `Tickets/ConsulterStatutQuery` | utilisateur | utilisateur | ticket | idem | idem |
| `Messagerie/PublierMessageCommand` | **société** (via l'IHM) | utilisateur | ticket | idem | idem |
| `Documents/DeposerDocumentCommand` | utilisateur | utilisateur | ticket | idem | idem |
| `Documents/TelechargerDocumentQuery` | utilisateur | utilisateur | document → ticket | idem | idem |
| `BillingImport/DemanderFactureCommand` | **société** (fixtures) | utilisateur | ticket | idem | idem |
| `BillingImport/GestionComplementManifesteCommand` | **société** (fixtures) | utilisateur | ticket | idem | idem |
| `BillingImport/DeclarerFinalisationPresentielleCommand` | utilisateur | utilisateur | ticket | idem | idem |
| `BillingImport/ConsulterDocumentsTicketClotureQuery` | **société** (IHM) | utilisateur | ticket | idem | idem |
| `BillingExport/GestionComplementExportCommand` | **société** (fixtures) | utilisateur | ticket | idem | idem |
| `Assistant/AssistantCommands` | utilisateur | utilisateur | ticket | idem | idem |

**Risque de différence comportementale**, mesuré et assumé : le contrôle central applique **ENT-03**
(RG-CPT-07), que la règle locale ignorait. Un collaborateur restreint à ses propres demandes ne peut
donc plus agir sur le ticket d'un collègue. **C'est une correction, pas une régression** — la règle
figure au CDC et n'était appliquée nulle part sur ces douze chemins.

---

## 3. Centralisation après

Option B retenue (§ 3 de la mission) : **façade sans logique d'autorisation propre**.

```csharp
public sealed class CloisonnementService(
    ITicketRepository tickets,
    ControleAccesRessourceService controleAcces)
{
    public async Task ExigerProprieteClientAsync(Guid utilisateurId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new AccesRefuseException(...);

        await controleAcces.ExigerConsultationTicketAsync(utilisateurId, ticket, ct).ConfigureAwait(false);
    }
}
```

`IDossierRepository` a disparu de ses dépendances : sans dossier à lire, aucune comparaison n'est
possible. Un test de câblage interdit désormais la réapparition de `dossier.ClientId ==`,
`== clientId` et de `IDossierRepository` dans ce fichier.

**Le paramètre s'appelle `utilisateurId`, plus `clientId`.** Le nom disait « client » là où le code
recevait tantôt un utilisateur, tantôt une société : c'est cette ambiguïté qui a rendu le
contournement possible sans qu'il paraisse anormal.

---

## 4. Identité utilisateur côté serveur

Deux sites fournissaient une société comme identité appelante :

| Site | Avant | Après |
|---|---|---|
| `Tickets/FicheTicket.razor:640` | `PublierMessageClientAsync(_fiche!.SocieteId, …)` | `PublierMessageClientAsync(_utilisateurId, …)` |
| `BillingImport/ConsulterTicketCloture.razor:116` | `ExecuterAsync(contexte.SocieteId.Value, …)` | `ExecuterAsync(contexte.UtilisateurId, …)` |

Le second n'avait été signalé par aucun audit précédent : il est sorti du balayage final. **Aucun
test ne couvrait ce chemin** — sans quoi la façade délégante l'aurait fait tomber immédiatement.

**Identités d'autorisation fournies par l'UI : 0.**

Un test de câblage vérifie qu'aucun appel de la voie client ne prend une société en premier
argument (`ClientAsync(...societe...)`).

---

## 5. ENT-03

La restriction de visibilité (RG-CPT-07) s'applique désormais uniformément sur la voie client,
puisqu'elle vit dans `ControleAccesRessourceService.ClientEstAdmisAsync` :

```csharp
var visibiliteTotale = rattachement.EstAdministrateur
    || rattachement.DroitConsultation == DroitConsultation.ToutesDemandesSociete;

var filtre = new VisibiliteDemandesFiltre(
    rattachement.SocieteId,
    visibiliteTotale ? null : utilisateurId);

return filtre.DemandeEstVisible(dossier);
```

Deux tests l'encadrent, dans les deux sens : un collaborateur restreint est refusé sur le ticket
d'un collègue (cas 03), un collaborateur habilité sur toute la société agit normalement (cas 04).
Le témoin est indispensable : sans lui, une correction qui refuserait tout passerait le cas 03.

---

## 6. Auteur des messages

`PublierMessageClientAsync(utilisateurId, …)` transmet cet identifiant à `PublierAsync`, qui en fait
l'`AuteurId` du message **et** de l'entrée d'historique. En recevant désormais une personne et non
une société, la trace redevient nominative — ce qu'exige RG-SEC-03 (« toute action significative est
tracée dans un journal d'audit inviolable »).

Deux tests le verrouillent : le cas 05 sur la valeur retournée, le cas 06 sur la trace persistée
(messages **et** historique), avec l'assertion explicite que la société n'y apparaît jamais.

---

## 7. Voie 2FA

`VerifierCode.razor` finalisait une session sans jamais consulter `EstActif`, en héritant du
contrôle de `Connexion.razor`. **Une voie d'ouverture de session qui hérite d'un contrôle n'en porte
aucun.**

Deux contrôles ont été posés, et le second n'est pas redondant :

```csharp
// AVANT la finalisation — la désactivation a pu survenir depuis la saisie du mot de passe.
if (!utilisateurEnAttente.EstActif) { await SignInManager.SignOutAsync(); … }

var resultat = await SignInManager.TwoFactorAuthenticatorSignInAsync(…);

// APRÈS la finalisation — la désactivation peut survenir pendant la validation elle-même.
// Sans ce second contrôle, la fenêtre serait déplacée, non fermée.
if (!await EstAccesAutoriseAsync(utilisateurEnAttente)) { await SignInManager.SignOutAsync(); … }
```

`EstAccesAutoriseAsync` reprend les deux règles de `Connexion.razor`, dans le même ordre :
activation pour **tous** les comptes, puis rattachement société (CPT-03) pour les seuls comptes
Client.

| Scénario (§ 6 de la mission) | Résultat |
|---|---|
| A — compte actif, mot de passe et code valides | Session autorisée |
| B — compte désactivé avant la saisie du code | **Refus** |
| C — désactivation entre les deux étapes | **Refus** (contrôle amont) |
| D — compte supprimé / inconnu | **Refus** — `GetTwoFactorAuthenticationUserAsync` ne résout rien |
| E — compte actif sans permission métier | Session possible, **aucune autorisation** (garde centrale) |

**Voies d'ouverture de session sans contrôle `EstActif` : 0.**

---

## 8. Dashboard Agent

La requête reçoit la garde et l'applique avant toute lecture :

```csharp
var userInfo = await garde.ObtenirCompteUtilisableAsync(agentId, ct).ConfigureAwait(false);
if (userInfo is null || userInfo.Nature != NatureUtilisateur.Agent)
{
    return DashboardAgentVue.Vide(agentId);
}
```

**Le refus prend la forme d'un tableau de bord vide plutôt que d'une exception** : cette requête
alimente un écran d'accueil, et un espace vide y est la réponse juste. Rien n'y transite — ni nom,
ni file, ni ticket, ni compteur. `DashboardAgentVue.Vide` matérialise cette forme, pour qu'elle ne
soit pas reconstruite au petit bonheur.

Six tests fonctionnels (§ 7 de la mission), appelant la requête **sans passer par la page** :

| Cas | Attendu |
|---|---|
| Agent habilité sur la file A | voit A |
| Agent non habilité sur la file B | ne voit pas B |
| Agent sans aucune habilitation | aucune file |
| **Agent désactivé** | **aucune donnée, pas même son nom** |
| Client (autre nature) porteur d'une habilitation | aucune donnée |
| Compte inconnu | aucune donnée |

---

## 9. Source unique d'autorisation

| Composant | Rôle | Décide ? |
|---|---|---|
| `GardeIdentiteService` | Identité valide + compte actif | Règle unique, consultée par les deux suivants |
| **`ControleAccesRessourceService`** | **Seule décision générique** : propriété, société, ENT-03, portée de file | **Oui** |
| `CloisonnementService` | Façade voie client | Non — délègue |
| `AutorisationService` | Permissions | Garde puis habilitations |
| `ObtenirDashboardAgentQuery` | Périmètre de files | Non — garde puis règle d'affichage |
| 4 fiches (ticket, dossier, enrôlement, avoir/exo) | Contenu | Non — délèguent, puis règles de module |
| `VisibiliteDemandesFiltre` | Filtre ENT-03 | Composant **du** contrôle central, seul appelant |

**Implémentations génériques d'accès : 1.**

Les règles métier spécifiques subsistent, et c'est le partage voulu : `enrolement.instruire` sur la
fiche Enrôlement, `avoirexoneration.instruire` pour les notes internes de l'Avoir, l'état du ticket
pour l'annulation. Aucune ne recrée propriété, rôle, société ni activité.

---

## 10. Tests ajoutés

| Suite | Cas | Objet |
|---|---|---|
| `IdentiteAppelanteEtCloisonnementTests` *(créée)* | **9** | Les 7 cas exigés au § 5, plus compte inconnu et dossier personnel du transporteur |
| `GardeTableauDeBordAgentTests` *(créée)* | **6** | Les 5 cas exigés au § 7, plus le compte inconnu |
| `CablageControleAccesTests` *(étendue)* | **21** (17 → 21) | 4 méthodes ajoutées, une par écart |

**Reproduction avant correction** — `IdentiteAppelanteEtCloisonnementTests` sur le code d'alors,
échouant sur assertion et non sur compilation :

```
échec : 4, réussite : 5, total : 9
```

Les quatre échecs montrent les **deux faces du même défaut** :

- **Cas 02** — fournir une société comme identité était **accepté** (le contrôle tautologique) ;
- **Cas 04, 05, 06** — un client légitime ne pouvait **pas** agir, et aucun message n'était créé
  (la comparaison utilisateur/société échouant toujours).

---

## 11. Mutations

Quatre mutations temporaires, toutes restaurées et vérifiées par recomptage des marqueurs.

| Mutation | Régression simulée | Tests tombés |
|---|---|---|
| **A** — garde centrale retirée de la façade | `CloisonnementService` redevient permissif | **19** (17 fonctionnels + 2 câblage) |
| **B** — `SocieteId` réintroduit comme identité appelante | Le contournement de l'IHM revient | **4** (3 fonctionnels + 1 câblage) |
| **C** — `EstActif` retiré de la voie 2FA | Une session s'ouvre pour un compte désactivé | **1** (câblage) |
| **D** — garde retirée du tableau de bord agent | Décision locale du périmètre | **3** (2 fonctionnels + 1 câblage) |

**La mutation D a d'abord été capturée par le seul test de câblage.** C'était trop faible : un test
de câblage prouve que l'appel existe, jamais qu'il produit un effet. Les six tests fonctionnels du
§ 8 ont été ajoutés, puis la mutation rejouée — elle tombe désormais sur les deux plans.

La mutation C reste couverte par le seul câblage : éprouver le refus 2FA fonctionnellement
demanderait de piloter `SignInManager` et son cookie `TwoFactorUserId` hors d'un vrai navigateur.
C'est une limite assumée, inscrite au § 14.

---

## 12. Balayage final

| Recherche | Résultat |
|---|---|
| Identités d'autorisation fournies par l'UI | **0** |
| Décisions génériques locales | **0** — le seul reste, `VisibiliteDemandesFiltre`, est un composant du contrôle central, dont il est l'unique appelant |
| Voies d'authentification ignorant `EstActif` | **0** — `Connexion.razor` et `VerifierCode.razor` |
| `CloisonnementService` — règle propre | **0** — la mention `dossier.ClientId ==` ne subsiste que dans la documentation du fichier, pour mémoire du défaut |
| Consommateurs `CloisonnementService` migrés | **12 / 12** |

---

## 13. Régressions

| Suite | Résultat |
|---|---|
| `ApiCloisonnementTests` (P0) | **12 / 12** |
| `CloisonnementAgentFichesTests` (P0-bis) | **17 / 17** |
| `EtatCompteEtAutorisationTests` (P0-ter) | **13 / 13** |
| `IdentiteAppelanteEtCloisonnementTests` (consolidation) | **9 / 9** |
| `GardeTableauDeBordAgentTests` (consolidation) | **6 / 6** |
| `CablageControleAccesTests` (câblage) | **21 / 21** |
| `SecCert05DesactivationTests` | **4 / 4** |
| **Suite globale** | **1 334 / 1 334 — 0 échec** |

Domain 208 · Application 679 · Infrastructure 139 · Web 308. Build : 0 erreur.

**Fixtures adaptées, et pourquoi ce n'est pas un affaiblissement.** 35 tests ont échoué après la
centralisation, pour deux causes, l'une et l'autre relevant du défaut corrigé :

1. **Des fixtures passaient une société comme identité appelante** — `c.SocieteId`, `societe.Id` —
   à des commandes de la voie client. C'est le comportement que le cas 02 doit désormais refuser :
   elles encodaient l'anti-patron. Elles passent maintenant l'utilisateur réel, qui dispose d'un
   rattachement actif à cette société. **Aucune assertion n'a été modifiée.**

2. **Des dossiers personnels étaient créés pour des `Guid` sans compte.** Même incomplétude qu'en
   P0-ter, étendue à la voie client. Le helper idempotent `ComptesDeTest.CompteActif` matérialise un
   compte **actif** ; les scénarios éprouvent ce qu'ils éprouvaient.

Un seul test a vu son **corps** réécrit : `RecSce14CloisonnementCompteTests` passait explicitement
`societeA.Id` / `societeB.Id` comme identités, avec un commentaire l'assumant. Son objectif métier —
un client de la société A n'atteint pas une demande de la société B — est inchangé, mais il est
désormais éprouvé sur des utilisateurs réels, et **une assertion a été ajoutée** : une société
fournie comme identité est refusée.

---

## 14. Risques résiduels

| # | Risque | Gravité |
|---|---|---|
| **R1** | Le refus 2FA n'est couvert que par un test de câblage — l'éprouver fonctionnellement demanderait de piloter le cookie `TwoFactorUserId` | Faible |
| **R2** | `ConsulterTicketCloture.razor` n'était couvert par aucun test : le défaut d'identité y a survécu à trois audits et n'est sorti que du balayage. D'autres pages peuvent être dans ce cas | Moyenne |
| **R3** | Le durcissement d'ENT-03 sur la voie client change un comportement observable : un collaborateur restreint perd l'accès aux tickets de ses collègues sur les douze chemins concernés. Conforme au CDC, mais à annoncer aux utilisateurs | Moyenne (métier) |
| **R4** | `SocieteId` fourni par l'appelant côté **tarification** (P2-02, hors périmètre) — deux requêtes non migrées | Faible |
| **R5** | Journalisation des connexions toujours absente (P2-03, hors périmètre) : un refus par compte désactivé n'est pas tracé | Moyenne |
| **R6** | `ValidationInterval = TimeSpan.Zero` — une lecture du compte par requête (P3 déjà classé) | Faible |
| **R7** | `avoirexoneration.instruire` vérifiée sans portée de file ; notes internes filtrées sur la nature (ticket, enrôlement) et sur la permission (avoir/exo) | Faible |
| **R8** | Le helper `ComptesDeTest` est dupliqué dans les trois projets de test | Nul (échafaudage) |
| **R9** | Aucune vérification en base réelle ; R-15 toujours ouvert | — |

---

## 15. Conclusion

La cible du § 2 est atteinte : **une identité fournie par le serveur, une garde centrale, une
autorisation métier explicite.** Le contrôle générique d'accès aux ressources n'existe plus qu'en un
seul exemplaire, la règle « compte valide et actif » n'est écrite qu'une fois, et aucune identité
d'autorisation ne transite plus par l'interface.

Ce qu'il faut retenir de la série P0 → P0-bis → P0-ter → consolidation, c'est que les quatre défauts
n'en formaient qu'un seul : **une décision d'accès dupliquée finit toujours par diverger, et la
divergence se referme par un contournement plutôt que par une correction.** `CloisonnementService`
en est la démonstration complète — sa règle refusait les clients légitimes, l'interface a fourni la
valeur qui la satisfaisait, et le contrôle a survécu quatre audits en ne contrôlant plus rien.

Deux enseignements de méthode, tous deux à mon débit :

- **Un contrôle qui refuse les cas légitimes finit contourné, pas corrigé.** Le commentaire de
  `FicheTicket.razor` documentait le contournement depuis des mois. Une règle de sécurité qui gêne
  le fonctionnement normal est un défaut de conception, pas une rigueur.
- **Ce qui n'est pas testé n'est pas protégé.** `ConsulterTicketCloture.razor` portait le même
  défaut d'identité et n'est apparu que dans le balayage final : aucun test ne traversait cette
  page. Le nombre de tests ne dit rien des chemins qu'ils ne touchent pas.

Restent hors périmètre, tels quels : P2-02 (tarification), P2-03 (journalisation des connexions),
R-15, lot 3FN. Le score global n'est pas recalculé.
