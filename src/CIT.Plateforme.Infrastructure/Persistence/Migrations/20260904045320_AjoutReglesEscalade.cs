using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutReglesEscalade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReglesEscalade",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TypeDemande = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrioriteCibleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstActive = table.Column<bool>(type: "bit", nullable: false),
                    Ordre = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglesEscalade", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReglesEscalade_Portee",
                table: "ReglesEscalade",
                columns: new[] { "Module", "TypeDemande", "FileId" },
                filter: "[EstActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReglesEscalade");
        }
    }
}
