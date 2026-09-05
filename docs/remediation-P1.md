# Remédiation P1 — Bloquants métier

> **Document** : `docs/remediation-P1.md`
> **Établi le** : 2026-09-05
> **Origine** : `docs/audit-conformite-merise.md` § 27
> **Prérequis** : `docs/remediation-P0-securite.md` (P0 fermé et vérifié)
> **Périmètre** : P1 uniquement. Aucune P2 ni P3 n'a été traitée.

---

## Résumé exécutif

Les trois P1 sont corrigés. La suite complète passe : **1 268 tests, 0 échec**, dont les
**12 tests de sécurité P0** rejoués sans régression.

Un constat s'impose avant tout le reste : **mon audit sous-estimait P1-01**. Il décrivait la garde
d'annulation comme « désactivée depuis l'interface », laissant entendre qu'une protection
d'affichage subsistait. La réalité est plus grave : `Ticket.MarquerEtatEngageant()` n'avait
**aucun appelant en production** — uniquement un test unitaire. Le drapeau `EstEnEtatEngageant`
valait donc `false` en toutes circonstances, ce qui rendait inopérants **à la fois** la fermeture
de l'annulation directe (RG-TCK-07) **et** la garde sur la suggestion de clôture pour absence de
réponse (RG-TCK-11, que j'avais classée ✅ CONFORME dans l'audit). Un client pouvait annuler un
ticket après édition de sa facture ou validation de son paiement. La correction ci-dessous traite
les deux.

---

## P1 initiaux

| ID | Règles | Statut à l'audit | Statut après |
|:---|:---|:---|:---|
| P1-01 | RG-TCK-07 (CYC-01, CYC-02) | 🔴 Non conforme | ✅ Corrigé |
| P1-02 | RG-COM-01, RG-WKF-10 (COM-01, COM-02, TRF-05) | 🔴 Non conforme | 🟡 Écart de code corrigé ; envoi réel suspendu à PC-16 |
| P1-03 | RG-IA-01, RG-IA-02 (IA-01, IA-02, IA-03, IA-06) | ⚫ Non implémenté | ✅ Architecture livrée, adaptateur simulé (option retenue) |

---

## P1-01 — Fermeture de l'annulation directe

### Exigence

RG-TCK-07 (CYC-01) : « Le client peut annuler librement un ticket tant qu'aucun traitement métier
engageant ni document officiel n'a été produit ; au-delà, l'annulation directe est fermée **selon
un seuil paramétrable par workflow**. »

### Cause racine

Trois défauts se cumulaient, et leur effet combiné était l'**absence totale de protection** :

1. **La décision n'était pas prise, elle était reçue.** `AnnulerLibrementAsync` acceptait un
   paramètre `etapeBloquanteAtteinte` que l'appelant devait calculer.
2. **L'unique appelant passait `false` en dur.** `DetailTicket.razor:235`.
3. **L'état engageant n'était jamais posé.** `MarquerEtatEngageant()` n'avait aucun appelant en
   production. Conséquence collatérale : `EstEligibleASuggestionDeCloture` (RG-TCK-11) était tout
   aussi inerte.

Aucun seuil paramétrable n'existait par ailleurs — ni table, ni écran.

### Reproduction

`AnnulationSeuilBloquantTests` (nouveau). Sur le code d'origine :

```
Échoué!  - échec : 1, réussite : 2, total : 3
  CYC_01_annulation_libre_refusee_lorsque_le_ticket_est_en_etat_engageant [FAIL]
```

Un ticket dont la facture a été éditée s'annulait sans résistance.

### Correction

**La commande décide seule**, à partir de deux sources et d'elles seules :

- **l'état engageant du ticket**, désormais réellement posé par les six commandes métier qui
  produisent un effet engageant : édition de facture (import et export), validation de preuve de
  paiement (import et export), confirmation de lettrage (import et export) ;
- **le seuil paramétrable par workflow** — nouvelle entité `EtapeBloquanteAnnulation`
  (module, type de demande optionnel, file bloquante, motif). Le seuil est franchi dès que le
  ticket a **traversé** la file déclarée, pas seulement s'il s'y trouve encore : un ticket reparti
  ailleurs a tout de même engagé CIT au passage. La traversée est lue dans `PassagesEnFile`, le
  mécanisme central déjà en place pour TPS-01.

