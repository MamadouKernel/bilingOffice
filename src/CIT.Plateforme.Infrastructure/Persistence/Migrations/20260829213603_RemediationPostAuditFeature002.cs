using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemediationPostAuditFeature002 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypeClient",
                table: "Dossiers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UtilisateursSociete_Societes_SocieteId",
                table: "UtilisateursSociete",
                column: "SocieteId",
                principalTable: "Societes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UtilisateursSociete_Societes_SocieteId",
                table: "UtilisateursSociete");

            migrationBuilder.DropColumn(
                name: "TypeClient",
                table: "Dossiers");
        }
    }
}
