using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;

namespace CIT.Plateforme.Tests.Partages;

/// <summary>
/// Garantit qu'un identifiant porteur d'une habilitation, d'un rattachement ou d'un dossier
/// personnel correspond à un compte RÉEL et ACTIF.
///
/// POURQUOI CE HELPER EXISTE — REMÉDIATION P0-ter.
/// Jusqu'ici, une fixture accordait une permission à un <c>Guid.NewGuid()</c> sans jamais créer le
/// compte associé. Cela fonctionnait parce que <c>AutorisationService</c> ne consultait que la table
/// des habilitations : une permission suffisait, l'identité ne comptait pas. C'est très exactement
/// le défaut corrigé — une habilitation ne rattache plus rien si aucun compte ne la porte.
///
/// La correction rend donc ces fixtures incomplètes, non fausses : en production, une habilitation
/// ne peut pas exister sans compte, <c>CreerCompteAgentCommand</c> étant le seul chemin qui en crée.
/// Ce helper rétablit cette vérité dans les tests, sans rien affaiblir : le compte créé est ACTIF,
/// donc les scénarios continuent d'éprouver ce qu'ils éprouvaient.
///
/// POURQUOI IL EST PARTAGÉ — REMÉDIATION P3.
/// Il existait en trois copies identiques, une par projet de test. Un helper qui décide ce qu'est
/// « un compte utilisable » ne peut pas vivre en trois exemplaires : c'est le motif même que les
/// remédiations de sécurité ont passé leur temps à défaire dans le code de production.
///
/// Il est idempotent : plusieurs habilitations pour un même agent ne créent qu'un compte.
/// </summary>
public static class ComptesDeTest
{
    /// <summary>Renvoie l'identifiant reçu, après s'être assuré qu'un compte actif le porte.</summary>
    public static Guid CompteActif(
        PlateformeDbContext db, Guid utilisateurId, NatureUtilisateur nature = NatureUtilisateur.Agent)
    {
        if (utilisateurId == Guid.Empty)
        {
            return utilisateurId;
        }

        var dejaPresent = db.Users.Local.Any(u => u.Id == utilisateurId)
            || db.Users.Any(u => u.Id == utilisateurId);
        if (dejaPresent)
        {
            return utilisateurId;
        }

        db.Users.Add(new ApplicationUser
        {
            Id = utilisateurId,
            UserName = $"{utilisateurId:N}@fixture.test",
            Email = $"{utilisateurId:N}@fixture.test",
            NomAffiche = "Agent de fixture",
            Nature = nature,
            EstActif = true,
        });

        return utilisateurId;
    }
}
