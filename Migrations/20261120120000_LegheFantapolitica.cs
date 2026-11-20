using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class LegheFantapolitica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leghe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatoDaId = table.Column<int>(type: "integer", nullable: true),
                    CreatoIl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leghe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leghe_Utenti_CreatoDaId",
                        column: x => x.CreatoDaId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MembriLeghe",
                columns: table => new
                {
                    LegaId = table.Column<int>(type: "integer", nullable: false),
                    UtenteId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembriLeghe", x => new { x.LegaId, x.UtenteId });
                    table.ForeignKey(
                        name: "FK_MembriLeghe_Leghe_LegaId",
                        column: x => x.LegaId,
                        principalTable: "Leghe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MembriLeghe_Utenti_UtenteId",
                        column: x => x.UtenteId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leghe_CreatoDaId",
                table: "Leghe",
                column: "CreatoDaId");

            migrationBuilder.CreateIndex(
                name: "IX_MembriLeghe_UtenteId",
                table: "MembriLeghe",
                column: "UtenteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MembriLeghe");

            migrationBuilder.DropTable(
                name: "Leghe");
        }
    }
}
