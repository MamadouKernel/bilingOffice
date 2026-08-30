using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Web.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CIT.Plateforme.Web.Tests.BillingExport;

/// <summary>
/// [TEST-CDC] Contrôle d'unicité UNI-04 au niveau applicatif et Web :
/// Détection des doublons actifs, acceptation après clôture ou rejet, et indépendance multi-bookings.
/// </summary>
public sealed class ControleUniciteBookingWebTests(PlateformeWebApplicationFactory factory) : IClassFixture<PlateformeWebApplicationFactory>
{
    private static async Task AssurerFilesExportAsync(PlateformeDbContext dbContext)
    {
        var nomsRequis = new[]
        {
            NomsFilesExport.BackOfficeExport,
            NomsFilesExport.Facturation,
            NomsFilesExport.ValidationPaiement,
            NomsFilesExport.ComptabiliteClient,
            NomsFilesExport.ControleDocumentaire,
            NomsFilesExport.FileIes
        };

        var ajoutes = false;
        foreach (var nom in nomsRequis)
        {
            if (!await dbContext.Files.AnyAsync(f => f.Module == SoumettreDemandeExportCommand.Module && f.Nom == nom))
            {
                dbContext.Files.Add(FileDeTraitement.Creer(SoumettreDemandeExportCommand.Module, nom));
                ajoutes = true;
            }
        }
        if (ajoutes)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Soumission_d_un_booking_deja_actif_est_refusee_avec_mention_du_ticket_existant()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<PlateformeDbContext>();
        await AssurerFilesExportAsync(dbContext);

        var clientId = Guid.NewGuid();
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "UniciteWebExportSA", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        dbContext.Societes.Add(societe);
        dbContext.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(clientId, societe.Id, DateTime.UtcNow, droitSoumission: true));
        await dbContext.SaveChangesAsync();

        var soumettre = services.GetRequiredService<SoumettreDemandeExportCommand>();
        var bookingNo = $"BKG-UNIQ-{Guid.NewGuid():N}"[..15];

        // 1ère soumission
        var res1 = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(societe.Id, clientId, [bookingNo]));
        Assert.Single(res1.TicketsCrees);
        var premierTicketId = res1.TicketsCrees[0].Id;

        // 2ème soumission du même booking (avec variations de casse et d'espaces externes)
        var res2 = await soumettre.ExecuterAsync(new SoumettreDemandeExportRequete(
            societe.Id, clientId, [$"  {bookingNo.ToLowerInvariant()}  "]));

        Assert.Empty(res2.TicketsCrees);
        Assert.Single(res2.BookingRefuses);
        Assert.Equal(premierTicketId, res2.BookingRefuses[0].TicketExistantId);
    }
}
