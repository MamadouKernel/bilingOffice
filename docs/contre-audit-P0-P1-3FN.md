# Contre-audit après P0/P1

> **Document** : `docs/contre-audit-P0-P1-3FN.md`
> **Établi le** : 2026-09-05
> **Mode** : lecture seule — aucune correction n'a été développée
> **Références** : `Analyse_Merise_CIT_Plateforme_Digitale.docx`, `docs/audit-conformite-merise.md`,
> `docs/remediation-P0-securite.md`, `docs/remediation-P1.md`

---

## 1. Résumé exécutif

**Le P0 n'est pas clos. Il est RÉOUVERT.**

La remédiation P0 a fermé la surface API et deux variantes, et ses 12 tests passent. Mais elle
reposait sur une affirmation de ma part qui était **fausse** : mon rapport P0 classait
`ObtenirFicheTicketQuery` et `ObtenirFicheDossierQuery` parmi les « surfaces vérifiées et déjà
correctement gardées ». Je n'avais examiné que leur branche **client**. Leur branche **agent**
n'existe pas : il n'y a aucun contrôle.

Conséquence, sur la page principale de consultation d'un ticket : **un compte de nature `Agent`
sans aucune habilitation lit n'importe quel ticket par son identifiant, notes internes comprises.**
C'est la même classe de défaut que le P0 d'origine — une ressource désignée par un identifiant
fourni par l'utilisateur, rendue sans décision d'autorisation — et elle divulgue davantage que la
faille API corrigée, puisque la fiche inclut les échanges internes.

Conformément à la consigne, **l'analyse de priorité est suspendue** : je ne recalcule pas le score
global ni l'ordre des P2 tant qu'un P0 est ouvert. Les sections indépendantes — contre-audit du lot
3FN et analyse R-15 — sont livrées, car elles conditionnent une décision distincte.

Sur le reste : **P1-01 et P1-03 sont validés**, **P1-02 est validé dans sa part technique**, et le
**lot 3FN est conforme** au modèle du document, en deux points même plus strict. En revanche,
**R-15 est plus grave qu'annoncé** : la migration tarifaire n'est pas « risquée » sur une base
peuplée, elle est **inapplicable** — elle échouera.

---

## 2. État du repository

| Élément | Valeur |
|:---|:---|
| HEAD | `75bd614` |
| Fichiers non committés | 237 |
| Migrations non appliquées | `NormalisationReferentielsTarifaires` (3FN), `AjoutSeuilAnnulationParWorkflow` (P1-01), `AjoutAssistantVirtuel` (P1-03) |
| Suite de tests | 1 268 / 1 268 au vert |

Les trois lots — 3FN, P0, P1 — coexistent dans l'arbre de travail, non committés.

---

## 3. Validation P0 — **RÉOUVERT**

### 3.1 Ce qui est effectivement fermé

| Surface | État | Preuve |
|:---|:---|:---|
| `GET /api/v1/tickets/{numero}` | ✅ Fermé | `ApiEndpoints.cs:46` → `ControleAccesRessourceService` |
| `GET /api/v1/dossiers/{reference}` | ✅ Fermé | `ApiEndpoints.cs:85` |
| `ConsulterDossierQuery` (`/dossiers/{guid}`) | ✅ Fermé | Identité exigée, décision déléguée |
| `IndicateursTemps.razor` (`/tickets/{guid}/temps`) | ✅ Fermé | Contrôle avant affichage |
| `ControleAccesRessourceService` | ✅ Fail-closed sur **tous** les chemins | Identité vide, ressource nulle, compte inconnu, rattachement inactif, agent sans habilitation → `false` |
| Tests `ApiCloisonnementTests` | ✅ 12 / 12 | Rejoués après chaque P1 |

### 3.2 Le défaut résiduel — comparaison des trois implémentations

