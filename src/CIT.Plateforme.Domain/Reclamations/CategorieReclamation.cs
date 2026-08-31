namespace CIT.Plateforme.Domain.Reclamations;

public sealed class CategorieReclamation
{
    public Guid Id { get; private set; }
    
    public string Code { get; private set; } = string.Empty;
    
    public string Libelle { get; private set; } = string.Empty;
    
    public Guid? FileCibleDefautId { get; private set; }
    
    public bool EstActif { get; private set; }

    private CategorieReclamation() { }

    public static CategorieReclamation Creer(string code, string libelle, Guid? fileCibleDefautId)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code est obligatoire.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé est obligatoire.", nameof(libelle));
        }

        return new CategorieReclamation
        {
            Id = Guid.NewGuid(),
            Code = code,
            Libelle = libelle,
            FileCibleDefautId = fileCibleDefautId,
            EstActif = true
        };
    }

    public void Desactiver()
    {
        EstActif = false;
    }

    public void Activer()
    {
        EstActif = true;
    }

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
