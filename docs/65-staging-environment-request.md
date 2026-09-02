# Demande d'environnement Staging — dossier à l'attention de la DSI

> **Document** : `docs/65-staging-environment-request.md`
> **Établi le** : 2026-09-02, sur le commit `b87ad15`
> **Objet** : réunir en une seule pièce tout ce que la DSI et l'exploitation doivent fournir pour
> qu'un environnement de recette existe.

---

## 1. Pourquoi cette demande

La plateforme est développée, testée et documentée. **1041 tests automatisés passent**, le socle est
sans avertissement de compilation, et le modèle de données est aligné sur les migrations.

Ce qui manque n'est pas du développement : c'est **un environnement à déployer**. Trois exigences
**Must** du CDC — NFR-03 (disponibilité), NFR-04 (temps de réponse), NFR-10 (accessibilité 24/7) —
ne peuvent être ni vérifiées ni certifiées sans lui. Elles n'attendent aucune ligne de code : elles
attendent une adresse.

C'est le meilleur rapport valeur / effort du projet à ce stade.

---

## 2. Ce qui est demandé — infrastructure

| # | Élément | Détail | Détenteur |
|---:|:---|:---|:---|
| 1 | Serveur applicatif | Selon l'option retenue au § 4 | Exploitation |
| 2 | Runtime .NET 10 | Hosting Bundle ASP.NET Core 10 si IIS | Exploitation |
| 3 | Instance SQL Server | Base dédiée à la recette | Exploitation |
| 4 | Répertoire de stockage documentaire | **Persistant**, accessible en écriture au compte applicatif | Exploitation |
| 5 | Certificat TLS | Lié au nom de domaine de recette | Exploitation |
| 6 | Nom de domaine de recette | Fournit la `BASE_URL` | Exploitation |
| 7 | Antivirus opérationnel sur le serveur | Voir § 4 | Exploitation |
| 8 | Accès d'exploitation | RDP ou SSH | Exploitation |
| 9 | Sauvegarde de la base | Voir `docs/41` | Exploitation |

**Le point 4 mérite d'être lu deux fois.** Sans répertoire fourni, l'application écrit les documents
dans le répertoire temporaire du système, qui est purgé : le dépôt réussit, la base garde la
référence, **le fichier disparaît plus tard**. La panne est différée et silencieuse. C'est le risque
technique le plus élevé de ce déploiement.

---

## 3. Ce qui est demandé — flux réseau

Cinq flux sont nécessaires au fonctionnement, trois à l'exploitation. Le détail figure dans
`docs/56`.

| # | Source | Destination | Port | Requis |
|---:|:---|:---|:---|:---|
| 1 | Utilisateurs | Application | 443 | **Oui** |
| 2 | Utilisateurs | Application | 80 → redirection | Recommandé |
| 3 | Application | SQL Server | 1433 | **Oui** |
| 4 | Application | Stockage documentaire | — | **Oui** |
| 5 | Application | Antivirus local | — | **Oui** |
| 6 | Supervision | `/healthz`, `/ready` | 443 | **Oui** |
| 7 | Exploitation | Serveur | RDP / SSH | **Oui** |
| 8 | Poste de recette | Application | 443 | Pour F016 |

**Aucun flux sortant vers Internet n'est requis.** La plateforme ne charge aucune ressource externe
— propriété vérifiée par un test automatisé, qui échouerait si une dépendance externe était
réintroduite.

---

## 4. Décision attendue — option de déploiement

`docs/58` documente deux options ; **aucune n'est retenue**, le choix appartient à la DSI (`PC-13`).

| Option | Effort | Point d'attention |
|:---|:---|:---|
| **A — IIS sur Windows Server** | Faible | Fonctionne tel quel |
| **B — Conteneur** | Moyen | **L'antivirus demande un travail d'adaptation** |

**L'élément technique à verser au débat** : l'analyse antivirus invoque `MpCmdRun.exe`, qui n'existe
que sous Windows. En conteneur Linux, il faudrait un adaptateur pour un autre moteur — et comme
`FailClosed` vaut `true`, un antivirus absent **bloquerait tous les dépôts de documents**.

Ce n'est pas un argument décisif : c'est un coût à intégrer dans la comparaison.

---

## 5. Décisions attendues — paramètres

Ces valeurs ne sont pas techniques : ce sont des choix. **Le projet ne les invente pas.**

### 5.1 Bloquants — sans eux, rien ne démarre

| Paramètre | Qui décide |
|:---|:---|
| Chaîne de connexion SQL | Exploitation — **secret, hors dépôt** |
| Répertoire de stockage documentaire | Exploitation |
| `ASPNETCORE_ENVIRONMENT` = `Staging` | Exploitation |
| Compte et mot de passe de l'administrateur initial | DSI — **secret, hors dépôt** |
| `Notifications:Reessai:NombreTentativesMax` | **DSI** — voir ci-dessous |
| `Notifications:Reessai:DelaiEntreTentatives` | **DSI** — voir ci-dessous |

