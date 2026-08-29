# Note de décision — D6 : Numérotation unique des tickets et dossiers

**Feature** : `001-socle-tickets-workflows` | **Date** : 2026-08-29
**Origine** : `post-implementation-audit.md` §3.A — divergence constatée entre `research.md` (décision D6) et l'implémentation réelle (`ServiceNumerotation.cs`)
**Statut** : **DÉCIDÉ — Option B validée par le maître d'ouvrage le 2026-08-29 (revue de fermeture Feature 001).**

> Ce document a rempli son rôle de comparaison. La décision actée est reportée dans
> `research.md` (décision D6, section révisée) avec rationale, garanties, alternatives rejetées et
> absence d'impact CDC. **Aucune ligne de code n'a été modifiée** : l'implémentation
> (`ServiceNumerotation.cs`) correspondait déjà à l'Option B validée. Le contenu comparatif
> ci-dessous est conservé tel quel pour la traçabilité de la décision.

---

## Rappel du constat (audit)

**research.md, décision D6** (texte actuel, inchangé) :

> « service de numérotation dans le Domain s'appuyant sur des séquences SQL Server (SEQUENCE)
> **par (module, type)**, préfixe et format paramétrables ; attribution dans la même transaction
> que la création. »

**Implémentation réelle** (`src/CIT.Plateforme.Infrastructure/Persistence/ServiceNumerotation.cs`) :

- Le service vit en **Infrastructure**, pas en **Domain**.
- Il utilise **deux séquences SQL Server globales** : `seq_ticket_numero` et `seq_dossier_numero`
  — une seule séquence pour TOUS les tickets, une seule pour TOUS les dossiers, quel que soit le
  module ou le type de demande.
- Le numéro final (ex. `BIL-IMP-000042`) encode le module et le type dans un **préfixe textuel
  lisible** (3 lettres de chaque), mais le compteur numérique sous-jacent (`000042`) est partagé
  entre tous les modules/types.
- Ce choix est documenté explicitement dans le code (commentaire XML) comme un écart assumé, avec
  la justification suivante : les couples (module, type) ne sont pas fixés à l'avance dans le
  socle (ils sont eux-mêmes des données fournies par les modules appelants, paramétrables —
  Principe VI) ; créer une séquence SQL par couple supposerait donc de créer dynamiquement des
  objets SQL SEQUENCE à la volée, ce que SQL Server ne permet pas de façon transactionnellement
  simple.

**Ce que l'audit a vérifié réellement** (SQL Server réel, `NumerotationTests`) : l'unicité et la
non-réutilisation des numéros (SOC-01, SOC-09) sont garanties et démontrées avec l'implémentation
actuelle. Aucune collision, aucune réutilisation observée.

---

## Option A — Séquence par (module, type), conforme à `research.md` actuel

### Description

Une séquence SQL Server `SEQUENCE` distincte serait créée pour chaque couple (module, type de
demande) effectivement rencontré (ex. `seq_ticket_billingoffice_import`,
`seq_ticket_billingoffice_export`, `seq_ticket_customercomplaint_reclamation`, …). Chaque nouveau
couple observé nécessiterait la création dynamique d'une nouvelle séquence (via SQL dynamique,
DDL exécuté à l'exécution) ou un provisionnement préalable de toutes les séquences connues.

### Avantages

- Conforme littéralement au texte déjà validé de `research.md`.
- Le compteur numérique de chaque module/type repart réellement de 1 pour lui-même — un
  numéro `BIL-IMP-000001` correspondrait toujours au premier ticket **Import** jamais créé, pas
  au premier ticket toutes catégories confondues.
- Isolation totale entre modules : la charge de numérotation d'un module ne consomme jamais la
  plage de numéros d'un autre (utile si un jour un module génère un volume disproportionné).

### Inconvénients

- **Création dynamique de séquences SQL** : nécessite soit du DDL exécuté à l'exécution
  (`CREATE SEQUENCE IF NOT EXISTS ...` dynamique — contraire à la pratique standard EF Core/SQL
  Server de séquences déclarées statiquement dans le modèle), soit un provisionnement manuel
  préalable de toutes les combinaisons (module, type) connues — ce qui contredit le principe
  « paramétrable sans développement » si un nouveau module/type est ajouté après coup sans
  déploiement de schéma.
