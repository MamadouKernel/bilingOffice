# Checklist d'observabilité pour la recette

> **Document** : `docs/63-uat-observability-checklist.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Objet** : ce qu'on peut observer d'un environnement de recette, ce qu'on ne peut pas, et ce
> qu'il faut mettre en place avant d'ouvrir la campagne.

---

## 1. Ce qui existe réellement — vérifié dans le code

| Moyen d'observation | Emplacement | État |
|:---|:---|:---|
| Sonde de vivacité `/healthz` | `Program.cs:113` — `Predicate = _ => false` | Disponible |
| Sonde de disponibilité `/ready` | `Program.cs:118` — filtre sur le tag `ready` | Disponible |
| Contrôle base de données | `HealthChecks/DatabaseHealthCheck.cs` | Disponible |
| Contrôle stockage documentaire | `HealthChecks/StorageHealthCheck.cs` | Disponible |
| Contrôle antivirus | `HealthChecks/AntivirusHealthCheck.cs` | Disponible |
| Contrôle mode maintenance | `HealthChecks/MaintenanceHealthCheck.cs` | Disponible |
| En-tête `Server-Timing` | `Middlewares/MetrologiePerformanceMiddleware.cs` | Disponible |
| Journal d'audit métier | Table d'audit, append-only | Disponible |
| Journal d'événements socle | `EvenementSocle` | Disponible |
| Journalisation applicative | `ILogger` — **11 fichiers seulement** | Partielle |

### `/ready` détaille ses contrôles

La réponse de `/ready` est un JSON nommant chaque contrôle, son statut et sa durée. C'est
exploitable directement : en cas d'échec, la réponse dit **lequel** des quatre a échoué, sans avoir
à ouvrir un journal.

---

## 2. Trois constats à connaître avant d'ouvrir la recette

### 2.1 Le mode maintenance rend `/ready` non disponible

`MaintenanceHealthCheck` porte le tag `ready`. Une bascule en maintenance fait donc **échouer
`/ready`**, par construction — c'est le comportement voulu : un répartiteur de charge doit retirer
l'instance.

**Conséquence pour la mesure de disponibilité (`docs/60`)** : toute fenêtre de maintenance sera
comptée comme indisponible si on ne l'exclut pas explicitement. Les horodatages d'entrée et de
sortie de maintenance doivent être consignés, faute de quoi le taux mesuré sera faussement bas et
la cause introuvable après coup.

### 2.2 La durée des requêtes n'est journalisée nulle part

`MetrologiePerformanceMiddleware` **écrit l'en-tête `Server-Timing` mais ne journalise rien** : son
champ `_logger` est injecté et jamais utilisé.

Le temps de réponse n'est donc observable **que par un client qui lit l'en-tête au moment de
l'appel**. Aucune trace n'en subsiste côté serveur.

**Conséquence pour la recette** : un testeur qui signale « c'était lent hier vers 15 h » ne laissera
aucune trace exploitable. Sans mesure conservée, l'anomalie ne sera ni reproductible ni
diagnosticable.

**Palliatif sans modification du produit** : activer la journalisation des requêtes du serveur
d'hébergement — journaux IIS, ou journal du proxy inverse — qui conservent URL, code et durée. À
faire **avant** l'ouverture de la campagne, pas après le premier incident.

### 2.3 La purge de rétention ne s'exécute pas d'elle-même

`GestionnaireRetentionService` est enregistré en `AddScoped` (`DependencyInjection.cs:429`) et
**n'est appelé par aucun code applicatif** — seuls les tests l'invoquent. Ce n'est pas un service de
fond : rien ne le déclenche.

**Conséquence** : en recette, aucune purge ne se produira spontanément. Le scénario de rétention de
`docs/61 § 4` ne pourra être observé que si la purge est déclenchée explicitement.

Ce point est **reporté comme écart** dans `docs/67`. Il n'est pas corrigé ici : la cadence de purge
n'est fixée nulle part dans le CDC, et l'inventer serait un choix métier déguisé en correctif
technique.

---

## 3. Ce qui n'existe pas

| Moyen | État | Conséquence |
|:---|:---|:---|
| Traçage distribué (OpenTelemetry) | **Absent** — aucun paquet | Pas de corrélation inter-composants |
| Export de métriques (Prometheus, OTLP) | **Absent** | Pas de tableau de bord temps réel |
| Journalisation structurée dédiée (Serilog…) | **Absent** | Journaux au format par défaut du runtime |
| Agrégation centralisée des journaux | **Absent** | Journaux locaux à l'instance |
| Alerte automatique sur incident | **Absent** | Détection humaine |

**Aucune de ces absences n'est un défaut** : le CDC ne les exige pas, et aucune n'a été promise.
Elles définissent simplement ce qu'on ne pourra pas faire en recette — et il vaut mieux le savoir
avant qu'un incident ne le révèle.

---

## 4. À mettre en place avant l'ouverture de la campagne

| # | Action | Responsable | Pourquoi |
|---:|:---|:---|:---|
| 1 | Sondage périodique de `/ready`, avec horodatage conservé | `TBD_INFRA` | Seule source de la mesure NFR-03 |
| 2 | **Journalisation des requêtes du serveur d'hébergement** | `TBD_INFRA` | Compense le § 2.2 |
| 3 | Destination des journaux applicatifs définie et accessible | `TBD_INFRA` | Sans cela, un incident est inanalysable |
| 4 | Rétention des journaux de recette | `TBD_DSI` | Doit couvrir la durée de la campagne |
| 5 | Accès en lecture aux journaux pour l'équipe de recette | `TBD_DSI` | Sinon chaque analyse passe par l'exploitation |
| 6 | Supervision de l'espace disque du stockage documentaire | `TBD_INFRA` | Un disque plein fait échouer les dépôts |
| 7 | Supervision de l'espace des fichiers de la base | `TBD_INFRA` | Idem, côté données |
| 8 | Consignation des fenêtres de maintenance | Recette | Voir § 2.1 |

**L'action 2 est la plus rentable** : elle demande une case à cocher côté hébergement et comble le
seul angle mort réellement gênant pour la recette.

---

## 5. Contrôles quotidiens pendant la campagne

| # | Contrôle | Attendu |
|---:|:---|:---|
| 1 | `/ready` en début de journée | `Healthy` sur les quatre contrôles |
| 2 | Journaux applicatifs — niveau `Error` | Aucune entrée inattendue |
| 3 | Espace disque du stockage documentaire | Marge suffisante |
| 4 | Volume de la base | Croissance cohérente |
| 5 | File de distribution des notifications | Pas d'accumulation |
| 6 | Échecs d'envoi en réessai | Pas de croissance continue |
| 7 | Journal d'audit alimenté | Entrées du jour présentes |

**Le contrôle 6 mérite un mot** : les envois échoués sont repris par un service de réessai. Une
croissance continue signale que le canal ne fonctionne pas — et non que le réessai fonctionne mal.

---

## 6. Consignation

Pour chaque journée de campagne : résultat des sept contrôles, incidents observés, fenêtres de
maintenance, et **le commit déployé**.

**Statut : `NOT EXECUTED`** — aucun environnement de recette n'existe à ce jour.
