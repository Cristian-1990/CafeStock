using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeStock.Back.Migrations
{
    /// <inheritdoc />
    public partial class AddColorProveedorYFichaCafe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Proveedores",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Altitud",
                table: "Productos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Finca",
                table: "Productos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NotasCata",
                table: "Productos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pais",
                table: "Productos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Proceso",
                table: "Productos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Productos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Variedad",
                table: "Productos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Color interno de los proveedores que ya existen: uno distinto a cada uno, por orden de
            // Id, con la misma paleta que PaletaProveedores (10 tonos; si hay más, se repite).
            migrationBuilder.Sql(@"
                UPDATE Proveedores
                SET Color = CASE (SELECT COUNT(*) FROM Proveedores p2 WHERE p2.Id < Proveedores.Id) % 10
                    WHEN 0 THEN '#A8481F'
                    WHEN 1 THEN '#3F6B2E'
                    WHEN 2 THEN '#1F5F8B'
                    WHEN 3 THEN '#8E4585'
                    WHEN 4 THEN '#C17817'
                    WHEN 5 THEN '#2E7D7A'
                    WHEN 6 THEN '#B23A48'
                    WHEN 7 THEN '#5D6D2B'
                    WHEN 8 THEN '#6D4C41'
                    ELSE '#4A5FA5'
                END
                WHERE Color = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Altitud",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Finca",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "NotasCata",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Pais",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Proceso",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Variedad",
                table: "Productos");
        }
    }
}
