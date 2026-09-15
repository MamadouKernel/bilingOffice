# Backlog UI/UX final — après recette navigateur authentifiée

> ## ⟴ Mise à jour — 7 septembre 2026 (session « maximum technique »)
>
> Suite à **1594/1594**. Détails : [`remediation-ui-ux-max-technique.md`](remediation-ui-ux-max-technique.md),
> [`audit-ui-ux-final-recalcule.md`](audit-ui-ux-final-recalcule.md),
> [`matrice-responsive-81-pages.md`](matrice-responsive-81-pages.md). Score **92 → 93/100**.
>
> **Fermés ce tour (défauts de contraste RÉELS, WCAG 1.4.3, vérifiés en navigateur, deux thèmes) :**
> - Pastilles/panneaux `bg-*.bg-opacity-10 / bg-*-subtle` illisibles en sombre (2,9:1 → 5,16–8,73:1).
> - `.text-primary` / liens en **thème clair par défaut** (`data-theme` absent) : 3,16:1 → **8,06:1**
>   — mauvaise portée `:root:not([data-theme="light"])` re-portée vers `:root[data-theme="dark"]`.
> - Boutons `btn-warning`/`btn-light.text-dark` : encre claire sur ambre en sombre (1,84:1 → 7,7:1).
> - Garde ajoutée : `ThemeSombreScopingTests` (mutation vérifiée).
>
> **Restant, inchangé (décisions/audit/risque accepté) :** boutons de marque blanc-sur-azur 4,16:1
> (décision token `--action`) · recette live des 68 pages authentifiées (env sans exposer le secret) ·
> audit WCAG formel · emojis décoratifs `aria-hidden` (risque accepté) · sweep inline Design ·
> **D-04** (infra) · **AM-01/02/03** (métier) · `!important` Bootstrap (risque accepté) · CSV (faux
> positif D-22).

**Source** : [`recette-ui-ux-authentifiee.md`](recette-ui-ux-authentifiee.md) — 18 profils, 86 routes,
5 tailles, 2 thèmes, 18 parcours, exécutés dans un navigateur le 6 septembre 2026.
**Nature** : backlog exploitable. Chaque ligne porte un constat mesuré, une cause identifiée et un
effort estimé. Aucune ligne ne repose sur une lecture de code seule sans observation associée.

**Effort** : `XS` < 1 h · `S` ≈ ½ j · `M` ≈ 1-3 j · `L` > 3 j

---

## ⟴ État après clôture P1/P2 (6 septembre 2026)

Détails et preuves : [`remediation-ui-ux-finale.md`](remediation-ui-ux-finale.md). Suite à
**1579/1579**.

### ✅ Fermés

