using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.BillingImport;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using CIT.Plateforme.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CIT.Plateforme.Infrastructure.Tests.BillingImport;

/// <summary>
/// [AUDIT — TEST DE PREUVE] research.md D4 / CDC IMP-02 : une soumission multi-BL n'est jamais
/// traitée comme une transaction globale « tout ou rien ». Ce test vérifie qu'une exception
/// **inattendue** (donc non traduite en <see cref="BlDejaActifException"/>) survenant au milieu de
/// la boucle de <see cref="SoumettreDemandeImportCommand"/> laisse les BL déjà enregistrés avec
/// succès **durablement committés** en base — pas seulement présents dans un ChangeTracker en
/// mémoire non sauvegardé. La panne est simulée en forçant une collision sur l'index unique
/// préexistant <c>IX_Tickets_Numero</c> (F001, non filtré) pour le second BL — une violation
/// **volontairement non liée** à <c>IX_Tickets_NumeroBl</c>, afin qu'elle ne soit PAS interceptée
/// par la traduction dédiée de <see cref="UniteDeTravail"/> et remonte donc telle quelle.
/// </summary>
public sealed class AtomiciteMultiBlSqlTests
{
    /// <summary>Numérotation truquée : renvoie un numéro de ticket déjà utilisé par un ticket
    /// préexistant en base au second appel, pour provoquer un <see cref="DbUpdateException"/> brut
    /// (violation de <c>IX_Tickets_Numero</c>) sans passer par la fenêtre de course NumeroBl.</summary>
    private sealed class NumerotationAvecCollisionSurLeDeuxiemeTicket(string numeroDossier, string numeroTicketDejaUtilise) : IServiceNumerotation
    {
        private int appels;

        public Task<string> ProchainNumeroDossierAsync(CancellationToken ct = default) => Task.FromResult(numeroDossier);

        public Task<string> ProchainNumeroTicketAsync(string module, string typeDemande, CancellationToken ct = default)
        {
            appels++;
            return appels == 2
                ? Task.FromResult(numeroTicketDejaUtilise)
                : Task.FromResult($"TCK-ATOMIC-{Guid.NewGuid():N}"[..20]);
        }
    }

    [Fact]
    public async Task Exception_inattendue_au_milieu_de_la_boucle_ne_fait_pas_de_rollback_du_BL_deja_enregistre()
    {
        var suffixe = Guid.NewGuid().ToString("N")[..8];
        var (blA, blB, blC) = ($"ATOMIC-A-{suffixe}", $"ATOMIC-B-{suffixe}", $"ATOMIC-C-{suffixe}");
        var numeroTicketDejaUtilise = $"TCK-DUP-{suffixe}";

        await using var setup = BaseDeTestSql.CreerContexte();
        var dossierExistant = Dossier.Creer("DOS-ATOMIC-EXISTANT", Guid.NewGuid(), DateTime.UtcNow);
        setup.Dossiers.Add(dossierExistant);
        // Ticket préexistant dont le Numero sera délibérément réutilisé par le 2e BL de la
        // soumission testée ci-dessous, pour provoquer la collision.
        setup.Tickets.Add(Ticket.Creer(numeroTicketDejaUtilise, "BillingImport", "Import", Guid.NewGuid(), dossierExistant.Id, DateTime.UtcNow));
        // La file « Pilote Manifeste » est désormais semée une seule fois par BaseDeTestSql
        // (post-implementation-audit.md §10.4) — ne plus la réinsérer ici.
        await setup.SaveChangesAsync();

        await using var contexte = BaseDeTestSql.CreerContexte();
        var horloge = new HorlogeSysteme();
        var journalisation = new JournalisationService(contexte, horloge);
        var uniteDeTravail = new UniteDeTravail(contexte);
        var societe = SocieteCliente.Soumettre(Guid.NewGuid(), "Informations", TypeClient.Normal, DateTime.UtcNow);
        societe.Valider();
        var auteurId = Guid.NewGuid();
        contexte.Societes.Add(societe);
        contexte.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(contexte, auteurId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), societe.Id, DateTime.UtcNow, droitSoumission: true));
        await contexte.SaveChangesAsync();

        var numerotationTruquee = new NumerotationAvecCollisionSurLeDeuxiemeTicket($"DOS-ATOMIC-{suffixe}", numeroTicketDejaUtilise);
        var soumettre = new SoumettreDemandeImportCommand(
            new TicketRepository(contexte), new DossierRepository(contexte), new SocieteClienteRepository(contexte),
            new UtilisateurSocieteRepository(contexte), new FileDeTraitementRepository(contexte),
            numerotationTruquee, journalisation, horloge, uniteDeTravail);

        // BL A doit s'enregistrer avec succès (1er appel de numérotation, sans collision). BL B
        // provoque la collision de Numero — exception BRUTE (pas BlDejaActifException) qui doit
        // interrompre toute la boucle : BL C n'est jamais tenté.
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            soumettre.ExecuterAsync(new SoumettreDemandeImportRequete(societe.Id, auteurId, [blA, blB, blC])));
        Assert.IsNotType<BlDejaActifException>(exception);

        // Preuve de durabilité réelle : nouveau DbContext totalement indépendant, aucun état
        // partagé avec celui utilisé par la commande.
        await using var verification = BaseDeTestSql.CreerContexte();
        var ticketA = await verification.Tickets.SingleOrDefaultAsync(t => t.NumeroBl == blA);
        var ticketC = await verification.Tickets.SingleOrDefaultAsync(t => t.NumeroBl == blC);

        Assert.NotNull(ticketA);
        Assert.Null(ticketC);
    }
}
