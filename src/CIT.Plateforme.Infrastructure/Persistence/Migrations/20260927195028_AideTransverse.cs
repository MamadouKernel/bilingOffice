using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AideTransverse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Transverse",
                table: "ArticlesAide",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Articles de départ déjà installés : les écrans ouverts à tous hors menu.
            // EXEC : dans un script idempotent, SQL Server compile tout le lot avant de l'exécuter, à un
            // moment où la colonne Transverse (ajoutée juste au-dessus) n'existe pas encore.
            migrationBuilder.Sql("EXEC(N'UPDATE [ArticlesAide] SET [Transverse] = 1 WHERE [Route] IN (N''/tickets'', N''/notifications'');');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Transverse",
                table: "ArticlesAide");
        }
    }
}
