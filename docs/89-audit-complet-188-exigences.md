# Audit complet — les 188 exigences confrontées au code livré

> **Document** : `docs/89-audit-complet-188-exigences.md`
> **Établi le** : 2026-09-04
> **Source** : `docs/CDC_Plateforme_Digitale_CIT_v3.docx`, 188 exigences extraites des 54 tableaux
> **Méthode** : vérification de **substance**, pas de traçabilité
> **Remplace** : la passe par échantillon de `docs/88`

---

## Pourquoi cet audit existe

`docs/88` avait examiné une trentaine d'exigences et en avait trouvé huit sur-certifiées. Il
concluait lui-même que **166 était un plafond constaté, pas un acquis démontré**.

Deux événements ont confirmé cette réserve :

1. L'utilisateur a constaté que **le changement de langue ne produit rien**. `NFR-07` était
   `CERTIFIED`. Mon audit l'avait vue citée dans le code et ne l'avait pas essayée.
2. Une exception `DbContext` en production locale a révélé que `HeaderBar` n'avait pas reçu
   le correctif de concurrence appliqué aux autres écrans.

Les deux ont la même cause : **on avait vérifié que le code parlait de l'exigence, pas qu'il la
tenait.** Cet audit refait le travail dans l'autre sens.

---

## La découverte centrale

**29 des 150 commandes et requêtes applicatives ne sont appelées par aucun écran.**

Cinq sont des traitements de fond, légitimement sans interface. Les vingt-quatre autres portent
des fonctions que le CDC exige, elles sont **implémentées, testées, tracées — et hors d'atteinte
de tout utilisateur.**

Le « terminé » du projet s'est arrêté à la couche Application.

### Le cas qui résume tout

`ErrCert01WebBrouillonsTests` est un test du projet **Web**. Il résout
`EnregistrerBrouillonCommand` depuis le conteneur d'injection et vérifie qu'elle fonctionne.

Il ne rend aucun écran. Aucun écran n'appelle cette commande. Le test passe, la traçabilité est
parfaite, et **aucun client ne peut enregistrer un brouillon** — alors que `ERR-01` (Must) exige
« aucune perte de données saisies en cas d'incident ».

### Fonctions inatteignables, et l'exigence qu'elles portent

| Service applicatif | Exigence | Prio. | Conséquence |
|:---|:---|:---|:---|
| `AffecterManuellementCommand` | `SUP-04`, `PEC-05` | Must / Should | L'écran de supervision n'injecte que des requêtes de **lecture** : le superviseur ne peut pas affecter un ticket non pris en charge |
| `ActionDeMasseCommand` | `MAS-01`, `MAS-02` | Should / Must | Aucune redistribution en masse |
| `ModifierPrioriteCommand` | `PRI-03` | Must | Aucun écran ne modifie une priorité |
| `LierTicketsCommand` | `SOC-10` | Must | Aucune liaison entre tickets |
| `EnregistrerBrouillonCommand` + 2 | `ERR-01` | Must | Aucun brouillon |
| `DemanderFactureCommand` | `IMP-10` | Must | **Le client ne peut pas demander sa facture** — `EditerFactureCommand`, côté agent, est bien câblée |
| `TelechargerDocumentQuery` | `SOC-02` | Must | Les documents se déposent et se listent, **ils ne se téléchargent pas** |
| `ExporterTicketsCsvQuery`, `ExporterIndicateursQuery` | `SOC-14` | Could | Aucun export |
| `GererMatriceTransitionsCommand` | § 17.3 | Must | Matrice de transitions non administrable |
| `GererPerimetresSupervisionCommand` | `SUP-05` | — | Périmètres non administrables |
| `GererCompetencesOperateurCommand` | `AFF-06` | Should | Compétences non administrables |
| `RappelerExpirationBadgesCommand` | `ENR-05` | Could | **Appelée par rien du tout** — ni écran, ni traitement de fond |
| `ValiderCoherenceSimulationQuery` | `SIM-11` | Could | Aucun contrôle de cohérence à la saisie |
| `DeposerPreuvePaiementExportCommand` | `EXP-03` | Must | Côté client absent ; `ValiderPreuvePaiementExportCommand`, côté agent, est câblée |

**Une asymétrie revient** : sur la facture comme sur la preuve de paiement, la commande **agent**
est branchée et la commande **client** ne l'est pas. Le parcours client s'arrête après la
soumission.

### Deux faux positifs, écartés

`ActionsWorkflowStandardCommand` et `BasculerAlternativePresentielleCommand` n'ont pas d'appelant
non plus, mais les écrans font le travail par d'**autres** commandes —
`ValiderTicketImportCommand`, `RejeterTicketImportCommand`,
`DeclarerFinalisationPresentielleCommand`. `SOC-06` et `IMP-15` sont donc satisfaites : ces deux
services sont du code mort, pas une fonction manquante.

---

## Le défaut le plus grave : quatre écrans prototypes en production

Quatre routes portent un motif que le reste de l'application ne présente nulle part.

