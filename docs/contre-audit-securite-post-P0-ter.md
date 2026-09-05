# Contre-audit sécurité post P0-ter

*Mode AUDIT / READ ONLY — aucun code modifié.*
*Objet : confirmer indépendamment la fermeture de P0, P0-bis et P0-ter, et vérifier que la
centralisation ne laisse plus de variante parallèle.*

---

## Résumé exécutif

**Les trois vulnérabilités nommées sont réellement fermées.** P0 (IDOR sur `/api/v1`), P0-bis
(cloisonnement agent des fiches) et P0-ter (désactivation sans effet sur les permissions) ont été
re-vérifiés en lisant les chemins de décision, pas en relisant les rapports. Aucun n'est réouvert.

**Mais la centralisation revendiquée par P2-01 est incomplète, et mon propre rapport P0-ter l'a
surestimée.** Il annonce « Implémentations génériques restantes : 1 ». Le chiffre exact est **3** :
ce rapport n'avait inventorié que les *fiches*, et laissé hors du champ deux mécanismes qui décident
pourtant d'un accès générique.

| Implémentation générique | Portée | Consulte la garde d'identité |
|---|---|---|
| `ControleAccesRessourceService` + `GardeIdentiteService` | Fiches ticket, dossier, enrôlement, avoir/exo ; endpoints API | **Oui** |
| **`CloisonnementService`** | **12 commandes et requêtes de la voie CLIENT** | **Non** |
| **`ObtenirDashboardAgentQuery`** | Périmètre de files de l'agent | **Non** |

Deux constats supplémentaires, tous deux dans mon angle mort précédent :

- **`CloisonnementService` porte une règle de propriété différente de la centrale, et l'interface la
  neutralise.** `FicheTicket.razor:640` passe `_fiche!.SocieteId` là où le paramètre s'appelle
  `clientId` : la valeur comparée provient de la ressource elle-même. Le contrôle devient
  tautologique, et l'auteur enregistré du message devient la société, non la personne.
- **La page de vérification 2FA n'applique aucun contrôle d'activité.** J'avais corrigé
  `Connexion.razor` sans balayer les autres points d'ouverture de session — la même erreur de
  méthode que le balayage incomplet du P0-bis.

Aucun de ces points n'est exploitable en l'état : les chemins concernés ne sont atteints qu'après
une lecture déjà gardée, le tableau de bord n'expose que les tickets de l'agent lui-même, et la
session obtenue par la faille 2FA n'ouvre aucune autorisation. **La sécurité tient ; c'est
l'architecture de la décision qui reste fragmentée.**

**Décision : P0 = 🟠 FERMÉ AVEC RISQUE RÉSIDUEL.**

---

## Source unique d'autorisation

### Inventaire

| Catégorie | Surfaces | Mécanisme décisionnaire |
|---|---|---|
| **Services d'autorisation** | `AutorisationService` (92 consommateurs applicatifs) | `GardeIdentiteService` **puis** habilitations, dans cet ordre |
| **Services de cloisonnement** | `ControleAccesRessourceService` | `GardeIdentiteService` puis propriété / société / file |
| | **`CloisonnementService`** | **aucun — propriété nominale seule** |
| **Query handlers — fiches** | Ticket, Dossier, Enrôlement, Avoir/Exo | délèguent tous à `ExigerConsultationTicketAsync` / `…DossierAsync` |
| **Query handlers — listes** | 15 requêtes de liste de tickets | 12 via `ExigerAsync`, 2 via `ControleAcces`, 1 portée par l'appelant |
| | **`ObtenirDashboardAgentQuery`** | **aucun — habilitations lues en local** |
| **Commandes** | 83 atteignables depuis une page | `ExigerAsync`, `CloisonnementService`, `EstAdministrateur` (CPT-06), ou portée par l'appelant |
| **Pages Razor** | 80 pages routables | Garde ergonomique (`EstAuthentifie`) — **jamais** la frontière |
| **Accès direct DbContext** | `IndicateursTemps.razor` (seul) | `controleAcces.PeutConsulterLeTicketAsync` |
| **`IServiceScopeFactory`** | Isolation de contexte, pas de décision | sans objet |
| **Endpoints API** | 3 (`/tickets/{numero}`, `/dossiers/{reference}`, `/simulations/calculer`) | Les deux premiers : `controleAcces` ; le troisième calcule sans lire de ressource |

