using CIT.Plateforme.Domain.Tarification;

namespace CIT.Plateforme.Application.Tarification;

public sealed record ConteneurValidationRequete(
    TailleConteneur Taille,
    TypeConteneur Type,
    string CategorieMarchandise,
    bool AvecBranchementFrigorifique,
    decimal PoidsTonnes);

public sealed record ValidationCoherenceResultat(
    bool EstValide,
    IReadOnlyList<string> Alertes,
    IReadOnlyList<string> MessagesAide);

/// <summary>
/// Contrat C13-07 (SIM-11) : Contrôles de cohérence à la saisie (ex. taille et type reconnus,
/// catégorie compatible avec le sens, dates, branchement frigorifique) avec messages d'aide contextuels.
/// </summary>
public sealed class ValiderCoherenceSimulationQuery
{
    public ValidationCoherenceResultat Executer(
        SensTrafic sens,
        DateOnly dateArrivee,
        DateOnly dateEnlevement,
        IReadOnlyList<ConteneurValidationRequete> conteneurs)
    {
        var alertes = new List<string>();
        var aides = new List<string>();

        // 1. CDC SIM-11 : Cohérence chronologique des dates de séjour
        if (dateEnlevement < dateArrivee)
        {
            alertes.Add("La date d'enlèvement prévisionnel ne peut pas être antérieure à la date d'arrivée.");
        }
        else
        {
            var duree = dateEnlevement.DayNumber - dateArrivee.DayNumber;
            if (duree > 10)
            {
                aides.Add($"Séjour prévu de {duree} jours : des frais de stationnement prolongé s'appliqueront au-delà de la franchise.");
            }
            else
            {
                aides.Add($"Séjour prévu de {duree} jours dans la limite de la franchise standard.");
            }
        }

        // 2. Présence minimale de conteneurs
        if (conteneurs is null || conteneurs.Count == 0)
        {
            alertes.Add("Au moins un conteneur doit être configuré.");
            return new ValidationCoherenceResultat(false, alertes, aides);
        }

        foreach (var c in conteneurs)
        {
            // 3. CDC SIM-11 : Taille et type reconnus
            if (!Enum.IsDefined(typeof(TailleConteneur), c.Taille) || !Enum.IsDefined(typeof(TypeConteneur), c.Type))
            {
                alertes.Add("Taille ou type de conteneur non reconnu par le référentiel tarifaire.");
            }

            // 4. CDC SIM-11 : Catégorie compatible avec le sens de trafic
            var categorie = c.CategorieMarchandise ?? string.Empty;
            if ((sens == SensTrafic.ImportLocal || sens == SensTrafic.ImportTransit) &&
                categorie.Contains("Export", StringComparison.OrdinalIgnoreCase))
            {
                alertes.Add($"La catégorie '{categorie}' est incompatible avec un flux d'importation.");
            }
            else if ((sens == SensTrafic.ExportLocal || sens == SensTrafic.ExportTransit) &&
                     categorie.Contains("Import", StringComparison.OrdinalIgnoreCase))
            {
                alertes.Add($"La catégorie '{categorie}' est incompatible avec un flux d'exportation.");
            }

            // 5. CDC SIM-11 : Exclusivité Reefer pour le branchement frigorifique
            if (c.AvecBranchementFrigorifique && c.Type != TypeConteneur.Reefer)
            {
                alertes.Add("L'option branchement frigorifique est strictement réservée aux conteneurs de type Reefer.");
            }

            // 6. Limite de masse admissible
            var maxPoids = c.Taille == TailleConteneur.QuarantePieds ? 40m : 32m;
            if (c.PoidsTonnes > maxPoids)
            {
                alertes.Add($"Le poids déclaré ({c.PoidsTonnes}t) dépasse la capacité maximale admissible ({maxPoids}t) pour un conteneur {c.Taille}.");
            }

            if (c.Type == TypeConteneur.Reefer && !c.AvecBranchementFrigorifique)
            {
                aides.Add("Note : Le conteneur Reefer est configuré sans branchement frigorifique sur parc.");
            }
        }

        return new ValidationCoherenceResultat(alertes.Count == 0, alertes, aides);
    }
}
