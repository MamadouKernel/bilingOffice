using CIT.Plateforme.Application.Abstractions;
using CIT.Plateforme.Domain.Comptes;
using Microsoft.EntityFrameworkCore;

namespace CIT.Plateforme.Infrastructure.Persistence;

/// <summary>CPT-01 — référentiel paramétrable des justificatifs exigés à l'ouverture d'un compte.</summary>
public sealed class PieceExigeeCompteRepository(PlateformeDbContext dbContext) : IPieceExigeeCompteRepository
{
    public async Task<IReadOnlyList<PieceExigeeCompte>> ListerAsync(bool activesUniquement, CancellationToken ct = default)
    {
        var requete = dbContext.PiecesExigeesCompte.AsQueryable();
        if (activesUniquement)
        {
            requete = requete.Where(p => p.EstActif);
        }

        return await requete.OrderBy(p => p.Ordre).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<PieceExigeeCompte?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.PiecesExigeesCompte.SingleOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);

    public async Task<bool> CodeExisteAsync(string code, CancellationToken ct = default) =>
        await dbContext.PiecesExigeesCompte.AnyAsync(p => p.Code == code, ct).ConfigureAwait(false);

    public async Task AjouterAsync(PieceExigeeCompte piece, CancellationToken ct = default) =>
        await dbContext.PiecesExigeesCompte.AddAsync(piece, ct).ConfigureAwait(false);
}

/// <summary>CPT-01 — justificatifs déposés à l'appui d'une demande de compte.</summary>
public sealed class JustificatifDemandeCompteRepository(PlateformeDbContext dbContext) : IJustificatifDemandeCompteRepository
{
    public async Task<IReadOnlyList<JustificatifDemandeCompte>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) =>
        await dbContext.JustificatifsDemandeCompte
            .Where(j => j.SocieteId == societeId)
            .OrderBy(j => j.PieceCode)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AjouterAsync(JustificatifDemandeCompte justificatif, CancellationToken ct = default) =>
        await dbContext.JustificatifsDemandeCompte.AddAsync(justificatif, ct).ConfigureAwait(false);
}

/// <summary>ENT-01 — invitations de collaborateurs.</summary>
public sealed class InvitationCollaborateurRepository(PlateformeDbContext dbContext) : IInvitationCollaborateurRepository
{
    public async Task AjouterAsync(InvitationCollaborateur invitation, CancellationToken ct = default) =>
        await dbContext.InvitationsCollaborateur.AddAsync(invitation, ct).ConfigureAwait(false);

    public async Task<InvitationCollaborateur?> ObtenirParJetonAsync(string jeton, CancellationToken ct = default) =>
        await dbContext.InvitationsCollaborateur.SingleOrDefaultAsync(i => i.Jeton == jeton, ct).ConfigureAwait(false);

    public async Task<InvitationCollaborateur?> ObtenirParIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.InvitationsCollaborateur.SingleOrDefaultAsync(i => i.Id == id, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<InvitationCollaborateur>> ListerParSocieteAsync(Guid societeId, CancellationToken ct = default) =>
        await dbContext.InvitationsCollaborateur
            .Where(i => i.SocieteId == societeId)
            .OrderByDescending(i => i.DateCreationUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
