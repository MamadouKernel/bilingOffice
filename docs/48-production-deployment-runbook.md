# Runbook de déploiement en production

> **Document** : `docs/48-production-deployment-runbook.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Statut** : procédure prête — **aucun déploiement possible**, la cible n'est pas arbitrée
> (`PC-13`).

---

## 1. Avertissement

Cette procédure est **neutre quant à la cible**. Elle ne présuppose ni serveur applicatif, ni
conteneur, ni orchestrateur : ce choix appartient à la DSI et n'est pas fait ici.

Chaque valeur marquée `TBD_INFRA` doit être renseignée **avant** l'exécution, jamais pendant.

---

## 2. Prérequis — bloquants

| Prérequis | Statut |
|:---|:---|
| Recette UAT validée, zéro anomalie P1 | `BLOCKED` — recette non lancée |
| Certification environnementale F016 | `BLOCKED` — `docs/38` |
| Sauvegarde et restauration éprouvées | `BLOCKED` — `docs/41` |
| Cible d'hébergement homologuée | `BLOCKED` — `PC-13` |
| Fenêtre de déploiement autorisée | `TBD_DSI` |
| Procédure de retour arrière validée | Présente — § 10 |

**Aucun déploiement ne doit être entrepris tant que les cinq premières lignes ne sont pas levées.**

---

## 3. Préchecks

À exécuter et **consigner** avant toute action.

| # | Contrôle | Attendu |
|---:|:---|:---|
| 3.1 | Commit à déployer identifié | Empreinte notée |
| 3.2 | Build reproductible depuis ce commit | 0 erreur, 0 avertissement |
| 3.3 | Tests au vert sur ce commit | 1041 / 1041 |
| 3.4 | Aucune migration en attente non prévue | `migrations list` |
| 3.5 | Sauvegarde base **vérifiée** | `RESTORE VERIFYONLY` |
| 3.6 | Sauvegarde du stockage documentaire | Même fenêtre que la base |
| 3.7 | Espace disque suffisant | Base, journal, documents |
| 3.8 | Secrets disponibles sur la cible | Sans être écrits dans le dépôt |
| 3.9 | Version précédente conservée | Pour le retour arrière |

**Le point 3.6 est le plus souvent oublié.** Une sauvegarde de base sans le stockage documentaire
rend le retour arrière incohérent — voir `docs/41 § 2`.

---

## 4. Sauvegarde

Voir `docs/41`. Rappel du principe : base **et** documents, dans la **même fenêtre**, sauvegardes
**vérifiées**, chemins et horodatages consignés.

---

## 5. Mise en maintenance

L'application dispose d'un mode maintenance dédié (`MaintenanceMiddleware`).

1. Activer le mode maintenance.
2. Vérifier que les visiteurs reçoivent la page d'indisponibilité et l'en-tête `Retry-After`.
3. Vérifier qu'aucune écriture n'est plus en cours.

**Ne pas se contenter d'arrêter le service** : une coupure brutale peut interrompre une
transaction en cours.

---

## 6. Déploiement de l'artefact

| Étape | Action | Valeur |
|---:|:---|:---|
| 6.1 | Publier l'artefact depuis le commit validé | — |
| 6.2 | Déposer sur la cible | `TBD_INFRA` |
| 6.3 | Appliquer la configuration | `TBD_INFRA` |
| 6.4 | Injecter les secrets par le mécanisme retenu | `TBD_DSI` |
| 6.5 | Vérifier `ASPNETCORE_ENVIRONMENT` | **`Production`** — jamais `Development` |

**Le point 6.5 est critique.** En `Development`, le jeu de données de test s'exécute et HSTS est
désactivé. Une production démarrée en `Development` serait à la fois peuplée de comptes fictifs et
dégradée en sécurité.

---

## 7. Migration de la base

Voir `docs/40`. En production, **l'option script** est retenue : générer, **relire**, puis
exécuter. Rechercher explicitement `DROP` et `ALTER COLUMN` avant exécution.

---

## 8. Contrôles de santé

| # | Contrôle | Attendu |
|---:|:---|:---|
| 8.1 | `/healthz` | `200` |
| 8.2 | `/ready` | `200` — atteste l'accès à la base |
| 8.3 | Aucune erreur au démarrage | Journaux |
| 8.4 | Amorçage administrateur | Avertissement attendu si un administrateur existe déjà |

---

## 9. Tests de fumée

Avant sortie de maintenance :

1. connexion d'un compte réel ;
2. lecture d'un ticket existant ;
3. lecture d'un dossier existant ;
4. création d'une entité, puis vérification de la **trace d'audit** ;
5. dépôt d'un document, avec passage antivirus ;
6. appel d'un endpoint d'API authentifié.

**Critère de sortie de maintenance : les six passent.** Un seul échec déclenche le § 10.

---

## 10. Retour arrière

| Situation | Conduite |
|:---|:---|
| Échec avant migration | Redéployer la version précédente. Sortir de maintenance. |
| Échec après migration **additive** | Redéployer la version précédente ; le schéma reste compatible. |
| Échec après migration **destructive** | **Restaurer base et documents** depuis la sauvegarde, puis redéployer la version précédente. |
| Doute sur l'intégrité des données | Restaurer. Ne pas tenter de réparer en ligne. |

**Règle.** Redéployer une version antérieure sur un schéma migré échoue au démarrage, ou pire,
silencieusement à l'exécution. Le retour arrière applicatif et le retour arrière de schéma vont
**ensemble**.

---

## 11. Validation métier

Avant sortie de maintenance, un référent métier confirme sur un parcours réel. Sans cette
confirmation, la sortie de maintenance est prématurée : les tests de fumée vérifient que le
système répond, pas qu'il est juste.

---

## 12. Sortie de maintenance

1. Désactiver le mode maintenance.
2. Vérifier l'accès nominal.
3. Consigner l'heure de réouverture.

---

## 13. Surveillance renforcée

| Durée | `TBD_DSI` |
|:---|:---|

Surveiller : taux d'erreur, temps de réponse, disponibilité des sondes, occupation disque,
journaux d'erreur, et **volume d'écritures au journal d'audit** — une chute anormale signalerait
un dysfonctionnement silencieux.

**Critère de fin de surveillance renforcée** : aucun incident sur la période convenue.

---

## 14. Consignation

Pour chaque déploiement : commit déployé, horodatages, migrations appliquées, chemins de
sauvegarde, résultats des tests de fumée, incidents, et nom du valideur métier.

**Sans le commit déployé, un déploiement n'est pas traçable** — et un incident ultérieur ne peut
pas être rattaché à une version.
