# Remédiation UAT — P0 navigateur + filtrage de la sidebar

**Date** : 6 septembre 2026
**Périmètre autorisé** : D-14, D-11a, D-11b, D-01, puis le filtrage de la sidebar par permissions réelles.
**Environnement de validation** : `CIT_UAT_RECETTE` (LocalDB), application lancée sur `https://localhost:7097`.
**Ligne de base** : 1487/1487.

> **Décalage d'identifiants signalé.** Dans [`recette-ui-ux-authentifiee.md`](recette-ui-ux-authentifiee.md),
> `D-15` désigne le préfixe de numéro dupliqué (`AVO-AVO-…`) et c'est **`D-05`** qui désigne la sidebar
> non filtrée. La cinquième mission décrivait sans ambiguïté la sidebar : elle est traitée ici sous
> **D-05/D-15**. Le défaut de numérotation reste ouvert, hors périmètre.

---

## 1. D-14 — Customer Complaint : le module était inatteignable

### La règle métier, telle qu'elle existait déjà

Avant d'écrire quoi que ce soit, j'ai cherché où le routage était censé se décider. Il l'était déjà,
et correctement : `SoumettreReclamationCommand` lit `categorie.FileCibleDefautId` et route le ticket
**côté serveur**. La couche Application acceptait déjà cette file en création comme en modification,
et la colonne existait en base.

**Rien ne manquait au métier.** Ce qui manquait, c'était le moyen de configurer le lien :

| Fichier | Constat |
|---|---|
| `AdministrationCategories.razor:219` | `CreerCategorieReclamationRequete(..., null)` — file cible **en dur** |
| `AdministrationCategories.razor:243` | la modification recopiait la valeur existante : un `null` restait `null` à jamais |

Le sous-titre de la page annonçait pourtant « activation, désactivation, libellé, **file cible** ».

### Ce qui a été fait

L'utilisateur final ne saisit **aucun identifiant**. Le serveur énumère les files éligibles,
l'administrateur en choisit une **par son nom**, et la commande revérifie ce choix.

| Élément | Rôle |
|---|---|
| `ListerFilesCibleReclamationQuery` *(nouveau)* | énumère les files du module `CustomerComplaint`, sous habilitation `AdministrationCategoriesReclamation` |
| `GardeFileCibleReclamation` *(nouveau)* | vérifie **à la configuration** que la file existe et relève du bon module — un seul exemplaire, appelé par les deux commandes |
| `CreerCategorieReclamationCommand`, `ModifierCategoriesReclamationCommand` | appellent la garde |
| `AdministrationCategories.razor` | sélecteur de file cible en création **et** en modification ; colonne « File cible » ; alerte « ⚠️ Aucune file — inutilisable » ; bouton « Créer » désactivé tant que la file n'est pas choisie |
| `SoumettreReclamationCommand` | constante `Module` extraite, réutilisée comme filtre — plus de littéral recopié |

Le filtre par module n'est pas cosmétique : router une réclamation vers une file de facturation
import placerait le ticket devant des instructeurs qui n'ont ni le contexte ni les écrans pour la
traiter.

**L'absence de file reste permise au niveau du domaine** — une catégorie peut être préparée avant que
sa destination soit arrêtée — mais l'interface ne peut plus en produire, et la soumission refuse
toujours avec le même message exact.

### Vérifié en navigateur

1. La catégorie `FACTURATION`, héritée de la recette et sans destination, s'affiche désormais
   **« ⚠️ Aucune file — inutilisable »**. Le défaut est visible avant d'être subi.
2. « Modifier » propose un sélecteur ne contenant que **« Support Opérationnel »** — l'unique file du
   module. → *« Catégorie mise à jour. »*, colonne renseignée.
3. Création d'une catégorie `DELAI` : bouton « Créer » **désactivé** sans file, **actif** dès qu'elle
   est choisie. → *« Catégorie « Retard de traitement » créée. »*
4. Le client soumet : **« ✅ Réclamation soumise avec succès ! Numéro de suivi unique :
   CUS-REC-000007 »**
