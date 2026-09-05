# Remédiation P0 — Cloisonnement des données

> **Document** : `docs/remediation-P0-securite.md`
> **Établi le** : 2026-09-05
> **Origine** : `docs/audit-conformite-merise.md` § 26, P0-01
> **Règles concernées** : RG-CPT-04, RG-SEC-01, RG-SEC-02 · CDC : CPT-04, SEC-01, SEC-03, ENT-03
> **Périmètre** : P0 uniquement. Aucune P1, P2 ou P3 n'a été traitée.

---

## Vulnérabilité initiale

Les trois points d'entrée de l'API REST (`/api/v1`) réimplémentaient chacun leur contrôle de
cloisonnement au lieu d'appeler le mécanisme central de l'architecture. Le contrôle écrit sur les
deux endpoints de lecture était le suivant :

```csharp
if (ctx.Nature == NatureUtilisateur.Client && ctx.SocieteId.HasValue && dossier is not null &&
    ctx.SocieteId.Value != dossier.ClientId)
{
    return Results.Forbid();
}
```

**Cette condition échoue en mode ouvert.** Elle ne refuse l'accès que si l'appelant possède une
société de rattachement. Quand `ctx.SocieteId` est nul — information d'autorisation *absente* —
aucune vérification n'a lieu et la requête aboutit. L'absence d'information valait autorisation.

---

## Scénario d'exploitation

1. Un attaquant emprunte le parcours d'inscription **public** transporteur
   (`Comptes/InscriptionTransporteur.razor` → `InscrireTransporteurCommand`). Cette commande
   crée un compte de nature `Client` et **refuse explicitement** tout compte déjà rattaché à une
   société : le compte produit a donc nécessairement `SocieteId == null`.
2. Il s'authentifie normalement. `ObtenirContexteUtilisateurQuery` pose
   `SocieteId: societe?.Id` → `null`, faute de rattachement.
3. Il appelle `GET /api/v1/tickets/{numero}`. Les numéros sont produits par une séquence SQL au
   format `XXX-XXX-000001` (`ServiceNumerotation.ProchainNumeroTicketAsync`) : l'énumération est
   immédiate, aucun secret n'est à deviner.
4. La condition ci-dessus n'étant pas remplie, l'endpoint retourne **200 OK** avec le module, le
   type de demande, le statut, la date de création et le **numéro de connaissement** du ticket
   d'un client tiers.
5. `GET /api/v1/dossiers/{guid}` expose de la même manière la liste complète des tickets d'un
   dossier.

Aucun privilège particulier, aucune manipulation de jeton : un compte légitimement créé par le
parcours public suffisait.

---

## Root cause

**La décision d'autorisation était prise sur une information partielle, et son absence était
interprétée comme un feu vert.** Trois facteurs se conjuguaient.

**1. Un contrôle réécrit localement.** `CloisonnementService` existe depuis le socle, est
fail-closed par construction et est appelé par treize commandes. Les endpoints ne l'utilisaient
pas : ils portaient leur propre règle, plus faible, que rien ne maintenait en cohérence.

**2. Une propriété de dossier à deux formes, dont une seule était traitée.**
`Dossier.ClientId` est documenté comme « la société propriétaire » et l'est effectivement pour les
parcours Billing, Réclamation et Avoir. Mais `SoumettreEnrolementCommand` y écrit
l'identifiant de **l'utilisateur** (« Dossier propriétaire = appelant lui-même »). Un contrôle qui
ne compare qu'à une société laisse toute la seconde forme hors de portée du contrôle.

**3. Aucun test ne couvrait le cloisonnement de l'API.** Le répertoire `Web.Tests/Api` ne
contenait qu'un test de format (`NfrCert08ApiTests`). Rien n'obligeait ces endpoints à refuser
quoi que ce soit.

---

## Endpoints concernés

| Point d'entrée | Défaut | État |
|:---|:---|:---|
| `GET /api/v1/tickets/{numero}` | Contrôle local fail-open | Corrigé |
| `GET /api/v1/dossiers/{reference}` | Contrôle local fail-open | Corrigé |
| `POST /api/v1/simulations/calculer` | Aucune ressource tierce lue — calcul sur données transmises | Sans objet |

