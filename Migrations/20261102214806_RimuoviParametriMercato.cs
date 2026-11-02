using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <summary>
    /// Toglie la tabella del modulo Analisi di mercato (annullato su main) e la sua
    /// migrazione dall'elenco di quelle applicate: se il modulo tornerà, la sua
    /// migrazione ricreerà la tabella da sola.
    /// </summary>
    public partial class RimuoviParametriMercato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"ParametriMercato\";");
            migrationBuilder.Sql("DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20261102210513_AnalisiMercato';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Niente da ripristinare: la tabella appartiene al modulo, che la ricrea con la sua migrazione.
        }
    }
}
