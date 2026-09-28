using CIT.Plateforme.Application.Assistant;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Files;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Persistence;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Assistant;

/// <summary>
/// Assistant « données » : répond à partir des dossiers de l'usager, avec le cloisonnement des
/// écrans client — jamais le dossier d'une autre société, ni celui d'un collègue quand le droit
/// de consultation est restreint.
/// </summary>
public sealed class RepondeurDonneesAssistantTests
{
    private sealed record Monde(PlateformeDbContext Db, RepondeurDonneesAssistant Repondeur, Guid ClientA, Guid CollegueA, Guid ClientB, Ticket TicketA, Ticket TicketCollegue, Ticket TicketB);

    private static Monde Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var maintenant = HorlogeDeTest.Defaut().MaintenantUtc;
        var file = FileDeTraitement.Creer("BillingImport", "Pilote Manifeste");
        db.Files.Add(file);

        (Guid Utilisateur, Guid Societe) Client(string nom)
        {
            var societe = SocieteCliente.Soumettre(Guid.NewGuid(), nom, TypeClient.Normal, maintenant);
            societe.Valider();
            db.Societes.Add(societe);
            var id = Guid.NewGuid();
            db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(
                CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, id, NatureUtilisateur.Client), societe.Id, maintenant));
            return (id, societe.Id);
        }

        Ticket Dossier(string numero, string bl, Guid societeId, Guid auteurId)
        {
            var dossier = Domain.Tickets.Dossier.Creer($"DOS-{numero}", societeId, maintenant, auteurId: auteurId, typeClient: TypeClient.Normal);
            db.Dossiers.Add(dossier);
            var ticket = Ticket.Creer(numero, "BillingImport", "IMPORT", file.Id, dossier.Id, maintenant, numeroBl: bl);
            db.Tickets.Add(ticket);
            return ticket;
        }

        var (clientA, societeA) = Client("Société A");
        var collegueA = Guid.NewGuid();
        db.UtilisateursSociete.Add(UtilisateurSociete.Rattacher(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, collegueA, NatureUtilisateur.Client), societeA, maintenant));
        var (clientB, societeB) = Client("Société B");

        var ticketA = Dossier("IMP-000101", "MSCU1111111", societeA, clientA);
        ticketA.PrendreEnCharge(Guid.NewGuid(), maintenant);
        ticketA.MettreEnAttenteClient(maintenant);
        var ticketCollegue = Dossier("IMP-000102", "MSCU2222222", societeA, collegueA);
        var ticketB = Dossier("IMP-000201", "MSCU9999999", societeB, clientB);
        db.SaveChanges();

        return new Monde(db, AssistantApplicationTests.RepondeurDonnees(db, HorlogeDeTest.Defaut()),
            clientA, collegueA, clientB, ticketA, ticketCollegue, ticketB);
    }

    [Fact]
    public async Task L_etat_d_un_dossier_est_donne_par_son_numero_de_BL_avec_un_lien()
    {
        var m = Construire();

        var reponse = await m.Repondeur.RepondreAsync(m.ClientA, "Où en est mon BL mscu1111111 ?");

        Assert.NotNull(reponse);
        Assert.Contains("IMP-000101", reponse!.Texte);
        Assert.Contains("Pilote Manifeste", reponse.Texte);
        Assert.Contains("complément", reponse.Texte);
        Assert.Contains($"/tickets/{m.TicketA.Id}", reponse.Texte);
    }

    [Fact]
    public async Task Le_dossier_d_une_autre_societe_n_est_jamais_revele()
    {
        var m = Construire();

        var reponse = await m.Repondeur.RepondreAsync(m.ClientA, "Et le dossier IMP-000201 ?");

        Assert.NotNull(reponse);
        Assert.Contains("Je ne trouve aucun dossier", reponse!.Texte);
        Assert.DoesNotContain(m.TicketB.Id.ToString(), reponse.Texte);
        Assert.DoesNotContain("MSCU9999999", reponse.Texte);
    }

    [Fact]
    public async Task Le_dossier_d_un_collegue_reste_invisible_quand_le_droit_est_restreint()
    {
        var m = Construire();

        var reponse = await m.Repondeur.RepondreAsync(m.ClientA, "Statut de IMP-000102 ?");

        Assert.Contains("Je ne trouve aucun dossier", reponse!.Texte);
        Assert.DoesNotContain(m.TicketCollegue.Id.ToString(), reponse.Texte);
    }

    [Fact]
    public async Task Les_dossiers_en_attente_de_l_usager_sont_listes()
    {
        var m = Construire();

        var reponse = await m.Repondeur.RepondreAsync(m.ClientA, "Qu'attend-on de moi ?");

        Assert.Contains("1 dossier(s) attendent", reponse!.Texte);
        Assert.Contains("IMP-000101", reponse.Texte);
        Assert.DoesNotContain("IMP-000102", reponse.Texte);
    }

    [Fact]
    public async Task La_synthese_ne_compte_que_les_dossiers_visibles()
    {
        var m = Construire();

        var reponse = await m.Repondeur.RepondreAsync(m.ClientB, "Où en sont mes dossiers ?");

        Assert.Contains("Vous avez 1 dossier(s) en cours", reponse!.Texte);
        Assert.Contains("IMP-000201", reponse.Texte);
        Assert.DoesNotContain("IMP-000101", reponse.Texte);
    }

    [Fact]
    public async Task Une_question_generale_est_laissee_a_l_assistant_generaliste()
    {
        var m = Construire();

        Assert.Null(await m.Repondeur.RepondreAsync(m.ClientA, "Comment payer ma facture ?"));
    }

    [Fact]
    public async Task Un_compte_sans_societe_n_obtient_aucune_donnee_client()
    {
        var m = Construire();

        Assert.Null(await m.Repondeur.RepondreAsync(Guid.NewGuid(), "Où en est IMP-000101 ?"));
    }

    [Theory]
    [InlineData("Le 27/09/2026 j'ai déposé IMP-000101", new[] { "IMP-000101" })]
    [InlineData("bonjour", new string[0])]
    [InlineData("BL MSCU1111111 et booking BKG-44521", new[] { "MSCU1111111", "BKG-44521" })]
    public void Les_references_citees_ignorent_les_dates_et_les_mots(string question, string[] attendues)
    {
        Assert.Equal(attendues, RepondeurDonneesAssistant.ReferencesCitees(question));
    }
}
