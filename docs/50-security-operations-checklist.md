# Checklist de sécurité opérationnelle

> **Document** : `docs/50-security-operations-checklist.md`
> **Établi le** : 2026-09-02, sur le commit `d80a126`
> **Portée** : ce qui relève de l'exploitation. L'audit du code figure dans `docs/35`.

---

## 1. Comptes et privilèges

| # | Contrôle | État applicatif | Reste à faire |
|---:|:---|:---|:---|
| 1.1 | Aucun rôle métier codé en dur | **Conforme** — habilitations paramétrables | — |
| 1.2 | Moindre privilège appliqué aux comptes agents | Possible par construction | Revue périodique — `TBD_DSI` |
| 1.3 | Séparation administrateur fonctionnel / technique | **Prévue par le CDC § 6** | À appliquer en production |
| 1.4 | Compte d'amorçage désactivé après usage | — | **À faire au déploiement** |
| 1.5 | Revue périodique des habilitations | — | Fréquence — `TBD_DSI` |
| 1.6 | Comptes de service nominatifs et dédiés | — | `TBD_INFRA` |

**Point d'attention § 1.3.** Le CDC sépare délibérément les deux profils d'administration : le
**journal d'audit** relève du technique, non du fonctionnel. Un compte unique cumulant les deux
serait à la fois gestionnaire des droits et gardien du registre qui trace ses propres actions.
Cette séparation doit être tenue en production, quelles que soient les commodités de
développement.

**Point d'attention § 1.4.** Le provisionnement du premier administrateur est **inerte dès qu'un
administrateur existe**. Le secret de configuration doit néanmoins être retiré une fois d'autres
administrateurs désignés — un secret inutile qui subsiste finit par fuir.

---

## 2. Authentification

| # | Contrôle | État | Reste à faire |
|---:|:---|:---|:---|
| 2.1 | Mot de passe — longueur minimale 10 | **Conforme** | — |
| 2.2 | Verrouillage après 5 tentatives, 15 minutes | **Conforme** | — |
| 2.3 | Second facteur disponible | **Conforme** — optionnel par utilisateur | Imposer aux administrateurs ? `TBD_DSI` |
| 2.4 | Récupération de mot de passe en autonomie | **Conforme** | — |
| 2.5 | Expiration de session — 30 min glissantes | **Conforme** | Durée à confirmer — `TBD_DSI` |
| 2.6 | Message de refus générique | **Conforme** | — |

**Recommandation § 2.3.** Le second facteur est optionnel, conformément au CDC. Pour les comptes
détenant `administration.comptes` ou `audit.consulter`, l'imposer serait cohérent avec le niveau
de privilège — c'est un arbitrage, non une exigence contractuelle.

---

## 3. Transport et en-têtes

| # | Contrôle | État |
|---:|:---|:---|
| 3.1 | Redirection HTTPS | **Conforme** |
| 3.2 | HSTS hors développement | **Conforme** |
| 3.3 | `X-Content-Type-Options` | **Conforme** |
| 3.4 | `X-Frame-Options` | **Conforme** |
| 3.5 | `Referrer-Policy` | **Conforme** |
| 3.6 | `Content-Security-Policy` | **Conforme** |
| 3.7 | Version TLS minimale | `TBD_INFRA` |
| 3.8 | Validité et renouvellement du certificat | `TBD_INFRA` |

---

## 4. Autorisation

| # | Contrôle | État | Reste à faire |
|---:|:---|:---|:---|
| 4.1 | Vérification serveur systématique | **Conforme** | — |
| 4.2 | API authentifiée | **Conforme** | — |
| 4.3 | Cloisonnement par société | **Conforme** | Vérifier en recette — S-10 |
| 4.4 | Politique d'autorisation de repli | **NON CONFORME** | Correction proposée — `docs/35 § 3.1` |
| 4.5 | Antiforgery | **Conforme** | — |

---

## 5. Fichiers et antivirus

| # | Contrôle | État | Reste à faire |
|---:|:---|:---|:---|
| 5.1 | Extensions autorisées, paramétrables | **Conforme** | Valeurs à valider métier |
| 5.2 | Taille maximale | **Conforme** | Valeur à valider |
| 5.3 | Analyse antivirus avant mise à disposition | **Conforme** | Vérifier en environnement |
| 5.4 | Document non sain inaccessible | **Conforme** | Recette S-11 |
| 5.5 | Antivirus opérationnel sur la cible | — | `TBD_INFRA` |
| 5.6 | Permissions restreintes sur le stockage | — | `TBD_INFRA` |

