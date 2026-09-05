# Remédiation P2-03 — Journalisation des connexions

## Exigence

**RG-SEC-03** : « Toute action utilisateur ou administrateur significative (création, prise en
charge, changement de statut, dépôt, décision, transfert, réaffectation, téléchargement,
**connexion**) est tracée dans un journal d'audit **inviolable**. » (SOC-13, SEC-05)

La connexion y figure nommément. C'est le seul terme de l'énumération qui n'était pas couvert.

---

## Défaut

**Aucun événement d'authentification n'était consigné.** Ni succès, ni refus, ni second facteur, ni
déconnexion.

La conséquence s'est manifestée pendant la remédiation P0-ter : un compte désactivé conservait ses
permissions, et **rien dans l'exploitation n'aurait permis de s'en apercevoir** — pas davantage
après la correction, puisqu'un refus ne laissait aucune trace non plus. Une règle de sécurité qui ne
produit aucun signal ne se vérifie qu'en relisant le code.

---

## Reproduction

`tests/CIT.Plateforme.Web.Tests/Securite/CablageJournalConnexionTests.cs` — quatre assertions sur
les sources des voies d'authentification, échouant **sur assertion** :

```
échec : 4, réussite : 0, total : 4
```

| Test | Avant |
|---|---|
| La page de connexion consigne ses issues | ❌ aucun appel |
| La voie 2FA consigne ses issues | ❌ aucun appel |
| La déconnexion est tracée | ❌ aucun appel |
| Le journal n'écrit jamais de secret | ❌ le service n'existait pas |

---

## Cause racine

L'authentification vit dans la couche Web et **ne traverse aucune commande applicative** : elle
n'était donc rattachée à aucune unité de travail, et le double journal — historique fonctionnel +
audit — ne s'y appliquait pas. Le mécanisme d'audit existait et fonctionnait ; il n'avait simplement
jamais été branché sur ce parcours.

---

## Correction

`src/CIT.Plateforme.Application/Securite/JournalConnexionService.cs` — un service applicatif dédié,
qui écrit par `IJournalisationService`, donc dans le journal **append-only**.

**Six événements**, tous exprimés comme des constantes :

| Action | Émise par |
|---|---|
| `ConnexionReussie` | `Connexion.razor` (voie directe) · `VerifierCode.razor` (après second facteur) |
| `ConnexionRefusee` | `Connexion.razor` — identifiants invalides, verrouillage, compte désactivé, accès société refusé |
| `SecondFacteurDemande` | `Connexion.razor` avant redirection |
| `SecondFacteurReussi` | `VerifierCode.razor` |
| `SecondFacteurRefuse` | `VerifierCode.razor` — code invalide, compte désactivé, accès société refusé |
| `Deconnexion` | `Program.cs`, endpoint `/comptes/deconnexion` |

**Le motif de refus est une valeur close**, `MotifRefusConnexion` — jamais une chaîne libre :

```
IdentifiantsInvalides · CompteDesactive · AccesSocieteRefuse · CompteVerrouille · SecondFacteurInvalide
```

Deux raisons : la trace doit rester **agrégeable** pour l'exploitation, et une chaîne libre est un
véhicule commode pour une donnée sensible glissée par inadvertance.

**Ce qui est consigné n'est pas ce qui est montré.** L'utilisateur reçoit toujours le même message
générique — ne jamais révéler si un identifiant existe. Le journal, lui, distingue les cas, parce
que l'exploitation en a besoin. `CompteDesactive` est le motif le plus important : c'est le signal
d'une tentative d'usage d'un accès révoqué.

**Contenu de chaque entrée** :

| Champ | Valeur |
|---|---|
| `AuteurId` | l'utilisateur, ou **`null`** si l'identifiant saisi ne correspond à aucun compte |
| `Action` | l'un des six codes ci-dessus |
| `EntiteType` | `"Authentification"` |
| `EntiteId` | l'utilisateur, ou `Guid.Empty` |
| `DetailAvant` | origine réseau (`RemoteIpAddress`), si le contexte HTTP est disponible |
| `DetailApres` | motif de refus, ou `null` |
| `HorodatageUtc` | posé par l'horloge du service de journalisation |

Un identifiant inconnu est tracé **sans identité** : en inventer une attribuerait la tentative à un
tiers.

