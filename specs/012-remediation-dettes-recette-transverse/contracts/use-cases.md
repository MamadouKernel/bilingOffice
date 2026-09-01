# Contrats d'Acceptation & Cas d'Usage — Feature 012

> **Document** : `specs/012-remediation-dettes-recette-transverse/contracts/use-cases.md`  
> **Feature** : F012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves  
> **Date** : 1er septembre 2026  
> **Statut** : CONTRATS DÉFINIS (Phase 1)

---

## C12-01 : Déduction et Ordonnancement de Priorité (PRI-02, Must)

### Déclencheur
Création d'un nouveau ticket ou consultation d'une file de traitement.

### Préconditions
Le référentiel des niveaux de priorité contient au moins un niveau par défaut ("Normale", rang = 1).

### Flux Nominal
1. Lors de l'exécution d'une commande de création de ticket (`CreerTicketCommand`, `SoumettreDemandeImportCommand`, etc.), le système associe le `PrioriteId` par défaut si aucun n'est explicité.
2. L'agent accède à sa file de traitement (`ConsulterFileQuery`).
3. Les tickets sont ordonnés par `Priorite.Rang DESC`, puis `DateCreationUtc ASC`.
4. Les tickets modifiés manuellement vers une priorité haute (rang = 2 ou 3) s'affichent avant les tickets standards.

### Résultats Interdits
- ❌ Un ticket prioritaire créé après un ticket standard est relégué en bas de file.
- ❌ Une modification de priorité est appliquée sans traçage dans `JournalAudit`.

---

## C12-02 : Communication du Numéro de Ticket Import par E-mail (IMP-03, Must)

### Déclencheur
Soumission d'une demande Import multi-BL (`SoumettreDemandeImportCommand`).

### Préconditions
Le client dispose d'un compte actif avec un e-mail valide. Les règles de diffusion pour l'événement `TicketCree` sont activées pour InApp et Email.

### Flux Nominal
1. Le client soumet un ou plusieurs numéros de BL valides.
2. Pour chaque ticket créé, `SoumettreDemandeImportCommand` publie l'événement `TicketCree` dans `JournalEvenements`.
3. Le moteur de distribution (`DistribuerNotificationsUseCase`) génère :
   - une `Notification` in-app pour l'auteur de la demande ;
   - une entrée `EnvoiNotification` avec `Canal = Email` liée à cette notification.
4. L'historique et l'audit confirment l'enregistrement de la demande.

### Résultats Interdits
- ❌ Le ticket est créé mais aucun événement `TicketCree` n'est publié.
- ❌ Aucune entrée `EnvoiNotification` pour le canal Email n'est persistée.

---

## C12-03 : Administration des Catégories de Réclamations (REC-02, Must)

### Déclencheur
L'administrateur accède à l'écran `/reclamations/administration-categories`.

### Préconditions
L'administrateur dispose de la permission `complaint.categories.modifier`.

### Flux Nominal
1. L'administrateur crée une catégorie avec code "LIVRAISON" et libellé "Retard de livraison".
2. La catégorie est persistée avec `EstActif = true`.
3. L'administrateur modifie le libellé en "Retard de livraison conteneur".
4. L'administrateur désactive la catégorie : elle n'est plus proposée aux clients lors d'une nouvelle soumission mais les anciens tickets conservent leur lien.

### Résultats Interdits
- ❌ Un utilisateur sans permission accède ou modifie les catégories.
- ❌ Une catégorie désactivée apparaît dans la liste déroulante du formulaire client.

---

## C12-04 : Orientation Support Opérationnel et Instruction (REC-03, Must)

### Déclencheur
Soumission d'une réclamation par un client.

### Préconditions
La catégorie choisie pointe vers la file cible "Support Opérationnel".

### Flux Nominal
1. Le client soumet sa réclamation (`SoumettreReclamationCommand`).
2. Le ticket est créé avec `FileCouranteId` positionné sur la file Support Opérationnel.
3. L'agent Support consulte sa file, prend en charge le ticket et demande un complément d'information.
4. Le ticket passe en statut "En attente client".
5. Le client répond, fournissant les pièces demandées, et le ticket repasse en traitement Support.

