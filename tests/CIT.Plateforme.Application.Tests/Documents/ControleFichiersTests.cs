using System.Text;
using CIT.Plateforme.Application.Documents;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Documents;

/// <summary>
/// TEST-SPEC SEC-CERT-07-01, 07-02, 07-03 : Contrôle de Type, Taille et Magic Bytes.
/// </summary>
public sealed class ControleFichiersTests
{
    private readonly ValidateurFichierDocument _validateur = new();

    [Fact]
    public void SecCert07_01_FichierPdfValide_EstAccepte()
    {
        // En-tête standard PDF : %PDF-1.4
        var headerPdf = Encoding.ASCII.GetBytes("%PDF-1.4\nContenu valide");
        using var stream = new MemoryStream(headerPdf);

        var (valide, erreur) = _validateur.Valider("dossier.pdf", headerPdf.Length, stream);

        Assert.True(valide, $"Le fichier PDF valide doit être accepté. Erreur: {erreur}");
        Assert.Null(erreur);
    }

    [Fact]
    public void SecCert07_02_ExecutableMasqueEnPdf_EstRejeteParMagicBytes()
    {
        // En-tête standard exécutable Windows : MZ (0x4D, 0x5A)
        var fauxPdf = Encoding.ASCII.GetBytes("MZ\x90\x00\x03\x00\x00\x00CodeMalveillant");
        using var stream = new MemoryStream(fauxPdf);

        var (valide, erreur) = _validateur.Valider("malware.pdf", fauxPdf.Length, stream);

        Assert.False(valide, "Un fichier .exe renommé .pdf doit être rejeté par Magic Bytes.");
        Assert.Contains("signature binaire", erreur ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SecCert07_03_FichierDepassantTailleMax_EstRejete()
    {
        // 11 Mo > 10 Mo
        const long taille11Mo = 11 * 1024 * 1024;
        var headerPdf = Encoding.ASCII.GetBytes("%PDF-1.4");
        using var stream = new MemoryStream(headerPdf);

        var (valide, erreur) = _validateur.Valider("gros_fichier.pdf", taille11Mo, stream);

        Assert.False(valide, "Un fichier supérieur au plafond de 10 Mo doit être rejeté.");
        Assert.Contains("taille", erreur ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SecCert07_01_ExtensionNonAutorisee_EstRejetee()
    {
        var contenu = Encoding.ASCII.GetBytes("script.sh");
        using var stream = new MemoryStream(contenu);

        var (valide, erreur) = _validateur.Valider("script.sh", contenu.Length, stream);

        Assert.False(valide);
        Assert.Contains("type de fichier", erreur ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }
}
