# Preflight — Feature 007 (Avoir & Exonération)

> **Document** : `docs/11-preflight-feature-007.md`
> **Date** : 31 août 2026
> **Nature** : Audit de préparation uniquement. Aucun `specs/007-*` créé, aucun code écrit, aucune feature fermée modifiée, aucun commit.
> **Méthode** : reconstruction indépendante depuis le CDC et ses analyses (`docs/00` à `docs/04`), l'état d'avancement (`docs/09`), et `closure-review.md` de F006 — sans reprendre un classement antérieur comme acquis (§0 du brief).

---

## 1. État du projet après F006

- **Dernière feature fermée** : F006 — Customer Complaint (commit `794b9a6`, verdict *CLOSE WITH ACCEPTED DEBT*).
- **Couverture CDC** : **131/188** (125/188 avant F006 selon `docs/09-etat-avancement-global.md`, + les 6 exigences REC-01→06 désormais conformes — recoupé avec la projection de `specs/006-customer-complaint/spec.md`).
- **Baseline tests** : 512/512 verts, 0 warning (état certifié à la clôture de F006).
- **Dette acceptée reportée** : nom d'auteur générique sur les messages client (cosmétique, transversal F001-F006), T026 (bUnit dédié par composant, couverture fonctionnelle déjà assurée).
- **Aucun développement en cours.**

---

## 2. AVE-01 → AVE-04 (reconstitution exacte)

Source : `docs/01-matrice-exigences.md` lignes 193-196, `docs/00-analyse-cdc.md` §6.12/§7.8.

### AVE-01 (Must)
- **Texte CDC exact** : *"Le client peut soumettre une demande d'avoir ou d'exonération avec les justificatifs exigés ; chaque demande génère un ticket."*
- **Acteur** : Client.
- **Déclencheur** : Soumission d'une demande (avoir OU exonération — le CDC ne précise pas si ce sont deux types de demande distincts avec des règles propres, ou une même demande avec un attribut "type").
- **Workflow** : Création → génération d'un ticket (SOC-01, comme tous les modules).
- **Documents** : "justificatifs exigés" — **liste non fournie** (même nature d'ambiguïté que PC-08 pour d'autres modules, mais non couverte par PC-08 explicitement — aucun PC ne référence AVE-01 pour les pièces).
- **Validation / Approbation / Finance / Audit** : hors du texte d'AVE-01 (couverts par AVE-02/03).
- **Dépendance Billing** : aucune dépendance directe listée par le CDC (SOC-01 uniquement, générique).
- **Dépendance REC** : aucune — AVE-01 ne mentionne pas Customer Complaint.

### AVE-02 (Must)
- **Texte CDC exact** : *"Chaque type de demande suit son propre workflow de validation, avec étapes et intervenants paramétrables."*
- **Acteur** : Admin fonctionnel (pour le paramétrage) — **pas l'acteur qui exécute le workflow**, qui reste non nommé.
- **Déclencheur** : Suite à AVE-01.
- **Workflow** : Non détaillé — "étapes et intervenants paramétrables" est une déclaration de principe (comme WKF-01 pour les autres modules), pas une description du workflow réel.
- **Documents / Validation / Approbation / Finance** : **aucun détail** — c'est précisément l'objet de PC-15.
- **Audit** : non mentionné spécifiquement (hérite présumément de SOC-13, générique).
- **Dépendance Billing** : aucune.
- **Dépendance REC** : aucune.
- **Statut dans la matrice source** : `docs/01-matrice-exigences.md` marque déjà cette ligne **"À valider (PC-15)"** — pas "Confirmé" comme les autres lignes de la matrice.

