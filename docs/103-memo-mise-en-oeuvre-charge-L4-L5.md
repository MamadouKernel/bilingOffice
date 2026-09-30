# Mémo de mise en œuvre — Tenue en charge L4 & L5

> Suite de `docs/102-audit-securite-pentest.md` §12. **L1 (rate limiting), L2 (options circuit),
> L3 (filtrage SQL)** sont **livrés dans le code**. Restent **L4** (scale-out multi-nœuds) et **L5**
> (chaîne de connexion de production) — ils dépendent de l'**infrastructure et du déploiement**, pas
> du code applicatif seul. Ce mémo donne la marche à suivre concrète pour l'équipe exploitation/dev.

---

## L4 — Passage multi-nœuds (scale-out horizontal)

**Pourquoi** : l'application est en **Blazor Server**. Un seul nœud ne tient pas 10 000 circuits
(cf. §12.1). Pour répartir la charge sur plusieurs nœuds, **trois** prérequis, dans cet ordre.

### 4.1 Sessions collantes (sticky sessions) — OBLIGATOIRE

Un circuit Blazor Server vit **sur un seul nœud** (état de rendu en mémoire). Sans affinité, le
load-balancer enverrait les messages d'un même utilisateur sur des nœuds différents → circuit rompu.

À configurer **sur le load-balancer**, pas dans le code :

- **Nginx** : `ip_hash;` dans le bloc `upstream`, ou cookie de session (`sticky cookie`).
- **IIS / ARR** : activer *Client Affinity* (cookie ARRAffinity).
- **Azure App Service** : *ARR Affinity* = On.
- **Kubernetes (Service/Ingress)** : `sessionAffinity: ClientIP` (Service) ou annotations d'affinité
  par cookie sur l'Ingress (ex. `nginx.ingress.kubernetes.io/affinity: "cookie"`).
- **WebSocket** : s'assurer que le proxy autorise l'`Upgrade` WebSocket (sinon Blazor retombe en
  long-polling, plus coûteux).

### 4.2 Clés Data Protection partagées — OBLIGATOIRE

Cookies d'authentification et **jetons antiforgery** sont chiffrés avec les clés *Data Protection*.
Par défaut chaque nœud génère les siennes → un cookie émis par le nœud A est **rejeté** par le nœud B
(déconnexions aléatoires, échecs antiforgery). Il faut un **magasin de clés partagé**.

Option Redis (cohérente avec 4.3) — `Program.cs`, à ajouter :

```csharp
// Paquet : Microsoft.AspNetCore.DataProtection.StackExchangeRedis
var redis = builder.Configuration["Redis:ConnexionString"];
if (!string.IsNullOrWhiteSpace(redis))
{
    var multiplexeur = StackExchange.Redis.ConnectionMultiplexer.Connect(redis);
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(multiplexeur, "yaba-dataprotection-keys")
        .SetApplicationName("YABA"); // le MÊME nom sur tous les nœuds
}
```

> Alternative sans Redis : `PersistKeysToFileSystem(new DirectoryInfo(@"\\partage\keys"))` sur un
> partage réseau commun aux nœuds. Redis est préférable si déjà présent pour 4.3.

### 4.3 Propagation temps réel entre nœuds — `SignalSupervision`

`SignalSupervision` (`INotificateurTempsReel`) est un **singleton en mémoire** : un événement
« quelque chose a changé » déclenché sur le nœud A ne réveille que les tableaux de bord du nœud A.
Un superviseur connecté au nœud B ne verrait pas le rafraîchissement.

