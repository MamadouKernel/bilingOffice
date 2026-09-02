# Preflight — Feature 008 (Enrôlement Transporteurs)

> **Document** : `docs/12-preflight-feature-008.md`
> **Date** : 31 août 2026
> **Nature** : Audit de préparation uniquement. Aucun `specs/008-*` créé, aucun code écrit, aucune feature F001-F007 modifiée, aucun commit.
> **Méthode** : reconstruction indépendante depuis le CDC et ses analyses (`docs/00` à `docs/04`), sans reprendre un classement antérieur comme acquis — même discipline que `docs/11-preflight-feature-007.md`.

---

## 1. État du projet après F006

- **Dernière feature fermée** : F006 — Customer Complaint (commit `794b9a6`, verdict *CLOSE WITH ACCEPTED DEBT*).
- **Couverture CDC** : 131/188.
- **Baseline tests** : 512/512 verts, 0 warning.
- **Aucun développement en cours.**

## 2. Feature 007 mise en attente métier

`docs/11-preflight-feature-007.md` a conclu **NO-GO — BUSINESS CLARIFICATION REQUIRED** pour Avoir & Exonération : le CDC lui-même (§25) reconnaît que le circuit de validation Finance (PC-15) et l'acteur instructeur (PC-03) restent à formaliser avec la direction Finance. F007 (AVE) reste donc en attente d'un atelier métier externe, non planifiable par ce dépôt. Ce preflight examine le candidat suivant recommandé par la feuille de route (`docs/09-etat-avancement-global.md` §8) : **Enrôlement Transporteurs**.

---

## 3. ENR-01 → ENR-05 (reconstitution exacte)

Source : `docs/01-matrice-exigences.md` lignes 217-221, `docs/00-analyse-cdc.md` §6.14/§7.7.

### ENR-01 (Must)
- **Texte CDC exact** : *"Le chauffeur ou le transporteur renseigne à distance les informations nécessaires et dépose les pièces demandées ; la liste des informations et pièces est paramétrable."*
- **Acteur** : Transporteur/Chauffeur (**DEFINED_ROLE** — explicitement listé en section 6 du CDC, `docs/00-analyse-cdc.md` §4.1, "Personne physique").
- **Déclencheur** : Démarche d'enrôlement, à distance (pas de présence physique requise à cette étape).
- **Données requises** : "informations nécessaires" — non détaillées, explicitement **paramétrables**.
- **Documents** : "pièces demandées" — non détaillées, explicitement **paramétrables** (même formulation que REC-02 pour les catégories de réclamation, déjà résolue avec succès en F006).
- **Validation / Approbation** : hors du texte d'ENR-01 (couvert par ENR-02).
- **Statuts** : *Nouveau* → *Créé* (génération du ticket, SOC-01).
- **Audit** : non spécifique (hérite de SOC-13, générique à tous les modules).
- **Dépendances** : SOC-01 (D — dépendance directe confirmée).

### ENR-02 (Must)
- **Texte CDC exact** : *"La soumission génère un ticket transmis aux agents Gate, qui peuvent vérifier, demander des compléments ou valider."*
- **Acteur** : Agent Gate (**DEFINED_ROLE** — explicitement listé, `docs/00-analyse-cdc.md` §4.2 : *"Vérifie dossier, demande compléments, valide, convoque pour photo/badge"*).
- **Déclencheur** : Réception du ticket en file Agent Gate.
- **Données requises** : le dossier soumis en ENR-01.
- **Documents** : consultation des pièces déposées.
- **Validation** : "valider" — confirmé, mécanisme identique aux autres modules (Pilote Manifeste pour IMP, Support Opérationnel pour REC).
- **Approbation** : une seule étape de validation mentionnée, pas de niveaux multiples (contrairement à AVE où ce point était totalement absent).
- **Statuts** : *Créé* → *En vérification* → *Complément* (optionnel) → *Validé*.
- **Audit** : SOC-13 (générique).
- **Dépendances** : SOC-04 (D, files), SOC-06 (D, mécanisme attente client — déjà réutilisé identiquement par REC-03/F006).

