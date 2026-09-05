using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>
/// [TEST-SPEC] SLA-01 / SLA-02 — axes de paramétrage et seuil d'escalade.
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-03, <c>docs/88-audit-conformite-cdc.md</c>) : le CDC exige un
/// paramétrage « par module, type de demande, <b>étape, file, priorité</b> » et <b>quatre</b>
/// seuils, dont l'escalade. <c>ParametreSlaAdministrable</c> ne portait que module et type de
/// demande, et deux seuils. La matrice <c>docs/33</c> déclarait pourtant SLA-01 et SLA-02
/// <c>CERTIFIED</c>.
///
/// L'ÉTAPE EST MATÉRIALISÉE PAR LA FILE : le MOT décrit chaque étape du parcours comme une file.
/// C'est le seul rattachement qui n'invente pas de notion absente du modèle.
///
/// LE TEST QUI COMPTE LE PLUS est celui de la rétrocompatibilité : un paramétrage existant, qui ne
/// discrimine sur aucun axe, doit continuer de s'appliquer. Sans cela, la correction rendrait
/// muets tous les SLA déjà administrés en production.
/// </summary>
public sealed class ParametresSlaAxesTests
{
    private static readonly Guid FileControleBl = Guid.NewGuid();
    private static readonly Guid FileFacturation = Guid.NewGuid();
    private static readonly Guid PrioriteHaute = Guid.NewGuid();

    private static Ticket TicketSur(Guid fileId, Guid? prioriteId = null)
    {
        var horloge = HorlogeDeTest.Defaut();
        var dossier = Dossier.Creer("DOS-SLAAXE-000001", Guid.NewGuid(), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-SLAAXE-000001", "BillingOffice", "Import", fileId, dossier.Id, horloge.MaintenantUtc);

        if (prioriteId is Guid p)
        {
            ticket.DefinirPriorite(p);
        }

        return ticket;
    }

    private static ParametreSlaAdministrable Parametre(
        TimeSpan cible, Guid? fileId = null, Guid? prioriteId = null)
    {
        var parametre = ParametreSlaAdministrable.Definir("BillingOffice", "Import", cible, 0.5, 0.8);
        parametre.Cibler(fileId, prioriteId);
        return parametre;
    }

    [Fact]
    public async Task Un_parametrage_sans_axe_reste_applicable()
    {
        // RÉTROCOMPATIBILITÉ : c'est la forme de tous les paramétrages administrés avant
        // l'ouverture des axes. Les rendre inopérants serait une régression silencieuse.
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        dbContext.ParametresSla.Add(Parametre(TimeSpan.FromHours(4)));
        await dbContext.SaveChangesAsync();

        var configuration = await new ParametresSlaProvider(dbContext).ObtenirPourAsync(TicketSur(FileControleBl));

        Assert.NotNull(configuration);
        Assert.Equal(TimeSpan.FromHours(4), configuration!.Value.Parametres.DureeCible);
    }

    [Fact]
    public async Task Le_parametrage_cible_sur_la_file_l_emporte_sur_le_parametrage_general()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        dbContext.ParametresSla.Add(Parametre(TimeSpan.FromHours(4)));
        dbContext.ParametresSla.Add(Parametre(TimeSpan.FromHours(1), fileId: FileControleBl));
        await dbContext.SaveChangesAsync();

        var configuration = await new ParametresSlaProvider(dbContext).ObtenirPourAsync(TicketSur(FileControleBl));

        Assert.Equal(TimeSpan.FromHours(1), configuration!.Value.Parametres.DureeCible);
    }

    [Fact]
    public async Task Un_parametrage_cible_sur_une_autre_file_ne_s_applique_pas()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        dbContext.ParametresSla.Add(Parametre(TimeSpan.FromHours(4)));
        dbContext.ParametresSla.Add(Parametre(TimeSpan.FromHours(1), fileId: FileFacturation));
        await dbContext.SaveChangesAsync();

        var configuration = await new ParametresSlaProvider(dbContext).ObtenirPourAsync(TicketSur(FileControleBl));

        Assert.Equal(TimeSpan.FromHours(4), configuration!.Value.Parametres.DureeCible);
    }

    [Fact]
    public async Task Le_parametrage_le_plus_specifique_l_emporte_file_et_priorite()
    {
        // Trois paramétrages applicables simultanément. Avant correction, `SingleOrDefaultAsync`
        // aurait levé une exception ici — la pluralité était impossible par construction.
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        dbContext.ParametresSla.Add(Parametre(TimeSpan.FromHours(4)));
        dbContext.ParametresSla.Add(Parametre(TimeSpan.FromHours(2), fileId: FileControleBl));
        dbContext.ParametresSla.Add(Parametre(TimeSpan.FromMinutes(30), fileId: FileControleBl, prioriteId: PrioriteHaute));
        await dbContext.SaveChangesAsync();

        var configuration = await new ParametresSlaProvider(dbContext)
            .ObtenirPourAsync(TicketSur(FileControleBl, PrioriteHaute));

        Assert.Equal(TimeSpan.FromMinutes(30), configuration!.Value.Parametres.DureeCible);
    }

    [Fact]
    public void La_specificite_etablit_un_ordre_total()
    {
        // Les poids sont des puissances de deux : aucune égalité n'est possible, la résolution
        // est donc déterministe. Un ordre partiel rendrait le SLA appliqué imprévisible.
        var general = Parametre(TimeSpan.FromHours(1));
        var surPriorite = Parametre(TimeSpan.FromHours(1), prioriteId: PrioriteHaute);
        var surFile = Parametre(TimeSpan.FromHours(1), fileId: FileControleBl);
        var surLesDeux = Parametre(TimeSpan.FromHours(1), fileId: FileControleBl, prioriteId: PrioriteHaute);

        var specificites = new[] { general.Specificite, surPriorite.Specificite, surFile.Specificite, surLesDeux.Specificite };

        Assert.Equal(specificites.Length, specificites.Distinct().Count());
        Assert.True(surLesDeux.Specificite > surFile.Specificite);
        Assert.True(surFile.Specificite > surPriorite.Specificite);
        Assert.True(surPriorite.Specificite > general.Specificite);
    }

    [Fact]
    public void Sans_seuil_d_escalade_administre_aucune_echeance_d_escalade_n_est_deduite()
    {
        // PC-12 : aucune valeur métier n'est inventée. Le dépassement est signalé, sans escalade.
        var parametre = Parametre(TimeSpan.FromHours(10));

        Assert.Null(parametre.VersParametresSla().EcheanceEscalade);
    }

    [Fact]
    public void Le_seuil_d_escalade_administre_produit_une_echeance_au_dela_du_depassement()
    {
        var parametre = Parametre(TimeSpan.FromHours(10));
        parametre.FixerSeuilEscalade(1.5);

        var sla = parametre.VersParametresSla();

        Assert.Equal(TimeSpan.FromHours(15), sla.EcheanceEscalade);
        Assert.True(sla.EcheanceEscalade > sla.DureeCible);
    }

    [Fact]
    public void Un_seuil_d_escalade_anterieur_au_depassement_est_refuse()
    {
        // SLA-02 place l'escalade « au-delà » du dépassement. Un seuil à 80 % la ferait précéder
        // l'alerte, ce qui viderait la notion de sens.
        var parametre = Parametre(TimeSpan.FromHours(10));

        Assert.Throws<ArgumentException>(() => parametre.FixerSeuilEscalade(0.8));
        Assert.Throws<ArgumentException>(() => parametre.FixerSeuilEscalade(1.0));
    }
}
