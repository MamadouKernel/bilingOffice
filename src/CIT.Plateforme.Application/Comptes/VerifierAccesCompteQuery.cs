using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Contrat C2-03 — Vérifier l'accès au compte après authentification Identity réussie. CDC :
/// CPT-03. Complète l'authentification technique (D1, inchangée) d'une règle métier propre au
/// compte société : société active + rattachement actif (research.md D10).
///
/// CORRECTIF — la règle exigeait un rattachement à une société pour TOUT compte de nature
/// Client, sans distinguer deux situations que rien ne permet de confondre :
///
///   • un DEMANDEUR dont le dossier n'est pas encore tranché, ou l'a été négativement. L'accès
///     doit lui être refusé : c'est exactement ce qu'exige CPT-01, « soumise à validation par
///     CIT avant activation ».
///
///   • une PERSONNE PHYSIQUE qui n'a jamais déposé de demande de société — le transporteur ou
///     chauffeur du module Enrôlement, que le CDC § 6 désigne comme un profil à part entière et
///     que CLAR-ENR-01 décrit sans société rattachée. Lui refuser l'accès n'était pas une règle
///     métier : c'était l'effet de bord d'une condition trop large, et il rendait le module
///     Enrôlement inaccessible à ses propres usagers.
///
/// La distinction se lit sans ambiguïté : le demandeur a déposé une demande, la personne
/// physique n'en a aucune. CPT-01 encadre les demandes de compte société ; il ne dit rien de
/// quelqu'un qui n'en a jamais formulé.
///
/// L'ABSENCE N'AUTORISE JAMAIS À ELLE SEULE : le compte doit d'abord exister et être actif.
/// Sans cette condition, un identifiant inconnu serait autorisé par défaut — une permissivité
/// que rien ne justifie, même si l'appelant n'invoque cette requête qu'après authentification.
/// Ce contrôle referme du même coup l'accès d'un compte désactivé dès la connexion, sans
/// attendre la résolution du contexte (SEC-11).
/// </summary>
public sealed class VerifierAccesCompteQuery(
    IUtilisateurSocieteRepository rattachements,
    ISocieteClienteRepository societes,
    IUtilisateurInfoRepository utilisateurs)
{
    public async Task<bool> AccesAutoriseAsync(Guid utilisateurId, CancellationToken ct = default)
    {
        var compte = await utilisateurs.ObtenirInfoAsync(utilisateurId, ct).ConfigureAwait(false);
        if (compte is null || !compte.EstActif)
        {
            return false;
        }

        var rattachement = await rattachements.ObtenirParUtilisateurAsync(utilisateurId, ct).ConfigureAwait(false);

        if (rattachement is not null)
        {
            // Rattaché : la règle d'origine s'applique intégralement — le rattachement doit être
            // actif ET la société validée.
            if (!rattachement.EstActif)
            {
                return false;
            }

            var societe = await societes.ObtenirParIdAsync(rattachement.SocieteId, ct).ConfigureAwait(false);
            return societe is not null && societe.Etat == EtatSociete.Active;
        }

        // Non rattaché : l'accès dépend de l'existence d'une demande de société. Une demande non
        // tranchée ou refusée ferme l'accès (CPT-01) ; aucune demande signifie que l'utilisateur
        // ne relève pas de ce parcours et n'a donc pas à en subir la condition.
        var demande = await societes.ObtenirParDemandeurAsync(utilisateurId, ct).ConfigureAwait(false);
        return demande is null;
    }
}
