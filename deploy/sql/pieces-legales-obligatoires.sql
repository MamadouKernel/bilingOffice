-- Pièces légales de constitution rendues obligatoires sur les lignes DÉJÀ présentes en base.
--
-- Contexte : l'amorçage (ProvisionnementParametrageMetier) ne pose le défaut « obligatoire » que
-- pour les pièces absentes. Les bases antérieures au 05/10/2026 portent RCCM / DFE / CNI_REP en
-- « facultatif ». Ce script les bascule, une fois, en obligatoire.
--
-- Idempotent : ne touche que les lignes encore facultatives ; rejouable sans effet.
-- À exécuter dans SSMS sur la base yaba.

UPDATE dbo.PiecesExigeesCompte
SET Obligatoire = 1
WHERE Code IN ('RCCM', 'DFE', 'CNI_REP')
  AND Obligatoire = 0;

-- Contrôle : doit lister les 3 pièces en Obligatoire = 1.
SELECT Code, Libelle, Obligatoire, EstActif
FROM dbo.PiecesExigeesCompte
WHERE Code IN ('RCCM', 'DFE', 'CNI_REP')
ORDER BY Ordre;
