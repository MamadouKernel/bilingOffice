using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>
/// LOT 2 — Enrôlement société (ajout). Un objet de la flotte à enrôler : un chauffeur (permis +
/// photo) ou un camion (carte grise). Les pièces sont déposées dans le dossier ; le chauffeur ne se
/// déplace jamais.
/// </summary>
public sealed record ObjetEnrolementSoumission(
    TypeObjetEnrolement Type,
    string Objet,
    string Description,
    IReadOnlyList<EcheanceDocumentSaisie>? Echeances = null);

/// <summary>Date d'expiration déclarée pour un document du chauffeur ou du camion (03/10/2026).</summary>
public sealed record EcheanceDocumentSaisie(DocumentEnrolement Document, DateOnly DateExpiration);

/// <summary>
/// LOT 2 — Demande d'enrôlement d'une flotte par une société cliente : un dossier unique regroupant
/// un ticket par objet (chauffeur / camion), à l'image d'une soumission IMPORT multi-BL.
/// </summary>
public sealed record SoumettreEnrolementSocieteRequete(
    Guid SocieteId,
    IReadOnlyList<ObjetEnrolementSoumission> Objets);

/// <summary>Ticket produit pour un objet de la flotte.</summary>
public sealed record TicketEnrolementCree(Guid TicketId, string NumeroTicket, TypeObjetEnrolement Type);

/// <summary>Résultat de la soumission : le dossier commun et la liste des tickets créés.</summary>
public sealed record SoumettreEnrolementSocieteResultat(
    Guid DossierId,
    string NumeroDossier,
    IReadOnlyList<TicketEnrolementCree> Tickets);

/// <summary>
/// LOT 2 — Enrôlement société (ajout). Une société cliente monte, via l'application, un dossier
/// unique pour sa flotte et obtient un ticket par objet (chauffeur / camion), tous routés vers
/// l'Agent Gate. Le parcours dématérialisé fait que le chauffeur ne se déplace jamais : les pièces
/// (permis + photo / carte grise) sont déposées dans le dossier ; à la clôture des tickets, un
/// e-mail invite le représentant de la société à venir retirer les cartes / BAT.
/// <para>
/// Autorisation ENT-02 (patron <see cref="CustomerComplaint.SoumettreReclamationCommand"/>) : le
/// droit de soumettre pour une société est porté par le rattachement lui-même
/// (<c>rattachement.DroitSoumission</c>), pas par une permission RBAC globale — celle-ci n'est
/// jamais accordée par le parcours réel de validation de compte société.
/// </para>
/// </summary>
public sealed class SoumettreEnrolementSocieteCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IFileDeTraitementRepository files,
    IUtilisateurSocieteRepository utilisateursSociete,
    IServiceNumerotation numerotation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail,
    IEcheanceDocumentEnrolementRepository? echeances = null)
{
    public const string Module = "Enrolement";
    public const string TypeDemande = "Enrolement";
    public const string FileAgentGate = "Agent Gate";

    public async Task<SoumettreEnrolementSocieteResultat> ExecuterAsync(
        Guid appelantId,
        SoumettreEnrolementSocieteRequete requete,
        CancellationToken ct = default)
    {
        // 1. Autorisation ENT-02 : le rattachement société certifié par le serveur porte le droit.
        var rattachement = await utilisateursSociete.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        if (rattachement == null || !rattachement.EstActif || !rattachement.DroitSoumission)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} n'est pas habilité à soumettre un enrôlement société (ENT-02).");
        }

        // Le rattachement serveur DOIT correspondre à la société demandée : pas d'usurpation.
        if (rattachement.SocieteId != requete.SocieteId)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} ne peut pas soumettre pour la société {requete.SocieteId}.");
        }

        // 2. Validation du contenu — pré-vol complet AVANT toute écriture (aucune persistance
        // partielle si un objet est invalide).
        if (requete.Objets is null || requete.Objets.Count == 0)
        {
            throw new ArgumentException("Au moins un objet (chauffeur ou camion) doit être soumis.", nameof(requete));
        }

        foreach (var objet in requete.Objets)
        {
            if (string.IsNullOrWhiteSpace(objet.Objet))
            {
                throw new ArgumentException("L'objet de chaque enrôlement est obligatoire (ENR-01).", nameof(requete));
            }

            if (string.IsNullOrWhiteSpace(objet.Description))
            {
                throw new ArgumentException("La description de chaque enrôlement est obligatoire (ENR-01).", nameof(requete));
            }

            // Échéances : contrôlées AVANT toute écriture (document pertinent, pas déjà expiré).
            foreach (var e in objet.Echeances ?? [])
            {
                _ = EcheanceDocumentEnrolement.Declarer(Guid.Empty, objet.Type, e.Document, e.DateExpiration, DateOnly.FromDateTime(horloge.MaintenantUtc));
            }
        }

        // 3. Résolution de la file « Agent Gate » — donnée paramétrée, jamais codée en dur.
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileAgentGate = toutesLesFiles.SingleOrDefault(f => f.Module == Module && f.Nom == FileAgentGate)
            ?? throw new InvalidOperationException($"File « {FileAgentGate} » (module {Module}) non paramétrée.");

        var maintenant = horloge.MaintenantUtc;

        // 4. Dossier unique rattaché à la société (ClientId = société, AuteurId = représentant).
        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var dossier = Dossier.Creer(numeroDossier, rattachement.SocieteId, maintenant, appelantId);
        await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);

        // 5. Un ticket par objet, typé (objet + sens Ajout) et informé, tous dans le même dossier.
        var ticketsCrees = new List<TicketEnrolementCree>(requete.Objets.Count);
        foreach (var objet in requete.Objets)
        {
            var numeroTicket = await numerotation.ProchainNumeroTicketAsync(Module, TypeDemande, ct).ConfigureAwait(false);
            var ticket = Ticket.Creer(numeroTicket, Module, TypeDemande, fileAgentGate.Id, dossier.Id, maintenant);
            ticket.DefinirTypeEnrolement(objet.Type, SensEnrolement.Ajout);
            ticket.DefinirInformationsEnrolement(objet.Objet, objet.Description);
            await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

            if (echeances is not null)
            {
                foreach (var e in objet.Echeances ?? [])
                {
                    await echeances.AjouterAsync(
                        EcheanceDocumentEnrolement.Declarer(ticket.Id, objet.Type, e.Document, e.DateExpiration, DateOnly.FromDateTime(maintenant)), ct).ConfigureAwait(false);
                }
            }

            await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketCree", "Enrôlement société soumis", appelantId, ct).ConfigureAwait(false);
            await journalisation.ConsignerAuditAsync(appelantId, "TicketCree", nameof(Ticket), ticket.Id, null, ticket.Numero, ct).ConfigureAwait(false);
            await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"{Module}\"}}", ct).ConfigureAwait(false);

            ticketsCrees.Add(new TicketEnrolementCree(ticket.Id, ticket.Numero, objet.Type));
        }

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new SoumettreEnrolementSocieteResultat(dossier.Id, dossier.Numero, ticketsCrees);
    }
}