### Ce qui décide réellement, surface par surface

**Chaîne centrale — conforme.** `GardeIdentiteService` est bien le point unique de la règle
« identité valide et compte actif ». `AutorisationService` la consulte **avant** les habilitations
(ordre vérifié dans le code et verrouillé par un test de câblage) ; `ControleAccesRessourceService`
la consulte avant toute décision de propriété. Les quatre fiches délèguent. Les endpoints API
délèguent.

**Première variante parallèle — `CloisonnementService`.** 12 consommateurs de production :

```
Assistant/AssistantCommands.cs                     Documents/DeposerDocumentCommand.cs
BillingExport/GestionComplementExportCommand.cs    Documents/TelechargerDocumentQuery.cs
BillingImport/ConsulterDocumentsTicketClotureQuery Messagerie/PublierMessageCommand.cs
BillingImport/DeclarerFinalisationPresentielleCmd  Tickets/AnnulerTicketCommand.cs
BillingImport/DemanderFactureCommand.cs            Tickets/ConsulterStatutQuery.cs
BillingImport/GestionComplementManifesteCommand.cs Tickets/ReouvrirTicketCommand.cs
```

Sa règle entière :

```csharp
var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct);
return dossier is not null && dossier.ClientId == clientId;
```

Trois écarts avec la règle centrale :

1. **Aucune garde d'identité** — ni existence du compte, ni `EstActif`.
2. **Propriété nominale uniquement** — pas de rattachement société, pas d'ENT-03 (RG-CPT-07). Or
   `Dossier.ClientId` porte la **société** pour les parcours Billing, Réclamation et Avoir. La
   comparaison à un identifiant d'utilisateur est donc toujours fausse sur ces parcours.
