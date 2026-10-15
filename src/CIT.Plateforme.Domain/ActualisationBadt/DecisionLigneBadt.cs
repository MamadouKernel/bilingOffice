namespace CIT.Plateforme.Domain.ActualisationBadt;

/// <summary>
/// EXTENSION FEATURE 010 (Actualisation BADT) : décision portée par une ligne conteneur d'une
/// demande d'actualisation. La décision est prise au grain du conteneur (un même dossier peut mêler
/// conteneurs accordés et refusés). Seul un conteneur <see cref="Accordee"/> alimente le fichier
/// Navis d'actualisation.
/// </summary>
public enum DecisionLigneBadt
{
    /// <summary>Aucune décision prise : conteneur en cours d'analyse ou d'escalade.</summary>
    EnAttente,

    /// <summary>Actualisation accordée (N jours fixés par le Décideur) : alimente le fichier Navis.</summary>
    Accordee,

    /// <summary>Rejet (agent, cause non terminale) ou refus (Décideur) : aucune actualisation.</summary>
    Refusee,
}
