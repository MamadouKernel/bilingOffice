using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Infrastructure.Persistence;

namespace CIT.Plateforme.Web.Tests.Support;

/// <summary>
/// Compose le contrôle d'accès central tel que le conteneur le monte en production.
///
/// Rassemblé ici pour la même raison que la centralisation qu'il sert (remédiation P2-01) : une
/// évolution de la composition du contrôle d'accès ne doit se répercuter qu'à un seul endroit.
/// L'ajout de <see cref="GardeIdentiteService"/> en P0-ter avait à lui seul touché onze sites de
/// construction recopiés à l'identique.
/// </summary>
public static class FabriqueSecurite
{
    public static ControleAccesRessourceService ControleAcces(PlateformeDbContext db) => new(
        new DossierRepository(db),
        new TicketRepository(db),
        new UtilisateurSocieteRepository(db),
        new HabilitationRepository(db),
        new PerimetreSupervisionRepository(db),
        new GardeIdentiteService(new UtilisateurInfoRepository(db)));
}
