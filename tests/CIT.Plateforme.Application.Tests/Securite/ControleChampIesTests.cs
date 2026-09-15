using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Tests.Partages;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// [SÉCURITÉ — RG-SEC-01] LE CONTRÔLE AU NIVEAU CHAMP EST RÉELLEMENT APPLIQUÉ.
///
/// DÉFAUT CORRIGÉ (audit final, <c>docs/audit-final-conformite-98RG.md</c>) :
/// <c>AutorisationChampService</c> portait la matrice de droits du CDC (IMP-40/IMP-41), était
/// enregistré dans le conteneur, et <b>n'avait aucun appelant en production</b>. Les deux suites
/// qui le couvraient — <c>AutorisationChampServiceTests</c>, <c>SecCert02RbacChampTests</c> —
/// l'instanciaient directement : elles éprouvaient la matrice, jamais son effet.
///
/// CETTE SUITE N'INSTANCIE JAMAIS <c>AutorisationChampService</c>. Elle passe par la requête et la
/// commande réellement câblées derrière l'écran <c>FileIes.razor</c>, seule façon de démontrer que
/// la règle atteint l'utilisateur.
/// </summary>
public sealed class ControleChampIesTests
{
    private sealed record Contexte(
        PlateformeDbContext Db,
        ConsulterSuivisBranchesIesQuery Consulter,
        InstruireBrancheIesCommand Instruire,
        Guid TicketId,
        Guid FileIesId);

    private static Contexte Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var autorisation = new AutorisationService(db);

