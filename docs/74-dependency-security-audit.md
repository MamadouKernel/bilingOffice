# Audit de sécurité des dépendances

> **Document** : `docs/74-dependency-security-audit.md`
> **Établi le** : 2026-09-03
> **Méthode** : `dotnet list package --vulnerable --include-transitive`, exécuté ce jour.

---

## 1. Résultat

| Projet | Paquets vulnérables |
|:---|:---|
| `CIT.Plateforme.Domain` | **0** |
| `CIT.Plateforme.Application` | **0** |
| `CIT.Plateforme.Infrastructure` | **0** |
| `CIT.Plateforme.Web` | **0** |
| Projets de test | **0** |

**Transitives incluses.** Aucune vulnérabilité connue signalée par les sources NuGet configurées.

---

## 2. Surface de dépendance

Le socle est volontairement mince. Les paquets applicatifs sont :

| Paquet | Rôle |
|:---|:---|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Authentification et comptes |
| `Microsoft.EntityFrameworkCore.SqlServer` | Persistance |
| `Microsoft.EntityFrameworkCore.Design` | Outillage de migration |
| `QRCoder` | Génération locale du QR code 2FA |
| `Microsoft.AspNetCore.DataProtection` | Jetons Identity |

**Aucune dépendance réseau applicative** : ni client SMTP, ni SDK de fournisseur, ni client HTTP
tiers. C'est ce qui explique la faible surface — et c'est une propriété à préserver.

`QRCoder` est le seul paquet non-Microsoft. Il a été introduit précisément pour **supprimer** un
appel réseau : le QR code 2FA était auparavant généré par un service tiers, auquel le secret TOTP
était transmis.

---

## 3. Limite de cet audit

| Contrôle | État |
|:---|:---|
| Vulnérabilités connues des sources NuGet configurées | **Vérifié** |
| Paquets dépréciés | Non exécuté séparément — aucun signalement au build |
| Analyse de composition logicielle externe (SCA) | **`NOT_VERIFIED_EXTERNALLY`** |
| Signature et provenance des paquets | `NOT_VERIFIED_EXTERNALLY` |

Un résultat « 0 vulnérable » vaut **à la date de l'exécution et pour les sources configurées**. Il
ne dit rien des vulnérabilités publiées après, ni de celles non encore référencées. Un contrôle
récurrent en intégration continue est la seule réponse durable — à mettre en place avec la chaîne
CI, qui relève de la DSI.

---

## 4. Recommandation

| # | Action | Détenteur |
|---:|:---|:---|
| 1 | Rejouer cet audit à chaque livraison | Projet |
| 2 | Intégrer `--vulnerable` à la chaîne CI | DSI |
| 3 | Conserver la propriété « aucune dépendance réseau applicative » | Projet |

**Aucune action corrective n'est requise à ce jour.**
