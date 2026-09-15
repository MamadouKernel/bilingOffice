# Recette navigateur authentifiée — UAT complet

**Date d'exécution** : 6 septembre 2026
**Environnement** : UAT isolé — base `CIT_UAT_RECETTE` sur `(localdb)\MSSQLLocalDB`, application lancée
sur `https://localhost:7097` (profil `Development`)
**Mode** : observation. Aucun code, aucun test, aucune migration n'a été modifié pendant la recette.
**Portée** : 18 profils de recette, 86 routes déclarées, 5 tailles d'écran, 2 thèmes.

---

## 1. Garde-fous vérifiés avant de commencer

| Garde-fou | Vérification | Résultat |
|---|---|---|
| Base de recette désignée nommément | `Uat:NomBase` = `CIT_UAT_RECETTE` ; l'amorçage refuse toute autre base | ✅ |
| Aucune donnée de production | Base créée pour la recette, 18 comptes `UAT_*` sur le domaine réservé `@local.test` (RFC 6761) | ✅ |
| Aucune chaîne de connexion réelle | Les trois `appsettings*.json` portent `"PlateformeDb": ""` | ✅ |
| Aucun envoi réel (e-mail / SMS / WhatsApp) | 6 occurrences de `CanalJournalise` couvrant Email, SMS et WhatsApp ; **aucun** adaptateur SMTP, SendGrid, Twilio ou MailKit dans la solution | ✅ |
| Secret jamais versionné | Mot de passe fourni par `dotnet user-secrets`, absent du dépôt, de `launchSettings.json` et de ce rapport | ✅ |

> **Le mot de passe UAT n'apparaît nulle part dans ce document, ni dans la console.**
> Il reste à retirer du magasin local une fois les corrections engagées :
> `dotnet user-secrets remove "Uat:MotDePasse" --project src/CIT.Plateforme.Web`

---

## 2. Méthode — et ses limites, énoncées d'emblée

### 2.1 Un artefact d'outillage qu'il fallait comprendre avant de lire quoi que ce soit

Le volet du navigateur a une taille propre de **802 × 602 px**. Dès qu'on lui demande d'émuler une
taille supérieure, la page est **mise à l'échelle pour tenir dans le volet**, et les clics de synthèse
— exprimés en pixels CSS — n'atterrissent alors plus sur l'élément visé.

C'est ce qui a fait échouer mes premières tentatives de connexion : le formulaire était rempli, le
jeton antiforgery présent, le bouton visible et testable par `elementFromPoint`, et pourtant aucune
requête `POST` ne partait. `requestSubmit()` sur le même formulaire a immédiatement abouti et ouvert
la session.

> **Ce n'était pas un défaut de l'application.** Je l'écris explicitement parce que j'ai failli le
> classer comme tel. La méthode en découle :
> - **interaction** (clics, saisie, soumission) → toujours à la taille native du volet ;
> - **observation responsive** (mesures, captures) → sous émulation, sans jamais cliquer.

### 2.2 Deux autres limites, à ne pas confondre avec des constats

| Limite | Conséquence sur la lecture du rapport |
|---|---|
| Le volet refuse d'émuler une largeur inférieure à ~490 px | La taille **390 × 844 n'a pas pu être reproduite exactement** ; le mobile a été mesuré à **490 × 844**. Le constat mobile vaut pour ~490 px, pas pour 390 px. |
| Le bac à sable bloque les téléchargements réels | Le PDF proforma a été vérifié en **interceptant l'appel d'interop** `telechargerFichier` : nom, type MIME, taille et en-tête `%PDF-` du contenu réel. |
| La tabulation de synthèse ne déplace pas `document.activeElement` | Le **parcours clavier n'a pas pu être exercé**. Voir §7.4 — je ne conclus pas à une absence d'indicateur de focus. |

### 2.3 Données de recette créées pendant la session

Deux jeux de données ont dû être créés, **par l'interface**, pour débloquer des parcours :

| Donnée | Pourquoi | Comment |
|---|---|---|
| Grille tarifaire `UAT-v1.0`, 16 lignes, active | Sans elle : *« Erreur lors du calcul : Aucune version de grille tarifaire active trouvée. »* | Import CSV via `/admin/tarification` |
| Catégorie de réclamation `FACTURATION` | Aucune catégorie n'est livrée en standard | Création via `/reclamations/administration-categories` — **et restée inexploitable, voir D-14** |

---

## 3. Couverture réelle

### 3.1 Profils — 18 sur 18 exercés

