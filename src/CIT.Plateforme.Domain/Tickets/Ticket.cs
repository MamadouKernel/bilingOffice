namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// Unité de travail centrale du socle (principe P1 du CDC). Porte un numéro unique jamais
/// réutilisé (SOC-01, SOC-09), un statut, une file courante, un opérateur assigné éventuel, et un
/// rattachement optionnel à un dossier (SOC-11). Le modèle conceptuel est celui de
/// `data-model.md` ; ce type ne préjuge d'aucun schéma physique (Principe VIII).
/// </summary>
public sealed class Ticket
{
    public Guid Id { get; private set; }

    /// <summary>Numéro unique communiqué au client (SOC-01), jamais réutilisé (SOC-09).</summary>
    public string Numero { get; private set; } = string.Empty;

    /// <summary>Module d'origine (Billing Office, Customer Complaint, Enrôlement…) — donnée
    /// fournie par le module appelant, hors périmètre du socle (voir spec.md § Périmètre).</summary>
    public string Module { get; private set; } = string.Empty;

    public string TypeDemande { get; private set; } = string.Empty;

    /// <summary>Dossier de rattachement — toujours renseigné (SOC-11, contrat C-08 : un dossier
    /// est créé au premier ticket d'un sujet et réutilisé pour les suivants).</summary>
    public Guid DossierId { get; private set; }

    public Guid FileCouranteId { get; private set; }

    public Guid? OperateurAssigneId { get; private set; }

    public StatutTicket Statut { get; private set; }

    public MotifClotureType? MotifCloture { get; private set; }

    public Guid? PrioriteId { get; private set; }

    public DateTime DateCreationUtc { get; private set; }

    public DateTime DateArriveeEnFileUtc { get; private set; }

    public DateTime? DatePriseEnChargeUtc { get; private set; }

    public DateTime DateDerniereActiviteUtc { get; private set; }

    public DateTime? DateDebutAttenteClienteUtc { get; private set; }

    /// <summary>TPS-02 : cumul du temps déjà passé en attente client sur des cycles antérieurs
    /// (hors la période d'attente en cours, le cas échéant) — TPS-04 : jamais imputé à la
    /// performance opérateur/file.</summary>
    public TimeSpan DureeAttenteClienteCumulee { get; private set; } = TimeSpan.Zero;

    public DateTime? DateClotureUtc { get; private set; }

    /// <summary>Nombre de relances d'absence de réponse déjà émises (CYC-04, paramètre).</summary>
    public int NombreRelancesEnvoyees { get; private set; }

    /// <summary>CYC-05 : jamais de suggestion de clôture après un état engageant (paiement
    /// confirmé, document officiel produit). Le socle ne définit pas ce qui est « engageant » —
    /// c'est au module métier appelant (Billing Office…, hors périmètre) de le signaler via
    /// <see cref="MarquerEtatEngageant"/>.</summary>
    public bool EstEnEtatEngageant { get; private set; }

    /// <summary>Jeton de concurrence optimiste EF Core (D3/D6) — garantit qu'une seule prise en
    /// charge concurrente réussit sur un même ticket (règle métier critique).</summary>
    public byte[] VersionConcurrence { get; private set; } = [];

    private Ticket() { }

