using System.Globalization;
using Microsoft.Extensions.Localization;

namespace CIT.Plateforme.Web.Resources;

/// <summary>
/// Implémentation haute performance de localisation bilingue (Français / Anglais) pour CIT Digital Platform.
/// </summary>
public class SharedResourcesLocalizer : IStringLocalizer<SharedResources>
{
    private static readonly Dictionary<string, string> FrenchDictionary = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common
        ["Common.AppTitle"] = "CIT Digital Platform",
        ["Common.Save"] = "Enregistrer",
        ["Common.Cancel"] = "Annuler",
        ["Common.Back"] = "Retour",
        ["Common.Submit"] = "Soumettre",
        ["Common.Next"] = "Suivant",
        ["Common.Previous"] = "Précédent",
        ["Common.Search"] = "Rechercher",
        ["Common.Details"] = "Détails",
        ["Common.Actions"] = "Actions",
        ["Common.Loading"] = "Chargement en cours...",
        ["Common.DraftSaved"] = "Brouillon sauvegardé automatiquement.",
        ["Common.Success"] = "Opération effectuée avec succès.",
        ["Common.Error"] = "Une erreur est survenue.",
        ["Common.RequiredField"] = "Ce champ est obligatoire.",
        ["Common.InvalidFormat"] = "Le format saisi est invalide.",

        // Navigation
        ["Nav.Home"] = "Accueil",
        ["Nav.BillingImport"] = "Facturation Import",
        ["Nav.BillingExport"] = "Facturation Export",
        ["Nav.Complaints"] = "Réclamations",
        ["Nav.Enrollment"] = "Enrôlement",
        ["Nav.Simulation"] = "Simulation Proforma",
        ["Nav.MyApplications"] = "Mes Démarches",
        ["Nav.Login"] = "Connexion",
        ["Nav.Logout"] = "Déconnexion",
        ["Nav.Profile"] = "Mon Profil",

        // Home
        ["Home.HeroTitle"] = "Plateforme Digitale CIT Abidjan",
        ["Home.HeroSubtitle"] = "Espace unique digitalisé pour les opérations de facturation import/export, réclamations et enrôlement transporteurs.",
        ["Home.StartImport"] = "Démarrer une demande Import",
        ["Home.StartExport"] = "Démarrer une demande Export",
        ["Home.StartComplaint"] = "Déposer une réclamation",
        ["Home.StartEnrollment"] = "Enrôlement transporteur",
        ["Home.SimulateProforma"] = "Simuler une proforma",

        // Auth
        ["Auth.LoginTitle"] = "Connexion Sécurisée",
        ["Auth.Email"] = "Adresse e-mail",
        ["Auth.Password"] = "Mot de passe",
        ["Auth.RememberMe"] = "Se souvenir de moi",
        ["Auth.ForgotPassword"] = "Mot de passe oublié ?",
        ["Auth.SignInButton"] = "Se connecter",
        ["Auth.ShowPassword"] = "Afficher le mot de passe",
        ["Auth.HidePassword"] = "Masquer le mot de passe",
        ["Auth.TwoFactorTitle"] = "Vérification Double Facteur",
        ["Auth.TwoFactorCode"] = "Code d'authentification à 6 chiffres",
        ["Auth.VerifyButton"] = "Valider le code",
        ["Auth.InvalidCredentials"] = "Identifiants invalides.",

        // Billing Import
        ["BillingImport.Title"] = "Demande de Facturation Import",
        ["BillingImport.Subtitle"] = "Soumettez votre dossier de facturation pour le déchargement et la livraison de conteneurs import.",
        ["BillingImport.BlNumber"] = "Numéro de Connaissement (BL)",
        ["BillingImport.Armateur"] = "Armateur / Compagnie Maritime",
        ["BillingImport.DeliveryType"] = "Type de livraison",
        ["BillingImport.Containers"] = "Conteneurs déclarés",
        ["BillingImport.UploadDocuments"] = "Pièces justificatives requises (BL, Bon à Délivrer, Déclaration)",
        ["BillingImport.SubmitSuccess"] = "Votre demande d'import a été soumise avec succès.",

        // Billing Export
        ["BillingExport.Title"] = "Demande de Facturation Export",
        ["BillingExport.Subtitle"] = "Déposez votre dossier d'empotage et d'embarquement pour conteneurs à l'export.",
        ["BillingExport.BookingNumber"] = "Numéro de Booking",
        ["BillingExport.VesselCall"] = "Escale navire / Voyage",
        ["BillingExport.CustomsDeclaration"] = "Déclaration en douane",
        ["BillingExport.SubmitSuccess"] = "Votre demande d'export a été soumise avec succès.",

        // Complaints
        ["Complaint.Title"] = "Nouvelle réclamation",
        ["Complaint.Subtitle"] = "Décrivez votre demande — un ticket tracé de bout en bout sera créé pour votre société",
        ["Complaint.Category"] = "Catégorie de la réclamation",
        ["Complaint.Subject"] = "Objet du litige",
        ["Complaint.Description"] = "Description détaillée",
        ["Complaint.SubmitSuccess"] = "Réclamation soumise avec succès !",
        ["Complaint.MyComplaintsTitle"] = "Voir mes réclamations",

