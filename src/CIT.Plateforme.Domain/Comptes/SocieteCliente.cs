namespace CIT.Plateforme.Domain.Comptes;

/// <summary>
/// Compte client (société) — entité principale du référentiel Feature 002 (CPT-01, CPT-02,
/// `docs/cdc_text.txt` §10.1). Comme <see cref="CIT.Plateforme.Domain.Tickets.Ticket"/> en
/// Feature 001, aucune entité « demande » distincte n'existe : la société existe dès la
/// soumission, à l'état <see cref="EtatSociete.EnAttenteValidation"/> (research.md D2).
/// </summary>
public sealed class SocieteCliente
{
    public Guid Id { get; private set; }

    /// <summary>Utilisateur à l'origine de la demande — devient automatiquement le premier
    /// administrateur de société à la validation (spec.md § Assumptions, contrat C2-02).</summary>
    public Guid DemandeurId { get; private set; }

    /// <summary>Informations/justificatifs déclarés — contenu paramétrable, non fixé ici (PC-08).</summary>
    public string InformationsDeclarees { get; private set; } = string.Empty;

    /// <summary>
    /// Raison sociale saisie dans un champ DÉDIÉ et obligatoire. Nulle seulement pour les sociétés
    /// antérieures à ce champ, tant que la reprise ne l'a pas renseignée.
    /// </summary>
    public string? RaisonSociale { get; private set; }

    /// <summary>N° RCCM, normalisé en majuscules (voir <see cref="NumeroRccm"/>).</summary>
    public string? Rccm { get; private set; }

    /// <summary>Nom à afficher partout : le champ dédié, sinon le nom déduit du texte déclaré.</summary>
    public string NomAffichable =>
        !string.IsNullOrWhiteSpace(RaisonSociale) ? RaisonSociale : InformationsSociete.NomDe(InformationsDeclarees);

    /// <summary>RCCM à afficher : le champ dédié, sinon celui déduit du texte déclaré.</summary>
    public string? RccmAffichable => Rccm ?? InformationsSociete.Extraire(InformationsDeclarees).Rccm;

    public TypeClient Profil { get; private set; }

    public EtatSociete Etat { get; private set; }

    /// <summary>Invariant CLAR-03 : ne descend jamais à 0 tant que la société est active.</summary>
    public int NombreAdministrateursActifs { get; private set; }

    public DateTime DateCreationUtc { get; private set; }

    /// <summary>Jeton de concurrence optimiste (research.md D6/D7) — garantit l'invariant du
    /// dernier administrateur actif et l'unicité d'une décision de validation/rejet.</summary>
    public byte[] VersionConcurrence { get; private set; } = [];

    private SocieteCliente() { }

    public static SocieteCliente Soumettre(Guid demandeurId, string informationsDeclarees, TypeClient profilDeclare, DateTime maintenantUtc) => new()
    {
        Id = Guid.NewGuid(),
        DemandeurId = demandeurId,
        InformationsDeclarees = informationsDeclarees,
        Profil = profilDeclare,
        Etat = EtatSociete.EnAttenteValidation,
        NombreAdministrateursActifs = 0,
        DateCreationUtc = maintenantUtc,
    };

    /// <summary>
    /// Soumission avec IDENTITÉ STRUCTURÉE (voie du formulaire) : raison sociale et RCCM sont des
    /// champs distincts, obligatoires et validés ; le texte libre ne porte plus que les compléments.
    /// </summary>
    public static SocieteCliente Soumettre(
        Guid demandeurId, string raisonSociale, string rccm, string informationsComplementaires,
        TypeClient profilDeclare, DateTime maintenantUtc)
    {
        var societe = Soumettre(demandeurId, informationsComplementaires ?? string.Empty, profilDeclare, maintenantUtc);
        societe.RenseignerIdentite(raisonSociale, rccm);
        return societe;
    }

