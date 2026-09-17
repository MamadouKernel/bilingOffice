namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// FEATURE 003 (IMP-01) : levée lorsqu'une soumission Billing IMPORT dépasse le nombre maximal de
/// BL autorisé par demande (« jusqu'à 5 BL par demande », CDC IMP-01). Le comptage porte sur le
/// nombre de BL soumis par l'appelant, avant dédoublonnage — une demande de 6 BL est refusée même
/// si certains sont des doublons.
/// </summary>
public sealed class LimiteBlDepasseeException(int limite, int nombreSoumis)
    : Exception($"Une demande ne peut porter que sur {limite} BL au maximum (IMP-01) — {nombreSoumis} BL soumis.")
{
    public int Limite { get; } = limite;

    public int NombreSoumis { get; } = nombreSoumis;
}
