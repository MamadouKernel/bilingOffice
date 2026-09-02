# Audit de durcissement sécurité & architecture — post-F017

> **Document** : `docs/34-post-f017-security-hardening-audit.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Baseline** : build 0 avertissement · 1041 / 1041 tests · 0 pending model change · worktree propre
> **Méthode** : inspection statique du dépôt. Aucun test d'intrusion, aucune exécution en
> environnement représentatif — ce document ne remplace pas un audit de sécurité opérationnel.

---

## 1. Synthèse

| Sévérité | Nombre |
|:---|---:|
| `CRITICAL` | 0 |
| `HIGH` | 0 |
| `MEDIUM` | 4 |
| `LOW` | 1 |
| `INFO` (contrôles passés) | 12 |

**Aucune anomalie critique ni haute n'a été trouvée.** Les quatre points moyens relèvent de la
dette de conception et de la robustesse d'exploitation, non d'une exposition exploitable.

---

## 2. Contrôles passés

### 2.1 Cloisonnement des couches — vérifié

| Contrôle | Résultat |
|:---|:---|
| `Domain` référence-t-il `Application` ? | **Non** (0 occurrence) |
| `Domain` référence-t-il `Infrastructure` ? | **Non** (0 occurrence) |
| `Application` référence-t-elle `Infrastructure` ? | **Non** (0 occurrence) |

Le sens des dépendances est respecté sans exception. C'est le point le plus solide de
l'architecture : il garantit que la logique métier reste testable et que la couche technique est
remplaçable.

### 2.2 Chaîne HTTP

| Élément | État |
|:---|:---|
| `UseHsts` | Présent (hors développement) |
| `UseHttpsRedirection` | Présent |
| `UseSecurityHeaders` (middleware dédié) | Présent |
| `X-Content-Type-Options` | Posé |
| `X-Frame-Options` | Posé |
| `Referrer-Policy` | Posé |
| `Content-Security-Policy` | Posé |
| `UseAntiforgery` | Présent |
| `UseAuthentication` / `UseAuthorization` | Présents, dans le bon ordre |

### 2.3 API

Les trois endpoints `/api/v1/*` portent tous `.RequireAuthorization()`, et chaque handler
applique en outre un **cloisonnement par société** : un client dont la société ne correspond pas
au dossier reçoit `Results.Forbid()`. L'autorisation n'est donc pas déléguée au seul routage.

### 2.4 Autorisation applicative

Les requêtes et commandes sensibles passent par `IAutorisationService`. Contrôle effectué sur
`ConsulterFileQuery` : l'accès à une file exige une permission **bornée à cette file**, et lève
`AccesRefuseException` à défaut. Le masquage d'un élément d'interface n'est jamais l'unique garde.

### 2.5 Invariants de sécurité

| Invariant | État |
|:---|:---|
| Journal d'audit en ajout seul | `JournalAuditAppendOnlyGuard` appliqué à l'unité de travail |
| Antivirus | `ScannerAntivirusDefender` — un code de retour inattendu est traité comme `Infecte` |
| Rétention réglementaire | `MoteurRetentionService`, `CategorieRetention`, `LotPurgeRetention` livrés |
| Secrets en dur | **0** occurrence |
| URL externes | **0** occurrence (garde-fou de test en place) |
| PII ou secrets dans les journaux | **0** occurrence détectée |
| `TODO` / `FIXME` / `HACK` | **0** occurrence |

---

## 3. Anomalies

### 3.1 `MEDIUM` — Aucune politique d'autorisation de repli

**Constat.** `Program.cs` appelle `AddAuthorization()` sans `FallbackPolicy`. Une page Razor
dépourvue de `@attribute [Authorize]` est donc **anonyme par défaut**. 38 pages routables sont
dans ce cas.

**Analyse.** La majorité est légitimement publique (connexion, mot de passe oublié, inscription,
acceptation d'invitation, erreur). Les autres — files de traitement, fiches de ticket,
administration tarifaire — sont protégées **en profondeur** : soit par une garde explicite dans
`OnInitializedAsync`, soit par l'autorisation serveur de la requête appelée, qui lève. Aucune
fuite de données n'a été mise en évidence.

**Pourquoi c'est tout de même un défaut.** La sécurité repose sur le fait que chaque page pense à
se protéger. Une page ajoutée demain sans garde sera anonyme sans que rien ne le signale. Une
politique de repli inverse la charge : tout est fermé sauf ce qui est explicitement ouvert.

**Recommandation.** Poser `FallbackPolicy = RequireAuthenticatedUser()` et annoter `[AllowAnonymous]`
les pages publiques. **Non appliqué ici** : ce changement modifie le comportement de 38 pages et
demande une recette complète — hors du périmètre « correction locale incontestable ».

### 3.2 `MEDIUM` — Page morte, routable et défaillante

**Constat.** `Components/Pages/Tickets/MaFile.razor`, routée sur `/tickets/ma-file` :

```csharp
private string agentId = Guid.NewGuid().ToString();
private string fileId = string.Empty;
...
ticketsDeLaFile = await ConsulterFile.ExecuterAsync(Guid.Parse(agentId), Guid.Parse(fileId));
```

L'identifiant d'agent est un GUID **aléatoire** régénéré à chaque rendu — jamais l'utilisateur
authentifié — et `fileId` est vide : `Guid.Parse("")` lève. La page ne peut pas fonctionner.

**Ce n'est pas une faille.** Le GUID aléatoire ne détient aucune habilitation : la requête
refuserait de toute façon. Aucune donnée n'est exposée.

**C'est un risque de recette.** La page est atteignable en saisissant l'URL et produit une
exception non gérée. Elle porte le titre « Ma file de traitement (SOC-04) », exigence déclarée
certifiée — ce qui peut faire croire à une régression lors de l'UAT.

**Vérification faite.** La page est **orpheline** : aucun lien de navigation n'y mène, et elle est
la seule à consommer `ConsulterFileQuery`. SOC-04 est réellement couvert par les files par module
(`FilePiloteManifeste`, `FileFacturation`, etc.), qui utilisent correctement
`contexte.UtilisateurId`.

**Recommandation.** Supprimer la page. **Non appliqué ici** : retirer une route est une
modification de surface produit, pas une correction locale.

### 3.3 `MEDIUM` — Logique dupliquée dans sept fichiers

**Constat.** L'extraction de la raison sociale depuis `InformationsDeclarees` est réécrite à
l'identique dans :

- `Application/Client/ObtenirDashboardClientQuery.cs`
- `Application/Dossiers/ObtenirFicheDossierQuery.cs`
- `Application/Societe/ObtenirDashboardSocieteQuery.cs`
- `Application/Tickets/ObtenirFicheTicketQuery.cs`
- `Application/Web/ObtenirContexteUtilisateurQuery.cs`
- `Infrastructure/Persistence/DevelopmentSeeder.cs`
- `Infrastructure/Persistence/GuichetRepositories.cs`

**Risque.** Une correction de ce parsing ne se propagera pas : cinq copies continueront de se
comporter comme avant. C'est le mode de défaillance typique de la duplication — la divergence
silencieuse.

**Cause racine.** `SocieteCliente.InformationsDeclarees` est un champ de **texte libre** dont
plusieurs consommateurs tentent d'extraire une structure. Le défaut n'est pas le code dupliqué,
c'est l'absence de champ structuré.

**Recommandation.** À traiter avec le modèle, pas par une simple factorisation.

### 3.4 `MEDIUM` — Le module Tarification contourne l'horloge injectée

**Constat.** Cinq appels directs à `DateTime.UtcNow` dans la couche `Application` :

- `Tarification/CalculerSimulationProformaQuery.cs`
- `Tarification/CreerVersionGrilleTarifaireCommand.cs`
- `Tarification/ImporterGrilleTarifaireCsvCommand.cs`
- `Tarification/SauvegarderSimulationProformaCommand.cs`
- `Supervision/GestionnaireMaintenanceService.cs`

**Pourquoi c'est un défaut.** Le reste du projet injecte `IHorloge` précisément pour rendre les
invariants temporels testables — `MoteurRetentionService` le documente explicitement. Les grilles
tarifaires étant **versionnées par date d'effet**, ce sont justement les cas où une horloge
contrôlable compte le plus : on ne peut pas tester le basculement d'une version à sa date d'effet
sans pouvoir déplacer le temps.

**Recommandation.** Injecter `IHorloge`. **Non appliqué ici** : la modification touche cinq
signatures publiques et leurs enregistrements ; elle mérite ses propres tests.

### 3.5 `LOW` — Capture d'exception trop large

**Constat.** Quatre occurrences de `catch { }` sans filtre, dans le même utilitaire de parsing
dupliqué (§ 3.3). L'intention — retomber sur la chaîne brute — est légitime, mais la capture
avale **tout**, y compris ce qui ne devrait jamais être avalé.

**Recommandation.** Restreindre à `catch (Exception)` au minimum, idéalement aux types attendus,
et commenter l'intention. À traiter avec la déduplication.

---

## 4. Ce que cet audit ne couvre pas

Ces contrôles sont **hors de portée d'une inspection statique** et restent à faire en
environnement :

- test d'intrusion applicatif ;
- analyse de dépendances vulnérables (`dotnet list package --vulnerable` contre une base à jour) ;
- comportement réel de l'antivirus sous charge et en cas d'indisponibilité du service ;
- résistance au rejeu et à la concurrence sur les endpoints d'écriture ;
- configuration TLS effective (versions, suites) ;
- durcissement du serveur d'hébergement et du moteur SQL ;
- gestion des secrets en production.

Ils sont repris dans les livrables d'homologation et de mise en production.
