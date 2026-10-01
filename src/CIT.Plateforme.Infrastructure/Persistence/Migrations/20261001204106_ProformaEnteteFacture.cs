using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProformaEnteteFacture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientAFacturer",
                table: "SimulationsProforma",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Escale",
                table: "SimulationsProforma",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortChargement",
                table: "SimulationsProforma",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortDechargement",
                table: "SimulationsProforma",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeClientLibelle",
                table: "SimulationsProforma",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientAFacturer",
                table: "SimulationsProforma");

            migrationBuilder.DropColumn(
                name: "Escale",
                table: "SimulationsProforma");

            migrationBuilder.DropColumn(
                name: "PortChargement",
                table: "SimulationsProforma");

            migrationBuilder.DropColumn(
                name: "PortDechargement",
                table: "SimulationsProforma");

            migrationBuilder.DropColumn(
                name: "TypeClientLibelle",
                table: "SimulationsProforma");
        }
    }
}
