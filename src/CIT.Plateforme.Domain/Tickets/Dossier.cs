using CIT.Plateforme.Domain.Comptes;

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

    /// <summary>
    /// EXTENSION FEATURE 002 (CLAR-01, research.md D5/D11) : utilisateur individuel à l'origine de
    /// la demande, distinct de <see cref="ClientId"/> qui identifie la société propriétaire dès
    /// lors qu'un compte société existe (Feature 002). Nullable et non renseigné pour tout dossier
    /// créé avant cette extension — jamais rétroactif. Ne remplace jamais <see cref="ClientId"/>.
    /// </summary>
    public Guid? AuteurId { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 002 (CPT-06, research.md D3) : instantané du type de client applicable au
    /// moment de la création du dossier. Garantit la non-rétroactivité : un changement ultérieur du
    /// profil de la société cliente ne modifie jamais le type sous lequel ce dossier a été créé.
    /// </summary>
    public TypeClient? TypeClient { get; private set; }

    public DateTime DateCreationUtc { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 005 (CLAR-SIM-05, research.md D12) : référence facultative vers une simulation
    /// de proforma enregistrée (SIM-08) rattachée lors de la soumission de la demande.
    /// </summary>
    public string? ReferenceSimulationProforma { get; private set; }

    private Dossier() { }

    public static Dossier Creer(
        string numero,
        Guid clientId,
        DateTime maintenantUtc,
        Guid? auteurId = null,
        TypeClient? typeClient = null,
        string? referenceSimulationProforma = null) => new()
    {
        Id = Guid.NewGuid(),
        Numero = numero,
        ClientId = clientId,
        AuteurId = auteurId,
        TypeClient = typeClient,
        ReferenceSimulationProforma = string.IsNullOrWhiteSpace(referenceSimulationProforma) ? null : referenceSimulationProforma.Trim(),
        DateCreationUtc = maintenantUtc,
    };

    public void AssocierSimulationProforma(string referenceSimulation)
    {
        if (string.IsNullOrWhiteSpace(referenceSimulation)) return;
        ReferenceSimulationProforma = referenceSimulation.Trim();
    }
}