5. Côté agent, `uat.support` voit **CUS-REC-000007** dans la file Support Opérationnel, instructable.

---

## 2. D-11a — le flux d'un navigateur ne se rembobine pas

### Cause

`StockageDocumentsFichiers.EnregistrerAsync` exécutait `contenu.Position = 0` **avant**
`File.Create`. Le dépôt lit le contenu deux fois — stockage, puis analyse antivirale — et le code se
contentait de repositionner entre les deux. Cela fonctionne pour un `MemoryStream`, donc dans tous
les tests unitaires ; le flux d'un `InputFile` Blazor est à passe unique, et l'affectation de
`Position` y lève `NotSupportedException` **avant toute écriture**.

### Sites recherchés et traités

La recherche a porté sur `Position = 0`, `.Seek(`, `CanSeek` et `IBrowserFile.OpenReadStream` :

| Site | Décision |
|---|---|
| `Application/Documents/DeposerDocumentCommand.cs:70` | **corrigé** — passe par `FluxRejouable` |
| `Infrastructure/Documents/StockageDocumentsFichiers.cs:62` | **corrigé** — repositionnement conditionné à `CanSeek` |
| `Infrastructure/Documents/ScannerAntivirusDefender.cs:79` | **corrigé** — idem (défense en profondeur) |
| `Application/Documents/ValidateurFichierDocument.cs:55-59` | **inchangé** — gardait déjà `CanSeek` |
| `Tarification/GestionGrillesTarifaires.razor:202` | **inchangé** — repositionne un `MemoryStream` qu'il vient de créer |
| 11 pages appelant `OpenReadStream` | **inchangées** — 7 tamponnaient déjà ; les 4 autres passent désormais par le chemin corrigé |

### `FluxRejouable` — la stratégie retenue

- flux déjà rembobinable → **utilisé tel quel**, aucune copie ;
- flux à passe unique → recopie en mémoire, **plafonnée** par la taille maximale de l'étape.

Le plafond n'est pas décoratif : la taille annoncée par le navigateur est une déclaration, pas une
garantie. Un flux qui dépasse est interrompu et refusé (`DepotDocumentRefuseException`), sans jamais
allouer au-delà du plafond. Aucun fichier n'est chargé « arbitrairement » : la borne est celle que
l'étape déclare, 10 Mo par défaut.

### Vérifié en navigateur — les quatre actions

| Action | Résultat |
|---|---|
| **Draft IN** (`/billing-export/facturation`) | colonne DRAFT IN : **✅ Émis**, la mention « Draft IN requis » disparaît |
| **Facture export** | déposée, ticket passé en **Validation Paiement**, `EstEnEtatEngageant = 1` |
| **BEE** (`/billing-export/controle-documentaire`) | déposé, ticket **Clôturé** |
| **Ouverture de compte** (formulaire public, anonyme) | dossier enregistré avec son justificatif RCCM |

Les cinq fichiers sont physiquement présents dans le stockage, à leur taille exacte, et les trois
qui passent par la commande portent `StatutAntivirus = Sain` en base.

**RG-EXP-03 est désormais réellement exécutable.** La chaîne export complète a été rejouée de bout en
bout : soumission → back office → Draft IN → facture → validation paiement → BEE → clôture.

---

## 3. D-11b — l'extension, normalisée une seule fois

### Cause

`Tickets/Documents.razor:142` déclarait `Extensions = [".pdf", ".jpg", …]` **avec le point**, tandis
que `ReglesDepotDocument.Valide` comparait l'extension du fichier après `TrimStart('.')`. Aucune
correspondance n'était donc jamais possible : la page annonçait « Formats acceptés : PDF, JPG, PNG,
XLSX » et les refusait tous les quatre.

### Correction

`ExtensionFichier` (Domain) porte la convention, et **les deux membres de la comparaison** y passent —
la liste déclarée par l'appelant comme l'extension du fichier. Une liste écrite avec ou sans point
donne le même résultat. Les deux validateurs de la solution (`ReglesDepotDocument` et
`ValidateurFichierDocument`) l'utilisent ; la page n'a pas eu à changer.

