using Xunit;

// PlateformeWebApplicationFactory pointe systématiquement vers la même base LocalDB physique
// persistante (CitPlateformeTestsWeb), partagée par toutes les classes de test de cet assembly quelle
// que soit l'instance de fixture. Le seed de données de référence par nom (ex. FileDeTraitement
// « Pilote Manifeste ») suit un motif vérifier-puis-insérer non atomique : exécuté en parallèle par
// deux classes de test différentes (comportement xUnit par défaut), il produit une insertion en
// double détectée plus tard par un SingleOrDefault. Désactiver la parallélisation au niveau de
// l'assembly est la même protection que le verrou déjà utilisé pour la création du schéma
// (PlateformeWebApplicationFactory.VerrouCreation), étendue au seed de données.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