### AVE-03 (Must)
- **Texte CDC exact** : *"La décision (accord, rejet motivé) et les documents associés sont déposés dans le ticket et notifiés au client."*
- **Acteur** : "Agent habilité" — la matrice source annote elle-même *(ROLE_TO_CONFIRM — cf. PC-03 et PC-15)*. `docs/03-revue-coherence-analyse.md` (E-02) confirme qu'une version antérieure de la matrice avait inventé un acteur "Agents Finance" non présent dans le CDC, corrigé depuis en "Agent habilité (rôle à définir)".
- **Déclencheur** : Fin du workflow de validation (AVE-02).
- **Workflow** : Décision → dépôt documents → notification client. Ordre confirmé, **contenu de la décision (niveaux, seuils, qui décide en dernier ressort) non spécifié**.
- **Documents** : "documents associés" à la décision — non typés.
- **Validation / Approbation / Finance** : objet de PC-15, non résolu.
- **Audit** : implicite (SOC-13 générique).
- **Dépendance Billing** : aucune dépendance explicite vers Import/Export/Simulation.
- **Dépendance REC** : aucune.

### AVE-04 (Should)
- **Texte CDC exact** : *"Une demande d'avoir/exonération peut être liée au ticket de parcours client concerné."*
- **Acteur** : Client · Agent.
- **Workflow** : Liaison optionnelle (`peut être liée`), via le mécanisme déjà existant SOC-10 (`LiaisonTicket`, F001) — **la seule des 4 exigences AVE sans ambiguïté de fond**, purement une réutilisation de capacité déjà livrée.
- **Dépendance Billing** : indirecte — le "ticket de parcours concerné" est typiquement un ticket Billing Import/Export (F003/F004), mais rien n'impose que ce soit exclusivement Billing.
- **Dépendance REC** : le texte n'exclut ni n'impose un ticket Customer Complaint comme "parcours concerné" — voir §6.

---

## 3. Points bloquants (revalidés contre le CDC, pas repris tels quels)

