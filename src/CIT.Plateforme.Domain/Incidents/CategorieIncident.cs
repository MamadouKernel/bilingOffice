namespace CIT.Plateforme.Domain.Incidents;

/// <summary>
/// Catégorie d'incident technique — référentiel administrable, sur le même modèle que
/// <c>CategorieReclamation</c> : chaque catégorie route vers une file cible par défaut et se
/// désactive par soft-delete (jamais de suppression, l'historique des incidents doit rester
/// nominativement rattaché à sa catégorie).
/// </summary>
public sealed class CategorieIncident
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Libelle { get; private set; } = string.Empty;

    public Guid? FileCibleDefautId { get; private set; }

    public bool EstActif { get; private set; }

    private CategorieIncident() { }

    public static CategorieIncident Creer(string code, string libelle, Guid? fileCibleDefautId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code est obligatoire.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé est obligatoire.", nameof(libelle));
        }

        return new CategorieIncident
        {
            Id = Guid.NewGuid(),
            Code = code,
            Libelle = libelle,
            FileCibleDefautId = fileCibleDefautId,
            EstActif = true,
        };
    }

    public void Desactiver() => EstActif = false;

    public void Activer() => EstActif = true;

    public void MettreAJour(string libelle, Guid? fileCibleDefautId)
    {
        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé est obligatoire.", nameof(libelle));
        }

        Libelle = libelle;
        FileCibleDefautId = fileCibleDefautId;
    }
}
