# Phase 1 — Guide de Validation : Feature 009

> **Document** : `specs/009-notifications-multicanales/quickstart.md`
> **Date** : 1er septembre 2026
> **Nature** : guide de **validation exécutable**. Il ne contient aucun code d'implémentation — les détails de conception sont dans [data-model.md](./data-model.md) et [contracts/use-cases.md](./contracts/use-cases.md).

---

## Prérequis

1. SQL Server de développement démarré (conteneur existant du projet).
2. Migration F009 appliquée (data-model.md §7).
3. Seed de développement exécuté — règles de diffusion **de niveau CDC-EXIGENCE uniquement**.
4. Comptes de test : `docs/09-comptes-test-development.md` — **client A**, **client B** (sociétés distinctes), **agent rôle 1**, **agent rôle 2**, **admin fonctionnel**.
5. Paramètres de reprise renseignés en configuration — **sans valeur, le démarrage doit échouer explicitement** (research.md D9).

```bash
dotnet build
```

```bash
dotnet test
```

---

## Vérification préalable — non-régression

F009 touche trois features fermées de façon additive (research.md D2, D7, D14). **Avant tout commit**, la baseline doit rester verte :

```bash
dotnet test --filter "FullyQualifiedName~Tickets|FullyQualifiedName~BillingImport|FullyQualifiedName~BillingExport|FullyQualifiedName~Messagerie"
```

