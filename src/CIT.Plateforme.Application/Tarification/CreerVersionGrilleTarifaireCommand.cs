using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Domain.Tarification.Parametres;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Application.Tarification;

/// <summary>
/// Ligne de grille exprimée en codes de référentiel — la même forme que le CSV, parce que c'est
/// ce qu'un administrateur tarifaire manipule (« IMPORT_LOCAL », « 40 », « C1 »).
/// </summary>
public sealed record LigneTarifaireSaisie(
    string SensCode,
    string TailleCode,
    string CategorieCode,
    decimal Acconage,
    decimal Relevage,
    decimal TarifJourBranchement);

public sealed record MajorationSaisie(TypeMajoration Type, decimal Taux);

public sealed record SeuilPoidsSaisie(string TailleCode, decimal SeuilPoidsTonnes);

public sealed record TrancheSaisie(int BorneMinJour, int? BorneMaxJour, decimal TarifJournalier);

public sealed record FranchiseSaisie(string SensCode, int NombreJours);

public sealed record QuotePartSaisie(string Beneficiaire, decimal Pourcentage);

/// <summary>
/// Paramètres d'une grille à créer.
///
/// REMPLACE <c>ParametresTarification</c>, qui exposait quatorze propriétés à plat dont huit
/// nommaient une dimension (20 pieds, 40 pieds, import local, import transit, tranche 1 à 3,
/// CIT, OIC, PAA). Une grille ne pouvait donc pas porter une troisième taille ni une quatrième
/// tranche, quoi qu'en dise SIM-06. Ici, chaque dimension est une liste.
/// </summary>
public sealed record ParametresGrilleSaisie(
    int FranchiseBranchementJours,
    IReadOnlyList<MajorationSaisie> Majorations,
    IReadOnlyList<SeuilPoidsSaisie> SeuilsPoids,
    IReadOnlyList<TrancheSaisie> Tranches,
    IReadOnlyList<FranchiseSaisie> Franchises,
    IReadOnlyList<QuotePartSaisie> QuotesParts);

/// <summary>
/// Création d'une version de grille. <c>Parametres</c> non renseigné applique le paramétrage
/// initial de l'Annexe A.
/// </summary>
public sealed record CreerVersionGrilleTarifaireCommande(
    Guid AgentId,
    string NumeroVersion,
    DateTime DateEffetUtc,
    IReadOnlyList<LigneTarifaireSaisie> Lignes,
    ParametresGrilleSaisie? Parametres = null);

public sealed class CreerVersionGrilleTarifaireCommandHandler(
    IVersionGrilleTarifaireRepository repository,
    IReferentielsTarifairesRepository referentielsRepository,
    IAutorisationService autorisation)
{
    public async Task<Guid> HandleAsync(CreerVersionGrilleTarifaireCommande commande, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(commande);

        // FEATURE 005 (spec.md §20, research.md D15) : l'administration tarifaire (création de
        // version de grille) exige la permission dédiée, jamais un rôle Identity figé.
        await autorisation.ExigerAsync(commande.AgentId, Permission.TarificationAdministrer, ct: ct);

        var referentiels = await referentielsRepository.ChargerAsync(ct);

        var version = ConstructeurGrilleTarifaire.Construire(
            commande.NumeroVersion,
            commande.DateEffetUtc,
            commande.AgentId,
            DateTime.UtcNow,
            commande.Parametres,
            commande.Lignes,
            referentiels,
            out var erreurs);

        if (erreurs.Count > 0)
        {
            throw new InvalidOperationException(
                "La grille tarifaire n'a pas pu être constituée : " + string.Join(" ", erreurs));
        }

        await repository.AjouterAsync(version, ct);
        return version.Id;
    }
}

/// <summary>
/// Constitution d'une grille à partir de codes de référentiel. Partagé par la création manuelle et
/// l'import CSV : les deux font le même geste, et le faire à deux endroits garantirait qu'ils
/// finissent par diverger.
/// </summary>
internal static class ConstructeurGrilleTarifaire
{
    public static VersionGrilleTarifaire Construire(
        string numeroVersion,
        DateTime dateEffetUtc,
        Guid auteurId,
        DateTime dateCreationUtc,
        ParametresGrilleSaisie? parametres,
        IReadOnlyList<LigneTarifaireSaisie> lignes,
        ReferentielsTarifaires referentiels,
        out IReadOnlyList<string> erreurs)
    {
        var problemes = new List<string>();

        var version = VersionGrilleTarifaire.Creer(
            numeroVersion,
            dateEffetUtc,
            auteurId,
            dateCreationUtc,
            parametres?.FranchiseBranchementJours ?? ParametrageInitialAnnexeA.FranchiseBranchementJours);

        if (parametres is null)
        {
            ParametrageInitialAnnexeA.AppliquerParametres(version, referentiels);
        }
        else
        {
            AppliquerParametres(version, parametres, referentiels, problemes);
        }

        foreach (var l in lignes)
        {
            var sens = referentiels.SensParCode(l.SensCode);
            if (sens is null)
            {
                problemes.Add($"Sens d'opération « {l.SensCode} » inconnu du référentiel.");
                continue;
            }

            var taille = referentiels.TailleParCode(l.TailleCode);
            if (taille is null)
            {
                problemes.Add($"Taille de conteneur « {l.TailleCode} » inconnue du référentiel.");
                continue;
            }

            var categorie = referentiels.CategorieParCode(sens.Id, l.CategorieCode);
            if (categorie is null)
            {
                problemes.Add(
                    $"Catégorie de marchandise « {l.CategorieCode} » inconnue pour le sens « {l.SensCode} ». "
                    + "Une catégorie n'existe que pour un sens donné (Annexe A.6).");
                continue;
            }

            version.AjouterLigne(sens.Id, taille.Id, categorie.Id, l.Acconage, l.Relevage, l.TarifJourBranchement);
        }

        if (problemes.Count == 0)
        {
            version.ValiderRepartitionStationnement();
        }

        erreurs = problemes;
        return version;
    }

    private static void AppliquerParametres(
        VersionGrilleTarifaire version,
        ParametresGrilleSaisie parametres,
        ReferentielsTarifaires referentiels,
        List<string> problemes)
    {
        foreach (var m in parametres.Majorations)
        {
            version.DefinirMajoration(m.Type, m.Taux);
        }

        foreach (var s in parametres.SeuilsPoids)
        {
            var taille = referentiels.TailleParCode(s.TailleCode);
            if (taille is null)
            {
                problemes.Add($"Seuil de poids : taille de conteneur « {s.TailleCode} » inconnue du référentiel.");
                continue;
            }

            version.DefinirSeuilPoids(taille.Id, s.SeuilPoidsTonnes);
        }

        foreach (var t in parametres.Tranches)
        {
            version.AjouterTranche(t.BorneMinJour, t.BorneMaxJour, t.TarifJournalier);
        }

        foreach (var f in parametres.Franchises)
        {
            var sens = referentiels.SensParCode(f.SensCode);
            if (sens is null)
            {
                problemes.Add($"Franchise de stationnement : sens d'opération « {f.SensCode} » inconnu du référentiel.");
                continue;
            }

            version.DefinirFranchiseStationnement(sens.Id, f.NombreJours);
        }

        foreach (var q in parametres.QuotesParts)
        {
            version.DefinirQuotePart(q.Beneficiaire, q.Pourcentage);
        }
    }
}
