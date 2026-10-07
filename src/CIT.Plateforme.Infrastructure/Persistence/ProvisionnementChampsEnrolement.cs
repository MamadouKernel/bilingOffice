using CIT.Plateforme.Domain.Enrolement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// Valeurs métier confirmées (07/10/2026, responsable Agent Gate) : le formulaire d'enrôlement
/// CHAUFFEUR demande NOM, Prénoms, date d'expiration du permis (obligatoires), et catégorie du
/// permis (optionnelle, liste). Les photos (permis recto-verso, pièce d'identité) sont déposées via
/// les pièces jointes. On amorce ces champs une seule fois : si un champ Chauffeur existe déjà
/// (configuré par l'admin), on ne touche à rien — même prudence que les autres provisionnements.
/// </summary>
public sealed class ProvisionnementChampsEnrolement(
    PlateformeDbContext contexte,
    ILogger<ProvisionnementChampsEnrolement> journal)
{
    private static readonly (string Code, string Libelle, bool Obligatoire, int Ordre, TypeChampEnrolement Type, string? Options)[] ChampsChauffeur =
    [
        ("chauffeur.nom", "NOM", true, 1, TypeChampEnrolement.Texte, null),
        ("chauffeur.prenoms", "Prénoms", true, 2, TypeChampEnrolement.Texte, null),
        ("chauffeur.telephone", "Numéro de téléphone", true, 3, TypeChampEnrolement.Texte, null),
        ("chauffeur.permis_expiration", "Date d'expiration du permis", true, 4, TypeChampEnrolement.Date, null),
        ("chauffeur.permis_categorie", "Catégorie du permis", false, 5, TypeChampEnrolement.Liste, "B;C;D;E;F"),
    ];

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        // Idempotent PAR CODE : on ajoute les champs standard manquants (y compris ceux introduits
        // après un premier amorçage). Un code déjà présent — même désactivé par l'admin — n'est pas
        // recréé : l'administrateur garde la main.
        var codesExistants = (await contexte.ChampsFormulaireEnrolement.Select(c => c.Code).ToListAsync(ct).ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);

        var aCreer = ChampsChauffeur.Where(c => !codesExistants.Contains(c.Code)).ToList();
        if (aCreer.Count == 0)
        {
            return;
        }

        foreach (var (code, libelle, obligatoire, ordre, type, options) in aCreer)
        {
            contexte.ChampsFormulaireEnrolement.Add(
                ChampFormulaireEnrolement.Definir(code, libelle, obligatoire, ordre, TypeObjetEnrolement.Chauffeur, type, options));
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
        journal.LogInformation("{Nombre} champ(s) d'enrôlement chauffeur approvisionné(s).", aCreer.Count);
    }
}
