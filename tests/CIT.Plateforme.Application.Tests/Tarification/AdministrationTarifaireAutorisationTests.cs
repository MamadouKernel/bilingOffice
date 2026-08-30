using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tarification;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tarification;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Tarification;

/// <summary>
/// [TEST-FIRST] (spec.md §20, research.md D15) : l'administration tarifaire (création de version,
/// import CSV, consultation des grilles) exige explicitement <see cref="Permission.TarificationAdministrer"/>
/// — jamais un rôle Identity figé, jamais une simple authentification suffisante.
/// </summary>
public sealed class AdministrationTarifaireAutorisationTests
{
    [Fact]
    public void Permission_TarificationAdministrer_est_definie()
    {
        Assert.Equal("tarification.administrer", Permission.TarificationAdministrer);
    }

    private static IReadOnlyList<LigneTarifaireSaisie> UneLigne() =>
        [new LigneTarifaireSaisie(SensTrafic.ImportLocal, TailleConteneur.VingtPieds, "C1", 66800m, 25000m, 35000m)];

    [Fact]
    public async Task CreerVersionGrille_utilisateur_sans_permission_est_refuse()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var handler = new CreerVersionGrilleTarifaireCommandHandler(
            new VersionGrilleTarifaireRepository(db), new AutorisationService(db));
        var intrus = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() => handler.HandleAsync(
            new CreerVersionGrilleTarifaireCommande(intrus, "v2.0", DateTime.UtcNow, ParametresTarification.CreerDefautAnnexeA(), UneLigne())));
    }

    [Fact]
    public async Task CreerVersionGrille_utilisateur_habilite_reussit()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var administrateur = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(administrateur, Permission.TarificationAdministrer));
        await db.SaveChangesAsync();

        var handler = new CreerVersionGrilleTarifaireCommandHandler(
            new VersionGrilleTarifaireRepository(db), new AutorisationService(db));

        var id = await handler.HandleAsync(
            new CreerVersionGrilleTarifaireCommande(administrateur, "v2.0", DateTime.UtcNow, ParametresTarification.CreerDefautAnnexeA(), UneLigne()));

        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task ImporterCsv_utilisateur_sans_permission_est_refuse()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var handler = new ImporterGrilleTarifaireCsvCommandHandler(
            new VersionGrilleTarifaireRepository(db), new ImportateurGrilleTarifaireCsv(), new AutorisationService(db));
        var intrus = Guid.NewGuid();

        var csv = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            "Sens;Taille;Categorie;Acconage;Relevage;BranchementJour\nImportLocal;20;C1;66800;25000;35000\n"));

        await Assert.ThrowsAsync<AccesRefuseException>(() => handler.HandleAsync(
            new ImporterGrilleTarifaireCsvCommande(intrus, "v3.0", DateTime.UtcNow, ParametresTarification.CreerDefautAnnexeA(), csv)));
    }

    [Fact]
    public async Task ConsulterVersionsGrilles_utilisateur_sans_permission_est_refuse()
    {
        using var db = ContexteDeTest.CreerNouveauContexte();
        var handler = new ConsulterVersionsGrillesTarifairesQueryHandler(
            new VersionGrilleTarifaireRepository(db), new AutorisationService(db));
        var intrus = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() => handler.HandleAsync(
            new ConsulterVersionsGrillesTarifairesRequete(intrus)));
    }
}
