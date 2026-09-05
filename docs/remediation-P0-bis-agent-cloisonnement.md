# Remédiation P0-bis — Cloisonnement agent sur les fiches Ticket / Dossier

*Phase de correction ciblée, ouverte par le contre-audit `docs/contre-audit-P0-P1-3FN.md`.*
*Périmètre strict : cette vulnérabilité. Aucun P1/P2/P3 traité, lot 3FN non touché, script R-15 non créé, score global non recalculé.*

---

## 1. Vulnérabilité

`ObtenirFicheTicketQuery` et `ObtenirFicheDossierQuery` — les deux points d'entrée par lesquels
l'application sert une fiche complète — ne portaient de contrôle d'accès que pour les appelants de
nature **Client**. Toute autre nature obtenait la fiche entière.

Étaient donc lisibles, par simple substitution de l'identifiant dans l'URL :

- **par un agent sans aucune habilitation** — un compte agent créé et non encore doté de droits ;
- **par un agent habilité sur une autre file** — SEC-03 borne l'agent à ses files, ce que rien
  n'appliquait ici ;
- **par un agent désactivé** dont le compte n'avait pas été purgé ;
- **par un identifiant inconnu**, cas le plus net : n'étant pas « client », il n'était pas contrôlé.

La fiche ticket expose l'historique, les documents, **et les notes internes** — les échanges que
les agents tiennent hors de la vue du client. La fuite ne portait donc pas seulement sur des
données client, mais sur la délibération interne de CIT.

Classification : **IDOR / BOLA**, non-conformité **CPT-04** (« un client n'accède qu'à ses propres
dossiers ») et **SEC-03** (« un agent n'accède qu'aux files et actions de son rôle »).

---

## 2. Cause racine

La cause n'est pas l'absence d'une branche `else if (estAgent)`. Ajouter cette branche aurait
corrigé le symptôme en reconduisant le défaut.

La cause est que **la décision d'autorisation était réécrite sur place, une fois de plus**. Le
projet en comptait déjà trois implémentations distinctes — les endpoints `/api/v1`, la page
`IndicateursTemps`, ces deux requêtes de fiche — et **elles avaient divergé** : la remédiation P0
avait centralisé les deux premières sans que la troisième soit rejointe. Chaque copie a sa propre
notion de « qui est autorisé » ; il suffit qu'une seule raisonne par exclusion (« ce n'est pas un
client, donc on laisse passer ») pour rouvrir la porte que les autres ferment.

Deux causes secondaires, révélées en corrigeant :

- **L'ordre des opérations.** La garde s'exécutait *après* la résolution de la société. Un appelant
  non autorisé déclenchait donc des lectures et recevait un message d'erreur citant un identifiant
  interne. Un refus ne doit rien apprendre.
- **La société était supposée obligatoire.** `societeRepository.ObtenirParIdAsync(dossier.ClientId) ?? throw`
  traitait `ClientId` comme toujours société. Pour un dossier d'Enrôlement, `ClientId`
  est un **utilisateur** : la fiche échouait sur une exception « société introuvable » pour son
  propre propriétaire légitime.

---

## 3. Surface affectée

| Surface | Nature | État avant |
|---|---|---|
| `Application/Tickets/ObtenirFicheTicketQuery.cs` | Fiche ticket complète (historique, documents, messages, notes internes) | **Vulnérable** |
| `Application/Dossiers/ObtenirFicheDossierQuery.cs` | Fiche dossier + tickets rattachés | **Vulnérable** |
| `Web/Components/Pages/Tickets/FicheTicket.razor` | Consommatrice de la 1ʳᵉ | Vulnérable par transitivité |
| `Web/Components/Pages/Dossiers/FicheDossier.razor` | Consommatrice de la 2ᵈᵉ | Vulnérable par transitivité |

Les deux requêtes étant les seules sources de ces fiches, la correction en amont couvre les pages
sans les modifier.

---

## 4. Reproduction avant correction

