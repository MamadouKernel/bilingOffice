# Checklist Qualité de la Spécification — Feature 009 (Notifications Multicanales & Centre In-App)

> **Document** : `specs/009-notifications-multicanales/checklists/requirements.md`
> **Date** : 31 août 2026 · **Revalidée le** : 1er septembre 2026 (passe `/speckit.clarify`)
> **Portée** : vérifie la qualité et la fidélité de `spec.md` au CDC — ne vérifie **pas** l'implémentation (aucun code n'existe pour F009).

## 1. Fidélité au périmètre — COM

- [x] COM-01 reprise avec son texte CDC exact (spec.md §2)
- [x] COM-02 reprise avec son texte CDC exact (spec.md §2)
- [x] COM-03 reprise avec son texte CDC exact (spec.md §2)
- [x] COM-04 reprise avec son texte CDC exact, y compris la mention « Réaffirmation de SOC-05 » (spec.md §2)
- [x] COM-05 reprise avec son texte CDC exact (spec.md §2)
- [x] COM-06 reprise avec son texte CDC exact, y compris la relation CYC-04 (Must, mécanisme) / COM-06 (Could, canal) (spec.md §2)
- [x] **COM 6/6 présentes**, aucune omise, aucune ajoutée

## 2. Fidélité au périmètre — NOT

- [x] NOT-01 reprise avec son texte CDC exact (spec.md §2)
- [x] NOT-02 reprise avec son texte CDC exact (spec.md §2)
- [x] NOT-03 reprise avec son texte CDC exact (spec.md §2)
- [x] **NOT 3/3 présentes**
- [x] Priorités exactes vérifiées contre `docs/01-matrice-exigences.md` : 6 Must (COM-01/02/03/04, NOT-01/02), 2 Should (COM-05, NOT-03), 1 Could (COM-06)

## 3. CON — Aucune absorption

- [x] CON-01, CON-02, CON-03, CON-04 **absentes du périmètre** — aucune n'est spécifiée, listée comme livrable, ni rattachée à une User Story
- [x] Frontière explicite documentée (spec.md §23) avec les 4 CON nommées individuellement
- [x] ERR-04 (dépendance CON-01) explicitement renvoyée à la Feature 010 (spec.md §3, §15, §23)
- [x] Notification « rendez-vous confirmé » (proposition PC-17) explicitement marquée hors F009 faute d'événement source (spec.md §10, §23)
- [x] Aucune recherche guichet, prise de RDV, reprise présentiel/digital, continuité en cas d'indisponibilité ou saisie différée n'est spécifiée
- [x] **CON absorbées : 0/4**

## 4. REC-SCE-14 — Fidélité littérale

- [x] Description exacte de `docs/04-matrice-recette.md` (ligne 45) reprise **sans modification** (spec.md §18)
- [x] Liste des exigences Must couvertes reprise littéralement, **y compris CON-01·CON-02·CON-03** qui sont hors périmètre F009
- [x] Should/Could couvertes reprises littéralement (COM-05 · NOT-03)
- [x] Préconditions reprises exactement (« Canaux configurés · Comptes de test… »)
- [x] Acteurs repris exactement (« Client · Agent · Superviseur · Admin »)
- [x] **Aucune étape inventée, renumérotée ou renommée** — en particulier, aucune numérotation « 14.1 à 14.6 » n'est introduite (le CDC n'en définit aucune)
- [x] Conclusion de couverture **PARTIEL** et non « COUVERT », justifiée par les volets CON hors périmètre
- [x] NOT-01 et NOT-02 déclarées **sans couverture recette** (« Aucun | Absente », lignes 201-202 de la matrice) et assorties d'un **TEST-SPEC obligatoire**
- [x] COM-03 et COM-04 déclarées en couverture **Partielle**, conformément à la matrice source
- [x] COM-06 déclarée sans couverture REC-SCE
- [x] **ERR-02 également déclarée « Aucun | Absente »** (ligne 230 de la matrice) et assortie d'un TEST-SPEC — constat ajouté par la passe de clarification, absent de la version antérieure (spec.md §18)
- [x] **Matrice de répartition F009 / F010 produite** (spec.md §18.1), volet par volet, en citant littéralement les segments du scénario — **aucune étape numérotée inventée**
- [x] Verdict **PARTIEL** confirmé après clarification : 7 volets sur 10 relèvent de F009, 2 strictement de F010, 1 déjà acquis