### ENR-03 (Must)
- **Texte CDC exact** : *"Le déplacement physique du chauffeur n'est requis que pour les étapes qui l'exigent (photo, badge) ; le ticket trace la convocation et la réalisation."*
- **Acteur** : Agent Gate (convocation) · Chauffeur (déplacement).
- **Déclencheur** : Dossier validé (ENR-02).
- **Workflow** : Convocation → déplacement physique limité à la photo et la remise du badge (confirmé explicitement, `docs/00-analyse-cdc.md` §6.14, "Règles").
- **Statuts** : *Validé* → *Convoqué* → *Badge remis* → *[CLÔTURÉ]* (flux confirmé §6.14 et §7.7, deux sections concordantes).
- **Audit** : traçabilité de la convocation et de la réalisation, explicitement requise par le texte.
- **Dépendances** : COM-01 (D, notifications).

### ENR-04 (Should)
- **Texte CDC exact** : *"Notification au chauffeur (SMS/WhatsApp prioritaires) à chaque étape clé : dossier validé, convocation, badge disponible."*
- **Acteur** : Transporteur/Chauffeur (destinataire).
- **Canal** : SMS/WhatsApp "prioritaires" — implique que d'autres canaux (email, portail) restent possibles en complément, pas en remplacement.
- **Dépendances** : COM-01 (E, extension) — **et PC-16** (souscription SMS/WhatsApp, voir §10) pour la mise en production effective du canal prioritaire ; le mécanisme de notification générique lui-même (F001) ne dépend pas de PC-16.

### ENR-05 (Could)
- **Texte CDC exact** : *"Gestion du renouvellement des badges avec rappel automatique avant expiration."*
- **Acteur** : Transporteur/Chauffeur · Agent Gate.
- **Donnée requise** : une date d'expiration du badge — **non mentionnée par ENR-01→04**, implique une extension du modèle (champ date d'expiration) si ENR-05 est retenue au périmètre.
- **Dépendances** : COM-01 (D, rappel = notification).
- **Statut** : Could — non couvert par le scénario REC-SCE-08 (voir §9), cohérent avec sa priorité la plus basse.

**MUST : 3/5 (ENR-01, 02, 03). SHOULD : 1/5 (ENR-04). COULD : 1/5 (ENR-05).**

---

## 4. Acteurs (aucun rôle Identity nouveau requis)

| Question | Réponse sourcée |
|:---|:---|
| Qui initie l'enrôlement ? | Transporteur/Chauffeur (ENR-01) |
| Qui valide ? | Agent Gate (ENR-02) |
| Qui peut corriger/compléter ? | Transporteur/Chauffeur, en réponse à une demande de complément de l'Agent Gate (ENR-02) |
| Qui peut rejeter ? | **Non explicitement mentionné.** ENR-02 dit "vérifier, demander des compléments ou valider" — ne mentionne pas de rejet définitif, contrairement à IMP-04 (Pilote Manifeste) ou REC-03 qui prévoient un rejet motivé explicite. Absence à noter, pas à combler par une invention (voir §10). |
| Qui peut consulter ? | Transporteur/Chauffeur (son propre dossier), Agent Gate (dossiers de sa file) — cloisonnement générique, voir §9 |
| Rôle du transporteur | Personne physique, DEFINED_ROLE, initiateur du dossier |
| Rôle du chauffeur | Personne physique, DEFINED_ROLE — le CDC emploie "chauffeur OU transporteur" de façon quasi interchangeable dans le texte (voir §5, ambiguïté objet) |
| Rôle de l'administrateur CIT | **Non mentionné pour ENR spécifiquement.** L'Administrateur fonctionnel (DEFINED_ROLE générique, §4.2) reste présumé responsable du paramétrage (liste des pièces/informations, ENR-01) par cohérence avec son rôle transverse déjà établi (REC-02, SLA, etc.), sans texte ENR-spécifique qui le nomme. |

