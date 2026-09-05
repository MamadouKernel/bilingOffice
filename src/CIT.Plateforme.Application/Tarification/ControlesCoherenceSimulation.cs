using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Tarification.Referentiels;

namespace CIT.Plateforme.Application.Tarification;

public sealed record ConteneurValidationRequete(
    Guid TailleConteneurId,
    Guid TypeConteneurId,
    Guid CategorieMarchandiseId,
    bool AvecBranchementFrigorifique,
    decimal PoidsTonnes);

public sealed record ValidationCoherenceResultat(
    bool EstValide,
    IReadOnlyList<string> Alertes,
    IReadOnlyList<string> MessagesAide);

/// <summary>
/// Contrat C13-07 (SIM-11) : contrôles de cohérence à la saisie, avec messages d'aide contextuels.
///
/// CE QUE LA NORMALISATION A RETIRÉ D'ICI. Deux contrôles n'ont plus lieu d'être :
/// <list type="bullet">
///   <item>« taille et type reconnus » — ils sont désormais choisis dans un référentiel, donc
///   nécessairement reconnus ; l'identifiant inconnu est une erreur de programmation, traitée par
///   <see cref="ReferentielsTarifaires"/>, pas un message d'aide ;</item>
///   <item>« catégorie compatible avec le sens », qui cherchait les mots « Import » ou « Export »
///   dans un libellé saisi librement. C'est maintenant une clé étrangère : une catégorie
///   <b>appartient</b> à un sens (Annexe A.6). Le contrôle reste, mais sur la structure plutôt
///   que sur une heuristique de chaîne de caractères.</item>
/// </list>
/// </summary>
public sealed class ControlesCoherenceSimulation
{
    /// <summary>Masse maximale admissible par taille, exprimée par code de référentiel. Une
    /// taille inconnue de cette table n'est pas contrôlée plutôt que contrôlée au hasard.</summary>
    private static readonly Dictionary<string, decimal> PoidsMaximalParTaille = new(StringComparer.OrdinalIgnoreCase)
    {
        ["20"] = 32m,
        ["40"] = 40m,
    };

    /// <summary>Contrôles purs, sur des référentiels déjà résolus.</summary>
    public ValidationCoherenceResultat Executer(
        Guid sensOperationId,
        DateOnly dateArrivee,
        DateOnly dateEnlevement,
        IReadOnlyList<ConteneurValidationRequete> conteneurs,
        ReferentielsTarifaires referentiels)
    {
        ArgumentNullException.ThrowIfNull(referentiels);

        var alertes = new List<string>();
        var aides = new List<string>();

        // 1. CDC SIM-11 : cohérence chronologique des dates de séjour.
        if (dateEnlevement < dateArrivee)
        {
            alertes.Add("La date d'enlèvement prévisionnel ne peut pas être antérieure à la date d'arrivée.");
        }
        else
        {
            var duree = dateEnlevement.DayNumber - dateArrivee.DayNumber;
            aides.Add(duree > 10
                ? $"Séjour prévu de {duree} jours : des frais de stationnement prolongé s'appliqueront au-delà de la franchise."
                : $"Séjour prévu de {duree} jours dans la limite de la franchise standard.");
        }

        // 2. Présence minimale de conteneurs.
        if (conteneurs is null || conteneurs.Count == 0)
        {
            alertes.Add("Au moins un conteneur doit être configuré.");
            return new ValidationCoherenceResultat(false, alertes, aides);
        }

        foreach (var c in conteneurs)
        {
            var taille = referentiels.Taille(c.TailleConteneurId);
            var type = referentiels.Type(c.TypeConteneurId);
            var categorie = referentiels.Categorie(c.CategorieMarchandiseId);

            // 3. CDC SIM-11 : la catégorie appartient au sens du dossier. Contrôle structurel,
            // et non plus une recherche des mots « Import » / « Export » dans un libellé libre.
            if (categorie.SensOperationId != sensOperationId)
            {
                var sensDeLaCategorie = referentiels.Sens(categorie.SensOperationId);
                alertes.Add(
                    $"La catégorie « {categorie.Code} » relève du sens « {sensDeLaCategorie.Libelle} » "
                    + $"et ne s'applique pas à « {referentiels.Sens(sensOperationId).Libelle} ».");
            }

            // 4. CDC SIM-11 : exclusivité du branchement frigorifique. La condition est portée par
            // le référentiel (RG-SIM-05), pas par le nom du type.
            if (c.AvecBranchementFrigorifique && !type.DeclencheBranchementFrigorifique)
            {
                alertes.Add(
                    $"L'option branchement frigorifique ne s'applique pas à un conteneur de type « {type.Libelle} ».");
            }

            // 5. Limite de masse admissible.
            if (PoidsMaximalParTaille.TryGetValue(taille.Code, out var maxPoids) && c.PoidsTonnes > maxPoids)
            {
                alertes.Add(
                    $"Le poids déclaré ({c.PoidsTonnes} t) dépasse la capacité maximale admissible "
                    + $"({maxPoids} t) pour un conteneur {taille.Libelle}.");
            }

            if (type.DeclencheBranchementFrigorifique && !c.AvecBranchementFrigorifique)
            {
                aides.Add($"Note : le conteneur {type.Libelle} est configuré sans branchement frigorifique sur parc.");
            }
        }

        return new ValidationCoherenceResultat(alertes.Count == 0, alertes, aides);
    }
}

/// <summary>
/// Résout les référentiels puis délègue à <see cref="ControlesCoherenceSimulation"/>.
///
/// POURQUOI CE HANDLER EXISTE. Depuis que les référentiels sont des tables (analyse Merise § 3.3),
/// les contrôles ont besoin de les consulter. Or une page routable n'a pas le droit d'injecter un
/// dépôt — garde d'architecture vérifiée par <c>SurfaceRoutableTests</c>. Plutôt que d'ouvrir une
/// brèche dans cette règle, ou de doter la requête pure d'une dépendance qu'aucun de ses tests
/// n'utilise, le chargement est isolé ici.
/// </summary>
public sealed class ValiderCoherenceSimulationQuery(
    IReferentielsTarifairesRepository referentielsRepository,
    ControlesCoherenceSimulation controles)
{
    public async Task<ValidationCoherenceResultat> ExecuterAsync(
        Guid sensOperationId,
        DateOnly dateArrivee,
        DateOnly dateEnlevement,
        IReadOnlyList<ConteneurValidationRequete> conteneurs,
        CancellationToken ct = default)
    {
        var referentiels = await referentielsRepository.ChargerAsync(ct).ConfigureAwait(false);
        return controles.Executer(sensOperationId, dateArrivee, dateEnlevement, conteneurs, referentiels);
    }
}
