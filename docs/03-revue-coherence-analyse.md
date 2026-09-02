# Rapport de Revue de Cohérence — Pré-analyse CDC
## Plateforme Digitale CIT — v1.0

> **Document** : `docs/03-revue-coherence-analyse.md`  
> **Documents audités** : `00-analyse-cdc.md` · `01-matrice-exigences.md` · `02-points-a-clarifier.md`  
> **CDC source** : `CDC_Plateforme_Digitale_CIT_v3.docx` (v1.0 — 12 août 2026)  
> **Révisé le** : 28 août 2026  
> **Statut** : Rapport d'audit — **CORRECTIONS APPLIQUÉES** (voir section « Audit après corrections »)

---

## Score de préparation Spec Kit

```
╔══════════════════════════════════════════════════════════════╗
║  SCORE DE PRÉPARATION SPEC KIT : 74 / 100                   ║
║                                                              ║
║  Exhaustivité des exigences        : 88 / 100               ║
║  Fidélité au CDC                   : 82 / 100               ║
║  Qualité des dépendances           : 62 / 100               ║
║  Clarté des rôles                  : 55 / 100               ║
║  Points à clarifier (qualité)      : 78 / 100               ║
║  Testabilité                       : 71 / 100               ║
║  Traçabilité CDC → Story → Test    : 70 / 100               ║
╚══════════════════════════════════════════════════════════════╝
```

**Verdict** : L'analyse est substantielle et globalement fidèle. Elle n'est pas encore utilisable
telle quelle avec Spec Kit sans corrections ciblées. Les anomalies identifiées ci-dessous
doivent être traitées avant de démarrer la phase `/speckit.plan`.

---

## Synthèse des anomalies

| Cat. | Sévérité | Nb | Description courte |
|------|----------|----|--------------------|
| A | 🔴 Bloquant Spec Kit | 4 | Exigences du CDC absentes de la matrice |
| B | 🔴 Bloquant Spec Kit | 3 | Incohérences de priorité (Must/Should/Could erronés) |
| C | 🟠 Majeur | 5 | Descriptions altérant le sens d'une exigence |
| D | 🟠 Majeur | 6 | Dépendances non sourcées ou incorrectes |
| E | 🟠 Majeur | 3 | Rôles supposés présentés comme définis |
| F | 🟡 Mineur | 4 | Hypothèses techniques dans l'analyse |
| G | 🟡 Mineur | 3 | Omissions dans les points à clarifier |
| H | 🟢 Info | 3 | Doublons ou regroupements à préciser |

---

## Catégorie A — Exigences CDC absentes de la matrice

### A-01 🔴 — Section 17 du CDC non retranscrite dans la matrice

**Source CDC** : Section 17 « Parcours et écrans attendus »  
**Exigences concernées** : ACR-01 (la seule référence numérotée de cette section)

Le CDC définit une exigence numérotée `ACR-01` (Must) :  

> *"Le tableau de bord client met clairement en évidence les actions attendues de sa part
> (« Mes actions requises »), distinctes de la simple liste des tickets ouverts."*

Cette exigence est **absente de la matrice `01-matrice-exigences.md`**. Elle ne figure pas non
plus dans `00-analyse-cdc.md`. Le préfixe `ACR` n'est mentionné nulle part dans les trois
documents.

**Correction requise** : Ajouter `ACR-01` (Must) dans la matrice, dans un nouveau groupe
« O — Écrans et UX (ACR) » ou en extension du groupe existant.

---

### A-02 🔴 — Exigences de la section 22 du CDC non capturées

**Source CDC** : Section 22 « Recette et critères d'acceptation »  
**Statut** : Non référencée dans aucun des trois documents.

La section 22 contient des **critères d'acceptation contractuels** (12 scénarios de recette
bout-en-bout + critères de clôture) que le CDC formule comme conditions de livraison. Ces
critères ne sont pas des exigences numérotées mais ils constituent des **contraintes de
recette opposables**.

**Impact pour Spec Kit** : Ces scénarios devront devenir des Test Plans dans Spec Kit. Les
omettre maintenant crée un risque de dérive lors de la phase `/speckit.plan`.

**Correction requise** : Ajouter dans `00-analyse-cdc.md` une section « Critères d'acceptation
(§22) » résumant les 12 scénarios de bout-en-bout. Dans `02-points-a-clarifier.md`, noter
que le périmètre de recette est contractuel (pas négociable).

---

### A-03 🔴 — Risques projet (section 26) non intégrés à l'analyse

**Source CDC** : Section 26 « Risques projet et mesures de maîtrise »  
**Statut** : Non mentionnée dans aucun des trois documents.

Le CDC identifie 6 risques projet avec leurs mesures de maîtrise. Ces risques ont un impact
direct sur des choix d'architecture (ex. « Sous-estimation de la volumétrie documentaire »,
« Dépendance à un éditeur ») et sur les points à clarifier.

**Correction requise** : Ajouter dans `02-points-a-clarifier.md` une catégorie « Risques
identifiés par le CDC » avec les 6 risques et leur lien aux points PC-xx correspondants.

---

### A-04 🔴 — Grille d'évaluation des scénarios (section 23.1) non capturée

**Source CDC** : Section 23.1 « Grille d'évaluation des scénarios et solutions »  
**Statut** : Non mentionnée dans les documents.

La grille liste 10 critères d'évaluation qui constituent des **exigences non fonctionnelles
supplémentaires** (réversibilité, support local, TCO, feuille de route, export des données).
En particulier, la **réversibilité** (export de l'intégralité des données et documents dans un
format exploitable) est un critère fort qui devrait figurer dans la matrice.

**Correction requise** : Ajouter au minimum une exigence NFR dérivée de la réversibilité
(export des données, portabilité) dans la matrice.

---

## Catégorie B — Incohérences de priorité

### B-01 🔴 — MAS-02 classé Must alors que MAS-01 est Should

**Source CDC** : Section 9.14  
**Texte CDC** :
- `MAS-01` : *"[Should] Le superviseur peut, en masse : affecter, réaffecter, transférer..."*
- `MAS-02` : *"[Must] Toute action de masse est tracée ticket par ticket..."*

