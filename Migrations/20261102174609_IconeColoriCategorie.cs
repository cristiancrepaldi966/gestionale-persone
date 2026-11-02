using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionalePersone.Migrations
{
    /// <inheritdoc />
    public partial class IconeColoriCategorie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Colore",
                table: "Categorie",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#6c757d");

            migrationBuilder.AddColumn<string>(
                name: "Icona",
                table: "Categorie",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "tag");

            migrationBuilder.UpdateData(
                table: "Categorie",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Colore", "Icona" },
                values: new object[] { "#2a78d6", "house-door" });

            migrationBuilder.UpdateData(
                table: "Categorie",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Colore", "Icona" },
                values: new object[] { "#eb6834", "cart3" });

            migrationBuilder.UpdateData(
                table: "Categorie",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Colore", "Icona" },
                values: new object[] { "#eda100", "lightning-charge" });

            migrationBuilder.UpdateData(
                table: "Categorie",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Colore", "Icona" },
                values: new object[] { "#1baf7a", "car-front" });

            migrationBuilder.UpdateData(
                table: "Categorie",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Colore", "Icona" },
                values: new object[] { "#e34948", "heart-pulse" });

            migrationBuilder.UpdateData(
                table: "Categorie",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Colore", "Icona" },
                values: new object[] { "#e87ba4", "controller" });

            migrationBuilder.UpdateData(
                table: "Categorie",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Colore", "Icona" },
                values: new object[] { "#6c757d", "tag" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Colore",
                table: "Categorie");

            migrationBuilder.DropColumn(
                name: "Icona",
                table: "Categorie");
        }
    }
}
