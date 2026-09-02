# Assistant conversationnel — cadrage préparatoire (IA-01 à IA-07)

> **Document** : `docs/45-assistant-conversationnel-readiness.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Statut** : cadrage uniquement — **aucune implémentation**, `PC-18` reste OPEN.

---

## 1. Avertissement préalable

Les documents de pilotage antérieurs décrivaient `IA-01` à `IA-07` comme sept cas d'usage
distincts — prédiction d'affluence, détection d'anomalies documentaires, routage intelligent,
synthèse de réclamations, aide à la décision, contentieux prédictif.

**Aucune de ces formulations ne figure dans le CDC.** Le présent document repart exclusivement du
texte contractuel (`docs/cdc_text.txt`, lignes 794 à 817).

Les sept exigences portent toutes sur **un seul et même assistant conversationnel**, et comptent
**cinq Must et deux Should** — non « sept Could ».

---

## 2. Les sept exigences, texte contractuel

| Réf. | Prio. | Exigence |
|:---|:---|:---|
| `IA-01` | **Must** | L'assistant répond aux questions simples des clients (fonctionnement du portail, pièces à fournir, état d'avancement, prochaines étapes) **en langage naturel, en français**. |
| `IA-02` | **Must** | L'assistant **oriente** le client dans ses démarches (choix du parcours, du module, de la catégorie de réclamation) et l'aide à compléter ses dossiers. |
| `IA-03` | **Must** | L'assistant **n'escalade que lorsque c'est réellement nécessaire** ; le passage au traitement humain se fait **sans perte de contexte** (le fil est conservé dans le ticket). |
| `IA-04` | Should | L'assistant s'appuie sur une **base de connaissances administrable** (FAQ, procédures) **sans développement**, et **cite la source** de ses réponses lorsque pertinent. |
| `IA-05` | Should | L'assistant est accessible **depuis le portail** et, si possible, depuis les canaux de messagerie configurés. |
| `IA-06` | **Must** | L'assistant **identifie les situations nécessitant une intervention humaine** et permet le transfert vers un opérateur habilité **sans perte de contexte**. |
| `IA-07` | **Must** | Le client peut **demander une intervention humaine** ; l'assistant réduit les sollicitations inutiles **sans constituer une barrière** entre CIT et le client. |

---

## 3. Ce que ces exigences imposent réellement

Lues ensemble, elles contraignent l'architecture bien plus qu'il n'y paraît.

| Contrainte | Exigence(s) | Conséquence technique |
|:---|:---|:---|
| **Français** | IA-01 | Le modèle doit être compétent en français, pas seulement traduire. |
| **Base de connaissances administrable sans développement** | IA-04 | Le savoir ne peut pas être « dans le modèle » : il doit venir d'un corpus modifiable par un administrateur. |
| **Citation des sources** | IA-04 | Une réponse doit être **traçable** à un document. Cela exclut une génération libre. |
| **Escalade sans perte de contexte** | IA-03, IA-06 | Le fil de conversation doit être **repris dans le ticket** — donc persisté et rattachable. |
| **Ne pas faire barrière** | IA-07 | L'accès humain doit rester atteignable **à tout moment**, sans parcours dissuasif. |
| **Escalade seulement si nécessaire** | IA-03 | Suppose une mesure de confiance, donc une gestion explicite de l'incertitude. |

**Lecture d'architecture.** La conjonction « base administrable + citation des sources » oriente
vers une **génération augmentée par recherche documentaire** plutôt que vers un modèle génératif
libre : c'est la seule façon de citer une source et de laisser un administrateur modifier le
savoir sans redéploiement. Ceci **éclaire** le choix ; il reste à arbitrer.

---

## 4. Analyse par exigence

| Réf. | Données entrantes | Données sortantes | PII | Risque | Humain dans la boucle | Preuve attendue |
|:---|:---|:---|:---|:---|:---|:---|
| `IA-01` | Question libre du client | Réponse en français | Possible (le client peut citer son dossier) | Réponse fausse sur une procédure | Non requis | Jeu de questions de référence, taux de réponse correcte |
| `IA-02` | Question + contexte de parcours | Orientation vers un module | Faible | Mauvaise orientation → dossier mal déposé | Non requis | Scénarios d'orientation validés métier |
| `IA-03` | Fil de conversation | Décision d'escalade + contexte transmis | Oui (fil complet) | Escalade excessive ou insuffisante | **Oui** | Vérification que le fil est repris dans le ticket |
| `IA-04` | Question + corpus documentaire | Réponse **avec source citée** | Faible | Citation erronée | Non requis | Contrôle que chaque réponse cite un document existant |
| `IA-05` | Canal d'accès | — | — | — | — | Accès effectif depuis le portail |
| `IA-06` | Fil + signaux d'incertitude | Transfert vers opérateur habilité | Oui | Non-détection d'une situation critique | **Oui** | Scénarios de situations à escalader obligatoirement |
| `IA-07` | Demande explicite du client | Mise en relation | Oui | Parcours dissuasif | **Oui** | Vérification qu'un humain est joignable en une action |

**Point de vigilance.** `IA-07` est une exigence **anti-dissuasion**. Elle interdit qu'un
assistant serve à filtrer les sollicitations. Sa preuve est comportementale, pas technique : elle
se démontre en recette, par un parcours utilisateur.

---

## 5. Architecture conceptuelle — sans implémentation

```
Client → Portail
           │
           ▼
   Orchestrateur d'assistant          ← interface, à définir
           │
           ├── Recherche documentaire → Base de connaissances administrable   (IA-04)
           │
           ├── Fournisseur de modèle  → TBD_DSI                               (PC-18)
           │
           ├── Garde-fous             → confiance, sujets interdits, PII
           │
           └── Escalade               → Ticket existant, fil conservé         (IA-03, IA-06, IA-07)
