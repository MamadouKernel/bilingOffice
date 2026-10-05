using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Guichet;

public enum SensOperationGuichet { Import, Export }

public sealed record OuvrirDossierAuGuichetRequest(
    Guid AgentId,
    string EmailClient,
    SensOperationGuichet Sens,
    string ReferenceBlOuBooking,
    string MotifPassage
);

/// <summary>Creates the ordinary client ticket at the counter so the same ticket can continue online.</summary>
public sealed class OuvrirDossierAuGuichetCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    ISocieteClienteRepository societes,
    IUtilisateurSocieteRepository utilisateursSociete,
    IUtilisateurInfoRepository utilisateurs,
    IFileDeTraitementRepository files,
    IServiceNumerotation numerotation,
    IAutorisationService autorisation,
    IJournalisationService journal,
    IUniteDeTravail uniteDeTravail,
    IHorloge horloge)
{
    public async Task<(Guid TicketId, string NumeroTicket)> ExecuterAsync(
        OuvrirDossierAuGuichetRequest request, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(request.AgentId, Permission.GuichetRecherche, null, ct).ConfigureAwait(false);

        var reference = request.ReferenceBlOuBooking?.Trim() ?? string.Empty;
        if (reference.Length is 0 or > 100)
            throw new ArgumentException("Le numéro BL ou booking est obligatoire (100 caractères maximum).");
        if (string.IsNullOrWhiteSpace(request.MotifPassage) || request.MotifPassage.Trim().Length > 500)
            throw new ArgumentException("Le motif du passage est obligatoire (500 caractères maximum).");

        var client = await utilisateurs.ObtenirParEmailAsync(request.EmailClient, ct).ConfigureAwait(false);
        if (client is null || !client.EstActif || client.Nature != NatureUtilisateur.Client)
            throw new InvalidOperationException("Aucun compte client actif ne correspond à cette adresse e-mail.");

        var societe = await societes.ObtenirParDemandeurAsync(client.Id, ct).ConfigureAwait(false);
        var rattachement = await utilisateursSociete.ObtenirParUtilisateurAsync(client.Id, ct).ConfigureAwait(false);
        if (rattachement is { EstActif: true })
            societe = await societes.ObtenirParIdAsync(rattachement.SocieteId, ct).ConfigureAwait(false);
        if (societe is null || societe.Etat != EtatSociete.Active)
            throw new InvalidOperationException("Le compte client n'est pas rattaché à une société validée par CIT.");
        if (rattachement is not { EstActif: true })
            throw new InvalidOperationException("Le client doit avoir un rattachement actif à sa société pour poursuivre le dossier en ligne.");

        var estImport = request.Sens == SensOperationGuichet.Import;
        var module = estImport ? BillingImport.SoumettreDemandeImportCommand.Module : BillingExport.SoumettreDemandeExportCommand.Module;
        var typeDemande = estImport ? BillingImport.SoumettreDemandeImportCommand.TypeDemande : BillingExport.SoumettreDemandeExportCommand.TypeDemande;
        var fileNom = estImport ? BillingImport.SoumettreDemandeImportCommand.FilePiloteManifeste : BillingExport.SoumettreDemandeExportCommand.FileBackOfficeExport;

        var ticketExistant = estImport
            ? await tickets.ObtenirTicketActifParNumeroBlAsync(reference, ct).ConfigureAwait(false)
            : await tickets.ObtenirTicketActifParNumeroBookingAsync(reference, ct).ConfigureAwait(false);
        if (ticketExistant is not null)
            throw new InvalidOperationException("Un dossier actif existe déjà pour cette référence. Recherchez le BL ou booking dans la page Guichet pour le reprendre.");

        var file = (await files.ListerAsync(ct).ConfigureAwait(false))
            .SingleOrDefault(f => f.Module == module && f.Nom == fileNom)
            ?? throw new InvalidOperationException($"La file d'entrée « {fileNom} » du parcours {module} n'est pas configurée.");

        var maintenant = horloge.MaintenantUtc;
        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var dossier = Dossier.Creer(numeroDossier, societe.Id, maintenant, client.Id, societe.Profil);
        await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);

        var numeroTicket = await numerotation.ProchainNumeroTicketAsync(module, typeDemande, ct).ConfigureAwait(false);
        var ticket = Ticket.Creer(numeroTicket, module, typeDemande, file.Id, dossier.Id, maintenant,
            estImport ? reference : null, estImport ? null : reference);
        await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);
        await journal.ConsignerHistoriqueAsync(ticket.Id, "DossierInitialiseAuGuichet", request.MotifPassage.Trim(), request.AgentId, ct).ConfigureAwait(false);
        await journal.ConsignerAuditAsync(request.AgentId, "Guichet.DossierInitialise", nameof(Ticket), ticket.Id,
            null, $"{ticket.Numero} · {module} · {reference} · client {client.Id}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return (ticket.Id, ticket.Numero);
    }
}
