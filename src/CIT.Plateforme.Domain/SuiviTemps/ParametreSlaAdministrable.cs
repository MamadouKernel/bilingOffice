namespace CIT.Plateforme.Domain.SuiviTemps;

/// <summary>
/// Contrat C-05 — paramètre SLA administrable par module/type de demande (SLA-01, SLA-05).
/// Aucune valeur cible n'est fixée par défaut par le socle (PC-12) : tant qu'aucune entrée n'est
/// administrée pour un module/type donné, aucune surveillance SLA n'a lieu pour ces tickets — ce
/// n'est jamais remplacé par une valeur inventée.
/// </summary>
public sealed class ParametreSlaAdministrable
{
    public Guid Id { get; private set; }

    public string Module { get; private set; } = string.Empty;

    public string TypeDemande { get; private set; } = string.Empty;

    public TimeSpan DureeCible { get; private set; }

    public double SeuilInformation { get; private set; }

    public double SeuilAlerte { get; private set; }

    /// <summary>Horaires ouvrés — sérialisés simplement (heures, jours, fériés) pour ce paramètre.
    /// Un calendrier « toujours ouvert » est utilisé si non renseigné explicitement (voir
    /// <see cref="CalendrierOuvre.ToujoursOuvert"/>), jamais un horaire métier inventé.</summary>
    public bool UtiliserCalendrierToujoursOuvert { get; private set; } = true;

    /// <summary>REMÉDIATION AUDIT (post-implementation-audit.md §3.B) : heure de début du
    /// calendrier ouvré administré pour ce module/type, utilisée uniquement quand
    /// <see cref="UtiliserCalendrierToujoursOuvert"/> est faux. Aucune valeur métier n'est fixée
    /// par défaut au-delà du calendrier « toujours ouvert » déjà existant (PC-12 non inventé) :
    /// ces champs ne sont renseignés que par un administrateur, via <see cref="AppliquerCalendrierOuvre"/>.</summary>
    public TimeOnly HeureDebut { get; private set; } = TimeOnly.MinValue;

    public TimeOnly HeureFin { get; private set; } = TimeOnly.MaxValue;

    /// <summary>Masque de bits des jours ouvrés (bit i = <see cref="DayOfWeek"/> valant i est
    /// ouvré) — tous les jours par défaut, cohérent avec le calendrier « toujours ouvert ».
    /// Type scalaire volontairement choisi (pas de collection convertie) pour ne pas reproduire le
    /// défaut de comparateur de valeur documenté en §3.D de l'audit.</summary>
    public byte JoursOuvresBitmask { get; private set; } = 0b0111_1111;

    private ParametreSlaAdministrable() { }

    public static ParametreSlaAdministrable Definir(string module, string typeDemande, TimeSpan dureeCible, double seuilInformation, double seuilAlerte) => new()
    {
        Id = Guid.NewGuid(),
        Module = module,
        TypeDemande = typeDemande,
        DureeCible = dureeCible,
        SeuilInformation = seuilInformation,
        SeuilAlerte = seuilAlerte,
    };

    /// <summary>REMÉDIATION AUDIT : applique un calendrier ouvré réel (heures + jours) à ce
    /// paramètre SLA, et bascule <see cref="UtiliserCalendrierToujoursOuvert"/> à faux. Les
    /// valeurs elles-mêmes (heures, jours) sont fournies par l'appelant (administrateur
    /// fonctionnel ou test) — aucune valeur n'est inventée ici (PC-12 reste un paramètre).</summary>
    public void AppliquerCalendrierOuvre(TimeOnly heureDebut, TimeOnly heureFin, IEnumerable<DayOfWeek> joursOuvres)
    {
        HeureDebut = heureDebut;
        HeureFin = heureFin;

        byte masque = 0;
        foreach (var jour in joursOuvres)
        {
            masque |= (byte)(1 << (int)jour);
        }

        JoursOuvresBitmask = masque;
        UtiliserCalendrierToujoursOuvert = false;
    }

    public ParametresSla VersParametresSla() => new(DureeCible, SeuilInformation, SeuilAlerte);

    /// <summary>Reconstruit le calendrier ouvré pur (Domain.SuiviTemps.CalendrierOuvre) à partir
    /// des champs administrés — aucun jour férié administré ici (PC-12 non tranché, non inventé) :
    /// seuls les horaires/jours de la semaine sont couverts par cette remédiation.</summary>
    public CalendrierOuvre ConstruireCalendrierOuvre()
    {
        if (UtiliserCalendrierToujoursOuvert)
        {
            return CalendrierOuvre.ToujoursOuvert;
        }

        var jours = new HashSet<DayOfWeek>();
        foreach (DayOfWeek jour in Enum.GetValues<DayOfWeek>())
        {
            if ((JoursOuvresBitmask & (1 << (int)jour)) != 0)
            {
                jours.Add(jour);
            }
        }

        return new CalendrierOuvre(HeureDebut, HeureFin, jours, new HashSet<DateOnly>());
    }
}