**Constat** : les deux acteurs structurants (Transporteur/Chauffeur, Agent Gate) sont **DEFINED_ROLE** — statut le plus fort de la taxonomie CDC, explicitement listés en section 6. Aucun acteur ROLE_TO_CONFIRM n'intervient dans ENR-01→05, à la différence d'AVE (PC-03). **Aucun nouveau rôle Identity n'est nécessaire.**

---

## 5. Objets métier — ambiguïté réelle identifiée (non résolue ici)

Le CDC ne distingue pas clairement si l'enrôlement porte sur :
- une **société transporteur** (par analogie avec `SocieteCliente`/F002, qui modélise déjà des sociétés avec des `TypeClient` Normal/EnCompte/Deposit/IES) ;
- un **chauffeur individuel** (personne physique, sans société porteuse) ;
- une **combinaison** (société transporteur avec plusieurs chauffeurs rattachés, à l'image de `UtilisateurSociete`).

**Preuve du flou** : `docs/00-analyse-cdc.md` §4.1 liste "Transporteur/Chauffeur" comme **une seule ligne d'acteur**, "Personne physique" — pas "Société". ENR-01 dit "le chauffeur **ou** le transporteur" (alternative, pas composition claire). Aucun des 4 documents sources (`00-analyse-cdc.md`, `01-matrice-exigences.md`, `02-points-a-clarifier.md`, `03-revue-coherence-analyse.md`) ne lève cette ambiguïté — et **aucun PC-xx existant ne la référence** (recherche exhaustive de "transporteur" dans `02-points-a-clarifier.md` : seules les occurrences PC-08/PC-14/PC-16 déjà citées, aucune sur la structure de l'objet).

**Ce que F002 fournit déjà** : le modèle RBAC générique (`HabilitationUtilisateur`), le modèle de rattachement `UtilisateurSociete` (utilisateur ↔ société), mais **aucun `TypeClient` ne correspond à "Transporteur"** — c'est un profil fonctionnellement différent des 4 profils Billing existants (pas d'Import/Export, uniquement Enrôlement).

**Nature du blocage** : ceci est une question de **modélisation de données** (résolvable en `/speckit.plan` → `data-model.md`, via une décision de recherche documentée — exactement comme F006 a tranché "Ticket vs nouvelle entité" dans `research.md` D1, sans atelier métier externe). **Ce n'est pas un point bloquant pour la spec** : la spec peut décrire fonctionnellement "un chauffeur, éventuellement rattaché à une société transporteur, s'enrôle" sans trancher la modélisation exacte — exactement comme F006 a spécifié REC-01→06 sans figer le schéma EF avant `/speckit.plan`.

---

## 6. Documents

| Pièce | Obligatoire / Optionnelle | Qui dépose | Validité / Expiration | Visibilité | Validation |
|:---|:---|:---|:---|:---|:---|
| **Liste exacte non fournie par le CDC** | ENR-01 dit uniquement "la liste des informations et pièces est paramétrable" | Transporteur/Chauffeur (ENR-01) | Non mentionné pour les pièces d'enrôlement elles-mêmes | Transporteur (son dossier), Agent Gate (files) | Agent Gate (ENR-02) |
| **Badge** (résultat, pas une pièce déposée) | — | Délivré par Agent Gate | **Oui — ENR-05 implique une expiration**, mais aucune durée n'est donnée par le CDC | Chauffeur, Agent Gate | Agent Gate (remise physique) |

**Rattachement PC** : PC-08 ("Liste exacte des pièces exigées par étape et par parcours") référence explicitement *"les pièces exigées pour un dossier d'enrôlement transporteur"* dans son action requise (`docs/02-points-a-clarifier.md` ligne 206). Aucune pièce n'est inventée ici — la liste reste à paramétrer, comme le texte CDC le prescrit lui-même ("paramétrable").

---

## 7. Workflow (cycle de vie sourcé)

