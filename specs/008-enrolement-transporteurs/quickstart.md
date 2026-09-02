# Quickstart — Feature 008 (Enrôlement Transporteurs)

> **Document** : `specs/008-enrolement-transporteurs/quickstart.md`
> **Date** : 31 août 2026
> **But** : recette manuelle bout-en-bout validant REC-SCE-08 et les critères d'acceptation ENR-01→03 (spec.md §21), une fois l'implémentation réalisée (`/speckit.tasks` puis `/speckit.implement`). Ce document ne contient aucun code d'implémentation — il décrit le scénario à exécuter et le résultat attendu à chaque étape.

---

## Prérequis

1. Base SQL Server locale/Docker migrée avec les migrations F001-F006 (aucune migration F008 requise pour le schéma — cf. `data-model.md` §7).
2. Données de seed (`DevelopmentSeeder`) incluant :
   - File `Enrolement` / `Agent Gate`.
   - Un compte Agent Gate de démonstration, habilité `enrolement.instruire` sur cette file.
   - Un compte Transporteur/Chauffeur de démonstration (`Nature=Client`), habilité `enrolement.soumettre`, **sans société rattachée**.
3. Application Blazor Server démarrée (`dotnet run` sur `CIT.Plateforme.Web`).

---

## Parcours 1 — Demandeur (Transporteur/Chauffeur)

| # | Action | Résultat attendu |
|:---|:---|:---|
| 1 | Se connecter avec le compte Transporteur/Chauffeur de démonstration | Contexte utilisateur résolu en `EspaceFonctionnel.Client` (aucune société affichée — comportement déjà pris en charge nativement par `ObtenirContexteUtilisateurQuery`, cf. research.md D1) |
| 2 | Ouvrir "Nouvel enrôlement" | Formulaire de saisie (objet/description) + zone de dépôt de pièces, aucun champ "société" ni "véhicule" présent |
| 3 | Renseigner les informations et déposer au moins une pièce, puis soumettre | Ticket créé, numéro affiché, visible dans "Mes enrôlements" avec statut "En attente de prise en charge" |
| 4 | Tenter la soumission avec un champ obligatoire vide | Message d'erreur explicite, aucun ticket créé (C8-01, critère "Erreur") |
| 5 | Se déconnecter, tenter d'accéder directement à l'URL du formulaire de soumission | Redirection vers la page de connexion (critère "Autorisation") |

---

## Parcours 2 — Agent Gate : instruction jusqu'au complément

| # | Action | Résultat attendu |
|:---|:---|:---|
| 1 | Se connecter avec le compte Agent Gate de démonstration | Contexte résolu en `EspaceFonctionnel.Agent`, file "Agent Gate" visible |
| 2 | Ouvrir la file "Agent Gate" | Le dossier créé au Parcours 1 est visible (C8-10) |
| 3 | Prendre en charge le dossier | Statut → "En cours", dossier assigné à l'agent (C8-02) |
| 4 | Consulter les pièces déposées | Pièce du Parcours 1, étape 3, visible et téléchargeable |
| 5 | Demander un complément (motif libre) | Statut → "En attente client", le Transporteur/Chauffeur voit une action requise (C8-04) |

---

## Parcours 3 — Demandeur : réponse au complément

| # | Action | Résultat attendu |
|:---|:---|:---|
| 1 | Se reconnecter avec le compte Transporteur/Chauffeur | Le dossier du Parcours 1 affiche "En attente de votre réponse" |
| 2 | Répondre par un message et/ou déposer une pièce complémentaire | Message/pièce visible côté Agent Gate ; dès réception, le dossier repasse "En cours" (reprise automatique, C8-04) |
| 3 | Tenter de consulter le dossier d'un autre Transporteur (URL directe avec un autre ticketId) | Accès refusé (403) — cloisonnement vérifié (critère "Cloisonnement", C8-09) |

---

## Parcours 4 — Agent Gate : validation, convocation, badge, clôture

| # | Action | Résultat attendu |
|:---|:---|:---|
| 1 | Revenir sur le dossier (repassé "En cours") | Actions disponibles : Valider |
| 2 | Valider le dossier | Historique "EnrolementValide" ajouté ; statut reste "En cours" (C8-05, research.md D6) ; action "Déclencher la convocation" devient disponible |
| 3 | Déclencher la convocation | Historique "ConvocationDeclenchee" ajouté ; aucun envoi SMS/WhatsApp déclenché (frontière CLAR-ENR-06 vérifiée par absence, pas par test d'intégration externe) |
| 4 | Constater la remise physique du badge | Ticket clôturé (`DateClotureUtc` renseignée), n'apparaît plus dans la file active (C8-08) |
| 5 | Consulter l'historique complet du dossier | Séquence visible : TicketCree → TicketPrisEnCharge → TicketEnAttenteClient → ReponseClientRecue → EnrolementValide → ConvocationDeclenchee → BadgeRemisEtDossierCloture |

---

## Parcours 5 — Sécurité (tests négatifs, research.md D11)

| # | Action | Résultat attendu |
|:---|:---|:---|
| 1 | Un utilisateur `Nature=Client` sans `enrolement.instruire` tente de prendre en charge un dossier via un appel direct (contournement UI) | 403 côté serveur, quelle que soit l'UI |
| 2 | Un Agent Gate non habilité sur la file "Agent Gate" tente de consulter la file | 403 |
| 3 | Un Transporteur tente de valider ou de constater la remise d'un badge (même le sien) | 403 — ces actions sont réservées à l'Agent Gate, jamais accessibles au demandeur |
| 4 | Rejouer l'étape 3 du Parcours 1 avec un identifiant de ticket appartenant à un autre Transporteur transmis manuellement | Refusé — l'identité du déposant est toujours dérivée du contexte authentifié, jamais du client (anti-IDOR) |

---

## Rejeu intégral REC-SCE-08

Les Parcours 1 → 4 exécutés dans l'ordre reproduisent exactement le texte de `docs/04-matrice-recette.md` (ligne 39) : "Dossier à distance → Complément → Validation → Convocation photo/badge → Clôture", sans étape ajoutée ni omise. ENR-04 (notification) n'est pas testable en quickstart (canal non implémenté, CLAR-ENR-06) — seule l'absence d'implémentation est vérifiable (aucun appel sortant). ENR-05 (renouvellement badge) n'est pas couvert (Could, différé, CLAR-ENR-07).

---

## Non-régression à vérifier après implémentation

- Suite complète `dotnet test` (F001-F006, 512+ tests) toujours au vert.
- `dotnet ef migrations has-pending-model-changes` → aucun changement de modèle en attente (cohérent avec data-model.md : aucune migration de schéma requise).
- Parcours F006 (Customer Complaint) rejoué manuellement une fois : aucune régression sur `PrendreEnChargeCommand`, `AttenteClientCommand`, `PublierMessageCommand`, `ConsulterFileQuery` malgré l'ajout des branches Enrolement (research.md D3).
