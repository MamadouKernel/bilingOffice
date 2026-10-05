using CIT.Plateforme.Application.Tests.Support;
using System.Reflection;
using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Notifications;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Notifications;

/// <summary>
/// FEATURE 009 — T124 à T132, T182, T183, T187. Vérifications structurelles de périmètre :
/// IMP-11, préservation des producteurs existants, et scans d'invariants.
///
/// Ces tests lisent le CODE SOURCE réel du dépôt : ils échouent si une modification ultérieure
/// franchit une frontière posée par spec.md / research.md.
/// </summary>
public class Imp11EtProducteursTests
{
    private static string RacineDepot()
    {
        var repertoire = new DirectoryInfo(AppContext.BaseDirectory);
        while (repertoire is not null && !Directory.Exists(Path.Combine(repertoire.FullName, "src")))
        {
            repertoire = repertoire.Parent;
        }

        Assert.NotNull(repertoire);
        return repertoire!.FullName;
    }

    private static string LireSource(string cheminRelatif) =>
        File.ReadAllText(Path.Combine(RacineDepot(), cheminRelatif.Replace('/', Path.DirectorySeparatorChar)));

    private static IEnumerable<string> FichiersSource() =>
        Directory.EnumerateFiles(Path.Combine(RacineDepot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains("Migrations"));

    // ---- T124 / T125 : IMP-11 — exactement une publication par commande ----

    [Theory]
    [InlineData("src/CIT.Plateforme.Application/BillingImport/EditerFactureCommand.cs")]
    [InlineData("src/CIT.Plateforme.Application/BillingExport/EditerFactureExportCommand.cs")]
    public void EditerFacture_PublieExactementUnEvenementFactureEditee(string chemin)
    {
        var source = LireSource(chemin);

        var publications = System.Text.RegularExpressions.Regex.Matches(source, @"PublierEvenementAsync\(");

        Assert.Single(publications);
        Assert.Contains("\"FactureEditee\"", source, StringComparison.Ordinal);
    }

    // ---- T126 / T127 : aucune autre logique de notification dans ces commandes ----

    [Theory]
    [InlineData("src/CIT.Plateforme.Application/BillingImport/EditerFactureCommand.cs")]
    [InlineData("src/CIT.Plateforme.Application/BillingExport/EditerFactureExportCommand.cs")]
    public void EditerFacture_NeContientAucuneAutreLogiqueDeNotification(string chemin)
    {
        var source = LireSource(chemin);

        // La commande publie un événement ; elle ne connaît NI le canal, NI le modèle, NI le
        // destinataire — tout cela relève du moteur F009 (research.md D1/D7).
        Assert.DoesNotContain("Notification", source.Replace("PublierEvenementAsync", ""), StringComparison.Ordinal);
        Assert.DoesNotContain("CanalNotification", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RegleDiffusion", source, StringComparison.Ordinal);
        Assert.DoesNotContain("INotificationRepository", source, StringComparison.Ordinal);
    }

    // ---- T131 / T187 : les autres producteurs restent inchangés ----

    [Fact]
    public void LesProducteursDEvenements_SontAuNombreAttenduEtInchanges()
    {
        // research.md D1 : 21 sites d'appel répartis sur 19 fichiers. F009 en ajoute 2 (IMP-11),
        // portant le total à 23 sites sur 19 fichiers — AUCUN fichier producteur nouveau hors
        // les deux commandes de facture, qui existaient déjà.
        var fichiersProducteurs = FichiersSource()
            .Where(f => !f.Contains("AvoirExoneration", StringComparison.OrdinalIgnoreCase)
                        && (File.ReadAllText(f).Contains("journalisation.PublierEvenementAsync(", StringComparison.Ordinal)
                            || File.ReadAllText(f).Contains("journalisationService.PublierEvenementAsync(", StringComparison.Ordinal)))
            .Select(Path.GetFileName)
            .OrderBy(n => n)
            .ToList();

        // 19 fichiers producteurs AVANT F009 (research.md D1 : 21 sites d'appel sur 19
        // fichiers) + les 2 commandes de facture (IMP-11) + la commande d'import (IMP-03 / F012)
        // + les 2 commandes d'enrôlement F013 (ENR-05 : RappelerExpirationBadgesCommand, DemanderRenouvellementBadgeCommand)
        // + la commande de decision sur une demande de compte (CPT-01 : le demandeur est informe
        //   du sort de sa demande — silence du CDC traite comme un oubli, canal In-App seul)
        // + la commande d'escalade de l'assistant virtuel (RG-IA-02, remédiation P1-03 : l'opérateur
        //   doit être averti qu'un fil lui a été transmis)
        // → 26 fichiers producteurs.
        //
        // CETTE GARDE A FAIT SON OFFICE, PLUSIEURS FOIS : elle a signalé l'ajout du 26e producteur
        // au moment où il a été introduit, puis du 27e — EmettreDraftInExportCommand, remédiation
        // post-audit final RG-EXP-03 — puis des 28e et 29e (LOT 2, enrôlement société : les deux
        // commandes de soumission par la société publient TicketCree, comme toute création de
        // ticket). Son objectif est inchangé : détecter tout producteur d'événement non délibéré.
        // Seul l'inventaire attendu est mis à jour, et chaque nouveau producteur est nommé ci-dessous
        // pour que sa disparition soit à son tour détectée.
        // 30e producteur — SUPPORT IT : IncidentCommands.cs publie TicketCree à la déclaration d'un
        // incident technique, comme toute création de ticket. Producteur délibéré, nommé ci-dessous.
        // 31e producteur (03/10/2026) — RappelerEcheancesDocumentsCommand : rappel avant échéance
        // du permis, de l'assurance ou de la visite technique déclarés à l'enrôlement de flotte.
        // 32e à 46e producteurs (05/10/2026) — parcours Billing IMPORT / EXPORT : le client est
        // notifié à chaque étape (validation, rejet, complément, paiement confirmé ou refusé,
        // clôture) et la création d'un ticket export publie TicketCree comme l'import.
        // 47e producteur (05/10/2026) — GestionProformaCommand : proforma déposée, le client est notifié.
        // 48e producteur (05/10/2026) — DemanderReaffectationCommand : superviseurs et agent en charge notifiés.
        // 49e producteur (05/10/2026) — OptionsTraitementTicketQuery.cs porte EscaladerAuSuperviseurCommand.
        Assert.Equal(49, fichiersProducteurs.Count);
        Assert.Contains("OptionsTraitementTicketQuery.cs", fichiersProducteurs);
        Assert.Contains("DemanderReaffectationCommand.cs", fichiersProducteurs);
        Assert.Contains("GestionProformaCommand.cs", fichiersProducteurs);
        foreach (var producteur in new[]
        {
            "SoumettreDemandeExportCommand.cs", "ValiderTicketImportCommand.cs", "ValiderTicketExportCommand.cs",
            "RejeterTicketImportCommand.cs", "RejeterTicketExportCommand.cs", "GestionComplementManifesteCommand.cs",
            "GestionComplementExportCommand.cs", "ValiderPreuvePaiementCommand.cs", "ValiderPreuvePaiementExportCommand.cs",
            "ConfirmerLettrageCommand.cs", "ConfirmerLettrageExportCommand.cs", "DeposerDocumentsControleEtCloturerCommand.cs",
            "FinaliserControleDocumentaireExportCommand.cs", "ConstaterJointureIesEtFinaliserCommand.cs",
            "ConstaterJointureIesExportEtFinaliserCommand.cs",
        })
        {
            Assert.Contains(producteur, fichiersProducteurs);
        }
        Assert.Contains("RappelerEcheancesDocumentsCommand.cs", fichiersProducteurs);
        Assert.Contains("IncidentCommands.cs", fichiersProducteurs);
        Assert.Contains("SoumettreEnrolementSocieteCommand.cs", fichiersProducteurs);
        Assert.Contains("SoumettreRetraitEnrolementSocieteCommand.cs", fichiersProducteurs);
        Assert.Contains("EmettreDraftInExportCommand.cs", fichiersProducteurs);
        Assert.Contains("AssistantCommands.cs", fichiersProducteurs);
        Assert.Contains("ValiderDemandeCompteCommand.cs", fichiersProducteurs);
        Assert.Contains("EditerFactureCommand.cs", fichiersProducteurs);
        Assert.Contains("EditerFactureExportCommand.cs", fichiersProducteurs);
        Assert.Contains("SoumettreDemandeImportCommand.cs", fichiersProducteurs);
        Assert.Contains("RappelerExpirationBadgesCommand.cs", fichiersProducteurs);
        Assert.Contains("DemanderRenouvellementBadgeCommand.cs", fichiersProducteurs);

        // Les producteurs sensibles de F001 sont présents et n'ont pas disparu.
        Assert.Contains("MentionnerCollaborateurCommand.cs", fichiersProducteurs);
        Assert.Contains("SurveillerEcheancesUseCase.cs", fichiersProducteurs);
        Assert.Contains("RelancesAbsenceReponseUseCase.cs", fichiersProducteurs);
    }

    // ---- T091 : MSG-04 — MentionnerCollaborateurCommand inchangée ----

    [Fact]
    public void MentionnerCollaborateurCommand_NeConnaitAucunConceptDeFeature009()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Messagerie/MentionnerCollaborateurCommand.cs");

        // research.md D5 : le raccordement MSG-04 se fait par CONFIGURATION, sans toucher au
        // code métier. La commande ignore tout du moteur de notification.
        Assert.DoesNotContain("Notifications", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CanalNotification", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RegleDiffusion", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Resolveur", source, StringComparison.Ordinal);
        // La charge utile porte toujours collaborateurId, sur laquelle repose le raccordement.
        Assert.Contains("collaborateurId", source, StringComparison.Ordinal);
    }

    // ---- T097 : ALE-03 — SurveillerEcheancesUseCase inchangée ----

    [Fact]
    public void SurveillerEcheancesUseCase_NeConnaitAucunConceptDeFeature009()
    {
        var source = LireSource("src/CIT.Plateforme.Application/Supervision/SurveillerEcheancesUseCase.cs");

        Assert.DoesNotContain("Notifications", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RegleDiffusion", source, StringComparison.Ordinal);
        // Le point d'intégration existe déjà : l'événement SLA est publié par F001.
        Assert.Contains("PublierEvenementAsync", source, StringComparison.Ordinal);
    }

    // ---- T137 : les traitements de fond du socle ne sont pas refondus ----

    [Theory]
    [InlineData("src/CIT.Plateforme.Infrastructure/TraitementsDeFond/RelancesAbsenceReponseService.cs")]
    [InlineData("src/CIT.Plateforme.Infrastructure/TraitementsDeFond/SurveillanceSlaService.cs")]
    [InlineData("src/CIT.Plateforme.Infrastructure/TraitementsDeFond/DistributionAutomatiqueService.cs")]
    public void LesTraitementsDeFondDuSocle_NeConnaissentPasFeature009(string chemin)
    {
        // spec.md §17 : F009 réutilise le PATRON de ces services, jamais leurs capacités.
        var source = LireSource(chemin);

        Assert.DoesNotContain("Notifications.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DistribuerNotifications", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ReessayerEnvois", source, StringComparison.Ordinal);
    }

    // ---- T182 : scan des fournisseurs interdits ----

    [Fact]
    public void AucunFournisseurExterneNEstReferenceDansLeCodeSource()
    {
        // spec.md §8, research.md D10, PC-16, Constitution Principe XI.
        //
        // DÉCISION D-07 TRANCHÉE (2026-09-17) : le commanditaire a choisi SMTP générique pour le
        // canal Email (COM-01/ENR-04), en attendant le fournisseur définitif de la DSI/Finance —
        // voir CanalEmailSmtp.cs. System.Net.Mail.SmtpClient (BCL, aucun SDK tiers) est donc retiré
        // de cette liste. SMS et WhatsApp restent journalisés : aucune décision n'a été prise pour
        // eux, la garde reste donc entière sur ces fournisseurs.
        string[] fournisseursInterdits =
        [
            "Twilio", "SendGrid", "MailKit", "Azure.Communication",
            "Infobip", "Vonage", "Nexmo", "Mailgun", "Amazon.SimpleEmail",
            "graph.facebook.com", "api.whatsapp.com",
        ];

        var occurrences = new List<string>();

        foreach (var fichier in FichiersSource())
        {
            var source = File.ReadAllText(fichier);
            foreach (var fournisseur in fournisseursInterdits)
            {
                // Les mentions en commentaire d'INTERDICTION sont légitimes : on ne retient que
                // les usages réels (hors lignes de commentaire).
                var lignesFautives = source.Split('\n')
                    .Where(l => l.Contains(fournisseur, StringComparison.OrdinalIgnoreCase))
                    .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                                && !l.TrimStart().StartsWith("///", StringComparison.Ordinal)
                                && !l.TrimStart().StartsWith('*'));

                occurrences.AddRange(lignesFautives.Select(l => $"{Path.GetFileName(fichier)} : {l.Trim()}"));
            }
        }

        Assert.Empty(occurrences);
    }

    // ---- T183 : aucune valeur de reprise en dur, aucun backoff, aucun fallback ----

    [Fact]
    public void AucuneValeurDeReprise_NEstCodeeEnDurDansLeMoteurDeNotification()
    {
        // CLAR-NOT-02, invariant I2. Les paramètres viennent EXCLUSIVEMENT de la configuration.
        var source = LireSource("src/CIT.Plateforme.Application/Notifications/ReessayerEnvoisNotificationUseCase.cs");

        // Seul le CODE est scanné : les commentaires d'interdiction (« aucun backoff… »)
        // sont légitimes et attendus.
        var codeSeul = string.Join(
            Environment.NewLine,
            source.Split('\n')
                .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                            && !l.TrimStart().StartsWith("///", StringComparison.Ordinal)));

        Assert.DoesNotContain("TimeSpan.From", codeSeul, StringComparison.Ordinal);
        Assert.DoesNotContain("Math.Pow", codeSeul, StringComparison.Ordinal);
        Assert.DoesNotContain("backoff", codeSeul, StringComparison.OrdinalIgnoreCase);
        // Aucune valeur par défaut dans l'enregistrement de paramètres.
        Assert.DoesNotContain("NombreTentativesMax = ", codeSeul, StringComparison.Ordinal);
    }

    // ---- T201 : la garantie I9 n'emploie jamais « exactly-once » ----

    [Fact]
    public void LeTermeExactlyOnce_NApparaitNullePartDansLeCodeSource()
    {
        var occurrences = FichiersSource()
            .Where(f => File.ReadAllText(f).Contains("exactly-once", StringComparison.OrdinalIgnoreCase)
                        || File.ReadAllText(f).Contains("exactlyonce", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .ToList();

        Assert.Empty(occurrences);
    }

    // ---- T185 : aucune logique CON / Feature 010 absorbée ----

    [Fact]
    public void AucuneLogiqueDeContinuitePresentielle_NEstIntroduiteParFeature009()
    {
        // spec.md §23, invariant I4 : CON-01→04 et ERR-04 restent Feature 010.
        var fichiersF009 = FichiersSource()
            .Where(f => f.Contains("Notifications", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(fichiersF009);

        foreach (var fichier in fichiersF009)
        {
            var source = File.ReadAllText(fichier);
            Assert.DoesNotContain("RendezVous", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Creneau", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Guichet", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Agenda", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ---- T184 : aucune logique AVE / Feature 007 absorbée ----

    [Fact]
    public void AucuneLogiqueAvoirOuExoneration_NEstIntroduiteParFeature009()
    {
        var fichiersF009 = FichiersSource().Where(f => f.Contains("Notifications", StringComparison.Ordinal));

        foreach (var fichier in fichiersF009)
        {
            var source = File.ReadAllText(fichier);
            Assert.DoesNotContain("Exoneration", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("AvoirCommand", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("AVE-0", source, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ---- T199 : aucune coordonnée arbitraire ----

    [Fact]
    public void AucunPointDEntree_NAcceptteUneCoordonneeDeDestinationArbitraire()
    {
        // spec.md §13 : aucune saisie libre d'e-mail ou de téléphone destinataire, aucun
        // mécanisme de transfert, aucun tiers désignable.
        var typesF009 = typeof(DistribuerNotificationsUseCase).Assembly
            .GetTypes()
            .Where(t => t.Namespace?.Contains("Notifications", StringComparison.Ordinal) == true);

        foreach (var type in typesF009)
        {
            var parametresSuspects = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(m => m.GetParameters())
                .Where(p => p.ParameterType == typeof(string))
                .Where(p => p.Name is not null
                            && (p.Name.Contains("email", StringComparison.OrdinalIgnoreCase)
                                || p.Name.Contains("telephone", StringComparison.OrdinalIgnoreCase)
                                || p.Name.Contains("adresse", StringComparison.OrdinalIgnoreCase)
                                || p.Name.Contains("numero", StringComparison.OrdinalIgnoreCase)))
                .Select(p => $"{type.Name}.{p.Name}")
                .ToList();

            Assert.Empty(parametresSuspects);
        }
    }

    // ---- T158 : aucun destinataire collectif ----

    [Fact]
    public void AucunResolveur_NeRetourneUnDestinataireCollectif()
    {
        var implementations = typeof(IResolveurDestinataires).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && t.IsAssignableTo(typeof(IResolveurDestinataires)))
            .ToList();

        // Quatre depuis le 05/10/2026 (cf. RaccordementMentionTests, ou l'inventaire est nominatif).
        Assert.Equal(4, implementations.Count);

        // Aucun résolveur ne dépend d'un port de file ou d'habilitation globale — sauf celui des
        // superviseurs, qui lit les habilitations « réaffecter » de LA file du ticket (ALE-03).
        foreach (var implementation in implementations.Where(i => i.Name != "ResolveurSuperviseursDeLaFile"))
        {
            var dependances = implementation.GetConstructors().Single().GetParameters()
                .Select(p => p.ParameterType.Name).ToList();

            Assert.DoesNotContain(dependances, d => d.Contains("FileDeTraitement", StringComparison.Ordinal));
            Assert.DoesNotContain(dependances, d => d.Contains("Habilitation", StringComparison.Ordinal));
        }
    }
}
