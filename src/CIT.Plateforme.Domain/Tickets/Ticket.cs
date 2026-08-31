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

    /// <summary>
    /// EXTENSION FEATURE 003 (research.md D2) : numéro de BL métier fourni par le client, distinct
    /// de <see cref="Numero"/> (identifiant technique séquentiel). Nullable, uniquement renseigné
    /// pour les tickets du module Billing IMPORT. Portée d'unicité globale garantie par un index
    /// SQL Server dédié (D3), pas par ce type.
    /// </summary>
    public string? NumeroBl { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 004 (research.md D1) : numéro de booking métier fourni par le client.
    /// Nullable, uniquement renseigné pour les tickets du module Billing EXPORT.
    /// Portée d'unicité globale garantie par un index SQL Server filtré dédié (D2).
    /// </summary>
    public string? NumeroBooking { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 003 (research.md D12, IMP-15) : le client a déclaré vouloir finaliser
    /// physiquement au Billing Office à l'issue du contrôle BL. Point d'extension pour la future
    /// Feature Continuité Digital/Présentiel — aucun mécanisme de rendez-vous n'est porté ici.
    /// </summary>
    public bool DeclarationPresentielle { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 003 (research.md D13bis) : distingue un rejet Pilote Manifeste (IMP-04)
    /// d'une annulation client ordinaire (CYC-01) — les deux partagent <see cref="StatutTicket.Annule"/>
    /// pour le workflow générique et l'unicité BL (D3), mais restent distinguables en audit/
    /// historique/affichage/reporting via ce champ nullable.
    /// </summary>
    public TypeAnnulationImport? TypeAnnulationImport { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 004 (research.md D5bis) : distingue un rejet Back Office Export (EXP-02)
    /// d'une annulation client ordinaire (CYC-01). Nullable.
    /// </summary>
    public TypeAnnulationExport? TypeAnnulationExport { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 006 (Customer Complaint) : Identifiant de la catégorie métier.
    /// </summary>
    public Guid? CategorieReclamationId { get; private set; }

    /// <summary>
    /// REMÉDIATION POST-AUDIT FEATURE 006 (post-implementation-audit.md §2.3, use-cases.md C7-01) :
    /// objet court de la réclamation, jamais capturé avant cette remédiation.
    /// </summary>
    public string? Objet { get; private set; }

    /// <summary>
    /// REMÉDIATION POST-AUDIT FEATURE 006 (post-implementation-audit.md §2.3, use-cases.md C7-01) :
    /// description détaillée de la réclamation, jamais capturée avant cette remédiation.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 006 (Customer Complaint) : Réponse textuelle officielle de CIT saisie par l'agent à la clôture (REC-04, CLAR-REC-01).
    /// </summary>
    public string? ReponseFormalisee { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 006 (Customer Complaint) : Note de satisfaction (REC-06). 1 à 5.
    /// </summary>
    public int? EvaluationNote { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 006 (Customer Complaint) : Commentaire de satisfaction (REC-06).
    /// </summary>
    public string? EvaluationCommentaire { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 006 (Customer Complaint) : Date d'évaluation.
    /// </summary>
    public DateTime? EvaluationDateUtc { get; private set; }

    private Ticket() { }

    public static Ticket Creer(
        string numero,
        string module,
        string typeDemande,
        Guid fileCibleId,
        Guid dossierId,
        DateTime maintenantUtc,
        string? numeroBl = null,
        string? numeroBooking = null)
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
            NumeroBl = numeroBl,
            NumeroBooking = numeroBooking,
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

    /// <summary>
    /// EXTENSION FEATURE 003 (research.md D13bis, IMP-04) : rejet d'un ticket Billing IMPORT par le
    /// Pilote Manifeste. Réutilise le statut générique <see cref="StatutTicket.Annule"/> — jamais
    /// <see cref="StatutTicket.Cloture"/>, car <see cref="Rouvrir"/> n'autorise déjà que les tickets
    /// clôturés (CLAR-02 de Feature 001) : un ticket rejeté ne doit jamais pouvoir être rouvert, il
    /// nécessite une nouvelle soumission. Le motif (obligatoire, IMP-04) est tracé par l'appelant
    /// (Application) via le journal/historique existant, pas sur ce type.
    /// </summary>
    public void RejeterImport(DateTime maintenantUtc)
    {
        if (Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            throw new InvalidOperationException($"Le ticket {Numero} est déjà {Statut} et ne peut plus être rejeté.");
        }

        Statut = StatutTicket.Annule;
        TypeAnnulationImport = Tickets.TypeAnnulationImport.RejetManifeste;
        DateDerniereActiviteUtc = maintenantUtc;
    }

    /// <summary>
    /// EXTENSION FEATURE 004 (research.md D5bis, EXP-02) : rejet d'un ticket Billing EXPORT par le
    /// Back Office. Réutilise le statut générique <see cref="StatutTicket.Annule"/>.
    /// </summary>
    public void RejeterExport(DateTime maintenantUtc)
    {
        MarquerRejetExport(Tickets.TypeAnnulationExport.RejetBackOffice, maintenantUtc);
    }

    public void MarquerRejetExport(TypeAnnulationExport type, DateTime maintenantUtc)
    {
        if (Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            throw new InvalidOperationException($"Le ticket {Numero} est déjà {Statut} et ne peut plus être rejeté.");
        }

        Statut = StatutTicket.Annule;
        TypeAnnulationExport = type;
        DateDerniereActiviteUtc = maintenantUtc;
        DateClotureUtc = maintenantUtc;
    }

    /// <summary>
    /// EXTENSION FEATURE 003 (research.md D12, IMP-15) : le client déclare vouloir finaliser
    /// physiquement au Billing Office à l'issue du contrôle BL. N'implémente aucun mécanisme de
    /// rendez-vous (hors périmètre — future Feature Continuité Digital/Présentiel).
    /// </summary>
    public void DeclarerFinalisationPresentielle()
    {
        DeclarationPresentielle = true;
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

    /// <summary>
    /// EXTENSION FEATURE 006 : Définit la catégorie pour une réclamation.
    /// </summary>
    public void DefinirCategorieReclamation(Guid categorieId)
    {
        CategorieReclamationId = categorieId;
    }

    /// <summary>
    /// REMÉDIATION POST-AUDIT FEATURE 006 (post-implementation-audit.md §2.3) : renseigne le
    /// contenu réel de la réclamation (use-cases.md C7-01 — Objet et Description sont des entrées
    /// obligatoires du contrat).
    /// </summary>
    public void DefinirContenuReclamation(string objet, string description)
    {
        if (string.IsNullOrWhiteSpace(objet))
        {
            throw new ArgumentException("L'objet de la réclamation est obligatoire.", nameof(objet));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("La description de la réclamation est obligatoire.", nameof(description));
        }

        Objet = objet;
        Description = description;
    }

    /// <summary>
    /// EXTENSION FEATURE 006 : Clôture avec une réponse formalisée obligatoire.
    /// </summary>
    public void CloturerAvecReponseFormalisee(string reponse, MotifClotureType motif, DateTime maintenantUtc)
    {
        if (string.IsNullOrWhiteSpace(reponse))
        {
            throw new InvalidOperationException($"Le ticket {Numero} ne peut être clôturé sans réponse formalisée (REC-04).");
        }

        ReponseFormalisee = reponse;
        Cloturer(motif, maintenantUtc);
    }

    /// <summary>
    /// EXTENSION FEATURE 008 (Enrôlement Transporteurs, research.md D8, data-model.md §2.1) :
    /// renseigne les informations d'enrôlement (ENR-01) en réutilisant les colonnes génériques
    /// <see cref="Objet"/>/<see cref="Description"/> déjà existantes — aucune nouvelle propriété.
    /// </summary>
    public void DefinirInformationsEnrolement(string objet, string description)
    {
        if (string.IsNullOrWhiteSpace(objet))
        {
            throw new ArgumentException("L'objet de l'enrôlement est obligatoire (ENR-01).", nameof(objet));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("La description de l'enrôlement est obligatoire (ENR-01).", nameof(description));
        }

        Objet = objet;
        Description = description;
    }

    /// <summary>
    /// EXTENSION FEATURE 006 : Évalue un ticket de réclamation (REC-06).
    /// </summary>
    public void Evaluer(int note, string? commentaire, DateTime maintenantUtc)
    {
        if (Statut != StatutTicket.Cloture)
        {
            throw new InvalidOperationException($"Seul un ticket clôturé peut être évalué (statut actuel : {Statut}).");
        }

        if (EvaluationNote.HasValue)
        {
            throw new InvalidOperationException($"Le ticket {Numero} a déjà été évalué.");
        }

        if (note < 1 || note > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(note), "La note doit être comprise entre 1 et 5.");
        }

        EvaluationNote = note;
        EvaluationCommentaire = commentaire;
        EvaluationDateUtc = maintenantUtc;
    }
}