## 5. Canaux

- [x] Matrice des canaux produite avec les colonnes demandées (spec.md §9)
- [x] E-mail : Must, conforme à `docs/00-analyse-cdc.md` §6.4
- [x] Portail In-App : Must, conforme à §6.4
- [x] SMS et WhatsApp : traités comme **« et/ou »** — capacité mobile Must, choix du canal non imposé
- [x] **Push mobile non ajouté** (absent du CDC)
- [x] Aucun autre canal inventé (courrier, voix, fax, webhook)

## 6. Fournisseurs — Aucun choix technique

- [x] **Aucun fournisseur e-mail nommé** (ni SMTP, ni SendGrid, ni Azure, ni relais d'entreprise)
- [x] **Aucun fournisseur SMS nommé** (ni Twilio, ni opérateur local, ni agrégateur)
- [x] **Aucun fournisseur WhatsApp nommé** (ni API officielle, ni intermédiaire)
- [x] Mention explicite « À DÉCIDER AVANT IMPLEMENTATION/PRODUCTION » pour les trois canaux externes (spec.md §8)
- [x] PC-16 réexaminé contre le CDC et sa conclusion vérifiée sur trois points indépendants, pas seulement reprise du preflight (spec.md §8)
- [x] **Aucun fallback SMS↔WhatsApp inventé** — l'isolation exigée par ERR-02 est distinguée d'une substitution de canal
- [x] **Niveau de blocage de PC-16 décomposé par étape** — PLAN : non · IMPLEMENTATION : non · RECETTE : partiellement · PRODUCTION : oui (spec.md §8)
- [x] Trois confirmations explicites portées à la spec : **aucun fournisseur fixé**, **aucun fallback fixé**, **aucun choix technique arrêté**

## 7. ERR-02 — Aucune stratégie inventée

- [x] Texte exact d'ERR-02 cité (spec.md §15)
- [x] Les trois obligations réellement exigées sont retenues : nouvelle tentative automatique, traçage, isolation inter-canaux
- [x] **Quatrième obligation identifiée et sourcée** : le **statut d'envoi** est un attribut exigé par le CDC §20 (*« Notification (canal, modèle, destinataire, statut d'envoi) »*) — reclassé de « déduit » à « exigé » par la passe de clarification
- [x] Tableau « ce qui est arrêté / ce qui reste paramètre / ce qui est écarté » produit (spec.md §15), sans aucune valeur chiffrée
- [x] **Aucun nombre de tentatives inventé**
- [x] **Aucun délai ni cadence inventés**
- [x] **Aucune stratégie exponentielle / backoff inventée**
- [x] **Aucune condition d'arrêt inventée**
- [x] Fallback vers un autre canal explicitement écarté (non prévu par le CDC)
- [x] Intervention humaine et message d'échec à l'utilisateur explicitement non retenus (non exigés)
- [x] Points non spécifiés renvoyés à CLAR-NOT-02 plutôt que comblés

## 8. Préférences utilisateur — Périmètre CDC strict

- [x] Vérification faite que le CDC **prévoit bien** des préférences (COM-05) — elles ne sont donc ni inventées ni omises
- [x] Préférence **par canal** retenue (seule dimension nommée par COM-05)
- [x] Opt-in/opt-out global, fréquence, langue, préférence par type d'événement, plages horaires : **explicitement non inventés** (spec.md §21)
- [x] Notion de « notification obligatoire » retenue car nommée par COM-05, mais sa **liste non inventée** (CLAR-COM-03)

## 9. ACR-01 — Non dupliquée

- [x] Frontière formalisée sur 6 dimensions : nature, question posée, cycle de vie, source, emplacement, état (spec.md §11)
- [x] Statut « déjà implémentée en F003.5 » constaté, non re-livrée par F009
- [x] Interdiction explicite de créer un second moteur d'actions requises
- [x] Règle explicite : marquer une notification comme lue ne retire jamais l'action requise correspondante
- [x] ACR-01 **non comptée** comme exigence de couverture apportée par F009 (spec.md §3, §27)