| Étape | Acteur | Action | État avant | État après | Source |
|:---|:---|:---|:---|:---|:---|
| 1. Soumission à distance | Transporteur/Chauffeur | Saisit dossier + dépose pièces | *Nouveau* | Créé / en file Agent Gate | ENR-01 ; flux §6.14 |
| 2. Vérification | Agent Gate | Vérifie le dossier | En file | En vérification | ENR-02 |
| 3. Complément (si nécessaire) | Agent Gate → Chauffeur | Demande de complément → réponse | En vérification | Attente chauffeur → En vérification | ENR-02 ("demander des compléments") — mécanisme précis non détaillé, cohérence avec SOC-06 présumée mais non confirmée texte-à-texte pour ENR spécifiquement |
| 4. Validation du dossier | Agent Gate | Valide | En vérification | Validé | ENR-02 |
| 5. Convocation photo/badge | Agent Gate → Chauffeur | Convocation (notification) | Validé | Convoqué | ENR-03 |
| 6. Déplacement physique | Chauffeur | Se présente pour photo + badge | Convoqué | Réalisé | ENR-03 (déplacement "limité à la photo et la remise du badge") |
| 7. Badge remis / Clôture | Agent Gate | Remet le badge | Réalisé | **[CLÔTURÉ]** | Flux §6.14 et §7.7 (deux sections concordantes) |
| 8. (Should) Notifications à chaque étape clé | Système | Notifie (dossier validé, convocation, badge disponible) | — | — | ENR-04 |
| 9. (Could) Rappel avant expiration badge | Système | Notifie | Badge actif | Renouvellement à engager | ENR-05 |

**Constat** : à la différence de la table équivalente pour AVE (`docs/11-preflight-feature-007.md` §4, où 6 des 11 cases étaient "NON SPÉCIFIÉ"), **toutes les étapes structurantes (1 à 7) sont sourcées par un texte CDC explicite et concordant entre deux sections indépendantes** (§6.14 "Règles" et §7.7 "Flux"). Seule l'étape 3 (mécanisme exact du complément) manque d'un détail fin, sans remettre en cause la clarté globale.

---

## 8. Dépendances

| Capacité | F001 (Ticket/Workflow/Documents/Audit) | F002 (Compte/Société/Utilisateur) | F003.5 (UI/Navigation/AuthZ UX) | F003/F004 (Billing Import/Export) |
|:---|:---:|:---:|:---:|:---:|
| Génération ticket, numérotation (ENR-01) | ✅ Réutilisation directe (SOC-01) | — | — | — |
| File "Agent Gate", prise en charge (ENR-02) | ✅ Réutilisation directe (mécanisme de file générique, même patron que "Support Opérationnel"/F006) | — | — | — |
| Demande de complément / attente (ENR-02) | ✅ Réutilisation directe (SOC-06, déjà prouvé par REC-03/F006) | — | — | — |
| Dépôt de pièces (ENR-01) | ✅ Réutilisation directe (SOC-07, `DocumentJoint`) | — | — | — |
| Journal d'audit (tous) | ✅ Réutilisation directe (SOC-13) | — | — | — |
| Habilitation Agent Gate | — | ✅ Réutilisation directe (`HabilitationUtilisateur`, même patron que `Complaint.Instruire`) | — | — |
| Compte/rattachement Transporteur/Chauffeur | — | ⚠️ **Extension nécessaire** — aucun `TypeClient` existant ne couvre ce profil (voir §5) | — | — |
| Pages de soumission/instruction, navigation | — | — | ✅ Réutilisation directe (composants partagés déjà éprouvés par F006 : `PermissionDenied`, `IndicateurChargement`, `EtatVide`) | — |
| Notifications (ENR-04) | ✅ Mécanisme générique (`PublierEvenementAsync`) | — | — | — |

**F003/F004 sont-elles réellement nécessaires ?** **Non.** Aucune exigence ENR-01→05 ne mentionne de lien fonctionnel avec Import, Export, factures ou paiements. L'enrôlement est un module autonome, indépendant du domaine Billing — il ne réutilise que le **socle générique** (F001) et l'**identité générique** (F002), exactement comme Customer Complaint (F006) l'a démontré pour un module tout aussi autonome.

---

## 9. Cloisonnement

