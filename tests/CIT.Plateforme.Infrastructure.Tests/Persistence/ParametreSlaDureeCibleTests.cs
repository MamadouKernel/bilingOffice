using CIT.Plateforme.Domain.SuiviTemps;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Persistence;

/// <summary>
/// REMÉDIATION AUDIT (post-implementation-audit.md §3.D — DUREECIBLE SQL, point 2 de la demande
/// de remédiation) : avant correction, <c>ParametreSlaAdministrable.DureeCible</c> (TimeSpan)
/// était mappé par convention EF Core/SQL Server sur le type <c>time</c>, borné à moins de 24h —
/// toute cible SLA ≥ 24h provoquait un débordement au moment de l'enregistrement. Le mapping a été
/// corrigé (ticks/bigint, voir PlateformeDbContext), sans limite fonctionnelle absente du CDC.
/// Ces tests s'exécutent contre une vraie base SQL Server (le seul moyen de vérifier fidèlement un
/// type de colonne SQL — le fournisseur InMemory ne simule aucune contrainte de type SQL).
/// </summary>
public sealed class ParametreSlaDureeCibleTests
{
    [Theory]
    [InlineData(0, 30, 0)]     // 30 minutes — cas qui fonctionnait déjà avant correction.
    [InlineData(12, 0, 0)]     // 12 heures — idem.
    [InlineData(24, 0, 0)]     // 24 heures exactement — dépassait déjà la plage de `time`.
    [InlineData(48, 0, 0)]     // 48 heures — 2 jours.
    [InlineData(120, 0, 0)]    // 120 heures — 5 jours (plusieurs jours ouvrés, cas plausible CDC).
    public async Task DureeCible_survit_a_un_aller_retour_EF_Core_complet_quelle_que_soit_la_duree(int heures, int minutes, int secondes)
    {
        var dureeAttendue = new TimeSpan(heures, minutes, secondes);

        await using (var contexteEcriture = BaseDeTestSql.CreerContexte())
        {
            var parametre = ParametreSlaAdministrable.Definir(
                $"ModuleTest{Guid.NewGuid():N}", "TypeTest", dureeAttendue, 0.5, 0.8);
            contexteEcriture.ParametresSla.Add(parametre);

            // Le round-trip complet (INSERT réel + SELECT réel) est ce qui aurait échoué avant
            // correction pour heures >= 24 (débordement du type SQL `time`).
            await contexteEcriture.SaveChangesAsync();

            await using var contexteLecture = BaseDeTestSql.CreerContexte();
            var relu = await contexteLecture.ParametresSla.FindAsync(parametre.Id);

            Assert.NotNull(relu);
            Assert.Equal(dureeAttendue, relu!.DureeCible);
        }
    }
}