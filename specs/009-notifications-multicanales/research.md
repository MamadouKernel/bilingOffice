# Phase 0 — Recherche et Décisions de Conception : Feature 009

> **Document** : `specs/009-notifications-multicanales/research.md`
> **Date** : 1er septembre 2026
> **Méthode** : audit **fichier par fichier du code réel** (F001→F008) avant toute décision, conformément au Principe IV (aucune abstraction sans besoin démontré). Chaque décision cite le code constaté, pas une hypothèse.

---

## 0. Correction d'un constat de `spec.md` §17

`spec.md` §17 énonçait : *« Des traitements de fond couvrant des besoins voisins existent dans l'arbre de travail, mais leur statut Git n'est pas celui d'une capacité fermée (fichiers non commités au moment de la rédaction). »*

**Vérification faite** (`git ls-tree -r HEAD`) : les trois services **sont présents dans `HEAD`** —
`Infrastructure/TraitementsDeFond/DistributionAutomatiqueService.cs`, `RelancesAbsenceReponseService.cs`, `SurveillanceSlaService.cs`. Ils apparaissent en `M` (modifiés par rapport à `HEAD`), **et non en `??`** (non suivis). Ce sont donc des **capacités fermées et réutilisables**.

**Conséquence** : la réserve de §17 est levée. Le patron de traitement de fond de F001 est réutilisé tel quel (D8), au lieu d'être réexaminé comme le laissait entendre la spec. **`spec.md` §17 a été rectifié sur ce seul point factuel** (1er septembre 2026) ; aucune décision fonctionnelle ni clarification n'a été modifiée à cette occasion.

**Ce que ce constat n'implique pas.** Ces trois services **ne satisfont aucune exigence COM ou NOT** — aucun n'émet de notification. Ils couvrent des besoins voisins : relances CYC-04, surveillance SLA (ALE), distribution d'affectation (AFF). Leur existence **ne réduit ni le périmètre de F009, ni le nombre d'exigences qu'il apporte**. F009 en réutilise **le patron d'hébergement** (D8), pas les capacités.

---

## D1 — Point d'entrée du moteur : `EvenementSocle`, sans modifier les 20 commandes existantes

**Constat de code.** `Domain/Audit/EvenementSocle.cs` porte ce commentaire, écrit par F001 :

> *« Événement de domaine persisté (decision D10) — consommé in-process aujourd'hui ; **la future feature Notifications (PC-25, hors périmètre ici) s'y abonnera** pour les canaux externes. Aucun canal, modèle ou notification n'est implémenté dans ce socle. »*

`IJournalisationService.PublierEvenementAsync(typeEvenement, chargeUtile)` est appelé par **21 sites** répartis sur Tickets, Messagerie, BillingImport, Enrolement, CustomerComplaint, Affectation et Supervision. Les types publiés sont : `TicketCree` (×3), `TicketPrisEnCharge`, `TicketAffecte`, `TicketEnAttenteClient`, `TicketTransfere`, `TicketCloture`, `TicketAnnule`, `TicketRouvert`, `TicketRejete`, `ComplementDemande`, `TicketValide`, `MessagePoste`, `CollaborateurMentionne`, `RelanceEmise`, `SuggestionClotureAbsenceReponse`, `TicketReclamationCloture`, `EnrolementValide`, `ConvocationDeclenchee`, `BadgeRemisEtDossierCloture`, `SlaBientotDepasse`/`SlaDepasse`.

**Décision.** Le moteur F009 **consomme le journal d'événements** plutôt que d'appeler un dispatcher depuis chaque commande. Un cas d'usage `DistribuerNotificationsUseCase` lit les `EvenementSocle` non encore distribués, résout les destinataires, applique les règles de diffusion, rend les modèles et crée les notifications.

**Rationale.**
- C'est **l'intention explicitement écrite par F001** — s'en écarter serait un écart au Principe II.
- **Zéro modification des 21 sites existants** : aucune régression possible sur F001→F008 par ce chemin. *(Décompte exact et unique pour tout le dossier : **21 sites d'appel répartis sur 19 fichiers** — certains fichiers en portent deux, et tous ne sont pas des commandes : `RelancesAbsenceReponseUseCase`, `SurveillerEcheancesUseCase` et `DistributionAutomatiqueUseCase` sont des cas d'usage. Le terme « 21 commandes » employé dans une version antérieure était impropre.)*
- Le couplage reste unidirectionnel : le socle publie, F009 s'abonne. Aucune dépendance de F001→F008 vers F009.