| Profil | Connexion | Atterrissage |
|---|---|---|
| `uat.admin.fonctionnel` | ✅ | `/comptes/valider` |
| `uat.superviseur` | ✅ | `/supervision` |
| `uat.pilote.manifeste` | ✅ | `/agent/dashboard` |
| `uat.facturation` | ✅ | `/agent/dashboard` |
| `uat.controle.doc` | ✅ | `/agent/dashboard` |
| `uat.backoffice.export` | ✅ | `/agent/dashboard` |
| `uat.support` | ✅ | `/agent/dashboard` |
| `uat.agent.gate` | ✅ | `/agent/dashboard` |
| `uat.agent.sansdroits` | ✅ | `/agent/dashboard` |
| `uat.client.normal` | ✅ | `/client/dashboard` |
| `uat.client.compte` | ✅ | `/client/dashboard` |
| `uat.client.deposit` | ✅ | `/client/dashboard` |
| `uat.client.ies` | ✅ | `/client/dashboard` |
| `uat.admin.societe` | ✅ | `/societe/dashboard` |
| `uat.collab.limite` | ✅ | `/client/dashboard` |
| `uat.collab.global` | ✅ | `/client/dashboard` |
| `uat.transporteur` | ✅ | `/client/dashboard` |
| `uat.compte.desactive` | ⛔ **refusé — attendu** | — |

### 3.2 Parcours

| # | Parcours | État | Preuve ou blocage |
|---|---|---|---|
| 1 | Simulation proforma — Dry, Reefer, lourd, dangereux, OOG, multi | ✅ **complet** | 2 calculs, 6 variantes, montants conformes à la grille importée |
| 2 | Enregistrement + PDF proforma | ✅ **complet** | `Proforma_SIM-20260906-4254.pdf`, `application/pdf`, en-tête `%PDF-`, 2 205 o |
| 3 | Administration tarifaire — import CSV | ✅ **complet** | Import nominal + import invalide refusé atomiquement |
| 4 | Billing Import — soumission client | ✅ **complet** | `DOS-000001`, `BIL-IMP-000001/2` |
| 5 | Billing Import — unicité BL | ✅ **complet** | Doublon rejeté nommément — mais voir **D-10** |
| 6 | Billing Export — soumission client | ✅ **complet** | `DOS-000003`, `BIL-EXP-000003` |
| 7 | Billing Export — prise en charge + validation back-office | ✅ **complet** | Statut, opérateur assigné, historique, transmission |
| 8 | Billing Export — garde Draft IN | ✅ **visible et appliquée** | La colonne « Déposer facture export » affiche *« Draft IN requis »* |
| 9 | Billing Export — **émission du Draft IN** | ⛔ **bloqué** | **D-11a** |
| 10 | Avoir / Exonération — soumission avec justificatif | ✅ **complet** | `AVO-AVO-000005`, document persisté |
| 11 | Enrôlement — soumission + dépôt de pièce | ✅ **complet** | `ENR-ENR-000006`, *« Pièce déposée et analysée. »* |
| 12 | Réclamation client | ⛔ **bloqué** | **D-14** |
| 13 | Administration comptes agents — habilitations en masse | ⚠️ **fonctionne, IHM inutilisable** | **D-01** |
| 14 | Journal d'audit — pagination | ✅ **complet** | 111 entrées, 3 pages, bornes correctes |
| 15 | Comptes négatifs (désactivé, mauvais mot de passe) | ✅ **complet** | Message générique, aucune divulgation |
| 16 | Double-clic sur soumission | ✅ **complet** | 2 clics → 1 dossier, 2 tickets |
| 17 | Dépôt de pièce sur un ticket | ⛔ **bloqué** | **D-11b** |
| 18 | Responsive 5 tailles + 2 thèmes | ✅ (4 tailles exactes sur 5) | §7.2 |

**9 parcours complets, 4 bloqués par un défaut applicatif, 1 fonctionnel mais inutilisable à la souris.**
Les 4 blocages ne sont **pas** un défaut de couverture : ce sont des constats.

### 3.3 Tailles et thèmes

| Taille | Débordement horizontal | Verdict |
|---|---|---|
| 1920 × 1080 | aucun (`scrollWidth` 1920) | ✅ |
| 1366 × 768 | aucun (`scrollWidth` 1356) | ✅ |
| 1024 × 768 | **+126 px** (`scrollWidth` 1150) | ⛔ **D-17** |
| 768 × 1024 | **+160 px** (`scrollWidth` 928) | ⛔ **D-17** |
| 490 × 844 *(mobile approché)* | aucun | ✅ |

Thèmes : **clair** et **sombre** observés, avec mesure de contraste dans les deux.

---

## 4. Défauts bloquants (P0)

### D-01 — La confirmation des actions en masse est invisible et inatteignable