**Les deux derniers empêchent l'application de démarrer s'ils manquent.** Le CDC (ERR-02) exige une
nouvelle tentative d'envoi sans en fixer ni le nombre ni la cadence ; le code refuse d'inventer ces
valeurs et échoue explicitement plutôt que de deviner. Elles ne sont déclarées que dans le fichier
de configuration de développement — un Staging qui ne les fournirait pas ne se lancerait pas.

C'est une décision métier, pas un réglage technique : combien de fois relancer un envoi échoué, et à
quel intervalle.

### 5.2 Arbitrages métier

| Paramètre | Valeur actuelle | Nature de la valeur |
|:---|:---|:---|
| Durée d'expiration de session | 30 min | **Repli technique**, pas une décision |
| Validité d'une invitation | 7 jours | **Repli technique** |
| Extensions de fichier autorisées | `.pdf .jpg .jpeg .png .xlsx` | Défaut raisonnable |
| Taille maximale d'un dépôt | 10 Mo | Défaut raisonnable — peut être insuffisant pour une liasse |
| Politique de réessai des notifications | Défauts | À arbitrer |
| Niveau et rétention des journaux | `Information` | À arbitrer |

**Aucune de ces valeurs ne figure dans le CDC.** Elles existent parce qu'il fallait bien démarrer,
pas parce qu'elles ont été choisies. Les laisser telles quelles est possible — mais ce serait alors
une décision assumée, pas un défaut subi.

### 5.3 Paramètres de certification F016

| Paramètre | Nécessaire à |
|:---|:---|
| **Profil de charge cible** (`PC-01`) | NFR-04 — sans lui, un tir mesure une charge arbitraire |
| Définition des heures ouvrées | NFR-03 |
| Fenêtre d'observation | NFR-03 |
| Durée du tir d'endurance | NFR-10 |
| Autorisation de générer de la charge | Toute la campagne |

**Le profil de charge est le seul vrai bloquant** de cette section : sans volumétrie cible, le
chiffre obtenu serait exact et sans valeur probante.

---

## 6. Ce qui n'est **pas** demandé

Pour éviter tout malentendu de périmètre :

| Élément | Statut |
|:---|:---|
| Fournisseur e-mail, SMS ou WhatsApp | **Pas demandé ici** — `PC-16`, voir `docs/44` |
| Fournisseur de modèle de langage | **Pas demandé ici** — `PC-18`, voir `docs/45` |
| Interfaces IPAKI, IRIS, SydamWorld, IES, GED | **Hors périmètre CDC** |
| Environnement de production | Objet d'une demande ultérieure — `docs/48` |
| Copie de données de production | **Explicitement refusée** — voir § 8 |

Seul le canal In-App est opérationnel. Les scénarios de recette qui en dépendent le mentionnent.

---

## 7. Ce que le projet fournit en retour

| Livrable | Référence |
|:---|:---|
| Inventaire d'exécution | `docs/54` |
| Matrice de configuration | `docs/55` |
| Matrice de flux réseau | `docs/56` |
| Runbook de déploiement base de données | `docs/57` |
| Options de déploiement | `docs/58` |
| Plan de tests de fumée — 24 tests | `docs/59` |
| Checklist de certification F016 | `docs/60` |
| Plan de données de recette | `docs/61` |
| Matrice d'accès de recette | `docs/62` |
| Checklist d'observabilité | `docs/63` |
| Gestion des anomalies | `docs/64` |
| **Script de précontrôle exécutable** | `scripts/staging-precheck/` |

Le script de précontrôle est **en lecture seule** et **ne contient aucun secret**. Il vérifie en une
exécution que l'environnement réunit ce que la plateforme exige — avant tout déploiement, pas après
le premier échec.

```powershell
./precheck.ps1 -BaseUrl https://<staging> -RepertoireDocuments <chemin> -ServeurSql <instance>
```

---

## 8. Une exigence non négociable

**Aucune donnée personnelle réelle, aucune copie de production ne doit alimenter la recette.**

Un environnement de recette n'a pas le niveau de protection de la production : accès plus larges,
journaux moins surveillés, sauvegardes moins encadrées. Y déverser des données réelles de clients et
de chauffeurs y transporterait des données personnelles sans le cadre qui les protège.

Le jeu de recette est **synthétique**, et `docs/61` en détaille la composition.

---

## 9. Enchaînement

```
Décision § 4        ->  Fourniture § 2 et § 3  ->  precheck.ps1  ->  Migrations (docs/57)
   ->  Déploiement (docs/58)  ->  Fumée (docs/59)  ->  Données (docs/61)  ->  Accès (docs/62)
   ->  Recette (docs/36)  ->  Certification F016 (docs/60)  ->  Go/No-Go (docs/66)
```

Chaque étape est documentée et exécutable. **La chaîne entière attend son premier maillon.**

---

## 10. Statut

**Aucun environnement de recette n'existe à ce jour.** Ce document est une demande, non un compte
rendu : rien n'y est présenté comme réalisé.
