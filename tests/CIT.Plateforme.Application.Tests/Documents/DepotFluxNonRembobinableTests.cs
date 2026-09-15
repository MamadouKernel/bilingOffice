using System.Text;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tests.Support;
using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Securite;
using CIT.Plateforme.Domain.Tickets;
using CIT.Plateforme.Infrastructure.Identity;
using CIT.Plateforme.Infrastructure.Persistence;
using Moq;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Documents;

/// <summary>
/// REMÉDIATION RECETTE UAT (D-11a) — LE FLUX D'UN NAVIGATEUR NE SE REMBOBINE PAS.
///
/// Le dépôt lit le contenu deux fois : stockage, puis analyse antivirale. Entre les deux, le code
/// se contentait de <c>contenu.Position = 0</c>. Cela fonctionne pour un <see cref="MemoryStream"/>
/// — donc dans tous les tests unitaires écrits jusqu'ici — et échoue systématiquement pour le flux
/// d'un <c>InputFile</c> Blazor, qui est à passe unique. Quatre actions de dépôt étaient
/// inexploitables en production pour cette seule raison, dont l'émission du Draft IN (RG-EXP-03).
///
/// <b>Le double ci-dessous refuse explicitement le repositionnement</b>, exactement comme le fait
/// le flux réel du navigateur. C'est ce qui rend ces tests capables de voir le défaut, là où un
/// <c>MemoryStream</c> ne le pouvait pas.
/// </summary>
public sealed class DepotFluxNonRembobinableTests
{
    /// <summary>
    /// Flux à passe unique : lisible une fois, jamais rembobinable — comme
    /// <c>RemoteBrowserFileStream</c>. Toute tentative de repositionnement lève, au lieu de
    /// réussir silencieusement.
    /// </summary>
    private sealed class FluxAPasseUnique(byte[] donnees) : Stream
    {
        private readonly MemoryStream _source = new(donnees);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException("Le flux du navigateur ne connaît pas sa position.");
            set => throw new NotSupportedException("Le flux du navigateur ne se rembobine pas.");
        }

