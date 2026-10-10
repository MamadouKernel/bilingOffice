namespace CIT.Plateforme.Domain.SuiviTemps;

/// <summary>
/// SLA-04 — jour chômé exclu du calcul des délais SLA (contexte ivoirien : fêtes légales fixes et
/// mobiles). Référentiel administrable sans développement, au même titre que les horaires ouvrés.
///
/// La DATE est l'identité stable du jour férié (un seul enregistrement par date) : désactiver plutôt
/// que supprimer, comme les autres référentiels, pour qu'un historique de SLA calculé reste lisible.
/// Aucune date n'est livrée par défaut — en inventer reviendrait à décider à la place du métier.
/// </summary>
public sealed class JourFerie
{
    public Guid Id { get; private set; }

    /// <summary>Date du jour chômé. Immuable : c'est l'identité du férié. Pour corriger une date,
    /// on désactive l'ancienne et on en crée une nouvelle.</summary>
    public DateOnly Date { get; private set; }

    public string Libelle { get; private set; } = string.Empty;

    public bool EstActif { get; private set; }

    private JourFerie() { }

    public static JourFerie Definir(DateOnly date, string libelle)
    {
        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé du jour férié est obligatoire.", nameof(libelle));
        }

        return new JourFerie
        {
            Id = Guid.NewGuid(),
            Date = date,
            Libelle = libelle.Trim(),
            EstActif = true,
        };
    }

    public void Modifier(string libelle)
    {
        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé du jour férié est obligatoire.", nameof(libelle));
        }

        Libelle = libelle.Trim();
    }

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;
}
