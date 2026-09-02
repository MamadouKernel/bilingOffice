# Plan de recette UAT

> **Document** : `docs/36-uat-master-test-plan.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Statut** : prêt à l'emploi — **exécution bloquée**, aucun environnement de recette n'existe.

---

## 1. Objectif

Faire valider par le métier, sur un environnement représentatif, que les **174 exigences
certifiées** se comportent conformément au CDC dans des conditions réelles d'usage.

La recette UAT ne rejoue pas les tests automatisés. Elle vérifie ce qu'ils ne peuvent pas
vérifier : l'enchaînement des parcours, la compréhensibilité des écrans, la pertinence des
messages, et le comportement en conditions dégradées.

---

## 2. Périmètre

### Dans le périmètre

Les **174 exigences certifiées**, regroupées en parcours métier de bout en bout.

Une attention particulière est portée aux **35 exigences qu'aucun test automatisé ne cite par
identifiant** (`docs/37`) : pour celles-là, le scénario UAT constitue la **seule preuve
opposable**.

### Hors périmètre

| Exigences | Motif |
|:---|:---|
| `NFR-03`, `NFR-04`, `NFR-10` | Relèvent d'une campagne de mesure, non d'une recette fonctionnelle — voir `docs/38` |
| `NFR-05`, `NFR-06`, `NFR-09` | Non implémentées, bloquées hors développement |
| `ENR-04` | Aucun fournisseur contractualisé |
| `IA-01` à `IA-07` | Non implémentées, `PC-18` OPEN |

---

## 3. Prérequis — tous bloquants

| Prérequis | Statut | Détenteur |
|:---|:---|:---|
| URL de l'environnement de recette | `TBD_UAT` | DSI / Exploitation |
| Certificat TLS valide | `TBD_INFRA` | Infrastructure |
| Base de données de recette | `TBD_INFRA` | Exploitation |
| Stockage documentaire opérationnel | `TBD_INFRA` | Exploitation |
| Antivirus actif sur les dépôts de fichiers | `TBD_INFRA` | Exploitation |
| Jeu de données de recette | `TBD_UAT` | Métier |
| Comptes de recette (un par profil) | `TBD_UAT` | Administration CIT |
| Recetteurs désignés et disponibles | `TBD_UAT` | Métier |
| Fenêtre de recette | `TBD_UAT` | Direction de projet |

**Aucune recette ne peut démarrer tant que ces neuf lignes ne sont pas renseignées.** Les recetter
sur un poste de développement ne prouverait rien : ni les volumes, ni les temps de réponse, ni les
droits ne sont représentatifs.

---

## 4. Profils à provisionner

Le CDC § 6 définit les profils. Chaque scénario est joué **avec le profil concerné**, jamais avec
un compte d'administration — un compte tout-puissant masquerait précisément ce que la recette
doit vérifier.

| Profil | Rôle en recette |
|:---|:---|
| Client (normal, en compte, deposit, IES) | Parcours de dépôt et de suivi |
| Transporteur / chauffeur | Enrôlement à distance |
| Pilote Manifeste | File Contrôle BL import |
| Back Office Export | File Codification export |
| Pilote Facturation Terre | Édition de facture |
| Comptable Client | Lettrage |
| Pilote Contrôle Documentaire | Documents finaux, clôture |
| Support Opérationnel | Réclamations |
| Agent Gate | Enrôlements |
| Superviseur | Supervision, réaffectation, disponibilité |
| Administrateur fonctionnel | Comptes, files, SLA, catégories, modèles |
| Administrateur technique | Journal d'audit |

---

## 5. Critères d'entrée

| Critère | Vérification |
|:---|:---|
| Version déployée identifiée | Commit affiché ou consigné |
| `/healthz` et `/ready` répondent `200` | `curl` |
| Migrations appliquées, aucune en attente | `dotnet ef migrations list` |
| Comptes de recette opérationnels | Connexion effective de chaque profil |
| Jeu de données chargé | Contrôle métier |

**Si un seul critère échoue, la recette ne démarre pas.** Recetter sur une base incomplète produit
des anomalies fantômes qui coûtent plus cher que le report.

---

## 6. Scénarios

Chaque scénario indique : profil, prérequis, actions, résultat attendu, criticité, preuve.

### S-01 — Ouverture d'un compte entreprise
**Profil** : visiteur, puis administrateur fonctionnel · **Criticité** : P1
1. Déposer une demande depuis la page publique, avec les justificatifs paramétrés.
2. Vérifier que la demande est **refusée si une pièce obligatoire manque**, et que la pièce est nommée.
3. Vérifier que le compte **ne peut pas se connecter** avant validation.
4. Valider la demande ; vérifier que le demandeur devient **administrateur de sa société**.
5. Vérifier qu'il reçoit une **notification** de la décision.
**Preuve** : captures des cinq étapes + entrée du journal d'audit.

### S-02 — Rejet motivé
**Profil** : administrateur fonctionnel · **Criticité** : P2
Rejeter une demande sans motif → refus. Avec motif → société refusée, demandeur notifié, compte toujours inutilisable.

### S-03 — Invitation d'un collaborateur
**Profil** : administrateur de société · **Criticité** : P1
Inviter une adresse, transmettre le lien, créer le compte, vérifier le rattachement **sans création d'une société supplémentaire**. Vérifier qu'une invitation expirée ou révoquée est refusée.

### S-04 — Enrôlement d'un transporteur
**Profil** : chauffeur, puis Agent Gate · **Criticité** : P1
Inscription à distance, constitution du dossier, dépôt des pièces, soumission, instruction, demande de complément, validation, convocation.

### S-05 — Parcours Billing Office import
**Profil** : client, puis les quatre profils agents · **Criticité** : P1
Dépôt de demande → contrôle BL → facturation → validation de paiement → contrôle documentaire → documents finaux → clôture. Vérifier le **cloisonnement** : chaque agent ne voit que sa file.

### S-06 — Parcours Billing Office export
**Profil** : client, puis agents export · **Criticité** : P1
Symétrique de S-05 sur la branche export, y compris la branche IES.

### S-07 — Réclamation client
**Profil** : client, puis Support Opérationnel · **Criticité** : P2
Dépôt, catégorisation, instruction, demande de complément, réponse, clôture.

### S-08 — Avoir et exonération
**Profil** : client, puis instructeur et décideur · **Criticité** : P2
Soumission, instruction, décision motivée. Vérifier la **séparation** entre instruire et décider.

### S-09 — Simulation de proforma
**Profil** : client · **Criticité** : P2
Simulation, contrôle du calcul contre la référence métier, sauvegarde, consultation de l'historique.

### S-10 — Autorisations et cloisonnement
**Profil** : tous · **Criticité** : **P1 — sécurité**
1. Un client ne voit **que** ses propres demandes.
2. Un collaborateur au périmètre restreint ne voit pas celles de ses collègues.
3. Un agent n'accède pas à une file non habilitée.
4. Un agent d'une société ne voit pas les dossiers d'une autre.
5. L'accès direct par URL à une ressource non autorisée est refusé.
**Preuve** : capture du refus pour chacun des cinq cas.

### S-11 — Dépôt de fichiers et antivirus
**Profil** : client · **Criticité** : **P1 — sécurité**
Type autorisé → accepté. Type interdit → refusé. Fichier trop volumineux → refusé. Fichier de test antivirus (EICAR) → **refusé, et non téléchargeable**.

### S-12 — Notifications
**Profil** : client et agent · **Criticité** : P2
Vérifier la réception In-App aux événements attendus, le compteur de non-lues, et qu'**aucun utilisateur ne voit les notifications d'autrui**.

### S-13 — SLA, alertes et calendriers
**Profil** : superviseur · **Criticité** : P2
Vérifier le déclenchement des seuils, les alertes, et la prise en compte des jours ouvrés.

### S-14 — Supervision et réaffectation
**Profil** : superviseur · **Criticité** : P2
Vue de charge, réaffectation, action de masse, disponibilité d'un opérateur, tickets d'un agent absent.

### S-15 — Administration des comptes agents
**Profil** : administrateur fonctionnel · **Criticité** : P1
Créer un agent, lui attribuer des rôles, en retirer un, **le désactiver et vérifier que son historique subsiste**.

### S-16 — Journal d'audit
**Profil** : administrateur technique · **Criticité** : **P1 — conformité**
Vérifier la présence des traces pour chaque action sensible, et qu'**aucune modification ni suppression n'est possible**.

### S-17 — Rétention et gel légal
**Profil** : administrateur · **Criticité** : P1
Vérifier l'application des durées de rétention et qu'un **gel légal empêche la purge**.

### S-18 — Brouillons et reprise de saisie
**Profil** : client · **Criticité** : P3
Saisie partielle, abandon, reprise sans perte.

### S-19 — API
**Profil** : client authentifié · **Criticité** : P2
Consultation d'un ticket et d'un dossier. Vérifier le **refus** sur une ressource d'une autre société, et sur un appel non authentifié.

### S-20 — Exports
**Profil** : agent habilité · **Criticité** : P3
Export CSV, contrôle du contenu et de l'encodage.

### S-21 — Bilinguisme
**Profil** : client · **Criticité** : P3
Bascule de langue, contrôle qu'aucun libellé ne reste non traduit.

### S-22 — Mode maintenance
**Profil** : administrateur, puis client · **Criticité** : P2
Activation, message affiché, en-tête `Retry-After`, retour au nominal complet.

### S-23 — Récupération de compte
**Profil** : client · **Criticité** : P2
Mot de passe oublié, réinitialisation, verrouillage après cinq tentatives.

### S-24 — Double authentification
**Profil** : tout utilisateur · **Criticité** : P2
Activation, QR code affiché **sans appel à un service tiers**, connexion avec second facteur, désactivation.

---

## 7. Classification des anomalies

| Niveau | Définition | Effet sur le GO |
|:---|:---|:---|
| **P1** | Bloque un parcours métier, ou expose une donnée à qui ne doit pas la voir | **NO-GO** |
| **P2** | Dégrade fortement un parcours ; contournement lourd | GO conditionnel, correction planifiée |
| **P3** | Gêne d'usage, libellé, ergonomie | GO, correction ultérieure |
| **P4** | Cosmétique | GO |

**Toute anomalie de cloisonnement ou d'autorisation est P1 par construction**, quelle que soit sa
facilité de contournement.

---

## 8. Critères de sortie

| Critère | Seuil |
|:---|:---|
| Scénarios P1 exécutés | 100 % |
| Anomalies P1 ouvertes | **0** |
| Anomalies P2 ouvertes | Acceptées explicitement par le métier, avec échéance |
| Preuves collectées | Une par scénario |
| Validation métier | Signature du responsable désigné |

---

## 9. Preuves

Pour chaque scénario : horodatage, profil utilisé, **commit déployé**, captures des étapes clés,
et référence de l'entrée d'audit lorsqu'elle existe.

**Sans le commit déployé, une preuve de recette n'est pas opposable** : on ne sait pas ce qui a
été recetté.

---

## 10. Responsabilités

| Rôle | Responsable |
|:---|:---|
| Pilotage de la recette | `TBD_UAT` |
| Exécution des scénarios | `TBD_UAT` — métier |
| Mise à disposition de l'environnement | `TBD_INFRA` |
| Correction des anomalies | Équipe de développement |
| Décision GO / NO-GO | `TBD_UAT` — direction de projet |

---

## 11. Verdict

**Plan prêt. Recette non lançable.**

Le blocage est unique : `BLOCKED_ENVIRONMENT`. Les 24 scénarios sont écrits et exécutables dès que
les neuf prérequis du § 3 seront levés.
