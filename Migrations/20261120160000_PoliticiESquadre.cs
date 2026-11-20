using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class PoliticiESquadre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Politici",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cognome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Ruolo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Partito = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Politici", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Squadre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LegaId = table.Column<int>(type: "integer", nullable: false),
                    UtenteId = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatoIl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AggiornatoIl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Squadre", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Squadre_MembriLeghe_LegaId_UtenteId",
                        columns: x => new { x.LegaId, x.UtenteId },
                        principalTable: "MembriLeghe",
                        principalColumns: new[] { "LegaId", "UtenteId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Squadre_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoliticiSquadre",
                columns: table => new
                {
                    SquadraId = table.Column<int>(type: "integer", nullable: false),
                    PoliticoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticiSquadre", x => new { x.SquadraId, x.PoliticoId });
                    table.ForeignKey(
                        name: "FK_PoliticiSquadre_Politici_PoliticoId",
                        column: x => x.PoliticoId,
                        principalTable: "Politici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PoliticiSquadre_Squadre_SquadraId",
                        column: x => x.SquadraId,
                        principalTable: "Squadre",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Politici",
                columns: new[] { "Id", "Cognome", "Nome", "Partito", "Ruolo" },
                values: new object[,]
                {
                    { 1, "Barbujani", "Massimo", "Lista Bobo", "Sindaco" },
                    { 2, "Simoni", "Federico", null, "Vicesindaco" },
                    { 3, "Baratella", "Giselda", null, "Assessore" },
                    { 4, "Crepaldi", "Giorgio", null, "Assessore" },
                    { 5, "D'Angelo", "Giorgio", "Lega", "Assessore" },
                    { 6, "Paccagnella", "Vittoria", null, "Assessore" },
                    { 7, "Ravagnan", "Antonella", null, "Assessore" },
                    { 8, "Barbierato", "Omar", "Impegno per il bene comune", "Consigliere" },
                    { 9, "Bonato", "Enrico", "Impegno per il bene comune", "Consigliere" },
                    { 10, "Moda", "Sandra", "Impegno per il bene comune", "Consigliere" },
                    { 11, "Zanellato", "Giorgio", "PD", "Consigliere" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PoliticiSquadre_PoliticoId",
                table: "PoliticiSquadre",
                column: "PoliticoId");

            migrationBuilder.CreateIndex(
                name: "IX_Squadre_LegaId_UtenteId",
                table: "Squadre",
                columns: new[] { "LegaId", "UtenteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Squadre_UtenteId",
                table: "Squadre",
                column: "UtenteId");

            // I dati iniziali hanno id scelti a mano: la numerazione automatica riparte dopo l'ultimo.
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"Politici\"', 'Id'), (SELECT MAX(\"Id\") FROM \"Politici\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PoliticiSquadre");

            migrationBuilder.DropTable(
                name: "Politici");

            migrationBuilder.DropTable(
                name: "Squadre");
        }
    }
}