3. **Contournée par l'interface, et c'est documenté dans le code.** `FicheTicket.razor:640` :

   ```csharp
   // PublierMessageClientAsync vérifie la propriété via CloisonnementService, qui compare
   // l'appelant à Dossier.ClientId — documenté comme portant la SocieteId, jamais
   // l'UtilisateurId individuel. Passer _utilisateurId ici levait systématiquement AccesRefuseException.
   await PublierMessage.PublierMessageClientAsync(_fiche!.SocieteId, Id, _nouveauMessageContenu);
   ```

   L'appelant fournit donc la valeur à laquelle le contrôle se compare. C'est exactement
   l'anti-patron que `ControleAccesRessourceService` a été créé pour supprimer — son principe n° 2
   énonce que « la décision ne se fonde sur aucun objet fourni par l'appelant ».

   Effet de bord : `PublierMessageClientAsync` réutilise ce paramètre comme `AuteurId`. **Les
   messages clients sont attribués à la société, pas à la personne** — ce qui affaiblit RG-SEC-03
   (traçabilité de l'auteur) et rend ENT-03 inapplicable sur ce chemin.

Quatre de ces douze surfaces n'ont **aucun** autre contrôle : `DemanderFactureCommand`,
`DeclarerFinalisationPresentielleCommand`, `ConsulterStatutQuery`,
`ConsulterDocumentsTicketClotureQuery`.

**Seconde variante parallèle — `ObtenirDashboardAgentQuery`.** Elle lit les habilitations de
l'agent, liste **tous** les tickets et les filtre sur les files habilitées, sans jamais interroger
la garde. Sa seule protection est `DashboardAgent.razor:180` (`if (!contexte.EstAuthentifie)`).
L'exposition reste limitée — nom, disponibilité, statistiques par file, **ses propres tickets en
cours** et des compteurs — mais le principe est enfreint : une décision de périmètre y est prise
localement.

**Verdict § 1 : la source unique n'est PAS atteinte. 3 implémentations génériques subsistent.**

---

## Authentification

| Point d'ouverture de session | Contrôle d'activité |
|---|---|
| `Connexion.razor` — voie directe (ligne 153) | ✅ `if (!utilisateur.EstActif) return false;` |
| `Connexion.razor` — voie 2FA, avant redirection (ligne 133) | ✅ même règle, puis `SignOutAsync()` |
| **`VerifierCode.razor` — validation du second facteur (ligne 105)** | ❌ **aucun** |

`VerifierCode.razor` appelle `SignInManager.TwoFactorAuthenticatorSignInAsync(...)` puis redirige,
sans jamais consulter `EstActif`. L'objet `utilisateurEnAttente` est pourtant déjà chargé à la ligne
97 : le contrôle serait immédiat.

**Exploitabilité, mesurée honnêtement.** Le compte est vérifié *avant* la redirection vers le second
facteur, et `SignInManager.SignOutAsync()` invalide aussi le cookie `TwoFactorUserId`. La fenêtre
résiduelle est donc étroite : une désactivation survenant **entre** la saisie du mot de passe et
celle du code. Dans ce cas :

- une session est bien ouverte — la règle « un compte désactivé ne peut pas ouvrir une nouvelle
  session » est prise en défaut ;
- mais **aucune autorisation ne suit** : `AutorisationService` et `ControleAccesRessourceService`
  refusent, et `ObtenirContexteUtilisateurQuery` renvoie un contexte mort ;
- et la rotation du tampon de sécurité, effectuée à la désactivation, fait rejeter le cookie **dès
  la requête suivante** (`ValidationInterval = TimeSpan.Zero`).

C'est un défaut d'exhaustivité de ma correction, pas une brèche d'autorisation.

---

## Comptes désactivés

| Scénario | Attendu | Constat serveur |
|---|---|---|
| **A** — compte actif → nouvelle connexion | Autorisé selon droits | ✅ `EstActif` vrai, puis règle société pour les clients (CPT-03) |
| **B** — compte désactivé → nouvelle connexion | Refus | ✅ voie directe et voie 2FA amont · ❌ **voie de validation 2FA** |
| **C** — session existante → désactivation → requête suivante | Refus | ✅ **côté serveur**, indépendamment du cookie |

Le scénario C a été vérifié **au niveau de la décision**, pas de l'interface : `AutorisationService`
consulte la garde à chaque appel, et le cas de test correspondant n'implique aucun mécanisme de
session — même identité, même permission, seule l'activité change entre les deux appels.

---

## Sessions existantes

Deux couches, dont **une seule** est la frontière :

1. **Frontière — la décision serveur.** `GardeIdentiteService`, consultée par les deux services
   décisionnels. Fonctionne quel que soit l'état du cookie.
2. **Défense en profondeur — le tampon de sécurité.** `AdministrationComptesRepository` fait tourner
   le `SecurityStamp` dans les deux sens (désactivation et réactivation) ;
   `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero` impose la revalidation à
   chaque requête.

La séparation des rôles est correcte : la couche 2 rend le refus lisible, elle ne le produit pas.

**Réserve.** La rotation du tampon n'a lieu que dans `AdministrationComptesRepository.DefinirActivationAsync`.
La désactivation d'un **collaborateur de société** (`ActiverDesactiverUtilisateurCommand`, qui écrit
`UtilisateurSociete.EstActif`) ne la déclenche pas. Sans conséquence sur l'autorisation — le
rattachement inactif est refusé par `ControleAccesRessourceService` — mais la session survit jusqu'à
son expiration naturelle, là où celle d'un agent est coupée immédiatement. Asymétrie, pas faille.

---

## Autorisation