| | `ControleAccesRessourceService` | `ObtenirFicheTicketQuery` | `ObtenirFicheDossierQuery` |
|:---|:---|:---|:---|
| Compte inconnu (`info is null`) | **Refus** | `estClient` = false → **aucun contrôle** | idem |
| Client sans société | Refus sauf dossier personnel | **Refus** (même pour son propre dossier d'enrôlement) | idem |
| Client d'une autre société | Refus | Refus | Refus |
| Portée ENT-03 | Appliquée | Appliquée | Appliquée |
| **Agent sans habilitation** | **Refus** | **AUCUN CONTRÔLE** | **AUCUN CONTRÔLE** |
| Agent hors de sa file | Refus | **AUCUN CONTRÔLE** | **AUCUN CONTRÔLE** |

**Les trois ne sont pas équivalentes, et deux ne sont pas fail-closed.**
Preuves : `ObtenirFicheTicketQuery.cs:41-55` — le bloc de garde est intégralement enveloppé dans
`if (estClient)`. `ObtenirFicheDossierQuery.cs:32-46` — même structure.

### 3.3 Chemin d'exploitation

1. Un administrateur crée un compte agent : `CreerCompteAgentCommand` →
   `AdministrationComptesRepository.cs:42` pose `Nature = Agent`, `EstActif = true`, et
   **n'attribue aucune habilitation** — celles-ci relèvent d'une commande distincte.
2. Ce compte s'authentifie et ouvre `/tickets/{n'importe quel GUID}`
   (`FicheTicket.razor:511` → `FicheTicketQuery.ExecuterAsync(_utilisateurId, Id)`).
3. `estClient` vaut `false` → aucune garde ne s'exécute.
4. La fiche complète est rendue — et `ObtenirFicheTicketQuery.cs:104-106` sélectionne
   `ListerParTicketAsync` (et non `ListerVisiblesParClientAsync`) : **les notes internes CIT sont
   incluses**.

Le même chemin existe sur `/dossiers/{id}`.

### 3.4 Pourquoi c'est bien un défaut, et non un choix

`ObtenirFicheEnrolementQuery` traite exactement le même cas de figure et **garde correctement**
la branche agent :

```csharp
if (!estClient && !aDroitInstruction)
    throw new AccesRefuseException($"L'utilisateur {appelantId} n'est pas habilité …");
