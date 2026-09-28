using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CIT.Plateforme.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CIT.Plateforme.Infrastructure.Navis;

/// <summary>
/// Adaptateur REST de l'Universal Query API de Navis N4 — LECTURE SEULE.
///
/// <c>GET {BaseUrl}/api/query?filtername=…&amp;operatorId=…&amp;complexId=…&amp;facilityId=…&amp;yardId=…&amp;PARM_x=…</c>,
/// authentification HTTP Basic, réponse XML <c>&lt;query-response&gt;</c>.
///
/// Toute défaillance (désactivé, mal configuré, réseau, code HTTP ≠ 2xx, élément
/// <c>&lt;error&gt;</c>, XML illisible) devient un <see cref="ResultatNavis.Indisponible"/> :
/// l'assistant ne plante jamais et se rabat sur YABA.
/// </summary>
public sealed class SourceNavisRest(
    HttpClient http, IOptions<NavisOptions> options, ILogger<SourceNavisRest> logger) : ISourceNavis
{
    private readonly NavisOptions _opt = options.Value;

    public async Task<ResultatNavis> InterrogerAsync(
        string filtre, IReadOnlyDictionary<string, string> parametres, CancellationToken ct = default)
    {
        if (!_opt.EstConfiguree)
        {
            return ResultatNavis.Indisponible("Navis non configuré ou désactivé.");
        }

        var url = ConstruireUrl(filtre, parametres);

        try
        {
            using var requete = new HttpRequestMessage(HttpMethod.Get, url);
            var jeton = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opt.Utilisateur}:{_opt.MotDePasse}"));
            requete.Headers.Authorization = new AuthenticationHeaderValue("Basic", jeton);

            using var reponse = await http.SendAsync(requete, ct).ConfigureAwait(false);
            var corps = await reponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!reponse.IsSuccessStatusCode)
            {
                logger.LogWarning("Navis a répondu {Code} pour le filtre {Filtre}.", (int)reponse.StatusCode, filtre);
                return ResultatNavis.Indisponible($"Navis a répondu {(int)reponse.StatusCode}.");
            }

            return Analyser(corps);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogWarning(ex, "Navis injoignable pour le filtre {Filtre}.", filtre);
            return ResultatNavis.Indisponible("Navis injoignable.");
        }
    }

    private string ConstruireUrl(string filtre, IReadOnlyDictionary<string, string> parametres)
    {
        var sb = new StringBuilder(_opt.BaseUrl.TrimEnd('/'))
            .Append("/api/query?filtername=").Append(Uri.EscapeDataString(filtre))
            .Append("&operatorId=").Append(Uri.EscapeDataString(_opt.OperatorId))
            .Append("&complexId=").Append(Uri.EscapeDataString(_opt.ComplexId))
            .Append("&facilityId=").Append(Uri.EscapeDataString(_opt.FacilityId))
            .Append("&yardId=").Append(Uri.EscapeDataString(_opt.YardId));

        // Les paramètres de filtre sont préfixés PARM_ (cf. N4 Public APIs §1.3.5/1.3.6).
        foreach (var (cle, valeur) in parametres)
        {
            sb.Append("&PARM_").Append(Uri.EscapeDataString(cle)).Append('=').Append(Uri.EscapeDataString(valeur));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parse <c>&lt;query-response&gt;</c>. Un <c>&lt;error&gt;</c> devient « indisponible » (motif
    /// renvoyé, cf. §1.3.8).
    ///
    /// ponytail: parsage GÉNÉRIQUE des lignes — chaque élément enfant porteur de sous-éléments est
    /// une ligne, chaque sous-élément une colonne (nom → texte). Validé sur les formes error/recap
    /// documentées ; à resserrer quand un vrai filtre « défaut » CIT fournira son schéma réel.
    /// </summary>
    private static ResultatNavis Analyser(string xml)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException)
        {
            return ResultatNavis.Indisponible("Réponse Navis illisible.");
        }

        var racine = doc.Root;
        if (racine is null)
        {
            return ResultatNavis.Indisponible("Réponse Navis vide.");
        }

        var erreur = racine.Element("error");
        if (erreur is not null)
        {
            return ResultatNavis.Indisponible(erreur.Value.Trim());
        }

        var lignes = new List<LigneNavis>();
        foreach (var element in racine.Elements())
        {
            if (!element.HasElements)
            {
                continue;
            }

            var colonnes = element.Elements()
                .GroupBy(c => c.Name.LocalName)
                .ToDictionary(g => g.Key, g => g.First().Value.Trim());

            if (colonnes.Count > 0)
            {
                lignes.Add(new LigneNavis(colonnes));
            }
        }

        return ResultatNavis.Succes(lignes);
    }
}
