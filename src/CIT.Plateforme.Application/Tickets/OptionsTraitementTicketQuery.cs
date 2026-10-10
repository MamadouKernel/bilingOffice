using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

public sealed record MotifTransfertOption(string Code, string Libelle);

public sealed record DestinationTransfert(Guid FileId, string FileNom, string Module, IReadOnlyList<MotifTransfertOption> Motifs);

public sealed record AgentAffectable(Guid AgentId, string Nom, int TicketsEnCours);

/// <summary>Ce que l'agent peut faire du ticket depuis sa fiche : transférer, escalader, (ré)affecter.</summary>
public sealed record OptionsTraitementTicketVue(
    bool PeutTransferer,
    IReadOnlyList<DestinationTransfert> Destinations,
    bool PeutEscalader,
    bool PeutAffecter,
    bool EstAffecte,
    IReadOnlyList<AgentAffectable> Agents,
    bool PeutRouvrir = false,
    bool PeutCloturerAbsenceReponse = false,
    /// <summary>Motif de la demande d'annulation du client, si elle attend une décision.</summary>
    string? DemandeAnnulationMotif = null,
    bool PeutDeciderAnnulation = false,
    /// <summary>Passation de chiffre : l'agent qui détient le ticket (ou un superviseur) peut le
    /// rendre à sa file pour que l'équipe entrante le reprenne.</summary>
    bool PeutRendreALaFile = false);

/// <summary>
/// Options de traitement d'un ticket pour un agent (05/10/2026) :
/// <list type="bullet">
/// <item>TRANSFERT — seulement vers les files que la matrice autorise depuis la file courante, avec
/// les seuls motifs actifs admis par cette transition ; réservé à l'agent en charge ou au superviseur.</item>
/// <item>ESCALADE — vers les superviseurs de la file, par l'agent en charge (ou tout agent de la file
/// tant que personne ne l'a pris).</item>
/// <item>(RÉ)AFFECTATION — superviseur : agents habilités à prendre en charge sur la file, avec leur charge.</item>
/// </list>
/// Les commandes revérifient tout : cette vue ne fait qu'éviter de proposer une action vouée au refus.
/// </summary>
public sealed class OptionsTraitementTicketQuery(
    ITicketRepository tickets,
    IFileDeTraitementRepository files,
    ITransitionAutoriseeRepository transitions,
    IMotifTransfertRepository motifs,
    IHabilitationRepository habilitations,
    IUtilisateurInfoRepository utilisateurs,
    IHistoriqueTicketRepository? historique = null)
{
    private static readonly OptionsTraitementTicketVue Aucune = new(false, [], false, false, false, []);

    public async Task<OptionsTraitementTicketVue> ExecuterAsync(Guid agentId, Guid ticketId, CancellationToken ct = default)
    {
        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false);
        if (ticket is null || ticket.Statut == StatutTicket.Annule)
        {
            return Aucune;
        }

        var droits = await habilitations.ListerParUtilisateurAsync(agentId, ct).ConfigureAwait(false);
        bool Detient(string permission) => droits.Any(h => h.PermissionCode == permission && (h.FileId == null || h.FileId == ticket.FileCouranteId));
        if (!droits.Any(h => h.FileId == null || h.FileId == ticket.FileCouranteId))
        {
            return Aucune;
        }

        // Ticket clôturé : seule la réouverture motivée (CYC-06) reste possible.
        if (ticket.Statut == StatutTicket.Cloture)
        {
            return Aucune with { PeutRouvrir = Detient(Permission.TicketRouvrir) };
        }

        var superviseur = Detient(Permission.TicketReaffecter);
        var tenuParMoi = ticket.OperateurAssigneId == agentId;
        var libre = ticket.OperateurAssigneId is null;

        // Transfert : l'agent qui tient le ticket (statut « en cours ») ou un superviseur.
        var destinations = new List<DestinationTransfert>();
        var peutTransferer = Detient(Permission.TicketTransferer) && (superviseur || (tenuParMoi && ticket.Statut == StatutTicket.EnCours));
        if (peutTransferer)
        {
            var toutesFiles = (await files.ListerAsync(ct).ConfigureAwait(false)).ToDictionary(f => f.Id);
            var motifsActifs = await motifs.ListerAsync(activesUniquement: true, ct).ConfigureAwait(false);
            foreach (var transition in (await transitions.ListerAsync(ct).ConfigureAwait(false)).Where(t => t.FileSourceId == ticket.FileCouranteId))
            {
                if (!toutesFiles.TryGetValue(transition.FileCibleId, out var cible))
                {
                    continue;
                }

                var admis = motifsActifs
                    .Where(m => transition.MotifsAutorises.Count == 0 || transition.MotifsAutorises.Contains(m.Code))
                    .OrderBy(m => m.Ordre)
                    .Select(m => new MotifTransfertOption(m.Code, m.Libelle))
                    .ToList();
                // Référentiel vide : la commande accepte un motif libre ; sinon, au moins un motif admis.
                if (motifsActifs.Count == 0 || admis.Count > 0)
                {
                    destinations.Add(new DestinationTransfert(cible.Id, cible.Nom, cible.Module, admis));
                }
            }
        }

        // Escalade : l'agent en charge, ou tout agent de la file tant que personne ne l'a pris.
        var peutEscalader = tenuParMoi || libre || superviseur;

        // (Ré)affectation : superviseur, ou agent habilité à affecter manuellement un ticket libre.
        var peutAffecter = libre ? superviseur || Detient(Permission.TicketAffecterManuellement) : superviseur;
        var agents = new List<AgentAffectable>();
        if (peutAffecter)
        {
            var candidats = (await habilitations.ListerUtilisateursAvecPermissionAsync(Permission.TicketPrendreEnCharge, ticket.FileCouranteId, ct).ConfigureAwait(false))
                .Where(id => id != ticket.OperateurAssigneId)
                .ToList();
            var infos = await utilisateurs.ObtenirInfosAsync(candidats, ct).ConfigureAwait(false);
            var portefeuilles = await tickets.ListerParOperateursAsync(candidats, ct).ConfigureAwait(false);
            agents.AddRange(candidats
                .Select(id => new AgentAffectable(
                    id,
                    infos.TryGetValue(id, out var info) ? info.NomAffiche : "—",
                    portefeuilles.TryGetValue(id, out var liste) ? liste.Count(t => t.Statut == StatutTicket.EnCours) : 0))
                .OrderBy(a => a.TicketsEnCours).ThenBy(a => a.Nom, StringComparer.CurrentCulture));
        }

        // CYC-04 : clôture pour absence de réponse d'un ticket en attente du client, par l'agent en charge.
        var peutCloturerAbsence = ticket.Statut == StatutTicket.EnAttenteClient && (tenuParMoi || superviseur)
            && Detient(Permission.TicketClotureAbsenceReponse);

        // CYC-02 : demande d'annulation du client en attente — décidée par l'agent en charge ou le superviseur.
        string? demandeAnnulation = null;
        if (historique is not null && Detient(Permission.TicketAnnuler) && (tenuParMoi || libre || superviseur))
        {
            var derniere = (await historique.ListerParTicketAsync(ticket.Id, ct).ConfigureAwait(false))
                .Where(h => h.Evenement is "DemandeAnnulationSoumise" or "DemandeAnnulationAcceptee" or "DemandeAnnulationRejetee")
                .MaxBy(h => h.HorodatageUtc);
            demandeAnnulation = derniere?.Evenement == "DemandeAnnulationSoumise" ? derniere.Detail ?? "—" : null;
        }

        // Passation de chiffre : l'agent qui tient le ticket (ou un superviseur) peut le rendre à la file.
        var peutRendreALaFile = !libre && (tenuParMoi || superviseur) && ticket.Statut == StatutTicket.EnCours;

        return new OptionsTraitementTicketVue(peutTransferer, destinations, peutEscalader, peutAffecter && agents.Count > 0, !libre, agents,
            PeutCloturerAbsenceReponse: peutCloturerAbsence,
            DemandeAnnulationMotif: demandeAnnulation,
            PeutDeciderAnnulation: demandeAnnulation is not null,
            PeutRendreALaFile: peutRendreALaFile);
    }
}

