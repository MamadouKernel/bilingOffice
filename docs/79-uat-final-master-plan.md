# Plan de recette — version finale

> **Document** : `docs/79-uat-final-master-plan.md`
> **Établi le** : 2026-09-03
> **Complète** `docs/36` (plan initial), `docs/37` (traçabilité), `docs/61` (données),
> `docs/62` (accès), `docs/64` (anomalies).
> **Statut** : `NOT EXECUTED` — aucun environnement.

---

## 1. Conditions d'ouverture

| # | Condition | Référence |
|---:|:---|:---|
| 1 | Environnement fourni et `precheck.ps1` sans échec | `docs/65`, `scripts/staging-precheck/` |
| 2 | Migrations appliquées, `__EFMigrationsHistory` renseignée | `docs/57` |
| 3 | **Rotation du mot de passe effectuée** | `docs/70` |
| 4 | Jeu de données synthétique constitué | `docs/61` |
| 5 | Comptes et habilitations attribués | `docs/62` |
| 6 | Tests de fumée bloquants au vert | `docs/59` |

**La condition 3 n'est pas négociable** : ouvrir une recette sur un compte dont le secret est publié
reviendrait à tester dans un environnement dont on sait l'accès compromis.

---

## 2. Ce qui ne sera pas testable, et doit être annoncé avant

| Domaine | Exigences | Motif |
|:---|:---|:---|
| Canal e-mail | COM-01 et dépendantes | Aucun fournisseur — D-07 |
| SMS / WhatsApp | ENR-04 | Aucun fournisseur — D-07 |
| Assistant conversationnel | IA-01 à IA-07 | Non implémenté — D-08 |
| Réinitialisation autonome | CPT-03 (parcours complet) | Sans canal, non distribuable |
| Purge de rétention | SEC-06 (déclenchement) | Cadence non décidée — D-09 |

**Annoncer ces limites à l'ouverture**, pas au moment où un testeur cherchera le bouton.

---

## 3. Parcours nominaux

| # | Scénario | Profil | Exigences |
|---:|:---|:---|:---|
| N-01 | Inscription société, validation, premier accès | Client + Admin fonctionnel | CPT-01, CPT-02 |
| N-02 | Dépôt d'un dossier Import, jusqu'à récupération des documents | Client normal | Chaîne Billing Import |
| N-03 | Dossier Import client en compte — sans étape de paiement | Client en compte | Variante |
| N-04 | Dossier Import deposit — lettrage comptable | Client deposit | Variante |
| N-05 | Dossier Import IES | Client IES | Branche IES |
| N-06 | Dossier Export complet | Client + Back Office | Chaîne Billing Export |
| N-07 | Simulation de proforma, contrôlée contre l'Annexe A | Client | Tarification |
| N-08 | Réclamation : soumission, instruction, clôture, évaluation | Client + Support | Chaîne réclamation |
| N-09 | Enrôlement transporteur, jusqu'au badge | Transporteur + Agent Gate | Chaîne ENR |
| N-10 | Demande d'avoir / exonération, instruction et décision | Client + Support + Admin | Chaîne AVE |
| N-11 | Recherche guichet et reprise sans ressaisie | Agent habilité | CON-01, CON-02 |
| N-12 | Rendez-vous : création, présence, absence | Agent | CON-03, CON-04 |
| N-13 | Brouillon : saisie, abandon, reprise | Client | Brouillons |
| N-14 | Activation 2FA puis connexion avec second facteur | Tout profil | CPT-07 |
| N-15 | Gestion des collaborateurs d'une société | Admin société | ENT-02 |
| N-16 | Export CSV d'une liste | Agent | Exports |
| N-17 | Consultation du journal d'audit | Admin technique | SEC-05 |
| N-18 | Consultation via l'API authentifiée | Client | API |

---

## 4. Parcours négatifs — obligatoires

Une recette qui ne vérifie que ce qui doit marcher ne prouve rien sur ce qui doit être refusé.

| # | Tentative | Attendu |
|---:|:---|:---|
| G-01 | Compte sans habilitation ouvre une file | **Refus** |
| G-02 | Client de société A consulte un dossier de société B | **Refus** |
| G-03 | Collaborateur au périmètre restreint consulte la demande d'un autre | **Refus** |
| G-04 | Agent tente une action hors de sa file | **Refus** |
| G-05 | Administrateur fonctionnel ouvre le journal d'audit | **Refus** |
| G-06 | Administrateur technique administre les files | **Refus** |
| G-07 | Transporteur accède à un ticket | **Refus** |
| G-08 | Compte désactivé se connecte | **Refus** — SEC-11 |
| G-09 | Client d'une société non validée accède au portail | **Refus** — CPT-01 |
| G-10 | Appel API sans authentification | **401** |
| G-11 | Appel API authentifié sans droit | **403** |
| G-12 | Dépôt d'une extension interdite | **Refus** |
| G-13 | Dépôt au-delà de la taille maximale | **Refus** |
| G-14 | Dépôt d'un fichier EICAR | **Refus**, et non téléchargeable |
| G-15 | Administrateur fonctionnel consulte la notification d'un tiers | **Refus** — aucune permission ne l'autorise |
| G-16 | Modification d'une entrée du journal d'audit | **Impossible** — append-only |
| G-17 | Champ restreint dans un export | `[RESTREINT]` |
| G-18 | Mot de passe non conforme à la politique | **Refus** |
| G-19 | Formulaire d'oubli sur une adresse inexistante | Réponse **identique** à une adresse existante |
| G-20 | Description OpenAPI en Staging | **404** |

**G-15, G-16 et G-19 sont les plus faciles à oublier**, et les plus révélateurs.

---

## 5. Contrôles transverses

| Domaine | Contrôle |
|:---|:---|
| Audit | Chaque action sensible produit une entrée nominative et horodatée |
| Rétention | Un ticket sous gel légal n'est jamais purgé |
| Notifications | Le destinataire seul voit ses notifications |
| Bilinguisme | Bascule français / anglais sur les écrans principaux — NFR-07 |
| Thème sombre | Lisibilité et contraste |
| Accessibilité | Navigation clavier, libellés, focus visible |
| Mot de passe | Bascule afficher / masquer, re-masquage à la soumission |
| Maintenance | Activation, `Retry-After`, **et retour au nominal** |

---

## 6. Traçabilité

Chaque scénario doit citer les exigences CDC qu'il couvre, et chaque exigence **Must** testable doit
être couverte par au moins un scénario. La matrice de référence est `docs/37`.

**Le score CDC ne progresse que sur preuve d'exécution.** Il reste à **174/188** tant que la recette
n'a pas produit ses résultats.

---

## 7. Consignation

Par scénario : horodatage, **commit déployé**, compte utilisé et ses permissions, résultat, et pour
tout écart une fiche conforme à `docs/64 § 3`.

**Statut : `NOT EXECUTED`.**
