# Index du dossier de livraison

> **Document** : `docs/51-delivery-package-index.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Objet** : recenser ce qu'il faut transmettre pour qu'une équipe tierce puisse exploiter,
> maintenir et faire évoluer la plateforme.

---

## Légende

| Statut | Signification |
|:---|:---|
| `AVAILABLE` | Existe et est utilisable en l'état |
| `TO_COMPLETE` | Existe, mais incomplet ou à mettre à jour |
| `BLOCKED_DSI` | Ne peut être produit sans arbitrage |
| `BLOCKED_ENVIRONMENT` | Ne peut être produit sans environnement |

---

## 1. Contractuel et conformité

| Document | Emplacement | Statut |
|:---|:---|:---|
| Cahier des charges | `docs/cdc_text.txt`, `docs/CDC_..._v3.docx` | `AVAILABLE` |
| Matrice des exigences | `docs/01-matrice-exigences.md` | `AVAILABLE` |
| Points à clarifier | `docs/02-points-a-clarifier.md` | `AVAILABLE` |
| **Matrice CDC post-F017** | `docs/33-post-f017-global-cdc-matrix.md` | `AVAILABLE` |
| Traçabilité exigences ↔ preuves | `docs/37-uat-requirements-traceability-matrix.md` | `AVAILABLE` |
| Registre des décisions DSI | `docs/46-dsi-decision-register.md` | `AVAILABLE` |

## 2. Spécifications

| Élément | Emplacement | Statut |
|:---|:---|:---|
| Specs des features 001 à 015 | `specs/001` à `specs/015` | `AVAILABLE` — **non versionnées depuis `bf8c327`** |
| Spec F016 | `specs/016-...` | `AVAILABLE` — **jamais versionnée** |
| Spec F017 | `specs/017-...` | `AVAILABLE` — **jamais versionnée** |
| Rapports de clôture | `specs/*/implementation-report.md` | `AVAILABLE` — idem |

> ⚠️ **Réserve majeure.** 266 fichiers de `docs/` et `specs/` ne sont pas suivis par Git, dont les
> spécifications et rapports de clôture de **deux features closes**. Ils n'existent que sur le
> poste de travail. Détail et options : `docs/34-git-governance-audit.md`.

## 3. Architecture et code

| Élément | Emplacement | Statut |
|:---|:---|:---|
| Code source | `src/`, `tests/` | `AVAILABLE` — versionné, publié |
| Description de l'architecture | `README.md` | `AVAILABLE` |
| Audit sécurité et architecture | `docs/35-security-architecture-hardening-audit.md` | `AVAILABLE` |
| Audit de gouvernance Git | `docs/34-git-governance-audit.md` | `AVAILABLE` |
| Diagramme d'architecture détaillé | — | `TO_COMPLETE` |

## 4. Base de données

| Élément | Emplacement | Statut |
|:---|:---|:---|
| Migrations EF Core (25) | `src/CIT.Plateforme.Infrastructure/Persistence/Migrations/` | `AVAILABLE` |
| Runbook de migration | `docs/40-database-migration-runbook.md` | `AVAILABLE` |
| Dictionnaire de données | — | `TO_COMPLETE` |
| Script de création initial | Généré à la demande | `AVAILABLE` |

## 5. Tests et preuves

| Élément | Emplacement | Statut |
|:---|:---|:---|
| Suites automatisées — 1041 tests | `tests/` | `AVAILABLE` |
| Plan de recette UAT | `docs/36-uat-master-test-plan.md` | `AVAILABLE` |
| Preuves de recette | — | `BLOCKED_ENVIRONMENT` |
| Preuves F016 | `specs/016-.../evidence/` | `TO_COMPLETE` — **`NOT EXECUTED`** |
| Rapport de charge NFR-05 | — | `BLOCKED_DSI` — PC-01 |

## 6. Déploiement et exploitation

| Document | Emplacement | Statut |
|:---|:---|:---|
| Checklist Staging | `docs/39-staging-readiness-checklist.md` | `AVAILABLE` |
| Runbook de déploiement | `docs/48-production-deployment-runbook.md` | `AVAILABLE` |
| Runbook de certification F016 | `docs/38-f016-environment-certification-runbook.md` | `AVAILABLE` |
| Runbook d'exploitation et support | `docs/49-operations-support-runbook.md` | `AVAILABLE` |
| Checklist GO / NO-GO | `docs/47-go-no-go-checklist.md` | `AVAILABLE` |
| Procédure de retour arrière | `docs/40`, `docs/48` | `AVAILABLE` |
| Configuration de production | — | `BLOCKED_DSI` — PC-13 |

## 7. Sécurité

| Document | Emplacement | Statut |
|:---|:---|:---|
| Checklist de sécurité opérationnelle | `docs/50-security-operations-checklist.md` | `AVAILABLE` |
| Homologation infrastructure | `docs/43-nfr09-hosting-compliance-readiness.md` | `BLOCKED_DSI` |
| Rapport de test d'intrusion | — | `BLOCKED_ENVIRONMENT` |
| Analyse des dépendances vulnérables | — | `TO_COMPLETE` |

## 8. Continuité

| Document | Emplacement | Statut |
|:---|:---|:---|
| Sauvegarde, restauration, PRA | `docs/41-backup-restore-pra-readiness.md` | `BLOCKED_DSI` |
| Preuve de test de restauration | — | `BLOCKED_ENVIRONMENT` |

## 9. Points en attente

| Document | Emplacement | Statut |
|:---|:---|:---|
| Charge et volumétrie — NFR-05 | `docs/42-nfr05-load-test-readiness.md` | `BLOCKED_DSI` — PC-01 |
| Hébergement — NFR-09 | `docs/43-...` | `BLOCKED_DSI` — PC-13 |
| Fournisseur SMS/WhatsApp — ENR-04 | `docs/44-enr04-provider-readiness.md` | `BLOCKED_DSI` — PC-16 |
| Assistant conversationnel — IA-01..07 | `docs/45-assistant-conversationnel-readiness.md` | `BLOCKED_DSI` — PC-18 |

## 10. Exploitation courante

| Élément | Statut |
|:---|:---|
| Notes de version | `TO_COMPLETE` — aucune livraison |
| Comptes de service | `BLOCKED_DSI` |
| Inventaire des flux et des ports | `TO_COMPLETE` |
| Documentation de l'API | `TO_COMPLETE` |
| Manuel utilisateur | `TO_COMPLETE` |

---

## Synthèse

| Statut | Nombre |
|:---|---:|
| `AVAILABLE` | **24** |
| `TO_COMPLETE` | 8 |
| `BLOCKED_DSI` | 7 |
| `BLOCKED_ENVIRONMENT` | 4 |

**Le dossier est constituable pour tout ce qui relève du produit.** Ce qui manque relève de
l'environnement, de l'exploitation ou d'un arbitrage — à une exception près, qui est de notre
ressort : **la mise sous version des specs et de la documentation** (`docs/34`).
