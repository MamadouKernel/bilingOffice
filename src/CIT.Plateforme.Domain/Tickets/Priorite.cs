namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// PRI-01..04 : référentiel de priorité paramétrable. Un niveau est un simple libellé/rang
/// administrable (PRI-01) ; le socle ne fixe aucun jeu de niveaux par défaut (Principe VI).
/// </summary>
public sealed class NiveauPriorite
{
    public Guid Id { get; private set; }

    public string Libelle { get; private set; } = string.Empty;

    public int Rang { get; private set; }

    private NiveauPriorite() { }

    public static NiveauPriorite Definir(string libelle, int rang) => new()
    {
        Id = Guid.NewGuid(),
        Libelle = libelle,
        Rang = rang,
    };
}

/// <summary>PRI-03 : toute modification manuelle de priorité enregistre l'ancienne valeur, la
/// nouvelle, l'auteur, la date/heure et le motif.</summary>
public sealed class ModificationPriorite
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Guid? AncienNiveauId { get; private set; }

    public Guid NouveauNiveauId { get; private set; }

    public Guid AuteurId { get; private set; }

    public string Motif { get; private set; } = string.Empty;

    public DateTime HorodatageUtc { get; private set; }

    private ModificationPriorite() { }

    public static ModificationPriorite Creer(Guid ticketId, Guid? ancienNiveauId, Guid nouveauNiveauId, Guid auteurId, string motif, DateTime maintenantUtc)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif de modification de priorité est obligatoire (PRI-03).", nameof(motif));
        }

        return new ModificationPriorite
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AncienNiveauId = ancienNiveauId,
            NouveauNiveauId = nouveauNiveauId,
            AuteurId = auteurId,
            Motif = motif,
            HorodatageUtc = maintenantUtc,
        };
    }
}
