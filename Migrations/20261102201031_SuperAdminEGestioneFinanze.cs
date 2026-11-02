using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class SuperAdminEGestioneFinanze : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SuperAdmin",
                table: "Utenti",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Chi era Amministratore del modulo Admin diventa Super admin; il modulo Admin
            // non si assegna più per persona. Il modulo "uscite" si chiama ora "finanze"
            // (Gestione finanze).
            migrationBuilder.Sql("""
                UPDATE "Utenti" SET "SuperAdmin" = TRUE
                WHERE "Id" IN (SELECT "UtenteId" FROM "Autorizzazioni"
                               WHERE "Modulo" = 'admin' AND "Livello" = 'Amministratore');

                DELETE FROM "Autorizzazioni" WHERE "Modulo" = 'admin';

                UPDATE "Autorizzazioni" SET "Modulo" = 'finanze' WHERE "Modulo" = 'uscite';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ritorno indietro: finanze → uscite, Super admin → Amministratore del modulo Admin.
            migrationBuilder.Sql("""
                UPDATE "Autorizzazioni" SET "Modulo" = 'uscite' WHERE "Modulo" = 'finanze';

                INSERT INTO "Autorizzazioni" ("UtenteId", "Modulo", "Livello")
                SELECT "Id", 'admin', 'Amministratore' FROM "Utenti" WHERE "SuperAdmin";
                """);

            migrationBuilder.DropColumn(
                name: "SuperAdmin",
                table: "Utenti");
        }
    }
}