Conformément à la consigne, la reproduction précède la correction et échoue **sur une assertion**,
non sur une erreur de compilation : les tests ont été écrits contre la signature d'alors et
constatent le comportement réel.

Exécution de `CloisonnementAgentFichesTests` **avant** toute modification du code de production :

```
échec : 10, réussite : 7, total : 17
```

Les dix échecs sont les dix accès qui auraient dû être refusés et ne l'étaient pas — notamment
`Cas01` (agent sans habilitation), `Cas04` (agent d'une autre file), `Cas05` (agent désactivé),
`Cas09` (notes internes servies à un agent non habilité) et `Cas13` (utilisateur inconnu). Les sept
réussites sont les cas client, déjà couverts par la branche existante.

---

## 5. Correction

La décision n'est **pas** réécrite dans les fiches. Elle est déléguée au service central introduit
en remédiation P0, `ControleAccesRessourceService` — la troisième implémentation est supprimée, pas
remplacée par une quatrième.

Trois modifications, dans `ObtenirFicheTicketQuery` comme dans `ObtenirFicheDossierQuery` :

**a. Délégation.** Un paramètre `ControleAccesRessourceService controleAcces` est ajouté au
constructeur ; la garde locale disparaît au profit de `ExigerConsultationTicketAsync` /
`ExigerConsultationDossierAsync`.

**b. L'autorisation précède toute autre lecture.**

```csharp
// ORDRE (remédiation P0-bis) : l'autorisation précède TOUTE autre lecture. Elle
// s'exécutait auparavant après la résolution de la société, si bien qu'un appelant non
// autorisé déclenchait une requête et recevait un message citant un identifiant interne.
// Un refus ne doit rien apprendre.
await controleAcces.ExigerConsultationTicketAsync(appelantId, ticket, ct).ConfigureAwait(false);
```

**c. La société devient facultative**, le dossier personnel étant un cas légitime :

```csharp
// Un dossier PERSONNEL (Enrôlement) porte un utilisateur dans ClientId, non une société :
// l'absence de société y est normale et ne doit pas faire échouer la lecture.
var societe = await societeRepository.ObtenirParIdAsync(dossier.ClientId, ct).ConfigureAwait(false);
```

Les projections en aval retombent sur des valeurs neutres (`societe?.InformationsDeclarees ?? string.Empty`,
`SocieteProfil: societe?.Profil ?? TypeClient.Normal`).

**Durcissements apportés au service central lui-même :**

- **SEC-11 — compte désactivé.** La désactivation n'avait aucun effet sur la lecture, ce qui la
  vidait de son sens. Toute décision commence désormais par :

  ```csharp
  var info = await utilisateurs.ObtenirInfoAsync(utilisateurId, ct).ConfigureAwait(false);
  if (info is null || !info.EstActif) { return false; }
  ```

  Ce contrôle couvre du même coup l'identifiant inconnu (`Cas13`). Il a nécessité une sixième
  dépendance, `IUtilisateurInfoRepository`.

- **Propriété nominative uniquement.** Une première version de ma correction acceptait aussi
  `dossier.AuteurId == utilisateurId`. C'était une faille : un collaborateur dont le rattachement
  avait été désactivé continuait de lire les dossiers qu'il avait déposés, en contournant le
  contrôle de rattachement placé juste en dessous. Seul `dossier.ClientId == utilisateurId` établit
  désormais la propriété personnelle ; l'auteur d'un dossier de société passe par la voie société,
  qui vérifie que son rattachement est toujours actif.

**Nettoyage induit.** `IUtilisateurSocieteRepository` n'étant plus lu par les deux fiches — le
rattachement est résolu par le service central — le paramètre a été retiré de leurs constructeurs
et de leurs onze sites de construction. Laisser une dépendance morte laisserait entendre que la
requête effectue encore son propre contrôle : précisément la confusion à l'origine du défaut. Les
avertissements `CS9113` correspondants ont disparu.

---

## 6. Contrôle Agent

La règle retenue, explicitée parce qu'elle n'allait pas de soi :

**Ticket** — l'agent est admis s'il porte au moins une habilitation couvrant `FileCouranteId`
(habilitation non bornée, ou bornée à cette file précise), ou si son périmètre de supervision la
couvre. **La nature « Agent » n'est pas un droit** : un agent sans aucune habilitation n'accède à
rien.

**Dossier** — un dossier n'appartient à aucune file ; ce sont ses tickets qui en portent une. Un
agent y accède donc **si et seulement s'il accède à au moins un de ses tickets** :

```csharp
var ticketsDuDossier = await tickets.ListerParDossierAsync(dossier.Id, ct).ConfigureAwait(false);
foreach (var ticket in ticketsDuDossier)
{
    if (await AgentEstAdmisAsync(utilisateurId, ticket.FileCouranteId, ct).ConfigureAwait(false))
    {
        return true;
    }
}
return false;
```

Ma première version admettait tout agent porteur d'une habilitation quelconque. Elle ouvrait les
dossiers de files qu'il n'instruit pas — contraire à SEC-03. Un dossier sans aucun ticket n'est
ouvert à aucun agent : il n'y a rien à y instruire.

---

## 7. Protection des notes internes

Le filtrage repose sur la nature de l'appelant :

```csharp
IReadOnlyList<Message> msgs = estClient
    ? await messageRepository.ListerVisiblesParClientAsync(ticketId, ct).ConfigureAwait(false)
    : await messageRepository.ListerParTicketAsync(ticketId, ct).ConfigureAwait(false);
```

Ce filtre était correct et reste inchangé : un client n'a jamais vu de note interne, y compris avant
la correction. La fuite ne venait pas de lui mais de l'absence de garde en amont — un agent non
habilité, voire un identifiant inconnu, atteignait la branche « non-client » et recevait le fil
complet. La garde placée avant toute lecture ferme cette voie : seul un appelant déjà admis atteint
ce filtre.

Trois tests verrouillent les trois faces de la règle : `Cas09` (agent non habilité — aucune note,
car aucun accès), `Cas09bis` (agent habilité — les notes lui sont bien servies, la correction ne
doit pas dégrader son travail), `Cas09ter` (client propriétaire — jamais de note interne).

---

## 8. Tests ajoutés

`tests/CIT.Plateforme.Application.Tests/Securite/CloisonnementAgentFichesTests.cs` — **17 tests**,
sur base réelle (`PlateformeDbContext`), avec fixture à deux files, deux sociétés, un transporteur
sans société, et quatre profils d'agent (habilité file A, habilité file B, sans habilitation,
désactivé).

