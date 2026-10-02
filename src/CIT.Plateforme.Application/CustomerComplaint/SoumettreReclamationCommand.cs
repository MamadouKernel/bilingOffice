using System;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.CustomerComplaint;

public sealed record SoumettreReclamationRequete(Guid SocieteId, Guid CategorieReclamationId, string Objet, string Description);

public sealed record SoumettreReclamationResultat(Guid TicketId, string NumeroTicket);

public sealed class SoumettreReclamationCommand(
    ITicketRepository ticketRepository,
    ICategorieReclamationRepository categorieRepository,
    IDossierRepository dossierRepository,
    IUtilisateurSocieteRepository utilisateurSocieteRepository,
    IUniteDeTravail uniteDeTravail,
    IHorloge horloge,
    IServiceNumerotation numerotation,
    IJournalisationService journalisationService)
{
    /// <summary>
    /// Module porteur des reclamations. Sert aussi de filtre aux files eligibles comme file cible
    /// d'une categorie (REMEDIATION RECETTE UAT, D-14) : une reclamation ne peut etre routee que
    /// vers une file de son propre module.
    /// </summary>
    public const string Module = "CustomerComplaint";

    public async Task<SoumettreReclamationResultat> ExecuterAsync(Guid appelantId, SoumettreReclamationRequete requete, CancellationToken ct = default, string origine = "Portail")
    {
        // 1. Autorisation (ENT-02, même contrôle que BillingImport/BillingExport) : le droit de
        // soumettre pour une société est individuel et porté par le rattachement lui-même
        // (rattachement.DroitSoumission), pas par une permission RBAC globale distincte — celle-ci
        // (Permission.ComplaintSoumettre) n'était jamais accordée par le parcours réel de
        // validation de compte, rendant la soumission de réclamation impossible en conditions
        // réelles malgré une couverture de test qui la simulait toujours accordée (trouvé en test
        // manuel en direct, pas par lecture de code ni par la suite automatisée).
        var rattachement = await utilisateurSocieteRepository.ObtenirParUtilisateurAsync(appelantId, ct).ConfigureAwait(false);
        if (rattachement == null || !rattachement.EstActif || !rattachement.DroitSoumission)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} n'est pas habilité à soumettre une réclamation (ENT-02).");
        }

        // Le rattachement certifié par le serveur DOIT correspondre à celui demandé (ou on force celui du serveur).
        // Si l'utilisateur essaie de soumettre pour une autre société, on refuse.
        if (rattachement.SocieteId != requete.SocieteId)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} ne peut pas soumettre pour la société {requete.SocieteId}.");
        }

        // 3. Validation de la catégorie métier
        var categorie = await categorieRepository.ObtenirParIdAsync(requete.CategorieReclamationId, ct).ConfigureAwait(false);
        if (categorie == null)
        {
            throw new InvalidOperationException($"La catégorie de réclamation {requete.CategorieReclamationId} n'existe pas.");
        }

        if (!categorie.EstActif)
        {
            throw new InvalidOperationException($"La catégorie de réclamation {categorie.Code} est inactive et ne peut plus être utilisée.");
        }

        // 4. Numérotation (D6)
        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var numeroTicket = await numerotation.ProchainNumeroTicketAsync(Module, "Reclamation", ct).ConfigureAwait(false);
        var maintenant = horloge.MaintenantUtc;

        // 5. Création des entités (Ticket = F001)
        var dossier = Dossier.Creer(numeroDossier, rattachement.SocieteId, maintenant, appelantId);

        // Résolution de la file cible via la catégorie (REC-02/REC-05)
        var fileCibleId = categorie.FileCibleDefautId;
        if (!fileCibleId.HasValue)
        {
            throw new InvalidOperationException($"La catégorie {categorie.Code} n'a pas de file cible configurée.");
        }

        var ticket = Ticket.Creer(numeroTicket, Module, "Reclamation", fileCibleId.Value, dossier.Id, maintenant);

        // Extension métier (REC-02)
        ticket.DefinirCategorieReclamation(categorie.Id);

        // REMÉDIATION POST-AUDIT (post-implementation-audit.md §2.3, use-cases.md C7-01) : le
        // contenu réel de la réclamation doit être validé et capturé avant toute persistance.
        ticket.DefinirContenuReclamation(requete.Objet, requete.Description);

        // 6. Persistance
        await dossierRepository.AjouterAsync(dossier, ct).ConfigureAwait(false);
        await ticketRepository.AjouterAsync(ticket, ct).ConfigureAwait(false);

        // 7. Audit de création (SOC-13)
        await journalisationService.ConsignerHistoriqueAsync(ticket.Id, "TicketCree", $"Réclamation soumise (origine : {origine})", appelantId, ct).ConfigureAwait(false);
        await journalisationService.ConsignerAuditAsync(appelantId, "TicketCree", "Ticket", ticket.Id, null, origine, ct).ConfigureAwait(false);
        await journalisationService.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"CustomerComplaint\",\"origine\":\"{origine}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new SoumettreReclamationResultat(ticket.Id, ticket.Numero);
    }
}
