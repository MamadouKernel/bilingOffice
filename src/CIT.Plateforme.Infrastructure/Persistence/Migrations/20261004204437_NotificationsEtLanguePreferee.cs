using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificationsEtLanguePreferee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Langue",
                table: "Notifications",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "fr");

            migrationBuilder.AddColumn<string>(
                name: "GabaritCorpsEn",
                table: "ModelesNotification",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GabaritTitreEn",
                table: "ModelesNotification",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguePreferee",
                table: "AspNetUsers",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Langue",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "GabaritCorpsEn",
                table: "ModelesNotification");

            migrationBuilder.DropColumn(
                name: "GabaritTitreEn",
                table: "ModelesNotification");

            migrationBuilder.DropColumn(
                name: "LanguePreferee",
                table: "AspNetUsers");
        }
    }
}
