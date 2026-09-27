using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>Identité structurée : unicité du RCCM à la soumission, correction administrative tracée.</summary>
public sealed class IdentiteSocieteCommandesTests
{
    private static SoumettreDemandeCompteCommand Soumission(PlateformeDbContext db)
    {
        var horloge = HorlogeDeTest.Defaut();
        return new SoumettreDemandeCompteCommand(
            new SocieteClienteRepository(db), new PieceExigeeCompteRepository(db),
            new JustificatifDemandeCompteRepository(db), new JournalisationService(db, horloge),
            horloge, new UniteDeTravail(db));
    }

    private static CorrigerIdentiteSocieteCommand Correction(PlateformeDbContext db) =>
        new(new SocieteClienteRepository(db), new AutorisationService(db),
            new JournalisationService(db, HorlogeDeTest.Defaut()), new UniteDeTravail(db));

    private static Guid AdministrateurHabilite(PlateformeDbContext db)
    {
        var id = Guid.NewGuid();
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, id), Permission.CompteTypeModifier));
        db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task La_soumission_structuree_enregistre_la_raison_sociale_et_le_rccm_normalise()
    {
        var db = ContexteDeTest.CreerNouveauContexte();

        var societe = await Soumission(db).ExecuterAsync(new SoumettreDemandeCompteRequete(
            Guid.NewGuid(), "DFE 2049583A", TypeClient.Normal, RaisonSociale: "ACME SARL", Rccm: "ci abj 2024 b 1"));

        Assert.Equal("ACME SARL", societe.RaisonSociale);
        Assert.Equal("CI-ABJ-2024-B-1", societe.Rccm);
    }

    [Fact]
    public async Task Un_rccm_deja_utilise_est_refuse_a_la_soumission()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        await Soumission(db).ExecuterAsync(new SoumettreDemandeCompteRequete(
            Guid.NewGuid(), string.Empty, TypeClient.Normal, RaisonSociale: "ACME SARL", Rccm: "CI-ABJ-2024-B-1"));

        await Assert.ThrowsAsync<RccmDejaUtiliseException>(() => Soumission(db).ExecuterAsync(new SoumettreDemandeCompteRequete(
            Guid.NewGuid(), string.Empty, TypeClient.Normal, RaisonSociale: "ACME BIS", Rccm: "CI-ABJ-2024-B-1")));
    }

    [Fact]
    public async Task La_correction_d_identite_est_appliquee_et_tracee()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var adminId = AdministrateurHabilite(db);
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "TRANSIT DU PORT SA", TypeClient.Normal, DateTime.UtcNow);
        db.Societes.Add(societe);
        db.SaveChanges();

        await Correction(db).ExecuterAsync(adminId, societe.Id, "TRANSIT DU PORT SA", "CI-ABJ-2020-B-777");

        var maj = db.Societes.Single(s => s.Id == societe.Id);
        Assert.Equal("CI-ABJ-2020-B-777", maj.Rccm);
        Assert.Contains(db.JournalAudit, e => e.Action == "IdentiteSocieteCorrigee" && e.EntiteId == societe.Id && e.AuteurId == adminId);
    }

    [Fact]
    public async Task La_correction_sans_habilitation_est_refusee()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "X", TypeClient.Normal, DateTime.UtcNow);
        db.Societes.Add(societe);
        db.SaveChanges();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            Correction(db).ExecuterAsync(Guid.NewGuid(), societe.Id, "X", "CI-ABJ-2020-B-1"));
        Assert.Null(db.Societes.Single(s => s.Id == societe.Id).Rccm);
    }
}
