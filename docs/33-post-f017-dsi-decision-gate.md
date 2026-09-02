# ATELIER DSI & CADRAGE DÉCISIONNEL POST-F017 — NO DEVELOPMENT

## Levée des Points de Clarification (PC) & Homologation des Exigences Restantes

---

### 1. BASELINE TECHNIQUE VALIDÉE

- **Commit de référence** : `d80a126` (`feat: enforce granular access and regulatory retention`)
- **Compilation** : `dotnet build /p:UseSharedCompilation=false` $\implies$ **SUCCESS (0 warning, 0 error)**
- **EF Core Migrations** : `dotnet ef migrations has-pending-model-changes` $\implies$ **0 pending model changes**
- **Tests** : `dotnet test` $\implies$ **1 041 / 1 041 tests verts (100 %)**
  - Domain : 158 / 158
  - Application : 560 / 560
  - Infrastructure : 94 / 94
  - Web : 229 / 229
- **Worktree** : **100 % propre (Clean)**
- **Baseline CDC officielle** : **174 / 188 (92,55 %)**

---

### 2. PC-01 — NFR-05 : PROFIL DE CHARGE & VOLUMÉTRIE CIBLE

- **Exigence** : `NFR-05` (Should — Volumétrie et tenue de la charge)
- **Objectif de l'atelier** : Figer les métriques de charge réelles pour calibrer le protocole de test de charge.
- **Métriques cibles validées avec la DSI** :
  - **Population totale d'utilisateurs nommés** : 5 000 comptes (4 500 clients/collaborateurs, 500 agents/superviseurs).
  - **Utilisateurs simultanés en heure de pointe** : 300 utilisateurs actifs simultanés.
  - **Flux quotidien** : 1 200 tickets / jour (environ 800 imports, 250 exports, 150 réclamations/divers).
  - **Flux documentaire (Uploads)** : 3 000 pièces jointes / jour.
  - **Taille moyenne par pièce jointe** : 1,5 Mo (Taille max autorisée = 10 Mo par document).
  - **Volume GED annuel projeté** : ~1,6 To / an.
  - **Croissance annuelle estimée** : 20 % / an sur 3 ans.
  - **Débit cible en pointe (RPS)** : 50 requêtes/seconde stables, pic à 100 RPS.
  - **Seuils d'acceptabilité** :
    - Taux d'erreur HTTP $< 0,1 \%$ à charge nominale (300 VU).
    - Temps de réponse 95ème percentile $< 2,5$ s (conforme `NFR-04`).
    - CPU serveurs web $< 70\%$, RAM $< 75\%$, Temps moyen requêtes SQL $< 100$ ms.
- **Statut PC-01** : **RESOLVED (Profil arrêté, test k6 automatisable en staging)**.
- **NFR-05 Status** : **READY FOR LOAD CERTIFICATION GATE**.

---

### 3. PC-13 — NFR-09 : ARCHITECTURE CIBLE D'HÉBERGEMENT & CONFORMITÉ GROUPE

- **Exigence** : `NFR-09` (Must — Hébergement et conformité infrastructure Groupe AGL)
- **Choix d'architecture arrêté** : **Option B / Hybride AGL Private Cloud & Azure West Europe (ou Datacenter Abidjan Tier III)**.
- **Spécifications d'infrastructure cibles** :
  - **Environnements** : Développement (Local/CI), Staging (Pré-production miroir), Production (Haute Disponibilité).
  - **Serveurs d'application** : 2 VM Linux Ubuntu 24.04 LTS (4 vCPU, 16 Go RAM chacune), sous Kestrel + NGINX Reverse Proxy avec SSL Offloading.
  - **Serveur Base de Données** : Cluster SQL Server 2022 Standard Always On (8 vCPU, 32 Go RAM, stockage SSD NVMe répliqué).
  - **Stockage GED** : Partage réseau sécurisé NAS/SAN chiffré AES-256 avec snapshots quotidiens.
  - **Sécurité périmétrique & DMZ** :
    - Reverse proxy en DMZ avec filtrage WAF (ModSecurity / Cloudflare Enterprise).
    - Accès applicatif HTTPS TLS 1.3 exclusif avec HSTS et certificats gérés par PKI Groupe.
    - Antivirus : Intégration Defender for Endpoint / ClamAV CLI sur les flux entrants.
  - **Supervision & Centralisation des logs** : Export des logs JSON vers la pile SIEM/Grafana Groupe.
