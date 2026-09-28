using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CIT.Plateforme.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AideAssistantDonnees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La fiche « Assistant virtuel » affirmait que l'assistant ne voit pas les dossiers, ce
            // qui n'est plus vrai. Mise à jour UNIQUEMENT si la fiche contient encore cette phrase :
            // une fiche déjà réécrite par l'administration n'est pas touchée.
            string Sql(string texte) => "N'" + texte.Replace("'", "''", StringComparison.Ordinal) + "'";
            var fr = string.Join("\n",
                "Posez une question en langage courant : l'assistant répond à partir de VOS dossiers et de la base de réponses de CIT.",
                "- « Où en est le dossier IMP-000123 ? » (ou un n° de BL, de booking) : état, étape et lien vers le dossier.",
                "- « Qu'attend-on de moi ? » : les dossiers pour lesquels CIT attend votre réponse.",
                "- « Où en sont mes dossiers ? » et « Quand est mon rendez-vous ? ».",
                "! L'assistant ne voit que les dossiers que vous pouvez déjà ouvrir, et il ne modifie jamais rien.");
            var en = string.Join("\n",
                "Ask a question in plain language: the assistant answers from YOUR files and from CIT's answer base.",
                "- \"Where is file IMP-000123?\" (or a B/L or booking number): status, step and a link to the file.",
                "- \"What do you need from me?\": the files for which CIT is waiting for your reply.",
                "- \"Where are my files?\" and \"When is my appointment?\".",
                "! The assistant only sees the files you can already open, and it never changes anything.");
            migrationBuilder.Sql(
                $"UPDATE [ArticlesAide] SET [ContenuFr] = {Sql(fr)}, [ContenuEn] = {Sql(en)} " +
                "WHERE [Route] = N'/assistant' AND [ContenuFr] LIKE N'%ne voit pas vos dossiers%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
