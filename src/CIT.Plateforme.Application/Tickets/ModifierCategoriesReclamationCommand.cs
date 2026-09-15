using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Reclamations;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Tickets;

public sealed record ModifierCategoriesReclamationRequete(
    Guid AppelantId,
    Guid CategorieId,
    string Libelle,
    Guid? FileCibleDefautId,
    bool EstActif
);

public sealed class ModifierCategoriesReclamationCommand(
    ICategorieReclamationRepository categorieRepository,
    CIT.Plateforme.Application.CustomerComplaint.GardeFileCibleReclamation gardeFileCible,
    IAutorisationService autorisationService,
    IJournalisationService journalisationService,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(ModifierCategoriesReclamationRequete requete, CancellationToken ct = default)
    {
        await autorisationService.ExigerAsync(requete.AppelantId, Permission.AdministrationCategoriesReclamation, null, ct).ConfigureAwait(false);

        var categorie = await categorieRepository.ObtenirParIdAsync(requete.CategorieId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"La catégorie {requete.CategorieId} n'existe pas.");

        // REMÉDIATION RECETTE UAT (D-14) : même garde qu'à la création, même exemplaire.
        await gardeFileCible.ExigerEligibleAsync(requete.FileCibleDefautId, ct).ConfigureAwait(false);

        categorie.MettreAJour(requete.Libelle, requete.FileCibleDefautId);

        if (requete.EstActif && !categorie.EstActif)
        {
            categorie.Activer();
        }
        else if (!requete.EstActif && categorie.EstActif)
        {
            categorie.Desactiver();
        }

        await journalisationService.ConsignerAuditAsync(
            requete.AppelantId,
            "CategorieReclamationModifiee",
            nameof(CategorieReclamation),
            categorie.Id,
            null,
            null,
            ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