`AutorisationService.DeciderAsync` — décision positive uniquement si **tout** est réuni :

| Condition | Vérifiée par | Fail-closed |
|---|---|---|
| Identité non vide | `GardeIdentiteService` — `utilisateurId == Guid.Empty` | ✅ refus |
| Compte existant | `compte is null` | ✅ refus |
| Compte actif | `!compte.EstActif` | ✅ refus |
| Permission requise | `PermissionCode == permissionCode` | ✅ permission inconnue ⇒ aucune ligne ⇒ refus |
| Habilitation détenue | `db.Habilitations.AnyAsync(...)` | ✅ refus |
| Portée de ressource | `h.FileId == null \|\| h.FileId == fileId` | ✅ mauvaise file ⇒ refus |

**Ordre vérifié dans le code** : la garde précède la lecture des habilitations. Un test de câblage
compare les positions dans le fichier — l'ordre est traité comme une règle, non comme un détail de
rédaction.

`ControleAccesRessourceService` complète pour les ressources : rattachement absent ou inactif ⇒
refus ; agent sans habilitation ⇒ refus ; dossier sans ticket ⇒ aucun agent admis.

---

## Cloisonnement ressources

| Acteur | Cas | Ticket | Dossier | Enrôlement | Avoir/Exo | Documents | Échanges |
|---|---|---|---|---|---|---|---|
| **Client** | propre ressource | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| | ressource tierce | ✅ refus | ✅ refus | ✅ refus | ✅ refus | ✅ refus | ✅ refus |
| | ENT-03 (RG-CPT-07) | ✅ | ✅ | ✅ | ✅ *(appliqué depuis P2-01)* | 🟠 voie `CloisonnementService` | 🟠 idem |
| | désactivé | ✅ refus | ✅ refus | ✅ refus | ✅ refus | 🟠 non vérifié par la voie client | 🟠 idem |
| **Agent** | bonne file | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| | mauvaise file | ✅ refus | ✅ refus | ✅ refus | 🟠 sans portée file | ✅ refus | ✅ refus |
| | sans habilitation | ✅ refus | ✅ refus | ✅ refus | ✅ refus | ✅ refus | ✅ refus |
| | désactivé | ✅ refus | ✅ refus | ✅ refus | ✅ refus | ✅ refus | ✅ refus |
| **Transporteur** | propre dossier personnel | ✅ | ✅ | ✅ | s.o. | ✅ | ✅ |
| | ressource tierce | ✅ refus | ✅ refus | ✅ refus | s.o. | ✅ refus | ✅ refus |

**Documents.** Deux voies : agent → `ExigerAsync(DocumentTelecharger, ticket.FileCouranteId)`, donc
garde d'identité **et** portée de file ✅ ; client → `CloisonnementService`, sans garde d'identité 🟠.

**Avoir/Exonération.** Le contrôle générique est délégué depuis P2-01 ✅. Le contrôle métier
`avoirexoneration.instruire` reste appelé avec `fileId: null` : tout porteur de la permission accède,
quelle que soit la file. Module mono-file aujourd'hui ; sans effet actuel.

---

## Administration

Les dix pages ont été recontrôlées **au niveau des commandes serveur**.

| Action sensible | Commande | Garde |
|---|---|---|
| Création de compte | `CreerCompteAgentCommand` | `ExigerAsync(AdministrationComptes)` → garde |
| Modification de compte | `DefinirActivationCompteCommand` | idem |
| Attribution / retrait d'habilitation | `GererHabilitationAgentCommand` | idem |
| Attribution de rôle | `GererHabilitationAgentCommand` | idem |
| Désactivation · réactivation | `DefinirActivationCompteCommand` | idem |
| Paramétrage (SLA, matrice, escalade, pièces, périmètres, motifs, compétences) | 8 commandes | `ExigerAsync` → garde |
| **Auto-réactivation** | `DefinirActivationCompteCommand` | **refusée** — la garde rejette l'appelant avant toute lecture de la cible |