Les autres contrôles sont conservés intacts : taille, octets d'en-tête (`%PDF`, `\x89PNG`, `MZ`…),
analyse antivirale. L'extension reste un contrôle de forme, et rien d'autre.

### Vérifié en navigateur

| Cas | Résultat |
|---|---|
| `preuve-client-corrigee.pdf` sur un ticket | **« Document déposé et analysé. »**, listé, antivirus **Sain** |
| `charge-utile.exe` | *« Le type de fichier '.exe' n'est pas autorisé pour cette étape. Formats acceptés : .pdf, .jpg, .jpeg, .png, .xlsx. »* |

Le second cas montre l'effet d'une ligne indissociable de cette correction — voir §6.

---

## 4. D-01 — la modale de confirmation, enfin visible

### Cause

`.cit-modal-backdrop` était à `z-index: 1045`, sous la modale Bootstrap qui vaut 1050. Ouverte depuis
l'écran des habilitations, la confirmation était entièrement recouverte : `elementFromPoint` au centre
du dialogue renvoyait l'en-tête de la modale parente. Ni visible, ni cliquable.

### Correction — sans guerre de z-index

L'ordre de superposition est désormais **énoncé une fois**, dans `:root`, du fond vers la surface :

```
  25  en-tête collant          1050  Bootstrap : .modal
  30  rail de navigation       1060  voile du menu utilisateur
1045  voile du menu mobile     1061  menu utilisateur déroulant
1046  déclencheur mobile       1080  notifications (toasts)
                               1085  CONFIRMATION
                               1090  bannière d'erreur Blazor
```

Toutes les valeurs dispersées dans la feuille pointent maintenant vers ces jetons. Le voile de
navigation **reste** sous la modale Bootstrap — les deux usages du même voile n'ont pas les mêmes
contraintes, et une classe dédiée (`cit-voile-confirmation`) les sépare au lieu de casser l'un en
réparant l'autre.

Pourquoi la confirmation passe au-dessus des toasts : un dialogue qui demande une décision ne peut
pas être partiellement recouvert par une notification. Ce n'est pas une surenchère, c'est l'ordre
attendu.

**Deux défauts corrigés au passage, indissociables de « rendre la modale utilisable » :**
- **centrage vertical** — le voile était `fixed inset:0` mais son enfant se contentait de
  `margin:auto` dans un contexte de bloc ; le dialogue restait collé en haut ;
- **focus** — l'attribut `autofocus` était inopérant (le navigateur ne l'honore qu'à l'analyse du
  document, jamais sur un nœud inséré ensuite par Blazor). Le focus est désormais placé
  explicitement, **sur « Annuler »** : le geste sûr, jamais le geste destructeur. Cela répare aussi
  la fermeture par Échap, câblée sur l'arrière-plan et qui ne recevait donc jamais l'événement.

### Vérifié en navigateur (clics réels, taille native du volet)

| Contrôle | Résultat |
|---|---|
| Visible | ✅ capture à l'appui, au-dessus de la modale des habilitations |
| `z-index` du voile | **1085** |
| Recouvrement | `elementFromPoint` au centre → le paragraphe du dialogue lui-même |
| Centrage vertical | ✅ |
| Focus initial | **« Annuler »** |
| Échap | annule ; la modale parente reste ouverte ; **0/54 inchangé** |
| Tout accorder | **0 → 54/54**, toast « 54 permission(s) accordée(s). » |
| Tout révoquer | **54 → 0/54**, toast « 54 permission(s) révoquée(s). » |
| Désactivation (modale non imbriquée) | visible, centrée, focus sur « Annuler » ; compte passé **Inactif**, compteur 9/10 → 8/10 ; réactivation vérifiée |

---

## 5. D-05/D-15 — la sidebar filtrée sur les droits réels

### Principe : aucune seconde logique d'autorisation

