namespace CIT.Plateforme.Domain.Tarification.Parametres;

/// <summary>
/// Franchise de stationnement (en jours) pour un couple (grille, sens) — Annexe A.4, RG-SIM-08 :
/// « 11 jours en import local et 21 jours en import transit ».
///
/// ÉTAIT DEUX COLONNES <c>FranchiseStationnementImportLocalJours</c> et
/// <c>FranchiseStationnementImportTransitJours</c> — le second repeating group relevé par
/// l'analyse Merise § 3.3 (correction n° 2).
///
/// L'ABSENCE DE LIGNE RESTE PORTEUSE DE SENS, et c'est volontaire : RG-SIM-08 précise que la
/// franchise « n'est pas appliquée à l'export dans l'outil actuel ». Un sens sans franchise
/// enregistrée n'est donc pas soumis au stationnement. Le moteur ne teste jamais la valeur d'un
/// sens : il constate la présence ou l'absence d'une ligne.
/// </summary>
public sealed class FranchiseStationnement
{
    public Guid Id { get; private set; }

    public Guid VersionGrilleTarifaireId { get; private set; }

    public Guid SensOperationId { get; private set; }

    public int NombreJours { get; private set; }

    private FranchiseStationnement() { }

    public static FranchiseStationnement Creer(Guid versionGrilleTarifaireId, Guid sensOperationId, int nombreJours)
    {
        if (sensOperationId == Guid.Empty)
            throw new ArgumentException("Une franchise de stationnement doit être rattachée à un sens d'opération.", nameof(sensOperationId));
        if (nombreJours < 0)
            throw new ArgumentException("Une franchise de stationnement ne peut pas être négative.", nameof(nombreJours));

        return new FranchiseStationnement
        {
            Id = Guid.NewGuid(),
            VersionGrilleTarifaireId = versionGrilleTarifaireId,
            SensOperationId = sensOperationId,
            NombreJours = nombreJours,
        };
    }
}
