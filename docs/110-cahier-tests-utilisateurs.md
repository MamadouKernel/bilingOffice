# Cahier de tests utilisateurs YABA

**Version :** 1.0 · **Date :** 2 octobre 2026 · **À utiliser en environnement de recette ou de développement uniquement**

Ce cahier sert à vérifier les parcours avec les personnes qui utiliseront l’application. Il ne faut
pas connaître l’informatique pour le suivre : faites l’action décrite, comparez l’écran au résultat
attendu, puis cochez **Réussi** ou **À corriger**. Inscrivez les écarts dans la fiche d’anomalie à la
fin.

## Avant de commencer

- Faites ces vérifications sur une adresse de **test** confirmée par l’équipe projet. N’essayez pas
  les opérations de soumission, modification, clôture ou suppression sur le site de production.
- Demandez à l’équipe de recette des comptes et mots de passe temporaires pour les rôles testés.
  Aucun mot de passe n’est inscrit dans ce document.
- Prévoyez une application d’authentification sur téléphone pour le code de sécurité à 6 chiffres.
- Vérifiez que le jeu de données de démonstration est présent et que les courriels de recette ne
  partent pas vers de vraies adresses.
- Pour refaire le test après des changements, demandez à l’équipe de recette de réinitialiser le jeu
  de données. Ne demandez jamais une remise à zéro d’une base réelle.

## Jeu de données fictif

Ces données servent aux essais manuels. Elles sont inventées; ne les utilisez pas pour une demande
réelle. Le jeu local automatisé de démonstration est décrit dans
[`104-script-demo-live.md`](104-script-demo-live.md).

### Société et demandes

| Donnée de test | Valeur |
|---|---|
| Société | Société Démo Import SARL |
| Type de client | En compte |
| Contact de démonstration | Awa Client Démo |
| BL d’essai à créer | `TESTBL-2026-1001` |
| Booking d’essai à créer | `TESTBOOK-2026-1001` |
| Réclamation d’essai | « Pièce de facture illisible » |
| Chauffeur d’essai | Koffi Démo |
| Véhicule d’essai | `AB-1234-CD` |
| Conteneur d’essai | `CIT-DEMO-001`, taille 20 pieds, catégorie proposée dans le formulaire |

Ne créez pas à nouveau un dossier si le numéro de référence existe déjà : utilisez un suffixe
différent communiqué par l’équipe de recette.

### Données de démo déjà préparées

En développement, le seeder peut créer le compte client `client.demo@cit-demo.ci`, le compte interne
`m.konate@cotedivoireterminal.com` et quatre demandes IMPORT. Les identifiants de dossier finissent
par `0001` à `0004` : nouvelle, en cours, en attente de réponse client et clôturée. Le mot de passe
vient de la configuration locale `CIT_TEST_USERS_PASSWORD`; demandez-le à l’équipe technique. Ces
comptes ne sont pas des comptes de production. M. Konaté possède un profil super-administrateur de
développement et ne convient pas pour prouver le cloisonnement fin entre deux rôles internes.

## Résultats à cocher

Pour chaque test, cochez **Réussi** si l’écran correspond au résultat attendu. Sinon cochez
**À corriger**, notez ce que vous avez vu et faites une capture d’écran sans données personnelles.

### A. Visiteur et ouverture de compte

| ID | Action à réaliser | Résultat attendu | Résultat |
|---|---|---|---|
| A01 | Ouvrir la page d’accueil sans se connecter. | Les choix « Se connecter », « Créer un compte société » et « Simuler une proforma » sont faciles à repérer. | ☐ Réussi ☐ À corriger |
| A02 | Ouvrir le simulateur sans compte, ajouter un conteneur et calculer. | Un montant estimatif et les rubriques correspondantes apparaissent; le document est présenté comme non contractuel. | ☐ Réussi ☐ À corriger |
| A03 | Télécharger le PDF depuis le simulateur. | Un fichier PDF est téléchargé et contient la facture proforma, sans le formulaire ni le menu du site. | ☐ Réussi ☐ À corriger |
| A04 | Ouvrir le formulaire de demande de compte société. | Les champs demandés sont compréhensibles; les pièces requises sont indiquées avant l’envoi. | ☐ Réussi ☐ À corriger |
| A05 | Envoyer une demande de recette complète. | Un message confirme l’envoi et indique que CIT doit examiner la demande. | ☐ Réussi ☐ À corriger |