---

## Correction apportée

### Un point de décision unique

`src/CIT.Plateforme.Application/Securite/ControleAccesRessourceService.cs` (nouveau) porte
désormais la décision d'accès à toute ressource rattachée à un dossier. Il expose
`PeutConsulterLeTicketAsync`, `PeutConsulterLeDossierAsync` et leurs variantes `Exiger…` qui
lèvent `AccesRefuseException`.

### Trois principes

**Refus par défaut.** Chaque chemin part de « refusé » et ne concède l'accès que sur preuve
positive. Identité vide, ressource introuvable, compte inconnu, rattachement inactif, agent sans
habilitation : tous ces cas retournent `false`. C'est l'inversion exacte du défaut d'origine.

**Données faisant autorité.** La décision ne prend en entrée que l'identifiant de l'utilisateur et
la ressource ; tout le reste — nature du compte, rattachement, droit de consultation,
habilitations, périmètre de supervision — est relu depuis la base. Le contexte de session n'est
qu'une projection : il ne porte plus aucune décision de sécurité. Un contexte bien formé ne vaut
pas autorisation.

**Les deux formes de propriété sont traitées, explicitement.** Un client accède à un dossier si et
seulement si :

- *dossier de société* — il a un rattachement **actif** à la société propriétaire, la portée
  intra-société étant celle d'ENT-03, déléguée à la règle déjà établie
  `VisibiliteDemandesFiltre` (un collaborateur restreint ne lit pas la demande d'un collègue ;
  un administrateur de société voit tout, CLAR-02) ; **ou**
- *dossier personnel* — le dossier est nominativement à son nom (`ClientId`) ou il en est
  l'auteur (`AuteurId`). C'est le cas de l'Enrôlement.

Un agent accède à un ticket s'il porte une habilitation couvrant la file courante — non bornée ou
bornée à cette file précise — ou si son périmètre de supervision la couvre. **La nature « Agent »
n'est pas un droit** : un agent sans aucune habilitation n'accède à rien.

### Facteurs pris en compte par la décision

Identité · authentification · nature d'acteur · société de rattachement et son activité · droit de
consultation ENT-03 · qualité d'administrateur de société · propriétaire du dossier (société ou
personne) · auteur du dossier · file courante du ticket · habilitations et leur portée · périmètre
de supervision.

---

## Architecture de contrôle d'accès retenue

`CloisonnementService` **n'a pas été modifié**. Il reste la primitive étroite de propriété client
qu'appellent treize commandes métier, et son contrat est inchangé — modifier son constructeur
aurait touché quarante sites de construction dans les tests sans bénéfice de sécurité.

Le nouveau service se place au-dessus : il répond à la question complète « cet utilisateur peut-il
voir cette ressource ? », là où `CloisonnementService` répond à « ce client est-il propriétaire de
ce ticket ? ». Les deux réutilisent la même notion de propriété ; aucune règle n'est dupliquée.

---

## Tests ajoutés

`tests/CIT.Plateforme.Web.Tests/Securite/ApiCloisonnementTests.cs` — 12 tests d'intégration qui
démarrent l'application réelle, substituent l'identité appelante et interrogent les endpoints
publiés. Le jeu de données comporte deux sociétés clientes, trois collaborateurs (dont un restreint
par ENT-03), deux transporteurs sans société, un compte orphelin et deux agents habilités sur des
files distinctes — **tous persistés en base**, puisque la décision s'y réfère.

| # | Scénario | Attendu |
|:---|:---|:---|
| 01 | Client A → ticket de sa société | 200 |
| 02 | Client A → ticket du client B | 403 |
| 03 | Utilisateur société A → dossier de sa société | 200 |
| 04 | Utilisateur société A → dossier de la société B | 403 |
| 05 | Transporteur A → son propre ticket d'enrôlement | 200 |
| **06** | **Transporteur A → ticket d'un client (le P0)** | **403** |
| 07 | Transporteur A → ticket d'un autre transporteur | 403 |
| 08 | Compte sans rattachement → ticket et dossier arbitraires | 403 |
| 09 | Utilisateur non authentifié | 401/403/302 |
| 10 | Agent habilité sur la file du ticket | 200 |
| 11 | Agent habilité sur une autre file | 403 |
| 12 | Collaborateur restreint ENT-03 → demande d'un collègue | 403 |

