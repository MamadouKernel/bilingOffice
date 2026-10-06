using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Supervision;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Supervision;
using CIT.Plateforme.Domain.Tickets;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Supervision;

public sealed class RapportActiviteOperateurQueryTests
{
    private static readonly DateTime Debut = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Fin = new(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc);

    [Fact]
    public async Task Restreint_au_perimetre_et_enrichit_le_nom()
    {
        var superviseur = Guid.NewGuid();
        var agentA = Guid.NewGuid();
        var fileA = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        var fileB = FileDeTraitement.Creer("BillingImport", "Facturation");

        var passageA = PassageEnFile.Ouvrir(Guid.NewGuid(), fileA.Id, 1, new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc));
        passageA.PrendreEnCharge(agentA, new(2026, 10, 5, 8, 20, 0, DateTimeKind.Utc));
        passageA.Fermer(new(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc));

        var autorisation = new Mock<IAutorisationService>();
        var perimetres = new Mock<IPerimetreSupervisionRepository>();
        perimetres.Setup(p => p.ObtenirParSuperviseurAsync(superviseur, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PerimetreSupervision.Definir(superviseur, new[] { fileA.Id }));
        var files = new Mock<IFileDeTraitementRepository>();
        files.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileDeTraitement> { fileA, fileB });

        IEnumerable<Guid>? idsDemandes = null;
        var passages = new Mock<IPassageEnFileRepository>();
        passages.Setup(p => p.ListerParFilesEtPeriodeAsync(It.IsAny<IEnumerable<Guid>>(), Debut, Fin, It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Guid>, DateTime, DateTime, CancellationToken>((ids, _, _, _) => idsDemandes = ids.ToList())
            .ReturnsAsync(new List<PassageEnFile> { passageA });

        var utilisateurs = new Mock<IUtilisateurInfoRepository>();
        utilisateurs.Setup(u => u.ObtenirInfosAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UtilisateurInfo>
            {
                [agentA] = new(agentA, "agentA", "a@cit.ci", "Agent A", true, NatureUtilisateur.Agent),
            });

        var query = new RapportActiviteOperateurQuery(
            passages.Object, files.Object, perimetres.Object, utilisateurs.Object, autorisation.Object);

        var res = await query.ExecuterAsync(superviseur, Debut, Fin);

        autorisation.Verify(a => a.ExigerAsync(superviseur, Permission.SupervisionConsulter, null, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(new[] { fileA.Id }, idsDemandes); // périmètre respecté : fileB exclue
        var ligne = Assert.Single(res);
        Assert.Equal(agentA, ligne.OperateurId);
        Assert.Equal("Agent A", ligne.Nom);
        Assert.Equal(1, ligne.TicketsTraites);
        Assert.Equal(TimeSpan.FromMinutes(20), ligne.DelaiMoyenPriseEnCharge);
    }

    [Fact]
    public async Task Sans_perimetre_retourne_vide()
    {
        var superviseur = Guid.NewGuid();
        var autorisation = new Mock<IAutorisationService>();
        var perimetres = new Mock<IPerimetreSupervisionRepository>();
        perimetres.Setup(p => p.ObtenirParSuperviseurAsync(superviseur, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PerimetreSupervision?)null);
        var files = new Mock<IFileDeTraitementRepository>();
        files.Setup(f => f.ListerAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<FileDeTraitement>());

        var query = new RapportActiviteOperateurQuery(
            new Mock<IPassageEnFileRepository>().Object, files.Object, perimetres.Object,
            new Mock<IUtilisateurInfoRepository>().Object, autorisation.Object);

        var res = await query.ExecuterAsync(superviseur, Debut, Fin);
        Assert.Empty(res);
    }
}
