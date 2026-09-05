using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutSeuilAnnulationParWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EtapesBloquantesAnnulation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TypeDemande = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Motif = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EstActif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapesBloquantesAnnulation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EtapesBloquantesAnnulation_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EtapesBloquantesAnnulation_FileId",
                table: "EtapesBloquantesAnnulation",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_EtapesBloquantesAnnulation_Module_TypeDemande_FileId",
                table: "EtapesBloquantesAnnulation",
                columns: new[] { "Module", "TypeDemande", "FileId" },
                unique: true,
                filter: "[TypeDemande] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EtapesBloquantesAnnulation");
        }
    }
}