La règle de portée vit désormais dans **`RegleHabilitation`** (Domain), en un exemplaire, et **deux**
appelants s'en servent :

- `AutorisationService`, qui décide côté serveur et **refuse** ;
- `ConstruireNavigationQuery`, qui décide seulement ce qui est **affiché**.

Une habilitation globale (`FileId == null`) couvre toutes les files ; une habilitation bornée à une
file ne couvre que celle-ci et ne satisfait **pas** une vérification globale (SEC-11, WKF-01).
L'asymétrie est délibérée et partagée.

> **Aucun contrôle serveur n'a été supprimé ni affaibli.** Chaque page conserve le sien, inchangé.
> Masquer un lien n'a jamais empêché personne de taper l'URL — et le serveur refuse toujours.

### Architecture

| Élément | Rôle |
|---|---|
| `CatalogueNavigation` *(nouveau)* | **la matrice** : une entrée = libellé + route + permissions requises + portée de file éventuelle + condition de rattachement société |
| `ConstruireNavigationQuery` *(nouveau)* | applique `RegleHabilitation` aux habilitations réelles ; supprime les groupes vides |
| `NavMenu.razor` | **ne décide plus rien** : il affiche ce qu'on lui remet. Plus de `switch` sur l'espace, aucune mention de `Permission.` |

Les noms de files ne sont pas recopiés : ils viennent de `NomsFiles` / `NomsFilesExport`, qui les
déclarent déjà. Deux orthographes d'un même nom rendraient une entrée invisible sans que rien ne le
signale.

Une condition qui n'est **pas** une permission a dû être distinguée : plusieurs écrans (simulateur,
profil société, mes réclamations) refusent un compte **sans société rattachée** tout en n'exigeant
aucune habilitation. C'est le cas réel du transporteur. Les confondre aurait produit un filtrage faux.

### Matrice de navigation

