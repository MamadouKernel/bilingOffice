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
    AdministrationCit,

    /// <summary>SUPPORT IT — cockpit du Support N1 (équipe IT) : file Support IT et raccourcis de
    /// diagnostic (recherche de dossier, files en lecture, journal d'audit). Persona disponible dès
    /// que le compte détient <see cref="Domain.Securite.Permission.SupportIncidentInstruire"/>.</summary>
    SupportN1
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
    bool EstSuperviseur,
    bool? DroitValidation = false
)
{
    /// <summary>
    /// Compte transporteur/chauffeur (ENR-01, <c>InscrireTransporteurCommand</c>) : client rattaché
    /// à AUCUNE société et titulaire du droit d'enrôlement. Son espace utile est celui de ses
    /// enrôlements, pas les écrans « société » (tableau de bord, demandes, rendez-vous).
    /// <para>La nature <see cref="NatureUtilisateur.Client"/> est indispensable : un agent (ex. le
    /// Super Administrateur, qui détient TOUTES les permissions, dont l'enrôlement, sans société)
    /// n'est jamais un transporteur.</para>
    /// </summary>
    public bool EstTransporteur =>
        EstAuthentifie
        && Nature == NatureUtilisateur.Client
        && SocieteId is null
        && Permissions.Contains(Domain.Securite.Permission.EnrolementSoumettre);
}