### B. Client et administrateur de société

| ID | Action à réaliser | Résultat attendu | Résultat |
|---|---|---|---|
| B01 | Se connecter comme client société. | Le tableau de bord de la bonne société s’ouvre; les libellés expliquent les indicateurs. | ☐ Réussi ☐ À corriger |
| B02 | Ouvrir « Mes demandes ». | Les demandes autorisées apparaissent avec un état que l’on peut comprendre. | ☐ Réussi ☐ À corriger |
| B03 | Ouvrir la demande en attente de réponse client. | L’application explique clairement ce qui est attendu et comment joindre une pièce. | ☐ Réussi ☐ À corriger |
| B04 | Répondre au complément avec le texte « Pièce ajoutée pour vérification » et joindre un fichier de test. | La réponse et la pièce apparaissent dans l’historique; la demande repart vers CIT. | ☐ Réussi ☐ À corriger |
| B05 | Créer une demande IMPORT avec `TESTBL-2026-1001`. | Les champs et pièces à fournir sont expliqués; après envoi, un numéro de dossier est affiché. | ☐ Réussi ☐ À corriger |
| B06 | Soumettre deux fois la même référence BL encore active. | La seconde demande est bloquée ou l’application indique clairement le dossier existant. | ☐ Réussi ☐ À corriger |
| B07 | Créer une demande EXPORT avec `TESTBOOK-2026-1001`. | Le formulaire explique les pièces à joindre; après envoi, la demande peut être suivie. | ☐ Réussi ☐ À corriger |
| B08 | Déposer une réclamation fictive, puis la retrouver dans « Mes réclamations ». | La confirmation donne une référence; l’état et la prochaine étape sont visibles. | ☐ Réussi ☐ À corriger |
| B09 | Télécharger l’historique PDF d’une simulation enregistrée (si le compte y a accès). | Le fichier est un PDF lisible qui ne contient que la facture. | ☐ Réussi ☐ À corriger |
| B10 | Comme administrateur société, ouvrir le tableau de bord société et inviter un second compte de recette. | Le nombre de demandes de la société est cohérent; l’invitation montre son état et la prochaine action. | ☐ Réussi ☐ À corriger |
| B11 | Se connecter comme collaborateur de recette. | Le collaborateur accède aux fonctions accordées, mais ne peut pas gérer les collaborateurs ni les paramètres de la société. | ☐ Réussi ☐ À corriger |

### C. Transporteur ou chauffeur

| ID | Action à réaliser | Résultat attendu | Résultat |
|---|---|---|---|
| C01 | Créer/ouvrir le compte chauffeur de recette. | Le parcours explique les données et pièces nécessaires à l’enrôlement. | ☐ Réussi ☐ À corriger |
| C02 | Soumettre un dossier chauffeur avec Koffi Démo et `AB-1234-CD`. | Une confirmation et une référence de suivi apparaissent. | ☐ Réussi ☐ À corriger |
| C03 | Ouvrir « Mes enrôlements ». | Le chauffeur voit son dossier et son état, sans voir les dossiers d’autres chauffeurs. | ☐ Réussi ☐ À corriger |
| C04 | Essayer d’ouvrir une fonction de facturation société. | L’accès n’est pas accordé; un message indique quoi faire ou à qui s’adresser. | ☐ Réussi ☐ À corriger |

### D. Agent métier