| Groupe | Lien | Route | Permission requise | Portée |
|---|---|---|---|---|
| **Espace Structure** | Tableau de bord | `/client/dashboard` | — | |
| | Profil société | `/comptes/profil` | — | société requise |
| | Tableau de bord Société | `/societe/dashboard` | — | société requise |
| | Collaborateurs | `/societe/collaborateurs` | — | société requise |
| | Tableau de bord Opérateur | `/agent/dashboard` | — | |
| | Vue Supervision | `/supervision` | `supervision.consulter` | |
| | Accueil Admin CIT | `/comptes/valider` | `compte.valider` | |
| **Gestion des Dossiers** | Mes demandes | `/client/demandes` | — | |
| | Mes rendez-vous | `/rendez-vous/mes-rendez-vous` | — | |
| | Guichet Billing Office | `/guichet/recherche` | `guichet.recherche` | |
| | Agenda Guichet | `/guichet/agenda` | `rendez_vous.gerer` | |
| | Pilote Manifeste | `/billing-import/manifeste` | `billing_import.manifeste.instruire` | file *Pilote Manifeste* |
| | Contrôle Doc Import | `/billing-import/controle-documentaire` | `billing_import.controle_documentaire.traiter` | file *Contrôle Documentaire* |
| | Back Office Export | `/billing-export/back-office` | `billing_export.back_office.instruire` | file *Back Office Export* |
| | Contrôle Doc Export (BEE) | `/billing-export/controle-documentaire` | `billing_export.controle_documentaire.traiter` | file *Contrôle Documentaire* |
| | Traitement IES Import | `/billing-import/ies` | `…ies.manifeste.instruire` **ou** `…ies.controle_documentaire.instruire` | file *File IES* |
| | Traitement IES Export | `/billing-export/ies` | `…ies.back_office.instruire` **ou** `…ies.controle_documentaire.instruire` | file *File IES* |
| | File Réclamations | `/reclamations/file` | `complaint.instruire` | file *Support Opérationnel* |
| | File Agent Gate | `/enrolement/file-agent-gate` | `enrolement.instruire` | file *Agent Gate* |
| | Disponibilité agents | `/administration/disponibilite` | `supervision.consulter` | |
| | Stratégie des files · Motifs de transfert · Pièces par étape · Matrice des transitions | `/administration/*` | `administration.files` | |
| | Paramètres SLA · Règles d'escalade | `/administration/*` | `administration.sla` | |
| | Périmètres de supervision | `/administration/perimetres-supervision` | `administration.perimetres_supervision` | |
| | Compétences des opérateurs | `/administration/competences-operateur` | `competence.gerer` | |
| **Facturation & Simulation** | Demande IMPORT (BL) | `/billing-import/soumettre` | `billing_import.soumettre` | |
| | Demande EXPORT (Booking) | `/billing-export/soumettre` | `billing_export.soumettre` | |
| | Avoir / Exonération (2 liens) | `/avoir-exoneration/*` | — | société requise |
| | Simulation · Historique | `/simulation/*` | — | société requise |
| | Facturation Import · Caisse Import · Comptabilité Import | `/billing-import/*` | `billing_import.facturation.editer` · `…paiement.valider` · `…comptabilite.confirmer_lettrage` | files correspondantes |
| | Facturation Export · Caisse Export · Comptabilité Export | `/billing-export/*` | `billing_export.facturation.editer` · `…paiement.valider` · `…comptabilite.confirmer_lettrage` | files correspondantes |
| | File Avoirs & Exonérations | `/avoir-exoneration/file` | `avoir_exoneration.instruire` | file *Avoir & Exonération* |
| | Grilles tarifaires | `/admin/tarification` | `tarification.administrer` | |
| **Administration & Sécurité** | Nouvelle réclamation | `/reclamations/soumettre` | `complaint.soumettre` | |
| | Mes réclamations | `/reclamations/mes-reclamations` | — | société requise |
| | Nouvel enrôlement | `/enrolement/soumettre` | `enrolement.soumettre` | |
| | Mes enrôlements · Préférences · Sécurité (2FA) | — | — | |
| | Gestion collaborateurs | `/comptes/collaborateurs` | — | société requise |
| | Demandes de compte | `/comptes/valider` | `compte.valider` | |
| | Modifier type de client | `/comptes/modifier-type` | `compte.type.modifier` | |
| | Catégories réclamation | `/reclamations/administration-categories` | `administration.categories_reclamation` | |
| | Notifications | `/administration/notifications` | `administration.notifications` | |
| | Comptes agents · Pièces exigées | `/administration/*` | `administration.comptes` | |
| | Journal d'audit | `/administration/audit` | `audit.consulter` | |

### Mesure avant / après — en navigateur, sur l'environnement UAT

Pour chaque profil : liens réellement présents dans la sidebar, et combien mènent à « Accès Refusé ».

| Profil | Avant | Après |
|---|---|---|
| `uat.agent.sansdroits` | 22 liens — **16 interdits** | 2 liens — **0 interdit** |
| `uat.support` | 22 liens — **14 interdits** | 3 liens — **0 interdit** |
| `uat.pilote.manifeste` | 22 liens — **12 interdits** | 4 liens — **0 interdit** |
| `uat.facturation` | 22 liens — **12 interdits** | 4 liens — **0 interdit** |
| `uat.superviseur` | 16 liens — **10 interdits** | 2 liens — **0 interdit** |
| `uat.transporteur` | 19 liens — **5 interdits** | 7 liens — **0 interdit** |
| `uat.client.normal` | 19 liens — **1 interdit** | 15 liens — **0 interdit** |
| `uat.admin.fonctionnel` | 16 liens — 0 interdit | 8 liens — **0 interdit** |
| **TOTAL** | **70 liens interdits visibles** | **0** |

Les groupes vides disparaissent : `agent.sansdroits` ne voit plus que « Espace Structure » et
« Administration & Sécurité », `support` gagne « Gestion des Dossiers » avec sa seule file.

Le cas qui résume le défaut : un agent **sans aucune habilitation** atterrissait sur un tableau de
bord le déclarant « 🟢 Disponible pour affectation » avec un menu dont 16 entrées sur 22 menaient à un
mur. Il voit désormais son tableau de bord et sa page de sécurité.