/// <summary>
/// Escalade manuelle au superviseur, pour tous les modules (05/10/2026) : l'agent bloqué le signale avec
/// un motif ; les superviseurs de la file sont notifiés et décident (réaffecter, prioriser, transférer).
/// L'escalade ne change ni la file ni l'agent : elle déclenche une décision, elle ne la prend pas.
/// </summary>
public sealed class EscaladerAuSuperviseurCommand(
    ITicketRepository tickets,
    IHabilitationRepository habilitations,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public const string TypeEvenement = "EscaladeSuperviseur";

    public async Task ExecuterAsync(Guid agentId, Guid ticketId, string motif, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de l'escalade est obligatoire : le superviseur doit savoir ce qui bloque.", nameof(motif));
        }

        var ticket = await tickets.ObtenirParIdAsync(ticketId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Ticket {ticketId} introuvable.");
        if (ticket.Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            throw new InvalidOperationException($"Le ticket {ticket.Numero} est terminé : il ne peut plus être escaladé.");
        }

        var droits = await habilitations.ListerParUtilisateurAsync(agentId, ct).ConfigureAwait(false);
        if (!droits.Any(h => h.FileId is null || h.FileId == ticket.FileCouranteId))
        {
            throw new AccesRefuseException($"L'utilisateur {agentId} n'est pas habilité sur la file du ticket {ticket.Numero}.");
        }

        await GardeAgentAffecte.ExigerAsync(autorisation, agentId, ticket, ct).ConfigureAwait(false);

        var detail = motif.Trim();
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, TypeEvenement, detail, agentId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(agentId, TypeEvenement, nameof(Ticket), ticket.Id, null, detail, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync(TypeEvenement, $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
