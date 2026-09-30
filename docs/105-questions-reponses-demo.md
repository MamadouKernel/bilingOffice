# Q&R anticipées — Démo plateforme YABA

> Réponses courtes et honnêtes aux objections probables, classées par audience. À garder sous les yeux
> pendant la démo. Renvois : audit sécurité `docs/102`, tenue en charge `docs/103`, script démo `docs/104`.

## Métier / fonctionnel

**« Ça couvre quels processus ? »**
Import & Export facturation, réclamations client (Customer Complaint), Avoir & Exonération, enrôlement transporteurs (chauffeurs/camions), guichet & rendez-vous, supervision, administration, assistant YABA. Le tout piloté par un workflow de tickets typés.

**« Un client peut-il voir les demandes d'un autre client ? »**
Non — cloisonnement strict par société, vérifié à chaque requête par un service d'autorisation central (fail-closed) et couvert par des tests dédiés (`ApiCloisonnementTests`, `SecCert02Cloisonnement`).

**« Qui traite les demandes ? »**
Les agents CIT selon leurs habilitations par file ; les superviseurs affectent/réaffectent et suivent les SLA. Chaque rôle ne voit que son périmètre.

**« L'assistant, c'est de l'IA qui invente ? »**
Non : il répond sur les **vraies données** de la plateforme (état des dossiers, « qu'attend-on de moi ? ») et une base de connaissances administrable, en FR et EN. Aucune donnée inventée.

**« Bilingue ? »**
Oui, FR/EN sur toute l'interface (bascule en direct), parité des libellés vérifiée par test.

**« Notifications ? »**
In-App + Email uniquement (décision produit ; SMS/WhatsApp retirés). E-mails habillés (gabarit HTML de marque). Envoi activable par configuration SMTP.

## Sécurité

**« Avez-vous fait un test d'intrusion ? »**
Oui — statique + dynamique (`docs/102`). Une seule faille réellement exploitable trouvée (injection d'en-tête Host → redirection ouverte), **corrigée et testée**. Tout le reste conforme : CSP à nonce, antiforgery, cloisonnement, en-têtes durcis, aucune ressource externe.

**« Authentification ? »**
Mot de passe + **2FA obligatoire (TOTP)**, verrouillage après 5 échecs, codes de secours. Session révocable immédiatement.

**« Données envoyées à des tiers / CDN ? »**
Aucune. Tout est auto-hébergé (polices, Bootstrap vendorisé) ; un garde-fou de test échoue à la moindre ressource externe. Le QR code 2FA est généré localement.

**« Injection SQL / XSS ? »**
Requêtes paramétrées (EF Core) — pas de SQL concaténé. CSP + encodage systématique côté rendu. Testé en dynamique (payloads réfléchis : 0 reflet, 0 trace SQL).

**« Secrets en clair ? »**
Non — chaîne SQL, comptes SMTP, clés : tous hors dépôt (user-secrets / variables d'environnement). Vérifié.

## Montée en charge / technique

**« Ça tient combien d'utilisateurs ? »**
Réponse honnête : mesuré rapide en local, latence serveur ~18–25 ms (bien sous la cible NFR 3 s). Le vrai plafond dépend de l'infra et n'a pas encore été mesuré par un test de charge dédié (script k6 fourni). C'est du **Blazor Server** : chaque utilisateur = un circuit en mémoire serveur → un nœud tient quelques milliers d'utilisateurs, au-delà on passe en multi-nœuds.

**« Et 10 000 connexions simultanées ? »**
Pas sur un nœud unique — il faut du scale-out. Le nécessaire côté code est **déjà livré** : rate limiting, options de circuit, requêtes filtrées côté SQL (fin des scans de table), et le multi-nœuds (signal temps réel via Redis + clés Data Protection partagées) s'active par configuration. Restent 2 étapes purement infra (sessions collantes, chaîne SQL de prod), documentées dans `docs/103`.

**« Architecture ? »**
.NET 10, Blazor Server, DDD en couches (Domain / Application / Infrastructure / Web), EF Core + SQL Server. Séparation stricte : le métier ne dépend jamais de l'infrastructure.

**« Qualité / tests ? »**
~1 900 tests automatisés (unitaires, intégration, E2E) verts : Domain 253, Application 1001, Infrastructure 192, Web 519. Sécurité et cloisonnement couverts par des suites dédiées.

## Questions pièges — réponses cadrées

**« C'est prêt pour la production ? »**
Fonctionnellement et côté sécurité : oui. Avant une mise à l'échelle à très forte charge, 2 réglages d'infra restent à poser (documentés) et un test de charge sur la cible est recommandé — c'est de l'exploitation, pas du développement.

**« Pourquoi Blazor Server et pas une SPA ? »**
Rendu côté serveur = logique et données jamais exposées au client, surface d'attaque réduite, développement homogène. Le compromis (état en mémoire, besoin de scale-out) est connu et adressé (`docs/103`).

**« Que se passe-t-il si le SMTP tombe ? »**
Un échec d'envoi e-mail ne bloque jamais le métier (traité comme une donnée, jamais une exception) ; la notification In-App reste la source de vérité, et les envois sont ré-essayés.