    /// <summary>
    /// REPRISE des sociétés antérieures aux champs dédiés : l'identité est déduite du texte
    /// déclaré. Le RCCM n'est retenu que s'il est valide — jamais de donnée douteuse promue en
    /// champ de référence. Sans effet si l'identité est déjà renseignée. Renvoie vrai si modifiée.
    /// </summary>
    public bool ReprendreIdentiteDeclaree()
    {
        if (!string.IsNullOrWhiteSpace(RaisonSociale))
        {
            return false;
        }

        var deduites = InformationsSociete.Extraire(InformationsDeclarees);
        if (string.IsNullOrWhiteSpace(deduites.Nom))
        {
            return false;
        }

        RaisonSociale = deduites.Nom.Length > LongueurMaxRaisonSociale
            ? deduites.Nom[..LongueurMaxRaisonSociale]
            : deduites.Nom;
        Rccm = NumeroRccm.EstValide(deduites.Rccm) ? NumeroRccm.Nettoyer(deduites.Rccm) : null;
        return true;
    }

    public const int LongueurMaxRaisonSociale = 200;

    /// <summary>Renseigne (ou corrige) l'identité structurée : raison sociale non vide, RCCM valide.</summary>
    public void RenseignerIdentite(string raisonSociale, string rccm)
    {
        if (string.IsNullOrWhiteSpace(raisonSociale) || raisonSociale.Trim().Length > LongueurMaxRaisonSociale)
        {
            throw new ArgumentException(
                $"La raison sociale est obligatoire ({LongueurMaxRaisonSociale} caractères au plus).", nameof(raisonSociale));
        }

        RaisonSociale = raisonSociale.Trim();
        Rccm = NumeroRccm.Normaliser(rccm);
    }

    /// <summary>Validation par un administrateur fonctionnel CIT (CPT-01). Le profil déclaré est
    /// confirmé tel quel — aucune modification de valeur à la validation.</summary>
    public void Valider()
    {
        if (Etat != EtatSociete.EnAttenteValidation)
        {
            throw new InvalidOperationException(
                $"La société {Id} ne peut pas être validée depuis l'état {Etat} (CPT-01).");
        }

        Etat = EtatSociete.Active;
    }

    /// <summary>Rejet par un administrateur fonctionnel CIT, motif obligatoire (traçabilité —
    /// Principe IX ; motif consigné par l'appelant via le journal d'audit, non porté ici).</summary>
    public void Rejeter()
    {
        if (Etat != EtatSociete.EnAttenteValidation)
        {
            throw new InvalidOperationException(
                $"La société {Id} ne peut pas être rejetée depuis l'état {Etat} (CPT-01).");
        }

        Etat = EtatSociete.Refusee;
    }

    /// <summary>Modification tracée du profil par un administrateur fonctionnel CIT (CPT-06) —
    /// n'a aucun effet sur les dossiers/tickets déjà créés (Feature 001, non touchés ici).</summary>
    public void ModifierType(TypeClient nouveauProfil)
    {
        Profil = nouveauProfil;
    }

    /// <summary>Un utilisateur rattaché devient administrateur actif (premier administrateur à la
    /// validation, attribution ultérieure du rôle, ou réactivation) — CLAR-03.</summary>
    public void EnregistrerAdministrateurActif()
    {
        NombreAdministrateursActifs++;
    }

    /// <summary>
    /// Un administrateur actif est désactivé ou perd son rôle (ENT-02) — CLAR-03 : refuse de
    /// faire tomber le nombre d'administrateurs actifs à zéro. Le CDC ne décrit aucun mécanisme de
    /// transfert automatique du rôle ni d'intervention CIT pour ce cas : garde-fou préventif.
    /// </summary>
    public void DecrementerAdministrateursActifs()
    {
        if (NombreAdministrateursActifs <= 1)
        {
            throw new DernierAdministrateurActifException(
                $"La société {Id} ne peut pas perdre son dernier administrateur actif (ENT-02, CLAR-03).");
        }

        NombreAdministrateursActifs--;
    }
}