- Complexifie significativement `ServiceNumerotation` (recherche ou création de la séquence
  appropriée avant de l'interroger).
- Le DDL dynamique (CREATE SEQUENCE à l'exécution) pose des questions de permissions SQL
  (le compte applicatif devrait avoir le droit `ALTER SCHEMA` ou équivalent, pas seulement
  lecture/écriture de données) — écart avec le principe de moindre privilège (SEC-02/SEC-11).
- Aucun bénéfice fonctionnel démontré : SOC-01/SOC-09 (unicité, non-réutilisation) n'exigent pas
  que le compteur reparte à 1 par module — seule l'exigence de lisibilité (déjà satisfaite par le
  préfixe) est concernée.

### Impact migration

- Nécessite de remplacer les deux séquences globales par un mécanisme de séquences multiples, avec
  une migration de données pour les numéros déjà attribués (mapping des anciens numéros vers un
  nouveau compteur par module/type, ou conservation des anciens numéros en l'état avec démarrage
  des nouvelles séquences à une valeur de reprise cohérente).
- Risque de collision si la migration de données n'identifie pas correctement le point de reprise
  de chaque nouvelle séquence par rapport aux numéros déjà émis.

### Impact exploitation

- Prolifération d'objets SQL SEQUENCE au fil du temps (un par couple module/type observé) —
  nécessite un suivi/nettoyage si des modules/types sont retirés.
- Le provisionnement dynamique de schéma en production (CREATE SEQUENCE à l'exécution) est
  généralement déconseillé par les équipes DBA (DDL hors fenêtre de changement contrôlée).

### Impact concurrence

- Équivalent à l'option B : chaque séquence SQL Server est individuellement transactionnellement
  sûre (`NEXT VALUE FOR`), sans contention de table. Le fractionnement en plusieurs séquences
  réduit légèrement la contention en cas de très fort volume simultané sur un seul module (au prix
  de la complexité ci-dessus) — bénéfice non démontré comme nécessaire (PC-01, volumétrie, non
  tranché).

### Impact maintenabilité

- Code de résolution de séquence plus complexe (recherche dynamique du nom de séquence,
  création à la demande, gestion des cas concurrents de première création).
- Risque accru de bug si deux instances de l'application tentent de créer la même séquence pour la
  première fois simultanément (nécessite un verrou applicatif ou une gestion d'exception
  "séquence déjà existante").

### Impact volumétrie future

- Aucun avantage démontré à haute volumétrie : les séquences SQL Server supportent des débits très
  élevés sans contention, qu'elles soient uniques ou multiples. Le fractionnement n'apporte de
  bénéfice que si un module isolé devait un jour approcher la limite `bigint` d'une séquence
  (extrêmement improbable en pratique — des milliards de tickets).

---

## Option B — Séquences globales ticket/dossier avec préfixes, conforme au code actuel

### Description

Conserver l'implémentation actuelle : deux séquences SQL Server globales (`seq_ticket_numero`,
`seq_dossier_numero`), le module/type étant encodé uniquement dans le préfixe textuel du numéro
affiché, pas dans le compteur numérique lui-même. `research.md` (D6) serait mis à jour pour
refléter fidèlement ce choix (voir « Recommandation » ci-dessous).

### Avantages

- Déjà implémenté, déjà testé (SQL Server réel), déjà démontré conforme à SOC-01/SOC-09.
- Simplicité maximale : deux séquences statiques déclarées une fois dans le modèle EF Core, aucune
  création dynamique de schéma à l'exécution (Principe IV — simplicité).
- Aucun problème de permission SQL supplémentaire (le compte applicatif n'a besoin que de
  `SELECT`/lecture de séquence, jamais de DDL).
- Aucun nouveau module/type ne nécessite de migration de schéma pour être numéroté — cohérent avec
  le Principe VI (paramétrable sans développement) : un module/type totalement nouveau, jamais vu
  auparavant, obtient immédiatement un numéro valide sans aucune action technique.

### Inconvénients

- Le compteur numérique n'est pas repris à zéro par module/type : `BIL-IMP-000042` ne signifie pas
  « 42ᵉ ticket Import », mais « ticket dont le 42ᵉ numéro de la séquence globale ticket a été
  attribué ». Si cette lecture (« le Nᵉ ticket de ce module ») est une attente métier implicite du
  CDC ou des utilisateurs, l'option B ne la satisfait pas.
- La séquence globale progresse plus vite (tous modules confondus) — sans conséquence
  fonctionnelle connue (les séquences SQL Server gèrent des plages `bigint`, soit ~9,2×10¹⁸
  valeurs), mais purement esthétique/perceptuelle pour un utilisateur regardant les numéros.
- Écart entre le texte actuel de `research.md` et le code tant que la documentation n'est pas mise
  à jour (dette documentaire, déjà signalée dans l'audit comme DT-4).

### Impact migration

- Aucun — c'est l'état actuel, déjà en place dans les 8 migrations existantes.

### Impact exploitation

- Aucun changement : deux objets SQL SEQUENCE fixes, aucune prolifération, aucun DDL à
  l'exécution.

### Impact concurrence

- Déjà démontré transactionnellement sûr (audit §5, `NumerotationTests` sur SQL Server réel) :
  `NEXT VALUE FOR` est atomique, sans verrou de table, quel que soit le volume concurrent.

### Impact maintenabilité

- Code déjà minimal (`ServiceNumerotation.cs`, ~50 lignes) — aucune logique de résolution ou de
  création dynamique de séquence à maintenir.

### Impact volumétrie future

- Aucune limite pratique atteignable (plage `bigint`) même à très forte volumétrie (PC-01 non
  tranché, mais aucun scénario réaliste ne sature une séquence `bigint`).
- Seul risque théorique : si un jour l'équipe métier veut garantir qu'un numéro par module reparte
  à une valeur basse pour des raisons d'affichage/marketing (ex. les 100 premiers dossiers d'un
  nouveau module portent un numéro à 3 chiffres) — l'option B ne le permet pas nativement.

---

## Tableau de synthèse

| Critère | Option A (séquence par module/type) | Option B (séquences globales, code actuel) |
|---|---|---|
| Conformité au texte actuel de `research.md` | ✅ Conforme tel quel | ❌ Nécessite une mise à jour de `research.md` |
| Complexité d'implémentation | Élevée (création dynamique ou provisionnement) | Faible (déjà fait) |
| Permissions SQL requises | DDL (`CREATE SEQUENCE`) en plus de DML | DML uniquement |
| Paramétrable sans développement (Principe VI) | ⚠️ Un nouveau module/type peut exiger une action technique | ✅ Aucune action requise |
| Risque de migration | Élevé (redistribution des numéros déjà émis) | Nul (déjà en production de test) |
| Preuve de concurrence réelle | Non testé (hypothétique) | ✅ Testé sur SQL Server réel |
| Lecture « Nᵉ ticket de ce module » | ✅ Possible | ❌ Non garantie |
| Risque d'exploitation (DBA) | DDL en production à surveiller | Aucun DDL après déploiement initial |

---

## Recommandation technique (à valider par l'équipe / le maître d'ouvrage — non appliquée)

**L'auditeur recommande l'Option B**, pour les raisons suivantes :

1. Elle est déjà implémentée, déjà testée sur SQL Server réel, et fonctionnellement conforme à
   SOC-01/SOC-09 (aucune régression à corriger).
2. Elle respecte plus strictement le Principe VI (paramétrable sans développement : un nouveau
   module/type n'exige aucune action technique, contrairement à l'Option A).
3. Le seul avantage réel de l'Option A — un compteur qui « reparte à 1 » par module — n'est
   **explicitement exigé par aucune exigence CDC** (SOC-01/SOC-09 exigent l'unicité et la
   non-réutilisation, pas une numérotation contiguë par module). C'est une préférence esthétique
   possible, pas une exigence démontrée.
4. L'Option A introduit un risque d'exploitation (DDL dynamique ou provisionnement manuel) sans
   bénéfice fonctionnel prouvé.

**Cette recommandation n'est qu'un avis technique.** Si le métier confirme que la lecture « Nᵉ
ticket de ce module » est une attente réelle des utilisateurs ou des équipes CIT (à vérifier —
aucune exigence CDC explicite ne l'a été trouvée lors de l'analyse), l'Option A redevient
justifiée malgré son coût, et devra alors être implémentée avec une migration de données
soigneusement conçue pour ne pas perturber les numéros déjà émis.

## Décision

**TRANCHÉE — Option B validée.** Conformément à la décision humaine communiquée lors de la revue
de fermeture de la Feature 001 :

- **L'Option B (séquences globales ticket/dossier avec préfixes lisibles) est retenue.**
- Le code n'a nécessité **aucune modification** : `ServiceNumerotation.cs` correspondait déjà à
  cette option.
- `research.md` (décision D6) a été **mis à jour** pour refléter fidèlement ce choix — rationale,
  garanties (unicité, non-réutilisation, lisibilité, absence de limite de volumétrie pratique),
  alternatives rejetées (dont l'Option A, désormais explicitement écartée) et absence d'impact sur
  les exigences CDC (SOC-01, SOC-09 restent intégralement satisfaits et démontrés).
- Ce document reste la trace du raisonnement comparatif ayant mené à la décision.

| Champ | Valeur |
|---|---|
| Décision actée | ☑ Oui |
| Option retenue | **Option B — séquences globales avec préfixes lisibles** |
| Décideur | Maître d'ouvrage |
| Date | 2026-08-29 |
