# Checklist de préparation de l'environnement Staging

> **Document** : `docs/39-staging-readiness-checklist.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Objet** : ce que l'application exige de son environnement. **Aucun choix d'hébergement n'est
> fait ici** — `PC-13` reste OPEN.

---

## 1. Ce que l'application impose réellement

Ces contraintes sont **déduites du code**, pas supposées.

| Contrainte | Source dans le dépôt |
|:---|:---|
| Runtime **.NET 10** | `TargetFramework` des quatre projets |
| **SQL Server** | `UseSqlServer` dans `DependencyInjection.cs` |
| Chaîne de connexion **hors dépôt** | `appsettings.json` livre une chaîne vide ; la valeur vient des secrets |
| **HTTPS** | `UseHttpsRedirection` et `UseHsts` actifs hors développement |
| Stockage de fichiers accessible en écriture | `IStockageDocuments` |
| Antivirus | `ScannerAntivirusDefender` |
| Sondes `/healthz` et `/ready` | `Program.cs` |
| Migrations **hors démarrage** | Aucune application automatique — voir `docs/40` |
| Amorçage du premier administrateur | Deux clés de configuration, sinon l'administration est inaccessible |

---

## 2. Infrastructure

| Élément | Exigence de l'application | Valeur |
|:---|:---|:---|
| Système d'exploitation | Indifférent — .NET 10 est multiplateforme | `TBD_INFRA` |
| CPU / RAM | Dépend de la volumétrie | `TBD_DSI` — PC-01 |
| Stockage applicatif | Doit couvrir les documents déposés | `TBD_DSI` — PC-01 |
| Réseau | Accès sortant vers SQL Server et le stockage | `TBD_INFRA` |
| DNS | Un nom résolvable pour l'URL de recette | `TBD_INFRA` |
| TLS | Certificat valide pour ce nom | `TBD_INFRA` |
| Reverse proxy | Non requis, mais compatible | `TBD_INFRA` |
| Pare-feu | Ouverture applicative et base | `TBD_INFRA` |

**Point d'attention.** `UseHttpsRedirection` est actif : derrière un proxy terminant TLS, les
en-têtes de transfert doivent être propagés, faute de quoi l'application redirigera en boucle.

---

## 3. Application

| Élément | État | Valeur |
|:---|:---|:---|
| Runtime .NET 10 | **Requis** | `TBD_INFRA` |
| `ASPNETCORE_ENVIRONMENT` | `Staging` ou `Production` — **jamais `Development`** | À poser |
| `ConnectionStrings:PlateformeDb` | **Secret** — jamais dans le dépôt | `TBD_INFRA` |
| `Administration:AdministrateurInitial:Email` | Requis au premier démarrage | `TBD_DSI` |
| `Administration:AdministrateurInitial:MotDePasse` | **Secret** | `TBD_INFRA` |
| `Comptes:ValiditeInvitationJours` | Défaut technique : 7 | Ajustable |
| `Securite:Fichiers` | Extensions et taille maximale | Défauts présents |
| `Securite:Antivirus` | Mode et comportement en échec | Défauts présents |
| Journalisation | Destination et niveau | `TBD_INFRA` |

**`ASPNETCORE_ENVIRONMENT` est déterminant** : en `Development`, le jeu de données de test
s'exécute et HSTS est désactivé. Un Staging en `Development` ne serait pas représentatif.

---

## 4. Base de données

| Élément | Exigence | Valeur |
|:---|:---|:---|
| Version SQL Server | Compatible EF Core 10 | `TBD_INFRA` |
| Base dédiée | Une base par environnement | `TBD_INFRA` |
| Compte applicatif | **Compte dédié**, non `sa` | `TBD_INFRA` |
| Permissions | Lecture / écriture sur la base ; droits DDL **uniquement** pour la migration | `TBD_INFRA` |
| Stratégie de migration | Script revu puis exécuté — `docs/40` | Arrêtée |
| Sauvegarde avant migration | **Obligatoire** | `TBD_INFRA` |
| Collation | Cohérente entre bases | `TBD_INFRA` |

**Recommandation.** Le compte applicatif ne doit pas être `sa` : un compte limité à cette base
réduit la portée d'une injection ou d'une fuite de configuration. Les droits DDL peuvent être
accordés temporairement pour la migration, puis retirés.

---

## 5. Stockage documentaire

| Élément | Exigence | Valeur |
|:---|:---|:---|
| Emplacement | Accessible en lecture / écriture par le compte de service | `TBD_INFRA` |
| Capacité | Fonction du volume documentaire | `TBD_DSI` — PC-01 |
| Permissions | **Restreintes au seul compte de service** | `TBD_INFRA` |
| Sauvegarde | **Cohérente avec celle de la base** | `TBD_INFRA` |
| Antivirus | Actif sur les dépôts | `TBD_INFRA` |

**Point critique.** Sauvegarder la base sans le stockage documentaire produit un système
incohérent à la restauration : des références vers des fichiers absents. Les deux doivent être
sauvegardés **ensemble**.

---

## 6. Sécurité

| Contrôle | État applicatif | Reste à faire |
|:---|:---|:---|
| HTTPS | Redirection active | Certificat — `TBD_INFRA` |
| HSTS | Actif hors développement | — |
| En-têtes de sécurité | Middleware dédié en place | — |
| Antiforgery | Actif | — |
| Authentification | Cookie, expiration 30 min glissante | Durée à confirmer |
| Autorisation | Par permissions, vérifiée serveur | — |
| Comptes de service | — | `TBD_INFRA` |
| Accès administrateur au serveur | — | `TBD_INFRA` |

---

## 7. Observabilité

| Élément | Existant | Reste à faire |
|:---|:---|:---|
| `/healthz` | Livré | Sondage par la supervision |
| `/ready` | Livré | Idem |
| `Server-Timing` | Livré | Collecte |
| Journaux applicatifs | Émis | Destination et rétention — `TBD_INFRA` |
| Métriques | Middleware de métrologie | Agrégation — `TBD_INFRA` |
| Alerting | **Absent** | Outil et seuils — `TBD_INFRA` |

---

## 8. Verdict

**`READY_APPLICATION`** — l'application n'attend aucun développement pour être déployée.

**`BLOCKED_ENVIRONMENT`** — 22 valeurs marquées `TBD_INFRA` ou `TBD_DSI` doivent être fournies.
Aucune ne peut être choisie par l'équipe de développement sans se substituer à la DSI.
