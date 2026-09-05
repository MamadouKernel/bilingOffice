using CIT.Plateforme.Domain.Assistant;
using Xunit;

namespace CIT.Plateforme.Domain.Tests.Assistant;

/// <summary>
/// [P1-03 — RG-IA-01 / RG-IA-02] Fil de conversation de l'assistant virtuel.
///
/// FONCTION AUPARAVANT TOTALEMENT ABSENTE : aucune trace d'assistant dans `src/`. RG-IA-02 exige
/// que l'escalade se fasse « sans perte de contexte (le fil de conversation reste attaché au
/// ticket) » — ce que ces tests vérifient au niveau du domaine.
/// </summary>
public sealed class ConversationAssistantTests
{
    private static readonly DateTime T0 = new(2026, 9, 5, 9, 0, 0, DateTimeKind.Utc);

    // --- Cas nominal ---

    [Fact]
    public void Le_fil_conserve_les_messages_dans_l_ordre()
    {
        var conversation = ConversationAssistant.Ouvrir(Guid.NewGuid(), T0);

        conversation.AjouterMessage(AuteurMessageAssistant.Client, "Où en est ma demande ?", T0);
        conversation.AjouterMessage(AuteurMessageAssistant.Assistant, "Consultez « Mes demandes ».", T0);

        Assert.Equal(2, conversation.Messages.Count);
        Assert.Equal([1, 2], conversation.Messages.OrderBy(m => m.Rang).Select(m => m.Rang));
        Assert.False(conversation.EstEscaladee);
        Assert.Null(conversation.TicketId);
    }

    /// <summary>RG-IA-02 — le fil est rattaché au ticket, et la transcription le restitue en entier.</summary>
    [Fact]
    public void L_escalade_rattache_le_fil_au_ticket_sans_perte_de_contexte()
    {
        var conversation = ConversationAssistant.Ouvrir(Guid.NewGuid(), T0);
        conversation.AjouterMessage(AuteurMessageAssistant.Client, "Question compliquée", T0);
        conversation.AjouterMessage(AuteurMessageAssistant.Assistant, "Je ne sais pas répondre.", T0);

        var ticketId = Guid.NewGuid();
        conversation.Escalader(ticketId, "Hors sujets traités", T0.AddMinutes(1));

        Assert.True(conversation.EstEscaladee);
        Assert.Equal(ticketId, conversation.TicketId);
        Assert.Equal(T0.AddMinutes(1), conversation.DateEscaladeUtc);

        var transcription = conversation.Transcrire();
        Assert.Contains("[Client] Question compliquée", transcription);
        Assert.Contains("[Assistant] Je ne sais pas répondre.", transcription);
    }

    // --- Cas interdits ---

    /// <summary>Après reprise par un opérateur, le fil de l'assistant se ferme : laisser écrire
    /// des deux côtés produirait deux conversations parallèles sur le même sujet.</summary>
    [Fact]
    public void On_n_ecrit_plus_dans_un_fil_deja_escalade()
    {
        var conversation = ConversationAssistant.Ouvrir(Guid.NewGuid(), T0);
        conversation.AjouterMessage(AuteurMessageAssistant.Client, "Bonjour", T0);
        conversation.Escalader(Guid.NewGuid(), "motif", T0);

        Assert.Throws<ConversationEscaladeeException>(
            () => conversation.AjouterMessage(AuteurMessageAssistant.Client, "Encore une question", T0));
    }

    [Fact]
    public void Une_escalade_sans_ticket_est_refusee()
    {
        var conversation = ConversationAssistant.Ouvrir(Guid.NewGuid(), T0);

        Assert.Throws<ArgumentException>(() => conversation.Escalader(Guid.Empty, "motif", T0));
    }

    [Fact]
    public void Une_escalade_sans_motif_est_refusee()
    {
        var conversation = ConversationAssistant.Ouvrir(Guid.NewGuid(), T0);

        Assert.Throws<ArgumentException>(() => conversation.Escalader(Guid.NewGuid(), "  ", T0));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_message_vide_est_refuse(string contenu)
    {
        var conversation = ConversationAssistant.Ouvrir(Guid.NewGuid(), T0);

        Assert.Throws<ArgumentException>(
            () => conversation.AjouterMessage(AuteurMessageAssistant.Client, contenu, T0));
    }

    [Fact]
    public void Un_fil_sans_proprietaire_est_refuse()
    {
        Assert.Throws<ArgumentException>(() => ConversationAssistant.Ouvrir(Guid.Empty, T0));
    }

    // --- Cas limite ---

    /// <summary>Idempotence : une seconde escalade ne réécrit ni le ticket ni la date d'origine.</summary>
    [Fact]
    public void Une_seconde_escalade_est_sans_effet()
    {
        var conversation = ConversationAssistant.Ouvrir(Guid.NewGuid(), T0);
        var premier = Guid.NewGuid();
        conversation.Escalader(premier, "premier motif", T0);

        conversation.Escalader(Guid.NewGuid(), "second motif", T0.AddHours(1));

        Assert.Equal(premier, conversation.TicketId);
        Assert.Equal("premier motif", conversation.MotifEscalade);
        Assert.Equal(T0, conversation.DateEscaladeUtc);
    }

    [Fact]
    public void La_transcription_d_un_fil_vide_est_vide()
    {
        Assert.Equal(string.Empty, ConversationAssistant.Ouvrir(Guid.NewGuid(), T0).Transcrire());
    }
}