Le paramètre est supprimé de la signature : aucun appelant ne peut plus contourner la règle,
puisque aucun appelant ne la porte. Le refus expose le **motif** configuré — refuser sans
expliquer serait incompréhensible pour le client.

Un écran d'administration accompagne le référentiel
(`Administration/EtapesBloquantesAnnulation.razor`) : sans lui, « paramétrable » serait resté une
intention, comme le relève l'audit à propos de RG-SIM-11.

### Tests ajoutés

| Test | Nature |
|:---|:---|
| `CYC_01_annulation_libre_refusee_lorsque_le_ticket_est_en_etat_engageant` | Reproduction du défaut |
| `CYC_01_annulation_libre_acceptee_tant_qu_aucun_traitement_engageant` | Cas nominal |
| `CYC_02_la_demande_d_annulation_reste_possible_au_dela_du_seuil` | Cas limite — le droit de *demander* reste ouvert |

### Tests modifiés

| Test | Pourquoi | Objectif métier |
|:---|:---|:---|
| `AnnulationTests.CYC_02_annulation_libre_refusee_au_dela_de_l_etape_bloquante` | Pilotait le booléen supprimé | **Inchangé**, et **renforcé** : le refus est désormais provoqué par un seuil réellement paramétré, non par un argument de test |
| `AnnulationTests` (3 autres) | Signature de la commande | Assertions inchangées |
| `AuditDistinctionAnnulationRejetTests` | Signature de la commande | Assertions inchangées |

Aucune assertion n'a été affaiblie pour faire passer la suite.

### Résultat

9 / 9 tests ciblés. Migration `AjoutSeuilAnnulationParWorkflow`.

---

## P1-02 — Notification du client hors portail

### Exigence

RG-COM-01 : « le client est notifié via e-mail, notification portail, et au moins un canal mobile
(WhatsApp et/ou SMS) ; **chaque notification comporte un lien direct vers le ticket** ».
RG-WKF-10 : « Le client est notifié automatiquement d'une réorientation ».

### Cause racine

La règle se décompose en deux parties de nature différente, que l'audit traitait en bloc.

**(a) Un écart de code, corrigeable immédiatement.** `DistribuerNotificationsUseCase` recopiait les
gabarits **verbatim** : `var titre = modeleInApp?.GabaritTitre ?? typeEvenement;`. Un modèle
portant « Votre ticket {numeroTicket} » restituait ce texte littéral, et **il n'existait aucun
moyen de placer un lien vers le ticket dans un e-mail, un SMS ou un message WhatsApp**. Le lien
n'existait que dans le centre de notifications du portail.

**(b) Un arbitrage fournisseur, hors du code.** Email, SMS et WhatsApp sont servis par
`CanalJournalise`, qui journalise sans envoyer (PC-16 non arbitré).

### Correction

**(a)** `GabaritNotification` (domaine, pur) interpole cinq jetons — `{numeroTicket}`,
`{lienTicket}`, `{module}`, `{typeDemande}`, `{evenement}` — et **garantit** le lien : un gabarit
qui l'oublie se le voit ajouter. La règle ne peut pas dépendre du fait qu'un administrateur ait
pensé à écrire un jeton. Un jeton mal orthographié est laissé visible plutôt que vidé : masquer
l'erreur à celui-là même qui peut la corriger serait pire que l'afficher.

Le lien absolu est produit par un port `ILienPortail`, alimenté par `Portail:UrlBase`. Sans
configuration, le lien reste relatif — exact dans le portail, visiblement incomplet dans un
e-mail, ce qui se corrige par configuration. Une adresse devinée aurait produit des liens
silencieusement erronés.

**(b)** Non traité : c'est une décision, pas un développement. L'architecture reste prête —
`ICanalNotification`, règles de diffusion, modèles, préférences par utilisateur et file de
réessai. Y brancher un fournisseur est un changement d'enregistrement dans la composition.

### Tests ajoutés

`GabaritNotificationTests` (11) : jetons remplacés, casse insensible, jeton inconnu préservé,
gabarit vide, gabarit sans jeton, lien ajouté, lien non dupliqué, absence de ticket, corps vide.

