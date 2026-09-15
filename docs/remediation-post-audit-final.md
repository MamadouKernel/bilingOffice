# Remédiation post-audit final

*Périmètre strict : les quatre écarts techniques ouverts par l'audit final.*
*Aucun arbitrage métier n'a été pris. R-15 n'a pas été exécuté.*

| | |
|---|---|
| **Audit de référence** | `docs/audit-final-conformite-98RG.md` |
| **Commit audité** | `1c3cd38260c3b196de8641b8ec1d5a89f27a9762` |
| Conformité constatée | 96,9 % — P0 : 0 · P1 : 0 · P2 : 1 · P3 : 3 |
| Règles traitées | RG-SEC-01 · RG-EXP-03 · RG-SLA-01 · RG-TCK-06 |
| Règles **non** traitées | RG-CPT-08 · PC-16 / RG-COM-01 · RG-IA-01 · R-15 sur production |

---

## RG-SEC-01

### L'écart, cité de l'audit

> « `AutorisationChampService` est enregistré dans le conteneur, couvert par deux suites de tests,
> et **n'a aucun appelant en production**. […] les deux suites qui le couvrent l'instancient
> **directement** (`new AutorisationChampService()`), sans passer par le conteneur. »

### Cause racine

Deux causes distinctes, et il fallait traiter les deux.

**La première est un câblage manquant.** Aucun écran, aucun cas d'usage n'appelait la matrice. Elle
n'a jamais eu d'effet depuis son écriture.

**La seconde est plus gênante : le service n'était pas appelable tel quel.** Sa signature était
`EvaluerDroit(BlocDonneesIes, string? roleUtilisateur)`. Un nom de rôle est une donnée d'entrée — et
la règle 1 de `docs/architecture-autorisation.md` interdit qu'une donnée d'entrée fonde une décision
d'autorisation. Le câbler naïvement depuis une page aurait fait passer par l'IHM la valeur à
laquelle le contrôle se compare : exactement le défaut de la consolidation sécurité.

### Fichiers concernés

| Fichier | Nature |
|---|---|
| `Domain/Securite/RoleIes.cs` | **créé** — `RoleIes` et `ModuleIes`, énumérations fermées |
| `Application/Securite/IAutorisationChampService.cs` | surcharge sur `RoleIes` |
| `Application/Securite/AutorisationChampService.cs` | matrice portée sur l'énumération ; la surcharge par chaîne délègue |
| `Application/Securite/PorteeChampsIesService.cs` | **créé** — le pont |
| `Application/BillingImport/ConsulterSuivisBranchesIesQuery.cs` | rend `VueBranchesIes` (suivis **+** droits) |
| `Application/BillingExport/ConsulterSuivisBranchesIesExportQuery.cs` | symétrique |
| `Application/BillingImport/InstruireBrancheIesCommand.cs` | exige le droit d'écriture sur le bloc |
| `Application/BillingExport/InstruireBrancheIesExportCommand.cs` | symétrique |
| `Web/.../BillingImport/FileIes.razor` · `.../BillingExport/FileIesExport.razor` | n'offrent que les actions du rôle |
| `Infrastructure/DependencyInjection.cs` | enregistrement de `PorteeChampsIesService` |

### La correction

**Aucune deuxième logique n'a été écrite.** La matrice du CDC est restée mot pour mot ce qu'elle
était ; seule son *entrée* a changé. `PorteeChampsIesService` est le pont, et il applique l'ordre
imposé par l'architecture :

```
1. garde d'identité      → un compte inconnu ou désactivé n'obtient AUCUN droit
2. rôles détenus         → déduits des seules habilitations, jamais transmis
3. décision              → la matrice, et rien d'autre
```

Un appelant détenant plusieurs rôles obtient, bloc par bloc, le droit le plus permissif que la
matrice lui reconnaît. C'est une composition de la matrice, pas une règle nouvelle : sans elle,
cumuler deux habilitations aurait **dégradé** les droits.

Les deux couches coexistent dans les commandes : la permission répond « cet agent peut instruire
cette branche », la matrice répond « ce rôle peut écrire ce bloc ». La seconde n'a pas remplacé la
première.

### Ce que cela change pour l'utilisateur

