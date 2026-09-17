using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Application.Tarification;

/// <summary>
/// Administration des référentiels conteneur (type, taille, catégorie de marchandise) de
/// l'Annexe A.6. Ces référentiels étaient des données de départ figées dans
/// <see cref="CIT.Plateforme.Domain.Tarification.ParametrageInitialAnnexeA"/>, jamais
/// modifiables sans redéploiement — alors que RG-SIM-11 exige qu'ils le soient sans
/// développement (ex. un nouveau type de conteneur réel : Open Top, Flat Rack, Tank…).
///
/// Un ajout n'engage aucun tarif : une nouvelle combinaison (sens × taille × type × catégorie)
/// apparaît dans les listes de la simulation mais restera en erreur « tarif non trouvé » tant
/// qu'une ligne tarifaire ne lui est pas associée dans la grille active — c'est le comportement
/// attendu, pas une omission de cette commande.
/// </summary>
public sealed record CreerTypeConteneurCommande(
    Guid AgentId, string Code, string Libelle, bool DeclencheBranchementFrigorifique, int Ordre);

public sealed record ModifierTypeConteneurCommande(
    Guid AgentId, Guid TypeConteneurId, string Libelle, bool DeclencheBranchementFrigorifique);

public sealed record BasculerActivationTypeConteneurCommande(Guid AgentId, Guid TypeConteneurId, bool Actif);

public sealed record CreerTailleConteneurCommande(Guid AgentId, string Code, string Libelle, int Ordre);

public sealed record ModifierTailleConteneurCommande(Guid AgentId, Guid TailleConteneurId, string Libelle);

public sealed record BasculerActivationTailleConteneurCommande(Guid AgentId, Guid TailleConteneurId, bool Actif);

public sealed record CreerCategorieMarchandiseCommande(
    Guid AgentId, string SensOperationCode, string Code, string Libelle);

public sealed record ModifierCategorieMarchandiseCommande(Guid AgentId, Guid CategorieId, string Libelle);

public sealed record BasculerActivationCategorieMarchandiseCommande(Guid AgentId, Guid CategorieId, bool Actif);

public sealed class AdministrerReferentielsConteneurCommandHandler(
    IReferentielsTarifairesRepository referentiels,
    IAutorisationService autorisation)
{
    private async Task ExigerHabilitationAsync(Guid agentId, CancellationToken ct) =>
        await autorisation.ExigerAsync(agentId, Permission.TarificationAdministrer, ct: ct);

    public async Task<Guid> CreerTypeAsync(CreerTypeConteneurCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);
        await ExigerHabilitationAsync(commande.AgentId, ct);

        if (await referentiels.ObtenirTypeParCodeAsync(commande.Code, ct) is not null)
            throw new InvalidOperationException($"Le code de type de conteneur « {commande.Code} » existe déjà.");

        var type = TypeConteneur.Creer(commande.Code, commande.Libelle, commande.DeclencheBranchementFrigorifique, commande.Ordre);
        await referentiels.AjouterTypeAsync(type, ct);
        return type.Id;
    }

    public async Task ModifierTypeAsync(ModifierTypeConteneurCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);
        await ExigerHabilitationAsync(commande.AgentId, ct);

        var type = await referentiels.ObtenirTypeParIdAsync(commande.TypeConteneurId, ct)
            ?? throw new InvalidOperationException("Type de conteneur introuvable.");

        type.Modifier(commande.Libelle, commande.DeclencheBranchementFrigorifique);
        await referentiels.EnregistrerTypeAsync(type, ct);
    }

    public async Task BasculerActivationTypeAsync(BasculerActivationTypeConteneurCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);
        await ExigerHabilitationAsync(commande.AgentId, ct);

        var type = await referentiels.ObtenirTypeParIdAsync(commande.TypeConteneurId, ct)
            ?? throw new InvalidOperationException("Type de conteneur introuvable.");

        if (commande.Actif) type.Activer(); else type.Desactiver();
        await referentiels.EnregistrerTypeAsync(type, ct);
    }

    public async Task<Guid> CreerTailleAsync(CreerTailleConteneurCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);
        await ExigerHabilitationAsync(commande.AgentId, ct);

        if (await referentiels.ObtenirTailleParCodeAsync(commande.Code, ct) is not null)
            throw new InvalidOperationException($"Le code de taille de conteneur « {commande.Code} » existe déjà.");

        var taille = TailleConteneur.Creer(commande.Code, commande.Libelle, commande.Ordre);
        await referentiels.AjouterTailleAsync(taille, ct);
        return taille.Id;
    }

    public async Task ModifierTailleAsync(ModifierTailleConteneurCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);
        await ExigerHabilitationAsync(commande.AgentId, ct);

        var taille = await referentiels.ObtenirTailleParIdAsync(commande.TailleConteneurId, ct)
            ?? throw new InvalidOperationException("Taille de conteneur introuvable.");

        taille.Renommer(commande.Libelle);
        await referentiels.EnregistrerTailleAsync(taille, ct);
    }

    public async Task BasculerActivationTailleAsync(BasculerActivationTailleConteneurCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);
        await ExigerHabilitationAsync(commande.AgentId, ct);

        var taille = await referentiels.ObtenirTailleParIdAsync(commande.TailleConteneurId, ct)
            ?? throw new InvalidOperationException("Taille de conteneur introuvable.");

        if (commande.Actif) taille.Activer(); else taille.Desactiver();
        await referentiels.EnregistrerTailleAsync(taille, ct);
    }

    public async Task<Guid> CreerCategorieAsync(CreerCategorieMarchandiseCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);
        await ExigerHabilitationAsync(commande.AgentId, ct);

        var sens = await referentiels.ObtenirSensParCodeAsync(commande.SensOperationCode, ct)
            ?? throw new InvalidOperationException($"Sens d'opération « {commande.SensOperationCode} » inconnu.");

        if (await referentiels.ObtenirCategorieParCodeAsync(sens.Id, commande.Code, ct) is not null)
            throw new InvalidOperationException(
                $"Le code de catégorie « {commande.Code} » existe déjà pour le sens « {commande.SensOperationCode} ».");

        var categorie = CategorieMarchandise.Creer(commande.Code, commande.Libelle, sens.Id);
        await referentiels.AjouterCategorieAsync(categorie, ct);
        return categorie.Id;
    }

    public async Task ModifierCategorieAsync(ModifierCategorieMarchandiseCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);
        await ExigerHabilitationAsync(commande.AgentId, ct);

        var categorie = await referentiels.ObtenirCategorieParIdAsync(commande.CategorieId, ct)
            ?? throw new InvalidOperationException("Catégorie de marchandise introuvable.");

        categorie.Renommer(commande.Libelle);
        await referentiels.EnregistrerCategorieAsync(categorie, ct);
    }

    public async Task BasculerActivationCategorieAsync(BasculerActivationCategorieMarchandiseCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);
        await ExigerHabilitationAsync(commande.AgentId, ct);

        var categorie = await referentiels.ObtenirCategorieParIdAsync(commande.CategorieId, ct)
            ?? throw new InvalidOperationException("Catégorie de marchandise introuvable.");

        if (commande.Actif) categorie.Activer(); else categorie.Desactiver();
        await referentiels.EnregistrerCategorieAsync(categorie, ct);
    }
}
