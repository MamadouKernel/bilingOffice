using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReglePrioriteTypeClientReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DONNÉES — le type de client était un texte libre. Casse alignée sur le code du
            // référentiel ; une valeur inconnue ne peut plus être référencée : la règle est alors
            // DÉSACTIVÉE (et non élargie à « tout client ») pour rester sans effet, comme avant.
            migrationBuilder.Sql(@"
UPDATE r SET r.[TypeClient] = t.[Code]
FROM [ReglesPrioriteAutomatique] r JOIN [TypesClient] t ON LTRIM(RTRIM(r.[TypeClient])) = t.[Code];
UPDATE [ReglesPrioriteAutomatique] SET [EstActif] = 0, [TypeClient] = NULL
WHERE [TypeClient] IS NOT NULL AND [TypeClient] NOT IN (SELECT [Code] FROM [TypesClient]);");

            migrationBuilder.AlterColumn<string>(
                name: "TypeClient",
                table: "ReglesPrioriteAutomatique",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReglesPrioriteAutomatique_TypeClient",
                table: "ReglesPrioriteAutomatique",
                column: "TypeClient");

            migrationBuilder.AddForeignKey(
                name: "FK_ReglesPrioriteAutomatique_TypesClient_TypeClient",
                table: "ReglesPrioriteAutomatique",
                column: "TypeClient",
                principalTable: "TypesClient",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReglesPrioriteAutomatique_TypesClient_TypeClient",
                table: "ReglesPrioriteAutomatique");

            migrationBuilder.DropIndex(
                name: "IX_ReglesPrioriteAutomatique_TypeClient",
                table: "ReglesPrioriteAutomatique");

            migrationBuilder.AlterColumn<string>(
                name: "TypeClient",
                table: "ReglesPrioriteAutomatique",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);
        }
    }
}
