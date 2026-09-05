namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// CYC-01 / RG-TCK-07 — « au-delà, l'annulation directe est fermée selon un <b>seuil paramétrable
/// par workflow</b> ».
///
/// CE SEUIL N'EXISTAIT PAS. <c>AnnulerTicketCommand</c> recevait un booléen
/// <c>etapeBloquanteAtteinte</c> que l'appelant devait calculer lui-même, et le seul appelant —
/// l'écran de détail du ticket — passait <c>false</c> en dur. La règle n'était donc jamais
/// appliquée, et rien dans la base ne permettait de la paramétrer.
///
/// UNE LIGNE = UNE ÉTAPE À PARTIR DE LAQUELLE L'ANNULATION LIBRE SE FERME, pour un workflow donné.
/// L'étape est désignée par la <b>file</b> : c'est ce que le CDC appelle une étape, et c'est déjà
/// l'unité du moteur de workflow, de la matrice de transitions et des SLA. Le seuil est atteint dès
/// que le ticket a <b>traversé</b> cette file — pas seulement s'il s'y trouve encore : un ticket
/// passé par la Facturation puis reparti ailleurs a bel et bien engagé CIT.
///
/// <see cref="TypeDemande"/> nul vise tous les types du module : un seuil se déclare le plus
/// souvent au niveau du module, et se raffine seulement s'il le faut.
/// </summary>
public sealed class EtapeBloquanteAnnulation
{
    public Guid Id { get; private set; }

    /// <summary>Module concerné (« BillingImport », « BillingExport »…). Obligatoire.</summary>
    public string Module { get; private set; } = string.Empty;

    /// <summary>Type de demande visé, ou <c>null</c> pour tous les types du module.</summary>
    public string? TypeDemande { get; private set; }

    /// <summary>File dont la traversée ferme l'annulation libre.</summary>
    public Guid FileId { get; private set; }

    /// <summary>Motif du caractère bloquant, restitué au client lorsqu'on lui refuse l'annulation
    /// directe — un refus sans explication est un refus incompréhensible.</summary>
    public string Motif { get; private set; } = string.Empty;

    public bool EstActif { get; private set; } = true;

    private EtapeBloquanteAnnulation() { }

    public static EtapeBloquanteAnnulation Definir(string module, string? typeDemande, Guid fileId, string motif)
    {
        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException("Le module est obligatoire.", nameof(module));
        if (fileId == Guid.Empty)
            throw new ArgumentException("La file bloquante est obligatoire.", nameof(fileId));
        if (string.IsNullOrWhiteSpace(motif))
            throw new ArgumentException("Le motif du caractère bloquant est obligatoire (CYC-03).", nameof(motif));

        return new EtapeBloquanteAnnulation
        {
            Id = Guid.NewGuid(),
            Module = module.Trim(),
            TypeDemande = string.IsNullOrWhiteSpace(typeDemande) ? null : typeDemande.Trim(),
            FileId = fileId,
            Motif = motif.Trim(),
        };
    }

    /// <summary>Ce seuil concerne-t-il le workflow d'un ticket donné ?</summary>
    public bool SApplique(string module, string typeDemande) =>
        EstActif
        && string.Equals(Module, module, StringComparison.OrdinalIgnoreCase)
        && (TypeDemande is null || string.Equals(TypeDemande, typeDemande, StringComparison.OrdinalIgnoreCase));

    public void Modifier(string motif)
    {
        if (string.IsNullOrWhiteSpace(motif))
            throw new ArgumentException("Le motif du caractère bloquant est obligatoire.", nameof(motif));

        Motif = motif.Trim();
    }

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;
}