Les douze cas exigés :

| Cas | Attendu |
|---|---|
| `Cas01` Agent sans habilitation → ticket | Refus |
| `Cas02` Agent sans habilitation → dossier | Refus |
| `Cas03` Agent habilité sur la file du ticket | Accès |
| `Cas04` Agent habilité sur une autre file | Refus |
| `Cas05` Agent désactivé malgré habilitation | Refus |
| `Cas06` Agent sans aucun rôle | Refus |
| `Cas07` Agent avec rôle mais sans rattachement à la file | Refus |
| `Cas08` Agent d'une autre file → dossier | Refus |
| `Cas09` Agent non habilité → notes internes | Refus |
| `Cas10` Client propriétaire | Accès |
| `Cas11` Client non propriétaire | Refus |
| `Cas12` Transporteur → ticket d'un client | Refus |

Cinq cas ajoutés pour couvrir les zones que ces douze laissaient ouvertes — sans eux, une
correction trop large (tout refuser) ou trop étroite serait passée :

| Cas | Attendu | Ce qu'il verrouille |
|---|---|---|
| `Cas08bis` Agent habilité sur la file d'un des tickets du dossier | **Accès** | Que la règle dossier ne dégénère pas en refus systématique |
| `Cas09bis` Agent habilité → notes internes | **Accès** | Que la correction ne prive pas les agents de leur outil |
| `Cas09ter` Client propriétaire → notes internes | Refus | La face client de la règle |
| `Cas12bis` Transporteur → son propre ticket | **Accès** | Le dossier personnel d'Enrôlement (`ClientId` = utilisateur) |
| `Cas13` Utilisateur inconnu | Refus | Le cas le plus direct de l'ancienne faille |