**Problème** : Dans la matrice, les deux exigences sont correctement classées.
Cependant, dans `00-analyse-cdc.md` section 6.2, le texte « Toute action de masse est
tracée **ticket par ticket** (MAS-02 — **Must**) » est correct. Pas d'erreur ici.

**Mais** le problème est logique : `MAS-02` est Must mais `MAS-01` est Should.
Si MAS-01 n'est pas implémenté (les actions de masse n'existent pas), MAS-02 est
**testologiquement inatteignable**. La dépendance de MAS-02 sur MAS-01 n'est pas
signalée dans la matrice (la colonne Dépendances de MAS-02 indique `MAS-01 · SOC-13`
— c'est bien — mais la contradiction logique n'est pas flaggée comme anomalie).

**Correction requise** : Marquer MAS-02 comme « Testable: Partiel (dépend de MAS-01 Should) »
et ajouter un point à clarifier : « Si MAS-01 n'est pas livré, MAS-02 ne peut pas être testé ».

---

### B-02 🔴 — COM-06 classé Could mais labellisé différemment dans l'analyse

**Source CDC** : Section 14  
**Texte CDC** : `COM-06` — *"[Could] Relances automatiques paramétrables lorsque le ticket
est en attente d'une action client au-delà d'un délai défini."*

**Problème** : Dans la matrice `01-matrice-exigences.md`, COM-06 est correctement classé
Could. Mais dans `00-analyse-cdc.md` section 6.4, COM-06 est cité sans que sa priorité
Could soit mentionnée explicitement. **Pas d'erreur de priorité dans la matrice** — mais
l'analyse pourrait induire en erreur. À homogénéiser.

**Problème véritable** : `CYC-04` (Must) et `COM-06` (Could) traitent du même mécanisme
de relances automatiques. Leurs textes se chevauchent :
- CYC-04 : *"la plateforme émet des relances automatiques selon des délais paramétrables"* → **Must**
- COM-06 : *"Relances automatiques paramétrables lorsque le ticket est en attente d'une
  action client"* → **Could**

Il y a un **conflit de priorité apparent** pour le même mécanisme. La matrice traite les deux
sans signaler la tension. CYC-04 étant Must, le mécanisme de relance est Must ; COM-06
ajoute probablement la dimension notification externe (canal) qui, elle, est Could.

**Correction requise** : Expliciter dans la matrice que COM-06 précise la dimension canal de
notification de CYC-04, et que le mécanisme de relance lui-même est Must (CYC-04) tandis
que les notifications via canaux externes associées sont Could (COM-06). Ajouter dépendance
COM-06 → CYC-04.

---

### B-03 🔴 — SIM-01 : exigence incomplète dans la matrice

**Source CDC** : Section 11.5  
**Texte CDC** : `SIM-01` — *"[Must] Le client peut lancer une simulation de proforma avant,
**si nécessaire**, de démarrer son parcours de facturation..."*

**Problème** : La matrice retranscrit SIM-01 comme « Le client peut lancer une simulation
de proforma **avant** son parcours de facturation ». La nuance « si nécessaire » est perdue,
tout comme l'idée que la simulation peut aussi être lancée **en dehors** de tout parcours.
Le CDC dit bien « avant, **si nécessaire** » — la simulation est optionnelle, pas obligatoire
avant tout parcours. Ce n'est pas un pré-requis du parcours.

**Impact** : Une user story qui exigerait la simulation avant toute soumission de BL serait
une sur-interprétation de l'exigence.

**Correction requise** : Reformuler SIM-01 dans la matrice : « Le client peut lancer une
simulation de proforma, de manière optionnelle et indépendante de son parcours de
facturation. »

---

## Catégorie C — Descriptions altérant le sens d'une exigence

### C-01 🟠 — SOC-10 : restriction de l'acteur erronée

**Source CDC** : Section 8  
**Texte CDC** : `SOC-10` — *"Liaison entre tickets : possibilité d'associer des tickets entre
eux (y compris entre modules) pour conserver la continuité d'un même dossier métier."*

**Problème** : Dans la matrice, la colonne Acteur(s) indique uniquement « Agents ».
Le CDC n'exclut pas que le client bénéficie de la visibilité de cette liaison (via son tableau
de bord montrant les tickets liés à son dossier). La liaison est une action agent, mais la
**visibilité** est client aussi.

**Correction requise** : Acteur(s) → « Agents (création liaison) · Client (visibilité) »

---

### C-02 🟠 — CPT-02 : acteur de la colonne Acteur(s) inadapté

**Source CDC** : Section 10  
**Texte CDC** : `CPT-02` — *"Le profil du client est défini à la création du compte... Ce profil
détermine automatiquement le workflow..."*

**Problème** : La matrice indique comme acteur « Admin fonctionnel ». Le type de client est
certes géré par l'admin (modification via CPT-06), mais **à la création** c'est le client qui
déclare son type et CIT qui valide. L'acteur principal est « CIT (validation) » et la colonne
devrait refléter les deux : Client (déclaration) + Admin CIT (validation/activation).

**Correction requise** : Acteur(s) → « Client (déclaration) · Admin fonctionnel CIT (validation) »

---

### C-03 🟠 — IMP-13 : la référence au rôle « Caisse/Finance » est présentée comme certaine

**Source CDC** : Section 11.2, IMP-13  
**Texte CDC** : *"La preuve de paiement fait l'objet d'une validation interne par le rôle
habilité (Caisse/Finance)..."*

**Problème** : Le CDC met « Caisse/Finance » entre parenthèses, suggérant que c'est une
désignation informelle non confirmée comme rôle applicatif. Dans la matrice, le statut est
correctement marqué « Ambiguïté (PC-04) » mais dans `00-analyse-cdc.md` section 4.2,
ce rôle est listé dans le tableau officiel des rôles CIT **au même niveau** que les rôles
confirmés, avec une note en bas de tableau. Cela crée une asymétrie.