        public override int Read(byte[] buffer, int offset, int count) => _source.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException("Le flux du navigateur ne se rembobine pas.");

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _source.Dispose();
            base.Dispose(disposing);
        }
    }

    private sealed class AntivirusQuiLitVraiment : IAnalyseAntivirus
    {
        public byte[]? ContenuAnalyse { get; private set; }

        public async Task<StatutAnalyseAntivirus> AnalyserAsync(Stream contenu, CancellationToken ct = default)
        {
            using var tampon = new MemoryStream();
            await contenu.CopyToAsync(tampon, ct);
            ContenuAnalyse = tampon.ToArray();
            return StatutAnalyseAntivirus.Sain;
        }
    }

    private static readonly ReglesDepotDocument Regles = new(["pdf"], TailleMaxOctets: 10 * 1024 * 1024);

    // -------------------------------------------------------------------------------------
    // FluxRejouable — le comportement isolé
    // -------------------------------------------------------------------------------------

    [Fact]
    public async Task Un_flux_rembobinable_est_utilise_tel_quel_sans_copie()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 contenu"));

        await using var rejouable = await FluxRejouable.CreerAsync(source, 1024);

        Assert.False(rejouable.ACopie);
        Assert.Same(source, rejouable.Flux);
    }

    [Fact]
    public async Task Un_flux_a_passe_unique_est_recopie_et_devient_relisable()
    {
        var octets = Encoding.UTF8.GetBytes("%PDF-1.4 contenu de recette");
        using var source = new FluxAPasseUnique(octets);

        await using var rejouable = await FluxRejouable.CreerAsync(source, 1024);

        Assert.True(rejouable.ACopie);

        using var premiere = new MemoryStream();
        await rejouable.Flux.CopyToAsync(premiere);

        rejouable.Rembobiner();
        using var seconde = new MemoryStream();
        await rejouable.Flux.CopyToAsync(seconde);

        Assert.Equal(octets, premiere.ToArray());
        Assert.Equal(octets, seconde.ToArray());
    }

    [Fact]
    public async Task Un_flux_a_passe_unique_qui_depasse_le_plafond_est_refuse()
    {
        // Le plafond n'est pas décoratif : la taille annoncée par le navigateur est une
        // déclaration, pas une garantie.
        using var source = new FluxAPasseUnique(new byte[4096]);

        var exception = await Assert.ThrowsAsync<DepotDocumentRefuseException>(
            () => FluxRejouable.CreerAsync(source, tailleMaxOctets: 1024));

        Assert.Contains("taille maximale", exception.Message);
    }

    // -------------------------------------------------------------------------------------
    // Le dépôt complet, avec un flux de navigateur
    // -------------------------------------------------------------------------------------

    [Fact]
    public async Task Un_depot_depuis_un_flux_de_navigateur_aboutit_et_le_contenu_est_integralement_analyse()
    {
        var octets = Encoding.UTF8.GetBytes("%PDF-1.4 justificatif de recette UAT");
        var (db, cmd, agentId, ticketId, stockage, antivirus) = PreparerDepot();

        using var flux = new FluxAPasseUnique(octets);

        var document = await cmd.DeposerPourAgentAsync(
            agentId, ticketId, "draft-in.pdf", "application/pdf", flux, octets.Length, Regles);

        Assert.Equal(StatutAnalyseAntivirus.Sain, document.StatutAntivirus);

        // Le contenu écrit dans le stockage est complet…
        Assert.Equal(octets, stockage.ContenuEcrit);

        // …et l'antivirus a bien vu le MÊME contenu, en entier : c'est la seconde lecture qui
        // échouait auparavant.
        Assert.Equal(octets, antivirus.ContenuAnalyse);
    }

    [Fact]
    public async Task Un_depot_depuis_un_flux_rembobinable_reste_inchange()
    {
        // Non-régression : le chemin qui fonctionnait déjà ne doit rien perdre.
        var octets = Encoding.UTF8.GetBytes("%PDF-1.4 piece deja fonctionnelle");
        var (db, cmd, agentId, ticketId, stockage, antivirus) = PreparerDepot();

        using var flux = new MemoryStream(octets);

        var document = await cmd.DeposerPourAgentAsync(
            agentId, ticketId, "piece.pdf", "application/pdf", flux, octets.Length, Regles);

        Assert.Equal(StatutAnalyseAntivirus.Sain, document.StatutAntivirus);
        Assert.Equal(octets, stockage.ContenuEcrit);
        Assert.Equal(octets, antivirus.ContenuAnalyse);
    }

    [Fact]
    public async Task Une_extension_refusee_donne_un_refus_metier_et_non_une_erreur_technique()
    {
        var (db, cmd, agentId, ticketId, _, _) = PreparerDepot();
        using var flux = new FluxAPasseUnique(Encoding.UTF8.GetBytes("MZ"));

        var exception = await Assert.ThrowsAsync<DepotDocumentRefuseException>(
            () => cmd.DeposerPourAgentAsync(
                agentId, ticketId, "charge-utile.exe", "application/octet-stream", flux, 2, Regles));

        Assert.Contains(".exe", exception.Message);
    }

    // -------------------------------------------------------------------------------------

    private sealed class StockageEspion : IStockageDocuments
    {
        public byte[]? ContenuEcrit { get; private set; }

        public async Task<string> EnregistrerAsync(Stream contenu, string nomFichier, CancellationToken ct = default)
        {
            using var tampon = new MemoryStream();
            await contenu.CopyToAsync(tampon, ct);
            ContenuEcrit = tampon.ToArray();
            return $"cle-{nomFichier}";
        }

        public Task<Stream> LireAsync(string cleStockage, CancellationToken ct = default) =>
            Task.FromResult<Stream>(new MemoryStream(ContenuEcrit ?? []));
    }

    private static (PlateformeDbContext Db, DeposerDocumentCommand Cmd, Guid AgentId, Guid TicketId,
        StockageEspion Stockage, AntivirusQuiLitVraiment Antivirus) PreparerDepot()
    {
        var db = ContexteDeTest.CreerNouveauContexte();
        var horloge = HorlogeDeTest.Defaut();
        var uow = new UniteDeTravail(db);
        var journal = new JournalisationService(db, horloge);
        var docsRepo = new DocumentRepository(db);
        var ticketsRepo = new TicketRepository(db);
        var autorisation = new AutorisationService(db);
        var cloisonnement = new CloisonnementService(ticketsRepo, FabriqueRequetes.ControleAcces(db));

        var agentId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        var dossier = Dossier.Creer(
            "DOS-UAT-0001",
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, clientId, NatureUtilisateur.Client),
            horloge.MaintenantUtc);
        var ticket = Ticket.Creer("TCK-UAT-0001", "BillingExport", "Export", fileId, dossier.Id, horloge.MaintenantUtc);

        db.Dossiers.Add(dossier);
        db.Tickets.Add(ticket);
        db.Habilitations.Add(HabilitationUtilisateur.Attribuer(
            CIT.Plateforme.Tests.Partages.ComptesDeTest.CompteActif(db, agentId), Permission.DocumentDeposer, fileId));
        db.SaveChanges();

        var stockage = new StockageEspion();
        var antivirus = new AntivirusQuiLitVraiment();

        var cmd = new DeposerDocumentCommand(
            docsRepo, ticketsRepo, stockage, antivirus, autorisation, cloisonnement, journal, horloge, uow);

        return (db, cmd, agentId, ticket.Id, stockage, antivirus);
    }
}
