using System;
using System.Collections.Generic;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Web;

public enum EspaceFonctionnel
{
    Client,
    AdministrateurSociete,
    Agent,
    Supervision,
    AdministrationCit
}

public sealed record ContexteUtilisateurVue(
    Guid UtilisateurId,
    string NomAffiche,
    string Email,
    NatureUtilisateur Nature,
    bool EstAuthentifie,
    Guid? SocieteId,
    string? SocieteNom,
    TypeClient? SocieteProfil,
    bool EstAdministrateurSociete,
    DroitConsultation? DroitConsultation,
    bool? DroitSoumission,
    IReadOnlyList<EspaceFonctionnel> EspacesDisponibles,
    EspaceFonctionnel EspaceActif,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<Guid> FilesHabilitees,
    bool EstSuperviseur
);
