using System.Globalization;
using Microsoft.AspNetCore.Identity;

namespace CIT.Plateforme.Infrastructure.Identity;

/// <summary>
/// Messages d'ASP.NET Identity (règles de mot de passe, adresse déjà utilisée, lien expiré…) dans la
/// langue de l'utilisateur. Sans lui, Identity répond toujours en anglais, y compris en français.
/// </summary>
public sealed class IdentityErreursBilingues : IdentityErrorDescriber
{
    private static bool Anglais => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    private static IdentityError Erreur(string code, string fr, string en) =>
        new() { Code = code, Description = Anglais ? en : fr };

    public override IdentityError DefaultError() =>
        Erreur(nameof(DefaultError), "Une erreur inconnue s'est produite.", "An unknown error has occurred.");

    public override IdentityError PasswordMismatch() =>
        Erreur(nameof(PasswordMismatch), "Mot de passe incorrect.", "Incorrect password.");

    public override IdentityError InvalidToken() =>
        Erreur(nameof(InvalidToken), "Ce lien n'est plus valide ou a déjà été utilisé.", "This link is no longer valid or has already been used.");

    public override IdentityError InvalidEmail(string? email) =>
        Erreur(nameof(InvalidEmail), $"L'adresse e-mail « {email} » n'est pas valide.", $"The e-mail address \"{email}\" is not valid.");

    public override IdentityError InvalidUserName(string? userName) =>
        Erreur(nameof(InvalidUserName), $"L'identifiant « {userName} » n'est pas valide.", $"The user name \"{userName}\" is not valid.");

    public override IdentityError DuplicateEmail(string email) =>
        Erreur(nameof(DuplicateEmail), $"Un compte existe déjà avec l'adresse « {email} ».", $"An account already exists with the address \"{email}\".");

    public override IdentityError DuplicateUserName(string userName) =>
        Erreur(nameof(DuplicateUserName), $"Un compte existe déjà avec l'identifiant « {userName} ».", $"An account already exists with the user name \"{userName}\".");

    public override IdentityError PasswordTooShort(int length) =>
        Erreur(nameof(PasswordTooShort), $"Le mot de passe doit contenir au moins {length} caractères.", $"The password must be at least {length} characters long.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Erreur(nameof(PasswordRequiresNonAlphanumeric), "Le mot de passe doit contenir au moins un caractère spécial.", "The password must contain at least one special character.");

    public override IdentityError PasswordRequiresDigit() =>
        Erreur(nameof(PasswordRequiresDigit), "Le mot de passe doit contenir au moins un chiffre (0-9).", "The password must contain at least one digit (0-9).");

    public override IdentityError PasswordRequiresLower() =>
        Erreur(nameof(PasswordRequiresLower), "Le mot de passe doit contenir au moins une minuscule (a-z).", "The password must contain at least one lowercase letter (a-z).");

    public override IdentityError PasswordRequiresUpper() =>
        Erreur(nameof(PasswordRequiresUpper), "Le mot de passe doit contenir au moins une majuscule (A-Z).", "The password must contain at least one uppercase letter (A-Z).");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Erreur(nameof(PasswordRequiresUniqueChars), $"Le mot de passe doit contenir au moins {uniqueChars} caractères différents.", $"The password must contain at least {uniqueChars} different characters.");

    public override IdentityError UserAlreadyHasPassword() =>
        Erreur(nameof(UserAlreadyHasPassword), "Ce compte a déjà un mot de passe.", "This account already has a password.");

    public override IdentityError UserLockoutNotEnabled() =>
        Erreur(nameof(UserLockoutNotEnabled), "Le verrouillage n'est pas activé pour ce compte.", "Lockout is not enabled for this account.");

    public override IdentityError ConcurrencyFailure() =>
        Erreur(nameof(ConcurrencyFailure), "Le compte a été modifié entre-temps. Rechargez la page et réessayez.", "The account was modified in the meantime. Reload the page and try again.");

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        Erreur(nameof(RecoveryCodeRedemptionFailed), "Code de récupération invalide.", "Invalid recovery code.");
}
