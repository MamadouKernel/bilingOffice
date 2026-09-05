namespace CIT.Plateforme.Domain.Tarification.Parametres;

/// <summary>
/// Seuil de poids déclenchant la majoration « lourd », pour un couple (grille, taille) —
/// Annexe A.2, RG-SIM-06 : « 15 t en 20", 26 t en 40" ».
///
/// ÉTAIT DEUX COLONNES <c>SeuilPoidsLourd20Pieds</c> et <c>SeuilPoidsLourd40Pieds</c>. L'analyse
/// Merise § 3.3 (correction n° 2) le nomme pour ce qu'il était : un <b>repeating group déguisé en
/// colonnes multiples</b>. L'information « selon quelle taille ? » vivait dans le nom de la
/// colonne, si bien qu'ajouter une taille de conteneur imposait une migration et un déploiement —
/// exactement ce que SIM-06 interdit.
///
/// Une ligne par couple (grille, taille) : une taille nouvelle est désormais une <b>donnée</b>.
/// </summary>
public sealed class SeuilPoidsMajoration
{
    public Guid Id { get; private set; }

    public Guid VersionGrilleTarifaireId { get; private set; }

    public Guid TailleConteneurId { get; private set; }

    /// <summary>Poids en tonnes au-delà duquel la majoration s'applique (comparaison stricte).</summary>
    public decimal SeuilPoidsTonnes { get; private set; }

    private SeuilPoidsMajoration() { }

    public static SeuilPoidsMajoration Creer(Guid versionGrilleTarifaireId, Guid tailleConteneurId, decimal seuilPoidsTonnes)
    {
        if (tailleConteneurId == Guid.Empty)
            throw new ArgumentException("Un seuil de poids doit être rattaché à une taille de conteneur.", nameof(tailleConteneurId));
        if (seuilPoidsTonnes < 0)
            throw new ArgumentException("Un seuil de poids ne peut pas être négatif.", nameof(seuilPoidsTonnes));

        return new SeuilPoidsMajoration
        {
            Id = Guid.NewGuid(),
            VersionGrilleTarifaireId = versionGrilleTarifaireId,
            TailleConteneurId = tailleConteneurId,
            SeuilPoidsTonnes = seuilPoidsTonnes,
        };
    }
}
