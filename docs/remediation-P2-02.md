# Remédiation P2-02 — Identité et société fournies par l'appelant (Tarification)

## Exigence

**RG-SEC-01** (moindre privilège), **RG-CPT-04** (« un compte client ne peut consulter ou agir que
sur ses propres tickets, documents et échanges ») et **RG-CPT-07 / ENT-03** (visibilité selon les
droits accordés : ses seules demandes, ou l'ensemble de celles de sa société).

Principe posé par la mission : **l'identité de sécurité ne doit jamais venir de l'UI.** L'utilisateur
connecté, sa société, ses droits ENT-03 et la propriété de la ressource se résolvent côté serveur.

---

## Défaut

Quatre surfaces recevaient un `SocieteId` **de l'appelant** et s'en servaient comme preuve de
propriété.

| Surface | Défaut |
|---|---|
| `ConsulterSimulationProformaQuery` | `if (simulation.SocieteId != requete.SocieteId) throw` — deux valeurs, dont la seconde vient du client |
| `GenererDocumentProformaPdfQuery` | même comparaison |
| `ListerSimulationsProformaQuery` | listait les simulations de la société **demandée**, sans aucun contrôle |
| `SauvegarderSimulationProformaCommand` | enregistrait la simulation au nom de la société **fournie** |

Aucune ne vérifiait l'existence du compte, son activité, son rattachement, ni ENT-03.

---

## Reproduction

`tests/CIT.Plateforme.Application.Tests/Tarification/IdentiteTarificationTests.cs`, écrite contre le
contrat d'alors et échouant **sur assertion**, non sur compilation :

```
échec : 6, réussite : 3, total : 9
```

| Cas | Attendu | Avant correction |
|---|---|---|
| 01 — l'auteur consulte sa simulation | Accès | ✅ passait (témoin) |
| 02 — collaborateur société autorisé liste | Accès | ✅ passait (témoin) |
| **03 — collaborateur restreint (ENT-03)** | Refus | ❌ **accédait** |
| 04 — client d'une autre société | Refus | ✅ passait |
| **05 — société forgée** | Refus | ❌ **accédait** |
| **06 — liste avec société forgée** | Vide | ❌ **listait les simulations d'autrui** |
| **07 — compte sans rattachement** | Refus | ❌ **accédait** |
| **08 — compte désactivé** | Refus | ❌ **accédait** |
| **09 — compte inconnu** | Refus | ❌ **accédait** |

Le cas 05 est décisif : le client B obtenait la simulation de la société A en transmettant
simplement `SocieteA`. Il fournissait la valeur que le contrôle attendait.

---

## Cause racine

**Une décision fondée sur une donnée fournie par celui qu'elle doit contrôler n'est pas une
décision.** C'est le motif du P0 d'origine (`ctx.SocieteId.HasValue &&`) et celui que la
consolidation vient de fermer sur la voie client (`_fiche.SocieteId` passé comme identité) — ici
sous sa forme la plus directe, puisque la société figurait dans le **contrat** de la requête.

---

## Correction

**Aucune nouvelle implémentation générique.** La résolution de société rejoint le contrôle central,
qui portait déjà cette logique pour les dossiers.

`ControleAccesRessourceService` expose désormais :

```csharp
public sealed record PorteeSociete(Guid SocieteId, Guid? AuteurIdRestriction);

public async Task<PorteeSociete?> ResoudrePorteeSocieteAsync(Guid utilisateurId, CancellationToken ct = default)
{
    var info = await garde.ObtenirCompteUtilisableAsync(utilisateurId, ct);   // identité + activité
    if (info is null) { return null; }

    var rattachement = await rattachements.ObtenirParUtilisateurAsync(utilisateurId, ct);
    if (rattachement is null || !rattachement.EstActif) { return null; }

    var visibiliteTotale = rattachement.EstAdministrateur
        || rattachement.DroitConsultation == DroitConsultation.ToutesDemandesSociete;

    return new PorteeSociete(rattachement.SocieteId, visibiliteTotale ? null : utilisateurId);
}

public async Task<bool> PeutConsulterRessourceDeSocieteAsync(
    Guid utilisateurId, Guid societeProprietaire, Guid? auteurRessource, CancellationToken ct = default)
```

**Les contrats perdent leur champ de société** — la forge n'est plus exprimable :

| Contrat | Avant | Après |
|---|---|---|
| `ConsulterSimulationProformaRequete` | `(AppelantId, SocieteId, SimulationId)` | `(AppelantId, SimulationId)` |
| `GenererDocumentProformaPdfRequete` | `(AppelantId, SocieteId, SimulationId)` | `(AppelantId, SimulationId)` |
| `ListerSimulationsProformaRequete` | `(AppelantId, SocieteId, Page, TaillePage)` | `(AppelantId, Page, TaillePage)` |
| `SauvegarderSimulationProformaCommande` | `(AuteurId, SocieteId, Requete)` | `(AuteurId, Requete)` |

La liste applique en outre ENT-03 sur les résultats :

```csharp
if (portee.AuteurIdRestriction is Guid auteur)
{
    simulations = [.. simulations.Where(s => s.AuteurId == auteur)];
}
```

Et la sauvegarde rattache la simulation à la société **résolue**, non à celle reçue.

---

## Tests

| Suite | Résultat |
|---|---|
| `IdentiteTarificationTests` *(créée, 9 cas)* | **9 / 9** |
| Suites tarification existantes (cloisonnement, consultation, PDF, gestion, golden masters) | vertes |
| **Sécurité** — API, câblage, agent/fiches, état de compte, identité appelante, tableau de bord | **78 / 78** |
| **Suite globale** | **1 343 / 1 343** |

Le cas 05 est désormais exprimé sur la **forme du contrat** : aucun des quatre types ne porte de
propriété `SocieteId` ou `ClientId`. C'est cette absence qui constitue la correction — pas un effet
de bord de celle-ci.

**Fixtures adaptées.** Les suites de tarification appelaient avec `Guid.NewGuid()` — un appelant
anonyme, qui n'a désormais aucun périmètre. Le helper `FabriqueRequetes.AppelantDeSociete(db, societeId)`
crée un compte actif rattaché à la société visée. Aucune assertion métier n'a été modifiée ; seul le
type d'exception attendu change (`UnauthorizedAccessException` → `AccesRefuseException`), l'objectif
de refus restant identique.

---

## Mutations

Aucune mutation dédiée n'a été jouée pour ce lot : **la correction est structurelle**, et le test du
cas 05 la vérifie directement. Retirer la résolution de société ne peut se faire qu'en réintroduisant
un champ dans les contrats, ce que ce test interdit ; retirer l'appel à
`ExigerConsultationRessourceDeSocieteAsync` fait tomber les cas 03, 04, 06, 07, 08 et 09.

---

## Fichiers modifiés

**Production — 6**

| Fichier | Nature |
|---|---|
| `Application/Securite/ControleAccesRessourceService.cs` | `ResoudrePorteeSocieteAsync` + `PeutConsulter/ExigerConsultationRessourceDeSocieteAsync` |
| `Application/Tarification/ConsulterSimulationProformaQuery.cs` | contrat + délégation |
| `Application/Tarification/GenererDocumentProformaPdfQuery.cs` | contrat + délégation |
| `Application/Tarification/ListerSimulationsProformaQuery.cs` | contrat + périmètre résolu + ENT-03 |
| `Application/Tarification/SauvegarderSimulationProformaCommand.cs` | contrat + société résolue |
| `Web/Components/Pages/Tarification/*.razor` (3 pages) | ne transmettent plus la société |

**Tests — 1 créé, 6 modifiés** (dont `FabriqueRequetes` pour le helper d'appelant).

---

## Risques résiduels

1. **Un utilisateur relevant de plusieurs sociétés n'est pas modélisé.** `ObtenirParUtilisateurAsync`
   renvoie un rattachement unique. Conforme au modèle actuel, mais la résolution devrait être revue
   si le multi-rattachement devenait possible.
2. **Le durcissement ENT-03 change un comportement observable** : un collaborateur restreint ne voit
   plus les simulations de ses collègues. Conforme à RG-CPT-07, mais visible pour les utilisateurs.
3. **`CalculerSimulationProformaQuery` reste sans contrôle d'accès** — c'est un calcul pur, sans
   lecture de ressource ni persistance ; l'endpoint `/api/v1/simulations/calculer` porte
   `RequireAuthorization()`. Aucun cloisonnement n'y est applicable.
4. **Aucune vérification en base réelle** — tests sur `InMemory` et LocalDB.