Bonne nouvelle : **ce signal ne transporte AUCUN état** (les vues sont toujours relues via les cas
d'usage). Une simple diffusion **pub/sub Redis** d'un message vide suffit : chaque nœud, à réception,
re-déclenche son événement local.

Implémentation cible (nouveau fichier `src/CIT.Plateforme.Web/Services/SignalSupervisionRedis.cs`) :

```csharp
using CIT.Plateforme.Application.Abstractions;
using StackExchange.Redis; // paquet StackExchange.Redis

namespace CIT.Plateforme.Web.Services;

// Diffuse « changement supervision » à TOUS les nœuds via un canal Redis. Aucun état métier ne
// transite : le message est vide, chaque nœud relit ses vues localement (comme aujourd'hui).
public sealed class SignalSupervisionRedis : INotificateurTempsReel, IAsyncDisposable
{
    private const string Canal = "yaba:supervision:change";
    private readonly ISubscriber _bus;
    public event Func<Task>? Change;

    public SignalSupervisionRedis(IConnectionMultiplexer redis)
    {
        _bus = redis.GetSubscriber();
        _bus.Subscribe(RedisChannel.Literal(Canal), async (_, _) => await DeclencherLocalAsync());
    }

    // Publie sur le bus : tous les nœuds (dont celui-ci) recevront et déclencheront leur événement.
    public Task NotifierChangementAsync(CancellationToken ct = default) =>
        _bus.PublishAsync(RedisChannel.Literal(Canal), RedisValue.EmptyString);

    private async Task DeclencherLocalAsync()
    {
        foreach (var abonne in Change?.GetInvocationList().Cast<Func<Task>>() ?? [])
        {
            try { await abonne(); } catch { /* un tableau de bord fermé ne bloque pas les autres */ }
        }
    }

    public async ValueTask DisposeAsync() =>
        await _bus.UnsubscribeAsync(RedisChannel.Literal(Canal));
}
```

Enregistrement **conditionnel** dans `Program.cs` (Redis si configuré, sinon le singleton mémoire
actuel — mono-nœud, comportement inchangé) :

```csharp
var redis = builder.Configuration["Redis:ConnexionString"];
if (!string.IsNullOrWhiteSpace(redis))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        _ => ConnectionMultiplexer.Connect(redis));
    builder.Services.AddSingleton<SignalSupervisionRedis>();
    builder.Services.AddSingleton<INotificateurTempsReel>(sp => sp.GetRequiredService<SignalSupervisionRedis>());
}
else
{
    builder.Services.AddSingleton<SignalSupervision>();
    builder.Services.AddSingleton<INotificateurTempsReel>(sp => sp.GetRequiredService<SignalSupervision>());
}
```

> Le rate limiter (L1) est aujourd'hui **par nœud** (`FixedWindow` en mémoire). Pour un plafond
> **global** au cluster, il pourra plus tard être adossé au même Redis. Non bloquant : par-nœud reste
> une protection valable.

### 4.4 Configuration L4

`appsettings.Production.json` (valeur réelle **via variable d'environnement**, pas en clair) :

```jsonc
"Redis": { "ConnexionString": "" }   // ex. env: Redis__ConnexionString=mon-redis:6379,ssl=true,password=...
```

### 4.5 Paquets NuGet à ajouter
- `StackExchange.Redis`
- `Microsoft.AspNetCore.DataProtection.StackExchangeRedis`

---

## L5 — Chaîne de connexion SQL de production

**Pourquoi** : un secret de développement observé pendant l'audit portait
`Pooling=False;Command Timeout=0` — motif **catastrophique** sous charge s'il atteint la production
(une connexion physique par opération → épuisement ; une requête lente jamais interrompue →
connexions bloquées). Voir §12.2 (L5, sévérité critique *si* répliqué en prod).

### 5.1 Chaîne recommandée (production)

À fournir **via variable d'environnement** `ConnectionStrings__PlateformeDb`, **jamais** en clair
dans un fichier versionné (`appsettings.Production.json` la laisse vide) :

```
Server=<hote-sql>;Database=<base>;User Id=<compte>;Password=<secret>;
Pooling=True;Max Pool Size=200;Min Pool Size=5;
Connect Timeout=15;Command Timeout=30;
Encrypt=True;TrustServerCertificate=False;
MultipleActiveResultSets=False
```

### 5.2 Règles impératives (liste de contrôle)

- [ ] **`Pooling=True`** (ou paramètre absent = pooling actif par défaut). **JAMAIS `Pooling=False`.**
- [ ] **`Command Timeout` fini** (ex. 30 s). **JAMAIS `Command Timeout=0`** (infini).
- [ ] `Max Pool Size` dimensionné (défaut 100 ; monter à ~200 selon la charge mesurée).
- [ ] `Connect Timeout` raisonnable (10–15 s).
- [ ] **`Encrypt=True`** en production (transport chiffré) ; `TrustServerCertificate=False` avec un
      certificat SQL valide.
- [ ] Le secret vit dans le **coffre** (variables d'environnement, Key Vault…), pas dans le dépôt.

### 5.3 Vérification

- Démarrer avec la chaîne de prod, ouvrir `/ready` → `database: Healthy`.
- Sous charge (test k6 §12.6 / `tools/load/charge-k6.js`), surveiller côté SQL le nombre de
  connexions (`sys.dm_exec_sessions`) : il doit **plafonner** (pooling), pas croître linéairement.
- Provoquer une requête lente artificielle et confirmer qu'elle **expire** (Command Timeout), sans
  bloquer les connexions.

---

## Ordre de déploiement conseillé

1. **L5** d'abord (chaîne de connexion) — sans risque fonctionnel, gain immédiat sous charge.
2. **L4.2** (Data Protection partagé) — prérequis avant d'ajouter un 2ᵉ nœud.
3. **L4.1** (sticky sessions) — au moment d'introduire le 2ᵉ nœud.
4. **L4.3** (`SignalSupervisionRedis`) — pour rétablir le temps réel entre nœuds.
5. **Test de charge** (k6) sur l'architecture cible pour valider les chiffres réels et dimensionner.

## Estimation d'effort (code)

- L4.2 + L4.3 : ~½ journée de dev (2 fichiers + DI conditionnelle + 2 paquets) + tests.
- L4.1 + L5 : configuration d'infra/déploiement (pas de code).
- Test de charge k6 sur cible : ~1 journée (scénarios + exécution + analyse).

> **Rappel de franchise (docs/102 §12.6)** : aucune capacité maximale n'est garantie sans un **vrai
> test de charge** sur l'infrastructure cible. Ce mémo lève les blocages connus ; les chiffres
> définitifs viennent de la mesure.
