namespace CIT.Plateforme.Domain.Notifications;

/// <summary>
/// FEATURE 009 (T019) — COM-03 : « Les événements déclencheurs et les modèles de messages sont
/// paramétrables par l'administrateur fonctionnel (par module, par étape, par canal) ».
/// Le CDC §20 fait du « modèle » un attribut exigé de la notification, et le §19 le range parmi
/// les référentiels paramétrables.
///
/// Patron du référentiel administrable de F006 (<c>CategorieReclamation</c>, research.md D4) :
/// fabrique validante, mise à jour, activation/désactivation, JAMAIS de suppression physique.
///
/// AUCUN contenu métier n'est figé par le domaine : les gabarits sont une donnée
/// d'administration, dont les textes restent à fournir avant recette (CLAR-NOT-05).
/// </summary>
public sealed class ModeleNotification
{
    public Guid Id { get; private set; }

    /// <summary>Type d'événement déclencheur (ex. <c>TicketCree</c>). Avec <see cref="Canal"/>,
    /// forme la clé fonctionnelle du modèle — « par module, par étape, par canal ».</summary>
    public string TypeEvenement { get; private set; } = string.Empty;

    public CanalNotification Canal { get; private set; }

    public string GabaritTitre { get; private set; } = string.Empty;

    public string GabaritCorps { get; private set; } = string.Empty;

    public bool EstActif { get; private set; }

    /// <summary>Version anglaise facultative : sans elle, un destinataire anglophone reçoit le français.</summary>
    public string? GabaritTitreEn { get; private set; }

    public string? GabaritCorpsEn { get; private set; }

    private ModeleNotification() { }

    public static ModeleNotification Creer(string typeEvenement, CanalNotification canal, string gabaritTitre, string gabaritCorps)
    {
        ExigerNonVide(typeEvenement, nameof(typeEvenement), "Le type d'événement est obligatoire.");
        ExigerNonVide(gabaritTitre, nameof(gabaritTitre), "Le gabarit de titre est obligatoire.");
        ExigerNonVide(gabaritCorps, nameof(gabaritCorps), "Le gabarit de corps est obligatoire.");

        return new ModeleNotification
        {
            Id = Guid.NewGuid(),
            TypeEvenement = typeEvenement,
            Canal = canal,
            GabaritTitre = gabaritTitre,
            GabaritCorps = gabaritCorps,
            EstActif = true,
        };
    }

    public void MettreAJour(string gabaritTitre, string gabaritCorps)
    {
        ExigerNonVide(gabaritTitre, nameof(gabaritTitre), "Le gabarit de titre est obligatoire.");
        ExigerNonVide(gabaritCorps, nameof(gabaritCorps), "Le gabarit de corps est obligatoire.");

        GabaritTitre = gabaritTitre;
        GabaritCorps = gabaritCorps;
    }

    public void DefinirAnglais(string? gabaritTitreEn, string? gabaritCorpsEn)
    {
        GabaritTitreEn = string.IsNullOrWhiteSpace(gabaritTitreEn) ? null : gabaritTitreEn.Trim();
        GabaritCorpsEn = string.IsNullOrWhiteSpace(gabaritCorpsEn) ? null : gabaritCorpsEn;
    }

    /// <summary>Titre dans la langue du destinataire (repli sur le français).</summary>
    public string Titre(bool anglais) => anglais && GabaritTitreEn is not null ? GabaritTitreEn : GabaritTitre;

    /// <summary>Corps dans la langue du destinataire (repli sur le français).</summary>
    public string Corps(bool anglais) => anglais && GabaritCorpsEn is not null ? GabaritCorpsEn : GabaritCorps;

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;

    private static void ExigerNonVide(string valeur, string parametre, string message)
    {
        if (string.IsNullOrWhiteSpace(valeur))
        {
            throw new ArgumentException(message, parametre);
        }
    }
}
