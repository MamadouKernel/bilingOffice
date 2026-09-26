using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Incidents;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.SupportTechnique;

/// <summary>Constantes du module Support IT (incident technique).</summary>
public static class SupportTechnique
{
    /// <summary>Module porteur des incidents techniques. Sert aussi de filtre aux files éligibles
    /// comme cible d'une catégorie : un incident ne peut être routé que vers une file de son module.</summary>
    public const string Module = "SupportTechnique";

    /// <summary>Type de demande discriminant sur le ticket générique.</summary>
    public const string TypeDemande = "Incident";

    /// <summary>Nom de la file d'instruction du Support IT.</summary>
    public const string NomFileSupportIt = "Support IT";
}

public sealed record SoumettreIncidentRequete(Guid CategorieIncidentId, GraviteIncident Gravite, string Objet, string Description);

public sealed record SoumettreIncidentResultat(Guid TicketId, string NumeroTicket);

/// <summary>
/// Déclaration d'un incident technique. À la différence d'une réclamation métier, la déclaration
/// n'exige PAS de société rattachée : une panne (connexion, page en erreur, lenteur) doit pouvoir
/// être signalée par tout utilisateur authentifié. L'incident appartient à son déclarant — le
/// dossier a pour client et pour auteur l'appelant lui-même, sans couplage à une société.
/// </summary>
public sealed class SoumettreIncidentCommand(
    ITicketRepository ticketRepository,
    ICategorieIncidentRepository categorieRepository,
    IDossierRepository dossierRepository,
    IUniteDeTravail uniteDeTravail,
    IHorloge horloge,
    IServiceNumerotation numerotation,
    IJournalisationService journalisationService)
{
    public async Task<SoumettreIncidentResultat> ExecuterAsync(Guid appelantId, SoumettreIncidentRequete requete, CancellationToken ct = default)
    {
        if (appelantId == Guid.Empty)
        {
            throw new AccesRefuseException("Un incident ne peut être déclaré que par un utilisateur authentifié.");
        }

        if (string.IsNullOrWhiteSpace(requete.Objet) || string.IsNullOrWhiteSpace(requete.Description))
        {
            throw new ArgumentException("L'objet et la description de l'incident sont obligatoires.");
        }

        var categorie = await categorieRepository.ObtenirParIdAsync(requete.CategorieIncidentId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"La catégorie d'incident {requete.CategorieIncidentId} n'existe pas.");

        if (!categorie.EstActif)
        {
            throw new InvalidOperationException($"La catégorie d'incident {categorie.Code} est inactive et ne peut plus être utilisée.");
        }

        var fileCibleId = categorie.FileCibleDefautId
            ?? throw new InvalidOperationException($"La catégorie {categorie.Code} n'a pas de file cible configurée.");

        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var numeroTicket = await numerotation.ProchainNumeroTicketAsync(SupportTechnique.Module, SupportTechnique.TypeDemande, ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        // L'incident appartient à son déclarant : client = auteur = appelant, aucun rattachement société.
        var dossier = Dossier.Creer(numeroDossier, appelantId, maintenant, appelantId);

        var ticket = Ticket.Creer(numeroTicket, SupportTechnique.Module, SupportTechnique.TypeDemande, fileCibleId, dossier.Id, maintenant);
        ticket.DefinirIncident(categorie.Id, requete.Gravite);
        ticket.DefinirContenuIncident(requete.Objet, requete.Description);

        await dossierRepository.AjouterAsync(dossier, ct).ConfigureAwait(false);
        await ticketRepository.AjouterAsync(ticket, ct).ConfigureAwait(false);

        await journalisationService.ConsignerHistoriqueAsync(ticket.Id, "TicketCree", "Incident technique déclaré", appelantId, ct).ConfigureAwait(false);
        await journalisationService.ConsignerAuditAsync(appelantId, "TicketCree", "Ticket", ticket.Id, null, null, ct).ConfigureAwait(false);
        await journalisationService.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"SupportTechnique\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new SoumettreIncidentResultat(ticket.Id, ticket.Numero);
    }
}
