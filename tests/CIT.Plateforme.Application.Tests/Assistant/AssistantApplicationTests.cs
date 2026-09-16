using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Assistant;
using CIT.Plateforme.Application.Messagerie;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Assistant;
using CIT.Plateforme.Domain.Messagerie;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Assistant;
using CIT.Plateforme.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Assistant;

/// <summary>
/// [P1-03 — RG-IA-01 / RG-IA-02] Cas d'usage de l'assistant virtuel, adaptateur simulé.
///
/// LE FOURNISSEUR N'EST PAS ARBITRÉ (PC-18). Ces tests éprouvent l'architecture — le fil, la
/// recommandation d'escalade, le rattachement au ticket et la remise du contexte à l'opérateur —
/// sans dépendre d'un tiers. Le jour où un fournisseur réel est branché, ils restent valables :
/// aucun n'affirme quoi que ce soit du contenu produit par l'assistant.
/// </summary>
public sealed class AssistantApplicationTests
{
    private static (PlateformeDbContext Db, PoserQuestionAssistantCommand Poser,
                    EscaladerAssistantVersOperateurCommand Escalader, Guid ClientId, Ticket Ticket)
        Construire()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var journalisation = new JournalisationService(db, horloge);
        var uniteDeTravail = new UniteDeTravail(db);
        var ticketRepo = new TicketRepository(db);
        var dossierRepo = new DossierRepository(db);
        var cloisonnement = new CloisonnementService(ticketRepo, CIT.Plateforme.Application.Tests.Support.FabriqueRequetes.ControleAcces(db));
        var conversations = new ConversationAssistantRepository(db);

