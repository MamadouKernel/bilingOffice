# Data Model — Feature 008 (Enrôlement Transporteurs)

> **Document** : `specs/008-enrolement-transporteurs/data-model.md`
> **Date** : 31 août 2026
> **Principe directeur** (research.md, synthèse) : F008 ne modifie **aucun schéma SQL existant** et n'introduit **aucune nouvelle table**. Toute la persistance nécessaire est déjà fournie par F001 (Ticket, Dossier, Document, Message, Historique/Audit, FileDeTraitement) et F002 (ApplicationUser, HabilitationUtilisateur) — conformément à D1-D9 de `research.md`.

---

## 1. Entités réutilisées sans extension

| Entité | Origine | Usage F008 |
|:---|:---|:---|
| `Ticket` | F001 (`Domain/Tickets/Ticket.cs`) | Un ticket par dossier d'enrôlement, `Module = "Enrolement"`, `TypeDemande = "Enrolement"` |
| `Dossier` | F001 (`Domain/Tickets/Dossier.cs`) | `ClientId` = `ApplicationUser.Id` du Transporteur/Chauffeur (D1) ; `AuteurId` = `null` ; `TypeClient` = `null` (pas de notion de type client pour un individu sans société) |
| `Document` | F001 (`Domain/Documents/Document.cs`) | Pièces déposées à la soumission (étape 1) et en réponse à un complément (étape 3), via `DeposerDocumentCommand` inchangée |
| `Message` | F001 (`Domain/Messagerie/Message.cs`) | Échanges Agent Gate ↔ Transporteur/Chauffeur (vérification, complément), via `PublierMessageCommand` |
| `PeriodeAttenteClient` | F001 (remédiation audit) | Suivi de la période "en attente de complément" (SLA suspendu, TPS-04) |
| `FileDeTraitement` | F001 (`Domain/Files/FileDeTraitement.cs`) | Une ligne de donnée : `Module="Enrolement"`, `Nom="Agent Gate"` |
| `HabilitationUtilisateur` | F002 (`Domain/Securite/HabilitationUtilisateur.cs`) | Attribution de `Permission.EnrolementSoumettre` / `Permission.EnrolementInstruire` |
| `ApplicationUser` | F002 (`Infrastructure/Identity/ApplicationUser.cs`) | Identité du Transporteur/Chauffeur (`Nature = Client`) et de l'Agent Gate (`Nature = Agent`) |
| `EntreeHistorique` (Audit) | F001 | Trace de chaque étape : soumission, prise en charge, complément demandé/fourni, validation, convocation, badge remis/clôture |

**Aucune** `SocieteCliente`, `UtilisateurSociete`, ou entité de type "société transporteur" n'est créée ni référencée (CLAR-ENR-01, RÉSOLU).

---

## 2. Extension additive de Domain

### 2.1 `Ticket` — nouvelle méthode (pas de nouvelle colonne)

```
Ticket.DefinirInformationsEnrolement(string objet, string description)
```

- Réutilise les colonnes existantes `Ticket.Objet` / `Ticket.Description` (`string?`, ajoutées en F006, génériques dans leur définition EF — voir `PlateformeDbContext`, aucune contrainte nommée "Reclamation").
- Mêmes garde-fous que `DefinirContenuReclamation` : `objet` et `description` non vides (`ArgumentException` sinon).
- **Aucune migration EF requise** — les colonnes existent déjà, nullable, compatibles avec cet usage.

Invariant : `Objet`/`Description` sont renseignés à la création du ticket d'enrôlement (étape 1), jamais après — cohérent avec le fait qu'ENR-01 ne prévoit pas de modification ultérieure du contenu de la demande hors mécanisme de complément (qui passe par `Message`/`Document`, pas par ces champs).

### 2.2 Pas d'autre extension Domain

`StatutTicket`, `MotifClotureType`, `Ticket` (autres propriétés), `Dossier`, `Document`, `Message`, `FileDeTraitement` : **aucune modification**. Voir research.md D6 (aucun nouvel état de cycle de vie — convocation et remise de badge sont des événements d'historique, jamais des colonnes ni des valeurs d'énumération).

---

## 3. Nouvelles permissions (données de configuration, pas de schéma)

Ajout dans `Domain.Securite.Permission` (fichier existant, ajout de constantes — pas une nouvelle table, `HabilitationUtilisateur.PermissionCode` est déjà un `string` libre) :

```csharp
public const string EnrolementSoumettre = "enrolement.soumettre";
public const string EnrolementInstruire = "enrolement.instruire";
```

