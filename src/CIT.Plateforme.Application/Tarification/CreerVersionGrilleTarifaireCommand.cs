using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record LigneTarifaireSaisie(
    SensTrafic Sens,
    TailleConteneur Taille,
    string CategorieMarchandise,
    decimal Acconage,
    decimal Relevage,
    decimal TarifJourBranchement);

public sealed record CreerVersionGrilleTarifaireCommande(
    Guid AgentId,
    string NumeroVersion,
    DateTime DateEffetUtc,
    ParametresTarification Parametres,
    IReadOnlyList<LigneTarifaireSaisie> Lignes);

public sealed class CreerVersionGrilleTarifaireCommandHandler(
    IVersionGrilleTarifaireRepository repository,
    IAutorisationService autorisation)
{
    public async Task<Guid> HandleAsync(CreerVersionGrilleTarifaireCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);

        // FEATURE 005 (spec.md §20, research.md D15) : l'administration tarifaire (création de
        // version de grille) exige la permission dédiée, jamais un rôle Identity figé.
        await autorisation.ExigerAsync(commande.AgentId, Permission.TarificationAdministrer, ct: ct);

        var version = VersionGrilleTarifaire.Creer(
            commande.NumeroVersion,
            commande.DateEffetUtc,
            commande.AgentId,
            DateTime.UtcNow,
            commande.Parametres);

        foreach (var l in commande.Lignes)
        {
            version.AjouterLigne(l.Sens, l.Taille, l.CategorieMarchandise, l.Acconage, l.Relevage, l.TarifJourBranchement);
        }

        await repository.AjouterAsync(version, ct);
        return version.Id;
    }
}
