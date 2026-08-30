using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Comptes;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.Comptes;

/// <summary>
/// [TEST-TECH] Remédiation post-audit (post-implementation-audit.md §9, CPT-06). Preuve sur SQL Server
/// réel de la non-rétroactivité du changement de TypeClient : le snapshot persisté sur Dossier
/// garantit qu'un changement ultérieur de TypeClient sur la société cliente ne modifie jamais le type
/// applicable aux dossiers déjà créés.
/// Exigence CDC: CPT-06 → US5 → FR-006 → Scénario 2 (non-rétroactivité).
/// </summary>
public sealed class NonRetroactiviteTypeClientSqlTests
{
    private sealed class HorlogeFixe(DateTime instant) : IHorloge
    {
        public DateTime MaintenantUtc => instant;
    }

    [Fact]
    public async Task Changement_type_client_sur_SQL_Server_ne_modifie_pas_les_dossiers_existants_et_s_applique_aux_nouveaux()
    {
        var maintenant = DateTime.UtcNow;
        var horloge = new HorlogeFixe(maintenant);
        var adminCitId = Guid.NewGuid();
        Guid societeId;
        Guid premierDossierId;
        Guid secondDossierId;

        // Étape 1 & 2 : Société créée en TypeClient = NORMAL, puis création du premier dossier avec snapshot
        await using (var setup = BaseDeTestSql.CreerContexte())
        {
            setup.Habilitations.Add(HabilitationUtilisateur.Attribuer(adminCitId, Permission.CompteTypeModifier));

            var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Société Test Non-Rétroactivité", TypeClient.Normal, maintenant);
            societe.Valider();
            setup.Societes.Add(societe);
            await setup.SaveChangesAsync();
            societeId = societe.Id;

            var premierDossier = Dossier.Creer("DOS-SQL-001", societe.Id, maintenant, typeClient: societe.Profil);
            setup.Dossiers.Add(premierDossier);
            await setup.SaveChangesAsync();
            premierDossierId = premierDossier.Id;
        }

        // Étape 3 : La société passe à DEPOSIT via la commande applicative réelle
        await using (var contexteModification = BaseDeTestSql.CreerContexte())
        {
            var commande = new ModifierTypeClientCommand(
                new SocieteClienteRepository(contexteModification),
                new AutorisationService(contexteModification),
                new JournalisationService(contexteModification, horloge),
                new UniteDeTravail(contexteModification));

            await commande.ExecuterAsync(adminCitId, societeId, TypeClient.Deposit);
        }

        // Étape 4 & 5 : Vérification sur SQL Server réel — le premier dossier reste NORMAL, le nouveau prend DEPOSIT
        await using (var contexteNouveau = BaseDeTestSql.CreerContexte())
        {
            var societeModifiee = await contexteNouveau.Societes.SingleAsync(s => s.Id == societeId);
            Assert.Equal(TypeClient.Deposit, societeModifiee.Profil);

            // Nouveau dossier créé après le changement
            var secondDossier = Dossier.Creer("DOS-SQL-002", societeModifiee.Id, maintenant, typeClient: societeModifiee.Profil);
            contexteNouveau.Dossiers.Add(secondDossier);
            await contexteNouveau.SaveChangesAsync();
            secondDossierId = secondDossier.Id;
        }

        // Roundtrip complet et assertions finales depuis un contexte de lecture indépendant
        await using (var verification = BaseDeTestSql.CreerContexte())
        {
            var dossier1 = await verification.Dossiers.SingleAsync(d => d.Id == premierDossierId);
            var dossier2 = await verification.Dossiers.SingleAsync(d => d.Id == secondDossierId);

            // Invariant vérifié sur SQL Server réel :
            Assert.Equal(societeId, dossier1.ClientId);
            Assert.Equal(TypeClient.Normal, dossier1.TypeClient);

            Assert.Equal(societeId, dossier2.ClientId);
            Assert.Equal(TypeClient.Deposit, dossier2.TypeClient);
        }
    }
}
