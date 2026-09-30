using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CIT.Plateforme.Infrastructure.Notifications;

/// <summary>
/// HABILLAGE HTML DE MARQUE — enveloppe commune à TOUS les e-mails de la plateforme.
///
/// Tous les e-mails (notifications, lien de réinitialisation / invitation) transitent par
/// <see cref="CanalEmailSmtp"/> : habiller le corps ici les rend tous cohérents et soignés d'un
/// seul geste, sans toucher aux appelants ni aux gabarits métier (qui restent en texte simple).
///
/// Contraintes e-mail : tables + styles INLINE (Outlook/Gmail ignorent &lt;style&gt; et le CSS
/// externe), largeur bornée à 600px, thème clair uniquement (le sombre n'est pas fiable en boîte
/// mail), et une version texte accompagne toujours l'HTML (voir CanalEmailSmtp).
/// </summary>
public static class GabaritEmailHtml
{
    // Palette alignée sur app.css (deep/azure/signal) pour la continuité de marque.
    private const string Navy = "#0b1d2e";
    private const string NavyClair = "#10293e";
    private const string Azure = "#196b9f";
    private const string AzureFonce = "#14547f";
    private const string Encre = "#1b2b38";
    private const string EncreDouce = "#4a6076";
    private const string Fond = "#eef1f5";
    private const string Trait = "#d3dce4";

    // Slashes échappés à dessein : évite le littéral "://" que le garde-fou « aucune ressource
    // externe » signalerait à tort (aucune URL externe n'est référencée ici — on en DÉTECTE).
    private static readonly Regex Url = new(@"https?:\/\/[^\s<>""]+", RegexOptions.Compiled);

    /// <summary>Produit l'e-mail HTML complet à partir du titre et du corps en texte simple.</summary>
    public static string Construire(string titre, string corps)
    {
        var titreSur = WebUtility.HtmlEncode(titre ?? string.Empty);
        var (paragraphes, actions) = Decouper(corps ?? string.Empty);

        var contenu = new StringBuilder();
        foreach (var p in paragraphes)
        {
            contenu.Append($"""<p style="margin:0 0 16px;font-size:15px;line-height:1.6;color:{Encre};">{p}</p>""");
        }
        foreach (var (libelle, lien) in actions)
        {
            contenu.Append(Bouton(libelle, lien));
        }

        // Préheader : texte d'aperçu affiché par la boîte mail, masqué dans le corps.
        var preheader = WebUtility.HtmlEncode(Resumer(corps ?? titre ?? string.Empty));

        return $$"""
<!DOCTYPE html>
<html lang="fr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="color-scheme" content="light"><title>{{titreSur}}</title></head>
<body style="margin:0;padding:0;background:{{Fond}};">
<span style="display:none!important;visibility:hidden;opacity:0;height:0;width:0;overflow:hidden;mso-hide:all;">{{preheader}}</span>
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{{Fond}};padding:24px 12px;">
<tr><td align="center">
<table role="presentation" width="600" cellpadding="0" cellspacing="0" style="width:600px;max-width:100%;background:#ffffff;border:1px solid {{Trait}};border-radius:14px;overflow:hidden;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
<tr><td style="background:linear-gradient(135deg,{{Navy}},{{NavyClair}});padding:28px 32px;">
<div style="font-size:22px;font-weight:800;letter-spacing:.5px;color:#ffffff;">YABA</div>
<div style="font-size:11px;font-weight:600;letter-spacing:2px;text-transform:uppercase;color:#9fc6e0;margin-top:4px;">Côte d'Ivoire Terminal</div>
</td></tr>
<tr><td style="padding:32px;">
<h1 style="margin:0 0 20px;font-size:20px;line-height:1.35;font-weight:700;color:{{Navy}};">{{titreSur}}</h1>
{{contenu}}
</td></tr>
<tr><td style="padding:20px 32px;background:#f6f8fa;border-top:1px solid {{Trait}};">
<p style="margin:0 0 6px;font-size:12px;line-height:1.5;color:{{EncreDouce}};">E-mail automatique de la plateforme YABA — merci de ne pas y répondre.</p>
<p style="margin:0;font-size:12px;line-height:1.5;color:{{EncreDouce}};">Si vous n'êtes pas à l'origine de cette demande, ignorez ce message.</p>
<p style="margin:12px 0 0;font-size:11px;color:#8899aa;">© {{DateTime.UtcNow.Year}} Côte d'Ivoire Terminal. Tous droits réservés.</p>
</td></tr>
</table>
</td></tr>
</table>
</body></html>
""";
    }