- **Attendu** : cliquer « Tout accorder (SuperAdmin) » ouvre la modale de confirmation livrée à
  l'étape de remédiation UX-P0, l'utilisateur lit le récapitulatif et confirme.
- **Observé** : la modale **existe dans le DOM avec le bon contenu**
  (*« UAT_Agent Sans Habilitation recevra 54 permission(s) supplémentaire(s), soit l'intégralité des
  54 permissions de la plateforme — administration des comptes, tarification et journal d'audit
  compris. »*) mais elle est **entièrement recouverte** par la modale « Habilitations ».
- **Mesure** : `.cit-modal-backdrop` est à `z-index: 1045` (`app.css:330`), la modale Bootstrap à
  `z-index: 1050`. `elementFromPoint` au centre exact du dialogue renvoie l'en-tête de la modale
  parente — le dialogue n'est donc **ni visible ni cliquable**.
- **Reproduit** sur « Tout accorder » **et** « Tout révoquer ».
- **Effet** : l'utilisateur clique, rien ne se passe visuellement, et il n'a aucun moyen de confirmer
  ni d'annuler. La fonctionnalité livrée en UX-P0 est inutilisable à la souris.
- **Le back-end est correct** : confirmation déclenchée par programme, `0/54 → 54/54` puis
  `54/54 → 0/54`, toasts *« 54 permission(s) accordée(s) »* / *« révoquée(s) »*, et 110 entrées
  unitaires + 2 entrées de synthèse dans le journal d'audit.

### D-11a — Quatre actions de dépôt échouent : flux non rembobinable

- **Attendu** : le Draft IN est déposé, le ticket devient éligible à l'édition de la facture.
- **Observé** : *« Échec de l'émission du Draft IN : L'opération n'a pas pu être effectuée en raison
  d'une erreur technique. »*
- **Vérifications** : aucun fichier écrit dans le répertoire de stockage (deux essais, deux noms de
  fichier différents), table `Documents` inchangée.
- **Cause** : `StockageDocumentsFichiers.EnregistrerAsync` exécute `contenu.Position = 0` **avant**
  `File.Create`. Le flux d'un `InputFile` Blazor n'est pas rembobinable → `NotSupportedException`,
  levée avant toute écriture. `DeposerDocumentCommand.DeposerValideAsync` refait la même opération
  avant l'antivirus.
- **Preuve par contraste** : les pages qui **recopient le flux dans un `MemoryStream`** fonctionnent —
  vérifié en navigateur sur l'avoir/exonération (document persisté, `Documents` passe de 1 à 2), sur
  l'enrôlement (*« Pièce déposée et analysée. »*) et sur l'import CSV tarifaire.
- **Actions touchées** :
  - `FileFacturationExport.razor:191` — émission du Draft IN *(reproduit)*
  - `FileFacturationExport.razor:222` — dépôt de la facture export
  - `FileControleDocumentaireExport.razor:170` — délivrance du BEE
  - `Comptes/SoumettreDemande.razor:381-382` — justificatifs d'ouverture de compte
- **Conséquence directe sur la remédiation précédente** : **RG-EXP-03 est juste en logique, visible
  en IHM, et inexécutable.** Les tests unitaires utilisaient des `MemoryStream` et ne pouvaient pas
  le voir. C'est le motif « règle écrite, jamais exercée de bout en bout » — cette fois dans ma
  propre correction.

### D-11b — La page générique de dépôt de pièces rejette tous les formats qu'elle annonce

- **Attendu** : `/tickets/{id}/documents` accepte un PDF — la page annonce *« Formats acceptés : PDF,
  JPG, PNG, XLSX — 10 Mo maximum »*.
- **Observé** : *« L'opération n'a pas pu être effectuée en raison d'une erreur technique. »*
- **Reproduit deux fois** : voie agent (`uat.facturation`) **et** voie client (`uat.client.normal`).
- **Cause** : `Documents.razor:142` déclare `Extensions = [".pdf", ".jpg", ".jpeg", ".png", ".xlsx"]`
  **avec le point**, alors que `ReglesDepotDocument.Valide` compare après `TrimStart('.')`. Aucune
  extension ne correspond donc jamais.

### D-14 — Aucune catégorie de réclamation exploitable ne peut être créée par l'interface

- **Attendu** : un administrateur crée une catégorie et un client peut soumettre une réclamation.
- **Observé** : la création réussit (*« Catégorie « Contestation de facturation » créée. »*), mais la
  soumission est refusée : *« La catégorie FACTURATION n'a pas de file cible configurée. »*
- **Cause** : le sous-titre de la page annonce *« activation, désactivation, libellé, **file cible** »*,
  mais le formulaire de création n'expose que Code et Libellé —
  `AdministrationCategories.razor:219` passe `FileCibleDefautId: null` **en dur** — et le formulaire
  « Modifier » n'expose que le libellé, en recopiant la valeur existante (`:243`). Un `null` reste
  donc `null` définitivement.
