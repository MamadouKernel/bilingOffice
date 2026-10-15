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
        ("ActualisationBadt", "Actualisation BADT"),
        ("Enrolement", "Agent Gate"),
    ];

    private static readonly (string Code, string Libelle)[] CategoriesReclamation =
    [
        ("FACT", "Problème de facturation"),
        ("TECH", "Problème technique"),
        ("INFO", "Demande d'information"),
    ];

    private static readonly (string Code, string Libelle, bool Obligatoire, int Ordre, Domain.Comptes.TypeClient? Type)[] PiecesExigees =
    [
        // Pièces légales de constitution : obligatoires par défaut (l'administration peut les assouplir).
        ("RCCM", "Registre du Commerce et du Crédit Mobilier (RCCM)", true, 1, null),
        ("DFE", "Déclaration Fiscale d'Existence (DFE / Fiche Contribuable)", true, 2, null),
        ("CNI_REP", "Pièce d'Identité du Représentant Légal (CNI / Passeport)", true, 3, null),
        ("RIB", "Relevé d'Identité Bancaire (RIB) Entreprise", false, 4, null),
        // Propre aux entreprises de transport (03/10/2026) ; rendue obligatoire au besoin par l'administration.
        ("AGREMENT_TRANSPORT", "Agrément / autorisation de transport routier de marchandises", false, 5, Domain.Comptes.TypeClient.Transporteur),
    ];

    // PRI-01 : jeu de niveaux de priorité de départ (exemple du CDC). Amorçage additif, inerte dès
    // qu'un niveau existe ; l'administration renomme, réordonne ou complète ensuite sans développement.
    private static readonly (string Libelle, int Rang)[] NiveauxPriorite =
    [
        ("Standard", 10),
        ("Urgent", 20),
        ("Critique", 30),
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

        foreach (var (code, libelle, obligatoire, ordre, type) in PiecesExigees)
        {
            if (!await contexte.PiecesExigeesCompte.AnyAsync(p => p.Code == code, ct).ConfigureAwait(false))
            {
                contexte.PiecesExigeesCompte.Add(PieceExigeeCompte.Definir(code, libelle, obligatoire, ordre, type));
            }
        }

        // PRI-01 : niveaux de priorité de départ, uniquement si le référentiel est vide (comme les
        // catégories). Sans eux, l'attribution automatique de priorité reste inconfigurable.
        if (!await contexte.NiveauxPriorite.AnyAsync(ct).ConfigureAwait(false))
        {
            foreach (var (libelle, rang) in NiveauxPriorite)
            {
                contexte.NiveauxPriorite.Add(Domain.Tickets.NiveauPriorite.Definir(libelle, rang));
            }

            journal.LogInformation("Niveaux de priorité de départ approvisionnés.");
        }

        await contexte.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
