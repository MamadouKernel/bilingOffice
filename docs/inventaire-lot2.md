# Inventaire LOT 2 — Public / Auth (avant modification)

Chaînes utilisateur codées en dur à migrer, par surface. Exclues : données dynamiques (noms de
société, e-mails, numéros de dossier, `piece.Libelle` issu de la base), placeholders purement
décoratifs (`••••`, `0 0 0 0 0 0`), codes techniques stables (HTTP 500/404, CIT-SEC-403, RCCM, DFE,
BL, IES, 2FA, TOTP), et endonymes.

**Frontière documentée (NON TRAITÉ au LOT 2, chantier transverse) :**
- Messages d'**exceptions métier** rendus via `MessagesUtilisateur.Pour(ex)` : libellés définis dans
  Domain/Application (français). Concernent tous les lots → passe dédiée.
- Messages de **règles de mot de passe** d'ASP.NET Identity (`resultat.Errors[].Description`) :
  chaînes du framework. → `IdentityErrorDescriber` localisé, passe dédiée.
- `MessagesUtilisateur.Generique` (couche Web) : **SERA localisé** dans ce lot.

---

## PUBLIC

### Home.razor — 47 chaînes
PageTitle ; « Redirection vers votre espace de travail... » ; alt logo ; badge « Espace Digital •
Billing Office » ; H1 (2 fragments) ; lead hero ; 3 CTA (Se Connecter / Créer compte société /
Simuler Proforma) ; 3 badges réassurance ; alt illustration ; badge « Démarche Simplifiée » ; H2
parcours ; sous-texte parcours ; 3 cartes profil × (titre + badge + paragraphe + CTA) = 12 ;
badge « Guide d'Enregistrement » ; H3 3 étapes ; sous-texte ; 3 étapes × (titre + paragraphe) = 6 ;
badge « Avantages & Services » ; H3 fonctionnalités ; 4 services × (titre + paragraphe) = 8 ;
bandeau contact (H4 + sous-texte + 2 CTA) = 4.

### Error.razor — 9 chaînes
PageTitle ; aria-label « Incident » ; badge « Incident d'Exploitation • HTTP 500 » ; H1 « Incident
Technique » ; paragraphe ; « Identifiant de suivi (Request ID) : » ; badge « Traçabilité Audit » ;
CTA « Retour à l'accueil » ; CTA « Réessayer ».

### NotFound.razor — 11 chaînes (2 réutilisées : Retour accueil, Tableau de bord)
PageTitle ; aria-label « Introuvable » ; badge « Ressource Introuvable • HTTP 404 » ; H1 « Page
Introuvable » ; paragraphe ; lien « Espace de Connexion » + sous-texte ; lien « Simulateur
Proforma » + sous-texte ; CTA « Retour à l'accueil » (réutil.) ; CTA « Tableau de bord » (réutil.).

## AUTH / PRÉ-CONNEXION

### Connexion.razor — 9 nouvelles (5 déjà localisées : LoginTitle, Email, Password, ForgotPassword, SignInButton)
alt×2 ; « Retour à l'accueil » (réutil.) ; « Créer un compte société » (réutil. Nav) ; H3 « Bienvenue
sur l'Espace Digital » ; tagline ; sous-titre « Renseignez vos identifiants… » ; placeholder e-mail ;
erreur générique « Identifiants incorrects ou compte non accessible. » (code).

### VerifierCode.razor — 8 nouvelles (4 déjà localisées : TwoFactorTitle, TwoFactorCode, VerifyButton, AppTitle)
alt×2 ; H3 « Double Authentification (2FA) » ; tagline ; badge « Sécurité Renforcée » ; sous-titre
6 chiffres ; aide « code se renouvelle toutes les 30 s » ; lien « Annuler et revenir à la
connexion » ; erreur « Code invalide ou expiré. » (code).

### MotDePasseOublie.razor — 12 chaînes
PageTitle ; alt×2 ; H3 (réutil. Connexion) ; tagline (réutil.) ; H1 « Mot de passe oublié » ;
sous-titre ; label « E-mail Professionnel » ; placeholder e-mail (réutil.) ; bouton « Recevoir le
lien » ; lien « Retour à la connexion » ; alerte anti-énumération ; (dev-only) « Environnement de
développement uniquement ».

