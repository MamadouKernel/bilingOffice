using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Tests.Partages;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>
/// [TEST-SPEC — RG-SLA-01] LA CATÉGORIE DE CLIENT EST UN AXE DE PARAMÉTRAGE EFFECTIF.
///
/// DÉFAUT CORRIGÉ (audit final, <c>docs/audit-final-conformite-98RG.md</c>) : le CDC énumère six
/// axes — « module, type de demande, étape, file, priorité, <b>catégorie de client</b> ». Les cinq
/// premiers étaient portés ; le sixième n'existait ni dans le modèle, ni dans le moteur de
/// sélection. Aucun SLA ne pouvait être différencié par profil client.
///
/// CES TESTS PASSENT PAR LA RÉSOLUTION RÉELLE (<c>ParametresSlaProvider</c>), pas par la seule
/// présence de la colonne : un axe qui ne participe pas au choix n'est pas un axe.
/// </summary>
public sealed class AxeCategorieClientSlaTests
{
    private sealed record Contexte(
        PlateformeDbContext Db,
        GererParametresSlaCommand GererSla,
        ParametresSlaProvider Provider,
        Guid AdministrateurId);

    private static async Task<Contexte> ConstruireAsync()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var administrateurId = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            ComptesDeTest.CompteActif(db, administrateurId), Permission.AdministrationSla));
        await db.SaveChangesAsync();

        var gererSla = new GererParametresSlaCommand(
            new ParametreSlaRepository(db), new FileDeTraitementRepository(db),
            new PrioriteRepository(db), new AutorisationService(db),
            new JournalisationService(db, horloge), new UniteDeTravail(db));

        return new Contexte(db, gererSla, new ParametresSlaProvider(db), administrateurId);
    }

    /// <summary>Crée un dossier portant l'instantané de profil demandé, et son ticket.</summary>
    private static Ticket TicketDeProfil(PlateformeDbContext db, TypeClient profil, string suffixe)
    {
        var dossier = Dossier.Creer(
            $"DOS-SLA-{suffixe}", Guid.NewGuid(), DateTime.UtcNow, Guid.NewGuid(), profil);
        db.Dossiers.Add(dossier);

        var ticket = Ticket.Creer(
            $"TCK-SLA-{suffixe}", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }

    /// <summary>
    /// LE CŒUR DE LA RÈGLE : deux tickets identiques en tout — même module, même type de demande,
    /// même file, même priorité — obtiennent des durées cibles DIFFÉRENTES du seul fait de leur
    /// catégorie de client.
    /// </summary>
    [Fact]
    public async Task Changer_la_categorie_de_client_selectionne_un_sla_different()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;

        // Un SLA large (toutes catégories) et un SLA serré pour les clients IES.
        await c.GererSla.DefinirAsync(
            c.AdministrateurId, "BillingOffice", "Import", TimeSpan.FromHours(8), 0.5, 0.8);
        await c.GererSla.DefinirAsync(
            c.AdministrateurId, "BillingOffice", "Import", TimeSpan.FromHours(2), 0.5, 0.8,
            typeClient: TypeClient.IES);

        var ticketIes = TicketDeProfil(c.Db, TypeClient.IES, "IES01");
        var ticketNormal = TicketDeProfil(c.Db, TypeClient.Normal, "NRM01");

        var slaIes = await c.Provider.ObtenirPourAsync(ticketIes);
        var slaNormal = await c.Provider.ObtenirPourAsync(ticketNormal);

        Assert.NotNull(slaIes);
        Assert.NotNull(slaNormal);
        Assert.Equal(TimeSpan.FromHours(2), slaIes!.Value.Parametres.DureeCible);
        Assert.Equal(TimeSpan.FromHours(8), slaNormal!.Value.Parametres.DureeCible);
    }

    /// <summary>
    /// Un paramétrage ciblé sur une catégorie ne s'applique PAS aux autres. Sans ce refus, l'axe
    /// serait décoratif.
    /// </summary>
    [Fact]
    public async Task Un_sla_cible_sur_une_categorie_ne_s_applique_pas_aux_autres()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;

        await c.GererSla.DefinirAsync(
            c.AdministrateurId, "BillingOffice", "Import", TimeSpan.FromHours(2), 0.5, 0.8,
            typeClient: TypeClient.Deposit);

        var ticketNormal = TicketDeProfil(c.Db, TypeClient.Normal, "NRM02");

        // Aucun paramétrage applicable : le socle n'invente aucune durée (PC-12).
        Assert.Null(await c.Provider.ObtenirPourAsync(ticketNormal));
    }

    /// <summary>Un paramétrage sans catégorie reste applicable à tous, comme avant l'ouverture de l'axe.</summary>
    [Fact]
    public async Task Un_sla_sans_categorie_s_applique_a_toutes_les_categories()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;

        await c.GererSla.DefinirAsync(
            c.AdministrateurId, "BillingOffice", "Import", TimeSpan.FromHours(6), 0.5, 0.8);

        foreach (var profil in new[] { TypeClient.Normal, TypeClient.EnCompte, TypeClient.Deposit, TypeClient.IES })
        {
            var ticket = TicketDeProfil(c.Db, profil, $"ALL-{profil}");
            var sla = await c.Provider.ObtenirPourAsync(ticket);

            Assert.NotNull(sla);
            Assert.Equal(TimeSpan.FromHours(6), sla!.Value.Parametres.DureeCible);
        }
    }

    /// <summary>
    /// PRÉCÉDENCE : le CDC énumère les axes par portée décroissante — « étape, file, priorité,
    /// catégorie de client ». La file l'emporte donc sur la catégorie.
    /// </summary>
    [Fact]
    public async Task La_file_prime_sur_la_categorie_de_client()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;

        var ticket = TicketDeProfil(c.Db, TypeClient.IES, "PREC01");

        await c.GererSla.DefinirAsync(
            c.AdministrateurId, "BillingOffice", "Import", TimeSpan.FromHours(2), 0.5, 0.8,
            typeClient: TypeClient.IES);
        await c.GererSla.DefinirAsync(
            c.AdministrateurId, "BillingOffice", "Import", TimeSpan.FromHours(1), 0.5, 0.8,
            fileId: ticket.FileCouranteId);

        var sla = await c.Provider.ObtenirPourAsync(ticket);

        Assert.NotNull(sla);
        Assert.Equal(TimeSpan.FromHours(1), sla!.Value.Parametres.DureeCible);
    }

    /// <summary>
    /// L'axe lit l'INSTANTANÉ du dossier. Un retypage de la société ne doit pas déplacer le SLA
    /// d'un ticket en cours — même exigence que la non-rétroactivité de CPT-06 (RG-CPT-05).
    /// </summary>
    [Fact]
    public async Task L_axe_lit_l_instantane_du_dossier_et_non_le_profil_courant_de_la_societe()
    {
        var c = await ConstruireAsync();
        using var _ = c.Db;

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Societe SLA", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        c.Db.Societes.Add(societe);

        var dossier = Dossier.Creer("DOS-SLA-SNAP", societe.Id, DateTime.UtcNow, Guid.NewGuid(), TypeClient.Normal);
        c.Db.Dossiers.Add(dossier);
        var ticket = Ticket.Creer("TCK-SLA-SNAP", "BillingOffice", "Import", Guid.NewGuid(), dossier.Id, DateTime.UtcNow);
        c.Db.Tickets.Add(ticket);
        c.Db.SaveChanges();

        await c.GererSla.DefinirAsync(
            c.AdministrateurId, "BillingOffice", "Import", TimeSpan.FromHours(8), 0.5, 0.8,
            typeClient: TypeClient.Normal);
        await c.GererSla.DefinirAsync(
            c.AdministrateurId, "BillingOffice", "Import", TimeSpan.FromHours(2), 0.5, 0.8,
            typeClient: TypeClient.Deposit);

        // La société est retypée APRÈS la création du dossier.
        societe.ModifierType(TypeClient.Deposit);
        c.Db.SaveChanges();

        var sla = await c.Provider.ObtenirPourAsync(ticket);

        Assert.NotNull(sla);
        Assert.Equal(TimeSpan.FromHours(8), sla!.Value.Parametres.DureeCible);
    }
}
