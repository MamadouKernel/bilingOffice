using CIT.Plateforme.Application.BillingExport;
using CIT.Plateforme.Application.Documents;
using CIT.Plateforme.Application.Securite;
using CIT.Plateforme.Application.Tickets;
using CIT.Plateforme.Domain.Comptes;
using CIT.Plateforme.Domain.Tarification.Exceptions;
using CIT.Plateforme.Domain.Tickets;

namespace CIT.Plateforme.Web.Services;

/// <summary>
/// REMÉDIATION UI/UX (UX-P1, défaut UI-06) — CE QU'UNE EXCEPTION DIT À L'UTILISATEUR.
///
/// DÉFAUT CORRIGÉ (audit UI/UX final) : les écrans écrivaient <c>_erreur = ex.Message</c> dans un
/// <c>catch (Exception)</c> générique. Le message d'une <c>SqlException</c>, d'un délai
/// d'expiration ou d'une erreur de sérialisation atteignait donc l'utilisateur tel quel —
/// incompréhensible pour lui, et révélateur de la structure interne.
///
/// LE PRINCIPE EST UNE LISTE BLANCHE, PAS UNE LISTE NOIRE. Seules les exceptions dont le message est
/// <b>écrit pour l'utilisateur</b> sont restituées : ce sont les exceptions métier du domaine et de
/// la couche applicative, dont les libellés citent des règles du CDC. Tout le reste — donc tout ce
/// qui vient de l'infrastructure, connu ou non — reçoit un message générique.
///
/// Ce sens est délibéré : une exception nouvelle, non prévue ici, est <b>masquée par défaut</b>.
/// L'inverse aurait laissé passer le prochain type d'erreur technique introduit dans le socle.
/// </summary>
public static class MessagesUtilisateur
{
    /// <summary>
    /// Message générique. Il dit ce qui s'est passé, ce que l'utilisateur peut faire, et n'invente
    /// aucune cause.
    /// </summary>
    public const string Generique =
        "L'opération n'a pas pu être effectuée en raison d'une erreur technique. "
        + "L'incident a été journalisé. Réessayez, et contactez l'administrateur si le problème persiste.";

    /// <summary>
    /// Rend le message à afficher pour <paramref name="exception"/>.
    /// </summary>
    public static string Pour(Exception exception) => EstMessageMetier(exception)
        ? exception.Message
        : Generique;

    /// <summary>
    /// Vrai lorsque le message de l'exception a été rédigé à destination de l'utilisateur.
    ///
    /// <para>Les deux premières familles couvrent les refus et les invariants exprimés par la couche
    /// applicative ; les suivantes sont les exceptions métier nommées du domaine, dont chaque
    /// libellé cite une règle (CYC-01, TRF-03, IMP-41, RG-EXP-03, CLAR-03…).</para>
    /// </summary>
    private static bool EstMessageMetier(Exception exception) => exception is
        AccesRefuseException
        or ArgumentException
        or InvalidOperationException
        or BlDejaActifException
        or JointureIesIncompleteException
        or DernierAdministrateurActifException
        or TransitionNonAutoriseeException
        or MotifTransfertInconnuException
        or DraftInRequisException
        // REMEDIATION RECETTE UAT : consequence directe de D-11a et D-11b. Un refus de depot est
        // un refus METIER — extension non autorisee, fichier trop lourd — dont le message dit a
        // l'utilisateur quoi corriger. Son absence de cette liste est precisement ce qui a masque
        // les deux defauts pendant des mois : les deux echecs se presentaient comme une « erreur
        // technique », sans indiquer ni la cause ni le geste attendu.
        or DepotDocumentRefuseException
        or TarifNonTrouveException;
}
