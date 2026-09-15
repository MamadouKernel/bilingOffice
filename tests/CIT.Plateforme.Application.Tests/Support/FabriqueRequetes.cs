using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Dossiers;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Moq;

namespace CIT.Plateforme.Application.Tests.Support;

/// <summary>
/// Construit les requêtes de fiche à partir d'un contexte de test.
///
/// Rassemblé ici plutôt que recopié dans chaque classe : ces requêtes ont beaucoup de
/// dépendances, et une évolution de leur composition — l'ajout du contrôle d'accès central en
/// remédiation P0-bis, par exemple — ne doit se répercuter qu'à un seul endroit.
/// </summary>
public static class FabriqueRequetes
{
    /// <summary>La garde d'identité seule — pour les surfaces bornées par l'appelant lui-même.</summary>
    public static GardeIdentiteService Garde(PlateformeDbContext db) =>
        new(new UtilisateurInfoRepository(db));

    /// <summary>
    /// RG-SEC-01 — la portée des champs IES, composée comme en production : garde d'identité,
    /// service d'autorisation réel, matrice de droits réelle. Aucun test ne doit instancier
    /// <c>AutorisationChampService</c> à la main pour éprouver le comportement produit : c'est
    /// exactement ce qui avait laissé la règle sans câblage.
    /// </summary>
    public static PorteeChampsIesService PorteeChampsIes(PlateformeDbContext db) => new(
        Garde(db),
        new AutorisationService(db),
        new AutorisationChampService());

    /// <summary>Le contrôle d'accès central, tel que le conteneur le compose en production.</summary>
    public static ControleAccesRessourceService ControleAcces(PlateformeDbContext db) => new(
        new DossierRepository(db),
        new TicketRepository(db),
        new UtilisateurSocieteRepository(db),
        new HabilitationRepository(db),
        new PerimetreSupervisionRepository(db),
        new GardeIdentiteService(new UtilisateurInfoRepository(db)));

    /// <summary>
    /// Contrôle d'accès adossé à des dépôts vides : il refuse tout. Réservé aux suites qui
    /// reçoivent <see cref="ControleAccesRessourceService"/> comme dépendance sans jamais
    /// l'exercer — celles qui n'éprouvent que la voie AGENT. Le nommer « inerte » évite qu'on le
    /// prenne un jour pour un contrôle permissif.
    /// </summary>
    public static ControleAccesRessourceService ControleAccesInerte() => new(
        Mock.Of<IDossierRepository>(),
        Mock.Of<ITicketRepository>(),
        Mock.Of<IUtilisateurSocieteRepository>(),
        Mock.Of<IHabilitationRepository>(),
        Mock.Of<IPerimetreSupervisionRepository>(),
        new GardeIdentiteService(Mock.Of<IUtilisateurInfoRepository>()));

    /// <summary>
    /// Crée un compte client ACTIF rattaché à la société donnée, avec visibilité sur toute la
    /// société, et renvoie son identifiant.
    ///
    /// Nécessaire depuis la remédiation P2-02 : les surfaces de tarification ne reçoivent plus de
    /// <c>SocieteId</c>, elles la résolvent depuis le rattachement de l'appelant. Un appelant
    /// anonyme (<c>Guid.NewGuid()</c>) n'a donc plus aucun périmètre — ce qui est le comportement
    /// voulu, et ce que ce helper permet aux fixtures d'exprimer correctement.
    /// </summary>
    public static Guid AppelantDeSociete(PlateformeDbContext db, Guid societeId, bool visibiliteTotale = true)
    {
        var compte = new CIT.Plateforme.Infrastructure.Identity.ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid():N}@societe.test",
            Email = $"{Guid.NewGuid():N}@societe.test",
            NomAffiche = "Client de société",
            Nature = CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client,
            EstActif = true,
        };
        db.Users.Add(compte);

        var rattachement = CIT.Plateforme.Domain.Comptes.UtilisateurSociete.Rattacher(
            compte.Id, societeId, DateTime.UtcNow, estAdministrateur: false, droitSoumission: true);
        if (visibiliteTotale)
        {
            rattachement.ModifierDroits(
                CIT.Plateforme.Domain.Comptes.DroitConsultation.ToutesDemandesSociete, droitSoumission: true);
        }

        db.UtilisateursSociete.Add(rattachement);
        db.SaveChanges();
        return compte.Id;
    }

    public static ObtenirFicheTicketQuery FicheTicket(PlateformeDbContext db) => new(
        new TicketRepository(db),
        new DossierRepository(db),
        new FileDeTraitementRepository(db),
        new SocieteClienteRepository(db),
        new UtilisateurInfoRepository(db),
        new HistoriqueTicketRepository(db),
        new DocumentRepository(db),
        new MessageRepository(db),
        new HabilitationRepository(db),
        new CategorieReclamationRepository(db),
        new LiaisonTicketRepository(db),
        ControleAcces(db));

    public static ObtenirFicheDossierQuery FicheDossier(PlateformeDbContext db) => new(
        new DossierRepository(db),
        new TicketRepository(db),
        new FileDeTraitementRepository(db),
        new SocieteClienteRepository(db),
        new UtilisateurInfoRepository(db),
        ControleAcces(db));
}