        var fileIes = FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, NomsFiles.FileIes);
        db.Files.Add(fileIes);

        var dossier = Dossier.Creer("DOS-IES-0001", Guid.NewGuid(), DateTime.UtcNow);
        db.Dossiers.Add(dossier);

        var ticket = Ticket.Creer(
            "TCK-IES-0001", SoumettreDemandeImportCommand.Module, "STANDARD",
            fileIes.Id, dossier.Id, DateTime.UtcNow, "BL-IES-1");
        db.Tickets.Add(ticket);

        db.SuivisBranchesIes.AddRange(
            SuiviBrancheIes.Creer(ticket.Id, BrancheIes.Manifeste),
            SuiviBrancheIes.Creer(ticket.Id, BrancheIes.ControleDocumentaire));
        db.SaveChanges();

        var consulter = new ConsulterSuivisBranchesIesQuery(
            new TicketRepository(db), new SuiviBrancheIesRepository(db),
            autorisation, FabriqueRequetes.PorteeChampsIes(db));

        var instruire = new InstruireBrancheIesCommand(
            new TicketRepository(db), new SuiviBrancheIesRepository(db),
            autorisation, FabriqueRequetes.PorteeChampsIes(db),
            new JournalisationService(db, horloge), horloge, new UniteDeTravail(db));

        return new Contexte(db, consulter, instruire, ticket.Id, fileIes.Id);
    }

    /// <summary>Crée un compte ACTIF et lui attribue les permissions demandées, bornées à la file.</summary>
    private static Guid Agent(PlateformeDbContext db, Guid fileId, params string[] permissions)
    {
        var id = ComptesDeTest.CompteActif(db, Guid.NewGuid(), NatureUtilisateur.Agent);
        foreach (var permission in permissions)
        {
            db.Habilitations.Add(HabilitationUtilisateur.Attribuer(id, permission, fileId));
        }

        db.SaveChanges();
        return id;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Rôle A — Pilote Manifeste
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Le pilote Manifeste écrit son bloc, et l'écran lui offre l'action correspondante.</summary>
    [Fact]
    public async Task Pilote_manifeste_peut_ecrire_son_bloc()
    {
        var c = Construire();
        using var _ = c.Db;
        var agent = Agent(c.Db, c.FileIesId, Permission.BillingImportIesManifesteInstruire);

        var vue = await c.Consulter.ExecuterAsync(agent, c.TicketId);

        Assert.Equal(EtatDroitChamp.AllowWrite, vue.Droits.Pour(BlocDonneesIes.Manifeste));
        Assert.True(vue.PeutInstruireManifeste);

        // Et l'action passe réellement.
        await c.Instruire.ExecuterAsync(agent, c.TicketId, BrancheIes.Manifeste);
        Assert.Contains(c.Db.SuivisBranchesIes, s => s.Branche == BrancheIes.Manifeste && s.EstTerminee);
    }

    /// <summary>
    /// Il ne peut PAS écrire le bloc réservé au Contrôle Documentaire — ni dans la vue, ni en action.
    /// C'est la moitié d'IMP-40 que le socle ne rendait pas : « chacun n'agissant que sur les
    /// champs/actions de son rôle ».
    /// </summary>
    [Fact]
    public async Task Pilote_manifeste_ne_peut_pas_ecrire_le_bloc_du_controle_documentaire()
    {
        var c = Construire();
        using var _ = c.Db;
        var agent = Agent(c.Db, c.FileIesId, Permission.BillingImportIesManifesteInstruire);

        var vue = await c.Consulter.ExecuterAsync(agent, c.TicketId);

        Assert.Equal(EtatDroitChamp.ReadOnly, vue.Droits.Pour(BlocDonneesIes.Douane));
        Assert.False(vue.PeutInstruireControleDocumentaire);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Instruire.ExecuterAsync(agent, c.TicketId, BrancheIes.ControleDocumentaire));
    }

    /// <summary>Le bloc financier lui est MASQUÉ — il ne le lit pas davantage qu'il ne l'écrit.</summary>
    [Fact]
    public async Task Pilote_manifeste_ne_voit_pas_le_bloc_financier()
    {
        var c = Construire();
        using var _ = c.Db;
        var agent = Agent(c.Db, c.FileIesId, Permission.BillingImportIesManifesteInstruire);

        var vue = await c.Consulter.ExecuterAsync(agent, c.TicketId);

        Assert.Equal(EtatDroitChamp.Masked, vue.Droits.Pour(BlocDonneesIes.Financier));
        Assert.False(vue.Droits.PeutLire(BlocDonneesIes.Financier));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Rôle B — Pilote Contrôle Documentaire (symétrique)
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pilote_controle_documentaire_peut_ecrire_son_bloc()
    {
        var c = Construire();
        using var _ = c.Db;
        var agent = Agent(c.Db, c.FileIesId, Permission.BillingImportIesControleDocumentaireInstruire);

        var vue = await c.Consulter.ExecuterAsync(agent, c.TicketId);

        Assert.Equal(EtatDroitChamp.AllowWrite, vue.Droits.Pour(BlocDonneesIes.Douane));
        Assert.True(vue.PeutInstruireControleDocumentaire);

        await c.Instruire.ExecuterAsync(agent, c.TicketId, BrancheIes.ControleDocumentaire);
        Assert.Contains(c.Db.SuivisBranchesIes, s => s.Branche == BrancheIes.ControleDocumentaire && s.EstTerminee);
    }

    [Fact]
    public async Task Pilote_controle_documentaire_ne_peut_pas_ecrire_le_bloc_manifeste()
    {
        var c = Construire();
        using var _ = c.Db;
        var agent = Agent(c.Db, c.FileIesId, Permission.BillingImportIesControleDocumentaireInstruire);

        var vue = await c.Consulter.ExecuterAsync(agent, c.TicketId);

        Assert.Equal(EtatDroitChamp.ReadOnly, vue.Droits.Pour(BlocDonneesIes.Manifeste));
        Assert.False(vue.PeutInstruireManifeste);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Instruire.ExecuterAsync(agent, c.TicketId, BrancheIes.Manifeste));
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Refus
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>Sans aucune habilitation IES, la consultation elle-même est refusée.</summary>
    [Fact]
    public async Task Utilisateur_sans_habilitation_est_refuse()
    {
        var c = Construire();
        using var _ = c.Db;
        var agent = Agent(c.Db, c.FileIesId);

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Consulter.ExecuterAsync(agent, c.TicketId));

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Instruire.ExecuterAsync(agent, c.TicketId, BrancheIes.Manifeste));
    }

    /// <summary>
    /// UN COMPTE DÉSACTIVÉ N'OBTIENT AUCUN DROIT DE CHAMP, même s'il détient encore l'habilitation.
    /// La garde d'identité précède la matrice — c'est la règle de P0-ter appliquée à cette surface.
    /// </summary>
    [Fact]
    public async Task Compte_desactive_n_obtient_aucun_droit_de_champ()
    {
        var c = Construire();
        using var _ = c.Db;
        var agent = Agent(c.Db, c.FileIesId, Permission.BillingImportIesManifesteInstruire);

        var compte = c.Db.Users.Single(u => u.Id == agent);
        compte.EstActif = false;
        c.Db.SaveChanges();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Consulter.ExecuterAsync(agent, c.TicketId));

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => c.Instruire.ExecuterAsync(agent, c.TicketId, BrancheIes.Manifeste));
    }

    /// <summary>
    /// La portée résolue seule, sans passer par une requête : un compte désactivé obtient
    /// <c>Aucun</c> — tous blocs masqués — et non pas les droits de ses habilitations.
    /// </summary>
    [Fact]
    public async Task Portee_d_un_compte_desactive_masque_tous_les_blocs()
    {
        var c = Construire();
        using var _ = c.Db;
        var agent = Agent(c.Db, c.FileIesId, Permission.BillingImportIesManifesteInstruire);
        var compte = c.Db.Users.Single(u => u.Id == agent);
        compte.EstActif = false;
        c.Db.SaveChanges();

        var droits = await FabriqueRequetes.PorteeChampsIes(c.Db)
            .ResoudreAsync(agent, c.FileIesId, ModuleIes.Import);

        Assert.True(droits.AucunAcces);
    }

    /// <summary>
    /// Un agent habilité sur les DEUX branches obtient l'union des droits, jamais moins que sur une
    /// seule. Sans cette composition, cumuler deux habilitations aurait dégradé les droits.
    /// </summary>
    [Fact]
    public async Task Agent_habilite_sur_les_deux_branches_obtient_l_union_des_droits()
    {
        var c = Construire();
        using var _ = c.Db;
        var agent = Agent(
            c.Db, c.FileIesId,
            Permission.BillingImportIesManifesteInstruire,
            Permission.BillingImportIesControleDocumentaireInstruire);

        var vue = await c.Consulter.ExecuterAsync(agent, c.TicketId);

        Assert.Equal(EtatDroitChamp.AllowWrite, vue.Droits.Pour(BlocDonneesIes.Manifeste));
        Assert.Equal(EtatDroitChamp.AllowWrite, vue.Droits.Pour(BlocDonneesIes.Douane));
        Assert.True(vue.PeutInstruireManifeste);
        Assert.True(vue.PeutInstruireControleDocumentaire);

        // Le bloc financier reste masqué : l'union ne fabrique pas de droit que la matrice refuse
        // aux deux rôles.
        Assert.Equal(EtatDroitChamp.Masked, vue.Droits.Pour(BlocDonneesIes.Financier));
    }
}