        // Enrollment
        ["Enrollment.Title"] = "Enrôlement Transporteur & Chauffeur",
        ["Enrollment.Subtitle"] = "Formulaire de demande de badge et d'autorisation d'accès portuaire.",
        ["Enrollment.TransportCompany"] = "Société de transport",
        ["Enrollment.DriverIdentity"] = "Identité du chauffeur",
        ["Enrollment.VehicleRegistration"] = "Immatriculation du véhicule",
        ["Enrollment.SubmitSuccess"] = "La demande d'enrôlement a été transmise pour instruction.",

        // Simulation Proforma
        ["Simulation.Title"] = "Simulateur Tarifaire Proforma",
        ["Simulation.Subtitle"] = "Estimez les frais de passage portuaire et de magasinage en fonction de la taille et du séjour.",
        ["Simulation.ContainerSize"] = "Taille du conteneur",
        ["Simulation.ContainerType"] = "Type de conteneur",
        ["Simulation.StayDays"] = "Nombre de jours de séjour prévus",
        ["Simulation.Calculate"] = "Calculer la simulation",
        ["Simulation.EstimatedTotal"] = "Montant total estimé (XOF)",
        ["Simulation.FreeDays"] = "Franchise applicable : 7 jours ouvrés.",

        // Status & Enums
        ["Status.EnAttente"] = "En attente",
        ["Status.EnCours"] = "En cours",
        ["Status.Valide"] = "Validé",
        ["Status.Rejete"] = "Rejeté",
        ["Status.Cloture"] = "Clôturé",
        ["Status.Paye"] = "Payé",