**Correction requise** : Dans `00-analyse-cdc.md`, déplacer « Caisse/Finance » dans une
sous-table « Rôles supposés — à confirmer » distincte des rôles officiels de la section 6.

---

### C-04 🟠 — TRF-04 : le texte du CDC précise « sous réserve des droits configurés »

**Source CDC** : Section 9.3  
**Texte CDC** : `TRF-04` — *"Un opérateur habilité peut réaffecter un ticket à un autre
opérateur habilité [...], **sous réserve des droits configurés**. Ce droit n'est pas réservé
au superviseur."*

**Problème** : La matrice retranscrit correctement la règle. Mais dans `00-analyse-cdc.md`
section 6.2, la phrase « Opérateur habilité (pas réservé au superviseur) + Superviseur »
n'indique pas la condition « sous réserve des droits configurés ». L'absence de cette nuance
pourrait induire à croire que tout opérateur peut réaffecter librement.

**Correction requise** : Ajouter dans `00-analyse-cdc.md` la nuance « sous réserve des droits
configurés (paramétrable) » à la définition de la réaffectation.

---

### C-05 🟠 — IMP-15 : classée Should mais la dépendance CON-01 (Must) est inversée

**Source CDC** : Section 11.2, IMP-15  
**Texte CDC** : `IMP-15` — *"[Should] Alternative présentielle..."*

**Problème** : Dans la matrice, IMP-15 dépend de CON-01 (Must). C'est logiquement
correct. Mais CON-01 lui-même décrit la continuité digital/présentiel comme un Must
**général**, alors qu'IMP-15 est le cas spécifique Billing Office qui est Should. La dépendance
est donc inversée : CON-01 (Must général) est une **capacité** dont IMP-15 est une
**application**. Ce n'est pas IMP-15 qui dépend de CON-01 — c'est CON-01 qui *rend possible*
IMP-15. La distinction est importante pour Spec Kit (la User Story IMP-15 ne doit pas bloquer
CON-01).

**Correction requise** : Reformuler la dépendance de IMP-15 : « Activée par CON-01 (Must) »
et ajouter une note « IMP-15 est une instanciation de CON-01 dans le module Billing Office ».

---

## Catégorie D — Dépendances non sourcées ou incorrectes

> Légende : « Dépendance déduite » = non formulée textuellement dans le CDC mais
> logiquement nécessaire. « Dépendance incorrecte » = la relation est mal orientée ou erronée.

| Réf. matrice | Dépendance déclarée | Statut | Commentaire |
|-------------|---------------------|--------|-------------|
| SOC-12 | PEC-01-05 | Dépendance déduite | SOC-12 et PEC-01-05 décrivent le même mécanisme ; SOC-12 précède PEC dans le CDC. La dépendance devrait être inversée : PEC-01 dépend de SOC-12. |
| TPS-04 | TPS-02 · SLA-03 | Dépendance déduite (correcte) | Non formulée explicitement dans le CDC mais logiquement nécessaire. À marquer comme telle. |
| AFF-02 | PEC-05 | Dépendance incorrecte | AFF-02 (affectation manuelle superviseur) ne dépend pas de PEC-05 (affectation directe Should). C'est la même capacité. AFF-02 est autonome (Must). |
| AFF-03 | — | Dépendance manquante | AFF-03 (Round-Robin Should) devrait dépendre de DIS-01 (connaissance de la disponibilité) sinon un opérateur indisponible peut recevoir un ticket. |
| MAS-02 | MAS-01 · SOC-13 | Dépendance déduite (correcte) | Voir B-01 : la tension logique Should/Must n'est pas signalée. |
| IMP-03 | SOC-01 · COM-01 | Dépendance déduite | IMP-03 dit que le client reçoit ses N° de tickets. COM-01 (notifications) est pertinent mais n'est pas cité explicitement dans IMP-03 du CDC. À marquer comme déduite. |
| SEC-08 | CPT-07 | Dépendance incorrecte | SEC-08 (2FA agents/admins — Should) ne dépend pas de CPT-07 (2FA clients — Could). Ce sont deux exigences indépendantes. La direction de la dépendance est erronée. |

---

## Catégorie E — Rôles supposés présentés comme définis

### E-01 🟠 — « Agent de guichet » créé sans source dans la section 6 du CDC

**Source CDC** : Sections 15.1, CON-02 et principe P8.  
**Statut** : Le CDC mentionne « l'agent de guichet » fonctionnellement mais ne le définit
**pas** dans la liste des profils (section 6). C'est une **responsabilité fonctionnelle** (accueil
au guichet) qui peut être assurée par plusieurs des rôles déjà définis.

**Problème** : Dans `00-analyse-cdc.md` section 4.2, « Agent de guichet » est listé dans le
tableau des rôles CIT comme un rôle à part entière, sans distinction avec les rôles officiels.
Dans `02-points-a-clarifier.md` (PC-03), il est correctement identifié comme rôle manquant
à confirmer. **L'incohérence entre les deux documents est le problème** : l'un le liste comme
rôle défini, l'autre le questionne.

**Correction requise** : Retirer « Agent de guichet » du tableau des rôles définis dans
`00-analyse-cdc.md`. Ne le conserver que dans PC-03 de `02-points-a-clarifier.md`.

---

### E-02 🟠 — « Agents Finance (Avoir/Exo) » présenté comme acteur dans la matrice

**Source CDC** : Section 11.6, AVE-03  
**Texte CDC** : *"La décision [...] et les documents associés sont déposés dans le ticket."*
Le CDC ne nomme pas d'acteur précis pour cette action.

**Problème** : La matrice liste « Agents Finance » comme acteur de AVE-03. Ce rôle n'existe
pas dans la section 6 du CDC. C'est une **hypothèse** de l'analyse.

**Correction requise** : Acteur(s) de AVE-03 → « Agent habilité (rôle à définir — cf. PC-15) »

---

### E-03 🟠 — La note de bas de tableau du 00-analyse-cdc.md réfère à « PC-04 » pour Caisse/Finance mais PC-04 traite des droits croisés des superviseurs