Avant, détenir **une** des deux permissions de branche IES donnait accès à l'écran entier, actions
des deux branches comprises. Le Pilote Manifeste voyait — et pouvait déclencher — l'action Contrôle
Documentaire. La commande l'aurait refusée, mais l'écran l'offrait, et IMP-40 exige que chacun
n'agisse « que sur les champs/actions de son rôle ».

Désormais l'écran n'offre que ce que le rôle autorise, et la commande le revérifie.

**Statut : CONFORME.**

---

## Extension de la garde d'atteignabilité

L'audit avait relevé pourquoi le défaut avait traversé : le corpus d'exigence de
`ServicesAtteignablesTests` s'arrête aux `Command`, `Query` et `UseCase`.

**Aucune exception n'a été écrite pour `AutorisationChampService`.** Le critère retenu est générique
et vérifiable : **être enregistré dans le conteneur**. Un service qu'on déclare fournir engage à le
rendre atteignable.

Trois corrections ont été nécessaires pour que la garde ne produise pas de faux positifs :

| Correction | Pourquoi |
|---|---|
| Atteignabilité **par héritage** | Un service injecté par son interface n'était pas vu : l'appelant écrit `IX`, jamais `X`. Trois résolveurs de notifications, consommés via `IEnumerable<IResolveurDestinataires>`, étaient signalés à tort |
| Corpus de **relais** étendu à l'Infrastructure | Un service applicatif atteint à travers un adaptateur d'infrastructure paraissait orphelin |
| Lecture des enregistrements depuis `DependencyInjection.cs` | L'enregistrement est une décision écrite ; c'est elle qui engage |

### Ce que la garde a trouvé

Quatre services **enregistrés dans le conteneur et atteints par personne** — dans l'état exact où se
trouvait `AutorisationChampService` :

| Service | Constat |
|---|---|
| `GestionnaireRetentionService` | rétention/purge (SEC-06) : enregistré, testé, aucun déclencheur — ni écran, ni service hébergé |
| `MoteurRetentionService` | atteint seulement par le précédent, lui-même orphelin |
| `DerogationWorkflowService` | WKF-02/03 : la dérogation est servie en production par `TransfererTicketCommand` et `ReaffecterTicketCommand` |
| `CreerVersionGrilleTarifaireCommandHandler` | SIM-11 : la commande du même fichier est câblée, ce gestionnaire ne l'est pas |

**Ils n'ont pas été traités** : la remédiation est bornée aux quatre règles autorisées. Ils sont
inscrits nommément dans `ServicesEnregistresSansConsommateurAssume`, avec leur raison — ce qui les
rend visibles et engage une décision, au lieu de les laisser passer en silence. La liste ne doit que
rétrécir.

**Le plus notable est le premier.** La chaîne de rétention SEC-06 — celle qui purge et anonymise —
est complète, testée, et **rien ne la déclenche**. C'est un constat de même nature que RG-SEC-01,
et il mérite une décision.

---

## RG-EXP-03

### L'écart, cité de l'audit

> « le **Draft IN** que la règle exige avant l'édition de la facture export **n'existe pas dans le
> code**. Recherche exhaustive : aucune occurrence. »

### Cause racine

Aucun test ne réclamait le Draft IN, et le parcours export aboutissait sans lui. Une exigence que
rien n'éprouve n'est pas surveillée — elle est seulement écrite.

### Position dans le workflow

```
codification (Back Office)
  → Pilote Facturation : DRAFT IN        ← ajouté
  → Pilote Facturation : facture
  → suite du parcours selon le profil client
```

| Élément | Décision |
|---|---|
| **Acteur** | Pilote Facturation — `billing_export.facturation.editer`, la même habilitation que l'édition de la facture, puisque le CDC lui confie les deux gestes |
| **État du ticket** | inchangé, file inchangée. Un brouillon n'est pas une étape de plus : il conditionne la suivante, dans la même file |
| **Document produit** | le Draft IN, déposé sur le ticket comme toute autre pièce |
| **Engagement** | **aucun**. Un brouillon n'est pas un document officiel : le ticket ne devient pas engageant ici, mais à l'édition de la facture (CYC-01/CYC-05) |
| **Transition suivante** | édition de la facture, puis aiguillage par profil — inchangé |
| **Historique / audit** | `DraftInExportEmis` |
| **Notification** | événement publié ; sa diffusion reste administrable (RG-NOT-02) — le socle ne décide d'aucun canal |

