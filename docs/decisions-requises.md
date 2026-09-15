# Décisions requises pour atteindre 100/100

**Date** : 7 septembre 2026. Score démontré : **95/100**. Ces sujets **ne sont pas décidés** ici :
ils appellent une réponse du métier / de la DSI. Chaque fiche pose la question de façon qu'on puisse
répondre A/B/C ou OUI/NON.

---

## AM-01 — Durée de séjour facturée : borne incluse ou exclue ?

| | |
|---|---|
| **Règle concernée** | Calcul de la durée de séjour (facturation / branchement frigo). |
| **Écran / parcours** | Simulateur proforma · Historique des simulations · Facturation. |
| **Comportement actuel** | Affichages divergents pour un même intervalle (06/09 → 20/09) : simulateur « 14 j », historique « 15 j », frigo facturé « 13 j ». |
| **Pourquoi décider** | Le CDC ne fixe pas la convention de bornes ; le code ne doit pas la deviner. |
| **Options** | **A** : borne exclue (14 j). **B** : borne incluse (15 j). |
| **Impact A/B** | Montant facturé et affiché ; cohérence des 3 écrans. |
| **Sécurité** | Nul. **UX** : lisibilité du montant. **Dev** : faible (unifier le calcul + tests). |
| **Recommandation technique** | Une seule règle appliquée partout, alignée sur la convention de facturation réelle. |
| **Question** | **La durée de séjour facturée compte-t-elle la borne de fin ? OPTION A (exclue, 14 j) / OPTION B (incluse, 15 j).** |

---

## AM-02 — Périmètre du « collaborateur limité »

| | |
|---|---|
| **Règle concernée** | Périmètre fonctionnel d'un collaborateur à droits réduits. |
| **Écran / parcours** | Espace client (`collab.limite`). |
| **Comportement actuel** | `collab.limite` (permission `BillingImportSoumettre` seule) atterrit sur le **même** dashboard que `client.normal` (import+export+réclamation). L'écart de périmètre n'est pas formalisé côté UX. |
| **Pourquoi décider** | Le niveau de restriction attendu est un choix métier (le cloisonnement serveur existe déjà via les permissions ; c'est l'UX/le périmètre qui est à définir). |
| **Options** | **A** : restreindre visiblement l'espace (masquer/désactiver ce qui est hors permission). **B** : statu quo (permissions seules). |
| **Impact A/B** | Lisibilité du périmètre. **Sécurité** : inchangée (permissions serveur). **Dev** : moyen (option A). |
| **Recommandation technique** | Définir le périmètre réduit attendu, puis test de cloisonnement dédié. |
| **Question** | **Un collaborateur limité doit-il avoir un espace visiblement restreint ? OPTION A (restreindre) / OPTION B (statu quo).** |

---

## AM-03 — Confirmation des transitions de workflow

| | |
|---|---|
| **Règle concernée** | Confirmation avant une transition de workflow. |
| **Écran / parcours** | « Valider Booking » (et transitions analogues). |
| **Comportement actuel** | La transition avance **sans confirmation**, alors qu'une attribution d'habilitation, elle, en exige une (modale de confirmation câblée). |
| **Pourquoi décider** | Où placer la frontière « action nécessitant confirmation » est un arbitrage métier (friction vs sécurité du geste). |
| **Options** | **A** : confirmer **toute** transition de workflow. **B** : ne confirmer que les transitions **destructives/irréversibles**. **C** : statu quo. |
| **Impact** | **UX** : friction. **Sécurité** : réduit les gestes involontaires. **Dev** : faible (la modale existe). |
| **Recommandation technique** | Option B (confirmer le destructif/irréversible), harmonisée sur tous les workflows. |
| **Question** | **Quelles transitions doivent être confirmées ? OPTION A (toutes) / OPTION B (destructives) / OPTION C (aucune).** |

---

## D-04 — Session / antiforgery / DataProtection (infrastructure)

| | |
|---|---|
| **Sujets** | Antiforgery (jeton périmé), expiration de session, persistance des clés DataProtection, mono/multi-instance, redémarrage, rotation de clés. |
| **Comportement actuel** | En Production, un jeton antiforgery périmé mène à l'écran de marque `/Error`. Les clés DataProtection ne sont pas explicitement persistées sur un stockage partagé. |
| **Risque** | **Mono-instance** : au redémarrage, les clés en mémoire changent → cookies invalidés → déconnexions. **Multi-instance** : un cookie émis par une instance est rejeté par une autre (clés non partagées) → déconnexions aléatoires. |

**Options :**

| Option | Description | Avantages | Risques / prérequis | Multi-instance | Effort |
|---|---|---|---|---|---|
| **A** | DataProtection **local** (défaut) | Zéro configuration | Clés volatiles ; déconnexions au redémarrage / entre instances | ❌ inadapté | nul |
| **B** | Clés **persistées sur stockage partagé** (`PersistKeysTo` + `SetApplicationName`) | Sessions stables mono ET multi-instance | Prérequis : un stockage partagé (chemin réseau / base / coffre) ; protection au repos des clés | ✅ adapté | faible-moyen |
| **C** | Mécanisme **adapté à l'infra réelle** (Redis, Key Vault, DPAPI-NG…) | Optimal selon la cible | Dépend de l'infrastructure de production réelle | ✅ | moyen |

**+ message spécifique** « session expirée » (au lieu de l'écran d'erreur générique) sur jeton
antiforgery périmé — petit correctif applicatif, à faire **avec** la décision infra.

| **Recommandation technique** | **Option B** si mono/quelques instances avec un stockage partagé simple ; **Option C** si l'infra de production a déjà Redis/Key Vault. Ne modifier le pipeline de sécurité qu'**avec** cette décision. |
| **Question** | **Quelle persistance des clés DataProtection en Production ? OPTION A (local) / OPTION B (stockage partagé) / OPTION C (adapté infra). Et : ajouter un message « session expirée » ? OUI / NON.** |

---

## Matrice — 95/100 → 100/100

| Point | Score actuel | Condition pour le gagner | Dépendance | Statut |
|---|---|---|---|---|
| Accessibilité | 9/10 | Passe **lecteur d'écran** (NVDA/JAWS) réussie **+** décision **1.4.11** (bordures de champ ≥3:1) | Audit externe (SR indisponible ici) + décision de token | BLOQUÉ (audit + décision) |
| Gestion des erreurs | 8/10 | Décision **D-04** (persistance clés) + message « session expirée » ; le 2ᵉ −1 (cumul CSV) est un **faux positif** documenté | Décision INFRA | BLOQUÉ (infra) |
| Parcours utilisateur | 9/10 | Décisions **AM-01 + AM-02 + AM-03** rendues et appliquées | Décision MÉTIER | BLOQUÉ (métier) |
| Qualité frontend | 4/5 | Retrait des `!important` — **régresserait** le thème sombre (démontré) | Risque accepté | NON GAGNABLE sans régression |
| Design visuel | 15/15 | — | — | ACQUIS |

**Répartition des 5 points manquants :** audit formel **1** · infra **1** · métier **1** · faux
positif/risque accepté **2** (CSV + `!important`).
