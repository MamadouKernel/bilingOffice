using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Application.Tarification;

public sealed record MajorationVue(string Type, decimal Taux);

public sealed record SeuilPoidsVue(ReferenceTarifaireVue Taille, decimal SeuilPoidsTonnes);

public sealed record TrancheStationnementVue(int BorneMinJour, int? BorneMaxJour, decimal TarifJournalier);

public sealed record FranchiseStationnementVue(ReferenceTarifaireVue Sens, int NombreJours);

/// <summary>
/// Paramètres d'une grille tels que l'administration les voit.
///
/// ÉTAIT UN OBJET DE QUATORZE PROPRIÉTÉS À PLAT (<c>ParametresTarification</c>), où les seuils de
/// poids, les franchises, les tranches et les bénéficiaires étaient nommés dans le type lui-même.
/// L'écran ne pouvait donc afficher que ce que le code avait prévu. Il affiche désormais ce que la
/// grille porte — c'est ce qui rend l'administration sans développement effective (RG-SIM-11).
/// </summary>
public sealed record ParametresGrilleVue(
    int FranchiseBranchementJours,
    IReadOnlyList<MajorationVue> Majorations,
    IReadOnlyList<SeuilPoidsVue> SeuilsPoids,
    IReadOnlyList<TrancheStationnementVue> Tranches,
    IReadOnlyList<FranchiseStationnementVue> Franchises,
    IReadOnlyList<PartBeneficiaireVue> QuotesParts);

public sealed record VersionGrilleTarifaireSommaireVue(
    Guid Id,
    string NumeroVersion,
    DateTime DateEffetUtc,
    DateTime DateCreationUtc,
    bool EstActive,
    int NombreLignes,
    ParametresGrilleVue Parametres);

public sealed record ConsulterVersionsGrillesTarifairesRequete(Guid AgentId);

public sealed class ConsulterVersionsGrillesTarifairesQueryHandler(
    IVersionGrilleTarifaireRepository repository,
    IReferentielsTarifairesRepository referentielsRepository,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<VersionGrilleTarifaireSommaireVue>> HandleAsync(
        ConsulterVersionsGrillesTarifairesRequete requete,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(requete);

        // FEATURE 005 (spec.md §26) : aucune donnée tarifaire sensible n'est restituée avant
        // autorisation, y compris en simple consultation.
        await autorisation.ExigerAsync(requete.AgentId, Permission.TarificationAdministrer, ct: ct);

        var versions = await repository.ListerToutesAsync(ct);
        if (versions.Count == 0)
        {
            return [];
        }

        var referentiels = await referentielsRepository.ChargerAsync(ct);

        return versions.Select(v => new VersionGrilleTarifaireSommaireVue(
            v.Id,
            v.NumeroVersion,
            v.DateEffetUtc,
            v.DateCreationUtc,
            v.EstActive,
            v.Lignes.Count,
            new ParametresGrilleVue(
                v.FranchiseBranchementJours,
                [.. v.Majorations
                    .OrderBy(m => m.Type)
                    .Select(m => new MajorationVue(m.Type.ToString(), m.Taux))],
                [.. v.SeuilsPoids
                    .Select(s => new SeuilPoidsVue(Reference(referentiels.Taille(s.TailleConteneurId)), s.SeuilPoidsTonnes))
                    .OrderBy(s => s.Taille.Code, StringComparer.Ordinal)],
                [.. v.TranchesOrdonnees
                    .Select(t => new TrancheStationnementVue(t.BorneMinJour, t.BorneMaxJour, t.TarifJournalier))],
                [.. v.Franchises
                    .Select(f => new FranchiseStationnementVue(Reference(referentiels.Sens(f.SensOperationId)), f.NombreJours))
                    .OrderBy(f => f.Sens.Code, StringComparer.Ordinal)],
                [.. v.QuotesParts
                    .OrderBy(q => q.Beneficiaire, StringComparer.Ordinal)
                    .Select(q => new PartBeneficiaireVue(q.Beneficiaire, q.Pourcentage))]))).ToList();
    }

    private static ReferenceTarifaireVue Reference(TailleConteneur t) => new(t.Id, t.Code, t.Libelle);

    private static ReferenceTarifaireVue Reference(SensOperation s) => new(s.Id, s.Code, s.Libelle);
}
