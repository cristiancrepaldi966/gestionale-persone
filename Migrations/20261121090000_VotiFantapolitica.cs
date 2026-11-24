using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class VotiFantapolitica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Gli eventi diventano "giornate": un titolo, il voto di ogni politico e i bonus e
            // malus del listino dati ai singoli politici.
            migrationBuilder.CreateTable(
                name: "BonusEventi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventoId = table.Column<int>(type: "integer", nullable: false),
                    PoliticoId = table.Column<int>(type: "integer", nullable: false),
                    VocePunteggioId = table.Column<int>(type: "integer", nullable: false),
                    Punti = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusEventi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonusEventi_Eventi_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BonusEventi_Politici_PoliticoId",
                        column: x => x.PoliticoId,
                        principalTable: "Politici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BonusEventi_VociPunteggio_VocePunteggioId",
                        column: x => x.VocePunteggioId,
                        principalTable: "VociPunteggio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VotiEventi",
                columns: table => new
                {
                    EventoId = table.Column<int>(type: "integer", nullable: false),
                    PoliticoId = table.Column<int>(type: "integer", nullable: false),
                    Voto = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VotiEventi", x => new { x.EventoId, x.PoliticoId });
                    table.ForeignKey(
                        name: "FK_VotiEventi_Eventi_EventoId",
                        column: x => x.EventoId,
                        principalTable: "Eventi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VotiEventi_Politici_PoliticoId",
                        column: x => x.PoliticoId,
                        principalTable: "Politici",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<string>(
                name: "Titolo",
                table: "Eventi",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            // Eventi già registrati: il titolo è il nome della voce, e la voce diventa un bonus
            // (o malus) per ogni politico coinvolto, senza voto.
            migrationBuilder.Sql("""
                UPDATE "Eventi" AS e SET "Titolo" = v."Nome"
                    FROM "VociPunteggio" AS v WHERE v."Id" = e."VocePunteggioId";
                INSERT INTO "BonusEventi" ("EventoId", "PoliticoId", "VocePunteggioId", "Punti")
                    SELECT ep."EventoId", ep."PoliticoId", e."VocePunteggioId", e."Punti"
                    FROM "EventiPolitici" AS ep JOIN "Eventi" AS e ON e."Id" = ep."EventoId";
                """);

            migrationBuilder.DropTable(
                name: "EventiPolitici");

            migrationBuilder.DropForeignKey(
                name: "FK_Eventi_VociPunteggio_VocePunteggioId",
                table: "Eventi");

            migrationBuilder.DropIndex(
                name: "IX_Eventi_VocePunteggioId",
                table: "Eventi");

            migrationBuilder.DropColumn(
                name: "VocePunteggioId",
                table: "Eventi");

            migrationBuilder.DropColumn(
                name: "Punti",
                table: "Eventi");

            migrationBuilder.CreateIndex(
                name: "IX_BonusEventi_EventoId",
                table: "BonusEventi",
                column: "EventoId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusEventi_PoliticoId",
                table: "BonusEventi",
                column: "PoliticoId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusEventi_VocePunteggioId",
                table: "BonusEventi",
                column: "VocePunteggioId");

            migrationBuilder.CreateIndex(
                name: "IX_VotiEventi_PoliticoId",
                table: "VotiEventi",
                column: "PoliticoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Si torna a un evento = una voce: i voti si perdono, e di ogni evento resta solo
            // la prima voce usata.
            migrationBuilder.AddColumn<int>(
                name: "VocePunteggioId",
                table: "Eventi",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Punti",
                table: "Eventi",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.Sql("""
                UPDATE "Eventi" AS e SET "VocePunteggioId" = b."VocePunteggioId", "Punti" = b."Punti"
                    FROM (SELECT DISTINCT ON ("EventoId") "EventoId", "VocePunteggioId", "Punti"
                          FROM "BonusEventi" ORDER BY "EventoId", "Id") AS b
                    WHERE b."EventoId" = e."Id";
                DELETE FROM "Eventi" WHERE "VocePunteggioId" IS NULL;
                INSERT INTO "EventiPolitici" ("EventoId", "PoliticoId")
                    SELECT DISTINCT b."EventoId", b."PoliticoId" FROM "BonusEventi" AS b
                    JOIN "Eventi" AS e ON e."Id" = b."EventoId" AND e."VocePunteggioId" = b."VocePunteggioId";
                """);

            migrationBuilder.AlterColumn<int>(
                name: "VocePunteggioId",
                table: "Eventi",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "BonusEventi");

            migrationBuilder.DropTable(
                name: "VotiEventi");

            migrationBuilder.DropColumn(
                name: "Titolo",
                table: "Eventi");

            migrationBuilder.CreateIndex(
                name: "IX_Eventi_VocePunteggioId",
                table: "Eventi",
                column: "VocePunteggioId");

            migrationBuilder.CreateIndex(
                name: "IX_EventiPolitici_PoliticoId",
                table: "EventiPolitici",
                column: "PoliticoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Eventi_VociPunteggio_VocePunteggioId",
                table: "Eventi",
                column: "VocePunteggioId",
                principalTable: "VociPunteggio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
