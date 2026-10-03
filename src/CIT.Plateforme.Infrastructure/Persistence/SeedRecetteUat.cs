using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>Compte-rendu d'un amorçage de recette. Ne contient jamais de secret.</summary>
public sealed record ResultatSeedUat(
    bool Execute,
    string Motif,
    int ComptesCrees,
    int ComptesDejaPresents,
    int SocietesCreees,
    int TicketsCrees);

/// <summary>
/// AMORÇAGE DU JEU DE RECETTE UAT — identités et données strictement fictives.
///
/// Distinct de <see cref="DevelopmentSeeder"/>, et volontairement : celui-ci crée un compte au nom
/// et à l'adresse d'une personne réelle, ce qu'une recette ne doit pas faire. Aucune de ses
/// identités n'est réutilisée ici.
///
/// GARANTIES
/// <list type="bullet">
///   <item><b>Aucune donnée réelle</b> : toutes les adresses sont sous <c>@local.test</c>, domaine
///   réservé aux tests par la RFC 6761 — aucune ne peut exister. Les noms sont inventés et préfixés
///   <c>UAT_</c>.</item>
///   <item><b>Aucun secret en dur</b> : le mot de passe vient de la configuration locale
///   (<c>dotnet user-secrets</c>), jamais du code, jamais d'un fichier versionné.</item>
///   <item><b>Idempotent</b> : relancer l'amorçage ne duplique rien. Les comptes sont retrouvés par
///   adresse, les sociétés par raison sociale, les files par (module, nom), les tickets par
///   numéro.</item>
///   <item><b>Jamais automatique</b> : trois barrières cumulatives, voir
///   <see cref="PolitiqueRecetteUat"/>.</item>
///   <item><b>Aucun mot de passe réécrit</b> : un compte déjà présent garde le sien, comme dans
///   l'amorçage de démonstration — une rotation faite par un opérateur n'est jamais défaite.</item>
/// </list>
/// </summary>
public sealed class SeedRecetteUat(
    PlateformeDbContext context,
    UserManager<ApplicationUser> userManager,
    IHostEnvironment environnement,
    IConfiguration configuration,
    ILogger<SeedRecetteUat> logger)
{
    // ─── Identités de recette ────────────────────────────────────────────────────────────────
    // Le domaine @local.test est réservé aux tests : aucune de ces adresses ne peut exister.

    public const string ClientNormal = "uat.client.normal@local.test";
    public const string ClientEnCompte = "uat.client.compte@local.test";
    public const string ClientDeposit = "uat.client.deposit@local.test";
    public const string ClientIes = "uat.client.ies@local.test";
    public const string AdminSociete = "uat.admin.societe@local.test";
    public const string CollaborateurLimite = "uat.collab.limite@local.test";
    public const string CollaborateurGlobal = "uat.collab.global@local.test";
    public const string PiloteManifeste = "uat.pilote.manifeste@local.test";
    public const string PiloteFacturation = "uat.facturation@local.test";
    public const string PiloteControleDocumentaire = "uat.controle.doc@local.test";
    public const string BackOfficeExport = "uat.backoffice.export@local.test";
    public const string SupportOperationnel = "uat.support@local.test";
    public const string AgentGate = "uat.agent.gate@local.test";
    public const string Superviseur = "uat.superviseur@local.test";
    public const string AdminFonctionnel = "uat.admin.fonctionnel@local.test";
    public const string Transporteur = "uat.transporteur@local.test";
    public const string Caisse = "uat.caisse@local.test";
    public const string Comptabilite = "uat.comptabilite@local.test";
    public const string Guichet = "uat.guichet@local.test";
    public const string AvoirExoneration = "uat.avoir@local.test";
    public const string SupportIt = "uat.support.it@local.test";
    public const string AgentSansDroits = "uat.agent.sansdroits@local.test";
    public const string CompteDesactive = "uat.compte.desactive@local.test";

    /// <summary>Les vingt-trois profils de recette, avec leur nature et leurs permissions.</summary>
    public static readonly IReadOnlyList<(string Email, string Nom, NatureUtilisateur Nature, bool Actif, string[] Permissions)> Profils =
    [
        (ClientNormal, "UAT_Client Normal", NatureUtilisateur.Client, true,
            [Permission.BillingImportSoumettre, Permission.BillingExportSoumettre, Permission.ComplaintSoumettre]),
        (ClientEnCompte, "UAT_Client En Compte", NatureUtilisateur.Client, true,
            [Permission.BillingImportSoumettre, Permission.BillingExportSoumettre, Permission.ComplaintSoumettre]),
        (ClientDeposit, "UAT_Client Deposit", NatureUtilisateur.Client, true,
            [Permission.BillingImportSoumettre, Permission.BillingExportSoumettre, Permission.ComplaintSoumettre]),
        (ClientIes, "UAT_Client IES", NatureUtilisateur.Client, true,
            [Permission.BillingImportSoumettre, Permission.BillingExportSoumettre, Permission.ComplaintSoumettre]),

        (AdminSociete, "UAT_Administrateur Societe", NatureUtilisateur.Client, true,
            [Permission.CompteGererCollaborateurs, Permission.BillingImportSoumettre, Permission.ComplaintSoumettre]),
        (CollaborateurLimite, "UAT_Collaborateur Limite", NatureUtilisateur.Client, true,
            [Permission.BillingImportSoumettre]),
        (CollaborateurGlobal, "UAT_Collaborateur Global", NatureUtilisateur.Client, true,
            [Permission.BillingImportSoumettre]),

        (PiloteManifeste, "UAT_Pilote Manifeste", NatureUtilisateur.Agent, true,
            [Permission.TicketPrendreEnCharge, Permission.TicketTransferer, Permission.MessageNoteInterne,
             Permission.DocumentDeposer, Permission.DocumentTelecharger,
             Permission.BillingImportManifesteInstruire, Permission.BillingImportIesManifesteInstruire]),
        (PiloteFacturation, "UAT_Pilote Facturation Terre", NatureUtilisateur.Agent, true,
            [Permission.TicketPrendreEnCharge, Permission.MessageNoteInterne, Permission.DocumentDeposer,
             Permission.DocumentTelecharger, Permission.BillingImportFacturationEditer,
             Permission.BillingExportFacturationEditer]),
        (PiloteControleDocumentaire, "UAT_Pilote Controle Documentaire", NatureUtilisateur.Agent, true,
            [Permission.TicketPrendreEnCharge, Permission.TicketCloturer, Permission.MessageNoteInterne,
             Permission.DocumentDeposer, Permission.DocumentTelecharger,
             Permission.BillingImportControleDocumentaireTraiter, Permission.BillingExportControleDocumentaireTraiter,
             Permission.BillingImportIesControleDocumentaireInstruire, Permission.BillingExportIesControleDocumentaireInstruire]),
        (BackOfficeExport, "UAT_Back Office Export", NatureUtilisateur.Agent, true,
            [Permission.TicketPrendreEnCharge, Permission.TicketTransferer, Permission.MessageNoteInterne,
             Permission.DocumentDeposer, Permission.BillingExportBackOfficeInstruire,
             Permission.BillingExportIesBackOfficeInstruire]),
        (SupportOperationnel, "UAT_Support Operationnel", NatureUtilisateur.Agent, true,
            [Permission.TicketPrendreEnCharge, Permission.MessageNoteInterne, Permission.ComplaintInstruire,
             Permission.ComplaintRepondre]),
        (AgentGate, "UAT_Agent Gate", NatureUtilisateur.Agent, true,
            [Permission.TicketPrendreEnCharge, Permission.MessageNoteInterne, Permission.EnrolementInstruire,
             Permission.DocumentTelecharger]),
        (Superviseur, "UAT_Superviseur", NatureUtilisateur.Agent, true,
            [Permission.SupervisionConsulter, Permission.SupervisionAgir, Permission.ActionDeMasse,
             Permission.DisponibiliteGerer, Permission.TicketReaffecter, Permission.TicketTransferer,
             Permission.PrioriteModifier]),
        (AdminFonctionnel, "UAT_Administrateur Fonctionnel", NatureUtilisateur.Agent, true,
            [Permission.AdministrationComptes, Permission.CompteValider, Permission.CompteTypeModifier,
             Permission.AdministrationFiles, Permission.AdministrationSla, Permission.AuditConsulter,
             Permission.TarificationAdministrer, Permission.AdministrationNotifications,
             Permission.AdministrationCategoriesReclamation, Permission.CompetenceGerer,
             Permission.AdministrationPerimetresSupervision, Permission.AdministrationChampsEnrolement,
             Permission.AdministrationAssistant, Permission.AdministrationReferentiels, Permission.AdministrationAide,
             Permission.AdministrationCategoriesIncident]),

        // Postes métier sans compte jusque-là : caisse, comptabilité client, guichet, avoirs et support IT.
        (Caisse, "UAT_Caisse", NatureUtilisateur.Agent, true,
            [Permission.TicketPrendreEnCharge, Permission.DocumentTelecharger,
             Permission.BillingImportPaiementValider, Permission.BillingExportPaiementValider]),
        (Comptabilite, "UAT_Comptabilite Client", NatureUtilisateur.Agent, true,
            [Permission.TicketPrendreEnCharge, Permission.DocumentTelecharger,
             Permission.BillingImportComptabiliteConfirmerLettrage, Permission.BillingExportComptabiliteConfirmerLettrage]),
        (Guichet, "UAT_Agent Guichet", NatureUtilisateur.Agent, true,
            [Permission.GuichetRecherche, Permission.RendezVousGerer, Permission.TicketPrendreEnCharge,
             Permission.DocumentTelecharger]),
        (AvoirExoneration, "UAT_Avoir Exoneration", NatureUtilisateur.Agent, true,
            [Permission.TicketPrendreEnCharge, Permission.DocumentTelecharger, Permission.AvoirInstruire,
             Permission.AvoirDecider, Permission.ExonerationInstruire, Permission.ExonerationDecider]),
        (SupportIt, "UAT_Support IT", NatureUtilisateur.Agent, true,
            [.. Permission.ProfilSupportN1, Permission.SupportIncidentInstruireN2]),

        (Transporteur, "UAT_Transporteur Chauffeur", NatureUtilisateur.Client, true,
            [Permission.EnrolementSoumettre]),

        // ── Comptes négatifs — leur valeur EST leur absence de droit ou d'activité ──
        (AgentSansDroits, "UAT_Agent Sans Habilitation", NatureUtilisateur.Agent, true, []),
        (CompteDesactive, "UAT_Compte Desactive", NatureUtilisateur.Agent, false,
            // Il DÉTIENT des permissions et reste refusé : c'est ce qui prouve que la garde
            // d'identité précède la permission (P0-ter).
            [Permission.TicketPrendreEnCharge, Permission.BillingImportManifesteInstruire]),
    ];

    private static readonly (string Module, string Nom)[] FilesRequises =
    [
        ("BillingImport", "Pilote Manifeste"),
        ("BillingImport", "Facturation"),
        ("BillingImport", "Validation Paiement"),
        ("BillingImport", "Comptabilité Client"),
        ("BillingImport", "Contrôle Documentaire"),
        ("BillingImport", "File IES"),
        ("BillingExport", "Back Office Export"),
        ("BillingExport", "Facturation"),
        ("BillingExport", "Validation Paiement"),
        ("BillingExport", "Comptabilité Client"),
        ("BillingExport", "Contrôle Documentaire"),
        ("BillingExport", "File IES"),
        ("CustomerComplaint", "Support Opérationnel"),
        ("AvoirExoneration", "Avoir & Exonération"),
        ("Enrolement", "Agent Gate"),
    ];

    public async Task<ResultatSeedUat> ExecuterAsync(CancellationToken ct = default)
    {
        var motDePasse = configuration[PolitiqueRecetteUat.CleMotDePasse]
            ?? configuration[PolitiqueRecetteUat.CleMotDePasseEnv];

        var decision = PolitiqueRecetteUat.Decider(
            environnement.EnvironmentName,
            motDePasse,
            configuration[PolitiqueRecetteUat.CleActivation],
            configuration[PolitiqueRecetteUat.CleBaseRecette],
            context.Database.GetDbConnection().Database);

        // Le motif décrit la décision, jamais la valeur d'un secret.
        logger.LogInformation("Amorçage de recette UAT : {Motif}", decision.Motif);

        if (!decision.AmorcageAutorise)
        {
            return new ResultatSeedUat(false, decision.Motif, 0, 0, 0, 0);
        }

        var maintenant = DateTime.UtcNow;

        await SeedFilesAsync(ct).ConfigureAwait(false);
        var societes = await SeedSocietesAsync(maintenant, ct).ConfigureAwait(false);
        var (crees, presents) = await SeedComptesAsync(motDePasse!, ct).ConfigureAwait(false);
        await SeedRattachementsAsync(societes, maintenant, ct).ConfigureAwait(false);
        await SeedPerimetreSuperviseurAsync(ct).ConfigureAwait(false);
        var tickets = await SeedDonneesMetierAsync(societes, maintenant, ct).ConfigureAwait(false);

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Amorçage de recette UAT terminé : {Crees} compte(s) créé(s), {Presents} déjà présent(s), {Tickets} ticket(s).",
            crees, presents, tickets);

        return new ResultatSeedUat(true, decision.Motif, crees, presents, societes.Count, tickets);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────

    private async Task SeedFilesAsync(CancellationToken ct)
    {
        foreach (var (module, nom) in FilesRequises)
        {
            var existe = await context.Files.AnyAsync(f => f.Module == module && f.Nom == nom, ct).ConfigureAwait(false);
            if (!existe)
            {
                context.Files.Add(FileDeTraitement.Creer(module, nom));
            }
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<Dictionary<TypeClient, SocieteCliente>> SeedSocietesAsync(DateTime maintenant, CancellationToken ct)
    {
        var resultat = new Dictionary<TypeClient, SocieteCliente>();

        foreach (var profil in new[] { TypeClient.Normal, TypeClient.EnCompte, TypeClient.Deposit, TypeClient.IES })
        {
            var raisonSociale = $"{PolitiqueRecetteUat.Prefixe}Societe {profil}";
            var societe = await context.Societes
                .FirstOrDefaultAsync(s => s.InformationsDeclarees == raisonSociale, ct)
                .ConfigureAwait(false);

            if (societe is null)
            {
                societe = SocieteCliente.Soumettre(Guid.NewGuid(), raisonSociale, profil, maintenant);
                societe.Valider();
                context.Societes.Add(societe);
            }

            resultat[profil] = societe;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return resultat;
    }

    private async Task<(int Crees, int Presents)> SeedComptesAsync(string motDePasse, CancellationToken ct)
    {
        var crees = 0;
        var presents = 0;

        foreach (var (email, nom, nature, actif, permissions) in Profils)
        {
            var compte = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
            if (compte is null)
            {
                compte = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    NomAffiche = nom,
                    Nature = nature,
                    EmailConfirmed = true,
                    EstActif = actif,
                };

                var creation = await userManager.CreateAsync(compte, motDePasse).ConfigureAwait(false);
                if (!creation.Succeeded)
                {
                    // Les codes d'erreur sont journalisés, jamais le secret.
                    logger.LogWarning(
                        "Compte de recette {Email} non créé : {Codes}",
                        email, string.Join(", ", creation.Errors.Select(e => e.Code)));
                    continue;
                }

                crees++;
            }
            else
            {
                // Le mot de passe d'un compte existant n'est jamais réécrit.
                presents++;

                // L'état d'activité, lui, est reposé : c'est une donnée de scénario, pas un secret.
                if (compte.EstActif != actif)
                {
                    compte.EstActif = actif;
                    await userManager.UpdateAsync(compte).ConfigureAwait(false);
                }
            }

            foreach (var permission in permissions)
            {
                var deja = await context.Habilitations
                    .AnyAsync(h => h.UtilisateurId == compte.Id && h.PermissionCode == permission && h.FileId == null, ct)
                    .ConfigureAwait(false);

                if (!deja)
                {
                    context.Habilitations.Add(HabilitationUtilisateur.Attribuer(compte.Id, permission));
                }
            }

            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return (crees, presents);
    }

    /// <summary>Sans périmètre, la supervision ne montre rien (fermée par défaut) : le superviseur de
    /// recette reçoit un périmètre global (liste vide = toutes les files), modifiable ensuite par
    /// l'administration.</summary>
    private async Task SeedPerimetreSuperviseurAsync(CancellationToken ct)
    {
        var superviseur = await userManager.FindByEmailAsync(Superviseur).ConfigureAwait(false);
        if (superviseur is null
            || await context.PerimetresSupervision.AnyAsync(p => p.SuperviseurId == superviseur.Id, ct).ConfigureAwait(false))
        {
            return;
        }

        context.PerimetresSupervision.Add(Domain.Supervision.PerimetreSupervision.Definir(superviseur.Id, []));
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task SeedRattachementsAsync(
        Dictionary<TypeClient, SocieteCliente> societes, DateTime maintenant, CancellationToken ct)
    {
        // Chaque client de profil est rattaché à la société de son profil.
        await RattacherAsync(ClientNormal, societes[TypeClient.Normal], false, DroitConsultation.PropresDemandesUniquement, ct).ConfigureAwait(false);
        await RattacherAsync(ClientEnCompte, societes[TypeClient.EnCompte], false, DroitConsultation.PropresDemandesUniquement, ct).ConfigureAwait(false);
        await RattacherAsync(ClientDeposit, societes[TypeClient.Deposit], false, DroitConsultation.PropresDemandesUniquement, ct).ConfigureAwait(false);
        await RattacherAsync(ClientIes, societes[TypeClient.IES], false, DroitConsultation.PropresDemandesUniquement, ct).ConfigureAwait(false);

        // ENT-03 : les deux portées de visibilité, sur la MÊME société, pour qu'elles soient
        // comparables en recette.
        await RattacherAsync(AdminSociete, societes[TypeClient.Normal], true, DroitConsultation.ToutesDemandesSociete, ct).ConfigureAwait(false);
        await RattacherAsync(CollaborateurLimite, societes[TypeClient.Normal], false, DroitConsultation.PropresDemandesUniquement, ct).ConfigureAwait(false);
        await RattacherAsync(CollaborateurGlobal, societes[TypeClient.Normal], false, DroitConsultation.ToutesDemandesSociete, ct).ConfigureAwait(false);

        // Le transporteur n'a VOLONTAIREMENT aucun rattachement : c'est ce qui le distingue
        // fonctionnellement d'un client (RG-CPT-08).
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task RattacherAsync(
        string email, SocieteCliente societe, bool administrateur, DroitConsultation droit, CancellationToken ct)
    {
        var compte = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (compte is null)
        {
            return;
        }

        var existant = await context.UtilisateursSociete
            .FirstOrDefaultAsync(u => u.UtilisateurId == compte.Id && u.SocieteId == societe.Id, ct)
            .ConfigureAwait(false);

        if (existant is not null)
        {
            return;
        }

        var rattachement = UtilisateurSociete.Rattacher(compte.Id, societe.Id, DateTime.UtcNow, administrateur, droitSoumission: true);
        rattachement.ModifierDroits(droit, droitSoumission: true);
        context.UtilisateursSociete.Add(rattachement);

        if (administrateur)
        {
            societe.EnregistrerAdministrateurActif();
        }
    }

    private async Task<int> SeedDonneesMetierAsync(
        Dictionary<TypeClient, SocieteCliente> societes, DateTime maintenant, CancellationToken ct)
    {
        var clientNormal = await userManager.FindByEmailAsync(ClientNormal).ConfigureAwait(false);
        var transporteur = await userManager.FindByEmailAsync(Transporteur).ConfigureAwait(false);
        var agent = await userManager.FindByEmailAsync(PiloteManifeste).ConfigureAwait(false);
        if (clientNormal is null || transporteur is null || agent is null)
        {
            return 0;
        }

        var tickets = 0;

        // ── IMPORT : un dossier, deux BL (SOC-11 : regroupement sous un dossier unique) ──
        var fileManifeste = await FileAsync("BillingImport", "Pilote Manifeste", ct).ConfigureAwait(false);
        var dossierImport = await DossierAsync(
            "UAT-DOS-IMPORT-01", societes[TypeClient.Normal].Id, clientNormal.Id, TypeClient.Normal, maintenant, ct).ConfigureAwait(false);

        foreach (var bl in new[] { "UATBL0000000001", "UATBL0000000002" })
        {
            if (await CreerTicketAsync($"UAT-TCK-{bl}", "BillingImport", "Import", fileManifeste, dossierImport, maintenant, bl, null, ct).ConfigureAwait(false))
            {
                tickets++;
            }
        }

        // ── EXPORT : un booking déjà codifié, posé en Facturation — prêt pour le Draft IN ──
        var fileFacturationExport = await FileAsync("BillingExport", "Facturation", ct).ConfigureAwait(false);
        var dossierExport = await DossierAsync(
            "UAT-DOS-EXPORT-01", societes[TypeClient.Normal].Id, clientNormal.Id, TypeClient.Normal, maintenant, ct).ConfigureAwait(false);

        if (await CreerTicketAsync("UAT-TCK-EXPORT-01", "BillingExport", "Export", fileFacturationExport, dossierExport, maintenant, null, "UATBKG00000001", ct).ConfigureAwait(false))
        {
            tickets++;
        }

        // ── RÉCLAMATION ──
        var fileSupport = await FileAsync("CustomerComplaint", "Support Opérationnel", ct).ConfigureAwait(false);
        var dossierReclamation = await DossierAsync(
            "UAT-DOS-RECLAMATION-01", societes[TypeClient.Normal].Id, clientNormal.Id, TypeClient.Normal, maintenant, ct).ConfigureAwait(false);

        if (await CreerTicketAsync("UAT-TCK-RECLAMATION-01", "CustomerComplaint", "Reclamation", fileSupport, dossierReclamation, maintenant, null, null, ct).ConfigureAwait(false))
        {
            tickets++;
        }

        // ── ENRÔLEMENT : dossier PERSONNEL du transporteur, sans société (RG-CPT-08) ──
        var fileGate = await FileAsync("Enrolement", "Agent Gate", ct).ConfigureAwait(false);
        var dossierEnrolement = await DossierAsync(
            "UAT-DOS-ENROLEMENT-01", transporteur.Id, transporteur.Id, null, maintenant, ct).ConfigureAwait(false);

        if (await CreerTicketAsync("UAT-TCK-ENROLEMENT-01", "Enrolement", "Enrolement", fileGate, dossierEnrolement, maintenant, null, null, ct).ConfigureAwait(false))
        {
            tickets++;
        }

        // ── AVOIR / EXONÉRATION ──
        var fileAvoir = await FileAsync("AvoirExoneration", "Avoir & Exonération", ct).ConfigureAwait(false);
        if (!await context.Tickets.AnyAsync(t => t.Numero == "UAT-TCK-AVOIR-01", ct).ConfigureAwait(false))
        {
            context.Tickets.Add(Ticket.CreerDemandeAvoirExo(
                "UAT-TCK-AVOIR-01", "Avoir", fileAvoir, dossierImport,
                "UAT_Demande d'avoir de recette", "Jeu de recette — contestation fictive.", 250_000m, maintenant));
            tickets++;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        // ── Documents, messages et note interne sur le premier ticket import ──
        var premierTicket = await context.Tickets
            .FirstOrDefaultAsync(t => t.Numero == "UAT-TCK-UATBL0000000001", ct).ConfigureAwait(false);

        if (premierTicket is not null)
        {
            if (!await context.Documents.AnyAsync(d => d.TicketId == premierTicket.Id, ct).ConfigureAwait(false))
            {
                context.Documents.Add(Document.Deposer(
                    premierTicket.Id, clientNormal.Id, "UAT_connaissement.pdf", "application/pdf",
                    2048, "uat/connaissement.pdf", maintenant));
            }

            if (!await context.Messages.AnyAsync(m => m.TicketId == premierTicket.Id, ct).ConfigureAwait(false))
            {
                context.Messages.Add(Message.Publier(
                    premierTicket.Id, clientNormal.Id, TypeMessage.Externe,
                    "UAT_Message client de recette : où en est mon dossier ?", maintenant));

                context.Messages.Add(Message.Publier(
                    premierTicket.Id, agent.Id, TypeMessage.NoteInterne,
                    "UAT_Note interne de recette : ne doit jamais être visible du client.", maintenant));
            }
        }

        // ── Un BL en attente d'un complément du client : sans lui, « Mes actions requises » et
        // l'assistant (« qu'attend-on de moi ? ») resteraient vides en recette. Ne s'applique
        // qu'au ticket encore neuf : relancer l'amorçage ne rejoue rien.
        var secondTicket = await context.Tickets
            .FirstOrDefaultAsync(t => t.Numero == "UAT-TCK-UATBL0000000002", ct).ConfigureAwait(false);
        if (secondTicket is { Statut: StatutTicket.EnAttenteDePriseEnCharge })
        {
            secondTicket.PrendreEnCharge(agent.Id, maintenant);
            secondTicket.MettreEnAttenteClient(maintenant);
            context.Messages.Add(Message.Publier(
                secondTicket.Id, agent.Id, TypeMessage.Externe,
                "UAT_Complément demandé : la copie du BL est illisible, merci de la déposer à nouveau.", maintenant));
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return tickets;
    }

    private async Task<Guid> FileAsync(string module, string nom, CancellationToken ct) =>
        await context.Files.Where(f => f.Module == module && f.Nom == nom).Select(f => f.Id)
            .FirstAsync(ct).ConfigureAwait(false);

    private async Task<Guid> DossierAsync(
        string numero, Guid clientId, Guid auteurId, TypeClient? typeClient, DateTime maintenant, CancellationToken ct)
    {
        var existant = await context.Dossiers.FirstOrDefaultAsync(d => d.Numero == numero, ct).ConfigureAwait(false);
        if (existant is not null)
        {
            return existant.Id;
        }

        var dossier = Dossier.Creer(numero, clientId, maintenant, auteurId, typeClient);
        context.Dossiers.Add(dossier);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return dossier.Id;
    }

    private async Task<bool> CreerTicketAsync(
        string numero, string module, string typeDemande, Guid fileId, Guid dossierId,
        DateTime maintenant, string? numeroBl, string? numeroBooking, CancellationToken ct)
    {
        if (await context.Tickets.AnyAsync(t => t.Numero == numero, ct).ConfigureAwait(false))
        {
            return false;
        }

        context.Tickets.Add(Ticket.Creer(numero, module, typeDemande, fileId, dossierId, maintenant, numeroBl, numeroBooking));
        return true;
    }
}
