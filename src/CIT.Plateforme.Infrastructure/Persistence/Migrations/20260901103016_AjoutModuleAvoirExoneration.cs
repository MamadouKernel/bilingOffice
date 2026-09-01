using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutModuleAvoirExoneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateDecisionAvoirExoUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DecideurAvoirExoId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionAvoirExo",
                table: "Tickets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantAccorde",
                table: "Tickets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantConteste",
                table: "Tickets",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotifRejetAvoirExo",
                table: "Tickets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Module_TypeDemande_Statut",
                table: "Tickets",
                columns: new[] { "Module", "TypeDemande", "Statut" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Module_TypeDemande_Statut",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DateDecisionAvoirExoUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DecideurAvoirExoId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DecisionAvoirExo",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MontantAccorde",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MontantConteste",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "MotifRejetAvoirExo",
                table: "Tickets");
        }
    }
}