- **La couche Application supporte pourtant `FileCibleDefautId`** en création et en modification, et
  la colonne existe en base.
- **Effet** : sur une installation neuve, **aucune réclamation ne peut jamais être soumise**. Le
  module Customer Complaint est entièrement inatteignable par l'interface.
- Le parcours agent (file Support Opérationnel) n'a donc pas pu être exercé. C'est un blocage
  applicatif, pas une lacune de recette.

---

## 5. Défauts majeurs (P1)

### D-05 — Le menu est filtré par espace, pas par habilitation

Mesure sur les liens réellement présents dans le menu de chaque profil :

| Profil | Liens du menu | Menant à « Accès Refusé 403 » |
|---|---|---|
| `agent.sansdroits` | 22 | **16** |
| `support` | 22 | **14** |
| `pilote.manifeste` | 22 | **12** |
| `facturation` | 22 | **12** |
| `superviseur` | 16 | **10** |
| `transporteur` | 19 | 5 |
| `client.normal` / `compte` / `ies` / `collab.limite` | 19 | 1 |
| `admin.societe` | 16 | **0** |

- **La sécurité est correcte** : le serveur refuse systématiquement. La visibilité UI n'est pas une
  frontière de sécurité, et elle n'en tient pas lieu ici.
- **L'UX est une impasse massive** sur les espaces opérateur : plus de la moitié du menu d'un agent
  sans habilitation mène à un mur.
- **Cas extrême** : `agent.sansdroits` (aucune habilitation) atterrit sur un tableau de bord qui
  affiche *« 🟢 Statut Opérateur : Disponible pour affectation — Vous êtes éligible à la
  distribution »*, avec 16 des 22 entrées de son menu en refus.
- **Ceci corrige mon analyse précédente** — voir §8.

### D-17 — L'en-tête déborde sur toute la plage tablette

- **Attendu** : aucun défilement horizontal de page.
- **Observé** : `documentElement.scrollWidth` = **1150** à 1024 px, **928** à 768 px.
- **Cause** : ce ne sont **pas** les tableaux — ceux-ci sont bien dans un `.table-responsive`
  `overflow-x: auto` qui fonctionne (724 px visibles pour 829 px de contenu). C'est l'en-tête :
  `.cit-header` contient 882 px de contenu dans une boîte de 746 px à 1024 px.
- **Plage cassée** : le bloc `.cit-user-identity` porte `d-none d-sm-flex`, donc il apparaît à partir
  de 576 px. Le débordement existe de **~576 px à ~1100 px** — exactement la tablette. En dessous de
  576 px l'identité est masquée et le débordement disparaît, ce qui explique pourquoi la recette
  publique précédente n'avait rien vu à 390 px.

### D-04 — Un échec de jeton antiforgery affiche la page d'erreur brute du framework

- **Attendu** : un écran de la plateforme, en français, invitant à recharger la page.
- **Observé** : `A valid antiforgery token was not provided with the request. Add an antiforgery
  token, or disable antiforgery validation for this endpoint.` — sans `<title>`, sans charte, sans
  `<main>`, sans lien de retour, en anglais.
- **Reproduit deux fois**, dont une avec un jeton délibérément périmé sur `/comptes/connexion`.
- **Portée** : tous les formulaires SSR — connexion, déconnexion, `/contexte/espace`, `/culture/set`.
- **Déclencheur réaliste** : un onglet laissé ouvert, ou un redémarrage du serveur si les clés de
  protection de données ne sont pas persistées.

### D-10 — Un dossier vide est créé et annoncé comme un succès

- **Attendu** : une soumission dont tous les BL sont des doublons ne crée aucun dossier et affiche un
  refus.
- **Observé** : *« ✅ Demande enregistrée avec succès ! Votre dossier DOS-000002 a été créé. »*
  immédiatement suivi de *« ⚠️ Connaissements non pris en compte (doublons déjà actifs) :
  UATBL0000001 »*.
- **Vérifié en base** : `DOS-000001` → 2 tickets ; **`DOS-000002` → 0 ticket**.
- **Effet** : message contradictoire et pollution de la séquence des dossiers.

### D-03 — Le journal d'audit n'identifie jamais son auteur

- **Attendu** : un registre d'imputabilité nomme qui a fait quoi.
- **Observé** : les 111 entrées affichent toutes `Utilisateur`.
- **Cause** : `JournalAudit.razor:71` — `@(entree.AuteurId is null ? "Système CIT" : "Utilisateur")`.
- La donnée existe en base (`AuteurId`) ; seul l'affichage est générique. Un registre d'imputabilité
  sans imputation lisible perd sa raison d'être.

