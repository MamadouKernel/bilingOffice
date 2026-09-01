using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Notifications.Resolveurs;

/// <summary>
/// FEATURE 009 (T051) — RACCORDEMENT DE MSG-04 (research.md D5, contrat C9-11, Option B).
///
/// MSG-04 (Should) : « Un collaborateur peut être mentionné (@Nom) dans une note interne ; la
/// personne mentionnée reçoit une notification dans la plateforme et, selon paramétrage, un
/// e-mail avec lien vers le ticket. »
///
/// MSG-04 RESTE UNE EXIGENCE DE F001 et n'est PAS recomptée dans les 9 exigences de F009
/// (spec.md §27). F009 en livre uniquement le raccordement notification manquant.
///
/// COÛT EN CODE MÉTIER : ZÉRO. <c>MentionnerCollaborateurCommand</c> (F001) publie déjà
/// l'événement <c>CollaborateurMentionne</c> avec <c>collaborateurId</c> dans sa charge utile,
/// et n'est PAS modifiée. <c>Message</c> n'est pas modifiée non plus. MSG-05 (aucune
/// réaffectation) et MSG-02 (étanchéité client) sont donc préservés PAR CONSTRUCTION : ce
/// résolveur ne produit aucun effet de workflow et ne résout jamais un compte client.
///
/// Le destinataire est le collaborateur NOMMÉMENT mentionné — jamais la file, jamais les autres
/// agents habilités, jamais le client (moindre privilège, CLAR-NOT-04).
/// </summary>
public sealed class ResolveurCollaborateurMentionne(IUtilisateurInfoRepository utilisateurs) : IResolveurDestinataires
{
    public const string TypeEvenement = "CollaborateurMentionne";

    public bool PeutResoudre(string typeEvenement) => typeEvenement == TypeEvenement;

    public async Task<IReadOnlyList<Guid>> ResoudreAsync(string typeEvenementRecu, string chargeUtile, CancellationToken ct = default)
    {
        if (!PeutResoudre(typeEvenementRecu))
        {
            return [];
        }

        var collaborateurId = ResolveurChargeUtile.LireGuid(chargeUtile, "collaborateurId");
        if (collaborateurId is null)
        {
            return [];
        }

        var profil = await utilisateurs.ObtenirProfilAsync(collaborateurId.Value, ct).ConfigureAwait(false);

        // Collaborateur inexistant (T194) : aucune notification, aucune exception — la
        // distribution des autres événements n'est pas interrompue.
        if (profil is null)
        {
            return [];
        }

        // Collaborateur désactivé (T194) : aucune notification.
        if (!profil.EstActif)
        {
            return [];
        }

        // Destinataire hors périmètre (T195) : une note interne ne peut JAMAIS devenir une
        // notification client, même si la charge utile désigne un compte client par erreur
        // (MSG-02, spec.md §14 règle 3). Seul un agent peut être notifié d'une mention interne.
        if (profil.Nature != NatureUtilisateur.Agent)
        {
            return [];
        }

        return [collaborateurId.Value];
    }

    public async Task<Guid?> ResoudreTicketAsync(string chargeUtile, CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return ResolveurChargeUtile.LireGuid(chargeUtile, "ticketId");
    }
}
