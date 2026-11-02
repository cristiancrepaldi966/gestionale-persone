using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class AnalisiMercato : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParametriMercato",
                columns: table => new
                {
                    Modulo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AggiornatoIl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AggiornatoDaId = table.Column<int>(type: "integer", nullable: true),
                    UtentiIniziali = table.Column<int>(type: "integer", nullable: false),
                    PrezzoMensile = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    QuotaPaganti = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    RicavoPubblicitario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    BudgetMarketing = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CostoPerUtente = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CrescitaOrganica = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Abbandono = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    OreSettimanali = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    CostoOrario = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CostiFissi = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Mesi = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametriMercato", x => x.Modulo);
                    table.ForeignKey(
                        name: "FK_ParametriMercato_Utenti_AggiornatoDaId",
                        column: x => x.AggiornatoDaId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParametriMercato_AggiornatoDaId",
                table: "ParametriMercato",
                column: "AggiornatoDaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParametriMercato");
        }
    }
}
