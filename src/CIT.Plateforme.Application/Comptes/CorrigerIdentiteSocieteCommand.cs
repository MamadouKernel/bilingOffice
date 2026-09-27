using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Correction administrative de l'identité structurée d'une société (raison sociale, RCCM) —
/// notamment pour les sociétés reprises dont le RCCM n'a pas pu être déduit. Même habilitation
/// que la modification du type de client ; l'ancienne et la nouvelle valeur sont consignées à
/// l'audit ; le RCCM reste unique (la société corrigée elle-même exceptée).
/// </summary>
public sealed class CorrigerIdentiteSocieteCommand(
    ISocieteClienteRepository societes,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(
        Guid appelantId, Guid societeId, string raisonSociale, string rccm, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.CompteTypeModifier, ct: ct).ConfigureAwait(false);

        var societe = await societes.ObtenirParIdAsync(societeId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Société {societeId} introuvable.");

        var rccmNormalise = NumeroRccm.Normaliser(rccm);
        if (await societes.RccmDejaUtiliseAsync(rccmNormalise, saufSocieteId: societe.Id, ct).ConfigureAwait(false))
        {
            throw new RccmDejaUtiliseException(rccmNormalise);
        }

        var avant = $"{societe.RaisonSociale} | {societe.Rccm}";
        societe.RenseignerIdentite(raisonSociale, rccmNormalise);

        await journalisation.ConsignerAuditAsync(
            appelantId, "IdentiteSocieteCorrigee", nameof(SocieteCliente), societe.Id,
            avant, $"{societe.RaisonSociale} | {societe.Rccm}", ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