**Un administrateur désactivé ne peut effectuer aucune de ces actions.** La chaîne d'escalade du
P0-ter est rompue à son deuxième maillon, celui qui compte : `AutorisationService`.

---

## Notes internes

| Surface | Filtrage | Accès à la ressource lui-même autorisé ? |
|---|---|---|
| Fiche ticket | `estClient ? ListerVisiblesParClient : ListerParTicket` | ✅ `ExigerConsultationTicketAsync` **avant** toute lecture |
| Fiche enrôlement | même patron | ✅ contrôle central **puis** `enrolement.instruire` |
| Fiche avoir/exo | `estAgent \|\| Type != NoteInterne` — clé sur la **permission** | ✅ contrôle central |

**La question posée au § 6 de la mission — l'accès à la ressource est-il lui-même autorisé — reçoit
une réponse positive sur les trois surfaces.** Un client ou un transporteur (nature `Client`) est
filtré ; un agent non habilité n'atteint pas la ressource, donc pas le filtre.

**Réserve de forme.** Sur la fiche ticket et la fiche enrôlement, le filtre s'appuie sur la
**nature** de l'appelant, alors que la fiche avoir/exo s'appuie sur une **permission** — la seconde
est plus rigoureuse. Sans effet aujourd'hui, la garde amont excluant déjà les non-admis.

---

## Surface IDOR / BOLA

Balayage complet sur `GetById`, `GetByNumero`, `FindAsync`, `SingleOrDefault`, `FirstOrDefault`,
`DbContext`, `ListerParTicket`, `ListerParDossier`, `IServiceScopeFactory`, `ObtenirFiche`,
`Consulter`, `Télécharger`, `Document`, `Historique`, `Echange`.

| Surface | Décision atteinte |
|---|---|
| 4 fiches (ticket, dossier, enrôlement, avoir/exo) | ✅ contrôle central |
| 3 endpoints API | ✅ `controleAcces` (2) · calcul sans lecture de ressource (1) |
| `IndicateursTemps.razor` — seule page lisant le `DbContext` directement | ✅ `PeutConsulterLeTicketAsync` |
| Listes par ticket (messages, historique, documents, liaisons) | ✅ appelées **uniquement** depuis les 4 fiches gardées |
| 15 requêtes de liste de tickets | ✅ 14 gardées · ❌ `ObtenirDashboardAgentQuery` |
| Téléchargement de document — voie agent | ✅ `ExigerAsync` + portée file |
| Téléchargement de document — voie client | 🟠 `CloisonnementService` |
| 11 autres consommateurs de `CloisonnementService` | 🟠 idem |
| Ouverture de session — `VerifierCode.razor` | ❌ aucun contrôle d'activité |

**Surfaces sensibles n'atteignant pas la garde centrale : 14** — les 12 consommateurs de
`CloisonnementService`, `ObtenirDashboardAgentQuery`, et `VerifierCode.razor`.

Aucune ne constitue un IDOR exploitable : les chemins `CloisonnementService` sont atteints depuis
des pages dont la lecture préalable est gardée, le tableau de bord n'expose que les tickets propres
de l'agent, et la session 2FA n'ouvre aucune autorisation.

---

## Validation fixtures

Échantillon des 107 fixtures complétées, et lecture du helper `ComptesDeTest` :

| Point de contrôle | Constat |
|---|---|
| Un utilisateur authentifié possède un compte Identity | ✅ le helper insère un `ApplicationUser` réel |
| Une habilitation agent pointe vers un compte réel | ✅ l'identifiant est enveloppé au site d'attribution |
| Un rattachement client pointe vers un compte réel | ✅ même traitement, avec `NatureUtilisateur.Client` |
| **Le helper ne crée aucune permission supplémentaire** | ✅ il n'écrit que `Users` — aucune ligne `Habilitations` |
| Idempotence | ✅ `Users.Local.Any(...) \|\| Users.Any(...)` avant insertion |
| Les assertions métier restent inchangées | ✅ les diffs ne portent que sur les lignes d'attribution |
| Le compte créé est **actif** | ✅ les scénarios éprouvent donc ce qu'ils éprouvaient |

