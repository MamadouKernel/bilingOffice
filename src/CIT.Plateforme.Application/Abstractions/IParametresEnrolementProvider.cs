namespace CIT.Plateforme.Application.Abstractions;

public interface IParametresEnrolementProvider
{
    int? ObtenirDureeValiditeBadgeJours();
    int? ObtenirDelaiRappelExpirationJours();
}
