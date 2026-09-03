# Décisions externes — ce qui ne dépend plus du projet

> **Document** : `docs/82-external-decisions-only.md`
> **Établi le** : 2026-09-03
> **Ce document ne contient QUE ce qui échappe au projet.** Tout ce qui était corrigeable par le
> code, les tests, la configuration ou la documentation l'a été.
> **`DEFAULT_IF_NO_DECISION = NONE`** pour chaque ligne : aucune valeur n'est prise par défaut.

---

## D-01 — Rotation du mot de passe compromis

| | |
|:---|:---|
| **QUESTION** | Quel nouveau mot de passe pour `m.konate@cotedivoireterminal.com` ? |
| **OWNER** | Administrateur humain |
| **DECISION_REQUIRED** | Choisir le secret et exécuter la rotation |
| **WHY** | L'ancien a été publié dans l'historique Git d'un dépôt qui a été public. Il doit être considéré connu de tiers. Le compte porte les 54 permissions. |
| **BLOCKS** | Test d'intrusion, recette, production — **tout** |
| **TECHNICAL_READINESS** | **Prête.** Secret retiré du code, réécriture par l'amorçage supprimée, procédure à saisie masquée dans `docs/68 § 4` et `docs/70` |
| **RECOMMENDED_OPTIONS** | Aucune option : la rotation est la seule issue. Le traitement de l'historique Git est un choix distinct — `docs/71` |

**C'est la seule ligne de ce document qui est urgente.**

---

## D-02 — Environnement Staging / UAT

| | |
|:---|:---|
| **QUESTION** | Quand un environnement sera-t-il fourni ? |
| **OWNER** | DSI / Exploitation |
| **DECISION_REQUIRED** | Serveur, base SQL, répertoire de stockage persistant, TLS, nom de domaine, flux réseau |
| **WHY** | Aucune exigence d'environnement ne peut être vérifiée sans cible |
| **BLOCKS** | NFR-03, NFR-04, NFR-10, recette, test d'intrusion, déploiement |
| **TECHNICAL_READINESS** | **Prête.** `docs/54` à `docs/60`, `docs/65`, script `scripts/staging-precheck/` |
| **RECOMMENDED_OPTIONS** | `docs/58` compare IIS/Windows et conteneur — **aucune n'est retenue** |

---

## D-03 — Option de déploiement (PC-13 / NFR-09)

| | |
|:---|:---|
| **QUESTION** | IIS sur Windows Server, ou conteneur ? |
| **OWNER** | DSI |
| **DECISION_REQUIRED** | Architecture d'hébergement |
| **WHY** | L'analyse antivirus invoque `MpCmdRun.exe`, qui n'existe que sous Windows. En conteneur Linux, un adaptateur est à écrire ; `FailClosed=true` bloquerait sinon **tous** les dépôts |
| **BLOCKS** | D-02, NFR-09 (hébergement souverain) |
| **TECHNICAL_READINESS** | Option A fonctionne telle quelle. Option B exige un `Dockerfile` et un adaptateur antivirus |
| **RECOMMENDED_OPTIONS** | `docs/58` — le seul discriminant technique est l'antivirus |

---

## D-04 — Profil de charge (PC-01 / NFR-05)

| | |
|:---|:---|
| **QUESTION** | Quelle volumétrie cible : utilisateurs simultanés, dossiers par jour, dépôts par heure ? |
| **OWNER** | Métier + DSI |
| **DECISION_REQUIRED** | Profil de charge de référence |
| **WHY** | Sans volumétrie, un tir de charge mesure une charge arbitraire : le chiffre serait exact et sans valeur probante |
| **BLOCKS** | NFR-04 (temps de réponse < 3 s), NFR-05 |
| **TECHNICAL_READINESS** | **Prête.** Script k6 livré, métrologie instrumentée, journalisation de latence active |
| **RECOMMENDED_OPTIONS** | Aucune — inventer une volumétrie reviendrait à certifier contre une cible fictive |

---

## D-05 — Fenêtre et définition des heures ouvrées (NFR-03)

| | |
|:---|:---|
| **QUESTION** | Quelles heures ouvrées, quelle fenêtre d'observation, comment traiter les maintenances planifiées ? |
| **OWNER** | DSI |
| **DECISION_REQUIRED** | Paramètres de mesure de la disponibilité |
| **WHY** | NFR-03 vise ≥ 99 % **en heures ouvrées**. Une fenêtre mal choisie invaliderait la mesure |
| **BLOCKS** | NFR-03 |
| **TECHNICAL_READINESS** | **Prête.** `/ready` détaille chaque contrôle ; `docs/60 § A` |
| **RECOMMENDED_OPTIONS** | Aucune |

