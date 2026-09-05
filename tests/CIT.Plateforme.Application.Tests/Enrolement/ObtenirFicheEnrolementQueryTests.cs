using System;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

// FEATURE 008 (Enrôlement Transporteurs) — découverte à l'implémentation (cf. implementation-report.md
// "Écarts") : ObtenirFicheTicketQuery (F001/F006) exige un dossier.ClientId résolvable en
// SocieteCliente — toujours faux pour un ticket Enrolement (D1, personne physique). Une query
// dédiée, minimale, est nécessaire — mirroring ObtenirFicheTicketQuery sans la dépendance société.
public sealed class ObtenirFicheEnrolementQueryTests
{
    [Fact]
    public async Task Transporteur_proprietaire_consulte_sa_fiche()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var transporteurId = Guid.NewGuid();

        db.Users.Add(new ApplicationUser { Id = transporteurId, UserName = "transporteur@test.local", Email = "transporteur@test.local", NomAffiche = "Transporteur Test", Nature = NatureUtilisateur.Client, EstActif = true });
        var dossier = Dossier.Creer("DOS-ENR-000001", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, transporteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-ENR-000001", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        ticket.DefinirInformationsEnrolement("Enrôlement chauffeur", "Camion frigorifique.");
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var query = ConstruireQuery(db);

        var fiche = await query.ExecuterAsync(transporteurId, ticket.Id);

        Assert.Equal("TCK-ENR-000001", fiche.NumeroTicket);
        Assert.Equal("Enrôlement chauffeur", fiche.Objet);
        Assert.True(fiche.PeutDeposerDocument);
    }

    [Fact]
    public async Task Transporteur_non_proprietaire_est_refuse()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var proprietaireId = Guid.NewGuid();
        var intrusId = Guid.NewGuid();

        db.Users.Add(new ApplicationUser { Id = proprietaireId, UserName = "proprietaire@test.local", Email = "proprietaire@test.local", NomAffiche = "Propriétaire Test", Nature = NatureUtilisateur.Client, EstActif = true });
        db.Users.Add(new ApplicationUser { Id = intrusId, UserName = "intrus@test.local", Email = "intrus@test.local", NomAffiche = "Intrus Test", Nature = NatureUtilisateur.Client, EstActif = true });
        var dossier = Dossier.Creer("DOS-ENR-000002", proprietaireId, horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-ENR-000002", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var query = ConstruireQuery(db);

        await Assert.ThrowsAsync<AccesRefuseException>(() => query.ExecuterAsync(intrusId, ticket.Id));
    }

    [Fact]
    public async Task Agent_avec_EnrolementInstruire_peut_valider_et_convoquer_si_EnCours()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var transporteurId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var dossier = Dossier.Creer("DOS-ENR-000003", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, transporteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-ENR-000003", "Enrolement", "Enrolement", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        ticket.PrendreEnCharge(agentId, horloge.MaintenantUtc);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.EnrolementInstruire, ticket.FileCouranteId));
        await db.SaveChangesAsync();

        var query = ConstruireQuery(db);

        var fiche = await query.ExecuterAsync(agentId, ticket.Id);

        Assert.True(fiche.PeutValider);
        Assert.True(fiche.PeutDemanderComplement);
        Assert.True(fiche.PeutConvoquer);
        // research.md D6 : aucune séquence machine stricte n'est imposée entre valider/convoquer/
        // constater le badge (tous trois disponibles tant que EnCours) — le séquencement relève du
        // processus métier (Agent Gate), pas d'une garde de statut, cohérent avec le reste du socle.
        Assert.True(fiche.PeutConstaterBadge);
    }

    private static ObtenirFicheEnrolementQuery ConstruireQuery(PlateformeDbContext db)
    {
        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var historiqueRepo = new HistoriqueTicketRepository(db);
        var documentRepo = new DocumentRepository(db);
        var messageRepo = new MessageRepository(db);
        var habilitationRepo = new HabilitationRepository(db);
        var utilisateurInfoRepo = new UtilisateurInfoRepository(db);

        return new ObtenirFicheEnrolementQuery(
            ticketRepo, dossierRepo, fileRepo, historiqueRepo, documentRepo, messageRepo, habilitationRepo, utilisateurInfoRepo,
            Support.FabriqueRequetes.ControleAcces(db));
    }
}