---

## 6. Extension de périmètre — signalée, pas dissimulée

**Une ligne hors des quatre P0 a été ajoutée** : `DepotDocumentRefuseException` dans la liste blanche
de `MessagesUtilisateur` (défaut **D-12**, classé P2).

Elle est indissociable de D-11a et D-11b. Sans elle, un refus **métier** légitime — extension non
autorisée, fichier trop lourd — continue de s'afficher comme « une erreur technique », sans dire quoi
corriger. C'est précisément ce qui a masqué les deux défauts pendant toute la recette : les deux
échouaient derrière le même message opaque. Corriger le dépôt sans rendre ses refus lisibles aurait
livré la moitié du travail.

C'est la seule sortie de périmètre. Aucun autre P1/P2/P3 n'a été traité.

---

## 7. Une régression introduite, trouvée et corrigée

En câblant la sidebar sur `ConstruireNavigationQuery`, j'ai fait interroger la base **à chaque rendu
du menu** — donc sur chaque page — via le `DbContext` partagé de la requête. Le `DbContext` n'est pas
sûr en accès concurrent : le menu se retrouvait en compétition avec le rendu de la page elle-même.

Le symptôme n'était pas là où on l'attendait : **6 tests HTTP** ont commencé à échouer
(`ApiCloisonnementTests` × 5, `SecCert04SessionTests`), avec `InternalServerError` au lieu de
`Forbidden` — et chacun **passait en isolation**, ce qui m'a d'abord fait conclure à tort à
l'instabilité LocalDB déjà documentée. La comparaison sur l'arbre de référence (314/314 vert) a
écarté cette hypothèse.

La correction est le motif que ce dépôt applique déjà partout ailleurs (cf. `75bd614`) : un
`IServiceScopeFactory`, un scope par appel. Suite revenue au vert intégral.

**La leçon vaut d'être écrite** : un composant de layout n'est pas une page. Ce qui est anodin dans un
écran devient systémique quand c'est rendu partout.

---

## 8. Tests

### 72 tests nouveaux

| Fichier | Nb | Ce qu'il verrouille |
|---|---|---|
| `Domain.Tests/Documents/ExtensionFichierTests.cs` | 23 | `.pdf` / `PDF` / `pdf` / plusieurs points / extension interdite / sans extension ; la liste déclarée **avec** le point accepte le fichier ; la taille reste refusée |
| `Application.Tests/Documents/DepotFluxNonRembobinableTests.cs` | 6 | un **flux non rembobinable réel** (double qui lève sur `Position` et `Seek`) ; contenu intégralement stocké **et** analysé ; plafond dépassé refusé ; non-régression du flux rembobinable |
| `Application.Tests/CustomerComplaint/FileCibleReclamationTests.cs` | 10 | réclamation réellement créée ; catégorie sans mapping → message exploitable ; file inexistante / d'un autre module refusée ; énumération filtrée ; aucun Guid fourni par l'appelant |
| `Application.Tests/Web/NavigationParPermissionsTests.cs` | 20 | agent sans droits → aucun lien métier ; permission accordée / retirée ; groupe vide absent ; portée par file ; admin partiel ; client et transporteur sans écran d'agent |
| `Web.Tests/Components/ModaleConfirmationVisibiliteTests.cs` | 9 | classe de couche dédiée ; sémantique de dialogue ; confirmer / annuler / Échap / clic arrière-plan ; état occupé neutralisant tout ; **la CSS place bien la confirmation à 1085** |
| `Web.Tests/Architecture/CablageNavigationTests.cs` | 4 | **test de câblage** : si `NavMenu` recommençait à décider lui-même, ou si l'une des deux règles cessait d'appeler `RegleHabilitation`, le test échoue |

### Tests existants modifiés — signalés

Aucune assertion n'a été affaiblie pour faire passer une correction. Les modifications sont
mécaniques (une dépendance ajoutée à un constructeur) ou expriment la nouvelle précondition légitime.