---

## D-06 — Durée du tir d'endurance (NFR-10)

| | |
|:---|:---|
| **QUESTION** | Quelle durée, quel créneau hors heures ouvrées, quel seuil de dérive acceptable ? |
| **OWNER** | DSI |
| **BLOCKS** | NFR-10 |
| **TECHNICAL_READINESS** | **Prête.** `docs/60 § C` |
| **RECOMMENDED_OPTIONS** | Aucune |

---

## D-07 — Fournisseur de notification (PC-16 / ENR-04, COM-01)

| | |
|:---|:---|
| **QUESTION** | Quel fournisseur e-mail, SMS et WhatsApp ? |
| **OWNER** | DSI + Finance |
| **DECISION_REQUIRED** | Choix, contrat, coût |
| **WHY** | Le CDC n'en nomme aucun. Sans canal, la réinitialisation autonome reste indisponible et ENR-04 ne peut être livrée |
| **BLOCKS** | COM-01, ENR-04, réinitialisation autonome hors développement |
| **TECHNICAL_READINESS** | **Prête.** `ICanalNotification` abstrait le canal ; l'implémentation journalisée ne contacte aucun tiers et ne divulgue rien. Un fournisseur se substitue **sans toucher au code métier** |
| **RECOMMENDED_OPTIONS** | Aucune — nommer un fournisseur serait décider à la place de la DSI |

---

## D-08 — Assistant conversationnel (PC-18 / IA-01 à IA-07)

| | |
|:---|:---|
| **QUESTION** | Quel fournisseur de modèle de langage, sous quelles conditions de confidentialité ? |
| **OWNER** | DSI |
| **WHY** | 5 exigences Must et 2 Should. Aucune implémentation n'est engagée |
| **BLOCKS** | IA-01 à IA-07 |
| **TECHNICAL_READINESS** | **Non commencée** — délibérément. `docs/45` |
| **RECOMMENDED_OPTIONS** | Aucune |

---

## D-09 — Cadence de la purge de rétention

| | |
|:---|:---|
| **QUESTION** | À quelle périodicité purger, et par quel mécanisme ? |
| **OWNER** | Métier + DSI |
| **WHY** | Le CDC n'en fixe aucune, et la suppression est **irréversible** |
| **BLOCKS** | G-02 — la purge ne s'exécute pas aujourd'hui |
| **TECHNICAL_READINESS** | **`TECHNICALLY_READY_FOR_SCHEDULING`** — `docs/73`. Réserves : pas de mode simulation, pas de verrou multi-instance, contrat d'audit à définir |
| **RECOMMENDED_OPTIONS** | Aucune |

---

## D-10 — Paramètres de réessai des notifications

| | |
|:---|:---|
| **QUESTION** | Combien de tentatives, à quel intervalle ? |
| **OWNER** | Métier / DSI |
| **CLÉS À RENSEIGNER** | **`Notifications:Reessai:NombreTentativesMax`** et **`Notifications:Reessai:DelaiEntreTentatives`** |
| **WHY** | ERR-02 exige une nouvelle tentative **sans en fixer la valeur** |
| **BLOCKS** | **Le démarrage de l'application** hors développement |
| **TECHNICAL_READINESS** | **Prête** — le code refuse de démarrer plutôt que de deviner. `docs/55 § 1` |
| **RECOMMENDED_OPTIONS** | Aucune. Les valeurs de développement (3 tentatives / 5 min) sont explicitement non contractuelles |

Ces deux clés ne figurent que dans la configuration de développement. Elles se fournissent par
variable d'environnement (`Notifications__Reessai__NombreTentativesMax`,
`Notifications__Reessai__DelaiEntreTentatives`) ou par un `appsettings` propre à l'environnement.

---

## D-11 — Répertoire de stockage documentaire

| | |
|:---|:---|
| **QUESTION** | Quel chemin persistant pour les pièces déposées ? |
| **OWNER** | Exploitation |
| **CLÉ À RENSEIGNER** | **`Stockage:RepertoireDocuments`** |
| **WHY** | Sans lui, les documents seraient perdus. Le code **refuse désormais de servir** hors développement plutôt que de se rabattre sur un répertoire temporaire |
| **BLOCKS** | Tout dépôt documentaire hors développement |
| **TECHNICAL_READINESS** | **Prête** — comportement fail-safe et testé |
| **RECOMMENDED_OPTIONS** | Aucune — deviner un chemin de production n'aurait aucun sens |

