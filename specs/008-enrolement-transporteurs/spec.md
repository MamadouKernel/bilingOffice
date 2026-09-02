# Spécification Fonctionnelle — Feature 008 (Enrôlement Transporteurs)

> **Document** : `specs/008-enrolement-transporteurs/spec.md`
> **Feature** : F008 — Enrôlement Transporteurs
> **Date** : 31 août 2026
> **Statut** : CLARIFIED (passe `/speckit.clarify` exécutée le 31 août 2026 — CLAR-ENR-01 et CLAR-ENR-04 résolues par lecture directe du CDC, aucune décision métier substituée)
> **Couverture CDC actuelle** : 131/188 — **la projection après clôture complète de F008 (136/188, ≈72,3 %) n'est pas acquise et ne doit pas être annoncée comme telle avant clôture réelle.**

---

## 1. Objectif

La Feature 008 implémente le module **Enrôlement Transporteurs**. Elle permet à un transporteur ou un chauffeur de soumettre à distance un dossier d'enrôlement (informations + pièces paramétrables), à l'Agent Gate de l'instruire (vérification, complément, validation), et trace la convocation et la remise physique du badge jusqu'à la clôture du dossier.

---

## 2. Périmètre Contractuel (Exigences CDC)

F008 couvre **exclusivement** la famille **ENR**. Aucune exigence AVE, COM, NOT, CON, IA, SEC ou NFR/ERR n'est absorbée par ce périmètre — ces familles restent dans leurs features respectives (F007 en attente métier, futures Notifications/Continuité/IA/Hardening).

### ENR-01 (Must)
- **Texte CDC** : Le chauffeur ou le transporteur renseigne à distance les informations nécessaires et dépose les pièces demandées ; la liste des informations et pièces est paramétrable.
- **Acteur(s)** : Transporteur · Chauffeur
- **Déclencheur** : Démarche d'enrôlement à distance
- **Workflow** : Création → génération d'un ticket (SOC-01)
- **Données** : Informations et pièces paramétrables (liste non fixée par le CDC — cf. CLAR-ENR-02)

### ENR-02 (Must)
- **Texte CDC** : La soumission génère un ticket transmis aux agents Gate, qui peuvent vérifier, demander des compléments ou valider.
- **Acteur(s)** : Agent Gate
- **Workflow** : Affectation file "Agent Gate". Actions possibles : Vérification, Demande de Complément, Validation.
- **Note** : le texte ne mentionne aucune action de rejet définitif (contrairement à IMP-04/REC-03) — cf. CLAR-ENR-04.

### ENR-03 (Must)
- **Texte CDC** : Le déplacement physique du chauffeur n'est requis que pour les étapes qui l'exigent (photo, badge) ; le ticket trace la convocation et la réalisation.
- **Acteur(s)** : Agent Gate (convocation) · Chauffeur (déplacement)
- **Workflow** : Dossier validé → convocation → déplacement physique (photo + remise du badge) → traçabilité de la réalisation.

### ENR-04 (Should)
- **Texte CDC** : Notification au chauffeur (SMS/WhatsApp prioritaires) à chaque étape clé : dossier validé, convocation, badge disponible.
- **Acteur(s)** : Système → Transporteur/Chauffeur
- **Déclencheur** : Changements d'état clés du dossier.

### ENR-05 (Could)
- **Texte CDC** : Gestion du renouvellement des badges avec rappel automatique avant expiration.
- **Acteur(s)** : Transporteur/Chauffeur · Agent Gate
- **Déclencheur** : Approche de la date d'expiration d'un badge.

**MUST : 3 (ENR-01, 02, 03). SHOULD : 1 (ENR-04). COULD : 1 (ENR-05).**

---

## 3. Clarifications Fonctionnelles (CLAR-ENR)

Section mise à jour lors de la passe `/speckit.clarify` du 31 août 2026. Recherche exhaustive menée sur les 5 documents sources (`00-analyse-cdc.md`, `01-matrice-exigences.md`, `02-points-a-clarifier.md`, `03-revue-coherence-analyse.md`, `04-matrice-recette.md`) pour chaque terme pertinent — aucune hypothèse n'a été transformée en règle métier sans citation exacte.

