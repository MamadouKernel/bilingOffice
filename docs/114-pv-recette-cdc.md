# Procès-verbal de recette — YABA (§22 du CDC)

> Modèle à remplir et signer. Chaque scénario Must du CDC est relié à son test E2E automatisé
> (`RecSce*`) qui sert de base reproductible. La recette est **réussie** quand chaque scénario a
> été rejoué en environnement de recette, son résultat constaté, daté, et signé.
>
> Critère d'acceptation (CDC §22) : **100 % des scénarios Must en succès**, aucune anomalie
> bloquante ou majeure ouverte, exigences de sécurité vérifiées, documentation livrée.

## En-tête

| Champ | Valeur |
|---|---|
| Version applicative recettée | `__________` (commit) |
| Environnement de recette | `__________` |
| Jeu de données | `__________` |
| Date de campagne | `____ / ____ / 2026` |
| Recetteur métier | `__________` |
| Responsable recette (CIT) | `__________` |

Statuts possibles par scénario : **OK** · **KO** (anomalie bloquante/majeure) · **Réserve** (mineure) · **N/T** (non testé).

## 1. Scénarios fonctionnels (§22)

| # | Scénario CDC | Test auto de référence | Statut | Date | Testeur | Anomalie / réserve | Visa |
|---|---|---|---|---|---|---|---|
| 1 | IMPORT client normal (3 BL : validé / rejet motivé / complément→validation ; facture ; paiement+preuve ; validation paiement ; BS+BADT ; clôture+archivage) | `RecSce01ImportNormalTests` | | | | | |
| 2 | IMPORT client en compte (facture → contrôle doc direct, sans paiement) | `RecSce02ImportEnCompteTests` | | | | | |
| 3 | IMPORT client deposit (facture → lettrage → contrôle doc → clôture) | `RecSce03ImportDepositTests` | | | | | |
| 4 | IMPORT client IES (traitement parallèle Manifeste/Contrôle doc, champs réservés, jointure) | `RecSce04ImportIesTests` | | | | | |
| 5 | EXPORT (4 types de clients, codification Back Office, Draft IN, BEE) | `RecSce05ExportE2ETests` | | | | | |
| 6 | Simulation proforma (multi-conteneurs, majorations, stationnement par tranches, au franc vs Annexe A ; paramètre modifié se répercute) | `RecSce06SimulationE2ETests` | | | | | |
| 7 | Customer Complaint (création par catégorie, instruction, complément, orientation, clôture avec réponse) | `RecSce07CustomerComplaintE2ETests` | | | | | |
| 8 | Enrôlement transporteur (dossier à distance, complément, validation, convocation photo/badge, clôture) | `RecSce08EnrolementTransporteurE2ETests` | | | | | |
| 9 | Prise en charge (arrivée en file → acceptation ; horodatages arrivée/prise en charge/dernière activité exploités) | `RecSce09PriseEnChargeTests` | | | | | |
| 10 | Transfert de file (Contrôle BL → Facturation, motif obligatoire ; conservation n°/dossier/docs/historique ; client notifié ; refus hors matrice) | `RecSce10TransfertTests` | | | | | |
| 11 | Réaffectation entre opérateurs (A → B selon habilitations, tracée) | `RecSce11ReaffectationTests` | | | | | |
| 12 | Temps & SLA (attente/traitement par étape ; temps interne vs client ; SLA suspendu « en attente client » ; seuils 50 % / 80 % / dépassement / escalade) | `RecSce12TempsSlaTests` | | | | | |
| 13 | Supervision (vue temps réel files & charge, alertes, affectation d'un ticket non pris en charge, déblocage) | `RecSce13SupervisionTests` | | | | | |
| 14 | Transverse (notifications e-mail + portail avec lien ; continuité digital/présentiel ; regroupement en dossier ; habilitations ; journal d'audit) | `RecSce14TransverseTests`, `RecSce14ContinuiteGuichetRendezVousE2ETests`, `RecSce14NotificationsE2ETests`, `RecSce14CloisonnementCompteTests` | | | | | |

> Note de périmètre : SMS / WhatsApp exclus (décision DSI). COM-01 est recetté sur e-mail + portail.

## 2. Exigences non fonctionnelles à prouver (§19)

| Réf | Exigence | Preuve attendue | Statut | Date | Visa |
|---|---|---|---|---|---|
| NFR-04 | Temps de réponse < 3 s (écrans courants) | rapport `deploy/mesurer-disponibilite-et-temps-reponse.ps1` (p95) | | | |
| NFR-03/10 | Disponibilité ≥ 99 % / 24-7 hors maintenance | rapport de mesure sur période | | | |
| NFR-06 | Sauvegardes + **restauration testée** | sortie `deploy/sql/sauvegarde-et-test-restauration.sql` | | | |
| SEC-04 | Chiffrement transport + documents | certificat reconnu installé ; `/ready` chiffrement Healthy | | | |
| SEC-07 | Antivirus des dépôts | `/ready` antivirus Healthy sur le serveur | | | |
| SEC-09 | Cloisonnement environnements ; compte applicatif dédié | compte `sa` remplacé (`deploy/sql/creer-compte-applicatif.sql`) | | | |

## 3. Synthèse et décision

| Indicateur | Valeur |
|---|---|
| Scénarios Must en succès | `____ / 14` |
| Anomalies bloquantes ouvertes | `____` |
| Anomalies majeures ouvertes | `____` |
| Réserves mineures | `____` |

**Décision de recette** : ☐ Prononcée sans réserve ☐ Prononcée avec réserves ☐ Refusée

| Rôle | Nom | Date | Signature |
|---|---|---|---|
| Recetteur métier | | | |
| Responsable recette CIT | | | |
| DSI | | | |
