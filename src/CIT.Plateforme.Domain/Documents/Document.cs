namespace CIT.Plateforme.Domain.Documents;

public enum StatutAnalyseAntivirus
{
    NonAnalyse,
    Sain,
    Infecte,
}

/// <summary>
/// Pièce attachée à un ticket (SOC-07). Le contenu binaire est stocké derrière
/// <see cref="IStockageDocuments"/> (D9) — cette entité ne porte que les métadonnées.
/// </summary>
public sealed class Document
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Guid DeposeParId { get; private set; }

    public string NomFichier { get; private set; } = string.Empty;

    public string TypeContenu { get; private set; } = string.Empty;

    public long TailleOctets { get; private set; }

    public string CleStockage { get; private set; } = string.Empty;

    public StatutAnalyseAntivirus StatutAntivirus { get; private set; } = StatutAnalyseAntivirus.NonAnalyse;

    public DateTime DateDepotUtc { get; private set; }

    private Document() { }

    public static Document Deposer(Guid ticketId, Guid deposeParId, string nomFichier, string typeContenu, long tailleOctets, string cleStockage, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        DeposeParId = deposeParId,
        NomFichier = nomFichier,
        TypeContenu = typeContenu,
        TailleOctets = tailleOctets,
        CleStockage = cleStockage,
        DateDepotUtc = maintenantUtc,
    };

    public void MarquerResultatAntivirus(StatutAnalyseAntivirus resultat)
    {
        StatutAntivirus = resultat;
    }

    public bool EstScelle { get; private set; } = false;
    public DateTime? DateScellementUtc { get; private set; }
    public string? EmpreinteScellementSha256 { get; private set; }

    public void Sceller(string empreinteSha256, DateTime dateScellementUtc)
    {
        EstScelle = true;
        EmpreinteScellementSha256 = empreinteSha256;
        DateScellementUtc = dateScellementUtc;
    }

    /// <summary>SEC-12 : un document non validé (antivirus, SEC-07) n'est pas accessible.</summary>
    public bool EstAccessible => StatutAntivirus == StatutAnalyseAntivirus.Sain;
}
