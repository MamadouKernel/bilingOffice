namespace CIT.Plateforme.Domain.Files;

/// <summary>
/// Une transition autorisée entre deux files (CLAR-04) : destination, rôles habilités à
/// initier le transfert (permission requise, vérifiée par l'appelant), motifs autorisés. Le
/// contenu réel (quelles files, quels motifs) reste un paramètre métier non fixé ici (PC-07).
/// </summary>
public sealed class TransitionAutorisee
{
    public Guid Id { get; private set; }

    public Guid FileSourceId { get; private set; }

    public Guid FileCibleId { get; private set; }

    private readonly List<string> motifsAutorises = [];

    public IReadOnlyList<string> MotifsAutorises => motifsAutorises;

    private TransitionAutorisee() { }

    public static TransitionAutorisee Definir(Guid fileSourceId, Guid fileCibleId, IEnumerable<string> motifsAutorises)
    {
        var transition = new TransitionAutorisee
        {
            Id = Guid.NewGuid(),
            FileSourceId = fileSourceId,
            FileCibleId = fileCibleId,
        };
        transition.motifsAutorises.AddRange(motifsAutorises);
        return transition;
    }

    public bool AutoriseLeMotif(string motif) => motifsAutorises.Count == 0 || motifsAutorises.Contains(motif);

    /// <summary>
    /// REMÉDIATION AUDIT (post-implementation-audit.md §3.D/§5, contrat C-02 « créer/modifier ») :
    /// ajoute un motif autorisé à une transition existante, sans la recréer. Mutation en place de
    /// la collection — nécessite le <c>ValueComparer</c> explicite configuré dans
    /// <c>PlateformeDbContext</c> pour être correctement détectée et persistée par EF Core (avant
    /// correction, ce type de mutation n'aurait pas été détectée par le suivi de changement).
    /// </summary>
    public void AjouterMotif(string motif)
    {
        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new ArgumentException("Le motif ne peut pas être vide.", nameof(motif));
        }

        if (!motifsAutorises.Contains(motif))
        {
            motifsAutorises.Add(motif);
        }
    }

    /// <summary>
    /// Remplace la liste des motifs autorisés. Même raison que pour le périmètre de supervision :
    /// <c>ObtenirAsync(source, cible)</c> lit par <c>SingleOrDefaultAsync</c>, donc redéfinir une
    /// transition en ajoutant une seconde ligne rendrait ce couple de files illisible.
    /// </summary>
    public void RedefinirMotifs(IEnumerable<string> nouveauxMotifs)
    {
        motifsAutorises.Clear();
        foreach (var motif in nouveauxMotifs)
        {
            if (!string.IsNullOrWhiteSpace(motif) && !motifsAutorises.Contains(motif))
            {
                motifsAutorises.Add(motif);
            }
        }
    }
}