## 10. Messages F001 ↔ Notifications — Non confondus

- [x] Frontière formalisée sur 6 dimensions : nature, rattachement, auteur, bidirectionnalité, état, confidentialité (spec.md §12)
- [x] Règle explicite « un seul journal de messages par ticket » (SOC-05 / COM-04)
- [x] Règle explicite : une notification peut signaler un message sans être ce message
- [x] **Aucun modèle de données technique décidé** — renvoyé à `/speckit.plan`

## 11. Sécurité et cloisonnement — Testables

- [x] Six règles de sécurité formalisées (spec.md §14)
- [x] Chaque règle dispose d'au moins un critère d'acceptation Given/When/Then (spec.md §20)
- [x] Cloisonnement par destinataire testable (autre destinataire, y compris avec identifiant connu)
- [x] Cloisonnement inter-sociétés testable
- [x] Étanchéité des notes internes testable (aucune notification client depuis une note interne)
- [x] SEC-12 testable (lien ouvert sans authentification)
- [x] Cas « lien vers objet devenu inaccessible » couvert, sans divulgation d'existence ni d'appartenance
- [x] Cas « notification déjà lue » couvert
- [x] Cas « canal indisponible » couvert avec préservation de la transaction métier

## 12. Traitements de fond — Aucune implémentation présumée

- [x] Besoins décrits fonctionnellement uniquement (spec.md §17)
- [x] **Aucun composant existant cité comme implémentation retenue** — les trois services présents dans l'arbre de travail ne sont nommés nulle part dans la spec
- [x] Statut Git non-fermé explicitement signalé ; décision de réutilisation renvoyée à `/speckit.plan` après audit

## 13. Clarifications

- [x] 9 clarifications identifiées (CLAR-COM-01→04, CLAR-NOT-01→05)
- [x] Chacune classée selon l'un des 5 niveaux imposés
- [x] Aucune décision métier prise à la place du métier là où le CDC est silencieux
- [x] PC-16 (production) et PC-17 (implémentation) rattachés à leur clarification, avec leur classement source respecté
- [x] **PC-25 (spécification) rattaché à CLAR-NOT-01**, avec sa source CDC (MSG-04 · ALE-01 · SUP-01) — rattachement absent de la version antérieure
- [x] **PC-14 rattaché à CLAR-NOT-03** pour la politique de conservation
- [x] Absence de clarification BLOQUANTE POUR PLAN justifiée par un argument, pas affirmée
- [x] **Audit détaillé des 9 clarifications produit** (spec.md §25.1) avec, pour chacune : question exacte · source CDC · options envisagées · décision retenue · ce qui reste ouvert · impact COM/NOT · impact REC-SCE-14 · statut final
- [x] **Session de clarification datée** enregistrée (spec.md §25, Session 2026-09-01), une entrée `Q: … → A: …` par clarification
- [x] Les options **écartées** sont nommées et motivées, jamais passées sous silence

## 14. Testabilité et couverture

- [x] 8 User Stories définies, chacune rattachée à des exigences précises (spec.md §19)
- [x] Chaque Must dispose d'au moins un scénario Given/When/Then
- [x] Les 7 cas exigés sont présents : nominal, non autorisé, autre société, canal indisponible, notification déjà lue, lien vers objet inaccessible, donnée interne confidentielle
- [x] Matrice de couverture des 9 exigences produite, aucune orpheline (spec.md §26)
- [x] Couverture CDC présentée en **projection** avec trois hypothèses, jamais comme acquise (spec.md §27)

## 15. Absence d'anticipation technique

- [x] Aucune table SQL, aucune classe, aucun endpoint, aucun nom de service technique
- [x] Aucune architecture de dispatch imposée
- [x] Le choix du modèle de données Messages/Notifications explicitement laissé à `/speckit.plan`

## 16. Déclencheurs métier — reconstitution sourcée (CLAR-COM-02)

