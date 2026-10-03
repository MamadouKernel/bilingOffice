namespace CIT.Plateforme.Domain.Enrolement;

/// <summary>Document à durée de validité déclaré lors d'un enrôlement de flotte.</summary>
public enum DocumentEnrolement
{
    /// <summary>Permis de conduire — chauffeur.</summary>
    PermisConduire = 1,

    /// <summary>Assurance du véhicule — camion.</summary>
    AssuranceVehicule = 2,

    /// <summary>Visite technique — camion.</summary>
    VisiteTechnique = 3,
}

/// <summary>
/// Échéance d'un document de chauffeur ou de camion (décision du 03/10/2026). Un permis, une
/// assurance ou une visite technique expirée rend l'accès au terminal irrégulier même si le badge
/// est encore valide : la date est donc enregistrée par ticket et un rappel UNIQUE part avant
/// l'échéance. Renouveler le document = nouvelle déclaration (nouveau ticket).
/// </summary>
public sealed class EcheanceDocumentEnrolement
{
    public Guid Id { get; private set; }

    /// <summary>Ticket d'enrôlement du chauffeur ou du camion concerné.</summary>
    public Guid TicketId { get; private set; }

    public DocumentEnrolement Document { get; private set; }

    public DateOnly DateExpiration { get; private set; }

    /// <summary>Horodatage du rappel émis ; null tant qu'aucun rappel n'est parti.</summary>
    public DateTime? RappelEmisUtc { get; private set; }

    private EcheanceDocumentEnrolement() { }

    /// <summary>Documents pertinents pour un type d'objet : le permis pour un chauffeur, l'assurance
    /// et la visite technique pour un camion.</summary>
    public static IReadOnlyList<DocumentEnrolement> DocumentsAttendus(TypeObjetEnrolement type) => type switch
    {
        TypeObjetEnrolement.Chauffeur => [DocumentEnrolement.PermisConduire],
        TypeObjetEnrolement.Camion => [DocumentEnrolement.AssuranceVehicule, DocumentEnrolement.VisiteTechnique],
        _ => [],
    };

    public static EcheanceDocumentEnrolement Declarer(
        Guid ticketId, TypeObjetEnrolement typeObjet, DocumentEnrolement document, DateOnly dateExpiration, DateOnly aujourdhui)
    {
        if (!DocumentsAttendus(typeObjet).Contains(document))
        {
            throw new ArgumentException($"Le document {document} ne concerne pas un objet de type {typeObjet}.", nameof(document));
        }

        if (dateExpiration <= aujourdhui)
        {
            throw new ArgumentException($"Le document {document} est déjà expiré : il ne peut pas servir à un enrôlement.", nameof(dateExpiration));
        }

        return new EcheanceDocumentEnrolement
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Document = document,
            DateExpiration = dateExpiration,
        };
    }

    public void MarquerRappelEmis(DateTime maintenantUtc) => RappelEmisUtc = maintenantUtc;
}