### D-09 — L'historique des simulations affiche le `ToString` brut d'un enregistrement .NET

- **Observé**, colonne SENS : `ReferenceTarifaireVue { Id = 3b11f121-567e-4e45-a7af-edd53073cf0e,
  Code = IMPORT_LOCAL, Libelle = Import local }` au lieu de « Import local ».
- Un identifiant interne est exposé à l'utilisateur final.

### UI-R01 — REVALIDÉ, TOUJOURS OUVERT

Visiteur anonyme, page d'accueil : le bouton **« 🧮 Simuler une Proforma »** pointe vers
`/simulation/proforma`, qui répond **« Accès Refusé »**. Le principal appel à l'action de la vitrine
reste une impasse. *(Arbitrage métier — voir §9.)*

### UI-R02 — REVALIDÉ, TOUJOURS OUVERT, ET PLUS ÉTENDU QUE MESURÉ

En thème sombre, sur la page d'accueil, **10 éléments** passent sous 4,5:1 — contre 4 relevés lors de
la recette publique :

| Élément | Contraste | Fond réellement peint |
|---|---|---|
| « Comment obtenir votre compte entreprise en 3 étapes » | **1,12:1** | `#f8fafc` en dur |
| « Nouvelle Entreprise » | **1,17:1** | `#ffffff` en dur |
| « Collaborateur d'Entreprise » | **1,17:1** | `#ffffff` en dur |
| « Transporteur / Chauffeur » | **1,17:1** | `#ffffff` en dur |
| Bouton « Pré-enrôlement Transporteur » | 1,84:1 | `#f0a020` |
| « J'ai reçu mon invitation » | 2,09:1 | `#ffffff` en dur |
| Les deux descriptifs de carte | 3,12:1 | `#ffffff` en dur |
| Bouton « Ouvrir mon compte société » | 4,16:1 | `#2183bd` |

**Capture prise** : les trois titres de carte sont visuellement illisibles, alors que le corps de
texte en dessous se lit sans peine.

En thème clair, le même titre mesure **17,64:1** — le défaut est strictement lié au thème sombre.

> **Deux aggravations par rapport au constat précédent.**
> 1. **Aucune bascule de thème n'est offerte au visiteur anonyme** : le bouton est dans l'en-tête
>    authentifié. Un visiteur dont le système est en sombre n'a **aucun moyen** de rendre la page
>    lisible.
> 2. Le défaut touche aussi les deux boutons d'action et les descriptifs, pas seulement les titres.

---

## 6. Défauts mineurs (P2 / P3)

