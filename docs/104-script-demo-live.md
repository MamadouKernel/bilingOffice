# Script de démonstration live — Plateforme YABA

> Objectif : présenter la plateforme en direct sans accroc. Prépare **tout la veille** (répétition à
> blanc), notamment l'activation 2FA des comptes (voir §5, le piège n°1 d'une démo).

---

## 1. Pré-requis machine
- SQL Server local accessible (chaîne `ConnectionStrings:PlateformeDb` en user-secrets, déjà configurée).
- .NET 10 SDK.
- Une **application d'authentification** (Google Authenticator, Microsoft Authenticator, Authy) sur ton téléphone — indispensable pour la 2FA.
- Navigateur ; accepter le certificat de dev `https://localhost:7097` une fois.

## 2. Lancement (environnement Development + jeu de démo)
Le jeu de démo ne s'amorce **que** si la variable `CIT_TEST_USERS_PASSWORD` est renseignée (barrière volontaire).

1. Choisis un mot de passe de démo (respecte la politique : maj/min/chiffre/spécial, ex. `Demo@YABA2026`).
2. Renseigne-le pour le lancement. Le plus simple : dans `src/CIT.Plateforme.Web/Properties/launchSettings.json`, profil **`https`**, mettre ta valeur dans `CIT_TEST_USERS_PASSWORD` (⚠️ **ne pas committer** ce mot de passe — remets `""` après la démo).
3. Lance (PowerShell, à la racine du dépôt) :

```bash
dotnet run --project src/CIT.Plateforme.Web --launch-profile https
```

4. Le navigateur s'ouvre sur `https://localhost:7097`. Dans les logs, tu dois voir :
   `Amorçage démo client terminé : société active + 4 dossiers Import.`

> Option remise à zéro avant la démo : variables `CIT_ALLOW_DESTRUCTIVE_DEMO_SEED=true` (+ éventuellement `CIT_DEMO_DATABASE_NAME`) pour purger et recréer un jeu propre. À n'utiliser que sur la base de démo.

## 3. Comptes de démonstration
Mot de passe = la valeur de `CIT_TEST_USERS_PASSWORD` choisie ci-dessus.

| Rôle | Identifiant | Sert à montrer |
|---|---|---|
| **Client** | `client.demo@cit-demo.ci` (Awa Client Démo) | Espace client : mes demandes, soumission, assistant, notifications |
| **Agent / Super Admin CIT** | `m.konate@cotedivoireterminal.com` (Mamadou KONATE) | File de traitement, prise en charge, supervision, administration |

## 4. Jeu de données déjà en place (côté client)
Société **« Société Démo Import SARL »** (active, type *En compte*) + **4 dossiers Import** à des étapes différentes :
- `BIL-IMP-DEMO-0001` — **nouveau** (en attente de prise en charge)
- `BIL-IMP-DEMO-0002` — **en cours**
- `BIL-IMP-DEMO-0003` — **en attente d'un complément client** (idéal pour « qu'attend-on de moi ? »)
- `BIL-IMP-DEMO-0004` — **clôturé**

## 5. ⚠️ Piège n°1 — la 2FA (à préparer AVANT)
La double authentification (TOTP) est **obligatoire** : à la **première** connexion, chaque compte est redirigé vers l'activation (QR code rendu **localement**, aucune ressource externe).

**La veille**, connecte-toi une fois avec **chaque** compte et **active la 2FA** (scanne le QR avec ton app, note les codes de secours). Le jour J, la connexion ne demandera plus que le **code à 6 chiffres** courant — fluide. Ne découvre pas l'écran d'activation en direct.

## 6. Scénario conseillé (~12–15 min)

**A. Ouverture (1 min)** — page d'accueil publique, thème clair/sombre, bilingue FR/EN (bascule de langue).

**B. Espace Client (5 min)** — connexion `client.demo@cit-demo.ci` + code 2FA.
1. Tableau de bord client + **« Mes demandes »** : montrer les 4 dossiers aux différents stades.
2. Ouvrir le dossier **en attente de complément** (`…0003`) → « Mes actions requises ».
3. **Assistant YABA** : poser « qu'attend-on de moi ? » → il répond sur les vraies données.
4. **Soumettre une nouvelle demande** d'import (parcours de saisie, brouillon).
5. **Préférences de notification** (In-App + Email) et **notifications** reçues.
6. **Export** de la liste (xlsx/pdf).

**C. Espace Agent / Supervision (5 min)** — connexion `m.konate@cotedivoireterminal.com` + code 2FA.
1. **File de traitement** : prendre en charge le dossier `…0001`, le faire avancer.
2. **Supervision temps réel** : vue par file / par opérateur, indicateurs SLA (proche dépassement / dépassement), ancienneté, charge. *(Astuce : garde un 2e onglet client ouvert — une action côté agent rafraîchit la supervision en direct.)*
3. **Réaffectation / transfert** d'un ticket, priorité.

**D. Administration (2 min)** — gestion des **comptes & habilitations**, **référentiels** (files, priorités, SLA), **modèles de notification**.

**E. Sécurité (1 min, en mots)** — 2FA obligatoire, cloisonnement strict par société/rôle, en-têtes durcis (CSP), zéro ressource externe (tout auto-hébergé), rate limiting. Renvoyer à `docs/102` pour l'audit complet.

## 7. Check-list T-1h (répétition à blanc)
- [ ] `dotnet build` OK, app démarre, logs « Amorçage démo … terminé ».
- [ ] 2FA **déjà activée** sur les 2 comptes ; codes à portée de main.
- [ ] Connexion client + agent testée de bout en bout.
- [ ] Les 4 dossiers de démo visibles ; assistant répond.
- [ ] Certificat `localhost` accepté ; pas d'avertissement navigateur en direct.
- [ ] 2e onglet prêt pour la démo temps réel de supervision.
- [ ] Après la démo : remettre `CIT_TEST_USERS_PASSWORD` à `""` dans launchSettings (ne rien committer).

## 8. Replis si accroc
- **Seeding vide** → la variable `CIT_TEST_USERS_PASSWORD` n'était pas prise en compte (profil `https` la remet à `""`). Corrige la valeur dans le profil et relance.
- **Bloqué sur l'écran 2FA** → compte non pré-activé : active-le (QR local) ou bascule sur l'autre compte déjà prêt.
- **Rien ne se rafraîchit en supervision** → mono-nœud, normal ; c'est instantané en local (le scale-out multi-nœuds est documenté `docs/103`).