**Alternatives écartées.**
- *Appel direct d'un `NotificationDispatcher` depuis chaque commande* (suggestion du preflight `docs/13` §11) : impose de toucher 21 fichiers de features fermées, pour un bénéfice de latence non exigé par le CDC. **Écartée au titre du Principe IV.**
- *Événements MediatR / bus in-process* : introduit une dépendance nouvelle pour un besoin déjà couvert par une table existante. **Écartée (YAGNI).**

**Conséquence assumée et documentée.** La distribution est **asynchrone** : une notification In-App apparaît au tour de scrutation suivant, pas dans la même milliseconde que la transaction métier. Le CDC **ne fixe aucune latence de notification** (PC-02 ne couvre que la supervision temps réel) ; aucune valeur n'est donc inventée. L'intervalle est une **valeur technique configurable**, pas une règle métier.

---

## D2 — Suivi de distribution : une colonne additive sur `EvenementSocle`

**Problème.** Consommer un journal suppose de savoir où l'on s'est arrêté, et de ne jamais distribuer deux fois le même événement (RSQ-02 du preflight : *« Idempotence des notifications par événement »*).

**Décision.** Ajouter à `EvenementSocle` une **unique propriété additive nullable** : `DistribueLeUtc` (`DateTime?`, `null` par défaut). Les événements à traiter sont ceux dont `DistribueLeUtc is null`. Idempotence renforcée par un **index unique** sur `Notification (EvenementSourceId, DestinataireId)`.