---

## Résultats avant correction

Exécution des 12 tests **sur le code d'origine**, avant toute modification applicative :

```
Échoué!  - échec : 5, réussite : 7, total : 12
  Cas06_P0_Transporteur_ne_peut_pas_lire_le_ticket_dun_client              [FAIL]
  Cas07_Transporteur_ne_peut_pas_lire_le_ticket_dun_autre_transporteur     [FAIL]
  Cas08_Compte_sans_rattachement_ne_peut_lire_aucun_ticket_tiers           [FAIL]
  Cas11_Agent_non_habilite_sur_la_file_du_ticket_est_refuse                [FAIL]
  Cas12_Collaborateur_restreint_ne_lit_pas_la_demande_dun_collegue         [FAIL]
```

La vulnérabilité est donc reproduite, et l'audit confirmé : cinq accès qui auraient dû être refusés
étaient accordés. Les sept accès légitimes fonctionnaient déjà.

---

## Résultats après correction

```
Réussi!  - échec : 0, réussite : 12, total : 12
```

---

## Recherche IDOR / BOLA

Balayage de toutes les surfaces recevant un identifiant fourni par l'utilisateur : douze pages
routables à paramètre, trois endpoints API, et toutes les commandes et requêtes applicatives
prenant un `Guid ticketId` ou `dossierId`.

**Deux variantes du même défaut ont été trouvées et corrigées :**

| Variante | Défaut | Correction |
|:---|:---|:---|
| `ConsulterDossierQuery` / `Dossiers/DetailDossier.razor` (`/dossiers/{guid}`) | La requête ne recevait **aucune identité d'appelant** : elle n'avait aucun moyen de refuser. Tout utilisateur authentifié lisait le numéro, l'état et la liste des tickets de n'importe quel dossier | Identité exigée, décision déléguée au contrôle central |
| `Tickets/IndicateursTemps.razor` (`/tickets/{guid}/temps`) | Lisait le ticket par `db.Tickets.FindAsync(TicketId)` **sans aucun contrôle**, en résolvant le `DbContext` via `IServiceScopeFactory` — ce qui contournait au passage la garde d'architecture `SurfaceRoutableTests` | Contrôle central appliqué avant tout affichage |

**Surfaces vérifiées et déjà correctement gardées** (aucune modification) :

| Surface | Garde constatée |
|:---|:---|
| `ObtenirFicheTicketQuery` (`/tickets/{id}`) | Refus si rattachement absent/inactif, + portée ENT-03 |
| `ObtenirFicheDossierQuery` (`/dossiers/{id}` fiche) | Idem |
| `ObtenirFicheEnrolementQuery` | Compare `dossier.ClientId` à l'appelant — traite la forme personnelle |
| `DemandeAvoirExoQueries` | Contrôle anti-IDOR SEC-AVE-01 explicite |
| `TelechargerDocumentQuery` | `ExigerProprieteClientAsync` à **chaque** accès + garde antivirus |
| `DeposerDocumentCommand` et ses relais (`DeposerPreuvePaiement*`) | `ExigerProprieteClientAsync` transitif |
| `PublierMessageCommand`, `AnnulerTicketCommand`, `ReouvrirTicketCommand`, `DemanderFactureCommand`, `GestionComplement*` | `ExigerProprieteClientAsync` |
| `ConsulterSuivisBranchesIes*`, `ConstaterJointureIes*` | Permission exigée |
| `POST /api/v1/simulations/calculer` | Ne lit aucune ressource tierce |

Scénarios BOLA contrôlés : lecture de ticket, de dossier, de document, téléchargement, dépôt de
message, annulation, réouverture — par identifiant technique **et** par numéro séquentiel.

---

## Régressions vérifiées

```
Domain ............ 187 / 187
Application ....... 619 / 619
Infrastructure .... 139 / 139
Web ............... 287 / 287
TOTAL ............. 1 232 / 1 232
```

Aucun test existant n'a été cassé. **Deux tests existants ont dû être adaptés**, non parce qu'ils
attendaient un comportement contraire au document, mais parce que `ConsulterDossierQuery` exige
désormais l'identité de l'appelant :

