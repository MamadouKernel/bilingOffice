# CHECKLIST DES EXIGENCES CLARIFIÉES — FEATURE 017

## Sécurité Granulaire Métier & Rétention Documentaire Réglementaire (ARTCI)

---

### EXIGENCE 1 : SEC-02 (Must) — Contrôle d'Accès par Rôles au Niveau Champ

- [x] **SEC-02.1** : Texte CDC exact préservé (§ 17.1).
- [x] **SEC-02.2** : Matrice RBAC 4 blocs validée (Manifeste, Douane/CD, Données Financières, Chauffeur).
- [x] **SEC-02.3** : Contrôle d'accès opéré côté serveur dans la couche Application / Domain (fail-secure, DEC-017-01).
- [x] **SEC-02.4** : Masquage contextuel effectif sur les réponses d'API REST NFR-08 (DEC-017-02).
- [x] **SEC-02.5** : Masquage contextuel effectif sur les exports CSV / Excel (DEC-017-02).
- [x] **SEC-02.6** : Journalisation d'audit des tentatives d'accès non autorisées aux champs restreints (DEC-017-07).

---

### EXIGENCE 2 : SEC-06 (Must) — Protection des Données Personnelles & Rétention ARTCI

- [x] **SEC-06.1** : Texte CDC exact conforme à la loi ivoirienne ARTCI (Loi n° 2013-450).
- [x] **SEC-06.2** : Rétention décennale (10 ans) des factures et pièces comptables avec scellement d'intégrité (DEC-017-03, DEC-017-04).
- [x] **SEC-06.3** : Rétention quinquennale (5 ans) des dossiers opérationnels à compter de la date de clôture (DEC-017-03, DEC-017-04).
- [x] **SEC-06.4** : Conservation inaltérable pendant 3 ans des journaux d'audit de sécurité (DEC-017-03).
- [x] **SEC-06.5** : Purge automatique des brouillons abandonnés (`ERR-01`) après 30 jours d'inactivité (DEC-017-03, DEC-017-04).
- [x] **SEC-06.6** : Anonymisation irréversible des données à caractère personnel à l'expiration du délai légal (DEC-017-04).
- [x] **SEC-06.7** : Idempotence de la commande de purge et génération d'un certificat d'audit de lot (DEC-017-06).
- [x] **SEC-06.8** : Mécanisme de gel légal (*Legal Hold*) permettant de différer la purge d'un dossier sous contentieux (DEC-017-05).

---

### TRAÇABILITÉ DES CONTRATS & TEST-SPEC

- [x] **TEST-SPEC SEC-CERT-02** : Scénarios nominatifs sur les 4 blocs et profils autorisés / interdits.
- [x] **TEST-SPEC SEC-CERT-06** : Scénarios temporels nominatifs (30j, 3 ans, 5 ans, 10 ans, gel légal, idempotence).
- [x] **CONTRATS C17-01 à C17-05** : Définis, cohérents et traçables vers le code et les tests.
- [x] **ANTI-SCOPE** : Exclusion stricte de NFR-03, NFR-04, NFR-05, NFR-09, NFR-10, ENR-04, IA.
- [x] **INTÉGRITÉ FORENSIQUE** : 0 code, 0 test et 0 migration créés pendant la phase clarify.
