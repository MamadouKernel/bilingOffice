# Matrice des flux réseau

> **Document** : `docs/56-network-flow-matrix.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Objet** : les flux que l'application exige réellement, relevés dans le code. Les ports
> effectifs relèvent de l'infrastructure.

---

## 1. Flux requis

| # | Source | Destination | Protocole | Port | Objet | Requis | Détenteur | Statut |
|---:|:---|:---|:---|:---|:---|:---|:---|:---|
| 1 | Utilisateurs | Application web | HTTPS | 443 | Accès à la plateforme | **Oui** | Infra | `TBD_INFRA` |
| 2 | Utilisateurs | Application web | HTTP | 80 | Redirection vers HTTPS | Recommandé | Infra | `TBD_INFRA` |
| 3 | Application web | SQL Server | TDS | 1433 *(défaut)* | Persistance | **Oui** | Infra | `TBD_INFRA` |
| 4 | Application web | Stockage documentaire | Système de fichiers | — | Dépôt et lecture des pièces | **Oui** | Infra | `TBD_INFRA` |
| 5 | Application web | Antivirus local | Processus | — | Analyse des dépôts | **Oui** | Infra | `TBD_INFRA` |
| 6 | Supervision | `/healthz`, `/ready` | HTTPS | 443 | Sondes de santé | **Oui** | Infra | `TBD_INFRA` |
| 7 | Administrateurs | Serveur | RDP / SSH | `TBD_INFRA` | Exploitation | **Oui** | Infra | `TBD_INFRA` |
| 8 | Poste de recette | Application web | HTTPS | 443 | Campagne k6 | Pour F016 | Infra | `TBD_INFRA` |

---

## 2. Flux explicitement absents

C'est un point de conception, pas une omission.

| Flux | État | Motif |
|:---|:---|:---|
| Application → SMTP | **Aucun** | Aucun fournisseur d'e-mail n'est configuré |
| Application → Internet | **Aucun** | Aucune ressource externe n'est chargée — vérifié par test automatisé |
| Application → API tierce | **Aucun** | Le CDC place IPAKI, IRIS, SydamWorld, IES et la GED **hors périmètre** : les agents y opèrent manuellement |
| Application → CDN | **Aucun** | Polices, styles et scripts servis localement |

**L'application peut fonctionner sur un réseau sans accès Internet sortant.** C'est une propriété
utile dans un contexte de terminal portuaire, et elle est vérifiée en continu par un test qui
échoue si une ressource externe est réintroduite.

---

## 3. Flux futurs — non ouverts

À ne pas provisionner tant que la décision n'est pas rendue.

| # | Source | Destination | Objet | Statut |
|---:|:---|:---|:---|:---|
| F1 | Application | Fournisseur SMS | Notification chauffeur — ENR-04 | `BLOCKED_PROVIDER` — PC-16 |
| F2 | Application | API WhatsApp Business | Notification chauffeur — ENR-04 | `BLOCKED_PROVIDER` — PC-16 |
| F3 | Fournisseur | Application | Accusés de livraison | `BLOCKED_PROVIDER` — PC-16 |
| F4 | Application | Fournisseur de modèle de langage | Assistant conversationnel — IA-01..07 | `BLOCKED_DSI` — PC-18 |
| F5 | Application | Serveur SMTP | Canal e-mail | `BLOCKED_PROVIDER` |

**F4 mérite une attention particulière.** Si le modèle est hébergé hors de l'infrastructure, ce
flux transporterait des fils de conversation contenant des données personnelles et des références
de dossier. Son ouverture est indissociable de l'arbitrage de souveraineté — `docs/45`.

---

## 4. Points de vigilance

### Derrière un proxy inverse

`UseHttpsRedirection` est actif. Si le proxy termine TLS et transmet en HTTP, **les en-têtes de
transfert doivent être propagés et l'application configurée pour les honorer**, sinon elle
redirige indéfiniment. C'est la panne la plus fréquente de ce type de déploiement.

### Sondes accessibles à la supervision

`/healthz` et `/ready` n'exposent aucune donnée métier, mais renseignent sur la disponibilité de
la base. Leur exposition publique est à éviter : les restreindre au réseau de supervision est
préférable.

### Traitements de fond

Les cinq services hébergés n'ouvrent **aucun flux réseau propre** : ils écrivent en base par la
même connexion. Aucune règle supplémentaire n'est requise.

---

## 5. Récapitulatif

| Catégorie | Flux |
|:---|---:|
| Requis pour fonctionner | **5** |
| Requis pour exploiter et superviser | 3 |
| Explicitement absents | 4 |
| Futurs, bloqués | 5 |

Le périmètre réseau est **restreint** : cinq flux suffisent à faire fonctionner la plateforme,
tous internes.