| ID | Sév. | Constat |
|---|---|---|
| **D-02** | P2 | Le toast (`position: fixed`, `z-index: 1080`, bas-droite) **recouvre le bouton « Terminer »** de la modale d'habilitations. Mesuré : bouton `[512, 517, 106, 37]`, toast `[565, 451, 207, 131]` ; `elementFromPoint` au centre du bouton renvoie le toast. Le composant `ToastNotification` n'a **aucune fermeture automatique** (vérifié : encore présent après 12 s) — le recouvrement dure jusqu'à fermeture manuelle. Contournement : la croix de la modale. |
| **D-06** | P2 | L'écran 403 divulgue l'identifiant interne et le code technique : *« L'utilisateur 702bfafc-7295-4d93-48d5-08df0bebe2e9 ne dispose pas de la permission 'supervision.consulter'. »* |
| **D-07** | P2 | `/comptes/connexion` reste accessible en session ouverte et s'affiche **dans la coquille authentifiée** : bandeau « UAT_Pilote Facturation Terre » au-dessus d'un formulaire de connexion pour un autre compte. |
| **D-12** | P2 | `DepotDocumentRefuseException` est absente de la liste blanche de `MessagesUtilisateur` : un refus métier légitime (extension, taille) est présenté comme une « erreur technique », sans dire quoi corriger. C'est ce qui a masqué D-11a et D-11b. |
| **D-13** | P2 | Deux pages sans `<PageTitle>` — et ce sont **les deux seules** de l'application : `Tickets/FicheTicket.razor` et `Dossiers/FicheDossier.razor`. L'onglet affiche l'URL brute `localhost:7097/tickets/f0c8c9a0-…`. |
| **D-15** | P2 | Préfixe de numéro dupliqué : **`AVO-AVO-000005`**, **`ENR-ENR-000006`**. Observé sur deux modules ; les autres sont corrects (`BIL-IMP-000001`, `BIL-EXP-000003`, `DOS-000001`). |
| **D-16** | P2 | **Aucun `<h1>`** sur les 6 pages sondées (accueil, connexion, tableaux de bord client et opérateur, journal d'audit, simulateur) : le plan de titres démarre en `<h2>`. Un seul composant sur 86 en contient un. |
| **D-18** | P3 | La liaison à un ticket existant, sur la demande d'avoir/exonération, demande au client de saisir **un UUID à la main** : *« Identifiant UUID du ticket parent »*. |
| **D-19** | P3 | Le panneau de confirmation de soumission (Billing Import/Export) s'affiche **sous** le formulaire, qui reste affiché en entier ; à 802 × 602 il n'est pas visible et la page ne défile pas jusqu'à lui. |
| **D-20** | P3 | `/reclamations/administration-categories` affiche l'en-tête du tableau sans état vide, contrairement aux files métier qui en ont un de bonne qualité. |
| **D-21** | P3 | Un bouton désactivé n'explique jamais pourquoi (soumission de réclamation, soumission BL). Le garde-fou est bon ; l'utilisateur n'en connaît pas la condition. |
| **D-22** | P3 | Le rapport d'erreurs de l'import CSV tarifaire est incomplet : le parseur cesse d'accumuler les lignes dès la première erreur, si bien qu'une ligne à référentiel inconnu (`SENS_INCONNU`) n'est pas signalée dans le même rapport. |

---

## 7. Ce qui fonctionne, et qui mérite d'être dit

### 7.1 Le métier tient

- **Simulation tarifaire** : les 6 variantes exigées donnent des montants exacts et traçables jusqu'à
  la ligne CSV importée. Conteneur 40' Reefer 32 t Danger + OOG : acconage 97 000, relevage 23 750,
  branchement frigo 234 000, majoration lourd 29 100, dangereux 48 500, OOG 48 500, stationnement
  3 400 → **484 250 XOF** ; total général multi-conteneurs **563 650 XOF**.
- **Import CSV atomique** : un fichier invalide est refusé ligne par ligne — *« Import refusé — aucune
  donnée persistée : Ligne 2 : Montant d'acconage '-5' invalide ou négatif. Ligne 3 : … 'abc' … Ligne
  4 : Détection d'un doublon de clé (IMPORT_LOCAL, 20, 'C1'). »* — et la table reste inchangée.
- **Double-clic** : deux clics consécutifs sur « Valider et soumettre » produisent **un seul dossier**
  et deux tickets. Protection vérifiée en base.
- **Unicité des BL** : le doublon est nommé et écarté.
- **Comptes négatifs** : compte désactivé et mauvais mot de passe donnent **le même message
  générique** — aucune divulgation sur l'état réel du compte.
- **Garde Draft IN (RG-EXP-03)** : matérialisée en IHM, la colonne de dépôt de facture est remplacée
  par la mention « Draft IN requis » tant que le brouillon n'est pas émis.
- **Pagination du journal d'audit** : 111 entrées, 3 pages, bornes 1-50 / 51-100 / 101-111, boutons
  désactivés aux extrémités, 11 lignes sur la dernière page.
- **Aucune erreur serveur** sur toute la session : `preview_logs` niveau erreur → *« No server errors
  found »*.

### 7.2 Le responsive est bon aux deux extrémités

Desktop (1920, 1366) et mobile (~490) : aucun débordement. Les tableaux **sont** correctement
encapsulés dans un conteneur à défilement propre. Le défaut D-17 est localisé et son origine est
identifiée précisément.

### 7.3 Les micro-textes sont soignés

Les libellés de confirmation sont chiffrés et explicites, la mention légale non engageante figure sur
les deux écrans de simulation, les états vides des files métier sont rédigés (*« Tous les dossiers de
la file Back Office Export ont été traités. »*), et l'avertissement de séjour prolongé apparaît avant
le calcul.

### 7.4 Accessibilité — ce qui a pu être vérifié

| Contrôle | Résultat |
|---|---|
| `lang="fr"` sur la racine | ✅ |
| Champs de formulaire sans étiquette | **0** |
| Images sans `alt` | **0** |
| Boutons sans nom accessible | **0** |
| Toast | `role="status"`, `aria-live="polite"`, `aria-atomic="true"` — correct |
| Styles de focus | **Déclarés** : `app.css:240` `:focus-visible`, `:913` `.btn:focus-visible { box-shadow: var(--ring) }`, `:756` `.cit-user-trigger:focus-visible { outline: 2px solid var(--action) }` |
| **Parcours clavier réel** | ⚪ **NON VÉRIFIÉ** — la tabulation de synthèse du volet ne déplace pas `activeElement`. **Je ne conclus donc pas à une absence d'indicateur de focus.** |
| Plan de titres | ⛔ **D-16** |
| Contraste | Mesuré dans les deux thèmes — voir UI-R02 |

---

## 8. Corrections de mes propres analyses

Trois constats antérieurs, dont deux de ma main, sont infirmés ou précisés par l'observation réelle.

### 8.1 UI-05 « sans objet » était faux

J'avais classé UI-05 *sans objet* en constatant statiquement que le menu **est** filtré : quatre
`@switch` sur `EspaceActif`, dix-huit `case`, `EspacesDisponibles` calculé côté serveur. C'était exact
— **et hors sujet**. Le filtrage existe **par espace**, pas **par habilitation**. La mesure en
navigateur donne jusqu'à **16 entrées de menu sur 22** menant à un refus. Voir **D-05**.

### 8.2 La remédiation RG-EXP-03 que j'ai livrée est inexécutable

Le Draft IN est correct en logique, correctement placé dans le workflow, et sa garde est bien visible
en IHM. Mais **il ne peut pas être émis** : voir **D-11a**. Mes tests unitaires passaient des
`MemoryStream` et ne pouvaient pas révéler le problème. C'est exactement le motif que j'avais nommé
tout au long de l'audit — « règle écrite, jamais exercée de bout en bout » — cette fois dans ma propre
correction.

### 8.3 Deux hypothèses formulées puis écartées, et pourquoi

| Hypothèse | Pourquoi elle est écartée |
|---|---|
| « La connexion échoue silencieusement » | Artefact d'outillage : l'émulation d'une taille supérieure au volet décale les clics de synthèse. `requestSubmit()` sur le même formulaire authentifie immédiatement. **Aucun défaut applicatif.** |
| « L'interactivité est perdue après navigation interne » | Observé **une fois** : le bouton « Télécharger le PDF » est resté inerte 20 s, sans appel d'interop ni changement d'état. **Trois tentatives ultérieures par les mêmes chemins** (bouton « Enregistrer », lien « Voir le détail », URL directe) ont toutes abouti en ~314 ms. Cause plausible : circuit Blazor détaché dans le volet, dont la console montre des reconnexions WebSocket répétées. **Observation, pas défaut confirmé.** |
| « Aucun dépôt de document ne fonctionne » | **Formulation initiale trop large, corrigée.** Les pages qui recopient le flux dans un `MemoryStream` fonctionnent — vérifié sur trois d'entre elles. Le défaut porte sur **4 actions réparties sur 3 pages** (D-11a) **plus** la page générique de dépôt de pièces (D-11b), pour une cause différente. |

---

## 9. Arbitrages métier — DECISION REQUIRED, pas des bugs

| Réf | Question posée au métier |
|---|---|
| **UI-R01** | La simulation proforma doit-elle être réellement ouverte aux anonymes (l'allowlist le prévoit : *« SIM-01 : simulation accessible avant tout parcours »*), ou le bouton doit-il être masqué aux anonymes et la page retirée de l'allowlist ? |
| **AM-01** | La durée de séjour diffère selon l'écran : le simulateur annonce *« Séjour prévu de 14 jours »*, l'historique affiche *« 15 j »* pour la même simulation (06/09 → 20/09), et le branchement frigo est facturé 13 jours. Quelle borne fait foi ? |
| **AM-02** | `collab.limite` présente exactement le même menu et les mêmes accès que `client.normal` (19 liens, 1 refus). La limitation attendue n'est pas observable en IHM. Quelle restriction devait s'appliquer ? |
| **AM-03** | Une action qui fait avancer le workflow (« Valider le Booking ») s'exécute sans confirmation, alors qu'une attribution d'habilitation en demande une. Où placer la frontière ? |
| **AM-04** | Quelles catégories de réclamation, et vers quelles files, doivent être livrées en standard ? *(Prérequis de D-14.)* |

---

## 10. Score — complet, fondé sur observation réelle

> Ce score n'est **plus provisoire** et **plus partiel** : il porte sur les 18 profils, 5 tailles,
> 2 thèmes et 18 parcours listés au §3, tous observés dans un navigateur.

| Poste | Note | Fondement observé |
|---|---|---|
| Design visuel | **12 / 15** | Identité soignée, hiérarchie claire, thème sombre réellement implémenté. Pénalisé par UI-R02, sur la page d'entrée et sans échappatoire pour l'anonyme |
| Cohérence | **7 / 10** | Composants homogènes sur 20+ écrans. Pénalisé par le `ToString` brut (D-09), les préfixes dupliqués (D-15), un état vide manquant (D-20) |
| Navigation | **4 / 10** | Jusqu'à 16 entrées de menu sur 22 menant à un refus (D-05). La sécurité tient ; l'orientation, non |
| Formulaires | **8 / 10** | Boutons désactivés tant que la saisie est incomplète, unicité BL, import atomique, protection double-clic. Pénalisé par la saisie d'UUID à la main (D-18) |
| Feedback | **6 / 10** | Toasts et panneaux de succès de bonne facture. Pénalisé par la confirmation invisible (D-01), le succès trompeur (D-10), les refus métier travestis en erreurs techniques (D-12) |
| Responsive | **6 / 10** | Impeccable en desktop et en mobile ; cassé sur toute la plage tablette (D-17) |
| Accessibilité | **5 / 10** | `lang`, étiquettes, `alt`, noms accessibles et `aria-live` corrects. Pénalisé par l'absence de `<h1>` (D-16) et le contraste 1,12:1 en sombre. Clavier **non vérifié** |
| Gestion des erreurs | **5 / 10** | Écrans 403 soignés mais divulguant un GUID (D-06) ; page d'erreur brute du framework atteignable (D-04) |
| Parcours utilisateur | **5 / 10** | 9 complets sur 15 ; 4 bloqués par un défaut applicatif |
| Qualité frontend | **3 / 5** | Inchangé — 215 `style=` en ligne, ~40 couleurs codées en dur (UI-16), cause racine de UI-R02 |
| **TOTAL** | **61 / 100** | |

*Repères : 65/100 en analyse statique, ≈74/100 sur le seul périmètre public. L'élargissement du
périmètre fait baisser la note — c'est le résultat attendu d'une observation réelle et complète.*

---

## 11. Verdict

> ### DESIGN : 🟢 **VALIDÉ AVEC RÉSERVES**
> L'identité visuelle est soignée et cohérente sur plus de vingt écrans réellement observés. Le thème
> sombre existe et fonctionne partout **sauf** sur la page d'accueil publique. Réserve : **UI-R02**.

> ### UX : 🔴 **À REVOIR**
> Deux motifs lourds : un menu qui envoie l'utilisateur dans le mur jusqu'à 16 fois sur 22, et une
> confirmation d'action sensible que personne ne peut voir ni valider.

> ### RESPONSIVE : 🟠 **À REVOIR**
> Parfait à 1920, 1366 et ~490 px. Cassé de ~576 à ~1100 px — la tablette. Origine identifiée
> précisément (l'en-tête, pas les tableaux), correction ciblée.

> ### ACCESSIBILITÉ : 🟠 **À REVOIR**
> Les fondamentaux sont là (`lang`, étiquettes, `alt`, `aria-live`, styles de focus déclarés). Deux
> manques nets : aucun `<h1>`, et un contraste de 1,12:1 en thème sombre sur la porte d'entrée.
> Le clavier reste **non vérifié**.

> ### PARCOURS MÉTIER : 🔴 **À REVOIR**
> Le socle tient — simulation, import, export, avoir, enrôlement, audit, habilitations. Mais trois
> chaînes s'arrêtent net : la facturation export (Draft IN), le dépôt de pièces sur un ticket, et
> l'intégralité du module Réclamations.

---

> ## UI PRÊTE POUR PILOTE : 🔴 **NON**
>
> Quatre défauts P0 se dressent sur des parcours qu'un pilote traversera dès la première journée :
> **D-14** (aucune réclamation possible), **D-11a** (facturation export bloquée), **D-11b** (aucune
> pièce jointe sur un ticket), **D-01** (la fonctionnalité livrée en UX-P0 est inutilisable).
>
> Aucun de ces quatre ne demande un chantier : ce sont un champ manquant dans un formulaire, un
> `MemoryStream`, un point en trop dans une liste d'extensions, et un `z-index`.

> ## UI PRÊTE POUR PRODUCTION : 🔴 **NON**
>
> Aux quatre P0 s'ajoutent sept P1, dont deux — **UI-R01** et **UI-R02** — sont sur la page d'accueil
> publique et étaient déjà connus. Un registre d'audit qui n'identifie pas ses auteurs (**D-03**) et
> une page d'erreur brute du framework atteignable par un simple onglet laissé ouvert (**D-04**) ne
> sont pas des défauts d'esthétique.

---

## 12. Ce qu'il reste à faire côté environnement

1. **Retirer le secret de recette** du magasin local une fois les corrections engagées :
   `dotnet user-secrets remove "Uat:MotDePasse" --project src/CIT.Plateforme.Web`
2. **Nettoyer les données de recette** créées pendant la session : grille `UAT-v1.0`, catégorie
   `FACTURATION`, dossiers `DOS-000001` à `DOS-000005`, et le dossier vide `DOS-000002`.
3. **Ne pas rejouer cette recette sur une base de production**, ni sur une copie contenant des
   données personnelles réelles.