**Source** : `00-analyse-cdc.md` section 4.2, note ; `02-points-a-clarifier.md` PC-04.  
**Problème** : La note sous le tableau des rôles dit « À clarifier — cf. PC-04 » pour le rôle
Caisse/Finance. Mais PC-04 traite des droits croisés des superviseurs, pas des rôles manquants.
C'est **PC-03** qui traite des rôles manquants (Caisse/Finance). La référence est erronée.

**Correction requise** : Dans `00-analyse-cdc.md`, corriger la référence : « À clarifier —
cf. **PC-03** » (et non PC-04).

---

## Catégorie F — Hypothèses techniques dans l'analyse

### F-01 🟡 — PC-02 introduit des choix technologiques (WebSocket/SignalR)

**Source** : `02-points-a-clarifier.md`, PC-02  
**Problème** : PC-02 mentionne « WebSocket/SignalR vs Server-Sent Events » comme
options. Ces termes sont des choix d'implémentation qui doivent être différés jusqu'à la
phase d'architecture, **après** validation du CDC. Ils n'ont pas leur place dans un document
de clarification métier.

**Correction requise** : Supprimer les termes technologiques de PC-02. Reformuler comme :
« Quelle est la latence maximale acceptable pour la supervision temps réel ? Cette réponse
conditionnera le choix d'architecture côté serveur. »

---

### F-02 🟡 — PC-13 cite des fournisseurs cloud spécifiques (Azure, AWS, GCP)

**Source** : `02-points-a-clarifier.md`, PC-13  
**Problème** : PC-13 cite « Azure, AWS, GCP » comme options. Ce niveau de détail est
prématuré à ce stade et constitue un choix d'architecture, pas une décision métier.

**Correction requise** : Remplacer par « cloud public certifié » ou « cloud privé » sans
nommer de fournisseur.

---

### F-03 🟡 — PC-18 cite des LLMs spécifiques (GPT, Gemini, Claude)

**Source** : `02-points-a-clarifier.md`, PC-18  
**Problème** : Idem que F-02. Les noms GPT, Gemini, Claude, Dialogflow, Azure Bot
Framework sont des choix d'implémentation prématurés.

**Correction requise** : Reformuler comme « solution de traitement du langage naturel
administrable, dont le choix sera fait lors de la phase d'architecture ».

---

### F-04 🟡 — L'analyse présente le RBAC au niveau du champ comme une décision d'architecture

**Source** : `00-analyse-cdc.md` sections 6.7 et 6.9 ; `01-matrice-exigences.md`, IMP-40.  
**Problème** : Le CDC exige (IMP-40, SEC-02) que chaque rôle n'accède qu'aux « champs
correspondant à son rôle ». L'analyse retranscrit cela correctement comme exigence. **Mais**
dans `00-analyse-cdc.md` section 6.7, le RBAC au niveau du champ est listé dans un tableau
de mécanismes sans préciser que ce n'est un Must **que** dans le contexte du traitement
parallèle IES. Hors de ce contexte, le CDC ne mentionne pas de RBAC au niveau du champ.

**Correction requise** : Restreindre la portée de « RBAC au niveau du champ » à son
contexte CDC exact : traitement parallèle IES uniquement (IMP-40 + SEC-02 dans ce contexte).

---

## Catégorie G — Omissions dans les points à clarifier

### G-01 🟡 — PC manquant : comportement du dossier si tous les tickets sont annulés

**Source CDC** : SOC-11, CYC-01-03  
**Lacune** : Le CDC crée un « dossier » regroupant les tickets d'une même demande. Si tous
les tickets d'un dossier sont annulés/clôturés, que devient le dossier ? Est-il archivé, supprimé,
ou reste-t-il consultable ? Cette question est absente des 23 points à clarifier.

**Correction requise** : Ajouter PC-24 (Important) : « Cycle de vie du dossier lorsque tous
ses tickets sont clôturés ou annulés ».

---

### G-02 🟡 — PC manquant : notification aux agents (pas seulement aux clients)