| Fichier | Nature |
|---|---|
| `CreerCategorieReclamationCommandTests`, `CreerCategorieReclamationIntegrationTests`, `ModifierCategoriesReclamationCommandTests`, `RecCert02CategoriesTests` | ajout de la garde au constructeur, via une doublure renvoyant une file conforme — ces tests continuent d'éprouver la création et la modification, pas la garde |
| `Web.Tests/Pages/CustomerComplaint/AdministrationCategoriesTests` | enregistrement des deux nouveaux services ; le test de création **choisit désormais une file cible**, ce qu'exige l'écran |
| `NavMenuTests`, `NavMenuEnrolementTests`, `MainLayoutIntegrationTests` | déclarent ce que l'utilisateur **détient**, là où ils déclaraient seulement son espace. C'est le fond du défaut corrigé |
| `Components/Layout/NavigationDeTest.cs` *(nouveau support)* | doublures explicites — le projet Web n'embarque pas de bibliothèque de mocks |

### Résultat

| Projet | Résultat |
|---|---|
| `CIT.Plateforme.Application.Tests` | **823 / 823** |
| `CIT.Plateforme.Domain.Tests` | **231 / 231** |
| `CIT.Plateforme.Infrastructure.Tests` | **175 / 175** |
| `CIT.Plateforme.Web.Tests` | **330 / 330** |
| **TOTAL** | **1559 / 1559** — aucune régression |

---

## 9. Non-régression UAT rejouée en navigateur

| Contrôle | Résultat |
|---|---|
| Simulation — 6 variantes (Dry, Reefer, lourd, dangereux, OOG, multi) | **identique** : 484 250 + 79 400 = **563 650 XOF** |
| Import CSV — refus atomique | *« Import refusé — aucune donnée persistée »*, erreurs ligne par ligne, table inchangée |
| Unicité BL | doublon écarté nommément, BL valide créé (`BIL-IMP-000008`) |
| Compte désactivé | refus, message générique inchangé |
| Agent sans droits | connexion possible, **aucun lien métier** dans la sidebar |
| Pagination du journal d'audit | 296 entrées, 6 pages, bornes correctes |

---

## 10. Reste ouvert

Non traité, **par périmètre** : D-02 (toast couvrant « Terminer »), D-03 (auteur du journal d'audit),
D-04 (page antiforgery brute), D-06 (GUID dans le 403), D-07, D-09 (`ToString` brut), D-10 (dossier
vide + succès trompeur), D-13, **D-15 au sens du rapport de recette** (préfixe dupliqué `AVO-AVO-`,
`ENR-ENR-`), D-16 (aucun `<h1>`), D-17 (débordement tablette), UI-R01, UI-R02, et les P3.

Deux observations nouvelles, non corrigées :

- le numéro de suivi d'une demande d'ouverture de compte est affiché comme **GUID brut**
  (`a19de045-171d-4a53-aab0-d7c30ab326d7`) — même famille que D-09 ;
- sur `/administration/pieces-exigees-compte`, une saisie au clavier suivie d'un clic direct sur
  « Ajouter » n'enregistre rien : les champs sont liés à la perte de focus. Même motif que
  l'observation déjà consignée sur la soumission de BL.

## 11. Environnement

L'application tourne toujours sur `https://localhost:7097` (base `CIT_UAT_RECETTE`). Le secret de
recette reste dans le magasin local ; à retirer une fois les vérifications terminées :

```
dotnet user-secrets remove "Uat:MotDePasse" --project src/CIT.Plateforme.Web
```

Données créées pendant cette remédiation : catégories `FACTURATION` (complétée) et `DELAI`, pièce
exigée `RCCM`, dossier de compte `UAT TEST LOGISTICS`, réclamation `CUS-REC-000007`, dossiers
`DOS-000008`, et l'habilitation `billing_export.paiement.valider` accordée à `uat.facturation` pour
faire progresser le ticket export jusqu'au BEE.
