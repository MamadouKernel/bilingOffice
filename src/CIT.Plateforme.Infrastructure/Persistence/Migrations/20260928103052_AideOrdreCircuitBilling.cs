using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Fiches d'aide Billing Import/Export : l'ordre du circuit y était faux (contrôle documentaire
    /// placé avant la facturation). Le nouveau texte est celui du contenu de départ corrigé.
    ///
    /// Une fiche n'est réécrite QUE si elle contient encore la phrase erronée : une fiche déjà
    /// corrigée par l'administration n'est pas touchée. Sur une base neuve, la table est vide à ce
    /// stade (le provisionnement s'exécute après les migrations) : la migration ne fait rien.
    /// </summary>
    public partial class AideOrdreCircuitBilling : Migration
    {
        /// <summary>Route de la fiche → fragment de l'ancien texte erroné (français).</summary>
        private static readonly (string Route, string AncienFragment)[] Fiches =
        [
            ("/billing-import/soumettre", "le Contrôle documentaire, la Facturation"),
            ("/billing-export/soumettre", "Back Office, Contrôle documentaire, Facturation"),
            ("/billing-import/manifeste", "le dossier passe au Contrôle documentaire"),
            ("/billing-import/controle-documentaire", "Validez le contrôle"),
            ("/billing-import/facturation", "passe à la validation du paiement"),
            ("/billing-import/validation-paiement", "le dossier passe en comptabilité"),
            ("/billing-import/comptabilite-client", "Dernière étape du dossier Import"),
            ("/billing-export/back-office", "passe au Contrôle documentaire Export"),
            ("/billing-export/controle-documentaire", "validez pour passer à la Facturation"),
            ("/billing-export/facturation", "transmettre le dossier à la validation du paiement"),
            ("/billing-export/validation-paiement", "le dossier passe en comptabilité"),
            ("/billing-export/comptabilite", "Dernière étape du dossier Export"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            static string Sql(string texte) => "N'" + texte.Replace("'", "''", StringComparison.Ordinal) + "'";
            var contenus = ContenuAideInitial.Articles.ToDictionary(a => a.Route, StringComparer.OrdinalIgnoreCase);

            foreach (var (route, ancien) in Fiches)
            {
                var article = contenus[route];
                migrationBuilder.Sql(
                    $"UPDATE [ArticlesAide] SET [ContenuFr] = {Sql(article.ContenuFr)}, [ContenuEn] = {Sql(article.ContenuEn)} " +
                    $"WHERE [Route] = {Sql(route)} AND [ContenuFr] LIKE {Sql("%" + ancien + "%")};");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Correction de contenu : pas de retour vers un texte erroné.
        }
    }
}
