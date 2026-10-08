using CIT.Plateforme.Domain.Enrolement;

namespace CIT.Plateforme.Domain.Tickets;

/// <summary>
/// LOT 1 — Enrôlement société. Volet enrôlement du ticket, isolé dans une classe partielle : le
/// fichier principal <see cref="Ticket"/> approche le plafond de maintenabilité (800 lignes) et
/// cette dimension est propre à un seul module. Le ticket d'enrôlement porte deux dimensions
/// typées, toutes deux nulles pour un ticket non-enrôlement (compatibilité ascendante totale).
/// </summary>
public sealed partial class Ticket
{
    /// <summary>Type d'objet enrôlé (chauffeur / camion). <c>null</c> hors enrôlement société.</summary>
    public TypeObjetEnrolement? ObjetEnrolement { get; private set; }

    /// <summary>Sens de l'opération (ajout / retrait). <c>null</c> hors enrôlement société.</summary>
    public SensEnrolement? SensEnrolement { get; private set; }

    /// <summary>
    /// Fixe le type d'objet et le sens d'un ticket d'enrôlement. Réassignable avant traitement (la
    /// société peut corriger sa saisie) ; la valeur retenue est celle au moment de la prise en charge.
    /// Aucune entité carte/BAT n'est modélisée — l'émission reste un événement physique constaté à
    /// la clôture, conformément au parti pris du module (research.md D6/CLAR-ENR-05).
    /// </summary>
    public void DefinirTypeEnrolement(TypeObjetEnrolement objet, SensEnrolement sens)
    {
        ObjetEnrolement = objet;
        SensEnrolement = sens;
    }

    /// <summary>
    /// Validation interne « 4-yeux » (07/10/2026) : quand la société du demandeur compte au moins un
    /// validateur, une soumission d'enrôlement est PARQUÉE — ce champ porte l'id du préparateur — et
    /// reste INVISIBLE de l'Agent Gate (aucune alerte) jusqu'à ce qu'un validateur (≠ préparateur) la
    /// libère. <c>null</c> = pas de validation interne requise / déjà libérée (comportement direct,
    /// rétro-compatible).
    /// </summary>
    public Guid? EnAttenteValidationInterneParId { get; private set; }

    /// <summary>Parque le ticket en attente de validation interne par le préparateur indiqué.</summary>
    public void MarquerAValiderEnInterne(Guid preparateurId) => EnAttenteValidationInterneParId = preparateurId;

    /// <summary>Libère le ticket (validation interne approuvée) : il devient visible de l'Agent Gate.</summary>
    public void LibererValidationInterne() => EnAttenteValidationInterneParId = null;
}
