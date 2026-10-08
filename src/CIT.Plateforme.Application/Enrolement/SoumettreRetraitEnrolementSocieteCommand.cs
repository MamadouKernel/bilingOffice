using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// LOT 2 — Demande de retrait d'accès pour une flotte : un dossier unique regroupant un ticket de
/// retrait par objet (carte d'un chauffeur, BAT d'un camion). Réutilise <see cref="ObjetEnrolementSoumission"/> :
/// pour un retrait, le champ description porte le motif (départ, revente…), pas des pièces.
/// </summary>
public sealed record SoumettreRetraitEnrolementSocieteRequete(
    Guid SocieteId,
    IReadOnlyList<ObjetEnrolementSoumission> Objets);

/// <summary>
/// LOT 2 — Enrôlement société (retrait). Symétrique de <see cref="SoumettreEnrolementSocieteCommand"/> :
/// une société révoque des accès de sa flotte (chauffeur parti, camion vendu). Même modèle de
/// regroupement (un dossier, un ticket par objet, routage Agent Gate) et même autorisation ENT-02,
/// mais chaque ticket est typé <see cref="SensEnrolement.Retrait"/>. Le retrait référence un objet
/// déjà enrôlé : aucune pièce à déposer. La révocation physique (carte / BAT) est constatée à la
/// clôture du ticket par l'agent, comme l'émission l'est pour un ajout — aucune entité carte/BAT.
/// </summary>
public sealed class SoumettreRetraitEnrolementSocieteCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IFileDeTraitementRepository files,
    IUtilisateurSocieteRepository utilisateursSociete,
    IServiceNumerotation numerotation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail,
    ISocieteClienteRepository? societes = null)
{
    public const string Module = "Enrolement";
    public const string TypeDemande = "Enrolement";
    public const string FileAgentGate = "Agent Gate";

    public async Task<SoumettreEnrolementSocieteResultat> ExecuterAsync(
        Guid appelantId,
        SoumettreRetraitEnrolementSocieteRequete requete,
        CancellationToken ct = default)
    {
        // 1. Autorisation ENT-02 : le rattachement société certifié par le serveur porte le droit.
        var rattachement = await utilisateursSociete.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        if (rattachement == null || !rattachement.EstActif || !rattachement.DroitSoumission)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} n'est pas habilité à soumettre un retrait d'enrôlement société (ENT-02).");
        }

        if (rattachement.SocieteId != requete.SocieteId)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} ne peut pas soumettre pour la société {requete.SocieteId}.");
        }

        // Décision du 04/10/2026 : seule une entreprise de transport enrôle une flotte.
        if (societes is not null
            && (await societes.ObtenirParIdAsync(requete.SocieteId, ct).ConfigureAwait(false))?.EstTransporteur != true)
        {
            throw new AccesRefuseException("L'enrôlement de flotte est réservé aux entreprises de transport.");
        }

        // 2. Validation du contenu — pré-vol complet AVANT toute écriture.
        if (requete.Objets is null || requete.Objets.Count == 0)
        {
            throw new ArgumentException("Au moins un objet (chauffeur ou camion) doit être soumis.", nameof(requete));
        }

        foreach (var objet in requete.Objets)
        {
            if (string.IsNullOrWhiteSpace(objet.Objet))
            {
                throw new ArgumentException("L'objet de chaque retrait est obligatoire (ENR-01).", nameof(requete));
            }

            if (string.IsNullOrWhiteSpace(objet.Description))
            {
                throw new ArgumentException("Le motif de chaque retrait est obligatoire (ENR-01).", nameof(requete));
            }
        }

        // 3. Résolution de la file « Agent Gate » — donnée paramétrée.
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileAgentGate = toutesLesFiles.SingleOrDefault(f => f.Module == Module && f.Nom == FileAgentGate)
            ?? throw new InvalidOperationException($"File « {FileAgentGate} » (module {Module}) non paramétrée.");

        var maintenant = horloge.MaintenantUtc;

        // 4. Dossier unique rattaché à la société.
        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var dossier = Dossier.Creer(numeroDossier, rattachement.SocieteId, maintenant, appelantId);
        await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);

        // 5. Un ticket de retrait par objet, typé Retrait.
        var ticketsCrees = new List<TicketEnrolementCree>(requete.Objets.Count);
        foreach (var objet in requete.Objets)
        {
            var numeroTicket = await numerotation.ProchainNumeroTicketAsync(Module, TypeDemande, ct).ConfigureAwait(false);
            var ticket = Ticket.Creer(numeroTicket, Module, TypeDemande, fileAgentGate.Id, dossier.Id, maintenant);
            ticket.DefinirTypeEnrolement(objet.Type, SensEnrolement.Retrait);
            ticket.DefinirInformationsEnrolement(objet.Objet, objet.Description);
            await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

            await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketCree", "Retrait d'enrôlement société soumis", appelantId, ct).ConfigureAwait(false);
            await journalisation.ConsignerAuditAsync(appelantId, "TicketCree", nameof(Ticket), ticket.Id, null, ticket.Numero, ct).ConfigureAwait(false);
            await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"{Module}\"}}", ct).ConfigureAwait(false);
            // Alerte les AGENTS de la file Agent Gate (in-app + e-mail), comme pour l'ajout (07/10/2026).
            await journalisation.PublierEvenementAsync("TicketArriveeEnFile", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

            ticketsCrees.Add(new TicketEnrolementCree(ticket.Id, ticket.Numero, objet.Type));
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new SoumettreEnrolementSocieteResultat(dossier.Id, dossier.Numero, ticketsCrees);
    }
}