Ces codes sont ensuite attribués via des lignes `HabilitationUtilisateur` (mécanisme existant, table déjà en place) :
- `EnrolementSoumettre` : accordée à tout `ApplicationUser` de `Nature = Client` souhaitant s'enrôler (paramétrable — décision d'attribution par défaut vs sur inscription différée à `/speckit.tasks`, comme le sont déjà `ComplaintSoumettre`/`BillingImportSoumettre` pour les clients existants).
- `EnrolementInstruire` : accordée aux comptes Agent Gate, bornée à la file `"Agent Gate"` (`HabilitationUtilisateur.FileId`).

---

## 4. Nouvelle méthode de repository (interface, pas de table)

```csharp
// Application/Abstractions/Repositories.cs — IDossierRepository (extension additive)
Task<IReadOnlyList<Dossier>> ListerParClientAsync(Guid clientId, CancellationToken ct = default);
```

Implémentation Infrastructure : requête EF `context.Dossiers.Where(d => d.ClientId == clientId)` — équivalente à `ListerParSocieteAsync` déjà existante, aucun changement de schéma. Un index secondaire sur `Dossier.ClientId` **peut** être ajouté en migration pure-index (pas de nouvelle colonne) si le volume le justifie ; non requis pour la correction fonctionnelle et laissé à l'appréciation de `/speckit.tasks` (Principe XIII — ne pas inventer une volumétrie non fournie par le CDC).

---

## 5. Diagramme conceptuel des relations exploitées

```
ApplicationUser (Nature=Client)  ──1───1──  Dossier (ClientId = ApplicationUser.Id, AuteurId = null)
                                                  │
                                                  │ 1─N (toujours 1 pour F008, cf. D2)
                                                  ▼
                                               Ticket (Module="Enrolement", TypeDemande="Enrolement")
                                                  │
                        ┌─────────────┬───────────┼───────────┬──────────────┐
                        ▼             ▼           ▼           ▼              ▼
                  Document[]     Message[]  PeriodeAttenteClient[]  EntreeHistorique[]  (Objet, Description)
                        
FileDeTraitement (Module="Enrolement", Nom="Agent Gate")  ──  Ticket.FileCouranteId

ApplicationUser (Nature=Agent) ── HabilitationUtilisateur(PermissionCode="enrolement.instruire", FileId=Agent Gate.Id)
```

---

## 6. Invariants métier (portés par le Domain existant, aucun nouveau)

| Invariant | Portée par |
|:---|:---|
| Un ticket ne peut être clôturé deux fois | `Ticket.Cloturer` (garde existante, `StatutTicket.Cloture/Annule` → exception) |
| Un ticket ne peut être pris en charge que s'il est `EnAttenteDePriseEnCharge` | `Ticket.PrendreEnCharge` (garde existante) |
| Un ticket ne peut recevoir une réponse de complément que s'il est `EnAttenteClient` | `Ticket.RependreApresReponseClient` (garde existante) |
| Un client ne peut agir que sur les tickets dont il est propriétaire (`Dossier.ClientId`) | `CloisonnementService.ExigerProprieteClientAsync` (générique, inchangé) |
| Une habilitation Agent Gate est vérifiée côté serveur avant toute action d'instruction | `IAutorisationService.ExigerAsync` (générique, inchangé) |

Aucun invariant nouveau n'est nécessaire côté Domain — F008 compose des capacités déjà garanties par construction.

---

## 7. Stratégie EF Core / SQL Server

- **Migration** : néant pour le schéma (aucune colonne, aucune table). Une migration **facultative** peut être produite pour l'index secondaire optionnel sur `Dossier.ClientId` (§4) — à trancher en `/speckit.tasks`.
- **Seed de données** (`DevelopmentSeeder.cs`, pattern existant, pas de migration) :
  - `FileDeTraitement.Creer("Enrolement", "Agent Gate")`
  - Compte Agent Gate de démonstration avec `HabilitationUtilisateur(EnrolementInstruire, FileId=Agent Gate)`
  - Compte Transporteur/Chauffeur de démonstration (`Nature=Client`, `EnrolementSoumettre`)
- **Suppression/rétention** : aucune opération destructrice nouvelle — la clôture reste un simple horodatage (`DateClotureUtc`), jamais une suppression (SOC-09, Constitution Principe VIII, cohérent avec tous les modules existants).

---

## 8. Ce que ce modèle exclut explicitement (rappel des frontières spec.md §22)

- Pas d'entité "Véhicule/Camion".
- Pas d'entité "Badge" (numéro, technologie) — uniquement un événement d'historique.
- Pas d'entité "SociétéTransporteur".
- Pas de colonne "canal de notification" — hors périmètre F008 (CLAR-ENR-06).
- Pas de colonne "date d'expiration badge" — ENR-05/CLAR-ENR-07 différée (D12, research.md).