| PC | Question exacte | Source | Impact | Statut actuel | Bloque SPEC ? | Bloque PLAN ? | Bloque IMPLEMENTATION ? | Bloque RECETTE ? |
|:---|:---|:---|:---|:---|:---:|:---:|:---:|:---:|
| **PC-03** | Qui est l'"Intervenant Avoir/Exo" (AVE-02/03) ? Rôle applicatif distinct, ou action assignée à un rôle existant (Facturation, Caisse/Finance) ? | CDC §6, confirmé non résolu par `docs/00-analyse-cdc.md` §6.12 et `docs/03-revue-coherence-analyse.md` (E-02) | Sans acteur nommé, aucune permission, aucune file, aucun contrat d'autorisation ne peut être écrit sans invention. | **Ouvert.** Classé 🔴 BLOQUANT, "Bloque : SPECIFICATION" dans `docs/02-points-a-clarifier.md` — reclassement confirmé, pas hérité aveuglément. | **OUI** | OUI (dérivé) | OUI (dérivé) | OUI (dérivé) |
| **PC-15** | Quel est le circuit de validation interne Finance (étapes, intervenants, niveaux d'approbation, seuils) ? | CDC §25 lui-même : *"le statut exact du workflow Avoir/Exonération est « à formaliser avec la Finance »"* — donc **le CDC reconnaît explicitement ne pas trancher ce point**, ce n'est pas une lacune d'analyse. | Sans ce circuit, AVE-02/03 ne peuvent être spécifiées sans inventer un workflow financier (violerait le Principe II — Source de Vérité — de `constitution.md` : le CDC prévaut, rien ne comble ses silences par une règle métier fabriquée). | **Ouvert.** Classé 🟠 CRITIQUE, "Bloque : SPECIFICATION" dans `docs/02-points-a-clarifier.md`. Confirmé également par `docs/09-etat-avancement-global.md` §6/§8 ("Ce point paralyse le développement du module G", F007 listée "*Sous réserve de levée du point PC-15*"). | **OUI** | OUI (dérivé) | OUI (dérivé) | OUI (dérivé) |

**Autres PC applicables à AVE** : aucun autre point de `docs/02-points-a-clarifier.md` ne référence AVE-01→04 (vérifié par recherche exhaustive du texte "AVE" et "Avoir" dans le document). PC-08 (pièces exigées, généraliste) pourrait s'appliquer à "justificatifs exigés" (AVE-01) mais n'est pas classé bloquant pour la spec dans les autres features (traité comme "paramétrable, à confirmer avant implémentation" pour REC/IMP/EXP) — **PC-08 ne bloque pas la spec**, seulement PC-03 et PC-15 le font.

---

## 4. PC-15 — Table détaillée du manquant (priorité maximale)

Aucune des lignes suivantes n'est comblée par une supposition : chaque case vide signifie que le CDC est silencieux, pas qu'une valeur par défaut a été choisie.

| Étape | Acteur | Action | Décision | Source CDC | Ambiguïté |
|:---|:---|:---|:---|:---|:---|
| 1. Initiation | Client | Soumet demande + justificatifs | — | AVE-01 (texte confirmé) | Aucune — seule étape entièrement spécifiée |
| 2. Instruction | **NON SPÉCIFIÉ** | **NON SPÉCIFIÉ** | — | AVE-02 ("intervenants paramétrables", sans liste) | Qui instruit ? Facturation, Finance, un rôle dédié ? |
| 3. Validation / Approbation | **NON SPÉCIFIÉ** | **NON SPÉCIFIÉ** | Accord / Rejet motivé (seul point confirmé) | AVE-03 | Nombre de niveaux d'approbation ? Direction Financière impliquée directement ou seulement en escalade ? |
| 4. Seuils de montant | **NON SPÉCIFIÉ** | — | — | Absent du CDC | Un seuil déclenche-t-il un niveau de validation supplémentaire ? |
| 5. Motifs (rejet/accord) | — | — | "Rejet motivé" (texte confirmé, sans taxonomie) | AVE-03 | Motifs libres ou liste paramétrable (comme REC-02 pour les catégories) ? |
| 6. Pièces justificatives | Client | Dépôt | — | AVE-01 ("justificatifs exigés", sans liste) | Quelles pièces exactement ? (nature similaire à PC-08, non couverte par lui) |
| 7. Retour pour complément | **NON MENTIONNÉ** | — | — | Absent des 4 exigences AVE | Le mécanisme SOC-06 (attente client, réutilisé par REC-03) s'applique-t-il ? Le CDC ne le dit pas pour AVE. |
| 8. Impact sur facture | **NON MENTIONNÉ** | — | — | Absent | Un avoir accordé modifie-t-il une facture Billing existante (F003/F004) ? Génère-t-il un document financier séparé ? |
| 9. Impact sur paiement | **NON MENTIONNÉ** | — | — | Absent | Un avoir affecte-t-il le solde/la validation de paiement d'un ticket Billing ? |
| 10. Clôture | Agent (implicite) | Dépôt documents + notification client | — | AVE-03 (partiel) | Le dépôt de la décision clôture-t-il directement le ticket, ou une étape distincte existe-t-elle ? |
| 11. Audit | **NON SPÉCIFIÉ pour AVE** | — | — | Absent (SOC-13 générique présumé applicable) | Aucune exigence AVE ne le mentionne explicitement — hérité par défaut du socle, à confirmer sans ambiguïté réelle |

**Constat** : sur 11 étapes nécessaires pour décrire un workflow financier exploitable, seules 2 (initiation, nature de la décision finale) sont réellement fixées par le CDC. Rédiger une spec aujourd'hui obligerait à inventer au moins 6 des 11 étapes — contraire au Principe II de `constitution.md` ("rien ne prévaut sur le CDC en matière de règle métier").

---

## 5. Dépendances existantes (F001, F002, F003/F004, F003.5, F006)

| Besoin AVE | Déjà disponible | Extension nécessaire | Nouvelle capacité |
|:---|:---|:---|:---|
| Génération de ticket, numérotation, statuts (AVE-01) | ✅ F001 (`Ticket`, `SOC-01`) | Aucune — réutilisation directe | — |
| Workflow paramétrable par type de demande (AVE-02) | ⚠️ F001 fournit un moteur de workflow générique (`TransitionAutorisee`, files) | Paramétrage du workflow spécifique AVE **une fois PC-15 résolu** — pas une nouvelle architecture | — |
| Rôle "Intervenant Avoir/Exo" avec permission dédiée (AVE-02/03) | ⚠️ F002 fournit le modèle RBAC générique (`HabilitationUtilisateur`, permission codes) | Ajout d'une permission `Avoir.Instruire`/`Avoir.Approuver` (mécanisme déjà prouvé par F006 avec `Complaint.*`) — **mais le nom de la permission ne peut être posé qu'après PC-03** | — |
| Dépôt de documents (AVE-03) | ✅ F001 (`DocumentJoint`, SOC-07) | Aucune | — |
| Notification client (AVE-03) | ⚠️ F001 publie des événements génériques (`PublierEvenementAsync`), mais le canal effectif (email, portail) reste, comme pour F006, hors périmètre applicatif détaillé (cf. PC-16/17) | Aucune extension propre à AVE | — |
| Liaison au ticket de parcours (AVE-04) | ✅ F001 (`LiaisonTicket`, SOC-10) | Aucune — réutilisation directe, y compris avec un ticket Customer Complaint (F006) si applicable | — |
| Impact sur facture/paiement (si confirmé lors du cadrage Finance) | ⚠️ F003/F004 fournissent `Ticket`, facturation, validation de paiement | **Extension probable** de `Ticket` ou d'un mécanisme de liaison Facture↔Avoir — **nature exacte dépendante de PC-15** | Possible si le cadrage Finance révèle un impact structurel sur le modèle de facturation (pas déterminable aujourd'hui) |
| Interface de soumission/instruction | ✅ F003.5 fournit la navigation, les composants partagés (`PermissionDenied`, `IndicateurChargement`, etc.) réutilisés par F006 | Nouvelles pages `NouvelleDemandeAveExoneration.razor`, file d'instruction — même patron que F006 | — |
| Rattachement à une réclamation (AVE ⇐ REC) | ✅ F006 (`Ticket.Module == "CustomerComplaint"`, `LiaisonTicket`) | Aucune extension F006 requise si le lien se fait par `LiaisonTicket` générique (déjà AVE-04) | — |

