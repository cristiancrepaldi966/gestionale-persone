using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class Progetti3d : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Progetti3d",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Oggetto = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Parametri = table.Column<string>(type: "jsonb", nullable: false),
                    CreatoDaId = table.Column<int>(type: "integer", nullable: true),
                    CreatoIl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AggiornatoIl = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Progetti3d", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Progetti3d_Utenti_CreatoDaId",
                        column: x => x.CreatoDaId,
                        principalTable: "Utenti",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Progetti3d_CreatoDaId",
                table: "Progetti3d",
                column: "CreatoDaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Progetti3d");
        }
    }
}