**Rationale.** Additive, rétro-compatible (toutes les lignes existantes valent `null` — elles seront donc examinées une fois puis marquées, sans produire de notification si aucune règle ne s'applique), indexable, et lisible en exploitation. C'est exactement le type d'extension additive d'une entité de feature fermée déjà pratiqué par F008 (`Ticket.DefinirInformationsEnrolement`).

**Alternatives écartées.**
- *Table curseur séparée* : impose un ordre total sur des `Guid` non monotones — fragile.
- *Rejouer tout le journal à chaque tour, en s'appuyant sur le seul index unique* : coût croissant sans borne. **Écartée (Principe XIII).**

**Écart signalé (Principe XVII).** Cette colonne modifie une entité de F001. L'écart est **additif, nullable, sans effet sur aucune règle métier existante**, et documenté ici. Aucune règle du CDC n'est modifiée.

---

## D3 — Modèle : une notification par destinataire, un statut d'envoi par canal

**Constat CDC.** §20 (*Modèle de données minimal*) impose : *« Notification (**canal, modèle, destinataire, statut d'envoi**) »*.

**Tension à résoudre.** NOT-01 exige un état **Lu/Non lu par notification** — pas par canal. ERR-02 exige un **statut d'envoi isolé par canal**. Un objet plat ne peut porter les deux sans ambiguïté.

**Décision.** Deux entités :
- **`Notification`** — le fait adressé à **un** destinataire : événement source, type, titre, corps rendu, référence au ticket, horodatage, **état Lu/Non lu**.
- **`EnvoiNotification`** — une tentative de diffusion sur **un** canal : canal, **statut d'envoi**, nombre de tentatives, horodatages, motif d'échec.

**Rationale.** Le quadruplet du CDC §20 est intégralement porté : *destinataire* et *modèle* sur `Notification`, *canal* et *statut d'envoi* sur `EnvoiNotification`. NOT-01 et NOT-03 ne lisent que `Notification` ; ERR-02 n'agit que sur `EnvoiNotification`. Le canal In-App **n'est pas un `EnvoiNotification`** : l'existence même de la `Notification` **est** sa présence dans le centre — aucun état de transport n'a de sens pour un canal interne.

**Alternative écartée.** *Une seule entité avec un statut d'envoi agrégé* : rend l'isolation inter-canaux d'ERR-02 indémontrable. **Écartée.**

---

## D4 — Référentiels d'administration : patron `CategorieReclamation` (F006)

**Constat de code.** `Domain/Reclamations/CategorieReclamation.cs` est le patron maison d'un référentiel administrable : `Code` / `Libelle` / `EstActif`, fabrique `Creer` validante, `MettreAJour`, `Activer`/`Desactiver`, jamais de suppression physique.

**Décision.** Deux référentiels suivant strictement ce patron :
- **`ModeleNotification`** (COM-03) — modèle de message par **type d'événement × canal**.
- **`RegleDiffusion`** (NOT-02) — règle **événement → canaux**, portant l'indicateur `EstObligatoire` (CLAR-COM-03).

**Rationale.** Aucune structure inédite ; l'administration, l'audit et les tests réutilisent des formes déjà éprouvées. `EstObligatoire` est une **propriété de la règle**, jamais une constante codée en dur par type d'événement — conformément à `spec.md` §21.1.

---

## D5 — Raccordement MSG-04 : aucune modification de code métier requise

**Constat de code — le plus important de cette phase.** `Application/Messagerie/MentionnerCollaborateurCommand.cs` publie déjà :

```
PublierEvenementAsync("CollaborateurMentionne", "{\"ticketId\":\"…\",\"collaborateurId\":\"…\"}")
```

La charge utile **porte déjà l'identifiant du collaborateur mentionné**. Le commentaire de la commande annonce *« il reçoit une notification »* — **notification qui n'a jamais été produite**, faute de moteur.

**Décision.** Le raccordement MSG-04 exigé par la décision **Option B** (`spec.md` §13.1) se réalise **entièrement dans F009**, par :
1. un résolveur de destinataire lisant `collaborateurId` dans la charge utile ;
2. une `RegleDiffusion` semée pour `CollaborateurMentionne` → **canal In-App** ;
3. un `ModeleNotification` correspondant.

**`MentionnerCollaborateurCommand` n'est pas modifiée. `Message` n'est pas modifiée. MSG-05 (non-réaffectation) et MSG-02 (étanchéité client) sont préservés par construction** — F009 ne touche à aucun mécanisme de workflow ni de visibilité.

**Conséquence de traçabilité.** MSG-04 **reste une exigence de F001** et **n'est pas recomptée** dans les 9 exigences de F009 (`spec.md` §27). F009 en livre le seul raccordement manquant, démontré par `TEST-MSG-04`.

---

## D6 — Point d'intégration ALE-03 : déjà existant, volontairement non activé

**Constat de code.** `Application/Supervision/SurveillerEcheancesUseCase.cs` publie déjà un `EvenementSocle` par type d'alerte (`SlaBientotDepasse`, `SlaDepasse`) au moment où elle crée l'`Alerte`.

**Décision.** Le **point d'intégration réclamé par l'Option B existe donc déjà et ne demande aucun développement**. F009 livre le **contrat générique** (`IResolveurDestinataires`, table `RegleDiffusion`) qui permettra d'activer l'escalade le jour où le métier tranchera, **et ne livre rien d'autre** :
- **aucun résolveur « superviseur du périmètre » n'est implémenté** ;
- **aucune `RegleDiffusion` n'est semée** pour `SlaBientotDepasse` / `SlaDepasse` ;
- **aucune notification de supervision n'est émise par F009.**

**Rationale.** C'est la lettre de la décision validée : *« F009 fournit seulement le contrat/point d'intégration… sans réimplémenter le workflow SLA de F001 »*. ALE-01→03 restent acquises par F001 et **ne sont pas recomptées**. L'activation relève de **PC-25**.

**Vérification prévue.** `TEST-MSG-04` cas 8 démontre qu'un dépassement de SLA **ne produit aucune notification** dans cette feature.

---

## D7 — Événements métier manquants : deux ajouts additifs exigés par le CDC

**Constat de code.** `spec.md` §10.2 classe *« facture disponible »* en **CDC-EXIGENCE** (IMP-11 : *« édite la facture et la joint au ticket ; le client est notifié »*). Or `BillingImport/EditerFactureCommand.cs` et `BillingExport/EditerFactureExportCommand.cs` **consignent l'historique et l'audit mais ne publient aucun `EvenementSocle`**.

**Conséquence.** Sans correction, un Must du CDC serait inatteignable par le moteur F009 — situation interdite par le Principe XVI (*« aucune exigence Must associée n'est silencieusement ignorée »*).

**Décision.** Ajouter **une seule ligne** à chacune de ces deux commandes, strictement calquée sur le précédent `TransfererTicketCommand` (qui publie déjà `TicketTransfere` avec le commentaire *« TRF-05 : le client est notifié »*) :

```
await journalisation.PublierEvenementAsync("FactureEditee", "{\"ticketId\":\"…\"}", ct);
```

**Rationale.** Additif, sans effet de bord, aligné sur un patron déjà présent dans le socle. Aucune règle métier de F003/F004 n'est modifiée.

**Écart signalé (Principe XVII).** Cette modification touche deux features fermées (F003, F004). Elle est **additive**, **exigée par IMP-11**, et sa suite de régression existante devra être rejouée avant commit.

**Non retenu.** Aucun autre événement n'est ajouté. En particulier « preuve de paiement reçue », « paiement validé » et « documents de sortie disponibles » restent des **PC-17-PROPOSITION** (`spec.md` §10.2) : aucune exigence CDC ne les impose, donc **aucun événement n'est créé pour eux**.

---

## D8 — Traitement de fond : patron `RelancesAbsenceReponseService` (F001)

**Constat de code.** `Infrastructure/TraitementsDeFond/RelancesAbsenceReponseService.cs` établit le patron maison :
- une `BackgroundService` **ne portant aucune règle métier** (*« règle 12 »*) ;
- toute la logique testable dans un `…UseCase` de la couche Application ;
- une méthode `TraiterUneFoisAsync(ct)` **exposée séparément de la boucle temporelle**, pour être appelable depuis un test d'intégration sans attendre le minuteur ;
- un `IntervalleParDefaut` documenté comme **valeur technique**, pas comme règle métier.

**Décision.** Deux services de fond F009 suivent ce patron **à l'identique** :
- `DistributionNotificationsService` → `DistribuerNotificationsUseCase` (D1) ;
- `ReessaiEnvoisNotificationService` → `ReessayerEnvoisNotificationUseCase` (D9).

**Rationale.** Zéro forme nouvelle ; testabilité acquise par construction.

---

## D9 — ERR-02 : comportement arrêté, valeurs jamais codées en dur

**Contrainte de spécification.** `spec.md` §15 arrête **quatre obligations** (statut d'envoi par canal, traçage, nouvelle tentative automatique, isolation inter-canaux) et **interdit** d'inventer le nombre de tentatives, la cadence et la condition d'arrêt (CLAR-NOT-02).

**Décision.**
- L'isolation est structurelle : chaque `EnvoiNotification` est traité **indépendamment**, un échec ne remonte jamais en exception hors de son propre traitement, et **ne participe jamais à la transaction métier d'origine** (elle est déjà committée depuis longtemps — conséquence directe de D1).
- Les paramètres de reprise (`NombreTentativesMax`, `DelaiEntreTentatives`) sont lus **depuis la configuration**, via un enregistrement `ParametresReessaiNotification`, et **aucune valeur par défaut n'est écrite dans le code de domaine ou d'application**.
- **Aucun backoff exponentiel, aucun fallback inter-canaux, aucune alerte administrateur, aucun message d'échec au destinataire** — les quatre sont explicitement écartés par `spec.md` §15.

**Précédent à ne pas reproduire.** `RelancesAbsenceReponseService` code en dur `new ParametresRelance(3, 2 jours)` avec un commentaire de dette technique. **F009 ne reproduit pas ce raccourci** : l'absence de configuration doit être une erreur de démarrage explicite, jamais une valeur devinée. La valeur retenue en exploitation reste **à confirmer avant implémentation** (CLAR-NOT-02).

---

## D10 — Canaux : une abstraction, une implémentation réelle, une implémentation journalisée

**Contrainte.** `spec.md` §8 et la Constitution (Principe XI) interdisent de nommer ou de coder en dur un fournisseur.

**Décision.**
- **In-App** : **pas un canal de transport**. La création de la `Notification` **est** sa diffusion (D3). Aucune abstraction, aucun envoi.
- **E-mail, SMS, WhatsApp** : une seule abstraction `ICanalNotification` exposant `Canal` et `EnvoyerAsync(...)`, avec une implémentation unique **journalisée** (`CanalJournalise`) qui enregistre la tentative et son résultat **sans contacter aucun tiers**.

**Rationale.** Le comportement exigé par le CDC (émission, statut d'envoi, traçage, reprise, isolation) est **intégralement démontrable** avec cette implémentation. Le jour où un fournisseur est choisi (PC-16), il se substitue derrière l'abstraction **sans toucher au code métier**.

**Aucun fournisseur n'est nommé nulle part** : ni SMTP, ni SendGrid, ni Azure Communication Services, ni Twilio, ni API WhatsApp Business, ni agrégateur local. **Aucun fallback n'est implémenté.**

---

## D11 — Destinataires : réutilisation stricte du modèle d'habilitation existant

**Constat de code.** `Domain/Comptes/UtilisateurSociete.cs` porte un `DroitConsultation` dont la valeur par défaut est documentée *« `PropresDemandesUniquement` (moindre privilège, Principe VII) »*. `Domain/Tickets/Dossier.cs` porte `ClientId`. `Application/Securite/CloisonnementService.cs` expose déjà `ClientEstProprietaireDuTicketAsync` / `ExigerProprieteClientAsync`.

**Décision.** `IResolveurDestinataires` produit une **liste d'identifiants d'utilisateurs nommés**, jamais un groupe. Deux implémentations, et deux seulement :
- **`ResolveurClientDuDossier`** — le propriétaire du dossier, **plus** les collaborateurs actifs de sa société dont le `DroitConsultation` couvre les demandes de la société. Le droit existant est **lu**, jamais réinterprété.
- **`ResolveurCollaborateurMentionne`** — l'identifiant lu dans la charge utile de `CollaborateurMentionne` (D5).

**Écartés explicitement** : résolveur « file », résolveur « tous les agents », résolveur « tous les habilités », résolveur « superviseur » (D6). Conforme à `spec.md` §13 et à CLAR-NOT-04.

**Sécurité de lecture.** L'accès à une notification est contrôlé par **égalité stricte** entre l'utilisateur authentifié et `Notification.DestinataireId` — jamais par une règle dérivée. C'est plus restrictif que `CloisonnementService`, et c'est voulu (`spec.md` §14, règle 1).

---

## D12 — SEC-12 : lien relatif, résolu côté serveur, sans jeton

**Décision.** Une notification porte `TicketId` (référence forte), **jamais une URL absolue construite en base**. Le lien affiché ou inséré dans un message externe est **relatif** (`/tickets/{id}`), et l'accès est arbitré par les mécanismes d'authentification et d'habilitation déjà en place.

**Interdictions actives.** Aucun jeton d'accès, aucun lien signé, aucun paramètre de contournement, aucun lien direct vers un document. Un lien ouvert sans authentification conduit au contrôle d'identité — **aucune donnée du dossier n'est exposée avant** (`spec.md` §20, US3).

**Contenu externe.** Le corps rendu pour un canal externe reste limité à l'information nécessaire (`spec.md` §14, règle 6) ; **le contenu intégral émis n'est pas journalisé** (`spec.md` §16, Principe XIV).

---

## D13 — Conservation : aucune suppression, aucun archivage

**Décision.** Aucune purge, aucun archivage automatique, aucune corbeille, aucune suppression physique d'une notification. Marquer comme lu est la **seule** transition d'état offerte à l'utilisateur.

**Rationale.** Le CDC est silencieux (CLAR-NOT-03) et la Constitution interdit de combler un silence (Principe XV). Cohérent avec `spec.md` §6, qui écarte déjà l'archivage et la suppression.

---

## D14 — Interface : câblage de la cloche existante, pas de composant inédit

**Constat de code.** `Web/Components/Layout/HeaderBar.razor` contient déjà le bouton `cit-notification-btn` avec son `<span class="cit-notification-badge">` — **sans gestionnaire de clic, sans compteur, sans source de données**. Le marquage existe, le câblage n'existe pas.

**Décision.** Câbler l'existant plutôt que créer : la cloche déclenche l'ouverture du centre, le badge affiche le compteur de non-lus (NOT-03). Une page `CentreNotifications.razor` porte la liste (NOT-01). Deux pages d'administration portent les modèles et les règles (COM-03, NOT-02). Aucun `DbContext` ni repository dans le Razor, aucun GUID saisi à la main — règles UI déjà établies par F003.5→F008.

---

## Synthèse des décisions

| # | Décision | Impact code | Touche une feature fermée ? |
|:--|:---|:---|:---|
| D1 | Consommation de `EvenementSocle`, pas d'appel depuis les commandes | Nouveau cas d'usage | **Non** |
| D2 | Colonne additive `DistribueLeUtc` sur `EvenementSocle` | 1 propriété + 1 migration | **Oui — F001, additif** |
| D3 | `Notification` + `EnvoiNotification` | 2 entités nouvelles | Non |
| D4 | `ModeleNotification` + `RegleDiffusion` (patron F006) | 2 entités nouvelles | Non |
| D5 | Raccordement MSG-04 par configuration seule | **0 ligne de code métier modifiée** | **Non** |
| D6 | ALE-03 : point d'intégration existant, non activé | **0 ligne** | Non |
| D7 | Publication de `FactureEditee` (IMP-11) | 2 lignes | **Oui — F003 et F004, additif** |
| D8 | Patron de traitement de fond F001 réutilisé | 2 services | Non |
| D9 | ERR-02 : comportement arrêté, valeurs en configuration | 1 cas d'usage | Non |
| D10 | `ICanalNotification` + implémentation journalisée | 1 abstraction, 1 implémentation | Non |
| D11 | Résolveurs de destinataires nommés (2, et 2 seulement) | 1 abstraction, 2 implémentations | Non |
| D12 | SEC-12 : lien relatif, sans jeton | Contrainte de conception | Non |
| D13 | Aucune conservation/purge inventée | Absence volontaire | Non |
| D14 | Câblage de la cloche existante | 1 page + 2 pages d'administration | **Oui — `HeaderBar.razor`, additif** |

**Trois écarts additifs sur features fermées** (D2, D7, D14), tous justifiés, tous sans modification de règle métier, tous soumis au rejeu de leur suite de régression avant commit (Principe XVII).

---

## Points ouverts non résolus par cette phase — et volontairement non comblés

| Point | Statut | Traitement en conception |
|:---|:---|:---|
| **CLAR-COM-01** / PC-16 — canal mobile et fournisseur | Ouvert, **AVANT PRODUCTION** | D10 : abstraction sans fournisseur ; l'absence de souscription ne rend aucun Must inatteignable |
| **CLAR-COM-02** / PC-17 — liste initiale de paramétrage | Ouvert, **AVANT IMPLEMENTATION** | Seules les règles de niveau **CDC-EXIGENCE** sont semées ; les propositions PC-17 ne le sont pas |
| **CLAR-COM-03** — liste des notifications obligatoires | Ouvert, **AVANT IMPLEMENTATION** | D4 : `EstObligatoire` porté par la règle ; seules SOC-01 et IMP-03 sont semées à `true` |
| **CLAR-COM-04** — coordonnées, comportement si absente | Ouvert, **AVANT IMPLEMENTATION** | D3 : un canal non servable produit un `EnvoiNotification` en statut d'échec **tracé** ; la sémantique définitive reste à confirmer |
| **CLAR-NOT-01** / PC-25 — règles de notification interne | **Résolu pour F009** (Option B) ; ouvert pour les règles internes | D5 (MSG-04 raccordé) et D6 (ALE-03 non activé) |
| **CLAR-NOT-02** — valeurs de reprise | Ouvert, **AVANT IMPLEMENTATION** | D9 : configuration obligatoire, aucune valeur par défaut |
| **CLAR-NOT-05** — textes des modèles | Ouvert, **AVANT RECETTE** | D4 : modèles administrables ; aucun libellé définitif figé |
