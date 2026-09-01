# Data Model — Feature 011 (Certification Sécurité & Socle NFR)

> **Phase 1.** Ce document existe pour **démontrer explicitement** qu'aucun changement de modèle n'est nécessaire — et non pour décrire un modèle.

---

## 1. Verdict

| Élément | Nombre |
|:---|:---:|
| **Nouvelles entités de domaine** | **0** |
| **Nouvelles tables** | **0** |
| **Nouvelles colonnes** | **0** |
| **Nouveaux index** | **0** |
| **Migrations** | **0** |
| **Modifications de propriétés existantes** | **0** |
| **Modifications de contraintes existantes** | **0** |

---

## 2. Démonstration exigence par exigence

Chaque exigence est examinée sous l'angle : *« l'état nécessaire à sa certification est-il déjà persisté ? »*

| Exigence | État nécessaire à la certification | Déjà persisté ? | Support existant | Besoin de modèle |
|:---|:---|:---:|:---|:---:|
| **SEC-01** | Compteur d'échecs et instant de fin de verrouillage sur le compte | ✅ | Champs du magasin d'identité (`AccessFailedCount`, `LockoutEnd`) — présents depuis la migration initiale du socle | **0** |
| **SEC-03** | Rattachement du ticket à son dossier et à son client ; habilitations par file | ✅ | Entités `Ticket`, `Dossier`, `HabilitationUtilisateur`, `UtilisateurSociete` (F001/F002) | **0** |
| **SEC-05** | Entrées d'audit immuables | ✅ | Entité `EntreeJournalAudit` (F001). L'immuabilité est une **garde applicative**, pas une contrainte de schéma | **0** |
| **SEC-10** | Aucun état persisté — la session vit dans un cookie signé, la durée dans la configuration | ✅ *(sans objet)* | Politique de cookie d'authentification | **0** |
| **SEC-11** | Indicateur d'activité du compte + historique produit par ce compte | ✅ | `ApplicationUser.EstActif` ; `EntreeHistorique`, `EntreeJournalAudit`, `Message` conservent l'auteur | **0** |
| **SEC-12** | Référence du ticket portée par la notification | ✅ | `Notification.TicketId` (F009) — **aucune URL stockée, aucun jeton** | **0** |
| **ERR-02** | Statut d'envoi par canal ; compteur de tentatives | ✅ | `EnvoiNotification.Statut`, `NombreTentatives`, `DerniereTentativeUtc` (F009). **La correction porte sur la validation de configuration, pas sur le modèle** | **0** |
| **ERR-03** | Règles de dépôt par étape (types, taille) | ✅ | `ReglesDepotDocument` — objet de configuration, **non persisté comme entité** ; documents rattachés au ticket | **0** |
| **NFR-01** | Aucun état — propriété des écrans rendus | ✅ *(sans objet)* | — | **0** |
| **NFR-02** | Aucun état — propriété de la mise en page | ✅ *(sans objet)* | — | **0** |
| **NFR-11** | Calendrier ouvré : heures, jours ouvrés, jours fériés | ✅ | `CalendrierOuvre` + paramètres SLA administrables (F001) — **les jours fériés sont déjà modélisés et paramétrables** | **0** |

**Aucune exigence du périmètre ne requiert d'état nouveau.**

---

## 3. Pourquoi c'était attendu

F011 est une **Feature de certification**. Par construction, elle vise des exigences **déjà servies par le socle** : si l'état nécessaire n'existait pas, l'exigence ne serait pas « déjà servie », et elle ne relèverait pas de la certification mais de la construction.

**Un besoin de modèle serait donc le signal que le périmètre a dérivé.**

---

## 4. Règle d'arrêt

> **Si l'implémentation conclut qu'une entité, une table, une colonne, un index ou une migration est nécessaire :**
>
> 1. **ARRÊTER** — ne pas créer l'objet ;
> 2. **RAPPORTER** l'écart, en nommant l'exigence concernée et l'état manquant ;
> 3. **RÉEXAMINER** le périmètre : soit l'exigence n'était pas « déjà servie » et sort de F011, soit la conception a dérivé vers de la construction.
>
> Aucune tâche ne doit contenir de commande de création de migration.

---

## 5. Contrôle de sortie

Contrôles à exécuter en fin d'implémentation :

| Contrôle | Attendu |
|:---|:---|
| `has-pending-model-changes` | **0** — inchangé par rapport à la baseline |
| Nombre de fichiers de migration | **inchangé** |
| Diff sur les entités de domaine | **aucune ligne** |
| Diff sur le contexte de persistance | **aucune ligne** |

Tout écart sur l'un de ces quatre contrôles invalide la nature de certification de la Feature.