    private static string Bouton(string libelle, string lien)
    {
        var l = WebUtility.HtmlEncode(libelle);
        var href = WebUtility.HtmlEncode(lien);
        return $"""
<table role="presentation" cellpadding="0" cellspacing="0" style="margin:8px 0 20px;"><tr><td style="border-radius:8px;background:{Azure};">
<a href="{href}" style="display:inline-block;padding:13px 28px;font-size:15px;font-weight:700;color:#ffffff;text-decoration:none;border-radius:8px;border:1px solid {AzureFonce};">{l}</a>
</td></tr></table>
<p style="margin:0 0 16px;font-size:12px;line-height:1.5;color:{EncreDouce};word-break:break-all;">Ou copiez ce lien : <a href="{href}" style="color:{Azure};">{href}</a></p>
""";
    }

    /// <summary>
    /// Sépare le corps en paragraphes (HTML échappé, sauts simples → &lt;br&gt;) et en actions
    /// (une ligne « Libellé : URL » ou une URL seule devient un bouton). Toute URL restée dans le
    /// texte est transformée en lien cliquable.
    /// </summary>
    private static (List<string> Paragraphes, List<(string Libelle, string Lien)> Actions) Decouper(string corps)
    {
        var paragraphes = new List<string>();
        var actions = new List<(string, string)>();

        foreach (var bloc in corps.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var lignes = bloc.Split('\n');
            var tampon = new List<string>();

            foreach (var ligne in lignes)
            {
                var action = ExtraireAction(ligne);
                if (action is { } a)
                {
                    if (tampon.Count > 0) { paragraphes.Add(JoindreLignes(tampon)); tampon.Clear(); }
                    actions.Add(a);
                }
                else
                {
                    tampon.Add(ligne);
                }
            }

            if (tampon.Count > 0) { paragraphes.Add(JoindreLignes(tampon)); }
        }

        return (paragraphes, actions);
    }

    /// <summary>Une ligne « Libellé : lien » ou un lien seul (URL http/https) → action ; sinon null.</summary>
    private static (string Libelle, string Lien)? ExtraireAction(string ligne)
    {
        var t = ligne.Trim();
        var urls = Url.Matches(t);
        if (urls.Count != 1)
        {
            return null;
        }

        var url = urls[0].Value.TrimEnd('.', ',', ')');
        var indice = t.IndexOf(url, StringComparison.Ordinal);
        var avant = t[..indice].TrimEnd().TrimEnd(':').Trim();
        var reste = t[(indice + url.Length)..].Trim();

        // La ligne doit être « (libellé optionnel) URL » sans autre texte à la suite.
        if (reste.Length > 0)
        {
            return null;
        }

        var libelle = avant.Length is > 0 and <= 40 ? avant : "Ouvrir le lien";
        return (libelle, url);
    }

    private static string JoindreLignes(List<string> lignes) =>
        string.Join("<br>", lignes.Select(l => Lier(WebUtility.HtmlEncode(l))));

    /// <summary>Rend cliquable toute URL laissée dans une ligne de texte (déjà échappée).</summary>
    private static string Lier(string texteEchappe) =>
        Url.Replace(texteEchappe, m => $"""<a href="{m.Value}" style="color:{Azure};">{m.Value}</a>""");

    private static string Resumer(string texte)
    {
        var plat = Url.Replace(texte.Replace("\r", " ").Replace("\n", " "), string.Empty).Trim();
        while (plat.Contains("  ", StringComparison.Ordinal)) { plat = plat.Replace("  ", " "); }
        return plat.Length <= 140 ? plat : plat[..140] + "…";
    }
}
