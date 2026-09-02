# Matrice d'accès de recette

> **Document** : `docs/62-uat-access-matrix.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Source des codes** : `Domain/Securite/NatureUtilisateur.cs` — **54 permissions**, vérifiées une
> à une contre le code.

---

## 1. Ce que cette matrice est, et ce qu'elle n'est pas

La plateforme **ne connaît aucun rôle métier**. Le socle n'expose que des **permissions** attribuées
individuellement (`decision D4` ; `PC-03` reste non arbitré). Les intitulés de profils ci-dessous
sont des **regroupements de recette**, pas des objets de l'application.

Ce document ne crée donc pas de rôles : il indique **quelles permissions attribuer à quel compte de
recette** pour que chaque scénario de `docs/36` soit jouable par le bon profil.

**Aucun de ces regroupements ne vaut décision d'habilitation pour la production.** Le profilage
définitif relève de `PC-03`.

---

## 2. Regroupements de recette

### 2.1 Clients — comptes de nature `Client`

| Compte | Permissions | Nombre |
|:---|:---|---:|
| `client.normal.1` et `client.normal.2` | `ticket.creer`, `document.deposer`, `document.telecharger`, `billing_import.soumettre`, `billing_export.soumettre`, `complaint.soumettre`, `compte.gerer_collaborateurs` | 7 |
| `client.encompte` | Identiques | 7 |
| `client.deposit` | Identiques | 7 |
| `client.ies` | Identiques | 7 |
| `client.collab.restreint` | `ticket.creer`, `document.deposer`, `document.telecharger`, `billing_import.soumettre` — **sans** `compte.gerer_collaborateurs` | 4 |
| `client.collab.societe` | Idem, périmètre société | 4 |
| `transporteur.1` et `transporteur.2` | `enrolement.soumettre` **uniquement** | 1 |

**Le compte transporteur n'a qu'une permission.** C'est délibéré : le parcours ENR-01 est public et
n'ouvre aucun autre accès. Si un transporteur de recette voit autre chose que son enrôlement, c'est
un défaut.

**`compte.gerer_collaborateurs` distingue les deux comptes collaborateurs** : c'est le point exact
que le scénario ENT-02 doit vérifier.

### 2.2 Agents opérationnels — comptes de nature `Agent`

| Compte | Permissions | Nombre |
|:---|:---|---:|
| `agent.manifeste` | `ticket.prendre_en_charge`, `ticket.transferer`, `message.note_interne`, `document.telecharger`, `billing_import.manifeste.instruire`, `billing_import.ies.manifeste.instruire`, `guichet.recherche` | 7 |
| `agent.backoffice.export` | `ticket.prendre_en_charge`, `ticket.transferer`, `message.note_interne`, `document.telecharger`, `billing_export.back_office.instruire`, `billing_export.ies.back_office.instruire`, `guichet.recherche` | 7 |
| `agent.facturation` | `ticket.prendre_en_charge`, `ticket.transferer`, `message.note_interne`, `document.telecharger`, `billing_import.facturation.editer`, `billing_export.facturation.editer` | 6 |
| `agent.comptable` | `ticket.prendre_en_charge`, `ticket.transferer`, `message.note_interne`, `document.telecharger`, `billing_import.paiement.valider`, `billing_import.comptabilite.confirmer_lettrage`, `billing_export.paiement.valider`, `billing_export.comptabilite.confirmer_lettrage` | 8 |
| `agent.controle.doc` | `ticket.prendre_en_charge`, `ticket.cloturer`, `message.note_interne`, `document.deposer`, `document.telecharger`, `billing_import.controle_documentaire.traiter`, `billing_import.ies.controle_documentaire.instruire`, `billing_export.controle_documentaire.traiter`, `billing_export.ies.controle_documentaire.instruire` | 9 |
| `agent.support` | `ticket.prendre_en_charge`, `ticket.cloturer`, `ticket.cloturer_absence_reponse`, `ticket.rouvrir`, `message.note_interne`, `document.telecharger`, `complaint.instruire`, `complaint.repondre`, `avoirexoneration.instruire`, `guichet.recherche`, `rendezvous.gerer` | 11 |
| `agent.gate` | `enrolement.instruire`, `document.telecharger`, `message.note_interne`, `guichet.recherche`, `rendezvous.gerer` | 5 |
| **`agent.sansdroit`** | **aucune** | **0** |

### 2.3 Encadrement et administration

| Compte | Permissions | Nombre |
|:---|:---|---:|
| `superviseur` | `supervision.consulter`, `supervision.agir`, `supervision.action_de_masse`, `ticket.affecter_manuellement`, `ticket.reaffecter`, `ticket.annuler`, `priorite.modifier`, `disponibilite.gerer` | 8 |
| `admin.fonctionnel` | `administration.comptes`, `administration.files`, `administration.sla`, `administration.perimetres_supervision`, `administration.categories_reclamation`, `administration.notifications`, `compte.valider`, `compte.type_modifier`, `competence.gerer`, `tarification.administrer`, `avoirexoneration.decider` | 11 |
| `admin.technique` | **`audit.consulter`** — et rien d'autre | 1 |

---

## 3. Deux séparations à ne pas effacer par confort

### `audit.consulter` reste seul sur l'administrateur technique

Le CDC § 6 confie la consultation du journal d'audit à l'**administrateur technique**, distinct de
l'administrateur fonctionnel. Réunir les deux sur un même compte de recette rendrait la séparation
intestable, et masquerait précisément ce que le CDC demande de garantir : que celui qui administre
les comptes ne soit pas celui qui relit les traces de ses propres actions.

### `agent.sansdroit` doit rester à zéro permission

C'est le seul compte qui démontre que le contrôle d'accès **refuse**. Lui accorder « juste
`document.telecharger` pour qu'il puisse se connecter » le viderait de son objet.

---

## 4. Couverture des 54 permissions

| Permission peu visible | Compte porteur |
|:---|:---|
| `ticket.affecter_manuellement` | `superviseur` |
| `ticket.annuler` | `superviseur` |
| `ticket.rouvrir` | `agent.support` |
| `ticket.cloturer_absence_reponse` | `agent.support` — et le service de relance automatique |
| `avoirexoneration.instruire` | `agent.support` |
| `avoirexoneration.decider` | `admin.fonctionnel` |
| `enrolement.soumettre` | `transporteur.1` et `transporteur.2` |
| `complaint.repondre` | `agent.support` |

**Les 54 permissions sont couvertes par au moins un compte de recette.** Aucune ne reste intestable.

---

## 5. Contrôles négatifs — obligatoires

Une matrice qui ne vérifie que les autorisations ne prouve rien. Les scénarios suivants doivent
**échouer** :

| # | Tentative | Attendu |
|---:|:---|:---|
| 1 | `agent.sansdroit` ouvre une file de traitement | **Refus** |
| 2 | `client.normal.1` accède à un dossier de `client.normal.2` | **Refus** — SEC-03 |
| 3 | `client.collab.restreint` accède à la demande d'un autre collaborateur | **Refus** — ENT-03 |
| 4 | `agent.facturation` tente une action de manifeste | **Refus** |
| 5 | `admin.fonctionnel` ouvre le journal d'audit | **Refus** — pas de `audit.consulter` |
| 6 | `admin.technique` administre les files | **Refus** |
| 7 | `agent.gate` accède à un dossier Billing | **Refus** |
| 8 | `transporteur.1` accède à un ticket | **Refus** |
| 9 | Un compte désactivé se connecte | **Refus** — SEC-11 |
| 10 | Un client d'une société non validée accède au portail | **Refus** — CPT-01 |
| 11 | Appel API sans authentification | **`401`** |
| 12 | `admin.fonctionnel` consulte la notification d'un tiers | **Refus** — aucune permission ne l'autorise, par conception |

**Le contrôle 12 mérite attention** : aucune permission de lecture des notifications d'autrui
n'existe dans le code, et il ne doit jamais en exister. Le vérifier en recette protège cette
propriété d'une régression future.

---

## 6. Attribution

Les habilitations s'attribuent depuis l'écran d'administration des comptes agents, avec le compte
`admin.fonctionnel` — lui-même créé depuis l'administrateur initial (`docs/55`).

Un code de permission inconnu est **refusé à l'attribution** : il n'accorderait rien tout en
paraissant accordé.

---

## 7. Consignation

Pour chaque compte : identifiant, permissions effectivement attribuées, date, **et l'entrée du
journal d'audit correspondante**. L'attribution d'une habilitation est elle-même un acte tracé — la
recette doit le constater.

**Aucun mot de passe ne figure dans ce document ni dans aucun autre document du dépôt.**
