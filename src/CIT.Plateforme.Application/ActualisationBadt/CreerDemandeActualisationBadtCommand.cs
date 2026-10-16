using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.ActualisationBadt;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.ActualisationBadt;

/// <summary>FEATURE 010 (Actualisation BADT) : un conteneur saisi au formulaire, rattaché à un BL.</summary>
public sealed record LigneDemandeBadt(string NumeroBl, string NumeroConteneur);

/// <summary>
/// FEATURE 010 (Actualisation BADT) : requête de création d'une demande d'actualisation déposée par
/// le client via l'entrée « Actualisation BADT » de sa sidebar. Le client saisit le minimum (BL,
/// conteneur(s), récit) ; le reste est enrichi par l'agent (chat + extraction Navis). La file cible
/// et la société sont résolues par le handler : l'écran n'a aucun identifiant technique à fournir.
/// </summary>
public sealed record CreerDemandeActualisationBadtRequete(
    Guid DemandeurId,
    string Description,
    IReadOnlyList<LigneDemandeBadt> Lignes);

/// <summary>
/// FEATURE 010 (Actualisation BADT) : crée le dossier (SOC-11), le ticket de la demande
/// (<see cref="Ticket.CreerDemandeActualisationBadt"/>) et une ligne par conteneur. Calqué sur
/// <c>CreerTicketCommand</c> (même numérotation, même journalisation, même unité de travail).
/// </summary>
public sealed class CreerDemandeActualisationBadtCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    ILigneActualisationBadtRepository lignes,
    IFileDeTraitementRepository files,
    IUtilisateurSocieteRepository utilisateurSocietes,
    IServiceNumerotation numerotation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IMesureActualisationBadtRepository mesures,
    IUniteDeTravail uniteDeTravail)
{
    /// <summary>Module et nom de la file BADT, approvisionnés par <c>ProvisionnementParametrageMetier</c>.</summary>
    public const string NomFile = "Actualisation BADT";

    public async Task<Ticket> ExecuterAsync(CreerDemandeActualisationBadtRequete requete, CancellationToken ct = default)
    {
        if (requete.Lignes is null || requete.Lignes.Count == 0)
        {
            throw new ArgumentException("Au moins un conteneur est requis.", nameof(requete));
        }

        var maintenant = horloge.MaintenantUtc;

        // La demande porte sur les dossiers de la société du demandeur (comme Avoir/Exonération).
        var rattachement = await utilisateurSocietes.ObtenirParUtilisateurAsync(requete.DemandeurId, ct).ConfigureAwait(false);
        var societeId = rattachement?.SocieteId ?? requete.DemandeurId;

        var toutesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileCible = toutesFiles.FirstOrDefault(f => f.Module == Ticket.ModuleActualisationBadt)
            ?? throw new InvalidOperationException($"La file « {NomFile} » n'est pas approvisionnée.");

        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var dossier = Dossier.Creer(numeroDossier, societeId, maintenant, requete.DemandeurId);
        await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);

        var numeroTicket = await numerotation
            .ProchainNumeroTicketAsync(Ticket.ModuleActualisationBadt, Ticket.ModuleActualisationBadt, ct)
            .ConfigureAwait(false);
        var ticket = Ticket.CreerDemandeActualisationBadt(numeroTicket, fileCible.Id, dossier.Id, requete.Description, maintenant);
        await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

        foreach (var ligne in requete.Lignes)
        {
            await lignes.AjouterAsync(
                LigneActualisationBadt.Creer(ticket.Id, ligne.NumeroBl, ligne.NumeroConteneur), ct).ConfigureAwait(false);
        }

        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketCree", $"numero={ticket.Numero}", requete.DemandeurId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(requete.DemandeurId, "TicketCree", nameof(Ticket), ticket.Id, null, ticket.Numero, ct).ConfigureAwait(false);
        // TicketCree notifie le CLIENT (accusé de réception) ; TicketArriveeEnFile notifie les AGENTS
        // de la file BADT (Support Client). Sans ce second événement, le support ne reçoit rien — in-app
        // comme e-mail — car la couche Notifications se déclenche sur l'événement, pas sur la commande.
        await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"numero\":\"{ticket.Numero}\"}}", ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketArriveeEnFile", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        // Reporting (approche B) : volume de demandes par période.
        await mesures.AjouterAsync(MesureActualisationBadt.Creer(
            ticket.Id, null, null, null, TypeMesureBadt.Creation, requete.DemandeurId, maintenant), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return ticket;
    }
}
