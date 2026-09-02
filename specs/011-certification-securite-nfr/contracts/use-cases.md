# Contrats — Feature 011 (Certification Sécurité & Socle NFR)

> **Phase 1.** F011 étant une Feature de certification, ses contrats sont **des contrats de certification**, pas des commandes ou requêtes applicatives.
>
> **Un seul contrat applicatif** est produit — `C11-P01` — parce qu'une seule correction de produit est nécessaire. Créer onze commandes pour onze exigences serait un artefact de forme, pas un besoin.

---

## 1. Nature des contrats

| Type | Nombre | Nature |
|:---|:---:|:---|
| **Contrats de certification** (`C11-C01`→`C11-C11`) | 11 | Démontrent qu'une exigence CDC est tenue. **Ne produisent aucun code applicatif.** |
| **Contrat de produit** (`C11-P01`) | 1 | Seule modification de comportement de la Feature |
| **Total contrats applicatifs nouveaux** | **1** | — |

---

## 2. Contrats de certification

> **Régime** — R1 : rattachement nominatif seul (preuve existante suffisante) · R2 : complément de preuve · R3 : correction produit.

### C11-C01 — SEC-01 · Authentification et verrouillage

| | |
|:---|:---|
| **Exigence** | *« Authentification obligatoire pour tout accès ; politique de mots de passe robuste ; verrouillage après échecs répétés. »* |
| **Régime** | **R2** — complément de preuve |
| **Mécanisme certifié** | Politique de mot de passe et verrouillage portées par le fournisseur d'identité |
| **Preuve existante** | 4 suites nomment SEC-01 — elles couvrent l'**autorisation**, pas le **verrouillage** |
| **Preuve à produire** | Scénario négatif de verrouillage |
| **Code applicatif** | **AUCUN** |
| **Interdiction** | Ne pas réimplémenter le fournisseur d'identité ; ne pas modifier la politique existante |

### C11-C02 — SEC-03 · Cloisonnement strict

| | |
|:---|:---|
| **Exigence** | *« Un client n'accède qu'à ses tickets ; un agent n'accède qu'aux files et actions de son rôle. »* |
| **Régime** | **R2** |
| **Mécanisme certifié** | Service de cloisonnement + filtres serveur appliqués **dans la requête** ; habilitations par file |
| **Preuve existante** | Cloisonnement massivement exercé (≥ 6 suites) mais **aucune ne nomme SEC-03** |
| **Preuve à produire** | Rattachement nominatif ; scénario **agent hors de sa file** ; accès direct par identifiant connu |
| **Code applicatif** | **AUCUN** |
| **Interdiction** | **Ne pas dupliquer le RBAC.** Les tests consomment les services existants |

### C11-C03 — SEC-05 · Journal d'audit non modifiable

| | |
|:---|:---|
| **Exigence** | *« Journal d'audit complet et non modifiable des actions utilisateurs et administrateurs. »* |
| **Régime** | **R1 — rattachement seul** |
| **Mécanisme certifié** | Garde applicative rejetant modification et suppression avant enregistrement |
| **Preuve existante** | **Suite complète sur SQL Server réel** : modification impossible · suppression impossible · ajout possible |
| **Preuve à produire** | **AUCUNE** — rattachement nominatif de la suite existante |
| **Code applicatif** | **AUCUN** |
| **Interdiction** | **Ne pas créer de second journal** ; ne pas dupliquer la suite existante |

### C11-C04 — SEC-10 · Expiration de session paramétrable

| | |
|:---|:---|
| **Exigence** | *« Les sessions utilisateurs expirent après une période d'inactivité paramétrable. »* |
| **Régime** | **R2** |
| **Mécanisme certifié** | Durée de session lue depuis la configuration, avec glissement sur activité |
| **Preuve existante** | **AUCUNE** |
| **Preuve à produire** | (1) une valeur configurée est **effectivement appliquée** ; (2) une session dépassée se voit **refuser l'accès** |
| **Code applicatif** | **AUCUN** — configuration de test uniquement |
| **Interdiction** | **Ne pas inscrire de durée dans la configuration de production.** La durée de test est technique et non contractuelle. Ne pas modifier le défaut technique existant |

### C11-C05 — SEC-11 · Désactivation sans perte d'historique

