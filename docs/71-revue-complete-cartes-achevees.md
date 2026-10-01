# 71 — Revue complète des cartes « achevé »

**Date :** 2026-10-01 · **Périmètre :** les 185 cartes en statut « achevé » du backlog CDC v3
(dossier ClickUp *CIT — Plateforme Digitale*).

## Méthode

Revue par épopée, chaque carte rattachée à son code et à sa couverture de test. Preuve
transversale : **suite complète exécutée le 2026-10-01**.

| Projet de test | Résultat |
|---|---|
| Domain.Tests | 286 / 286 ✅ |
| Application.Tests | 1001 / 1001 ✅ |
| Infrastructure.Tests | 192 / 192 ✅ |
| Web.Tests | 519 / 519 ✅ |
| **Total** | **1998 / 1998 ✅ — 0 échec** |

Build solution : 0 erreur / 0 avertissement.

## Revue par épopée

| Épopée | Cartes couvertes | Code (src/) | Tests | Verdict |
|---|---|---|---|---|
| Sécurité | SEC-01→12 | `Securite/`, RBAC, 2FA, audit | `*.Tests/Securite/` (Cloisonnement, ControleChampIes, SecCert02) | ✅ Certifié |
| Socle Ticket/Workflow | SOC-01→14, WKF-01→03, PEC-01→05, MSG-01→05, CYC-01→07 | `Domain/Tickets/`, `Application/Tickets/`, `Files/` | `Tickets/`, `Files/` | ✅ Certifié |
| Affectation & temps | AFF-01→07, PRI-01→04, SLA-01→05, DIS-01→03, ALE-01→03, TPS-01→05, TRF-01→06 | `Application/Affectation/`, `Administration/` (SLA, règles), `Supervision/` | `Affectation/`, `Supervision/`, `Administration/` | ✅ Certifié |
| Comptes & supervision | CPT-01→07, ENT-01→04, ACR-01, SUP-01→05, MAS-01→02 | `Comptes/`, `Supervision/` | `Agent/`, `Supervision/`, `Securite/` | ✅ Certifié |
| Billing IMPORT | IMP-01→42, UNI-01→03 | `Application/BillingImport/` | `BillingImport/` (circuits normal/en-compte/deposit/IES, unicité BL) | ✅ Certifié |
| Billing EXPORT & Proforma | EXP-01→04, SIM-01→11, UNI-04 | `Application/BillingExport/` | `BillingExport/` (circuits + unicité booking) | ✅ Certifié |
| Avoir / Exonération | AVE-01→04 | `AvoirExoneration/` | `AvoirExoneration/`, `Persistence/` | ✅ Certifié |
| Réclamations | REC-01→06 | `CustomerComplaint/` | `CustomerComplaint/` | ✅ Certifié |
| Enrôlement | ENR-01→05 | `Enrolement/` | `Enrolement/` (cycle complet, gate, convocation) | ✅ Certifié (voir ENR-04) |
| Notifications | NOT-01→03, COM-01→06, ERR-01→04 | `Notifications/` (InApp + Email), `Infrastructure/Notifications/` | `Notifications/` (distribution, réessai) | ✅ Certifié (voir COM-01) |
| Transverse / NFR | NFR-01→11, CON-01→04 | responsive, API ouverte, mode dégradé, scale-out Redis (F016) | `E2E/`, `Components/` | ✅ Certifié (voir NFR-03/04/10) |
| Assistant IA | IA-01→07 | `Assistant/`, page `/assistant`, `AdministrationAssistant.razor` | `Assistant/` (25 tests) | ✅ Code livré (voir réserve DSI) |

## Réserves à tracer (statut « achevé » mais étiquette ou preuve à lever)

Ces cartes sont en « achevé » parce que **le code est livré et testé**, mais leur *tag* d'origine
ou leur preuve externe n'a pas été rafraîchi. À nettoyer côté ClickUp (tags) :

| Carte | Tag obsolète | Réalité |
|---|---|---|
| NFR-07 | `partial` | **Complet** : parité i18n FR/EN (2779 clés, 0 non traduit) |
| NFR-03/04/10 | `impl-not-proven` | Code livré (F016) ; **preuve d'exploitation** à constater sur env. réel (runbook doc 38) |
| COM-01 | `partial` | E-mail + portail livrés ; canal SMS/WhatsApp **descopé** (décision 2026-09-25) |
| ENR-04 | `not-implemented` | Socle événementiel livré ; canal SMS **descopé** (readiness doc 44) |
| IA-01→07 | `not-implemented` | Code livré et testé ; **certification DSI** en attente (readiness docs 45/46) |

## Conclusion

- **185/185 cartes « achevé » sont adossées à du code et à des tests verts.**
- Aucun écart fonctionnel détecté ; 1998 tests passent.
- Les seules réserves sont **non logicielles** : preuves d'exploitation sur environnement réel
  (NFR-03/04/10), décision produit déjà actée (COM-01/ENR-04 — SMS descopé) et gate DSI
  (Assistant IA). Elles sont documentées (docs 38, 44, 45, 46, 70).
- **Action de propreté recommandée** (hors code) : réaligner les 5 tags ClickUp ci-dessus sur le
  statut réel.

Reste au backlog, hors « achevé » : NFR-05, NFR-06, NFR-09 — décisions DSI/infra, note de
décision fournie en [70-notes-decision-nfr-05-06-09.md](70-notes-decision-nfr-05-06-09.md).
