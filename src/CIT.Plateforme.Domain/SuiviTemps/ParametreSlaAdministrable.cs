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

    /// <summary>
    /// SLA-02 — quatrième niveau exigé par le CDC : « un seuil d'information (ex. 50 %), un seuil
    /// d'alerte (ex. 80 %), le dépassement (100 %) et <b>l'escalade au-delà</b> ».
    ///
    /// Le dépassement est la durée cible elle-même et n'a pas besoin d'être stocké. L'escalade,
    /// si : elle se situe AU-DELÀ de 100 %, et sa valeur est une décision métier. <c>0</c>
    /// signifie « aucune escalade administrée » — jamais une valeur inventée (PC-12).
    /// </summary>
    public double SeuilEscalade { get; private set; }

    /// <summary>
    /// SLA-01 — le CDC exige un paramétrage « par module, type de demande, <b>étape, file,
    /// priorité</b> ». <c>null</c> signifie « ce paramétrage ne discrimine pas sur cet axe » et
    /// s'applique donc plus largement.
    ///
    /// L'ÉTAPE EST MATÉRIALISÉE PAR LA FILE dans ce modèle : le MOT (§8.3 de
    /// <c>docs/85-analyse-merise.md</c>) décrit chaque étape du parcours comme une file — Contrôle
    /// BL, Facturation, Contrôle Documentaire. Introduire une entité « étape » distincte
    /// inventerait une notion que ni le CDC ni le modèle ne portent.
    /// </summary>
    public Guid? FileId { get; private set; }

    /// <summary>SLA-01 — axe « priorité ». <c>null</c> = ne discrimine pas.</summary>
    public Guid? PrioriteId { get; private set; }

    /// <summary>
    /// Départage deux paramétrages applicables : <b>le plus spécifique l'emporte</b>.
    ///
    /// Les poids sont des puissances de deux, ce qui rend l'ordre TOTAL — deux paramétrages
    /// distincts ne peuvent jamais être à égalité, et la résolution est donc déterministe. La
    /// file prime sur la priorité parce que le CDC les énumère dans cet ordre de portée
    /// décroissante (« étape, file, priorité »).
    ///
    /// `À VALIDER` — cet ordre de précédence est une convention technique déduite de
    /// l'énumération du CDC, non une règle qu'il énonce. Si le métier veut qu'un SLA de priorité
    /// prime sur un SLA de file, seuls ces poids changent.
    /// </summary>
    public int Specificite => (FileId.HasValue ? 2 : 0) + (PrioriteId.HasValue ? 1 : 0);

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

    /// <summary>
    /// SLA-01 — restreint ce paramétrage à une file et/ou une priorité. Appelé séparément de
    /// <see cref="Definir"/> pour que les paramétrages existants, qui ne discriminent sur aucun
    /// de ces axes, restent valides sans être réécrits.
    /// </summary>
    public void Cibler(Guid? fileId, Guid? prioriteId)
    {
        FileId = fileId;
        PrioriteId = prioriteId;
    }

    /// <summary>SLA-02 — fixe le seuil d'escalade. Au-delà de 100 % par définition : un seuil
    /// inférieur au dépassement n'aurait pas de sens, l'escalade suit le dépassement.</summary>
    public void FixerSeuilEscalade(double seuilEscalade)
    {
        if (seuilEscalade != 0 && seuilEscalade <= 1.0)
        {
            throw new ArgumentException(
                "Le seuil d'escalade se situe au-delà du dépassement (100 %) : une valeur inférieure ou " +
                "égale à 1,0 la placerait avant, ce que le CDC exclut. Utiliser 0 pour « aucune escalade ».",
                nameof(seuilEscalade));
        }

        SeuilEscalade = seuilEscalade;
    }

    public ParametresSla VersParametresSla() => new(DureeCible, SeuilInformation, SeuilAlerte, SeuilEscalade);

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
