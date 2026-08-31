using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutSupportCustomerComplaint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategorieReclamationId",
                table: "Tickets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvaluationCommentaire",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EvaluationDateUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvaluationNote",
                table: "Tickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReponseFormalisee",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CategoriesReclamation",
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
                    table.PrimaryKey("PK_CategoriesReclamation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriesReclamation_Files_FileCibleDefautId",
                        column: x => x.FileCibleDefautId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CategorieReclamationId",
                table: "Tickets",
                column: "CategorieReclamationId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesReclamation_Code",
                table: "CategoriesReclamation",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesReclamation_EstActif",
                table: "CategoriesReclamation",
                column: "EstActif");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriesReclamation_FileCibleDefautId",
                table: "CategoriesReclamation",
                column: "FileCibleDefautId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_CategoriesReclamation_CategorieReclamationId",
                table: "Tickets",
                column: "CategorieReclamationId",
                principalTable: "CategoriesReclamation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_CategoriesReclamation_CategorieReclamationId",
                table: "Tickets");

            migrationBuilder.DropTable(
                name: "CategoriesReclamation");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CategorieReclamationId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CategorieReclamationId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EvaluationCommentaire",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EvaluationDateUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "EvaluationNote",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ReponseFormalisee",
                table: "Tickets");
        }
    }
}
