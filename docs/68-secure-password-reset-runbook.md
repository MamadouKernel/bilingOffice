# Runbook — réinitialisation sûre d'un mot de passe

> **Document** : `docs/68-secure-password-reset-runbook.md`
> **Établi le** : 2026-09-02, sur le commit `2ed2b8b`
> **Destinataire** : administrateur humain. Aucune étape de ce runbook n'est exécutable par un agent
> automatique.
> **Règle absolue** : aucun mot de passe ne figure dans ce document, ne doit figurer dans une
> commande, un fichier, un journal, un historique de shell ou un rapport.

---

## 1. Constat bloquant — à traiter avant tout usage réel

### G-04 — Le lien de réinitialisation est affiché à l'écran, sans authentification

| | |
|:---|:---|
| **Classe** | `GAP_PRODUIT` |
| **Statut** | **`OPEN` — `CRITICAL_SECURITY`** |
| **Gravité** | **Critique — prise de contrôle de compte sans authentification** |
| **Vérifié dans** | `Components/Pages/Comptes/MotDePasseOublie.razor:84-87` et `:53-58` |

**Ce que fait la page.** `/comptes/mot-de-passe-oublie` est publique : elle ne porte aucun attribut
`[Authorize]`, et `Program.cs:44` appelle `AddAuthorization()` sans politique de repli — rien
n'exige donc d'être authentifié. Sur soumission d'une adresse existante, elle appelle
`GeneratePasswordResetTokenAsync`, encode le jeton, puis **rend le lien complet dans la réponse
HTTP** :

```razor
<strong class="d-block mb-1 text-info">Lien de réinitialisation généré (mode démo) :</strong>
<code>@lienGenere</code>
```

**Aucune garde d'environnement n'entoure cet affichage.** Le libellé dit « mode démo », mais le code
ne teste ni `IsDevelopment()` ni quoi que ce soit d'autre : le comportement est identique en
Staging et en Production.

### Pourquoi c'est critique

Une personne quelconque, non authentifiée, qui connaît ou devine une adresse e-mail :

1. ouvre `/comptes/mot-de-passe-oublie` ;
2. saisit l'adresse visée ;
3. **lit le lien de réinitialisation directement sur la page** ;
4. le suit et choisit un nouveau mot de passe ;
5. se connecte à la place du titulaire.

