using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.BillingImport;

/// <summary>
/// Contrat C3-01/C3-02 — Soumettre une demande IMPORT multi-BL, avec contrôle d'unicité par BL.
/// CDC : IMP-01, IMP-02, IMP-03, UNI-01, UNI-02, UNI-03.
/// </summary>
public sealed record SoumettreDemandeImportRequete(
    Guid SocieteId,
    Guid AuteurId,
    IReadOnlyList<string> NumerosBl,
    string? ReferenceSimulationProforma = null);

/// <summary>
/// UNI-01, CPT-04 : un BL refusé (déjà actif) ne révèle <see cref="TicketExistantId"/> que si le
/// ticket actif appartient à la **même société** que l'appelant (cloisonnement absolu, aucune
/// exception) — sinon un message générique, sans divulgation d'appartenance.
/// </summary>
public sealed record BlRefuseInfo(string NumeroBl, Guid? TicketExistantId);

/// <summary>
/// research.md D4 : chaque BL est traité indépendamment — un dossier est toujours créé (même si
/// tous les BL sont refusés, cohérent avec IMP-01) ; <see cref="BlRefuses"/> ne bloque jamais les
/// autres BL de la même soumission (jamais de transaction globale « tout ou rien »).
/// </summary>
/// <summary>
/// Résultat d'une soumission import. <see cref="Dossier"/> est <c>null</c> lorsqu'AUCUN
/// connaissement n'a été retenu (tous en doublon) : dans ce cas aucun dossier n'est créé
/// (REMÉDIATION D-10). <see cref="EstRetenu"/> distingue le succès du refus intégral.
/// </summary>
public sealed record SoumettreDemandeImportResultat(Dossier? Dossier, IReadOnlyList<Ticket> TicketsCrees, IReadOnlyList<BlRefuseInfo> BlRefuses)
{
    public bool EstRetenu => Dossier is not null;
}