```
— `ObtenirFicheEnrolementQuery.cs:82-85`

La règle est donc connue du projet et appliquée ailleurs. Son absence sur les deux requêtes
centrales est une omission.

### 3.5 Ce qui protège aujourd'hui — et ce qui ne protège pas

| Protection | Effet réel |
|:---|:---|
| `[Authorize]` sur la page | Exige une authentification, **pas** une habilitation |
| Masquage des actions (`peutPrendreEnCharge`…) | Masque les **boutons**, pas la **lecture** |
| `ApiCloisonnementTests.Cas11` | Couvre l'API, **pas** la page |

Aucun test ne couvre le cloisonnement agent sur les deux requêtes de fiche.

### 3.6 Verdict

**P0 = RÉOUVERT.** L'analyse de priorité est arrêtée ici, conformément à la consigne.

---

## 4. Validation P1-01 — **VALIDÉ, avec une réserve de couverture**

### 4.1 Matrice de l'état engageant

| Événement métier | Engageant attendu ? | Appel présent ? | Test verrouillant ? | Résultat |
|:---|:---|:---|:---|:---|
| Facture éditée (import) | Oui — document officiel | ✅ `EditerFactureCommand.cs:59` | ❌ | ✅ câblé |
| Facture éditée (export) | Oui | ✅ `EditerFactureExportCommand.cs:63` | ❌ | ✅ câblé |
| Preuve de paiement validée (import) | Oui — paiement confirmé | ✅ `ValiderPreuvePaiementCommand.cs:33` | ❌ | ✅ câblé |
| Preuve de paiement validée (export) | Oui | ✅ `ValiderPreuvePaiementExportCommand.cs:32` | ❌ | ✅ câblé |
| Lettrage confirmé (import) | Oui | ✅ `ConfirmerLettrageCommand.cs:41` | ❌ | ✅ câblé |
| Lettrage confirmé (export) | Oui | ✅ `ConfirmerLettrageExportCommand.cs:35` | ❌ | ✅ câblé |
| BS/BADT déposés (import) | Oui — document officiel | ❌ absent | — | 🟡 sans effet observable |
| BEE déposé (export) | Oui | ❌ absent | — | 🟡 sans effet observable |
| Jointure IES finalisée | Oui — remise des documents | ❌ absent | — | 🟡 sans effet observable |

Les trois derniers surviennent **à la clôture**. `Ticket.Annuler()` refuse déjà tout ticket
`Cloture` ou `Annule` (`Ticket.cs:337-340`), et `EstEligibleASuggestionDeCloture` exige
`EnAttenteClient`. Leur omission n'a donc **aucun effet observable** aujourd'hui — mais elle
serait un piège si la règle d'annulation évoluait.

### 4.2 Vérification conceptuelle des transitions

| Situation | Comportement attendu | Comportement constaté |
|:---|:---|:---|
| Avant tout traitement engageant | Annulation directe autorisée selon paramétrage | ✅ `AnnulationSeuilBloquantTests` |
| Après état engageant | Annulation directe interdite | ✅ `AnnulationLibreFermeeException` |
| Après état engageant | Demande d'annulation toujours recevable | ✅ testé |
| Après traversée d'une file bloquante déclarée | Annulation directe interdite | ✅ `AnnulationTests` (seuil réel) |
| Après état engageant | Suggestion de clôture pour absence de réponse interdite | ✅ `EstEligibleASuggestionDeCloture` consulte `!EstEnEtatEngageant` |

### 4.3 Règles recontrôlées

| Règle | Statut | Preuve |
|:---|:---|:---|
| RG-TCK-07 | ✅ CONFORME | Décision prise par la commande, deux sources, seuil paramétrable + écran d'administration |
| RG-TCK-10 | ✅ CONFORME | `ParametresRelance` administrable, inchangé |
| RG-TCK-11 | ✅ CONFORME — **était un faux positif** | Le drapeau est désormais réellement posé |
| RG-TCK-12 | ✅ CONFORME | `ReouvrirTicketCommand` + permission, inchangé |

### 4.4 Réserve

**Aucun test ne verrouille le câblage.** `grep EstEnEtatEngageant tests/` ne retourne qu'un
commentaire. Retirer `ticket.MarquerEtatEngageant()` d'une des six commandes ne ferait échouer
aucun test — c'est-à-dire exactement la situation qui a produit le faux positif d'origine. La
correction est bonne ; sa protection contre la régression ne l'est pas.

---

## 5. Validation P1-02 — **VALIDÉ techniquement, 🟡 fonctionnellement**

### 5.1 Distinction imposée par la consigne

| Dimension | État | Nature |
|:---|:---|:---|
| Interpolation des gabarits | ✅ Livrée | Développement |
| Lien direct vers le ticket dans le corps | ✅ Garanti même si le gabarit l'omet | Développement |
| Propagation aux canaux externes | ✅ `ReessayerEnvoisNotificationUseCase.cs:125` envoie `notification.Titre` / `.Corps` | Développement |
| Notification portail | ✅ Lien présent (`CentreNotifications.razor:87`) | Développement |
| **Envoi réel e-mail / SMS / WhatsApp** | ⛔ `CanalJournalise` | **Décision — PC-16 non arbitré** |

Le corps interpolé alimente bien la couche d'envoi : la correction n'est pas cosmétique, elle
atteint le point où un adaptateur réel consommerait le message.

### 5.2 Règles recontrôlées

| Règle | Statut | Motif |
|:---|:---|:---|
| RG-COM-01 | 🟡 PARTIEL | Structure et lien conformes ; envoi réel suspendu à PC-16 |
| RG-COM-02 | ✅ CONFORME | Modèles et règles paramétrables par module/étape/canal |
| RG-COM-03 | ✅ CONFORME | Fil du ticket = canal de référence |
| RG-NOT-01 | ✅ CONFORME | `EstLue` / `MarquerCommeLue` + lien direct |
| RG-NOT-02 | ✅ CONFORME | `RegleDiffusion(TypeEvenement, Canal, Module?, FileId?)` |
| RG-SEC-04 | ⚠️ **NON VÉRIFIABLE en l'état** | Voir ci-dessous |

### 5.3 RG-SEC-04 — réserve nouvelle

La règle exige qu'un lien transmis par e-mail/SMS ne donne **jamais** d'accès direct à un document
protégé, et conduise au ticket après authentification. Le lien produit pointe vers
`/tickets/{id}`, page portant `[Authorize]` — donc l'authentification est bien exigée. **Mais**
l'habilitation à consulter ce ticket n'est pas vérifiée par `ObtenirFicheTicketQuery` pour un
non-client (§ 3). Le lien reste donc sûr vis-à-vis d'un client ; il ne l'est pas vis-à-vis d'un
agent non habilité. Le statut de RG-SEC-04 dépend de la correction du P0 réouvert.

---

## 6. Validation P1-03 — **VALIDÉ en architecture, 🟡 en capacité**

Décomposition demandée :

| Aspect | État | Preuve |
|:---|:---|:---|
| A. Architecture assistant | ✅ | Port `IAssistantConversationnel`, domaine, cas d'usage, écran |
| B. Adaptateur | 🟡 **SIMULATEUR / FAKE ADAPTER** | `AssistantSimule` — 8 sujets, appariement par mots-clés, aucun modèle de langage |
| C. Conversation persistée | ✅ | `ConversationsAssistant` + `MessagesAssistant`, migration |
| D. Rattachement au ticket | ✅ | `ConversationAssistant.TicketId`, FK, testé |
| E. Escalade humaine | ✅ | Action explicite de l'usager, jamais effet de bord ; cloisonnée |
| F. Conservation du contexte | ✅ | `Transcrire()` publié dans le fil du ticket, testé |
| G. **Capacité réelle de compréhension/orientation** | ⛔ **ABSENTE** | 8 sujets par mots-clés : ni reformulation, ni raisonnement sur le dossier |

### 6.1 Statuts

| Règle | Statut | Motif |
|:---|:---|:---|
| RG-IA-01 | 🟡 PARTIEL | « répond aux questions de premier niveau » : partiellement, sur 8 sujets. « l'aide à compléter ses dossiers » (IA-02) : **non** — l'adaptateur ne consulte aucun dossier |
| RG-IA-02 | ✅ CONFORME | Escalade sur décision, sans perte de contexte, fil attaché au ticket, vérifié par 4 tests |

**Le score n'est pas gonflé par la présence de l'architecture.** RG-IA-01 reste partiel parce que
le comportement réellement disponible ne satisfait pas IA-02. L'adaptateur est un **simulateur**,
et doit être désigné comme tel dans tout document destiné au commanditaire.

---

## 7. Faux positifs de l'audit initial

| Règle | Statut initial | Statut réel | Cause |
|:---|:---|:---|:---|
| RG-TCK-11 | ✅ CONFORME | 🔴 à l'époque | `MarquerEtatEngageant()` sans appelant en production ; le test pilotait lui-même la condition |
| RG-CPT-04 / RG-SEC-02 | 🔴 (API) | 🔴 **plus large** | Le défaut existait aussi sur `/tickets/{id}` et `/dossiers/{id}` — non détecté |
| RG-SEC-01 | ✅ CONFORME | 🟡 | Le cloisonnement agent par file n'est pas appliqué sur les deux requêtes de fiche |

**Cause commune aux trois : j'ai conclu de l'existence d'un contrôle à son application.** Pour
RG-TCK-11, un drapeau existait sans être posé. Pour les requêtes de fiche, une garde existait sans
couvrir tous les acteurs. Le rapport P0 reproduit la même erreur en affirmant ces deux requêtes
« correctement gardées » après n'avoir lu que leur branche client.

## 8. Faux négatifs de l'audit initial

Aucun identifié. Les règles classées 🔴 ou 🟡 l'étaient à juste titre.

---

## 9. Contre-audit du lot 3FN — **CONFORME**

### 9.1 Correspondance table par table

| Table du MPD | Table réelle | PK | FK | Unicité MPD | Unicité réelle | Verdict |
|:---|:---|:---|:---|:---|:---|:---|
| `GRILLE_TARIFAIRE` | `VersionsGrillesTarifaires` | Guid | — | — | — | ✅ |
| `SENS_OPERATION` | `SensOperations` | Guid | — | `code` | `Code` unique | ✅ |
| `TAILLE_CONTENEUR` | `TaillesConteneur` | Guid | — | `code` | `Code` unique | ✅ |
| `TYPE_CONTENEUR` | `TypesConteneur` | Guid | — | `code` | `Code` unique | ✅ |
| `CATEGORIE_MARCHANDISE` | `CategoriesMarchandise` | Guid | → `SensOperations` | `(code, id_sens)` | `(Code, SensOperationId)` | ✅ |
| `LIGNE_TARIF` | `LignesTarifaires` | Guid | → grille, sens, taille, catégorie | `(grille, sens, taille, catégorie)` | identique | ✅ |
| `PARAMETRE_MAJORATION` | `ParametresMajoration` | Guid | → grille | `(grille, type)` | identique | ✅ |
| `SEUIL_POIDS_MAJORATION` | `SeuilsPoidsMajoration` | Guid | → grille, taille | `(grille, taille)` | identique | ✅ |
| `TRANCHE_STATIONNEMENT` | `TranchesStationnement` | Guid | → grille | *aucune* | `(grille, BorneMinJour)` unique | ✅ **plus strict** |
| `FRANCHISE_STATIONNEMENT` | `FranchisesStationnement` | Guid | → grille, sens | `(grille, sens)` | identique | ✅ |
| `QUOTE_PART_BENEFICIAIRE` | `QuotesPartsBeneficiaire` | Guid | → grille | *aucune* | `(grille, Beneficiaire)` unique | ✅ **plus strict** |
| `SIMULATION` | `SimulationsProforma` | Guid | → sens, ticket | `Reference` | `Reference` unique + `(Societe, Date)` | ✅ |
| `CONTENEUR_SIMULE` | `LignesResultatsSimulation` | Guid | → simulation, taille, type, catégorie | — | index sur chaque FK | ✅ |
| — | `PartsStationnementSimulees` | Guid | → ligne conteneur | — | `(ligne, Beneficiaire)` | ✅ **au-delà du MPD** |

Les deux contraintes ajoutées interdisent des états que le MPD laissait passer : deux tranches
commençant le même jour, deux quotes-parts pour le même bénéficiaire. Amélioration, non écart.

### 9.2 Suppression du repeating group

Le document (§ 3.3, correction n° 2) désignait comme « repeating group déguisé » les colonnes
`seuil_poids_20/40` et `franchise_import_local/transit`. Vérification sur le code courant : la
migration supprime bien **13 colonnes `Parametres_*`** de `VersionsGrillesTarifaires` et les
remplace par cinq tables. Le moteur ne porte plus aucune borne en dur — vérifié par le test
« quatrième tranche ajoutée par paramétrage ».

### 9.3 Utilisation réelle

Les cinq collections sont chargées **systématiquement** avec la grille
(`VersionGrilleTarifaireRepository.AvecParametres`). Sans cela, le moteur — qui traite un
paramètre absent comme « ne s'applique pas » — calculerait des montants nuls sans erreur. Le test
`Persistance_et_relecture_grille_tarifaire_et_ses_16_lignes` vérifie explicitement la relecture
des cinq collections.

---

## 10. Normalisation — 1FN / 2FN / 3FN

| Forme | Vérification | Verdict |
|:---|:---|:---|
| 1FN | Aucune colonne multivaluée ; les repeating groups sont supprimés | ✅ |
| 2FN | Toutes les tables ont une PK mono-attribut (`Guid`) — pas de dépendance partielle possible | ✅ |
| 3FN | Aucun attribut non-clé ne dépend d'un autre attribut non-clé | ✅ |

**Contrôle de non-réintroduction des chaînes.** Recherche de colonnes persistées portant un
libellé de référentiel : aucune. Les quatre occurrences restantes de `CategorieMarchandise` +
`string` sont des **paramètres de méthodes de recherche par code**
(`ParCategorie`, `CategorieParCode`, `Creer`, un DTO de vue), jamais des colonnes.

**Dénormalisations volontaires.** `LignesResultatsSimulation` conserve les huit montants calculés
et `PartsStationnementSimulees` la ventilation. C'est bien un **instantané historique justifié** —
la reproductibilité exigée par SIM-09 et RG-SIM-12 — et non une duplication accidentelle : la
lecture (`ConsulterSimulationProformaQuery`) restitue ces montants **sans jamais recalculer**.

---

## 11. Versionnement tarifaire — RG-SIM-11 / RG-SIM-12

### 11.1 Scénario V1 → S1 → V2 → consultation de S1

| Étape | Mécanisme | Vérifié |
|:---|:---|:---|
| Grille V1 avec date d'effet | `VersionGrilleTarifaire.DateEffetUtc`, `EstActive` | ✅ |
| Simulation S1 | Fige `VersionGrilleTarifaireId`, `VersionGrilleLibelle` et les montants ligne à ligne | ✅ |
| Grille V2 activée | `ObtenirActiveADateUtcAsync` retient la plus récente à date | ✅ testé |
| Consultation de S1 | Lit `l.MontantAcconage`, `l.MontantStationnement`… **de l'instantané** | ✅ par construction |
| Signalement d'obsolescence | `EstGrilleObsolete` comparé à la grille active | ✅ testé |

**Aucun recalcul silencieux n'est possible** : `ConsulterSimulationProformaQuery` ne référence ni
`MoteurCalculProforma`, ni la grille de la simulation — seulement les référentiels, pour résoudre
des libellés.

### 11.2 Réserve de couverture

`ConsulterSimulationProformaQueryTests.ConsulterSimulation_detecte_si_la_grille_a_ete_revisee`
crée V1, une simulation, puis V2 — et vérifie les **libellés** et le drapeau d'obsolescence.
**Il ne vérifie pas que les montants restitués sont ceux de V1.** La propriété centrale de
RG-SIM-12 découle de la structure, mais n'est verrouillée par aucune assertion. Même nature de
réserve qu'en § 4.4.

### 11.3 RG-SIM-11

🟡 **PARTIEL, inchangé.** Les données sont administrables, mais **aucun écran n'expose les
référentiels tarifaires ni les paramètres de grille**. L'écran ajouté en P1-01 concerne le seuil
d'annulation, pas la tarification. Seul l'import CSV des lignes existe.

---

## 12. Analyse R-15 — **plus grave qu'annoncé**

### 12.1 Ce que fait réellement la migration

Analyse de `20260905033506_NormalisationReferentielsTarifaires` :

- **23 colonnes supprimées**, toutes porteuses de données ;
- **7 colonnes `Guid` NOT NULL ajoutées**, avec pour valeur par défaut
  `00000000-0000-0000-0000-000000000000` ;
- **8 clés étrangères créées** dans la même migration, dont 7 vers les tables de référentiel ;
- **0 instruction de reprise de données** (`migrationBuilder.Sql` ou `InsertData`).

### 12.2 Conséquence

Sur une base contenant ne serait-ce qu'**une ligne** dans `LignesTarifaires`,
`SimulationsProforma` ou `LignesResultatsSimulation` :

1. les lignes existantes reçoivent `Guid.Empty` dans les nouvelles colonnes FK ;
2. `AddForeignKey` vers `SensOperations` / `TaillesConteneur` / `TypesConteneur` /
   `CategoriesMarchandise` s'exécute ;
3. aucune de ces tables ne contient de ligne d'identifiant `Guid.Empty` — elles sont créées
   **vides** par cette même migration et peuplées seulement au démarrage applicatif par
   `ProvisionnementReferentielsTarifaires` ;
4. **la création de la clé étrangère échoue** et la migration est annulée.

**La migration n'est donc pas « risquée » sur une base peuplée : elle est inapplicable.** C'est
le moins mauvais des deux comportements — elle échoue bruyamment plutôt que de corrompre — mais
cela invalide la formulation de mon rapport 3FN, qui parlait de « perte de données ». Il n'y aura
pas de perte : il y aura un blocage.

**L'ordonnancement est également incorrect** pour une reprise : les référentiels doivent exister
*avant* que les colonnes FK puissent être renseignées, or ils sont peuplés après la migration.

### 12.3 Cartographie de reprise

| Ancienne structure | Nouvelle structure | Transformation | Risque | Contrôle nécessaire |
|:---|:---|:---|:---|:---|
| `LignesTarifaires.Sens` (int, enum) | `SensOperationId` (FK) | 1→4 : `ImportLocal`→`IMPORT_LOCAL`, etc. | Faible — correspondance totale | Aucun `Sens` hors des 4 valeurs |
| `LignesTarifaires.Taille` (int : 20/40) | `TailleConteneurId` (FK) | `20`→code `20`, `40`→code `40` | Faible | Aucune autre valeur |
| `LignesTarifaires.CategorieMarchandise` (nvarchar 64, **libre**) | `CategorieMarchandiseId` (FK vers `(code, sens)`) | Appariement par code **et** par sens | **Élevé** — texte libre : casse, espaces, fautes de frappe, catégories inexistantes | Inventaire préalable des valeurs distinctes ; création des catégories manquantes ; **échec si une valeur ne s'apparie pas** |
| `SimulationsProforma.Sens` | `SensOperationId` | idem sens | Faible | idem |
| `LignesResultatsSimulation.Taille` / `Type` / `CategorieMarchandise` | 3 FK | idem, sur données **historiques** | **Élevé** — une simulation archivée peut porter une catégorie jamais référencée | Créer les catégories historiques même désactivées, pour ne pas perdre l'instantané |
| `LignesResultatsSimulation.PartCit/Oic/Paa` | `PartsStationnementSimulees` (lignes) | 3 colonnes → 3 lignes, `Beneficiaire` = `CIT`/`OIC`/`PAA` | Moyen | Somme des parts inchangée ; ne pas créer de ligne si montant nul et stationnement nul |
| `VersionsGrillesTarifaires.Parametres_TauxMajoration*` (3 col.) | `ParametresMajoration` (3 lignes) | Colonne → ligne typée | Faible | 3 lignes par grille |
| `Parametres_SeuilPoidsLourd20/40Pieds` | `SeuilsPoidsMajoration` (2 lignes) | Colonne → ligne + FK taille | Faible | 2 lignes par grille |
| `Parametres_TarifStationnementTranche1/2/3Jour` | `TranchesStationnement` (3 lignes) | Colonne → ligne **avec bornes** `1-5`, `6-15`, `16-∞` | **Moyen** — les bornes n'existaient nulle part : elles sont **reconstituées** depuis le code du moteur d'origine | Vérifier par test de parité que le découpage reproduit l'ancien calcul |
| `Parametres_FranchiseStationnementImportLocal/Transit` | `FranchisesStationnement` (2 lignes) | Colonne → ligne + FK sens | **Moyen** — l'export n'avait aucune colonne : cette absence signifiait « pas de stationnement » et doit rester une **absence de ligne** | Vérifier qu'aucune franchise n'est créée pour l'export |
| `Parametres_QuotePartCit/Oic/Paa` | `QuotesPartsBeneficiaire` (3 lignes) | Colonne → ligne | Faible | Somme = 1 |
| `Parametres_FranchiseBranchementJours` | `VersionsGrillesTarifaires.FranchiseBranchementJours` | Colonne → colonne | Nul | Valeur identique |

Deux points appellent une vigilance particulière : la **catégorie de marchandise**, seul champ
autrefois en texte libre, et les **bornes de tranches**, qui n'existaient pas en base et sont
déduites du code d'origine.

---

## 13. Propriétés exigées du futur script R-15

Le script n'est pas écrit — c'est hors du périmètre de cette phase. Voici ce qu'il devra garantir.

| Propriété | Exigence |
|:---|:---|
| Transactionnel | Une seule transaction englobant l'intégralité de la reprise ; aucun état intermédiaire visible |
| Ordonnancement | Peupler les **référentiels d'abord**, apparier ensuite, créer les FK en dernier |
| Idempotent | Une seconde exécution ne duplique ni référentiel ni ligne de paramètre |
| Vérifiable | Rapport avant/après horodaté, conservé |
| Reproductible | Même entrée → même sortie ; aucun `NEWID()` non déterministe sur les appariements |
| Journalisé | Chaque appariement de catégorie consigné (valeur d'origine → identifiant retenu) |
| Annulable | Sauvegarde complète préalable ; script `Down` testé |
| Testé sur copie | Exécution sur restauration de la base réelle avant toute application |
| **Échec bruyant** | Toute valeur non appariable **arrête** la reprise ; jamais de repli sur une valeur par défaut |

### Contrôles AVANT / APRÈS

| Contrôle | Avant | Après | Critère |
|:---|:---|:---|:---|
| Nombre de lignes tarifaires | N | N | Égalité stricte |
| Nombre de simulations | M | M | Égalité stricte |
| Nombre de lignes conteneur | P | P | Égalité stricte |
| Somme des acconages | Σ | Σ | Égalité au franc |
| Somme des montants totaux de simulation | Σ | Σ | Égalité au franc |
| Valeurs distinctes de catégorie | K | K catégories référentielles | Aucune perte |
| Orphelins FK | — | 0 | Aucun `Guid.Empty` résiduel |
| Doublons de clé (grille, sens, taille, catégorie) | 0 | 0 | Contrainte respectée |
| Franchises créées pour l'export | 0 | 0 | L'absence est une règle |
| Quotes-parts par grille | 3 | 3, somme = 1 | Invariant de l'agrégat |

---

## 14. Test de parité tarifaire nécessaire

Objectif : démontrer que, pour un même jeu d'entrée, l'ancien et le nouveau modèle produisent des
résultats identiques poste par poste.

**Protocole.** Sur une copie de la base, **avant** migration, exporter pour chaque simulation
historique les huit montants et le total. Après reprise, recalculer avec le moteur 3FN à partir de
la grille rattachée, et comparer.

| Poste | Critère |
|:---|:---|
| Acconage, relevage | Égalité stricte |
| Branchement frigorifique | Égalité stricte |
| Majorations lourd / dangereux / OOG | Égalité stricte |
| Stationnement | Égalité stricte — **poste le plus exposé**, les bornes étant reconstituées |
| Quotes-parts CIT / OIC / PAA | Égalité stricte, somme conservée |
| Total conteneur | Égalité stricte |
| Total dossier | Égalité stricte |

**Jeux de données nécessaires :** (1) les cinq cas de l'Annexe A.8, déjà couverts par les golden
masters ; (2) **l'intégralité des simulations réellement enregistrées**, qui seules exercent les
catégories en texte libre et les durées de séjour réelles ; (3) des cas aux bornes de tranches —
0, 1, 5, 6, 15, 16 jours taxables — pour éprouver le découpage reconstitué ; (4) l'export, pour
vérifier que l'absence de franchise reste une absence.

---

## 15 à 20. Analyse de priorité — **suspendue**

Conformément au § 2 de la commande : le P0 étant réouvert, l'analyse de priorité est arrêtée.

Ne sont donc **pas** produits ici : la matrice recalculée des 98 règles, le score global actualisé,
la reclassification complète P0/P1/P2/P3 et l'ordre recommandé des P2. Les produire maintenant
donnerait un chiffre immédiatement périmé — la correction du P0 réouvert modifiera au minimum
RG-CPT-04, RG-SEC-01, RG-SEC-02 et RG-SEC-04.

Les statuts recontrôlés dans cette phase, avec preuve, sont consignés aux § 4.3, 5.2, 6.1, 7 et
11.3. Ils constituent la base de la matrice à recalculer **après** fermeture du P0.

**Écarts connus à ce stade, sans ordre de priorité :**

| Réf. | Objet | Nature |
|:---|:---|:---|
| **P0-02** | Cloisonnement agent absent sur `ObtenirFicheTicketQuery` et `ObtenirFicheDossierQuery` | Sécurité — bloquant |
| P1-a | Aucun test ne verrouille le câblage de l'état engageant | Couverture |
| P1-b | Aucun test ne verrouille la reproductibilité de RG-SIM-12 | Couverture |
| P1-c | Aucun test ne couvre le cloisonnement agent sur les requêtes de fiche | Couverture |
| P2-a | Trois implémentations concurrentes du cloisonnement | Dette — cause du P0-02 |
| P2-b | RG-SIM-11 : pas d'écran d'administration des référentiels tarifaires | Paramétrage |
| P2-c | État engageant non posé sur BS/BADT, BEE, jointure IES | Complétude |
| P2-d | Connexion non consignée au journal d'audit (RG-SEC-03) | Traçabilité |
| — | Les autres P2/P3 de `docs/audit-conformite-merise.md` § 28-29 restent inchangés | — |

---

## 21. Go / No-Go migration 3FN

### 🟠 GO SOUS CONDITIONS

**Le lot 3FN lui-même est conforme** : structure, contraintes, normalisation et versionnement
répondent au document, en deux points plus strictement. Ce n'est pas le modèle qui pose problème,
c'est son application à des données existantes.

**GO sans réserve** — uniquement si les trois tables `LignesTarifaires`, `SimulationsProforma` et
`LignesResultatsSimulation` sont **vides** sur la base cible. À vérifier par requête avant toute
application, pas par présomption.

**NO-GO** — dans tous les autres cas, et ce n'est pas une précaution : la migration **échouera** à
la création des clés étrangères. Aucune donnée ne sera perdue, mais rien ne sera migré.

**Conditions à lever avant un GO sur base peuplée :**

1. Script R-15 écrit selon les propriétés du § 13, **et** exécuté avec succès sur une restauration
   de la base réelle.
2. Test de parité du § 14 vert sur l'intégralité des simulations historiques.
3. Inventaire des valeurs distinctes de `CategorieMarchandise` effectué, et catégories
   correspondantes créées dans le référentiel — y compris désactivées, pour les valeurs
   historiques.
4. Sauvegarde complète vérifiée restaurable.
5. Fenêtre d'indisponibilité arbitrée : la reprise n'est pas compatible avec une exploitation
   concurrente.

---

## 22. Conclusion

Le contre-audit atteint son objet : il a trouvé ce que l'audit initial et la remédiation P0
avaient manqué.

**Le P0 est réouvert**, et pour la même raison que le faux positif RG-TCK-11 : j'ai conclu de
l'existence d'un contrôle à son application. Deux requêtes gardent leur branche client et ignorent
entièrement la branche agent. Un compte agent sans habilitation lit n'importe quel ticket, notes
internes comprises. Mon rapport P0 affirmait ces requêtes « correctement gardées » — c'était faux,
et cette contre-revue existe précisément pour que de telles affirmations ne survivent pas.

**Les P1 tiennent**, avec une réserve commune qui mérite d'être entendue : les corrections sont
bonnes, mais **trois propriétés centrales ne sont verrouillées par aucun test** — le câblage de
l'état engageant, la reproductibilité des simulations, et le cloisonnement agent. Une règle non
testée à l'endroit où elle s'applique est une règle qui peut redevenir inerte sans que personne ne
le voie. C'est le mécanisme exact qui a produit les deux faux positifs de ce projet.

**Le lot 3FN est conforme.** Le blocage n'est pas dans le modèle, il est dans la reprise : la
migration est inapplicable à une base peuplée, et la catégorie de marchandise — seul champ
autrefois en texte libre — est le point où cette reprise peut réellement échouer.

La priorité est claire : fermer le P0 avant tout le reste, puis verrouiller par des tests ce qui
ne l'est pas, puis seulement traiter la dette de centralisation qui a rendu ce P0 possible.
