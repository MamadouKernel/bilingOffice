namespace CIT.Plateforme.Domain.Documents;

/// <summary>
/// SOC-07 (Must) — « Dépôt de documents par le client et par les agents, avec types de pièces
/// <b>paramétrables par étape</b> ; formats acceptés et taille maximale configurables. »
///
/// DÉFAUT CORRIGÉ (audit du 2026-09-03, <c>docs/88-audit-conformite-cdc.md</c>) : formats et
/// taille maximale étaient bien configurables (<c>ValidateurFichierDocument</c>), mais aucun
/// référentiel ne disait quelles pièces sont attendues à quelle étape. Le CDC l'exige en quatre
/// endroits — SOC-07, § 6, § 17.3 et la liste des référentiels paramétrables — et la matrice
/// <c>docs/33</c> donnait pourtant SOC-07 pour certifiée.
///
/// <see cref="Comptes.PieceExigeeCompte"/> couvre l'OUVERTURE DE COMPTE. Cette entité couvre les
/// étapes du PARCOURS. Les deux référentiels restent séparés parce qu'ils sont administrés par
/// des rôles différents et n'ont pas le même cycle de vie ; les fusionner aurait mélangé deux
/// paramétrages que le CDC nomme séparément.
///
/// L'ÉTAPE EST MATÉRIALISÉE PAR LA FILE : le MOT §8.3 décrit chaque étape du parcours comme une
/// file — Contrôle BL, Facturation, Contrôle Documentaire. Introduire une entité « étape »
/// distincte inventerait une notion absente du CDC comme du modèle.
///
/// AUCUNE PIÈCE N'EST LIVRÉE PAR DÉFAUT. Le CDC est explicite : « Liste exacte des pièces exigées
/// par étape et par parcours, <b>à valider avec les responsables métiers</b> ». Tant que le
/// référentiel est vide, aucune exigence documentaire nouvelle ne s'applique — le comportement
/// reste celui d'avant.
/// </summary>
public sealed class PieceExigeeEtape
{
    public Guid Id { get; private set; }

    /// <summary>Identifiant stable, indépendant du libellé : c'est lui que porte le document
    /// déposé, pour qu'une reformulation ne délie pas les dépôts déjà effectués.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Libelle { get; private set; } = string.Empty;

    /// <summary>L'étape à laquelle la pièce est attendue, c'est-à-dire la file (MOT §8.3).</summary>
    public Guid FileId { get; private set; }

    /// <summary>Restreint la pièce à un module. <c>null</c> = attendue quel que soit le module,
    /// même convention que les paramètres SLA et les règles de diffusion.</summary>
    public string? Module { get; private set; }

    /// <summary>Une pièce facultative est proposée sans bloquer ; seule une pièce obligatoire
    /// entre dans le contrôle de complétude de l'étape.</summary>
    public bool Obligatoire { get; private set; }

    /// <summary>Désactiver plutôt que supprimer : les documents déjà déposés référencent ce code,
    /// et leur historique doit rester lisible (SEC-05).</summary>
    public bool EstActif { get; private set; }

    public int Ordre { get; private set; }

    private PieceExigeeEtape() { }

    public static PieceExigeeEtape Definir(
        string code, string libelle, Guid fileId, bool obligatoire, int ordre, string? module = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code de la pièce est obligatoire.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé de la pièce est obligatoire.", nameof(libelle));
        }

        if (fileId == Guid.Empty)
        {
            throw new ArgumentException(
                "L'étape (la file) est obligatoire : une pièce « exigée par étape » sans étape ne " +
                "serait exigée nulle part.", nameof(fileId));
        }

        return new PieceExigeeEtape
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Libelle = libelle.Trim(),
            FileId = fileId,
            Module = string.IsNullOrWhiteSpace(module) ? null : module.Trim(),
            Obligatoire = obligatoire,
            EstActif = true,
            Ordre = ordre,
        };
    }

    public void Modifier(string libelle, bool obligatoire, int ordre)
    {
        if (string.IsNullOrWhiteSpace(libelle))
        {
            throw new ArgumentException("Le libellé de la pièce est obligatoire.", nameof(libelle));
        }

        Libelle = libelle.Trim();
        Obligatoire = obligatoire;
        Ordre = ordre;
    }

    public void Activer() => EstActif = true;

    public void Desactiver() => EstActif = false;

    /// <summary>La pièce est-elle attendue dans ce contexte ?</summary>
    public bool EstAttendue(string module, Guid fileId) =>
        EstActif
        && FileId == fileId
        && (Module is null || string.Equals(Module, module, StringComparison.OrdinalIgnoreCase));
}