| ID (+ libellé) | Tour |
|---|---|
| **UX-P0** : D-14, D-11a, D-11b, D-01 | P0 |
| **D-05/D-15 (sidebar filtrée par permission)** | P0 · re-validé |
| **D-12** (refus de dépôt dans la liste blanche) | P0 |
| **UI-R01** (simulation publique) — *décision métier rendue : publique* | P1/P2 |
| **UI-R02** (mode sombre lisible — cause racine : fonds codés en dur) | P1/P2 |
| **D-03** (auteur du journal d'audit) | P1/P2 |
| **D-06** (GUID dans le 403 — retiré ; *résidu : code de permission dans un conseil*) | P1/P2 |
| **D-09** (`ToString` brut historique) | P1/P2 |
| **D-10** (dossier vide + faux succès) | P1/P2 |
| **D-13** (`<PageTitle>` manquant) | P1/P2 |
| **D-17** (débordement horizontal tablette) | P1/P2 |

### ⌛ Encore ouverts

| ID (+ libellé) | Sévérité | Statut |
|---|---|---|
| **D-04** (page antiforgery brute) | P1 → **PARTIEL** | Symptôme **dev/UAT uniquement** (prod = écran `/Error` de marque). Résidu = message spécifique + persistance des clés (décision de déploiement). |
| **D-02** (toast recouvre « Terminer », pas d'auto-fermeture) | P1 | Ouvert. |
| **D-07** (connexion rendue dans la coquille authentifiée) | P1 | Ouvert. |
| **UI-16** (styles inline / couleurs en dur) | P1 | **Partiel** : fonds clairs éliminés + garde ; refactor de fond restant. |
| **UI-02 / UI-03 / UI-04 / UI-06** | P1 | Fermés sur surfaces exposées (P0) ; ouverts ailleurs. |
| **D-18** (UUID saisi à la main), **D-22** (CSV non cumulatif) | P2 | Ouverts. |
| **numérotation — préfixe dupliqué** (`AVO-AVO-`, `ENR-ENR-`) | P2 | Ouvert (changement de format à fort rayon). |
| **D-19, D-20, D-21** | P2/P3 | Cosmétiques ; D-21 partiel (P0). |
| **UI-R03, UI-07…UI-15, UI-17…UI-20** | P2/P3 | Ouverts. |

### 🧭 Décisions métier (DECISION REQUIRED)

| ID | Statut |
|---|---|
| **AM-00** (= UI-R01) | **RÉSOLU** — simulation publique. |
| **AM-04** (catégories de réclamation standard) | Adressé — le mapping catégorie → file est désormais configurable (D-14). |
| **AM-01** (séjour 14 vs 15 j), **AM-02** (collab.limite = client.normal), **AM-03** (« Valider Booking » sans confirmation) | Ouverts. |

**Score UI/UX** : **76 / 100** (après recette navigateur ; était 65/100 provisoire statique).
**Pilote : OUI · Production : OUI SOUS CONDITIONS.**

> Les tableaux d'origine ci-dessous sont conservés pour la traçabilité — se référer à l'état
> ci-dessus pour le statut courant.

---

## Priorité 0 — bloquants fonctionnels

Ces quatre lignes interdisent un pilote. Aucune ne dépasse l'effort `S`.

| ID | Écran / fichier | Constat mesuré | Impact | Correction | Effort |
|---|---|---|---|---|---|
| **D-14** | `CustomerComplaint/AdministrationCategories.razor:219` et `:243` | La création passe `FileCibleDefautId: null` **en dur** ; la modification recopie la valeur existante. Aucun champ « file cible » n'existe dans l'IHM, alors que le sous-titre de la page l'annonce et que la couche Application le supporte. Soumission refusée : *« La catégorie FACTURATION n'a pas de file cible configurée. »* | **Aucune réclamation ne peut jamais être soumise sur une installation neuve.** Module Customer Complaint entièrement inatteignable | Ajouter un sélecteur de file cible aux deux formulaires ; le passer aux deux requêtes | **S** |
| **D-11a** | `Infrastructure/Documents/StockageDocumentsFichiers.cs` — `EnregistrerAsync` | `contenu.Position = 0` est exécuté **avant** `File.Create`. Le flux d'un `InputFile` Blazor n'est pas rembobinable → `NotSupportedException` avant toute écriture. Reproduit 2× sur le Draft IN : aucun fichier sur disque, table `Documents` inchangée | Bloque **4 actions** : Draft IN, facture export, BEE, justificatifs d'ouverture de compte. **RG-EXP-03 devient inexécutable** | Rembobiner seulement si `CanSeek`, sinon copier dans un `MemoryStream` **dans la commande** — comme le font déjà 7 pages sur 11. Idem dans `DeposerDocumentCommand.DeposerValideAsync` avant l'antivirus | **S** |
| **D-11b** | `Tickets/Documents.razor:142` | `Extensions = [".pdf", ".jpg", ".jpeg", ".png", ".xlsx"]` **avec le point**, alors que `ReglesDepotDocument.Valide` compare après `TrimStart('.')`. Reproduit 2× (voie agent et voie client) | Aucune pièce ne peut être jointe à un ticket, alors que la page annonce *« Formats acceptés : PDF, JPG, PNG, XLSX »* | Retirer les points — ou normaliser des deux côtés et ajouter un test de non-régression sur la forme des extensions | **XS** |
| **D-01** | `wwwroot/app.css:330` — `.cit-modal-backdrop` | `z-index: 1045` contre `1050` pour la modale Bootstrap parente. `elementFromPoint` au centre du dialogue renvoie l'en-tête de la modale parente : le dialogue n'est **ni visible ni cliquable**. Reproduit sur « Tout accorder » et « Tout révoquer » | **La confirmation livrée en UX-P0 est inutilisable à la souris.** L'utilisateur clique, rien ne se passe, aucun moyen de confirmer ni d'annuler | Porter `.cit-modal-backdrop` au-dessus de la couche Bootstrap (≥ 1060) ; ajouter un `display: flex` + `align-items: center` pour corriger au passage le centrage vertical | **XS** |

---

## Priorité 1 — majeurs

| ID | Écran / fichier | Constat mesuré | Impact | Correction | Effort |
|---|---|---|---|---|---|
| **D-05** | Menu latéral, tous espaces | Filtré par **espace**, jamais par **habilitation**. Mesures : `agent.sansdroits` 16 refus / 22 liens · `support` 14/22 · `pilote.manifeste` 12/22 · `facturation` 12/22 · `superviseur` 10/16 | Impasse UX massive sur les espaces opérateur. Un agent sans habilitation voit un tableau de bord qui le déclare *« Disponible pour affectation »* et un menu majoritairement mort. **La sécurité, elle, tient : refus serveur systématique** | Filtrer chaque entrée sur la permission qu'exige sa page, avec la même source de vérité que le contrôle serveur — jamais une seconde liste | **M** |
| **UI-R02** | `Home.razor` | Thème sombre : **10 éléments sous 4,5:1**, dont les 3 titres d'orientation à **1,17:1** et un titre de section à **1,12:1**, sur des fonds `#ffffff` / `#f8fafc` codés en dur. En clair : **17,64:1**. Capture à l'appui. **Aucune bascule de thème n'est offerte à l'anonyme** | La porte d'entrée de la plateforme est illisible pour tout visiteur dont le système est en sombre, sans échappatoire | Remplacer les fonds codés en dur par les tokens de thème sur cette page. *(Cause racine : UI-16.)* | **S** |
| **UI-R01** | `Home.razor` → `/simulation/proforma` | Revalidé : le CTA *« 🧮 Simuler une Proforma »* mène à **« Accès Refusé »** pour l'anonyme, alors que la route est dans l'allowlist publique | Le principal appel à l'action de la vitrine est une impasse pour un prospect | **Arbitrage métier requis** : ouvrir réellement la simulation sans compte, ou masquer le CTA et retirer la page de l'allowlist | **S** |
| **D-17** | `app.css` — `.cit-header`, `.cit-user-identity` | Débordement horizontal de page : **+126 px à 1024×768**, **+160 px à 768×1024**. `.cit-header` contient 882 px dans une boîte de 746 px. Les tableaux **ne sont pas en cause** : leur `.table-responsive` fonctionne (724 visibles / 829 de contenu) | Toute la plage tablette (~576 → ~1100 px) défile horizontalement | Réduire ou tronquer le bloc d'identité entre 576 et 1100 px, ou repousser son point de bascule `d-sm-flex` vers `d-lg-flex` | **S** |
| **D-04** | Pipeline antiforgery, `Program.cs` | Un jeton périmé ou altéré affiche la page brute du framework : *« A valid antiforgery token was not provided… »* — sans `<title>`, sans charte, sans `<main>`, sans retour, en anglais. Reproduit 2× | Touche **tous** les formulaires SSR (connexion, déconnexion, `/contexte/espace`, `/culture/set`). Déclencheur réaliste : onglet laissé ouvert, redémarrage serveur sans clés persistées | Intercepter `AntiforgeryValidationException` et rendre un écran de la plateforme invitant à recharger. Persister les clés de protection de données | **S** |
| **D-10** | `BillingImport/SoumettreDemande.razor` | Une soumission dont **tous** les BL sont des doublons crée quand même un dossier et affiche *« ✅ Demande enregistrée avec succès ! Votre dossier DOS-000002 a été créé. »* Vérifié en base : `DOS-000002` → **0 ticket** | Message contradictoire ; pollution de la séquence des dossiers par des dossiers vides | Ne créer le dossier que si au moins un BL est retenu ; sinon afficher un refus | **S** |
| **D-03** | `Administration/JournalAudit.razor:71` | `@(entree.AuteurId is null ? "Système CIT" : "Utilisateur")`. Les 111 entrées affichent toutes `Utilisateur` | Un registre d'imputabilité sans imputation lisible. La donnée existe pourtant en base | Résoudre l'auteur (nom + e-mail) dans la requête de lecture ; afficher l'identité | **S** |
| **D-09** | `Tarification/HistoriqueSimulationsPage.razor` | Colonne SENS : `ReferenceTarifaireVue { Id = 3b11f121-…, Code = IMPORT_LOCAL, Libelle = Import local }` | Enregistrement .NET brut affiché à l'utilisateur, GUID interne exposé | Afficher `.Libelle` | **XS** |

---

## Priorité 2 — mineurs à impact réel

| ID | Écran / fichier | Constat mesuré | Correction | Effort |
|---|---|---|---|---|
| **D-02** | `Shared/ToastNotification.razor`, `app.css` | Le toast (`fixed`, `z-index: 1080`, bas-droite, `[565, 451, 207, 131]`) recouvre le bouton « Terminer » de la modale (`[512, 517, 106, 37]`) ; `elementFromPoint` au centre du bouton renvoie le toast. Le composant **n'a aucune fermeture automatique** (encore présent après 12 s) | Ajouter une temporisation de fermeture, et décaler le toast quand une modale est ouverte | **S** |
| **D-12** | `Web/Services/MessagesUtilisateur.cs` | `DepotDocumentRefuseException` absente de la liste blanche : un refus métier légitime devient une « erreur technique ». **C'est ce qui a masqué D-11a et D-11b pendant la recette** | Ajouter l'exception à la liste blanche | **XS** |
| **D-06** | `Comptes/AccesRefuse.razor` | *« L'utilisateur 702bfafc-7295-4d93-48d5-08df0bebe2e9 ne dispose pas de la permission 'supervision.consulter'. »* | Retirer le GUID et le code technique du message visible ; les conserver dans le journal | **XS** |
| **D-13** | `Tickets/FicheTicket.razor`, `Dossiers/FicheDossier.razor` | Les **deux seules** pages sans `<PageTitle>` ; l'onglet affiche `localhost:7097/tickets/f0c8c9a0-…` | Ajouter `<PageTitle>` avec le numéro du ticket / dossier | **XS** |
| **D-15** | Génération des numéros — avoir/exonération et enrôlement | Préfixe dupliqué : **`AVO-AVO-000005`**, **`ENR-ENR-000006`**. Les autres modules sont corrects | Corriger la composition du numéro ; ajouter un test sur le format | **XS** |
| **D-16** | 86 composants de page | **Aucun `<h1>`** sur les 6 pages sondées ; le plan démarre en `<h2>`. Un seul composant en contient un | Promouvoir le titre principal de chaque page en `<h1>` | **S** |
| **D-07** | `Comptes/Connexion.razor` | Accessible en session ouverte, rendu **dans la coquille authentifiée** : bandeau « UAT_Pilote Facturation Terre » au-dessus d'un formulaire de connexion pour un autre compte | Rediriger l'utilisateur authentifié vers son espace, ou rendre la page hors coquille | **S** |

---

## Priorité 3 — confort

| ID | Écran | Constat | Correction | Effort |
|---|---|---|---|---|
| **D-18** | `AvoirExoneration/NouvelleDemandeAvoirExo.razor` | Le client doit saisir **un UUID à la main** : *« Identifiant UUID du ticket parent »* | Remplacer par un sélecteur parmi ses propres tickets | **M** |
| **D-19** | Soumission Billing Import / Export | Le panneau de succès s'affiche **sous** le formulaire, hors écran à 802 × 602, sans défilement automatique | Faire défiler jusqu'au panneau, ou remplacer le formulaire par le résultat | **XS** |
| **D-20** | `CustomerComplaint/AdministrationCategories.razor` | En-tête de tableau sans état vide, contrairement aux files métier | Ajouter un état vide cohérent avec les autres écrans | **XS** |
| **D-21** | Réclamation, soumission BL | Un bouton désactivé n'explique jamais sa condition | Ajouter un texte d'aide sous le bouton | **XS** |
| **D-22** | `Tarification/ImportateurGrilleTarifaireCsv.cs` | Le parseur cesse d'accumuler les lignes dès la première erreur : une ligne à référentiel inconnu n'est pas signalée dans le même rapport | Continuer l'analyse et cumuler toutes les erreurs avant de refuser | **S** |
| **UI-16** | 5 pages (Home 34, SoumettreDemande 29, InscriptionTransporteur 19, ComptesAgents 16, Connexion 13) | 215 `style=` en ligne, ~40 couleurs codées en dur. **Cause racine de UI-R02** | Remplacer par les tokens existants ; promouvoir les motifs récurrents en classes `cit-` | **M** |

---

## Arbitrages métier — DECISION REQUIRED

Ces points **ne sont pas des défauts techniques**. Ils appellent une décision, pas une correction.

| Réf | Question | Bloque |
|---|---|---|
| **AM-00** *(= UI-R01)* | La simulation proforma est-elle ouverte aux anonymes, ou le CTA doit-il disparaître de la vitrine ? | La correction de UI-R01 |
| **AM-01** | Le simulateur annonce *« Séjour prévu de 14 jours »*, l'historique affiche *« 15 j »*, le branchement frigo est facturé 13 jours — pour la même simulation (06/09 → 20/09). Quelle borne fait foi ? | La cohérence des trois affichages |
| **AM-02** | `collab.limite` présente exactement le même menu et les mêmes accès que `client.normal` (19 liens, 1 refus). Quelle restriction devait s'appliquer ? | La vérification du cloisonnement collaborateur |
| **AM-03** | « Valider le Booking » fait avancer le workflow **sans confirmation**, alors qu'une attribution d'habilitation en exige une. Où placer la frontière ? | L'harmonisation des confirmations |
| **AM-04** | Quelles catégories de réclamation, et vers quelles files, livrer en standard ? | L'exploitabilité du module après D-14 |

---

## Ordre d'attaque conseillé

Les quatre P0 pèsent ensemble **moins d'une journée** et débloquent trois chaînes métier entières.

| # | Lot | Effort | Ce qu'il débloque |
|---|---|---|---|
| 1 | **D-11b** (un point en trop) + **D-12** (une exception dans une liste) | XS + XS | Les pièces jointes de ticket, et surtout : les prochains refus de dépôt deviennent **lisibles** au lieu d'être des « erreurs techniques » |
| 2 | **D-01** (`z-index`) | XS | Rend utilisable la remédiation UX-P0 déjà livrée |
| 3 | **D-11a** (`MemoryStream`) | S | Draft IN, facture export, BEE, ouverture de compte — et rend **RG-EXP-03 réellement exécutable** |
| 4 | **D-14** (un champ de formulaire) | S | Le module Réclamations dans son entier |
| 5 | **D-09**, **D-15**, **D-06**, **D-13** | 4 × XS | Nettoyage d'affichage à coût quasi nul |
| 6 | **UI-R02** + **D-17** | S + S | La page d'accueil en thème sombre, et toute la plage tablette |
| 7 | **D-10**, **D-03**, **D-04** | 3 × S | Cohérence des messages, imputabilité de l'audit, écran d'erreur maîtrisé |
| 8 | **D-05** | M | La fin des impasses de navigation côté opérateur |
| 9 | **D-16**, **D-07**, **D-02** puis les P3 | S à M | Accessibilité et confort |

---

## Point de vigilance transversal

Trois des quatre P0 partagent la même origine : **une règle correcte, jamais exercée de bout en bout
dans un navigateur.**

- Le Draft IN (RG-EXP-03) est juste, bien placé, sa garde est visible — et il ne peut pas être émis,
  parce que les tests unitaires passaient des `MemoryStream` là où l'IHM passe un flux Blazor.
- La modale de confirmation UX-P0 a le bon texte, la bonne logique et le bon appel — et personne ne
  peut la voir.
- La page de dépôt de pièces annonce quatre formats et les refuse tous les quatre.

Aucun test unitaire ne pouvait attraper ces trois défauts. **La contre-mesure n'est pas un test de
plus au même niveau, mais au moins un test de bout en bout par chaîne métier, exécuté sur le rendu
réel** — dans la lignée de `ParcoursConnexion2FaTests`, qui avait déjà révélé le bug de mappage de
formulaire de la page de connexion, invisible à tous les tests qui l'avaient précédé.