---

## 6. Journalisation et audit

| # | Contrôle | État | Reste à faire |
|---:|:---|:---|:---|
| 6.1 | Journal d'audit en ajout seul | **Conforme** — garde à l'unité de travail | — |
| 6.2 | Actions sensibles tracées | **Conforme** | — |
| 6.3 | Aucune donnée personnelle ni secret dans les journaux applicatifs | **Conforme** — contrôle statique | Revérifier après tout ajout |
| 6.4 | Rétention des journaux | — | `TBD_DSI` |
| 6.5 | Protection des journaux contre l'altération | — | `TBD_INFRA` |
| 6.6 | Consultation du journal réservée | **Conforme** — `audit.consulter` | — |

---

## 7. Secrets

| # | Contrôle | État | Reste à faire |
|---:|:---|:---|:---|
| 7.1 | Aucun secret dans le dépôt | **Conforme** — 0 occurrence | — |
| 7.2 | Chaîne de connexion hors dépôt | **Conforme** | — |
| 7.3 | Mécanisme de conservation en production | — | `TBD_DSI` — PC-13 |
| 7.4 | Rotation | — | `TBD_DSI` |
| 7.5 | Accès aux secrets tracé | — | `TBD_INFRA` |

---

## 8. Base de données

| # | Contrôle | Reste à faire |
|---:|:---|:---|
| 8.1 | Compte applicatif dédié, **non `sa`** | `TBD_INFRA` — **recommandé** |
| 8.2 | Droits limités à la base concernée | `TBD_INFRA` |
| 8.3 | Droits DDL retirés hors migration | `TBD_INFRA` |
| 8.4 | Chiffrement au repos | `TBD_DSI` |
| 8.5 | Chiffrement en transit | `TBD_INFRA` |

**§ 8.1 et 8.3.** Une application qui se connecte avec un compte d'administration d'instance rend
toute injection ou fuite de configuration bien plus grave qu'elle ne devrait l'être. Les droits de
modification de schéma ne sont nécessaires **que pendant la migration**.

---

## 9. Système et réseau

| # | Contrôle | Reste à faire |
|---:|:---|:---|
| 9.1 | Correctifs système | `TBD_INFRA` |
| 9.2 | Segmentation réseau | `TBD_INFRA` |
| 9.3 | Règles de pare-feu minimales | `TBD_INFRA` |
| 9.4 | Accès administrateur restreint et tracé | `TBD_INFRA` |
| 9.5 | Antivirus serveur | `TBD_INFRA` |

---

## 10. Revues périodiques

| Revue | Fréquence | Responsable |
|:---|:---|:---|
| Habilitations des comptes agents | `TBD_DSI` | Administrateur fonctionnel |
| Comptes inactifs | `TBD_DSI` | Administrateur fonctionnel |
| Journal d'audit — actions sensibles | `TBD_DSI` | Administrateur technique |
| Rotation des secrets | `TBD_DSI` | Exploitation |
| Dépendances vulnérables | **Recommandé : à chaque livraison** | Développement |
| Test de restauration | `TBD_DSI` | Exploitation |

**Sur les dépendances.** `dotnet list package --vulnerable` n'a pas été exécuté dans cet audit —
il suppose une base de vulnérabilités à jour et un accès réseau. À intégrer à la chaîne de
livraison.

---

## Synthèse

| Catégorie | Conforme | Non conforme | `TBD` |
|:---|---:|---:|---:|
| Comptes et privilèges | 2 | 0 | 4 |
| Authentification | 6 | 0 | 2 |
| Transport | 6 | 0 | 2 |
| Autorisation | 4 | **1** | 0 |
| Fichiers | 4 | 0 | 2 |
| Journalisation | 4 | 0 | 2 |
| Secrets | 2 | 0 | 3 |
| Base | 0 | 0 | 5 |
| Système | 0 | 0 | 5 |

**Un seul écart applicatif** : la politique d'autorisation de repli (§ 4.4). Tout le reste des
lignes ouvertes relève de l'infrastructure et de l'exploitation.