```

**Points d'ancrage existants dans la plateforme.** Le socle apporte déjà ce dont l'escalade a
besoin : ticket, fil de messages, journal d'audit, habilitations. L'assistant n'aurait pas à
réinventer le canal humain — il s'y raccorderait.

**Repli obligatoire.** Si le fournisseur de modèle est indisponible, l'assistant doit dégrader
vers la mise en relation humaine, jamais vers une réponse inventée. `IA-07` l'impose de fait.

---

## 6. Décisions requises — `PC-18`

Aucune de ces questions ne peut être tranchée par l'équipe technique.

| # | Question | Pourquoi elle bloque |
|---:|:---|:---|
| 1 | L'usage d'un modèle de langage est-il **autorisé** par le Groupe ? | Sans autorisation, les sept exigences sont sans objet. |
| 2 | Quel fournisseur, et **hébergé où** ? | Détermine la souveraineté des données. |
| 3 | SaaS, cloud privé, ou auto-hébergé ? | Change entièrement l'architecture et le coût. |
| 4 | Quelles données **peuvent** être transmises au modèle ? | Le fil contient des données personnelles et des références de dossier. |
| 5 | Quelles données sont **interdites** ? | Nécessaire pour dimensionner l'expurgation. |
| 6 | Région d'hébergement imposée ? | Conformité ARTCI / RGPD. |
| 7 | Les conversations peuvent-elles servir à **entraîner** le modèle du fournisseur ? | Réponse par défaut de nombreux services : oui. Souvent inacceptable. |
| 8 | Durée de **rétention** des échanges ? | Le moteur de rétention existe déjà ; il faut une catégorie et une durée. |
| 9 | Quelle **base documentaire** est autorisée comme source ? | Détermine ce que l'assistant a le droit de citer. |
| 10 | **Budget** — coût par échange, plafond mensuel ? | Conditionne le choix de modèle. |
| 11 | Quel **SLA** attendu, et quel comportement en cas d'indisponibilité ? | Détermine le repli. |
| 12 | Validation **juridique / DPO** obtenue ? | Traitement de données personnelles par un tiers. |

---

## 7. Matrice d'options — à arbitrer, non à décider ici

| Option | Souveraineté | Coût | Latence | Effort | Compatible IA-04 ? |
|:---|:---|:---|:---|:---|:---|
| Modèle auto-hébergé on-premise | **Totale** | Élevé (matériel) | Maîtrisée | Élevé | Oui |
| Cloud privé Groupe | Bonne | Moyen | Bonne | Moyen | Oui |
| Service d'entreprise avec engagement de non-réutilisation | Contractuelle | Moyen | Bonne | Faible | Oui |
| API externe grand public | **Faible** | Faible | Variable | Faible | Oui, mais données exposées |
| **Renoncement formel** | — | Nul | — | Nul | — |

Le renoncement est une option **légitime** — mais elle doit être **actée contractuellement** :
sept exigences dont cinq Must ne peuvent pas être abandonnées par omission.

---

## 8. Verdict

`IA-01` à `IA-07` : **`BLOCKED_DSI`** — `PC-18` OPEN.

**Aucune implémentation ne doit être entreprise avant l'arbitrage.** Non par prudence excessive,
mais parce que le choix d'hébergement du modèle détermine l'architecture entière : un assistant
bâti sur une API externe ne se transpose pas sur un modèle auto-hébergé.

C'est, en volume, le **premier poste du reste à faire** : sept exigences sur quatorze, dont cinq
Must. Il a été jusqu'ici sous-évalué dans les documents de pilotage.
