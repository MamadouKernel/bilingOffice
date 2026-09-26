using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutIncidentTechnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategorieIncidentId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gravite",
                table: "Tickets",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriesIncident",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileCibleDefautId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriesIncident", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriesIncident_Files_FileCibleDefautId",
                        column: x => x.FileCibleDefautId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CategorieIncidentId",
                table: "Tickets",
                column: "CategorieIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesIncident_Code",
                table: "CategoriesIncident",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesIncident_EstActif",
                table: "CategoriesIncident",
                column: "EstActif");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesIncident_FileCibleDefautId",
                table: "CategoriesIncident",
                column: "FileCibleDefautId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_CategoriesIncident_CategorieIncidentId",
                table: "Tickets",
                column: "CategorieIncidentId",
                principalTable: "CategoriesIncident",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_CategoriesIncident_CategorieIncidentId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "CategoriesIncident");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CategorieIncidentId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CategorieIncidentId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "Gravite",
                table: "Tickets");
        }
    }
}
