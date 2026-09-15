namespace CIT.Plateforme.Web.Components.Shared;

/// <summary>
/// Décrit une action destructive/irréversible en attente de confirmation par l'utilisateur (AM-03).
///
/// Partagé entre les pages qui gèrent des actions à confirmer, pour éviter de redéfinir la même
/// structure page par page. Le composant <see cref="ModaleConfirmation"/> l'affiche ; l'action
/// n'est exécutée qu'après confirmation explicite.
/// </summary>
/// <param name="Titre">Titre du dialogue.</param>
/// <param name="Message">Explication de la conséquence, en clair.</param>
/// <param name="LibelleConfirmer">Libellé du bouton de confirmation.</param>
/// <param name="ClasseConfirmer">Classe CSS du bouton de confirmation (ex. <c>btn-danger</c>).</param>
/// <param name="Action">Le travail à exécuter UNIQUEMENT si l'utilisateur confirme.</param>
public sealed record ConfirmationEnAttente(
    string Titre,
    string Message,
    string LibelleConfirmer,
    string ClasseConfirmer,
    Func<Task> Action);
