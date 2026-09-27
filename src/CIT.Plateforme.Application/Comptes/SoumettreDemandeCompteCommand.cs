using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>Justificatif déjà stocké, décrit par ses seules métadonnées : le contenu binaire a été
/// confié au stockage documentaire avant l'appel.</summary>
public sealed record JustificatifDepose(string PieceCode, string NomFichier, string TypeContenu, long TailleOctets, string CleStockage);

/// <summary>
/// CPT-01 — le dossier ne réunit pas les pièces obligatoires en vigueur. Distincte d'une erreur
/// technique : elle nomme les pièces manquantes pour que le demandeur puisse compléter.
/// </summary>
public sealed class DossierIncompletException(IReadOnlyList<string> piecesManquantes)
    : Exception($"Justificatif(s) manquant(s) : {string.Join(", ", piecesManquantes)}.")
{
    public IReadOnlyList<string> PiecesManquantes { get; } = piecesManquantes;
}

/// <summary>
/// Contrat C2-01 — Soumettre une demande de compte société. CDC : CPT-01, CPT-02.
/// </summary>
/// <summary>
/// <paramref name="DemandeurId"/> : identifiant du compte Identity du demandeur (créé côté Web au
/// moment de la soumission — D1, aucune modification d'Identity) ; conservé pour désigner
/// automatiquement le premier administrateur de société à la validation (spec.md § Assumptions,
/// contrat C2-02).
///
/// <paramref name="Justificatifs"/> : pièces produites à l'appui de la demande (CPT-01). Leur
/// complétude est vérifiée ici contre le référentiel paramétrable en vigueur.
/// </summary>
// RaisonSociale / Rccm : identité STRUCTURÉE, obligatoire depuis le formulaire (champs dédiés,
// RCCM validé et unique). Laissées nulles, la demande suit l'ancienne voie (texte déclaré seul),
// conservée pour les appelants existants.
public sealed record SoumettreDemandeCompteRequete(
    Guid DemandeurId,
    string InformationsDeclarees,
    TypeClient ProfilDeclare,
    IReadOnlyList<JustificatifDepose>? Justificatifs = null,
    string? RaisonSociale = null,
    string? Rccm = null);

/// <summary>Un compte existe déjà (en attente ou actif) pour ce RCCM.</summary>
public sealed class RccmDejaUtiliseException(string rccm)
    : InvalidOperationException($"Une société est déjà enregistrée avec le RCCM {rccm}.");

public sealed class SoumettreDemandeCompteCommand(
    ISocieteClienteRepository societes,
    IPieceExigeeCompteRepository piecesExigees,
    IJustificatifDemandeCompteRepository justificatifs,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<SocieteCliente> ExecuterAsync(SoumettreDemandeCompteRequete requete, CancellationToken ct = default)
    {
        var deposes = requete.Justificatifs ?? Array.Empty<JustificatifDepose>();

        // CPT-01 — contrôle de complétude contre le référentiel PARAMÉTRABLE, jamais contre une
        // liste codée en dur. Tant qu'aucune pièce obligatoire n'est paramétrée, le contrôle
        // passe : c'est le métier qui décide de ce qu'il exige, pas ce code.
        var attendues = await piecesExigees.ListerAsync(activesUniquement: true, ct).ConfigureAwait(false);
        var codesDeposes = deposes.Select(j => j.PieceCode).ToHashSet(StringComparer.Ordinal);

        var manquantes = attendues
            .Where(p => p.Obligatoire && !codesDeposes.Contains(p.Code))
            .OrderBy(p => p.Ordre)
            .Select(p => p.Libelle)
            .ToList();

        if (manquantes.Count > 0)
        {
            throw new DossierIncompletException(manquantes);
        }

        SocieteCliente societe;
        if (requete.RaisonSociale is null && requete.Rccm is null)
        {
            societe = SocieteCliente.Soumettre(requete.DemandeurId, requete.InformationsDeclarees, requete.ProfilDeclare, horloge.MaintenantUtc);
        }
        else
        {
            // Validation du domaine (raison sociale obligatoire, format RCCM) AVANT l'unicité.
            var rccm = NumeroRccm.Normaliser(requete.Rccm);
            if (await societes.RccmDejaUtiliseAsync(rccm, ct: ct).ConfigureAwait(false))
            {
                throw new RccmDejaUtiliseException(rccm);
            }

            societe = SocieteCliente.Soumettre(
                requete.DemandeurId, requete.RaisonSociale ?? string.Empty, rccm,
                requete.InformationsDeclarees, requete.ProfilDeclare, horloge.MaintenantUtc);
        }
        await societes.AjouterAsync(societe, ct).ConfigureAwait(false);

        foreach (var depose in deposes)
        {
            await justificatifs.AjouterAsync(
                JustificatifDemandeCompte.Deposer(
                    societe.Id, depose.PieceCode, depose.NomFichier, depose.TypeContenu,
                    depose.TailleOctets, depose.CleStockage, horloge.MaintenantUtc),
                ct).ConfigureAwait(false);
        }

        await journalisation.ConsignerAuditAsync(requete.DemandeurId, "DemandeCompteSoumise", nameof(SocieteCliente), societe.Id, null, requete.ProfilDeclare.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return societe;
    }
}