| Écran | Route | Défauts cumulés |
|:---|:---|:---|
| `Tickets/Documents.razor` | `/tickets/{id}/documents` | Ni `[Authorize]`, ni contrôle en page ; `IDocumentRepository` injecté **directement** ; identité saisie |
| `Tickets/FilMessages.razor` | `/tickets/{id}/messages` | Idem, plus la publication de **notes internes** |
| `Tickets/MaFile.razor` | `/tickets/ma-file` | Identité de l'agent saisie |
| `Tickets/Soumettre.razor` | `/tickets/soumettre` | Identité du client saisie |

### Ce que cela permet concrètement

```csharp
private string deposantId = string.Empty;     // saisi par l'utilisateur
...
await DeposerDocument.DeposerPourAgentAsync(Guid.Parse(deposantId), TicketId, ...);
```

La commande contrôle bien l'habilitation — `ExigerAsync(agentId, Permission.DocumentDeposer, …)` —
**mais sur une identité que l'appelant a choisie lui-même.** Le contrôle porte sur une valeur de
formulaire.

Et le chargement de la page ne contrôle rien du tout :

```csharp
protected override async Task OnInitializedAsync()
    => documents = await DepotDocuments.ListerParTicketAsync(TicketId);
```

Connaître un identifiant de ticket suffit pour lister ses documents. Sur `FilMessages`, le même
chemin expose les **notes internes**, que `MSG-02` interdit de montrer au client.

### Pourquoi le reste de l'application n'a pas ce problème

Les 33 autres pages sans `[Authorize]` sont couvertes : `FilePiloteManifeste`,
`FileBackOfficeExport` ou `GestionGrillesTarifaires` vérifient `EstAuthentifie` **et** les
permissions en page, et rendent `PermissionDenied` le cas échéant. C'est le Principe VII assumé du
projet — le contrôle vit dans le cas d'usage, pas sur la route.

**Ces quatre écrans sont les seuls à ne rien vérifier**, et les seuls à injecter un repository en
contournant la couche applicative. Aucun lien de l'interface n'y mène : ce sont des écrans de
démonstration du début du projet, restés routables. `Documents.razor` porte d'ailleurs encore un
objet nommé `ReglesDemonstration`.

### Exigences touchées

`SEC-01` (authentification obligatoire), `SEC-02` (moindre privilège), `SEC-03` (cloisonnement
strict), `SEC-12` (aucun accès direct à un document sans contrôle d'identité), `MSG-02` (note
interne jamais visible du client).

---

## Score corrigé

| | `docs/33` | Après `docs/88` | Après cet audit | Après corrections |
|:---|---:|---:|---:|---:|
| `CERTIFIED` | 174 | 172 | 159 | **172 / 188** (91,49 %) |

Treize exigences supplémentaires sont déclassées : `SUP-04`, `PEC-05`, `MAS-01`, `MAS-02`,
`PRI-03`, `SOC-10`, `SOC-02`, `ERR-01`, `IMP-10`, `SOC-14`, `ENR-05`, `SEC-03`, `SEC-12`.

**Aucune n'est un défaut de logique métier.** Toutes sont des fonctions écrites, testées, et non
reliées à une interface — ou reliées par un écran qui ne contrôle rien.

---

## Corrections appliquées le jour même

**Le point de sécurité est traité.** Les quatre écrans prototypes : `FilMessages`, `MaFile` et
`Soumettre` **retirés** — aucun lien n'y menait et les écrans par module font le travail ;
`Documents` **sécurisé**, car seul à porter le dépôt de document. Il lit désormais par
`ObtenirFicheTicketQuery`, qui applique le cloisonnement, et prend l'identité dans le contexte
authentifié au lieu d'un champ de saisie.

**Un test structurel ferme la porte** — `SurfaceRoutableTests`. Toute page routable qui
injecterait un repository, ou qui n'aurait ni `[Authorize]` ni inscription explicite parmi les
pages publiques, échoue désormais au build.

Il ne vérifie pas la correction d'un écran : il empêche le retour de la famille. Et il a
immédiatement prouvé son utilité en trouvant **deux fuites que mon audit manuel avait manquées** :

| Écran | Fuite | Correction |
|:---|:---|:---|
| `PrendreRendezVous` | Lisait un ticket par `ITicketRepository` : un client authentifié pouvait afficher le numéro, le module et le BL de **n'importe quel ticket** en devinant son identifiant | Passe par `ObtenirFicheTicketQuery` |
| `AgendaGuichet` | Lisait l'agenda du jour sans contrôle d'habilitation | Nouvelle requête `ListerAgendaGuichetQuery`, habilitation exigée |

**21 pages reçoivent `[Authorize]`.** Elles vérifiaient déjà l'habilitation en page ; la garde de
route ajoute une redirection vers la connexion plutôt qu'une page qui se rend puis refuse.

`SEC-03` et `SEC-12` repassent à `CERTIFIED`. **Score à ce stade de la journée : 161 / 188
(85,6 %).**

Les onze autres déclassements — fonctions sans interface — ont été traités dans la nuit ; voir la
section suivante.

---

