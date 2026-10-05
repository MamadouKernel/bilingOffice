using System;
using System.Collections.Generic;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Tickets;

public sealed record ChronologieItemVue(
    Guid Id,
    string Evenement,
    string? Detail,
    Guid? AuteurId,
    string AuteurNom,
    DateTime HorodatageUtc
);

public sealed record DocumentItemVue(
    Guid Id,
    string NomFichier,
    string TypeContenu,
    long TailleOctets,
    DateTime DateDepotUtc,
    StatutAnalyseAntivirus StatutAntivirus,
    /// <summary>IMP-01 : catégorie de la pièce sur le parcours Billing Import ; <c>null</c>
    /// ailleurs.</summary>
    CategorieDocumentBillingImport? Categorie = null
);

public sealed record MessageItemVue(
    Guid Id,
    string Contenu,
    string AuteurNom,
    bool EstInterne,
    DateTime DateEnvoiUtc
);

/// <summary>SOC-10 — un ticket auquel celui-ci est lié, vu depuis la fiche. La liaison est
/// symétrique : peu importe de quel côté elle a été créée, les deux fiches se voient.</summary>
public sealed record LiaisonItemVue(
    Guid TicketId,
    string Numero,
    string Module,
    StatutTicket Statut,
    DateTime DateLiaisonUtc
);

public sealed record FicheTicketVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    string Module,
    string TypeDemande,
    StatutTicket Statut,
    Guid DossierId,
    string NumeroDossier,
    Guid FileId,
    string FileNom,
    Guid? OperateurAssigneId,
    string? OperateurAssigneNom,
    Guid SocieteId,
    string SocieteNom,
    DateTime DateCreationUtc,
    DateTime? DatePriseEnChargeUtc,
    DateTime? DateClotureUtc,
    // RG-TCK-06 : l'archivage est une PROPRIETE OBSERVABLE du ticket, pas une affirmation de
    // commentaire. La fiche la restitue, de sorte qu'elle soit verifiable par le chemin reel.
    bool EstArchive,
    DateTime? DateArchivageUtc,
    bool EstEnEtatEngageant,
    bool DeclarationPresentielle,
    bool PeutPrendreEnCharge,
    bool PeutTransferer,
    bool PeutMettreEnAttenteClient,
    bool PeutReprendreApresClient,
    bool PeutCloturer,
    bool PeutAnnuler,
    bool PeutAjouterMessage,
    bool PeutDeposerDocument,
    IReadOnlyList<ChronologieItemVue> Chronologie,
    IReadOnlyList<DocumentItemVue> Documents,
    IReadOnlyList<MessageItemVue> Messages,
    string? NumeroBooking = null,
    TypeAnnulationExport? TypeAnnulationExport = null,
    Guid? CategorieReclamationId = null,
    string? CategorieReclamationLibelle = null,
    string? Objet = null,
    string? Description = null,
    string? ReponseFormalisee = null,
    int? EvaluationNote = null,
    string? EvaluationCommentaire = null,
    DateTime? EvaluationDateUtc = null,

    /// <summary>IMP-10 — « le client demande la facture depuis son ticket ». Le droit est calculé
    /// ici, comme tous les autres, et jamais déduit dans la page.</summary>
    bool PeutDemanderFacture = false,

    /// <summary>SOC-10 — « liaison entre tickets, y compris entre modules, visible par les agents
    /// ET par le client concerné ». La liste part vide plutôt qu'absente : une fiche sans liaison
    /// est un cas normal, pas une donnée manquante.</summary>
    IReadOnlyList<LiaisonItemVue>? Liaisons = null,

    /// <summary>SOC-10 — seul un agent habilité sur la file crée une liaison ; le client la voit
    /// sans pouvoir la poser.</summary>
    bool PeutLierTickets = false,

    /// <summary>IMP-13 — libellé littéral CDC du statut de paiement (« En attente paiement »,
    /// « En attente validation paiement », « Payé confirmé »), affichage uniquement — dérivé de
    /// la file courante, jamais une nouvelle donnée persistée. <c>null</c> hors cycle paiement.</summary>
    string? LibellePaiementCdc = null,

    /// <summary>Cas particuliers du BL import (dangereux, sous palan, véhicule usagé).</summary>
    CIT.Plateforme.Domain.Tickets.CasParticulierImport CasParticuliers = CIT.Plateforme.Domain.Tickets.CasParticulierImport.Aucun,

    /// <summary>Étape métier lisible par le client (clé « Etape.* », voir <see cref="EtapeParcoursClient"/>).</summary>
    string? EtapeParcours = null,

    /// <summary>Proforma déposée dans le ticket et réponse du client.</summary>
    EtatProforma EtatProforma = EtatProforma.Aucune,

    /// <summary>Date d'enlèvement souhaitée par le client, retenue par la proforma (import).</summary>
    DateOnly? DateEnlevementSouhaitee = null,

    /// <summary>Le client peut valider la proforma ou en demander la correction.</summary>
    bool PeutRepondreProforma = false,

    /// <summary>Le client peut joindre sa preuve de paiement (file Validation Paiement).</summary>
    bool PeutDeposerPreuvePaiement = false,

    /// <summary>Nom de l'agent qui a le ticket en charge quand l'appelant (agent non superviseur) ne
    /// peut pas le traiter : la fiche passe en lecture seule et propose la demande de réaffectation.</summary>
    string? PrisEnChargeParAutre = null
);