`DistribuerNotificationsUseCaseTests` (3 nouveaux) : interpolation avec les données du ticket,
garantie du lien même si le gabarit l'omet, **non-régression** du repli sans modèle administré.

### Tests modifiés

Quatre fichiers ont reçu la nouvelle dépendance `ILienPortail` par injection de constructeur
(`ImpCert03NotificationEmailTests`, `ComCert06CanauxRelanceTests`,
`DistribuerNotificationsUseCaseTests`, `RecSce14NotificationsE2ETests`). **Aucune assertion n'a
changé.**

### Résultat

33 / 33 tests ciblés. RG-COM-01 passe de 🔴 à 🟡 : conforme en structure et en comportement
testable, l'envoi réel restant suspendu à PC-16. RG-WKF-10 suit la même dépendance.

---

## P1-03 — Assistant virtuel

### Exigence

RG-IA-01 : réponses de premier niveau en français, orientation, aide au remplissage (IA-01, IA-02).
RG-IA-02 : escalade humaine **uniquement lorsque nécessaire**, **sans perte de contexte**, le fil
restant attaché au ticket (IA-03, IA-06).

### Cause racine

Fonction totalement absente. Aucune trace d'assistant dans `src/`.

### Correction

Option retenue par le commanditaire : **câbler l'architecture, servir un adaptateur simulé**.

- **Domaine** — `ConversationAssistant` (agrégat) et `MessageAssistant`. Le fil porte son
  propriétaire, son état d'escalade et le ticket de rattachement. `Transcrire()` produit le
  contexte remis à l'opérateur. **Écart au MPD assumé** : le document ne prévoit aucune table pour
  l'IA, mais RG-IA-02 en suppose une — un fil qui ne survit pas à l'escalade perd précisément ce
  que la règle demande de conserver.