**Conclusion §5** : la couche technique (F001, F002, F003/F004, F003.5, F006) est **structurellement prête** — aucune nouvelle capacité d'architecture n'est nécessaire. Le blocage est **entièrement fonctionnel** (acteurs et workflow), pas technique.

---

## 6. Frontière REC ↔ AVE

Le CDC ne rend **jamais** REC obligatoire pour déclencher AVE, et ne l'exclut pas non plus :
- AVE-01 dit uniquement *"le client peut soumettre une demande"* — aucune précondition de réclamation préalable.
- AVE-04 (Should) permet de lier une demande AVE à *"le ticket de parcours client concerné"* — formulation générique, pas restreinte à un ticket Customer Complaint. Le "parcours concerné" est plus naturellement un ticket Billing Import/Export dans la majorité des cas d'usage (ex. contestation d'une facture).
- Rien dans REC-01→06 (F006) ne mentionne AVE ni la génération d'une demande d'avoir.

**Conclusion** : AVE peut naître de **plusieurs sources indépendantes** — d'une décision interne, d'une facture contestée (lien vers un ticket Billing), ou éventuellement d'une réclamation (lien vers un ticket Customer Complaint) — sans qu'aucune ne soit imposée ou privilégiée par le texte. Ne pas figer REC comme point d'entrée obligatoire dans une future spec ; le CDC n'en dit rien et l'inventer serait une extrapolation.

---

## 7. REC-SCE dédié à AVE

**Aucun scénario contractuel dédié n'existe.** Vérifié exhaustivement dans `docs/04-matrice-recette.md` : REC-SCE-01 à REC-SCE-14 couvrent IMPORT (4 profils), EXPORT, Simulation, Customer Complaint, Enrôlement, Prise en charge, Transfert, Réaffectation, Temps/SLA, Supervision, Transverse — **aucun n'est consacré à Avoir/Exonération**, et aucune des 14 lignes ne cite `AVE-01` à `AVE-04` dans sa colonne "Exigences directes" ou "Partielle".

