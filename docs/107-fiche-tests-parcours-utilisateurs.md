# Fiche de tests — Parcours utilisateurs YABA (version métier)

> Pour des testeurs **non techniques**. On vérifie que chaque type d'utilisateur peut faire son
> travail de bout en bout. Pour chaque étape : faites l'action, regardez ce qui s'affiche, puis cochez
> **OK** si le résultat correspond, ou **Problème** en écrivant ce qui ne va pas.

> **De la première à la dernière page.** Ce document suit le parcours **complet** :
> **① la page d'accueil publique** (sans être connecté) → **② la simulation de tarif** et
> **③ la création d'un compte société** → **④ la validation du compte par CIT** → **⑤ la première
> connexion et la sécurité à 6 chiffres** → **⑥ l'usage quotidien** par chaque rôle (client,
> admin société, agent, superviseur, administration, support) → **⑦ la fin de vie d'une demande**
> (traitement → clôture → archivage → enquête de satisfaction) → **⑧ la déconnexion**.
> Le **Parcours 0** ci-dessous couvre ① à ⑤ ; les parcours 1 à 7 couvrent ⑥ à ⑧.

## Avant de commencer
- **Adresse du site** : celle communiquée par l'équipe technique (ex. `https://…`).
- **Se connecter** : saisir l'**e-mail**, le **mot de passe**, puis le **code de sécurité à 6 chiffres** affiché par l'application d'authentification sur le téléphone (il change toutes les 30 s).
  - À la **toute première** connexion, l'application demande d'installer ce code de sécurité (un carré à scanner s'affiche). L'équipe technique l'aura préparé avant les tests.
- **Changer d'espace / de rôle** : en haut de l'écran, un menu permet de basculer entre les espaces disponibles pour le compte.
- **Langue et thème** : boutons en haut pour passer Français ⇆ Anglais et clair ⇆ sombre.

## Comptes de test fournis
| Profil testé | Compte à utiliser |
|---|---|
| Client **et** Administrateur de société | `client.demo@cit-demo.ci` (Awa) |
| Agent, Superviseur, Administration, Support | `m.konate@cotedivoireterminal.com` (Konate) |

Mot de passe : celui communiqué par l'équipe technique. Cochez : **☐ OK  ☐ Problème**.

---

## Parcours 0 — Découverte & création de compte (AVANT d'avoir un compte)
*Le tout début : ce que voit un visiteur, puis comment une société obtient son accès.*
**À faire sans être connecté** (ouvrir une fenêtre de navigation privée est idéal).

| # | Ce que je fais | Ce que je dois voir | OK / Problème |
|---|---|---|---|
| 0.1 | J'ouvre l'adresse du site (page d'accueil) | La page publique YABA : présentation, et 3 boutons — **Se connecter**, **Créer un compte société**, **Simuler une proforma** | ☐ / ☐ |
| 0.2 | Sur l'accueil, je passe Français ⇆ Anglais et clair ⇆ sombre | La page reste correcte et traduite, **sans être connecté** | ☐ / ☐ |
| 0.3 | Je clique sur **Simuler une proforma** | J'accède au simulateur **sans me connecter** ; je saisis des conteneurs et j'obtiens un montant estimé | ☐ / ☐ |
| 0.4 | Je reviens à l'accueil et je clique sur **Créer un compte société** | Un formulaire de demande de compte s'affiche (identité de la société, coordonnées, type de client) | ☐ / ☐ |
| 0.5 | Je remplis et j'envoie la demande de compte | Un message confirme que la demande est **envoyée et en attente de validation par CIT** | ☐ / ☐ |
| 0.6 | *(Côté CIT — compte Konate)* J'ouvre la **validation des comptes** et je valide la demande précédente | Le compte passe à **activé** ; le client peut désormais se connecter | ☐ / ☐ |
| 0.7 | *(Client)* **Première connexion** : e-mail + mot de passe | On me demande d'**installer la sécurité à 6 chiffres** : un carré à scanner s'affiche, à ajouter dans l'application d'authentification du téléphone | ☐ / ☐ |
| 0.8 | Je saisis le **code à 6 chiffres** affiché par le téléphone | Je suis connecté et j'arrive sur mon tableau de bord | ☐ / ☐ |
| 0.9 | *(Collaborateur invité — voir 2.2)* J'ouvre le **lien d'invitation** reçu | Je peux créer mon accès et rejoindre la société | ☐ / ☐ |

> À partir d'ici, le compte existe : les parcours 1 à 7 testent l'usage quotidien, puis la fin de vie
> d'une demande (clôture + archivage) et la déconnexion.

## Parcours 1 — Client (compte : Awa)
*Un client d'une société qui suit et dépose ses demandes.*

| # | Ce que je fais | Ce que je dois voir | OK / Problème |
|---|---|---|---|
| 1.1 | Je me connecte avec le compte client | J'arrive sur mon tableau de bord | ☐ / ☐ |
| 1.2 | J'ouvre « Mes demandes » | La liste de mes dossiers, avec leur état (nouveau, en cours, en attente, terminé) | ☐ / ☐ |
| 1.3 | J'ouvre le dossier marqué « en attente d'un complément » | Une indication claire de ce qu'on attend de moi | ☐ / ☐ |
| 1.4 | Je crée une **nouvelle demande** (import) et je la valide | La demande est enregistrée et apparaît dans « Mes demandes » | ☐ / ☐ |
| 1.5 | Je commence une demande, je l'enregistre sans finir, puis j'y reviens | Je retrouve ma saisie là où je l'avais laissée | ☐ / ☐ |
| 1.6 | Je pose une question à l'assistant : « qu'attend-on de moi ? » | Une réponse claire, basée sur mes vraies demandes | ☐ / ☐ |
| 1.7 | Je bascule en anglais et je repose la question | La réponse s'affiche en anglais | ☐ / ☐ |
| 1.8 | J'ouvre mes notifications | Je vois les messages me concernant, je peux les marquer comme lus | ☐ / ☐ |
| 1.9 | J'ouvre mes préférences de notification | Je peux choisir de recevoir par e-mail (en plus des messages dans l'application) | ☐ / ☐ |
| 1.10 | J'exporte ma liste de demandes | Un fichier se télécharge avec mes demandes | ☐ / ☐ |
| 1.11 | Je me déconnecte | Je reviens à l'accueil ; si j'essaie de revenir en arrière, on me redemande de me connecter | ☐ / ☐ |

## Parcours 2 — Administrateur de société (compte : Awa)
*Le responsable qui gère les collaborateurs de sa société.*

| # | Ce que je fais | Ce que je dois voir | OK / Problème |
|---|---|---|---|
| 2.1 | Je vais sur le tableau de bord de ma société | Une vue d'ensemble de l'activité de ma société | ☐ / ☐ |
| 2.2 | J'invite un collaborateur (saisie de son e-mail) | Une invitation est créée ; un lien à lui transmettre s'affiche | ☐ / ☐ |
| 2.3 | Je consulte la liste des invitations | L'invitation apparaît avec son état (en attente) | ☐ / ☐ |
| 2.4 | Je révoque une invitation | Elle passe à l'état révoqué | ☐ / ☐ |

## Parcours 3 — Agent (compte : Konate)
*L'agent CIT qui traite les demandes.*

| # | Ce que je fais | Ce que je dois voir | OK / Problème |
|---|---|---|---|
| 3.1 | Je me connecte et j'ouvre ma file de traitement | La liste des demandes à traiter | ☐ / ☐ |
| 3.2 | Je prends en charge une demande « nouvelle » | Elle passe « en cours » et m'est attribuée | ☐ / ☐ |
| 3.3 | Je demande un complément au client | La demande passe « en attente client » ; un message part au client | ☐ / ☐ |
| 3.4 | J'écris une réponse / une note sur le dossier | Le message s'ajoute à l'historique de la demande | ☐ / ☐ |
| 3.5 | Je clôture une demande traitée | La demande passe « terminée » avec son motif | ☐ / ☐ |

## Parcours 4 — Superviseur (compte : Konate)
*Le responsable qui pilote l'activité et répartit le travail.*

| # | Ce que je fais | Ce que je dois voir | OK / Problème |
|---|---|---|---|
| 4.1 | J'ouvre la supervision par file | Pour chaque file : nombre de demandes en attente, en cours, ancienneté, alertes de délai | ☐ / ☐ |
| 4.2 | J'ouvre la supervision par agent | La charge de chaque agent et sa disponibilité | ☐ / ☐ |
| 4.3 | Pendant que je regarde la supervision, une demande est traitée (2ᵉ onglet ou collègue) | L'écran se met à jour **tout seul**, sans recharger la page | ☐ / ☐ |
| 4.4 | Je réaffecte / transfère une demande à un autre agent | Le changement est pris en compte | ☐ / ☐ |
| 4.5 | Je change la priorité d'une demande | Elle remonte dans la file | ☐ / ☐ |

## Parcours 5 — Administration CIT (compte : Konate)
*Le paramétrage de la plateforme.*

| # | Ce que je fais | Ce que je dois voir | OK / Problème |
|---|---|---|---|
| 5.1 | J'ouvre la gestion des comptes agents | La liste des agents et leurs droits | ☐ / ☐ |
| 5.2 | J'ajoute puis je retire un droit à un agent | Le changement s'applique à cet agent | ☐ / ☐ |
| 5.3 | Je modifie un référentiel (files, priorités, délais) | La modification est enregistrée | ☐ / ☐ |
| 5.4 | J'ouvre les modèles de notification | Je peux consulter/éditer les messages types | ☐ / ☐ |
| 5.5 | J'ouvre le journal d'audit | Les actions importantes sont tracées (qui, quand) | ☐ / ☐ |
| 5.6 | J'ouvre les archives | Les demandes archivées sont consultables (lecture seule) | ☐ / ☐ |
| 5.7 | Un utilisateur a **perdu son téléphone ET ses codes de secours** : j'ouvre sa fiche (Comptes agents / Comptes utilisateurs) et je lance **Réinitialiser la double authentification** | Un message confirme la réinitialisation ; à sa prochaine connexion l'utilisateur devra réinstaller son code à 6 chiffres (mot de passe inchangé) | ☐ / ☐ |
| 5.8 | J'essaie de réinitialiser **ma propre** double authentification | L'action est refusée (un admin ne peut pas le faire sur lui-même) | ☐ / ☐ |

## Parcours 6 — Support IT N1 (compte : Konate)
*L'équipe support qui diagnostique.*

| # | Ce que je fais | Ce que je dois voir | OK / Problème |
|---|---|---|---|
| 6.1 | J'ouvre l'espace Support IT | La file des incidents support | ☐ / ☐ |
| 6.2 | Je recherche un dossier / une demande | Je retrouve l'élément recherché en consultation | ☐ / ☐ |

## Parcours 7 — Contrôles transverses (tous profils)
| # | Ce que je fais | Ce que je dois voir | OK / Problème |
|---|---|---|---|
| 7.1 | Je passe Français ⇆ Anglais sur plusieurs écrans | Tout est bien traduit (aucun libellé étrange) | ☐ / ☐ |
| 7.2 | Je passe thème clair ⇆ sombre | L'affichage reste lisible | ☐ / ☐ |
| 7.3 | J'ouvre le site sur un téléphone (ou fenêtre étroite) | L'affichage s'adapte et reste utilisable | ☐ / ☐ |
| 7.4 | En tant que client, j'essaie d'ouvrir une page d'administration | L'accès m'est refusé | ☐ / ☐ |

---

## Feuille de relevé des problèmes
| # étape | Ce qui s'est passé (au lieu du résultat attendu) | Gravité (bloquant / gênant / mineur) |
|---|---|---|
|  |  |  |
|  |  |  |
|  |  |  |

**Testeur :** ______________________  **Date :** ____________  **Version testée :** ____________

> Astuce présentateur : garder deux onglets ouverts (un « client », un « superviseur ») rend la
> mise à jour en temps réel (étape 4.3) très parlante.