- [x] Trois **niveaux de source** définis et distingués : CDC-EXIGENCE / CDC-EXEMPLE / PC-17-PROPOSITION (spec.md §10.1)
- [x] Matrice des déclencheurs produite avec les colonnes demandées — Événement · Source CDC · Niveau de source · Destinataire · Canal · COM/NOT · Obligatoire ? (spec.md §10.2)
- [x] **Aucune règle « notifier tout changement de statut »** n'est introduite ; le rejet de cette option est justifié (SOC-08 rend les statuts visibles sans créer d'obligation de notification)
- [x] Les **5 événements client de niveau CDC-EXIGENCE** dans le périmètre F009 sont identifiés : SOC-01, IMP-03, IMP-05, IMP-11, TRF-05
- [x] **SOC-01 identifiée** comme exigence globale imposant la notification « ticket créé » **et ses canaux** (portail + e-mail), tous modules — corrigeant l'affirmation antérieure selon laquelle IMP-03 était le seul texte nommant l'e-mail pour un événement
- [x] **IMP-11 identifiée** comme source de l'événement « facture disponible », précédemment classé à tort en proposition PC-17
- [x] **AVE-03 identifiée** comme CDC-EXIGENCE mais **explicitement placée hors périmètre** (Feature 007 BLOCKED)
- [x] Les exemples littéraux du CDC (§14.1 et NOT-02) sont **cités comme illustrations**, jamais convertis en règle globale (Constitution, Principe XV)
- [x] Les lignes PC-17 restent marquées **sans valeur contractuelle**
- [x] Matrice **exploitable en l'état par `/speckit.plan`** : chaque ligne porte sa source, son destinataire, son canal et son caractère obligatoire

## 17. Notifications obligatoires (CLAR-COM-03)

- [x] Grille de qualification à **4 statuts** produite : OBLIGATOIRE / OPTIONNELLE / CONFIGURABLE / NON SPÉCIFIÉE (spec.md §21.1)
- [x] Grille **appliquée à chaque événement** de la matrice §10.2
- [x] **Seuls SOC-01 et IMP-03 sont classés OBLIGATOIRE**, sur le critère vérifiable « le CDC impose l'événement **et** nomme le canal »
- [x] **Aucune liste de notifications obligatoires n'est inventée** ; la liste définitive est laissée NON SPÉCIFIÉE
- [x] Règle explicite : le caractère obligatoire est une **propriété du paramétrage** (NOT-02), jamais une propriété codée en dur d'un type d'événement
- [x] **Aucun exemple du CDC n'est transformé en règle globale** — garde-fou écrit dans la spec

## 18. Coordonnées destinataires (CLAR-COM-04)

