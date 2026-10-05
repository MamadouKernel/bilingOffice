using System;
using System.Collections.Generic;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Client;

public enum TypeActionRequise
{
    ComplementManifeste,
    ComplementExport,
    PaiementAttendu,
    NouvellePreuveRequise,
    ComplementReclamation,
    ProformaAValider
}

public sealed record ActionRequiseItemVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    TypeActionRequise TypeAction,
    string Titre,
    string Description,
    string ActionLibelle,
    string UrlCible,
    DateTime DateDeclenchementUtc,
    string? NumeroBooking = null
);

public enum StatutVisuelDossier
{
    EnAttentePriseEnCharge,
    EnCoursTraitement,
    EnAttenteClient,
    ClotureTermine,
    Rejete,
    Annule
}

public sealed record TicketResumeVue(
    Guid TicketId,
    string NumeroTicket,
    string? NumeroBl,
    StatutTicket Statut,
    DateTime DateDerniereActiviteUtc,
    string? NumeroBooking = null,
    /// <summary>Étape métier du parcours Billing (clé « Etape.* »), voir EtapeParcoursClient.</summary>
    string? Etape = null
);

public sealed record DossierResumeVue(
    Guid DossierId,
    string NumeroDossier,
    string Module,
    string TypeDemande,
    DateTime DateCreationUtc,
    StatutVisuelDossier EtatGlobal,
    int NombreTickets,
    IReadOnlyList<TicketResumeVue> Tickets
);

public sealed record DashboardClientVue(
    Guid SocieteId,
    string SocieteNom,
    TypeClient SocieteProfil,
    EtatSociete SocieteStatut,
    IReadOnlyList<ActionRequiseItemVue> ActionsRequises,
    IReadOnlyList<DossierResumeVue> DemandesRecentes,
    int TotalDemandesActives,
    int TotalEnAttenteCit,
    int TotalActionsRequises,
    // AM-02 — droit de soumission INDIVIDUEL du collaborateur, résolu côté serveur à partir du
    // rattachement (EstActif && DroitSoumission). Reflète EXACTEMENT la règle que les commandes
    // SoumettreDemandeImport/Export appliquent pour refuser : l'IHM ne fait que masquer le
    // raccourci qui mènerait à un 403, la frontière de sécurité reste la commande serveur.
    bool PeutSoumettre
);
