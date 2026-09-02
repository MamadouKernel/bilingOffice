# Checklist Qualité de la Spécification — Feature 012 (Remédiation Dettes Métier & Recette Transverse)

> **Document** : `specs/012-remediation-dettes-recette-transverse/checklists/requirements.md`  
> **Feature** : F012 — Remédiation Dettes Métier, Recette Transverse & Clôture Réserves  
> **Date** : 1er septembre 2026  
> **Portée** : Contrôle qualité, traçabilité et validation des clarifications de `spec.md`.

---

## 1. Périmètre — 7 Exigences + 1 Recette Transverse

- [x] **PRI-02 présente** avec son texte CDC exact (§2)
- [x] **IMP-03 présente** avec son texte CDC exact (§2)
- [x] **REC-02 présente** avec son texte CDC exact (§2)
- [x] **REC-03 présente** avec son texte CDC exact (§2)
- [x] **SEC-08 présente** avec son texte CDC exact (§2)
- [x] **SOC-14 présente** avec son texte CDC exact (§2)
- [x] **COM-06 présente** avec son texte CDC exact (§2)
- [x] **REC-SCE-14 présente** en tant que scénario de recette transverse (§2)
- [x] **Total d'exigences CDC = 7** (4 Must, 1 Should, 2 Could)
- [x] REC-SCE-14 est explicitement comptée avec **Gain CDC = +0** (preuve transverse)

---

## 2. Clarifications Spécifiques Intégrées

- [x] **PRI-02 (CLAR-01)** : Déduction automatique à la création et ordonnancement dans `ConsulterFileQuery` clarifiés. Aucune matrice complexe inventée.
- [x] **IMP-03 (CLAR-02)** : Événement `TicketCree` et génération d'envoi e-mail via F009 clarifiés. Fournisseur SMTP réel classé hors périmètre sans bloquer.
- [x] **REC-02 (CLAR-03)** : Qualifiée en **Certification seule** (code et IHM déjà 100% en place). Zéro nouveau code produit requis.
- [x] **REC-03 (CLAR-04)** : Qualifiée en **Certification seule** (routage Support Opérationnel et instruction déjà en place).
- [x] **SEC-08 (CLAR-05)** : Diagnostiquée comme **défaut de harnais de test** (isolation de collection xUnit / LocalDB). Critère de 10 runs consécutifs stables formalisé.
- [x] **SOC-14 (CLAR-06)** : Export tabulaire CSV délimité : colonnes standard, encodage UTF-8 avec BOM, respect strict du cloisonnement CPT-04. Moteur BI exclu.
- [x] **COM-06 (CLAR-07)** : Relances automatiques multicanales paramétrables sur InApp et Email. SMS/WhatsApp exclus tant que PC-16 est ouvert.
- [x] **REC-SCE-14 (CLAR-08)** : Les 7 volets contractuels du CDC sont détaillés pour l'E2E sans découpage arbitraire en "14 étapes".

---

## 3. Exclusions Formelles et Anti-Scope

- [x] **SEC-02 formellement exclue** (bloquée par PC-05, aucun champ inventé)
- [x] **SEC-07 formellement exclue** (aucun fournisseur antivirus réel)
- [x] **IA-01 à IA-07 formellement exclues** (bloquées par PC-18)
- [x] **ERR-01 formellement exclue** (réservée pour la Feature 013 dédiée aux brouillons)
- [x] **SMS/WhatsApp réels exclus** (bloqués par PC-16)
- [x] **Production Readiness / Hébergement exclus** (SEC-04, SEC-06, SEC-09, NFR-03→10)
- [x] **Aucune refonte UI globale** ni moteur BI complexe dans le périmètre

---

## 4. Contraintes Architecturales

- [x] **0 migration Entity Framework** : aucune table, colonne ou index nouveau requis
- [x] **0 nouvelle entité métier dans Domain** : réutilisation exhaustive des entités existantes
- [x] **0 fournisseur externe** (Twilio, SendGrid, SMTP externe) requis pour valider les tests
- [x] **Cloisonnement multi-tenant absolu (CPT-04 / SEC-03)** respecté pour l'export CSV et les notifications

---

## 5. Fiches TEST-SPEC et Scénarios

- [x] **PRI-CERT-01** défini pour PRI-02
- [x] **IMP-CERT-03** défini pour IMP-03
- [x] **REC-CERT-02** défini pour REC-02
- [x] **REC-CERT-03** défini pour REC-03
- [x] **SEC-CERT-08** défini pour SEC-08 (avec exigence de 10 runs consécutifs stables)
- [x] **SOC-CERT-14** défini pour SOC-14
- [x] **COM-CERT-06** défini pour COM-06
- [x] **REC-SCE-14-E2E** défini pour le scénario transverse couvrant les 7 volets contractuels

---

## 6. Traçabilité et Métriques de Couverture

- [x] Baseline de départ fixée à **157 / 188 (83,51 %)**
- [x] Gain strict visé : **+7 exigences**
- [x] Baseline cible projetée : **164 / 188 (87,23 %)** (mentionnée comme non acquise avant Closure Review)
- [x] Tableau de traçabilité liant chaque exigence à sa classe de test et son statut

---

## 7. Definition of Done (DoD)

- [x] Critères de succès clairs, mesurables et vérifiables sans ambiguïté
- [x] Exigence de 100% de tests verts (858 existants + nouveaux tests)
- [x] 0 avertissement compilateur et 0 pending model change exigés