### Résultats Interdits
- ❌ Le ticket est orienté vers une file non configurée ou codée en dur de manière incorrecte.
- ❌ L'instruction ne permet pas d'échanger des messages ou des documents.

---

## C12-05 : Stabilisation et Certification 2FA (SEC-08, Should)

### Déclencheur
Connexion d'un utilisateur ayant activé le second facteur TOTP.

### Préconditions
L'utilisateur possède un compte actif avec `TwoFactorEnabled = true` et une clé TOTP configurée.

### Flux Nominal
1. L'utilisateur saisit son e-mail et mot de passe sur `/comptes/connexion`.
2. Le système valide les identifiants et redirige vers `/comptes/verifier-code` sans ouvrir de session complète.
3. L'utilisateur saisit le code TOTP à 6 chiffres valide.
4. Le système valide le code via `TwoFactorAuthenticatorSignInAsync`, pose le cookie de session et redirige vers la page d'accueil ou de profil.
5. L'accès aux pages protégées (`[Authorize]`) est accordé.

### Résultats Interdits
- ❌ Le mot de passe seul permet d'accéder aux pages protégées.
- ❌ Un code invalide, vide ou périmé permet la connexion.
- ❌ La suite de test échoue en exécution répétée (flaky test).

---

## C12-06 : Export de Données Tabulaire au Format CSV (SOC-14, Could)

### Déclencheur
L'utilisateur clique sur le bouton "Exporter CSV" depuis la liste de ses tickets.

### Préconditions
L'utilisateur est authentifié.

### Flux Nominal
1. L'utilisateur déclenche la requête d'export `ExporterTicketsCsvQuery`.
2. Le système récupère la liste des tickets autorisés (filtrés par `ClientId` pour un client, ou par file pour un agent).
3. Le système génère une chaîne textuelle formatée :
   - En-tête : `NumeroTicket;Module;TypeDemande;Statut;FileCourante;DateCreationUtc;Priorite;ReferenceDossier`
   - Encodage : UTF-8 avec BOM (`0xEF, 0xBB, 0xBF`).
   - Séparateur : `;`.
4. Le navigateur télécharge le fichier `export-tickets-AAAA-MM-JJ-HHmm.csv`.

### Résultats Interdits
- ❌ Un client peut exporter les tickets d'une société concurrente (violation CPT-04 / SEC-03).
- ❌ Les caractères accentués sont corrompus à l'ouverture dans un tableur.

---

## C12-07 : Configuration Multicanale des Relances Automatiques (COM-06, Could)

### Déclencheur
Déclenchement du traitement périodique des relances sur un ticket en attente client au-delà du délai paramétré.

### Préconditions
L'événement `RelanceAutomatique` dispose de règles actives pour les canaux InApp et Email.

### Flux Nominal
1. Le système émet l'événement `RelanceAutomatique` pour le ticket éligible.
2. Le moteur de distribution lit les règles actives et les préférences du client.
3. Une notification InApp et un envoi Email sont créés.
4. Si le client a désactivé le canal Email dans ses préférences non obligatoires, seule la notification InApp est générée.

### Résultats Interdits
- ❌ L'échec ou la désactivation d'un canal bloque la distribution sur les autres canaux.

---

## C12-08 : Scénario Transverse de Bout en Bout REC-SCE-14

### Déclencheur
Exécution de la suite de test E2E `RecSce14NotificationsE2ETests.cs`.

### Flux Nominal (7 Volets Intégrés)
1. **Volet 1 (Notification)** : Le client soumet un dossier -> notification multicanale générée.
2. **Volet 2 (Lien sécurisé)** : Le lien de notification exige une authentification pour accéder au ticket.
3. **Volet 3 (Guichet)** : L'agent de guichet retrouve le dossier par recherche de numéro.
4. **Volet 4 (Rendez-vous)** : Prise de rendez-vous confirmée pour passage au bureau de facturation.
5. **Volet 5 (Dossier)** : Les tickets associés sont consultables sous leur référence dossier commune.
6. **Volet 6 (Habilitations)** : Un agent d'une autre file ne peut pas modifier indûment le ticket.
7. **Volet 7 (Audit)** : Chaque transition d'état et action est tracée de manière inviolable dans le journal d'audit.

### Résultats Interdits
- ❌ Le test E2E contourne l'authentification ou mocke destructivement la couche applicative.
