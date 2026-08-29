namespace CIT.Plateforme.Domain.Tickets;

public enum EtatDossier
{
    Actif,
    Termine,
}

/// <summary>
/// CLAR-01 : le dossier ne porte aucun état propre — son état est entièrement dérivé de ses
/// tickets. « Terminé » signifie que tous les tickets du dossier sont clôturés ou annulés ; ce
/// n'est jamais une action explicite sur le dossier lui-même (aucune méthode « Clôturer » sur
/// <see cref="Dossier"/>). Un dossier « Terminé » redevient « Actif » dès la réouverture de l'un
/// de ses tickets.
/// </summary>
public static class EtatDossierDerive
{
    public static EtatDossier Calculer(IReadOnlyCollection<Ticket> ticketsDuDossier)
    {
        if (ticketsDuDossier.Count == 0)
        {
            return EtatDossier.Actif;
        }

        var tousTerminees = ticketsDuDossier.All(t => t.Statut is StatutTicket.Cloture or StatutTicket.Annule);
        return tousTerminees ? EtatDossier.Termine : EtatDossier.Actif;
    }
}
