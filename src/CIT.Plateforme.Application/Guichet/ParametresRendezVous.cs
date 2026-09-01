using System;

namespace CIT.Plateforme.Application.Guichet;

/// <summary>
/// FEATURE 010 (research.md D4, tasks.md Section 2) — paramètres configurables de gestion des créneaux.
/// VALEURS DE DÉVELOPPEMENT / DÉFAUT STRICTEMENT NON CONTRACTUELLES.
/// Toutes ces valeurs proviennent de la configuration (section Guichet:RendezVous) ou de ces défauts
/// raisonnables, et ne constituent pas des invariants domaine.
/// </summary>
public sealed record ParametresRendezVous(
    int CapaciteParCreneau = 5,
    TimeSpan DureeCreneau = default,
    TimeSpan HeureOuverture = default,
    TimeSpan HeureFermeture = default,
    int LimiteResultatsRecherche = 50
)
{
    public TimeSpan DureeCreneauEffective => DureeCreneau == default ? TimeSpan.FromMinutes(30) : DureeCreneau;
    public TimeSpan HeureOuvertureEffective => HeureOuverture == default ? new TimeSpan(8, 0, 0) : HeureOuverture;
    public TimeSpan HeureFermetureEffective => HeureFermeture == default ? new TimeSpan(16, 30, 0) : HeureFermeture;

    public void Valider()
    {
        if (CapaciteParCreneau <= 0)
            throw new InvalidOperationException("La capacité par créneau doit être strictement positive.");

        if (DureeCreneauEffective <= TimeSpan.Zero)
            throw new InvalidOperationException("La durée d'un créneau doit être strictement positive.");

        if (HeureOuvertureEffective >= HeureFermetureEffective)
            throw new InvalidOperationException("L'heure d'ouverture doit précéder l'heure de fermeture.");

        if (LimiteResultatsRecherche <= 0)
            throw new InvalidOperationException("La limite de résultats de recherche doit être strictement positive.");
    }
}

public sealed class CreneauCompletException : Exception
{
    public CreneauCompletException(DateOnly date, TimeSpan heureDebut)
        : base($"Le créneau horaire du {date:dd/MM/yyyy} à {heureDebut:hh\\:mm} est complet. Veuillez choisir un autre créneau disponible.")
    {
    }
}
