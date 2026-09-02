# Guide d'Exécution Rapide & Parcours de Validation — Feature 012

> **Document** : `specs/012-remediation-dettes-recette-transverse/quickstart.md`  
> **Feature** : F012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves  
> **Date** : 1er septembre 2026  
> **Statut** : PARCOURS DE CERTIFICATION VALIDÉS (Phase 1)

---

## 1. Vue d'Ensemble des 8 Parcours de Validation

| Parcours | Exigence / Sujet | Type de Preuve | Commande de Test Rapide |
|:---|:---:|:---:|:---|
| **A** | `PRI-02` | Test unitaire / intégration | `dotnet test tests/CIT.Plateforme.Application.Tests --filter FullyQualifiedName~PriCert01` |
| **B** | `IMP-03` | Test d'intégration | `dotnet test tests/CIT.Plateforme.Application.Tests --filter FullyQualifiedName~ImpCert03` |
| **C** | `REC-02` | Test de composant / query | `dotnet test tests/CIT.Plateforme.Application.Tests --filter FullyQualifiedName~RecCert02` |
| **D** | `REC-03` | Test applicatif | `dotnet test tests/CIT.Plateforme.Application.Tests --filter FullyQualifiedName~RecCert03` |
| **E** | `SEC-08` | Test d'intégration Web (10 runs) | `dotnet test tests/CIT.Plateforme.Web.Tests --filter FullyQualifiedName~SecCert08` |
| **F** | `SOC-14` | Test unitaire CSV & sécurité | `dotnet test tests/CIT.Plateforme.Application.Tests --filter FullyQualifiedName~SocCert14` |
| **G** | `COM-06` | Test de notification | `dotnet test tests/CIT.Plateforme.Application.Tests --filter FullyQualifiedName~ComCert06` |
| **H** | `REC-SCE-14` | Test E2E transverse complet | `dotnet test tests/CIT.Plateforme.Web.Tests --filter FullyQualifiedName~RecSce14` |

---

## 2. Détail des Parcours

### Parcours A — Priorité Automatique (PRI-02)
1. Créer un ticket via `CreerTicketCommand` ou `SoumettreDemandeImportCommand` sans spécifier de priorité.
2. Vérifier que `ticket.PrioriteId` correspond au niveau standard paramétré.
3. Insérer un ticket avec priorité Haute (rang 2) après un ticket standard (rang 1).
4. Exécuter `ConsulterFileQuery` et constater que le ticket prioritaire est positionné en premier.

### Parcours B — E-mail Ticket Import (IMP-03)
1. Exécuter `SoumettreDemandeImportCommand` avec un numéro de BL valide.
2. Vérifier qu'une ligne `TicketCree` est insérée dans `JournalEvenements`.
3. Exécuter `DistribuerNotificationsUseCase.DistribuerAsync()`.
4. Vérifier la création d'une entrée `Notification` pour le client et d'une entrée `EnvoiNotification` avec `Canal = CanalNotification.Email`.

### Parcours C — Catégories de Réclamations (REC-02)
1. Vérifier via `AdministrationCategories.razor` / `CreerCategorieReclamationCommand` la création d'une nouvelle catégorie.
2. Modifier son libellé et la désactiver.
3. S'assurer qu'elle n'est plus proposée pour de nouvelles soumissions (`ListerCategoriesReclamationQuery`).

### Parcours D — Orientation Support Opérationnel (REC-03)
1. Soumettre une réclamation via `SoumettreReclamationCommand`.
2. Vérifier que `ticket.FileCouranteId` pointe vers la file cible Support Opérationnel.
3. Vérifier que l'agent Support peut demander un complément et que le ticket passe en attente client.

### Parcours E — Stabilité 2FA (SEC-08)
1. Exécuter la suite `ParcoursConnexion2FaTests` / `SecCert08DoubleAuthentificationTests`.
2. Vérifier les 10 runs consécutifs sans aucun échec.

### Parcours F — Export CSV (SOC-14)
1. Exécuter `ExporterTicketsCsvQuery` pour un utilisateur client.
2. Vérifier que le flux texte commence par le BOM UTF-8 et contient les en-têtes avec séparateur point-virgule.
3. Vérifier que seuls les tickets appartenant à sa propre société sont inclus dans le flux.

### Parcours G — Relances Multicanales (COM-06)
1. Simuler l'événement `RelanceAutomatique`.
2. Vérifier la diffusion sur les canaux configurés (InApp et Email).
3. Désactiver le canal Email dans les préférences client et vérifier que seul le canal InApp est déclenché.

### Parcours H — Recette Transverse REC-SCE-14 E2E
1. Lancer `RecSce14NotificationsE2ETests`.
2. Vérifier le déroulement fluide et vert des 7 volets sans erreur d'isolation de données.
