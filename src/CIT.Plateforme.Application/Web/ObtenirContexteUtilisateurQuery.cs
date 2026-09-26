using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Web;

public sealed class ObtenirContexteUtilisateurQuery(
    IUtilisateurInfoRepository userInfoRepository,
    IUtilisateurSocieteRepository utilisateurSocieteRepository,
    ISocieteClienteRepository societeRepository,
    IHabilitationRepository habilitationRepository,
    IPerimetreSupervisionRepository perimetreSupervisionRepository)
{
    public async Task<ContexteUtilisateurVue> ExecuterAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var userInfo = await userInfoRepository.ObtenirInfoAsync(utilisateurId, ct).ConfigureAwait(false);
        if (userInfo is null || !userInfo.EstActif)
        {
            return new ContexteUtilisateurVue(
                UtilisateurId: utilisateurId,
                NomAffiche: string.Empty,
                Email: string.Empty,
                Nature: NatureUtilisateur.Client,
                EstAuthentifie: false,
                SocieteId: null,
                SocieteNom: null,
                SocieteProfil: null,
                EstAdministrateurSociete: false,
                DroitConsultation: null,
                DroitSoumission: null,
                EspacesDisponibles: Array.Empty<EspaceFonctionnel>(),
                EspaceActif: EspaceFonctionnel.Client,
                Permissions: Array.Empty<string>(),
                FilesHabilitees: Array.Empty<Guid>(),
                EstSuperviseur: false
            );
        }

        // 1. Informations société
        var userSoc = await utilisateurSocieteRepository.ObtenirParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        SocieteCliente? societe = null;
        if (userSoc is not null && userSoc.EstActif)
        {
            societe = await societeRepository.ObtenirParIdAsync(userSoc.SocieteId, ct).ConfigureAwait(false);
        }

        // 2. Habilitations & Permissions internes
        var habilitations = await habilitationRepository.ListerParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);
        var permissions = habilitations.Select(h => h.PermissionCode).Distinct().ToList();
        var filesHabilitees = habilitations.Where(h => h.FileId.HasValue).Select(h => h.FileId!.Value).Distinct().ToList();

        // 3. Supervision & Administration CIT
        var perimetreSup = await perimetreSupervisionRepository.ObtenirParSuperviseurAsync(utilisateurId, ct).ConfigureAwait(false);
        var estSuperviseur = perimetreSup is not null || permissions.Contains(Permission.SupervisionConsulter);
        var estAdminCit = permissions.Contains(Permission.CompteValider) ||
                          permissions.Contains(Permission.CompteTypeModifier) ||
                          permissions.Contains(Permission.AdministrationFiles) ||
                          permissions.Contains(Permission.AuditConsulter);

        // 4. Espaces disponibles
        var espaces = new List<EspaceFonctionnel>();
        if (estSuperviseur)
        {
            espaces.Add(EspaceFonctionnel.Supervision);
        }
        if (estAdminCit)
        {
            espaces.Add(EspaceFonctionnel.AdministrationCit);
        }
        if (filesHabilitees.Count > 0 || permissions.Contains(Permission.TicketPrendreEnCharge) || userInfo.Nature == NatureUtilisateur.Agent)
        {
            espaces.Add(EspaceFonctionnel.Agent);
        }
        // SUPPORT IT : cockpit dédié du Support N1, disponible dès que le compte peut instruire les
        // incidents techniques. Persona additionnel (le Super-Admin le voit aussi) ; l'espace actif
        // par défaut reste le premier détecté, jamais forcé sur celui-ci.
        if (permissions.Contains(Permission.SupportIncidentInstruire))
        {
            espaces.Add(EspaceFonctionnel.SupportN1);
        }
        if (userSoc is not null && userSoc.EstAdministrateur)
        {
            espaces.Add(EspaceFonctionnel.AdministrateurSociete);
        }
        if (userSoc is not null || userInfo.Nature == NatureUtilisateur.Client)
        {
            espaces.Add(EspaceFonctionnel.Client);
        }

        // Si aucun espace détecté mais utilisateur actif
        if (espaces.Count == 0)
        {
            espaces.Add(userInfo.Nature == NatureUtilisateur.Agent ? EspaceFonctionnel.Agent : EspaceFonctionnel.Client);
        }

        // Espace actif prioritaire selon CLAR-UX-01 / D3
        var espaceActif = espaces.First();

        var societeNom = societe?.InformationsDeclarees;
        if (!string.IsNullOrWhiteSpace(societeNom) && societeNom.Contains("RaisonSociale"))
        {
            // Extraire un nom convivial si stocké sous forme clé/valeur, sinon garder le texte
            societeNom = ExtraireNomSociete(societeNom);
        }

        return new ContexteUtilisateurVue(
            UtilisateurId: userInfo.Id,
            NomAffiche: string.IsNullOrWhiteSpace(userInfo.NomAffiche) ? userInfo.UserName : userInfo.NomAffiche,
            Email: userInfo.Email,
            Nature: userInfo.Nature,
            EstAuthentifie: true,
            SocieteId: societe?.Id,
            SocieteNom: societeNom,
            SocieteProfil: societe?.Profil,
            EstAdministrateurSociete: userSoc?.EstAdministrateur ?? false,
            DroitConsultation: userSoc?.DroitConsultation,
            DroitSoumission: userSoc?.DroitSoumission,
            EspacesDisponibles: espaces,
            EspaceActif: espaceActif,
            Permissions: permissions,
            FilesHabilitees: filesHabilitees,
            EstSuperviseur: estSuperviseur
        );
    }

    private static string ExtraireNomSociete(string raw)
    {
        try
        {
            var parts = raw.Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (part.Contains("RaisonSociale", StringComparison.OrdinalIgnoreCase))
                {
                    var kv = part.Split(':');
                    if (kv.Length > 1) return kv[1].Trim().Trim('"', '{', '}', ' ');
                }
            }
        }
        catch
        {
            // En cas d'erreur de parsing, retourner la chaîne brute
        }
        return raw;
    }
}
