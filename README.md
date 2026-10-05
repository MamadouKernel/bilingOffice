# bilingOffice

Plateforme digitale de **Côte d'Ivoire Terminal** — dématérialisation du parcours client au
Billing Office : contrôle documentaire, facturation, paiement et délivrance des documents de
sortie, aujourd'hui traités au guichet.

## Modules

| Module | Objet |
|---|---|
| **Billing Office** | Parcours import et export, simulation de proforma, avoir et exonération |
| **Customer Complaint** | Dépôt et instruction des réclamations |
| **Enrôlement Transporteurs** | Constitution à distance du dossier chauffeur, instruction par les agents Gate |
| **Socle transverse** | Tickets, files par rôle, messagerie, documents, notifications, audit |

## Guides utilisateurs

- [Parcours complets et responsabilités par profil](docs/94-parcours-par-role.md) — inscription,
  accès, démarches client, traitement des dossiers, administration et support.
- [Cahier de tests utilisateurs](docs/110-cahier-tests-utilisateurs.md) — essais pas à pas avec
  données fictives et résultats attendus.
- [Présentation métier YABA](docs/111-presentation-yaba.html) — diaporama hors ligne pour une
  démonstration auprès des équipes et partenaires.
- [Architecture d'autorisation](docs/architecture-autorisation.md) — règles de sécurité et
  habilitations côté serveur.

## Architecture

Quatre projets, dépendances orientées vers le domaine :

```
CIT.Plateforme.Domain           entités et règles métier, sans dépendance technique
CIT.Plateforme.Application      cas d'usage et ports (interfaces)
CIT.Plateforme.Infrastructure   persistance EF Core, identité, adaptateurs
CIT.Plateforme.Web              Blazor Server
```

**Autorisation par permissions**, jamais par rôles codés en dur : un rôle est un ensemble de
permissions attribuables sans développement, éventuellement bornées à une file de traitement.
Toute vérification est faite côté serveur — un contrôle masqué dans l'interface ne vaut jamais
autorisation.

## Prérequis

- .NET 10 SDK
- SQL Server (instance locale ou conteneur)

## Démarrage

La chaîne de connexion n'est pas versionnée. Renseignez-la dans les secrets utilisateur :

```bash
dotnet user-secrets set "ConnectionStrings:PlateformeDb" "<votre chaîne>" --project src/CIT.Plateforme.Web
```

Appliquez le schéma, puis lancez l'application :

```bash
dotnet ef database update --project src/CIT.Plateforme.Infrastructure --startup-project src/CIT.Plateforme.Web
```

```bash
dotnet run --project src/CIT.Plateforme.Web
```

### Premier administrateur

Au premier démarrage, aucun compte ne détient le droit d'administrer les comptes. Un
provisionnement s'exécute alors, à condition d'être configuré :

```bash
dotnet user-secrets set "Administration:AdministrateurInitial:Email" "<adresse>" --project src/CIT.Plateforme.Web
```

```bash
dotnet user-secrets set "Administration:AdministrateurInitial:MotDePasse" "<mot de passe>" --project src/CIT.Plateforme.Web
```

Il est **inerte dès qu'un administrateur existe** : il ne peut ni recréer un compte supprimé, ni
réhabiliter un compte désactivé. Changez le mot de passe à la première connexion.

## Tests

```bash
dotnet test
```

Quatre suites — domaine, application, infrastructure et web — dont des tests de bout en bout et
des garde-fous qui échouent si une ressource externe est réintroduite dans les pages.

## Conventions

- Le schéma évolue **par migrations**, jamais au démarrage de l'application : une mise en route
  ne doit pas pouvoir modifier une base de production.
- Aucune ressource n'est chargée depuis un serveur tiers : la plateforme doit fonctionner sur un
  terminal portuaire dont la connectivité sortante n'est pas garantie.
- Le journal d'audit est en ajout seul.