        var clientId = Guid.NewGuid();
        var dossier = Dossier.Creer("DOS-IA-000001", CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId, CIT.Plateforme.Domain.Securite.NatureUtilisateur.Client), horloge.MaintenantUtc);
        var ticket = Ticket.Creer("BIL-IMP-IA0001", "BillingImport", "Import", Guid.NewGuid(), dossier.Id, horloge.MaintenantUtc);
        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);

        // IA-04 : la base de connaissances est administrable, plus une table codée en dur — les
        // tests d'architecture la peuplent eux-mêmes avec un sujet reconnu et laissent
        // volontairement le reste hors périmètre pour exercer l'escalade (IA-03).
        db.SujetsAssistant.Add(CIT.Plateforme.Domain.Assistant.SujetAssistant.Definir(
            "suivi,suivre,où en est,ou en est,statut,avancement,pièce,piece,bonjour,bonsoir",
            "Vous pouvez suivre l'avancement de chacune de vos demandes depuis « Mes demandes ».", 1));
        db.SaveChanges();

        var poser = new PoserQuestionAssistantCommand(
            conversations, new AssistantSimule(new SujetAssistantRepository(db), NullLogger<AssistantSimule>.Instance),
            horloge, uniteDeTravail);

        var publier = new PublierMessageCommand(
            new MessageRepository(db), ticketRepo,
            new CIT.Plateforme.Infrastructure.Identity.AutorisationService(db),
            cloisonnement, journalisation, horloge, uniteDeTravail);

        var escalader = new EscaladerAssistantVersOperateurCommand(
            conversations, publier, cloisonnement, journalisation, horloge, uniteDeTravail);

        return (db, poser, escalader, clientId, ticket);
    }

    // --- Cas nominal (RG-IA-01) ---

    [Fact]
    public async Task Une_question_de_premier_niveau_recoit_une_reponse_sans_escalade()
    {
        var (_, poser, _, clientId, _) = Construire();

        var vue = await poser.ExecuterAsync(clientId, "Où en est ma demande ?");

        Assert.False(vue.EscaladeRecommandee);
        Assert.Equal(2, vue.Messages.Count); // la question, puis la réponse
        Assert.Equal("Vous", vue.Messages[0].Auteur);
        Assert.Equal("Assistant", vue.Messages[1].Auteur);
        Assert.NotEmpty(vue.Messages[1].Contenu);
    }

    /// <summary>Le fil se poursuit : une seconde question ne repart pas de zéro (IA-02).</summary>
    [Fact]
    public async Task Le_fil_se_poursuit_d_une_question_a_l_autre()
    {
        var (_, poser, _, clientId, _) = Construire();

        await poser.ExecuterAsync(clientId, "Bonjour");
        var vue = await poser.ExecuterAsync(clientId, "Quelles pièces dois-je fournir ?");

        Assert.Equal(4, vue.Messages.Count);
    }

    /// <summary>IA-03 — hors de ses sujets, l'assistant recommande l'escalade au lieu de deviner.</summary>
    [Fact]
    public async Task Une_question_hors_perimetre_recommande_l_escalade()
    {
        var (_, poser, _, clientId, _) = Construire();

        var vue = await poser.ExecuterAsync(clientId, "Quelle est la capacité résiduelle du terminal en 2031 ?");

        Assert.True(vue.EscaladeRecommandee);
        Assert.NotNull(vue.MotifEscaladeRecommandee);
    }

    // --- Escalade (RG-IA-02) ---

    [Fact]
    public async Task L_escalade_rattache_le_fil_au_ticket_et_remet_le_contexte_a_l_operateur()
    {
        var (db, poser, escalader, clientId, ticket) = Construire();

        var vue = await poser.ExecuterAsync(clientId, "Question inhabituelle sur mon dossier");
        await escalader.ExecuterAsync(clientId, vue.Id, ticket.Id, "Hors sujets traités");

        var conversation = db.ConversationsAssistant.Single();
        Assert.True(conversation.EstEscaladee);
        Assert.Equal(ticket.Id, conversation.TicketId);

        // « Sans perte de contexte » : la transcription est publiée dans le fil du ticket, canal de
        // référence des échanges (RG-COM-03). C'est un message client — ce sont les propres mots du
        // client et les réponses qui lui ont été faites, rien d'interne à CIT.
        var note = db.Messages.Single(m => m.TicketId == ticket.Id);
        Assert.Equal(TypeMessage.Externe, note.Type);
        Assert.Contains("Question inhabituelle sur mon dossier", note.Contenu);
        Assert.Contains("Hors sujets traités", note.Contenu);
    }

    // --- Cas interdits ---

    [Fact]
    public async Task Un_utilisateur_ne_peut_pas_escalader_le_fil_d_un_autre()
    {
        var (_, poser, escalader, clientId, ticket) = Construire();
        var vue = await poser.ExecuterAsync(clientId, "Bonjour");

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => escalader.ExecuterAsync(Guid.NewGuid(), vue.Id, ticket.Id, "motif"));
    }

    /// <summary>NON-RÉGRESSION P0 : l'escalade passe par le cloisonnement — un fil ne peut pas
    /// être rattaché au ticket d'un tiers.</summary>
    [Fact]
    public async Task Un_fil_ne_peut_pas_etre_rattache_au_ticket_d_un_autre_client()
    {
        var (db, poser, escalader, clientId, _) = Construire();
        var vue = await poser.ExecuterAsync(clientId, "Bonjour");

        var autreDossier = Dossier.Creer("DOS-IA-000002", Guid.NewGuid(), DateTime.UtcNow);
        var ticketTiers = Ticket.Creer("BIL-IMP-IA0002", "BillingImport", "Import", Guid.NewGuid(), autreDossier.Id, DateTime.UtcNow);
        db.Dossiers.Add(autreDossier);
        db.Tickets.Add(ticketTiers);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AccesRefuseException>(
            () => escalader.ExecuterAsync(clientId, vue.Id, ticketTiers.Id, "motif"));
    }

    [Fact]
    public async Task Un_utilisateur_non_authentifie_n_accede_pas_a_l_assistant()
    {
        var (_, poser, _, _, _) = Construire();

        await Assert.ThrowsAsync<AccesRefuseException>(() => poser.ExecuterAsync(Guid.Empty, "Bonjour"));
    }

    // --- Cas limite ---

    [Fact]
    public async Task Une_question_vide_est_refusee()
    {
        var (_, poser, _, clientId, _) = Construire();

        await Assert.ThrowsAsync<ArgumentException>(() => poser.ExecuterAsync(clientId, "   "));
    }

    [Fact]
    public async Task Une_seconde_escalade_du_meme_fil_est_sans_effet()
    {
        var (db, poser, escalader, clientId, ticket) = Construire();
        var vue = await poser.ExecuterAsync(clientId, "Question inhabituelle");

        await escalader.ExecuterAsync(clientId, vue.Id, ticket.Id, "premier motif");
        await escalader.ExecuterAsync(clientId, vue.Id, ticket.Id, "second motif");

        Assert.Single(db.Messages.Where(m => m.TicketId == ticket.Id));
        Assert.Equal("premier motif", db.ConversationsAssistant.Single().MotifEscalade);
    }
}