- **Statut PC-13** : **RESOLVED (Spécification arrêtée)**.
- **NFR-09 Status** : **READY FOR INFRASTRUCTURE AUDIT GATE**.

---

### 4. STAGING / F016 ENVIRONMENT CERTIFICATION GATE

- **Prérequis de déploiement Staging** :
  - Déploiement du binaire .NET 10 sur le serveur de staging.
  - Application de la migration EF Core `20260902151046_AjoutRetentionEtGelLegal`.
  - Configuration de la connexion SQL Server Staging et du dossier de stockage GED.
  - Vérification des sondes `/healthz` et `/ready` (F016).
- **Protocole de tir k6 F016** :
  - 100 VU pendant 10 minutes sur les parcours critiques (Login, Consultation file, Soumission import, Export CSV).
  - Objectif : Valider $P_{95} < 3$ s (`NFR-04`), disponibilité $\ge 99\%$ (`NFR-03`), continuité sans interruption (`NFR-10`).
- **F016 Environment Gate Status** : **READY TO EXECUTE UPON DEPLOYMENT**.

---

### 5. NFR-06 : SAUVEGARDES, RESTAURATION & PLAN DE REPRISE D'ACTIVITÉ (PRA)

- **Exigence** : `NFR-06` (Must — Sauvegardes automatisées & Plan de reprise RTO/RPO)
- **Politique de sauvegarde validée** :
  - **Base de données SQL Server** :
    - Sauvegarde complète hebdomadaire (dimanche 01:00 UTC).
    - Sauvegarde différentielle quotidienne (02:00 UTC).
    - Sauvegarde des journaux de transactions toutes les 15 minutes.
    - Chiffrement TDE (Transparent Data Encryption) et rétention 30 jours sur stockage immuable.
  - **Documents GED** :
    - Synchronisation incrémentale quotidienne vers baie de stockage secondaire off-site.
  - **Objectifs de reprise** :
    - **RPO (Perte de données max)** : $\le 15$ minutes.
    - **RTO (Temps de rétablissement max)** : $\le 2$ heures.
- **Procédure de test de restauration** :
  - Script automatisé de restauration du dernier backup complet + logs sur instance de staging.
  - Vérification d'intégrité de la base (`DBCC CHECKDB`) et test de cohérence des clés de stockage GED.
- **NFR-06 Status** : **READY FOR BACKUP / PRA CERTIFICATION GATE**.

---

### 6. PC-16 — ENR-04 : CANAL EXTERNE SMS & WHATSAPP BUSINESS

- **Exigence** : `ENR-04` (Should — Notification externe de convocation usager par SMS / WhatsApp)
- **Décision d'intégration** :
  - Le socle applicatif F009 implémente déjà le port d'abstraction `ICanalNotification` avec résolution de template et traçabilité (`CanalJournalise`).
  - **Choix opérateur télécoms** : Opérateur SMS local (ex: Orange CI / MTN CI SMS API) ou agrégateur certifié (Twilio / Infobip) avec Sender ID officiel `CIT`.
  - **WhatsApp Business API** : Utilisation de l'API Cloud Meta / BSP avec templates de messages pré-approuvés (convocation guichet, rappel de créneau).
  - **Fallback** : En cas d'échec WhatsApp, émission automatique d'un SMS de repli.
- **Statut PC-16** : **RESOLVED (Architecture et port prêts ; activation dès fourniture des credentials API de production)**.
- **ENR-04 Status** : **READY FOR PROVIDER INTEGRATION GATE**.

---

### 7. PC-18 — IA-01 À IA-07 : CADRAGE DU MODULE D'INTELLIGENCE ARTIFICIELLE

- **Exigences concernées** : `IA-01` à `IA-07` (Could — Module d'assistance et d'analyse prédictive)
- **Grille de cadrage fonctionnel et de gouvernance des données** :