---

## 9. Tests de câblage

`tests/CIT.Plateforme.Web.Tests/Securite/CablageControleAccesTests.cs` — **7 méthodes, 9 cas**
(deux `[Theory]` à deux jeux).

Ces tests lisent les sources. C'est inhabituel et délibéré : le projet a produit **deux fois** le
même type de défaut — une règle écrite, testée, déclarée conforme, et pourtant sans effet
(`MarquerEtatEngageant()` sans appelant ; la garde des fiches sans branche agent). Dans les deux
cas aucun test ne tombait, parce qu'ils éprouvaient le comportement là où la condition était posée
à la main, jamais le câblage lui-même.

- `FicheTicket_appelle_le_controle_central` / `FicheDossier_…` — tombent si l'appel est retiré.
- `Aucune_garde_d_acces_n_est_reservee_a_la_branche_client` — tombe si un `if (estClient)` levant
  `AccesRefuseException` réapparaît : la forme exacte du défaut P0-bis.
- `L_autorisation_precede_la_resolution_de_la_societe` — compare les positions dans le fichier.
- `Le_controle_central_reste_fail_closed` — vérifie les quatre refus par défaut.
- `La_nature_agent_n_est_pas_une_autorisation` — vérifie que les habilitations sont bien interrogées.
- `Les_endpoints_api_delèguent_au_controle_central` — vérifie que `ctx.SocieteId.HasValue &&`, la
  condition fail-open du P0 d'origine, ne réapparaît pas.

**Validation par mutation.** Ces tests n'ont de valeur que s'ils tombent. Vérification faite :
la suppression de l'appel `ExigerConsultationTicketAsync` fait échouer **2 tests de câblage et 8
tests fonctionnels**. Le fichier a ensuite été restauré et l'appel recompté.

---

## 10. Recherche de variantes (§ 8 de la mission)

Balayage du dépôt sur `ObtenirFiche*`, `Consulter*`, `GetById*`, `GetByNumero*`, `ListerParTicket*`,
`ListerParDossier*`, `DbContext` direct, `IServiceScopeFactory`, `FindAsync`, `FirstOrDefault`,
`SingleOrDefault`.

| Surface | Verdict |
|---|---|
| `ObtenirFicheTicketQuery`, `ObtenirFicheDossierQuery` | **Corrigées ici** |
| Endpoints `/api/v1` (`ApiEndpoints.cs`) | Délèguent au contrôle central (P0) |
| `Pages/Tickets/IndicateursTemps.razor` — seule page routable lisant le `DbContext` directement (`db.Tickets.FindAsync`) | Gardée : appelle `PeutConsulterLeTicketAsync` (variante corrigée en P0) |
| `Tarification/ConsulterSimulationProformaQuery` | Gardée — refus si `simulation.SocieteId != requete.SocieteId` |
| `Tarification/GenererDocumentProformaPdfQuery` | Gardée — même contrôle |
| `Comptes/VerifierAccesCompteQuery` | Sans objet — c'est elle-même une décision d'autorisation |
| `Web/ObtenirContexteUtilisateurQuery` | Sans objet — construction du contexte de session |

Aucune autre lecture de ressource exposée à l'utilisateur n'est dépourvue de décision d'autorisation.

---

## 11. Résultats

**Avant correction** — `CloisonnementAgentFichesTests` : `échec : 10, réussite : 7, total : 17`.

**Après correction** — suite complète de la solution :

| Projet | Résultat |
|---|---|
| `CIT.Plateforme.Domain.Tests` | 208 / 208 |
| `CIT.Plateforme.Application.Tests` | 651 / 651 |
| `CIT.Plateforme.Infrastructure.Tests` | 139 / 139 |
| `CIT.Plateforme.Web.Tests` | 296 / 296 |
| **Total** | **1 294 / 1 294 — 0 échec** |

