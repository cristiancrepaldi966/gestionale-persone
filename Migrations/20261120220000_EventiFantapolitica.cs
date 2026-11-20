using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class EventiFantapolitica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PoliticiSquadre diventa lo storico degli acquisti: una riga per acquisto,
            // con il giorno di acquisto e (quando lo si vende) di vendita.
            migrationBuilder.DropPrimaryKey(
                name: "PK_PoliticiSquadre",
                table: "PoliticiSquadre");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PoliticiSquadre",
                type: "integer",
                nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AcquistatoIl",
                table: "PoliticiSquadre",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "CedutoIl",
                table: "PoliticiSquadre",
                type: "date",
                nullable: true);

            // Le squadre già fatte: acquisto il giorno in cui la squadra è stata creata.
            migrationBuilder.Sql("""
                UPDATE "PoliticiSquadre" AS ps SET "AcquistatoIl" = (s."CreatoIl" AT TIME ZONE 'Europe/Rome')::date
                    FROM "Squadre" AS s WHERE s."Id" = ps."SquadraId";
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PoliticiSquadre",
                table: "PoliticiSquadre",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "VociPunteggio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Punti = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VociPunteggio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Eventi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    VocePunteggioId = table.Column<int>(type: "integer", nullable: false),
                    Punti = table.Column<int>(type: "integer", nullable: false),
                    Descrizione = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatoIl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eventi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eventi_VociPunteggio_VocePunteggioId",
                        column: x => x.VocePunteggioId,
                        principalTable: "VociPunteggio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EventiPolitici",
                columns: table => new
                {
                    EventoId = table.Column<int>(type: "integer", nullable: false),
                    PoliticoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventiPolitici", x => new { x.EventoId, x.PoliticoId });
                    table.ForeignKey(
                        name: "FK_EventiPolitici_Eventi_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventiPolitici_Politici_PoliticoId",
                        column: x => x.PoliticoId,
                        principalTable: "Politici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "VociPunteggio",
                columns: new[] { "Id", "Nome", "Punti" },
                values: new object[,]
                {
                    { 1, "Intervento in consiglio", 2 },
                    { 2, "Mozione o interrogazione presentata", 3 },
                    { 3, "Mozione approvata", 5 },
                    { 4, "Articolo sui giornali locali", 1 },
                    { 5, "Inaugurazione o evento pubblico", 2 },
                    { 6, "Assenza in consiglio", -2 },
                    { 7, "Polemica o gaffe", -3 },
                    { 8, "Cambio di partito o lista", -5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoliticiSquadre_SquadraId_PoliticoId",
                table: "PoliticiSquadre",
                columns: new[] { "SquadraId", "PoliticoId" },
                unique: true,
                filter: "\"CedutoIl\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Eventi_Data",
                table: "Eventi",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_Eventi_VocePunteggioId",
                table: "Eventi",
                column: "VocePunteggioId");

            migrationBuilder.CreateIndex(
                name: "IX_EventiPolitici_PoliticoId",
                table: "EventiPolitici",
                column: "PoliticoId");

            // I dati iniziali hanno id scelti a mano: la numerazione automatica riparte dopo l'ultimo.
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"VociPunteggio\"', 'Id'), (SELECT MAX(\"Id\") FROM \"VociPunteggio\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventiPolitici");

            migrationBuilder.DropTable(
                name: "Eventi");

            migrationBuilder.DropTable(
                name: "VociPunteggio");

            migrationBuilder.DropIndex(
                name: "IX_PoliticiSquadre_SquadraId_PoliticoId",
                table: "PoliticiSquadre");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PoliticiSquadre",
                table: "PoliticiSquadre");

            // Lo storico si perde: restano solo i politici ancora in squadra.
            migrationBuilder.Sql("DELETE FROM \"PoliticiSquadre\" WHERE \"CedutoIl\" IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PoliticiSquadre");

            migrationBuilder.DropColumn(
                name: "AcquistatoIl",
                table: "PoliticiSquadre");

            migrationBuilder.DropColumn(
                name: "CedutoIl",
                table: "PoliticiSquadre");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PoliticiSquadre",
                table: "PoliticiSquadre",
                columns: new[] { "SquadraId", "PoliticoId" });
        }
    }
}
