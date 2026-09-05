using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Securite;

/// <summary>
/// Décision unique d'accès à une ressource rattachée à un dossier (ticket, dossier, et tout ce
/// qui en dépend). CDC : CPT-04, SEC-01, SEC-03, ENT-03.
///
/// POURQUOI CE SERVICE EXISTE — REMÉDIATION P0 (`docs/audit-conformite-merise.md` § 26).
/// Les endpoints de <c>/api/v1</c> réimplémentaient chacun leur contrôle de cloisonnement au lieu
/// d'appeler un mécanisme central, et ce contrôle <b>échouait en mode ouvert</b> : il ne
/// s'appliquait que si l'appelant possédait une société de rattachement. Un compte sans société —
/// exactement ce que produit <c>InscrireTransporteurCommand</c> — passait donc au travers et
/// pouvait lire n'importe quel ticket par énumération de son numéro séquentiel.
///
/// TROIS PRINCIPES GOUVERNENT CE SERVICE.
///
/// <b>1. Refus par défaut.</b> Chaque chemin part de « refusé » et ne concède l'accès que sur
/// preuve positive. Une information d'autorisation absente — pas de société, pas d'habilitation,
/// pas de rattachement actif — ne vaut jamais autorisation : c'est précisément la confusion qui a
/// produit la faille.
///
/// <b>2. Données faisant autorité.</b> La décision ne se fonde sur aucun objet fourni par
/// l'appelant, seulement sur l'identifiant de l'utilisateur et sur ce que la base en dit. Un
/// contexte de session, aussi bien construit soit-il, reste une projection : il n'a pas à porter
/// une décision de sécurité.
///
/// <b>3. Deux formes de propriété, et non une.</b> <see cref="Dossier.ClientId"/> désigne la
/// <b>société</b> propriétaire pour les parcours Billing, Réclamation et Avoir, mais
/// l'<b>utilisateur</b> lui-même pour l'Enrôlement — <c>SoumettreEnrolementCommand</c> l'établit
/// ainsi (« Dossier propriétaire = appelant lui-même »). Ne reconnaître que la première forme
/// laissait la seconde sans contrôle. Les deux sont traitées ici, explicitement.
/// </summary>
public sealed class ControleAccesRessourceService(
    IDossierRepository dossiers,
    ITicketRepository tickets,
    IUtilisateurSocieteRepository rattachements,
    IHabilitationRepository habilitations,
    IPerimetreSupervisionRepository perimetres,
    GardeIdentiteService garde)
{
    /// <summary>
    /// Accès en consultation à un ticket. <paramref name="ticket"/> porte la file courante, qui
    /// borne le périmètre d'un agent.
    /// </summary>
    public async Task<bool> PeutConsulterLeTicketAsync(Guid utilisateurId, Ticket ticket, CancellationToken ct = default)
    {
        if (ticket is null)
        {
            return false;
        }

        var dossier = await dossiers.ObtenirParIdAsync(ticket.DossierId, ct).ConfigureAwait(false);
        return await PeutConsulterAsync(utilisateurId, dossier, ticket.FileCouranteId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Accès en consultation à un dossier.
    ///
    /// RÈGLE RETENUE POUR L'AGENT, explicitée en remédiation P0-bis : un dossier n'appartient à
    /// aucune file — ce sont ses tickets qui en portent une. Un agent y accède donc si et seulement
    /// s'il accède à <b>au moins un</b> de ses tickets. Admettre tout agent porteur d'une
    /// habilitation quelconque, comme le faisait la première version, ouvrait les dossiers de files
    /// qu'il n'instruit pas — contraire à SEC-03.
    ///
    /// Un dossier sans aucun ticket n'est ouvert à aucun agent : il n'y a rien à y instruire.
    /// </summary>
    public async Task<bool> PeutConsulterLeDossierAsync(Guid utilisateurId, Dossier dossier, CancellationToken ct = default)
    {
        if (utilisateurId == Guid.Empty || dossier is null)
        {
            return false;
        }

        var info = await garde.ObtenirCompteUtilisableAsync(utilisateurId, ct).ConfigureAwait(false);
        if (info is null)
        {
            return false;
        }

        if (info.Nature != NatureUtilisateur.Agent)
        {
            return await ClientEstAdmisAsync(utilisateurId, dossier, ct).ConfigureAwait(false);
        }

        var ticketsDuDossier = await tickets.ListerParDossierAsync(dossier.Id, ct).ConfigureAwait(false);
        foreach (var ticket in ticketsDuDossier)
        {
            if (await AgentEstAdmisAsync(utilisateurId, ticket.FileCouranteId, ct).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    public async Task ExigerConsultationTicketAsync(Guid utilisateurId, Ticket ticket, CancellationToken ct = default)
    {
        if (!await PeutConsulterLeTicketAsync(utilisateurId, ticket, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException(
                $"L'utilisateur {utilisateurId} n'est pas autorisé à consulter le ticket {ticket?.Id} (CPT-04, SEC-03).");
        }
    }

    public async Task ExigerConsultationDossierAsync(Guid utilisateurId, Dossier dossier, CancellationToken ct = default)
    {
        if (!await PeutConsulterLeDossierAsync(utilisateurId, dossier, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException(
                $"L'utilisateur {utilisateurId} n'est pas autorisé à consulter le dossier {dossier?.Id} (CPT-04, SEC-03).");
        }
    }

    /// <summary>
    /// Portée de consultation d'un appelant au sein de sa société : la société dont il relève, et
    /// la restriction d'auteur qu'ENT-03 lui impose (<c>null</c> = il voit toute la société).
    /// </summary>
    public sealed record PorteeSociete(Guid SocieteId, Guid? AuteurIdRestriction);

    /// <summary>
    /// Résout CÔTÉ SERVEUR la société dont relève l'appelant, et la restriction ENT-03 associée.
    /// Renvoie <c>null</c> lorsque rien n'est ouvert : compte invalide ou désactivé, ou absence de
    /// rattachement actif.
    ///
    /// POURQUOI CETTE MÉTHODE EXISTE — REMÉDIATION P2-02. Les surfaces de tarification recevaient
    /// un <c>SocieteId</c> DE L'APPELANT et s'en servaient comme preuve de propriété. Une décision
    /// fondée sur une donnée fournie par celui qu'elle doit contrôler n'est pas une décision : il
    /// suffisait de transmettre la société convoitée. La société se résout donc ici, à partir de la
    /// seule identité, et n'est jamais reçue.
    /// </summary>
    public async Task<PorteeSociete?> ResoudrePorteeSocieteAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var info = await garde.ObtenirCompteUtilisableAsync(utilisateurId, ct).ConfigureAwait(false);
        if (info is null)
        {
            return null;
        }

        var rattachement = await rattachements.ObtenirParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (rattachement is null || !rattachement.EstActif)
        {
            return null;
        }

        // CLAR-02 — l'administrateur de société voit toute sa société, quel que soit son propre
        // droit de consultation individuel.
        var visibiliteTotale = rattachement.EstAdministrateur
            || rattachement.DroitConsultation == DroitConsultation.ToutesDemandesSociete;

        return new PorteeSociete(rattachement.SocieteId, visibiliteTotale ? null : utilisateurId);
    }

    /// <summary>
    /// Accès à une ressource porteuse d'une société propriétaire et d'un auteur — une simulation
    /// de proforma, par exemple. Même décision que pour un dossier de société, appliquée à une
    /// ressource qui n'en est pas un.
    /// </summary>
    public async Task<bool> PeutConsulterRessourceDeSocieteAsync(
        Guid utilisateurId, Guid societeProprietaire, Guid? auteurRessource, CancellationToken ct = default)
    {
        var portee = await ResoudrePorteeSocieteAsync(utilisateurId, ct).ConfigureAwait(false);
        if (portee is null || portee.SocieteId != societeProprietaire)
        {
            return false;
        }

        // ENT-03 (RG-CPT-07) : un collaborateur restreint ne lit que ce dont il est l'auteur.
        return portee.AuteurIdRestriction is null || auteurRessource == portee.AuteurIdRestriction;
    }

    public async Task ExigerConsultationRessourceDeSocieteAsync(
        Guid utilisateurId, Guid societeProprietaire, Guid? auteurRessource, CancellationToken ct = default)
    {
        if (!await PeutConsulterRessourceDeSocieteAsync(utilisateurId, societeProprietaire, auteurRessource, ct).ConfigureAwait(false))
        {
            throw new AccesRefuseException(
                $"L'utilisateur {utilisateurId} n'est pas autorisé à consulter cette ressource (CPT-04, ENT-03).");
        }
    }

    // ---------------------------------------------------------------------
    // Décision
    // ---------------------------------------------------------------------

    private async Task<bool> PeutConsulterAsync(
        Guid utilisateurId, Dossier? dossier, Guid? fileCourante, CancellationToken ct)
    {
        // Refus par défaut : sans identité ou sans ressource, rien n'est accordé.
        if (utilisateurId == Guid.Empty || dossier is null)
        {
            return false;
        }

        // SEC-11 — un compte désactivé n'accède plus à rien, quelles que soient les habilitations
        // qu'on lui a laissées. La règle n'est PAS réécrite ici : elle appartient à
        // GardeIdentiteService, qui la porte aussi pour AutorisationService (remédiation P0-ter).
        // Deux copies de « compte actif » finiraient par diverger, comme tout le reste.
        var info = await garde.ObtenirCompteUtilisableAsync(utilisateurId, ct).ConfigureAwait(false);
        if (info is null)
        {
            return false;
        }

        return info.Nature == NatureUtilisateur.Agent
            ? await AgentEstAdmisAsync(utilisateurId, fileCourante, ct).ConfigureAwait(false)
            : await ClientEstAdmisAsync(utilisateurId, dossier, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// SEC-03 — « un agent n'accède qu'aux files et actions de son rôle ». Un agent est admis s'il
    /// porte au moins une habilitation couvrant la file du ticket (habilitation non bornée, ou
    /// bornée à cette file précise), ou si son périmètre de supervision la couvre.
    ///
    /// Un agent sans aucune habilitation n'accède à rien : la nature « Agent » n'est pas un droit.
    /// </summary>
    private async Task<bool> AgentEstAdmisAsync(Guid agentId, Guid? fileCourante, CancellationToken ct)
    {
        var siennes = await habilitations.ListerParUtilisateurAsync(agentId, ct).ConfigureAwait(false);

        if (fileCourante is not Guid file)
        {
            // Ressource sans file (dossier) : une habilitation quelconque suffit à établir la
            // qualité d'agent instruisant, mais son absence totale reste un refus.
            return siennes.Count > 0;
        }

        if (siennes.Any(h => h.FileId is null || h.FileId == file))
        {
            return true;
        }

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(agentId, ct).ConfigureAwait(false);
        return perimetre is not null && perimetre.CouvreLaFile(file);
    }

    /// <summary>
    /// CPT-04 — un client n'accède qu'à ses propres dossiers. Deux formes de propriété, aucune
    /// autre :
    /// <list type="number">
    ///   <item><b>Dossier de société</b> — l'appelant a un rattachement <b>actif</b> à la société
    ///   propriétaire. La portée intra-société est celle d'ENT-03, appliquée par la règle déjà
    ///   établie <see cref="VisibiliteDemandesFiltre"/> : un collaborateur restreint à ses propres
    ///   demandes ne lit pas celles d'un collègue.</item>
    ///   <item><b>Dossier personnel</b> — le dossier est nominativement au nom de l'appelant, ou
    ///   il en est l'auteur déclaré. C'est le cas de l'Enrôlement, où le transporteur n'a pas de
    ///   société.</item>
    /// </list>
    /// Tout le reste est refusé.
    /// </summary>
    private async Task<bool> ClientEstAdmisAsync(Guid utilisateurId, Dossier dossier, CancellationToken ct)
    {
        // Forme 2 en premier : elle ne dépend d'aucune lecture supplémentaire.
        //
        // SEULE la propriété NOMINATIVE compte ici. Une première version acceptait aussi
        // `dossier.AuteurId == utilisateurId`, ce qui laissait un collaborateur dont le
        // rattachement avait été désactivé continuer de lire les dossiers qu'il avait déposés —
        // en contournant le contrôle de rattachement placé juste en dessous. L'auteur d'un dossier
        // de société est traité par la forme 1, qui vérifie que son rattachement est toujours actif.
        if (dossier.ClientId == utilisateurId)
        {
            return true;
        }

        var rattachement = await rattachements.ObtenirParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        if (rattachement is null || !rattachement.EstActif)
        {
            // Aucune société, ou rattachement désactivé : aucun dossier de société n'est lisible.
            // C'est ici que la faille se refermait — l'ancien contrôle concluait « autorisé ».
            return false;
        }

        // CLAR-02 — l'administrateur de société voit toute sa société, quel que soit son propre
        // droit de consultation individuel.
        var visibiliteTotale = rattachement.EstAdministrateur
            || rattachement.DroitConsultation == DroitConsultation.ToutesDemandesSociete;

        var filtre = new VisibiliteDemandesFiltre(
            rattachement.SocieteId,
            visibiliteTotale ? null : utilisateurId);

        return filtre.DemandeEstVisible(dossier);
    }
}