---

## D-15 — Sauvegardes et procédure de restauration testée (NFR-06)

| | |
|:---|:---|
| **QUESTION** | Quelle politique de sauvegarde des données **et des documents**, et quand la restauration sera-t-elle réellement testée ? |
| **OWNER** | Exploitation / DSI |
| **DECISION_REQUIRED** | Périodicité, rétention des sauvegardes, RPO / RTO, et exécution d'un test de restauration |
| **WHY** | NFR-06 est une exigence **Must** : « sauvegardes régulières (données et documents) et procédure de restauration **testée** ». Le mot testée est contractuel — une sauvegarde jamais restaurée n'est pas une sauvegarde démontrée |
| **BLOCKS** | NFR-06 |
| **TECHNICAL_READINESS** | **Sans objet côté applicatif** — la sauvegarde est une responsabilité d'infrastructure. La plateforme n'a aucun mécanisme à fournir ; elle a seulement deux jeux à protéger : la base SQL **et** le répertoire documentaire de D-11 |
| **RECOMMENDED_OPTIONS** | Aucune — RPO et RTO sont des engagements de service, pas des valeurs techniques. Éléments préparatoires : `docs/41` |

**Point d'attention** : sauvegarder la base sans sauvegarder le répertoire documentaire donnerait
une restauration incohérente — les références de documents survivraient, les fichiers non.

---

## D-12 — Seuils de limitation de débit

| | |
|:---|:---|
| **QUESTION** | Quelles limites sur connexion, mot de passe oublié, API, dépôt de fichiers ? |
| **OWNER** | DSI + Métier |
| **WHY** | Seul le verrouillage de compte (5 échecs / 15 min) existe. Une limite trop basse gêne l'usage légitime, trop haute ne protège de rien |
| **BLOCKS** | Rien de contractuel — le CDC n'exige pas de limitation de débit |
| **TECHNICAL_READINESS** | Non implémentée, en attente de seuils |
| **RECOMMENDED_OPTIONS** | Aucune valeur proposée |

---

## D-13 — Arbitrages de paramétrage aux valeurs de repli

Ces réglages **fonctionnent** avec une valeur de repli technique. Les laisser en l'état est possible,
mais ce serait alors une décision assumée, non un défaut subi.

| Paramètre | Repli actuel | Détenteur |
|:---|:---|:---|
| Expiration de session | 30 minutes | DSI |
| Validité d'une invitation | 7 jours | Métier |
| Extensions de fichier autorisées | `.pdf .jpg .jpeg .png .xlsx` | Métier |
| Taille maximale d'un dépôt | 10 Mo | Métier |
| Niveau et rétention des journaux | `Information` | DSI |
| Durée de vie du jeton de réinitialisation | 24 h (défaut Identity) | DSI |

**Aucune de ces valeurs ne figure dans le CDC.**

---

## D-14 — Sort de l'historique Git contenant le secret

| | |
|:---|:---|
| **QUESTION** | Conserver l'historique, le réécrire, ou repartir d'un dépôt neuf ? |
| **OWNER** | DSI |
| **WHY** | Aucune des trois options ne récupère un secret déjà diffusé — d'où la priorité de D-01 |
| **BLOCKS** | Rien de technique ; une exigence de conformité peut l'imposer |
| **TECHNICAL_READINESS** | Sans objet — geste Git |
| **RECOMMENDED_OPTIONS** | `docs/71` compare A / B / C. **A** (dépôt privé + rotation) offre le meilleur rapport risque / bénéfice si aucune obligation formelle n'impose davantage |

---

## Synthèse

| Réf. | Bloque | Urgence |
|:---|:---|:---|
| **D-01** | **Tout** | **Immédiate** |
| D-02 | Recette, pentest, déploiement | Haute |
| D-03 | D-02 | Haute |
| D-10, D-11 | Le démarrage hors développement | Haute — avec D-02 |
| D-04, D-05, D-06 | NFR-03 / 04 / 05 / 10 | Moyenne |
| D-07 | COM-01, ENR-04 | Moyenne |
| D-09 | G-02 | Moyenne |
| D-08 | IA-01 à IA-07 | Basse |
| **D-15** | **NFR-06 — sauvegardes** | Haute — avec D-02 |
| D-12, D-13, D-14 | Rien de contractuel | Basse |

**15 décisions. Aucune n'est prise par défaut. Aucune n'est inventée.**

Tout le reste — code, tests, configuration, durcissement, documentation — a été traité.