Le jeton est valide **24 heures** (durée par défaut d'Identity, non reconfigurée). Les adresses
professionnelles suivent un schéma prévisible.

**Appliqué au compte qui porte les 54 permissions, cela donne l'administration complète de la
plateforme à un inconnu.** La précaution prise juste au-dessus — ne pas révéler si l'e-mail existe —
est annulée par les cinq lignes qui suivent : l'affichage du lien révèle l'existence du compte *et*
donne les moyens de le prendre.

### Origine

Ce n'est pas une négligence isolée : c'est une **conséquence directe de `PC-16`**. Aucun fournisseur
d'e-mail n'est configuré, le lien ne peut donc pas être envoyé. L'affichage à l'écran a été mis en
place pour que le parcours reste testable, et il n'a jamais été refermé.

### Ce qu'il faut faire

**Ne pas déployer cette page en l'état sur un environnement accessible à des tiers**, Staging
compris dès lors qu'il est joignable au-delà de l'équipe.

Trois traitements possibles, par ordre de préférence :

| # | Traitement | Effet |
|---:|:---|:---|
| 1 | Envoyer le lien par e-mail, ne rien afficher | Correct — **suppose `PC-16` levé** |
| 2 | N'afficher le lien que si `IsDevelopment()` | Referme Staging et Production ; le parcours reste indisponible tant que `PC-16` n'est pas levé |
| 3 | Retirer l'affichage et passer par le § 4 | Referme immédiatement ; la réinitialisation devient un acte d'administration |

**Aucun de ces traitements n'est appliqué ici.** Ce runbook constate, il ne corrige pas : la
correction touche le code produit, hors du périmètre autorisé.

---

## 2. Audit du mécanisme officiel — ce qui existe

| Élément | Emplacement | État |
|:---|:---|:---|
| Écran « mot de passe oublié » | `Pages/Comptes/MotDePasseOublie.razor` | Présent — **voir § 1** |
| Génération de jeton | `GeneratePasswordResetTokenAsync` | Présent, mécanisme Identity standard |
| Écran de réinitialisation | `Pages/Comptes/ReinitialiserMotDePasse.razor` | Présent |
| Application du nouveau secret | `ResetPasswordAsync` | Présent, correct |
| Fournisseurs de jetons | `.AddDefaultTokenProviders()` | Enregistrés |
| Politique de mot de passe | `DependencyInjection.cs:28` — 10 caractères minimum | Active |
| Verrouillage | 5 échecs, 15 minutes | Actif |
| **Réinitialisation par un administrateur** | — | **ABSENTE** |
| **Changement depuis « Mon profil »** | — | **ABSENTE** |
| Envoi du lien par e-mail | — | **ABSENT** — `PC-16` |

### Deux manques structurants

**Aucune réinitialisation administrative.** L'écran d'administration des comptes agents crée un
compte avec un mot de passe initial, mais n'offre aucune action « réinitialiser le mot de passe » sur
un compte existant. Un agent qui perd son mot de passe dépend donc entièrement du parcours autonome
— lequel dépend d'un canal e-mail qui n'existe pas.

**Aucun changement volontaire.** Un utilisateur connecté ne peut pas changer son mot de passe. C'est
gênant après une réinitialisation d'urgence : le secret transmis par un tiers ne peut pas être
remplacé par l'intéressé.

**Le mécanisme cryptographique est sain.** Ce qui manque relève du parcours et de l'exposition, pas
de l'implémentation d'Identity.

---

## 3. Ce qui est proscrit, et pourquoi

| Pratique | Pourquoi elle est proscrite |
|:---|:---|
| `UPDATE AspNetUsers SET PasswordHash = ...` | Contourne la politique de mot de passe, ne renouvelle pas le `SecurityStamp` — les sessions volées restent valides |
| Copier un hash d'un compte vers un autre | Deux comptes partagent alors le même secret, à l'insu des deux titulaires |
| Passer le mot de passe en argument | Il atterrit dans l'historique du shell, la table des processus et les journaux |
| Le placer dans une variable d'environnement | Lisible par tout processus enfant, souvent capturé par les outils de diagnostic |
| L'écrire dans un fichier, même supprimé ensuite | Reste récupérable sur disque ; l'oubli de suppression est la règle, pas l'exception |
| Le transmettre par messagerie ou chat | Persiste hors de tout contrôle, indéfiniment |
| **Le confier à un agent automatique** | **Interdit sans exception** |

---

## 4. Procédure sûre — utilitaire de maintenance à saisie interactive

À utiliser tant que le § 1 n'est pas traité et qu'aucune réinitialisation administrative n'existe.

### 4.1 Principe

Un utilitaire hors dépôt, exécuté **par l'administrateur humain sur le serveur**, qui :

- passe par le `UserManager` réel de la plateforme — donc par la politique de mot de passe, le
  hachage Identity et le renouvellement du `SecurityStamp` ;
- **lit le secret au clavier, masqué**, sans écho ;
- ne l'accepte **ni en argument, ni en variable d'environnement, ni depuis un fichier** ;
- ne le journalise jamais et ne le réaffiche jamais.

### 4.2 Projet

Hors du dépôt. `PwdReset.csproj` :

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="10.0.11" />
    <ProjectReference Include="..\..\src\CIT.Plateforme.Infrastructure\CIT.Plateforme.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

`Microsoft.AspNetCore.DataProtection` est **nécessaire** : sans lui, la résolution du `UserManager`
échoue sur `IDataProtectionProvider`, dont dépend le fournisseur de jetons de réinitialisation.

### 4.3 Programme

```csharp
using CIT.Plateforme.Infrastructure;
using CIT.Plateforme.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Le secret n'est jamais un argument, jamais une variable d'environnement,
// jamais un fichier : il est saisi au clavier, masqué, et reste en mémoire.
static string LireSecretMasque(string invite)
{
    Console.Write(invite);
    var saisie = new System.Text.StringBuilder();
    while (true)
    {
        var touche = Console.ReadKey(intercept: true);
        if (touche.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
        if (touche.Key == ConsoleKey.Backspace)
        {
            if (saisie.Length > 0) saisie.Length--;
            continue;
        }
        if (!char.IsControl(touche.KeyChar)) saisie.Append(touche.KeyChar);
    }
    return saisie.ToString();
}

var email = args.Length > 0 ? args[0] : throw new InvalidOperationException("Usage : PwdReset <email>");
var chaine = Environment.GetEnvironmentVariable("RESET_CS")
    ?? throw new InvalidOperationException("RESET_CS manquant (chaîne de connexion, sans mot de passe).");

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:PlateformeDb"] = chaine,
    })
    .Build();

var services = new ServiceCollection();
services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
services.AddSingleton<IConfiguration>(configuration);
services.AddHttpContextAccessor();
services.AddDataProtection();
services.AjouterInfrastructure(configuration);

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

var compte = await userManager.FindByEmailAsync(email);
if (compte is null) { Console.WriteLine($"ECHEC : aucun compte pour {email}"); return 1; }

Console.WriteLine($"Compte : {compte.Email} (Id {compte.Id})");
var secret = LireSecretMasque("Nouveau mot de passe : ");
var confirmation = LireSecretMasque("Confirmation         : ");

if (!string.Equals(secret, confirmation, StringComparison.Ordinal))
{
    Console.WriteLine("ECHEC : les deux saisies diffèrent. Aucune modification.");
    return 1;
}

var jeton = await userManager.GeneratePasswordResetTokenAsync(compte);
var resultat = await userManager.ResetPasswordAsync(compte, jeton, secret);
if (!resultat.Succeeded)
{
    // Les messages d'Identity décrivent la règle violée, jamais la valeur saisie.
    Console.WriteLine("ECHEC : " + string.Join(" | ", resultat.Errors.Select(e => $"{e.Code}: {e.Description}")));
    return 1;
}

// Invalide les sessions et jetons antérieurs : un changement de mot de passe
// doit déconnecter partout, pas seulement changer la prochaine connexion.
await userManager.UpdateSecurityStampAsync(compte);
await userManager.SetLockoutEndDateAsync(compte, null);
await userManager.ResetAccessFailedCountAsync(compte);

Console.WriteLine("OK — mot de passe réinitialisé, SecurityStamp renouvelé, verrouillage levé.");
return 0;
```

### 4.4 Exécution

L'adresse e-mail est un argument ; **le secret ne l'est jamais**.

```bash
$env:RESET_CS="Data Source=<serveur>;Initial Catalog=<base>;Integrated Security=True;Encrypt=True"; dotnet run -c Release -- m.konate@cotedivoireterminal.com
```

Si l'instance exige une authentification SQL, la chaîne contient alors un mot de passe : la fournir
par un coffre à secrets, jamais en clair dans la console.

### 4.5 Après l'opération

| # | Action | Motif |
|---:|:---|:---|
| 1 | Fermer la session console | Vide le tampon de la fenêtre |
| 2 | Purger l'historique du shell | `Remove-Item (Get-PSReadlineOption).HistorySavePath` — l'e-mail y figure |
| 3 | Ne consigner que l'horodatage, l'opérateur et le compte visé | **Jamais le secret** |

---

## 5. Contrôles après réinitialisation

### 5.1 En base — lecture seule

```sql
SELECT UserName,
       LEN(PasswordHash)   AS LongueurHash,
       LEFT(PasswordHash,4) AS Format,
       LockoutEnd,
       AccessFailedCount,
       LEFT(SecurityStamp,8) AS StampCourt
FROM AspNetUsers
WHERE Email = '<adresse>';
```

| Contrôle | Attendu |
|:---|:---|
| `PasswordHash` non nul | Renseigné |
| `Format` | **`AQAA`** — en-tête Identity V3 |
| `LongueurHash` | 84 caractères |
| `AccessFailedCount` | **`0`** |
| `LockoutEnd` | **nul**, ou passé |
| `SecurityStamp` | **différent** de la valeur d'avant l'opération |

**Relever le `SecurityStamp` avant l'opération** : c'est le seul moyen de démontrer qu'il a bien été
renouvelé, donc que les sessions antérieures sont invalidées.

### 5.2 Fonctionnel

| # | Contrôle | Attendu |
|---:|:---|:---|
| 1 | Connexion avec le nouveau secret | **Réussit** |
| 2 | Connexion avec l'ancien secret | **Échoue** |
| 3 | Session ouverte avant l'opération | **Rejetée** à la requête suivante |
| 4 | Permissions du compte | **Inchangées** — une réinitialisation ne touche pas aux habilitations |
| 5 | Journal d'audit | Voir § 5.3 |

**Le contrôle 3 dépend de la validation périodique du `SecurityStamp`** par le cookie
d'authentification. S'il échoue, l'invalidation n'est pas immédiate : à consigner comme réserve, et
à traiter en réduisant l'intervalle de validation.

### 5.3 Réserve — la traçabilité

Une réinitialisation menée par cet utilitaire **n'apparaît pas dans le journal d'audit métier** : le
`UserManager` écrit dans les tables Identity, pas dans le journal applicatif.

C'est une conséquence directe de l'absence d'écran d'administration dédié (§ 2). **Consigner
l'opération manuellement** : date, opérateur, compte visé, motif — jamais le secret.

---

## 6. Politique du secret choisi

| Règle | Exigence |
|:---|:---|
| Longueur | **10 caractères minimum** — imposé par la plateforme |
| Composition | Majuscule, minuscule, chiffre, caractère spécial |
| Interdits | Mot du dictionnaire, prénom, nom de l'entreprise, **année en cours** |
| Réutilisation | Jamais un secret ayant déjà servi ailleurs |
| Transmission | Canal distinct de celui qui porte l'identifiant |
| Durée de vie | À changer par le titulaire dès la première connexion — **voir la réserve ci-dessous** |

**Réserve** : le changement à la première connexion n'est pas réalisable aujourd'hui, faute d'écran
de changement de mot de passe (§ 2). Un secret transmis par un tiers reste donc en place jusqu'à la
prochaine intervention d'administration. C'est une raison de plus de traiter le § 1 et de doter la
plateforme d'un parcours de changement volontaire.

---

## 7. Récapitulatif des écarts

| Réf. | Écart | Gravité | Statut |
|:---|:---|:---|:---|
| **G-04** | Lien de réinitialisation affiché en clair, page publique | **Critique** | **`OPEN` — `CRITICAL_SECURITY`** |
| G-05 | Aucune réinitialisation administrative | Majeure | `OPEN` |
| G-06 | Aucun changement de mot de passe volontaire | Majeure | `OPEN` |
| G-07 | Réinitialisation hors application non tracée à l'audit | Mineure | `OPEN` |
| G-08 | Durée de vie du jeton non arbitrée — défaut 24 h | Mineure | `OPEN` — `TBD_DSI` |

**Aucun de ces écarts n'est corrigé par ce document.** Chacun touche le code produit ou une décision
de politique, l'un et l'autre hors du périmètre de ce runbook.

---

## 8. Statut

**`NOT EXECUTED`** — aucune réinitialisation n'a été conduite selon cette procédure.

Aucun mot de passe ne figure dans ce document.
