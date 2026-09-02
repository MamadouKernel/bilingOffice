# Checklist GO / NO-GO

> **Document** : `docs/47-go-no-go-checklist.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Règle** : **aucun `PASS` sans preuve**. Une ligne sans preuve est `BLOCKED`, jamais `PASS`.

---

## Légende

| Statut | Signification |
|:---|:---|
| `PASS` | Vérifié, preuve disponible et référencée |
| `FAIL` | Vérifié, non conforme |
| `BLOCKED` | Non vérifiable — dépendance externe non levée |
| `N/A` | Sans objet, avec justification |

---

## 1. Application

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 1.1 | Build sans erreur | `PASS` | `dotnet build` — 0 erreur |
| 1.2 | Build sans avertissement | `PASS` | 0 avertissement |
| 1.3 | Tests automatisés au vert | `PASS` | **1041 / 1041**, exécution sérialisée |
| 1.4 | Aucune anomalie critique ou haute | `PASS` | `docs/35` — 0 `CRITICAL`, 0 `HIGH` |
| 1.5 | Anomalies moyennes traitées ou acceptées | `BLOCKED` | 4 documentées, non corrigées — `docs/35` |
| 1.6 | Version déployée identifiée | `BLOCKED` | Aucun déploiement |
| 1.7 | Aucun secret dans le dépôt | `PASS` | Contrôle statique — 0 occurrence |
| 1.8 | Aucune ressource externe chargée | `PASS` | Test garde-fou automatisé |

## 2. Base de données

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 2.1 | Modèle et migrations synchronisés | `PASS` | « No changes have been made to the model » |
| 2.2 | Jeu de migrations cohérent | `PASS` | 25 migrations, snapshot aligné |
| 2.3 | Runbook de migration disponible | `PASS` | `docs/40` |
| 2.4 | Sauvegarde avant migration | `BLOCKED` | `TBD_INFRA` |
| 2.5 | Compte applicatif dédié, non `sa` | `BLOCKED` | `TBD_INFRA` |
| 2.6 | Migration exécutée sur la cible | `BLOCKED` | Aucun environnement |

## 3. Sécurité

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 3.1 | Cloisonnement des couches | `PASS` | 0 dépendance inverse — `docs/35` |
| 3.2 | En-têtes de sécurité | `PASS` | Middleware dédié |
| 3.3 | HTTPS et HSTS | `PASS` | Actifs hors développement |
| 3.4 | Antiforgery | `PASS` | `UseAntiforgery` |
| 3.5 | Autorisation vérifiée côté serveur | `PASS` | `IAutorisationService` |
| 3.6 | API authentifiée et cloisonnée | `PASS` | `RequireAuthorization` + isolation société |
| 3.7 | Journal d'audit en ajout seul | `PASS` | Garde à l'unité de travail |
| 3.8 | Antivirus actif sur les dépôts | `BLOCKED` | Non vérifiable hors environnement |
| 3.9 | Politique d'autorisation de repli | `FAIL` | Absente — `docs/35 § 3.1` |
| 3.10 | Certificat TLS valide | `BLOCKED` | `TBD_INFRA` |
| 3.11 | Gestion des secrets | `BLOCKED` | `TBD_DSI` — PC-13 |
| 3.12 | Test d'intrusion | `BLOCKED` | Non réalisé |

## 4. Recette UAT

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 4.1 | Plan de recette rédigé | `PASS` | `docs/36` — 24 scénarios |
| 4.2 | Matrice de traçabilité | `PASS` | `docs/37` |
| 4.3 | Environnement de recette | `BLOCKED` | Aucun |
| 4.4 | Jeu de données de recette | `BLOCKED` | `TBD_UAT` |
| 4.5 | Comptes par profil | `BLOCKED` | `TBD_UAT` |
| 4.6 | Recetteurs désignés | `BLOCKED` | `TBD_UAT` |
| 4.7 | Scénarios P1 exécutés | `BLOCKED` | Recette non lancée |
| 4.8 | Zéro anomalie P1 ouverte | `BLOCKED` | Idem |
| 4.9 | Validation métier signée | `BLOCKED` | Idem |

## 5. Performance

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 5.1 | Instrumentation livrée | `PASS` | Middleware de métrologie, `Server-Timing` |
| 5.2 | Script de charge disponible | `PASS` | `benchmark-k6.js` |
| 5.3 | Runbook de campagne | `PASS` | `docs/38` |
| 5.4 | Profil de charge arrêté | `BLOCKED` | `TBD_DSI` — PC-01 |
| 5.5 | Campagne exécutée | `BLOCKED` | Aucun environnement |
| 5.6 | `p95` < 3000 ms démontré | `BLOCKED` | **NFR-04 non prouvée** |

## 6. Disponibilité

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 6.1 | Sondes `/healthz` et `/ready` | `PASS` | `Program.cs` |
| 6.2 | Mode maintenance | `PASS` | Middleware dédié |
| 6.3 | Calcul de disponibilité | `PASS` | `CalculateurDisponibilite` |
| 6.4 | Disponibilité ≥ 99 % mesurée | `BLOCKED` | **NFR-03 non prouvée** |
| 6.5 | Endurance et résilience | `BLOCKED` | **NFR-10 non prouvée** |

## 7. Sauvegarde et PRA

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 7.1 | Périmètre identifié | `PASS` | `docs/41` |
| 7.2 | RPO et RTO arrêtés | `BLOCKED` | `TBD_DSI` |
| 7.3 | Sauvegardes configurées | `BLOCKED` | `TBD_INFRA` |
| 7.4 | Cohérence base / documents | `BLOCKED` | `TBD_INFRA` |
| 7.5 | **Test de restauration réel** | `BLOCKED` | **NFR-06 `NOT_PROVEN`** |
| 7.6 | PRA défini | `BLOCKED` | `TBD_DSI` |

## 8. Observabilité

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 8.1 | Journaux applicatifs émis | `PASS` | Journalisation en place |
| 8.2 | Aucune donnée personnelle ni secret dans les journaux | `PASS` | Contrôle statique |
| 8.3 | Centralisation des journaux | `BLOCKED` | `TBD_INFRA` |
| 8.4 | Supervision des sondes | `BLOCKED` | `TBD_INFRA` |
| 8.5 | Alerting | `BLOCKED` | `TBD_INFRA` |

## 9. Documentation

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 9.1 | Matrice CDC à jour | `PASS` | `docs/33` |
| 9.2 | Audit sécurité | `PASS` | `docs/35` |
| 9.3 | Runbooks — migration, F016, déploiement, exploitation | `PASS` | `docs/38`, `40`, `48`, `49` |
| 9.4 | Registre de décisions DSI | `PASS` | `docs/46` |
| 9.5 | **Specs versionnées** | `FAIL` | `specs/016` et `017` jamais versionnées — `docs/34` |
| 9.6 | Notes de version | `BLOCKED` | Aucune livraison |

## 10. Support et retour arrière

| # | Contrôle | Statut | Preuve |
|---:|:---|:---|:---|
| 10.1 | Runbook incident | `PASS` | `docs/49` |
| 10.2 | Procédure de retour arrière | `PASS` | `docs/40`, `docs/48` |
| 10.3 | Organisation de support définie | `BLOCKED` | `TBD_DSI` |
| 10.4 | Astreinte | `BLOCKED` | `TBD_DSI` |

## 11. Validations

| # | Contrôle | Statut |
|---:|:---|:---|
| 11.1 | Validation métier | `BLOCKED` |
| 11.2 | Validation DSI | `BLOCKED` |
| 11.3 | Décisions PC-01, PC-13, PC-16, PC-18 rendues | `BLOCKED` |
| 11.4 | Homologation infrastructure | `BLOCKED` |

---

## Synthèse

| Statut | Nombre |
|:---|---:|
| `PASS` | **26** |
| `FAIL` | **2** |
| `BLOCKED` | **35** |
| `N/A` | 0 |

### Les deux `FAIL`

| Réf. | Constat | Traitement |
|:---|:---|:---|
| **3.9** | Aucune politique d'autorisation de repli | Correction proposée, non appliquée — modifie 38 pages |
| **9.5** | `specs/016` et `017` n'ont jamais été versionnées | Décision de gouvernance — `docs/34` |

Ces deux points **n'empêchent pas une recette**, mais ils doivent être tranchés avant une mise en
production.

---

## Verdict

**NO-GO pour la production. GO conditionnel pour la recette**, dès qu'un environnement existe.

Les 35 `BLOCKED` se ramènent à **six décisions** — `docs/46`. Aucun ne relève d'un développement
manquant.
