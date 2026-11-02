using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class RimuoviDescrizione : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Non perdere le descrizioni già inserite: spostale all'inizio delle note.
            migrationBuilder.Sql("""
                UPDATE "Uscite"
                SET "Note" = left(
                    CASE
                        WHEN "Note" IS NULL OR btrim("Note") = '' THEN "Descrizione"
                        ELSE "Descrizione" || E'\n' || "Note"
                    END, 1000)
                WHERE btrim("Descrizione") <> '';
                """);

            migrationBuilder.DropColumn(
                name: "Descrizione",
                table: "Uscite");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Descrizione",
                table: "Uscite",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