| | |
|:---|:---|
| **Exigence** | *« Les comptes agents peuvent être désactivés immédiatement, sans suppression de leur historique. »* |
| **Régime** | **R2** |
| **Mécanisme certifié** | Indicateur d'activité consulté à l'authentification |
| **Preuve existante** | L'indicateur est exercé sous l'angle de la **gestion des collaborateurs**, pas de l'**accès** |
| **Preuve à produire** | Scénario **double, dans un même test** : accès refusé **ET** historique intégralement conservé |
| **Code applicatif** | **AUCUN** |
| **Interdiction** | **Ne pas extrapoler vers la suppression de compte** — le CDC ne l'évoque pas. Ne pas traiter l'invalidation d'une session déjà ouverte : le CDC n'en parle pas |

### C11-C06 — SEC-12 · Lien de notification jamais un accès direct

| | |
|:---|:---|
| **Exigence** | *« Aucun lien transmis par e-mail, SMS ou WhatsApp ne donne accès direct à un document protégé […] le lien conduit au ticket après authentification. »* |
| **Régime** | **R2** |
| **Mécanisme certifié** | Lien portant la référence du ticket ; **relatif, sans jeton** ; accès arbitré par les contrôles existants |
| **Preuve existante** | 4 scénarios d'accès documentaire (antivirus, propriétaire, non-propriétaire, agent habilité) + absence de jeton |
| **Preuve à produire** | Scénario **« lien ouvert sans authentification »** — absent |
| **Code applicatif** | **AUCUN** |
| **Interdiction** | Ne pas introduire de chemin d'accès parallèle |

### C11-C07 — ERR-02 · Reprise d'envoi (volet certification)

| | |
|:---|:---|
| **Exigence** | *« Échec d'envoi d'une notification : nouvelle tentative automatique et traçage ; l'échec d'un canal n'empêche pas les autres. »* |
| **Régime** | **R3** — complété par `C11-P01` |
| **Preuve existante** | 6 tests couvrant statut par canal, reprise, traçage, isolation, absence de suppléance |
| **Preuve à produire** | Comportement au démarrage **sans** configuration |
| **Code applicatif** | **`C11-P01`** |

### C11-C08 — ERR-03 · Vérification des documents à l'upload

| | |
|:---|:---|
| **Exigence** | *« Les documents déposés sont vérifiés à l'upload (intégrité, type, taille) avec message d'erreur explicite. »* |
| **Régime** | **R1 — rattachement seul**, **sous GATE-ERR03** |
| **Mécanisme certifié** | Contrôles de type et de taille paramétrables par étape, avec messages explicites |
| **Preuve existante** | **Suite complète** : dépôt conforme accepté · type non autorisé refusé **avec message** · taille dépassée refusée **avec message** |
| **Preuve à produire** | **AUCUNE** pour type et taille — rattachement nominatif |
| **Gate** | **GATE-ERR03** — le volet « intégrité » n'a **aucun mécanisme dédié**. À trancher **avant** certification |
| **Code applicatif** | **AUCUN** |
| **Interdiction** | **Ne jamais inventer une somme de contrôle pour conserver +11.** Si le métier exige un contrôle d'intégrité distinct, **ERR-03 sort de F011** |

### C11-C09 — NFR-01 · Interface en français

| | |
|:---|:---|
| **Exigence** | *« Interface en français ; ergonomie simple adaptée à des utilisateurs non techniciens. »* |
| **Régime** | **R2** |
| **Preuve existante** | **AUCUNE** |
| **Preuve à produire** | Vérification reproductible : libellés des écrans exposés à l'utilisateur en français |
| **Code applicatif** | **AUCUN** |
| **Interdiction** | **Aucun score UX, aucun seuil d'accessibilité contractuel, aucun nombre de clics, aucune métrique non sourcée.** Ne pas modifier les écrans en l'absence d'anomalie démontrée |

### C11-C10 — NFR-02 · Portail responsive

