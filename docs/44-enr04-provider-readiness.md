# Notification du chauffeur — ENR-04 : préparation fournisseur

> **Document** : `docs/44-enr04-provider-readiness.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Statut** : `BLOCKED_PROVIDER` — `PC-16` OPEN. **Aucun fournisseur retenu, aucun code écrit.**

---

## 1. Ce que le CDC exige

> **ENR-04 (Should)** — « Notification au chauffeur (**SMS/WhatsApp prioritaires**) à chaque étape
> clé [...] »

Deux canaux sont **nommés** et qualifiés de prioritaires. C'est inhabituel dans ce CDC, qui reste
généralement neutre sur les canaux — ici, l'intention est explicite : le chauffeur n'est pas
supposé consulter un portail.

---

## 2. Ce qui existe déjà

Le socle de notification livré en F009 sépare l'**événement** du **canal**. Concrètement :

| Élément | État |
|:---|:---|
| Publication d'événements métier | Livrée |
| Résolution des destinataires | Livrée |
| Règles de diffusion paramétrables par canal | Livrées |
| Modèles de message administrables | Livrés |
| Canal In-App | **Opérationnel** |
| Canal e-mail | Déclaré, **aucun fournisseur raccordé** |
| Canal SMS | **À raccorder** |
| Canal WhatsApp | **À raccorder** |

**Conséquence importante.** Le raccordement d'un fournisseur ne demande **aucune refonte** : la
règle de diffusion existe déjà, il manque l'adaptateur qui transmet effectivement le message.

---

## 3. Interface cible — conceptuelle

L'architecture existante suggère une abstraction par canal, cohérente avec les ports déjà en
place. **Aucune de ces interfaces n'est écrite** — elles décrivent la forme attendue, pas une
implémentation.

```
ICanalNotification                     ← abstraction existante du socle
   ├── CanalInApp                      ← livré
   ├── CanalSms         (à écrire)     ← adaptateur fournisseur
   └── CanalWhatsApp    (à écrire)     ← adaptateur fournisseur
```

Ce qu'un adaptateur devra porter, indépendamment du fournisseur :

| Responsabilité | Pourquoi |
|:---|:---|
| Envoi d'un message à un numéro | Fonction de base |
| Gestion des échecs et des reprises | Un SMS non délivré ne doit pas perdre l'événement |
| Délai d'attente borné | Un fournisseur lent ne doit pas bloquer le traitement |
| Journalisation de l'envoi | Traçabilité — sans le contenu, qui est une donnée personnelle |
| Accusé de réception, si disponible | Preuve de notification |
| Repli | Si le canal échoue, l'In-App reste |

**Aucun code fournisseur ne doit être écrit avant `PC-16`** : l'API, l'authentification et le
modèle de message diffèrent radicalement d'un fournisseur à l'autre. Écrire l'adaptateur
maintenant reviendrait à le réécrire ensuite.

---

## 4. Informations à recueillir

| # | Information | Pourquoi elle est nécessaire |
|---:|:---|:---|
| 1 | Fournisseur retenu — SMS | Détermine l'API et l'authentification |
| 2 | Fournisseur retenu — WhatsApp | Souvent distinct du SMS |
| 3 | Contrat signé ? | Sans contrat, pas d'accès de production |
| 4 | Point d'entrée de l'API | Configuration |
| 5 | Mode d'authentification | Clé, jeton, certificat |
| 6 | Environnement de test disponible ? | **Indispensable** pour la recette |
| 7 | Identifiant d'expéditeur | Souvent soumis à enregistrement préalable |
| 8 | Compte WhatsApp Business validé ? | Prérequis du canal |
| 9 | Modèles de message approuvés | WhatsApp impose des modèles pré-validés |
| 10 | Quotas et limites de débit | Dimensionne les reprises |
| 11 | Coût unitaire et plafond | Un envoi en boucle coûte de l'argent réel |
| 12 | Engagement de service | Conditionne le repli |
| 13 | Politique de reprise | Combien de tentatives, à quel rythme |
| 14 | Rappel d'état de livraison | Preuve de notification |
| 15 | Localisation des données | Conformité ARTCI et RGPD |
| 16 | Contrat de sous-traitance | Le numéro de téléphone est une donnée personnelle |
| 17 | Mécanisme de conservation des secrets | Lié à `PC-13` |

**Le point 9 est souvent sous-estimé.** WhatsApp Business n'autorise pas l'envoi de texte libre
hors fenêtre de conversation : les messages doivent utiliser des **modèles approuvés au
préalable**. Cela contraint la formulation des convocations, et le délai d'approbation se compte
en jours.

**Le point 6 est bloquant pour la recette.** Sans environnement de test, le scénario UAT
correspondant ne peut être joué qu'en envoyant de vrais messages à de vrais numéros.

---

## 5. Ce qui reste faisable sans fournisseur

| Action | Faisable ? |
|:---|:---|
| Définir les événements déclencheurs de notification chauffeur | **Oui** — relève du métier |
| Rédiger les textes des messages | **Oui**, sous réserve d'approbation WhatsApp |
| Paramétrer les règles de diffusion | **Oui** — l'écran existe |
| Écrire l'adaptateur | **Non** — dépend du fournisseur |
| Recetter le canal | **Non** — nécessite un environnement de test |

---

## 6. Verdict

**ENR-04 : `BLOCKED_PROVIDER` — `PC-16` OPEN.**

Le blocage est **contractuel et commercial**, pas technique. Le socle est prêt ; il manque un
contrat, des identifiants et un environnement de test.

Estimation de l'effort une fois le fournisseur connu : l'écriture d'un adaptateur et sa recette.
**Cette estimation ne vaut que pour un fournisseur disposant d'une API REST documentée** — elle ne
couvre ni les délais d'enregistrement de l'expéditeur, ni l'approbation des modèles WhatsApp, qui
échappent au projet.
