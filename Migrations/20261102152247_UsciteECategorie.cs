using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class UsciteECategorie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Persone");

            migrationBuilder.CreateTable(
                name: "Categorie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorie", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Uscite",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Importo = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Descrizione = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false),
                    CreatoDaId = table.Column<int>(type: "integer", nullable: true),
                    CreatoIl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uscite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Uscite_Categorie_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorie",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Uscite_Utenti_CreatoDaId",
                        column: x => x.CreatoDaId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "Categorie",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { 1, "Casa" },
                    { 2, "Spesa alimentare" },
                    { 3, "Bollette" },
                    { 4, "Trasporti" },
                    { 5, "Salute" },
                    { 6, "Svago" },
                    { 7, "Altro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categorie_Nome",
                table: "Categorie",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uscite_CategoriaId",
                table: "Uscite",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Uscite_CreatoDaId",
                table: "Uscite",
                column: "CreatoDaId");

            migrationBuilder.CreateIndex(
                name: "IX_Uscite_Data",
                table: "Uscite",
                column: "Data");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Uscite");

            migrationBuilder.DropTable(
                name: "Categorie");

            migrationBuilder.CreateTable(
                name: "Persone",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cognome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persone", x => x.Id);
                });
        }
    }
}
