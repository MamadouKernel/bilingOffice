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
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(administrateurFonctionnelId, Permission.CompteValider));
        await dbContext.SaveChangesAsync();

        var demandeurId = Guid.NewGuid();
        var societe = await soumettre.ExecuterAsync(new SoumettreDemandeCompteRequete(demandeurId, raisonSociale, TypeClient.Normal));
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

        // Le cloisonnement inter-société dur (CPT-04) réutilise CloisonnementService de
        // Feature 001 TEL QUEL, en lui passant l'identifiant société (research.md D5).
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            cloisonnement.ExigerProprieteClientAsync(societeB.Id, ticketDeA.Id));

        // Le propriétaire réel (société A), lui, n'est jamais refusé.
        await cloisonnement.ExigerProprieteClientAsync(societeA.Id, ticketDeA.Id);
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
