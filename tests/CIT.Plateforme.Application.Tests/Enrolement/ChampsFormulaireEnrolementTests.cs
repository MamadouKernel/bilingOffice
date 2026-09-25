using System;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Enrolement;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Enrolement;

/// <summary>
/// ENR-01 (remédiation) — « la liste des informations et pièces est paramétrable ». Ces tests
/// établissent que : (1) tant que le référentiel des champs est vide, la soumission reste celle
/// d'hier (objet/description libres) ; (2) dès qu'un champ actif est obligatoire, son absence
/// rejette la soumission ; (3) les valeurs soumises sont persistées et relisibles sur la fiche.
/// </summary>
public sealed class ChampsFormulaireEnrolementTests
{
    private static (PlateformeDbContext Db, SoumettreEnrolementCommand Soumettre, ObtenirFicheEnrolementQuery ObtenirFiche, Guid TransporteurId) Preparer()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(db, horloge);
        var uow = new UniteDeTravail(db);
        var numerotation = new NumerotationEnMemoirePourTests();
        var autorisation = new AutorisationService(db);
        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var fileRepo = new FileDeTraitementRepository(db);
        var champsFormulaireRepo = new ChampFormulaireEnrolementRepository(db);
        var valeursChampRepo = new ValeurChampEnrolementRepository(db);
        var habilitationRepo = new HabilitationRepository(db);
        var utilisateurInfoRepo = new UtilisateurInfoRepository(db);
        var historiqueRepo = new HistoriqueTicketRepository(db);
        var documentRepo = new DocumentRepository(db);
        var messageRepo = new MessageRepository(db);

        db.Files.Add(FileDeTraitement.Creer("Enrolement", "Agent Gate"));

        var transporteurId = Guid.NewGuid();
        CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, transporteurId, NatureUtilisateur.Client);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(transporteurId, Permission.EnrolementSoumettre));
        db.SaveChanges();

        var soumettre = new SoumettreEnrolementCommand(
            ticketRepo, dossierRepo, fileRepo, champsFormulaireRepo, valeursChampRepo, numerotation, journalisation, horloge, autorisation, uow);

        var obtenirFiche = new ObtenirFicheEnrolementQuery(
            ticketRepo, dossierRepo, fileRepo, historiqueRepo, documentRepo, messageRepo, habilitationRepo, utilisateurInfoRepo,
            valeursChampRepo, champsFormulaireRepo, Support.FabriqueRequetes.ControleAcces(db));

        return (db, soumettre, obtenirFiche, transporteurId);
    }

    [Fact]
    public async Task ReferentielVide_SoumissionResteLibreEtReussit()
    {
        var (_, soumettre, _, transporteurId) = Preparer();

        var resultat = await soumettre.ExecuterAsync(transporteurId, new SoumettreEnrolementRequete("Enrôlement chauffeur", "Camion frigorifique."));

        Assert.NotEmpty(resultat.NumeroTicket);
    }

    [Fact]
    public async Task ChampObligatoireManquant_RejetteAvecMessageClair()
    {
        var (db, soumettre, _, transporteurId) = Preparer();

        db.ChampsFormulaireEnrolement.Add(CIT.Plateforme.Domain.Enrolement.ChampFormulaireEnrolement.Definir("immatriculation", "Immatriculation", estObligatoire: true, ordre: 1));
        db.ChampsFormulaireEnrolement.Add(CIT.Plateforme.Domain.Enrolement.ChampFormulaireEnrolement.Definir("type_vehicule", "Type de véhicule", estObligatoire: false, ordre: 2));
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            soumettre.ExecuterAsync(transporteurId, new SoumettreEnrolementRequete("Enrôlement chauffeur", "Camion frigorifique.")));

        Assert.Contains("Immatriculation", ex.Message);
    }

    [Fact]
    public async Task Lot4_ChampObligatoireTypeCamion_NeBloquePasLAutoInscriptionChauffeur()
    {
        var (db, soumettre, _, transporteurId) = Preparer();

        // Un champ obligatoire réservé au CAMION ne doit pas s'appliquer à l'auto-inscription
        // individuelle (personne physique = chauffeur, research.md D1).
        db.ChampsFormulaireEnrolement.Add(CIT.Plateforme.Domain.Enrolement.ChampFormulaireEnrolement.Definir(
            "carte_grise", "N° carte grise", estObligatoire: true, ordre: 1, CIT.Plateforme.Domain.Enrolement.TypeObjetEnrolement.Camion));
        await db.SaveChangesAsync();

        var resultat = await soumettre.ExecuterAsync(transporteurId, new SoumettreEnrolementRequete("Enrôlement chauffeur", "Sans camion."));

        Assert.NotEmpty(resultat.NumeroTicket);
    }

    [Fact]
    public async Task Lot4_ChampObligatoireTypeChauffeur_BloqueLAutoInscriptionChauffeur()
    {
        var (db, soumettre, _, transporteurId) = Preparer();

        db.ChampsFormulaireEnrolement.Add(CIT.Plateforme.Domain.Enrolement.ChampFormulaireEnrolement.Definir(
            "num_permis", "Numéro de permis", estObligatoire: true, ordre: 1, CIT.Plateforme.Domain.Enrolement.TypeObjetEnrolement.Chauffeur));
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            soumettre.ExecuterAsync(transporteurId, new SoumettreEnrolementRequete("Enrôlement chauffeur", "Sans permis.")));

        Assert.Contains("permis", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TousLesChampsFournis_SoumissionReussitEtValeursPersistees()
    {
        var (db, soumettre, obtenirFiche, transporteurId) = Preparer();

        db.ChampsFormulaireEnrolement.Add(CIT.Plateforme.Domain.Enrolement.ChampFormulaireEnrolement.Definir("immatriculation", "Immatriculation", estObligatoire: true, ordre: 1));
        db.ChampsFormulaireEnrolement.Add(CIT.Plateforme.Domain.Enrolement.ChampFormulaireEnrolement.Definir("type_vehicule", "Type de véhicule", estObligatoire: false, ordre: 2));
        await db.SaveChangesAsync();

        var requete = new SoumettreEnrolementRequete(
            "Enrôlement chauffeur",
            "Camion frigorifique.",
            [
                new ValeurChampEnrolementSoumission("immatriculation", "CI-1234-AB"),
                new ValeurChampEnrolementSoumission("type_vehicule", "Camion frigorifique"),
            ]);

        var resultat = await soumettre.ExecuterAsync(transporteurId, requete);

        var fiche = await obtenirFiche.ExecuterAsync(transporteurId, resultat.TicketId);

        Assert.Equal(2, fiche.ChampsRenseignes.Count);
        Assert.Contains(fiche.ChampsRenseignes, c => c.ChampCode == "immatriculation" && c.Valeur == "CI-1234-AB" && c.Libelle == "Immatriculation");
        Assert.Contains(fiche.ChampsRenseignes, c => c.ChampCode == "type_vehicule" && c.Valeur == "Camion frigorifique");
    }
}
