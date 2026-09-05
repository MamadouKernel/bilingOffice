using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Comptes;

/// <summary>
/// ENR-01 (Must) — « Le chauffeur ou le transporteur renseigne À DISTANCE les informations
/// nécessaires à son enrôlement et dépose les pièces demandées. »
///
/// Le CDC fait du transporteur un profil à part entière (§ 6) et prévoit un « compte
/// transporteur/chauffeur » dans son modèle de données. Aucun parcours ne le produisait : les
/// trois portes existantes mènent toutes à une société. Le module Enrôlement n'avait donc aucun
/// usager possible.
///
/// Ce parcours étant PUBLIC — l'intéressé n'a pas encore de compte — la garde ne peut pas être
/// une autorisation. Elle porte sur la nature du compte visé : cette suite vérifie surtout qu'on
/// ne peut pas s'en servir pour élargir les droits d'un compte existant.
/// </summary>
public sealed class InscrireTransporteurCommandTests
{
    private static readonly DateTime Maintenant = new(2026, 9, 2, 8, 0, 0, DateTimeKind.Utc);

    private static InscrireTransporteurCommand Construire(PlateformeDbContext dbContext) =>
        new(new HabilitationRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext),
            new SocieteClienteRepository(dbContext),
            new JournalisationService(dbContext, HorlogeDeTest.Defaut()),
            new UniteDeTravail(dbContext));

    private static SocieteCliente AjouterSociete(PlateformeDbContext dbContext, Guid demandeurId, bool validee)
    {
        var societe = SocieteCliente.Soumettre(demandeurId, "Entreprise", TypeClient.Normal, Maintenant);
        if (validee)
        {
            societe.Valider();
        }

        dbContext.Societes.Add(societe);
        dbContext.SaveChanges();
        return societe;
    }

    [Fact]
    public async Task Un_transporteur_recoit_le_droit_de_soumettre_un_enrolement()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var transporteur = Guid.NewGuid();

        await Construire(dbContext).ExecuterAsync(transporteur);

        var habilitation = Assert.Single(dbContext.Habilitations.Where(h => h.UtilisateurId == transporteur));
        Assert.Equal(Permission.EnrolementSoumettre, habilitation.PermissionCode);
        Assert.Null(habilitation.FileId);
    }

    /// <summary>
    /// Le droit accordé est le SEUL nécessaire pour soumettre un dossier. Une inscription
    /// publique ne doit jamais ouvrir davantage.
    /// </summary>
    [Fact]
    public async Task L_inscription_n_accorde_aucun_autre_droit()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var transporteur = Guid.NewGuid();

        await Construire(dbContext).ExecuterAsync(transporteur);

        var accordees = dbContext.Habilitations
            .Where(h => h.UtilisateurId == transporteur)
            .Select(h => h.PermissionCode)
            .ToList();

        Assert.Equal([Permission.EnrolementSoumettre], accordees);
    }

    /// <summary>
    /// LA GARDE QUI COMPTE : un compte déjà rattaché à une société relève du parcours client.
    /// Sans ce refus, la page publique deviendrait un moyen d'élargir les droits d'un compte
    /// existant en contournant l'administration des habilitations.
    /// </summary>
    [Fact]
    public async Task Un_compte_rattache_a_une_societe_est_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var utilisateur = Guid.NewGuid();
        var societe = AjouterSociete(dbContext, Guid.NewGuid(), validee: true);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, utilisateur, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, Maintenant));
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Construire(dbContext).ExecuterAsync(utilisateur));

        Assert.Empty(dbContext.Habilitations.Where(h => h.UtilisateurId == utilisateur));
    }

    /// <summary>
    /// Un demandeur dont le dossier société n'est pas tranché ne doit pas contourner CPT-01 en
    /// se déclarant transporteur : cela lui rendrait un accès que la validation lui refuse.
    /// </summary>
    [Fact]
    public async Task Un_demandeur_de_compte_societe_est_refuse()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var demandeur = Guid.NewGuid();
        AjouterSociete(dbContext, demandeur, validee: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Construire(dbContext).ExecuterAsync(demandeur));

        Assert.Empty(dbContext.Habilitations.Where(h => h.UtilisateurId == demandeur));
    }

    [Fact]
    public async Task Une_reprise_du_parcours_ne_duplique_pas_l_habilitation()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var transporteur = Guid.NewGuid();
        var commande = Construire(dbContext);

        await commande.ExecuterAsync(transporteur);
        await commande.ExecuterAsync(transporteur);

        Assert.Single(dbContext.Habilitations.Where(h => h.UtilisateurId == transporteur));
    }

    /// <summary>ENT-04 et Principe IX : l'ouverture d'un accès laisse une trace.</summary>
    [Fact]
    public async Task L_inscription_est_journalisee()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var transporteur = Guid.NewGuid();

        await Construire(dbContext).ExecuterAsync(transporteur);

        Assert.Contains(dbContext.JournalAudit, e => e.Action == "TransporteurInscrit");
    }
}
