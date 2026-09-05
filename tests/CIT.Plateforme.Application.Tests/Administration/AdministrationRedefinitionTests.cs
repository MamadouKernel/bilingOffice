using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Administration;

/// <summary>
/// [TEST-SPEC] CLAR-05 / § 17.3 — administrer, c'est aussi CORRIGER.
///
/// DÉFAUT COUVERT (2026-09-04) : <c>GererPerimetresSupervisionCommand</c> et
/// <c>GererMatriceTransitionsCommand</c> ne savaient qu'AJOUTER. Redéfinir créait une seconde
/// ligne, et les deux dépôts lisent par <c>SingleOrDefaultAsync</c> : la lecture levait ensuite à
/// chaque appel. Corriger un périmètre aurait donc rendu la supervision inaccessible au
/// superviseur, et corriger une transition aurait bloqué tout transfert entre les deux files.
///
/// Le défaut était latent parce qu'AUCUN écran n'appelait ces commandes. Les brancher sans le
/// corriger l'aurait rendu certain.
/// </summary>
public sealed class AdministrationRedefinitionTests
{
    [Fact]
    public async Task Redefinir_un_perimetre_remplace_les_files_sans_creer_de_second_perimetre()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var superviseurId = Guid.NewGuid();
        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, superviseurId), Permission.SupervisionConsulter));
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationPerimetresSupervision));

        var premiereFile = FileDeTraitement.Creer("BillingOffice", "Contrôle BL");
        var secondeFile = FileDeTraitement.Creer("CustomerComplaint", "Support Opérationnel");
        dbContext.Files.AddRange(premiereFile, secondeFile);
        await dbContext.SaveChangesAsync();

        var perimetres = new PerimetreSupervisionRepository(dbContext);
        var gererPerimetres = new GererPerimetresSupervisionCommand(perimetres, autorisation, journalisation, uniteDeTravail);

        await gererPerimetres.DefinirAsync(administrateurId, superviseurId, [premiereFile.Id]);
        await gererPerimetres.DefinirAsync(administrateurId, superviseurId, [secondeFile.Id]);

        // Une seule ligne : sans cela, ObtenirParSuperviseurAsync lèverait dès la lecture suivante.
        var tous = await perimetres.ListerAsync();
        Assert.Single(tous);

        var perimetre = await perimetres.ObtenirParSuperviseurAsync(superviseurId);
        Assert.NotNull(perimetre);
        Assert.Equal([secondeFile.Id], perimetre!.FilesCouvertes);

        // Et la supervision reste utilisable après la correction.
        var vueFiles = new VueFilesQuery(
            new FileDeTraitementRepository(dbContext), new TicketRepository(dbContext), perimetres, autorisation);
        var vues = await vueFiles.ExecuterAsync(superviseurId);

        Assert.Single(vues);
        Assert.Equal(secondeFile.Id, vues[0].FileId);
    }

    [Fact]
    public async Task Redefinir_une_transition_remplace_ses_motifs_sans_dedoubler_la_matrice()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationFiles));

        var source = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        var cible = FileDeTraitement.Creer("BillingImport", "Contrôle Documentaire");
        dbContext.Files.AddRange(source, cible);
        await dbContext.SaveChangesAsync();

        var transitions = new TransitionAutoriseeRepository(dbContext);
        var gererMatrice = new GererMatriceTransitionsCommand(
            transitions, new FileDeTraitementRepository(dbContext), autorisation, journalisation, uniteDeTravail);

        await gererMatrice.DefinirTransitionAsync(administrateurId, source.Id, cible.Id, ["erreur_orientation"]);
        await gererMatrice.DefinirTransitionAsync(administrateurId, source.Id, cible.Id, ["piece_manquante", "bl_illisible"]);

        var matrice = await transitions.ListerAsync();
        Assert.Single(matrice);
        Assert.Equal(["piece_manquante", "bl_illisible"], matrice[0].MotifsAutorises);

        // La lecture par couple de files, qui décide de tout transfert, reste possible.
        var relue = await transitions.ObtenirAsync(source.Id, cible.Id);
        Assert.NotNull(relue);
        Assert.False(relue!.AutoriseLeMotif("erreur_orientation"));
    }

    [Fact]
    public async Task Une_transition_ne_peut_pas_relier_une_file_a_elle_meme()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var administrateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, administrateurId), Permission.AdministrationFiles));

        var file = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        dbContext.Files.Add(file);
        await dbContext.SaveChangesAsync();

        var gererMatrice = new GererMatriceTransitionsCommand(
            new TransitionAutoriseeRepository(dbContext), new FileDeTraitementRepository(dbContext),
            autorisation, journalisation, uniteDeTravail);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            gererMatrice.DefinirTransitionAsync(administrateurId, file.Id, file.Id, []));
    }

    [Fact]
    public async Task Administrer_la_matrice_exige_l_habilitation()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);

        var source = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        var cible = FileDeTraitement.Creer("BillingImport", "Contrôle Documentaire");
        dbContext.Files.AddRange(source, cible);
        await dbContext.SaveChangesAsync();

        var gererMatrice = new GererMatriceTransitionsCommand(
            new TransitionAutoriseeRepository(dbContext), new FileDeTraitementRepository(dbContext),
            autorisation, journalisation, uniteDeTravail);

        var sansHabilitation = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() => gererMatrice.ListerAsync(sansHabilitation));
        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            gererMatrice.DefinirTransitionAsync(sansHabilitation, source.Id, cible.Id, []));
    }
}