- [x] Provenance du téléphone **constatée, pas supposée** : compte utilisateur uniquement (spec.md §22)
- [x] **Société vérifiée comme ne portant aucune coordonnée** — écartée comme source
- [x] **Absence totale de coordonnée pour le transporteur/chauffeur constatée** — conséquence explicite sur ENR-04, indépendante de PC-16
- [x] Caractère **facultatif, auto-déclaré et non vérifié** du numéro documenté comme besoin, sans le combler
- [x] **Aucun champ technique inventé** ; aucune table, aucune propriété nouvelle proposée — la conception est renvoyée à `/speckit.plan`
- [x] Niveau de blocage **décomposé par chemin** (In-App / E-mail / mobile client / mobile chauffeur / comportement en cas d'absence) plutôt que globalisé (spec.md §22.1)
- [x] Conclusion explicite : **ne bloque ni PLAN ni l'implémentation du moteur** ; borne SMS/WhatsApp à partir de la RECETTE ; bloque ENR-04 en PRODUCTION pour une raison propre

## 19. Destinataires et événements agents (CLAR-NOT-01, CLAR-NOT-04)

- [x] **MSG-04 identifiée** comme seul texte CDC nommant un destinataire agent individuel (le collaborateur mentionné) — la spec antérieure affirmait qu'*« aucune exigence ne définit les événements notifiant un agent »*
- [x] **ALE-03 / ALE-02 identifiées** comme source de la notification au superviseur, avec destinataires et canaux paramétrables
- [x] **Frontière F001 formalisée** (spec.md §13.1) : MSG-04 et ALE-01→03 sont **propriété de F001** ; F009 **ne revendique pas leur couverture**
- [x] **Option B validée et consignée** (1er septembre 2026) : F009 fournit la **capacité générique de notification adressable clients + agents** (NOT-01)
- [x] **MSG-04 effectivement raccordée** : mention `@Nom` → **notification In-App** au collaborateur mentionné ; **e-mail conditionné au paramétrage prévu par MSG-04**, jamais activé par défaut (spec.md §13.1)
- [x] **Portée du raccordement MSG-04 décomposée volet par volet** : détection de la mention **non** réimplémentée, émission In-App **livrée**, e-mail **conditionnel**, invariants MSG-02 et MSG-05 **préservés**
- [x] **ALE-03 hors périmètre de raccordement** : F009 fournit **seulement le contrat / point d'intégration** ; **aucun workflow SLA réimplémenté**, aucune notification de supervision émise dans cette feature
- [x] **MSG-04 non recomptée** dans les 9 exigences COM/NOT de F009 — total apporté inchangé à 9 (spec.md §27)
- [x] **ALE-01→03 non recomptées** dans la couverture F009 (spec.md §27)
- [x] **US9 ajoutée** (spec.md §19) et assortie de critères d'acceptation Given/When/Then (spec.md §20)
- [x] **Traçabilité du raccordement portée à la matrice de couverture** (spec.md §26) comme ligne explicitement **non comptée**, au même titre qu'ERR-02
- [x] **PC-25 rattaché** à CLAR-NOT-01, avec sa source et son classement d'origine
- [x] **Divergence de libellé du preflight `docs/13` signalée** (PC-25 y est réintitulé « Bus / Dispatcher de notifications », qualifié « Technique ») sans modifier le document source, et la source de vérité rappelée (`docs/02-points-a-clarifier.md`, Constitution Principe II)
- [x] **Moindre privilège respecté** : ni « tous les agents », ni « tous les habilités », ni « les membres d'une file » ne sont retenus comme destinataires
- [x] **Aucun broadcast global créé** — justification sourcée par le CDC §20, qui modélise un **destinataire au singulier**
- [x] Événements agents **non sourcés** explicitement écartés (ticket arrivé en file, réaffectation, nouveau message, SLA proche) avec justification : les vues temps réel du socle les couvrent, et une vue n'est pas une notification

## 20. TEST-SPEC — Scénarios de substitution

- [x] **`TEST-NOT-01` défini** (spec.md §18 bis) — 8 cas Given/When/Then, tracé à NOT-01 (Must), justifié par `04-matrice-recette.md` ligne 201
- [x] **`TEST-NOT-02` défini** — 8 cas Given/When/Then, tracé à NOT-02 (Must), justifié par la ligne 202
- [x] **`TEST-ERR-02` défini** — 6 cas Given/When/Then, tracé à ERR-02 (Must), justifié par la ligne 230
- [x] **`TEST-COM-06` défini** — 4 cas Given/When/Then, tracé à COM-06 (Could), conditionnel au maintien de l'exigence au périmètre
- [x] **`TEST-MSG-04` défini** — 8 cas Given/When/Then démontrant *« Mention `@Nom` → notification In-App du collaborateur mentionné »*, tracé à **NOT-01 (capacité F009)** et rattaché à **MSG-04 (exigence F001, non recomptée)**
- [x] `TEST-MSG-04` couvre l'**e-mail conditionnel** (cas 2), le **destinataire unique** (cas 3), l'**étanchéité client MSG-02** (cas 4), la **non-réaffectation MSG-05** (cas 5) et l'**absence de raccordement ALE-03** (cas 8)
- [x] `TEST-MSG-04` **ne réimplémente ni ne re-teste** la détection de la mention par F001
- [x] Chaque scénario porte explicitement **son exigence Must/Could de rattachement** et **la justification de son existence**
- [x] `TEST-COM-06` **n'empiète pas sur CYC-04** : il n'exerce que la dimension canal, et vérifie explicitement l'absence de second mécanisme de relance
- [x] `TEST-ERR-02` **ne fige aucune valeur** de retry — le cas 4 renvoie explicitement au paramétrage
- [x] Les TEST-SPEC sont reportés dans la **matrice de couverture** (spec.md §26) et dans le tableau des exigences sans couverture recette (spec.md §18)
- [x] **Aucun REC-SCE contractuel n'est modifié, renuméroté ni remplacé** par ces TEST-SPEC

## 21. Gate `/speckit.plan`

- [x] **COM 6/6 clarifiées suffisamment** pour concevoir
- [x] **NOT 3/3 clarifiées suffisamment** pour concevoir
- [x] **Déclencheurs définis** — matrice sourcée §10.2
- [x] **Notifications obligatoires définies** — grille de qualification §21.1
- [x] **Destinataires définis** — §13, §13.1, avec interdictions formelles
- [x] **Comportement d'échec suffisamment défini** — 4 obligations arrêtées, valeurs laissées ouvertes §15
- [x] **In-App défini** — capacités sourcées §6, exclusions explicites
- [x] **E-mail défini fonctionnellement** — Must global au niveau du canal + Must sur événement pour SOC-01/IMP-03 §7
- [x] **Mobile suffisamment borné** — capacité configurable, aucun fournisseur, aucun fallback §8, §9
- [x] **REC-SCE-14 correctement partagé F009/F010** — §18.1
- [x] **`TEST-NOT-01` et `TEST-NOT-02` définis** — §18 bis
- [x] **`TEST-MSG-04` défini** — raccordement de la mention `@Nom` (Option B), §18 bis
- [x] **Décision CLAR-NOT-01 alignée sur l'Option B validée** ; **aucune autre décision de `/speckit.clarify` modifiée**
- [x] **Aucun fournisseur inventé** — vérifié §6 de la présente checklist
- [x] **Aucune exigence CON absorbée** — vérifié §3 de la présente checklist (0/4)

## Statut global

**PASS** — 21/21 catégories vérifiées, aucune case non cochée.

La spécification a été **clarifiée le 1er septembre 2026** (`spec.md` §25, Session 2026-09-01) et est **prête pour `/speckit.plan`** — non lancé par ce document.

Les **9 clarifications sont analysées**. Chacune dispose désormais d'une décision retenue démontrable à partir du CDC **et** d'un périmètre résiduel explicitement borné. **Aucune n'est bloquante pour `/speckit.plan`** — conformément à l'esprit du processus, et non par omission : ce qui reste ouvert relève de valeurs de paramétrage, de textes de modèles et de décisions d'exploitation, jamais de la conception fonctionnelle.

**Amendement du 1er septembre 2026 — CLAR-NOT-01, Option B validée** : la décision CLAR-NOT-01 a été corrigée après validation métier. F009 fournit la capacité générique de notification adressable aux clients **et aux agents** (NOT-01) ; **MSG-04 est effectivement raccordée** (mention `@Nom` → notification In-App du collaborateur mentionné, e-mail conditionné au paramétrage MSG-04) ; **ALE-03 reste non raccordée** et ne reçoit qu'un point d'intégration, sans réimplémentation du workflow SLA de F001. **MSG-04 et ALE-01→03 restent propriété de F001 et ne sont pas recomptées** — le total apporté par F009 demeure **9 exigences**. Un test dédié `TEST-MSG-04` et une User Story US9 ont été ajoutés. **Aucune autre décision de `/speckit.clarify` n'a été modifiée.**

**Note sur la passe de clarification du 1er septembre 2026** : sept corrections de fidélité ont été apportées après vérification directe contre `docs/cdc_text.txt` — elles sont listées en fin de `spec.md` §25. Les trois plus structurantes : (1) **SOC-01** impose la notification « ticket créé » et ses canaux pour tous les modules, ce que la spec ignorait ; (2) le **statut d'envoi** d'une notification est **exigé** par le CDC §20 et non déduit d'ERR-02 ; (3) **MSG-04** et **ALE-03** sont bien des sources CDC d'événements notifiant un acteur interne, contrairement à l'affirmation antérieure — mais elles appartiennent à F001, et F009 n'en revendique pas la couverture.

**Note sur la version antérieure** : un `spec.md` produit hors session existait avant cette passe. Il a été réécrit après détection de trois erreurs factuelles vérifiées contre les sources — (1) invention d'étapes « REC-SCE-14.1 à 14.6 » absentes du CDC, (2) déclaration de NOT-01/NOT-02 en couverture recette « Directe » alors que `docs/04-matrice-recette.md` lignes 201-202 les indiquent littéralement « Aucun | **Absente** », (3) omission du fait que REC-SCE-14 couvre également CON-01/02/03, hors périmètre, rendant ce scénario nécessairement **PARTIEL** pour F009.