### ReinitialiserMotDePasse.razor — 10 chaînes
PageTitle ; H1 ; sous-titre ; label « Nouveau mot de passe » ; placeholder règles ; lien « Annuler
et se connecter » ; bouton « Valider le nouveau mot de passe » ; erreur « Lien de réinitialisation
invalide. » (code) ; succès « Mot de passe redéfini avec succès. » ; lien « Accéder à la
connexion ». (Errors Identity = hors périmètre, documenté.)

### SoumettreDemande.razor — 45 chaînes
PageTitle ; top-nav (Retour accueil réutil. + « Déjà un compte ? Se connecter ») ; panneau gauche
(Côte d'Ivoire Terminal + H3 + tagline + 3 bénéfices×2 + légende image) = 9 ; header (badge
Onboarding + H1 + sous-titre) = 3 ; succès (H4 + paragraphe + « Numéro de suivi dossier : » +
« Accéder à la connexion ») = 4 ; bloc 1 (H6 + labels Email/MDP + astérisques + placeholders) ;
bloc 2 (H6 + label profil + « Comprendre les 4 profils » + 4 descriptions courtes + guide (titre +
aria Fermer + 4 descriptions longues) + label « Raison Sociale & RCCM/DFE » + placeholder) ;
bloc 3 (H6 + « Obligatoire » + « Facultatif ») ; boutons (« Déjà un compte ? Se connecter » +
« Soumettre le dossier ») ; lien transporteur. (Options enum TypeClient = codes, invariants.)

### InscriptionTransporteur.razor — 30 chaînes
PageTitle ; top-nav (Retour accueil réutil. + « Guide & Aide ») ; panneau d'aide (H5 + aria Fermer +
3 étapes×2) = 8 ; panneau gauche (Côte d'Ivoire Terminal + H3 + tagline + 3 bénéfices×2 + légende) =
9 ; header (badge + H1 + sous-titre) = 3 ; succès (H4 + paragraphe + CTA) = 3 ; formulaire (3 labels
+ astérisques + placeholders + aide 10 caractères) ; boutons (« Déjà inscrit ? Se connecter » +
« Créer mon accès chauffeur ») ; bas de page (question + « Ouvrir plutôt un compte entreprise ») ;
erreur « L'adresse e-mail et le nom complet sont obligatoires. » (code).

### AccepterInvitation.razor — 13 chaînes (SocieteNom/Email = données)
PageTitle ; H1 « Rejoindre une entreprise » ; « Vérification de l'invitation… » ; invitation
inutilisable (titre + détail) ; succès (titre + « Vous êtes rattaché à … » + « Se connecter ») ;
« Vous avez été invité à rejoindre … » ; label « Adresse e-mail » (réutil.) ; aide nominative ;
label « Choisissez votre mot de passe » ; bouton « Créer mon compte ».

## RECERTIFICATION

### Simulation Proforma publique (`/simulation/proforma`)
Classée physiquement ailleurs mais **recertifiée** dans le parcours public : accès anonyme FR/EN,
saisie, calcul, résultat. Parité calcul FR==EN à prouver. (Traduction de la page = son lot d'origine
si déjà couverte ; ici on RECERTIFIE le comportement bilingue + parité métier.)

---

## TOTAL LOT 2

| Surface | Chaînes à migrer |
|---|---|
| Home | 47 |
| Error | 9 |
| NotFound | 11 |
| Connexion | 9 |
| VerifierCode | 8 |
| MotDePasseOublie | 12 |
| ReinitialiserMotDePasse | 10 |
| SoumettreDemande | 45 |
| InscriptionTransporteur | 30 |
| AccepterInvitation | 13 |
| AccesRefuse | 3 |
| CompteCloture | 5 |
| **TOTAL** | **202** |

Réutilisations comptées dans les surfaces (Retour accueil, Créer compte société, taglines auth,
placeholder e-mail, Adresse e-mail, Tableau de bord) : réduites à des clés partagées uniques.
