namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// TPS-01 / TPS-05 — un passage d'un ticket dans une file de traitement, avec ses bornes exactes.
///
/// DÉFAUT CORRIGÉ : les horodatages de file étaient portés par <c>Ticket</c> en colonnes uniques
/// (<c>DateArriveeEnFileUtc</c>, <c>DatePriseEnChargeUtc</c>). <c>Ticket.Transferer</c> les
/// réinitialise — au premier transfert, les temps du passage précédent étaient donc **écrasés**.
/// Un ticket ayant traversé Contrôle BL, Facturation puis Contrôle Documentaire ne conservait que
/// les temps de la dernière file.
///
/// Deux exigences en dépendaient :
/// <list type="bullet">
///   <item><b>TPS-01</b> — « pour <b>chaque étape/file traversée</b>, la plateforme mesure le temps
///   d'attente et le temps de traitement » ;</item>
///   <item><b>TPS-05</b> — « l'historique restitue la <b>chronologie détaillée par étape</b> ».</item>
/// </list>
///
/// Une occurrence est ouverte à la création du ticket puis à chaque transfert ; la précédente est
/// fermée au même instant. Les colonnes du ticket restent la vue « étape courante » ; cette entité
/// porte l'historique complet.
///
/// Suit le même motif que <see cref="PeriodeAttenteClient"/> : entité persistée indépendamment,
/// jamais une collection de l'agrégat.
/// </summary>
public sealed class PassageEnFile
{
    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Guid FileId { get; private set; }

    /// <summary>Ordre du passage dans le parcours, à partir de 1. Rend la chronologie lisible même
    /// si deux passages partagent le même horodatage.</summary>
    public int Rang { get; private set; }

    public DateTime DateArriveeUtc { get; private set; }

    public DateTime? DatePriseEnChargeUtc { get; private set; }

    public Guid? OperateurId { get; private set; }

    /// <summary>Sortie de l'étape : transfert vers une autre file, ou clôture du ticket.</summary>
    public DateTime? DateSortieUtc { get; private set; }

    private PassageEnFile() { }

    public static PassageEnFile Ouvrir(Guid ticketId, Guid fileId, int rang, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        FileId = fileId,
        Rang = rang,
        DateArriveeUtc = maintenantUtc,
    };

    /// <summary>
    /// PEC-03 — enregistre la prise en charge. Idempotent : une seconde prise en charge du même
    /// passage ne réécrit pas l'horodatage initial, qui est celui que TPS-01 mesure.
    /// </summary>
    public void PrendreEnCharge(Guid operateurId, DateTime maintenantUtc)
    {
        if (DatePriseEnChargeUtc is not null)
        {
            return;
        }

        DatePriseEnChargeUtc = maintenantUtc;
        OperateurId = operateurId;
    }

    /// <summary>Ferme le passage. Idempotent, pour la même raison.</summary>
    public void Fermer(DateTime maintenantUtc)
    {
        DateSortieUtc ??= maintenantUtc;
    }

    /// <summary>
    /// TPS-01 — temps d'attente : arrivée en file → prise en charge. Si le passage s'est terminé
    /// sans prise en charge (transfert d'un ticket jamais accepté), l'attente court jusqu'à la
    /// sortie.
    /// </summary>
    public TimeSpan TempsAttente(DateTime instantMesureUtc)
    {
        var borne = DatePriseEnChargeUtc ?? DateSortieUtc ?? instantMesureUtc;
        var duree = borne - DateArriveeUtc;
        return duree < TimeSpan.Zero ? TimeSpan.Zero : duree;
    }

    /// <summary>
    /// TPS-01 — temps de traitement : prise en charge → sortie de l'étape. Nul tant que le ticket
    /// n'a pas été pris en charge.
    /// </summary>
    public TimeSpan TempsTraitement(DateTime instantMesureUtc)
    {
        if (DatePriseEnChargeUtc is not DateTime priseEnCharge)
        {
            return TimeSpan.Zero;
        }

        var duree = (DateSortieUtc ?? instantMesureUtc) - priseEnCharge;
        return duree < TimeSpan.Zero ? TimeSpan.Zero : duree;
    }

    public bool EstEnCours => DateSortieUtc is null;
}