**Non-régression P0 (§ 10 de la mission).** Les 12 tests de `ApiCloisonnementTests` restent verts,
sans modification. Aucune assertion n'a été affaiblie, aucun test supprimé, aucune couverture réduite.

**Deux fixtures complétées, et pourquoi.** `FicheTicketTests.FicheTicket_RenduComplet_…` et
`FicheDossierTests.FicheDossier_RenduComplet_…` échouaient après correction. Vérification faite
avant de les toucher : `ObtenirInfoAsync` lit la table **Identity**, dont tout utilisateur
authentifié possède une ligne — le cookie en est émis. Ces deux fixtures authentifiaient un
identifiant **sans compte correspondant**, situation qui ne se produit en production que si le
compte a été supprimé alors qu'un cookie survivait : le refus y est alors le comportement voulu.
La fixture était donc incomplète, non la correction. Le compte Identity manquant a été ajouté ;
**aucune assertion n'a été modifiée** et l'objectif métier des deux tests — le rendu complet de la
fiche pour un client autorisé — est inchangé.

---

## 12. Risques résiduels

1. **Quatre implémentations réduites à deux, pas à une.** Les deux requêtes de tarification
   (`ConsulterSimulationProformaQuery`, `GenererDocumentProformaPdfQuery`) comparent elles-mêmes un
   `SocieteId` **fourni par l'appelant**, au lieu de le dériver de l'identité. Les appelants actuels
   sont des pages qui le tirent du contexte serveur, donc non exploitable depuis l'IHM — mais le
   motif est celui-là même qui a produit ce P0-bis. À rattacher au contrôle central lors de la
   refonte P2 de centralisation, non traitée ici sur consigne.

2. **Le filtre des notes internes est fail-open pris isolément.** `estClient = appelantInfo?.Nature == NatureUtilisateur.Client` :
   un `appelantInfo` nul donnerait `estClient = false`, donc la vue agent. Ce chemin est aujourd'hui
   **inatteignable**, la garde placée avant refusant déjà tout appelant introuvable. La protection
   repose donc sur l'ordre des opérations — ce que verrouille
   `L_autorisation_precede_la_resolution_de_la_societe`.

3. **Coût de la règle dossier.** L'admission d'un agent parcourt les tickets du dossier, soit une
   lecture d'habilitations par ticket. Acceptable au volume actuel ; à surveiller si un dossier
   venait à porter beaucoup de tickets.

4. **Aucune vérification en base réelle.** Tous les tests tournent sur `InMemory` ou LocalDB.
   Aucune migration n'a été exécutée sur une base réelle, conformément aux consignes.

5. **Le risque R-15 reste ouvert** — hors périmètre de cette phase, script non créé.

---

## 13. Conclusion

La vulnérabilité est fermée sur ses deux surfaces, et fermée **par suppression d'une
réimplémentation** plutôt que par ajout d'une branche. La décision d'accès aux ressources de
dossier vit désormais en deux endroits au lieu de quatre, dont un seul — le service central — pour
tout ce qui touche tickets et dossiers.

Trois enseignements méritent d'être retenus au-delà de ce correctif :

- **Le refus par défaut ne se raisonne pas par exclusion.** « Ce n'est pas un client, donc on
  laisse passer » est la formulation exacte du défaut. Une information d'autorisation absente ne
  vaut jamais autorisation.
- **Centraliser à moitié est pire que ne pas centraliser.** La remédiation P0 avait rallié deux
  implémentations sur trois ; la troisième, restée en place, a continué de diverger en silence.
- **Une règle écrite et testée peut n'avoir aucun effet.** C'est le deuxième cas dans ce projet.
  Les tests de câblage du § 9 existent pour que le troisième tombe tout de suite.

Deux points restent à traiter hors de cette phase : la centralisation des requêtes de tarification
(P2) et le risque R-15. Le score global de conformité n'est pas recalculé ici.
