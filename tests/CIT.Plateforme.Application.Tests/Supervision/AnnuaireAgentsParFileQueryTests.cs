using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Domain.Affectation;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

/// <summary>Annuaire des agents regroupés par rubrique (file), borné au périmètre.</summary>
public sealed class AnnuaireAgentsParFileQueryTests
{
    [Fact]
    public async Task Regroupe_les_agents_par_file_et_exclut_les_files_hors_carte()
    {
        var superviseur = Guid.NewGuid();
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();
        var fileGate = FileDeTraitement.Creer("Enrolement", "Agent Gate");       // dans la carte
        var fileInconnue = FileDeTraitement.Creer("Enrolement", "File ad hoc");  // hors carte -> exclue

        var autorisation = new Mock<IAutorisationService>();
        var perimetres = new Mock<IPerimetreSupervisionRepository>();
        perimetres.Setup(p => p.ObtenirParSuperviseurAsync(superviseur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PerimetreSupervision.Definir(superviseur, new[] { fileGate.Id, fileInconnue.Id }));
        var files = new Mock<IFileDeTraitementRepository>();
        files.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileDeTraitement> { fileGate, fileInconnue });

        var habilitations = new Mock<IHabilitationRepository>();
        habilitations.Setup(h => h.ListerUtilisateursAvecPermissionAsync(Permission.EnrolementInstruire, fileGate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid> { agent1, agent2 });

        var disponibilites = new Mock<IDisponibiliteRepository>();
        disponibilites.Setup(d => d.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Disponibilite>()); // aucun enregistrement -> Disponible par défaut

        var utilisateurs = new Mock<IUtilisateurInfoRepository>();
        utilisateurs.Setup(u => u.ObtenirInfosAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UtilisateurInfo>
            {
                [agent1] = new(agent1, "a1", "a1@cit.ci", "Agent Un", true, NatureUtilisateur.Agent),
                [agent2] = new(agent2, "a2", "a2@cit.ci", "Agent Deux", true, NatureUtilisateur.Agent),
            });

        var query = new AnnuaireAgentsParFileQuery(
            files.Object, habilitations.Object, disponibilites.Object, utilisateurs.Object, perimetres.Object, autorisation.Object);

        var res = await query.ExecuterAsync(superviseur);

        autorisation.Verify(a => a.ExigerAsync(superviseur, Permission.SupervisionConsulter, null, It.IsAny<CancellationToken>()), Times.Once);
        var rubrique = Assert.Single(res); // "File ad hoc" exclue (hors carte)
        Assert.Equal("Agent Gate", rubrique.NomFile);
        Assert.Equal(2, rubrique.Agents.Count);
        Assert.All(rubrique.Agents, a => Assert.Equal(StatutDisponibilite.Disponible, a.Disponibilite));
        Assert.Contains(rubrique.Agents, a => a.Nom == "Agent Un");
    }

    [Fact]
    public async Task Sans_perimetre_retourne_vide()
    {
        var superviseur = Guid.NewGuid();
        var perimetres = new Mock<IPerimetreSupervisionRepository>();
        perimetres.Setup(p => p.ObtenirParSuperviseurAsync(superviseur, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerimetreSupervision?)null);

        var query = new AnnuaireAgentsParFileQuery(
            new Mock<IFileDeTraitementRepository>().Object, new Mock<IHabilitationRepository>().Object,
            new Mock<IDisponibiliteRepository>().Object, new Mock<IUtilisateurInfoRepository>().Object,
            perimetres.Object, new Mock<IAutorisationService>().Object);

        Assert.Empty(await query.ExecuterAsync(superviseur));
    }
}
