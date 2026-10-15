namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// EXTENSION FEATURE 010 (Actualisation BADT) : la demande d'actualisation d'un BADT est un ticket
/// du socle (un seul objet à suivre côté client, comme un fil d'e-mails = un cas). Les conteneurs
/// concernés sont portés par des <see cref="CIT.Plateforme.Domain.ActualisationBadt.LigneActualisationBadt"/>
/// rattachées au ticket. Le ticket réutilise le workflow générique (file, prise en charge, SLA,
/// clôture) ; la décision est prise au grain du conteneur (ligne), pas via un nouveau StatutTicket.
/// </summary>
public sealed partial class Ticket
{
    /// <summary>Discriminant de module pour les demandes d'actualisation BADT.</summary>
    public const string ModuleActualisationBadt = "ActualisationBadt";

    /// <summary>
    /// Crée une demande d'actualisation BADT. <paramref name="description"/> est le récit libre saisi
    /// par le client au formulaire ; les BL/conteneurs et leurs décisions vivent dans les lignes.
    /// </summary>
    public static Ticket CreerDemandeActualisationBadt(
        string numero,
        Guid fileCibleId,
        Guid dossierId,
        string description,
        DateTime maintenantUtc)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new ArgumentException("Le numéro de ticket est obligatoire (SOC-01).", nameof(numero));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("La description de la demande est obligatoire.", nameof(description));
        }

        return new Ticket
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            Module = ModuleActualisationBadt,
            TypeDemande = ModuleActualisationBadt,
            DossierId = dossierId,
            FileCouranteId = fileCibleId,
            Statut = StatutTicket.EnAttenteDePriseEnCharge,
            Objet = "Demande d'actualisation BADT",
            Description = description.Trim(),
            DateCreationUtc = maintenantUtc,
            DateArriveeEnFileUtc = maintenantUtc,
            DateDerniereActiviteUtc = maintenantUtc,
        };
    }
}
