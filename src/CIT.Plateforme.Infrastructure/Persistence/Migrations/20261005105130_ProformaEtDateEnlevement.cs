using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProformaEtDateEnlevement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateEnlevementSouhaitee",
                table: "Tickets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EtatProforma",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tickets_EtatProforma",
                table: "Tickets",
                sql: "[EtatProforma] IN (0, 1, 2, 3)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tickets_EtatProforma",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DateEnlevementSouhaitee",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EtatProforma",
                table: "Tickets");
        }
    }
}
