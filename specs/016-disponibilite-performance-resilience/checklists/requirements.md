# CHECKLIST DES EXIGENCES & CONFORMITÉ CONTRACTUELLE — FEATURE 016

## Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

---

### 1. ALIGNEMENT CONTRACTUEL DU CAHIER DES CHARGES

- [x] **NFR-03** : Texte exact respecté (*« Disponibilité cible en heures ouvrées ≥ 99 % ; maintenance planifiée hors heures de pointe »*).
- [x] **Seuil NFR-03** : Seuil exact de ≥ 99 % en heures ouvrées (et non 99.9%).
- [x] **Sondes NFR-03** : Endpoints `/healthz` et `/ready` spécifiés sans confondre la présence d'une sonde avec la preuve du SLA 99%.
- [x] **NFR-04** : Texte exact respecté (*« Temps de réponse < 3 secondes pour les écrans courants dans des conditions normales de charge »*).
- [x] **Seuil NFR-04** : Seuil exact de < 3 secondes (et non < 2 secondes).
- [x] **Écrans courants NFR-04** : Périmètre défini sur les parcours nominaux (connexion, dashboards, consultation tickets, API).
- [x] **NFR-10** : Texte exact respecté (*« La plateforme est accessible 24h/24 et 7j/7, hors périodes de maintenance planifiées »*).
- [x] **Souveraineté & Autonomie** : 0 dépendance cloud ou externe imposée.

---

### 2. TRAÇABILITÉ DES CONTRATS & TEST-SPEC

- [x] **`C16-01` / `NFR-CERT-03-01`** : Sondes Health Checks ASP.NET Core (`/healthz` Liveness, `/ready` Readiness SQL & Storage).
- [x] **`C16-02` / `NFR-CERT-03-02`** : Formule de calcul de disponibilité sur heures ouvrées et fenêtres de maintenance.
- [x] **`C16-03` / `NFR-CERT-04-01`** : Middleware de métrologie et diagnostic de latence HTTP.
- [x] **`C16-04` / `NFR-CERT-04-02`** : Benchmarks automatisés prouvant que les écrans courants s'exécutent en < 3 secondes.
- [x] **`C16-05` / `NFR-CERT-10-01`** : Dépôts autorisés 24/7 et gestion transparente du statut de maintenance.

---

### 3. ANTI-SCOPE STRICT

- [x] **NFR-05 (PC-01)** : Cadrage de la volumétrie formellement exclu de F016.
- [x] **NFR-06** : Sauvegardes et restauration d'exploitation formellement exclues de F016.
- [x] **NFR-09 (PC-13)** : Hébergement Groupe AGL formellement exclu de F016.
- [x] **SEC-02, SEC-06, ENR-04, IA-01..07** : Formellement exclus de F016.
- [x] **Schéma de Données** : 0 migration relationnelle, 0 nouvelle entité, 0 table SQL.