| | |
|:---|:---|
| **Exigence** | *« Portail client responsive, utilisable sur mobile. »* |
| **Régime** | **R2** — preuve **structurelle** |
| **Preuve existante** | **AUCUNE** ; **aucun navigateur automatisé dans le dépôt** |
| **Preuve à produire** | 5 points structurels (directive d'affichage · points de rupture · primitives adaptatives · conteneur défilant pour les contenus larges · navigation et actions atteignables en variante réduite) |
| **Code applicatif** | **AUCUN** |
| **Limite déclarée** | Démontre que **le contrat responsive est appliqué**, non l'absence de défaut visuel. Validation navigateur = **dette de recette** |
| **Interdiction** | **Ne pas introduire de navigateur automatisé.** Aucune largeur ne doit apparaître comme seuil CDC — ni dans le domaine, ni dans la documentation métier |

### C11-C11 — NFR-11 · Horaires de traitement et SLA

| | |
|:---|:---|
| **Exigence** | *« Les services CIT conservent leurs horaires de fonctionnement propres, utilisés notamment dans le calcul des SLA. Un client peut déposer un dossier hors heures ouvrées ; le SLA de traitement démarre selon les règles de la file concernée. »* |
| **Régime** | **R2** |
| **Mécanisme certifié** | Calendrier ouvré paramétrable : heures, jours ouvrés, **jours fériés**, mode « toujours ouvert » par défaut |
| **Preuve existante** | 11 tests sur le calendrier et le moteur SLA |
| **Preuve à produire** | Scénario limite **« dépôt hors heures ouvrées »** — dépôt accepté, décompte selon les règles de la file |
| **Code applicatif** | **AUCUN** |
| **Interdiction** | **Aucun calendrier national, aucun service externe de jours fériés, aucune règle calendaire nouvelle.** Le mécanisme de jours fériés existe déjà — le certifier, pas l'étendre |

---

## 3. Contrat de produit — le seul

### C11-P01 — Validation de la configuration de reprise au démarrage du composant

| Dimension | Contenu |
|:---|:---|
| **Exigence** | **ERR-02** |
| **Acteur** | Exploitant déployant la plateforme |
| **Portée** | **Le composant de reprise uniquement** — pas l'application entière |
| **Comportement attendu** | Au démarrage du composant, si les paramètres de reprise sont absents ou invalides, le démarrage de **ce composant** échoue avec un message désignant **le paramètre manquant** et **la raison** de son caractère obligatoire |
| **Comportement interdit** | Démarrer puis échouer silencieusement à chaque passage · substituer une valeur par défaut · dégrader sans signaler · **faire échouer l'application entière** |
| **Mécanisme** | Validation d'options déclenchée au démarrage, selon le dispositif déjà employé par la plateforme. **La spécification n'impose aucun mécanisme particulier** |
| **Portée du changement** | **Un point unique** dans l'enregistrement des services |
| **Autorisation** | Sans objet (démarrage) |
| **Cloisonnement** | Sans objet |
| **Audit** | Sans objet — l'échec est signalé à l'exploitant, pas journalisé métier |

**Contraintes absolues, opposables à l'implémentation** :

- ❌ aucun nombre de tentatives, aucune cadence, aucune condition d'arrêt **en dur** ;
- ❌ aucune valeur par défaut de production ;
- ❌ **CLAR-NOT-02 n'est pas résolue** — la validation **signale** l'absence, elle ne la **comble** pas ;
- ❌ le moteur de notification F009 **n'est pas modifié**, hormis le point de validation ;
- ❌ aucun fournisseur tiers.

**Ordre de mise en œuvre — contraignant** :

```
1. HARNAIS : fournir explicitement une configuration valide
2. BASELINE : suite complète verte
3. PRODUIT : activer le fail-fast
4. TEST : configuration absente/invalide → échec explicite
5. TEST : configuration valide → composant démarré
6. NON-RÉGRESSION : F009 + suite Web complète
```

> **Règle non négociable** : **le comportement du produit n'est jamais modifié pour accommoder un harnais mal configuré.** Si un test échoue après l'étape 3, le harnais est corrigé — jamais le fail-fast affaibli.

---

## 4. Contrats explicitement absents

| Sujet | Motif |
|:---|:---|
| **SEC-02** — RBAC au niveau des champs | Exclue — PC-05 bloque la spécification. **Aucun moteur de Field-Level Security n'est contractualisé** |
| **SEC-07** — analyse antivirale | Exclue — aucun moteur réel. **Aucun contrat d'antivirus, aucun fournisseur** |
| **SEC-08** — second facteur | Exclue — harnais instable, cause indéterminée. **Aucun contrat touchant l'authentification ou le 2FA** |
| SOC-14, PRI-02, IMP-03, REC-02, REC-03, COM-06 | Feature de remédiation ultérieure |
| REC-SCE-14 E2E, PC-16, PC-18, IA, SMS, WhatsApp, Production Readiness | Hors périmètre |

---

## 5. Synthèse

| | |
|:---|:---|
| Contrats de certification | **11** (C11-C01 → C11-C11) |
| Contrats de produit | **1** (C11-P01) |
| Commandes applicatives nouvelles | **0** |
| Requêtes applicatives nouvelles | **0** |
| Entités, tables, migrations | **0** |
| Écrans nouveaux | **0** |
| Exigences en régime **R1** (rattachement seul) | **2** — SEC-05, ERR-03 |
| Exigences en régime **R2** (complément de preuve) | **8** |
| Exigences en régime **R3** (correction produit) | **1** — ERR-02 |
