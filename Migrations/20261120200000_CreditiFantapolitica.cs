using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class CreditiFantapolitica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Costo",
                table: "Politici",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Prezzo",
                table: "PoliticiSquadre",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ImpostazioniFantapolitica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    CreditiPerSquadra = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpostazioniFantapolitica", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ImpostazioniFantapolitica",
                columns: new[] { "Id", "CreditiPerSquadra" },
                values: new object[] { 1, 100 });

            // Costi iniziali per ruolo (anche per i politici aggiunti dal Super admin),
            // poi il prezzo delle squadre già fatte è il costo del politico.
            migrationBuilder.Sql("""
                UPDATE "Politici" SET "Costo" = CASE "Ruolo"
                    WHEN 'Sindaco' THEN 35
                    WHEN 'Vicesindaco' THEN 25
                    WHEN 'Assessore' THEN 20
                    ELSE 10 END;
                UPDATE "PoliticiSquadre" AS ps SET "Prezzo" = p."Costo"
                    FROM "Politici" AS p WHERE p."Id" = ps."PoliticoId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImpostazioniFantapolitica");

            migrationBuilder.DropColumn(
                name: "Prezzo",
                table: "PoliticiSquadre");

            migrationBuilder.DropColumn(
                name: "Costo",
                table: "Politici");
        }
    }
}