| Exigence | Statut |
|:---|:---|
| Transporteur voit uniquement ses propres données | Principe générique déjà éprouvé (SEC-01 à SEC-12, appliqué identiquement par F001-F006) — mécanisme réutilisable, mais **dépend de la résolution de l'objet Transporteur/Chauffeur** (§5) pour définir précisément la portée "ses données" (dossier individuel, ou dossiers de tous les chauffeurs d'une société transporteur ?). |
| Chauffeur rattaché au bon transporteur | **Non déterminable avant résolution de §5** — si "Transporteur" et "Chauffeur" sont un seul et même objet (personne physique), la question ne se pose pas ; si une société transporteur avec plusieurs chauffeurs est modélisée, un mécanisme de rattachement analogue à `UtilisateurSociete` sera nécessaire. |
| CIT (Agent Gate) voit uniquement selon habilitation | ✅ Mécanisme générique déjà éprouvé (`HabilitationUtilisateur` scopée par file), directement réutilisable. |
| Aucun IDOR | Principe transverse (SEC-02, SEC-03) déjà appliqué à chaque feature fermée — aucune raison technique de ne pas le reproduire pour ENR ; dépend de la même résolution objet (§5) pour être vérifié précisément. |

**Constat** : le cloisonnement CIT↔Agent Gate est déjà résolu par le socle. Le cloisonnement côté Transporteur/Chauffeur dépend entièrement de la décision de modélisation (§5) — **à trancher en `/speckit.plan`, pas bloquant pour `/speckit.specify`**.

---

## 10. Points à clarifier (PC-xx) — classification revalidée

| PC | Description | Niveau officiel | Applicable à ENR ? | Reclassement pour ENR |
|:---|:---|:---|:---|:---|
| **PC-08** | Liste exacte des pièces exigées par étape/parcours (inclut explicitement "dossier d'enrôlement transporteur") | 🔴 BLOQUANT, "Bloque : SPECIFICATION" (label générique du document) | Oui, explicitement cité | **À CONFIRMER AVANT IMPLEMENTATION** — pas un blocage réel de la spec : précédent direct sur 4 features déjà fermées (F001, F003, F004, F006) qui ont toutes traité une liste de pièces "paramétrable" de façon identique (clarification documentée dans la spec, liste réelle validée plus tard). ENR-01 emploie lui-même le mot "paramétrable" — cohérent avec ce patron déjà éprouvé, pas une invention. |
| **PC-14** | Politique de conservation/archivage (mentionne "les données personnelles des chauffeurs ont-elles une durée de conservation spécifique (ARTCI) ?") | 🟠 CRITIQUE, "Bloque : **ARCHITECTURE**" | Oui, cas particulier cité | **NON BLOQUANT POUR SPEC/PLAN/IMPLEMENTATION** — classé "Bloque : ARCHITECTURE" par le document source lui-même, pas SPECIFICATION. À trancher avant mise en production, pas avant `/speckit.specify`. |
| **PC-16** | Souscription SMS/WhatsApp (ENR-04 cité explicitement comme source) | 🟠 CRITIQUE, "Bloque : **PRODUCTION**" | Oui, pour ENR-04 uniquement (Should) | **NON BLOQUANT POUR SPEC** — le mécanisme de notification générique (F001) fonctionne sans canal SMS/WhatsApp actif (repli email/portail, comme documenté pour PC-16 : "le démarrage sans WhatsApp/SMS est possible"). Bloque uniquement la mise en production du canal prioritaire, pas ENR-01→03 (Must). |
| **Objet Transporteur/Chauffeur (§5)** | Aucun PC existant ne couvre cette ambiguïté de modélisation | — (non documenté ailleurs) | Oui, nouvellement identifiée par ce preflight | **À CONFIRMER EN PLAN** (`data-model.md`, décision de recherche documentée) — pas un point de clarification métier bloquant, une décision de conception à trancher au moment venu, comme F006 l'a fait pour "Ticket vs nouvelle entité". |
| **Rejet définitif du dossier (§4)** | Absence de mention explicite d'un rejet dans ENR-02 (contrairement à IMP-04/REC-03) | — (non documenté ailleurs) | Oui, nouvellement identifiée | **À CONFIRMER AVANT IMPLEMENTATION** — le texte CDC ne l'exclut ni ne l'impose ; à documenter comme clarification dans la spec (à l'image de CLAR-REC-XX pour F006), pas à inventer un mécanisme de rejet motivé sans base texte. |

