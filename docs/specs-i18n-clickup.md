# Specs i18n FR/EN — backlog ClickUp

Backlog prêt à être créé dans ClickUp. **Épopée** = chantier i18n ; **Tâches** = LOTs ;
**Sous-tâches** = surfaces / étapes. Chaque tâche porte la **gate** en critères d'acceptation.

> Convention statut : `À faire` / `En cours` / `Terminé`.
> Règle transverse (toutes tâches) : architecture i18n inchangée, routes/permissions indépendantes
> de la culture, business codes invariants, **AUCUN COMMIT**, ne jamais déclarer 100 % sans preuve
> navigateur FR **et** EN.

---

## ÉPOPÉE — i18n FR/EN à 100 % par lots

**Objectif :** passer de ~7 % de couverture réelle à 100 %, lot par lot, sans régression
métier/sécurité. Baseline tests : 1644/1644.

**Definition of Done globale (gate, appliquée à CHAQUE lot) :**
- [ ] Traduit (toutes surfaces du lot : pages, PageTitle, placeholders, aria-labels, validations, enums/statuts, modales, toasts, erreurs)
- [ ] Compilé (0 erreur / 0 avertissement)
- [ ] Testé (suite verte, guards du lot inclus)
- [ ] Parité FR/EN (clés FR == clés EN)
- [ ] Mutation vérifiée (casser une traduction EN fait tomber un test)
- [ ] Recette navigateur FR
- [ ] Recette navigateur EN
- [ ] 0 texte résiduel injustifié
- [ ] Aucun commit

---

## TÂCHE 1 — LOT 1 : Shared / Navigation  ✅ TERMINÉ

**Description :** coquille partagée rendue sur toutes les pages.
**Sous-tâches :**
- [x] Menu latéral (`NavMenu`, 65 libellés) — clé = libellé FR canonique, routes FR==EN
- [x] `HeaderBar` (marque, thème, cloche, menu utilisateur, libellés d'espace)
- [x] `PersonaSwitcher` (libellés d'espace)
- [x] `ModaleConfirmation` (défauts localisés)
- [x] `ToastNotification`, `EtatVide`, `IndicateurChargement`, `FilAriane`
- [x] `PermissionDenied` (403)
- [x] `ReconnectModal` (superposition framework)
- [x] Guards : `TraductionParityTests`, `NavigationI18nTests`, `TextesEnDurLot1Tests`
- [x] Recette FR + EN, 6 profils (Admin, Superviseur, Agent gate, Agent sans droits, Client, Transporteur)

**Gate : 9/9 ✅** — Web 426/426. Détail dans `docs/progression-i18n.md`.

---

## TÂCHE 2 — LOT 2 : Public / Auth  ⏳ À FAIRE

**Description :** pages publiques + flux d'authentification pré-connexion.
**Surfaces (sous-tâches) :**
- [ ] `Home.razor` (page d'accueil publique)
- [ ] `Error.razor`
- [ ] `NotFound.razor`
- [ ] `Comptes/Connexion.razor`
- [ ] `Comptes/VerifierCode.razor` (2FA)
- [ ] `Comptes/MotDePasseOublie.razor`
- [ ] `Comptes/ReinitialiserMotDePasse.razor`
- [ ] `Comptes/SoumettreDemande.razor` (création compte société)
- [ ] `Comptes/InscriptionTransporteur.razor`
- [ ] `Comptes/AccepterInvitation.razor`
- [ ] `Comptes/AccesRefuse.razor`
- [ ] `Comptes/CompteCloture.razor`
- [ ] Guards du lot : PageTitle, placeholders, aria-labels, validations + mutation
- [ ] Recette FR + EN (comptes non authentifiés et flux 2FA)

**Critères d'acceptation :** gate globale ci-dessus.

---

## TÂCHE 3 — LOT 3 : Client  ⏳ À FAIRE

**Description :** espace Client + Société + tickets côté demandeur.
**Surfaces principales (sous-tâches) :**
- [ ] `Pages/Client/*` (dashboard, demandes)
- [ ] `Pages/Societe/*` (dashboard société, collaborateurs, profil)
- [ ] `Pages/AvoirExoneration/*` (demandeur)
- [ ] `Pages/BillingImport/*` + `BillingExport/*` (soumission)
- [ ] `Pages/CustomerComplaint/*` (réclamations demandeur)
- [ ] `Pages/Enrolement/*` (demandeur)
- [ ] `Pages/Tickets/*` (fiche ticket côté client)
- [ ] Composants partagés « feature » : `ChronologieTicket`, `ZoneActionsRequises`
- [ ] `Comptes/MonProfil`, `ProfilSociete`, `ActiverDoubleAuthentification`
- [ ] Guards du lot + mutation
- [ ] Recette FR + EN

**Critères d'acceptation :** gate globale ci-dessus.

---

## TÂCHE 4 — LOT 4 : Agent / Superviseur  ⏳ À FAIRE

**Description :** écrans opérateurs et supervision.
**Surfaces principales (sous-tâches) :**
- [ ] `Pages/Agent/*` (dashboard opérateur)
- [ ] `Pages/Guichet/*` (recherche, agenda, rendez-vous)
- [ ] `Pages/BillingImport/*` + `BillingExport/*` (instruction : manifeste, contrôle doc, IES, facturation, caisse, comptabilité)
- [ ] `Pages/CustomerComplaint/*` (file réclamations)
- [ ] `Pages/AvoirExoneration/*` (file avoirs)
- [ ] `Pages/Supervision/*` (vue supervision temps réel)
- [ ] Composant partagé « feature » : `BandeauDisponibiliteAgent`
- [ ] Guards du lot + mutation
- [ ] Recette FR + EN

**Critères d'acceptation :** gate globale ci-dessus.

---

## TÂCHE 5 — LOT 5 : Administration  ⏳ À FAIRE

**Description :** administration & sécurité CIT.
**Surfaces principales (sous-tâches) :**
- [ ] `Pages/Administration/*` (comptes agents, matrice transitions, paramètres SLA, règles escalade, stratégie files, motifs transfert, pièces exigées, périmètres supervision, compétences, disponibilité, notifications, audit)
- [ ] `Pages/Tarification/*` (grilles tarifaires)
- [ ] `Comptes/ValiderDemandes`, `ModifierTypeClient`
- [ ] Guards du lot + mutation
- [ ] Recette FR + EN

**Critères d'acceptation :** gate globale ci-dessus.

---

## TÂCHE 6 — LOT 6 : Reste  ⏳ À FAIRE

**Description :** surfaces restantes non couvertes par 1–5 + revue finale.
**Sous-tâches :**
- [ ] `Pages/Dossiers/*`, `Pages/Notifications/*`, `Pages/Assistant/*`
- [ ] Toute page/composant résiduel détecté par un scan final
- [ ] Guard global anti-texte-en-dur (scan large, allowlist minimale)
- [ ] Recette finale FR + EN sur tous les profils
- [ ] Doc de clôture `audit-i18n-final.md` + `recette-i18n-finale.md`

**Critères d'acceptation :** gate globale + **GLOBAL 100 % uniquement après ce lot**.

---

## TÂCHE 7 — Transverse : Guards & Documentation  ⏳ EN CONTINU

- [x] `progression-i18n.md` (checkpoints par lot)
- [x] `matrice-i18n-fr-en` (déjà initiée)
- [ ] `remediation-i18n-lot2..6.md`
- [ ] `recette-i18n-finale.md`
- [ ] `audit-i18n-final.md`
