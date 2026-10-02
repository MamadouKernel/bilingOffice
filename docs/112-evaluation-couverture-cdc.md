# Évaluation de la couverture du cahier des charges YABA — 02/10/2026

## Méthode (à lire avant les chiffres)

- Chaque exigence numérotée du CDC (sections 8 à 21) a été recherchée dans `src/` et `tests/`. Le code porte les identifiants d'exigence en commentaire.
- Les zones douteuses ont été vérifiées en lisant le code : antivirus, canaux mobiles, IA, affectation automatique, horaires ouvrés, 2FA, API.
- Suites de tests sur `main` propre : Domain 286/286, Application 1011/1011, Web 519/519.
- « Implémenté » signifie que le code et des tests automatisés existent. Cela ne veut **pas** dire que la recette métier (§22) a été jouée : aucun PV de recette n'existe à ce jour.

**Légende des statuts**
- ✅ **Implémenté** : code et tests présents.
- 🟡 **Partiel** : une partie du besoin manque, ou la solution est approchée.
- ⛔ **Non fait** : absent, ou retiré délibérément.
- 🏗️ **Hors code** : dépend de l'infrastructure ou d'une décision de la DSI.

## Synthèse par domaine

| § | Domaine | Nb | ✅ | 🟡 | ⛔ | 🏗️ | Remarques |
|---|---|---|---|---|---|---|---|
| 8 | Socle tickets SOC-01→14 | 14 | 14 | | | | Export Excel/PDF présent |
| 8.1 | Cycle de vie CYC-01→07 | 7 | 7 | | | | |
| 8.2 | Messages / notes internes MSG-01→05 | 5 | 5 | | | | Mentions @ incluses |
| 9.1–9.4 | Workflow, prise en charge, transferts, matrice | 14 | 14 | | | | Matrice paramétrable |
| 9.5–9.7 | Temps, SLA, alertes, escalades | 13 | 13 | | | | Calendrier ouvré (SLA-04) |
| 9.8–9.14 | Supervision, disponibilité, affectation, priorités, masse | 21 | 20 | 1 | | | AFF-03 : Round-Robin approché |
| 10 | Comptes et entreprise CPT/ENT | 11 | 11 | | | | 2FA présente, obligatoire pour les agents (middleware) |
| 11.1–11.2 | IMPORT, 4 types de clients, unicité BL | 26 | 26 | | | | |
| 11.3 | EXPORT (Draft IN, BEE) | 4 | 4 | | | | |
| 11.5 | Simulation de proforma SIM-01→11 | 11 | 11 | | | | Annexe A chargée ; import CSV et xlsx |
| 11.6 | Avoir / exonération | 4 | 4 | | | | Circuit Finance à valider (§25) |
| 12 | Customer Complaint | 6 | 6 | | | | |
| 13 | Enrôlement transporteurs | 5 | 4 | | 1 | | ENR-04 : SMS/WhatsApp absents |
| 14 | Notifications COM/NOT | 9 | 8 | 1 | | | COM-01 : e-mail et portail seulement |
| 15 | Continuité digital/présentiel | 4 | 4 | | | | |
| 16 | Assistant IA | 7 | 3 | 3 | 1 | | Pas d'IA générative |
| 17 | Écrans (ACR-01) | 1 | 1 | | | | |
| 18 | Sécurité SEC-01→12 | 12 | 9 | 3 | | | Certificat, antivirus, environnements |
| 19 | Non fonctionnelles NFR-01→11 | 11 | 4 | | | 7 | Preuves sur l'environnement réel |
| 21 | Erreurs et continuité | 4 | 4 | | | | |
| | **Total** | **189** | **172 (91 %)** | **8** | **2** | **7** | Corrigé le 02/10 : la 1re version annonçait 185/167 par erreur d'addition |

## Écarts détaillés (toutes les lignes qui ne sont pas ✅)

