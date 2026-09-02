using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// CDC § 17.3 — « Administration : gestion des comptes clients (validation, typage) et des
/// comptes agents (rôles). »
///
/// POURQUOI CES COMMANDES EXISTENT : jusqu'ici, les comptes internes n'étaient produits que par
/// le jeu de données de développement, qui ne s'exécute jamais en production. Aucun compte agent
/// ne pouvait donc naître sur un environnement réel, aucun rôle ne pouvait lui être attribué, et
/// SEC-11 (Must — « les comptes agents peuvent être désactivés immédiatement ») reposait sur un
/// indicateur qu'aucun code ne savait basculer.
///
/// Toutes les opérations exigent <see cref="Permission.AdministrationComptes"/>, vérifiée côté
/// serveur (Principe VII, decision D4) : le masquage d'un bouton n'a jamais valeur d'autorisation.
/// </summary>
public sealed class CreerCompteAgentCommand(
    IAdministrationComptesRepository comptes,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<ResultatCreationCompte> ExecuterAsync(
        Guid appelantId, string email, string nomAffiche, string motDePasseInitial, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("L'adresse e-mail du compte est obligatoire.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(nomAffiche))
        {
            throw new ArgumentException("Le nom affiché est obligatoire.", nameof(nomAffiche));
        }

        var resultat = await comptes.CreerAgentAsync(email.Trim(), nomAffiche.Trim(), motDePasseInitial, ct).ConfigureAwait(false);
        if (!resultat.Succes)
        {
            return resultat;
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, "CompteAgentCree", "ApplicationUser", resultat.UtilisateurId, null, email.Trim(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return resultat;
    }
}

/// <summary>
/// SEC-11 (Must) — « Les comptes agents peuvent être désactivés immédiatement, sans suppression
/// de leur historique. »
///
/// La désactivation bascule un indicateur ; elle ne supprime ni n'anonymise rien. C'est ce qui
/// permet à l'historique produit par l'agent de rester nominativement attribuable après coup.
/// </summary>
public sealed class DefinirActivationCompteCommand(
    IAdministrationComptesRepository comptes,
    IUtilisateurInfoRepository utilisateurs,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid appelantId, Guid cibleId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);

        var cible = await utilisateurs.ObtenirInfoAsync(cibleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Compte {cibleId} introuvable.");

        // Se désactiver soi-même reviendrait à se verrouiller hors de l'administration, sans
        // qu'aucun autre chemin ne permette de revenir en arrière.
        if (appelantId == cibleId && !actif)
        {
            throw new InvalidOperationException("Un administrateur ne peut pas désactiver son propre compte.");
        }

        var avant = cible.EstActif ? "actif" : "inactif";
        await comptes.DefinirActivationAsync(cibleId, actif, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, actif ? "CompteActive" : "CompteDesactive", "ApplicationUser", cibleId,
            avant, actif ? "actif" : "inactif", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>
/// CDC § 17.3, volet « rôles ». Le socle ne nomme aucun rôle métier en dur (decision D4,
/// PC-03 non résolu) : un rôle est un ensemble de permissions attribuées une à une, chacune
/// éventuellement bornée à une file.
/// </summary>
public sealed class GererHabilitationAgentCommand(
    IHabilitationRepository habilitations,
    IUtilisateurInfoRepository utilisateurs,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task AttribuerAsync(Guid appelantId, Guid cibleId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);
        await ExigerCibleConnueAsync(utilisateurs, cibleId, ct).ConfigureAwait(false);
        ExigerPermissionConnue(permissionCode);

        // Idempotence : réattribuer une permission déjà accordée ne doit pas produire de doublon,
        // qui rendrait ensuite le retrait partiel et l'affichage trompeur.
        var existantes = await habilitations.ListerParUtilisateurAsync(cibleId, ct).ConfigureAwait(false);
        if (existantes.Any(h => h.PermissionCode == permissionCode && h.FileId == fileId))
        {
            return;
        }

        await habilitations.AjouterAsync(HabilitationUtilisateur.Attribuer(cibleId, permissionCode, fileId), ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "HabilitationAttribuee", "HabilitationUtilisateur", cibleId, null, permissionCode, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    public async Task RetirerAsync(Guid appelantId, Guid cibleId, string permissionCode, Guid? fileId = null, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);
        await ExigerCibleConnueAsync(utilisateurs, cibleId, ct).ConfigureAwait(false);

        // Retirer à autrui le droit d'administrer est légitime ; se le retirer à soi-même ferme
        // la porte de l'intérieur.
        if (appelantId == cibleId && permissionCode == Permission.AdministrationComptes)
        {
            throw new InvalidOperationException("Un administrateur ne peut pas se retirer le droit d'administrer les comptes.");
        }

        await habilitations.RetirerAsync(cibleId, permissionCode, fileId, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "HabilitationRetiree", "HabilitationUtilisateur", cibleId, permissionCode, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    private static async Task ExigerCibleConnueAsync(IUtilisateurInfoRepository utilisateurs, Guid cibleId, CancellationToken ct)
    {
        _ = await utilisateurs.ObtenirInfoAsync(cibleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Compte {cibleId} introuvable.");
    }

    /// <summary>Une permission inconnue n'accorderait rien tout en paraissant accordée : le refus
    /// est explicite plutôt que silencieux.</summary>
    private static void ExigerPermissionConnue(string permissionCode)
    {
        if (!Permission.Tous.Contains(permissionCode))
        {
            throw new ArgumentException($"Permission inconnue : « {permissionCode} ».", nameof(permissionCode));
        }
    }
}

/// <summary>Vue d'administration d'un compte agent et des permissions qui lui sont accordées.</summary>
public sealed record CompteAgentVue(Guid Id, string Email, string NomAffiche, bool EstActif, IReadOnlyList<string> Permissions);

/// <summary>CDC § 17.3 — liste des comptes agents alimentant l'écran d'administration.</summary>
public sealed class ListerComptesAgentsQuery(
    IAdministrationComptesRepository comptes,
    IHabilitationRepository habilitations,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<CompteAgentVue>> ExecuterAsync(Guid appelantId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);

        var agents = await comptes.ListerAgentsAsync(ct).ConfigureAwait(false);
        var vues = new List<CompteAgentVue>(agents.Count);

        foreach (var agent in agents)
        {
            var accordees = await habilitations.ListerParUtilisateurAsync(agent.Id, ct).ConfigureAwait(false);
            vues.Add(new CompteAgentVue(
                agent.Id,
                agent.Email,
                string.IsNullOrWhiteSpace(agent.NomAffiche) ? agent.UserName : agent.NomAffiche!,
                agent.EstActif,
                accordees.Select(h => h.PermissionCode).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToList()));
        }

        return vues;
    }
}