### Fichiers concernés

| Fichier | Nature |
|---|---|
| `Domain/Tickets/Ticket.cs` | `DraftInExportEmisLeUtc`, `DraftInExportEmis`, `EmettreDraftInExport()` |
| `Application/BillingExport/EmettreDraftInExportCommand.cs` | **créé**, avec `DraftInRequisException` |
| `Application/BillingExport/EditerFactureExportCommand.cs` | refuse la facture sans Draft IN ; **ordre des refus corrigé** |
| `Web/.../BillingExport/FileFacturationExport.razor` | colonne Draft IN ; la facture n'est proposée qu'ensuite |
| `Infrastructure/Persistence/Migrations/20260906034753_AjoutDraftInExport.cs` | **créée**, non exécutée |

### Un défaut d'ordre corrigé au passage

`EditerFactureExportCommand` déposait le document **puis** rejetait le type de client. Un ticket IES
conservait donc une facture qui n'aurait jamais dû y être jointe. Tous les refus sont désormais
prononcés avant le dépôt.

Cet ordre a aussi une conséquence de justesse : le refus IES précède celui du Draft IN, de sorte que
**la règle du Draft IN ne s'oppose jamais à un parcours IES** (RG-EXP-04, inchangé).

**Statut : CONFORME.**

---

## RG-SLA-01

### L'écart, cité de l'audit

> « le paramétrage SLA porte cinq axes sur les six énumérés. La **catégorie de client** manque. »

### Cause racine

L'axe n'existait ni dans le modèle, ni dans le moteur de sélection. Conformément à la consigne, la
correction porte sur **les deux** : une colonne qui ne participe pas au choix n'est pas un axe.

### Fichiers concernés

| Fichier | Nature |
|---|---|
| `Domain/SuiviTemps/ParametreSlaAdministrable.cs` | propriété `TypeClient`, `Cibler(...)` étendu, `Specificite` rebarémée |
| `Infrastructure/Persistence/ParametresSlaProvider.cs` | **le filtre de résolution** |
| `Application/Administration/GererParametresSlaCommand.cs` | axe exposé à l'administration, tracé en audit |
| `Infrastructure/Persistence/Migrations/20260906035…_AjoutAxeTypeClientSla.cs` | **créée**, non exécutée |

### Deux décisions

**Précédence.** Les poids passent de `file=2, priorité=1` à `file=4, priorité=2, client=1`. L'ordre
relatif existant est préservé, et le nouvel axe s'insère en dernier — dans l'ordre où le CDC les
énumère, par portée décroissante : « étape, file, priorité, catégorie de client ». L'ordre reste
**total** : deux paramétrages distincts ne peuvent jamais être à égalité.

**Quelle valeur lire.** L'axe lit `Dossier.TypeClient`, l'**instantané** figé à la création, jamais
le profil courant de la société. Sans cela, retyper un client aurait déplacé le SLA de ses tickets
en cours — ce que RG-CPT-05 interdit précisément ailleurs. Un test le vérifie.

**Statut : CONFORME.**

---

## RG-TCK-06

### L'écart, cité de l'audit

> « un ticket clôturé reste consultable, mais l'archivage n'est pas matérialisé comme état. »

### Cause racine

Le code **affirmait** l'archivage sans le faire :

> `SOC-09 (archivage automatique — représenté par la simple présence de DateClotureUtc)`

Or `DateArchivageUtc` existait — et n'était alimentée que par `MarquerAnonymise`, c'est-à-dire par la
purge de rétention. Le champ d'archivage servait donc à marquer l'**anonymisation**, l'opération qui
retire la lisibilité, là où l'archivage doit la préserver. Les deux notions étaient confondues dans
le même champ.

### Fichiers concernés

