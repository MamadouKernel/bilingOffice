using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Modification par un utilisateur de ses propres données de compte.
/// <para>
/// <b>Périmètre volontairement restreint</b> : nom affiché et numéro de téléphone uniquement.
/// L'e-mail est exclu — il constitue l'identifiant de connexion, et sa modification est une
/// opération d'identité qui relève d'un parcours dédié (vérification du nouveau courriel), pas
/// d'un simple formulaire de profil.
/// </para>
/// <para>
/// <b>Anti-IDOR par construction</b> : la requête ne porte aucun identifiant d'utilisateur.
/// La cible est toujours <c>appelantId</c>, dérivé du contexte authentifié — il n'existe donc
/// aucun chemin permettant de modifier le profil d'un tiers via ce contrat.
/// </para>
/// </summary>
public sealed record ModifierMonProfilRequete(string NomAffiche, string? Telephone);

public sealed class ModifierMonProfilCommand(
    IUtilisateurInfoRepository utilisateurs,
    IJournalisationService journalisation)
{
    public const int LongueurMaxNomAffiche = 128;
    public const int LongueurMaxTelephone = 32;

    public async Task ExecuterAsync(Guid appelantId, ModifierMonProfilRequete requete, CancellationToken ct = default)
    {
        var nomAffiche = requete.NomAffiche?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nomAffiche))
        {
            throw new ArgumentException("Le nom affiché est obligatoire.", nameof(requete));
        }

        if (nomAffiche.Length > LongueurMaxNomAffiche)
        {
            throw new ArgumentException($"Le nom affiché ne peut pas dépasser {LongueurMaxNomAffiche} caractères.", nameof(requete));
        }

        // Le téléphone reste facultatif : une chaîne vide équivaut à « aucun numéro ».
        // Aucun format n'est imposé — le CDC n'en fixe aucun et les formats de numérotation
        // varient (national, international) ; inventer une contrainte serait une règle métier.
        var telephone = string.IsNullOrWhiteSpace(requete.Telephone) ? null : requete.Telephone.Trim();

        if (telephone is not null && telephone.Length > LongueurMaxTelephone)
        {
            throw new ArgumentException($"Le numéro de téléphone ne peut pas dépasser {LongueurMaxTelephone} caractères.", nameof(requete));
        }

        var profilAvant = await utilisateurs.ObtenirProfilAsync(appelantId, ct).ConfigureAwait(false)
            ?? throw new AccesRefuseException($"Aucun compte ne correspond à l'utilisateur {appelantId}.");

        var misAJour = await utilisateurs.MettreAJourProfilAsync(appelantId, nomAffiche, telephone, ct).ConfigureAwait(false);
        if (!misAJour)
        {
            throw new AccesRefuseException($"Le profil de l'utilisateur {appelantId} n'a pas pu être mis à jour.");
        }

        // Audit (SOC-13) : l'auteur est aussi la cible, ce qui est le propre d'une
        // modification de son propre profil. Le numéro n'est pas journalisé en clair
        // (donnée personnelle — Principe XIV) : seul le fait qu'il ait changé l'est.
        var avant = $"nom={profilAvant.NomAffiche}; telephoneRenseigne={profilAvant.Telephone is not null}";
        var apres = $"nom={nomAffiche}; telephoneRenseigne={telephone is not null}";

        await journalisation.ConsignerAuditAsync(
            appelantId, "ProfilPersonnelModifie", "Utilisateur", appelantId, avant, apres, ct).ConfigureAwait(false);
    }
}
