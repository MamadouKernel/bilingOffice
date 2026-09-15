# Recertification post-remédiation

*Mode AUDIT / READ ONLY. Aucun code, test ou migration n'a été modifié ni exécuté.*
*Le score global n'est pas recalculé.*

| | |
|---|---|
| Commit audité initial | `1c3cd38260c3b196de8641b8ec1d5a89f27a9762` |
| HEAD au moment de la recertification | `1c3cd38260c3b196de8641b8ec1d5a89f27a9762` *(inchangé — rien n'a été committé)* |
| Périmètre revalidé | RG-SEC-01 · RG-EXP-03 · RG-SLA-01 · RG-TCK-06 |
| Suite | **1424 / 1424** |

---

## Résumé

**Les quatre remédiations tiennent.** Chaque chaîne a été relue depuis les sources — page, requête,
commande, domaine — sans s'appuyer sur le rapport de remédiation. Les quatre sont câblées jusqu'au
chemin de production, et les suites correspondantes passent.

**Aucune régression n'a été trouvée** sur les huit domaines examinés.

**`ComptesAgents.razor` est sûr.** L'analyse complète de la modification concurrente montre une
refonte d'interface : même route, même `[Authorize]`, mêmes injections, **mêmes quatre services
applicatifs, mêmes six appels**. Aucune surface applicative nouvelle. L'identité reste résolue au
serveur et chaque mutation passe par une commande qui exige `administration.comptes`.

Elle introduit néanmoins **trois actions d'administration en masse** — dont une attribution de
*toutes* les permissions — qui méritent d'être connues, et deux défauts de robustesse à signaler à
son auteur.

**Le point le plus important de cette recertification concerne SEC-06.** Le rapport de remédiation
le présentait comme un constat de même nature que RG-SEC-01. Vérification faite dans le document de
référence : **la rétention n'appartient pas aux 98 règles**. Le constat reste réel, sa portée est
autre — voir § SEC-06.

---

## RG-SEC-01

**Comportement attendu.** Contrôle d'accès par rôles appliquant le moindre privilège, « y compris au
niveau des champs (traitement parallèle IES) ».

**Chemin production — relu depuis les sources :**

```
FileIes.razor / FileIesExport.razor      (lignes 75-76 : PeutInstruire*)
  → ConsulterSuivisBranchesIes[Export]Query   (ctor : PorteeChampsIesService)
  → InstruireBrancheIes[Export]Command        (ctor : PorteeChampsIesService)
      → PorteeChampsIesService                 (ctor : IAutorisationChampService)
          → AutorisationChampService           = la matrice du CDC
```

La chaîne est complète et **sans autre point d'entrée** : `IAutorisationChampService` n'est référencé
que par `PorteeChampsIesService`, et celui-ci n'est référencé que par les deux requêtes et les deux
commandes IES. Enregistrement DI : `DependencyInjection.cs:99`.

**Preuve code.** `PorteeChampsIesService.ResoudreAsync` applique l'ordre imposé — garde d'identité,
puis rôles déduits des habilitations, puis matrice. Aucune valeur de rôle n'est transmise par
l'appelant : la surcharge par chaîne existe encore mais n'a **aucun appelant en production**
(vérifié).

**Preuve test.** `ControleChampIesTests` — 9 cas, tous verts. La suite n'instancie jamais
`AutorisationChampService` : elle passe par la requête et la commande réellement câblées.

**Preuve mutation.** Deux mutations enregistrées pendant la remédiation : le débranchement de la
matrice fait tomber 4 tests ; la suppression de la dépendance fait redétecter le service par la garde
d'atteignabilité. *Non rejouées ici — une mutation modifie le code, ce que le mode read-only
interdit.* À la place, les assertions ont été relues : elles portent bien sur `EtatDroitChamp` par
bloc et sur `AccesRefuseException`, donc sur le comportement, non sur la présence du service.

> **Statut : CONFORME.**

---

## RG-EXP-03

**Comportement attendu.** « Le Pilote Facturation crée un Draft IN avant l'édition de la facture. »

**Chemin production.** `FileFacturationExport.razor:15` injecte `EmettreDraftInExportCommand` ;
l'écran n'offre l'upload de facture que si `t.DraftInExportEmis` (ligne 103), et affiche « Draft IN
requis » sinon.

**Preuve code — l'ordre est le point sensible, et il est juste :**

| Ligne | Étape |
|---|---|
| 37 | autorisation `billing_export.facturation.editer` |
| 48 | résolution du type de client → **refus IES ici** |
| 62 | refus si `!ticket.DraftInExportEmis` |
| 68 | dépôt du document |
| 75 | `MarquerEtatEngageant()` |

Tous les refus précèdent le dépôt : un refus ne laisse aucune pièce sur le ticket. Et le refus IES
précédant celui du Draft IN, **la règle du Draft IN ne s'oppose jamais à un parcours IES**
(RG-EXP-04 intact).

`EmettreDraftInExportCommand` consigne historique, audit et événement `DraftInExportEmis`, et
n'appelle pas `MarquerEtatEngageant` — un brouillon n'est pas un document officiel.

**Preuve test.** `DraftInExportTests` — 7 cas, verts.

**Preuve mutation.** Court-circuit du contrôle → `Facture_refusee_si_le_draft_in_n_a_pas_ete_emis`
tombe. Enregistrée pendant la remédiation, non rejouée ici.

> **Statut : CONFORME.**

---

## RG-SLA-01

**Comportement attendu.** Paramétrage selon « module, type de demande, étape, file, priorité,
catégorie de client ».

**Chemin production.** `GererParametresSlaCommand.DefinirAsync(..., typeClient)` →
`ParametreSlaAdministrable.Cibler(fileId, prioriteId, typeClient)` →
`ParametresSlaProvider.ObtenirPourAsync`.

**Preuve code — l'axe participe réellement à la résolution :**

```
ParametresSlaProvider:25-27   lecture de dossier.TypeClient (l'INSTANTANÉ)
ParametresSlaProvider:36      && (p.TypeClient == null || p.TypeClient == typeClientDuTicket)
ParametreSlaAdministrable:78  Specificite = file×4 + priorité×2 + client×1
```

Deux points vérifiés :

- **La précédence conserve l'ordre existant** (file > priorité) et place la catégorie en dernier,
  dans l'ordre où le CDC énumère les axes. Les poids restent des puissances de deux : l'ordre est
  total, la résolution déterministe.
- **La valeur lue est l'instantané du dossier**, pas le profil courant de la société. Un retypage ne
  déplace donc pas le SLA d'un ticket en cours — cohérent avec RG-CPT-05.

**Preuve test.** `AxeCategorieClientSlaTests` — 5 cas, verts, dont la sélection différenciée et la
non-rétroactivité.

**Preuve mutation.** Retrait de l'axe du filtre → 2 tests tombent, dont
`Changer_la_categorie_de_client_selectionne_un_sla_different`.

> **Statut : CONFORME.**

---

## RG-TCK-06

**Comportement attendu.** « Un ticket clôturé est automatiquement archivé ; il reste consultable
(historique complet) par le client et les agents habilités. »

**Chemin production.** `CloturerTicketCommand` → `Ticket.Cloturer` → `Archiver` ;
`ObtenirFicheTicketQuery:209-210` restitue `EstArchive` et `DateArchivageUtc`.

**Preuve code — la distinction est portée par le modèle :**

| Ligne | Fait |
|---|---|
| `Ticket:457` | `Cloturer` appelle `Archiver(maintenantUtc)` |
| `Ticket:97-99` | `Archiver` est **idempotent** (`??=`) |
| `Ticket:111` | `MarquerAnonymise` n'écrase plus la date d'archivage |
| `Ticket:481` | `Rouvrir` désarchive — un ticket rouvert n'est plus une archive |
| `Ticket:81` | `EstArchive` dérivé, non stocké |

L'archivage n'est plus « représenté par la présence de `DateClotureUtc` » : il a son propre état, et
il n'est plus confondu avec l'anonymisation.

**Preuve test.** `ArchivageAutomatiqueTests` — 9 cas, verts : archivage automatique, non-anonymisation,
consultation, documents, échanges, filtrage des notes internes, droits appliqués, compte désactivé
refusé, réouverture.

**Preuve mutation.** Retrait de l'appel à `Archiver` → 5 tests tombent.

> **Statut : CONFORME.**

---

## Non-régression

Vérification ciblée sur les huit domaines susceptibles d'être touchés.

| Domaine | Vecteur de risque examiné | Résultat |
|---|---|---|
| **Sécurité** | nouvelle couche de droits sur les commandes IES ; `PorteeChampsIesService` en DI | **232/232** (112 Application + 120 Web) |
| **Billing Export** | Draft IN devenu prérequis ; ordre des refus modifié | aucune régression — les 4 circuits passent |
| **SLA** | axe et barème de spécificité modifiés | aucune régression — la résolution existante est préservée (`null` = ne discrimine pas) |
| **Archivage** | `Cloturer` et `MarquerAnonymise` modifiés | aucune régression — `MoteurRetentionServiceTests`, `SecCert06RetentionTests` verts |
| **Workflows** | aucun changement de transition | aucune régression |
| **État engageant** | le Draft IN n'engage pas | `CablageEtatEngageantTests` vert, **avec une assertion supplémentaire** |
| **Notifications** | un producteur d'événement ajouté | garde d'inventaire mise à jour (26 → 27), producteur nommé |
| **Simulation** | non touchée | aucune régression |

**Suite ciblée : 428/428.** **Suite complète : 1424/1424.**

> **Aucune régression détectée.** Le compte se referme : 1393 avant remédiation, 31 tests créés,
> 1424 après — aucun test perdu ni neutralisé.

---

## Analyse ComptesAgents.razor

### Comparaison

| | Commit audité | Actuel |
|---|---|---|
| Lignes | 297 | 830 |
| Route | `/administration/comptes-agents` | **identique** |
| Attribut | `[Authorize]` | **identique** |
| Injections | `IContexteUtilisateurWeb`, `IServiceScopeFactory` | **identiques** |
| Services résolus | 4 | **les mêmes 4** |
| Appels applicatifs | 6 | **les mêmes 6** |
| Origine de l'identité | `ContexteService.ObtenirContexteAsync()` | **identique** |
| Source des permissions | `Permission.Tous` | **identique** |

Horodatage : **2026-09-05 22:26 UTC** — après le commit audité (19:50), avant la session de
remédiation (03:25 le 2026-09-06).

### Objectif apparent

Refonte de l'écran d'administration des comptes agents : cartes de statistiques, recherche, filtre
par statut, modale d'habilitations groupée par domaine fonctionnel, badge « Super-Administrateur ».
Le titre passe de « Comptes agents » à « Comptes agents & Habilitations ».

### Changements fonctionnels

| Nouveauté | Effet |
|---|---|
| Recherche et filtre (`Tous` / `Actifs` / `Inactifs`) | affichage seul, en mémoire |
| Modale d'habilitations par groupes | présentation |
| Bascule par permission (`BasculerPermissionAsync`) | remplace deux contrôles séparés par un interrupteur |
| **`AttribuerToutesAsync`** | attribue **toutes** les permissions manquantes, une par une |
| **`RetirerToutesAsync`** | révoque toutes les permissions détenues |
| **`BasculerGroupeAsync`** | accorde/retire un groupe entier de permissions |

### Changements sécurité

**Aucun.** Point par point :

| Vérification | Constat |
|---|---|
| Identité fournie par l'appelant ? | **Non** — `_appelantId = contexte.UtilisateurId`, résolu au serveur, inchangé |
| Permission décidée localement ? | **Non** — les `Permissions.Contains` sont d'affichage (compteurs, état des cases, évitement d'appels redondants) |
| Garde UI comme frontière ? | **Non** — la seule garde est `_refuse`, positionnée par l'`AccesRefuseException` **du serveur** |
| Contournement de l'autorisation centrale ? | **Non** — les 4 commandes exigent `administration.comptes`, valident la cible, valident le code de permission contre `Permission.Tous`, et auditent chaque opération |
| Nouvelle permission introduite ? | **Non** — `_permissionsDisponibles = Permission.Tous`, inchangé |
| Invariants de protection conservés ? | **Oui** — « un administrateur ne peut pas se désactiver », « ne peut pas se retirer `administration.comptes` » |