| Fichier | Nature |
|---|---|
| `Domain/Tickets/Ticket.cs` | `Archiver()`, `EstArchive` ; `Cloturer()` archive ; `MarquerAnonymise()` n'écrase plus ; `Rouvrir()` désarchive |
| `Application/Tickets/FicheTicketVue.cs` | `EstArchive`, `DateArchivageUtc` |
| `Application/Tickets/ObtenirFicheTicketQuery.cs` | restitution par le chemin de lecture réel |

Aucune migration : la colonne existait déjà.

### Ce qui distingue désormais les deux notions

| | Clôture | Archivage |
|---|---|---|
| Nature | décision d'un agent habilité | conséquence automatique |
| Porté par | `Statut`, `MotifCloture`, `DateClotureUtc` | `DateArchivageUtc`, `EstArchive` |
| Effet sur les données | aucun | aucun — ni suppression, ni déplacement |
| Réversible | oui (RG-TCK-12) | oui : un ticket rouvert n'est plus archivé |
| À ne pas confondre avec | — | l'anonymisation, qui retire la lisibilité |

L'archivage est **idempotent** : la date ne se réécrit pas. Une anonymisation ultérieure ne déplace
pas la date à laquelle le ticket a été archivé.

**Il n'est pas simulé par un statut visuel** : c'est une propriété du ticket, restituée par la fiche,
et vérifiée à travers la requête de production.

**Statut : CONFORME.**

---

## Tests de reproduction

Conformément à la consigne, chaque règle a d'abord été confrontée à un test avant correction.

**RG-SEC-01 — la reproduction a révélé une impossibilité, et c'est le constat le plus parlant.** Le
contrat de `ConsulterSuivisBranchesIesQuery` rendait `IReadOnlyList<SuiviBrancheIes>` : aucun droit
de champ n'y figurait. L'exigence du CDC n'était **pas exprimable** contre l'API existante. C'est
une forme forte du défaut : ce n'est pas que la règle échouait, c'est qu'on ne pouvait pas la poser.

**RG-EXP-03** — l'édition de facture sans Draft IN réussissait, sur les trois circuits export.

**RG-SLA-01** — deux tickets ne différant que par leur catégorie de client obtenaient le même SLA.

**RG-TCK-06** — `DateArchivageUtc` restait nulle après clôture.

### Suites créées

| Suite | Cas | Portée |
|---|---|---|
| `ControleChampIesTests` | **9** | rôle A, rôle B, refus, compte désactivé, union de rôles |
| `DraftInExportTests` | **7** | nominal, refus, historisation, non-engagement, rôle, import, IES |
| `AxeCategorieClientSlaTests` | **5** | sélection différenciée, ciblage, portée large, précédence, instantané |
| `ArchivageAutomatiqueTests` | **9** | archivage, non-anonymisation, consultation, documents, échanges, droits, réouverture |
| `ServicesAtteignablesTests` | 2 → **3** | garde d'atteignabilité étendue |
| **Total nouveaux** | **30** | |

`ControleChampIesTests` **n'instancie jamais** `AutorisationChampService` : elle passe par la requête
et la commande réellement câblées derrière l'écran. C'était la condition posée, et c'est la seule
façon de démontrer qu'une règle atteint l'utilisateur.

---

## Tests mutation

Chaque correction a été débranchée, les tests observés, puis la correction restaurée et revérifiée.

| # | Mutation | Tests tombés | Restauré |
|---|---|---|---|
| 1 | `PorteeChampsIesService` : câblage vers la matrice retiré, tous droits en écriture | **4** — 2 sur la vue, 2 sur le refus d'action | ✅ 9/9 |
| 2 | `PorteeChampsIesService` : dépendance à `IAutorisationChampService` supprimée | **1** — la garde d'atteignabilité redétecte `AutorisationChampService` | ✅ 3/3 |
| 3 | `EditerFactureExportCommand` : contrôle du Draft IN court-circuité | **1** — `Facture_refusee_si_le_draft_in_n_a_pas_ete_emis` | ✅ 7/7 |
| 4 | `Ticket.Cloturer` : appel à `Archiver` retiré | **5** | ✅ 9/9 |
| 5 | `ParametresSlaProvider` : axe catégorie retiré du filtre | **2** — dont `Changer_la_categorie_de_client_selectionne_un_sla_different` | ✅ 5/5 |

