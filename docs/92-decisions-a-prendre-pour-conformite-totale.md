# Décisions à prendre pour atteindre 100% de conformité

> **Document** : `docs/92-decisions-a-prendre-pour-conformite-totale.md`
> **Établi le** : 2026-09-17
> **Pour qui** : direction / DSI / métier CIT — ce document sert de support de décision, pas de
> document technique.
> **Ce qu'il couvre** : les 11 points du rapport d'audit qui restent hors de portée d'un audit ou
> d'une correction de code (les « 2 Non conformes » et les « 9 Non vérifiables »). Chaque ligne
> ci-dessous est une décision que seule CIT peut prendre — le projet ne peut ni la prendre à sa
> place, ni la deviner sans mentir sur l'état réel de conformité.
>
> Ce document synthétise, dans un format de décision, ce qui est détaillé techniquement dans
> `docs/82-external-decisions-only.md` (registre complet D-01 à D-15) et
> `docs/90-runbook-exploitation-nfr-infrastructure.md` (préparation opérationnelle des 7 exigences
> infrastructure). Il n'ajoute aucune information nouvelle : il les rend actionnables.

---

## Comment lire ce document

Pour chaque décision : **ce qui est demandé**, **qui doit trancher**, **ce que ça débloque**, et
une case à cocher une fois la décision prise et communiquée au projet. Aucune case n'est cochée
d'avance — c'est le principe du projet depuis le début : `DEFAULT_IF_NO_DECISION = NONE`.

---

## Groupe A — Les « 2 Non conformes » (fournisseurs externes)

Ces deux points sont bloqués uniquement par l'absence d'un contrat avec un fournisseur réel. Le
code est prêt à recevoir n'importe lequel sans développement supplémentaire.

### A-1 — Fournisseur SMS / WhatsApp (couvre ENR-04)

- [ ] **Décision prise**
- **Ce qui est demandé** : choisir un fournisseur (ex. Twilio, Vonage, Orange, MTN…) et signer un contrat/API key.
- **Qui tranche** : DSI + Finance.
- **Débloque** : la réinitialisation de mot de passe et les notifications par SMS/WhatsApp (ENR-04), aujourd'hui indisponibles en dehors du développement.
- **Référence détaillée** : `docs/82` D-07.

### A-2 — Fournisseur de modèle de langage (LLM) pour l'assistant conversationnel (couvre IA-05, et plus largement IA-01 à IA-07)

- [ ] **Décision prise**
- **Ce qui est demandé** : choisir un fournisseur de LLM (ex. OpenAI, Anthropic, Azure OpenAI, Mistral…) et valider les conditions de confidentialité des données envoyées.
- **Qui tranche** : DSI.
- **Débloque** : les réponses génératives de l'assistant conversationnel. Le socle (base de connaissances administrable IA-04, historique, journalisation) est déjà livré ; seul l'appel au modèle manque.
- **Référence détaillée** : `docs/82` D-08.

---

## Groupe B — Les « 9 Non vérifiables » (exploitation / infrastructure réelle)

Ces neuf points ne peuvent être vérifiés qu'une fois la plateforme réellement déployée et
exploitée. Le code est prêt (point de contrôle `/ready`, script de charge k6, configuration
externalisée) ; ce qui manque est soit un paramètre à fixer, soit une période d'exploitation
réelle à observer.

### B-1 — Environnement de recette / staging (préalable à presque tout le reste)

- [ ] **Décision prise**
- **Ce qui est demandé** : provisionner un serveur, une base SQL, un stockage persistant, un nom de domaine et le réseau associé.
- **Qui tranche** : DSI / Exploitation.
- **Débloque** : B-2 à B-6, la recette elle-même, un éventuel test d'intrusion, et le déploiement.
- **Référence détaillée** : `docs/82` D-02.

### B-2 — Choix d'hébergement : IIS/Windows ou conteneur Linux (couvre NFR-09)

- [ ] **Décision prise**
- **Ce qui est demandé** : trancher l'architecture cible. Windows/IIS fonctionne tel quel dès aujourd'hui ; un conteneur Linux exigerait un développement additionnel (adaptateur antivirus).
- **Qui tranche** : DSI.
- **Débloque** : NFR-09, et conditionne B-1.
- **Référence détaillée** : `docs/82` D-03, `docs/90` NFR-09.

### B-3 — Volumétrie cible (couvre NFR-05, conditionne NFR-04)

- [ ] **Décision prise**
- **Ce qui est demandé** : chiffrer le nombre de tickets/jour attendu, la taille moyenne des pièces jointes, et le nombre d'utilisateurs simultanés en pic.
- **Qui tranche** : Métier (Billing Office / Customer Complaint / Enrôlement) + DSI.
- **Débloque** : un tir de charge réaliste pour NFR-04 (temps de réponse < 3s) et NFR-05.
- **Référence détaillée** : `docs/82` D-04, `docs/90` NFR-04/05.

