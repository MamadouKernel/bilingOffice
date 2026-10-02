using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Reclamations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>
/// PARAMÉTRAGE MÉTIER DE DÉPART — files de traitement, catégories de réclamation et pièces exigées
/// à la création de compte, approvisionnés dans TOUS les environnements.
///
/// INCIDENT PRODUCTION 02/10/2026 : ce paramétrage n'était amorcé que par
/// <see cref="DevelopmentSeeder"/>. Une production sans lui rejette toute soumission (« File
/// « Pilote Manifeste » (module BillingImport) non paramétrée ») : les commandes de soumission
/// cherchent leur file d'entrée par couple (Module, Nom) exact.
///
/// STRICTEMENT ADDITIF : chaque file et chaque pièce n'est créée que si elle est absente ; les
/// catégories ne sont amorcées que si la table est entièrement vide. Rien de ce qu'un
/// administrateur a créé, renommé ou paramétré n'est réécrit.
/// </summary>
public sealed class ProvisionnementParametrageMetier(
    PlateformeDbContext contexte,
    ILogger<ProvisionnementParametrageMetier> journal)
{
    /// <summary>Files d'entrée et d'étape attendues par les workflows (couple Module, Nom).</summary>
    private static readonly (string Module, string Nom)[] Files =
    [
        ("BillingImport", "Pilote Manifeste"),
        ("BillingImport", "Facturation"),
        ("BillingImport", "Validation Paiement"),
        ("BillingImport", "Contrôle Documentaire"),
        ("BillingImport", "Comptabilité Client"),
        ("BillingImport", "File IES"),
        ("BillingExport", "Back Office Export"),
        ("BillingExport", "Facturation"),
        ("BillingExport", "Validation Paiement"),
        ("BillingExport", "Contrôle Documentaire"),
        ("BillingExport", "Comptabilité Client"),
        ("BillingExport", "File IES"),
        ("CustomerComplaint", "Support Opérationnel"),
        ("AvoirExoneration", "Avoir & Exonération"),
        ("Enrolement", "Agent Gate"),
    ];

    private static readonly (string Code, string Libelle)[] CategoriesReclamation =
    [
        ("FACT", "Problème de facturation"),
        ("TECH", "Problème technique"),
        ("INFO", "Demande d'information"),
    ];

    private static readonly (string Code, string Libelle, bool Obligatoire, int Ordre)[] PiecesExigees =
    [
        ("RCCM", "Registre du Commerce et du Crédit Mobilier (RCCM)", false, 1),
        ("DFE", "Déclaration Fiscale d'Existence (DFE / Fiche Contribuable)", false, 2),
        ("CNI_REP", "Pièce d'Identité du Représentant Légal (CNI / Passeport)", false, 3),
        ("RIB", "Relevé d'Identité Bancaire (RIB) Entreprise", false, 4),
    ];

    public async Task ExecuterAsync(CancellationToken ct = default)
    {
        foreach (var (module, nom) in Files)
        {
            if (!await contexte.Files.AnyAsync(f => f.Module == module && f.Nom == nom, ct).ConfigureAwait(false))
            {
                contexte.Files.Add(FileDeTraitement.Creer(module, nom));
                journal.LogInformation("File « {Nom} » (module {Module}) approvisionnée.", nom, module);
            }
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);

        if (!await contexte.CategoriesReclamation.AnyAsync(ct).ConfigureAwait(false))
        {
            var fileSupportId = await contexte.Files
                .Where(f => f.Module == "CustomerComplaint" && f.Nom == "Support Opérationnel")
                .Select(f => f.Id)
                .FirstAsync(ct)
                .ConfigureAwait(false);

            foreach (var (code, libelle) in CategoriesReclamation)
            {
                contexte.CategoriesReclamation.Add(CategorieReclamation.Creer(code, libelle, fileSupportId));
            }

            journal.LogInformation("Catégories de réclamation de départ approvisionnées.");
        }

        foreach (var (code, libelle, obligatoire, ordre) in PiecesExigees)
        {
            if (!await contexte.PiecesExigeesCompte.AnyAsync(p => p.Code == code, ct).ConfigureAwait(false))
            {
                contexte.PiecesExigeesCompte.Add(PieceExigeeCompte.Definir(code, libelle, obligatoire, ordre));
            }
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
