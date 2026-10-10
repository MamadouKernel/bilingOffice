using CIT.Plateforme.Application.Securite;
using Xunit;

namespace CIT.Plateforme.Application.Tests.Securite;

/// <summary>
/// La garde d'emprunt d'identité est une règle de sécurité : chaque condition de refus est
/// verrouillée ici, et l'ordre d'évaluation (permission d'abord, mot de passe avant la résolution de
/// la cible) est délibéré — il ne doit rien révéler à qui n'a pas le droit d'emprunter.
/// </summary>
public sealed class ImpersonationGuardTests
{
    private static VerdictImpersonation Decider(
        bool permission = true, bool cibleExiste = true, bool autoEmprunt = false,
        bool dejaEnEmprunt = false, bool motDePasse = true, bool cibleProtegee = false) =>
        ImpersonationGuard.Decider(permission, cibleExiste, autoEmprunt, dejaEnEmprunt, motDePasse, cibleProtegee);

    [Fact]
    public void Toutes_conditions_reunies_autorise() =>
        Assert.Equal(VerdictImpersonation.Autorise, Decider());

    [Fact]
    public void Sans_permission_refuse_en_premier() =>
        // Même avec tout le reste valide, l'absence de permission prime : on ne va pas plus loin.
        Assert.Equal(VerdictImpersonation.PermissionManquante, Decider(permission: false, cibleExiste: false, motDePasse: false));

    [Fact]
    public void Emprunt_imbrique_refuse() =>
        Assert.Equal(VerdictImpersonation.DejaEnEmprunt, Decider(dejaEnEmprunt: true));

    [Fact]
    public void Mot_de_passe_invalide_refuse_avant_de_resoudre_la_cible() =>
        // La confirmation d'identité est vérifiée AVANT l'existence de la cible : un mot de passe
        // faux ne doit jamais permettre de sonder quels comptes existent.
        Assert.Equal(VerdictImpersonation.MotDePasseInvalide, Decider(motDePasse: false, cibleExiste: false));

    [Fact]
    public void Cible_introuvable_refuse() =>
        Assert.Equal(VerdictImpersonation.CibleIntrouvable, Decider(cibleExiste: false));

    [Fact]
    public void Auto_emprunt_refuse() =>
        Assert.Equal(VerdictImpersonation.AutoEmprunt, Decider(autoEmprunt: true));

    [Fact]
    public void Cible_elle_meme_habilitee_a_l_emprunt_est_protegee() =>
        // Pas d'escalade latérale : on n'emprunte pas un autre administrateur habilité à l'impersonation.
        Assert.Equal(VerdictImpersonation.CibleProtegee, Decider(cibleProtegee: true));

    // ── Expiration de l'emprunt ──

    [Fact]
    public void Emprunt_sans_echeance_est_traite_comme_expire() =>
        // Un claim d'échéance absent ou illisible ne doit jamais laisser courir un emprunt.
        Assert.True(ImpersonationGuard.EmpruntExpire(null, DateTimeOffset.UtcNow));

    [Fact]
    public void Emprunt_dont_l_echeance_est_passee_est_expire()
    {
        var maintenant = DateTimeOffset.UtcNow;
        Assert.True(ImpersonationGuard.EmpruntExpire(maintenant.AddSeconds(-1), maintenant));
    }

    [Fact]
    public void Emprunt_dans_sa_fenetre_n_est_pas_expire()
    {
        var maintenant = DateTimeOffset.UtcNow;
        Assert.False(ImpersonationGuard.EmpruntExpire(maintenant.Add(ImpersonationGuard.DureeEmprunt), maintenant));
    }
}