public sealed class SoumettreDemandeImportCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    ISocieteClienteRepository societes,
    IUtilisateurSocieteRepository utilisateursSociete,
    IFileDeTraitementRepository files,
    IServiceNumerotation numerotation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IUniteDeTravail uniteDeTravail,
    IPrioriteRepository? priorites = null,
    Tickets.ResoudrePrioriteAutomatiqueService? resoudrePriorite = null)
{
    public const string Module = "BillingImport";
    public const string TypeDemande = "Import";
    public const string FilePiloteManifeste = "Pilote Manifeste";

    /// <summary>IMP-01 : « jusqu'à 5 BL par demande ». Plafond métier, contrôlé serveur quel que
    /// soit ce qu'affiche l'interface (D-10-like : jamais une confiance aveugle dans le client).</summary>
    public const int NombreMaxBlParDemande = 5;

    public async Task<SoumettreDemandeImportResultat> ExecuterAsync(SoumettreDemandeImportRequete requete, CancellationToken ct = default)
    {
        // IMP-01 : contrôle avant toute autre lecture — une demande hors limite est refusée
        // d'emblée, sans consommer de numérotation ni créer le moindre dossier.
        if (requete.NumerosBl.Count > NombreMaxBlParDemande)
        {
            throw new LimiteBlDepasseeException(NombreMaxBlParDemande, requete.NumerosBl.Count);
        }

        var maintenant = horloge.MaintenantUtc;

        // ENT-02 (Feature 002) : le droit de soumission est individuel, rattaché à la société —
        // distinct du moteur de permissions IAutorisationService (agents CIT). Un utilisateur
        // désactivé ou sans droit de soumission est refusé, quel que soit l'état de l'interface.
        var rattachement = await utilisateursSociete.ObtenirParUtilisateurAsync(requete.AuteurId, ct).ConfigureAwait(false);
        if (rattachement is null || rattachement.SocieteId != requete.SocieteId || !rattachement.EstActif || !rattachement.DroitSoumission)
        {
            throw new AccesRefuseException($"L'utilisateur {requete.AuteurId} n'est pas habilité à soumettre pour la société {requete.SocieteId} (ENT-02).");
        }

        // research.md D5 : instantané du type de client figé à la création du dossier (Dossier.
        // TypeClient, Feature 002 CPT-06) — jamais relu ensuite pour piloter l'aiguillage (IMP-06).
        var societe = await societes.ObtenirParIdAsync(requete.SocieteId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Société {requete.SocieteId} introuvable.");

        // Une entreprise de transport n'a pas de parcours Billing Office : elle enrôle sa flotte.
        // Refus serveur, quel que soit ce qu'affiche le menu (qui masque déjà ces écrans).
        if (societe.EstTransporteur)
        {
            throw new AccesRefuseException($"La société {requete.SocieteId} est un transporteur : aucune demande Billing Office.");
        }

        // T028 (quickstart.md §3) : la file Pilote Manifeste est une donnée paramétrée, jamais un
        // identifiant codé en dur.
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileManifeste = toutesLesFiles.SingleOrDefault(f => f.Module == Module && f.Nom == FilePiloteManifeste)
            ?? throw new InvalidOperationException($"File « {FilePiloteManifeste} » (module {Module}) non paramétrée — voir quickstart.md §3.");

        // REMÉDIATION D-10 — LE DOSSIER N'EST CRÉÉ QUE SI AU MOINS UN BL EST RETENU.
        // Auparavant le dossier était persisté AVANT la boucle : une soumission entièrement en
        // doublon laissait un dossier vide en base et affichait pourtant « succès ». La détection
        // des doublons se fait donc désormais en pré-passe, avant toute écriture de dossier.
        var blRefuses = new List<BlRefuseInfo>();
        var candidats = new List<string>();
        foreach (var numeroBlBrut in requete.NumerosBl)
        {
            // research.md D2 : seul le trim des espaces externes est appliqué — jamais la casse
            // (collation explicite, D3), jamais les tirets/espaces internes.
            var numeroBl = numeroBlBrut.Trim();
            if (numeroBl.Length == 0)
            {
                continue;
            }

            var actif = await tickets.ObtenirTicketActifParNumeroBlAsync(numeroBl, ct).ConfigureAwait(false);
            if (actif is not null)
            {
                blRefuses.Add(await CreerRefusAsync(numeroBl, actif, requete.SocieteId, ct).ConfigureAwait(false));
            }
            else
            {
                candidats.Add(numeroBl);
            }
        }

        if (candidats.Count == 0)
        {
            // Aucun connaissement retenu : aucun dossier n'est créé, la demande est refusée.
            return new SoumettreDemandeImportResultat(null, [], blRefuses);
        }

        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var dossier = Dossier.Creer(
            numeroDossier,
            requete.SocieteId,
            maintenant,
            requete.AuteurId,
            societe.Profil,
            requete.ReferenceSimulationProforma);
        await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);
        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        var ticketsCrees = new List<Ticket>();

        // research.md D4 : chaque BL retenu est créé indépendamment — un doublon détecté à
        // l'écriture (course, D3) n'affecte jamais les autres BL de la même soumission.
        foreach (var numeroBl in candidats)
        {
            var numeroTicket = await numerotation.ProchainNumeroTicketAsync(Module, TypeDemande, ct).ConfigureAwait(false);
            var ticket = Ticket.Creer(numeroTicket, Module, TypeDemande, fileManifeste.Id, dossier.Id, maintenant, numeroBl);

            // PRI-01 : une règle administrable (module/type de demande/type de client) peut
            // attribuer automatiquement une priorité. Aucune règle configurée → comportement
            // inchangé (repli sur le niveau de plus faible rang, comme avant PRI-01).
            var prioriteParRegle = resoudrePriorite is not null
                ? await resoudrePriorite.ResoudreAsync(Module, TypeDemande, societe.Profil.ToString(), ct).ConfigureAwait(false)
                : null;

            if (prioriteParRegle is Guid idRegle)
            {
                ticket.DefinirPriorite(idRegle);
            }
            else if (priorites is not null)
            {
                var niveaux = await priorites.ListerNiveauxAsync(ct).ConfigureAwait(false);
                var niveauDefaut = niveaux.OrderBy(n => n.Rang).FirstOrDefault();
                if (niveauDefaut is not null)
                {
                    ticket.DefinirPriorite(niveauDefaut.Id);
                }
            }

            await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

            try
            {
                await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
            }
            catch (BlDejaActifException)
            {
                // Fenêtre de course (D3) : l'index unique filtré SQL Server a rejeté l'insertion
                // entre notre pré-vérification et l'écriture — traité comme un refus ordinaire,
                // sans affecter les autres BL de la soumission (D4). Le ticket gagnant n'est pas
                // encore visible dans ce contexte (rollback implicite de l'entrée en échec) ; un
                // rechargement ultérieur de la page de soumission permettrait d'y accéder si les
                // droits le permettent.
                blRefuses.Add(new BlRefuseInfo(numeroBl, null));
                continue;
            }

            ticketsCrees.Add(ticket);
            await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketImportCree", $"numeroBl={numeroBl}", requete.AuteurId, ct).ConfigureAwait(false);
            await journalisation.ConsignerAuditAsync(requete.AuteurId, "TicketImportCree", nameof(Ticket), ticket.Id, null, numeroBl, ct).ConfigureAwait(false);
            await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"{Module}\"}}", ct).ConfigureAwait(false);
            await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
        }

        return new SoumettreDemandeImportResultat(dossier, ticketsCrees, blRefuses);
    }

    /// <summary>
    /// CPT-04 (cloisonnement absolu) : l'accès au ticket existant n'est proposé que si son dossier
    /// appartient à la **même société** que l'appelant — jamais d'exception, quel que soit le rôle.
    /// </summary>
    private async Task<BlRefuseInfo> CreerRefusAsync(string numeroBl, Ticket ticketActif, Guid societeAppelant, CancellationToken ct)
    {
        var dossierExistant = await dossiers.ObtenirParIdAsync(ticketActif.DossierId, ct).ConfigureAwait(false);
        var accesAutorise = dossierExistant is not null && dossierExistant.ClientId == societeAppelant;
        return new BlRefuseInfo(numeroBl, accesAutorise ? ticketActif.Id : null);
    }
}
