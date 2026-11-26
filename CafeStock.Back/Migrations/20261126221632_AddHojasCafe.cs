using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeStock.Back.Migrations
{
    /// <inheritdoc />
    public partial class AddHojasCafe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HojaId",
                table: "RegistrosCafe",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaqueteId",
                table: "RegistrosCafe",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HojasCafe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechaApertura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HojasCafe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaquetesCafe",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Grupo = table.Column<int>(type: "INTEGER", nullable: false),
                    Formato = table.Column<int>(type: "INTEGER", nullable: false),
                    HojaOrigenId = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaTueste = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RecepcionadoPor = table.Column<string>(type: "TEXT", nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaquetesCafe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaquetesCafe_HojasCafe_HojaOrigenId",
                        column: x => x.HojaOrigenId,
                        principalTable: "HojasCafe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaquetesCafe_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCafe_HojaId",
                table: "RegistrosCafe",
                column: "HojaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCafe_PaqueteId",
                table: "RegistrosCafe",
                column: "PaqueteId");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesCafe_HojaOrigenId",
                table: "PaquetesCafe",
                column: "HojaOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_PaquetesCafe_ProductoId",
                table: "PaquetesCafe",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosCafe_HojasCafe_HojaId",
                table: "RegistrosCafe",
                column: "HojaId",
                principalTable: "HojasCafe",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosCafe_PaquetesCafe_PaqueteId",
                table: "RegistrosCafe",
                column: "PaqueteId",
                principalTable: "PaquetesCafe",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosCafe_HojasCafe_HojaId",
                table: "RegistrosCafe");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosCafe_PaquetesCafe_PaqueteId",
                table: "RegistrosCafe");

            migrationBuilder.DropTable(
                name: "PaquetesCafe");

            migrationBuilder.DropTable(
                name: "HojasCafe");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosCafe_HojaId",
                table: "RegistrosCafe");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosCafe_PaqueteId",
                table: "RegistrosCafe");

            migrationBuilder.DropColumn(
                name: "HojaId",
                table: "RegistrosCafe");

            migrationBuilder.DropColumn(
                name: "PaqueteId",
                table: "RegistrosCafe");
        }
    }
}