Confirmé indépendamment par `docs/03-revue-coherence-analyse.md` (constat E, ligne 514) : *"AVE : Aucun des 12 scénarios de recette du §22 ne couvre les avoirs/exonérations."*

**Ce n'est pas un oubli d'analyse — c'est une absence du CDC lui-même.** Aucun scénario n'est inventé ici pour combler ce vide.

---

## 8. Décision GO / NO-GO

Le périmètre fonctionnel n'est **pas** suffisamment clarifié pour écrire une spec sans inventer le workflow financier :
- 2 points bloquants **SPECIFICATION** confirmés indépendamment par 4 sources distinctes (CDC §25 lui-même, `02-points-a-clarifier.md`, `03-revue-coherence-analyse.md`, `09-etat-avancement-global.md`).
- Sur 11 étapes nécessaires à un workflow financier exploitable, 6 sont totalement absentes du CDC (voir §4).
- Aucun REC-SCE de référence pour valider la conformité une fois implémenté.
- Le seul acteur métier nommé sans ambiguïté sur l'ensemble du parcours AVE est le Client (AVE-01) — l'ensemble du côté "traitement interne" (instruction, validation, approbation) repose sur un rôle non défini.

**DÉCISION : NO-GO — BUSINESS CLARIFICATION REQUIRED**

---

## 9. Questions métier à poser (strictement bloquantes)

### Question 1 — Qui instruit et valide une demande Avoir/Exonération ?

- **Option A** : Un rôle applicatif dédié "Intervenant Avoir/Exo", avec sa propre file de traitement (même patron que "Support Opérationnel" pour F006).
- **Option B** : Le rôle "Caisse/Finance" existant (déjà confirmé pour IMP-13, validation de preuve de paiement) instruit aussi les AVE — pas de nouveau rôle.
- **Option C** : L'instruction est assignée à l'agent Facturation déjà en charge du ticket Billing concerné (si un lien existe via AVE-04), sans rôle dédié.
- **Recommandation technique** : Option A est la plus cohérente avec le patron déjà établi par F006 (`Complaint.Instruire`) et évite de surcharger un rôle existant avec une responsabilité financière distincte — mais c'est une préférence d'architecture, pas une décision métier.
- **Impact par option** : A nécessite la création d'un nouveau rôle/habilitation et une file dédiée (effort le plus élevé, le plus propre). B réutilise l'existant mais mélange deux responsabilités (paiement Billing vs décision financière AVE) qui n'ont pas la même portée. C évite tout nouveau rôle mais rend AVE dépendant d'un ticket Billing existant, ce qui contredit AVE-04 (liaison "Should", donc optionnelle).

### Question 2 — Combien de niveaux d'approbation, et existe-t-il un seuil de montant ?

- **Option A** : Un seul niveau d'approbation, quel que soit le montant.
- **Option B** : Deux niveaux — instruction puis validation finale par un second intervenant, systématiquement.
- **Option C** : Niveau variable selon un seuil de montant (ex. sous X FCFA = 1 niveau, au-delà = 2 niveaux avec escalade Direction Financière).
- **Recommandation technique** : aucune — c'est une décision purement métier/financière, sans élément CDC pour orienter un choix technique.
- **Impact par option** : A est le plus simple à implémenter (aucune notion de seuil dans le domaine). B ajoute une étape fixe. C nécessite un paramètre de seuil configurable (Admin fonctionnel) et une logique de branchement conditionnel dans le workflow — le CDC ne mentionne aucun seuil nulle part, donc si l'option C est retenue, le mécanisme de configuration du seuil devra lui-même être spécifié.

### Question 3 — La demande d'avoir a-t-elle un impact structurel sur la facture ou le paiement Billing (F003/F004) ?