| ID | Action à réaliser | Résultat attendu | Résultat |
|---|---|---|---|
| D01 | Se connecter avec un compte agent de recette habilité sur une file. | Seules les files et les actions prévues pour cet agent sont visibles. | ☐ Réussi ☐ À corriger |
| D02 | Ouvrir une demande de test et la prendre en charge. | Le nom de l’agent et le nouvel état sont visibles dans l’historique. | ☐ Réussi ☐ À corriger |
| D03 | Demander un complément en précisant « Merci de joindre la pièce manquante ». | Le motif est obligatoire, compréhensible pour le client et transmis dans le dossier. | ☐ Réussi ☐ À corriger |
| D04 | Envoyer un message client et une note interne sur le dossier de test. | Le message client est visible côté client; la note interne ne l’est pas. | ☐ Réussi ☐ À corriger |
| D05 | Tenter d’ouvrir une file non attribuée au compte. | Le lien n’est pas proposé et l’accès direct est refusé. | ☐ Réussi ☐ À corriger |
| D06 | Sur un dossier de recette dédié, choisir la transition de fin disponible et fournir le motif demandé. | Le dossier passe à l’état suivant autorisé et l’action est inscrite à l’historique. | ☐ Réussi ☐ À corriger |

### E. Supervision et administration CIT

| ID | Action à réaliser | Résultat attendu | Résultat |
|---|---|---|---|
| E01 | Ouvrir la supervision avec un compte superviseur dédié. | Les indicateurs correspondent au périmètre de supervision de ce compte. | ☐ Réussi ☐ À corriger |
| E02 | Ouvrir la disponibilité des agents. | Les états affichés sont compréhensibles et à jour. | ☐ Réussi ☐ À corriger |
| E03 | Réaffecter un dossier test si cette fonction est accordée. | Le nouvel agent et le motif sont enregistrés; le superviseur ne dépasse pas son périmètre. | ☐ Réussi ☐ À corriger |
| E04 | Ouvrir l’administration des comptes agents avec un compte autorisé. | La liste, l’état et les habilitations sont lisibles; chaque droit peut être relié à une tâche. | ☐ Réussi ☐ À corriger |
| E05 | Accorder à un agent test une habilitation temporaire sur une file, puis la retirer. | L’accès suit le changement et aucune autre habilitation n’est modifiée. | ☐ Réussi ☐ À corriger |
| E06 | Consulter le journal d’audit avec un compte autorisé. | Les actions importantes indiquent au minimum l’auteur et la date. | ☐ Réussi ☐ À corriger |

### F. Support et contrôles de confort

| ID | Action à réaliser | Résultat attendu | Résultat |
|---|---|---|---|
| F01 | Depuis le bouton « Aide », lire l’explication de la page et ouvrir le centre d’aide. | L’aide répond à « à quoi sert cette page ? » et « que faire ensuite ? » avec des mots métier. | ☐ Réussi ☐ À corriger |
| F02 | Reprendre une visite guidée depuis l’en-tête. | Les étapes expliquent où se trouvent les fonctions principales du profil. | ☐ Réussi ☐ À corriger |
| F03 | Ouvrir une page en anglais, puis revenir en français. | Les instructions et boutons restent compréhensibles dans les deux langues. | ☐ Réussi ☐ À corriger |
| F04 | Afficher le site dans une fenêtre étroite ou sur téléphone. | Les champs, boutons et tableaux restent utilisables. | ☐ Réussi ☐ À corriger |
| F05 | Signaler un incident technique fictif depuis l’aide. | Le formulaire distingue un problème de la plateforme d’une question sur un dossier. | ☐ Réussi ☐ À corriger |

## Fiche de signalement d’un problème

| Champ | À remplir par le testeur |
|---|---|
| Numéro du test (ex. B03) | |
| Testeur et profil utilisé | |
| Date et heure (heure locale) | |
| Ce que je voulais faire | |
| Ce que j’ai vu à la place | |
| Gravité | ☐ Bloque le travail ☐ Gênant ☐ Détail visuel |
| Capture jointe (sans donnée personnelle) | ☐ Oui ☐ Non |

## Fin de la séance

- Informer l’équipe de recette des résultats et des anomalies.
- Se déconnecter de chaque compte utilisé et supprimer les fichiers fictifs téléchargés.
- Demander la réinitialisation des données de recette si elles doivent être rejouées.
- Ne jamais copier de vrais mots de passe, pièces d’identité ou dossiers clients dans une anomalie.