### B-4 — Fenêtre de mesure de la disponibilité en heures ouvrées (couvre NFR-03)

- [ ] **Décision prise**
- **Ce qui est demandé** : définir les heures ouvrées exactes, le calendrier des jours fériés/maintenances, et brancher un outil de supervision externe (uptime monitor) sur `/ready`.
- **Qui tranche** : DSI.
- **Débloque** : la mesure de NFR-03 (≥ 99% en heures ouvrées) sur un cycle d'au moins 4 semaines.
- **Référence détaillée** : `docs/82` D-05, `docs/90` NFR-03.

### B-5 — Fenêtre et politique de maintenance pour la disponibilité 24/7 (couvre NFR-10)

- [ ] **Décision prise**
- **Ce qui est demandé** : définir la politique de fenêtres de maintenance planifiées et la durée du cycle de mesure 24/7 (recommandé : tir d'endurance hors heures de pointe).
- **Qui tranche** : DSI.
- **Débloque** : la mesure de NFR-10.
- **Référence détaillée** : `docs/82` D-06, `docs/90` NFR-10.

### B-6 — Politique de sauvegarde et test de restauration (couvre NFR-06)

- [ ] **Décision prise**
- **Ce qui est demandé** : fixer la périodicité de sauvegarde, la rétention, le RPO/RTO, **et** planifier un exercice réel de restauration (le mot « testée » du CDC est contractuel). Attention : la base SQL et le répertoire documentaire (`Stockage:RepertoireDocuments`) doivent être sauvegardés **ensemble**, sous peine de restauration incohérente.
- **Qui tranche** : Exploitation / DSI.
- **Débloque** : NFR-06.
- **Référence détaillée** : `docs/82` D-15, `docs/90` NFR-06.

### B-7 — Topologie réseau de cloisonnement production/recette (couvre SEC-09)

- [ ] **Décision prise**
- **Ce qui est demandé** : mettre en place et documenter la séparation physique/réseau réelle entre les environnements (bases distinctes, comptes distincts, réseaux ou groupes de sécurité distincts). Le code est déjà prêt à recevoir n'importe quelle topologie (configuration externalisée, environnement UAT déjà isolé).
- **Qui tranche** : DSI / Exploitation.
- **Débloque** : SEC-09.
- **Référence détaillée** : `docs/90` SEC-09.

### B-8 — Répertoire de stockage documentaire en production

- [ ] **Décision prise**
- **Ce qui est demandé** : fixer le chemin persistant de production pour les documents déposés (`Stockage:RepertoireDocuments`).
- **Qui tranche** : Exploitation.
- **Débloque** : tout dépôt documentaire hors développement, et conditionne B-6.
- **Référence détaillée** : `docs/82` D-11.

### B-9 — Paramètres de réessai des notifications

- [ ] **Décision prise**
- **Ce qui est demandé** : fixer le nombre de tentatives et le délai entre tentatives pour l'envoi des notifications (`Notifications:Reessai:NombreTentativesMax` et `DelaiEntreTentatives`).
- **Qui tranche** : Métier / DSI.
- **Débloque** : le démarrage de l'application hors développement (le code refuse de démarrer sans ces valeurs plutôt que de deviner).
- **Référence détaillée** : `docs/82` D-10.

---

## Ce qui n'exige aucune décision immédiate

Deux points du lot des « ~28 Partielles » restent en configuration ouverte (SOC-07, TRF-05) et
suivent la même logique que ci-dessus mais sans bloquer la conformité fonctionnelle du reste de
la plateforme — ils sont paramétrables sans développement dès qu'un choix métier est fait
(types de documents attendus, règles de diffusion des notifications). Voir le rapport d'audit,
section « Clôture des ~28 Partielles ».

---

## Synthèse pour décision rapide

| Groupe | Décisions | Bloque |
|:---|:---|:---|
| **A** — Fournisseurs externes | A-1, A-2 | ENR-04, IA-01 à IA-07 |
| **B** — Exploitation / infrastructure | B-1 à B-9 | NFR-03/04/05/06/09/10, SEC-09 |

**11 décisions au total. Aucune n'est prise par défaut. Aucune ne peut être devinée sans fausser
le rapport de conformité.** Dès qu'une décision de ce document est prise, elle peut être
communiquée au projet pour configuration (aucun développement supplémentaire n'est nécessaire pour
la quasi-totalité des lignes ci-dessus — seul le choix d'un conteneur Linux en B-2 exigerait un
lot de développement dédié).