- **Option A** : Aucun impact structurel — l'avoir est un document/décision autonome, purement informatif du point de vue du ticket Billing (la compensation financière réelle se fait hors plateforme, comme documenté pour F006 en frontière avec AVE).
- **Option B** : L'avoir modifie une facture existante (nouveau montant, avoir déductible sur prochaine facture).
- **Option C** : L'avoir déclenche un remboursement ou un crédit dans un système financier externe (non modélisé dans cette plateforme).
- **Recommandation technique** : Option A minimise le risque d'introduire une logique financière complexe (facturation, comptabilité) hors du périmètre déjà démontré comme sensible (cf. frontière AVE déjà tracée par F006 : *"Aucune entité Avoir, aucune file Finance n'est implémentée"*).
- **Impact par option** : A = aucune extension du modèle Billing. B = extension du modèle `Ticket`/Facture existant, risque de complexité comptable non couverte par le CDC. C = intégration externe non spécifiée, hors périmètre probable de la plateforme actuelle.

---

## 10. Roadmap alternative (F007 bloquée)

Comparaison des candidats proposés, sur la base des mêmes sources :

| Candidat | Exigences CDC | PC bloquant SPEC identifié | Prêt pour /speckit.specify ? |
|:---|:---|:---|:---|
| **Avoir & Exonération (AVE)** | 4 (module G) | PC-03, PC-15 | ❌ NON |
| **Enrôlement Transporteurs (ENR)** | 5 (module I) | Aucun — recherche exhaustive de "ENR" dans `docs/02-points-a-clarifier.md` : aucune occurrence | ✅ Probable (à confirmer par un preflight dédié, non fait ici) |
| **Notifications / Continuité (NOT/CON)** | 13 (module J&K, déjà 30 % couvert via F001 partiel) | PC-16 (fournisseur SMS/WhatsApp, 🟠 CRITIQUE mais "Bloque : PRODUCTION", pas SPECIFICATION), PC-17 (liste des événements, 🟡 IMPORTANT, "Bloque : IMPLEMENTATION") | ⚠️ Partiellement — aucun blocage de spec, mais 2 PC à lever avant implémentation/production |
| **Assistant IA** | 7 (module L) | PC-18 (gouvernance/déploiement, 🟡 IMPORTANT, "Bloque : SPECIFICATION") | ❌ NON (même nature de blocage que AVE, à un niveau moindre) |

**Recommandation** : `docs/09-etat-avancement-global.md` (§8, rédigé avant même le démarrage de F006) anticipait déjà ce blocage et recommandait explicitement l'ordre F006 → **F008 Enrôlement Transporteurs** en cas de blocage AVE — cette feuille de route est corroborée ici indépendamment : Enrôlement est le seul candidat sans aucun point bloquant identifié dans `docs/02-points-a-clarifier.md`.

**ALTERNATIVE RECOMMANDÉE : Enrôlement Transporteurs (ENR-01→04, module I)**, sous réserve d'un preflight dédié (non réalisé dans ce document, qui porte exclusivement sur AVE) pour confirmer l'absence de blocage similaire.

---

## VERDICT

```
FEATURE CANDIDATE : 007 — Avoir & Exonération
AVE : 4/4 analysées
POINTS BLOQUANTS : PC-03 (acteur "Intervenant Avoir/Exo" non défini), PC-15 (circuit de validation Finance non formalisé par le CDC lui-même) — les deux classés "Bloque : SPECIFICATION"
PC-03 : OUVERT — bloque SPEC
PC-15 : OUVERT — bloque SPEC (CDC §25 : "à formaliser avec la Finance")
REC-SCE : ABSENT — aucun scénario contractuel dédié AVE dans les 14 REC-SCE existants (confirmé CDC + coherence review)
DÉPENDANCES : techniquement prêtes (F001/F002/F003/F004/F003.5/F006 couvrent tous les besoins structurels) — blocage exclusivement fonctionnel, pas technique
GO / NO-GO : NO-GO — BUSINESS CLARIFICATION REQUIRED
ALTERNATIVE RECOMMANDÉE : Feature 008 — Enrôlement Transporteurs (aucun point bloquant identifié)
VERDICT : NO-GO
```

**STOP. Aucun `specs/007-*` créé. Aucun code écrit. Aucune feature fermée modifiée. Aucun commit. `/speckit.specify` non lancé. Feature 007 non commencée.**
