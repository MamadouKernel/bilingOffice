using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutMatriceTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransitionsAutorisees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileCibleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    motifsAutorises = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransitionsAutorisees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransitionsAutorisees_FileSourceId_FileCibleId",
                table: "TransitionsAutorisees",
                columns: new[] { "FileSourceId", "FileCibleId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransitionsAutorisees");
        }
    }
}
