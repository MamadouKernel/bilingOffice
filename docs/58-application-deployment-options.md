# Options de déploiement applicatif

> **Document** : `docs/58-application-deployment-options.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Aucune des deux options n'est retenue comme architecture officielle.** Ce choix relève de
> `PC-13`, non arbitré.

---

## 1. Ce que l'application impose, quelle que soit l'option

| Contrainte | Origine | Conséquence sur le choix |
|:---|:---|:---|
| Runtime .NET 10 | Cible des projets | Aucune — disponible partout |
| SQL Server accessible | `UseSqlServer` | Aucune |
| Système de fichiers accessible en écriture | Stockage documentaire | **Volume persistant obligatoire en conteneur** |
| **Antivirus Microsoft Defender** | `ScannerAntivirusDefender` (`MpCmdRun.exe`) | **Contrainte Windows** — voir § 4 |
| Secrets hors dépôt | Conception | Mécanisme à fournir |
| En-têtes de transfert si proxy | `UseHttpsRedirection` | Configuration requise derrière un proxy |

**Une seule de ces contraintes discrimine réellement les options : l'antivirus.**

---

## 2. Option A — IIS sur Windows Server

### Compatibilité : **totale**

C'est l'option qui demande le moins d'adaptation : l'adaptateur antivirus cible Defender, présent
nativement sur Windows Server.

### Prérequis

| Élément | Valeur |
|:---|:---|
| Windows Server | `TBD_INFRA` |
| IIS avec ASP.NET Core Hosting Bundle **.NET 10** | Obligatoire |
| Compte de service applicatif | `TBD_INFRA` |
| Répertoire de stockage documentaire | `TBD_INFRA` |
| Certificat TLS lié au site | `TBD_INFRA` |

### Publication

```bash
dotnet publish src/CIT.Plateforme.Web -c Release -o publish
```

### Déploiement

1. Arrêter le pool d'applications.
2. Déposer le contenu de `publish/` dans le répertoire du site.
3. Poser les variables d'environnement — `ASPNETCORE_ENVIRONMENT`, secrets.
4. Accorder au compte de service les droits **lecture / écriture** sur le répertoire de stockage.
5. Redémarrer le pool.

### Points d'attention

- Le **profil utilisateur du compte de pool** doit être chargé si la protection des données ASP.NET
  Core persiste ses clés sur disque, faute de quoi les cookies d'authentification sont invalidés à
  chaque redémarrage — les utilisateurs sont déconnectés sans raison apparente.
- Les **limites de taille de requête** d'IIS doivent être alignées sur
  `Securite:Fichiers:TailleMaxOctets` (10 Mo par défaut), sinon IIS rejette avant l'application,
  avec un message peu explicite.

### Retour arrière

Redéposer la version précédente et redémarrer le pool. Si le schéma a été migré, voir `docs/57`.

---

## 3. Option B — Conteneur derrière un proxy inverse

### Compatibilité : **partielle — l'antivirus ne fonctionne pas en l'état**

Voir § 4. Tout le reste est compatible.

### Prérequis

| Élément | Valeur |
|:---|:---|
| Moteur de conteneurs | `TBD_INFRA` |
| Proxy inverse terminant TLS | `TBD_INFRA` |
| **Volume persistant** pour le stockage documentaire | **Obligatoire** |
| Mécanisme d'injection des secrets | `TBD_DSI` |

### Points d'attention

- **Sans volume persistant, les documents déposés disparaissent au redémarrage du conteneur.**
  C'est l'erreur la plus coûteuse de cette option, et elle est silencieuse : le dépôt réussit, la
  base garde la référence, le fichier n'existe plus.
- Le proxy terminant TLS impose la propagation des en-têtes de transfert, faute de quoi
  l'application redirige en boucle.
- Aucun `Dockerfile` n'existe dans le dépôt : il resterait à écrire.

### Retour arrière

Redéployer l'image précédente. Le volume documentaire n'est pas affecté.

---

## 4. Le point qui distingue réellement les deux options

`ScannerAntivirusDefender` invoque **`MpCmdRun.exe`**, l'interface en ligne de commande de
Microsoft Defender. C'est la seule dépendance de la plateforme liée au système d'exploitation.

| Option | Conséquence |
|:---|:---|
| **A — Windows Server** | Fonctionne tel quel |
| **B — conteneur Linux** | **`MpCmdRun.exe` n'existe pas.** Il faudrait soit un conteneur Windows, soit un adaptateur pour un autre moteur |

`Securite:Antivirus:FailClosed` étant à `true`, un antivirus absent **bloque tous les dépôts de
documents** : la plateforme démarre mais refuse toute pièce jointe.

**L'abstraction `IAnalyseAntivirus` permet de raccorder un autre moteur sans toucher au reste** —
c'est un travail d'adaptateur, à chiffrer si l'option B est retenue.

---

## 5. Comparatif

| Critère | A — IIS / Windows | B — Conteneur |
|:---|:---|:---|
| Compatibilité antivirus | **Native** | **À traiter** |
| Effort de mise en œuvre | Faible | Moyen — `Dockerfile` à écrire |
| Persistance documentaire | Système de fichiers | **Volume obligatoire** |
| Reproductibilité | Moyenne | **Élevée** |
| Mise à l'échelle | Limitée | Meilleure — sous réserve du § 6 |
| Alignement standards Groupe | `TBD_DSI` | `TBD_DSI` |

---

## 6. Réserve commune aux deux options — mise à l'échelle

La plateforme exécute **cinq services de fond** au démarrage. Plusieurs instances les exécuteraient
chacune.

La conception protège des doublons — prise atomique et index unique sur la distribution des
notifications. Mais **ce comportement n'a jamais été vérifié en multi-instance**. Avant toute
décision de répartition de charge, ce point doit être testé, pas supposé.

En instance unique, la question ne se pose pas.

---

## 7. Verdict

Les deux options sont **documentées, aucune n'est retenue**. Le choix appartient à `PC-13`.

Le seul élément technique à verser au débat : **l'option B demande un travail supplémentaire sur
l'antivirus**, que l'option A n'exige pas. Ce n'est pas un argument décisif — c'est un coût à
intégrer dans la comparaison.
