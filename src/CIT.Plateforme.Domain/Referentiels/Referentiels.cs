using CIT.Plateforme.Domain.Documents;
using CIT.Plateforme.Domain.Incidents;
using CIT.Plateforme.Domain.Notifications;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Domain.Referentiels;

// Référentiels métier issus de la normalisation (3FN) des énumérations affichées aux usagers.
// Une classe par entité Merise (une table chacune) ; tout le comportement est dans la base.

/// <summary>Gravités d'incident technique (Support IT) — table <c>GravitesIncident</c>.</summary>
public sealed class ReferentielGraviteIncident : ValeurReferentielle<GraviteIncident>
{
    private ReferentielGraviteIncident() { }

    public ReferentielGraviteIncident(GraviteIncident code, string libelleFr, string libelleEn, int ordre)
        : base(code, libelleFr, libelleEn, ordre) { }
}

/// <summary>Motifs de clôture d'un ticket — table <c>MotifsCloture</c>.</summary>
public sealed class ReferentielMotifCloture : ValeurReferentielle<MotifClotureType>
{
    private ReferentielMotifCloture() { }

    public ReferentielMotifCloture(MotifClotureType code, string libelleFr, string libelleEn, int ordre)
        : base(code, libelleFr, libelleEn, ordre) { }
}

/// <summary>Catégories de pièces du dépôt Billing Import (IMP-01) — table <c>CategoriesDocument</c>.</summary>
public sealed class ReferentielCategorieDocument : ValeurReferentielle<CategorieDocumentBillingImport>
{
    private ReferentielCategorieDocument() { }

    public ReferentielCategorieDocument(CategorieDocumentBillingImport code, string libelleFr, string libelleEn, int ordre)
        : base(code, libelleFr, libelleEn, ordre) { }
}

/// <summary>Canaux de diffusion des notifications — table <c>CanauxNotification</c>.</summary>
public sealed class ReferentielCanalNotification : ValeurReferentielle<CanalNotification>
{
    private ReferentielCanalNotification() { }

    public ReferentielCanalNotification(CanalNotification code, string libelleFr, string libelleEn, int ordre)
        : base(code, libelleFr, libelleEn, ordre) { }
}
