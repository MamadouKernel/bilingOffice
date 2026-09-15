# Audit complet de la fonctionnalité de traduction

**Date** : 8 septembre 2026. Audit factuel, preuves à l'appui. Conclusion d'ensemble en tête :
**le mécanisme de bascule de langue fonctionne, mais la couverture est minime (~8 % des pages)**.
La traduction n'est donc **pas** une fonctionnalité complète.

---

## 1. Mécanisme (CORRECT)

| Élément | Constat | Preuve |
|---|---|---|
| Localisation | `AddLocalization(ResourcesPath="Resources")` + `SharedResourcesLocalizer` custom (dictionnaires FR/EN, 97 clés chacun) | `Program.cs:34-37` |
| Cultures | `fr-FR` (défaut) + `en-US` | `Program.cs:38-43` |
| Fournisseurs de culture | `CookieRequestCultureProvider` + `AcceptLanguageHeaderRequestCultureProvider` | `Program.cs:44-48` |
| Middleware | `app.UseRequestLocalization(locOptions)` | `Program.cs:211-212` |
| Commutateur | `LanguageSwitcher.razor` → `POST /culture/set` (form + `AntiforgeryToken`) | composant + `Program.cs:325` |
| Écriture cookie | cookie standard `.AspNetCore.Culture`, expiration 1 an, `IsEssential`, `SameSite=Lax`, `Secure` si HTTPS, puis **redirect complet** | `Program.cs:329-347` |
| `<html lang>` | dynamique : `@CultureInfo.CurrentUICulture.TwoLetterISOLanguageName` | `App.razor:4` |

**Vérifié en navigateur.** Pose du cookie `c=en-US|uic=en-US` + rechargement →
`document.documentElement.lang === "en"`, cookie présent. Le modèle POST→Redirect→GET garantit que
la nouvelle culture s'applique dès la requête suivante et au nouveau circuit Blazor. La persistance
(cookie 1 an) et la conservation au refresh/navigation sont **assurées par conception**.

**Le mécanisme n'est donc pas la cause du dysfonctionnement.**

## 2. Couverture (INCOMPLÈTE — cause réelle)

| Mesure | Valeur | Preuve |
|---|---|---|
| Fichiers Razor totaux | 101 | `find … -name *.razor` |
| Fichiers Razor utilisant le localizer | **8** (~8 %) | `grep -l IStringLocalizer/@Localizer` |
| Pages codées en dur en français | ~93 | complément |
| Clés de dictionnaire | 97 FR / 97 EN (parité OK) | comptage |

Fichiers réellement localisés : `Home`, `Connexion`, `VerifierCode`, `SoumettreDemande` (import),
`SoumettreDemandeExport`, `NouvelleReclamation`, `SimulationProformaPage`, `ChampMotDePasse`.

**Preuve du symptôme (navigateur).** Culture EN active sur la page d'accueil (une des 8 « localisées ») :
- `<html lang="en">` ✅
- **titre** de page : « Bienvenue sur la Plateforme Digitale… » → **français** ❌
- hero « Pilotez toutes vos opérations portuaires… » → **français** ❌
- CTAs « Se Connecter », « Créer un compte Société »… → **français** ❌

Résultat : bascule en anglais ⇒ interface **mixte FR/EN dominée par le français**. C'est le
« ne fonctionne pas à 100 % » signalé.

## 3. Points bruts non localisés

| Domaine | Constat | Preuve |
|---|---|---|
| Enums / statuts | `StatutBadge` codait les libellés en dur FR ; `@_fiche.Statut`, `@d.Statut` affichent l'énum brut ailleurs | `StatutBadge.razor`, `FicheAvoirExoneration:37`, `MesDemandesAvoirExo:93` |
| Clés `Status.*` | définies mais **jamais utilisées** par l'UI (seulement par un test) | `grep` |
| PageTitle | plusieurs `<PageTitle>` en dur FR (ex. accueil) | navigateur |
| Validation | messages non passés par le localizer | audit composants |
| Texte dynamique / backend | messages métier, erreurs, refus d'accès non localisés | audit |
| Sidebar / nav | libellés issus de `ConstruireNavigationQuery` (serveur) en FR | audit |
| Commutateur pour l'anonyme | **absent** de l'accueil public (présent seulement dans l'en-tête authentifié) | navigateur (`switcherPresent:false`) |

## 4. Verdict

- **Mécanisme** : correct (cookie + reload + `lang` dynamique + persistance). Rien à réparer côté
  plomberie.
- **Couverture** : ~8 %. La fonctionnalité est un vernis sur une poignée de pages ; l'essentiel de
  l'application ne se traduit pas.

**La traduction n'est PAS une fonctionnalité complète.** Elle ne peut pas être notée comme telle.
Voir [`remediation-traduction.md`](remediation-traduction.md) pour le correctif borné apporté et la
feuille de route, et [`recette-traduction.md`](recette-traduction.md) pour les preuves.