| Exigence | Statut | Constat vérifié | Pour clore |
|---|---|---|---|
| COM-01, ENR-04, IA-05 | ⛔/🟡 | Seuls l'e-mail et le portail existent. Le SMS et WhatsApp ont été retirés le 25/09, faute de fournisseur. | Souscrire un fournisseur SMS/WhatsApp (docs/44), puis développer le connecteur. |
| IA-01, IA-02, IA-04 | ✅ sans IA externe (02/10) | Décision du 02/10 : pas d'IA externe. Compréhension tolérante (accents, fautes de frappe, sujet le plus pertinent), orientation par sujets (import, export, réclamation, avoir, enrôlement, rendez-vous) et source citée dans chaque réponse (commit 37cd372). Limite assumée : pas de génération libre. | — |
| IA-07 | ✅ (02/10) | Bouton « Parler à un conseiller » toujours disponible, saisie du numéro de ticket, règle `Assistant:DemandeHumaineAutorisee` (commit 8bb35ef). | — |
| AFF-03 | ✅ (02/10) | Vraie rotation : l'opérateur servi le moins récemment sur la file passe en premier (commit c327306). | — |
| SEC-04 | ✅ code / 🏗️ certificat | Les pièces sont chiffrées au repos en AES-256-GCM ; les pièces historiques restent lisibles ; un health check est ajouté (commit 579aa11). HTTPS et HSTS étaient déjà actifs. Il reste à installer un certificat reconnu sur IIS. | Configurer `Stockage__CleChiffrement` et la sauvegarder au coffre ; certificat (DSI). |
| SEC-07 | ✅ code (02/10) | Correction de l'évaluation initiale : le scanner Defender est bien celui enregistré et il est fail-closed. Seul le health check mentait ; il vérifie désormais la présence réelle de MpCmdRun.exe (commit 0992036). | Consulter `/ready` sur le serveur IIS. |
| SEC-09 | 🟡 → exploitation | Le mot de passe n'a jamais été commité (il n'était que dans une copie locale). Un test bloque désormais tout mot de passe dans une configuration versionnée (commit ccfc672). Le script `deploy/sql/creer-compte-applicatif.sql` remplace `sa`. | Exécuter le script ; changer le mot de passe `sa` ; instance de recette distincte. |
| NFR-03, 04, 10 | 🏗️ outillé (02/10) | Script de mesure `deploy/mesurer-disponibilite-et-temps-reponse.ps1` (CSV, rapport dispo % et p95). | Planifier la tâche ; le rapport constitue la preuve. |
| NFR-06 | 🏗️ outillé (02/10) | `deploy/sql/sauvegarde-et-test-restauration.sql` : sauvegarde, VERIFYONLY, restauration réelle dans une base jetable, comparaison des volumes. | Planifier dans SQL Agent ; conserver la sortie. |
| NFR-05, 09 | 🏗️ | Volumétrie et hébergement : décisions DSI (docs/42-43). Aucun code ne peut les trancher. | DSI. |
| NFR-08 | ✅ (02/10) | Spécification OpenAPI 3.1 publiée : `docs/api/openapi-v1.json` (commit bea3abb). | — |
| §22 Recette | ⛔ | Le cahier de recette v1.4 et les guides sont prêts, mais **aucun scénario n'a été joué ni signé**. | Campagne de recette et PV. |
| §24 Livrables | 🟡 | Conception, MOT, cahier de recette et guides : livrés. PV et documentation d'exploitation validée : manquants. | |

## Incident en cours

Le 02/10/2026, les clients ne peuvent plus se connecter en production. Le diagnostic est en attente du motif enregistré dans `JournalAudit`.

## Conclusion

**Fonctionnellement, environ 90 % du CDC est développé et testé**, dont tout le socle prioritaire Billing Office. Les vrais écarts sont :
1. le SMS/WhatsApp ;
2. l'IA générative ;
3. la sécurité d'exploitation (certificat, `sa`, antivirus à confirmer) ;
4. les preuves en environnement réel, dont la recette signée.

Tant que la recette du §22 n'est pas exécutée, la plateforme ne peut pas être déclarée conforme au CDC.