- **Ports** — `IAssistantConversationnel` (historique + question → réponse et recommandation
  d'escalade) et `IConversationAssistantRepository`. **Aucun fournisseur n'est nommé** (PC-18).
- **Adaptateur simulé** — `AssistantSimule` : huit sujets de premier niveau tirés du CDC (suivi,
  pièces, délais, simulation, annulation, réclamation, enrôlement, accueil). Hors de ces sujets,
  il **recommande l'escalade au lieu de deviner** : un assistant qui invente une réponse est plus
  nuisible qu'un assistant qui passe la main. Aucune donnée ne quitte la machine.
- **Cas d'usage** — `PoserQuestionAssistantCommand`, `EscaladerAssistantVersOperateurCommand`,
  `ConsulterConversationAssistantQuery`.
- **Écran** — `Assistant/AssistantVirtuel.razor`.

**L'escalade est une action de l'usager, jamais un effet de bord** : l'assistant *recommande*, il
ne décide pas. Elle réutilise la messagerie du ticket (RG-COM-03) plutôt que d'inventer un second
canal, et passe par `CloisonnementService` — un fil ne peut pas être rattaché au ticket d'un tiers.

**Une erreur de conception a été corrigée en cours de route.** La première version publiait la
transcription en **note interne**. La messagerie l'a refusée, à juste titre : écrire une note
interne exige `message.note_interne`, permission d'agent qu'un client n'a jamais. Le refus était
le bon comportement et il pointait une faute de raisonnement de ma part — cette transcription est
constituée des propres mots du client et des réponses qui lui ont été faites. Elle n'a rien
d'interne, et la masquer à son auteur n'aurait eu aucun sens. Elle est publiée comme message
client.

**Un défaut de persistance a également été isolé.** Un message ajouté à un fil *déjà persisté*
était marqué `Modified` au lieu d'`Added` — EF déduisant l'état d'une entité neuve de la seule
présence d'une clé, ici produite par le domaine. L'enregistrement échouait sur « update an entity
that does not exist ». Corrigé par `ValueGeneratedNever()` sur la clé. Le cas est propre à cet
agrégat : ailleurs, les enfants naissent avec leur parent.

### Tests ajoutés

`ConversationAssistantTests` (10, domaine) : ordre du fil, escalade rattachant le ticket et
transcrivant le contexte, écriture interdite après escalade, escalade sans ticket ou sans motif
refusée, message vide refusé, fil sans propriétaire refusé, idempotence, transcription vide.

`AssistantApplicationTests` (9, application) : réponse sans escalade, fil qui se poursuit,
recommandation d'escalade hors périmètre, escalade rattachant et remettant le contexte, escalade
du fil d'autrui refusée, **rattachement au ticket d'un tiers refusé (non-régression P0)**, usager
non authentifié refusé, question vide refusée, seconde escalade sans effet.

### Tests modifiés

`Imp11EtProducteursTests.LesProducteursDEvenements_SontAuNombreAttenduEtInchanges` : cette garde
d'inventaire compte les fichiers publiant un événement, précisément pour signaler tout ajout non
délibéré. **Elle a fait son office** en détectant le 26ᵉ producteur. L'inventaire attendu passe de
25 à 26, et `AssistantCommands.cs` est nommé explicitement pour que sa disparition soit à son tour
détectée. Objectif de la garde inchangé, couverture augmentée.

### Résultat

19 / 19 tests ciblés. Migration `AjoutAssistantVirtuel`.

---

## Régressions P0 vérifiées

`ApiCloisonnementTests` rejoué après chaque P1 : **12 / 12**.

| Contrôle | État |
|:---|:---|
| Cloisonnement client | ✅ |
| Cloisonnement société (dont ENT-03) | ✅ |
| Transporteur sans société | ✅ |
| Agent par file habilitée | ✅ |
| Dossier | ✅ |
| Ticket | ✅ |
| Document | ✅ (inchangé) |
| IDOR / BOLA | ✅ |

P1-03 ajoute même une couverture P0 supplémentaire : un fil d'assistant ne peut pas être rattaché
au ticket d'un autre client.

---

## Suite globale

```
Domain ............ 208 / 208
Application ....... 634 / 634
Infrastructure .... 139 / 139
Web ............... 287 / 287
TOTAL ............. 1 268 / 1 268
```

48 tests ajoutés par cette phase (1 220 → 1 268).

---

## Fichiers modifiés

**Créés — P1-01**
- `src/CIT.Plateforme.Domain/Tickets/EtapeBloquanteAnnulation.cs`
- `src/CIT.Plateforme.Application/Administration/GererEtapesBloquantesAnnulationCommand.cs`
- `src/CIT.Plateforme.Web/Components/Pages/Administration/EtapesBloquantesAnnulation.razor`
- `tests/CIT.Plateforme.Application.Tests/Tickets/AnnulationSeuilBloquantTests.cs`
- Migration `AjoutSeuilAnnulationParWorkflow`

**Créés — P1-02**
- `src/CIT.Plateforme.Domain/Notifications/GabaritNotification.cs`
- `src/CIT.Plateforme.Application/Abstractions/ILienPortail.cs`
- `src/CIT.Plateforme.Infrastructure/Notifications/LienPortailConfigure.cs`
- `tests/CIT.Plateforme.Domain.Tests/Notifications/GabaritNotificationTests.cs`
- `tests/CIT.Plateforme.Application.Tests/Support/LienPortailDeTest.cs`

**Créés — P1-03**
- `src/CIT.Plateforme.Domain/Assistant/ConversationAssistant.cs`
- `src/CIT.Plateforme.Application/Abstractions/IAssistantConversationnel.cs`
- `src/CIT.Plateforme.Application/Assistant/AssistantCommands.cs`
- `src/CIT.Plateforme.Infrastructure/Assistant/AssistantSimule.cs`
- `src/CIT.Plateforme.Web/Components/Pages/Assistant/AssistantVirtuel.razor`
- `tests/CIT.Plateforme.Domain.Tests/Assistant/ConversationAssistantTests.cs`
- `tests/CIT.Plateforme.Application.Tests/Assistant/AssistantApplicationTests.cs`
- Migration `AjoutAssistantVirtuel`

**Modifiés**
- `src/CIT.Plateforme.Application/Tickets/AnnulerTicketCommand.cs`
- `src/CIT.Plateforme.Application/Abstractions/Repositories.cs`
- `src/CIT.Plateforme.Application/Notifications/DistribuerNotificationsUseCase.cs`
- `src/CIT.Plateforme.Application/BillingImport/{EditerFacture,ValiderPreuvePaiement,ConfirmerLettrage}Command.cs`
- `src/CIT.Plateforme.Application/BillingExport/{EditerFactureExport,ValiderPreuvePaiementExport,ConfirmerLettrageExport}Command.cs`
- `src/CIT.Plateforme.Infrastructure/Persistence/{PlateformeDbContext,Repositories}.cs`
- `src/CIT.Plateforme.Infrastructure/DependencyInjection.cs`
- `src/CIT.Plateforme.Web/Components/Pages/Tickets/DetailTicket.razor`
- 7 fichiers de tests (adaptations documentées ci-dessus)

Aucun fichier du lot `[lot 3FN]` n'a été modifié : aucun P1 n'en dépendait.

---

## Risques résiduels

**R-01 — RG-COM-01 reste 🟡, et le restera jusqu'à l'arbitrage PC-16.** Aucun client ne reçoit
d'e-mail, de SMS ni de WhatsApp. Le lien direct est désormais présent dans le corps du message,
mais ce message n'est journalisé nulle part ailleurs que dans les traces applicatives.

**R-02 — L'assistant simulé n'est pas un assistant.** Il reconnaît huit sujets par mots-clés. Il
ne comprend pas une question reformulée, ne raisonne pas sur le dossier de l'usager et n'aide pas
réellement « à compléter ses dossiers » au sens de IA-02. Il rend l'architecture vérifiable ; il ne
rend pas le service. C'est la limite explicite de l'option retenue.

**R-03 — L'escalade exige que l'usager saisisse un identifiant de ticket.** L'écran demande un
GUID, ce qui n'est pas utilisable en l'état par un client. Une liste déroulante de ses tickets
serait le bon geste — c'est une amélioration d'ergonomie (P3), non un écart de règle.

**R-04 — Le seuil d'annulation est livré vide.** Tant qu'aucune étape bloquante n'est déclarée,
seule la survenue d'un état engageant ferme l'annulation directe. C'est délibéré : le CDC ne nomme
aucune étape bloquante, et en inventer une reviendrait à décider à la place du métier. La règle
est désormais **applicable** ; son paramétrage relève de la recette.

**R-05 — `EstEnEtatEngageant` est irréversible.** Aucun chemin ne le lève. C'est cohérent avec la
notion (un paiement confirmé ne se déconfirme pas), mais une annulation acceptée par un agent
laisse le ticket marqué engageant. Sans conséquence observable aujourd'hui.

---

## Éléments reportés en P2/P3

Aucune P2 ni P3 n'a été traitée. Deux points méritent d'être rappelés :

- **R-15 du plan d'audit** (script de reprise des données du lot 3FN) porte l'étiquette P1 dans le
  tableau de remédiation, mais ce n'est **pas une non-conformité** : c'est une action de
  déploiement. Elle reste ouverte et **doit précéder toute application de la migration
  `NormalisationReferentielsTarifaires`** sur une base contenant des grilles ou des simulations
  réelles.
- **P2-03** (connexion non consignée au journal d'audit) touche RG-SEC-03, cité dans la commande
  de cette phase. Il n'a **pas** été traité : c'est une P2, et les corrections P1 n'ont dégradé
  aucune consignation existante.

---

## Conclusion

Les trois P1 sont traités. Deux le sont pleinement — la fermeture de l'annulation directe et
l'architecture de l'assistant. Le troisième l'est autant que le code le permet : le lien direct
vers le ticket, qui manquait réellement, est en place ; l'envoi effectif attend une décision qui
n'appartient pas au développement.

Le point le plus important de cette phase n'est pas une correction mais un constat : **une règle
peut être écrite, testée et déclarée conforme sans jamais s'appliquer**. `MarquerEtatEngageant()`
existait, était documenté, était couvert par un test — et n'était appelé nulle part. Mon propre
audit l'avait manqué, et avait classé RG-TCK-11 conforme sur cette base. La leçon vaut au-delà de
ce P1 : un test qui pilote lui-même la condition qu'il vérifie ne prouve pas que la condition se
produise en production.
