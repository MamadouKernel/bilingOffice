using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// [TEST-SPEC] §11.4 / §9.10 — libellés de statut paramétrables sans développement.
///
/// DÉFAUT CORRIGÉ : les statuts n'existaient que sous forme d'énumération. Renommer un libellé vu
/// par le client exigeait une recompilation, alors que le CDC les déclare paramétrables.
///
/// LE POINT LE PLUS IMPORTANT ICI est le second test : un approvisionnement qui réécrirait les
/// libellés à chaque démarrage annulerait silencieusement le paramétrage de l'administrateur — et
/// rendrait la fonctionnalité inutile tout en paraissant présente.
/// </summary>
public sealed class LibellesStatutParametrablesTests
{
    private static ProvisionnementLibellesStatut Provisionnement(PlateformeDbContext contexte) =>
        new(contexte, NullLogger<ProvisionnementLibellesStatut>.Instance);

    private static async Task ViderAsync(PlateformeDbContext contexte)
    {
        var existants = await contexte.LibellesStatutTicket.ToListAsync();
        contexte.LibellesStatutTicket.RemoveRange(existants);
        await contexte.SaveChangesAsync();
    }

    [Fact]
    public async Task Chaque_statut_du_cycle_de_vie_recoit_un_libelle()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        await ViderAsync(contexte);

        await Provisionnement(contexte).ExecuterAsync();

        var codes = await contexte.LibellesStatutTicket.Select(l => l.Code).ToListAsync();

        foreach (var statut in Enum.GetValues<StatutTicket>())
        {
            Assert.Contains(statut.ToString(), codes);
        }
    }

    [Fact]
    public async Task Un_libelle_modifie_n_est_jamais_reecrit_par_un_nouvel_approvisionnement()
    {
        // C'est la propriété qui rend la paramétrabilité réelle. Sans elle, l'administrateur
        // verrait son libellé revenir à la valeur d'origine au redémarrage suivant, sans
        // explication — le même piège que la réécriture de mot de passe par l'amorçage.
        await using var contexte = BaseDeTestSql.CreerContexte();
        await ViderAsync(contexte);
        await Provisionnement(contexte).ExecuterAsync();

        var code = StatutTicket.EnCours.ToString();
        var libelle = await contexte.LibellesStatutTicket.FirstAsync(l => l.Code == code);
        libelle.Modifier("Contrôle BL en cours", "BL check in progress", 25, estEngageant: false, actif: true);
        await contexte.SaveChangesAsync();

        await Provisionnement(contexte).ExecuterAsync();

        var relu = await contexte.LibellesStatutTicket.AsNoTracking().FirstAsync(l => l.Code == code);
        Assert.Equal("Contrôle BL en cours", relu.LibelleFr);
        Assert.Equal("BL check in progress", relu.LibelleEn);
        Assert.Equal(25, relu.Ordre);
    }

    [Fact]
    public async Task L_approvisionnement_est_idempotent()
    {
        await using var contexte = BaseDeTestSql.CreerContexte();
        await ViderAsync(contexte);

        await Provisionnement(contexte).ExecuterAsync();
        var apresPremier = await contexte.LibellesStatutTicket.CountAsync();

        await Provisionnement(contexte).ExecuterAsync();
        var apresSecond = await contexte.LibellesStatutTicket.CountAsync();

        Assert.Equal(apresPremier, apresSecond);
    }

    [Fact]
    public async Task Les_deux_langues_sont_renseignees()
    {
        // NFR-07 — interface disponible en anglais.
        await using var contexte = BaseDeTestSql.CreerContexte();
        await ViderAsync(contexte);
        await Provisionnement(contexte).ExecuterAsync();

        var libelles = await contexte.LibellesStatutTicket.AsNoTracking().ToListAsync();

        Assert.All(libelles, l => Assert.False(string.IsNullOrWhiteSpace(l.LibelleFr)));
        Assert.All(libelles, l => Assert.False(string.IsNullOrWhiteSpace(l.LibelleEn)));
    }

    [Fact]
    public async Task Les_statuts_engageants_sont_marques()
    {
        // CYC-05 — un état engageant interdit la suggestion de clôture pour absence de réponse.
        // Le caractère engageant est paramétrable, le CDC n'en donnant que des exemples.
        await using var contexte = BaseDeTestSql.CreerContexte();
        await ViderAsync(contexte);
        await Provisionnement(contexte).ExecuterAsync();

        var cloture = await contexte.LibellesStatutTicket.AsNoTracking()
            .FirstAsync(l => l.Code == StatutTicket.Cloture.ToString());
        var enCours = await contexte.LibellesStatutTicket.AsNoTracking()
            .FirstAsync(l => l.Code == StatutTicket.EnCours.ToString());

        Assert.True(cloture.EstEngageant);
        Assert.False(enCours.EstEngageant);
    }

    [Fact]
    public void Un_libelle_vide_est_refuse()
    {
        Assert.Throws<ArgumentException>(() =>
            LibelleStatutTicket.Creer("EnCours", "   ", "In progress", 10, false));

        var libelle = LibelleStatutTicket.Creer("EnCours", "En cours", "In progress", 10, false);
        Assert.Throws<ArgumentException>(() =>
            libelle.Modifier("En cours", "  ", 10, false, true));
    }
}
