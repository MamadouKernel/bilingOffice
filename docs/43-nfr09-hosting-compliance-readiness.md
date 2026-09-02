# Hébergement et conformité Groupe — NFR-09

> **Document** : `docs/43-nfr09-hosting-compliance-readiness.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Priorité corrigée** : **NFR-09 = Should** (les documents antérieurs indiquaient « Must »).
> **Statut** : `BLOCKED_DSI` — `PC-13` OPEN.

---

## 1. Ce que le CDC exige

> **NFR-09 (Should)** — « Hébergement conforme aux standards du groupe ; le choix (infrastructure
> [...]) »

L'exigence est **organisationnelle**. Elle ne demande aucun développement : elle demande que
l'hébergement retenu respecte des standards que le CDC ne détaille pas et qui appartiennent au
Groupe.

`PC-13` (niveau critique, bloque l'architecture) porte précisément ce point.

---

## 2. Ce que l'équipe technique ne peut pas décider

Le choix d'hébergement engage la souveraineté des données, la conformité réglementaire et le
modèle d'exploitation. **Aucun critère technique ne le tranche** — une préférence exprimée ici
serait déplacée.

Ce document se limite donc à énoncer les questions et ce que chaque réponse impliquerait.

---

## 3. Checklist d'homologation

### 3.1 Localisation et architecture

| Question | Valeur | Implication |
|:---|:---|:---|
| Hébergement sur site CIT, datacenter Groupe / AGL, ou cloud ? | `TBD_DSI` | Détermine tout le reste |
| Localisation géographique des données | `TBD_DSI` | Conformité ARTCI et RGPD |
| Machine virtuelle, matériel dédié, conteneurs, orchestrateur ? | `TBD_INFRA` | Modèle de déploiement |
| Système d'exploitation | `TBD_INFRA` | .NET 10 est multiplateforme — sans contrainte applicative |
| Haute disponibilité attendue ? | `TBD_DSI` | Conditionne NFR-03 et NFR-10 |
| Site de repli ? | `TBD_DSI` | Lié au PRA — `docs/41` |

### 3.2 Base de données

| Question | Valeur |
|:---|:---|
| Version SQL Server cible | `TBD_INFRA` |
| Instance dédiée ou mutualisée | `TBD_INFRA` |
| Haute disponibilité — cluster, groupe de disponibilité, réplication | `TBD_INFRA` |
| Compte de service dédié, non `sa` | `TBD_INFRA` — **recommandé** |
| Chiffrement au repos | `TBD_DSI` |

### 3.3 Réseau et exposition

| Question | Valeur |
|:---|:---|
| Segmentation — DMZ, zone applicative, zone base | `TBD_INFRA` |
| Reverse proxy | `TBD_INFRA` |
| Pare-feu applicatif | `TBD_DSI` |
| Règles de pare-feu | `TBD_INFRA` |
| DNS et nom de service | `TBD_INFRA` |
| TLS — version minimale, suites autorisées | `TBD_DSI` |
| Autorité de certification | `TBD_INFRA` |

### 3.4 Identité, accès et secrets

| Question | Valeur |
|:---|:---|
| Comptes de service applicatifs | `TBD_INFRA` |
| Accès administrateur aux serveurs | `TBD_INFRA` |
| Second facteur pour les accès d'administration | `TBD_DSI` |
| Mécanisme de gestion des secrets | `TBD_DSI` — **conditionne le déploiement** |
| Rotation des secrets | `TBD_DSI` |

**Point bloquant concret.** L'application attend sa chaîne de connexion et le mot de passe
d'amorçage **hors du dépôt**. Le mécanisme retenu — coffre, variables d'environnement, secrets
d'orchestrateur — doit être arrêté avant tout déploiement.

### 3.5 Exploitation

| Question | Valeur |
|:---|:---|
| Supervision — outil | `TBD_INFRA` |
| Centralisation des journaux | `TBD_INFRA` |
| Rétention des journaux | `TBD_DSI` |
| Alerting et astreinte | `TBD_DSI` |
| Politique de correctifs | `TBD_INFRA` |
| Antivirus serveur | `TBD_INFRA` |
| Sauvegarde | `TBD_DSI` — `docs/41` |

### 3.6 Conformité Groupe

| Question | Valeur |
|:---|:---|
| Référentiel de standards applicable | `TBD_DSI` |
| Processus d'homologation | `TBD_DSI` |
| Audit de sécurité préalable exigé ? | `TBD_DSI` |
| Validation DPO — données personnelles | `TBD_DSI` |

---

## 4. Ce que l'application impose, quel que soit le choix

Ces contraintes sont **déduites du code** et s'appliquent à toutes les options.

| Contrainte | Origine |
|:---|:---|
| Runtime .NET 10 | Cible des projets |
| SQL Server | Fournisseur EF Core retenu |
| Système de fichiers accessible en écriture | Stockage documentaire |
| Chaîne de connexion hors dépôt | Conception |
| HTTPS, avec en-têtes de transfert si proxy | `UseHttpsRedirection` |
| Migrations exécutées hors démarrage | Conception — `docs/40` |

Aucune n'oriente vers une option particulière : **le choix reste entier**.

---

## 5. Verdict

**NFR-09 : `BLOCKED_DSI` — `PC-13` OPEN.**

L'exigence est **Should** — sa correction de priorité la rend moins contraignante que ce qui était
annoncé, mais elle **bloque en cascade** des exigences Must : sans hébergement, pas de Staging,
donc pas de certification NFR-03, NFR-04 ni NFR-10.

C'est le blocage dont la levée débloque le plus d'autres.
