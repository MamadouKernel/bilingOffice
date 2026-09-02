namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// CPT-01 — justificatif déposé à l'appui d'une demande de compte société.
///
/// Volontairement distinct de <see cref="CIT.Plateforme.Domain.Documents.Document"/> : celui-ci
/// est attaché à un ticket par une clé non nullable, et une demande de compte n'est pas un ticket.
/// Rendre ce rattachement optionnel aurait affaibli un invariant de la Feature 001 pour un besoin
/// qui lui est étranger.
///
/// Comme pour les documents de ticket, seules les métadonnées vivent ici : le contenu binaire est
/// confié à <see cref="CIT.Plateforme.Domain.Documents.IStockageDocuments"/>.
/// </summary>
public sealed class JustificatifDemandeCompte
{
    public Guid Id { get; private set; }

    public Guid SocieteId { get; private set; }

    /// <summary>Code de la <see cref="PieceExigeeCompte"/> à laquelle ce dépôt répond.</summary>
    public string PieceCode { get; private set; } = string.Empty;

    public string NomFichier { get; private set; } = string.Empty;

    public string TypeContenu { get; private set; } = string.Empty;

    public long TailleOctets { get; private set; }

    public string CleStockage { get; private set; } = string.Empty;

    public DateTime DateDepotUtc { get; private set; }

    private JustificatifDemandeCompte() { }

    public static JustificatifDemandeCompte Deposer(
        Guid societeId,
        string pieceCode,
        string nomFichier,
        string typeContenu,
        long tailleOctets,
        string cleStockage,
        DateTime maintenantUtc)
    {
        if (string.IsNullOrWhiteSpace(pieceCode))
        {
            throw new ArgumentException("Le code de la pièce justifiée est obligatoire.", nameof(pieceCode));
        }

        if (string.IsNullOrWhiteSpace(nomFichier))
        {
            throw new ArgumentException("Le nom du fichier est obligatoire.", nameof(nomFichier));
        }

        return new JustificatifDemandeCompte
        {
            Id = Guid.NewGuid(),
            SocieteId = societeId,
            PieceCode = pieceCode.Trim(),
            NomFichier = nomFichier.Trim(),
            TypeContenu = typeContenu,
            TailleOctets = tailleOctets,
            CleStockage = cleStockage,
            DateDepotUtc = maintenantUtc,
        };
    }
}
