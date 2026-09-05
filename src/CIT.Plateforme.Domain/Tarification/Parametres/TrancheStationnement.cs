namespace CIT.Plateforme.Domain.Tarification.Parametres;

/// <summary>
/// Tranche de stationnement à tarif journalier propre — Annexe A.4, RG-SIM-08 :
/// « 1-5 j : 850 XOF ; 6-15 j : 1 250 XOF ; 16 j et + : 1 850 XOF ».
///
/// ÉTAIT TROIS COLONNES de tarif (<c>TarifStationnementTranche1Jour</c> à <c>3Jour</c>) dont les
/// <b>bornes</b> — 5 et 15 — n'existaient nulle part comme donnée : elles étaient écrites en dur
/// dans le moteur de calcul (<c>Math.Min(5, …)</c>, <c>joursStationnement - 15</c>). Les tarifs
/// étaient administrables, le découpage ne l'était pas, alors que RG-SIM-11 range explicitement
/// les « bornes de tranches » parmi les paramètres administrables sans développement.
///
/// Une ligne par tranche, bornes comprises. <see cref="BorneMaxJour"/> nul signifie « et au-delà ».
/// </summary>
public sealed class TrancheStationnement
{
    public Guid Id { get; private set; }

    public Guid VersionGrilleTarifaireId { get; private set; }

    /// <summary>Premier jour taxable couvert par la tranche, à partir de 1 (borne incluse).</summary>
    public int BorneMinJour { get; private set; }

    /// <summary>Dernier jour couvert (borne incluse). <c>null</c> pour la tranche terminale.</summary>
    public int? BorneMaxJour { get; private set; }

    public decimal TarifJournalier { get; private set; }

    private TrancheStationnement() { }

    public static TrancheStationnement Creer(
        Guid versionGrilleTarifaireId, int borneMinJour, int? borneMaxJour, decimal tarifJournalier)
    {
        if (borneMinJour < 1)
            throw new ArgumentException("La borne minimale d'une tranche commence au premier jour taxable (1).", nameof(borneMinJour));
        if (borneMaxJour is int max && max < borneMinJour)
            throw new ArgumentException("La borne maximale d'une tranche ne peut pas précéder sa borne minimale.", nameof(borneMaxJour));
        if (tarifJournalier < 0)
            throw new ArgumentException("Un tarif journalier ne peut pas être négatif.", nameof(tarifJournalier));

        return new TrancheStationnement
        {
            Id = Guid.NewGuid(),
            VersionGrilleTarifaireId = versionGrilleTarifaireId,
            BorneMinJour = borneMinJour,
            BorneMaxJour = borneMaxJour,
            TarifJournalier = tarifJournalier,
        };
    }

    /// <summary>
    /// Nombre de jours de <paramref name="joursTaxables"/> qui tombent dans cette tranche.
    ///
    /// Intersection de l'intervalle [1, joursTaxables] avec [BorneMinJour, BorneMaxJour]. Écrit
    /// une fois ici plutôt que déroulé tranche par tranche dans le moteur : c'est ce qui permet
    /// d'ajouter une quatrième tranche sans toucher au code de calcul.
    /// </summary>
    public int JoursApplicables(int joursTaxables)
    {
        if (joursTaxables <= 0)
        {
            return 0;
        }

        var borneHaute = Math.Min(joursTaxables, BorneMaxJour ?? joursTaxables);
        return Math.Max(0, borneHaute - (BorneMinJour - 1));
    }
}
