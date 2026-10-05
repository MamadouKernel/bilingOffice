using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.Comptes;

/// <summary>
/// [TEST-CDC] REC-SCE-14 (`docs/04-matrice-recette.md`), portion CPT-04 uniquement — « un client ne
/// voit pas les tickets d'un autre » (le reste du scénario — canaux de notification, continuité
/// digital/présentiel, dossier, files par rôle — est hors périmètre de Feature 002, comme déjà
/// traité par Feature 001 pour sa propre portion — voir `recipe-scope-audit.md` et
/// `spec.md` § Testabilité).
/// CDC couverts : CPT-04.
/// <para>
/// Exécuté contre l'application réellement composée (DI, Identity, EF Core —
/// <see cref="PlateformeWebApplicationFactory"/>), avec deux sociétés de test distinctes.
/// </para>
/// </summary>
public sealed class RecSce14CloisonnementCompteTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private static async Task<SocieteCliente> CreerSocieteActiveAsync(IServiceProvider services, string raisonSociale)
    {
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var soumettre = services.GetRequiredService<SoumettreDemandeCompteCommand>();
        var validerOuRejeter = services.GetRequiredService<ValiderOuRejeterDemandeCompteCommand>();

        var administrateurFonctionnelId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurFonctionnelId), Permission.CompteValider));
        await dbContext.SaveChangesAsync();

        // Le demandeur devient administrateur de sa société à la validation : il lui faut un compte
        // réel, la consultation passant désormais par la garde centrale.
        var demandeurId = CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(
            dbContext, Guid.NewGuid(), CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client);
        await dbContext.SaveChangesAsync();
        // RCCM, DFE et CNI du représentant sont obligatoires par défaut (amorçage) : on joint des
        // justificatifs factices pour que la soumission passe le contrôle de complétude.
        var justificatifs = new[]
        {
            new JustificatifDepose("RCCM", "rccm.pdf", "application/pdf", 1024, $"test/{Guid.NewGuid()}"),
            new JustificatifDepose("DFE", "dfe.pdf", "application/pdf", 1024, $"test/{Guid.NewGuid()}"),
            new JustificatifDepose("CNI_REP", "cni.pdf", "application/pdf", 1024, $"test/{Guid.NewGuid()}"),
        };
        var societe = await soumettre.ExecuterAsync(new SoumettreDemandeCompteRequete(demandeurId, raisonSociale, TypeClient.Normal, justificatifs));
        await validerOuRejeter.ValiderAsync(societe.Id, administrateurFonctionnelId);

        return societe;
    }

    [Fact]
    public async Task Un_client_de_la_societe_A_ne_peut_pas_consulter_une_demande_de_la_societe_B()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var creerTicket = services.GetRequiredService<CreerTicketCommand>();
        var cloisonnement = services.GetRequiredService<CloisonnementService>();

        var societeA = await CreerSocieteActiveAsync(services, "Société A");
        var societeB = await CreerSocieteActiveAsync(services, "Société B");
        var utilisateurA = dbContext.UtilisateursSociete.Single(u => u.SocieteId == societeA.Id);
        var utilisateurB = dbContext.UtilisateursSociete.Single(u => u.SocieteId == societeB.Id);

        var ticketDeA = await creerTicket.ExecuterAsync(new CreerTicketRequete(
            societeA.Id, "BillingOffice", "Import", Guid.NewGuid(), DossierExistantId: null));

        // CONSOLIDATION SÉCURITÉ — L'IDENTITÉ EST CELLE D'UNE PERSONNE, PAS D'UNE SOCIÉTÉ.
        //
        // Ce test passait auparavant `societeA.Id` / `societeB.Id` comme identité appelante, en
        // s'appuyant sur le fait que CloisonnementService comparait l'appelant à Dossier.ClientId.
        // Une société n'est pas un compte : elle ne s'authentifie pas et n'agit pas. Le contrôle
        // délègue désormais à la garde centrale, qui résout le rattachement société de la personne.
        //
        // L'OBJECTIF MÉTIER EST INCHANGÉ — un client de la société A n'atteint pas une demande de
        // la société B — mais il est désormais éprouvé sur des utilisateurs réels, ce qui est la
        // seule forme sous laquelle il se produit en exploitation.
        var administrateurA = dbContext.UtilisateursSociete.Single(u => u.SocieteId == societeA.Id);
        var administrateurB = dbContext.UtilisateursSociete.Single(u => u.SocieteId == societeB.Id);

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            cloisonnement.ExigerProprieteClientAsync(administrateurB.UtilisateurId, ticketDeA.Id));

        // Une société fournie comme identité n'autorise rien non plus : c'est le contournement que
        // la consolidation ferme.
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            cloisonnement.ExigerProprieteClientAsync(societeA.Id, ticketDeA.Id));

        // Le propriétaire réel — l'administrateur de la société A — n'est jamais refusé.
        await cloisonnement.ExigerProprieteClientAsync(administrateurA.UtilisateurId, ticketDeA.Id);
    }

    [Fact]
    public async Task Determiner_visibilite_ne_permet_jamais_a_un_utilisateur_de_voir_les_demandes_d_une_autre_societe()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        var determinerVisibilite = services.GetRequiredService<DeterminerVisibiliteDemandesQuery>();
        var rattachements = services.GetRequiredService<CIT.Plateforme.Application.Abstractions.IUtilisateurSocieteRepository>();
        var gererDroits = services.GetRequiredService<ModifierDroitsUtilisateurCommand>();

        var societeA = await CreerSocieteActiveAsync(services, "Société A");
        var societeB = await CreerSocieteActiveAsync(services, "Société B");
        var adminA = dbContext.UtilisateursSociete.Single(u => u.SocieteId == societeA.Id);
        var adminB = dbContext.UtilisateursSociete.Single(u => u.SocieteId == societeB.Id);
        // L'administrateur B a un droit de consultation total sur SA société — jamais sur A.
        await gererDroits.ExecuterAsync(adminB.UtilisateurId, adminB.Id, DroitConsultation.ToutesDemandesSociete, droitSoumission: true);

        var dossierDeA = Domain.Tickets.Dossier.Creer("DOS-RECSCE14-001", societeA.Id, DateTime.UtcNow, adminA.UtilisateurId);
        dbContext.Dossiers.Add(dossierDeA);
        await dbContext.SaveChangesAsync();

        var filtreB = await determinerVisibilite.ExecuterAsync(adminB.UtilisateurId);

        Assert.False(filtreB.DemandeEstVisible(dossierDeA));
    }
}
