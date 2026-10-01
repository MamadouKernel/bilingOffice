# 72 — Mémo de déploiement IIS (YABA)

> But : déployer la plateforme sur un serveur Windows / IIS pour les tests. Complète
> [48-production-deployment-runbook.md](48-production-deployment-runbook.md),
> [55-environment-configuration-matrix.md](55-environment-configuration-matrix.md) et
> [58-application-deployment-options.md](58-application-deployment-options.md).
>
> **Règle d'or :** les **user-secrets de la machine de dev ne sont pas déployés** (ASP.NET Core ne
> les lit qu'en environnement `Development`). Tout ce qui était en user-secrets (BDD, SMTP, Navis)
> **doit être re-fourni sur le serveur** via variables d'environnement ou `appsettings.Production.json`.

## 1. Prérequis serveur

- Windows Server avec **IIS** + **ASP.NET Core Hosting Bundle .NET 10** (installe le module
  `AspNetCoreModuleV2`). Redémarrer IIS après : `net stop was /y && net start w3svc`.
- **App Pool** dédié : .NET CLR = **« No Managed Code »**, mode intégré, identité avec accès au
  dossier de l'app et à SQL Server.
- **WebSockets** activé (rôle IIS) — requis par Blazor Server (InteractiveServer).
- SQL Server accessible depuis le serveur ; base créée.
- **Windows Defender présent et actif** : l'antivirus d'upload est en `FailClosed=true` → si
  Defender est absent, **tout dépôt de fichier est refusé** (sécurité SEC-07). Voir §5.

## 2. Publication

```
dotnet publish src/CIT.Plateforme.Web/CIT.Plateforme.Web.csproj -c Release -o C:\inetpub\yaba
```

Créer le site/app dans IIS pointant sur `C:\inetpub\yaba`, lié à l'App Pool ci-dessus, avec un
**binding HTTPS** (certificat) — la plateforme impose HTTPS/TLS (SEC-04).

## 3. Variables d'environnement (À DÉFINIR SUR LE SERVEUR)

À poser au niveau de l'App Pool (ou dans `web.config` → `<environmentVariables>`). Le double
underscore `__` remplace le `:` de la configuration.

| Variable | Valeur | Obligatoire |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ✅ |
| `ConnectionStrings__PlateformeDb` | chaîne SQL du serveur | ✅ |
| `Notifications__Smtp__Actif` | `true` | ✅ (sinon aucun mail) |
| `Notifications__Smtp__Hote` | `mail-relay.aglgroup.com` | ✅ |
| `Notifications__Smtp__Port` | `25` | ✅ |
| `Notifications__Smtp__UtiliserSsl` | `false` ou `true` (voir §6) | ✅ |
| `Notifications__Smtp__Utilisateur` | compte d'envoi | selon relais |
| `Notifications__Smtp__MotDePasse` | mot de passe | selon relais |
| `Notifications__Smtp__Expediteur` | `no-reply.yaba@cotedivoireterminal.com` | ✅ |
| `Base__MigrationAutomatiqueAuDemarrage` | `false` en prod (voir §4) | ✅ |
| `Navis__Active` | `false` (tant que non branché) | — |
| `Navis__Utilisateur` / `Navis__MotDePasse` | si `Navis__Active=true` | — |
| `Redis__ConnexionString` | **vide** si un seul nœud IIS ; `hote:6379,ssl=true,password=...` si multi-nœuds | voir §7 |

> ⚠️ Ne jamais écrire ces secrets dans un fichier commité. `appsettings.Production.json` sur le
> serveur (hors Git) est acceptable s'il est protégé par les ACL du dossier.

## 4. Base de données & migrations

`Base:MigrationAutomatiqueAuDemarrage` est `true` par défaut (pratique en dev/UAT). **En production,
le passer à `false`** et appliquer les migrations par une étape dédiée pour éviter les courses au
démarrage :

```
dotnet ef database update --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

(ou un bundle EF). Pour un **test rapide à un seul nœud**, on peut laisser `true` et laisser l'app
migrer au premier démarrage.

## 5. Antivirus upload (SEC-07)

`Securite:Antivirus:Mode=Defender`, `FailClosed=true` : chaque fichier déposé est scanné par
Windows Defender ; **si Defender est indisponible, l'upload est refusé** (choix sécurité). Vérifier
que Defender tourne sur le serveur, sinon aucun dépôt de pièce ne passera pendant les tests.

## 6. SMTP — vérification au premier envoi

Config actuelle : hôte `mail-relay.aglgroup.com`, **port 25** avec `UtiliserSsl=true`. Ce couple est
inhabituel (25 = clair/STARTTLS, 587 = STARTTLS, 465 = SSL implicite). Au **test d'envoi** (ex.
création de compte → mail de notification), si l'envoi échoue, essayer dans l'ordre :
1. `UtiliserSsl=false` sur le port **25** (relais interne typique, souvent sans auth) ;
2. sinon port **587** avec `UtiliserSsl=true` + compte/mot de passe.
Aucun changement de code — uniquement de la configuration.

## 7. Data Protection (IMPORTANT même à un seul nœud)

Sans `Redis__ConnexionString`, les clés Data Protection (cookies de session, jetons) sont stockées
**localement**. Par défaut sur IIS elles peuvent **ne pas survivre à un recyclage de l'App Pool** →
les utilisateurs sont déconnectés et les flux 2FA/réinitialisation cassent. Deux options :
- **Simple (1 nœud)** : fixer un **dossier persistant** pour les clés + désactiver le recyclage
  intempestif de l'App Pool (ou activer « Load User Profile » et `setProfileEnvironment`).
- **Multi-nœuds** : renseigner `Redis__ConnexionString` → partage des clés + diffusion temps réel
  de la supervision (L4.2/L4.3). Activer aussi l'**affinité de session** sur le répartiteur pour
  Blazor Server.

## 8. Vérifications post-déploiement (smoke test)

1. La page d'accueil `/` s'affiche en HTTPS.
2. Connexion + code 2FA fonctionnent (Data Protection OK → pas de déconnexion au recycle).
3. Création d'un compte société (`/comptes/soumettre`) → **le mail de notification arrive** (SMTP OK).
4. Dépôt d'une pièce jointe accepté (Defender OK).
5. Supervision temps réel : un changement se reflète sans recharger la page.

> Enchaîner ensuite la fiche de recette [107-fiche-tests-parcours-utilisateurs.md](107-fiche-tests-parcours-utilisateurs.md),
> Parcours 0 → 7.
