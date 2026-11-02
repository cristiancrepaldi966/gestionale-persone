using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class ModuliEAutorizzazioni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Autorizzazioni",
                columns: table => new
                {
                    UtenteId = table.Column<int>(type: "integer", nullable: false),
                    Modulo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Livello = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Autorizzazioni", x => new { x.UtenteId, x.Modulo });
                    table.ForeignKey(
                        name: "FK_Autorizzazioni_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Conversione dei ruoli esistenti in autorizzazioni per modulo:
            // amministratori → Amministratore in Uscite e Admin; utenti → Utente in Uscite.
            migrationBuilder.Sql("""
                INSERT INTO "Autorizzazioni" ("UtenteId", "Modulo", "Livello")
                SELECT "Id", 'uscite', CASE WHEN "IsAdmin" THEN 'Amministratore' ELSE 'Utente' END
                FROM "Utenti";

                INSERT INTO "Autorizzazioni" ("UtenteId", "Modulo", "Livello")
                SELECT "Id", 'admin', 'Amministratore'
                FROM "Utenti" WHERE "IsAdmin";
                """);

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Utenti");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Utenti",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Ritorno indietro: amministratore del modulo Admin → IsAdmin.
            migrationBuilder.Sql("""
                UPDATE "Utenti" SET "IsAdmin" = TRUE
                WHERE "Id" IN (SELECT "UtenteId" FROM "Autorizzazioni"
                               WHERE "Modulo" = 'admin' AND "Livello" = 'Amministratore');
                """);

            migrationBuilder.DropTable(
                name: "Autorizzazioni");
        }
    }
}
