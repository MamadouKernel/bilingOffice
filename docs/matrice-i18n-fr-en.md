# Matrice i18n FR/EN — état par surface

**Date** : 12 septembre 2026. `✅ conforme · 🟡 partiel · ❌ non localisé`.
Vue par domaine (81 pages / 101 composants ; détail des 7 localisés en bas).

| Domaine | Composants | FR | EN | Title | Validation | Enum | A11y | Browser | Statut |
|---|---|---|---|---|---|---|---|---|---|
| Mécanisme localizer | localizer + culture + `<html lang>` | ✅ | ✅ | — | — | ✅ (StatutBadge) | — | ✅ (culture) | ✅ |
| Public — formulaires localisés | Connexion, VerifierCode, SoumettreDemande (import/export), NouvelleReclamation, SimulationProforma | ✅ | ✅ | 🟡 | 🟡 | ✅ | 🟡 | 🟡 | 🟡 |
| Public — reste | Accueil, création/validation compte, inscription transporteur, mot de passe, clôture, erreurs (404/403/Error) | ✅ | ❌ | ❌ | — | — | ❌ | ❌ | ❌ |
| Navigation partagée | sidebar (`CatalogueNavigation`), header, fil d'Ariane, menus, notifications, EtatVide, Toast, PermissionDenied | ✅ | ❌ | — | — | — | ❌ | ❌ | ❌ |
| Espace Client | dashboard, mes-demandes, tickets, réclamations, enrôlement, avoir/exo, RDV, historique | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Espace Agent / Superviseur | files (manifeste/IES/contrôle/backoffice/facturation…), supervision, SLA, disponibilité | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Administration | comptes-agents, habilitations, matrice, SLA, motifs, pièces, audit, notifications, catégories | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| Société | dashboard société, collaborateurs, profil | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |

## Les 7 composants réellement localisés

| Composant | FR | EN | Note |
|---|---|---|---|
| `Comptes/Connexion.razor` | ✅ | ✅ | clés `Auth.*` |
| `Comptes/VerifierCode.razor` | ✅ | ✅ | 2FA |
| `BillingImport/SoumettreDemande.razor` | ✅ | ✅ | clés `BillingImport.*` |
| `BillingExport/SoumettreDemandeExport.razor` | ✅ | ✅ | |
| `CustomerComplaint/NouvelleReclamation.razor` | ✅ | ✅ | |
| `Tarification/SimulationProformaPage.razor` | ✅ | ✅ | simulation publique |
| `Shared/ChampMotDePasse.razor` | ✅ | ✅ | afficher/masquer mot de passe |

## Synthèse chiffrée

- Composants localisés : **7 / 101** (~7 %).
- `PageTitle` localisés : **10 / 81**.
- Placeholders localisés : **0 / 72** en dur.
- La sidebar (labels Application) : **❌ EN**.
- **Aucun composant partiellement migré** (état cohérent).

> Ce n'est pas 100 %. Chaque `❌` est un travail identifié, pas une supposition. Voir
> [`audit-traduction-final.md`](audit-traduction-final.md) pour le plan par lots.
