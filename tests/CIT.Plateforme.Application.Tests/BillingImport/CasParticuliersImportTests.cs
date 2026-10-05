using CIT.Plateforme.Application.Administration;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.BillingImport;

/// <summary>
/// Cas particuliers d'une demande d'import (parcours client Billing Office, documents métier du
/// 05/10/2026) : marchandise dangereuse, enlèvement sous palan, véhicule usagé.
/// </summary>
public sealed class CasParticuliersImportTests
{
    private sealed record Contexte(SoumettreDemandeImportCommand Soumettre, Guid SocieteId, Guid AuteurId, Guid Standard, Guid Critique);

    private static async Task<Contexte> PreparerAsync(PlateformeDbContext dbContext, bool regleVersStandard = false)
    {
        var horloge = HorlogeDeTest.Defaut();
        var standard = NiveauPriorite.Definir("Standard", rang: 1);
        var critique = NiveauPriorite.Definir("Critique", rang: 100);
        dbContext.NiveauxPriorite.AddRange(standard, critique);

        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(dbContext, auteurId, NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        dbContext.Files.Add(FileDeTraitement.Creer(SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.FilePiloteManifeste));
        if (regleVersStandard)
        {
            dbContext.ReglesPrioriteAutomatique.Add(ReglePrioriteAutomatique.Definir(
                SoumettreDemandeImportCommand.Module, SoumettreDemandeImportCommand.TypeDemande, null, standard.Id, 1));
        }
        await dbContext.SaveChangesAsync();

        var soumettre = new SoumettreDemandeImportCommand(
            new TicketRepository(dbContext), new DossierRepository(dbContext), new SocieteClienteRepository(dbContext),
            new UtilisateurSocieteRepository(dbContext), new FileDeTraitementRepository(dbContext), new NumerotationEnMemoirePourTests(),
            new JournalisationService(dbContext, horloge), horloge, new UniteDeTravail(dbContext), new PrioriteRepository(dbContext),
            new ResoudrePrioriteAutomatiqueService(new ReglePrioriteAutomatiqueRepository(dbContext)));

        return new Contexte(soumettre, societe.Id, auteurId, standard.Id, critique.Id);
    }

    [Fact]
    public async Task Sans_cas_particulier_le_ticket_garde_la_priorite_par_defaut()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);

        var resultat = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(c.SocieteId, c.AuteurId, ["BL-CAS-000"]));

        var ticket = Assert.Single(resultat.TicketsCrees);
        Assert.Equal(CasParticulierImport.Aucun, ticket.CasParticuliers);
        Assert.Equal(c.Standard, ticket.PrioriteId);
    }

    [Fact]
    public async Task Les_cas_coches_sont_portes_par_chaque_ticket_et_traces_a_l_historique()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext);
        var cas = CasParticulierImport.MarchandiseDangereuse | CasParticulierImport.VehiculeUsage;

        var resultat = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(
            c.SocieteId, c.AuteurId, ["BL-CAS-001", "BL-CAS-002"], CasParticuliers: cas));

        Assert.Equal(2, resultat.TicketsCrees.Count);
        Assert.All(resultat.TicketsCrees, t => Assert.Equal(cas, t.CasParticuliers));
        Assert.All(resultat.TicketsCrees, t => Assert.Equal(c.Standard, t.PrioriteId));
        var details = dbContext.HistoriqueTickets.Where(h => h.Evenement == "TicketImportCree").Select(h => h.Detail).ToList();
        Assert.Equal(2, details.Count);
        Assert.All(details, d => Assert.Contains("cas=MarchandiseDangereuse, VehiculeUsage", d));
    }

    /// <summary>Sous palan = risque de réembarquement : le niveau le plus élevé du référentiel
    /// s'impose, même face à une règle automatique contraire.</summary>
    [Fact]
    public async Task L_enlevement_sous_palan_rend_le_ticket_critique_meme_avec_une_regle()
    {
        await using var dbContext = ContexteDeTest.CreerNouveauContexte();
        var c = await PreparerAsync(dbContext, regleVersStandard: true);

        var resultat = await c.Soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(
            c.SocieteId, c.AuteurId, ["BL-CAS-003"], CasParticuliers: CasParticulierImport.EnlevementSousPalan));

        Assert.Equal(c.Critique, Assert.Single(resultat.TicketsCrees).PrioriteId);
    }
}
