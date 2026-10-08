using System;
using System.Collections.Generic;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Societe;

public sealed record CollaborateurVue(
    Guid Id,
    Guid UtilisateurId,
    string NomAffiche,
    string Email,
    bool EstAdministrateur,
    bool DroitSoumission,
    DroitConsultation DroitConsultation,
    bool EstActif,
    DateTime DateRattachementUtc,
    bool DroitValidation = false
);

public sealed record DashboardSocieteVue(
    Guid SocieteId,
    string SocieteNom,
    TypeClient SocieteProfil,
    EtatSociete SocieteStatut,
    int NombreAdministrateursActifs,
    IReadOnlyList<CollaborateurVue> Collaborateurs,
    int TotalDossiersSociete,
    int TotalTicketsEnCours
);
