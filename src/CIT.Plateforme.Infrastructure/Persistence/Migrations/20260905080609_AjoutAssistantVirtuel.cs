using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AjoutAssistantVirtuel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationsAssistant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UtilisateurId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateOuvertureUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateDernierMessageUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstEscaladee = table.Column<bool>(type: "bit", nullable: false),
                    DateEscaladeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotifEscalade = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationsAssistant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationsAssistant_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessagesAssistant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationAssistantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Auteur = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Contenu = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    HorodatageUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rang = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagesAssistant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagesAssistant_ConversationsAssistant_ConversationAssistantId",
                        column: x => x.ConversationAssistantId,
                        principalTable: "ConversationsAssistant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationsAssistant_TicketId",
                table: "ConversationsAssistant",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationsAssistant_UtilisateurId_EstEscaladee",
                table: "ConversationsAssistant",
                columns: new[] { "UtilisateurId", "EstEscaladee" });

            migrationBuilder.CreateIndex(
                name: "IX_MessagesAssistant_ConversationAssistantId_Rang",
                table: "MessagesAssistant",
                columns: new[] { "ConversationAssistantId", "Rang" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessagesAssistant");

            migrationBuilder.DropTable(
                name: "ConversationsAssistant");
        }
    }
}
