namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// Unité de travail centrale du socle (principe P1 du CDC). Porte un numéro unique jamais
/// réutilisé (SOC-01, SOC-09), un statut, une file courante, un opérateur assigné éventuel, et un
/// rattachement optionnel à un dossier (SOC-11). Le modèle conceptuel est celui de
/// `data-model.md` ; ce type ne préjuge d'aucun schéma physique (Principe VIII).
/// </summary>
public sealed partial class Ticket
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

    /// <summary>
    /// RG-EXP-03 — date d'émission du Draft IN sur le parcours EXPORT. <c>null</c> tant qu'il n'a
    /// pas été émis, et l'édition de la facture est alors refusée. Toujours <c>null</c> à l'import,
    /// qui ne connaît pas cette étape.
    /// </summary>
    public DateTime? DraftInExportEmisLeUtc { get; private set; }

    /// <summary>RG-EXP-03 — prérequis à l'édition de la facture export.</summary>
    public bool DraftInExportEmis => DraftInExportEmisLeUtc.HasValue;
    public bool EstSousGelLegal { get; private set; } = false;
    public DateTime? DateArchivageUtc { get; private set; }
    public bool EstAnonymise { get; private set; } = false;

    /// <summary>
    /// RG-TCK-06 — le ticket est ARCHIVÉ. Distinct de la clôture, qui est une décision métier :
    /// l'archivage en est la conséquence automatique, et il n'entraîne ni suppression, ni
    /// déplacement, ni perte de lisibilité. Un ticket archivé reste consultable avec son historique
    /// complet, ses documents et ses échanges.
    /// </summary>
    public bool EstArchive => DateArchivageUtc.HasValue;

    public void ActiverGelLegal() => EstSousGelLegal = true;
    public void DesactiverGelLegal() => EstSousGelLegal = false;

    /// <summary>
    /// RG-TCK-06 (remédiation post-audit final) — archivage automatique.
    ///
    /// L'audit final a constaté que l'archivage n'était « représenté » que par la présence de
    /// <see cref="DateClotureUtc"/> : rien ne le matérialisait, et <see cref="DateArchivageUtc"/>
    /// n'était alimentée que par la purge de rétention — ce qui confondait <b>archiver</b> et
    /// <b>anonymiser</b>, deux opérations opposées quant à la lisibilité des données.
    ///
    /// Idempotent : archiver un ticket déjà archivé ne déplace pas sa date. La date d'archivage est
    /// une donnée d'historique, elle ne se réécrit pas.
    /// </summary>
    public void Archiver(DateTime maintenantUtc)
    {
        DateArchivageUtc ??= maintenantUtc;
    }

    /// <summary>Valeur de remplacement des champs texte à caractère personnel une fois le ticket
    /// anonymisé (SEC-06, droit à l'effacement) — jamais une chaîne vide, pour rester distinguable
    /// d'un champ simplement jamais renseigné.</summary>
    public const string ValeurAnonymisee = "[Anonymisé]";

    /// <summary>
    /// Rétention (SEC-06) : le contenu est anonymisé. L'anonymisation n'EST PAS l'archivage — elle
    /// intervient bien après, et retire de la lisibilité là où l'archivage la préserve. La date
    /// d'archivage n'est donc pas écrasée ici ; elle est seulement renseignée si le ticket n'avait
    /// jamais été archivé (cas d'un ticket purgé sans être passé par la clôture).
    ///
    /// REMÉDIATION SEC-06 (droit à l'effacement) : marquer <see cref="EstAnonymise"/> sans jamais
    /// vider aucun champ ne matérialisait rien — le drapeau existait, mais aucune donnée
    /// personnelle n'était réellement effacée. Les champs texte libres susceptibles de porter des
    /// données à caractère personnel (objet/description de réclamation, réponse formalisée,
    /// commentaire d'évaluation, motif de rejet) sont désormais remplacés par
    /// <see cref="ValeurAnonymisee"/> quand ils sont renseignés. Idempotent : ré-appeler sur un
    /// ticket déjà anonymisé ne fait rien de plus.
    /// </summary>
    public void MarquerAnonymise(DateTime dateArchivageUtc)
    {
        if (EstAnonymise)
        {
            return;
        }

        EstAnonymise = true;
        DateArchivageUtc ??= dateArchivageUtc;

        if (Objet is not null)
        {
            Objet = ValeurAnonymisee;
        }

        if (Description is not null)
        {
            Description = ValeurAnonymisee;
        }

        if (ReponseFormalisee is not null)
        {
            ReponseFormalisee = ValeurAnonymisee;
        }

        if (EvaluationCommentaire is not null)
        {
            EvaluationCommentaire = ValeurAnonymisee;
        }

        if (MotifRejetAvoirExo is not null)
        {
            MotifRejetAvoirExo = ValeurAnonymisee;
        }
    }

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

    /// <summary>Cas particuliers signalés à la demande d'import (dangereux, sous palan, véhicule usagé).</summary>
    public CasParticulierImport CasParticuliers { get; private set; }

    /// <summary>
    /// Date d'enlèvement souhaitée par le client (import). Les frais de magasinage courent jusqu'à
    /// la sortie du conteneur : la proforma puis la facture sont calculées jusqu'à cette date, que
    /// le Pilote Facturation confirme et que le client peut modifier en demandant une correction.
    /// </summary>
    public DateOnly? DateEnlevementSouhaitee { get; private set; }

    /// <summary>Proforma déposée dans le ticket et réponse du client (voir <see cref="Tickets.EtatProforma"/>).</summary>
    public EtatProforma EtatProforma { get; private set; }

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

    /// <summary>
    /// EXTENSION FEATURE 007 (Avoir & Exonération) : Décision finale enregistrée (AVE-03).
    /// </summary>
    public CIT.Plateforme.Domain.AvoirExoneration.DecisionAvoirExo? DecisionAvoirExo { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 007 (Avoir & Exonération) : Motif de rejet obligatoire en cas de refus (AVE-03).
    /// </summary>
    public string? MotifRejetAvoirExo { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 007 (Avoir & Exonération) : Montant réclamé / contesté en XOF (AVE-01).
    /// </summary>
    public decimal? MontantConteste { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 007 (Avoir & Exonération) : Montant final accordé en XOF (AVE-03).
    /// </summary>
    public decimal? MontantAccorde { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 007 (Avoir & Exonération) : Date UTC de prise de décision (AVE-03).
    /// </summary>
    public DateTime? DateDecisionAvoirExoUtc { get; private set; }

    /// <summary>
    /// EXTENSION FEATURE 007 (Avoir & Exonération) : Identifiant de l'agent ayant enregistré la décision (AVE-03).
    /// </summary>
    public Guid? DecideurAvoirExoId { get; private set; }

    /// <summary>
    /// EXTENSION Support IT (incident technique) : catégorie technique du signalement, routant vers
    /// sa file cible (même principe que <see cref="CategorieReclamationId"/>).
    /// </summary>
    public Guid? CategorieIncidentId { get; private set; }

    /// <summary>
    /// EXTENSION Support IT (incident technique) : gravité déclarée (Mineure / Majeure / Critique).
    /// Caractéristique du signalement, orientant priorité et SLA — pas un état du workflow.
    /// </summary>
    public CIT.Plateforme.Domain.Incidents.GraviteIncident? Gravite { get; private set; }

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

    /// <summary>
    /// ALE-03 — action « réaffectation » d'une escalade : le ticket quitte l'opérateur qui le
    /// retient et retourne à la distribution. Aucun destinataire n'est désigné ici — en choisir un
    /// supposerait une règle d'affectation que l'escalade n'a pas à trancher (CLAR-03).
    /// </summary>
    public void RemettreEnDistribution(DateTime maintenantUtc)
    {
        if (Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            throw new InvalidOperationException($"Le ticket {Numero} est {Statut} et ne peut plus être remis en distribution.");
        }

        OperateurAssigneId = null;
        Statut = StatutTicket.EnAttenteDePriseEnCharge;
        DatePriseEnChargeUtc = null;
        DateDerniereActiviteUtc = maintenantUtc;
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

    /// <summary>Cas particuliers déclarés par le client à la soumission de la demande d'import.</summary>
    public void DeclarerCasParticuliers(CasParticulierImport cas)
    {
        CasParticuliers = cas;
    }

    public void DefinirDateEnlevementSouhaitee(DateOnly? date)
    {
        DateEnlevementSouhaitee = date;
    }

    /// <summary>
    /// Le client change sa date d'enlèvement avant la facture. Le magasinage dépend de cette date :
    /// une proforma déjà envoyée ou validée ne vaut plus et repasse en « correction demandée ».
    /// Retourne vrai si la proforma doit être refaite.
    /// </summary>
    public bool ModifierDateEnlevement(DateOnly nouvelleDate, DateTime maintenantUtc)
    {
        DateEnlevementSouhaitee = nouvelleDate;
        DateDerniereActiviteUtc = maintenantUtc;
        switch (EtatProforma)
        {
            case EtatProforma.EnAttenteValidation:
                DemanderCorrectionProforma(maintenantUtc);
                return true;
            case EtatProforma.Validee:
                EtatProforma = EtatProforma.CorrectionDemandee;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// La proforma est déposée : le ticket attend la réponse du client, temps interne suspendu
    /// (TPS-02). Une nouvelle proforma (après correction) remplace la précédente.
    /// </summary>
    public void EmettreProforma(DateTime maintenantUtc)
    {
        if (EtatProforma == EtatProforma.Validee)
        {
            throw new InvalidOperationException($"La proforma du ticket {Numero} est déjà validée par le client.");
        }

        MettreEnAttenteClient(maintenantUtc);
        EtatProforma = EtatProforma.EnAttenteValidation;
    }

    public void ValiderProforma(DateTime maintenantUtc)
    {
        ExigerProformaEnAttente();
        RependreApresReponseClient(maintenantUtc);
        EtatProforma = EtatProforma.Validee;
    }

    public void DemanderCorrectionProforma(DateTime maintenantUtc)
    {
        ExigerProformaEnAttente();
        RependreApresReponseClient(maintenantUtc);
        EtatProforma = EtatProforma.CorrectionDemandee;
    }

    private void ExigerProformaEnAttente()
    {
        if (EtatProforma != EtatProforma.EnAttenteValidation)
        {
            throw new InvalidOperationException($"Aucune proforma n'attend de réponse sur le ticket {Numero}.");
        }
    }

    /// <summary>
    /// SOC-09 / RG-TCK-06 : la clôture déclenche un archivage automatique.
    ///
    /// Les deux notions restent DISTINCTES — la clôture est la décision, l'archivage sa conséquence
    /// — mais elles sont désormais liées ici, et non plus seulement affirmées en commentaire.
    /// Le ticket n'est ni supprimé ni déplacé : il reste consultable avec tout son contenu.
    /// </summary>
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
        Archiver(maintenantUtc);
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

        // RG-TCK-06 / RG-TCK-12 : l'archivage suit la clôture. Un ticket rouvert redevient actif —
        // le laisser marqué archivé le décrirait faussement. L'historique de la clôture et de la
        // réouverture, lui, est conservé : c'est le journal qui porte la mémoire, pas ce drapeau.
        DateArchivageUtc = null;
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

    /// <summary>
    /// RG-EXP-03 (remédiation post-audit final) — le Pilote Facturation a émis le <b>Draft IN</b>.
    ///
    /// C'est l'adaptation que le CDC exige à l'export et qui manquait entièrement : « le Pilote
    /// Facturation crée un Draft IN <b>avant</b> l'édition de la facture ». Le brouillon n'est pas
    /// un document officiel — il ne rend donc PAS le ticket engageant, et ne change pas d'étape :
    /// il conditionne seulement l'édition de la facture, qui suit dans la même file.
    /// </summary>
    public void EmettreDraftInExport(DateTime maintenantUtc)
    {
        DraftInExportEmisLeUtc = maintenantUtc;
        DateDerniereActiviteUtc = maintenantUtc;
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

    /// <summary>CYC-04 : la suggestion de clôture compte comme une activité — elle n'est pas
    /// réémise avant le délai suivant.</summary>
    public void EnregistrerSuggestionCloture(DateTime maintenantUtc) => DateDerniereActiviteUtc = maintenantUtc;

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
    /// EXTENSION Support IT : catégorie technique + gravité d'un incident.
    /// </summary>
    public void DefinirIncident(Guid categorieIncidentId, CIT.Plateforme.Domain.Incidents.GraviteIncident gravite)
    {
        CategorieIncidentId = categorieIncidentId;
        Gravite = gravite;
    }

    /// <summary>
    /// EXTENSION Support IT : contenu réel d'un incident (objet court + description). Réutilise
    /// <see cref="Objet"/>/<see cref="Description"/>, comme la réclamation et l'enrôlement.
    /// </summary>
    public void DefinirContenuIncident(string objet, string description)
    {
        if (string.IsNullOrWhiteSpace(objet))
        {
            throw new ArgumentException("L'objet de l'incident est obligatoire.", nameof(objet));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("La description de l'incident est obligatoire.", nameof(description));
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

    /// <summary>
    /// EXTENSION FEATURE 007 (AVE-01) : Crée une demande d'Avoir ou d'Exonération.
    /// </summary>
    public static Ticket CreerDemandeAvoirExo(
        string numero,
        string typeDemande,
        Guid fileCibleId,
        Guid dossierId,
        string objet,
        string description,
        decimal? montantConteste,
        DateTime maintenantUtc)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new ArgumentException("Le numéro de ticket est obligatoire (SOC-01).", nameof(numero));
        }

        if (typeDemande is not ("Avoir" or "Exoneration"))
        {
            throw new ArgumentException("Le type de demande doit être 'Avoir' ou 'Exoneration' (AVE-01).", nameof(typeDemande));
        }

        if (string.IsNullOrWhiteSpace(objet))
        {
            throw new ArgumentException("L'objet de la demande est obligatoire.", nameof(objet));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("La description de la demande est obligatoire.", nameof(description));
        }

        if (montantConteste.HasValue && montantConteste.Value < 0)
        {
            throw new ArgumentException("Le montant contesté ne peut pas être négatif.", nameof(montantConteste));
        }

        return new Ticket
        {
            Id = Guid.NewGuid(),
            Numero = numero,
            Module = "AvoirExoneration",
            TypeDemande = typeDemande,
            DossierId = dossierId,
            FileCouranteId = fileCibleId,
            Statut = StatutTicket.EnAttenteDePriseEnCharge,
            Objet = objet,
            Description = description,
            MontantConteste = montantConteste,
            DateCreationUtc = maintenantUtc,
            DateArriveeEnFileUtc = maintenantUtc,
            DateDerniereActiviteUtc = maintenantUtc
        };
    }

    /// <summary>
    /// EXTENSION FEATURE 007 (AVE-03) : Enregistre la décision finale d'accord ou de rejet motivé.
    /// </summary>
    public void EnregistrerDecisionAvoirExo(
        CIT.Plateforme.Domain.AvoirExoneration.DecisionAvoirExo decision,
        string? motifRejet,
        decimal? montantAccorde,
        Guid decideurId,
        DateTime maintenantUtc)
    {
        if (Statut is StatutTicket.Cloture or StatutTicket.Annule)
        {
            throw new InvalidOperationException($"Impossible d'enregistrer une décision sur un ticket au statut {Statut}.");
        }

        if (decision == CIT.Plateforme.Domain.AvoirExoneration.DecisionAvoirExo.Rejete)
        {
            if (string.IsNullOrWhiteSpace(motifRejet))
            {
                throw new ArgumentException("Le motif de rejet est obligatoire en cas de refus (AVE-03).", nameof(motifRejet));
            }

            DecisionAvoirExo = CIT.Plateforme.Domain.AvoirExoneration.DecisionAvoirExo.Rejete;
            MotifRejetAvoirExo = motifRejet.Trim();
            MontantAccorde = 0m;
        }
        else if (decision == CIT.Plateforme.Domain.AvoirExoneration.DecisionAvoirExo.Accorde)
        {
            DecisionAvoirExo = CIT.Plateforme.Domain.AvoirExoneration.DecisionAvoirExo.Accorde;
            MotifRejetAvoirExo = null;
            MontantAccorde = montantAccorde;
        }

        Statut = StatutTicket.Cloture;
        MotifCloture = MotifClotureType.Normale;
        DecideurAvoirExoId = decideurId;
        DateDecisionAvoirExoUtc = maintenantUtc;
        DateDerniereActiviteUtc = maintenantUtc;
        DateClotureUtc = maintenantUtc;
    }
}
