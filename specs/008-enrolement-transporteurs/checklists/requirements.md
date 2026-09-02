# Checklist Qualité de la Spécification — Feature 008 (Enrôlement Transporteurs)

> **Document** : `specs/008-enrolement-transporteurs/checklists/requirements.md`
> **Date** : 31 août 2026
> **Portée** : vérifie la qualité et la fidélité de `spec.md` au CDC — ne vérifie pas l'implémentation (aucun code n'existe pour F008).

## 1. Fidélité au périmètre

- [x] ENR-01 reprise avec son texte CDC exact (spec.md §2)
- [x] ENR-02 reprise avec son texte CDC exact (spec.md §2)
- [x] ENR-03 reprise avec son texte CDC exact (spec.md §2)
- [x] ENR-04 reprise avec son texte CDC exact (spec.md §2)
- [x] ENR-05 reprise avec son texte CDC exact (spec.md §2)
- [x] Priorités exactes : 3 Must (ENR-01/02/03), 1 Should (ENR-04), 1 Could (ENR-05) — conformes à `docs/01-matrice-exigences.md`
- [x] Aucune exigence AVE absorbée (vérifié : aucune mention de workflow financier, Direction Financière, ou AVE-0x dans spec.md)
- [x] Aucune exigence COM/NOT absorbée (canal de notification explicitement exclu du périmètre, §11/§22 de spec.md)
- [x] Aucune exigence CON absorbée (continuité présentiel explicitement hors périmètre, §22)
- [x] Aucune exigence IA absorbée
- [x] Aucune exigence SEC/NFR/ERR nouvelle absorbée (cloisonnement §14 réutilise les principes transverses déjà établis, sans en créer de nouveaux)

## 2. REC-SCE-08

- [x] Préconditions reprises exactement ("Liste pièces configurée · Compte Agent Gate activé") — spec.md §19
- [x] Acteurs repris exactement ("Transporteur/Chauffeur · Agent Gate") — spec.md §19
- [x] Étapes reprises fidèlement au texte de `docs/04-matrice-recette.md` ligne 39, sans modification ni ajout
- [x] Couverture ENR-04 explicitement qualifiée de "partielle" (pas directe) — cohérent avec la matrice source
- [x] Absence de couverture ENR-05 constatée et non comblée par une invention

## 3. Acteurs

- [x] Transporteur, Chauffeur, Agent Gate décrits en capacités fonctionnelles uniquement (spec.md §4)
- [x] Aucun rôle Identity technique nommé dans la spec (permissions renvoyées explicitement à `/speckit.plan`)
- [x] Aucun acteur inventé (ex. "Finance", "Direction Enrôlement") au-delà de ceux du CDC

## 4. Documents

- [x] Aucune pièce nommée en dur — liste explicitement qualifiée "configurable" (spec.md §7, cohérent avec ENR-01 "paramétrable")
- [x] PC-08 référencé et reclassé avec justification (précédent F001/F003/F004/F006), pas ignoré ni traité comme bloquant sans nuance

## 5. Badge

- [x] Aucune technologie de badge inventée (RFID/QR/carte/API) — spec.md §12 limite explicitement le badge à un événement de remise constaté
- [x] Limite du texte CDC actée explicitement (CLAR-ENR-05), pas comblée par une supposition

## 6. Notifications

- [x] Aucune implémentation de canal SMS/WhatsApp dans le périmètre F008 (spec.md §11, §22)
- [x] Besoin futur documenté (événement de convocation traçable) sans anticiper son implémentation

## 7. Ambiguïté Transporteur/Chauffeur

- [x] Ambiguïté résolue lors de `/speckit.clarify` (31/08/2026) : Option A retenue — Transporteur et Chauffeur désignent la même personne physique, sourcée par `docs/00-analyse-cdc.md` §4.1 ("Personne physique") et par l'absence totale, dans les 5 documents sources, de toute mention d'une société transporteur ou d'une relation 1-N chauffeurs↔transporteur
- [x] Spec mise à jour en conséquence (§4, §6, §14, §18) — aucun champ "société transporteur" ni relation chauffeurs↔société n'est modélisé
- [x] Reclassée "RÉSOLUE" dans §24 — ne bloque plus `/speckit.plan` (seule la représentation technique, ex. nommage des classes, y reste à fixer)

## 8. Rejet (point clarifié)

- [x] Absence de mécanisme de rejet dans le texte ENR-02 confirmée par recherche exhaustive et négative ("rejet"/"refus" : 0 occurrence à proximité d'Enrôlement/Gate/badge/transporteur/chauffeur dans `docs/00-analyse-cdc.md`)
- [x] Aucun rejet inventé dans le workflow (§5), les User Stories (§20) ou les critères d'acceptation (§21)
- [x] Point résolu par application de la règle par défaut de l'instruction de clarification : "le rejet définitif n'appartient pas au périmètre fonctionnel F008 tant qu'aucune exigence du CDC ne le prévoit explicitement" — reclassé "RÉSOLUE" dans §24

## 9. Testabilité des Must

- [x] ENR-01 : critère nominal + erreur + autorisation + cloisonnement (spec.md §21)
- [x] ENR-02 : critère nominal + complément + autorisation + cloisonnement (spec.md §21)
- [x] ENR-03 : critère nominal + autorisation (spec.md §21)
- [x] ENR-04/ENR-05 (Should/Could) explicitement non élevées au rang de Must — aucun critère Given/When/Then Must écrit pour elles

## 10. Réutilisation F001/F002

- [x] Matrice de réutilisation produite (spec.md §18), aucun doublon fonctionnel proposé (Ticket, Dossier, Files, Documents, Messages, Audit, SLA, Comptes, Habilitations tous réutilisés conceptuellement)
- [x] Seule extension identifiée : modélisation Société/Transporteur potentielle (F002), explicitement différée à `/speckit.plan`

## 11. Couverture CDC

- [x] Baseline actuelle (131/188) rappelée sans confusion avec la projection
- [x] Projection post-F008 (136/188, ≈72,3 %) explicitement qualifiée de non acquise (spec.md en-tête)

## 12. Matrice de couverture

- [x] Les 5 exigences ENR sont toutes rattachées à au moins une User Story (spec.md §23)
- [x] Aucune exigence orpheline

## Statut global

**PASS** — 12/12 catégories vérifiées, aucune case non cochée. Mise à jour du 31 août 2026 (post-`/speckit.clarify`) : les deux points laissés ouverts à la spécification (CLAR-ENR-01, CLAR-ENR-04) sont désormais **RÉSOLUS** par lecture directe du CDC, de même que CLAR-ENR-03. La spec est **READY FOR `/speckit.plan`** — aucun point bloquant résiduel.
