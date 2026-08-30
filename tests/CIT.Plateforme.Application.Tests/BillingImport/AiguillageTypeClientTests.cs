using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// [TEST-SPEC] Exigence CDC: IMP-06 → US4 → critères d'acceptation 1-3 (FR-011, FR-012) → Test.
/// research.md D5 : aiguillage exclusivement piloté par l'instantané `Dossier.TypeClient` figé à la
/// création — jamais le type courant de `SocieteCliente` (CPT-06 jamais rouverte).
/// </summary>
public sealed class AiguillageTypeClientTests
{
    private sealed record Contexte(
        PlateformeDbContext DbContext, SoumettreDemandeImportCommand Soumettre, ValiderTicketImportCommand Valider,
        Guid AgentId, SocieteClienteRepository Societes);

    private static async Task<Contexte> ConstruireContexteAsync()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var numerotation = new NumerotationEnMemoirePourTests();
        var ticketRepository = new TicketRepository(dbContext);
        var dossierRepository = new DossierRepository(dbContext);
        var fileRepository = new FileDeTraitementRepository(dbContext);
        var societesRepository = new SocieteClienteRepository(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var agentId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(agentId, Permission.BillingImportManifesteInstruire));

        dbContext.Files.AddRange(
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.Facturation),
            FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes));
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            ticketRepository, dossierRepository, societesRepository, new UtilisateurSocieteRepository(dbContext),
            fileRepository, numerotation, journalisation, horloge, uniteDeTravail);
        var valider = new ValiderTicketImportCommand(
            ticketRepository, dossierRepository, fileRepository, new SuiviBrancheIesRepository(dbContext),
            autorisation, journalisation, horloge, uniteDeTravail);

        return new Contexte(dbContext, soumettre, valider, agentId, societesRepository);
    }

    private static async Task<Guid> CreerSocieteEtUtilisateurAsync(PlateformeDbContext dbContext, TypeClient profil, Guid auteurId)
    {
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", profil, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(auteurId, societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();
        return societe.Id;
    }

    [Theory]
    [InlineData(TypeClient.Normal, NomsFiles.Facturation)]
    [InlineData(TypeClient.EnCompte, NomsFiles.Facturation)]
    [InlineData(TypeClient.Deposit, NomsFiles.Facturation)]
    [InlineData(TypeClient.IES, NomsFiles.FileIes)]
    public async Task Ticket_valide_est_aiguille_vers_la_file_du_circuit_correspondant(TypeClient profil, string fileAttendue)
    {
        var c = await ConstruireContexteAsync();
        var auteurId = Guid.NewGuid();
        var societeId = await CreerSocieteEtUtilisateurAsync(c.DbContext, profil, auteurId);
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, auteurId, ["BL-AIG"]));
        var ticketId = soumission.TicketsCrees[0].Id;

        await c.Valider.ExecuterAsync(c.AgentId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        var file = await c.DbContext.Files.FindAsync(ticket!.FileCouranteId);
        Assert.Equal(fileAttendue, file!.Nom);
    }

    [Fact]
    public async Task Changement_de_type_apres_creation_du_dossier_avant_aiguillage_n_affecte_pas_le_circuit()
    {
        var c = await ConstruireContexteAsync();
        var auteurId = Guid.NewGuid();
        var societeId = await CreerSocieteEtUtilisateurAsync(c.DbContext, TypeClient.Normal, auteurId);
        var soumission = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societeId, auteurId, ["BL-SNAP"]));
        var ticketId = soumission.TicketsCrees[0].Id;

        // La société change de type APRÈS la création du dossier, AVANT la validation Manifeste
        // (CPT-06, Feature 002 — jamais rouverte ici).
        var societe = await c.Societes.ObtenirParIdAsync(societeId);
        societe!.ModifierType(TypeClient.IES);
        c.DbContext.Societes.Update(societe);
        await c.DbContext.SaveChangesAsync();

        await c.Valider.ExecuterAsync(c.AgentId, ticketId);

        var ticket = await c.DbContext.Tickets.FindAsync(ticketId);
        var file = await c.DbContext.Files.FindAsync(ticket!.FileCouranteId);
        // Toujours Facturation (instantané "Normal" figé à la création), jamais File IES.
        Assert.Equal(NomsFiles.Facturation, file!.Nom);
    }
}