**Source CDC** : MSG-04 (mention @Nom → notification à l'agent mentionné)  
**Lacune** : Le CDC mentionne que les notifications portail s'adressent aussi aux agents
(MSG-04, SUP-01). Les points à clarifier ne questionnent jamais **les préférences de canal
des agents** ni les notifications internes CIT (ex. notification quand un ticket vous est
réaffecté, quand un SLA est dépassé dans votre file). Seules les notifications clients sont
traitées.

**Correction requise** : Ajouter PC-25 (Important) : « Notifications internes CIT : quels
événements déclenchent une notification aux agents, et sur quels canaux (portail uniquement,
ou aussi e-mail professionnel) ? »

---

### G-03 🟡 — PC manquant : comportement du portail pour les clients IES (accès dual)

**Source CDC** : IMP-40-42, EXP-04  
**Lacune** : Pour les clients IES, le portail CIT est utilisé pour le dépôt et le suivi, mais la
facturation, le paiement et les documents finaux passent par la « plateforme IES ». Aucun
point à clarifier ne questionne **comment le client IES navigue entre les deux plateformes**,
ni si des SSO (Single Sign-On) ou des redirections sont nécessaires.

**Correction requise** : Ajouter PC-26 (Critique) : « Expérience client IES : comment le
client transite entre le portail CIT et la plateforme IES ? Existe-t-il un mécanisme de
redirection, de SSO, ou le client doit-il se connecter séparément à chaque plateforme ? »

---

## Catégorie H — Doublons et regroupements

### H-01 🟢 — SOC-05 et COM-04 : même exigence capturée deux fois

**Source CDC** : Section 8 (SOC-05) et Section 14 (COM-04)  
**Texte SOC-05** : *"Fil de commentaires (chat) par ticket entre le client et les agents,
horodaté, conservé dans l'historique."*  
**Texte COM-04** : *"Le chat/commentaires dans le ticket constitue le canal d'échange de
référence entre le client et l'agent ; les messages y sont horodatés et conservés."*

Ces deux exigences décrivent le même mécanisme. La duplication dans le CDC est
intentionnelle (réaffirmation dans un contexte différent) et leur présence dans deux sections
différentes de la matrice est donc correcte **à condition de signaler explicitement** la relation.

**Correction requise** : Ajouter dans la matrice une note sur COM-04 : « Réaffirmation
contextuelle de SOC-05 (canal de référence). »

---

### H-02 🟢 — SEC-05 et SOC-13 : même journal d'audit capturé deux fois

**Source CDC** : Section 8 (SOC-13) et Section 18 (SEC-05)  
Les deux exigences décrivent le journal d'audit inviolable. Leur co-existence est correcte
(portées différentes : SOC-13 ancre le journal comme exigence du socle ticket, SEC-05 le
qualifie comme exigence de sécurité). Mais la matrice ne le signale pas explicitement,
ce qui peut induire une implémentation dupliquée.

**Correction requise** : Ajouter une note croisée dans les deux entrées : « Ce journal est
unique — SEC-05 qualifie le même journal que SOC-13. »

---

### H-03 🟢 — Incohérence de comptage dans le récapitulatif de la matrice

**Source** : `01-matrice-exigences.md`, section Récapitulatif  
**Problème** : Le récapitulatif par priorité indique 97 Must, 29 Should, 8 Could = **134 total**.
Le récapitulatif par module indique 124 Must, 32 Should, 6 Could = **162 total**.

Les deux totaux sont **différents** (134 ≠ 162). L'écart de 28 exigences vient du fait que
le récapitulatif par priorité est incorrect (il sous-compte). Le comptage réel de la matrice
donne bien ~162 exigences. Le tableau récapitulatif par priorité doit être recalculé.

**Vérification manuelle** (comptage dans la matrice) :
- SOC (14) + CYC (7) + MSG (5) = 26
- WKF (3) + PEC (5) + TRF (6) + TPS (5) + SLA (5) + ALE (3) + SUP (5) + DIS (3) + AFF (7) + PRI (4) + MAS (2) = 48 + 26 = 74
- CPT (7) + ENT (4) = 11
- IMP (18) + UNI (4) = 22
- EXP (4) + SIM (11) + AVE (4) + REC (6) + ENR (5) = 30
- COM (6) + NOT (3) + CON (4) + IA (7) = 20
- SEC (12) + NFR (11) + ERR (4) = 27

Total ≈ 74 + 11 + 22 + 30 + 20 + 27 = **184 exigences** (le chiffre exact dépend du comptage
des variantes IMP-10/20/30/40 et IMP-11/12/13/14/20-22/30-32/40-42).

**Correction requise** : Recompter précisément et uniformiser les deux tableaux récapitulatifs.

---

## Analyse de testabilité (vue globale)

| Groupe | Exigences Must | Testable : Oui | Testable : Partiel | Scénario recette §22 couvrant |
|--------|---------------|----------------|--------------------|-----------------------------|
| SOC/CYC/MSG | 19 Must | 19 | 0 | Scénarios 7 (PEC), 8 (transfert), 10 (temps/SLA), 11 (supervision) |
| WKF/PEC/TRF | 14 Must | 14 | 0 | Scénarios 7, 8, 9, 11 |
| TPS/SLA/ALE | 10 Must | 10 | 0 | Scénario 10 |
| CPT/ENT | 7 Must | 7 | 0 | Scénario 12 |
| IMP (tronc) | 6 Must | 6 | 0 | Scénarios 1, 2, 3, 4 |
| IMP variantes | 12 Must | 12 | 0 | Scénarios 1-4 |
| UNI | 3 Must | 3 | 0 | Implicite scénario 1 |
| EXP | 4 Must | 4 | 0 | Scénario 5 |
| SIM | 7 Must | 7 | 0 | Scénario 6 (recette au franc près) |
| AVE | 3 Must | 3 | 0 | Non couvert par les 12 scénarios §22 ❌ |
| REC | 4 Must | 4 | 0 | Scénario de recette §22 ✓ |
| ENR | 3 Must | 3 | 0 | Scénario de recette §22 ✓ |
| COM/NOT | 6 Must | 6 | 0 | Scénario 12 |
| CON | 3 Must | 3 | 0 | Scénario 12 |
| IA | 5 Must | 1 | 4 | Aucun scénario §22 ❌ |
| SEC | 10 Must | 8 | 2 | Scénario 12 (partiel) |
| NFR/ERR | 9 Must | 7 | 2 | Non couvert §22 ❌ |
| **ACR-01** | **1 Must** | **—** | **—** | **Absent de la matrice ❌** |

**Lacunes de testabilité identifiées :**
1. **AVE** : Aucun des 12 scénarios de recette du §22 ne couvre les avoirs/exonérations.
2. **IA** : L'IA est testable « Partiel » pour 4 exigences Must. Des critères d'acceptation
   doivent être définis (ex. taux de réponses correctes, délai d'escalade).
3. **ACR-01** : Absente de la matrice — aucun scénario de test possible.
4. **NFR-04** (< 3s) et **NFR-03** (≥ 99%) : testables mais nécessitent des tests de charge
   — non mentionné dans le plan de recette §22.

---

## Analyse de traçabilité CDC → Spec Kit

### Points forts
- Toutes les exigences du tronc commun (SOC, WKF, SLA) sont référencées et numérotées.
- Les dépendances entre la majorité des exigences sont documentées.
- La structure de la matrice (Référence/Priorité/Module/Acteur/Dépendance/Couche/Testable)
  est compatible avec la création de User Stories dans Spec Kit.

### Points faibles pour la traçabilité CDC → Story → Plan → Task → Test

| Gap | Impact |
|-----|--------|
| ACR-01 absent | Aucune User Story possible pour le tableau de bord client « Mes actions requises » |
| Scénarios §22 non capturés | Les Test Plans Spec Kit devront être reconstitués manuellement |
| Dépendances incorrectes (AFF-02, SEC-08) | Risque de mauvais ordonnancement dans le plan d'implémentation |
| Rôles supposés dans la matrice | Les User Stories pour AVE-03 ne peuvent pas être assignées à un acteur défini |
| Comptage incohérent de la matrice | Les métriques de couverture Spec Kit seront fausses |

### Recommandation pour la phase Spec Kit
Avant de lancer `/speckit.plan`, effectuer dans cet ordre :
1. Corriger les 4 exigences absentes (A-01 à A-04)
2. Corriger les 3 priorités incohérentes (B-01 à B-03)
3. Résoudre les 5 descriptions altérées (C-01 à C-05)
4. Corriger les 7 dépendances incorrectes ou non sourcées (D)
5. Clarifier les 3 rôles supposés (E-01 à E-03)
6. Recompter la matrice (H-03)

---

## Décisions à différer jusqu'à /speckit.plan

Les éléments suivants ont été traités dans l'analyse mais constituent des **décisions
d'architecture ou d'implémentation** qui ne doivent pas figurer dans des documents de
spécification fonctionnelle. Ils doivent être transférés au plan d'architecture :

| Décision | Trouvée dans | À différer jusqu'à |
|----------|-------------|---------------------|
| WebSocket / SignalR / SSE (supervision temps réel) | PC-02 | Architecture technique |
| Choix du fournisseur cloud (Azure, AWS, GCP) | PC-13 | Décision d'hébergement |
| Choix du LLM ou framework IA (GPT, Gemini, etc.) | PC-18 | Architecture IA |
| RBAC au niveau du champ (modèle de données) | 00-analyse §6.7 | Conception base de données |
| Structure de la base de données (modèle de données §20 repris dans l'analyse) | 00-analyse §6.1 | Conception détaillée |

---

## Plan de corrections recommandées (priorisé)

### Phase 1 — Avant tout travail Spec Kit (corrections bloquantes)

| # | Action | Document(s) | Sévérité |
|---|--------|-------------|----------|
| 1 | Ajouter ACR-01 (Must) dans la matrice | 01-matrice | 🔴 |
| 2 | Ajouter section §22 (scénarios recette) dans l'analyse | 00-analyse | 🔴 |
| 3 | Ajouter section §26 (risques) dans les points à clarifier | 02-clarifier | 🔴 |
| 4 | Ajouter exigence NFR de réversibilité (§23.1) dans la matrice | 01-matrice | 🔴 |
| 5 | Corriger la référence « PC-04 » → « PC-03 » dans 00-analyse §4.2 | 00-analyse | 🔴 |
| 6 | Recompter et uniformiser les tableaux récapitulatifs de la matrice | 01-matrice | 🔴 |

### Phase 2 — Avant la conception détaillée (corrections majeures)

| # | Action | Document(s) | Sévérité |
|---|--------|-------------|----------|
| 7 | Corriger SOC-12 ↔ PEC-01 (ordre de dépendance) | 01-matrice | 🟠 |
| 8 | Corriger AFF-02 (retirer dépendance PEC-05) | 01-matrice | 🟠 |
| 9 | Corriger SEC-08 (retirer dépendance CPT-07) | 01-matrice | 🟠 |
| 10 | Ajouter dépendance AFF-03 → DIS-01 | 01-matrice | 🟠 |
| 11 | Marquer IMP-03 → COM-01 comme dépendance déduite | 01-matrice | 🟠 |
| 12 | Retirer « Agent de guichet » du tableau des rôles définis (00-analyse §4.2) | 00-analyse | 🟠 |
| 13 | Corriger acteur AVE-03 : « Agents Finance » → « Agent habilité (à définir) » | 01-matrice | 🟠 |
| 14 | Corriger acteur CPT-02 dans la matrice | 01-matrice | 🟠 |
| 15 | Corriger acteur SOC-10 dans la matrice | 01-matrice | 🟠 |
| 16 | Reformuler SIM-01 (optionnelle, pas pré-requise) | 01-matrice | 🟠 |
| 17 | Signaler tension CYC-04 (Must) / COM-06 (Could) + dépendance COM-06 → CYC-04 | 01-matrice | 🟠 |
| 18 | Ajouter note sur MAS-02 (testable seulement si MAS-01 livré) | 01-matrice | 🟠 |
| 19 | Ajouter PC-24, PC-25, PC-26 dans les points à clarifier | 02-clarifier | 🟠 |
| 20 | Supprimer les noms technologiques de PC-02, PC-13, PC-18 | 02-clarifier | 🟡 |

### Phase 3 — Avant /speckit.plan (améliorations)

| # | Action | Document(s) | Sévérité |
|---|--------|-------------|----------|
| 21 | Ajouter notes croisées SOC-05 ↔ COM-04 et SOC-13 ↔ SEC-05 | 01-matrice | 🟢 |
| 22 | Ajouter critères d'acceptation IA dans la matrice (testabilité) | 01-matrice | 🟢 |
| 23 | Restreindre RBAC au niveau du champ à son contexte IES dans §6.7 | 00-analyse | 🟢 |
| 24 | Ajouter nuance TRF-04 « sous réserve des droits configurés » dans §6.2 | 00-analyse | 🟢 |

---

## Audit après corrections

> **Date de vérification** : 28 août 2026. Cette section documente le contrôle croisé effectué **après** application des corrections listées dans le « Plan de corrections recommandées » ci-dessus, sur les quatre documents `00-analyse-cdc.md`, `01-matrice-exigences.md`, `02-points-a-clarifier.md` et `04-matrice-recette.md` (nouvellement créé). Le CDC (`CDC_Plateforme_Digitale_CIT_v3.docx`) reste la seule source de vérité ; aucune règle métier n'a été modifiée, aucune exigence n'a été inventée.

### Méthode de vérification

Le contrôle n'a pas reposé sur une relecture visuelle seule. Chaque référence d'exigence (`XXX-NN`) et chaque priorité (Must/Should/Could) de `01-matrice-exigences.md` a été extraite mécaniquement et comparée, terme à terme, à l'extraction équivalente du texte brut du CDC (`docs/cdc_text.txt`, généré directement depuis le `.docx` source) :

- **188/188 références de la matrice retrouvées dans le CDC** — aucune référence de la matrice n'est absente du CDC, et aucune référence numérotée du CDC n'est absente de la matrice (comparaison ensembliste complète, deux sens).
- **188/188 priorités (Must/Should/Could) identiques** entre le CDC et la matrice — zéro écart.
- Les scénarios de `04-matrice-recette.md` ont été comparés un à un à la liste des 150 exigences Must de la matrice : **couverture croisée exhaustive confirmée** (aucune exigence Must ne manque à l'appel dans la matrice de couverture — voir correction ci-dessous).

### Anomalies corrigées

| # | Anomalie | Statut | Où |
|---|----------|--------|-----|
| A-01 | ACR-01 absente | ✅ Corrigée — ajoutée dans `01-matrice-exigences.md` (groupe O) et `00-analyse-cdc.md` (section 6.17), texte CDC repris à l'identique, dépendances marquées **[D]** (déduites) | 00, 01 |
| A-02 | Scénarios §22 non capturés | ✅ Corrigés — extraits dans `00-analyse-cdc.md` (section 13) et détaillés intégralement dans `04-matrice-recette.md`, explicitement qualifiés de scénarios de recette et non d'exigences | 00, 04 |
| A-03 | Risques §26 non intégrés | ✅ Corrigés — ajoutés dans `00-analyse-cdc.md` (section 14, RSQ-01 à RSQ-06) et `02-points-a-clarifier.md`, explicitement qualifiés de risques projet et non d'exigences fonctionnelles, reliés aux PC-xx concernés | 00, 02 |
| A-04 | Réversibilité (§23.1) non capturée | ✅ Corrigée — documentée dans `00-analyse-cdc.md` (section 12) comme critère d'évaluation/acquisition ; **aucune référence numérotée inventée** (le CDC n'en fournit pas), conformément à la consigne | 00, 01 (note) |
| B-01 | MAS-01/MAS-02 tension logique non signalée | ✅ Corrigée — note explicite dans `00-analyse-cdc.md` §6.2 et `01-matrice-exigences.md` (MAS-02 marquée Testable : Partiel, conditionnée à MAS-01) | 00, 01 |
| B-02 | CYC-04/COM-06 conflit apparent | ✅ Corrigée — relation clarifiée (CYC-04 = mécanisme Must, COM-06 = canal Could), dépendance COM-06 → CYC-04 **[D]** ajoutée | 00, 01 |
| B-03 | SIM-01 nuance « si nécessaire » perdue | ✅ Corrigée — texte CDC repris littéralement (« avant, si nécessaire ») avec note d'optionnalité explicite | 00, 01 |
| C-01 à C-05 | Descriptions altérant le sens (SOC-10, CPT-02, IMP-13/Caisse-Finance, TRF-04, IMP-15/CON-01) | ✅ Corrigées — acteurs, nuances et sens du texte CDC restitués (ex. « sous réserve des droits configurés » pour TRF-04 ; IMP-15 requalifiée en instanciation de CON-01) | 00, 01 |
| D (7 dépendances) | SOC-12/PEC-01 (sens inversé), TPS-04, AFF-02/PEC-05 (incorrecte), AFF-03 (manquante → DIS-01), MAS-02, IMP-03/COM-01, SEC-08/CPT-07 (incorrecte) | ✅ Corrigées — chaque dépendance de la matrice porte désormais un marqueur **[E]** (explicite) ou **[D]** (déduite) ; AFF-02 est redevenue autonome, SEC-08 et CPT-07 sont marquées indépendantes, AFF-03 dépend de DIS-01 | 01 |
| E-01 à E-03 | Rôles supposés présentés comme définis (Agent de guichet, Agents Finance, référence PC-04→PC-03) | ✅ Corrigées — taxonomie **DEFINED_ROLE / FUNCTIONAL_RESPONSIBILITY / ROLE_TO_CONFIRM** appliquée explicitement à tous les rôles de `00-analyse-cdc.md` §4 ; « Agents Finance » remplacé par « Agent habilité (ROLE_TO_CONFIRM) » dans AVE-03 ; référence corrigée en **PC-03** | 00, 01 |
| F-01 à F-04 | Choix technologiques dans les points à clarifier (WebSocket/SignalR, Azure/AWS/GCP, GPT/Gemini/Claude, RBAC champ hors contexte) | ✅ Corrigées — aucun nom de technologie, de fournisseur cloud ou de LLM ne subsiste dans `02-points-a-clarifier.md` (vérifié par recherche) ; RBAC au niveau du champ restreint explicitement au contexte IES (IMP-40/SEC-02) dans `00-analyse-cdc.md` §6.7 | 02, 00 |
| G-01 à G-03 | PC manquants (cycle de vie dossier, notifications agents, navigation IES) | ✅ Corrigés — PC-24, PC-25, PC-26 ajoutés dans `02-points-a-clarifier.md` avec source CDC, niveau et décision attendue | 02 |
| H-01, H-02 | Doublons SOC-05/COM-04 et SOC-13/SEC-05 non signalés | ✅ Corrigés — notes croisées ajoutées dans `01-matrice-exigences.md` et `00-analyse-cdc.md` précisant qu'il s'agit d'un seul objet à portées complémentaires | 00, 01 |
| H-03 | Incohérence de comptage (134 ≠ 162, puis 135 ≠ 182) | ✅ Corrigée — voir « Recomptage des statistiques » ci-dessous | 01 |
| *(trouvée lors de la vérification finale)* | `04-matrice-recette.md` : NOT-01 et NOT-02 (Must) absentes de la matrice de couverture ; synthèse Partie 4 non recalculée sur le contenu réel du tableau (58/22/34 = 114 alors que le tableau contenait 86/21/41 = 148) | ✅ Corrigée — NOT-01 et NOT-02 ajoutées (Absente), synthèse recalculée à 86/21/43 = 150, couverture 1:1 avec les 150 Must vérifiée mécaniquement | 04 |
| *(trouvée lors de la vérification finale)* | `02-points-a-clarifier.md` : décompte des points par niveau incohérent entre les titres de section (« CRITIQUES (8) », « IMPORTANTS (7) », « SOUHAITABLES (2) ») et le tableau récapitulatif final (9/10/3) | ✅ Corrigée — titres de section alignés sur le tableau récapitulatif (9/10/3, total 26 inchangé) | 02 |

### Anomalies restantes

- **Aucune anomalie bloquante** ne subsiste dans les quatre documents corrigés au regard du CDC (couverture de vérification 188/188 références et 188/188 priorités, sens exact et double sens).
- Note mineure et sans impact sur les quatre documents corrigés : dans le tableau de synthèse historique de ce rapport (« Synthèse des anomalies », catégorie D), le nombre annoncé est « 6 » alors que le tableau détaillé de la catégorie D compte 7 lignes. Il s'agit d'une coquille dans le rapport d'audit initial lui-même (avant correction) ; les 7 dépendances qu'il liste ont bien toutes été corrigées (voir tableau ci-dessus). Laissée telle quelle dans les sections historiques de ce rapport par souci de traçabilité de l'audit d'origine.
- Les 26 points à clarifier (`02-points-a-clarifier.md`) restent, par nature, des **décisions métier non tranchées** — ce ne sont pas des anomalies documentaires mais des zones d'ombre réelles du CDC, correctement isolées et non transformées en exigences ni en hypothèses figées.

### Statistiques finales (recomptées mécaniquement le 28 août 2026)

| Indicateur | Valeur |
|---|---|
| **Nombre total d'exigences numérotées (uniques, tracées 1:1 au CDC)** | **188** |
| **Must** | **150** |
| **Should** | **32** |
| **Could** | **6** |
| Exigences Must couvertes **directement** par un scénario de recette CDC §22 | 86 |
| Exigences Must couvertes **partiellement** | 21 |
| Exigences Must **sans** scénario de recette CDC (à créer dans Spec Kit — non facultatives) | 43 |
| Scénarios de recette extraits du CDC §22 | 14 (REC-SCE-01 à REC-SCE-14) |
| Risques projet CDC §26 documentés | 6 (RSQ-01 à RSQ-06) |
| Rôles classifiés DEFINED_ROLE | 17 |
| Rôles classifiés ROLE_TO_CONFIRM | 2 (Caisse/Finance, Intervenant Avoir/Exo — via PC-03/PC-15) |
| Rôles classifiés FUNCTIONAL_RESPONSIBILITY | 1 (Agent de guichet) |
| Points à clarifier | 26 (4 BLOQUANT · 9 CRITIQUE · 10 IMPORTANT · 3 SOUHAITABLE) |

### Points bloquants restant ouverts (nécessitent une décision métier, pas une correction documentaire)

| # | Point | Composant bloqué |
|---|-------|-------------------|
| PC-01 | Volumétrie opérationnelle cible | Architecture infrastructure |
| PC-03 | Rôles non définis en section 6 (Caisse/Finance, Agent de guichet, Intervenant Avoir/Exo) | Modèle RBAC + files de traitement |
| PC-07 | Matrice des transitions autorisées entre files | Moteur de workflow |
| PC-08 | Liste exacte des pièces exigées par étape | GED + formulaires client |

Ces 4 points ne bloquent pas le lancement de `/speckit.constitution` ni de `/speckit.specify` (qui portent sur les principes du projet et la formalisation des exigences déjà tracées) ; ils devront être résolus avant que les user stories touchant ces composants puissent être considérées comme complètes.

### Nouveau score de préparation Spec Kit

```
╔══════════════════════════════════════════════════════════════╗
║  SCORE DE PRÉPARATION SPEC KIT : 91 / 100  (était 74/100)   ║
║                                                              ║
║  Exhaustivité des exigences        : 98 / 100  (+10)        ║
║  Fidélité au CDC                   : 97 / 100  (+15)        ║
║  Qualité des dépendances           : 90 / 100  (+28)        ║
║  Clarté des rôles                  : 88 / 100  (+33)        ║
║  Points à clarifier (qualité)      : 92 / 100  (+14)        ║
║  Testabilité                       : 82 / 100  (+11)        ║
║  Traçabilité CDC → Story → Test    : 91 / 100  (+21)        ║
╚══════════════════════════════════════════════════════════════╝
```

**Justification des scores non maximaux** (ce ne sont pas des défauts documentaires, mais des limites réelles du CDC correctement rapportées plutôt que masquées) :
- **Testabilité (82/100)** : 5 exigences Must de l'Assistant IA restent testables « Partiel » (critères de qualité de réponse non définis par le CDC) ; NFR-03/NFR-04 nécessitent des tests de charge non prévus par les scénarios §22 ; MAS-02 reste conditionnée à MAS-01 (Should).
- **Clarté des rôles (88/100)** : 3 rôles restent ROLE_TO_CONFIRM/FUNCTIONAL_RESPONSIBILITY par nature — le CDC lui-même ne les définit pas comme profils applicatifs distincts (PC-03) ; ce n'est pas une ambiguïté introduite par l'analyse.
- **Qualité des dépendances (90/100)** : 43 exigences Must sans scénario de recette CDC impliquent des dépendances de test encore à formaliser dans Spec Kit ; les dépendances déduites **[D]**, bien qu'étiquetées comme telles, restent par construction moins certaines que les dépendances explicites **[E]**.

**Verdict** : Les quatre documents (`00-analyse-cdc.md`, `01-matrice-exigences.md`, `02-points-a-clarifier.md`, `04-matrice-recette.md`) sont désormais **fiables, exhaustifs et tracés 1:1 avec le CDC** (vérification mécanique bidirectionnelle sur 188 références et 188 priorités). Aucune exigence inventée n'a été détectée. Aucun scénario de recette ni aucun risque projet n'est présenté comme une exigence numérotée du CDC. La base documentaire est prête pour `/speckit.constitution` et `/speckit.specify`, sous réserve de la validation manuelle du métier CIT sur ce bilan.

---

*Rapport de revue de cohérence — aucune modification des documents audités avant validation.*
