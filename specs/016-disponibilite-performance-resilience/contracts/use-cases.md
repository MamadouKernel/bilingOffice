# CAS D'UTILISATION & CONTRATS TECHNIQUES — FEATURE 016

## Disponibilité Opérationnelle, Performance Applicative & Résilience Continue

---

### 1. CAS D'UTILISATION PRINCIPAUX

#### UC-16-01 : Interrogation des Sondes de Santé (Health Checks)
- **Acteur** : Sonde de monitoring système / Load Balancer / Équipe d'exploitation.
- **Scénario Nominal** :
  1. Le système appelle `GET /healthz`.
  2. L'application répond immédiatement `200 OK` (`{"status":"Healthy"}`).
  3. Le système appelle `GET /ready`.
  4. L'application vérifie la connectivité SQL, l'accès au stockage et le moteur antivirus.
  5. Si tous les composants sont sains, l'application répond `200 OK` avec le rapport JSON détaillé.
- **Scénario Dégradé** :
  1. Le stockage ou la base est indisponible.
  2. L'application répond `503 Service Unavailable` avec l'indication du composant défaillant.

#### UC-16-02 : Mesure de Latence et Budget de Temps (< 3s)
- **Acteur** : Client ou Agent navigant sur le portail.
- **Scénario Nominal** :
  1. L'usager demande le chargement d'un écran courant (ex: `/client/dashboard`).
  2. Le middleware démarre la mesure du temps d'exécution serveur.
  3. L'application résout la requête, agrège les données et produit le rendu.
  4. La réponse est émise avec l'en-tête `Server-Timing: total;dur=...` et un temps d'exécution strictement inférieur à 3 000 ms.

#### UC-16-03 : Accessibilité Continue 24/7 et Notification de Maintenance
- **Acteur** : Usager déposant un dossier hors heures ouvrées.
- **Scénario Nominal** :
  1. L'usager se connecte à 22h00 un samedi.
  2. Le portail est accessible normalement.
  3. L'usager soumet sa demande de facturation/dépôt.
  4. La demande est enregistrée avec succès dans la base SQL et le statut est notifié.
  5. Le calcul du SLA de traitement démarre au prochain créneau ouvré (`NFR-11`).

---

### 2. CONTRATS D'INTERFACE

#### C16-01 : Sonde Liveness `/healthz`
- **Méthode** : `GET`
- **Route** : `/healthz`
- **Code retour nominal** : `200 OK`
- **Corps de réponse** : `{"status":"Healthy"}`

#### C16-02 : Sonde Readiness `/ready`
- **Méthode** : `GET`
- **Route** : `/ready`
- **Code retour nominal** : `200 OK`
- **Code retour dégradé** : `503 Service Unavailable`
- **Corps de réponse** :
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0150000",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0050000"
    },
    "storage": {
      "status": "Healthy",
      "duration": "00:00:00.0020000"
    },
    "antivirus": {
      "status": "Healthy",
      "duration": "00:00:00.0080000"
    }
  }
}
```