        // Ticket & Dossier Details
        ["Ticket.DetailsTitle"] = "Détail de la démarche",
        ["Ticket.Reference"] = "Référence démarche",
        ["Ticket.CreationDate"] = "Date de dépôt",
        ["Ticket.CurrentStatus"] = "Statut actuel",
        ["Ticket.History"] = "Historique et jalons"
    };

    private static readonly Dictionary<string, string> EnglishDictionary = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common
        ["Common.AppTitle"] = "CIT Digital Platform",
        ["Common.Save"] = "Save",
        ["Common.Cancel"] = "Cancel",
        ["Common.Back"] = "Back",
        ["Common.Submit"] = "Submit",
        ["Common.Next"] = "Next",
        ["Common.Previous"] = "Previous",
        ["Common.Search"] = "Search",
        ["Common.Details"] = "Details",
        ["Common.Actions"] = "Actions",
        ["Common.Loading"] = "Loading...",
        ["Common.DraftSaved"] = "Draft saved automatically.",
        ["Common.Success"] = "Operation completed successfully.",
        ["Common.Error"] = "An error occurred.",
        ["Common.RequiredField"] = "This field is required.",
        ["Common.InvalidFormat"] = "The format is invalid.",

        // Navigation
        ["Nav.Home"] = "Home",
        ["Nav.BillingImport"] = "Import Billing",
        ["Nav.BillingExport"] = "Export Billing",
        ["Nav.Complaints"] = "Complaints",
        ["Nav.Enrollment"] = "Enrollment",
        ["Nav.Simulation"] = "Proforma Simulation",
        ["Nav.MyApplications"] = "My Applications",
        ["Nav.Login"] = "Sign In",
        ["Nav.Logout"] = "Sign Out",
        ["Nav.Profile"] = "My Profile",

        // Home
        ["Home.HeroTitle"] = "CIT Abidjan Digital Platform",
        ["Home.HeroSubtitle"] = "Single digital window for import/export billing, customer complaints and carrier enrollment operations.",
        ["Home.StartImport"] = "Start Import Application",
        ["Home.StartExport"] = "Start Export Application",
        ["Home.StartComplaint"] = "Submit a Complaint",
        ["Home.StartEnrollment"] = "Carrier Enrollment",
        ["Home.SimulateProforma"] = "Simulate Proforma",

        // Auth
        ["Auth.LoginTitle"] = "Secure Sign In",
        ["Auth.Email"] = "Email address",
        ["Auth.Password"] = "Password",
        ["Auth.RememberMe"] = "Remember me",
        ["Auth.ForgotPassword"] = "Forgot password?",
        ["Auth.SignInButton"] = "Sign In",
        ["Auth.ShowPassword"] = "Show password",
        ["Auth.HidePassword"] = "Hide password",
        ["Auth.TwoFactorTitle"] = "Two-Factor Authentication",
        ["Auth.TwoFactorCode"] = "6-digit authentication code",
        ["Auth.VerifyButton"] = "Verify Code",
        ["Auth.InvalidCredentials"] = "Invalid credentials.",

        // Billing Import
        ["BillingImport.Title"] = "Import Billing Application",
        ["BillingImport.Subtitle"] = "Submit your billing application for import container discharge and delivery.",
        ["BillingImport.BlNumber"] = "Bill of Lading (BL) Number",
        ["BillingImport.Armateur"] = "Shipping Line / Carrier",
        ["BillingImport.DeliveryType"] = "Delivery Type",
        ["BillingImport.Containers"] = "Declared Containers",
        ["BillingImport.UploadDocuments"] = "Required supporting documents (BL, Delivery Order, Customs Declaration)",
        ["BillingImport.SubmitSuccess"] = "Your import application has been submitted successfully.",

        // Billing Export
        ["BillingExport.Title"] = "Export Billing Application",
        ["BillingExport.Subtitle"] = "Submit your stuffing and loading application for export containers.",
        ["BillingExport.BookingNumber"] = "Booking Number",
        ["BillingExport.VesselCall"] = "Vessel Call / Voyage",
        ["BillingExport.CustomsDeclaration"] = "Customs Declaration",
        ["BillingExport.SubmitSuccess"] = "Your export application has been submitted successfully.",

        // Complaints
        ["Complaint.Title"] = "Customer Complaint Submission",
        ["Complaint.Subtitle"] = "Report a dispute, billing inquiry or operational delay.",
        ["Complaint.Category"] = "Complaint Category",
        ["Complaint.Subject"] = "Dispute Subject",
        ["Complaint.Description"] = "Detailed Description",
        ["Complaint.SubmitSuccess"] = "Your complaint has been registered and forwarded to customer support.",
        ["Complaint.MyComplaintsTitle"] = "My Complaints Tracking",

        // Enrollment
        ["Enrollment.Title"] = "Carrier & Driver Enrollment",
        ["Enrollment.Subtitle"] = "Application form for port access badge and carrier authorization.",
        ["Enrollment.TransportCompany"] = "Transport Company",
        ["Enrollment.DriverIdentity"] = "Driver Identity",
        ["Enrollment.VehicleRegistration"] = "Vehicle Registration",
        ["Enrollment.SubmitSuccess"] = "The enrollment application has been submitted for review.",

        // Simulation Proforma
        ["Simulation.Title"] = "Proforma Tariff Simulator",
        ["Simulation.Subtitle"] = "Estimate port throughput and storage fees based on container size and stay duration.",
        ["Simulation.ContainerSize"] = "Container Size",
        ["Simulation.ContainerType"] = "Container Type",
        ["Simulation.StayDays"] = "Estimated stay duration (days)",
        ["Simulation.Calculate"] = "Calculate Simulation",
        ["Simulation.EstimatedTotal"] = "Estimated Total Amount (XOF)",
        ["Simulation.FreeDays"] = "Applicable free days: 7 business days.",

        // Status & Enums
        ["Status.EnAttente"] = "Pending",
        ["Status.EnCours"] = "In progress",
        ["Status.Valide"] = "Approved",
        ["Status.Rejete"] = "Rejected",
        ["Status.Cloture"] = "Closed",
        ["Status.Paye"] = "Paid",

        // Ticket & Dossier Details
        ["Ticket.DetailsTitle"] = "Application Details",
        ["Ticket.Reference"] = "Application Reference",
        ["Ticket.CreationDate"] = "Submission Date",
        ["Ticket.CurrentStatus"] = "Current Status",
        ["Ticket.History"] = "History & Milestones"
    };

    public LocalizedString this[string name]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            var isEnglish = culture.StartsWith("en", StringComparison.OrdinalIgnoreCase);
            var dict = isEnglish ? EnglishDictionary : FrenchDictionary;

            if (dict.TryGetValue(name, out var val))
            {
                return new LocalizedString(name, val, resourceNotFound: false);
            }

            // Fallback vers le français si clé absente en anglais
            if (isEnglish && FrenchDictionary.TryGetValue(name, out var frenchFallback))
            {
                return new LocalizedString(name, frenchFallback, resourceNotFound: false);
            }

            // Fallback ultime : nom de la clé
            return new LocalizedString(name, name, resourceNotFound: true);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var localized = this[name];
            var formatted = string.Format(CultureInfo.CurrentUICulture, localized.Value, arguments);
            return new LocalizedString(name, formatted, localized.ResourceNotFound);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        var isEnglish = culture.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var dict = isEnglish ? EnglishDictionary : FrenchDictionary;

        return dict.Select(kvp => new LocalizedString(kvp.Key, kvp.Value, resourceNotFound: false));
    }

    public static LocalizedString T(string key)
    {
        var localizer = new SharedResourcesLocalizer();
        return localizer[key];
    }

    public static string Get(string key)
    {
        var localizer = new SharedResourcesLocalizer();
        return localizer[key].Value;
    }

    public static IReadOnlyDictionary<string, string> GetFrenchDictionary() => FrenchDictionary;
    public static IReadOnlyDictionary<string, string> GetEnglishDictionary() => EnglishDictionary;
}
