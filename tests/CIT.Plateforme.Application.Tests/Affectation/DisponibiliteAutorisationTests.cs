using CIT.Plateforme.Application.Affectation;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Affectation;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §6 — AUTORISATIONS MANQUANTES, point 3 de la
/// demande de remédiation) : avant correction, <c>GererDisponibiliteCommand</c> n'effectuait
/// aucun contrôle d'autorisation — n'importe quel appelant pouvait changer la disponibilité de
/// n'importe quel opérateur. L'auto-déclaration (un opérateur change sa propre disponibilité)
/// reste toujours possible (DIS-01) ; agir pour un tiers exige désormais
/// <see cref="Permission.DisponibiliteGerer"/> (DIS-02).
/// </summary>
public sealed class DisponibiliteAutorisationTests
{
    private static (PlateformeDbContext DbContext, GererDisponibiliteCommand Commande) ConstruireContexte()
    {
        var dbContext = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(dbContext, horloge);
        var uniteDeTravail = new UniteDeTravail(dbContext);
        var autorisation = new AutorisationService(dbContext);
        var commande = new GererDisponibiliteCommand(new DisponibiliteRepository(dbContext), autorisation, journalisation, horloge, uniteDeTravail);
        return (dbContext, commande);
    }

    [Fact]
    public async Task Un_operateur_peut_toujours_declarer_sa_propre_disponibilite()
    {
        var (_, commande) = ConstruireContexte();
        var operateurId = Guid.NewGuid();

        await commande.DefinirAsync(operateurId, operateurId, StatutDisponibilite.Indisponible); // ne doit pas lever
    }

    [Fact]
    public async Task Un_superviseur_habilite_peut_changer_la_disponibilite_d_un_tiers()
    {
        var (dbContext, commande) = ConstruireContexte();
        var superviseurId = Guid.NewGuid();
        var operateurId = Guid.NewGuid();
        dbContext.Habilitations.Add(HabilitationUtilisateur.Attribuer(superviseurId, Permission.DisponibiliteGerer));
        await dbContext.SaveChangesAsync();

        await commande.DefinirAsync(superviseurId, operateurId, StatutDisponibilite.Absent); // ne doit pas lever

        var disponibilite = await new DisponibiliteRepository(dbContext).ObtenirParOperateurAsync(operateurId);
        Assert.Equal(StatutDisponibilite.Absent, disponibilite!.Statut);
    }

    [Fact]
    public async Task Un_appelant_non_habilite_ne_peut_pas_changer_la_disponibilite_d_un_tiers()
    {
        var (_, commande) = ConstruireContexte();
        var appelantNonHabilite = Guid.NewGuid();
        var operateurCible = Guid.NewGuid();

        await Assert.ThrowsAsync<AccesRefuseException>(() =>
            commande.DefinirAsync(appelantNonHabilite, operateurCible, StatutDisponibilite.Indisponible));
    }
}