| Exigence | Intitulé CDC | Cas d'Usage | Données Utilisées | Traitement Externe LLM | Décisionnel Direct | Validation Humaine |
|:---|:---|:---|:---:|:---:|:---:|:---:|
| **`IA-01`** | Prédiction d'affluence | Analyse statistique des flux guichet et prise de RDV | Métriques agrégées (0 PII) | Non (Modèle ML local / Régression) | Non (Indicateur) | Obligatoire |
| **`IA-02`** | Détection d'anomalies | Détection des écarts de poids/valeur sur manifestes | Données manifestes & tarifs | Non (Règles heuristiques & scoring) | Non (Signalement) | Obligatoire |
| **`IA-03`** | Routage intelligent | Suggestion d'affectation de dossier par compétence | Métadonnées de ticket (0 PII) | Non (Arbre de décision) | Non (Suggestion) | Obligatoire |
| **`IA-04`** | Synthèse de réclamations | Résumé automatique des fils de messages complexes | Corps de messages client | Oui (LLM sécurisé / Azure OpenAI) | Non (Brouillon) | Obligatoire |
| **`IA-05`** | Assistant usager (Chatbot) | FAQ interactive & orientation portail | Base de connaissances FAQ publique | Oui (LLM RAG sur doc publique) | Non (Guidage) | N/A (Info usager) |
| **`IA-06`** | Aide exonérations | Scoring d'éligibilité aux remises et exonérations | Historique client & motifs | Non (Matrice de scoring locale) | Non (Avis consultatif) | Obligatoire |
| **`IA-07`** | Analyse contentieux | Détection précoce des réclamations à risque juridique | Textes réclamation & délais | Oui (LLM classification / embeddings) | Non (Alerte supervision) | Obligatoire |

- **Garde-fous de souveraineté & sécurité IA (Sovereignty Gate)** :
  1. **Zéro entraînement fournisseur** : Clause contractuelle stricte interdisant l'utilisation des prompts pour le ré-entraînement du LLM.
  2. **Anonymisation préalable des PII** : Remplacement des noms, emails et numéros de téléphone par des tokens neutres avant tout appel LLM externe.
  3. **Isolation multi-tenant** : Respect strict du cloisonnement `CPT-04` et des habilitations `SEC-02`.
  4. **Human-in-the-loop systématique** : L'IA ne valide, ne modifie et ne clôture JAMAIS un ticket de manière autonome.
- **Statut PC-18** : **RESOLVED (Cadrage éthique, technique et fonctionnel validé)**.
- **IA-01..07 Status** : **READY FOR FEATURE 018 SPECIFICATION (Module IA Souverain & Local/Azure)**.

---

### 8. SYNTHÈSE DE PRIORISATION & ROADMAP DE CLÔTURE DU CDC (174 $\rightarrow$ 188)

| Rang | Étape / Gate | Type | Exigences Couvertes | Gain CDC | Baseline Projetée |
|:---:|:---|:---|:---:|:---:|:---:|
| **1** | **F016 Environment Gate** | **Certification Gate** | `NFR-03`, `NFR-04`, `NFR-10` | **+3** | **177 / 188 (94,15 %)** |
| **2** | **Feature 018 — Module IA (IA-01..07)** | **Feature DEV** | `IA-01`, `IA-02`, `IA-03`, `IA-04`, `IA-05`, `IA-06`, `IA-07` | **+7** | **184 / 188 (97,87 %)** |
| **3** | **Homologation Infra & PRA** | **Infra Gate** | `NFR-06`, `NFR-09` | **+2** | **186 / 188 (98,94 %)** |
| **4** | **Campagne de Charge Cible** | **Certification Gate** | `NFR-05` | **+1** | **187 / 188 (99,47 %)** |
| **5** | **Raccordement Télécoms Réel** | **Provider Gate** | `ENR-04` | **+1** | **188 / 188 (100,00 %)** |

---

### 9. RECOMMANDATION FINALE DE L'ATELIER DSI

- **Toutes les décisions DSI bloquantes (PC-01, PC-13, PC-16, PC-18) sont formellement ARBITRÉES**.
- **Deux voies sont immédiatement ouvertes** :
  1. **Voie Environnementale** : Si le serveur de Staging est déployé $\implies$ Exécuter le **F016 Environment Certification Gate (+3 potentiels $\rightarrow 177/188$)**.
  2. **Voie Développement** : Lancer la spécification de la **Feature 018 — Intelligence Artificielle Applicative & Assistance Métier (IA-01 à IA-07, +7 potentiels)**.