- `RecSce14TransverseTests` — le propriétaire du dossier devient un compte réel, et c'est lui qui
  consulte. L'objet du test (regroupement multi-tickets, SOC-11) est inchangé.
- `RecSce14NotificationsE2ETests` — la consultation est faite par `clientUser`, administrateur de
  la société propriétaire du dossier, donc légitimement habilité (CLAR-02).

Aucun assouplissement de contrôle n'a été fait pour faire passer un test.

**Journal d'audit** : aucune modification. Les chemins d'audit existants ne sont pas touchés — un
refus survient avant la lecture, et ne retire aucune consignation préexistante.

---

## Risques résiduels

**R-01 — La règle de cloisonnement reste écrite à trois endroits.** `ObtenirFicheTicketQuery` et
`ObtenirFicheDossierQuery` portent chacun leur propre implémentation, correcte et fail-closed, mais
distincte du service central. C'est la duplication même qui a produit ce P0. **Consolidation
recommandée en P2** — non faite ici, hors périmètre.

**R-02 — Un transporteur ne peut pas ouvrir son propre ticket d'enrôlement via `/tickets/{id}`.**
`ObtenirFicheTicketQuery` ne reconnaît pas la forme personnelle de propriété et refuse. Le
comportement est **fail-closed** — donc sans risque de sécurité — mais c'est une limitation
fonctionnelle. L'écran dédié `Enrolement/FicheEnrolement.razor` fonctionne, lui, correctement.
Constat antérieur à cette remédiation, non introduit par elle.

**R-03 — Énumérabilité des numéros de ticket.** Le format `XXX-XXX-000001` reste prédictible.
Ce n'est **pas** la cause du P0 et sa correction n'aurait rien réglé : le contrôle d'accès serveur
est la seule protection qui compte. À traiter éventuellement en défense en profondeur (P3).

**R-04 — Deux sémantiques pour `Dossier.ClientId`.** Le champ porte tantôt une société, tantôt un
utilisateur. Le contrôle central les traite désormais toutes deux explicitement, mais l'ambiguïté
du modèle demeure et reste un piège pour tout code futur. Clarification recommandée en P2.

---

## Fichiers modifiés

**Créés**
- `src/CIT.Plateforme.Application/Securite/ControleAccesRessourceService.cs`
- `tests/CIT.Plateforme.Web.Tests/Securite/ApiCloisonnementTests.cs`

**Modifiés**
- `src/CIT.Plateforme.Web/Api/ApiEndpoints.cs` — deux contrôles locaux remplacés par le service central
- `src/CIT.Plateforme.Application/Tickets/ConsulterDossierQuery.cs` — identité d'appelant exigée
- `src/CIT.Plateforme.Web/Components/Pages/Dossiers/DetailDossier.razor` — identité + rendu du refus
- `src/CIT.Plateforme.Web/Components/Pages/Tickets/IndicateursTemps.razor` — contrôle avant affichage
- `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs` — une ligne d'enregistrement
- `tests/CIT.Plateforme.Web.Tests/RecetteCdc/RecSce14TransverseTests.cs` — identité légitime
- `tests/CIT.Plateforme.Web.Tests/E2E/RecSce14NotificationsE2ETests.cs` — identité légitime

Aucun fichier du lot `[lot 3FN]` (normalisation tarifaire) n'a été touché.

---

## Conclusion

Le P0 est fermé. La vulnérabilité a été reproduite par un test automatique avant correction — cinq
scénarios d'accès illégitime aboutissaient — puis refermée par un point de décision unique,
fail-closed, fondé exclusivement sur des données faisant autorité.

Deux variantes du même défaut, trouvées lors du balayage IDOR/BOLA, ont été corrigées avec le même
mécanisme : c'est la cause générale qui est traitée, non le seul symptôme du transporteur.

Le risque résiduel principal n'est pas technique mais structurel : la règle de cloisonnement reste
écrite à trois endroits. Tant qu'elle l'est, un quatrième point d'entrée pourra la réécrire
imparfaitement — exactement ce qui s'est produit ici. La consolidation en P2 est la mesure qui
empêchera la récidive.