**Aucun PC ne bloque la spécification d'ENR** — différence structurante avec AVE, où PC-03 et PC-15 bloquaient explicitement "SPECIFICATION".

---

## 11. Scénario contractuel (REC-SCE-08)

`docs/04-matrice-recette.md` ligne 39 : *"REC-SCE-08 | Enrôlement transporteur | Dossier à distance, complément, validation, convocation photo/badge, clôture | ENR-01 · ENR-02 · ENR-03 | ENR-04 | Liste pièces configurée · Compte Agent Gate activé | Transporteur/Chauffeur · Agent Gate | À jouer en recette"*.

- **Préconditions** : liste des pièces configurée · compte Agent Gate activé.
- **Acteurs** : Transporteur/Chauffeur · Agent Gate.
- **Étapes** : dossier à distance → complément → validation → convocation photo/badge → clôture (concordant exactement avec la table §7).
- **Résultat attendu** : cycle complet jusqu'à clôture, tel que déjà décrit par le flux CDC.
- **Exigences couvertes** : ENR-01, ENR-02, ENR-03 en couverture directe ; ENR-04 en couverture partielle (E) ; **ENR-05 non couverte** (Could, cohérent avec sa priorité).

**Ce scénario existe et est exploitable sans invention** — différence structurante supplémentaire avec AVE (aucun REC-SCE dédié).

---

## 12. Risques

- **Risque principal** : l'ambiguïté de modélisation Transporteur/Chauffeur (§5) pourrait, si mal cadrée en `/speckit.plan`, conduire à un modèle de données à refaire si le métier confirme plus tard une structure "société transporteur avec plusieurs chauffeurs". Atténuation : traiter comme une décision de recherche documentée (`research.md`), pas une supposition silencieuse, et signaler explicitement l'hypothèse retenue dans le plan pour validation ultérieure.
- **Risque secondaire** : absence de mention d'un rejet définitif (§4/§10) — si le workflow réel en prévoit un, une clarification devra être ajoutée en cours de spec (mécanisme déjà maîtrisé, voir CLAR-REC-xx de F006).
- **Risque mineur** : ENR-04/ENR-05 dépendent de canaux (SMS/WhatsApp) et de champs (expiration badge) non encore présents dans le socle — n'affecte pas les 3 exigences Must, seulement le périmètre Should/Could.
- **Aucun risque de blocage métier externe** (contrairement à AVE) : aucune question posée dans ce document ne nécessite un atelier avec une direction externe au projet — toutes sont résolubles par les mécanismes de clarification/recherche déjà maîtrisés par ce dépôt.

---

## VERDICT

```
FEATURE CANDIDATE : 008 — Enrôlement Transporteurs
ENR : 5/5 analysées
MUST / SHOULD / COULD : 3 Must (ENR-01, 02, 03) / 1 Should (ENR-04) / 1 Could (ENR-05)
REC-SCE : REC-SCE-08 existe et couvre ENR-01→03 directement, ENR-04 partiellement (ENR-05 non couverte, Could)
POINTS BLOQUANTS : AUCUN bloquant SPEC — PC-08 reclassé "à confirmer avant implémentation" (précédent établi sur 4 features déjà fermées), PC-14 bloque seulement ARCHITECTURE, PC-16 bloque seulement PRODUCTION (et seulement ENR-04/Should)
DÉPENDANCES : F001 (Ticket/Workflow/Documents/Audit) et F002 (RBAC générique) suffisent ; F003/F004 non nécessaires ; extension F002 nécessaire pour modéliser Transporteur/Chauffeur (à trancher en plan, pas en spec)
GO / NO-GO : GO FOR /speckit.specify
VERDICT : GO
```

**STOP. Aucun `specs/008-*` créé. Aucun code écrit. Aucune feature F001-F007 modifiée. Aucun commit. `/speckit.specify` non lancé. Feature 008 non commencée.**