| ID | Question | Éléments CDC | Éléments REC-SCE-08 | Décision retenue | Justification | Impact sur ENR-01→05 | Statut |
|:---|:---|:---|:---|:---|:---|:---|:---|
| **CLAR-ENR-01** | Le "Transporteur" est-il (A) le chauffeur lui-même (personne physique), (B) une société avec plusieurs chauffeurs, ou (C) les deux coexistent ? | Recherche exhaustive de "Transporteur/Chauffeur/conducteur" dans les 5 documents sources : `docs/00-analyse-cdc.md` §4.1 classe explicitement l'acteur **"Transporteur/Chauffeur" — "Personne physique"** (pas "Société", catégorie réservée au Client dans le même tableau) ; §6.14 et §7.7 (deux sections indépendantes) emploient "Chauffeur/Transporteur" en alternance, et §7.7 (le flux détaillé) n'utilise **que** "Chauffeur" seul pour l'action de soumission ; ENR-04 parle des "chauffeurs" comme destinataires ; NFR-02 mentionne "clients et chauffeurs" côte à côte. **Aucune occurrence, dans les 5 documents, de "société transporteur", "entreprise de transport", ou d'une relation 1-N chauffeurs↔transporteur.** | Acteurs : "Transporteur/Chauffeur · Agent Gate" (un seul acteur nommé côté demandeur, pas deux) | **Option A retenue : Transporteur = Chauffeur = la même personne physique.** Le CDC emploie les deux mots comme synonymes d'un même individu, jamais comme deux entités distinctes. | La classification explicite "Personne physique" (§4.1) est une donnée structurée du CDC, pas une inférence — c'est la même nature de preuve que "DEFINED_ROLE" pour Agent Gate. Aucune des 5 sources ne mentionne de structure organisationnelle (société, flotte, multi-chauffeurs) associée à l'enrôlement. Conclure à une relation 1-N (option B/C) reviendrait à ajouter une donnée absente du CDC — exactement ce que l'instruction interdit ("ne pas inventer une relation 1-N"). | La spec (US1-US6, critères §21) reste valide : "Transporteur/Chauffeur" est désormais un identifiant unique d'acteur, pas une paire acteur/organisation. Aucune US ni critère ne nécessite de modification de fond — la formulation "Transporteur/Chauffeur" est déjà conforme à cette lecture. | **RÉSOLU** — décision fonctionnelle sourcée par le CDC. La représentation technique (réutilisation ou non de `F002`) est affaire de `/speckit.plan`, mais le besoin fonctionnel ne requiert **pas** de modéliser une société transporteur ni une relation chauffeurs↔société. |
| **CLAR-ENR-02** | Quel est l'objet exact de l'enrôlement — le distinguer de l'acteur qui soumet ? | ENR-01 : l'acteur qui soumet est "le chauffeur ou le transporteur" (CLAR-ENR-01 : une seule personne). ENR-03 : "le déplacement physique **du chauffeur**" et "le badge" sont associés à cette même personne. | Étape "convocation photo/badge" — la photo et le badge concernent l'individu convoqué, identique au soumissionnaire. | **L'ACTEUR qui soumet et l'OBJET enrôlé sont la même personne physique** (le chauffeur/transporteur). Il n'existe pas d'objet "dossier" enrôlé indépendamment d'une personne, ni d'objet "véhicule" enrôlé (absent du texte, cf. spec.md §6). | Direct conséquence de CLAR-ENR-01 : puisqu'il n'y a qu'un seul acteur, il n'y a qu'un seul objet enrôlé — la personne elle-même, matérialisée par un badge nominatif à l'issue du parcours. | Confirme que le "Ticket" d'enrôlement (F001, réutilisé) porte nativement l'identité du chauffeur enrôlé sans nécessiter d'entité "objet enrôlé" séparée. | **RÉSOLU** — conséquence directe de CLAR-ENR-01, exploitable en `/speckit.plan`. |
| **CLAR-ENR-03** | Le mécanisme de "demande de complément" (ENR-02) doit-il réutiliser le socle F001 (Messages + Documents + EnAttenteClient), ou en créer un dédié ? | ENR-02 : "peuvent vérifier, demander des compléments ou valider" — vocabulaire strictement identique à IMP-04 (Pilote Manifeste) et REC-03 (Support Opérationnel), tous deux déjà résolus par réutilisation pure du socle F001 (`AttenteClientCommand`, Messages, Documents) dans F003 et F006, sans aucun mécanisme spécifique créé. | Étape 2 REC-SCE-08 : "complément" — un seul mot, aucun détail supplémentaire, cohérent avec un mécanisme déjà générique. | **Option A retenue : réutilisation intégrale du mécanisme générique F001** (Messages + Documents + attente client), sans second moteur. Qui demande : Agent Gate. Qui répond : le chauffeur/transporteur (même personne, CLAR-ENR-01). Statut pendant l'attente : "en attente de complément" (générique, SLA suspendu par cohérence transverse TPS-04, déjà appliqué identiquement à IMP/REC). Reprise après réponse : retour en vérification (file Agent Gate). Documents complémentaires : via le même référentiel configurable que la soumission initiale (CLAR-ENR précédemment "PC-08", §7 spec.md). Traçabilité : Messages + Historique (SOC-02, SOC-05), aucun second journal. | Aucun élément du texte ENR-02 ne diffère structurellement de IMP-04/REC-03 — les deux précédents ont validé ce mécanisme sur SQL Server réel (F003) et en bUnit interactif (F006). Créer un second moteur reviendrait à dupliquer une capacité déjà éprouvée sans justification textuelle. | Le workflow (§5 spec.md, étape 3) est confirmé exploitable tel quel en `/speckit.plan` — aucune réécriture nécessaire. | **RÉSOLU** — décision de réutilisation directement justifiée par la cohérence textuelle avec IMP-04/REC-03, pas une supposition. |
| **CLAR-ENR-04** | ENR-02 prévoit-il un rejet définitif du dossier ? | Recherche exhaustive de "rejet"/"refus" dans tout `docs/00-analyse-cdc.md` à proximité de "Gate", "Enrôlement", "badge", "transporteur", "chauffeur" : **0 occurrence.** ENR-02 dit exactement "vérifier, demander des compléments ou valider" — trois actions, aucune quatrième. Aucune autre section du CDC (§25, autres modules) n'étend cette liste pour ENR. | REC-SCE-08 ne mentionne aucune étape de rejet dans son texte exact ("dossier à distance, complément, validation, convocation photo/badge, clôture"). | **Application de la règle par défaut de l'instruction : "Le rejet définitif n'appartient pas au périmètre fonctionnel F008 tant qu'aucune exigence du CDC ne le prévoit explicitement."** | Confirmé par recherche exhaustive et négative — pas une absence de recherche, une absence réelle constatée. Ajouter un rejet par symétrie avec IMP-04/REC-03 serait une invention explicitement proscrite par l'instruction. | US4/critères §20-21 confirmés sans rejet — aucune modification nécessaire, la spec initiale avait déjà anticipé correctement cette absence. | **RÉSOLU (par absence constatée, pas par indécision)** — F008 ne couvre aucun rejet. Si un besoin réel existe, il devra être réintroduit par une exigence CDC explicite, pas par cette spec. |
| **CLAR-ENR-05** | Que recouvre exactement le "badge" ? | Le CDC ne décrit que l'événement "Badge remis → [CLÔTURÉ]" (§6.14, §7.7, concordants) — aucune référence, numéro, technologie, ni processus de fabrication. | Étape "convocation photo/badge" puis "clôture" — confirme l'ordre, pas la technologie. | Le badge reste **exclusivement un événement tracé** (constat de remise physique par l'Agent Gate), pas un objet technique. | Inchangé depuis la version initiale de la spec — aucune source supplémentaire trouvée lors de cette passe de clarification malgré une recherche dédiée. | Aucun changement aux §12/§21. | **RÉSOLU** (confirmé, pas seulement présumé). |
| **CLAR-ENR-06** | Quel canal pour la convocation (ENR-03) et les notifications d'étapes clés (ENR-04) ? | ENR-04 nomme "SMS/WhatsApp prioritaires" ; dépend de PC-16 ("Bloque : PRODUCTION", hors périmètre F008). | Non détaillé par REC-SCE-08. | Convocation = événement du ticket, tracé et exposé pour consommation future par un module Notifications ; **aucun canal n'est implémenté par F008**. | Cohérent avec la frontière déjà tracée (§22 spec.md) et avec PC-16 qui ne bloque que la PRODUCTION, pas la spec. | Aucun changement — §11 déjà correctement borné. | **RÉSOLU** (frontière confirmée). |
| **CLAR-ENR-07** | ENR-05 (renouvellement badge) — qui fixe la date d'expiration et selon quelle durée ? | Aucune mention dans le CDC. | REC-SCE-08 ne couvre pas ENR-05 (Could, absent du scénario). | Donnée paramétrable (par analogie SLA, Admin fonctionnel), sans valeur par défaut. | ENR-05 est Could — son absence de définition fine ne bloque ni la spec ni le plan pour les 3 Must. | Aucun changement — ENR-05 reste différée sans détail chiffré, conformément à l'instruction de ne jamais inventer de durée. | **À CONFIRMER AVANT IMPLEMENTATION** (uniquement si ENR-05 est retenue au périmètre effectif — ne bloque ni SPEC ni PLAN pour les Must). |

---

## 4. Acteurs Fonctionnels

Description des **capacités fonctionnelles**, pas de rôles Identity techniques (ceux-ci seront fixés en `/speckit.plan`, à l'image de `Complaint.*` pour F006).

- **Transporteur/Chauffeur** (un seul acteur, RÉSOLU par CLAR-ENR-01 : personne physique unique — le CDC n'emploie pas ces deux mots pour désigner deux entités distinctes) : peut soumettre un dossier d'enrôlement, consulter ses propres dossiers, répondre à une demande de complément, se présente physiquement pour la photo et la remise du badge.
- **Agent Gate** (DEFINED_ROLE CDC) : consulte la file d'enrôlement, vérifie un dossier, demande un complément, valide, déclenche la convocation, constate la remise du badge et la clôture.
- **Administrateur fonctionnel** (rôle transverse déjà établi, F001/F002) : paramètre la liste des informations/pièces exigées (ENR-01), par cohérence avec son rôle déjà confirmé pour des paramétrages équivalents (REC-02, SLA) — non nommé spécifiquement par le texte ENR mais seule lecture cohérente avec le reste du CDC.

*(Les noms exacts des permissions seront fixés dans `/speckit.plan`, comme annoncé pour F006.)*

---

## 5. Cycle de Vie Exact (Workflow)

Uniquement les étapes sourcées par le CDC (§6.14 et §7.7, deux sections concordantes) :

| Étape | Acteur | Action | Données | Documents | Résultat | Exigence |
|:---|:---|:---|:---|:---|:---|:---|
| **1. Soumission** | Transporteur/Chauffeur | Renseigne les informations, dépose les pièces | Informations paramétrables (CLAR-ENR-02) | Pièces paramétrables | Ticket créé, en file Agent Gate | ENR-01 |
| **2. Vérification** | Agent Gate | Consulte et vérifie le dossier | — | Pièces déposées | Dossier en cours de vérification | ENR-02 |
| **3. Complément (si nécessaire)** | Agent Gate → Transporteur/Chauffeur | Demande de complément → réponse | Information manquante précisée | Pièce(s) additionnelle(s) éventuelle(s) | Dossier en attente de complément → reprise de la vérification | ENR-02, CLAR-ENR-03 |
| **4. Validation** | Agent Gate | Valide le dossier | — | — | Dossier validé | ENR-02 |
| **5. Convocation** | Agent Gate → Chauffeur (événement tracé) | Déclenche la convocation pour photo/badge | Événement "convocation" | — | Chauffeur convoqué | ENR-03, CLAR-ENR-06 |
| **6. Déplacement physique** | Chauffeur | Se présente pour la photo et la remise du badge | — | — | Réalisation constatée | ENR-03 |
| **7. Badge remis / Clôture** | Agent Gate | Constate la remise physique du badge | Événement "badge remis" | — | **Ticket clôturé** | ENR-03, CLAR-ENR-05 |
| **8. Notification (Should)** | Système | Notifie à chaque étape clé (dossier validé, convocation, badge disponible) | — | — | Chauffeur informé (canal hors périmètre F008, cf. CLAR-ENR-06) | ENR-04 |
| **9. Rappel avant expiration (Could)** | Système | Rappel automatique | Date d'expiration (CLAR-ENR-07) | — | Renouvellement à engager | ENR-05 |

Aucune étape (rejet, second niveau de validation, réactivation) n'est ajoutée au-delà de ce que le texte confirme.

---

## 6. Soumission (ENR-01, détail)

- **Qui soumet** : Transporteur/Chauffeur — une seule personne physique (CLAR-ENR-01, RÉSOLU).
- **Pour qui** : le dossier concerne cette même personne (identité de la personne physique amenée à se présenter physiquement en étape 6) — conséquence directe de CLAR-ENR-01/CLAR-ENR-02, aucun objet "dossier" ou "société" distinct de la personne n'est enrôlé.
- **Informations obligatoires** : non fixées par le CDC — paramétrables (CLAR-ENR-02). Aucun champ n'est inventé ici.
- **Justificatifs** : "pièces demandées", paramétrables (CLAR-ENR-02).
- **Référence éventuelle** : non mentionnée par le CDC pour ENR — pas de champ "référence externe" inventé (à la différence de REC/F006 où SOC-10 était explicitement cité).
- **Date** : date de soumission, portée nativement par le ticket (SOC-01, générique).
- **Société / Véhicule** : le CDC ne mentionne explicitement ni "société transporteur" ni "véhicule/camion" comme objets distincts dans le texte ENR-01→05 — confirmé par la recherche exhaustive de la passe `/speckit.clarify` (CLAR-ENR-01). **Aucun objet "société transporteur" ni "véhicule" n'est spécifié par la spec** — leur absence dans le CDC est actée, pas comblée par une supposition.

---

## 7. Documents (ENR-01, détail)

| Pièce | Obligatoire / Optionnelle | Déposant | Étape | Visibilité | Contrôle | Expiration |
|:---|:---|:---|:---|:---|:---|:---|
| **Liste configurable, non fixée par cette spec** | Déterminé par le paramétrage (Admin fonctionnel) | Transporteur/Chauffeur | Soumission (étape 1) ; complément possible (étape 3) | Transporteur/Chauffeur (son dossier), Agent Gate (dossiers de sa file) | Agent Gate (étape 2) | Non spécifiée par le CDC pour les pièces de soumission |

Conformément à PC-08, la spec exige un **mécanisme documentaire configurable** (réutilisation conceptuelle de SOC-07/F001, comme documents déjà associés au ticket), sans figer la liste exacte des pièces. Toute pièce nommée avant clarification effective serait une invention.

---

## 8. Vérification / Instruction — Agent Gate (ENR-02, détail)

- **Consultation** : l'Agent Gate consulte les dossiers de sa file.
- **Vérification** : contrôle des informations et pièces déposées.
- **Demande de complément** : possible, réutilisant conceptuellement le mécanisme "attente de réponse client" déjà éprouvé (SOC-06, CLAR-ENR-03).
- **Validation** : action confirmée par le texte, unique niveau (**aucun second niveau de validation n'est spécifié ni inventé**).
- **Rejet éventuel** : **non spécifié par le CDC** — voir CLAR-ENR-04. Non implémenté par cette spec.
- **Convocation** : déclenchée après validation, événement tracé par le ticket (ENR-03).
- **Activation/badge** : la spec ne décrit que la **constatation de la remise physique du badge** par l'Agent Gate (§13 du preflight) — aucune "activation" logicielle distincte n'est mentionnée par le CDC.

---

## 9. Complément (ENR-02, détail)

- **Qui le demande** : Agent Gate.
- **Ce qui passe en attente** : le dossier (ticket), le temps de traitement CIT est suspendu conceptuellement (par analogie avec TPS-02/TPS-04, principe transverse déjà appliqué à tous les modules).
- **Qui répond** : Transporteur/Chauffeur, auteur initial de la soumission.
- **Pièces additionnelles** : toute pièce du référentiel configurable (§7), déposée en réponse au complément demandé.
- **Reprise de l'instruction** : après réponse, le dossier retourne à l'état "en vérification" (Agent Gate), par réutilisation intégrale confirmée du mécanisme générique F001 (Messages, Documents, attente client) — **aucun second moteur** (CLAR-ENR-03, RÉSOLU).

---

## 10. Validation / Rejet (ENR-02, détail)

- **Conditions de validation** : dossier complet, vérifié par l'Agent Gate — le CDC ne détaille pas de critère de validation au-delà de "vérifier... valider".
- **Conditions de rejet** : **aucune** — CLAR-ENR-04 (RÉSOLU) confirme, par recherche exhaustive et négative, que le rejet définitif n'appartient pas au périmètre fonctionnel F008 tant qu'aucune exigence du CDC ne le prévoit explicitement. Cette spec ne prévoit donc aucun mécanisme de rejet.
- **Motif obligatoire** : sans objet (pas de rejet spécifié).
- **Visibilité client** : le Transporteur/Chauffeur voit l'état de son propre dossier (principe transverse SEC-01/02/03, déjà appliqué par toutes les features fermées).
- **Audit** : chaque changement d'état est tracé (§16).
- **Effet sur le cycle de vie** : validation → convocation (étape 5) ; pas d'état "rejeté" dans le cycle décrit par cette spec.

---

## 11. Convocation (ENR-03, détail)

- **Déclencheur** : validation du dossier (étape 4 → 5).
- **Destinataire** : le Chauffeur (personne physique amenée à se présenter).
- **Contenu fonctionnel** : information de convocation pour la photo et la remise du badge — le contenu précis (date/heure, lieu) n'est pas détaillé par le CDC.
- **Lien avec validation** : direct et séquentiel (le CDC ne prévoit pas de convocation avant validation).
- **Date/heure** : non fixée par le CDC comme un champ structuré — traitée comme une information portée par l'événement de convocation, sans format imposé ici.
- **Canal** : **non implémenté par F008** (CLAR-ENR-06) — SMS/WhatsApp mentionnés par ENR-04 (Should) dépendent de PC-16, hors périmètre. F008 documente le besoin (l'événement doit exister et être consommable par un futur module Notifications) sans l'implémenter.

---

## 12. Badge (ENR-03, détail)

Voir CLAR-ENR-05. Le CDC limite le "badge" à un événement de remise physique constaté par l'Agent Gate, immédiatement suivi de la clôture du ticket. Cette spec :
- **Ne décrit aucune création ou activation logicielle du badge** — non mentionnée.
- **Ne décrit aucune référence/numéro de badge** — non mentionné.
- **Ne décrit aucune technologie** (RFID, QR, carte à puce, API externe) — non mentionnée, non inventée.
- **Décrit uniquement** : l'événement "badge remis", constaté par l'Agent Gate au moment du déplacement physique du chauffeur, qui déclenche la clôture du ticket.

---

## 13. Clôture (ENR-03, détail)

Le flux CDC (§6.14 et §7.7, concordants) est explicite : *"Badge remis → [CLÔTURÉ]"*. La clôture intervient donc **au moment de la constatation de la remise du badge par l'Agent Gate** — ni à la seule validation, ni à la seule convocation. Aucune étape d'activation distincte n'est ajoutée entre la remise du badge et la clôture.

---

## 14. Cloisonnement

- **Transporteur/Chauffeur** : accède uniquement à son (ses) propre(s) dossier(s) d'enrôlement — une seule personne physique par dossier (CLAR-ENR-01, RÉSOLU), pas de partage inter-chauffeurs à concevoir.
- **Agent Gate** : accède aux dossiers de la file Enrôlement selon son habilitation (réutilisation directe du modèle RBAC F002, principe transverse SEC-01→12).
- **Aucune fuite inter-chauffeurs** : CLAR-ENR-01 étant résolue (pas de relation société↔chauffeurs), le cloisonnement se réduit au principe déjà éprouvé "un utilisateur ne voit que ses propres dossiers" (F002/F006) — aucune conception de partage multi-chauffeurs n'est nécessaire.
- **Aucune usurpation d'identifiant** : le serveur détermine l'identité de l'appelant depuis le contexte authentifié, jamais depuis une donnée transmise par le client — principe déjà appliqué systématiquement (SoumettreReclamationCommand/F006 en est l'exemple le plus récent).

---

## 15. Audit

Réutilisation conceptuelle du journal F001 (SOC-13) — **aucun second journal n'est créé**. Événements à confirmer en `/speckit.plan` (liste indicative, pas figée par cette spec) :
- Soumission (étape 1)
- Complément demandé / complément fourni (étape 3)
- Validation (étape 4)
- Convocation (étape 5)
- Badge remis / Clôture (étape 7)

---

## 16. SLA

Aucun délai explicite n'est défini par ENR-01→05. **NON SPÉCIFIÉ.** Le moteur SLA générique de F001 (déjà réutilisé par tous les modules fermés) reste configurable par l'Admin fonctionnel si des cibles sont fixées ultérieurement — aucune durée n'est inventée ici.

---

## 17. UI / UX (parcours fonctionnels, sans technique)

**Transporteur / Chauffeur** :
- Mes enrôlements (liste des dossiers)
- Nouvel enrôlement (soumission)
- Détail d'un dossier
- Compléter un dossier (réponse à une demande de complément)
- Suivre le statut

**Agent Gate** :
- File Enrôlements
- Détail d'un dossier
- Vérifier
- Demander un complément
- Valider
- Convocation / constatation de la remise du badge

Aucun identifiant (GUID) saisi manuellement dans un parcours utilisateur — l'identité de l'appelant est toujours dérivée du contexte authentifié (principe déjà appliqué par F006).

---

## 18. Matrice de Réutilisation F001/F002

| Besoin | F001 | F002 | Réutilisable ? | Extension fonctionnelle nécessaire ? |
|:---|:---:|:---:|:---:|:---|
| Ticket, numérotation | ✅ | — | Oui | Aucune |
| Dossier | ✅ | — | Oui | Aucune |
| Files (Agent Gate) | ✅ | — | Oui | Paramétrage d'une nouvelle file "Agent Gate" (donnée, pas nouvelle capacité) |
| Documents | ✅ | — | Oui | Aucune |
| Messages | ✅ | — | Oui (par analogie, CLAR-ENR-03) | Aucune |
| Audit | ✅ | — | Oui | Aucune |
| SLA | ✅ | — | Oui (si des cibles sont fixées ultérieurement) | Aucune |
| Comptes / Utilisateur | — | ✅ | Oui (mécanisme générique `ApplicationUser`/Identity) | Aucune sur le mécanisme lui-même |
| Société (`SocieteCliente`/`TypeClient`) | — | ✅ | **Non nécessaire** | CLAR-ENR-01 (RÉSOLU) : le Transporteur/Chauffeur est une personne physique unique, sans relation société↔chauffeurs — aucune extension de type F002/`SocieteCliente` n'est requise |
| Habilitations | — | ✅ | Oui | Nouvelle permission fonctionnelle "instruire un enrôlement" (nommage en `/speckit.plan`, patron déjà éprouvé par `Complaint.Instruire`) |
| Cloisonnement | — | ✅ | Oui (principe) | Portée simplifiée par CLAR-ENR-01 : un utilisateur ↔ ses propres dossiers, sans niveau société |

**Aucun doublon fonctionnel** : F008 ne recrée ni ticket, ni workflow, ni mécanisme documentaire, ni journal d'audit distinct.

---

## 19. Scénario Contractuel Rejoué : REC-SCE-08

> **Intitulé** : Enrôlement transporteur
> **Préconditions (texte exact)** : Liste pièces configurée · Compte Agent Gate activé.
> **Acteurs (texte exact)** : Transporteur/Chauffeur · Agent Gate.

| Étape REC-SCE-08 | Exigence ENR | Résultat attendu | Ambiguïté restante |
|:---|:---:|:---|:---|
| 1. Dossier à distance | ENR-01 | Ticket créé, informations et pièces déposées, en file Agent Gate | Aucune — acteur unique confirmé (CLAR-ENR-01, RÉSOLU) |
| 2. Complément | ENR-02 | Agent Gate demande un complément ; dossier en attente ; réponse du transporteur/chauffeur ; reprise de la vérification | Aucune — mécanisme confirmé réutilisation F001 (CLAR-ENR-03, RÉSOLU) |
| 3. Validation | ENR-02 | Agent Gate valide le dossier | Aucune — absence de rejet confirmée (CLAR-ENR-04, RÉSOLU) |
| 4. Convocation photo/badge | ENR-03 | Convocation déclenchée et tracée | Aucune — canal hors périmètre confirmé (CLAR-ENR-06, RÉSOLU) |
| 5. Clôture | ENR-03 | Badge remis, constaté par l'Agent Gate ; ticket clôturé | Aucune — badge = événement tracé, confirmé (CLAR-ENR-05, RÉSOLU) |

Le texte exact de `docs/04-matrice-recette.md` (ligne 39) couvre directement ENR-01, ENR-02, ENR-03 ; ENR-04 en couverture partielle ; **ENR-05 n'est pas couvert par ce scénario** (Could, absent du texte REC-SCE-08 — cohérent, non modifié). Après la passe `/speckit.clarify`, aucune étape du scénario ne porte plus d'ambiguïté fonctionnelle non résolue susceptible de fausser `/speckit.plan`.

---

## 20. User Stories

- **US1 — Soumettre un enrôlement** (Must, ENR-01) : en tant que Transporteur/Chauffeur, je renseigne les informations et dépose les pièces demandées, afin que mon dossier soit créé et transmis à l'Agent Gate.
- **US2 — Suivre et compléter un dossier** (Must, ENR-02) : en tant que Transporteur/Chauffeur, je consulte le statut de mon dossier et réponds à une demande de complément, afin que l'instruction puisse reprendre.
- **US3 — Instruire un enrôlement** (Must, ENR-02) : en tant qu'Agent Gate, je vérifie un dossier de ma file, et je peux demander un complément ou valider, afin de faire progresser le dossier dans son cycle de vie.
- **US4 — Convoquer et clôturer** (Must, ENR-03) : en tant qu'Agent Gate, je déclenche la convocation d'un dossier validé, puis je constate la remise du badge, afin de clôturer le dossier.
- **US5 — Être notifié aux étapes clés** (Should, ENR-04) : en tant que Transporteur/Chauffeur, je suis notifié lorsque mon dossier est validé, que je suis convoqué, ou que mon badge est disponible.
- **US6 — Être rappelé avant expiration du badge** (Could, ENR-05) : en tant que Transporteur/Chauffeur, je reçois un rappel automatique avant l'expiration de mon badge, afin d'engager son renouvellement.

*(US4 ne comporte pas de rejet — cf. CLAR-ENR-04. Aucune US "Valider / rejeter" combinée n'est écrite tant que ce point reste ouvert, contrairement à ce que suggérait le nommage indicatif du preflight.)*

---

## 21. Critères d'Acceptation (Given/When/Then — Must uniquement)

### ENR-01 — Soumission

- **Nominal** : Given un Transporteur/Chauffeur authentifié, When il renseigne les informations requises et dépose les pièces exigées, Then un ticket est créé, catégorisé "Enrôlement", et visible dans "Mes enrôlements".
- **Erreur** : Given des informations obligatoires manquantes, When la soumission est tentée, Then elle est refusée avec un message explicite (pas de ticket créé).
- **Autorisation** : Given un utilisateur non authentifié, When il tente d'accéder au formulaire de soumission, Then l'accès est refusé et redirigé vers la connexion.
- **Cloisonnement** : Given deux Transporteurs/Chauffeurs distincts, When l'un consulte "Mes enrôlements", Then il ne voit que ses propres dossiers.

### ENR-02 — Vérification, complément, validation

- **Nominal** : Given un dossier en file Agent Gate, When l'Agent Gate le valide, Then le dossier passe à l'état "Validé" et l'étape de convocation devient disponible.
- **Complément** : Given un dossier en cours de vérification, When l'Agent Gate demande un complément, Then le dossier passe en attente et le Transporteur/Chauffeur est notifié d'une action requise ; When celui-ci répond, Then le dossier retourne en vérification.
- **Autorisation** : Given un agent sans l'habilitation d'instruction Enrôlement, When il tente de valider un dossier, Then l'action est refusée côté serveur.
- **Cloisonnement** : Given un Agent Gate, When il consulte la file, Then il ne voit que les dossiers de la file Enrôlement pour laquelle il est habilité.

### ENR-03 — Convocation, badge, clôture

- **Nominal** : Given un dossier validé, When l'Agent Gate déclenche la convocation, Then l'événement de convocation est tracé sur le ticket ; When l'Agent Gate constate ensuite la remise du badge, Then le ticket est clôturé.
- **Autorisation** : Given un utilisateur sans habilitation Agent Gate, When il tente de constater une remise de badge, Then l'action est refusée côté serveur.

*(ENR-04/ENR-05, Should/Could, ne sont pas assorties de critères Given/When/Then Must — conformément à l'instruction de ne pas les élever artificiellement au rang de Must ; leurs critères seront écrits lors de leur implémentation effective si retenue au périmètre.)*

---

## 22. Frontières Strictement Définies

Hors périmètre F008, explicitement :
- **Notifications multicanales** (SMS/WhatsApp effectifs) — dépend de PC-16, futur module Notifications.
- **Continuité présentiel** (CON) — futur module dédié.
- **Assistant IA** — hors périmètre, aucune interaction avec l'enrôlement.
- **AVE (Avoir/Exonération)** — aucun lien fonctionnel identifié par le CDC entre ENR et AVE.
- **Workflow financier** — sans objet pour l'enrôlement.
- **Technologie de badge** (RFID/QR/carte/API externe) — non spécifiée par le CDC, non implémentée.
- **Intégration Gate externe non spécifiée** — le CDC ne mentionne aucun système externe (type IPAKI/IRIS pour Billing) pour l'enrôlement ; aucune intégration n'est supposée ni inventée.
- **Rejet définitif d'un dossier** — cf. CLAR-ENR-04, non implémenté tant que non confirmé.
- **Objet "véhicule/camion"** — non mentionné par le CDC pour ENR, non modélisé.

---

## 23. Matrice de Couverture

| Exigence | Priorité | User Story | Critère d'acceptation | REC-SCE-08 | Clarification | Dépendance |
|:---|:---:|:---:|:---:|:---:|:---|:---|
| ENR-01 | Must | US1 | §21 (nominal, erreur, autorisation, cloisonnement) | Étape 1 | CLAR-ENR-01, CLAR-ENR-02 | SOC-01 |
| ENR-02 | Must | US2, US3 | §21 (nominal, complément, autorisation, cloisonnement) | Étapes 2-3 | CLAR-ENR-03, CLAR-ENR-04 | SOC-04, SOC-06 |
| ENR-03 | Must | US4 | §21 (nominal, autorisation) | Étapes 4-5 | CLAR-ENR-05, CLAR-ENR-06 | COM-01 |
| ENR-04 | Should | US5 | Différé (Should, non Must) | Étape partielle | CLAR-ENR-06 | ENR-03, PC-16 |
| ENR-05 | Could | US6 | Différé (Could, non Must) | Non couvert | CLAR-ENR-07 | ENR-03 |

**Aucune exigence orpheline** — les 5 exigences ENR sont toutes rattachées à au moins une User Story et une clarification ou dépendance identifiée.

---

## 24. Points à Clarifier — Classification

| Point | Classement |
|:---|:---|
| CLAR-ENR-01 (modèle Transporteur/Chauffeur) | **RÉSOLUE** (`/speckit.clarify`, 31/08/2026) — Option A, personne physique unique, sourcée directement par `docs/00-analyse-cdc.md` §4.1. Ne bloque plus `/speckit.plan` ; seule la représentation technique (nommage des classes) reste à fixer en plan, sans ambiguïté fonctionnelle résiduelle. |
| CLAR-ENR-02 (liste des pièces/informations) | À CONFIRMER AVANT IMPLEMENTATION (PC-08, précédent établi F001/F003/F004/F006) |
| CLAR-ENR-03 (mécanisme technique du complément) | **RÉSOLUE** (`/speckit.clarify`, 31/08/2026) — réutilisation intégrale confirmée du socle générique F001, alignée sur IMP-04/REC-03. |
| CLAR-ENR-04 (rejet définitif) | **RÉSOLUE** (`/speckit.clarify`, 31/08/2026) — recherche exhaustive négative sur "rejet"/"refus" ; application de la règle par défaut : hors périmètre fonctionnel F008 tant qu'aucune exigence CDC ne le prévoit. |
| CLAR-ENR-05 (nature du badge) | RÉSOLUE (limite actée, pas de blocage) |
| CLAR-ENR-06 (canal de convocation/notification) | NON BLOQUANTE — frontière déjà tracée (hors F008) |
| CLAR-ENR-07 (durée avant expiration badge, ENR-05) | À CONFIRMER AVANT IMPLEMENTATION (uniquement si ENR-05 retenue au périmètre effectif) |
| PC-08 | À CONFIRMER AVANT IMPLEMENTATION (reclassé, voir preflight §10) |
| PC-14 (conservation données chauffeur) | À CONFIRMER AVANT PRODUCTION (classement source : "Bloque ARCHITECTURE") |
| PC-16 (SMS/WhatsApp) | À CONFIRMER AVANT PRODUCTION (ne concerne qu'ENR-04, Should) |

**Aucun point restant ne bloque `/speckit.plan`.** Les 3 clarifications qui bloquaient (CLAR-ENR-01, CLAR-ENR-03, CLAR-ENR-04) sont résolues par lecture directe du CDC ; les points restants (CLAR-ENR-02/07, PC-08/14/16) sont différés "à confirmer avant implémentation/production" — un classement déjà appliqué sans blocage à 4 features closes (F001, F003, F004, F006).

---

*Document mis à jour lors de la passe `/speckit.clarify` du 31 août 2026. Aucune clarification n'a été résolue à la place du métier au-delà de ce que le CDC permet d'affirmer directement — les décisions retenues (CLAR-ENR-01, CLAR-ENR-03, CLAR-ENR-04) sont chacune sourcées par citation exacte, pas par convenance de conception.*
