using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.CustomerComplaint;

public sealed record CreerCategorieReclamationRequete(Guid AppelantId, string Code, string Libelle, Guid? FileCibleDefautId);

/// <summary>
/// REMÉDIATION FINALE REC-02 (closure-review.md "REMÉDIATION FINALE REC-02 — AJOUT CATÉGORIE") :
/// le CDC (§6.19, "Catégories de réclamation" listées comme administrables sans développement)
/// exige "ajout/modification" — <see cref="ModifierCategoriesReclamationCommand"/> (C7-05) ne
/// couvrait que la modification d'une catégorie déjà existante.
/// </summary>
public sealed class CreerCategorieReclamationCommand(
    ICategorieReclamationRepository categorieRepository,
    GardeFileCibleReclamation gardeFileCible,
    IAutorisationService autorisationService,
    IJournalisationService journalisationService,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> ExecuterAsync(CreerCategorieReclamationRequete requete, CancellationToken ct = default)
    {
        await autorisationService.ExigerAsync(requete.AppelantId, Permission.AdministrationCategoriesReclamation, null, ct).ConfigureAwait(false);

        var existante = await categorieRepository.ObtenirParCodeAsync(requete.Code, ct).ConfigureAwait(false);
        if (existante is not null)
        {
            throw new InvalidOperationException($"Une catégorie avec le code '{requete.Code}' existe déjà.");
        }

        // REMÉDIATION RECETTE UAT (D-14) : la file cible est contrôlée ici, à la configuration —
        // pas devant le client au moment où il soumet sa réclamation.
        await gardeFileCible.ExigerEligibleAsync(requete.FileCibleDefautId, ct).ConfigureAwait(false);

        // CategorieReclamation.Creer valide déjà la non-vacuité du code et du libellé (invariant
        // existant, réutilisé — REC-REM aucun nouvel invariant inventé).
        var categorie = CategorieReclamation.Creer(requete.Code, requete.Libelle, requete.FileCibleDefautId);
        await categorieRepository.AjouterAsync(categorie, ct).ConfigureAwait(false);

        await journalisationService.ConsignerAuditAsync(
            requete.AppelantId, "CategorieReclamationCreee", nameof(CategorieReclamation), categorie.Id, null, null, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return categorie.Id;
    }
}