**Ce qui n'entre jamais dans le journal** : mot de passe, code de second facteur, jeton, cookie,
tampon de sécurité. Le service ne reçoit qu'un identifiant, un motif clos et une origine réseau —
aucune autre donnée de requête n'est consignée, ni en-tête, ni empreinte, ni identifiant de session.

**La déconnexion est consignée avant d'être effectuée** : après `SignOutAsync`, l'identité n'est
plus lisible dans le principal.

---

## Tests

| Suite | Cas | Résultat |
|---|---|---|
| `JournalConnexionTests` *(créée)* | 7 | **7 / 7** |
| `CablageJournalConnexionTests` *(créée)* | 4 | **4 / 4** |
| **Suite globale** | | **1 354 / 1 354** |

Les tests fonctionnels vérifient le **contenu réellement persisté**, pas l'existence d'un appel :
action, type d'entité, identité, origine, motif, horodatage. Deux d'entre eux méritent d'être
signalés :

- `Un_identifiant_inconnu_est_trace_sans_identite` — `AuteurId` nul, `EntiteId` vide, motif présent.
- `Une_trace_d_authentification_ne_peut_pas_etre_modifiee` — la suppression d'une entrée est refusée.
  **Ce test passe par `UniteDeTravail`, le chemin d'écriture de production** : le garde append-only
  s'y exerce, et non sur le `DbContext` nu. La première version du test appelait `db.SaveChanges()`
  directement et ne prouvait rien — elle court-circuitait la garde qu'elle prétendait vérifier.

Deux faux positifs de mes propres tests ont été corrigés : le test « aucun secret » comptait les
termes cités **dans la documentation du fichier** (qui les énumère précisément pour dire qu'ils n'y
entrent jamais), puis le mot `CancellationToken`, type d'infrastructure sans rapport avec un jeton
d'accès.

---

## Mutations

| Mutation | Régression simulée | Tests tombés |
|---|---|---|
| Retrait des appels de journalisation de `Connexion.razor` (6 → 1) | Les connexions redeviennent invisibles | **1** — `La_page_de_connexion_consigne_ses_issues` |

Restauration vérifiée par recomptage : 6 appels présents, suite de câblage 4/4.

**Limite assumée** : la couverture repose sur des tests de câblage pour le branchement, et sur des
tests fonctionnels pour le contenu. Éprouver de bout en bout qu'une connexion HTTP réelle écrit une
entrée demanderait de piloter `SignInManager` et son cookie hors d'un navigateur — même limite que
pour le refus 2FA, inscrite en risque résiduel.

---

## Fichiers modifiés

**Production — 5**

| Fichier | Nature |
|---|---|
| `Application/Securite/JournalConnexionService.cs` | **créé** — six événements, motifs clos |
| `Web/Components/Pages/Comptes/Connexion.razor` | 4 issues consignées + origine réseau |
| `Web/Components/Pages/Comptes/VerifierCode.razor` | 3 issues consignées + origine réseau |
| `Web/Program.cs` | déconnexion consignée avant `SignOutAsync` |
| `Infrastructure/DependencyInjection.cs` | enregistrement du service |

**Tests — 2 créés.**

---

## Risques résiduels

1. **Aucun test de bout en bout HTTP.** Le branchement est vérifié par lecture de source, le contenu
   par test fonctionnel ; le chemin complet (requête → cookie → entrée) ne l'est pas.
2. **L'origine réseau est l'adresse IP de connexion.** Derrière un répartiteur de charge sans
   `X-Forwarded-For` traité, ce sera celle du répartiteur. Aucun en-tête n'est lu — choix délibéré :
   un en-tête est falsifiable, et le traiter suppose une configuration de proxy de confiance.
3. **La rétention des traces d'authentification n'est pas paramétrée.** Le moteur de rétention
   existe (`MoteurRetentionService`) mais ne connaît pas le type `Authentification`.
4. **Le garde append-only ne protège que l'unité de travail**, pas un accès direct au `DbContext`.
   Pré-existant, hors périmètre.
5. **Aucune alerte sur répétition.** Une série de `CompteDesactive` ou de `IdentifiantsInvalides`
   est tracée mais ne déclenche rien. La supervision de ces motifs relève d'un lot ultérieur.
