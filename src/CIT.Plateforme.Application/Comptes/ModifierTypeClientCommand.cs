using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Securite;

namespace CIT.Plateforme.Application.Comptes;

/// <summary>
/// Contrat C2-12 — Modifier le type de client d'une société. CDC : CPT-06. N'a jamais d'effet
/// rétroactif : `Dossier.ClientId` (Feature 001) des dossiers déjà créés n'est jamais réécrit.
/// </summary>
public sealed class ModifierTypeClientCommand(
    ISocieteClienteRepository societes,
    IAutorisationService autorisation,
    IJournalisationService journalisation,
    IUniteDeTravail uniteDeTravail)
{
    public async Task ExecuterAsync(Guid appelantId, Guid societeId, TypeClient nouveauProfil, CancellationToken ct = default)
    {
        await autorisation.ExigerAsync(appelantId, Permission.CompteTypeModifier, ct: ct).ConfigureAwait(false);

        var societe = await societes.ObtenirParIdAsync(societeId, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Société {societeId} introuvable.");

        var ancienProfil = societe.Profil.ToString();
        societe.ModifierType(nouveauProfil);

        await journalisation.ConsignerAuditAsync(appelantId, "TypeClientModifie", nameof(SocieteCliente), societe.Id, ancienProfil, nouveauProfil.ToString(), ct).ConfigureAwait(false);

        await uniteDeTravail.EnregistrerAsync(ct).ConfigureAwait(false);
    }
}
