namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// Regroupement de tickets d'une même demande (SOC-11). Conformément à CLAR-01, le dossier ne
/// porte AUCUN état ni action métier propre : son état est dérivé de ses tickets (voir
/// <see cref="EtatDossierDerive"/>). Aucune méthode « Clôturer » n'existe ici — ce serait inventer
/// une règle absente du CDC.
/// </summary>
public sealed class Dossier
{
    public Guid Id { get; private set; }

    /// <summary>Numéro unique, jamais réutilisé (mêmes garanties que SOC-01/SOC-09 pour le ticket).</summary>
    public string Numero { get; private set; } = string.Empty;

    /// <summary>Client propriétaire — fonde le cloisonnement (SEC-01, CPT-04 cité).</summary>
    public Guid ClientId { get; private set; }

    public DateTime DateCreationUtc { get; private set; }

    private Dossier() { }

    public static Dossier Creer(string numero, Guid clientId, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        Numero = numero,
        ClientId = clientId,
        DateCreationUtc = maintenantUtc,
    };
}
