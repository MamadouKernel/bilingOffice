using System.Text;
using CIT.Plateforme.Application.Securite;
using QRCoder;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>
/// Rendu LOCAL d'un QR code au format SVG, encodé en data URI.
///
/// Remplace l'appel à <c>api.qrserver.com</c> qui transmettait le secret TOTP à un tiers dans
/// une query string. Ici rien ne sort du serveur : la matrice est calculée en mémoire et
/// l'image est embarquée dans la réponse HTML. L'enrôlement 2FA fonctionne donc hors ligne.
///
/// Portabilité : seule la surcharge <c>GetGraphic(int)</c> de QRCoder est utilisée. Elle ne
/// touche pas GDI+ (uniquement les primitives System.Drawing), ce qui garde le rendu valide
/// sur une cible non Windows.
/// </summary>
public sealed class GenerateurQrCodeSvg : IGenerateurQrCode
{
    /// <summary>
    /// Niveau de correction d'erreur Q (~25 %) : tolère la photo d'un écran par un smartphone
    /// sans gonfler la matrice autant que le niveau H.
    /// </summary>
    private const QRCodeGenerator.ECCLevel NiveauCorrection = QRCodeGenerator.ECCLevel.Q;

    /// <summary>
    /// Côté d'un module dans le SVG. Le rendu étant vectoriel, cette valeur ne fixe pas la
    /// taille affichée (pilotée par le CSS) mais la précision de la grille.
    /// </summary>
    private const int PixelsParModule = 8;

    /// <inheritdoc />
    public string GenererDataUri(string contenu)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contenu);

        using var generateur = new QRCodeGenerator();
        using var donnees = generateur.CreateQrCode(contenu, NiveauCorrection);
        using var qrCode = new SvgQRCode(donnees);

        var svg = qrCode.GetGraphic(PixelsParModule);

        // Encodage base64 plutôt qu'un SVG inline : le contenu est sensible et ne doit pas être
        // interpolé tel quel dans le balisage de la page.
        return $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";
    }
}