**Une exception délibérée et correcte** : `EtatCompteEtAutorisationTests` n'utilise pas le helper.
Son cas 10 attribue justement une habilitation à un identifiant **sans compte** — matérialiser le
compte l'aurait vidé de son objet. Le commentaire du fichier le dit explicitement.

---

## Tests de câblage

Preuves de mutation existantes, réutilisées sans relance ni modification :

| Mutation | Régression simulée | Détection |
|---|---|---|
| A — `AutorisationService` ignore la garde | Le P0-ter lui-même | 8 tests (7 fonctionnels + 1 câblage) |
| B — `GardeIdentiteService` ignore `EstActif` | Désactivation redevenue décorative | 11 tests (10 + 1) |
| C — Fiche enrôlement contourne le contrôle central | Réimplémentation locale | 3 tests (1 + 2) |

Les cinq régressions listées au § 8 de la mission sont couvertes :

| Régression | Test qui tombe |
|---|---|
| `EstActif` n'est plus pris en compte | `La_garde_d_identite_refuse_compte_inconnu_vide_ou_desactive` + mutation B |
| Le contrôle central est retiré d'une fiche | `Chaque_fiche_delegue_le_controle_generique` (4 jeux) + mutation C |
| Une permission est accordée sans compte existant | `Cas10_Compte_inconnu_avec_habilitation_residuelle_est_refuse` |
| Une commande admin contourne la garde | `Les_commandes_d_administration_des_comptes_exigent_une_permission` + cas 4 et 5 |
| Un agent de la mauvaise file est accepté | `CloisonnementAgentFichesTests` cas 04, 07, 08 |

**Lacune de couverture.** Aucun test de câblage ne protège les deux variantes parallèles :
`CloisonnementService` et `ObtenirDashboardAgentQuery` peuvent évoluer sans qu'aucun test ne
s'oppose, et rien ne détecterait l'ajout d'un treizième consommateur de `CloisonnementService`.
De même, aucun test ne couvre le contrôle d'activité sur la voie 2FA.

---

## Performance / compromis session

`SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`.

**Bénéfice sécurité.** Invalidation effective de la session à la première requête suivant la
désactivation. Le défaut d'Identity — 30 minutes — laisserait une demi-heure pendant laquelle
l'intéressé conserverait une session vivante, ce qui contredit le mot « immédiatement » de SEC-11.

**Coût.** Une lecture du compte par requête authentifiée, via `SignInManager.ValidateSecurityStampAsync`
(lecture indexée sur la clé primaire de `AspNetUsers`). Sur Blazor Server, le coût est moindre que
sur une application par requêtes HTTP classiques : la négociation de circuit limite le nombre de
validations réelles.

**Impact potentiel.** Une charge en pointe amplifie le nombre de lectures ; le risque est une
pression sur la base, non sur la latence perçue. Aucune mesure n'a été faite — ce point est une
analyse de conception, pas un résultat de test.

**Optimisation future compatible avec l'invalidation immédiate.** Un cache court du couple
(utilisateur, tampon) invalidé explicitement par la commande de désactivation conserverait
l'immédiateté tout en supprimant la lecture par requête. À condition que l'invalidation soit
diffusée à toutes les instances — sinon l'immédiateté est perdue, et le remède devient une
régression de sécurité.

**Classement : P3.** La sécurité sur cet axe est correcte ; il ne s'agit que d'un coût.

---

## Risques résiduels

