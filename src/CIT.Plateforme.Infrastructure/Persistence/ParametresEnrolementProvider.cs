using CIT.Plateforme.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CIT.Plateforme.Infrastructure.Persistence;

public sealed class ParametresEnrolementProvider(IConfiguration configuration) : IParametresEnrolementProvider
{
    public int? ObtenirDureeValiditeBadgeJours()
    {
        var val = configuration["Enrolement:DureeValiditeBadgeJours"];
        return int.TryParse(val, out var res) ? res : null;
    }

    public int? ObtenirDelaiRappelExpirationJours()
    {
        var val = configuration["Enrolement:DelaiRappelExpirationJours"];
        return int.TryParse(val, out var res) ? res : null;
    }
}