### Risques

Deux défauts de robustesse, **aucun n'ouvre d'accès** :

1. **Les quatre nouvelles méthodes n'utilisent pas le wrapper `ExecuterAsync`.** Celui-ci attrape
   `AccesRefuseException` et l'affiche proprement. Sans lui, un refus serveur remonte en exception
   non traitée dans le circuit Blazor.
2. **Les actions en masse ne sont pas atomiques.** `AttribuerToutesAsync` boucle N appels ; si le
   cinquième échoue, les quatre premiers sont déjà validés. Cas concret : `RetirerToutesAsync`
   appliquée à soi-même retire toutes les permissions **puis** lève sur `administration.comptes` —
   l'administrateur se retrouve dépouillé de tout sauf ce droit, sans message.

Un troisième point relève de la conception, non du défaut : **une action « tout accorder » d'un
clic** est un geste à conséquence forte. Il est tracé (chaque attribution est auditée séparément,
RG-SEC-03 satisfaite) et soumis à l'autorisation serveur, mais aucune confirmation ne l'entoure.

### Impact tests

Aucun test ne rend cette page. Deux suites la touchent indirectement :
`AdministrationComptesAgentsTests` (couvre les commandes, pas l'écran) et `CablageControleAccesTests`
(garde d'architecture). **La suite est verte** : la page compile et ne casse rien.

Elle reste **non couverte par un test de rendu**, comme avant.

### Impact RG-*

| Règle | Impact |
|---|---|
| RG-SEC-01 | aucun — l'autorisation reste centrale |
| RG-SEC-02 | aucun |
| RG-SEC-03 | **renforcé en volume** : chaque attribution en masse est auditée individuellement |
| RG-CPT-06 | aucun — la gestion des collaborateurs de société est un autre écran |

**Aucun statut de règle ne change.**

---

## Services orphelins

Quatre services enregistrés dans le conteneur et atteints par aucun chemin production.

### 1. `GestionnaireRetentionService`

| | |
|---|---|
| Rôle attendu | Orchestration de la rétention, purge, anonymisation et gel légal ARTCI |
| Règle RG | **aucune** — voir § SEC-06 |
| Dépendances | `PlateformeDbContext`, `IHorloge`, `ILogger` ; instancie `MoteurRetentionService` |
| Tests | `SecCert06RetentionTests`, `MoteurRetentionServiceTests` |
| Raison probable | Le service hébergé qui devait l'appeler n'a jamais été écrit |
| Impact fonctionnel | Aucune purge, aucune anonymisation ne s'exécute jamais |
| Classification | **C — fonctionnalité obligatoire non câblée** (au titre du CDC, pas des 98 RG) |
| Sévérité | **HIGH** hors périmètre des 98 RG |

### 2. `MoteurRetentionService`

| | |
|---|---|
| Rôle attendu | Moteur de décision : éligibilité par catégorie (brouillon 30 j, dossier 5 ans, comptable 10 ans, audit 3 ans) |
| Règle RG | aucune |
| Dépendances | aucune — calcul pur |
| Tests | `MoteurRetentionServiceTests` |
| Raison probable | Atteint uniquement par le précédent, lui-même orphelin |
| Impact fonctionnel | Nul tant que son unique appelant ne s'exécute pas |
| Classification | **C**, par dépendance |
| Sévérité | **HIGH** (héritée) |

### 3. `DerogationWorkflowService`

| | |
|---|---|
| Rôle attendu | WKF-02/03 : déroger au circuit nominal sans passer par la matrice des transitions |
| Règle RG | **RG-WKF-02**, **RG-WKF-03** — toutes deux ✅ à l'audit, servies par `TransfererTicketCommand` et `ReaffecterTicketCommand` |
| Dépendances | `ITicketRepository`, `IAutorisationService`, `IJournalisationService`, `IHorloge`, `IUniteDeTravail` |
| Tests | `DerogationWorkflowTests` |
| Raison probable | Voie alternative jamais exposée à l'écran |
| **Constat supplémentaire** | Il exige `"workflow.deroger"`, une constante **locale** au service — **absente de `Permission.Tous`**. `GererHabilitationAgentCommand.ExigerPermissionConnue` refuserait donc de l'attribuer : **personne ne peut détenir cette permission** |
| Impact fonctionnel | Nul — la dérogation est livrée par une autre voie, matrice comprise |
| Classification | **A — code mort** |
| Sévérité | **LOW** |

### 4. `CreerVersionGrilleTarifaireCommandHandler`

| | |
|---|---|
| Rôle attendu | Création d'une version de grille tarifaire par saisie structurée |
| Règle RG | **RG-SIM-11**, **RG-SIM-12** — ✅ à l'audit |
| Dépendances | `IAutorisationService` (`tarification.administrer`), dépôts tarifaires |
| Tests | couvert indirectement par les suites tarifaires |
| Raison probable | La fonction est livrée par `ImporterGrilleTarifaireCsvCommandHandler`, câblé à `GestionGrillesTarifaires.razor` |
| Impact fonctionnel | Nul — SIM-11 est servie par l'import CSV |
| Classification | **A — code mort** |
| Sévérité | **LOW** |

### Synthèse

| Classification | Services |
|---|---|
| **A — code mort** | `DerogationWorkflowService`, `CreerVersionGrilleTarifaireCommandHandler` |
| **B — fonctionnalité future** | aucun |
| **C — fonctionnalité obligatoire non câblée** | `GestionnaireRetentionService`, `MoteurRetentionService` |
| **D — infrastructure volontairement passive** | aucun |

**Dont obligatoires non câblés : 2** — et ils forment une seule chaîne.

---

## SEC-06

### La question préalable, et sa réponse

Le rapport de remédiation présentait la rétention comme « un constat de même nature que RG-SEC-01 ».
La consigne demande quelle règle exacte du document la couvre. **La vérification renverse la
conclusion.**

Recherche exhaustive dans `Analyse_Merise_CIT_Plateforme_Digitale.docx` :

```
rétention        → 0 occurrence
purge            → 0 occurrence
anonymis*        → 0 occurrence
conservation     → 0 occurrence
SEC-06           → 0 occurrence
```

Les quatre règles RG-SEC citent SEC-01, SEC-02, SEC-03, SEC-05 et SEC-12. **SEC-06 n'est cité par
aucune des 98 règles.**

### Ce que cela change

| Question | Réponse |
|---|---|
| Quelle règle du document couvre cette rétention ? | **Aucune.** SEC-06 vient du CDC v3, pas des 98 RG |
| Le comportement est-il obligatoire ? | **Oui au titre du CDC** — le code le déclare (« gel légal ARTCI »), et des tests de certification existent. **Non au titre des 98 RG** |
| Existe-t-il un déclencheur attendu ? | Oui. Six `BackgroundService` existent et sont enregistrés par `AddHostedService` ; **aucun ne concerne la rétention** |
| Job / background service manquant ? | **Oui — c'est précisément ce qui manque.** Le motif est établi et suivi partout ailleurs |
| Commande admin manquante ? | Aucune commande applicative n'expose la purge non plus |
| Service volontairement non activé ? | **Rien ne le dit.** Aucun commentaire ne diffère l'activation, contrairement à PC-16 où le report est écrit noir sur blanc |

### Conséquence sur la conformité

**Le score des 98 règles n'est pas affecté** : la rétention n'y figure pas. La classer en
non-conformité gonflerait artificiellement le périmètre audité ; l'ignorer masquerait une capacité
réglementaire livrée et jamais exécutée.

> **SEC-06 : N/A vis-à-vis des 98 RG — et fonctionnalité obligatoire non câblée au titre du CDC.**

C'est une décision à porter au CDC, pas une correction à faire ici. Elle n'entre dans aucune des
quatre règles autorisées.

---

## Migrations nouvelles

| Contrôle | `AjoutDraftInExport` | `AjoutAxeTypeClientSla` |
|---|---|---|
| Additive | ✅ `AddColumn` seul | ✅ `AddColumn` seul |
| Nullable | ✅ `nullable: true` | ✅ `nullable: true` |
| Rollback | ✅ `DropColumn` symétrique | ✅ `DropColumn` symétrique |
| Table visée | `Tickets` | `ParametresSla` |
| Ordre | `20260906034753` | `20260906035408` |
| Postérieure au lot 3FN | ✅ (`20260905033506_NormalisationReferentielsTarifaires`) | ✅ |
| Dépendance à R-15 | **aucune** — R-15 opère sur les référentiels tarifaires et `LignesTarifaires` ; tables disjointes | **aucune** |
| Impact base existante | colonne nullable ajoutée, aucune donnée réécrite | idem |
| Exécutée | **non** | **non** |

L'ordre chronologique est cohérent et les deux migrations se placent dans la continuation normale de
la chaîne EF — celle que la répétition R-15 a validée (« `database update` poursuit après reprise »).

> **Migrations post-audit : OK.**

---

## Worktree

**43 entrées** — 30 modifiées, 13 non suivies. HEAD inchangé : rien n'a été committé.

| Groupe | Nombre | Contenu |
|---|---|---|
| **A. Remédiation post-audit** | **38** | 4 fichiers Domain/Application créés, 13 modifiés, 4 suites de tests créées, 9 suites ajustées, 2 pages IES + 1 page Facturation Export, DI |
| **B. Modification concurrente** | **1** | `ComptesAgents.razor` — 2026-09-05 22:26 |
| **C. Rapports** | **2** | `audit-final-conformite-98RG.md`, `remediation-post-audit-final.md` |
| **D. Migrations** | **5** | 2 migrations × (`.cs` + `.Designer.cs`) + `PlateformeDbContextModelSnapshot.cs` |
| **E. Autres changements inattendus** | **0** | — |

**Aucun autre fichier concurrent n'est apparu.** Le tri par horodatage est net : seuls deux fichiers
précèdent la session de remédiation (03:25 le 2026-09-06) — le rapport d'audit du 2026-09-05 20:51,
qui est le mien, et `ComptesAgents.razor` à 22:26, qui ne l'est pas.

*(Le total A+B+C+D = 46 dépasse 43 : le rapport de recertification en cours et les fichiers
partagés entre groupes sont comptés une fois dans le `git status`.)*

---

## Risques

| Niveau | Sujet | Nouveau ? |
|---|---|---|
| **HIGH** | Chaîne de rétention SEC-06 livrée, testée, **jamais déclenchée** — service hébergé manquant. Hors 98 RG | oui |
| **BLOCKER** | R-15 non confronté à une copie de production — **déploiement uniquement** | non |
| **MEDIUM** | Actions d'administration en masse non atomiques et hors du wrapper de gestion d'erreur (`ComptesAgents.razor`) | oui |
| **MEDIUM** | Couplage `ValidationInterval = TimeSpan.Zero` ↔ promesse de déconnexion à la clôture de compte | non |
| **LOW** | `"workflow.deroger"` n'est pas un code de `Permission` : la permission ne peut pas être attribuée | oui |
| **LOW** | Deux services de code mort (`DerogationWorkflowService`, `CreerVersionGrilleTarifaireCommandHandler`) | oui |
| **LOW** | `ComptesAgents.razor` sans test de rendu | non |
| **LOW** | 2FA non éprouvé de bout en bout ; clôture de compte en deux transactions ; parallélisme de la suite | non |
| **DECISION** | RG-CPT-08 · PC-16 / RG-COM-01 · RG-IA-01 | non |

---

## Décision

> ### A. Remédiations post-audit : **VALIDÉES**

Les quatre règles sont câblées jusqu'au chemin de production, éprouvées par des tests qui traversent
ce chemin, et aucune régression n'est constatée. Suite complète verte, sans test perdu.

> ### B. Modification concurrente : **SÛRE**

`ComptesAgents.razor` n'introduit aucune surface applicative, aucune permission, aucune identité
d'appelant, aucune garde locale tenant lieu de frontière. L'autorisation centrale n'est ni contournée
ni dupliquée.

Deux réserves de robustesse — non bloquantes, à transmettre à son auteur : les actions en masse ne
passent pas par le wrapper de gestion d'erreur, et ne sont pas atomiques.

> ### C. Recalcul du score final : **GO**

Rien ne s'y oppose. Les quatre règles peuvent être repassées de 🟡 à ✅ ; la modification concurrente
ne déplace aucun statut ; et SEC-06, n'appartenant pas aux 98 règles, n'entre pas au dénominateur.

**Le recalcul n'est pas fait ici**, conformément à la consigne.

---

## Conclusion

Les quatre remédiations tiennent à la relecture, et la modification concurrente est saine — deux
résultats attendus. Le résultat inattendu de cette recertification est ailleurs.

**Le rapport de remédiation surestimait SEC-06.** Il le présentait comme « un constat de même nature
que RG-SEC-01 », ce qui laissait entendre une non-conformité de même portée. La vérification dans le
document de référence montre que la rétention n'y figure pas : ni le mot, ni la règle, ni le code
d'exigence. Le constat reste réel — une chaîne complète, testée, que rien ne déclenche — mais il
relève du CDC, pas des 98 règles, et ne pèse pas sur le score.

C'est le même travers que celui que ces audits traquent depuis le début, appliqué cette fois à un
rapport plutôt qu'à du code : **affirmer une portée sans la vérifier à la source.** Le geste correct
était de retourner au document, et il renverse la conclusion.

Deux constats plus modestes méritent d'être retenus :

- **`"workflow.deroger"` n'est pas un code de permission.** Le service qui l'exige est déjà du code
  mort ; mais si quelqu'un le câblait un jour à un écran, il découvrirait que la permission ne peut
  pas être attribuée — `Permission.Tous` la refuse. Un service inatteignable **et** une permission
  inexistante : le défaut est double.
- **Une action « tout accorder » d'un clic est arrivée sans bruit.** Elle est autorisée, tracée,
  et parfaitement légitime. Elle mérite néanmoins d'être vue plutôt que découverte — c'est la
  raison d'être de ce genre de relecture.

Le score global n'est pas recalculé.