La mutation n° 2 mérite d'être signalée : elle démontre que **la garde étendue redétecte exactement
le défaut d'origine**. Débrancher la seule référence à la matrice fait réapparaître
`AutorisationChampService` dans la liste des orphelins.

Aucune trace de mutation ne subsiste — vérifié par recherche sur l'ensemble des sources.

---

## Régressions

Vérification après chaque règle, puis suite complète en exécution séquentielle par projet.

| Périmètre | État |
|---|---|
| Tests sécurité | ✅ **232/232** — 112 Application + 120 Web |
| P0 / P0-bis / P0-ter | ✅ |
| Consolidation autorisation | ✅ |
| ENT-03 | ✅ |
| Billing Import | ✅ |
| Billing Export | ✅ — 3 parcours mis à jour, voir ci-dessous |
| Simulation | ✅ |
| SLA | ✅ |

### Les ajustements de tests existants

Huit tests ont dû être mis à jour. **Tous par la même cause, et aucun n'a été affaibli** : ils
encodaient le parcours export *sans* Draft IN, c'est-à-dire l'ancien workflow.

| Test | Ajustement |
|---|---|
| `CircuitClientNormalExportTests` | émet le Draft IN avant la facture |
| `CircuitClientEnCompteExportTests` | idem |
| `CircuitClientDepositExportTests` | idem |
| `CablageEtatEngageantTests.EditerFacture_export_rend_le_ticket_engageant` | idem, **et une assertion ajoutée** : le Draft IN ne rend pas le ticket engageant |
| `RecSce05ExportE2ETests` (×3, scénarios de recette Normal / En Compte / Deposit) | idem |
| `Imp11EtProducteursTests.LesProducteursDEvenements_SontAuNombreAttenduEtInchanges` | inventaire porté de 26 à 27 producteurs |

**Aucune assertion n'a été retirée.** Une étape conforme au CDC a été ajoutée au parcours, et le
cas `CablageEtatEngageantTests` en a gagné une : la vérification explicite que le brouillon
n'engage pas.

Le dernier mérite un mot. `Imp11EtProducteursTests` est une garde qui compte les producteurs
d'événements, précisément pour qu'aucun n'apparaisse par inadvertance. Elle a signalé
`EmettreDraftInExportCommand` — c'est son office. Son commentaire prévoit ce cas : l'inventaire est
mis à jour, et le nouveau producteur est **nommé** pour que sa disparition soit à son tour détectée.
Le seuil n'a pas été relâché, il a été porté à la valeur juste.

Le circuit IES export n'a pas bougé : il n'édite pas de facture (RG-EXP-04).

### Suite complète

```
Domain ............ 208/208
Application ....... 762/762
Infrastructure .... 139/139
Web ............... 315/315
──────────────────────────
TOTAL ............ 1424/1424      (1393 + 31 nouveaux)
```

Le compte se referme exactement : 1393 avant remédiation, 31 tests créés, 1424 après. **Aucun test
n'a été perdu ni retiré.**

### Une contention, pas une régression

Une exécution intermédiaire a montré 15 échecs sur le projet Web — dont sept dans
`AuthorizationUxTests` et six dans `CustomerComplaintSecurityTests`. Ils ont été vérifiés avant
toute conclusion : le projet Web **exécuté seul rend 315/315**, et deux exécutions successives
donnaient des ensembles d'échecs différents (15, puis 3, puis 2).

C'est la contention LocalDB déjà consignée comme risque LOW dans l'audit final, ici aggravée par
deux exécutions `dotnet test` simultanées de ma part. **Aucune régression de sécurité.** Le
diagnostic a été posé par mesure, non par supposition.

**Aucun arrêt.**

---

## Une modification concurrente, non absorbée

`src/CIT.Plateforme.Web/Components/Pages/Administration/ComptesAgents.razor` figure parmi les
fichiers modifiés du worktree — **et ne vient pas de cette remédiation.**

| | |
|---|---|
| Horodatage | 2026-09-05 **22:26 UTC** |
| Repère | après le commit audité `1c3cd38` (19:50), avant cette session (03:00 le 2026-09-06) |
| Ampleur | +621 / −88 lignes |
| Nature | refonte d'interface de l'écran d'administration des comptes agents — cartes de statistiques, styles, titre « Comptes agents & Habilitations » |

