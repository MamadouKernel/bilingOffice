# Research — Socle transverse de gestion des tickets et workflows

**Feature** : `001-socle-tickets-workflows` | **Date** : 2026-08-28 | **Phase 0 de `/speckit.plan`**

> **Directive projet (28/08/2026, maître d'ouvrage)** : le projet doit être **100 % C#/.NET et SQL Server**.
> Cette directive est cohérente avec le Principe I de la constitution (C#, .NET/ASP.NET Core, SQL Server,
> EF Core, application Web responsive) et oriente toutes les décisions ci-dessous : **aucune technologie
> hors stack .NET** (pas de framework JavaScript, pas de broker externe, pas de service tiers).
> Les valeurs métier ouvertes (PC-01, PC-02, PC-07, PC-12, PC-20, PC-04) restent des
> **paramètres administrables**, jamais des constantes de code (Principes VI et XIII).

---

## D1 — Version du socle .NET

- **Décision** : C# sur **.NET 10 (LTS)** — ASP.NET Core 10, EF Core 10.
- **Rationale** : dernier LTS à la date du projet, support long terme adapté à une plateforme
  d'exploitation CIT/DSI ; .NET 8 (LTS précédent) arrive en fin de support trop près du démarrage du
  projet, ce qui imposerait une migration immédiate ; .NET 9 (STS) a un support trop court.
- **Alternatives considérées** : .NET 8 LTS (rejeté — fenêtre de support trop courte) ; .NET 9 STS
  (rejeté — support 18 mois, non adapté).
- **Réserve** : si la DSI impose une version standardisée différente, le plan reste valable — aucune
  fonctionnalité ne dépend d'une API exclusive à .NET 10 ; l'ajustement est sans restructuration.

## D2 — Interface utilisateur : Blazor Server + SignalR (100 % .NET)

- **Décision** : UI **Blazor Server** (composants .NET rendus côté serveur, transport SignalR natif),
  complétée par des hubs SignalR dédiés pour le temps réel de supervision.
- **Rationale** : satisfait la directive « 100 % .NET » sans framework JavaScript ; un seul langage (C#)
  pour l'équipe CIT/DSI ; le transport SignalR intégré couvre nativement les besoins temps réel
  (SUP-01 à SUP-03, ALE-01) ; rendu serveur = logique métier et contrôles d'autorisation
  systématiquement côté serveur (Principe VII, SEC-01) ; Web responsive via Bootstrap.
- **Alternatives considérées** :
  - ASP.NET Core MVC/Razor Pages + SignalR : viable, mais interactions riches (files, supervision live,
    affectation) imposeraient du JavaScript — contraire à la directive ;
  - Blazor WebAssembly : rejeté (nécessite une API séparée, déplace la surface d'attaque côté client,
    apporte un mode déconnecté inutile ici) ;
  - SPA JavaScript (React/Vue) : rejeté (violation directe de la directive 100 % .NET).

## D3 — Persistance : SQL Server + EF Core

- **Décision** : **SQL Server** (2019+) via **Entity Framework Core**, migrations versionnées
  (`dotnet ef migrations`), accès exclusivement par la couche Infrastructure.
- **Rationale** : imposé par la constitution (Principe VIII) et la directive ; migrations versionnées =
  reproductibilité recette/production ; intégrité du SOC-09 (aucune suppression d'historique) contrôlée
  au niveau du modèle et interceptée côté EF.
- **Alternatives considérées** : Dapper en complément — réservé aux requêtes de supervision si les
  performances l'exigent (PC-01 inconnue) ; décision reportée à l'implémentation, sans impact design.

## D4 — Authentification et autorisation

- **Décision** : **ASP.NET Core Identity** pour l'authentification (comptes CIT et clients), et un
  modèle d'**autorisation par policies/permissions** : les permissions du socle (ex. transfert,
  clôture, réouverture, consultation supervision) sont vérifiées **côté serveur** ; rôles, habilitations,
  rattachements aux files et périmètres de supervision sont des **données paramétrables**, jamais des
  rôles codés en dur.
- **Rationale** : le socle ne nomme aucun rôle métier concret (PC-03 non résolu — il ne doit pas l'être
  à ce niveau) ; le CDC exige cloisonnement client/agent, moindre privilège et contrôle serveur
  systématique (SEC-01, SEC-11) ; CLAR-05 exige un périmètre superviseur borné et configurable sans
  développement ; PC-20 (rôles de clôture pour absence de réponse) = habilitation configurable par module.
- **Alternatives considérées** : RBAC figé en attributs `[Authorize(Roles=...)]` — rejeté (rendrait
  rattachements rôles/files et habilitations immuables, contraire à WKF/PEC/TRF paramétrables).

## D5 — Traitements de fond (SLA, escalades, relances, distribution)

- **Décision** : **`BackgroundService` .NET** (services hébergés du processus Web) avec intervalle de
  scrutation **configurable**, exposés derrière des ports testables dans le Domain ; ordonnancement par
  requêtes « échéances dues » (échéance SLA, relance due, distribution due).
- **Rationale** : couvre SLA-02/03, ALE-01/02/03, CYC-04 (relances + suggestion de clôture),
  AFF-03..06 sans dépendance externe ; la latence fine est un paramètre (PC-02 — non inventé, valeur
  par défaut technique documentée dans les tasks, modifiable sans développement) ; conforme au
  Principe XI (fonctionnement autonome, aucun service externe requis).
- **Alternatives considérées** :
  - Quartz.NET / Hangfire : rejetés (dépendance tierce inutile à ce stade — YAGNI, Principe IV) ;
  - SQL Server Agent / Service Broker : rejetés (logique métier déplacée en base — contraire au
    Principe V ; complexité d'exploitation DSI).

## D6 — Numérotation unique des tickets et dossiers

> **Révision du 2026-08-29 (revue de fermeture Feature 001, décision humaine actée)** : cette
> décision a été révisée pour refléter fidèlement l'implémentation réelle, après qu'un audit
> post-implémentation (`post-implementation-audit.md` §3.A) a constaté une divergence entre la
> décision D6 initialement rédigée (séquence SQL par couple module/type) et le code livré
> (séquences globales avec préfixe lisible). Les deux options ont été comparées avec avantages,
> inconvénients et impacts dans `decision-d6-numbering.md`. **Le maître d'ouvrage a validé l'Option
> B (séquences globales)** : c'est elle qui est actée ci-dessous. **Le code n'a nécessité aucune
> modification** : il correspondait déjà à cette option.

- **Décision (actée)** : service de numérotation en **Infrastructure**
  (`src/CIT.Plateforme.Infrastructure/Persistence/ServiceNumerotation.cs`, pas dans le Domain — un
  service dépendant de `SELECT NEXT VALUE FOR`, propre à SQL Server, ne peut pas être une classe de
  domaine pure sans dépendance framework, Principe IV), s'appuyant sur **deux séquences SQL Server
  globales** (`SEQUENCE`) — une pour les tickets (`seq_ticket_numero`), une pour les dossiers
  (`seq_dossier_numero`) — plutôt qu'une séquence par couple (module, type). Le numéro final encode
  le module et le type de demande dans un **préfixe textuel lisible** (3 lettres de chacun, ex.
  `BIL-IMP-000042`), paramétrable par construction du préfixe ; le compteur numérique sous-jacent
  est partagé entre tous les modules/types. Attribution dans la même transaction que la création.
- **Rationale** :
  - SOC-01 (identifiant unique) et SOC-09 (numéro jamais réutilisé — y compris après
    annulation/archivage) sont satisfaits par une séquence SQL Server, qu'elle soit globale ou
    partitionnée par couple — la garantie d'unicité et de non-réutilisation ne dépend pas du
    partitionnement.
  - Les couples (module, type) ne sont pas fixés à l'avance par le socle : ils sont eux-mêmes des
    données fournies par les modules appelants, paramétrables (Principe VI). Une séquence par
    couple supposerait soit un provisionnement dynamique de schéma SQL (DDL `CREATE SEQUENCE` à
    l'exécution) à chaque nouveau couple rencontré, soit une énumération figée à l'avance de tous
    les couples possibles — les deux contredisent l'objectif « paramétrable sans développement »
    qu'un couple totalement nouveau doit obtenir un numéro valide sans aucune action technique.
  - Aucune exigence CDC n'impose que le compteur numérique reparte à 1 par module/type : SOC-01 et
    SOC-09 exigent l'unicité et la non-réutilisation, jamais une numérotation contiguë par module —
    seule la **lisibilité** (déjà exigée par l'usage du CDC : un numéro communiqué à un agent ou un
    client doit être reconnaissable) est concernée, et elle est satisfaite par le préfixe textuel,
    indépendamment du partitionnement du compteur.
  - Le compte applicatif ne nécessite que des permissions SQL de lecture/écriture de données
    (`SELECT`/usage de séquence), jamais de droit DDL (`ALTER SCHEMA`/`CREATE SEQUENCE`) — cohérent
    avec le moindre privilège (SEC-02/SEC-11).
- **Garanties** (vérifiées sur SQL Server réel, voir `post-implementation-audit.md` §5 et
  `NumerotationTests`) :
  - Unicité stricte : chaque numéro est attribué exactement une fois (`NEXT VALUE FOR`,
    transactionnellement sûr, sans verrou de table, sans contention même en forte concurrence).
  - Non-réutilisation : un numéro annulé/archivé n'est jamais réattribué (la séquence ne recule
    jamais).
  - Lisibilité : le préfixe module/type reste présent dans le numéro final malgré le compteur
    partagé.
  - Aucune limite pratique de volumétrie : la plage `bigint` d'une séquence SQL Server
    (~9,2×10¹⁸ valeurs) est hors de portée de tout scénario réaliste, y compris à très forte
    volumétrie (PC-01 non tranché, mais sans impact sur cette garantie).
- **Alternatives considérées et rejetées** :
  - Compteur en table + verrou applicatif (rejeté — contention en écriture concurrente).
  - GUID seul (rejeté — référence non lisible par les agents, contraire à l'usage CDC).
  - **Séquence SQL Server par couple (module, type) — Option A de `decision-d6-numbering.md`**
    (rejetée par le maître d'ouvrage) : lecture « Nᵉ ticket de ce module » possible, mais exige un
    provisionnement dynamique de schéma (droits DDL, complexité opérationnelle, risque de
    migration pour redistribuer les numéros déjà émis) sans bénéfice fonctionnel démontré au
    regard des exigences CDC réellement formulées.
- **Absence d'impact sur les exigences CDC** : SOC-01 et SOC-09 restent intégralement satisfaits
  et démontrés (tests réels sur SQL Server), quelle que soit l'option de partitionnement retenue —
  cette décision est un choix d'architecture interne au socle, invisible pour le client et l'agent
  (seul le numéro final, avec son préfixe lisible, leur est exposé), et ne modifie, n'ajoute ni ne
  retire aucune exigence du CDC.
- **Décision actée par** : maître d'ouvrage, 2026-08-29 (revue de fermeture Feature 001) — voir
  `decision-d6-numbering.md` pour le détail comparatif complet des deux options.

## D7 — Temps, calendrier ouvré et moteur SLA

- **Décision** : horodatages stockés en **UTC** ; **calendrier ouvré paramétrable** (horaires par jour,
  jours fériés, fuseau de référence CIT) consommé par un **moteur SLA/temps pur** (classe de domaine
  sans dépendance infrastructure) : calcul des échéances, suspension en attente client (TPS-02/04,
  SLA-03), franchissement de seuils paramétrables (info/alerte/dépassement — valeurs PC-12 non fixées,
  SLA-05).
- **Rationale** : les valeurs SLA, seuils et horaires sont des paramètres administrables (Principe VI) ;
  le moteur temps/SLA est la partie la plus testable du socle → classes pures unitairement testables
  (Principe IV).
- **Alternatives considérées** : calcul SLA en SQL — rejeté (règles métier dispersées, non testables).

## D8 — Audit et historique : deux journaux distincts

- **Décision** : **historique fonctionnel du ticket** (chronologie visible client/agents habilités) et
  **journal d'audit** (registre technique pour l'administrateur) = **deux stores distincts** ; le journal
  d'audit est **append-only** (aucune modification/suppression métier possible, contrôle applicatif +
  droits base).
- **Rationale** : Principe IX de la constitution (finalités distinctes SOC-13/SEC-05) ; exigences
  PRI-03, TRF-02/05, DIS-03, MAS-02, WKF-03 s'y rattachent explicitement.

## D9 — Documents

- **Décision** : métadonnées en base ; contenu binaire sur un **stockage fichiers configuré** derrière
  un port (`IStockageDocuments`) ; téléchargement uniquement via flux autorisé (contrôle identité +
  habilitation à chaque accès — SEC-12) ; **hook antivirus configurable** avant validation du dépôt
  (SEC-07) ; politique de conservation = paramètre (PC-14 ouvert, non inventé).
- **Rationale** : Principe X (finalité de stockage propre) ; découplage du stockage physique pour rester
  remplaçable (Principe IV).
- **Alternatives considérées** : FILESTREAM SQL Server — alternative acceptable au moment des tasks si
  la DSI préfère du tout-SQL ; l'abstraction rend ce choix non structurant.

## D10 — Événements du socle pour la future feature Notifications

- **Décision** : le socle publie des **événements de domaine** persistés (journal d'événements en base,
  écrit dans la même transaction que la mutation — garantie de non-perte) et dispatchés **in-process** ;
  la future feature Notifications (canaux, modèles, préférences — PC-25) consommera ce journal. Aucun
  canal, modèle ou notification n'est implémenté dans ce socle.
- **Rationale** : MSG-04/ALE-01 exigent que les événements existent ; PC-25 reste ouvert (canaux non
  définis) ; le CDC impose le fonctionnement autonome (Principe XI — aucun service externe requis).
- **Alternatives considérées** : bus de messages externe — rejeté (hors directive 100 % .NET ; YAGNI).

## D11 — Moteur d'affectation paramétrable

- **Décision** : stratégie d'affectation **par file, paramétrable sans développement** (AFF-07) parmi
  des stratégies intégrées : prise libre (AFF-01), affectation manuelle (AFF-02), round-robin (AFF-03),
  charge (AFF-04), disponibilité (AFF-05), compétence/habilitation (AFF-06) ; toute stratégie respecte
  la disponibilité (DIS-01 : jamais d'affectation d'un Indisponible/Absent).
- **Rationale** : AFF-07 (Must) impose l'administrabilité ; AFF-03..06 (Should) sont implémentées comme
  stratégies du même moteur, activables par configuration — aucune règle d'affectation figée en code.

## D12 — Stratégie de tests

- **Décision** : **xUnit** pour tous les tests ; **WebApplicationFactory** pour l'intégration bout en
  bout (vrai pipeline ASP.NET Core : autorisation serveur, audit, workflows) ; **bUnit** en complément
  pour les composants Blazor critiques ; SQL Server de test (conteneur ou instance DSI dédiée — à
  trancher à l'implémentation, voir points ouverts).
- **Rationale** : Principe XII (chaque Must a une stratégie de validation) ; seuls les scénarios
  **REC-SCE-09→14** (6 scénarios) sont exécutables dans le périmètre du socle et sont reproduits
  tels quels en tests de bout en bout — **REC-SCE-01→08** relèvent de features métier futures
  (Billing Office IMPORT/EXPORT, Simulation de Proforma, Customer Complaint, Enrôlement
  Transporteurs) et ne sont pas des scénarios de recette de ce socle ; les 24 Must sans scénario
  exécutable dans ce périmètre (liste exacte dans `recipe-scope-audit.md`) reçoivent des
  scénarios complémentaires issus des critères d'acceptation du spec.

---

## Résolution des inconnues du contexte technique

| Inconnue | Statut | Résolution |
|---|---|---|
| Langage / version | Résolu | D1 — C#/.NET 10 LTS |
| UI / temps réel | Résolu | D2 — Blazor Server + SignalR (directive 100 % .NET) |
| Stockage | Résolu | D3 — SQL Server + EF Core (constitution + directive) |
| Tests | Résolu (env à confirmer) | D12 — xUnit / WebApplicationFactory / bUnit |
| Performance / volumétrie cibles | **Paramètre (PC-01)** | Non inventé — dimensionnement DSI avant production ; requêtes paginées, sans choix bloquant |
| Rafraîchissement supervision | **Paramètre (PC-02)** | D2 — SignalR push + intervalle configurable |
| Valeurs SLA / seuils / horaires ouvrés | **Paramètre (PC-12)** | D7 — référentiel administrable, aucune valeur figée |
| Matrice des transitions (contenu) | **Paramètre (PC-07)** | Structure riche (CLAR-04) administrable ; contenu validé en atelier métier |
| Rôles de clôture pour absence de réponse | **Paramètre (PC-20)** | D4 — habilitation configurable par module |
| Périmètres superviseurs (composition) | **Paramètre (PC-04)** | D4 — structure bornée configurable (CLAR-05) |
| Canaux de notification | **Hors socle (PC-25)** | D10 — événements persistés ; feature Notifications future |

## Points ouverts à l'entrée en implémentation (non bloquants pour le plan)

1. **Environnement de test SQL** : conteneur SQL Server vs instance DSI partagée (outillage CI à
   confirmer).
2. **Version .NET effectivement installée** sur les postes DSI (réserve D1).
3. **Volumétrie PC-01** : dimensionnement (indexation, pagination, rétention) à intégrer avant
   recette/production — aucune valeur inventée ici.
