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
    StatutAnalyseAntivirus StatutAntivirus
);

public sealed record MessageItemVue(
    Guid Id,
    string Contenu,
    string AuteurNom,
    bool EstInterne,
    DateTime DateEnvoiUtc
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
    DateTime? EvaluationDateUtc = null
);