## Suite donnée — les quinze fonctions sans interface sont câblées

**Les recommandations 2 à 5 sont exécutées.** La dette recensée par cet audit — quinze fonctions
écrites, testées et hors d'atteinte — est résorbée. Le détail par exigence figure dans
`docs/33`, section « Correction du 2026-09-04 (nuit) ». En résumé :

| Bloc | Exigences | État |
|:---|:---|:---|
| Superviseur | `SUP-04`, `PEC-05`, `MAS-01`, `MAS-02`, `PRI-03` | Câblées sur le tableau de bord de supervision |
| Documents et liaisons | `SOC-02`, `SOC-10` | Téléchargement par document ; panneau « Tickets liés » |
| Exports | `SOC-14` | Superviseur (par file) et client (ses demandes) |
| Enrôlement | `ENR-05`, renouvellement | Traitement de fond ordonnancé ; bouton sur un enrôlement clôturé |
| Messagerie et simulation | `MSG-04`, `SIM-11` | Étaient `CERTIFIED` **sans écran** ; le sont désormais avec |
| Administration | § 17.3, `SUP-05`, `AFF-06` | Trois écrans : matrice de transitions, périmètres, compétences |

**Score : 172 / 188 (91,49 %).** Deux exigences restent `PARTIAL` : `COM-01` (aucun fournisseur de
messagerie mobile) et `NFR-07` (880 chaînes françaises en dur).

### Deux défauts que seul le câblage a révélés

Câbler n'a pas été un travail d'assemblage : deux services ont dû être corrigés avant de pouvoir
être exposés.

**`ExporterIndicateursQuery` n'exigeait aucune habilitation et ne vérifiait aucun périmètre.**
Elle prenait un identifiant de file et rendait ses tickets. Aucun test ne la couvrait — elle
n'apparaissait dans aucun fichier de test du dépôt. Tant qu'aucun écran ne l'appelait, le défaut
restait théorique ; la brancher telle quelle aurait ouvert l'export de n'importe quelle file à
quiconque en devine l'identifiant, contre `SEC-03` et `CLAR-05`.

**Deux commandes d'administration ne savaient qu'ajouter.** `GererPerimetresSupervisionCommand` et
`GererMatriceTransitionsCommand` créaient une nouvelle ligne à chaque appel, alors que les deux
dépôts lisent par `SingleOrDefaultAsync`. Corriger un périmètre aurait rendu la supervision
inaccessible à ce superviseur, définitivement. Corriger une transition aurait bloqué tout
transfert entre les deux files concernées.

Ces deux défauts partagent la cause de l'audit lui-même : **une fonction que personne n'appelle
n'est pas seulement inutile, elle est aussi invérifiée.** Le premier écran qui l'appelle est aussi
le premier essai qu'elle subit.

### Un troisième, dans les tests

Le scénario de recette `REC-SCE-08` a échoué après ces changements. Il ne créait pas la file
« Agent Gate » du module Enrôlement : il dépendait d'une **fuite entre classes de test** — deux
autres classes créent cette file dans la même base LocalDB partagée, et la purge par classe
s'exécute pendant que d'autres classes tournent. Le scénario passait ou échouait selon
l'ordonnancement. Il crée désormais sa propre donnée, comme les six autres scénarios E2E.

---

## Ce que je recommande, dans cet ordre

1. **Traiter les quatre écrans prototypes.** Les retirer si rien n'y mène, ou les câbler sur
   `IContexteUtilisateurWeb` comme les autres. C'est le seul point de cet audit qui relève de la
   sécurité, et il précède tout le reste.
2. **Brancher les fonctions client manquantes** — demander la facture, déposer une preuve de
   paiement, enregistrer un brouillon. Le parcours client s'arrête aujourd'hui à la soumission.
3. **Brancher les fonctions superviseur** — affectation, action de masse, priorité.
4. **Les trois écrans d'administration restants** — matrice de transitions, périmètres,
   compétences.
5. **Décider du sort de `ENR-05`** : la commande existe et n'est appelée par rien. Soit on
   l'ordonnance, soit on la retire.

---

## Ce que cet audit ne couvre toujours pas

**La base est vide** — 0 ticket, 0 dossier, 0 alerte. Tout ce qui ne se juge qu'avec des données
reste hors de portée : densité des listes, débordement de colonnes, comportement des 55 tableaux
sur mobile, hiérarchie visuelle quand plusieurs alertes se disputent l'attention.

**Les 6 exigences d'infrastructure** — `NFR-03`, `NFR-05`, `NFR-06`, `NFR-09`, `NFR-10`, `SEC-09` —
ne se vérifient pas dans un dépôt.

**Les 9 exigences bloquées** — `COM-01`, `ENR-04`, `IA-01` à `IA-07` — attendent un fournisseur.

Contrairement aux deux passes précédentes, **je ne pose pas de réserve sur le chiffre lui-même** :
les 188 ont été confrontées au code, et la recherche des services orphelins est exhaustive, non
échantillonnée. La réserve porte sur ce que le code seul ne montre pas — l'exécution avec des
données réelles, et la recette.