| # | Risque | Gravité | Exploitable |
|---|---|---|---|
| **R1** | **`CloisonnementService` — seconde implémentation générique, 12 consommateurs, sans garde d'identité, avec une règle de propriété plus faible que la centrale** | Élevée (architecture) | Non en l'état |
| **R2** | **L'interface passe `SocieteId` comme identité cliente** — le contrôle se compare à une valeur fournie par l'appelant, et l'auteur des messages clients devient la société | Moyenne | Non depuis l'IHM |
| **R3** | **`VerifierCode.razor` n'applique aucun contrôle d'activité** — une session peut s'ouvrir pour un compte désactivé entre la saisie du mot de passe et celle du code | Faible | Fenêtre étroite, session sans autorisation |
| **R4** | **`ObtenirDashboardAgentQuery` décide localement du périmètre de files** | Faible | Non — page gardée, exposition limitée aux tickets propres |
| **R5** | Aucun test de câblage sur R1, R3, R4 | Moyenne | — |
| **R6** | La rotation du tampon ne couvre pas la désactivation d'un collaborateur de société | Faible | Non — le rattachement inactif est refusé |
| **R7** | `avoirexoneration.instruire` vérifiée sans portée de file | Faible | Non — module mono-file |
| **R8** | Notes internes filtrées sur la **nature** (ticket, enrôlement) et sur la **permission** (avoir/exo) | Faible | Non |
| **R9** | `SocieteId` fourni par l'appelant côté tarification (P2-02, hors périmètre) | Faible | Non depuis l'IHM |
| **R10** | Journalisation des connexions absente (P2-03) — un refus par compte désactivé n'est pas tracé | Moyenne | — |
| **R11** | Aucune vérification en base réelle ; R-15 toujours ouvert | — | — |

---

## Décision finale P0

# 🟠 P0 = FERMÉ AVEC RISQUE RÉSIDUEL

**Pourquoi pas 🔴 RÉOUVERT.** Aucun accès non autorisé n'est atteignable. Les trois vulnérabilités
nommées sont fermées, et chacune a été re-vérifiée sur le chemin de décision :

- **P0** — les endpoints `/api/v1` délèguent au contrôle central ; la condition fail-open d'origine
  (`ctx.SocieteId.HasValue &&`) n'est pas réapparue, et un test de câblage l'interdit.
- **P0-bis** — les quatre fiches délèguent ; un agent sans habilitation, d'une mauvaise file, ou
  désactivé est refusé sur ticket comme sur dossier ; les notes internes ne sont jamais servies à un
  appelant non admis.
- **P0-ter** — `AutorisationService` consulte la garde **avant** les habilitations ; un administrateur
  désactivé ne peut plus rien administrer, ni se réactiver ; la session existante ne permet plus
  d'agir, indépendamment du cookie.

**Pourquoi pas 🟢 FERMÉ.** Le § 1 de la mission exige une source unique. Elle n'est pas atteinte :
**3 implémentations génériques** subsistent au lieu d'une, dont `CloisonnementService` qui sert
12 commandes et requêtes de la voie client sans jamais consulter la garde d'identité, avec une règle
de propriété que l'interface neutralise en lui fournissant la valeur qu'elle doit comparer.

Ce n'est pas une brèche — c'est la même configuration qui a précédé P0, P0-bis et P0-ter : une
décision générique dupliquée, plus faible que la centrale, protégée en pratique par ce qui
l'entoure. Les trois défauts précédents sont nés exactement de là. Déclarer 🟢 reviendrait à
répéter l'erreur de mon rapport P0-ter, qui a annoncé une source unique après n'avoir inventorié que
les fiches.

**Condition de passage à 🟢** : rallier les 12 consommateurs de `CloisonnementService` et
`ObtenirDashboardAgentQuery` à `ControleAccesRessourceService`, supprimer le contournement
`SocieteId` de l'interface, appliquer le contrôle d'activité sur la voie 2FA, et couvrir le tout par
des tests de câblage validés par mutation.

*Aucune correction n'a été apportée : cet audit est en lecture seule. Le score global n'est pas
recalculé. Aucun P2/P3 n'a été traité.*