Elle n'a été ni écrite, ni relue, ni testée dans le cadre de cette remédiation. Elle compile et la
suite passe, mais **son contenu n'a pas été audité** : aucune des quatre règles traitées ne la
concerne, et l'étendre à elle sortirait du périmètre autorisé.

Elle est signalée ici pour la même raison que la clôture de compte l'avait été lors de la revue de
convergence : une modification concurrente qu'on absorbe en silence devient indistinguable du
travail commandé.

---

## Arbitrages toujours ouverts

Non traités, conformément à la consigne. Leur absence de décision n'a été transformée en aucune
implémentation.

| Sujet | État | Nature |
|---|---|---|
| **RG-CPT-08** | **OUVERT** | Lot dédié / décision métier. 13 sites impactés, dont 7 à conséquence de sécurité. Le préalable technique reste de nommer la notion d'usager externe avant d'introduire la nature |
| **PC-16 / RG-COM-01** | **OUVERT** | Décision d'activation des canaux. Rien de technique en attente : les trois canaux sont enregistrés, tous journalisés |
| **RG-IA-01** | **OUVERT** | Décision sur le moteur et sur la capacité. « Aider à compléter ses dossiers » est une capacité nouvelle, pas un adaptateur |

### Constats nouveaux, à arbitrer

L'extension de la garde d'atteignabilité a révélé **quatre services enregistrés sans consommateur**
(voir plus haut). Le plus significatif est la **chaîne de rétention SEC-06**, complète et jamais
déclenchée. Ils ne relèvent d'aucune des quatre règles autorisées et n'ont pas été traités.

---

## R-15

**Non exécuté.** Aucun script n'a été lancé, ni sur production ni ailleurs.

> ### R-15 : **GO SOUS CONDITIONS**

La condition est inchangée : confronter les valeurs réellement présentes en base aux correspondances
d'ordinaux de la section E du script, sur une copie restaurée, avant toute exécution réelle.

**Deux migrations ont été créées et NON exécutées :**

| Migration | Contenu |
|---|---|
| `20260906034753_AjoutDraftInExport` | colonne `DraftInExportEmisLeUtc` sur `Tickets` (nullable) |
| `…_AjoutAxeTypeClientSla` | colonne `TypeClient` sur `ParametresSla` (nullable) |

Toutes deux sont additives et nullables : elles n'altèrent aucune donnée existante et n'interfèrent
pas avec R-15.

---

## Conclusion

Les quatre écarts techniques sont fermés, chacun éprouvé par un test qui traverse le chemin de
production et par une mutation qui le fait tomber.

Deux d'entre eux se ressemblaient plus qu'il n'y paraissait. `AutorisationChampService` et le Draft
IN étaient l'un et l'autre des règles écrites que rien n'exerçait — la première existait en code
sans appelant, la seconde n'existait qu'en toutes lettres dans le CDC. Dans les deux cas, la suite de
tests était verte, et le comportement absent.

C'est pourquoi la correction de RG-SEC-01 ne s'est pas arrêtée au câblage. **Le filet a été élargi**,
et il a immédiatement rapporté quatre autres services dans le même état — dont la chaîne de rétention
SEC-06 tout entière. Un filet a toujours une maille ; celle-ci était large de tout ce qui n'est ni
`Command`, ni `Query`, ni `UseCase`.

Trois enseignements que cette remédiation confirme :

- **Câbler une règle d'autorisation demande de savoir d'où vient sa donnée d'entrée.**
  `AutorisationChampService` n'était pas seulement débranché : sa signature acceptait un nom de rôle,
  et le brancher naïvement depuis une page aurait reproduit le défaut de la consolidation sécurité.
- **Un axe de paramétrage qui ne participe pas à la résolution n'est pas un axe.** Ajouter la colonne
  `TypeClient` sans toucher au moteur aurait rendu RG-SLA-01 « conforme » sur le papier, et rigoureusement
  rien pour l'administrateur.
- **Un commentaire qui affirme un comportement est un endroit où le chercher.** « archivage automatique
  — représenté par la simple présence de DateClotureUtc » : la phrase désignait précisément ce qui
  manquait.

Le score final n'est pas recalculé.
