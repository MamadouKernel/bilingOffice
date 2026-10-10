using Microsoft.AspNetCore.DataProtection;

namespace CIT.Plateforme.Web.Services;

/// <summary>État d'un emprunt d'identité en cours, tel que porté par le cookie protégé.</summary>
public sealed record EtatEmprunt(Guid AdministrateurId, string AdministrateurNom, DateTimeOffset EcheanceUtc);

/// <summary>
/// Porte l'état d'un emprunt d'identité dans un cookie CHIFFRÉ + SIGNÉ (ASP.NET Data Protection),
/// indépendant du cookie d'authentification Identity.
///
/// POURQUOI un cookie dédié et non un claim. La plateforme règle
/// <c>SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero</c> : à chaque requête, Identity
/// reconstruit le principal depuis la base et EFFACE tout claim ajouté à la main (seul <c>amr</c> est
/// préservé par <c>RefreshSignInAsync</c>). Un claim « emprunteur » ne survivrait donc pas à la requête
/// suivante — on perdrait le bandeau et le retour. Le cookie protégé, lui, n'est pas touché par cette
/// revalidation, et reste inviolable : un cookie forgé ne peut pas désigner un administrateur arbitraire
/// pour le retour (le contenu est chiffré et signé par la clé Data Protection du serveur).
/// </summary>
public sealed class ImpersonationCookie(IDataProtectionProvider fournisseur)
{
    public const string NomCookie = "cit.imp";

    private readonly IDataProtector _protecteur = fournisseur.CreateProtector("CIT.Impersonation.v1");

    public void Deposer(HttpResponse reponse, EtatEmprunt etat)
    {
        // Le '|' n'apparaît ni dans un Guid ni dans un timestamp ; on neutralise sa présence éventuelle
        // dans le nom d'affichage (donnée de confort, non sensible).
        var nom = (etat.AdministrateurNom ?? string.Empty).Replace('|', ' ');
        var charge = $"{etat.AdministrateurId}|{etat.EcheanceUtc.ToUnixTimeSeconds()}|{nom}";

        reponse.Cookies.Append(NomCookie, _protecteur.Protect(charge), new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = true,
        });
    }

    public EtatEmprunt? Lire(HttpRequest requete)
    {
        if (!requete.Cookies.TryGetValue(NomCookie, out var protege) || string.IsNullOrEmpty(protege))
        {
            return null;
        }

        try
        {
            var parties = _protecteur.Unprotect(protege).Split('|', 3);
            if (parties.Length < 2 || !Guid.TryParse(parties[0], out var adminId) || !long.TryParse(parties[1], out var secondes))
            {
                return null;
            }

            var nom = parties.Length == 3 ? parties[2] : string.Empty;
            return new EtatEmprunt(adminId, nom, DateTimeOffset.FromUnixTimeSeconds(secondes));
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Cookie illisible (clé renouvelée, falsification) : traité comme absent.
            return null;
        }
    }

    public void Supprimer(HttpResponse reponse) => reponse.Cookies.Delete(NomCookie);
}