```bash
dotnet ef migrations has-pending-model-changes --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

> Attendu : suites vertes, et **aucun changement de modèle en attente** après application de la migration F009.

---

## Parcours 1 — Centre de notifications et état Lu/Non lu (`TEST-NOT-01`)

**Exigences** : NOT-01 (Must), NOT-03 (Should), COM-02

| Étape | Action | Résultat attendu |
|:--|:---|:---|
| 1 | Se connecter en **client A** et soumettre une demande | Ticket créé ; l'événement `TicketCree` est publié |
| 2 | Attendre un tour de distribution | Une notification apparaît, à l'état **Non lu** |
| 3 | Observer la cloche de l'en-tête | Le **badge affiche le nombre exact** de non-lues (NOT-03) |
| 4 | Ouvrir le centre de notifications | Chaque ligne affiche **état, date, type, titre** et un **lien vers le ticket** |
| 5 | Marquer une notification comme lue | L'état passe à **Lu** ; le badge **décroît de 1** |
| 6 | Re-consulter cette notification et tenter de la remarquer comme lue | Elle reste consultable, l'état demeure **Lu**, le badge est inchangé, **aucune erreur n'est affichée** |
| 7 | Suivre le lien | Le **ticket concerné** s'ouvre |

**Résultat attendu global** : liste, état, compteur et lien conformes à NOT-01 et NOT-03.

---

## Parcours 2 — Administration des règles et des modèles (`TEST-NOT-02`)

**Exigences** : COM-03 (Must), NOT-02 (Must), COM-05 (Should)

| Étape | Action | Résultat attendu |
|:--|:---|:---|
| 1 | Se connecter en **admin fonctionnel** ; ouvrir l'administration des règles de diffusion | Les règles semées sont listées |
| 2 | Ajouter le canal **E-mail** à un événement qui n'était diffusé qu'en In-App, puis déclencher cet événement | La notification est produite **sur les deux canaux**, **sans redéploiement** |
| 3 | Retirer le canal E-mail, puis re-déclencher | **Un seul** canal servi ; aucun envoi e-mail créé |
| 4 | Modifier le modèle d'un événement, puis consulter une notification **déjà émise** | Son titre et son corps sont **inchangés** — le rendu est figé à la création |
| 5 | Modifier la règle de l'événement E1, puis déclencher E2 | E2 est diffusé **selon sa propre règle** — E1 ne l'affecte pas |
| 6 | Se déconnecter, se reconnecter en **client A**, tenter d'atteindre l'administration | **Refus côté serveur**, indépendamment de ce que l'interface propose |
| 7 | Consulter le journal d'audit en admin | Les modifications sont tracées avec **auteur** et valeurs **avant / après** |

---

## Parcours 3 — Préférences de canaux et notification obligatoire (`TEST-NOT-02` cas 7)

**Exigence** : COM-05 (Should)

| Étape | Action | Résultat attendu |
|:--|:---|:---|
| 1 | En **client A**, désactiver le canal E-mail dans ses préférences | Préférence enregistrée et tracée |
| 2 | Déclencher un événement dont la règle est **non obligatoire** | **Aucun e-mail** ; la notification In-App reste produite |
| 3 | Déclencher un événement dont la règle est **obligatoire** (ex. `TicketCree` — SOC-01) | L'e-mail est transmis **malgré** la désactivation |
| 4 | Tenter de désactiver le canal **In-App** | **Impossible** — le centre de notifications n'est pas débrayable |

---

## Parcours 4 — Échec d'envoi, reprise et isolation (`TEST-ERR-02`)

**Exigence** : ERR-02 (Must, transverse) · CDC §20

| Étape | Action | Résultat attendu |
|:--|:---|:---|
| 1 | Configurer un événement sur trois canaux, dont un rendu indisponible | — |
| 2 | Déclencher l'événement | Les **deux canaux disponibles aboutissent** ; le troisième échoue **sans les empêcher** |
| 3 | Inspecter les envois | Chaque canal porte un **statut d'envoi distinct** (CDC §20) |
| 4 | Inspecter la trace | Destinataire, canal, horodatage et résultat sont tracés ; **le contenu du message n'apparaît pas** |
| 5 | Attendre un tour de reprise | Une **nouvelle tentative est engagée automatiquement**, sans action humaine ; `NombreTentatives` progresse |
| 6 | Vérifier la transaction métier d'origine | Elle **reste valide et committée** — l'échec ne l'a ni annulée ni bloquée |
| 7 | Vérifier l'absence de suppléance | **Aucune substitution** vers un autre canal n'a eu lieu |

> Les valeurs de reprise proviennent de la configuration. **Ce parcours ne valide aucune valeur chiffrée** — il valide le comportement (CLAR-NOT-02).

---

## Parcours 5 — Raccordement de la mention `@Nom` (`TEST-MSG-04`)

**Capacité** : NOT-01 appliquée à un destinataire **agent** · **Raccordement de MSG-04 (exigence F001, non recomptée)** · Décision **Option B**

| Étape | Action | Résultat attendu |
|:--|:---|:---|
| 1 | En **agent rôle 1**, publier une **note interne** mentionnant **agent rôle 2** | — |
| 2 | Se connecter en **agent rôle 2** | Une **notification In-App** est présente, à l'état **Non lu**, avec date, type, titre et **lien vers le ticket** ; son badge est incrémenté |
| 3 | Vérifier les autres agents habilités du ticket | **Aucun** ne reçoit cette notification — seul l'agent mentionné |
| 4 | Se connecter en **client A** (propriétaire du ticket) | **Aucune notification** liée à cette note interne, sur **aucun canal** (MSG-02) |
| 5 | Rouvrir le ticket en agent | Le ticket **reste affecté à son opérateur courant** (MSG-05) |
| 6 | Comparer fil du ticket et centre de notifications | La note reste dans le **fil unique** ; la notification la **signale sans la dupliquer** |
| 7 | Activer la règle `(CollaborateurMentionne, Email)` en admin, puis republier une mention | Un e-mail est émis **en plus** — jamais avant activation |
| 8 | Provoquer un dépassement de SLA déclenchant une escalade | **Aucune notification de supervision** n'est produite ; le mécanisme F001 s'exécute **inchangé** |

---

## Parcours 6 — Sécurité et cloisonnement (`TEST-NOT-01` cas 5→8)

**Exigences** : SEC-12, CPT-04, SEC-01 · `spec.md` §14

| Étape | Action | Résultat attendu |
|:--|:---|:---|
| 1 | Non authentifié, ouvrir le centre de notifications | Accès refusé, invitation à s'authentifier ; **aucun détail exposé** |
| 2 | Non authentifié, ouvrir un lien de notification | Conduite au contrôle d'identité ; **aucune donnée du dossier, aucune pièce protégée** avant authentification |
| 3 | En **client B**, tenter d'accéder à une notification de **client A** en connaissant son identifiant | **Refus côté serveur** ; elle n'apparaît ni dans sa liste ni dans son compteur ; son existence n'est pas divulguée |
| 4 | Idem pour le marquage comme lue | **Refus côté serveur** |
| 5 | En **client B**, consulter son centre | **Aucune** notification relative à la société de client A |
| 6 | Retirer l'habilitation sur un ticket, puis ouvrir la notification correspondante | Refus **propre** et explicite, **sans divulguer** l'existence, le contenu ni l'appartenance du ticket |
| 7 | En **admin fonctionnel**, tenter de consulter les notifications d'un client | **Refus** — l'administration porte sur le paramétrage, jamais sur les notifications d'autrui |

---

## Parcours 7 — Relances (`TEST-COM-06`, conditionnel)

**Exigence** : COM-06 (Could) — dimension **canal** de CYC-04

| Étape | Action | Résultat attendu |
|:--|:---|:---|
| 1 | Placer un ticket en attente client au-delà du délai paramétré | Le mécanisme **F001** émet la relance |
| 2 | Avec une règle configurée pour `RelanceEmise` | Une notification de rappel est produite **sur les canaux configurés** et **tracée** |
| 3 | Modifier les canaux, provoquer une relance ultérieure | Elle emprunte **les nouveaux canaux**, sans redéploiement |
| 4 | **Retirer toute règle** pour `RelanceEmise`, provoquer une relance | Le mécanisme CYC-04 **s'exécute normalement** (relance puis suggestion de clôture) ; l'absence de canal **ne le bloque pas** |

> Si COM-06 est différée, ce parcours n'est pas joué et l'écart est **tracé comme dette**, jamais silencieux.

---

## Couverture REC-SCE-14 — partielle par construction

`spec.md` §18.1 répartit le scénario contractuel entre F009 et F010. Ce guide couvre **les volets F009 uniquement** :

| Volet REC-SCE-14 | Parcours | Statut |
|:---|:---|:---|
| *« Notifications sur chaque canal configuré »* | 2, 3, 4 | ✅ Couvrable — sur les canaux effectivement configurés |
| *« …avec lien vers le ticket »* | 1, 6 | ✅ Couvrable |
| *« contrôle des habilitations »* (volet notifications) | 6 | ✅ Couvrable |
| *« journal d'audit »* (volet notifications) | 2, 4 | ✅ Couvrable |
| *« recherche de ticket au guichet »* | — | ❌ **Feature 010** |
| *« rendez-vous donnant accès au bureau de facturation »* | — | ❌ **Feature 010** |
| *« regroupement des tickets en dossier »* | — | ✅ Déjà acquis (F001), non rejoué ici |

> **REC-SCE-14 ne peut pas être déclaré entièrement rejoué à l'issue de F009.** Deux de ses volets dépendent de CON-01/02/03.

---

## Validation finale

```bash
dotnet test
```

```bash
dotnet build --no-incremental
```

Critères de sortie : suite complète verte, **0 warning**, **0 changement de modèle en attente**, parcours 1 à 6 rejoués (7 si COM-06 est retenue), et suites de régression Tickets / BillingImport / BillingExport / Messagerie vertes après les trois extensions additives.
