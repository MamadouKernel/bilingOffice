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

        // Anti-N+1 : habilitations de tous les agents en une seule requête regroupée.
        var habilitationsParAgent = await habilitations
            .ListerParUtilisateursAsync(agents.Select(a => a.Id), ct).ConfigureAwait(false);

        var vues = new List<CompteAgentVue>(agents.Count);
        foreach (var agent in agents)
        {
            var accordees = habilitationsParAgent.GetValueOrDefault(agent.Id) ?? [];
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


/// <summary>
/// Résultat d'une opération d'habilitation en masse. Rendu à l'écran pour que l'utilisateur sache
/// ce qui s'est réellement produit, plutôt qu'un « succès » indistinct.
/// </summary>
/// <param name="PermissionsAppliquees">Nombre de permissions réellement accordées ou retirées.</param>
/// <param name="PermissionsInchangees">Permissions déjà dans l'état demandé — aucune écriture.</param>
/// <param name="PermissionsConservees">
/// Permissions volontairement NON retirées parce qu'un invariant l'interdit. Aujourd'hui un seul
/// cas : un administrateur ne peut pas se retirer <c>administration.comptes</c>.
/// </param>
public sealed record ResultatHabilitationsEnMasse(
    int PermissionsAppliquees,
    int PermissionsInchangees,
    IReadOnlyList<string> PermissionsConservees);

/// <summary>
/// REMÉDIATION UI/UX (UX-P0, défaut UI-01) — HABILITATIONS EN MASSE, EN UNE SEULE TRANSACTION.
///
/// DÉFAUT CORRIGÉ. L'écran d'administration bouclait sur <see cref="GererHabilitationAgentCommand"/>,
/// dont chaque appel valide sa propre unité de travail. Accorder toutes les permissions déclenchait
/// donc une soixantaine de transactions indépendantes : un échec au n-ième laissait les n−1
/// précédentes acquises, sans message. Pire, « tout retirer » appliqué à soi-même retirait tout,
/// <b>puis</b> levait sur <c>administration.comptes</c> — l'administrateur se retrouvait dépouillé
/// et l'écran muet.
///
/// Cette commande ne change aucune règle : elle réunit les écritures sous <b>un seul</b>
/// <see cref="IUniteDeTravail.EnregistrerAsync"/>. Les dépôts et la journalisation empilent dans le
/// contexte sans écrire ; la validation finale est donc atomique — tout est appliqué, ou rien.
///
/// L'AUTORISATION RESTE CÔTÉ SERVEUR, et rien de ce que l'appelant transmet ne la fonde :
/// <list type="bullet">
///   <item>l'identité est le paramètre <c>appelantId</c>, résolu par l'écran depuis le contexte
///   d'authentification (règle 1 de <c>docs/architecture-autorisation.md</c>) ;</item>
///   <item>la permission requise est vérifiée <b>une fois, au début</b> — jamais déduite d'un rôle
///   ou d'une valeur reçue ;</item>
///   <item><b>la liste des permissions est déterminée ici</b>, depuis <see cref="Permission.Tous"/>,
///   et n'est jamais fournie par l'appelant.</item>
/// </list>
/// </summary>
public sealed class DefinirHabilitationsEnMasseCommand(
    IHabilitationRepository habilitations,
    IUtilisateurInfoRepository utilisateurs,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>
    /// Accorde à <paramref name="cibleId"/> toutes les permissions du référentiel qu'il ne détient
    /// pas encore. Idempotent : relancer l'opération n'écrit rien de plus.
    /// </summary>
    public async Task<ResultatHabilitationsEnMasse> AccorderToutesAsync(
        Guid appelantId, Guid cibleId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);
        await ExigerCibleConnueAsync(utilisateurs, cibleId, ct).ConfigureAwait(false);

        var detenues = await PermissionsNonBorneesAsync(cibleId, ct).ConfigureAwait(false);

        // La liste vient du référentiel serveur, jamais de l'appelant.
        var aAccorder = Permission.Tous.Where(p => !detenues.Contains(p)).OrderBy(p => p, StringComparer.Ordinal).ToList();

        foreach (var permissionCode in aAccorder)
        {
            await habilitations
                .AjouterAsync(HabilitationUtilisateur.Attribuer(cibleId, permissionCode), ct)
                .ConfigureAwait(false);

            // Trace individuelle conservée : RG-SEC-03 exige que chaque attribution soit traçable,
            // et une ligne de synthèse ne remplacerait pas le détail.
            await journalisation.ConsignerAuditAsync(
                appelantId, "HabilitationAttribuee", "HabilitationUtilisateur", cibleId, null, permissionCode, ct)
                .ConfigureAwait(false);
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, "HabilitationsAccordeesEnMasse", "HabilitationUtilisateur", cibleId,
            null, $"{aAccorder.Count} permission(s)", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new ResultatHabilitationsEnMasse(aAccorder.Count, Permission.Tous.Count - aAccorder.Count, []);
    }

    /// <summary>
    /// Applique le PROFIL « SUPPORT N1 » (équipe IT) à <paramref name="cibleId"/> : accorde, en UNE
    /// unité de travail, les permissions de <see cref="Permission.ProfilSupportN1"/> qu'il ne détient
    /// pas encore. N'en RETIRE aucune : appliquer un profil ne dépouille pas des habilitations déjà
    /// accordées par ailleurs — c'est un ajout, pas une remise à zéro.
    ///
    /// Comme <see cref="AccorderToutesAsync"/>, LA LISTE VIENT DU RÉFÉRENTIEL SERVEUR
    /// (<see cref="Permission.ProfilSupportN1"/>), jamais de l'appelant, et l'autorisation
    /// (<see cref="Permission.AdministrationComptes"/>) est vérifiée une fois au début. Idempotent :
    /// relancer n'écrit rien de plus.
    /// </summary>
    public async Task<ResultatHabilitationsEnMasse> AppliquerProfilSupportN1Async(
        Guid appelantId, Guid cibleId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);
        await ExigerCibleConnueAsync(utilisateurs, cibleId, ct).ConfigureAwait(false);

        var detenues = await PermissionsNonBorneesAsync(cibleId, ct).ConfigureAwait(false);

        var aAccorder = Permission.ProfilSupportN1
            .Where(p => !detenues.Contains(p))
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        foreach (var permissionCode in aAccorder)
        {
            await habilitations
                .AjouterAsync(HabilitationUtilisateur.Attribuer(cibleId, permissionCode), ct)
                .ConfigureAwait(false);

            await journalisation.ConsignerAuditAsync(
                appelantId, "HabilitationAttribuee", "HabilitationUtilisateur", cibleId, null, permissionCode, ct)
                .ConfigureAwait(false);
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, "ProfilSupportN1Applique", "HabilitationUtilisateur", cibleId,
            null, $"{aAccorder.Count} permission(s)", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new ResultatHabilitationsEnMasse(
            aAccorder.Count, Permission.ProfilSupportN1.Count - aAccorder.Count, []);
    }

    /// <summary>
    /// Retire à <paramref name="cibleId"/> toutes ses permissions.
    ///
    /// COMPORTEMENT DÉTERMINISTE SUR L'AUTO-RETRAIT : si l'appelant s'applique l'opération à
    /// lui-même, <c>administration.comptes</c> est <b>conservée</b> et rendue dans
    /// <see cref="ResultatHabilitationsEnMasse.PermissionsConservees"/>. L'invariant « un
    /// administrateur ne ferme pas la porte de l'intérieur » est préservé <b>sans</b> exception en
    /// cours de route : l'opération va à son terme et dit ce qu'elle a laissé.
    /// </summary>
    public async Task<ResultatHabilitationsEnMasse> RetirerToutesAsync(
        Guid appelantId, Guid cibleId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);
        await ExigerCibleConnueAsync(utilisateurs, cibleId, ct).ConfigureAwait(false);

        var detenues = await PermissionsNonBorneesAsync(cibleId, ct).ConfigureAwait(false);

        var conservees = new List<string>();
        var aRetirer = new List<string>();
        foreach (var permissionCode in detenues.OrderBy(p => p, StringComparer.Ordinal))
        {
            if (appelantId == cibleId && permissionCode == Permission.AdministrationComptes)
            {
                conservees.Add(permissionCode);
                continue;
            }

            aRetirer.Add(permissionCode);
        }

        foreach (var permissionCode in aRetirer)
        {
            await habilitations.RetirerAsync(cibleId, permissionCode, null, ct).ConfigureAwait(false);

            await journalisation.ConsignerAuditAsync(
                appelantId, "HabilitationRetiree", "HabilitationUtilisateur", cibleId, permissionCode, null, ct)
                .ConfigureAwait(false);
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, "HabilitationsRetireesEnMasse", "HabilitationUtilisateur", cibleId,
            $"{aRetirer.Count} permission(s)", null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new ResultatHabilitationsEnMasse(aRetirer.Count, 0, conservees);
    }

    /// <summary>
    /// Permissions détenues SANS borne de file. L'écran d'administration des comptes agents ne
    /// manipule que celles-là ; une habilitation bornée à une file relève d'un autre geste et n'est
    /// donc ni comptée, ni retirée ici.
    /// </summary>
    private async Task<HashSet<string>> PermissionsNonBorneesAsync(Guid cibleId, CancellationToken ct) =>
        (await habilitations.ListerParUtilisateurAsync(cibleId, ct).ConfigureAwait(false))
            .Where(h => h.FileId is null)
            .Select(h => h.PermissionCode)
            .ToHashSet(StringComparer.Ordinal);

    private static async Task ExigerCibleConnueAsync(IUtilisateurInfoRepository utilisateurs, Guid cibleId, CancellationToken ct)
    {
        _ = await utilisateurs.ObtenirInfoAsync(cibleId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Compte {cibleId} introuvable.");
    }
}
