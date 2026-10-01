using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>Une pièce attendue à l'ouverture d'un compte, telle que présentée au demandeur.</summary>
public sealed record PieceExigeeVue(Guid Id, string Code, string Libelle, bool Obligatoire, bool EstActif, int Ordre);

/// <summary>
/// CPT-01 — « les informations et justificatifs exigés sont PARAMÉTRABLES », et § 17.3
/// « paramétrage des pièces exigées ».
///
/// La liste attendue d'un nouveau client évolue avec la réglementation : elle est administrée,
/// jamais codée en dur. Aucune pièce n'est livrée par défaut — le CDC n'en nomme aucune, et en
/// inventer reviendrait à décider à la place du métier.
/// </summary>
public sealed class GererPiecesExigeesCommand(
    IPieceExigeeCompteRepository pieces,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task<Guid> DefinirAsync(
        Guid appelantId, string code, string libelle, bool obligatoire, int ordre, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);

        if (await pieces.CodeExisteAsync(code.Trim(), ct).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Une pièce portant le code « {code.Trim()} » existe déjà.");
        }

        var piece = PieceExigeeCompte.Definir(code, libelle, obligatoire, ordre);
        await pieces.AjouterAsync(piece, ct).ConfigureAwait(false);

        await journalisation.ConsignerAuditAsync(
            appelantId, "PieceExigeeDefinie", nameof(PieceExigeeCompte), piece.Id, null, piece.Code, ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        return piece.Id;
    }

    public async Task ModifierAsync(
        Guid appelantId, Guid pieceId, string libelle, bool obligatoire, int ordre, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);

        var piece = await pieces.ObtenirParIdAsync(pieceId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Pièce {pieceId} introuvable.");

        var avant = $"{piece.Libelle} (obligatoire={piece.Obligatoire})";
        piece.Modifier(libelle, obligatoire, ordre);

        await journalisation.ConsignerAuditAsync(
            appelantId, "PieceExigeeModifiee", nameof(PieceExigeeCompte), piece.Id,
            avant, $"{piece.Libelle} (obligatoire={piece.Obligatoire})", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Désactiver plutôt que supprimer : les demandes déjà déposées référencent ce code, et leur
    /// dossier doit rester lisible après coup.
    /// </summary>
    public async Task DefinirActivationAsync(Guid appelantId, Guid pieceId, bool actif, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.AdministrationComptes, ct: ct).ConfigureAwait(false);

        var piece = await pieces.ObtenirParIdAsync(pieceId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Pièce {pieceId} introuvable.");

        var avant = piece.EstActif ? "active" : "inactive";
        if (actif)
        {
            piece.Activer();
        }
        else
        {
            piece.Desactiver();
        }

        await journalisation.ConsignerAuditAsync(
            appelantId, actif ? "PieceExigeeActivee" : "PieceExigeeDesactivee", nameof(PieceExigeeCompte), piece.Id,
            avant, actif ? "active" : "inactive", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}

/// <summary>
/// CPT-01 — liste des pièces attendues.
///
/// Volontairement SANS contrôle d'autorisation : le formulaire d'ouverture de compte est public
/// par nature, et savoir quels justificatifs préparer n'est pas une information sensible. Exiger
/// une permission ici rendrait le parcours impraticable.
/// </summary>
public sealed class ListerPiecesExigeesQuery(IPieceExigeeCompteRepository pieces)
{
    public async Task<IReadOnlyList<PieceExigeeVue>> ExecuterAsync(bool activesUniquement = true, CancellationToken ct = default)
    {
        var toutes = await pieces.ListerAsync(activesUniquement, ct).ConfigureAwait(false);
        return toutes
            .OrderBy(p => p.Ordre)
            .ThenBy(p => p.Libelle, StringComparer.CurrentCulture)
            .Select(p => new PieceExigeeVue(p.Id, p.Code, p.Libelle, p.Obligatoire, p.EstActif, p.Ordre))
            .ToList();
    }
}

/// <summary>Justificatif déposé, tel que présenté à l'administrateur qui instruit la demande.</summary>
public sealed record JustificatifVue(Guid Id, string PieceCode, string NomFichier, long TailleOctets, DateTime DateDepotUtc);

/// <summary>
/// CPT-01 — pièces réellement produites à l'appui d'une demande, pour que l'administrateur
/// fonctionnel instruise sur pièces plutôt que sur déclaration.
/// </summary>
public sealed class ListerJustificatifsDemandeQuery(
    IJustificatifDemandeCompteRepository justificatifs,
    IAutorisationService autorisation)
{
    public async Task<IReadOnlyList<JustificatifVue>> ExecuterAsync(Guid appelantId, Guid societeId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.CompteValider, ct: ct).ConfigureAwait(false);

        var deposes = await justificatifs.ListerParSocieteAsync(societeId, ct).ConfigureAwait(false);
        return deposes
            .OrderBy(j => j.PieceCode, StringComparer.Ordinal)
            .Select(j => new JustificatifVue(j.Id, j.PieceCode, j.NomFichier, j.TailleOctets, j.DateDepotUtc))
            .ToList();
    }
}

/// <summary>Contenu binaire d'un justificatif, pour aperçu ou téléchargement dans l'application.</summary>
public sealed record ContenuJustificatif(byte[] Contenu, string TypeContenu, string NomFichier);

/// <summary>
/// CPT-01 — lecture du contenu d'un justificatif pour que l'administrateur fonctionnel instruise
/// la demande « sur pièces » directement dans l'application. Même habilitation que la liste
/// (<see cref="Permission.CompteValider"/>) : lire une pièce n'ouvre aucun accès que l'écran de
/// validation n'aurait pas déjà.
/// </summary>
public sealed class TelechargerJustificatifQuery(
    IJustificatifDemandeCompteRepository justificatifs,
    IStockageDocuments stockage,
    IAutorisationService autorisation)
{
    public async Task<ContenuJustificatif> ExecuterAsync(Guid appelantId, Guid justificatifId, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.CompteValider, ct: ct).ConfigureAwait(false);

        var justificatif = await justificatifs.ObtenirParIdAsync(justificatifId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Justificatif {justificatifId} introuvable.");

        await using var flux = await stockage.LireAsync(justificatif.CleStockage, ct).ConfigureAwait(false);
        using var memoire = new MemoryStream();
        await flux.CopyToAsync(memoire, ct).ConfigureAwait(false);

        return new ContenuJustificatif(memoire.ToArray(), justificatif.TypeContenu, justificatif.NomFichier);
    }
}
