using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Enrolement;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Application.Enrolement;

/// <summary>ENR-01 (remédiation) — valeur soumise pour un champ du référentiel
/// <see cref="ChampFormulaireEnrolement"/>, identifié par son code stable.</summary>
public sealed record ValeurChampEnrolementSoumission(string ChampCode, string Valeur);

/// <summary>
/// Contrat C8-01 — Soumettre un enrôlement. CDC : ENR-01.
/// <para>
/// research.md D1 : le Transporteur/Chauffeur est une personne physique unique (CLAR-ENR-01) —
/// <see cref="Dossier.ClientId"/> = <c>appelantId</c> directement, <see cref="Dossier.AuteurId"/>
/// reste <c>null</c> (aucune société, aucune distinction auteur/propriétaire nécessaire).
/// </para>
/// <para>
/// ENR-01 (remédiation) — « la liste des informations et pièces est paramétrable » : lorsque le
/// référentiel <see cref="ChampFormulaireEnrolement"/> comporte des champs actifs, la soumission
/// doit en outre fournir une valeur pour chacun des champs obligatoires. Tant que le référentiel
/// est vide (état livré par défaut), seuls l'objet et la description libres sont exigés — la
/// compatibilité ascendante est donc totale.
/// </para>
/// </summary>
public sealed record SoumettreEnrolementRequete(
    string Objet,
    string Description,
    IReadOnlyList<ValeurChampEnrolementSoumission>? Champs = null);

public sealed record SoumettreEnrolementResultat(Guid TicketId, string NumeroTicket);

public sealed class SoumettreEnrolementCommand(
    ITicketRepository tickets,
    IDossierRepository dossiers,
    IFileDeTraitementRepository files,
    IChampFormulaireEnrolementRepository champsFormulaire,
    IValeurChampEnrolementRepository valeursChamp,
    IServiceNumerotation numerotation,
    IJournalisationService journalisation,
    IHorloge horloge,
    IAutorisationService autorisation,
    IUniteDeTravail uniteDeTravail)
{
    public const string Module = "Enrolement";
    public const string TypeDemande = "Enrolement";
    public const string FileAgentGate = "Agent Gate";

    public async Task<SoumettreEnrolementResultat> ExecuterAsync(Guid appelantId, SoumettreEnrolementRequete requete, CancellationToken ct = default)
    {
        // A/C. Autorisation globale — vérifiée côté serveur avant toute écriture.
        var aLeDroit = await autorisation.EstAutoriseAsync(appelantId, Permission.EnrolementSoumettre, null, ct).ConfigureAwait(false);
        if (!aLeDroit)
        {
            throw new AccesRefuseException($"L'utilisateur {appelantId} ne possède pas la permission de soumettre un enrôlement.");
        }

        // D. Validation du contenu — défense en profondeur avant toute persistance (patron REC-REM-02/F006).
        if (string.IsNullOrWhiteSpace(requete.Objet))
        {
            throw new ArgumentException("L'objet de l'enrôlement est obligatoire (ENR-01).", nameof(requete));
        }

        if (string.IsNullOrWhiteSpace(requete.Description))
        {
            throw new ArgumentException("La description de l'enrôlement est obligatoire (ENR-01).", nameof(requete));
        }

        // E. Validation des champs structurés paramétrables (ENR-01, remédiation). Tant que le
        // référentiel est vide, aucune contrainte supplémentaire n'est ajoutée.
        // LOT 4 — l'auto-inscription individuelle concerne une personne physique = un chauffeur
        // (research.md D1). Seuls les champs applicables au chauffeur (globaux + typés Chauffeur)
        // sont exigés et persistés ; un champ propre au camion ne s'applique jamais ici.
        var champsActifs = (await champsFormulaire.ListerAsync(activesUniquement: true, ct).ConfigureAwait(false))
            .Where(c => c.EstApplicableA(CIT.Plateforme.Domain.Enrolement.TypeObjetEnrolement.Chauffeur))
            .ToList();
        var valeursSoumises = (requete.Champs ?? [])
            .Where(v => !string.IsNullOrWhiteSpace(v.Valeur))
            .ToDictionary(v => v.ChampCode, v => v.Valeur, StringComparer.OrdinalIgnoreCase);

        foreach (var champ in champsActifs.Where(c => c.EstObligatoire))
        {
            if (!valeursSoumises.ContainsKey(champ.Code))
            {
                throw new ArgumentException(
                    $"Le champ « {champ.Libelle} » est obligatoire (ENR-01).", nameof(requete));
            }
        }

        // F. Résolution de la file "Agent Gate" — donnée paramétrée, jamais un identifiant codé en dur.
        var toutesLesFiles = await files.ListerAsync(ct).ConfigureAwait(false);
        var fileAgentGate = toutesLesFiles.SingleOrDefault(f => f.Module == Module && f.Nom == FileAgentGate)
            ?? throw new InvalidOperationException($"File « {FileAgentGate} » (module {Module}) non paramétrée.");

        var maintenant = horloge.MaintenantUtc;

        // B. Dossier propriétaire = appelant lui-même (personne physique unique, D1) — aucune
        // donnée d'identité transmise par le client n'est utilisée ici.
        var numeroDossier = await numerotation.ProchainNumeroDossierAsync(ct).ConfigureAwait(false);
        var dossier = Dossier.Creer(numeroDossier, appelantId, maintenant);
        await dossiers.AjouterAsync(dossier, ct).ConfigureAwait(false);

        var numeroTicket = await numerotation.ProchainNumeroTicketAsync(Module, TypeDemande, ct).ConfigureAwait(false);
        var ticket = Ticket.Creer(numeroTicket, Module, TypeDemande, fileAgentGate.Id, dossier.Id, maintenant);
        ticket.DefinirInformationsEnrolement(requete.Objet, requete.Description);
        await tickets.AjouterAsync(ticket, ct).ConfigureAwait(false);

        // Persistance des champs structurés paramétrables (ENR-01, remédiation) — seuls les champs
        // connus du référentiel actif sont retenus, un code inconnu n'est pas persisté.
        foreach (var champ in champsActifs)
        {
            if (valeursSoumises.TryGetValue(champ.Code, out var valeur))
            {
                await valeursChamp.AjouterAsync(ValeurChampEnrolement.Creer(ticket.Id, champ.Code, valeur), ct).ConfigureAwait(false);
            }
        }

        // G. Audit.
        await journalisation.ConsignerHistoriqueAsync(ticket.Id, "TicketCree", "Enrôlement soumis", appelantId, ct).ConfigureAwait(false);
        await journalisation.ConsignerAuditAsync(appelantId, "TicketCree", nameof(Ticket), ticket.Id, null, ticket.Numero, ct).ConfigureAwait(false);
        await journalisation.PublierEvenementAsync("TicketCree", $"{{\"ticketId\":\"{ticket.Id}\",\"module\":\"{Module}\"}}", ct).ConfigureAwait(false);
        // Alerte les agents de la file Agent Gate : nouvel enrôlement à instruire.
        await journalisation.PublierEvenementAsync("TicketArriveeEnFile", $"{{\"ticketId\":\"{ticket.Id}\"}}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);

        return new SoumettreEnrolementResultat(ticket.Id, ticket.Numero);
    }
}
