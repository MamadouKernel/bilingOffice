namespace CIT.Plateforme.Domain.Formulaires;

/// <summary>
/// ERR-01 (Must) : Brouillon de formulaire permettant la sauvegarde et la reprise
/// d'une démarche sans perte de données saisies.
/// </summary>
public sealed class BrouillonSaisie
{
    public Guid Id { get; private set; }
    public Guid UtilisateurId { get; private set; }
    public Guid SocieteId { get; private set; }
    public string TypeFormulaire { get; private set; } = string.Empty;
    public string DonneesJson { get; private set; } = string.Empty;
    public DateTime DateCreationUtc { get; private set; }
    public DateTime DateDerniereModificationUtc { get; private set; }

    private BrouillonSaisie() { }

    public static BrouillonSaisie Creer(
        Guid id,
        Guid utilisateurId,
        Guid societeId,
        string typeFormulaire,
        string donneesJson,
        DateTime dateCreationUtc)
    {
        if (id == Guid.Empty) throw new ArgumentException("L'identifiant du brouillon ne peut pas être vide.", nameof(id));
        if (utilisateurId == Guid.Empty) throw new ArgumentException("L'identifiant utilisateur est requis.", nameof(utilisateurId));
        if (societeId == Guid.Empty) throw new ArgumentException("L'identifiant de la société est requis.", nameof(societeId));
        if (string.IsNullOrWhiteSpace(typeFormulaire)) throw new ArgumentException("Le type de formulaire est requis.", nameof(typeFormulaire));

        return new BrouillonSaisie
        {
            Id = id,
            UtilisateurId = utilisateurId,
            SocieteId = societeId,
            TypeFormulaire = typeFormulaire,
            DonneesJson = donneesJson ?? string.Empty,
            DateCreationUtc = dateCreationUtc,
            DateDerniereModificationUtc = dateCreationUtc
        };
    }

    public void MettreAJour(string donneesJson, DateTime dateModificationUtc)
    {
        DonneesJson = donneesJson ?? string.Empty;
        DateDerniereModificationUtc = dateModificationUtc;
    }
}