    public static Ticket Creer(string numero, string module, string typeDemande, Guid fileCibleId, Guid dossierId, DateTime maintenantUtc)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new ArgumentException("Le numéro de ticket est obligatoire (SOC-01).", nameof(numero));
        }

        return new Ticket
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            Module = module,
            TypeDemande = typeDemande,
            DossierId = dossierId,
            FileCouranteId = fileCibleId,
            Statut = StatutTicket.EnAttenteDePriseEnCharge,
            DateCreationUtc = maintenantUtc,
            DateArriveeEnFileUtc = maintenantUtc,
            DateDerniereActiviteUtc = maintenantUtc,
        };
    }

    /// <summary>PEC-01/PEC-02/PEC-03/SOC-12 : un opérateur habilité accepte le ticket. Ne
    /// s'applique qu'à un ticket non affecté (PEC-01) ; la vérification de concurrence effective
    /// est assurée par le jeton de concurrence EF Core au niveau Infrastructure (D6).</summary>
    public void PrendreEnCharge(Guid operateurId, DateTime maintenantUtc)
    {
        if (Statut != StatutTicket.EnAttenteDePriseEnCharge)
        {
            throw new InvalidOperationException(
                $"Le ticket {Numero} n'est pas en attente de prise en charge (statut actuel : {Statut}).");
        }

        OperateurAssigneId = operateurId;
        Statut = StatutTicket.EnCours;
        DatePriseEnChargeUtc = maintenantUtc;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    /// <summary>CLAR-03 : après un transfert, le ticket arrive non affecté « en attente de prise
    /// en charge » sauf destinataire explicitement désigné (TRF-03).</summary>
    public void Transferer(Guid fileCibleId, Guid? destinataireId, DateTime maintenantUtc)
    {
        FileCouranteId = fileCibleId;
        DateArriveeEnFileUtc = maintenantUtc;
        DateDerniereActiviteUtc = maintenantUtc;

        if (destinataireId is Guid destinataire)
        {
            OperateurAssigneId = destinataire;
            Statut = StatutTicket.EnCours;
            DatePriseEnChargeUtc = maintenantUtc;
        }
        else
        {
            OperateurAssigneId = null;
            Statut = StatutTicket.EnAttenteDePriseEnCharge;
            DatePriseEnChargeUtc = null;
        }
    }

    /// <summary>TRF-04 : réaffectation à un autre opérateur habilité de la même file, sous
    /// réserve des droits configurés (non réservé au superviseur).</summary>
    public void Reaffecter(Guid nouvelOperateurId, DateTime maintenantUtc)
    {
        if (Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            throw new InvalidOperationException($"Le ticket {Numero} est {Statut} et ne peut plus être réaffecté.");
        }

        OperateurAssigneId = nouvelOperateurId;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    /// <summary>Affectation manuelle superviseur (AFF-02) ou directe (PEC-05) — même effet que la
    /// prise en charge côté statut, initiée par un tiers habilité plutôt que l'opérateur lui-même.</summary>
    public void AffecterManuellement(Guid operateurId, DateTime maintenantUtc)
    {
        if (Statut != StatutTicket.EnAttenteDePriseEnCharge)
        {
            throw new InvalidOperationException(
                $"Le ticket {Numero} n'est pas en attente de prise en charge (statut actuel : {Statut}).");
        }

        OperateurAssigneId = operateurId;
        Statut = StatutTicket.EnCours;
        DatePriseEnChargeUtc = maintenantUtc;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    /// <summary>TPS-02/TPS-04 : mise en attente d'une action client — le temps interne CIT est
    /// suspendu pendant cet état.</summary>
    public void MettreEnAttenteClient(DateTime maintenantUtc)
    {
        if (Statut != StatutTicket.EnCours)
        {
            throw new InvalidOperationException($"Le ticket {Numero} n'est pas en cours de traitement.");
        }

        Statut = StatutTicket.EnAttenteClient;
        DateDebutAttenteClienteUtc = maintenantUtc;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    /// <summary>Reprise du traitement après réponse du client.</summary>
    public void RependreApresReponseClient(DateTime maintenantUtc)
    {
        if (Statut != StatutTicket.EnAttenteClient)
        {
            throw new InvalidOperationException($"Le ticket {Numero} n'est pas en attente client.");
        }

        if (DateDebutAttenteClienteUtc is DateTime debut)
        {
            DureeAttenteClienteCumulee += maintenantUtc - debut;
        }

        Statut = StatutTicket.EnCours;
        DateDebutAttenteClienteUtc = null;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    /// <summary>CYC-01 : annulation libre par le client tant qu'aucun état engageant n'a été
    /// atteint. Le contrôle de l'étape bloquante paramétrable est effectué par l'appelant
    /// (Application), ce type ne connaît que la transition elle-même.</summary>
    public void Annuler(DateTime maintenantUtc)
    {
        if (Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            throw new InvalidOperationException($"Le ticket {Numero} est déjà {Statut} et ne peut plus être annulé.");
        }

        Statut = StatutTicket.Annule;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    /// <summary>SOC-09 : la clôture déclenche un archivage automatique (représenté ici par
    /// DateClotureUtc — le ticket reste consultable en permanence, jamais supprimé).</summary>
    public void Cloturer(MotifClotureType motif, DateTime maintenantUtc)
    {
        if (Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            throw new InvalidOperationException($"Le ticket {Numero} est déjà {Statut}.");
        }

        Statut = StatutTicket.Cloture;
        MotifCloture = motif;
        DateClotureUtc = maintenantUtc;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    /// <summary>CLAR-02 : la réouverture ne s'applique qu'aux tickets clôturés (normale ou pour
    /// absence de réponse) — jamais à un ticket annulé. Le ticket redevient actif et repart en
    /// attente de prise en charge.</summary>
    public void Rouvrir(DateTime maintenantUtc)
    {
        if (Statut != StatutTicket.Cloture)
        {
            throw new InvalidOperationException(
                $"Seul un ticket clôturé peut être rouvert (CLAR-02) — statut actuel : {Statut}.");
        }

        Statut = StatutTicket.EnAttenteDePriseEnCharge;
        MotifCloture = null;
        DateClotureUtc = null;
        OperateurAssigneId = null;
        DateArriveeEnFileUtc = maintenantUtc;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    public void DefinirPriorite(Guid prioriteId)
    {
        PrioriteId = prioriteId;
    }

    /// <summary>Signale que le ticket a atteint un état engageant (paiement confirmé, document
    /// officiel produit…) — décision du module appelant, jamais du socle lui-même (CYC-05).</summary>
    public void MarquerEtatEngageant()
    {
        EstEnEtatEngageant = true;
    }

    /// <summary>CYC-04 : enregistre l'émission d'une relance automatique.</summary>
    public void EnregistrerRelance(DateTime maintenantUtc)
    {
        if (Statut != StatutTicket.EnAttenteClient)
        {
            throw new InvalidOperationException($"Le ticket {Numero} n'est pas en attente client.");
        }

        NombreRelancesEnvoyees++;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    /// <summary>CYC-04/CYC-05 : une suggestion de clôture pour absence de réponse peut être
    /// proposée uniquement si le nombre maximal de relances (paramètre) est atteint et que le
    /// ticket n'a jamais atteint un état engageant. La clôture elle-même reste une décision
    /// manuelle d'un agent habilité — cette méthode ne fait que qualifier l'éligibilité.</summary>
    public bool EstEligibleASuggestionDeCloture(int nombreRelancesMax) =>
        Statut == StatutTicket.EnAttenteClient && !EstEnEtatEngageant && NombreRelancesEnvoyees >= nombreRelancesMax;
}
